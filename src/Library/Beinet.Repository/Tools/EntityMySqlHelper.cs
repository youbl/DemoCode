using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Text;
using Beinet.Repository.Entitys;
using Beinet.Repository.Repositories;

namespace Beinet.Repository.Tools
{
    class EntityMySqlHelper
    {
        public const string DATA_SOURCE_CONFIG = "DB_DEFAULT";

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
                    ret.KeyPropName = propName;
                    ret.KeyName = colName; // 注意带上了表名
                }

                var generatedAtt = property.GetCustomAttribute<GeneratedValueAttribute>();
                var isIdentity = (generatedAtt != null && generatedAtt.Strategy == GenerationType.IDENTITY);

                sbSelect.AppendFormat("{0},", colName);
                if (colAtt.Insertable && !isIdentity)
                {
                    sbInsertKey.AppendFormat("{0},", colName);
                    sbInsertVal.AppendFormat("@{0},", propName);
                }

                // 主键不能更新
                if (colAtt.Updatable && !isIdentity && idAtt == null)
                    sbUpdate.AppendFormat("{0}=@{1},", colName, propName);

                var field = new EntityData.FieldAtt
                {
                    Name = colName,
                    Att = colAtt,
                    Property = property,
                    IsIdentity = isIdentity,
                };
                ret.Fields.Add(propName, field);
            }

            if (string.IsNullOrEmpty(ret.KeyName))
                throw new ArgumentException("未找到主键设置：" + entityType.FullName);

            sbSelect.Remove(sbSelect.Length - 1, 1)
                .Insert(0, "SELECT ")
                .AppendFormat(" FROM {0}", ret.TableName);
            ret.SelectSql = sbSelect.ToString();

            sbInsertVal.Remove(sbInsertVal.Length - 1, 1);
            sbInsertKey.Remove(sbInsertKey.Length - 1, 1)
                .Insert(0, " (")
                .Insert(0, ret.TableName)
                .Insert(0, "INSERT INTO ")
                .Append(") VALUES(")
                .Append(sbInsertVal)
                .Append(")");
            ret.InsertSql = sbInsertKey.ToString();

            sbUpdate.Remove(sbUpdate.Length - 1, 1).Insert(0, " SET ")
                .Insert(0, ret.TableName)
                .Insert(0, "UPDATE ")
                .Append(" WHERE ")
                .Append(ret.KeyName)
                .Append("=@")
                .Append(ret.KeyPropName);
            ret.UpdateSql = sbUpdate.ToString();
        }

        /// <summary>
        /// 解析实体接口类型和基础类型的方法，并返回映射关系
        /// </summary>
        /// <param name="entityRepostoryType">用户仓储接口类</param>
        /// <param name="runnerType">内部JpaRepostoryBase类</param>
        /// <param name="data">实体属性与字段映射关系</param>
        /// <returns></returns>
        public Dictionary<MethodInfo, object> ParseRepostory(Type entityRepostoryType, Type runnerType, EntityData data)
        {
            var baseType = entityRepostoryType.GetInterfaces()
                .FirstOrDefault(
                    tp => tp.Namespace == typeof(JpaRepository<,>).Namespace && tp.Name == "JpaRepository`2");
            if (baseType == null)
                throw new ArgumentException("仓储类未实现JpaRepository：" + entityRepostoryType.FullName);

            // 用户仓储接口的所有方法
            var arrEntityMethods = entityRepostoryType.GetMethods().ToList();
            arrEntityMethods.AddRange(baseType.GetMethods());

            // 实现类的所有方法
            var arrBaseMethods = runnerType.GetMethods();

            var ret = new Dictionary<MethodInfo, object>();
            foreach (var method in arrEntityMethods)
            {
                if (method.IsGenericMethod)
                    throw new ArgumentException("暂不支持泛型方法：" + entityRepostoryType.FullName);

                object baseMethod = arrBaseMethods.FirstOrDefault(baseItem =>
                {
                    if (baseItem.Name != method.Name)
                        return false;
                    var baseParams = baseItem.GetParameters();
                    var entityPams = method.GetParameters();
                    if (baseParams.Length != entityPams.Length)
                        return false;
                    for (var i = 0; i < baseParams.Length; i++)
                    {
                        if (baseParams[i].ParameterType != entityPams[i].ParameterType)
                            return false;
                    }

                    return true;
                });
                if (baseMethod == null)
                {
                    // 不属于基础方法，说明是用户自定义方法，要根据Query注解或方法名，进行解析处理
                    baseMethod = ParseUserMethod(method, data);
                }

                ret.Add(method, baseMethod); 
            }

            return ret;
        }

        /// <summary>
        /// 返回执行用户自定义方法的Method
        /// </summary>
        /// <param name="runnerType">内部JpaRepostoryBase类</param>
        /// <returns></returns>
        public MethodInfo GetCustomerMethod(Type runnerType)
        {
            var ret = runnerType.GetMethod(nameof(JpaRepositoryBase<int, int>.CustomerMethod),
                BindingFlags.Instance | BindingFlags.Public);
            if (ret == null)
                throw new ArgumentException("未找到底层方法GetCustomerMethod");
            return ret;
        }

        /// <summary>
        /// 解析数据库连接串
        /// </summary>
        /// <param name="entityRepostoryType"></param>
        /// <returns></returns>
        public DataSourceConfigurationAttribute ParseConnectionString(Type entityRepostoryType)
        {
            var dstt = entityRepostoryType.GetCustomAttribute<DataSourceConfigurationAttribute>();
            if (dstt == null)
            {
                var configStr = ConfigurationManager.AppSettings[DATA_SOURCE_CONFIG];
                if (configStr == null)
                    configStr = ConfigurationManager.ConnectionStrings[DATA_SOURCE_CONFIG]?.ConnectionString;
                dstt = new DataSourceConfigurationAttribute(configStr);
            }
            else
            {
                // 用配置替换连接串中的占位符
            }

            if (string.IsNullOrEmpty(dstt.Value))
                throw new ConfigurationErrorsException("数据库连接配置不存在：" + DATA_SOURCE_CONFIG);

            // 占位符替换
            dstt.Value = ReplaceHolder(dstt.Value);
            if (string.IsNullOrEmpty(dstt.Value))
                throw new ConfigurationErrorsException("占位符处理后，数据库连接配置为空");

            return dstt;
        }

        /// <summary>
        /// 从Config配置文件里读取配置，替换占位符
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private string ReplaceHolder(string value)
        {
            int idx = -1;
            do
            {
                idx = value.IndexOf('{', idx + 1);
                if (idx < 0)
                    break;
                var endIdx = value.IndexOf('}', idx + 1);
                if (endIdx < 0)
                    break;
                if (idx == endIdx - 1)
                    throw new ConfigurationErrorsException("不允许出现空占位符");

                var argName = value.Substring(idx + 1, endIdx - idx - 1);

                object val = ConfigurationManager.AppSettings[argName] ??
                             ConfigurationManager.ConnectionStrings[argName]?.ConnectionString;
                // 先取参数列表
                if (val == null)
                {
                    throw new ConfigurationErrorsException($"占位符{{{argName}}}在配置中不存在");
                }

                value = value.Replace("{" + argName + "}", Convert.ToString(val));
            } while (true);

            return value;
        }

        /// <summary>
        ///  解析用户自定义方法
        /// </summary>
        /// <param name="method">方法</param>
        /// <param name="data">实体属性与字段映射</param>
        /// <returns></returns>
        private CustomData ParseUserMethod(MethodInfo method, EntityData data)
        {
            var queryAtt = method.GetCustomAttribute<QueryAttribute>();
            if (queryAtt == null)
            {
                var sql = new SqlParser().GetSql(method.Name, method.GetParameters(), data);
                queryAtt = new QueryAttribute(sql);
            }

            if (string.IsNullOrEmpty(queryAtt.Value))
                throw new ArgumentException("方法的Query不允许为空：" + method.Name);

            var ret = new CustomData
            {
                Sql = queryAtt.Value,
                IsModify = method.GetCustomAttribute<ModifingAttribute>() != null,
                ReturnType = method.ReturnType,
            };
            return ret;
        }

    }
}