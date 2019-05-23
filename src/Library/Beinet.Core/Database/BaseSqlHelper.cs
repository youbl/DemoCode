using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Beinet.Core.Database
{
    /// <summary>
    /// 数据库操作基类定义
    /// </summary>
    public abstract class BaseSqlHelper
    {
        /// <summary>
        /// 
        /// </summary>
        protected static Dictionary<string, BaseSqlHelper> _connections = new Dictionary<string, BaseSqlHelper>();

        #region 属性
        private int _commandTimeout = 5;

        /// <summary>
        /// 获取或设置Command的CommandTimeout属性，默认5秒，设置小于1无效
        /// </summary>
        public virtual int CommandTimeout
        {
            get { return _commandTimeout; }
            set
            {
                if (value > 0)
                    _commandTimeout = value;
            }
        }

        /// <summary>
        /// 数据库连接字符串
        /// </summary>
        public virtual string ConnectionString { get; protected set; }
        /// <summary>
        /// 
        /// </summary>
        protected string _server;
        /// <summary>
        /// 返回连接字符串里的主机
        /// </summary>
        public virtual string Server
        {
            get
            {
                if (_server == null)
                {
                    if (string.IsNullOrEmpty(ConnectionString))
                    {
                        throw new ArgumentException("连接串不能为空", nameof(ConnectionString));
                    }
                    var regex = new Regex(@"server=([^;\s]+)|data\s+source=([^;\s]+)", RegexOptions.IgnoreCase);
                    var match = regex.Match(ConnectionString);
                    if (!match.Success)
                    {
                        return string.Empty;
                    }
                    var ret = match.Result("$1$2");
                    _server = ret;
                }
                return _server;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        protected string _database;
        /// <summary>
        /// 返回连接字符串里的数据库
        /// </summary>
        public virtual string Database
        {
            get
            {
                if (_database == null)
                {
                    if (string.IsNullOrEmpty(ConnectionString))
                    {
                        throw new ArgumentException("连接串不能为空", nameof(ConnectionString));
                    }
                    var regex = new Regex(@"database=([^;\s]+)|Initial\s+Catalog=([^;\s]+)", RegexOptions.IgnoreCase);
                    var match = regex.Match(ConnectionString);
                    if (!match.Success)
                    {
                        return string.Empty;
                    }
                    var ret = match.Result("$1$2");
                    _database = ret;
                }
                return _database;
            }
        }

        #endregion



        #region 虚方法
        /// <summary>
        /// 创建数据库连接对象
        /// </summary>
        /// <returns></returns>
        public abstract DbConnection CreateConnection();
        /// <summary>
        /// 创建数据适配器
        /// </summary>
        /// <returns></returns>
        public abstract DbDataAdapter CreateDataAdapter();

        /// <summary>
        /// 创建参数对象
        /// </summary>
        /// <param name="parameterName"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public abstract DbParameter CreatePara(string parameterName = "", object value = null);
        #endregion


        #region ExecuteNonQuery

        /// <summary>
        /// 对连接执行 Transact-SQL 语句并返回受影响的行数。
        /// </summary>
        /// <remarks>
        /// 示例:  
        ///  var result = ExecuteNonQuery("Insert into tb(id)values(@prodid)", new SqlParameter("@prodid", 24));
        /// </remarks>
        /// <param name="commandText">获取或设置要对数据源执行的 Transact-SQL 语句或存储过程</param>
        /// <param name="commandParameters">用来执行命令的参数数组</param>
        /// <returns>执行命令后受影响的行数</returns>
        public virtual int ExecuteNonQuery(string commandText, params DbParameter[] commandParameters)
        {
            using (DbConnection connection = CreateConnection())
            {
                connection.Open();
                return ExecuteNonQuery(connection, commandText, CommandType.Text, CommandTimeout, commandParameters);
            }
        }

        /// <summary>
        /// 对连接执行 Transact-SQL 语句并返回受影响的行数。
        /// </summary>
        /// <remarks>
        /// 示例:  
        ///  var result = ExecuteNonQuery("Insert into tb(id)values(@prodid)", 30, new SqlParameter("@prodid", 24));
        /// </remarks>
        /// <param name="commandText">获取或设置要对数据源执行的 Transact-SQL 语句或存储过程</param>
        /// <param name="commandTimeout">设定Command执行时间，秒，大于0有效</param>
        /// <param name="commandParameters">用来执行命令的参数数组</param>
        /// <returns>执行命令后受影响的行数</returns>
        public virtual int ExecuteNonQuery(string commandText, int commandTimeout, params DbParameter[] commandParameters)
        {
            using (DbConnection connection = CreateConnection())
            {
                connection.Open();
                return ExecuteNonQuery(connection, commandText, CommandType.Text, commandTimeout, commandParameters);
            }
        }

        /// <summary>
        /// 对连接执行 Transact-SQL 语句并返回受影响的行数。
        /// </summary>
        /// <remarks>
        /// 此方法不支持对输出参数或者存储过程里的返回参数的访问
        /// 
        /// 示例:  
        ///  int result = ExecuteNonQuerySP("PublishOrders", new SqlParameter("@prodid", 24));
        /// </remarks>
        /// <param name="spName">存储过程名称</param>
        /// <param name="commandParameters">用来执行命令的参数数组</param>
        /// <returns>执行命令后受影响的行数</returns>
        public virtual int ExecuteNonQuerySP(string spName, params DbParameter[] commandParameters)
        {
            using (DbConnection connection = CreateConnection())
            {
                connection.Open();
                return ExecuteNonQuery(connection, spName, CommandType.StoredProcedure, CommandTimeout, commandParameters);
            }
        }

        /// <summary>
        /// 对连接执行 Transact-SQL 语句并返回受影响的行数。
        /// </summary>
        /// <remarks>
        /// 此方法不支持对输出参数或者存储过程里的返回参数的访问
        /// 
        /// 示例:  
        ///  var result = ExecuteNonQuerySP("PublishOrders", 30, new SqlParameter("@prodid", 24));
        /// </remarks>
        /// <param name="spName">存储过程名称</param>
        /// <param name="commandTimeout">设定Command执行时间，秒，大于0有效</param>
        /// <param name="commandParameters">用来执行命令的参数数组</param>
        /// <returns>执行命令后受影响的行数</returns>
        public virtual int ExecuteNonQuerySP(string spName, int commandTimeout, params DbParameter[] commandParameters)
        {
            using (DbConnection connection = CreateConnection())
            {
                connection.Open();
                return ExecuteNonQuery(connection, spName, CommandType.StoredProcedure, commandTimeout, commandParameters);
            }
        }

        /// <summary>
        /// 对连接执行 Transact-SQL 语句并返回受影响的行数。
        /// </summary>
        /// <remarks></remarks>
        /// <param name="connection">有效的数据库连接对象</param>
        /// <param name="commandType">获取或设置一个值，该值指示如何解释 CommandText 属性</param>
        /// <param name="commandText">获取或设置要对数据源执行的 Transact-SQL 语句或存储过程</param>
        /// <param name="commandTimeout">设定Command执行时间，秒，大于0有效</param>
        /// <param name="commandParameters">用来执行命令的参数数组</param>
        /// <returns>执行命令后受影响的行数</returns>
        public virtual int ExecuteNonQuery(DbConnection connection, string commandText,
            CommandType commandType = CommandType.Text, int commandTimeout = 0, params DbParameter[] commandParameters)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            DbCommand cmd = connection.CreateCommand();
            bool mustCloseConnection;
            PrepareCommand(cmd, connection, commandText, out mustCloseConnection, null, commandType, commandParameters, commandTimeout);

            try
            {
                return cmd.ExecuteNonQuery();
            }
            finally
            {
                cmd.Parameters.Clear();

                if (mustCloseConnection)
                {
                    connection.Close();
                }
            }
        }

        #endregion ExecuteNonQuery

        #region ExecuteNonQueryAsync

        /// <summary>
        /// 对连接执行 Transact-SQL 语句并返回受影响的行数。
        /// </summary>
        /// <remarks>
        /// 示例:  
        ///  var result = ExecuteNonQueryAsync("Insert into tb(id)values(@prodid)", new SqlParameter("@prodid", 24));
        /// </remarks>
        /// <param name="commandText">获取或设置要对数据源执行的 Transact-SQL 语句或存储过程</param>
        /// <param name="commandParameters">用来执行命令的参数数组</param>
        /// <returns>执行命令后受影响的行数</returns>
        public virtual async Task<int> ExecuteNonQueryAsync(string commandText, params DbParameter[] commandParameters)
        {
            using (DbConnection connection = CreateConnection())
            {
                connection.Open();
                return await ExecuteNonQueryAsync(connection, commandText, CommandType.Text, CommandTimeout, commandParameters);
            }
        }

        /// <summary>
        /// 对连接执行 Transact-SQL 语句并返回受影响的行数。
        /// </summary>
        /// <remarks>
        /// 示例:  
        ///  var result = ExecuteNonQueryAsync("Insert into tb(id)values(@prodid)", 30, new SqlParameter("@prodid", 24));
        /// </remarks>
        /// <param name="commandText">获取或设置要对数据源执行的 Transact-SQL 语句或存储过程</param>
        /// <param name="commandTimeout">设定Command执行时间，秒，大于0有效</param>
        /// <param name="commandParameters">用来执行命令的参数数组</param>
        /// <returns>执行命令后受影响的行数</returns>
        public virtual async Task<int> ExecuteNonQueryAsync(string commandText, int commandTimeout, params DbParameter[] commandParameters)
        {
            using (DbConnection connection = CreateConnection())
            {
                connection.Open();
                return await ExecuteNonQueryAsync(connection, commandText, CommandType.Text, commandTimeout, commandParameters);
            }
        }

        /// <summary>
        /// 对连接执行 Transact-SQL 语句并返回受影响的行数。
        /// </summary>
        /// <remarks>
        /// 此方法不支持对输出参数或者存储过程里的返回参数的访问
        /// 
        /// 示例:  
        ///  int result = ExecuteNonQueryAsyncSP("PublishOrders", new SqlParameter("@prodid", 24));
        /// </remarks>
        /// <param name="spName">存储过程名称</param>
        /// <param name="commandParameters">用来执行命令的参数数组</param>
        /// <returns>执行命令后受影响的行数</returns>
        public virtual async Task<int> ExecuteNonQueryAsyncSP(string spName, params DbParameter[] commandParameters)
        {
            using (DbConnection connection = CreateConnection())
            {
                connection.Open();
                return await ExecuteNonQueryAsync(connection, spName, CommandType.StoredProcedure, CommandTimeout, commandParameters);
            }
        }

        /// <summary>
        /// 对连接执行 Transact-SQL 语句并返回受影响的行数。
        /// </summary>
        /// <remarks>
        /// 此方法不支持对输出参数或者存储过程里的返回参数的访问
        /// 
        /// 示例:  
        ///  var result = ExecuteNonQueryAsyncSP("PublishOrders", 30, new SqlParameter("@prodid", 24));
        /// </remarks>
        /// <param name="spName">存储过程名称</param>
        /// <param name="commandTimeout">设定Command执行时间，秒，大于0有效</param>
        /// <param name="commandParameters">用来执行命令的参数数组</param>
        /// <returns>执行命令后受影响的行数</returns>
        public virtual async Task<int> ExecuteNonQueryAsyncSP(string spName, int commandTimeout, params DbParameter[] commandParameters)
        {
            using (DbConnection connection = CreateConnection())
            {
                connection.Open();
                return await ExecuteNonQueryAsync(connection, spName, CommandType.StoredProcedure, commandTimeout, commandParameters);
            }
        }

        /// <summary>
        /// 对连接执行 Transact-SQL 语句并返回受影响的行数。
        /// </summary>
        /// <remarks></remarks>
        /// <param name="connection">有效的数据库连接对象</param>
        /// <param name="commandType">获取或设置一个值，该值指示如何解释 CommandText 属性</param>
        /// <param name="commandText">获取或设置要对数据源执行的 Transact-SQL 语句或存储过程</param>
        /// <param name="commandTimeout">设定Command执行时间，秒，大于0有效</param>
        /// <param name="commandParameters">用来执行命令的参数数组</param>
        /// <returns>执行命令后受影响的行数</returns>
        public virtual Task<int> ExecuteNonQueryAsync(DbConnection connection, string commandText,
            CommandType commandType = CommandType.Text, int commandTimeout = 0, params DbParameter[] commandParameters)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            DbCommand cmd = connection.CreateCommand();
            bool mustCloseConnection;
            PrepareCommand(cmd, connection, commandText, out mustCloseConnection, null, commandType, commandParameters, commandTimeout);

            try
            {
                return cmd.ExecuteNonQueryAsync();
            }
            finally
            {
                cmd.Parameters.Clear();

                if (mustCloseConnection)
                {
                    connection.Close();
                }
            }
        }

        #endregion ExecuteNonQueryAsync


        #region ExecuteDataset

        /// <summary>
        /// 对连接执行 Transact-SQL 语句并返回DataSet数据集
        /// </summary>
        /// <remarks>
        /// 示例:  
        ///  var result = ExecuteDataset("select * from tb where id=@id", new SqlParameter("@id", 24));
        /// </remarks>
        /// <param name="commandText">获取或设置要对数据源执行的 Transact-SQL 语句或存储过程</param>
        /// <param name="commandParameters">用来执行命令的参数数组</param>
        /// <returns>执行命令后受影响的行数</returns>
        public virtual DataSet ExecuteDataset(string commandText, params DbParameter[] commandParameters)
        {
            using (DbConnection connection = CreateConnection())
            {
                connection.Open();
                return ExecuteDataset(connection, commandText, CommandType.Text, CommandTimeout, commandParameters);
            }
        }

        /// <summary>
        /// 对连接执行 Transact-SQL 语句并返回DataSet数据集
        /// </summary>
        /// <remarks>
        /// 示例:  
        ///  var result = ExecuteDataset("select * from tb where id=@id", 30, new SqlParameter("@id", 24));
        /// </remarks>
        /// <param name="commandText">获取或设置要对数据源执行的 Transact-SQL 语句或存储过程</param>
        /// <param name="commandTimeout">设定Command执行时间，秒，大于0有效</param>
        /// <param name="commandParameters">用来执行命令的参数数组</param>
        /// <returns>执行命令后受影响的行数</returns>
        public virtual DataSet ExecuteDataset(string commandText, int commandTimeout, params DbParameter[] commandParameters)
        {
            using (DbConnection connection = CreateConnection())
            {
                connection.Open();
                return ExecuteDataset(connection, commandText, CommandType.Text, commandTimeout, commandParameters);
            }
        }

        /// <summary>
        /// 对连接执行 Transact-SQL 语句并返回数据集。
        /// </summary>
        /// <remarks>
        /// 此方法不支持对输出参数或者存储过程里的返回参数的访问
        /// 
        /// 示例:  
        ///  var result = ExecuteDatasetSP("PublishOrders", new SqlParameter("@prodid", 24));
        /// </remarks>
        /// <param name="spName">存储过程名称</param>
        /// <param name="commandParameters">用来执行命令的参数数组</param>
        /// <returns>执行命令后受影响的行数</returns>
        public virtual DataSet ExecuteDatasetSP(string spName, params DbParameter[] commandParameters)
        {
            using (DbConnection connection = CreateConnection())
            {
                connection.Open();
                return ExecuteDataset(connection, spName, CommandType.StoredProcedure, CommandTimeout, commandParameters);
            }
        }

        /// <summary>
        /// 对连接执行 Transact-SQL 语句并返回DataSet数据集
        /// </summary>
        /// <remarks>
        /// 此方法不支持对输出参数或者存储过程里的返回参数的访问
        /// 
        /// 示例:  
        ///  var result = ExecuteDatasetSP("PublishOrders", 30, new SqlParameter("@prodid", 24));
        /// </remarks>
        /// <param name="spName">存储过程名称</param>
        /// <param name="commandTimeout">设定Command执行时间，秒，大于0有效</param>
        /// <param name="commandParameters">用来执行命令的参数数组</param>
        /// <returns>执行命令后受影响的行数</returns>
        public virtual DataSet ExecuteDatasetSP(string spName, int commandTimeout, params DbParameter[] commandParameters)
        {
            using (DbConnection connection = CreateConnection())
            {
                connection.Open();
                return ExecuteDataset(connection, spName, CommandType.StoredProcedure, commandTimeout, commandParameters);
            }
        }

        /// <summary>
        /// 对连接执行 Transact-SQL 语句并返回DataSet数据集
        /// </summary>
        /// <remarks></remarks>
        /// <param name="connection">有效的数据库连接对象</param>
        /// <param name="commandType">获取或设置一个值，该值指示如何解释 CommandText 属性</param>
        /// <param name="commandText">获取或设置要对数据源执行的 Transact-SQL 语句或存储过程</param>
        /// <param name="commandTimeout">设定Command执行时间，秒，大于0有效</param>
        /// <param name="commandParameters">用来执行命令的参数数组</param>
        /// <returns>执行命令后受影响的行数</returns>
        public virtual DataSet ExecuteDataset(DbConnection connection, string commandText,
            CommandType commandType = CommandType.Text, int commandTimeout = 0, params DbParameter[] commandParameters)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            DbCommand cmd = connection.CreateCommand();
            bool mustCloseConnection;
            PrepareCommand(cmd, connection, commandText, out mustCloseConnection, null, commandType, commandParameters,
                commandTimeout);

            DataSet ds = new DataSet();
            try
            {
                using (var dataAdaper = CreateDataAdapter())
                {
                    dataAdaper.SelectCommand = cmd;
                    dataAdaper.Fill(ds);
                }
            }
            finally
            {
                cmd.Parameters.Clear();

                if (mustCloseConnection)
                {
                    connection.Close();
                }
            }
            return ds;
        }

        #endregion ExecuteDataset


        #region ExecuteReader

        /// <summary>
        /// 对连接执行 Transact-SQL 语句并返回DataReader对象
        /// </summary>
        /// <remarks>
        /// 示例:  
        ///  var result = ExecuteReader("select * from tb where id=@id", new SqlParameter("@id", 24));
        /// </remarks>
        /// <param name="commandText">要执行的 Transact-SQL 语句</param>
        /// <param name="commandParameters">用来执行命令的参数数组</param>
        /// <returns>执行命令后受影响的行数</returns>
        public virtual DbDataReader ExecuteReader(string commandText, params DbParameter[] commandParameters)
        {
            return ExecuteReader(commandText, CommandTimeout, commandParameters);
        }

        /// <summary>
        /// 对连接执行 Transact-SQL 语句并返回DataReader对象
        /// </summary>
        /// <remarks>
        /// 示例:  
        ///  var result = ExecuteReader("select * from tb where id=@id", 30, new SqlParameter("@id", 24));
        /// </remarks>
        /// <param name="commandText">要执行的 Transact-SQL 语句</param>
        /// <param name="commandTimeout">设定Command执行时间，秒，大于0有效</param>
        /// <param name="commandParameters">用来执行命令的参数数组</param>
        /// <returns>执行命令后受影响的行数</returns>
        public virtual DbDataReader ExecuteReader(string commandText, int commandTimeout, params DbParameter[] commandParameters)
        {
            DbConnection connection = CreateConnection();
            return ExecuteReader(connection, commandText, CommandType.Text, commandTimeout, commandParameters);
        }

        /// <summary>
        /// 对连接执行 Transact-SQL 语句并返回DataReader对象
        /// </summary>
        /// <remarks>
        /// 此方法不支持对输出参数或者存储过程里的返回参数的访问
        /// 
        /// 示例:  
        ///  var result = ExecuteReaderSP("PublishOrders", new SqlParameter("@prodid", 24));
        /// </remarks>
        /// <param name="spName">要执行的 存储过程名称</param>
        /// <param name="commandParameters">用来执行命令的参数数组</param>
        /// <returns>执行命令后受影响的行数</returns>
        public virtual DbDataReader ExecuteReaderSP(string spName, params DbParameter[] commandParameters)
        {
            return ExecuteReaderSP(spName, CommandTimeout, commandParameters);
        }

        /// <summary>
        /// 对连接执行 Transact-SQL 语句并返回DataReader对象
        /// </summary>
        /// <remarks>
        /// 此方法不支持对输出参数或者存储过程里的返回参数的访问
        /// 
        /// 示例:  
        ///  var result = ExecuteReaderSP("PublishOrders", 30, new SqlParameter("@prodid", 24));
        /// </remarks>
        /// <param name="spName">要执行的 存储过程名称</param>
        /// <param name="commandTimeout">设定Command执行时间，秒，大于0有效</param>
        /// <param name="commandParameters">用来执行命令的参数数组</param>
        /// <returns>执行命令后受影响的行数</returns>
        public virtual DbDataReader ExecuteReaderSP(string spName, int commandTimeout,
            params DbParameter[] commandParameters)
        {
            DbConnection connection = CreateConnection();
            return ExecuteReader(connection, spName, CommandType.StoredProcedure, commandTimeout, commandParameters);
        }

        /// <summary>
        /// 对连接执行 Transact-SQL 语句并返回DataReader对象
        /// </summary>
        /// <remarks></remarks>
        /// <param name="connection">有效的数据库连接对象</param>
        /// <param name="commandType">指示 CommandText 是语句还是存储过程</param>
        /// <param name="commandText">要执行的 Transact-SQL 语句或存储过程</param>
        /// <param name="commandTimeout">设定Command执行时间，秒，大于0有效</param>
        /// <param name="commandParameters">用来执行命令的参数数组</param>
        /// <returns>执行命令后受影响的行数</returns>
        public virtual DbDataReader ExecuteReader(DbConnection connection, string commandText,
            CommandType commandType = CommandType.Text, int commandTimeout = 0, params DbParameter[] commandParameters)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            DbCommand cmd = connection.CreateCommand();
            bool mustCloseConnection;
            PrepareCommand(cmd, connection, commandText, out mustCloseConnection, null, commandType, commandParameters, commandTimeout);

            try
            {
                return cmd.ExecuteReader(CommandBehavior.CloseConnection);
            }
            catch
            {
                if (mustCloseConnection)
                {
                    connection.Close();
                }
                throw;
            }
            finally
            {
                cmd.Parameters.Clear();
                // 连接关闭后，DataReader将不可使用
                // connection.Close();
            }
        }

        #endregion ExecuteReader

        #region ExecuteReaderAsync

        /// <summary>
        /// 对连接执行 Transact-SQL 语句并返回DataReader对象
        /// </summary>
        /// <remarks>
        /// 示例:  
        ///  var result = ExecuteReaderAsync("select * from tb where id=@id", new SqlParameter("@id", 24));
        /// </remarks>
        /// <param name="commandText">要执行的 Transact-SQL 语句</param>
        /// <param name="commandParameters">用来执行命令的参数数组</param>
        /// <returns>执行命令后受影响的行数</returns>
        public virtual async Task<DbDataReader> ExecuteReaderAsync(string commandText,
            params DbParameter[] commandParameters)
        {
            DbConnection connection = CreateConnection();
            return await ExecuteReaderAsync(connection, commandText, CommandType.Text, CommandTimeout,
                commandParameters);
        }

        /// <summary>
        /// 对连接执行 Transact-SQL 语句并返回DataReader对象
        /// </summary>
        /// <remarks>
        /// 示例:  
        ///  var result = ExecuteReaderAsync("select * from tb where id=@id", 30, new SqlParameter("@id", 24));
        /// </remarks>
        /// <param name="commandText">要执行的 Transact-SQL 语句</param>
        /// <param name="commandTimeout">设定Command执行时间，秒，大于0有效</param>
        /// <param name="commandParameters">用来执行命令的参数数组</param>
        /// <returns>执行命令后受影响的行数</returns>
        public virtual async Task<DbDataReader> ExecuteReaderAsync(string commandText, int commandTimeout, params DbParameter[] commandParameters)
        {
            DbConnection connection = CreateConnection();
            return await ExecuteReaderAsync(connection, commandText, CommandType.Text, commandTimeout,
                commandParameters);
        }

        /// <summary>
        /// 对连接执行 Transact-SQL 语句并返回DataReader对象
        /// </summary>
        /// <remarks>
        /// 此方法不支持对输出参数或者存储过程里的返回参数的访问
        /// 
        /// 示例:  
        ///  var result = ExecuteReaderAsyncSP("PublishOrders", new SqlParameter("@prodid", 24));
        /// </remarks>
        /// <param name="spName">要执行的 存储过程名称</param>
        /// <param name="commandParameters">用来执行命令的参数数组</param>
        /// <returns>执行命令后受影响的行数</returns>
        public virtual async Task<DbDataReader> ExecuteReaderAsyncSP(string spName,
            params DbParameter[] commandParameters)
        {
            DbConnection connection = CreateConnection();
            return await ExecuteReaderAsync(connection, spName, CommandType.StoredProcedure, CommandTimeout,
                commandParameters);
        }

        /// <summary>
        /// 对连接执行 Transact-SQL 语句并返回DataReader对象
        /// </summary>
        /// <remarks>
        /// 此方法不支持对输出参数或者存储过程里的返回参数的访问
        /// 
        /// 示例:  
        ///  var result = ExecuteReaderAsyncSP("PublishOrders", 30, new SqlParameter("@prodid", 24));
        /// </remarks>
        /// <param name="spName">要执行的 存储过程名称</param>
        /// <param name="commandTimeout">设定Command执行时间，秒，大于0有效</param>
        /// <param name="commandParameters">用来执行命令的参数数组</param>
        /// <returns>执行命令后受影响的行数</returns>
        public virtual async Task<DbDataReader> ExecuteReaderAsyncSP(string spName, int commandTimeout, params DbParameter[] commandParameters)
        {
            DbConnection connection = CreateConnection();
            return await ExecuteReaderAsync(connection, spName, CommandType.StoredProcedure, commandTimeout,
                commandParameters);
        }

        /// <summary>
        /// 对连接执行 Transact-SQL 语句并返回DataReader对象
        /// </summary>
        /// <remarks></remarks>
        /// <param name="connection">有效的数据库连接对象</param>
        /// <param name="commandType">指示 CommandText 是语句还是存储过程</param>
        /// <param name="commandText">要执行的 Transact-SQL 语句或存储过程</param>
        /// <param name="commandTimeout">设定Command执行时间，秒，大于0有效</param>
        /// <param name="commandParameters">用来执行命令的参数数组</param>
        /// <returns>执行命令后受影响的行数</returns>
        public virtual Task<DbDataReader> ExecuteReaderAsync(DbConnection connection, string commandText,
            CommandType commandType = CommandType.Text, int commandTimeout = 0, params DbParameter[] commandParameters)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            DbCommand cmd = connection.CreateCommand();
            bool mustCloseConnection;
            PrepareCommand(cmd, connection, commandText, out mustCloseConnection, null, commandType, commandParameters, commandTimeout);

            try
            {
                return cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);
            }
            catch
            {
                if (mustCloseConnection)
                {
                    connection.Close();
                }
                throw;
            }
            finally
            {
                cmd.Parameters.Clear();
                // 连接关闭后，DataReader将不可使用
                // connection.Close();
            }
        }

        #endregion ExecuteReaderAsync



        #region ExecuteScalar

        /// <summary>
        /// 对连接执行 Transact-SQL 语句并返回结果的首行首列
        /// </summary>
        /// <remarks>
        /// 示例:  
        ///  var result = ExecuteScalar("select * from tb where id=@id", new SqlParameter("@id", 24));
        /// </remarks>
        /// <param name="commandText">要执行的 Transact-SQL 语句</param>
        /// <param name="commandParameters">用来执行命令的参数数组</param>
        /// <returns>执行命令后受影响的行数</returns>
        public virtual object ExecuteScalar(string commandText, params DbParameter[] commandParameters)
        {
            using (DbConnection connection = CreateConnection())
            {
                connection.Open();
                return ExecuteScalar(connection, commandText, CommandType.Text, CommandTimeout, commandParameters);
            }
        }

        /// <summary>
        /// 对连接执行 Transact-SQL 语句并返回结果的首行首列
        /// </summary>
        /// <remarks>
        /// 示例:  
        ///  var result = ExecuteScalar("select * from tb where id=@id", 30, new SqlParameter("@id", 24));
        /// </remarks>
        /// <param name="commandText">要执行的 Transact-SQL 语句</param>
        /// <param name="commandTimeout">设定Command执行时间，秒，大于0有效</param>
        /// <param name="commandParameters">用来执行命令的参数数组</param>
        /// <returns>执行命令后受影响的行数</returns>
        public virtual object ExecuteScalar(string commandText, int commandTimeout, params DbParameter[] commandParameters)
        {
            using (DbConnection connection = CreateConnection())
            {
                connection.Open();
                return ExecuteScalar(connection, commandText, CommandType.Text, commandTimeout, commandParameters);
            }
        }

        /// <summary>
        /// 对连接执行 Transact-SQL 语句并返回结果的首行首列
        /// </summary>
        /// <remarks>
        /// 此方法不支持对输出参数或者存储过程里的返回参数的访问
        /// 
        /// 示例:  
        ///  var result = ExecuteScalarSP("PublishOrders", new SqlParameter("@prodid", 24));
        /// </remarks>
        /// <param name="spName">要执行的 存储过程名称</param>
        /// <param name="commandParameters">用来执行命令的参数数组</param>
        /// <returns>执行命令后受影响的行数</returns>
        public virtual object ExecuteScalarSP(string spName, params DbParameter[] commandParameters)
        {
            using (DbConnection connection = CreateConnection())
            {
                connection.Open();
                return ExecuteScalar(connection, spName, CommandType.StoredProcedure, CommandTimeout, commandParameters);
            }
        }

        /// <summary>
        /// 对连接执行 Transact-SQL 语句并返回结果的首行首列
        /// </summary>
        /// <remarks>
        /// 此方法不支持对输出参数或者存储过程里的返回参数的访问
        /// 
        /// 示例:  
        ///  var result = ExecuteScalarSP("PublishOrders", 30, new SqlParameter("@prodid", 24));
        /// </remarks>
        /// <param name="spName">要执行的 存储过程名称</param>
        /// <param name="commandTimeout">设定Command执行时间，秒，大于0有效</param>
        /// <param name="commandParameters">用来执行命令的参数数组</param>
        /// <returns>执行命令后受影响的行数</returns>
        public virtual object ExecuteScalarSP(string spName, int commandTimeout, params DbParameter[] commandParameters)
        {
            using (DbConnection connection = CreateConnection())
            {
                connection.Open();
                return ExecuteScalar(connection, spName, CommandType.StoredProcedure, commandTimeout, commandParameters);
            }
        }

        /// <summary>
        /// 对连接执行 Transact-SQL 语句并返回结果的首行首列
        /// </summary>
        /// <remarks></remarks>
        /// <param name="connection">有效的数据库连接对象</param>
        /// <param name="commandType">指示 CommandText 是语句还是存储过程</param>
        /// <param name="commandText">要执行的 Transact-SQL 语句或存储过程</param>
        /// <param name="commandTimeout">设定Command执行时间，秒，大于0有效</param>
        /// <param name="commandParameters">用来执行命令的参数数组</param>
        /// <returns>执行命令后受影响的行数</returns>
        public virtual object ExecuteScalar(DbConnection connection, string commandText,
            CommandType commandType = CommandType.Text, int commandTimeout = 0, params DbParameter[] commandParameters)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            DbCommand cmd = connection.CreateCommand();
            bool mustCloseConnection;
            PrepareCommand(cmd, connection, commandText, out mustCloseConnection, null, commandType, commandParameters, commandTimeout);

            try
            {
                return cmd.ExecuteScalar();
            }
            finally
            {
                cmd.Parameters.Clear();

                if (mustCloseConnection)
                {
                    connection.Close();
                }
            }
        }

        #endregion ExecuteScalar


        #region ExecuteScalarAsync

        /// <summary>
        /// 对连接执行 Transact-SQL 语句并返回结果的首行首列
        /// </summary>
        /// <remarks>
        /// 示例:  
        ///  var result = ExecuteScalarAsync("select * from tb where id=@id", new SqlParameter("@id", 24));
        /// </remarks>
        /// <param name="commandText">要执行的 Transact-SQL 语句</param>
        /// <param name="commandParameters">用来执行命令的参数数组</param>
        /// <returns>执行命令后受影响的行数</returns>
        public virtual async Task<object> ExecuteScalarAsync(string commandText, params DbParameter[] commandParameters)
        {
            using (DbConnection connection = CreateConnection())
            {
                connection.Open();
                return await ExecuteScalarAsync(connection, commandText, CommandType.Text, CommandTimeout, commandParameters);
            }
        }

        /// <summary>
        /// 对连接执行 Transact-SQL 语句并返回结果的首行首列
        /// </summary>
        /// <remarks>
        /// 示例:  
        ///  var result = ExecuteScalarAsync("select * from tb where id=@id", 30, new SqlParameter("@id", 24));
        /// </remarks>
        /// <param name="commandText">要执行的 Transact-SQL 语句</param>
        /// <param name="commandTimeout">设定Command执行时间，秒，大于0有效</param>
        /// <param name="commandParameters">用来执行命令的参数数组</param>
        /// <returns>执行命令后受影响的行数</returns>
        public virtual async Task<object> ExecuteScalarAsync(string commandText, int commandTimeout, params DbParameter[] commandParameters)
        {
            using (DbConnection connection = CreateConnection())
            {
                connection.Open();
                return await ExecuteScalarAsync(connection, commandText, CommandType.Text, commandTimeout, commandParameters);
            }
        }

        /// <summary>
        /// 对连接执行 Transact-SQL 语句并返回结果的首行首列
        /// </summary>
        /// <remarks>
        /// 此方法不支持对输出参数或者存储过程里的返回参数的访问
        /// 
        /// 示例:  
        ///  var result = ExecuteScalarAsyncSP("PublishOrders", new SqlParameter("@prodid", 24));
        /// </remarks>
        /// <param name="spName">要执行的 存储过程名称</param>
        /// <param name="commandParameters">用来执行命令的参数数组</param>
        /// <returns>执行命令后受影响的行数</returns>
        public virtual async Task<object> ExecuteScalarAsyncSP(string spName, params DbParameter[] commandParameters)
        {
            using (DbConnection connection = CreateConnection())
            {
                connection.Open();
                return await ExecuteScalarAsync(connection, spName, CommandType.StoredProcedure, CommandTimeout, commandParameters);
            }
        }

        /// <summary>
        /// 对连接执行 Transact-SQL 语句并返回结果的首行首列
        /// </summary>
        /// <remarks>
        /// 此方法不支持对输出参数或者存储过程里的返回参数的访问
        /// 
        /// 示例:  
        ///  var result = ExecuteScalarAsyncSP("PublishOrders", 30, new SqlParameter("@prodid", 24));
        /// </remarks>
        /// <param name="spName">要执行的 存储过程名称</param>
        /// <param name="commandTimeout">设定Command执行时间，秒，大于0有效</param>
        /// <param name="commandParameters">用来执行命令的参数数组</param>
        /// <returns>执行命令后受影响的行数</returns>
        public virtual async Task<object> ExecuteScalarAsyncSP(string spName, int commandTimeout, params DbParameter[] commandParameters)
        {
            using (DbConnection connection = CreateConnection())
            {
                connection.Open();
                return await ExecuteScalarAsync(connection, spName, CommandType.StoredProcedure, commandTimeout, commandParameters);
            }
        }

        /// <summary>
        /// 对连接执行 Transact-SQL 语句并返回结果的首行首列
        /// </summary>
        /// <remarks></remarks>
        /// <param name="connection">有效的数据库连接对象</param>
        /// <param name="commandType">指示 CommandText 是语句还是存储过程</param>
        /// <param name="commandText">要执行的 Transact-SQL 语句或存储过程</param>
        /// <param name="commandTimeout">设定Command执行时间，秒，大于0有效</param>
        /// <param name="commandParameters">用来执行命令的参数数组</param>
        /// <returns>执行命令后受影响的行数</returns>
        public virtual Task<object> ExecuteScalarAsync(DbConnection connection, string commandText,
            CommandType commandType = CommandType.Text, int commandTimeout = 0, params DbParameter[] commandParameters)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            DbCommand cmd = connection.CreateCommand();
            bool mustCloseConnection;
            PrepareCommand(cmd, connection, commandText, out mustCloseConnection, null, commandType, commandParameters, commandTimeout);

            try
            {
                return cmd.ExecuteScalarAsync();
            }
            finally
            {
                cmd.Parameters.Clear();

                if (mustCloseConnection)
                {
                    connection.Close();
                }
            }
        }

        #endregion ExecuteScalarAsync


        #region 其它方法
        /// <summary>
        /// 测试连接是否成功
        /// </summary>
        /// <returns></returns>
        public virtual bool TestConnection()
        {
            ExecuteScalar("select 1");
            return true;
        }


        #endregion

        #region 基类的静态方法


        /// <summary>
        /// 创建指定连接字符串的Helper对象
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="testConnection">是否要验证数据库连接正常</param>
        /// <returns></returns>
        public static T GetConnection<T>(string connectionString = null, bool testConnection = false) where T : BaseSqlHelper
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentException("连接串不能为空", nameof(connectionString));
            }
            T ret;

            var tp = typeof(T);
            lock (_connections)
            {
                if (!_connections.TryGetValue(connectionString, out var tmp))
                {
                    var typeName = tp.FullName ?? "";
                    var obj = tp.Assembly.CreateInstance(tp.FullName ?? "", false,
                        BindingFlags.Instance | BindingFlags.NonPublic, null, null, null, null);

                    tmp = obj as BaseSqlHelper;
                    if (tmp == null)
                    {
                        throw new Exception($"{typeName} 实例创建失败");
                    }
                    tmp.ConnectionString = connectionString;
                    //ret = new T { ConnectionString = connectionString };
                    _connections[connectionString] = tmp;
                }
                ret = tmp as T ?? throw new Exception($"缓存的实例不符，希望：{tp.Name}, 缓存：{tmp.GetType().Name}");
            }
            if (testConnection)
            {
                ret.TestConnection();
            }
            return ret;
        }

        /// <summary>
        /// 获取字段值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="colName"></param>
        /// <param name="reader"></param>
        /// <param name="allColNames"></param>
        /// <returns></returns>
        public static T GetValue<T>(string colName, IDataReader reader, List<string> allColNames = null)
        {
            colName = colName.ToLower();
            if (allColNames == null || allColNames.Contains(colName))
            {
                var obj = reader[colName];
                if (obj != null && obj != DBNull.Value)
                {
                    return (T)Convert.ChangeType(obj, typeof(T));
                }
            }
            return default(T);
        }
        

        /// <summary>
        /// 获取DataReader的每列列名
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="getLower">列名是否要转换小写</param>
        /// <returns></returns>
        public static List<string> GetColNames(IDataReader reader, bool getLower = false)
        {
            List<string> ret = new List<string>();
            if (reader == null)// || !reader.HasRows)
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


        /// <summary>
        /// 将参数数组关系到命令对象。
        /// 任何可输入输出参数或者空值，通过该方法将被分配一个 DbNull 值。
        /// </summary>
        /// <param name="command">要添加参数的命令对象</param>
        /// <param name="commandParameters">被添加到命令对象的参数数组</param>
        protected static void AttachParameters(DbCommand command, IEnumerable<DbParameter> commandParameters)
        {
            if (commandParameters == null)
            {
                return;
            }
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }
            foreach (DbParameter parameter in commandParameters)
            {
                if (parameter != null)
                {
                    if ((parameter.Direction == ParameterDirection.InputOutput ||
                         parameter.Direction == ParameterDirection.Input) && (parameter.Value == null))
                    {
                        // null传入会出现异常，必须改DBNull
                        parameter.Value = DBNull.Value;
                    }
                    command.Parameters.Add(parameter);
                }
            }
        }


        /// <summary>
        /// 准备数据操作命令
        /// </summary>
        /// <param name="command">待准备的命令对象</param>
        /// <param name="connection">执行该命令的有效数据库连接</param>
        /// <param name="transaction">有效数据事务对象，或者 null</param>
        /// <param name="commandType">获取或设置一个值，该值指示如何解释 CommandText 属性</param>
        /// <param name="commandText">获取或设置要对数据源执行的 Transact-SQL 语句或存储过程</param>
        /// <param name="commandParameters">DbParameter 参数数组，如果无参数则为 null</param>
        /// <param name="commandTimeout">命令超时时间</param>
        /// <param name="mustCloseConnection"><c>true</c> 如果打开数据库连接则为 true，否则为 false</param>
        protected static void PrepareCommand(DbCommand command, DbConnection connection, string commandText, out bool mustCloseConnection, 
            SqlTransaction transaction = null, CommandType commandType = CommandType.Text,
            IEnumerable<DbParameter> commandParameters = null, int commandTimeout = 30)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }
            if (string.IsNullOrEmpty(commandText))
            {
                throw new ArgumentNullException(nameof(commandText));
            }

            // 如果该数据库连接没有打开，则设置为打开状态
            if (connection.State != ConnectionState.Open)
            {
                mustCloseConnection = true;
                connection.Open();
            }
            else
            {
                mustCloseConnection = false;
            }

            command.Connection = connection;
            command.CommandText = commandText;
            command.CommandTimeout = commandTimeout;

            // 如果有提供数据事务
            if (transaction != null)
            {
                if (transaction.Connection == null)
                {
                    throw new ArgumentException("打开状态的事务允许数据操作回滚或者提交。", nameof(transaction));
                }
                command.Transaction = transaction;
            }

            command.CommandType = commandType;

            if (commandParameters != null)
            {
                AttachParameters(command, commandParameters);
            }
        }

        #endregion
    }

}
