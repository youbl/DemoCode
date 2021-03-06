﻿using System;
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
        /// Feign配置类构造方法委托
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public delegate IFeignConfig ConfigResolver(Type type);

        /// <summary>
        /// 全局统一的Feign配置构造方法
        /// </summary>
        public static ConfigResolver Resolver { get; set; }

        /// <summary>
        /// 返回指定接口类型的实例（代理类，不需要实现）
        /// </summary>
        /// <typeparam name="TInterface">返回的代理类型</typeparam>
        /// <param name="resolver">配置类解析方法</param>
        /// <returns></returns>
        public static TInterface GetProxy<TInterface>(ConfigResolver resolver = null)
        {
            return (TInterface)GetProxy(typeof(TInterface), resolver);
        }

        /// <summary>
        /// 返回指定接口类型的实例（代理类，不需要实现）
        /// </summary>
        /// <param name="type">返回的代理类型</param>
        /// <param name="resolver">配置类解析方法</param>
        /// <returns></returns>
        public static object GetProxy(Type type, ConfigResolver resolver = null)
        {
            return _feigns.GetOrAdd(type, typeInner =>
            {
                if (!type.IsInterface)
                    throw new ArgumentException("必须是接口类型");
                var atts = TypeHelper.GetCustomAttributes<FeignClientAttribute>(type);
                if (atts.Count <= 0)
                    throw new ArgumentException("未找到FeignClient特性配置");
                if (atts[0].Url == null || (atts[0].Url = atts[0].Url.Trim()).Length == 0)
                    throw new ArgumentException("FeignClient特性配置Url不能为空");
                var feignAtt = atts[0];

                var wrapper = new ProxyInvokeWrapper();
                var ret = (FeignProcess) _factory.CreateProxy(typeof(FeignProcess), wrapper, type);

                resolver = resolver ?? Resolver ?? ResolveConfig;
                ret.Config = resolver(feignAtt.Configuration);

                ret.Url = feignAtt.Url;
                return ret;
            });
        }

        static IFeignConfig ResolveConfig(Type configType)
        {
            if (configType == null)
                return new FeignDefaultConfig();

            if (!typeof(IFeignConfig).IsAssignableFrom(configType))
                throw new ArgumentException("配置类必须实现IFeignConfig.");

            return (IFeignConfig)Activator.CreateInstance(configType);
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
                var instance = info.Target as FeignProcess;
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

                var httpAtt = TypeHelper.GetRequestMappingAttribute(method);
                var args = GetArguments(method, info.Arguments);

                return instance.Process(httpAtt, args, method.ReturnType);
            }

            public void AfterInvoke(InvocationInfo info, object returnValue)
            {
                // Method intentionally left empty.
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

                    if (idx < valuesLen)
                    {
                        // ReSharper disable once PossibleNullReferenceException 这里不可能为null的，因为上面判断了
                        item.Value = values[idx];
                    }

                    item.Name = para.Name;
                    if (att == null)
                    {
                        item.HttpName = item.Name;

                        // Uri和Type类型参数，默认不作为BODY
                        if (item.Value is Uri || item.Value is Type)
                            item.Type = ArgType.None;
                        else
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

                    idx++;
                }

                return ret;
            }
        }
    }
}