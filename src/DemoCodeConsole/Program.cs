using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Beinet.Core;
using Beinet.Core.Reflection;
using Beinet.Core.Util;
using DemoCodeConsole.WindowsDemo;
using NLog;

namespace DemoCodeConsole
{
    class Program
    {
        private static ILogger logger = LogManager.GetCurrentClassLogger();

        private static List<IRunable> runners;
        /// <summary>
        /// 从配置中读取要运行的类列表
        /// </summary>
        private static List<IRunable> Runners
        {
            get
            {
                if (runners == null)
                {
                    runners = new List<IRunable>();
                }
                var config = ConfigHelper.GetSetting("RunClasses");
                if (string.IsNullOrEmpty(config))
                {
                    return runners;
                }
                var arr = config.Split(new char[] { ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string item in arr)
                {
                    var obj = TypeHelper.CreateInstance(item);
                    if (obj != null && obj is IRunable runner)
                    {
                        runners.Add(runner);
                    }
                }
                return runners;
            }
        }

        static void Main()
        {
            WindowsStateCheck.OutputWindowsLockedStatus();
            Console.Read();

            // 全局异常捕获
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            DoLog("Start");
            Parallel.ForEach(Runners, runner => runner.Run());
            DoLog("End");
            Console.Read();
        }

        /// <summary>
        /// 程序启动日志
        /// </summary>
        static void DoLog(string type)
        {
            var ass = Assembly.GetExecutingAssembly();
            var process = Process.GetCurrentProcess();
            var procid = process.Id.ToString();
            ThreadPool.GetMinThreads(out var minworkthreads, out var miniocpthreads);
            ThreadPool.GetMaxThreads(out var maxworkthreads, out var maxiocpthreads);
            ThreadPool.GetAvailableThreads(out var aviworkthreads, out var aviiocpthreads);
            var thid = Thread.CurrentThread.ManagedThreadId.ToString();
            string msg = $@"{type}
启动目录:{AppDomain.CurrentDomain.BaseDirectory}
启动文件:{process.MainModule?.FileName}
{ass}
.Net:{ass.ImageRuntimeVersion}
当前进程/线程ID:{procid}/{thid}
最小工作线程数/IO线程数:{minworkthreads.ToString()}/{miniocpthreads.ToString()}
最大工作线程数/IO线程数:{maxworkthreads.ToString()}/{maxiocpthreads.ToString()}
可用工作线程数/IO线程数:{aviworkthreads.ToString()}/{aviiocpthreads.ToString()}";
            Console.WriteLine(msg);
            logger.Info(msg);

        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            object exp = e == null ? "" : e.ExceptionObject;
            logger.Error("CurrentDomain_UnhandledExp\r\n{0}", exp);
        }
    }
}
