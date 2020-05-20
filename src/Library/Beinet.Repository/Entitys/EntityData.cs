

using System.Collections.Generic;
using System.Reflection;

namespace Beinet.Repository.Entitys
{
    /// <summary>
    /// 实体类对应的物理表信息
    /// </summary>
    class EntityData
    {
        /// <summary>
        /// 表名，可能带有数据库名
        /// </summary>
        public string TableName { get; set; }
        /// <summary>
        /// 主键属性名
        /// </summary>
        public string KeyPropName { get; set; }
        /// <summary>
        /// 主键字段名
        /// </summary>
        public string KeyName { get; set; }
        /// <summary>
        /// 实体类属性与数据库字段名映射表
        /// </summary>
        public Dictionary<string, FieldAtt> Fields { get; set; } = new Dictionary<string, FieldAtt>();
        /// <summary>
        /// 不带Where的SQL SELECT语句
        /// </summary>
        public string SelectSql { get; set; }
        /// <summary>
        /// INSERT SQL，里面都是SQLParameter
        /// </summary>
        public string InsertSql { get; set; }
        /// <summary>
        /// UPDATE SQL，里面都是SQLParameter
        /// </summary>
        public string UpdateSql { get; set; }

        /// <summary>
        /// 主键是否自增
        /// </summary>
        public bool IsKeyIdentity => Fields[KeyPropName].IsIdentity;

        public class FieldAtt
        {
            /// <summary>
            /// 字段名
            /// </summary>
            public string Name { get; set; }
            /// <summary>
            /// 字段配置
            /// </summary>
            public ColumnAttribute Att { get; set; }
            /// <summary>
            /// 对应的类属性
            /// </summary>
            public PropertyInfo Property { get; set; }
            /// <summary>
            /// 是否自增
            /// </summary>
            public bool IsIdentity { get; set; }
        }
    }
}
