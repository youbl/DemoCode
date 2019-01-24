using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Beinet.Core.Lock;

namespace Beinet.Core.Logging
{
    /// <summary>
    /// 日志记录默认实现
    /// </summary>
    public class LoggerDefault : ILogger, IDisposable
    {
        /// <summary>
        /// 单例
        /// </summary>
        public static LoggerDefault Default = new LoggerDefault();
        /// <summary>
        /// 
        /// </summary>
        public enum Level
        {
            /// <summary>
            /// 
            /// </summary>
            Debug,
            /// <summary>
            /// 
            /// </summary>
            Info,
            /// <summary>
            /// 
            /// </summary>
            Warn,
            /// <summary>
            /// 
            /// </summary>
            Error,
        }

        /// <summary>
        /// 只能单进程加锁，不能跨进程
        /// </summary>
        static ILock locker = new LockDefault();
        /// <summary>
        /// 默认日志目录
        /// </summary>
        static string defaultLogDir = Path.Combine(Environment.CurrentDirectory, "logs");

        /// <summary>
        /// 日志记录类名
        /// </summary>
        public string Name { get; protected set; } = nameof(LoggerDefault);

        /// <summary>
        /// 记录 Debug 信息
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void Debug(string format, params object[] args)
        {
            DoLog(Level.Debug, format, args);
        }

        /// <summary>
        /// 记录 Info 信息
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void Info(string format, params object[] args)
        {
            DoLog(Level.Info, format, args);
        }

        /// <summary>
        /// 记录 Warn 信息
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void Warn(string format, params object[] args)
        {
            DoLog(Level.Warn, format, args);
        }

        /// <summary>
        /// 记录 Error 信息
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void Error(string format, params object[] args)
        {
            DoLog(Level.Error, format, args);
        }

        /// <summary>
        /// 将日志写入自定义位置(pathname)
        /// </summary>
        /// <param name="pathname">消息的前缀或者前级路径</param>
        /// <param name="format">消息</param>
        /// <param name="args">消息</param>
        public void Custom(string pathname, string format, params object[] args)
        {
            var logFile = Path.Combine(defaultLogDir, pathname);
            WriteFile(logFile, format, args);
        }

        private void DoLog(Level level, string format, params object[] args)
        {
            var logFile = Path.Combine(defaultLogDir, $"{level.ToString()}") + "/";
            WriteFile(logFile, format, args);
        }

        private void WriteFile(string logPrefix, string format, params object[] args)
        {
            var now = DateTime.Now;
            var logFile = logPrefix + $"{now.ToString("yyyyMMdd")}.txt";

            var dir = Path.GetDirectoryName(logFile) ?? "";
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            var msg = args != null && args.Length > 0 ? string.Format(format, args) : format;
            msg = now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " " + msg;

            // 异步写入日志
            Task.Factory.StartNew(async () =>
            {
                await locker.DoAction(async () =>
                {
                    using (var sw = new StreamWriter(logFile, true, Encoding.UTF8))
                    {
                        await sw.WriteLineAsync(msg);
                    }
                });
            });
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            locker.Dispose();
        }

    }
}
