using System;
using System.Threading;
using System.Threading.Tasks;

namespace Beinet.Core.Lock
{
    /// <summary>
    /// 默认加锁实现，使用信号量.
    /// 注：不支持重复锁
    /// </summary>
    public class LockDefault : ILock
    {
        /// <summary>
        /// 信号量对象，初始化只允许一个线程进入，最大一个线程
        /// </summary>
        private SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

        /// <summary>
        /// 加锁后执行action方法
        /// </summary>
        /// <param name="action">无参方法</param>
        /// <param name="lockWaitMs">加锁前要等待的毫秒数</param>
        public async Task DoAction(Action action, int lockWaitMs = -1)
        {
            // 没有信号量时，这里会阻塞住，永久等待
            await semaphore.WaitAsync(lockWaitMs);
            try
            {
                await Task.Factory.StartNew(action).ConfigureAwait(false);
            }
            finally
            {
                // 退出代码时，释放信号量，让下一个线程可以进入了
                semaphore.Release();
            }
        }

        /// <summary>
        /// 加锁后执行action方法
        /// </summary>
        /// <param name="action">有参数方法</param>
        /// <param name="para">参数</param>
        /// <param name="lockWaitMs">加锁前要等待的毫秒数</param>
        public async Task DoAction(Action<object> action, object para, int lockWaitMs = -1)
        {
            // 没有信号量时，这里会阻塞住，永久等待
            await semaphore.WaitAsync(lockWaitMs);
            try
            {
                await Task.Factory.StartNew(action, para).ConfigureAwait(false);
            }
            finally
            {
                // 退出代码时，释放信号量，让下一个线程可以进入了
                semaphore.Release();
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            semaphore.Dispose();
        }
    }
}
