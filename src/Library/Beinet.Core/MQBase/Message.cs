using System;
using System.Threading;

namespace Beinet.Core.MQBase
{
    /// <summary>
    /// 消息对象
    /// </summary>
    public class Message
    {
        private static long globalSn = 0;
        /// <summary>
        /// 消息顺序号,仅保存在内存中，重启后重新从1开始
        /// </summary>
        public long Sn { get; }

        /// <summary>
        /// 事件发生时间
        /// </summary>
        public DateTime PublicTime { get; }
        /// <summary>
        /// 事件数据对象
        /// </summary>
        public object Data { get; set; }
        /// <summary>
        /// 构造函数，初始化序号和时间
        /// </summary>
        public Message()
        {
            Sn = Interlocked.Increment(ref globalSn);
            PublicTime = DateTime.Now;
        }
    }
}
