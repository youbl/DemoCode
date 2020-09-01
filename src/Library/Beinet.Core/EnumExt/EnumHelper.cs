namespace Beinet.Core.EnumExt
{
    /// <summary>
    /// 枚举相关辅助类
    /// </summary>
    public static class EnumHelper
    {
        /// <summary>
        /// 字符串转换为枚举
        /// </summary>
        /// <typeparam name="T">枚举类型</typeparam>
        /// <param name="source">字符串</param>
        /// <param name="defaultValue">转换失败时返回的默认枚举值</param>
        /// <returns>枚举</returns>
        public static T ToEnum<T>(string source, T defaultValue)
        {
            if (!string.IsNullOrEmpty(source))
            {
                try
                {
                    T value = (T)System.Enum.Parse(typeof(T), source, true);
                    if (System.Enum.IsDefined(typeof(T), value))
                    {
                        return value;
                    }
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }
    }
}