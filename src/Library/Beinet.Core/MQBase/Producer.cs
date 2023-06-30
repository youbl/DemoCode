using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Beinet.Core.Reflection;
using NLog;

namespace Beinet.Core.MQBase
{
    /// <summary>
    /// 生产者实现类
    /// </summary>
    public class Producer : IMqProducer, IDisposable
    {
        private static ILogger log = LogManager.GetCurrentClassLogger();

        public static Producer DEFAULT = new Producer();


        #region 非公共属性

        /// <summary>
        /// 储存生产的消息
        /// </summary>
        protected ConcurrentQueue<Message> messages { get; } = new ConcurrentQueue<Message>();

        /// <summary>
        /// 信号量，用于阻塞消费者分发线程.
        /// 一开始没有数据，所以用0初始化
        /// </summary>
        protected SemaphoreSlim semaphore { get; } = new SemaphoreSlim(0);

        /// <summary>
        /// 所有消息类型的消费者对象集合
        /// </summary>
        protected List<IMqConsumer> consumerAll { get; } = new List<IMqConsumer>();

        /// <summary>
        /// 指定消息类型的消费者对象收集
        /// </summary>
        protected Dictionary<Type, List<IMqConsumer>> consumerDic { get; } = new Dictionary<Type, List<IMqConsumer>>();

        #endregion


        /// <summary>
        /// 
        /// </summary>
        public Producer()
        {
            // 启动消息分发线程
            Task.Factory.StartNew(WaitForDelivery);
        }

        /// <summary>
        /// 生产消息入队
        /// </summary>
        /// <param name="msgs"></param>
        /// <returns></returns>
        public void Publish(params object[] msgs)
        {
            // SemaphoreSlim sss = new SemaphoreSlim(1, 1);

            foreach (var msg in msgs)
            {
                var item = new Message {Data = msg};
                messages.Enqueue(item);
                // 通知有数据了
                semaphore.Release();

                // 这里也可以不走上面的信号量和队列，直接分发，但是可能阻塞生产者主线程
                // DeliveryMsg(item);
            }
        }


        /// <summary>
        /// 监听消息，并进行投递
        /// </summary>
        /// <returns></returns>
        public void WaitForDelivery()
        {
            while (true)
            {
                semaphore.Wait(); // 等待信号量
                log.Debug("收到信号量");
                if (!messages.TryDequeue(out var msg))
                    continue;

                DeliveryMsg(msg);
            }
            // ReSharper disable once FunctionNeverReturns
        }

        /// <summary>
        /// 在指定的组件里查找消费者接口实现类，并进行注册
        /// </summary>
        /// <param name="assemblyName">为空表示当前组件</param>
        public void Register(string assemblyName = null)
        {
            Assembly ass;
            if (assemblyName == null || (assemblyName = assemblyName.Trim()).Length == 0)
            {
                ass = Assembly.GetCallingAssembly();
                // throw new ArgumentException("参数不能为空", nameof(assemblyName));
            }
            else
            {
                ass = GetAssembly(assemblyName);
            }

            if (ass == null)
            {
                throw new ArgumentException($"Assembly: {assemblyName} 无法找到。");
            }

            RegisterAssembly(ass);
        }

        /// <summary>
        /// 为所有的消息类型注册消费者
        /// </summary>
        /// <param name="consumer"></param>
        public void Register(IMqConsumer consumer)
        {
            CheckConsumer(consumer);
            lock (consumerAll)
            {
                consumerAll.Add(consumer);
            }

            log.Info($"类型 {consumer} 注册成功");
        }

        /// <summary>
        /// 为指定消息类型注册消费者
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="consumer"></param>
        public void Register<T>(IMqConsumer<T> consumer)
        {
            var type = typeof(T);
            RegisterType(type, consumer);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            semaphore.Dispose();
        }


        #region 非公共方法

        /// <summary>
        /// 遍历所有消费者，进行分发处理
        /// </summary>
        /// <param name="msg"></param>
        protected void DeliveryMsg(Message msg)
        {
            var consumers = new List<IMqConsumer>();
            // 这里不对 consumerAll 和 consumerDic 进行加锁，
            // 因为注册 理论上不会经常发生,不考虑冲突
            // ReSharper disable once InconsistentlySynchronizedField
            consumers.AddRange(consumerAll);

            var type = msg.Data.GetType();
            // ReSharper disable once InconsistentlySynchronizedField
            if (consumerDic.TryGetValue(type, out var consumerType) &&
                consumerType != null)
            {
                consumers.AddRange(consumerType);
            }

            if (consumers.Count == 0)
            {
                return;
            }

            // 多线程并行执行，线程安全性和顺序由消费者自行保障
            Parallel.ForEach(consumers, consumer => consumer.Process(msg));
        }

        /// <summary>
        /// 根据名称查找组件
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        protected Assembly GetAssembly(string name)
        {
            var assessName = new AssemblyName(name);
            var ass = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(x => x.GetName() == assessName);
            if (ass == null)
                ass = Assembly.Load(assessName);
            return ass;
        }


        /// <summary>
        /// 为指定消息类型注册消费者
        /// </summary>
        /// <param name="type"></param>
        /// <param name="consumer"></param>
        protected void RegisterType(Type type, IMqConsumer consumer)
        {
            CheckConsumer(consumer);
            if (type == typeof(object))
            {
                throw new ArgumentException("object类型请改用Register非泛型方法");
            }

            lock (consumerDic)
            {
                if (!consumerDic.TryGetValue(type, out var consumers))
                {
                    consumers = new List<IMqConsumer>();
                    consumerDic.Add(type, consumers);
                }

                consumers.Add(consumer);
            }

            log.Info($"{type} {consumer} 注册成功");
        }

        /// <summary>
        /// 从指定组件里查找 实现消费者接口的类进行注册和实例化
        /// </summary>
        /// <param name="ass"></param>
        internal void RegisterAssembly(Assembly ass)
        {
            var types = TypeHelper.GetLoadableTypes(ass);
            var constructParams = new Type[] { };
            var constructParamsObj = new object[] { };

            // 找到IMqConsumer的实现类, 且有无参构造函数的类
            var consumerType = typeof(IMqConsumer);
            var subTypes = types.Where(x =>
                consumerType.IsAssignableFrom(x) && x.GetConstructor(constructParams) != null);
            // 创建实例，并添加注册
            var tempResult = subTypes
                .Select(x => (IMqConsumer) (x.GetConstructor(constructParams)?.Invoke(constructParamsObj))).ToList();

            // 区分出继承泛型接口和非泛型接口的实例，分开注册
            var consumerTType = typeof(IMqConsumer<>);
            foreach (var consumer in tempResult)
            {
                Type genericParaType = consumer.GetType().GetInterfaces().FirstOrDefault(tp =>
                    tp.IsGenericType && tp.GetGenericTypeDefinition() == consumerTType);
                if (genericParaType == null)
                {
                    Register(consumer);
                    continue;
                }

                var genericArgs = genericParaType.GetGenericArguments();
                if (genericArgs.Length > 0)
                {
                    RegisterType(genericArgs[0], consumer);
                }
                else
                {
                    Register(consumer);
                }
            }
        }

        /// <summary>
        /// 检查消费者实例
        /// </summary>
        /// <param name="consumer"></param>
        protected void CheckConsumer(IMqConsumer consumer)
        {
            if (consumer == null)
            {
                throw new ArgumentException("参数不能为空", nameof(consumer));
            }
        }

        #endregion
    }
}