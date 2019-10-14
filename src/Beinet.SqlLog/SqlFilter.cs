using System.Data.Common;
using System.Text;
using NLog;

namespace Beinet.SqlLog
{
    /// <summary>
    /// 演示用Filter，用于输出每次执行的sql日志
    /// </summary>
    public class SqlFilter : IFilter
    {
        static ILogger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// 执行sql之前的方法，记录要执行的sql
        /// </summary>
        /// <param name="command"></param>
        public void BeforeExecute(DbCommand command)
        {
            var sb = new StringBuilder();
            sb.AppendFormat("{0}-{1} 准备执行", command.GetType().Name, command.CommandText);
            if (command.Parameters.Count > 0)
            {
                foreach (DbParameter parameter in command.Parameters)
                {
                    sb.AppendFormat("\r\n\t{0} : {1}", parameter.ParameterName, parameter.Value);
                }
            }

            _logger.Info(sb.ToString());
        }

        /// <summary>
        /// 执行sql之后的方法
        /// </summary>
        /// <param name="command"></param>
        public void AfterExecute(DbCommand command)
        {
            var sb = new StringBuilder();
            sb.AppendFormat("{0}-{1} 执行完成", command.GetType().Name, command.CommandText);
            _logger.Info(sb.ToString());
        }
    }
}
