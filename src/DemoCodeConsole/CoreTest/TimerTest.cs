using System;
using System.Threading;
using Beinet.Core;

namespace DemoCodeConsole.CoreTest
{
    class TimerTest : IRunable
    {
        public void Run()
        {
            var timer1 = new System.Threading.Timer(state =>
            {
                // 验证前一次任务未完成时，新任务依旧会执行
                OutMsg("state : " + state);
                Thread.Sleep(5000);
                OutMsg("OK");
            });
            OutMsg("准备运行");
            timer1.Change(2000, 3000);
        }

        static void OutMsg(string msg)
        {
            var tid = Thread.CurrentThread.ManagedThreadId;
            var now = DateTime.Now.ToString("HH:mm:ss.fff");
            Console.WriteLine($"{now} {tid} {msg}");
        }
    }
}
