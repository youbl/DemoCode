using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using LinFu.DynamicProxy;

namespace Beinet.Feign
{
    /// <summary>
    /// 完成接口层的代理
    /// </summary>
    public static class ProxyLoader
    {
        static ConcurrentDictionary<Type, object> _feigns =
            new ConcurrentDictionary<Type, object>();

        static ProxyFactory _factory = new ProxyFactory();

        /// <summary>
        /// 返回指定接口类型的实例（代理类，不需要实现）
        /// </summary>
        /// <typeparam name="TInterface"></typeparam>
        /// <returns></returns>
        public static TInterface GetProxy<TInterface>()
        {
            return (TInterface)GetProxy(typeof(TInterface));
        }

        /// <summary>
        /// 返回指定接口类型的实例（代理类，不需要实现）
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static object GetProxy(Type type)
        {
            return _feigns.GetOrAdd(type, typeInner =>
            {
                if (!type.IsInterface)
                    throw new Exception("必须是非接口类型");
                var atts = TypeHelper.GetCustomAttributes<FeignClientAttribute>(type);
                if (atts.Count <= 0)
                    throw new Exception("未找到FeignClient特性配置");
                if (atts[0].Url == null || (atts[0].Url = atts[0].Url.Trim()).Length == 0)
                    throw new Exception("FeignClient特性配置Url不能为空");

                var wrapper = new ProxyInvokeWrapper();
                var ret = (FeignProcess)_factory.CreateProxy(typeof(FeignProcess), wrapper, type);
                ret.Att = atts[0];
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
                var instance = info.Target as FeignProcess;
                if (instance == null)
                    throw new Exception("实例创建有问题: " + info.Target.GetType());

                var method = info.TargetMethod;
                var httpAtt = TypeHelper.GetMappingAttribute(method);
                var args = GetArguments(method, info.Arguments);

                return instance.Process(httpAtt, args, method.ReturnType);
            }

            public void AfterInvoke(InvocationInfo info, object returnValue)
            {
            }


            static Dictionary<string, object> GetArguments(MethodInfo method, object[] args)
            {
                var paras = method.GetParameters();
                if (paras.Length == 0)
                    return null;

                var ret = new Dictionary<string, object>();
                var idx = 0;
                var valuesLen = args != null ? args.Length : 0;
                foreach (var para in paras)
                {
                    if (idx < valuesLen)
                    {
                        // ReSharper disable once PossibleNullReferenceException 这里不可能为null的，因为上面判断了
                        ret.Add(para.Name, args[idx]);
                    }
                    else
                    {
                        ret.Add(para.Name, null);
                    }
                    idx++;
                }

                return ret;
            }
        }
    }
}