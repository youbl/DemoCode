
using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using Beinet.Core.Cron;
using NLog;

namespace OPJobs
{
    /// <summary>
    /// 执行一些OP运维的job
    /// </summary>
    static class Program
    {
        private static ILogger logger = LogManager.GetCurrentClassLogger();

        /**
         * 有参数时，按参数执行.
         * 没有参数时，按 Scheduled 特性配置执行
         */
        static void Main(string[] args)
        {
            // 全局异常捕获
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            DoStartLog();

            if (args != null && args.Length > 0)
            {
                JobSearcher.Start(args);
            }
            else
            {
                var jobNum = ScheduledWorker.StartAllScheduled();
                if (jobNum > 0)
                {
                    // 有任务时，要read阻塞，避免程序退出
                    Console.Read();
                }
            }

            DoStopLog();
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var msg = "CurrentDomain_UnhandledExp:" + (e == null ? "" : e.ExceptionObject);

            logger.Error(msg);
            Console.WriteLine(msg);
        }

        /// <summary>
        /// 程序启动日志
        /// </summary>
        static void DoStartLog()
        {
            var ass = Assembly.GetExecutingAssembly();
            var process = Process.GetCurrentProcess();
            var procid = process.Id.ToString();
            ThreadPool.GetMinThreads(out var minworkthreads, out var miniocpthreads);
            var thid = Thread.CurrentThread.ManagedThreadId.ToString();
            string msg = string.Format("启动目录:{0}\r\n启动文件:{1}\r\n{2}\r\n.Net:{3}\r\n" +
                                       "当前进程/线程ID:{4}/{5}\r\n最小工作线程数/IO线程数:{6}/{7}",
                AppDomain.CurrentDomain.BaseDirectory,
                process.MainModule?.FileName,
                ass,
                ass.ImageRuntimeVersion,
                procid,
                thid,
                minworkthreads.ToString(),
                miniocpthreads.ToString());
            Console.WriteLine(msg);
            logger.Warn(msg);
        }

        static void DoStopLog()
        {
            logger.Warn("程序退出");
        }
    }
}