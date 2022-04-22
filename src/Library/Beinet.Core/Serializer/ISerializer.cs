
namespace Beinet.Core.Serializer
{
    /// <summary>
    /// 序列化与反序列化接口
    /// </summary>
    public interface ISerializer
    {
        /// <summary>
        /// 将一个对象序列化成字符串
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <param name="camel">是否驼峰</param>
        /// <returns></returns>
        string SerializToStr<T>(T data, bool camel = false);

        /// <summary>
        /// 将一个对象序列化成字节数组
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        byte[] SerializToBytes<T>(T data);

        /// <summary>
        /// 将字符串反序列化为对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="str"></param>
        /// <returns></returns>
        T DeSerializFromStr<T>(string str);

        /// <summary>
        /// 将字节数组反序列化成对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        T DeSerializFromBytes<T>(byte[] data);
    }
}