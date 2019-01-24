using System.Threading;

namespace Beinet.Core.MQBase
{
    /// <summary>
    /// 生产者接口
    /// </summary>
    public interface IMqProducer
    {
        /// <summary>
        /// 发布消息
        /// </summary>
        /// <param name="msgs"></param>
        /// <returns></returns>
        void Publish(params object[] msgs);

        /// <summary>
        /// 监听消息，并进行投递
        /// </summary>
        /// <returns></returns>
        void WaitForDelivery();

        /// <summary>
        /// 注册所有消息的消费者
        /// </summary>
        /// <param name="consumer"></param>
        void Register(IMqConsumer consumer);

        /// <summary>
        /// 注册指定消息类型的消费者
        /// </summary>
        /// <param name="consumer"></param>
        void Register<T>(IMqConsumer<T> consumer);
    }
}
