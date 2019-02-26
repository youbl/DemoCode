using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization;
using System.Web;
using Beinet.Request.Filters;

namespace Beinet.Request
{
    internal class ExtendHttpRequestCreator : IWebRequestCreate
    {
        /// <summary>
        /// 已注册的所有拦截器
        /// </summary>
        public static List<IHttpWebRequestFilter> Filters { get; private set; } = new List<IHttpWebRequestFilter>();

        static ExtendHttpRequestCreator()
        {
            Register(new UserAgentFilter(100));
            Register(new NLogFilter(), 200);
        }

        /// <summary>
        /// 拦截器注册方法
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="order"></param>
        public static void Register(IHttpWebRequestFilter filter, int order = 0)
        {
            lock (Filters)
            {
                Filters.Add(filter);
                Filters = Filters.OrderBy(o => o.Sort).ToList();
            }
        }

        /// <summary>
        /// 创建WebRequest
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public WebRequest Create(Uri uri)
        {
            foreach (var filter in Filters)
            {
                uri = filter.OnCreate(uri);
            }
            return new ExtendHttpWebRequest(uri);
        }
    }

    /// <summary>
    /// HttpWebRequest扩展实现类
    /// </summary>
    public class ExtendHttpWebRequest : HttpWebRequest
    {
        private static readonly ConstructorInfo _constructor = typeof(HttpWebRequest).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic, null,
            new[] { typeof(Uri), typeof(ServicePoint) }, null);
        private static readonly StreamingContext _context = new StreamingContext(StreamingContextStates.Clone);
        private static readonly SerializationInfo _serializationInfo =
            new SerializationInfo(typeof(HttpWebRequest), new FormatterConverter());

        private ExtendHttpRequestStream _httpRequestStream;

        static ExtendHttpWebRequest()
        {
            // 因为HttpWebRequest过期原因，只能继承HttpWebRequest(SerializationInfo, StreamingContext)，要写这些代码
            var createor =
                (IWebRequestCreate)Activator.CreateInstance(
                    typeof(HttpWebRequest).Assembly.GetType("System.Net.HttpRequestCreator"));
            var webRequest = createor.Create(new Uri("http://blank")) as ISerializable;
            // 调用原生方法，填充 _HttpRequestHeaders
            webRequest.GetObjectData(_serializationInfo, _context);
        }

#pragma warning disable 618
        /// <summary>
        /// 因为HttpWebRequest只开放了 HttpWebRequest(SerializationInfo, StreamingContext)
        /// 这一个构造函数，因此只能从它继承
        /// </summary>
        /// <param name="uri"></param>
        public ExtendHttpWebRequest(Uri uri) : base(_serializationInfo, _context)
#pragma warning restore 618
        {
            _constructor.Invoke(this, new[] { uri, (object)null });
        }

        /// <summary>
        /// 返回可以复制body数据的流, 用于过滤器可以读取body进行处理
        /// </summary>
        /// <returns></returns>
        public override Stream GetRequestStream()
        {
            return _httpRequestStream ?? (_httpRequestStream = new ExtendHttpRequestStream(base.GetRequestStream()));
        }

        /// <summary>
        /// 重写GetResponse，实现请求前请求后的拦截器操作.
        /// </summary>
        /// <returns></returns>
        public override WebResponse GetResponse()
        {
            try
            {
                return GetResponseInter();
            }
            finally
            {
                _httpRequestStream?.Dispose();
            }
        }

        private WebResponse GetResponseInter()
        {
            HttpWebResponse response = null;
            Exception exception = null;

            var filters = ExtendHttpRequestCreator.Filters;
            foreach (IHttpWebRequestFilter filter in filters)
            {
                filter.BeforeGetResponse(this);
            }
            try
            {
                response = (HttpWebResponse)base.GetResponse();
            }
            catch (WebException webExp)
            {
                exception = webExp;
                if (webExp.Response != null)
                {
                    response = (HttpWebResponse)webExp.Response;
                }
            }
            catch (Exception exp)
            {
                exception = exp;
            }

            if (response != null)
            {
                // 组装成可重读的对象
                response = new ExtendHttpWebResponse(response);
            }
            foreach (IHttpWebRequestFilter filter in filters)
            {
                filter.AfterGetResponse(this, response, exception);
            }
            if (exception != null)
            {
                throw exception;
            }
            return response;
        }

        public virtual bool HasRequestStream => _httpRequestStream != null;
        /// <summary>
        /// 读取Request BODY的复制流
        /// </summary>
        /// <returns></returns>
        public virtual byte[] GetRequestBodyData()
        {
            if (!HasRequestStream)
                return null;
            var len = (int)_httpRequestStream.CopyedStream.Length;
            var arr = new byte[len];
            _httpRequestStream.CopyedStream.Read(arr, 0, len);
            return arr;
        }
        /// <summary>
        /// 读取Request BODY的字符串内容
        /// </summary>
        /// <returns></returns>
        public virtual string GetRequestBodyStr()
        {
            if (!HasRequestStream)
                return null;
            using (var sr = new StreamReader(_httpRequestStream.CopyedStream))
            {
                return sr.ReadToEnd();
            }
        }
    }
}
