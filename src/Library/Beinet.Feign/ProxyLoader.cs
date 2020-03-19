using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
                var feignAtt = atts[0];

                var wrapper = new ProxyInvokeWrapper();
                var ret = (FeignProcess) _factory.CreateProxy(typeof(FeignProcess), wrapper, type);

                if (feignAtt.Configuration == null)
                    ret.Config = new FeignDefaultConfig();
                else
                    ret.Config = (IFeignConfig) Activator.CreateInstance(feignAtt.Configuration);

                ret.Url = feignAtt.Url;
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
                switch (method.Name)
                {
                    case "GetHashCode":
                        return this.GetHashCode();
                    case "Equals":
                        return this.Equals(info.Arguments?.FirstOrDefault());
                    case "ToString":
                        return this.ToString();
                }

                var httpAtt = TypeHelper.GetRequestMappingAttribute(method);
                var args = GetArguments(method, info.Arguments);

                return instance.Process(httpAtt, args, method.ReturnType);
            }

            public void AfterInvoke(InvocationInfo info, object returnValue)
            {
            }

            
            static Dictionary<string, ArgumentItem> GetArguments(MethodInfo method, object[] values)
            {
                var methodArgs = TypeHelper.GetArgs(method);
                if (methodArgs == null || methodArgs.Count == 0)
                    return null;

                var bodyArgNum = 0; // 有几个body参数，不允许超过1个
                var ret = new Dictionary<string, ArgumentItem>();
                var idx = 0;
                var valuesLen = values?.Length ?? 0;
                foreach (var argument in methodArgs)
                {
                    var para = argument.Key;
                    var att = argument.Value;
                    var item = new ArgumentItem();
                    ret.Add(para.Name, item);

                    item.Name = para.Name;
                    if (att == null)
                    {
                        item.HttpName = item.Name;
                        item.Type = ArgType.Body;
                    }
                    else
                    {
                        item.HttpName = att.Name.Length <= 0 ? item.Name : att.Name;
                        item.Type = att.Type;
                    }

                    if (item.Type == ArgType.Body)
                    {
                        bodyArgNum++;
                        if (bodyArgNum > 1)
                            throw new Exception(
                                $"Body参数只允许一个，当前: {ret.Values.Aggregate("", (s, argumentItem) => s + argumentItem.Name + ",")}");
                    }

                    if (idx < valuesLen)
                    {
                        // ReSharper disable once PossibleNullReferenceException 这里不可能为null的，因为上面判断了
                        item.Value = values[idx];
                    }

                    idx++;
                }

                return ret;
            }
        }
    }
}