using System;
using System.Collections.Generic;
using System.Reflection;
using Beinet.Core.Cron;
using Beinet.Core.Reflection;

namespace Beinet.Core.MQBase
{
    /// <summary>
    /// 静态类，方便消息发送调用
    /// </summary>
    public static class Publisher
    {
        private static Producer _producer = Producer.DEFAULT;

        /// <summary>
        /// 扫描并启动所有库里的消费者
        /// </summary>
        public static void StartAllConsumer()
        {
            var arrAssembly = TypeHelper.Assemblys;
            foreach (var assembly in arrAssembly.Values)
                _producer.RegisterAssembly(assembly);
        }

        /// <summary>
        /// 发送事件消息
        /// </summary>
        /// <param name="msgs"></param>
        public static void Publish(params object[] msgs)
        {
            _producer.Publish(msgs);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public static void Dispose()
        {
            _producer.Dispose();
        }
    }
}