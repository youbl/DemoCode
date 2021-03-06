﻿using System;
using System.Data;
using System.Data.Common;
using System.Text;
using System.Web;
using NLog;

namespace Beinet.SqlLog
{
    /// <summary>
    /// 演示用Filter，用于输出每次执行的sql日志.
    /// 注： connection.Open() 会依次执行如下5个SQL：
    /// "SHOW VARIABLES"、"SHOW WARNINGS"、"SELECT TIMEDIFF(NOW(), UTC_TIMESTAMP())"、"SHOW COLLATION"、"SET character_set_results=NULL"
    /// </summary>
    public class SqlFilter : IFilter
    {
        static ILogger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// 执行sql之前的方法，记录要执行的sql
        /// </summary>
        public void BeforeExecute(DbCommand command)
        {
            var cmd = GetCommand(command);
            if (string.IsNullOrEmpty(cmd))
                return;

            var sb = new StringBuilder();
            sb.AppendFormat("{0} {1} 准备执行", command.GetType().Name, cmd);
            if (command.Parameters.Count > 0)
            {
                foreach (DbParameter parameter in command.Parameters)
                {
                    sb.AppendFormat("\r\n\t{0} : {1}", parameter.ParameterName, parameter.Value);
                }
            }

            LogInfo(sb);
        }

        /// <summary>
        /// 执行sql之后的方法.
        /// 注：抛异常时，不会来这里
        /// </summary>
        public void AfterExecute(DbCommand command, object result, long costMillis)
        {
            // return;
            var cmd = GetCommand(command);
            if (string.IsNullOrEmpty(cmd))
                return;

            var sb = new StringBuilder();
            sb.AppendFormat("{0} {1} 执行完成耗时:{2}ms", command.GetType().Name, cmd, costMillis);
            if (result is IDataReader reader)
            {
                sb.AppendFormat(" 字段数:{0}", reader.FieldCount);
                // 这里不能reader.Read, 会影响外部的操作
            }

            LogInfo(sb);
        }

        private string GetCommand(DbCommand command)
        {
            var ret = command?.CommandText;
            if (ret == null || (ret = ret.Trim()).Length == 0)
                return "";
            // 不记录事务语句
            if (ret.Equals("BEGIN", StringComparison.OrdinalIgnoreCase) ||
                ret.Equals("COMMIT", StringComparison.OrdinalIgnoreCase))
                return "";
            // 只记录更新语句
            if (ret.StartsWith("SHOW", StringComparison.OrdinalIgnoreCase) ||
                ret.StartsWith("SET ", StringComparison.OrdinalIgnoreCase))
                return "";
            return ret;
        }

        private void LogInfo(StringBuilder msg)
        {
            try
            {
                if (HttpContext.Current != null)
                {
                    var request = HttpContext.Current.Request;
                    msg.AppendLine().AppendFormat("{1}:{0}\r\n", request.Url, request.HttpMethod);

                    var post = Convert.ToString(request.Form);
                    if (post.Length > 0)
                    {
                        msg.AppendFormat("Post: {0}\r\n", HttpUtility.UrlDecode(post));
                    }

                    var header = Convert.ToString(request.Headers);
                    if (header.Length > 0)
                    {
                        msg.AppendFormat("Header: {0}\r\n", HttpUtility.UrlDecode(header));
                    }

                    string realip = request.ServerVariables["HTTP_X_REAL_IP"];
                    string forwardip = request.ServerVariables["HTTP_X_FORWARDED_FOR"];
                    msg.AppendFormat("UserHostAddress:{0};{1};{2}\r\n", request.UserHostAddress, realip, forwardip);
                    // sb.AppendFormat("WebServer:{0}\r\n", request.ServerVariables["LOCAL_ADDR"]);
                }
            }
            catch (Exception exp)
            {
                msg.AppendLine().Append(exp);
            }

            _logger.Info(msg);
        }
    }
}