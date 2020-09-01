using System;
using System.Configuration;
using System.IO;
using System.Threading.Tasks;
using Beinet.Core;
using Beinet.Core.Cron;
using Beinet.Core.Database;
using Beinet.MySql;
using NLog;

namespace OPJobs.MySql
{
    /// <summary>
    /// 备份数据库表结构的类
    /// </summary>
    class DbBackup : IRunable
    {
        private static ILogger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// 数据库连接配置文件
        /// </summary>
        private static string XmlFile = ConfigurationManager.AppSettings["DbBackupXmlFile"] ?? "DbBackup.xml";
        /// <summary>
        /// 数据写入哪里
        /// </summary>
        private static BaseSqlHelper dbWriter = MySqlHelper.GetConnection(ConfigurationManager.AppSettings["dbWrite"] ?? "");

        // 每15分钟支行一次
        [Scheduled("0 */15 * * * *")]
        public void Run()
        {
            try
            {
                if (string.IsNullOrEmpty(XmlFile))
                {
                    logger.Error("DbBackupXmlFile 未配置");
                    return;
                }
                if (!File.Exists(XmlFile))
                {
                    XmlFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, XmlFile);
                    if (!File.Exists(XmlFile))
                    {
                        logger.Error($"DbBackup:{XmlFile} 不存在");
                        return;
                    }
                }

                logger.Info($"DbBackup:采集配置 {XmlFile}");
                var configs = LoadDbConfig.GetConfigFromFile(XmlFile);
                if (configs == null || configs.Count <= 0)
                {
                    return;
                }

                logger.Info($"DbBackup:采集到配置 {configs.Count.ToString()} 条，开始处理");

                var option = new ParallelOptions { MaxDegreeOfParallelism = 100 };
                Parallel.ForEach(configs, option, ProcessData);
                logger.Info("DbBackup:完成处理");
            }
            catch (Exception exp)
            {
                logger.Error("DbBackup:线程异常：" + exp);
            }
        }

        static void ProcessData(DbItem item)
        {
            if (item.Type != DbType.MySql)
            {
                logger.Error($"DbBackup:暂时不支持 {item.Type}");
                return;
            }
            new MySqlBackup(item, dbWriter).BackupMySql();
        }
    }

}
