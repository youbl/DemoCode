using System.Data.Common;
using System.Text;
using NLog;

namespace Beinet.SqlLog
{
    internal class SqlExecuteLog
    {
        static ILogger _logger = LogManager.GetCurrentClassLogger();

        public static void Prefix(DbCommand __instance)
        {
            var command = __instance;
            var sb = new StringBuilder(command.CommandText);
            if (command.Parameters.Count > 0)
            {
                foreach (DbParameter parameter in command.Parameters)
                {
                    sb.AppendFormat("\r\n\t{0} : {1}", parameter.ParameterName, parameter.Value);
                }
            }

            _logger.Error(sb.ToString());
        }
    }
}
