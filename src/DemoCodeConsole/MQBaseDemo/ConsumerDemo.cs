using System;
using System.Threading;
using Beinet.Core.MQBase;

namespace DemoCodeConsole.MQBaseDemo
{
    /// <summary>
    /// 所有消息的消费者实现类示例
    /// </summary>
    public class ConsumerDemo : IMqConsumer
    {
        public void Process(Message msg)
        {
            Console.WriteLine($"{Thread.CurrentThread.ManagedThreadId}:ConsumerDemo收到消息:{msg.PublicTime.ToString("yyyy-MM-dd HH:mm:ss.fff")}\r\n  {msg.Data}\r\n");
        }
    }
}
