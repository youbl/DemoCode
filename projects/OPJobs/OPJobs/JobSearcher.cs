
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Threading;
using Beinet.Core;
using NLog;

namespace OPJobs
{
    /// <summary>
    /// 查找并启动任务
    /// </summary>
    static class JobSearcher
    {
        private static ILogger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// 执行指定的job，如果参数为空，从app.config里读取配置
        /// </summary>
        /// <param name="args"></param>
        public static void Start(string[] args)
        {
            if (args == null || args.Length <= 0)
            {
                var splitChar = new char[] { ',', ';', ' ' };
                args = (ConfigurationManager.AppSettings["RunClassName"] ?? "").Split(splitChar,
                    StringSplitOptions.RemoveEmptyEntries);
                if (args.Length <= 0)
                {
                    logger.Error("未指定要运行的任务");
                    return;
                }
            }

            var jobs = GetIJobClass(args);
            if (jobs.Count != args.Length)
            {
                logger.Error($"指定{args.Length}个任务, 找到{jobs.Count}个: {args.Aggregate("", (seed, item) => seed + item + ",")}");
                return;
            }

            RunMethod(jobs);
        }

        private static List<ThreadStart> GetIJobClass(string[] args)
        {
            var ret = new List<ThreadStart>();

            var ijobType = typeof(IRunable);
            var assembly = Assembly.GetExecutingAssembly();
            foreach (var type in assembly.GetTypes())
            {
                if (ijobType.IsAssignableFrom(type) && IsRunClass(type, args) &&
                    assembly.CreateInstance(type.FullName ?? "") is IRunable instance)
                {
                    ret.Add(instance.Run);
                }
            }

            return ret;
        }

        /// <summary>
        /// args里是否包含type的名称
        /// </summary>
        /// <param name="type"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private static bool IsRunClass(Type type, string[] args)
        {
            var name = type.FullName ?? "";
            foreach (var runClassName in args)
            {
                if (!string.IsNullOrWhiteSpace(runClassName) &&
                    (
                        name.Equals(runClassName, StringComparison.OrdinalIgnoreCase) ||
                        name.EndsWith("." + runClassName, StringComparison.OrdinalIgnoreCase)
                    ))
                {
                    return true;
                }
            }
            return false;
        }


        private static void RunMethod(List<ThreadStart> methods)
        {
            var threads = new Thread[methods.Count];
            int idx = 0;
            foreach (ThreadStart method in methods)
            {
                var thread = new Thread(method) { IsBackground = true };
                thread.Start();
                threads[idx] = thread;
                idx++;
            }
            logger.Info("启动任务个数：" + idx.ToString());

            // 堵塞等待所有任务完成
            foreach (var thread in threads)
            {
                thread.Join();
            }
            logger.Info("任务全部线程结束");
        }
    }
}
