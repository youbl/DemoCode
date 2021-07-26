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
        // 返回HTTP请求前后的拦截器
        public override List<IRequestInterceptor> GetInterceptor()
        {
            return new List<IRequestInterceptor>()
            {
                new RequestInterceptDemo()
            };
        }

        // 如果要对post数据，自定义序列化器，可以重写此方法
        public override string Encoding(object arg)
        {
            return base.Encoding(arg);
        }

        // 如果要对api返回的数据，自定义反序列化器，可以重写此方法
        public override object Decoding(string str, Type returnType)
        {
            // 注意：返回的object必须是returnType类型
            return base.Decoding(str, returnType);
        }

        // 如果要自行处理http请求返回的异常，重写此方法，返回null将不抛出异常
        public override Exception ErrorHandle(Exception exp)
        {
            return base.ErrorHandle(exp);
        }

        public override NLog.LogLevel LoggerLevel()
        {
            return NLog.LogLevel.Info;
        }
    }

    public class RequestInterceptDemo : IRequestInterceptor
    {
        private DateTime _beginTime;

        // 需要对FeignClient的属性Url进行处理时，在这里操作，注：不包含路由
        public string OnCreate(string url)
        {
            return url;
        }

        // 在HttpWebRequest.GetResponse之前执行的方法，比如记录日志，添加统一header
        public void BeforeRequest(HttpWebRequest request, string postStr)
        {
            request.Headers.Add("aaa", "bbb");
            request.UserAgent = "bbbbb";
            request.Timeout = 1000;

            Console.WriteLine(request.Method + " " + request.RequestUri);
            Console.WriteLine(request.Headers);
            Console.WriteLine(postStr);

            _beginTime = DateTime.Now;
        }

        // 在HttpWebRequest.GetResponse之后执行的方法，比如记录日志
        public void AfterRequest(HttpWebRequest request, HttpWebResponse response, string responseStr, Exception exp)
        {
            var costTime = (DateTime.Now - _beginTime).TotalMilliseconds.ToString("N0");
            Console.WriteLine($"{request.RequestUri} 耗时:{costTime}毫秒");
            Console.WriteLine($"响应内容: {responseStr}");
            if (response != null)
                Console.WriteLine(((int)response.StatusCode).ToString() + ":" + response.Headers);
            else if (exp != null)
                Console.WriteLine($"出错了：{exp.Message}");
        }
    }
}
