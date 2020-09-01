using System;
using System.Data.SqlClient;
using System.Text;
using System.Web;
using NLog;

namespace Beinet.Core.Logging
{
    /// <summary>
    /// NLog日志类的扩展方法，添加通用信息。
    /// 注：为了便于日志排查的效率，请在每个需要输出日志的类定义logger，如：
    /// static ILogger logger = LogManager.GetCurrentClassLogger();
    /// 这样，输出的日志，可以有所在类信息，可以快速知道哪个类输出的日志。
    /// </summary>
    public static class LogHelper
    {
        #region ILogger扩展方法
        /// <summary>
        /// 添加上下文信息后，再记录日志
        /// </summary>
        /// <param name="nlogger">日志对象</param>
        /// <param name="info">日志</param>
        public static void TraceExt(this NLog.ILogger nlogger, string info)
        {
            nlogger.Trace(() => BuildMessage(info));
        }

        /// <summary>
        /// 添加上下文信息后，再记录日志
        /// </summary>
        /// <param name="nlogger">日志对象</param>
        /// <param name="messageFunc">返回日志的方法</param>
        public static void TraceExt(this NLog.ILogger nlogger, LogMessageGenerator messageFunc)
        {
            nlogger.Trace(() => BuildMessage(messageFunc()));
        }

        /// <summary>
        /// 添加上下文信息后，再记录日志
        /// </summary>
        /// <param name="nlogger">日志对象</param>
        /// <param name="info">日志</param>
        public static void DebugExt(this NLog.ILogger nlogger, string info)
        {
            nlogger.Debug(() => BuildMessage(info));
        }

        /// <summary>
        /// 添加上下文信息后，再记录日志
        /// </summary>
        /// <param name="nlogger">日志对象</param>
        /// <param name="messageFunc">返回日志的方法</param>
        public static void DebugExt(this NLog.ILogger nlogger, LogMessageGenerator messageFunc)
        {
            nlogger.Debug(() => BuildMessage(messageFunc()));
        }

        /// <summary>
        /// 添加上下文信息后，再记录日志
        /// </summary>
        /// <param name="nlogger">日志对象</param>
        /// <param name="info">日志</param>
        public static void InfoExt(this NLog.ILogger nlogger, string info)
        {
            nlogger.Info(() => BuildMessage(info));
        }

        /// <summary>
        /// 添加上下文信息后，再记录日志
        /// </summary>
        /// <param name="nlogger">日志对象</param>
        /// <param name="messageFunc">返回日志的方法</param>
        public static void InfoExt(this NLog.ILogger nlogger, LogMessageGenerator messageFunc)
        {
            nlogger.Info(() => BuildMessage(messageFunc()));
        }


        /// <summary>
        /// 添加上下文信息后，再记录日志
        /// </summary>
        /// <param name="nlogger">日志对象</param>
        /// <param name="info">日志</param>
        public static void WarnExt(this NLog.ILogger nlogger, string info)
        {
            nlogger.Warn(() => BuildMessage(info));
        }

        /// <summary>
        /// 添加上下文信息后，再记录日志
        /// </summary>
        /// <param name="nlogger">日志对象</param>
        /// <param name="messageFunc">返回日志的方法</param>
        public static void WarnExt(this NLog.ILogger nlogger, LogMessageGenerator messageFunc)
        {
            nlogger.Warn(() => BuildMessage(messageFunc()));
        }

        /// <summary>
        /// 添加上下文信息后，再记录日志
        /// </summary>
        /// <param name="nlogger">日志对象</param>
        /// <param name="info">日志</param>
        /// <param name="exp">异常对象</param>
        public static void ErrorExt(this NLog.ILogger nlogger, string info, Exception exp = null)
        {
            nlogger.Error(() => BuildMessage(info, exp));
        }

        /// <summary>
        /// 添加上下文信息后，再记录日志
        /// </summary>
        /// <param name="nlogger">日志对象</param>
        /// <param name="messageFunc">返回日志的方法</param>
        /// <param name="exp">异常对象</param>
        public static void ErrorExt(this NLog.ILogger nlogger, LogMessageGenerator messageFunc, Exception exp = null)
        {
            nlogger.Error(() => BuildMessage(messageFunc(), exp));
        }

        /// <summary>
        /// 添加上下文信息后，再记录日志
        /// </summary>
        /// <param name="nlogger">日志对象</param>
        /// <param name="info">日志</param>
        /// <param name="exp">异常对象</param>
        public static void FatalExt(this NLog.ILogger nlogger, string info, Exception exp = null)
        {
            nlogger.Fatal(() => BuildMessage(info, exp));
        }

        /// <summary>
        /// 添加上下文信息后，再记录日志
        /// </summary>
        /// <param name="nlogger">日志对象</param>
        /// <param name="messageFunc">返回日志的方法</param>
        /// <param name="exp">异常对象</param>
        public static void FatalExt(this NLog.ILogger nlogger, LogMessageGenerator messageFunc, Exception exp = null)
        {
            nlogger.Fatal(() => BuildMessage(messageFunc(), exp));
        }
        #endregion

        /// <summary>
        /// 补充一些常见的上下文日志，方便问题排查
        /// </summary>
        /// <param name="info"></param>
        /// <param name="ex"></param>
        /// <returns></returns>
        static string BuildMessage(string info, Exception ex = null)
        {
            StringBuilder sb = new StringBuilder();
            HttpRequest request = null;
            try
            {
                if (HttpContext.Current != null)
                    request = HttpContext.Current.Request;
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch
            {
                // 在Web站点的Application_Start里，不允许访问 HttpContext.Current 上下文
            }
            sb.AppendFormat("{0}\r\n", info);

            if (request != null)
            {
                // 存在http上下文时，把请求的url和ip等信息带上
                sb.AppendFormat("{1}:{0}\r\n", request.Url, request.HttpMethod);

                var post = Convert.ToString(request.Form);
                if (post.Length > 0)
                {
                    sb.AppendFormat("Post: {0}\r\n", HttpUtility.UrlDecode(post));
                }

                var header = Convert.ToString(request.Headers);
                if (header.Length > 0)
                {
                    sb.AppendFormat("Header: {0}\r\n", HttpUtility.UrlDecode(header));
                }

                string realip = request.ServerVariables["HTTP_X_REAL_IP"];
                string forwardip = request.ServerVariables["HTTP_X_FORWARDED_FOR"];
                sb.AppendFormat("UserHostAddress:{0};{1};{2}\r\n", request.UserHostAddress, realip, forwardip);
                // sb.AppendFormat("WebServer:{0}\r\n", request.ServerVariables["LOCAL_ADDR"]);
            }

            if (ex != null)
            {
                var sqlException = ex as SqlException;
                if (sqlException != null)
                    sb.AppendFormat("Database:{0}\r\n", sqlException.Server);
                sb.AppendFormat("Exception:{0}\r\n", ex);
            }
            sb.AppendLine();
            return sb.ToString();
        }

    }
}
