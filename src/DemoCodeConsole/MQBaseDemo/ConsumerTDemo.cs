using System;
using System.Threading;
using Beinet.Core.MQBase;

namespace DemoCodeConsole.Demo
{
    /// <summary>
    /// DataDemo这种消息的消费者实现类示例
    /// </summary>
    public class ConsumerTDemo : IMqConsumer<DataDemo>
    {
        public void Process(Message msg)
        {
            var data = (DataDemo) msg.Data;
            Console.WriteLine($"{Thread.CurrentThread.ManagedThreadId}:ConsumerTDemo收到消息:{msg.PublicTime.ToString("yyyy-MM-dd HH:mm:ss.fff")}\r\n  {data.Xxx}\r\n");
        }
    }

    public class DataDemo
    {
        public string Xxx { get; set; }
    }
}
