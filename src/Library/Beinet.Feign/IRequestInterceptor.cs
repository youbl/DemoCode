using System;
using System.Net;

namespace Beinet.Feign
{
    /// <summary>
    /// Feign请求拦截器
    /// </summary>
    public interface IRequestInterceptor
    {
        /// <summary>
        /// 对FeignClient的Url进行自定义处理
        /// </summary>
        /// <param name="originUrl">FeignClient的Url属性</param>
        /// <returns></returns>
        string OnCreate(string originUrl);

        /// <summary>
        /// 在HttpWebRequest.GetResponse之前执行的方法
        /// </summary>
        /// <param name="request">请求对象</param>
        void BeforeRequest(HttpWebRequest request);

        /// <summary>
        /// 在HttpWebRequest.GetResponse之后执行的方法
        /// </summary>
        /// <param name="request">请求对象</param>
        /// <param name="response">响应对象</param>
        /// <param name="exception">请求中出现的异常</param>
        void AfterRequest(HttpWebRequest request, HttpWebResponse response, Exception exception);
    }
}
