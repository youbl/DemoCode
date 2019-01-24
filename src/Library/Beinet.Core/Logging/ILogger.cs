namespace Beinet.Core.Logging
{
    /// <summary>
    /// 日志记录接口
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// 日志记录类名
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 记录 Debug 信息
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        void Debug(string format, params object[] args);

        /// <summary>
        /// 记录 Info 信息
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        void Info(string format, params object[] args);

        /// <summary>
        /// 记录 Warn 信息
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        void Warn(string format, params object[] args);

        /// <summary>
        /// 记录 Error 信息
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        void Error(string format, params object[] args);

        /// <summary>
        /// 将日志写入自定义位置(pathname)
        /// </summary>
        /// <param name="pathname">消息的前缀或者前级路径</param>
        /// <param name="format">消息</param>
        /// <param name="args">消息</param>
        void Custom(string pathname, string format, params object[] args);
    }
}
