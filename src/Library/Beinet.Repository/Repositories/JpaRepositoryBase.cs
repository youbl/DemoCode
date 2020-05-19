using System.Collections.Generic;
using System.Data;

namespace Beinet.Repository.Repositories
{
    /// <summary>
    /// JPA操作类
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <typeparam name="ID">主键类型</typeparam>
    internal class JpaRepositoryBase<T, ID> : JpaRepository<T, ID>
    {
        #region 私有方法集

        private IDbConnection GetConnection()
        {
            return null;
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

        #endregion


        /// <summary>
        /// 返回所有记录
        /// </summary>
        /// <returns></returns>
        public List<T> FindAll()
        {
            var sql = "";
            var ret = new List<T>();
            using (var reader = ExecuteReader(sql))
            {
                while (reader.Read())
                {

                }
            }

            return ret;
        }

        /// <summary>
        /// 根据主键列表返回数据
        /// </summary>
        /// <param name="IEnumerable"></param>
        /// <returns></returns>
        public List<T> FindAllById(IEnumerable<ID> IEnumerable)
        {
            var ret = new List<T>();

            return ret;
        }

        /// <summary>
        /// 返回记录总数
        /// </summary>
        /// <returns></returns>
        public long Count()
        {
            return 0;
        }

        /// <summary>
        /// 根据主键删除
        /// </summary>
        /// <param name="aID">主键</param>
        public void DeleteById(ID aID)
        {

        }

        /// <summary>
        /// 根据实体删除
        /// </summary>
        /// <param name="T">实体</param>
        public void Delete(T T)
        {

        }

        /// <summary>
        /// 根据实体列表删除
        /// </summary>
        /// <param name="arr">实体列表</param>
        public void DeleteAll(IEnumerable<T> arr)
        {

        }

        /// <summary>
        /// 根据主键列表删除
        /// </summary>
        /// <param name="arr">主键列表</param>
        public void DeleteAll(IEnumerable<ID> arr)
        {

        }

        /// <summary>
        /// 保存实体
        /// </summary>
        /// <param name="s">实体</param>
        /// <returns></returns>
        public T Save(T s)
        {
            return default(T);
        }

        /// <summary>
        /// 批量保存实体
        /// </summary>
        /// <param name="IEnumerable"></param>
        /// <returns></returns>
        public List<T> SaveAll(IEnumerable<T> IEnumerable)
        {
            var ret = new List<T>();

            return ret;
        }

        /// <summary>
        /// 根据主键查找
        /// </summary>
        /// <param name="aID">主键</param>
        /// <returns></returns>
        public T FindById(ID aID)
        {
            return default(T);
        }

        /// <summary>
        /// 指定主键是否存在
        /// </summary>
        /// <param name="aID">主键</param>
        /// <returns></returns>
        public bool ExistsById(ID aID)
        {
            return false;
        }
    }
}
