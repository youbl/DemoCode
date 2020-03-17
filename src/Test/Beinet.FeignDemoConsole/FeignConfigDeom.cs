using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Beinet.Feign;

namespace Beinet.FeignDemoConsole
{
    public class FeignConfigDeom : FeignDefaultConfig
    {
        public override List<IRequestInterceptor> GetInterceptor()
        {
            return new List<IRequestInterceptor>()
            {
                new RequestInterceptDemo()
            };
        }
    }


    public class RequestInterceptDemo : IRequestInterceptor
    {
        /// <summary>
        /// 演示添加自定义Header
        /// </summary>
        /// <param name="request"></param>
        public void Apply(HttpWebRequest request)
        {
            request.Headers.Add("aaa", "bbb");
            request.UserAgent = "bbbbb";
            request.Timeout = 1000;
        }
    }
}
