using System;

namespace Beinet.Repository.Repositories
{
    /// <summary>
    /// 数据库连接串定义
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
    public class DataSourceConfigurationAttribute : Attribute
    {
        public DataSourceConfigurationAttribute(string value)
        {
            Value = value;
        }
        /// <summary>
        /// 要使用的连接串
        /// </summary>
        public string Value { get; set; }
    }
}
