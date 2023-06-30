using System;
using System.Text;
using Beinet.Core.Database;
using Beinet.MySql;
using NLog;

namespace OPJobs.MySql
{
    class MySqlBackup
    {
        private static ILogger logger = LogManager.GetCurrentClassLogger();

        private readonly DbItem dbInfo;
        private readonly string connectionStr;
        private readonly BaseSqlHelper dbWriter;

        public MySqlBackup(DbItem item, BaseSqlHelper dbWriter)
        {
            this.dbInfo = item;
            this.connectionStr = $"server={item.Server};Port={item.Port.ToString()};" +
                                 $"Database=mysql;uid={item.Uid};pwd={item.Pwd}";

            this.dbWriter = dbWriter;
        }

        public void BackupMySql()
        {
            DbItem item = this.dbInfo;

            //            var arrDb = GetAllDb(connectionStr);
            //
            //            logger.Info($"BackupMySql start:\r\n{item.Server} 将导出{arrDb.Count.ToString()}个数据库");
            //            foreach (var db in arrDb)
            //            {
            //                BackupDDL(item, db);
            //            }

            var rownum = BackupInfo();
            logger.Info($"BackupMySql 行数 {rownum} {item.Server}");
        }

        //        /// <summary>
        //        /// 返回所有数据库
        //        /// </summary>
        //        /// <param name="connectionStr">连接串</param>
        //        /// <returns></returns>
        //        static List<string> GetAllDb(string connectionStr)
        //        {
        //            var arrDb = new List<string>();
        //            var sql = @"SELECT distinct t.table_schema FROM information_schema.tables t
        //WHERE t.table_schema NOT IN ('performance_schema', 'mysql', 'information_schema', 'sys')
        //ORDER BY t.table_schema";
        //            var connection = MySqlHelper.GetConnection(connectionStr);
        //            using (var reader = connection.ExecuteReader(sql))
        //            {
        //                while (reader.Read())
        //                {
        //                    arrDb.Add(Convert.ToString(reader["table_schema"]));
        //                }
        //            }
        //
        //
        //            return arrDb;
        //        }

        //        static void BackupDDL(DbItem item, string db)
        //        {
        //            // mysqldump.exe的所在路径
        //            var dumpExe = ConfigurationManager.AppSettings["MySqlDumpPath"] ?? "";
        //            var cmdTmp = $"-h\"{item.Server}\" -P{item.Port.ToString()} -u{item.Uid} -p\"{item.Pwd}\" " +
        //                         $"--default-character-set=utf8 --skip-lock-tables --set-gtid-purged=OFF --skip-dump-date";
        //
        //            var backSqlFile = Path.Combine(BackupDir, $"{item.Server}-{db}.sql");
        //            var exportData = item.ExportData ? "" : "-d";// -d表示不导出数据
        //
        //            var cmd = $"{cmdTmp} \"{db}\" {exportData}";
        //            logger.Info($"BackupMySql DDL start:\r\n{cmd}");
        //            var ret = Helper.Exec(dumpExe, cmd);
        //
        //            Helper.WriteFile(backSqlFile, ClearDumpSql(ret));
        //            logger.Info($"BackupMySql DDL end:\r\n{backSqlFile}");
        //        }
        //        private static Regex RegAutoInc = new Regex(@"(?i)\s+AUTO_INCREMENT\s*=\s*\d+\s+");
        //        /// <summary>
        //        /// 清理导出的语句里，无关的语句，如AUTO_INCREMENT
        //        /// </summary>
        //        /// <param name="dumpSql"></param>
        //        /// <returns></returns>
        //        static string ClearDumpSql(string dumpSql)
        //        {
        //            var ret = RegAutoInc.Replace(dumpSql, " ");
        //            return ret;
        //        }


        /// <summary>
        /// 获取当前实例所有数据库的表名、行数、大小等信息
        /// </summary>
        /// <returns>插入行数</returns>
        int BackupInfo()
        {
            const string sql =
                @"SELECT t.table_schema, t.table_name, t.table_rows, t.avg_row_length, t.data_length , t.index_length, t.auto_increment
FROM information_schema.tables t
WHERE t.`TABLE_SCHEMA` NOT IN ('performance_schema', 'sys', 'information_schema', 'mysql')
ORDER BY t.table_schema, t.table_name";

            logger.Info($"BackupMySql INFO start:\r\n{connectionStr}");

            var content = new StringBuilder(100000);

            var connection = MySqlHelper.GetConnection(connectionStr);
            using (var reader = connection.ExecuteReader(sql))
            {
                if (!reader.HasRows)
                    return 0;

                while (reader.Read())
                {
                    if (content.Length > 0)
                        content.AppendLine(",");
                    content.AppendFormat("('{0}','{1}',{2},{3},{4},{5},{6})",
                        reader["table_schema"],
                        reader["table_name"],
                        GetNum(reader["table_rows"]),
                        GetNum(reader["avg_row_length"]),
                        GetNum(reader["data_length"]),
                        GetNum(reader["index_length"]),
                        GetNum(reader["auto_increment"]));
                }
            }

            content.Insert(0, @"INSERT INTO dbdata(dbname,tbname,rownum,avg_len,data_len,index_len,auto_idx)VALUES");
            return dbWriter.ExecuteNonQuery(content.ToString());
        }

        static string GetNum(object obj)
        {
            if (obj == null || obj == DBNull.Value)
                return "0";
            return Convert.ToInt64(obj).ToString();
        }
    }
}