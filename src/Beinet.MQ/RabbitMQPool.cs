using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Beinet.Core.FileExt;
using Beinet.Core.Logging;
using Beinet.Core.Serializer;
using NLog;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace Beinet.MQ
{
    /// <summary>
    /// RabbitMQ连接池管理，参数配置建议：
    /// &lt;add key="DefaultRabbitMqConn" value="amqp://aa:bb@10.2.0.174:5672" /&gt;
    /// </summary>
    public class RabbitMQPool : IDisposable
    {
        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

        private static readonly Encoding Utf8 = FileHelper.UTF8_NoBom;
        /// <summary>
        /// 根据服务器信息，存储所有的连接池，避免同一服务器创建多个连接
        /// </summary>
        private static readonly Dictionary<string, RabbitMQPool> _pools;

        /// <summary>
        /// 默认连接池名
        /// </summary>
        private static readonly string _defaultKey = "default";

        private static ISerializer _defaultSerial = new JsonSerializer();

        #region 静态构造函数 与 私有构造函数
        /// <summary>
        /// 创建默认的消息队列实例
        /// </summary>
        static RabbitMQPool()
        {
            _pools = new Dictionary<string, RabbitMQPool>(1);
        }

        /// <summary>
        /// 队列池构造函数
        /// </summary>
        /// <param name="config">服务器配置信息</param>
        private RabbitMQPool(RabbitMQConfigSection config)
        {
            Init(config);
        }

        #endregion


        #region 静态方法 ，暴露给外部用的获取连接池实例

        /// <summary>
        /// 根据Config配置，获取默认实例.
        /// 注：配置里的密码必须是明文，暂不支持加密
        /// </summary>
        public static RabbitMQPool Default
        {
            get
            {
                RabbitMQPool defaultPool;
                lock (_pools)
                {
                    if (!_pools.TryGetValue(_defaultKey, out defaultPool))
                    {
                        // 按ConfigSection方式配置
                        if (ConfigurationManager.GetSection("RabbitMQConfigSection") is RabbitMQConfigSection config)
                        {
                            defaultPool = new RabbitMQPool(config);
                            _pools[_defaultKey] = defaultPool;
                        }
                        else
                        {

                            //var mqConnstr = ConfigurationManager.AppSettings["DefaultRabbitMqConn"];
                            var mqConnstr = ConfigurationManager.AppSettings["DefaultRabbitMqConn"];
                            if (!string.IsNullOrEmpty(mqConnstr))
                            {
                                config = ParseConnectStr(mqConnstr);
                                config.HeartBeatSecond = 10;

                                defaultPool = new RabbitMQPool(config);
                                _pools[_defaultKey] = defaultPool;
                            }
                        }
                        if (defaultPool == null)
                        {
                            throw new ConfigurationErrorsException("必添加DefaultRabbitMqConn配置或RabbitMQConfigSection子节点");
                        }
                    }
                }
                return defaultPool;
            }
        }

        /// <summary> 
        /// 传入一个连接串的队列池构造函数
        /// </summary>
        /// <param name="constr">格式参考：amqp://aa:bb@10.2.0.174:5672/
        /// 注：连接串里的password必须是明文密码</param>
        /// <param name="heartBeatSecond">mq心跳时长，默认10秒</param>
        /// <param name="connectionName">mq连接的命名</param>
        /// <returns></returns>
        public static RabbitMQPool CreatePool(string constr, int heartBeatSecond = 10, string connectionName = "")
        {
            if (string.IsNullOrEmpty(constr))
            {
                throw new ArgumentException("参数不能为空");
            }
            var config = ParseConnectStr(constr);
            config.HeartBeatSecond = heartBeatSecond;
            if (!string.IsNullOrEmpty(connectionName))
                config.ConnectionName = connectionName;

            RabbitMQPool ret;
            string key = config.ServerIp + ":" + config.ServerPort.ToString() + ":" + config.UserName + ":" +
                         config.Password + ":" + heartBeatSecond.ToString();

            lock (_pools)
            {
                if (!_pools.TryGetValue(key, out ret))
                {
                    ret = new RabbitMQPool(config);
                    _pools[key] = ret;
                }
            }
            return ret;
        }

        #endregion



        #region 实例方法，实例创建队列和发送接收消息的方法

        /// <summary>
        /// 获取一个可用连接，并返回一个可以出入队的通道
        /// </summary>
        /// <returns></returns>
        public IModel GetChannel()
        {
            if (_connection == null)// || !_connection.IsOpen)
            {
                _connection = CreateNewConnection(_config);
                // throw new ApplicationException("MQ连接异常");
            }
            return _connection.CreateModel();
        }


        #region 定义交换器和队列
        /// <summary>
        /// 定义Exchange(如果没有Exchange，消息无法发布入队)
        /// 可以在Application_Start中调用此方法，也可以直接通过RabbitMQ的WEB控制台预先定义好
        /// </summary>
        /// <param name="exchange">要定义的交换器名</param>
        /// <param name="exchangeType">fanout：广播消息给Exchage绑定的所有队列；
        /// direct：直接转发给RouteKey指定的队列；
        /// topic：转发给匹配主题的队列；
        /// headers：尚未研究</param>
        /// <param name="durable">是否持久化</param>
        /// <param name="autoDelete">是否自动删除.
        /// 注：设置为true时，当有Queue或其它Exchange绑定到这个Exchange上后，
        ///     在这个Exchange的所有绑定都取消后，会自动删除这个Exchange</param>
        /// <param name="arguments">其它参数</param>
        public void ExchangeDeclare(string exchange, string exchangeType = "fanout", bool durable = false,
            bool autoDelete = false, IDictionary<string, object> arguments = null)
        {
            if (string.IsNullOrEmpty(exchange))
            {
                throw new ArgumentException("Exchange名不能为空", nameof(exchange));
            }

            using (IModel channel = GetChannel())
            {
                channel.ExchangeDeclare(exchange, exchangeType, durable, autoDelete, arguments);
            }
        }


        /// <summary>
        /// 定义Queue，如果存在Exchange，则绑定到它。
        /// 可以在Application_Start中调用此方法，也可以直接通过RabbitMQ的WEB控制台预先定义好
        /// </summary>
        /// <param name="queue">要定义的队列名</param>
        /// <param name="exchange">要绑定到的交换器名，为空时不绑定</param>
        /// <param name="durable">是否持久化</param>
        /// <param name="messageTtl">消息生存时长，毫秒，默认值0，无限时长</param>
        /// <param name="messageMaxLen">消息队列最大长度，默认值0，无限长度</param>
        /// <param name="autoDeleteTime">自动删除时间(ms)，在该时间段内如果没有被重新声明，且没有调用过get命令，将会删除</param>
        /// <param name="routingKey">绑定到队列上的路由Key，可选。特殊业务中可能需要通过路由Key来归类不同的队列</param>
        /// <param name="deadExchange">设置了messageTtl时，过期的死信要转发到的exg</param>
        /// <param name="deadRoutingKey">设置了messageTtl时，过期的死信要转发的routing-key</param>
        public void QueueDeclareAndBind(string queue, string exchange = "", bool durable = false,
            int messageTtl = 0, int messageMaxLen = 0, int autoDeleteTime = 0, string routingKey = null,
            string deadExchange = "", string deadRoutingKey = "")
        {
            if (string.IsNullOrEmpty(queue))
            {
                throw new ArgumentException("Queue名不能为空", nameof(queue));
            }

            using (IModel channel = GetChannel())
            {
                QueueBind(channel, exchange, queue, durable, messageTtl, messageMaxLen,
                    autoDeleteTime, routingKey, deadExchange, deadRoutingKey);
            }
        }





        /// <summary>
        /// 定义Exchange(如果没有Exchange，消息无法入队)，
        /// 如果提供的Queue名，则定义并绑定Queue。
        /// 可以在Application_Start中调用此方法，也可以直接通过RabbitMQ的WEB控制台预先定义好
        /// </summary>
        /// <param name="exchange">要定义的交换器名</param>
        /// <param name="queue">要定义和绑定到交换器的队列名，为空时不处理</param>
        /// <param name="exchangeType">fanout：广播消息给Exchage绑定的所有队列；
        /// direct：直接转发给RouteKey指定的队列；
        /// topic：转发给匹配主题的队列；
        /// headers：尚未研究</param>
        /// <param name="durable">是否持久化</param>
        /// <param name="messageTtl">消息生存时长，毫秒，默认值0，无限时长</param>
        /// <param name="messageMaxLen">消息队列最大长度，默认值0，无际长度</param>
        /// <param name="autoDeleteTime">自动删除时间(ms)，在该时间段内如果没有被重新声明，且没有调用过get命令，将会删除</param>
        /// <param name="routingKey">绑定到队列上的路由Key，可选。特殊业务中可能需要通过路由Key来归类不同的队列</param>
        /// <param name="deadExchange">设置了messageTtl时，过期的死信要转发到的exg</param>
        /// <param name="deadRoutingKey">设置了messageTtl时，过期的死信要转发的routing-key</param>
        [Obsolete("请改用ExchangeDeclare 和 QueueDeclareAndBind这2个方法之一")]
        public void QueueDeclare(string exchange, string queue = "", string exchangeType = "fanout", bool durable = false,
            int messageTtl = 0, int messageMaxLen = 0, int autoDeleteTime = 0, string routingKey = null,
            string deadExchange = "", string deadRoutingKey = "")
        {
            if (string.IsNullOrEmpty(exchange))
            {
                throw new ArgumentException("Exchange名不能为空", nameof(exchange));
            }

            using (IModel channel = GetChannel())
            {
                OperationInterruptedException declareExp = null;
                try
                {
                    channel.ExchangeDeclare(exchange, exchangeType, durable);
                }
                catch (OperationInterruptedException exp)
                {
                    // Exchange已经存在，重新定义参数不相同时，会报错，错误信息举例：
                    // "The AMQP operation was interrupted: AMQP close-reason, initiated by Peer, code=406, 
                    //  text =\"PRECONDITION_FAILED - inequivalent arg 'type' for exchange 'xxx' in vhost '/': received 'fanout' but current is 'topic'\", 
                    //  classId =40, methodId=10, cause="
                    declareExp = exp;
                }

                if (!string.IsNullOrEmpty(queue))
                {
                    QueueBind(channel, exchange, queue, durable, messageTtl, messageMaxLen,
                        autoDeleteTime, routingKey, deadExchange, deadRoutingKey);
                }
                if (declareExp != null)
                {
                    // Exchange存在时，允许绑定后，再抛出此异常
                    throw declareExp;
                }
            }
        }





        /// <summary>
        /// 定义延迟队列和交换器
        /// 可以在Application_Start中调用此方法，也可以直接通过RabbitMQ的WEB控制台预先定义好
        /// </summary>
        /// <param name="exchange">要定义的交换器名</param>
        /// <param name="queue">要定义和绑定到交换器的队列名，为空时不处理</param>
        /// <param name="exchangeType">fanout：广播消息给Exchage绑定的所有队列；
        /// direct：直接转发给RouteKey指定的队列；
        /// topic：转发给匹配主题的队列；
        /// headers：尚未研究</param>
        /// <param name="durable">是否持久化</param>
        /// <param name="messageTtl">消息生存时长，毫秒，默认值300000，表示5分钟</param>
        /// <param name="messageMaxLen">消息队列最大长度，默认值100000个</param>
        /// <param name="autoDeleteTime">自动删除时间(ms)，在该时间段内如果没有被重新声明，且没有调用过get命令，将会删除</param>
        /// <param name="routingKey">绑定到队列上的路由Key，可选。特殊业务中可能需要通过路由Key来归类不同的队列</param>
        public void DelayQueueDeclare(string exchange, string queue = "", string exchangeType = "fanout", bool durable = false,
            int messageTtl = 300000, int messageMaxLen = 100000, int autoDeleteTime = 0, string routingKey = null)
        {
            if (string.IsNullOrEmpty(exchange))
                throw new ArgumentException("Exchange不能为空", nameof(exchange));

            // 设置2个队列a和b，a队列设置过期，没有消费者，过期后路由到b队列，b队列有消费者
            var deadExchange = exchange + ".DeadLetter";
            // 设置常规Exchange
            ExchangeDeclare(exchange, exchangeType, durable);
            // 设置死信Exchange
            ExchangeDeclare(deadExchange, exchangeType, durable);

            if (!string.IsNullOrEmpty(queue))
            {
                var deadQueue = queue + ".DeadLetter";
                var deadRoutingKey = queue + ".DeadRoute";

                // 设置常规队列
                QueueDeclareAndBind(queue, exchange, durable, messageTtl, messageMaxLen, autoDeleteTime, "", deadExchange, deadRoutingKey);
                // 设置死信队列
                QueueDeclareAndBind(deadQueue, deadExchange, durable, 0, 0, 0, deadRoutingKey);
            }
        }

        /// <summary>
        /// 定义队列，并绑定交换器和队列
        /// </summary>
        private void QueueBind(IModel channel, string exchange, string queue = "", bool durable = false,
            int messageTtl = 0, int messageMaxLen = 0, int autoDeleteTime = 0, string routingKey = null,
            string deadExchange = "", string deadRoutingKey = "")
        {
            if (messageTtl != 0 && messageTtl < 1000)
                throw new ArgumentException("ttl必须大于1000毫秒", nameof(messageTtl));
            if (messageMaxLen < 0)
                throw new ArgumentException("消息队列最大长度必须大于0", nameof(messageMaxLen));
            Dictionary<string, object> arg = null;
            if (messageTtl >= 1000)
            {
                arg = new Dictionary<string, object>();
                arg.Add("x-message-ttl", messageTtl);
            }
            if (messageMaxLen > 0)
            {
                arg = arg ?? new Dictionary<string, object>();
                arg.Add("x-max-length", messageMaxLen);
            }
            if (autoDeleteTime > 0)
            {
                arg = arg ?? new Dictionary<string, object>();
                arg.Add("x-expires", autoDeleteTime);
            }
            if (!string.IsNullOrWhiteSpace(deadExchange))
            {
                arg = arg ?? new Dictionary<string, object>();
                arg.Add("x-dead-letter-exchange", deadExchange);
                if (!string.IsNullOrWhiteSpace(deadRoutingKey))
                {
                    arg.Add("x-dead-letter-routing-key", deadRoutingKey);
                }
            }
            channel.QueueDeclare(queue, durable, false, false, arg);
            if (!string.IsNullOrEmpty(exchange))
                channel.QueueBind(queue, exchange, routingKey ?? string.Empty);
        }

        #endregion



        #region 生产消息

        /// <summary>
        /// 提交消息到指定的Exchage；
        /// 当无可用连接或提交消息失败时，抛出异常
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="exchange">交换器名，为空表示使用没名字的默认交换器</param>
        /// <param name="message">消息对象，内部序列化为JSON后，再转字节数组</param>
        /// <param name="headers">消息的头信息</param>
        /// <param name="deliveryMode">是否持久化 1:不持久化 2：持久化</param>
        /// <param name="routingKey">路由的Key，可选。特殊业务中可能需要通过路由Key来归类不同的队列</param>
        public void PublishMessage<T>(string exchange, T message,
            Dictionary<string, object> headers = null, byte deliveryMode = 1, string routingKey = null)
        {
            if (message == null)
            {
                throw new ArgumentException("发送消息不能为空", nameof(message));
            }

            var byteMessage = _defaultSerial.SerializToBytes(message);
            PublishMessage(exchange, byteMessage, headers, deliveryMode, routingKey);
        }


        /// <summary>
        /// 提交消息到指定的Exchage；
        /// 当无可用连接或提交消息失败时，抛出异常
        /// </summary>
        /// <param name="exchange">交换器名，为空表示使用没名字的默认交换器</param>
        /// <param name="byteMessage">具体的消息内容</param>
        /// <param name="headers">消息的头信息</param>
        /// <param name="deliveryMode">是否持久化 1:不持久化 2：持久化</param>
        /// <param name="routingKey">路由的Key，可选。特殊业务中可能需要通过路由Key来归类不同的队列</param>
        /// <exception cref="Exception">当连接不可用时会上抛异常</exception>
        private void PublishMessage(string exchange, byte[] byteMessage,
            Dictionary<string, object> headers = null, byte deliveryMode = 1, string routingKey = null)
        {
            if (byteMessage == null || byteMessage.Length == 0)
            {
                throw new ArgumentException("发送消息不能为空", nameof(byteMessage));
            }
            //if (string.IsNullOrEmpty(exchange))
            //{
            //    throw new ArgumentException("ExChange不能为空", nameof(exchange));
            //}
            exchange = exchange ?? "";

            void DoPublish(Dictionary<string, object> nheaders)
            {
                using (IModel channel = GetChannel())
                {
                    channel.ConfirmSelect(); // 开启确认机制
                    IBasicProperties prop = channel.CreateBasicProperties();
                    prop.DeliveryMode = deliveryMode;
                    prop.Headers = nheaders;
                    channel.BasicPublish(exchange, routingKey ?? string.Empty, prop, byteMessage);
                    channel.WaitForConfirmsOrDie(); // 发送失败会抛出异常
                    logger.DebugExt($"Event Trigger.ExchangeName:{exchange},RoutingKey:{routingKey ?? string.Empty},Message:{Utf8.GetString(byteMessage)},Headers:{_defaultSerial.SerializToStr(headers)},DeliveryMode:{deliveryMode}");
                }
            }
            // 发布消息重试1次
            try
            {
                DoPublish(headers ?? new Dictionary<string, object>());
            }
            catch
            {
                Thread.Sleep(50);
                DoPublish(headers ?? new Dictionary<string, object>());
            }
        }

        /// <summary>
        /// 提交消息到指定的Exchage；
        /// 当无可用连接或提交消息失败时，抛出异常
        /// </summary>
        /// <param name="exchange">交换器名，为空表示使用没名字的默认交换器</param>
        /// <param name="strMessage">具体的消息内容</param>
        /// <param name="headers">消息的头信息</param>
        /// <param name="deliveryMode">是否持久化 1:不持久化 2：持久化</param>
        /// <param name="routingKey">路由的Key，可选。特殊业务中可能需要通过路由Key来归类不同的队列</param>
        /// <exception cref="Exception">当连接不可用时会上抛异常</exception>
        public void PublishMessage(string exchange, string strMessage,
            Dictionary<string, object> headers = null, byte deliveryMode = 1, string routingKey = null)
        {
            if (string.IsNullOrEmpty(strMessage))
            {
                throw new ArgumentException("发送消息不能为空", nameof(strMessage));
            }
            var byteMessage = Utf8.GetBytes(strMessage);
            PublishMessage(exchange, byteMessage, headers, deliveryMode, routingKey);
        }

        /// <summary>
        /// 提交消息到指定的Exchage；
        /// 当无可用连接或提交消息失败时，抛出异常
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="exchange">交换器名，为空表示使用没名字的默认交换器</param>
        /// <param name="message">消息对象，内部序列化为JSON后，再转字节数组</param>
        /// <param name="headers">消息的头信息</param>
        /// <param name="deliveryMode">是否持久化 1:不持久化 2：持久化</param>
        /// <param name="routingKey">路由的Key，可选。特殊业务中可能需要通过路由Key来归类不同的队列</param>
        public void Publish<T>(string exchange, T message,
            Dictionary<string, object> headers = null, byte deliveryMode = 1, string routingKey = null)
        {
            if (message == null)
            {
                throw new ArgumentException("发送消息不能为空", nameof(message));
            }

            var byteMessage = _defaultSerial.SerializToBytes(message);
            PublishMessage(exchange, byteMessage, headers, deliveryMode, routingKey);
        }

        /// <summary>
        /// 提交消息到延迟队列Exchage；
        /// 当无可用连接或提交消息失败时，抛出异常
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="exchange">交换器名</param>
        /// <param name="message">消息对象，内部序列化为JSON后，再转字节数组</param>
        /// <param name="headers">消息的头信息</param>
        /// <param name="deliveryMode">是否持久化 1:不持久化 2：持久化</param>
        /// <param name="routingKey">路由的Key，可选。特殊业务中可能需要通过路由Key来归类不同的队列</param>
        public void DelayPublish<T>(string exchange, T message,
            Dictionary<string, object> headers = null, byte deliveryMode = 1, string routingKey = null)
        {
            if (message == null)
            {
                throw new ArgumentException("发送消息不能为空", nameof(message));
            }

            var byteMessage = _defaultSerial.SerializToBytes(message);
            PublishMessage(exchange, byteMessage, headers, deliveryMode, routingKey);
        }

        #endregion


        #region 消费消息相关

        /// <summary>
        /// 获得队列数据时的回调函数委托
        /// </summary>
        /// <param name="queueData">消息体</param>
        /// <param name="headers">消息的头信息</param>
        public delegate void QueueCallback(byte[] queueData, IDictionary<string, object> headers);
        /// <summary>
        /// 获得队列数据时的回调函数委托
        /// </summary>
        /// <param name="queueData">消息对象</param>
        /// <param name="headers">消息的头信息</param>
        public delegate void QueueCallback<in T>(T queueData, IDictionary<string, object> headers);

        /// <summary>
        /// 获取并处理队列数据出现异常时的回调函数
        /// </summary>
        /// <param name="exception"></param>
        public delegate void QueueErrorCallback(Exception exception);

        /// <summary>
        /// 处理消息异常的回调
        /// </summary>
        public static QueueErrorCallback WaitErrorCallback { get; set; }

        /// <summary>
        /// Qos设置，最多预取的消息数量，未处理完不继续接收消息
        /// </summary>
        public static ushort WaitPrefetchCount { get; set; } = 5;

        ///// <summary>
        ///// 监听队列，并使用回调函数处理
        ///// </summary>
        ///// <param name="queue">队列名</param>
        ///// <param name="callback">获得队列数据时的回调函数</param>
        ///// <param name="errorCallback">获取并处理队列数据出现异常时的回调函数</param>
        //[Obsolete("请改用WaitQueue<T>方法")]
        //public void WaitQueue(string queue,
        //    QueueCallback callback, QueueErrorCallback errorCallback = null)
        //{
        //    if (string.IsNullOrEmpty(queue))
        //    {
        //        throw new ArgumentException("队列名不能为空", nameof(queue));
        //    }
        //    errorCallback = errorCallback ?? WaitErrorCallback;

        //    ThreadPool.UnsafeQueueUserWorkItem(state =>
        //    {
        //        try
        //        {
        //            // 要监听队列，所以不能关闭channel通道
        //            var channel = GetChannel();
        //            var consumer = new EventingBasicConsumer(channel);
        //            consumer.Received += (sender, e) =>
        //            {
        //                try
        //                {
        //                    callback?.Invoke(e.Body, e.BasicProperties.Headers);
        //                }
        //                catch (Exception exp)
        //                {
        //                    if (errorCallback == null)
        //                        throw;
        //                    errorCallback(exp);
        //                    //Console.WriteLine("队列接收处理出错：" + exp.ToString());
        //                }
        //            };
        //            channel.BasicConsume(queue, true, consumer);
        //        }
        //        catch (Exception exp)
        //        {
        //            if (errorCallback == null)
        //                throw;
        //            errorCallback(exp);
        //            //Console.WriteLine("队列接收处理出错：" + exp.ToString());
        //        }
        //    }, callback);
        //}


        /// <summary>
        /// 监听队列，并使用回调函数处理
        /// </summary>
        /// <param name="queue">队列名</param>
        /// <param name="callback">获得队列数据时的回调函数</param>
        /// <param name="errorCallback">获取并处理队列数据出现异常时的回调函数</param>
        /// <param name="prefetchCount">消息预取数量，默认5条，超过5个未ack时，不接收消息</param>
        /// <param name="useAsync">异步处理消息还是同步</param>
        public void WaitQueue(string queue,
            QueueCallback<string> callback, QueueErrorCallback errorCallback = null,
            ushort prefetchCount = 0, bool useAsync = false)
        {
            WaitQueue<string>(queue, callback, errorCallback, prefetchCount, useAsync);
        }


        /// <summary>
        /// 监听队列，并使用回调函数处理
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queue">队列名</param>
        /// <param name="callback">获得队列数据时的回调函数</param>
        /// <param name="errorCallback">获取并处理队列数据出现异常时的回调函数</param>
        /// <param name="prefetchCount">消息预取数量，默认5条，超过5个未ack时，不接收消息</param>
        /// <param name="useAsync">异步处理消息还是同步</param>
        public void WaitQueue<T>(string queue,
            QueueCallback<T> callback, QueueErrorCallback errorCallback = null,
            ushort prefetchCount = 0, bool useAsync = false)
        {
            if (string.IsNullOrEmpty(queue))
            {
                throw new ArgumentException("队列名不能为空", nameof(queue));
            }
            errorCallback = errorCallback ?? WaitErrorCallback;
            if (prefetchCount <= 0)
            {
                prefetchCount = WaitPrefetchCount;
            }
            try
            {
                // 要监听队列，所以不能关闭channel通道
                var channel = GetChannel();
                channel.BasicQos(0, prefetchCount, false);

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (sender, e) =>
                {
                    void msgCallback(object nullobj)
                    {
                        try
                        {
                            ReceiveContext(e.BasicProperties.Headers);
                            if (callback == null)
                                return;
                            var obj = _defaultSerial.DeSerializFromBytes<T>(e.Body);
                            callback(obj, e.BasicProperties.Headers);
                        }
                        catch (Exception exp)
                        {
                            if (errorCallback == null)
                                throw;
                            errorCallback(exp);
                            //Console.WriteLine("队列接收处理出错：" + exp.ToString());
                        }
                        finally
                        {
                            try
                            {
                                // 手工ack 参数2：multiple：是否批量.true:将一次性ack所有小于deliveryTag的消息
                                ((EventingBasicConsumer)sender).Model.BasicAck(e.DeliveryTag, false);
                            }
                            catch (Exception ackExp)
                            {
                                if (errorCallback == null)
                                    throw;
                                errorCallback(ackExp);
                            }
                            finally
                            {
                                logger.DebugExt($"Event Received. Queue:{queue},Message:{Utf8.GetString(e.Body)}");
                            }
                        }
                    }

                    if (useAsync)
                        ThreadPool.UnsafeQueueUserWorkItem(msgCallback, null);
                    else
                        msgCallback(null);
                };
                // 开启acknowledge机制，在接收事件里ack，配合qos进行流控
                channel.BasicConsume(queue, false, consumer);
            }
            catch (Exception exp)
            {
                if (errorCallback == null)
                    throw;
                errorCallback(exp);
                //Console.WriteLine("队列接收处理出错：" + exp.ToString());
            }
        }

        /// <summary>
        /// 监听队列，并使用回调函数处理(不反序列化,带routekey)
        /// </summary>
        /// <param name="queue"></param>
        /// <param name="callback">参数1:数据, 参数2:路由, 参数3:header</param>
        /// <param name="errorCallback">异常回调方法</param>
        /// <param name="prefetchCount">预取消息数</param>
        /// <param name="useAsync">是否异步处理收到的消息</param>
        public void WaitQueueWithRouteKey(string queue,
            Action<string, string, IDictionary<string, object>> callback,
            QueueErrorCallback errorCallback = null,
            ushort prefetchCount = 0, bool useAsync = false)
        {
            if (string.IsNullOrEmpty(queue))
            {
                throw new ArgumentException("队列名不能为空", nameof(queue));
            }
            errorCallback = errorCallback ?? WaitErrorCallback;
            if (prefetchCount <= 0)
            {
                prefetchCount = WaitPrefetchCount;
            }
            try
            {
                // 要监听队列，所以不能关闭channel通道
                var channel = GetChannel();
                channel.BasicQos(0, prefetchCount, false);

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (sender, e) =>
                {
                    void msgCallback(object nullobj)
                    {
                        try
                        {
                            ReceiveContext(e.BasicProperties.Headers);
                            if (callback == null)
                                return;
                            callback(Utf8.GetString(e.Body), e.RoutingKey, e.BasicProperties.Headers);
                        }
                        catch (Exception exp)
                        {
                            if (errorCallback == null)
                                throw;
                            errorCallback(exp);
                            //Console.WriteLine("队列接收处理出错：" + exp.ToString());
                        }
                        finally
                        {
                            try
                            {
                                // 手工ack 参数2：multiple：是否批量.true:将一次性ack所有小于deliveryTag的消息
                                ((EventingBasicConsumer)sender).Model.BasicAck(e.DeliveryTag, false);
                            }
                            catch (Exception ackExp)
                            {
                                if (errorCallback == null)
                                    throw;
                                errorCallback(ackExp);
                            }
                            finally
                            {
                                logger.DebugExt($"Event Received. Queue:{queue},Message:{Utf8.GetString(e.Body)}");
                            }
                        }
                    }

                    if (useAsync)
                        ThreadPool.UnsafeQueueUserWorkItem(msgCallback, null);
                    else
                        msgCallback(null);
                };
                // 开启acknowledge机制，在接收事件里ack，配合qos进行流控
                channel.BasicConsume(queue, false, consumer);
            }
            catch (Exception exp)
            {
                if (errorCallback == null)
                    throw;
                errorCallback(exp);
                //Console.WriteLine("队列接收处理出错：" + exp.ToString());
            }
        }

        /// <summary>
        /// 监听队列，并使用回调函数处理
        /// </summary>
        /// <param name="queue">队列名</param>
        /// <param name="consumerNum">启动几个消费者</param>
        /// <param name="callback">获得队列数据时的回调函数</param>
        /// <param name="errorCallback">获取并处理队列数据出现异常时的回调函数</param>
        /// <param name="prefetchCount">每个消费者的消息预取数量，默认5条，超过5个未ack时，不接收消息</param>
        /// <param name="useAsync">异步处理消息还是同步</param>
        public void WaitQueue(string queue, int consumerNum,
            QueueCallback<string> callback, QueueErrorCallback errorCallback = null,
            ushort prefetchCount = 0, bool useAsync = false)
        {
            if (consumerNum < 1)
            {
                throw new ArgumentException("消费者数量不能小于1");
            }
            for (var i = 0; i < consumerNum; i++)
            {
                WaitQueue(queue, callback, errorCallback, prefetchCount, useAsync);
            }
        }

        /// <summary>
        /// 监听队列，并使用回调函数处理
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queue">队列名</param>
        /// <param name="consumerNum">启动几个消费者</param>
        /// <param name="callback">获得队列数据时的回调函数</param>
        /// <param name="errorCallback">获取并处理队列数据出现异常时的回调函数</param>
        /// <param name="prefetchCount">每个消费者的消息预取数量，默认5条，超过5个未ack时，不接收消息</param>
        /// <param name="useAsync">异步处理消息还是同步</param>
        public void WaitQueue<T>(string queue, int consumerNum,
            QueueCallback<T> callback, QueueErrorCallback errorCallback = null,
            ushort prefetchCount = 0, bool useAsync = false)
        {
            if (consumerNum < 1)
            {
                throw new ArgumentException("消费者数量不能小于1");
            }
            for (var i = 0; i < consumerNum; i++)
            {
                WaitQueue(queue, callback, errorCallback, prefetchCount, useAsync);
            }
        }


        /// <summary>
        /// 监听延迟队列，并使用回调函数处理
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queue">队列名</param>
        /// <param name="callback">获得队列数据时的回调函数</param>
        /// <param name="errorCallback">获取并处理队列数据出现异常时的回调函数</param>
        public void DelayWaitQueue<T>(string queue,
            QueueCallback<T> callback, QueueErrorCallback errorCallback = null)
        {
            if (string.IsNullOrEmpty(queue))
            {
                throw new ArgumentException("队列名不能为空", nameof(queue));
            }
            //var deadRoutingKey = queue + ".DeadRoute";
            var deadQueue = queue + ".DeadLetter";
            WaitQueue(deadQueue, callback, errorCallback);
        }


        ///// <summary>
        ///// 启动线程主动获取消息(noAck=true), 并使用回调函数处理
        ///// </summary>
        ///// <param name="queue">队列名</param>
        ///// <param name="callback">获得队列数据时的回调函数</param>
        ///// <param name="errorCallback">获取并处理队列数据出现异常时的回调函数</param>
        ///// <param name="maxBatchSize">每次获取信息数量</param>
        ///// <param name="continueTask">出现异常时，是否继续主动获取</param>
        ///// <param name="sleepTime">当队列已经被取空后的休眠时间(毫秒)</param>
        ///// <param name="errorSleepTime">出现异常后休眠时间(毫秒)</param>
        //public void GetFromQueue(string queue, QueueCallback callback,
        //    QueueErrorCallback errorCallback = null, int maxBatchSize = 10, bool continueTask = true,
        //    int sleepTime = 1000, int errorSleepTime = 5000)
        //{
        //    if (string.IsNullOrEmpty(queue))
        //    {
        //        throw new ArgumentException("队列名不能为空", nameof(queue));
        //    }
        //    ThreadPool.UnsafeQueueUserWorkItem(state =>
        //    {
        //        while (true)
        //        {
        //            try
        //            {
        //                using (var channel = GetChannel())
        //                {
        //                    while (true)
        //                    {
        //                        var rmqList = new List<BasicGetResult>(maxBatchSize);
        //                        for (int i = 0; i < maxBatchSize; i++)
        //                        {
        //                            BasicGetResult result = channel.BasicGet(queue, true);
        //                            if (result != null)
        //                            {
        //                                rmqList.Add(result);
        //                            }
        //                            else
        //                            {
        //                                break;
        //                            }
        //                        }
        //                        if (callback != null)
        //                        {
        //                            foreach (var item in rmqList)
        //                            {
        //                                callback(item.Body, item.BasicProperties.Headers);
        //                            }
        //                        }
        //                        if (rmqList.Count < maxBatchSize)
        //                        {
        //                            // 当队列已经被取空, 休眠一会
        //                            Thread.Sleep(sleepTime);
        //                        }
        //                    }
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                errorCallback?.Invoke(ex);
        //                if (!continueTask)
        //                {
        //                    break;
        //                }
        //                if (errorSleepTime > 0)
        //                {
        //                    Thread.Sleep(errorSleepTime);
        //                }
        //            }
        //        }
        //    }, callback);
        //}

        #endregion


        /// <summary>
        /// 删除指定的队列, 并返回抛弃的消息数
        /// </summary>
        /// <param name="queue"></param>
        /// <returns></returns>
        public uint DelQueue(string queue)
        {
            using (IModel channel = GetChannel())
            {
                return channel.QueueDelete(queue);
            }
        }

        #endregion


        #region 非公开方法


        /// <summary>
        ///  连接服务器所需参数
        /// </summary>
        private RabbitMQConfigSection _config;

        /// <summary>
        /// 当前连接使用的参数
        /// </summary>
        public RabbitMQConfigSection Config => _config;

        // private readonly Random _random = new Random();
        private IConnection _connection;

        /// <summary>
        /// 供重载构造函数初始化连接参数用
        /// </summary>
        /// <param name="config"></param>
        protected void Init(RabbitMQConfigSection config)
        {
            _config = config;
            _connection = CreateNewConnection(_config);

            // 启动连接检测线程, AutomaticRecoveryEnabled 会自动恢复连接，所以不去检测了
            // Task.Factory.StartNew(CheckConnectionAlive);
        }

        /// <summary>
        /// 检测当前连接是否存活的方法
        /// </summary>
        protected async Task CheckConnectionAlive()
        {
            var sleepSecond = _config.HeartBeatSecond;
            while (true)
            {
                try
                {
                    using (var session = _connection.CreateModel())
                    {
                        session.ExchangeDeclare("__Heart", ExchangeType.Fanout, true);
                    }
                }
                catch (Exception exp1)
                {
                    OutputMsg("RabbitMQ 旧连接已断开：" + exp1.Message);
                    try
                    {
                        _connection = CreateNewConnection(_config);
                    }
                    catch (Exception exp2)
                    {
                        OutputMsg("RabbitMQ 连接创建失败：" + exp2.Message);
                    }
                }
                await Task.Delay(sleepSecond).ConfigureAwait(false);
                //Thread.Sleep(sleepSecond);
            }
            // ReSharper disable once FunctionNeverReturns
        }


        static RabbitMQConfigSection ParseConnectStr(string constr)
        {
            string serverIp = "127.0.0.1";
            int serverPort = 5672;
            string userName = "guest";
            string password = null;
            string connName = "";

            int idx = constr.IndexOf("//", StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
            {
                constr = constr.Substring(idx + 2);
            }
            var arr = constr.Split(':', '@');
            if (arr.Length == 4)
            {
                userName = arr[0];
                password = arr[1];
                serverIp = arr[2];
                var arrPortAndName = arr[3].Split('/');
                if (!int.TryParse(arrPortAndName[0], out serverPort))
                    serverPort = 5672;
                if (arrPortAndName.Length > 1)
                    connName = arrPortAndName[1];
            }
            //对  amqp://uname:pwd@address/conname  格式的支持修复
            else if (arr.Length == 3)
            {
                userName = arr[0];
                password = arr[1];
                var addressAndName = arr[2].Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                serverIp = addressAndName[0];
                if (addressAndName.Length > 1)
                    connName = addressAndName[1];
                serverPort = 5672;
            }
            else if (arr.Length == 2)
            {
                serverIp = arr[0];
                var arrPortAndName = arr[1].Split('/');
                if (!int.TryParse(arrPortAndName[0], out serverPort))
                    serverPort = 5672;
                if (arrPortAndName.Length > 1)
                    connName = arrPortAndName[1];
            }
            return new RabbitMQConfigSection
            {
                ServerIp = serverIp,
                ServerPort = serverPort,
                Password = password,
                UserName = userName,
                ConnectionName = connName
            };
        }


        /// <summary>
        /// 创建新连接，并放入池中
        /// </summary>
        /// <returns></returns>
        static IConnection CreateNewConnection(RabbitMQConfigSection config)
        {
            var heartSecond = Convert.ToUInt16(config.HeartBeatSecond);
            if (heartSecond < 5)
            {
                heartSecond = 10;
            }
            ConnectionFactory connectionFactory = new ConnectionFactory
            {
                HostName = config.ServerIp,
                Port = config.ServerPort,
                UserName = config.UserName,
                Password = config.Password,
                //Socket read timeout is twice the hearbeat
                RequestedHeartbeat = heartSecond,
                AutomaticRecoveryEnabled = true,
            };
            var name = string.IsNullOrEmpty(config.ConnectionName) ? "noName" : config.ConnectionName;
            var connection = connectionFactory.CreateConnection(name);
            return connection;
        }


        /// <summary>
        /// 输出调试信息
        /// </summary>
        /// <param name="msg"></param>
        [Conditional("DEBUG")]
        protected static void OutputMsg(string msg)
        {
#if DEBUG
            Console.WriteLine(msg);
#endif
        }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            _connection.Dispose();
        }

        // ReSharper disable once UnusedParameter.Local
        private void ReceiveContext(IDictionary<string, object> headers)
        {
            //if (headers.TryGetValue(ContextKeys.Language, out var languagValue))
            //    BaseContext.Current.Set(ContextKeys.Language, Encoding.UTF8.GetString((byte[])languagValue));
            //if (headers.TryGetValue(ContextKeys.TimeZone, out var timezoneValue))
            //    BaseContext.Current.Set(ContextKeys.TimeZone, Encoding.UTF8.GetString((byte[])timezoneValue));
        }
    }
}
