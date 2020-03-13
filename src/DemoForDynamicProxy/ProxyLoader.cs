using System;
using System.Text;
using System.Threading;
using LinFu.DynamicProxy;

namespace DemoForDynamicProxy
{
    /// <summary>
    /// 这个Demo用于演示，如何创建一个没有编写实现的接口实例.
    /// 使用场景：一些业务项目只需要定义接口，然后由框架进行统一实现的场景；
    /// 举例1：类似于Java的Feign，业务只需要定义接口和Attribute声明，框架层统一完成http请求。
    /// 举例2：类似于Java的JPA，业务只需要定义仓储层接口，框架层统一完成数据库操作
    /// </summary>
    public static class ProxyLoader
    {
        static ProxyFactory _factory = new ProxyFactory();

        /// <summary>
        /// 返回指定接口类型的实例（代理类，不需要实现）
        /// </summary>
        /// <typeparam name="TInterface"></typeparam>
        /// <returns></returns>
        public static TInterface GetProxy<TInterface>()
        {
            return (TInterface) GetProxy(typeof(TInterface));
        }

        /// <summary>
        /// 返回指定接口类型的实例（代理类，不需要实现）
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static object GetProxy(Type type)
        {
            if (!type.IsInterface)
                throw new Exception("必须是非接口类型");

            var wrapper = new ProxyInvokeWrapper();
            return _factory.CreateProxy(type, wrapper);
        }


        /// <summary>
        /// 返回指定接口类型的实例（代理类，不需要实现）
        /// </summary>
        /// <typeparam name="TInterface"></typeparam>
        /// <param name="instanceType"></param>
        /// <returns></returns>
        public static TInterface GetProxy<TInterface>(Type instanceType)
        {
            return (TInterface)GetProxy(typeof(TInterface), instanceType);
        }

        /// <summary>
        /// 返回指定接口类型的实例（代理类，不需要实现）
        /// </summary>
        /// <param name="interfaceType"></param>
        /// <param name="instanceType"></param>
        /// <returns></returns>
        public static object GetProxy(Type interfaceType, Type instanceType)
        {
            if (!interfaceType.IsInterface)
                throw new Exception("必须是非接口类型");

            var wrapper = new ProxyInvokeWrapper();
            return _factory.CreateProxy(instanceType, wrapper, interfaceType);
        }


        public class ProxyInvokeWrapper : IInvokeWrapper
        {
            public void BeforeInvoke(InvocationInfo info)
            {
                // 方法执行前
                Output("准备执行");
            }

            public object DoInvoke(InvocationInfo info)
            {
                var method = info.TargetMethod;
                var args = info.Arguments;

                var msg = new StringBuilder("执行中，");
                msg.AppendFormat("调用方法名: {1} {0}, 参数列表：\r\n", method.Name, method.DeclaringType);
                foreach (var arg in args)
                {
                    msg.AppendFormat("    {0}", arg);
                }

                var instanceType = info.Target.GetType();
                msg.AppendFormat("\r\n创建实例类型: {0} 父类: {1} 声明所在类: {2}\r\n接口清单：\r\n", instanceType.FullName, instanceType.BaseType?.FullName, instanceType.DeclaringType?.FullName);
                foreach (var infType in instanceType.GetInterfaces())
                {
                    msg.AppendFormat("    {0}\r\n", infType.FullName);
                }

                Output(msg.ToString());
                return "";
            }

            public void AfterInvoke(InvocationInfo info, object returnValue)
            {
                // 方法执行后
                Output("执行完毕");
            }

            void Output(string msg)
            {
                msg = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} {Thread.CurrentThread.ManagedThreadId.ToString()} {msg}";
                Console.WriteLine(msg);
            }
        }
    }

}
