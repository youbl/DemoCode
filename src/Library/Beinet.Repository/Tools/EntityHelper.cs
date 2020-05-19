using System;
using System.Reflection;
using System.Text;
using Beinet.Repository.Entitys;

namespace Beinet.Repository.Tools
{
    class EntityMySqlHelper
    {
        /// <summary>
        /// 解析实体类，暂时没有考虑数据库方言
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public EntityData ParseEntity(Type type)
        {
            if (type == null)
                throw new ArgumentException("实体类型不能为空");

            if (type.GetCustomAttribute<EntityAttribute>() == null)
                throw new ArgumentException("实体类型必须添加Entity注解:Not a managed type: class " + type.FullName);

            var ret = new EntityData
            {
                TableName = GetTableName(type),
            };
            ParseColumns(type, ret);
            return ret;
        }

        string GetTableName(Type type)
        {
            string tableName;
            var tableAtt = type.GetCustomAttribute<TableAttribute>();
            if (tableAtt == null || string.IsNullOrEmpty(tableAtt.Name))
            {
                tableName = type.Name;
            }
            else
            {
                tableName = tableAtt.Name;
            }

            if (tableAtt != null && !string.IsNullOrEmpty(tableAtt.Catalog))
            {
                tableName = tableAtt.Catalog + '.' + tableName;
            }

            return tableName;
        }

        void ParseColumns(Type type, EntityData ret)
        {
            var properties = type.GetProperties();
            if (properties.Length <= 0)
                throw new ArgumentException("指定的实体类型，没有找到公共属性：" + type.FullName);

            var sbSelect = new StringBuilder();
            var sbInsertKey = new StringBuilder();
            var sbInsertVal = new StringBuilder();
            var sbUpdate = new StringBuilder();

            foreach (var property in properties)
            {
                var propName = property.Name;
                var colName = propName;
                var colAtt = property.GetCustomAttribute<ColumnAttribute>() ?? new ColumnAttribute();
                if (!string.IsNullOrEmpty(colAtt.Name))
                    colName = colAtt.Name;
                if (!string.IsNullOrEmpty(colAtt.Table))
                    colName = colAtt.Table + '.' + colName;

                // 找主键
                var idAtt = property.GetCustomAttribute<IdAttribute>();
                if (idAtt != null)
                {
                    ret.KeyName = colName; // 注意带上了表名
                }

                var generatedAtt = property.GetCustomAttribute<GeneratedValueAttribute>();
                var isAuto = (generatedAtt == null || generatedAtt.Strategy == GenerationType.AUTO);

                sbSelect.AppendFormat("{0},", colName);
                if (colAtt.Insertable && isAuto)
                {
                    sbInsertKey.AppendFormat("{0},", colName);
                    sbInsertVal.AppendFormat("@{0},", propName);
                }

                // 主键不能更新
                if (colAtt.Updatable && isAuto && idAtt == null)
                    sbUpdate.AppendFormat("{0}=@{1},", colName, propName);

                var field = new EntityData.FieldAtt
                {
                    Name = colName,
                    Att = colAtt,
                };
                ret.Fields.Add(propName, field);
            }

            sbSelect.Remove(sbSelect.Length - 1, 1)
                .Insert(0, "SELECT ")
                .AppendFormat(" FROM {0}", ret.TableName);
            ret.SelectSql = sbSelect.ToString();

            sbInsertVal.Remove(sbSelect.Length - 1, 1);
            sbInsertKey.Remove(sbSelect.Length - 1, 1)
                .Insert(0, " (")
                .Insert(0, ret.TableName)
                .Insert(0, "INSERT INTO ")
                .Append(") VALUES(")
                .Append(sbInsertVal)
                .Append(")");
            ret.InsertSql = sbInsertKey.ToString();

            sbUpdate.Remove(sbSelect.Length - 1, 1).
                Insert(0, " SET ")
                .Insert(0, ret.TableName)
                .Insert(0, "UPDATE ");
        }
    }
}