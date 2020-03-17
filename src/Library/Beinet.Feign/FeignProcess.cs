using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;

namespace Beinet.Feign
{
    /// <summary>
    /// Feign主执行类
    /// </summary>
    public class FeignProcess
    {
        private const string GET = "GET";

        public FeignClientAttribute Att { get; set; }

        static Dictionary<string, object> _configs = new Dictionary<string, object>();

        static FeignProcess()
        {
            foreach (string setting in ConfigurationManager.AppSettings)
            {
                _configs[setting] = ConfigurationManager.AppSettings[setting];
            }
        }

        /// <summary>
        /// 主调方法
        /// </summary>
        /// <param name="att"></param>
        /// <param name="args">方法的入参数列表</param>
        /// <param name="returnType">方法的返回参数类型</param>
        /// <returns></returns>
        public object Process(RequestMappingAttribute att, Dictionary<string, object> args, Type returnType)
        {
            var method = att.Method;
            if (string.IsNullOrEmpty(method))
                method = GET;
            else
                method = method.ToUpper();

            // 拼接url和路由
            var url = CombineUrl(Att.Url, att.Route);
            // 用配置和参数替换url里的内容
            url = ParseUrl(url, args, method == GET);

            var bodyArg = args != null && args.Count > 0 ? args.First().Value : null;

            Type realType;
            try
            {
                var httpReturn = GetPage(url, method, bodyArg);
                var ret = Att.Config.Decoding(httpReturn, returnType);
                realType = ret.GetType();
                if (returnType.IsInstanceOfType(ret))
                    return ret;
            }
            catch (Exception exp)
            {
                var handledExp = Att.Config.ErrorHandle(exp);
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
        /// <param name="isGet"></param>
        /// <returns></returns>
        static string ParseUrl(string url, Dictionary<string, object> args, bool isGet)
        {
            if (string.IsNullOrEmpty(url))
                throw new Exception("url不允许为空");

            if (args == null)
                args = _configs;
            else
            {
                // 把配置加入参数列表
                foreach (var config in _configs)
                {
                    if (!args.ContainsKey(config.Key))
                        args.Add(config.Key, config.Value);
                }
            }

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
                if (args == null)
                    throw new Exception($"占位符{{{argName}}}不存在，因为参数列表为空");

                if (!args.TryGetValue(argName, out var val))
                {
                    throw new Exception($"占位符{{{argName}}}不存在");
                }

                url = url.Replace("{" + argName + "}", Convert.ToString(val));
            } while (true);

            if (isGet && args != null && args.Count > 0)
            {
                var sbArgs = new StringBuilder();
                if (url.IndexOf('?') > 0)
                    sbArgs.Append('&');
                else
                    sbArgs.Append('?');
                foreach (var pair in args)
                {
                    var encodingVal = System.Web.HttpUtility.UrlEncode(Convert.ToString(pair.Value));
                    sbArgs.AppendFormat("{0}={1}&", pair.Key, encodingVal);
                }

                url += sbArgs;
            }

            return url;
        }

        string GetPage(string url, string method, object arg)
        {
            if (!IsUrl(url))
                url = "http://" + url;
            var isGet = method == GET; // GET时，不对参数进行序列化处理

            var request = (HttpWebRequest) WebRequest.Create(url);
            request.Method = method;
            request.AllowAutoRedirect = false;
            request.Headers.Add("Accept-Encoding", "gzip, deflate");
            request.Headers.Add("Accept-Charset", Encoding.UTF8.WebName);
            // 禁止缓存可能导致的bug
            request.Headers.Add(HttpRequestHeader.CacheControl, "no-cache");

            var interceptors = Att.Config.GetInterceptor();
            if (interceptors != null)
            {
                foreach (var interceptor in interceptors)
                {
                    interceptor.Apply(request);
                }
            }

            if (!isGet)
            {
                if (request.ContentType == null)
                    request.ContentType = "application/json";

                var postStr = "";
                if (arg != null)
                {
                    postStr = Att.Config.Encoding(arg);
                }

                if (string.IsNullOrEmpty(postStr))
                {
                    request.ContentLength = 0; // POST必须设置的属性
                }
                else
                {
                    // 把数据转换为字节数组
                    byte[] l_data = Encoding.UTF8.GetBytes(postStr);
                    // request.ContentLength = l_data.Length;  // 可以不设置，写流会自动设置Length
                    // 打开GetRequestStream之后，不允许设置ContentLength，会抛异常
                    // ContentLength设置后，reqStream.Close前必须写入相同字节的数据，否则Request会被取消
                    using (var newStream = request.GetRequestStream())
                    {
                        newStream.Write(l_data, 0, l_data.Length);
                    }
                }
            }

            using (var response = (HttpWebResponse) request.GetResponse())
            {
                return GetResponseString(response);
            }
        }

        public static string GetResponseString(HttpWebResponse response, Encoding encoding = null)
        {
            encoding = encoding ?? Encoding.UTF8;
            using (var stream = response.GetResponseStream())
            {
                if (stream == null)
                    return "GetResponseStream is null";
                string str;
                string contentEncoding = response.ContentEncoding.ToLower();
                if (contentEncoding.Contains("gzip"))
                    using (Stream stream2 = new GZipStream(stream, CompressionMode.Decompress))
                        str = GetFromStream(stream2, encoding);
                else if (contentEncoding.Contains("deflate"))
                    using (Stream stream2 = new DeflateStream(stream, CompressionMode.Decompress))
                        str = GetFromStream(stream2, encoding);
                else
                    str = GetFromStream(stream, encoding);
                return str;
            }
        }

        static string GetFromStream(Stream stream, Encoding encoding)
        {
            using (StreamReader reader = new StreamReader(stream, encoding))
                return reader.ReadToEnd();
        }

        static bool IsUrl(string str)
        {
            if (str.IndexOf("http://", StringComparison.OrdinalIgnoreCase) != 0 &&
                str.IndexOf("https://", StringComparison.OrdinalIgnoreCase) != 0)
                return false;

//            if (str.IndexOf('\n') >= 0)
//                return false;
            return true;
        }

        static string CombineUrl(string url, string route)
        {
            route = route ?? "";
            if (IsUrl(route))
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

    }
}