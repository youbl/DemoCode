using System;
using System.ComponentModel;
using System.Reflection;

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
                    T value = (T) System.Enum.Parse(typeof(T), source, true);
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

        /// <summary>
        /// 获取枚举的字符串描述.
        /// 未添加Description注解时，返回ToString
        /// </summary>
        /// <param name="enumVal"></param>
        /// <returns></returns>
        public static string GetDesc(this Enum enumVal)
        {
            var ret = enumVal.ToString();

            var type = enumVal.GetType();
            var memInfo = type.GetMember(ret);
            if (memInfo.Length > 0)
            {
                object[] attrs = memInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);
                if (attrs.Length > 0)
                {
                    var tmp = ((DescriptionAttribute) attrs[0]).Description;
                    if (!string.IsNullOrEmpty(tmp))
                    {
                        ret = tmp;
                    }
                }
            }

            return ret;
        }
    }
}