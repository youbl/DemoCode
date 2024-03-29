﻿using System;
using System.Threading;
using Beinet.Core;
using Beinet.Core.MQBase;
using DemoCodeConsole.Demo;

namespace DemoCodeConsole.MQBaseDemo
{
    /// <summary>
    /// 所有消息的消费者实现类示例
    /// </summary>
    public class MQRun : IRunable
    {
        public void Run()
        {
            // 从当前组件中查找消费者进行注册
            Publisher.StartAllConsumer();

            Console.WriteLine($"\r\n主线程ID:{Thread.CurrentThread.ManagedThreadId}\r\n");

            // 测试发消息
            var data = new DataDemo { Xxx = "haha" };
            Publisher.Publish(data, DateTime.Now);

            // // 用线程发3条消息
            // new Thread(() => {
            //     Console.WriteLine($"\r\nddd1线程ID:{Thread.CurrentThread.ManagedThreadId}\r\n");
            //     Publisher.Publish("ddd1");
            // }).Start();
            new Thread(() => {
                Console.WriteLine($"\r\nddd2线程ID:{Thread.CurrentThread.ManagedThreadId}\r\n");
                Publisher.Publish("ddd2", data);
            }).Start();
            // new Thread(() => {
            //     Thread.Sleep(2000);
            //     Console.WriteLine($"\r\nddd3线程ID:{Thread.CurrentThread.ManagedThreadId}\r\n");
            //     Publisher.Publish("ddd3");
            // }).Start();

            // 等10秒释放生产者
            new Thread(() => {
                Thread.Sleep(10000);
                Publisher.Dispose();
            }).Start();
        }
    }
}
