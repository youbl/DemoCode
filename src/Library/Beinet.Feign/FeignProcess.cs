﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Web;

namespace Beinet.Feign
{
    /// <summary>
    /// Feign主执行类
    /// </summary>
    public class FeignProcess
    {
        private const string GET = "GET";
        static FeignProcess()
        {
            foreach (string setting in ConfigurationManager.AppSettings)
            {
                _configs[setting] = ConfigurationManager.AppSettings[setting];
            }

            //调大默认连接池
            ServicePointManager.DefaultConnectionLimit = 1024;
            //连接池中的TCP连接不使用Nagle算法
            //ServicePointManager.UseNagleAlgorithm = false;
            //https证书验证回调
            ServicePointManager.ServerCertificateValidationCallback = CheckValidationResult;
        }

        private string _url;

        /// <summary>
        /// Url前缀
        /// </summary>
        public string Url
        {
            get => _url;
            set
            {
                _url = value;
                ParseUrl();
            }
        }

        private IFeignConfig _config = new FeignDefaultConfig();

        /// <summary>
        /// 编码解码配置
        /// </summary>
        public IFeignConfig Config
        {
            get => _config;
            set
            {
                _config = value;
                Interceptors = Config.GetInterceptor();
                ParseUrl();
            }
        }

        /// <summary>
        /// 请求拦截器
        /// </summary>
        internal List<IRequestInterceptor> Interceptors { get; private set; }

        private void ParseUrl()
        {
            if (Interceptors != null && !string.IsNullOrEmpty(_url))
            {
                foreach (var interceptor in Interceptors)
                {
                    _url = interceptor.OnCreate(_url);
                }
            }
        }

        static Dictionary<string, object> _configs = new Dictionary<string, object>();

        /// <summary>
        /// 泛型版本HTTP调用
        /// </summary>
        /// <typeparam name="T">方法的返回参数类型</typeparam>
        /// <param name="methodAtt">方法的路由相关信息</param>
        /// <param name="args">方法的入参数列表</param>
        /// <returns></returns>
        public T Process<T>(RequestMappingAttribute methodAtt, Dictionary<string, ArgumentItem> args)
        {
            var obj = Process(methodAtt, args, typeof(T));
            return (T) obj;
        }

        /// <summary>
        /// 主调方法
        /// </summary>
        /// <param name="methodAtt">方法的路由相关信息</param>
        /// <param name="args">方法的入参数列表</param>
        /// <param name="returnType">方法的返回参数类型</param>
        /// <returns></returns>
        public object Process(RequestMappingAttribute methodAtt, Dictionary<string, ArgumentItem> args, Type returnType)
        {
            var method = methodAtt.Method;
            if (string.IsNullOrEmpty(method))
                method = GET;
            else
                method = method.ToUpper();

            Uri paraUri = null;
            object bodyArg = null;
            if (args != null)
            {
                foreach (var argumentItem in args.Values)
                {
                    if (argumentItem.Value == null)
                        continue;

                    if (argumentItem.Type == ArgType.None)
                    {
                        if (argumentItem.Value is Uri uri)
                        {
                            // 查找Uri类型参数数据，如果有，用于替换FeignClient的Url
                            paraUri = uri;
                        }
                        else if (argumentItem.Value is Type type)
                        {
                            if (!returnType.IsAssignableFrom(type))
                                throw new Exception($"指定的参数类型{type}，必须是返回类型{returnType}的子类");

                            // 查找Type类型参数数据，如果有，用于替换returnType
                            returnType = type;
                        }
                    }
                    else if (argumentItem.Type == ArgType.Body)
                    {
                        // 查找POST参数数据
                        bodyArg = argumentItem.Value;
                    }
                }
            }

            if (bodyArg != null && method == GET)
            {
                // 有body数据时，强制修改为POST，Java也是这么做的
                method = "POST";
            }

            // 拼接url和路由
            var url = paraUri == null ? Url : paraUri.ToString();
            url = CombineUrl(url, methodAtt.Route);
            // 用配置和参数替换url里的内容
            url = ParseUrl(url, args);

            var headers = CombineHeaders(methodAtt.Headers, args);

            Type realType;
            try
            {
                var postStr = "";
                if (bodyArg != null)
                    postStr = Config.Encoding(bodyArg);

                var httpReturn = WebHelper.GetPage(url, method, postStr, headers, Interceptors);

                var ret = Config.Decoding(httpReturn, returnType);
                if (ret == null)
                {
                    if(returnType.IsValueType)
                        return TypeHelper.GetDefaultValue(returnType);
                    return null;
                }

                realType = ret.GetType();
                if (returnType.IsInstanceOfType(ret))
                    return ret;
            }
            catch (Exception exp)
            {
                var handledExp = Config.ErrorHandle(exp);
                if (handledExp != null)
                    throw handledExp;

                return TypeHelper.GetDefaultValue(returnType);
            }

            throw new Exception("Decoding返回的对象类型必须是：" + returnType.FullName + "，不允许是：" + realType.FullName);
        }

        /// <summary>
        /// 替换URL里的占位符, 并把get参数放在url后面
        /// </summary>
        /// <param name="url"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        static string ParseUrl(string url, Dictionary<string, ArgumentItem> args)
        {
            if (string.IsNullOrEmpty(url))
                throw new Exception("url不允许为空");

            int idx = -1;
            do
            {
                idx = url.IndexOf('{', idx + 1);
                if (idx < 0)
                    break;
                var endIdx = url.IndexOf('}', idx + 1);
                if (endIdx < 0)
                    break;
                if (idx == endIdx - 1)
                    throw new Exception("不允许出现空占位符");

                var argName = url.Substring(idx + 1, endIdx - idx - 1);

                object val;
                // 先取参数列表
                if (args != null && args.TryGetValue(argName, out var item))
                {
                    val = item.Value;
                }
                // 再取config配置列表
                else if (!_configs.TryGetValue(argName, out val))
                {
                    throw new Exception($"占位符{{{argName}}}在参数列表或配置文件中，均不存在");
                }

                url = url.Replace("{" + argName + "}", Convert.ToString(val));
            } while (true);

            // 拼接列表里的查询参数
            if (args != null && args.Count > 0)
            {
                var sbArgs = new StringBuilder();
                foreach (var pair in args)
                {
                    var arg = pair.Value;
                    if (arg.Type != ArgType.Param)
                        continue;

                    var encodingVal = HttpUtility.UrlEncode(Convert.ToString(arg.Value));
                    sbArgs.AppendFormat("{0}={1}&", arg.HttpName, encodingVal);
                }

                if (sbArgs.Length > 0)
                {
                    sbArgs.Insert(0, url.IndexOf('?') > 0 ? '&' : '?');
                    url += sbArgs;
                }
            }

            return url;
        }




        static string CombineUrl(string url, string route)
        {
            route = route ?? "";
            if (WebHelper.IsUrl(route))
                return route;

            if (route.Length <=0 || route[0] != '/')
                route = '/' + route;
            if (url.Length > 0 && url[url.Length - 1] == '/')
                url = url.Substring(0, url.Length - 1);

            url += route;
//            if (!IsUrl(url))
//                url = "http://" + url;
            return url;
        }

        static Dictionary<string, string> CombineHeaders(string[] headers, Dictionary<string, ArgumentItem> args)
        {
            var ret = new Dictionary<string, string>();
            if (headers != null && headers.Length > 0)
            {
                foreach (var header in headers)
                {
                    var idx = header.IndexOf('=');
                    if (idx <= 0)
                        continue;

                    var key = header.Substring(0, idx).Trim();
                    if (key.Length <= 0)
                        continue;
                    var val = header.Substring(idx + 1).Trim();
                    ret[key] = val;
                }
            }

            if (args != null && args.Count > 0)
            {
                foreach (var header in args)
                {
                    if (header.Value.Type != ArgType.Header)
                        continue;

                    ret[header.Value.HttpName] = HttpUtility.UrlEncode(Convert.ToString(header.Value.Value));
                }
            }

            return ret;
        }

        static bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            // Always accept
            //Console.WriteLine("accept" + certificate.GetName());
            return true; //总是接受
        }
    }
}