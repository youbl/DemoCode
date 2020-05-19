using System;

namespace Beinet.Repository.Entitys
{
    /// <summary>
    /// 实体类标记
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class TableAttribute : Attribute
    {
        /// <summary>
        /// 物理表名，不设置时，使用类名
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 数据库名称，如果有值，所有查询将带上数据库名
        /// </summary>
        public string Catalog { get; set; }

        // 数据库名称,MySQL不支持
        // public string Schema { get; set; }
        // UniqueConstraint[] uniqueConstraints;
        // Index[] indexes;
    }
}