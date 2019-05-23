using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Reflection;

namespace Beinet.Core.Database
{
    /// <summary>
    /// 将常用的数据操作以高性能、可扩展方式进行封装，适用于 Microsoft SQLServer 2000 及以上版本。
    /// 请使用 SqlHelper.GetConnection 创建实例
    /// </summary>
    public class SqlHelper : BaseSqlHelper
    {
        private SqlHelper()
        {
        }
        
        /// <summary>
        /// 创建指定连接字符串的Helper对象
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="testConnection">是否要验证数据库连接正常</param>
        /// <returns></returns>
        public static SqlHelper GetConnection(string connectionString = null, bool testConnection = false)
        {
            return BaseSqlHelper.GetConnection<SqlHelper>(connectionString, testConnection);
        }

        /// <summary>
        /// 创建SqlServer连接对象
        /// </summary>
        /// <returns></returns>
        public override DbConnection CreateConnection()
        {
            return new SqlConnection(ConnectionString);
        }
        /// <summary>
        /// 创建SqlDataAdapter适配器对象
        /// </summary>
        /// <returns></returns>
        public override DbDataAdapter CreateDataAdapter()
        {
            return new SqlDataAdapter();
        }

        /// <summary>
        /// 创建参数对象
        /// </summary>
        /// <param name="parameterName"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public override DbParameter CreatePara(string parameterName = "", object value = null)
        {
            return new SqlParameter(parameterName, value);
        }

        #region SqlBulkCopy批量数据拷贝的方法
        static FieldInfo _rowsCopiedField;
        /// <summary>
        /// 获取SqlBulkCopy对象的已拷贝行数字段反射
        /// </summary>
        static FieldInfo RowsCopidField
        {
            get
            {
                if (_rowsCopiedField == null)
                {
                    _rowsCopiedField = typeof(SqlBulkCopy).GetField("_rowsCopied", BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Instance);
                    if (_rowsCopiedField == null)
                    {
                        throw new Exception("从SqlBulkCopy类型中获取_rowsCopied字段失败");
                    }
                }
                return _rowsCopiedField;
            }
        }

        /// <summary>
        /// 把DataTable里的数据通过SqlBulkCopy复制到当前数据库的指定表中
        /// </summary>
        /// <param name="sourceData">数据源</param>
        /// <param name="targetTableName">目标表名</param>
        /// <param name="timeOut">超时之前操作完成所需的秒数</param>
        /// <param name="keepIdentity">是否保留源标识值，如果为false，则由目标分配标识值</param>
        /// <param name="batchSize">每一批次中的行数。在每一批次结束时，将该批次中的行发送到服务器</param>
        /// <param name="copiedEventHandler">在每次处理完 batchSize条记录时触发此事件</param>
        public void BulkCopy(DataTable sourceData, string targetTableName,
            int timeOut = 30, bool keepIdentity = true, int batchSize = 2000,
            SqlRowsCopiedEventHandler copiedEventHandler = null)
        {
            IDataReader reader = sourceData.CreateDataReader();
            BulkCopy(reader, targetTableName, timeOut, keepIdentity, batchSize, copiedEventHandler);
        }

        /// <summary>
        /// 把DataReader里的数据通过SqlBulkCopy复制到当前数据库的指定表中
        /// </summary>
        /// <param name="sourceData">数据源</param>
        /// <param name="targetTableName">目标表名</param>
        /// <param name="timeOut">超时之前操作完成所需的秒数</param>
        /// <param name="keepIdentity">是否保留源标识值，如果为false，则由目标分配标识值</param>
        /// <param name="batchSize">每一批次中的行数。在每一批次结束时，将该批次中的行发送到服务器</param>
        /// <param name="copiedEventHandler">在每次处理完 batchSize条记录时触发此事件</param>
        public void BulkCopy(IDataReader sourceData, string targetTableName,
            int timeOut = 30, bool keepIdentity = true, int batchSize = 2000,
            SqlRowsCopiedEventHandler copiedEventHandler = null)
        {
            var opn = keepIdentity ? SqlBulkCopyOptions.KeepIdentity : SqlBulkCopyOptions.Default;
            using (SqlBulkCopy bcp = new SqlBulkCopy(ConnectionString, opn))
            {
                bcp.BulkCopyTimeout = timeOut;
                if (copiedEventHandler != null)
                    bcp.SqlRowsCopied += copiedEventHandler; // 用于进度显示

                bcp.BatchSize = batchSize;
                bcp.NotifyAfter = batchSize;// 设置为1，状态栏提示比较准确，但是速度很慢

                bcp.DestinationTableName = targetTableName;

                // 设置同名列的映射,避免建表语句列顺序不一致导致无法同步的bug
                List<string> arrColNames = GetColNames(sourceData);
                foreach (string colName in arrColNames)
                {
                    bcp.ColumnMappings.Add(colName, colName);
                }
                bcp.WriteToServer(sourceData);
            }
        }

        /// <summary>
        /// 把DataReader里的数据通过SqlBulkCopy复制到当前数据库的指定表中
        /// </summary>
        /// <param name="sourceData">数据源</param>
        /// <param name="targetTableName">目标表名</param>
        /// <param name="timeOut">超时之前操作完成所需的秒数</param>
        /// <param name="keepIdentity">是否保留源标识值，如果为false，则由目标分配标识值</param>
        /// <param name="batchSize">每一批次中的行数。在每一批次结束时，将该批次中的行发送到服务器</param>
        /// <param name="copiedEventHandler">在每次处理完 batchSize条记录时触发此事件</param>
        public int BulkCopyWithCnt(IDataReader sourceData, string targetTableName,
            int timeOut = 30, bool keepIdentity = true, int batchSize = 2000,
            SqlRowsCopiedEventHandler copiedEventHandler = null)
        {
            var opn = keepIdentity ? SqlBulkCopyOptions.KeepIdentity : SqlBulkCopyOptions.Default;
            using (SqlBulkCopy bcp = new SqlBulkCopy(ConnectionString, opn))
            {
                bcp.BulkCopyTimeout = timeOut;
                if (copiedEventHandler != null)
                    bcp.SqlRowsCopied += copiedEventHandler; // 用于进度显示

                bcp.BatchSize = batchSize;
                bcp.NotifyAfter = batchSize;// 设置为1，状态栏提示比较准确，但是速度很慢

                bcp.DestinationTableName = targetTableName;

                // 设置同名列的映射,避免建表语句列顺序不一致导致无法同步的bug
                List<string> arrColNames = GetColNames(sourceData);
                foreach (string colName in arrColNames)
                {
                    bcp.ColumnMappings.Add(colName, colName);
                }
                bcp.WriteToServer(sourceData);
                return GetRowsCopied(bcp);
            }
        }


        /// <summary>
        /// Gets the rows copied from the specified SqlBulkCopy object
        /// </summary>
        /// <param name="bulkCopy">The bulk copy.</param>
        /// <returns></returns>
        public static int GetRowsCopied(SqlBulkCopy bulkCopy)
        {
            return (int)RowsCopidField.GetValue(bulkCopy);
        }
        #endregion

    }
}
