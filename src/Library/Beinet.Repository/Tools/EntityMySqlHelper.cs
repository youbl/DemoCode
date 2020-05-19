using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Beinet.Repository.Entitys;

namespace Beinet.Repository.Tools
{
    class EntityMySqlHelper
    {
        private static ConcurrentDictionary<Type, EntityData> _arrCache = new ConcurrentDictionary<Type, EntityData>();
        /// <summary>
        /// 解析实体类，暂时没有考虑数据库方言
        /// </summary>
        /// <param name="entityType"></param>
        /// <param name="keyType"></param>
        /// <returns></returns>
        public EntityData ParseEntity(Type entityType, Type keyType)
        {
            if (entityType == null)
                throw new ArgumentException("实体类型不能为空");

            return _arrCache.GetOrAdd(entityType, typeInner =>
            {
                if (typeInner.GetCustomAttribute<EntityAttribute>() == null)
                    throw new ArgumentException("实体类型必须添加Entity注解:Not a managed type: class " + typeInner.FullName);

                var ret = new EntityData
                {
                    TableName = GetTableName(typeInner),
                };
                ParseColumns(typeInner, keyType, ret);
                return ret;
            });
        }

        /// <summary>
        /// 解析并返回表名
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 解析并收集字段列表
        /// </summary>
        /// <param name="entityType"></param>
        /// <param name="keyType"></param>
        /// <param name="ret"></param>
        void ParseColumns(Type entityType, Type keyType, EntityData ret)
        {
            var properties = entityType.GetProperties();
            if (properties.Length <= 0)
                throw new ArgumentException("指定的实体类型，没有找到公共属性：" + entityType.FullName);

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
                    if (property.PropertyType != keyType)
                        throw new ArgumentException("指定的主键与泛型类型不匹配：" + entityType.FullName);
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
                    Property = property,
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

        /// <summary>
        /// 解析实体接口类型和基础类型的方法，并返回映射关系
        /// </summary>
        /// <param name="entityRepostoryType"></param>
        /// <param name="baseType"></param>
        /// <returns></returns>
        public Dictionary<MethodInfo, MethodInfo> ParseRepostory(Type entityRepostoryType, Type baseType)
        {
            var arrEntityMethods = entityRepostoryType.GetMethods();
            var arrBaseMethods = baseType.GetMethods();

            var ret = new Dictionary<MethodInfo, MethodInfo>();
            foreach (var method in arrEntityMethods)
            {
                if (method.IsGenericMethod)
                    throw new ArgumentException("暂不支持泛型方法：" + entityRepostoryType.FullName);

                var baseMethod = arrBaseMethods.FirstOrDefault(baseItem =>
                {
                    if (baseItem.Name != method.Name)
                        return false;
                    var baseParams = baseItem.GetParameters();
                    var entityPams = method.GetParameters();
                    if (baseParams.Length != entityPams.Length)
                        return false;
                    for(var i = 0; i < baseParams.Length; i++)
                    {
                        if (baseParams[i].ParameterType != entityPams[i].ParameterType)
                            return false;
                    }

                    return true;
                });
                ret.Add(method, baseMethod); // 可能找不到，value就存null
            }

            return ret;
        }
    }
}