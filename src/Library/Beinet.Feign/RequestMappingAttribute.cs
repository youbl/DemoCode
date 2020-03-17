using System;

namespace Beinet.Feign
{
    /// <summary>
    /// 方法请求参数特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public class RequestMappingAttribute : Attribute
    {
        public RequestMappingAttribute(string route, string method = "GET")
        {
            Route = route;
            Method = method;
        }

        public string Route { get; private set; }

        public string Method { get; private set; }
    }

    /// <summary>
    /// Get方法请求参数特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public class GetMappingAttribute : RequestMappingAttribute
    {
        public GetMappingAttribute(string route) : base(route)
        {
        }
    }

    /// <summary>
    /// Post方法请求参数特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public class PostMappingAttribute : RequestMappingAttribute
    {
        public PostMappingAttribute(string route) : base(route, "POST")
        {
        }
    }
}