using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using NLog;

namespace Beinet.Feign
{
    /// <summary>
    /// Web请求方法
    /// </summary>
    internal static class WebHelper
    {
        private static ILogger _logger = LogManager.GetCurrentClassLogger();

        public delegate void BeforeRequest(HttpWebRequest request);

        public delegate void AfterRequest(HttpWebRequest request, HttpWebResponse response);

        /// <summary>
        /// 获取指定url的响应字符串
        /// </summary>
        /// <param name="url">url</param>
        /// <param name="method">GET/POST/PUT/DELETE等</param>
        /// <param name="postStr">要发送的body数据</param>
        /// <param name="headers">要传输的header清单</param>
        /// <param name="interceptors">拦截器清单</param>
        /// <param name="level">日志级别</param>
        /// <returns></returns>
        public static string GetPage(string url, string method, string postStr,
            Dictionary<string, string> headers,
            List<IRequestInterceptor> interceptors,
            LogLevel level)
        {
            if (!IsUrl(url))
                url = "http://" + url;

            var isGet = method == "GET"; // GET时，不对参数进行序列化处理

            var request = (HttpWebRequest) WebRequest.Create(url);
            request.Method = method;
            request.AllowAutoRedirect = false;
            request.Headers.Add("Accept-Encoding", "gzip, deflate");
            request.Headers.Add("Accept-Charset", Encoding.UTF8.WebName);
            // 禁止缓存可能导致的bug
            request.Headers.Add(HttpRequestHeader.CacheControl, "no-cache");

            if (headers != null)
            {
                foreach (KeyValuePair<string, string> pair in headers)
                {
                    if (pair.Key == null || pair.Value == null)
                        continue;
                    if (pair.Key.Equals("ContentType", StringComparison.OrdinalIgnoreCase) ||
                        pair.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                        request.ContentType = pair.Value;
                    else if (pair.Key.Equals("UserAgent", StringComparison.OrdinalIgnoreCase) ||
                             pair.Key.Equals("User-Agent", StringComparison.OrdinalIgnoreCase))
                        request.UserAgent = pair.Value;
                    else
                        request.Headers[pair.Key] = pair.Value; // 不用add，避免跟前面的key重复
                }
            }

            if (interceptors != null)
            {
                foreach (var interceptor in interceptors)
                {
                    interceptor.BeforeRequest(request, postStr);
                }
            }

            if (request.UserAgent == null)
                request.UserAgent = "Beinet feign.net1.0";

            if (!isGet)
            {
                if (request.ContentType == null)
                    request.ContentType = "application/json";

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

            _logger.Log(level, () =>
            {
                var sb = new StringBuilder();
                sb.AppendFormat("==Request Info:==\n{0} {1}\n", method, url);
                if (!string.IsNullOrEmpty(postStr))
                    sb.AppendFormat("Body: {0}\n", postStr);
                if (request.Headers.Count > 0)
                {
                    sb.AppendLine("Headers:");
                    foreach (string header in request.Headers)
                    {
                        sb.AppendFormat("  {0}={1}\n", header, request.Headers[header]);
                    }
                }

                return sb.ToString();
            });

            try
            {
                using (var response = (HttpWebResponse) request.GetResponse())
                {
                    var ret = GetResponseString(response);
                    if (interceptors != null)
                    {
                        foreach (var interceptor in interceptors)
                        {
                            interceptor.AfterRequest(request, response, ret, null);
                        }
                    }


                    var sb = new StringBuilder();
                    sb.AppendFormat("==Response Headers:==\n");
                    foreach (string header in response.Headers)
                    {
                        sb.AppendFormat("  {0}={1}\n", header, response.Headers[header]);
                    }

                    sb.Append(ret);
                    _logger.Log(level, sb.ToString);

                    return ret;
                }
            }
            catch (Exception exp)
            {
                if (interceptors != null)
                {
                    var responseStr = "";
                    HttpWebResponse response = null;
                    if (exp is WebException webExp && webExp.Response != null && webExp.Response is HttpWebResponse)
                    {
                        response = (HttpWebResponse) webExp.Response;
                        responseStr = GetResponseString(response);
                    }

                    foreach (var interceptor in interceptors)
                    {
                        interceptor.AfterRequest(request, response, responseStr, exp);
                    }
                }

                throw;
            }
        }

        /// <summary>
        /// 从Web响应流中读取字符串
        /// </summary>
        /// <param name="response"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 指定的字符串是否http协议的url
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool IsUrl(string str)
        {
            if (str.IndexOf("http://", StringComparison.OrdinalIgnoreCase) != 0 &&
                str.IndexOf("https://", StringComparison.OrdinalIgnoreCase) != 0)
                return false;

            //            if (str.IndexOf('\n') >= 0)
            //                return false;
            return true;
        }

    }
}