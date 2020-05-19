﻿

using System.Collections.Generic;

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
        }
    }
}
