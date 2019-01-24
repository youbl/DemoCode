using System.Threading;

namespace Beinet.Core.MQBase
{
    /// <summary>
    /// 消费者接口
    /// </summary>
    public interface IMqConsumer
    {
        /// <summary>
        /// 消费消息
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        void Process(Message msg);
    }

    /// <summary>
    /// 指定类型消息的消费者接口
    /// </summary>
    // ReSharper disable once UnusedTypeParameter
    public interface IMqConsumer<T> : IMqConsumer
    {
        
    }
}
