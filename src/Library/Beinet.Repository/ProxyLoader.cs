using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LinFu.DynamicProxy;

namespace Beinet.Repository
{
    /// <summary>
    /// 完成接口层的代理
    /// </summary>
    public static class ProxyLoader
    {
        /// <summary>
        /// 单例缓存列表
        /// </summary>
        static ConcurrentDictionary<Type, object> _proxys =
            new ConcurrentDictionary<Type, object>();

        static ProxyFactory _factory = new ProxyFactory();

        /// <summary>
        /// 返回指定接口类型的实例（代理类，不需要实现）
        /// </summary>
        /// <typeparam name="TInterface">返回的代理类型</typeparam>
        /// <returns></returns>
        public static TInterface GetProxy<TInterface>()
        {
            return (TInterface)GetProxy(typeof(TInterface));
        }

        /// <summary>
        /// 返回指定接口类型的实例（代理类，不需要实现）
        /// </summary>
        /// <param name="type">返回的代理类型</param>
        /// <returns></returns>
        public static object GetProxy(Type type)
        {
            return _proxys.GetOrAdd(type, typeInner =>
            {
                if (!type.IsInterface)
                    throw new ArgumentException("必须是接口类型");
                var repositoryType = type.GetInterfaces()
                    .FirstOrDefault(tp => tp.Namespace == "Beinet.Repository" && tp.Name == "JpaRepository`2");
                if (repositoryType == default(Type))
                    throw new ArgumentException("必须实现类型：Beinet.Repository.JpaRepository`2");
                if (repositoryType.GenericTypeArguments.Length != 2)
                    throw new ArgumentException("必须实现类型：Beinet.Repository.JpaRepository`2, 类型参数个数有误");
                
                var wrapper = new ProxyInvokeWrapper();
                var ret = (JpaProcess)_factory.CreateProxy(typeof(JpaProcess), wrapper, type);
                ret.EntityType = repositoryType.GenericTypeArguments[0];
                ret.KeyType = repositoryType.GenericTypeArguments[1];

                return ret;
            });
        }
        
        /// <summary>
        /// 拦截类
        /// </summary>
        internal class ProxyInvokeWrapper : IInvokeWrapper
        {

            public void BeforeInvoke(InvocationInfo info)
            {
            }

            public object DoInvoke(InvocationInfo info)
            {
                var instance = info.Target as JpaProcess;
                if (instance == null)
                    throw new ArgumentException("实例创建有问题: " + info.Target.GetType());
                
                var method = info.TargetMethod;
                switch (method.Name)
                {
                    case "GetHashCode":
                        return this.GetHashCode();
                    case "Equals":
                        return this.Equals(info.Arguments?.FirstOrDefault());
                    case "ToString":
                        return this.ToString();
                }
                
                return instance.Process(method.Name, method.ReturnType);
            }

            public void AfterInvoke(InvocationInfo info, object returnValue)
            {
            }

        }
    }
}