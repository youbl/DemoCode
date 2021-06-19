using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using Beinet.Repository.Entitys;
using Beinet.Repository.Tools;
using MySql.Data.MySqlClient;

namespace Beinet.Repository.Repositories
{
    /// <summary>
    /// JPA操作类
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <typeparam name="ID">主键类型</typeparam>
    internal class JpaRepositoryBase<T, ID> : JpaRepository<T, ID>, RepositoryProperty
    {
        /// <summary>
        /// 数据库连接字符串
        /// </summary>
        public DataSourceConfigurationAttribute DataSource { get; set; }

        /// <summary>
        /// T实体类的信息
        /// </summary>
        public EntityData Data { get; set; }

        #region 私有方法集

        private IDbConnection GetConnection()
        {
            return new MySqlConnection(DataSource.Value);
        }

        private IDbDataParameter CreatePara(string name, object val)
        {
            return new MySqlParameter(name, val);
        }

        private IDataReader ExecuteReader(string sql, params IDbDataParameter[] parameters)
        {
            var connection = GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = sql;
            foreach (var parameter in parameters)
            {
                command.Parameters.Add(parameter);
            }

            connection.Open();
            return command.ExecuteReader(CommandBehavior.CloseConnection);
        }

        private object ExecuteScalar(string sql, params IDbDataParameter[] parameters)
        {
            using (var connection = GetConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = sql;
                foreach (var parameter in parameters)
                {
                    command.Parameters.Add(parameter);
                }

                connection.Open();
                return command.ExecuteScalar();
            }
        }

        private int ExecuteNonQuery(string sql, params IDbDataParameter[] parameters)
        {
            using (var connection = GetConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = sql;
                foreach (var parameter in parameters)
                {
                    command.Parameters.Add(parameter);
                }

                connection.Open();
                return command.ExecuteNonQuery();
            }
        }

        private Target ConvertTo<Target>(IDataReader reader)
        {
            var targetType = typeof(Target);
            return (Target) ConvertTo(reader, targetType);
        }

        private object ConvertTo(IDataReader reader, Type targetType)
        {
            if (targetType == typeof(String))
            {
                return Convert.ToString(reader[0]);
            }

            if (targetType.IsValueType)
            {
                return Convert.ChangeType(reader[0], targetType);
            }

            var ret = Activator.CreateInstance(targetType);
            var returnFieldNames = GetColNames(reader, true);
            foreach (var dataField in Data.Fields)
            {
                // 返回的字段列表缺少时，这里会抛异常，所以要检查一下
                if (!returnFieldNames.Contains(dataField.Value.Name.ToLower()))
                    continue;
                var value = reader[dataField.Value.Name]; // 未验证带表名时，会不会有问题
                if (value == null || value == DBNull.Value)
                    continue;
                var useVal = Convert.ChangeType(value, dataField.Value.Property.PropertyType);
                dataField.Value.Property.SetValue(ret, useVal, null);
            }

            return ret;
        }

        /// <summary>
        /// 获取DataReader的每列列名
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="getLower">列名是否要转换小写</param>
        /// <returns></returns>
        public static HashSet<string> GetColNames(IDataReader reader, bool getLower = false)
        {
            var ret = new HashSet<string>();
            if (reader == null) // || !reader.HasRows)
                return ret;
            for (int i = 0; i < reader.FieldCount; i++)
            {
                var fieldname = reader.GetName(i) ?? "";
                if (getLower)
                {
                    fieldname = fieldname.ToLower();
                }

                ret.Add(fieldname);
            }

            return ret;
        }


        private bool IsNew(T entity)
        {
            var aID = (ID) Data.Fields[Data.KeyPropName].Property.GetValue(entity, null);
            if (aID == null)
                return true;
            if (aID.Equals((ID) TypeHelper.GetDefaultValue(typeof(ID))))
                return true;
            return false;
        }

        private long Insert(string sql, params IDbDataParameter[] parameters)
        {
            using (var connection = GetConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = sql;
                foreach (var parameter in parameters)
                {
                    command.Parameters.Add(parameter);
                }

                connection.Open();
                var ret = command.ExecuteNonQuery();
                if (ret <= 0)
                    throw new Exception("插入影响行数为0：" + sql);
                if (Data.IsKeyIdentity)
                {
                    command.Parameters.Clear();
                    command.CommandText = "SELECT LAST_INSERT_ID()";
                    return Convert.ToInt64(command.ExecuteScalar());
                }

                return 0; // 表示id不是自增主键
            }
        }

        private void Update(string sql, params IDbDataParameter[] parameters)
        {
            var ret = ExecuteNonQuery(sql, parameters);
            if (ret <= 0)
                throw new Exception("更新影响行数为0：" + sql);
        }

        private IDbDataParameter[] GetParameters(T entity)
        {
            var parameters = new IDbDataParameter[Data.Fields.Count];
            var i = 0;
            foreach (var pair in Data.Fields)
            {
                var paraVal = pair.Value.Property.GetValue(entity, null) ?? "";
                parameters[i] = CreatePara(pair.Key, paraVal);
                i++;
            }

            return parameters;
        }

        #endregion


        #region SQL方法集

        public List<T> Query(string sql, params IDbDataParameter[] parameters)
        {
            var ret = new List<T>();
            using (var reader = ExecuteReader(sql, parameters))
            {
                while (reader.Read())
                {
                    ret.Add(ConvertTo<T>(reader));
                }
            }

            return ret;
        }

        public T QueryFirst(string sql, params IDbDataParameter[] parameters)
        {
            using (var reader = ExecuteReader(sql, parameters))
            {
                if (!reader.Read())
                    return default;
                return ConvertTo<T>(reader);
            }
        }

        #endregion


        #region Jpa基础方法集

        /// <summary>
        /// 返回所有记录
        /// </summary>
        /// <returns></returns>
        public List<T> FindAll()
        {
            var sql = Data.SelectSql;
            return Query(sql);
        }

        /// <summary>
        /// 根据主键列表返回数据
        /// </summary>
        /// <param name="arrIds"></param>
        /// <returns></returns>
        public List<T> FindAllById(IEnumerable<ID> arrIds)
        {
            if (arrIds == null)
                throw new ArgumentException("参数不能为空");

            var arr = arrIds.ToList();
            var cnt = arr.Count;
            if (cnt == 0)
                throw new ArgumentException("参数不能为空.");

            var arrParam = new List<IDbDataParameter>();
            var sql = new StringBuilder(Data.SelectSql);
            sql.Append(" WHERE ").Append(Data.KeyName).Append(" IN (");
            for (var i = 0; i < cnt; i++)
            {
                var paraName = "id" + i.ToString();
                sql.Append("@").Append(paraName).Append(",");
                arrParam.Add(CreatePara(paraName, arr[i]));
            }

            sql.Remove(sql.Length - 1, 1);
            sql.Append(")");
            return Query(sql.ToString(), arrParam.ToArray());
        }

        /// <summary>
        /// 返回记录总数
        /// </summary>
        /// <returns></returns>
        public long Count()
        {
            var sql = $"SELECT COUNT(1) FROM {Data.TableName}";
            return Convert.ToInt32(ExecuteScalar(sql));
        }

        /// <summary>
        /// 根据主键删除
        /// </summary>
        /// <param name="aID">主键</param>
        public int DeleteById(ID aID)
        {
            if (aID == null)
                throw new ArgumentException("参数不能为空");

            var sql = $"DELETE FROM {Data.TableName} WHERE {Data.KeyName}=@id";
            var para = CreatePara("id", aID);
            return ExecuteNonQuery(sql, para);
        }

        /// <summary>
        /// 根据实体删除
        /// </summary>
        /// <param name="entity">实体</param>
        public int Delete(T entity)
        {
            if (entity == null)
                throw new ArgumentException("参数不能为空");

            var aID = (ID) Data.Fields[Data.KeyPropName].Property.GetValue(entity, null);
            return DeleteById(aID);
        }

        /// <summary>
        /// 根据实体列表删除
        /// </summary>
        /// <param name="arrEntities">实体列表</param>
        public int DeleteAll(IEnumerable<T> arrEntities)
        {
            if (arrEntities == null)
                throw new ArgumentException("参数不能为空");

            var property = Data.Fields[Data.KeyPropName].Property;
            var arrIds = arrEntities.Select(item => (ID) property.GetValue(item, null));
            return DeleteAll(arrIds);
        }

        /// <summary>
        /// 根据主键列表删除
        /// </summary>
        /// <param name="arrIds">主键列表</param>
        public int DeleteAll(IEnumerable<ID> arrIds)
        {
            if (arrIds == null)
                throw new ArgumentException("参数不能为空");

            var arr = arrIds.ToList();
            var cnt = arr.Count;
            if (cnt == 0)
                throw new ArgumentException("参数不能为空.");

            var arrParam = new List<IDbDataParameter>();
            var sql = new StringBuilder("DELETE FROM ");
            sql.Append(Data.TableName).Append(" WHERE ").Append(Data.KeyName).Append(" IN (");
            for (var i = 0; i < cnt; i++)
            {
                var paraName = "id" + i.ToString();
                sql.Append("@").Append(paraName).Append(",");
                arrParam.Add(CreatePara(paraName, arr[i]));
            }

            sql.Remove(sql.Length - 1, 1);
            sql.Append(")");
            return ExecuteNonQuery(sql.ToString(), arrParam.ToArray());
        }

        /// <summary>
        /// 保存实体
        /// </summary>
        /// <param name="entity">实体</param>
        /// <returns></returns>
        public T Save(T entity)
        {
            if (entity == null)
                throw new ArgumentException("参数不能为空");

            var parameters = GetParameters(entity);

            if (IsNew(entity))
            {
                var sql = Data.InsertSql;
                var lastId = Insert(sql, parameters);
                // 有新插入的主键，则重新检索实体返回，以填充主键
                if (lastId > 0)
                {
                    ID useId = (ID) Convert.ChangeType(lastId, typeof(ID));
                    return FindById(useId); // (ID) (object) lastId);
                }
            }
            else
            {
                var sql = Data.UpdateSql;
                Update(sql, parameters);
            }

            return entity;
        }

        /// <summary>
        /// 批量保存实体
        /// </summary>
        /// <param name="arrEntities"></param>
        /// <returns></returns>
        public List<T> SaveAll(IEnumerable<T> arrEntities)
        {
            if (arrEntities == null)
                throw new ArgumentException("参数不能为空");

            var arr = arrEntities.ToList();
            var cnt = arr.Count;
            if (cnt == 0)
                throw new ArgumentException("参数不能为空.");

            var ret = new List<T>();
            using (var connection = GetConnection())
            using (var command = connection.CreateCommand())
            {
                connection.Open();
                using (var traction = connection.BeginTransaction())
                {
                    foreach (var entity in arr)
                    {
                        if (entity == null)
                            continue;

                        var isNew = IsNew(entity);
                        command.CommandText = isNew ? Data.InsertSql : Data.UpdateSql;
                        command.Parameters.Clear();
                        foreach (var parameter in GetParameters(entity))
                        {
                            command.Parameters.Add(parameter);
                        }

                        var rownum = command.ExecuteNonQuery();
                        if (rownum <= 0)
                            throw new Exception("影响行数为0：" + command.CommandText);

                        if (isNew && Data.IsKeyIdentity)
                        {
                            command.Parameters.Clear();
                            command.CommandText = "SELECT LAST_INSERT_ID()";

                            var lastId = Convert.ToInt64(command.ExecuteScalar());
                            // 重新检索实体返回，以填充主键
                            if (lastId > 0)
                            {
                                command.Parameters.Clear();
                                command.CommandText = $"{Data.SelectSql} WHERE {Data.KeyName}=@id";
                                command.Parameters.Add(CreatePara("id", lastId));
                                using (var reader = command.ExecuteReader())
                                {
                                    reader.Read();
                                    ret.Add(ConvertTo<T>(reader));
                                }
                            }
                            else
                            {
                                ret.Add(entity);
                            }
                        }
                        else
                        {
                            ret.Add(entity);
                        }
                    }

                    traction.Commit();
                }
            }

            return ret;
        }

        /// <summary>
        /// 根据主键查找
        /// </summary>
        /// <param name="aID">主键</param>
        /// <returns></returns>
        public T FindById(ID aID)
        {
            var existSql = $"{Data.SelectSql} WHERE {Data.KeyName}=@id";
            var para = CreatePara("id", aID);
            return QueryFirst(existSql, para);
        }

        /// <summary>
        /// 指定主键是否存在
        /// </summary>
        /// <param name="aID">主键</param>
        /// <returns></returns>
        public bool ExistsById(ID aID)
        {
            var existSql = $"SELECT 1 FROM {Data.TableName} WHERE {Data.KeyName}=@id";
            var para = CreatePara("id", aID);
            var result = ExecuteScalar(existSql, para);
            return Convert.ToString(result) == "1";
        }

        #endregion

        /// <summary>
        /// 自定义方法入口，暂不支持类型T或基础类型以外的类型
        /// </summary>
        /// <param name="query"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public object CustomerMethod(CustomData query, object[] parameters)
        {
            if (query.ReturnType.IsArray)
            {
                throw new ArgumentException("只支持List，不支持数组");
            }

            // https://docs.spring.io/spring-data/jpa/docs/current/reference/html/#reference  Query支持表名占位符替换
            var sql = query.Sql.Replace("#{#entityName}", Data.TableName);

            var arrPara = new List<IDbDataParameter>(parameters.Length);

            // JPA参数索引从1开始, 从高往低替换，避免把 ?12 的 ?1 替换掉了
            for (var i = parameters.Length; i >= 1; i--)
            {
                var strIdx = i.ToString();
                var symbol = "?" + i.ToString();
                var varName = "PARA" + strIdx;
                var paraVal = parameters[i - 1];
                if (!(paraVal is string) && paraVal is IEnumerable arrList)
                {
                    // 参数是数组，且不是字符串时，应该是in 查询，要生成n个参数
                    var arrVarName = AddParaArray(varName, arrList, arrPara);
                    if (arrVarName.Length <= 0)
                    {
                        throw new Exception("传入的数组参数长度不能为空:" + symbol);
                    }

                    sql = sql.Replace(symbol, arrVarName);
                }
                else
                {
                    sql = sql.Replace(symbol, "@" + varName);
                    arrPara.Add(CreatePara(varName, paraVal));
                }
            }

            if (query.IsModify)
            {
                var retModiNum = ExecuteNonQuery(sql, arrPara.ToArray());
                if (query.ReturnType == typeof(void))
                    return null;
                return Convert.ChangeType(retModiNum, query.ReturnType);
            }

            CheckIsModifySql(sql);

            if (query.ReturnType == typeof(void))
                throw new ArgumentException("查询方法返回值不能为空:" + query.Sql);

            var isSimpleType = query.ReturnType.IsValueType ||
                               query.ReturnType == typeof(string);

            var isArray = typeof(IEnumerable).IsAssignableFrom(query.ReturnType);

            IList ret = null;
            if (isArray)
            {
                ret = (IList) Activator.CreateInstance(
                    typeof(List<>).MakeGenericType(query.ReturnType.GenericTypeArguments));
            }

            using (var reader = ExecuteReader(sql, arrPara.ToArray()))
            {
                while (reader.Read())
                {
                    if (isSimpleType)
                    {
                        //if (reader.FieldCount > 1)
                        //    throw new Exception("查询结果不止一列");
                        return Convert.ChangeType(reader[0], query.ReturnType);
                    }

                    if (!isArray)
                    {
                        return ConvertTo(reader, query.ReturnType);
                    }

                    ret.Add(ConvertTo(reader, query.ReturnType.GenericTypeArguments[0]));
                }
            }

            // 数组只支持List<T>, 不支持T[]
//            if (query.ReturnType.IsArray)
//            {
//                 return ret.ToArray();
//            }
            if (ret == null && query.ReturnType.IsValueType)
            {
                return Activator.CreateInstance(query.ReturnType);
            }

            return ret;
        }

        private void CheckIsModifySql(string sql)
        {
            sql = sql.Trim();
            if (sql.IndexOf("insert", StringComparison.OrdinalIgnoreCase) == 0
                || sql.IndexOf("update", StringComparison.OrdinalIgnoreCase) == 0
                || sql.IndexOf("delete", StringComparison.OrdinalIgnoreCase) == 0)
            {
                throw new ArgumentException("更新方法必须指定:" + nameof(ModifingAttribute));
            }
        }

        private string AddParaArray(string varName, IEnumerable arrList, List<IDbDataParameter> arrPara)
        {
            var newVarName = new StringBuilder();
            var idx = 0;
            foreach (var subItem in arrList)
            {
                idx++;
                var subItemName = varName + "_" + idx;
                if (newVarName.Length > 0)
                    newVarName.Append(",");
                newVarName.Append("@").Append(subItemName);
                arrPara.Add(CreatePara(subItemName, subItem));
            }

            return newVarName.ToString();
        }
    }

    internal class CustomData
    {
        /// <summary>
        /// SQL
        /// </summary>
        public string Sql { get; set; }

        /// <summary>
        /// 是否DML
        /// </summary>
        public bool IsModify { get; set; }

        /// <summary>
        /// 返回参数类型
        /// </summary>
        public Type ReturnType { get; set; }
    }
}