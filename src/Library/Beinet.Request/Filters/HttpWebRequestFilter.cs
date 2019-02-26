using System;
using System.Net;

namespace Beinet.Request.Filters
{
    /// <summary>
    /// 对HttpWebRequest的生命周期进行控制
    /// </summary>
    public abstract class HttpWebRequestFilter : IHttpWebRequestFilter
    {
        /// <summary>
        /// 创建HttpWebRequest前的操作
        /// </summary>
        /// <param name="uri">要创建的Uri对象</param>
        /// <returns></returns>
        public virtual Uri OnCreate(Uri uri)
        {
            return uri;
        }

        /// <summary>
        /// 发出请求前的操作
        /// </summary>
        /// <param name="request">要发请求的Request源</param>
        public virtual void BeforeGetResponse(HttpWebRequest request)
        {
        }

        /// <summary>
        /// 完成请求之后的操作
        /// </summary>
        /// <param name="request">发出请求的Request源</param>
        /// <param name="response">收到的响应</param>
        /// <param name="exception">发出请求出现的异常</param>
        public virtual void AfterGetResponse(HttpWebRequest request, HttpWebResponse response, Exception exception)
        {
            
        }

        /// <summary>
        /// 当前Filter的执行顺序
        /// </summary>
        public virtual int Sort { get; set; }
    }
}
