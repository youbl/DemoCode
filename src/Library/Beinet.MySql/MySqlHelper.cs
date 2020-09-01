using System.Data.Common;
using Beinet.Core.Database;
using MySql.Data.MySqlClient;

namespace Beinet.MySql
{
    /// <summary>
    /// MySql操作辅助类
    /// </summary>
    public sealed class MySqlHelper : BaseSqlHelper
    {
        /// <summary>
        /// 私有化构造函数，以避免多实例
        /// </summary>
        private MySqlHelper()
        {
        }

        /// <summary>
        /// 创建指定连接字符串的Helper对象
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="testConnection">是否要验证数据库连接正常</param>
        /// <returns></returns>
        public static MySqlHelper GetConnection(string connectionString = null, bool testConnection = false)
        {
            return BaseSqlHelper.GetConnection<MySqlHelper>(connectionString, testConnection);
        }

        /// <summary>
        /// 创建MySqlServer连接对象
        /// </summary>
        /// <returns></returns>
        public override DbConnection CreateConnection()
        {
            return new MySqlConnection(ConnectionString);
        }
        /// <summary>
        /// 创建MySqlDataAdapter适配器对象
        /// </summary>
        /// <returns></returns>
        public override DbDataAdapter CreateDataAdapter()
        {
            return new MySqlDataAdapter();
        }

        /// <summary>
        /// 创建参数对象
        /// </summary>
        /// <param name="parameterName"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public override DbParameter CreatePara(string parameterName = "", object value = null)
        {
            return new MySqlParameter(parameterName, value);
        }

    }
}
