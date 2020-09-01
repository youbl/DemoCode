

using System;
using System.Collections.Generic;
using System.Text;
using Beinet.Core.NumberExt;

namespace Beinet.Core.StringExt
{
    /// <summary>
    /// 字符串操作辅助类
    /// </summary>
    public static class StringHelper
    {
        /// <summary>
        /// 随机字符串取值范围类型
        /// </summary>
        public enum RndType
        {
            /// <summary>
            /// 仅数字
            /// </summary>
            Num = 0,
            /// <summary>
            /// 仅小写字母
            /// </summary>
            LowChar,
            /// <summary>
            /// 仅大写字母
            /// </summary>
            UpChar,
            /// <summary>
            /// 小写字母与数字
            /// </summary>
            LowAndNum,
            /// <summary>
            /// 大写字母与数字
            /// </summary>
            UpAndNum,
            /// <summary>
            /// 大写字母、小写字母与数字
            /// </summary>
            CharAndNum
        }


        /// <summary>
        /// 返回指定长度的随机字符串
        /// </summary>
        /// <param name="length">返回字符串长度</param>
        /// <param name="rndType">字符取值范围</param>
        /// <returns></returns>
        public static string GetRndStr(int length, RndType rndType = RndType.CharAndNum)
        {
            string charScope;
            switch (rndType)
            {
                case RndType.Num:
                    charScope = "0123456789";
                    break;
                case RndType.LowChar:
                    charScope = "abcdefghijklmnopqrstuvwxyz";
                    break;
                case RndType.UpChar:
                    charScope = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                    break;
                case RndType.LowAndNum:
                    // 不含字母i、l、o，避免和数字混淆
                    charScope = "abcdefghjkmnpqrstuvwxyz0123456789";
                    break;
                case RndType.UpAndNum:
                    // 不含字母i、l、o，避免和数字混淆
                    charScope = "ABCDEFGHJKMNPQRSTUVWXYZ0123456789";
                    break;
                case RndType.CharAndNum:
                    // 不含字母i、l、o和数字01，避免字母和数字混淆
                    charScope = "ABCDEFGHJKMNPQRSTUVWXYZ23456789abcdefghjkmnpqrstuvwxyz";
                    break;
                default:
                    throw new ArgumentException("取值范围参数有误", nameof(rndType));
            }
            return GetRndStr(length, charScope);
        }

        /// <summary>
        /// 返回指定长度的随机字符串
        /// </summary>
        /// <param name="length">返回字符串长度</param>
        /// <param name="charScope">字符取值范围</param>
        /// <returns></returns>
        public static string GetRndStr(int length, string charScope = null)
        {
            if (string.IsNullOrEmpty(charScope))
            {
                // 不含字母i、l、o，避免和数字混淆
                charScope = "abcdefghjkmnpqrstuvwxyz0123456789";
            }
            var maxrnd = charScope.Length;
            var ret = new StringBuilder(length);
            for (var i = 0; i < length; i++)
            {
                var idx = NumHelper.GetRndNum(0, maxrnd);
                ret.Append(charScope[idx]);
            }
            return ret.ToString();
        }

        /// <summary>
        /// 返回指定字符串的Ascii码值，
        /// 如 a 返回 97， aa返回9797，AaAa返回65976597
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static long GetAsciiCode(string str)
        {
            var len = str.Length;
            if (len == 0)
                return 0;
            var ret = (int)str[len - 1];
            var mul = 100;
            for (var i = len - 2; i >= 0; i--)
            {
                ret += str[i] * mul;
                mul *= 100;
            }
            return ret;
        }


        /// <summary>
        /// 基于Ascii码的排序方案
        /// 如： a,b,c,A,B,C，排序结果是A,B,C,a,b,c
        /// 调用方式：arr.OrderBy(item => item, AsciiComparer.Default);
        /// </summary>
        public sealed class AsciiComparer : IComparer<string>
        {
            private AsciiComparer() { }
            /// <summary>
            /// 默认单例
            /// </summary>
            public static AsciiComparer Default { get; } = new AsciiComparer();
            /// <summary>
            /// 对比2个字符串
            /// </summary>
            /// <param name="x"></param>
            /// <param name="y"></param>
            /// <returns></returns>
            public int Compare(string x, string y)
            {
                if (x == null && y == null)
                {
                    return 0;
                }
                if (x == null)
                {
                    return -1;
                }
                if (y == null)
                {
                    return 1;
                }
                int length = Math.Min(x.Length, y.Length);
                for (int i = 0; i < length; ++i)
                {
                    if (x[i] == y[i]) continue;
                    // if (x[i] == '-') return 1;
                    // if (y[i] == '-') return -1;
                    return x[i].CompareTo(y[i]);
                }

                return x.Length - y.Length;
            }
        }
    }
}
