using System;
using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json;

namespace Beinet.Feign
{
    /// <summary>
    /// Feign请求配置接口
    /// </summary>
    public interface IFeignConfig
    {
        
        /// <summary>
        /// 请求拦截接口清单
        /// </summary>
        /// <returns></returns>
        List<IRequestInterceptor> GetInterceptor();

        /// <summary>
        /// 序列化到HTTP Body的方法(用于POST、PUT等）
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        string Encoding(object arg);

        /// <summary>
        /// 返回正常时的反序列化方法
        /// </summary>
        /// <param name="str"></param>
        /// <param name="returnType"></param>
        /// <returns></returns>
        object Decoding(string str, Type returnType);

        /// <summary>
        /// 要记录日志的级别，默认为Debug
        /// </summary>
        /// <returns>日志级别</returns>
        NLog.LogLevel LoggerLevel();

        /// <summary>
        /// 出异常时的异常处理方法
        /// </summary>
        /// <param name="exp"></param>
        /// <returns></returns>
        Exception ErrorHandle(Exception exp);
    }

    public class FeignDefaultConfig : IFeignConfig
    {
        public virtual List<IRequestInterceptor> GetInterceptor()
        {
            return null;
        }

        public virtual string Encoding(object arg)
        {
            if (arg == null)
                return null;
            if (arg is string str)
                return str;
            return JsonConvert.SerializeObject(arg);
        }

        public virtual object Decoding(string str, Type returnType)
        {
            if (returnType == typeof(void))
                return null;
            else if (returnType == typeof(object))
                return str;

            if (string.IsNullOrEmpty(str))
                return TypeHelper.GetDefaultValue(returnType);

            if (returnType == typeof(string))
                return str;

            return JsonConvert.DeserializeObject(str, returnType);
        }

        public virtual NLog.LogLevel LoggerLevel()
        {
            return NLog.LogLevel.Debug;
        }

        public virtual Exception ErrorHandle(Exception exp)
        {
            if (exp is WebException webExp)
            {
                if (webExp.Response != null)
                {
                    using (var responseErr = (HttpWebResponse)webExp.Response)
                    {
                        var result = WebHelper.GetResponseString(responseErr);
                        return new Exception(result, webExp);
                        // return Decoding(result, errorType);
                    }
                }
            }

            return exp;
        }
        
    }
}
