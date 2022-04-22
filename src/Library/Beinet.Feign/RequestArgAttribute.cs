using System;

namespace Beinet.Feign
{
    /// <summary>
    /// 方法的参数特性定义父类
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
    public class RequestArgAttribute : Attribute
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="type">HTTP发送形式</param>
        /// <param name="httpName">HTTP发送时使用的参数名称</param>
        public RequestArgAttribute(ArgType type, string httpName = "")
        {
            Type = type;
            Name = (httpName ?? "".Trim());
        }
        /// <summary>
        /// HTTP发送形式
        /// </summary>
        public virtual ArgType Type { get; private set; }
        /// <summary>
        /// HTTP发送时使用的参数名称
        /// </summary>
        public virtual string Name { get; private set; }
    }

    /// <summary>
    /// 方法的参数作为Header发送的特性定义
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
    public class RequestHeaderAttribute : RequestArgAttribute
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="headerName">HTTP发送Header时使用的参数名称</param>
        public RequestHeaderAttribute(string headerName = "") : base(ArgType.Header, headerName)
        {
        }
    }


    /// <summary>
    /// 方法的参数作为URL参数发送的特性定义
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
    public class RequestParamAttribute : RequestArgAttribute
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="queryName">HTTP发送URL时使用的参数名称</param>
        public RequestParamAttribute(string queryName = "") : base(ArgType.Param, queryName)
        {
        }
    }

    /// <summary>
    /// 方法的参数作为Body发送的特性定义
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
    public class RequestBodyAttribute : RequestArgAttribute
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public RequestBodyAttribute() : base(ArgType.Body, "")
        {
        }
    }


    /// <summary>
    /// 方法的参数不传递。
    /// 通常用于在url里拼接参数
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
    public class RequestNoneAttribute : RequestArgAttribute
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public RequestNoneAttribute() : base(ArgType.None, "")
        {
        }
    }
    /// <summary>
    /// 参数发送时使用的类型
    /// </summary>
    public enum ArgType
    {
        /// <summary>
        /// 不作为参数
        /// </summary>
        None,
        /// <summary>
        /// URL参数
        /// </summary>
        Param,
        /// <summary>
        /// POST参数
        /// </summary>
        Body,
        /// <summary>
        /// Header参数
        /// </summary>
        Header
    }
}