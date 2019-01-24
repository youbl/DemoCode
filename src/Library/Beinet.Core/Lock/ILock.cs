using System;
using System.Threading.Tasks;

namespace Beinet.Core.Lock
{
    /// <summary>
    /// 
    /// </summary>
    public interface ILock : IDisposable
    {
        /// <summary>
        /// 加锁后执行action方法
        /// </summary>
        /// <param name="action">无参方法</param>
        /// <param name="lockWaitMs">加锁前要等待的毫秒数</param>
        Task DoAction(Action action, int lockWaitMs = -1);

        /// <summary>
        /// 加锁后执行action方法
        /// </summary>
        /// <param name="action">有参数方法</param>
        /// <param name="para">参数</param>
        /// <param name="lockWaitMs">加锁前要等待的毫秒数</param>
        Task DoAction(Action<object> action, object para, int lockWaitMs = -1);
    }
}
