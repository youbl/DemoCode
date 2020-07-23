using System;

namespace Beinet.Feign
{
    /// <summary>
    /// 方法请求参数特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public class RequestMappingAttribute : Attribute
    {
        private string method;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="route">方法路由</param>
        /// <param name="method">GET/POST/PUT/DELETE等</param>
        public RequestMappingAttribute(string route, string method = "GET")
        {
            Route = route;
            Method = method;
        }
        /// <summary>
        /// 方法路由
        /// </summary>
        public virtual string Route { get; private set; }

        /// <summary>
        /// GET/POST/PUT/DELETE等
        /// </summary>
        public virtual string Method
        {
            get { return method;}
            private set
            {
                if (string.IsNullOrEmpty(value))
                    value = "GET";
                else
                    value = value.ToUpper();// HTTP协议要求大写
                method = value;
            }
        }

        /// <summary>
        /// 请求使用的Header头信息，格式为： user-agent=beinet-1.0
        /// </summary>
        public virtual string[] Headers { get; set; }
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
    /// <summary>
    /// Put方法请求参数特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public class PutMappingAttribute : RequestMappingAttribute
    {
        public PutMappingAttribute(string route) : base(route, "PUT")
        {
        }
    }

    /// <summary>
    /// Delete方法请求参数特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public class DeleteMappingAttribute : RequestMappingAttribute
    {
        public DeleteMappingAttribute(string route) : base(route, "DELETE")
        {
        }
    }
}