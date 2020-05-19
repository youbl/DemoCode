using System;

namespace Beinet.Repository.Entitys
{
    /// <summary>
    /// 实体类的字段定义标记
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class ColumnAttribute : Attribute
    {
        /// <summary>
        /// 数据库字段名，默认使用实体属性名
        /// </summary>
        public String Name { get; set; }
        /// <summary>
        /// 该字段值是否唯一
        /// </summary>
        public bool Unique { get; set; }
        /// <summary>
        /// 该字段值是否可null
        /// </summary>
        public bool Nullable { get; set; } = true;
        /// <summary>
        /// 该字段是否需要插入，比如创建时间应该设置为false，由数据库自动生成
        /// </summary>
        public bool Insertable { get; set; } = true;
        /// <summary>
        /// 该字段是否允许更新，比如创建时间应该设置为false，不允许改变
        /// </summary>
        public bool Updatable { get; set; } = true;
        /// <summary>
        /// 需要JPA自动创建表时，该字段的定义语句
        /// </summary>
        public String ColumnDefinition { get; set; }
        /// <summary>
        /// 映射到多张表时，该字段所属的表名,默认使用主表
        /// </summary>
        public String Table { get; set; }
        /// <summary>
        /// varchar类型字段的长度，默认255
        /// </summary>
        public int Length { get; set; } = 255;
        /// <summary>
        /// double类型字段的总长度
        /// </summary>
        public int Precision { get; set; }
        /// <summary>
        /// double类型字段的小数长度
        /// </summary>
        public int Scale { get; set; }
    }
}