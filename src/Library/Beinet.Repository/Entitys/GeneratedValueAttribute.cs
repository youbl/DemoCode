using System;

namespace Beinet.Repository.Entitys
{
    /// <summary>
    /// 主键字段生成策略
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class GeneratedValueAttribute : Attribute
    {
        /// <summary>
        /// 指定主键生成策略
        /// </summary>
        public GenerationType Strategy { get; set; } = GenerationType.AUTO;
    }


    /// <summary>
    /// 主键生成策略
    /// </summary>
    public enum GenerationType
    {
        /// <summary>
        /// 默认，由JPA自动选择合适策略
        /// </summary>
        AUTO,
        /// <summary>
        /// 数据库自增方式，注：Oracle没有自增字段
        /// </summary>
        IDENTITY
        //            /// <summary>
        //            /// 通过一张序列表来生成主, 配合注解 TableGenerator 使用
        //            /// </summary>
        //            TABLE,
        //            /// <summary>
        //            /// 序列生成主键，配合注解 SequenceGenerator 使用；MySQL不支持
        //            /// </summary>
        //            SEQUENCE,
    }
}