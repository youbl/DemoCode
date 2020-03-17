using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace Beinet.Feign
{
    /// <summary>
    /// Feign请求拦截器
    /// </summary>
    public interface IRequestInterceptor
    {
        /// <summary>
        /// Web请求拦截方法
        /// </summary>
        /// <param name="request"></param>
        void Apply(HttpWebRequest request);
    }
}
