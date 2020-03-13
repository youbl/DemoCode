using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DemoCodeConsole.ThreadDemo
{
    public class SemaphoreTest
    {
        /// <summary>
        /// 第一个参数表示初始释放几个信号量，比如设置为2，相当于设置为0，再执行2次：_sem.Release();
        /// 第二个参数表示最多允许可用的信号量，比如设置为7，如果连续8次Release会抛异常（中间如果有Wait就没事）。
        /// 另：Semaphore与SemaphoreSlim区别：SemaphoreSlim不支持跨进程，适用于几乎无等待的场景
        /// </summary>
        public SemaphoreSlim _sem = new SemaphoreSlim(2, 7);

        public ConcurrentQueue<object> _queue = new ConcurrentQueue<object>();

        public void Test()
        {
            ThreadPool.UnsafeQueueUserWorkItem(Start, null);
            Thread.Sleep(10);

            ThreadPool.UnsafeQueueUserWorkItem(state =>
            {
                for (var i = 1; i <= 100; i++)
                {
                    Add(i);
                    if (i % 10 == 0)
                        Thread.Sleep(1000);
                }
            }, null);

        }


        public int Add(object obj)
        {
            if (obj == null)
                return _queue.Count;

            _queue.Enqueue(obj);
            _sem.Release();
            return _queue.Count;
        }

        public void Start(object obj)
        {
            while (true)
            {
                _sem.Wait();
                _queue.TryDequeue(out var aa);

                Console.WriteLine(DateTime.Now.ToString("HH:mm:ss.fffffff") + " " + aa);
            }
        }
    }
}