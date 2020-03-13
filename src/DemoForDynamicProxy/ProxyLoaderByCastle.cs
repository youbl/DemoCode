using System;
using System.Text;
using System.Threading;
using Castle.Core.Interceptor;
using Castle.DynamicProxy;

namespace DemoForDynamicProxy
{
    /// <summary>
    /// 这个Demo 基于 LinFu.DynamicProxy，
    /// 用于演示，如何创建一个没有编写实现的接口实例.
    /// 使用场景：一些业务项目只需要定义接口，然后由框架进行统一实现的场景；
    /// 举例1：类似于Java的Feign，业务只需要定义接口和Attribute声明，框架层统一完成http请求。
    /// 举例2：类似于Java的JPA，业务只需要定义仓储层接口，框架层统一完成数据库操作
    /// </summary>
    public static class ProxyLoaderByCastle
    {
        static ProxyGenerator proxyGenerator = new ProxyGenerator();

        /// <summary>
        /// 创建指定接口和指定类的实例代理（代理类，不需要实现）
        /// </summary>
        /// <typeparam name="TInterface"></typeparam>
        /// <param name="instanceType"></param>
        /// <returns></returns>
        public static TInterface GetProxy<TInterface>(Type instanceType)
        {
            return (TInterface)GetProxy(typeof(TInterface), instanceType);
        }

        /// <summary>
        /// 创建指定接口和指定类的实例代理（代理类，不需要实现）
        /// </summary>
        /// <param name="interfaceType"></param>
        /// <param name="instanceType"></param>
        /// <returns></returns>
        public static object GetProxy(Type interfaceType, Type instanceType)
        {
            if (!interfaceType.IsInterface)
                throw new Exception("必须是非接口类型");

            Type[] arrInterfaces = new Type[] { interfaceType };
            return proxyGenerator.CreateClassProxy(instanceType, arrInterfaces, new SomeInterceptor());
        }

        public class SomeInterceptor : IInterceptor
        {
            public void Intercept(IInvocation invocation)
            {
                Output("准备执行");
                try
                {
                    var method = invocation.Method;
                    var args = invocation.Arguments;

                    var msg = new StringBuilder("执行中，");
                    msg.AppendFormat("调用方法名: {1} {0}, 参数列表：\r\n", method.Name, method.DeclaringType);
                    foreach (var arg in args)
                    {
                        msg.AppendFormat("    {0}", arg);
                    }

                    var instanceType = invocation.Proxy.GetType();
                    msg.AppendFormat("\r\n创建实例类型: {0} 父类: {1} 声明所在类: {2}\r\n接口清单：\r\n", instanceType.FullName, instanceType.BaseType?.FullName, instanceType.DeclaringType?.FullName);
                    foreach (var infType in instanceType.GetInterfaces())
                    {
                        msg.AppendFormat("    {0}\r\n", infType.FullName);
                    }

                    Output(msg.ToString());

                    // invocation.Proceed();// 如果没有具体接口实现，这里会抛异常
                }
                catch (Exception exp)
                {
                    Output("执行出错:" + exp);
                }
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
