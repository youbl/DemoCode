using System;
using System.Collections.Generic;
using System.Data;
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
            return command.ExecuteReader();
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

        private T ConvertTo(IDataReader reader)
        {
            var ret = Activator.CreateInstance(typeof(T));
            foreach (var dataField in Data.Fields)
            {
                var value = reader[dataField.Value.Name]; // 未验证带表名时，会不会有问题
                if (value == null || value == DBNull.Value)
                    continue;
                dataField.Value.Property.SetValue(ret, value, null);
            }

            return (T)ret;
        }

        private bool IsNew(T entity)
        {
            var aID = (ID)Data.Fields[Data.KeyPropName].Property.GetValue(entity, null);
            if (aID == null)
                return true;
            if (aID.Equals((ID)TypeHelper.GetDefaultValue(typeof(ID))))
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

                return 0;// 表示id不是自增主键
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
                parameters[i] = CreatePara(pair.Key, pair.Value.Property.GetValue(entity, null));
                i++;
            }

            return parameters;
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
            var ret = new List<T>();
            using (var reader = ExecuteReader(sql))
            {
                while (reader.Read())
                {
                    ret.Add(ConvertTo(reader));
                }
            }

            return ret;
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
            var ret = new List<T>();
            using (var reader = ExecuteReader(sql.ToString(), arrParam.ToArray()))
            {
                while (reader.Read())
                {
                    ret.Add(ConvertTo(reader));
                }
            }

            return ret;
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

            var aID = (ID)Data.Fields[Data.KeyPropName].Property.GetValue(entity, null);
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
            var arrIds = arrEntities.Select(item => (ID)property.GetValue(item, null));
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
                    return FindById((ID)(object)lastId);
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
                                    ret.Add(ConvertTo(reader));
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
            using (var reader = ExecuteReader(existSql, para))
            {
                if (!reader.Read())
                    return default;
                return ConvertTo(reader);
            }
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
    }
}
