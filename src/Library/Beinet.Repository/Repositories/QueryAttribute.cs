using System;

namespace Beinet.Repository.Repositories
{
    /// <summary>
    /// 仓储类的方法查询定义
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class QueryAttribute : Attribute
    {
        public QueryAttribute(string value)
        {
            Value = value;
        }
        /// <summary>
        /// 要使用的SQL
        /// </summary>
        public string Value { get; set; }
        /// <summary>
        /// 是否使用本地化查询语言，暂不支持JQL语法
        /// </summary>
        public bool NativeQuery { get; set; } = true;
    }
}
