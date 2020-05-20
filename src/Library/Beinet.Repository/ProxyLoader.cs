using System;
using System.Collections.Concurrent;
using System.Linq;
using Beinet.Repository.Repositories;
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

                var baseType = typeof(JpaRepository<,>);

                var repositoryType = type.GetInterfaces()
                    .FirstOrDefault(tp => tp.Namespace == baseType.Namespace && tp.Name == "JpaRepository`2");
                if (repositoryType == default)
                    throw new ArgumentException($"必须实现类型：{baseType.FullName}");
                if (repositoryType.GenericTypeArguments.Length != 2)
                    throw new ArgumentException($"必须实现类型：{baseType.FullName}, 类型参数个数有误");
                
                var wrapper = new ProxyInvokeWrapper();
                var ret = (JpaProcess)_factory.CreateProxy(typeof(JpaProcess), wrapper, type);
                ret.RepositoryType = type;
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
                // Method intentionally left empty.
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
                
                return instance.Process(method, info.Arguments);
            }

            public void AfterInvoke(InvocationInfo info, object returnValue)
            {
                // Method intentionally left empty.
            }

        }
    }
}