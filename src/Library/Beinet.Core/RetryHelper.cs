using System;
using System.Threading;

namespace Beinet.Core
{
    /// <summary>
    /// 重试辅助类
    /// </summary>
    public static class RetryHelper
    {
        /// <summary>
        /// 执行method方法，出错时进行重试
        /// </summary>
        /// <param name="retryTime">最多执行次数</param>
        /// <param name="method">要执行的方法</param>
        /// <param name="retryWaitMillisecond">重试前等待的毫秒数，0为不等待</param>
        public static void Retry(Action method, int retryTime = 1, int retryWaitMillisecond = 100)
        {
            try
            {
                method();
            }
            catch
            {
                retryTime--;
                if (retryTime <= 0)
                    throw;
                if (retryWaitMillisecond > 0)
                    Thread.Sleep(retryWaitMillisecond);
                Retry(method, retryTime, retryWaitMillisecond);
            }
        }

    }
}