using System;
using System.Threading;
using Beinet.Core.Reflection;
using NLog;

namespace Beinet.Core.Runs
{
    /// <summary>
    /// 查找所有可执行方法，并启动
    /// </summary>
    public static class RunableUtil
    {
        private static ILogger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// 判断并启动方法
        /// </summary>
        /// <param name="mutiThread">true表示以线程池启动，false表示串行启动</param>
        public static void RunAll(bool mutiThread = true)
        {
            var allRunableObj = Scanner.ScanInstanceByParentType(typeof(RunableBase));
            logger.Info("Find {0} runable obj.", allRunableObj.Count);
            foreach (var item in allRunableObj)
            {
                if (mutiThread)
                {
                    ThreadPool.UnsafeQueueUserWorkItem(Run, item);
                }
                else
                {
                    Run(item);
                }
            }
        }

        private static void Run(object item)
        {
            var runable = (RunableBase) item;
            try
            {
                runable.Run();
            }
            catch (Exception exp)
            {
                logger.Error(exp, "errorBy {0}", runable.GetType().FullName);
            }
        }
    }
}