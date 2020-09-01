
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Beinet.Core.DateExt;

namespace Beinet.Core.NumberExt
{
    /// <summary>
    /// 数值相关辅助类
    /// </summary>
    public static class NumHelper
    {
        /// <summary>
        /// 生成随机数使用
        /// </summary>
        private static Random _rnd = new Random(Guid.NewGuid().GetHashCode());

        /// <summary>
        /// 返回一个随机数， minValue &lt;= 返回值 &lt; maxValue
        /// </summary>
        /// <param name="minValue">随机数最小值（包含minValue）</param>
        /// <param name="maxValue">随机数最大值（不含maxValue）</param>
        /// <returns></returns>
        public static int GetRndNum(int minValue = 0, int maxValue = int.MaxValue)
        {
            if (maxValue < minValue)
            {
                throw new ArgumentException("最小值不能比最大值还大");
            }

            lock (_rnd)
                return _rnd.Next(minValue, maxValue);
        }


        /// <summary>
        /// 返回一个随机数字字符串， minValue &lt;= 返回值 &lt; maxValue
        /// </summary>
        /// <param name="minValue">随机数最小值（包含minValue）</param>
        /// <param name="maxValue">随机数最大值（不含maxValue）</param>
        /// <param name="strLen">返回字符串长度，默认为最大值的长度</param>
        /// <returns></returns>
        public static string GetRndNumStr(int minValue = 0, int maxValue = int.MaxValue, int strLen = 0)
        {
            var ret = GetRndNum(minValue, maxValue);
            if (strLen <= 0)
            {
                strLen = maxValue.ToString().Length;
            }

            // D5表示输出最少5位，如 12.ToString("D5") 输出 00012
            return ret.ToString("D" + strLen.ToString());
        }

        /// <summary>
        /// 返回一个递增的ID：由毫秒 + n位随机数组成,
        /// ID长度：13 + rndNumLength
        /// </summary>
        /// <param name="rndNumLength">随机数长度</param>
        /// <returns></returns>
        public static long GetID(int rndNumLength)
        {
            // 得到毫秒时间戳
            var now = DateHelper.GetTimeStampMillisecond();
            var maxRnd = (int) Math.Pow(10, rndNumLength);
            int rndNum;
            lock (_rnd)
                rndNum = _rnd.Next(maxRnd);
            return now * maxRnd + rndNum;
        }

        /// <summary>
        /// 返回一个递增的ID：由毫秒 + n位随机数 + 项目代号 组成,
        /// ID长度：13 + rndNumLength + 2(项目代号长度为2）
        /// </summary>
        /// <param name="projectNum">项目代号，0~99</param>
        /// <param name="rndNumLength">随机数长度</param>
        /// <returns></returns>
        public static long GetID(int projectNum, int rndNumLength)
        {
            if (projectNum < 0 || projectNum > 99)
            {
                throw new ArgumentException("项目代号不允许大于99", nameof(projectNum));
            }

            return GetID(rndNumLength) * 100 + projectNum;
        }

        /// <summary>
        /// 返回一个递增的ID：由毫秒 + n位随机数 + 项目代号 + 分区号 组成,
        /// ID长度：19位
        /// </summary>
        /// <param name="projectNum">项目代号，0~99</param>
        /// <param name="rndNumLength">随机数长度</param>
        /// <param name="areaNum">所属分区</param>
        /// <returns></returns>
        public static long GetID(int projectNum, int rndNumLength, int areaNum)
        {
            if (projectNum < 0 || projectNum > 99)
            {
                throw new ArgumentException("项目代号不允许大于99", nameof(projectNum));
            }

            if (areaNum < 0 || areaNum > 99)
            {
                throw new ArgumentException("所属不允许大于99", nameof(areaNum));
            }

            return GetID(projectNum, rndNumLength) * 100 + areaNum;
        }


        #region 雪花算法获取批量id


        private static DateTime baseTime = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(2018, 1, 1));
        private static long LastCall = 0;

        /// <summary>
        /// 仿雪花算法，批量获取递增的随机数
        /// </summary>
        /// <param name="projectNum">项目代号，0~99</param>
        /// <param name="areaNum">所属分区</param>
        /// <param name="count">获取数量1~999个</param>
        /// <returns></returns>
        public static long[] GetRandomIDList(int projectNum, int areaNum, int count)
        {
            if (projectNum < 0 || projectNum > 99)
            {
                throw new ArgumentException("项目代号不允许大于99", nameof(projectNum));
            }

            if (areaNum < 0 || areaNum > 99)
            {
                throw new ArgumentException("所属不允许大于99", nameof(areaNum));
            }

            if (count < 1 || count > 999)
            {
                throw new ArgumentException("最多允许一次获取999个ID");
            }

            var result = new long[count];
            long timespac;
            while (true)
            {
                // 避免同一毫秒调用2次
                var now = (long) (DateTime.Now - baseTime).TotalMilliseconds;
                var oldVal = Interlocked.Exchange(ref LastCall, now);
                if (now != oldVal)
                {
                    timespac = now;
                    break;
                }

                Thread.Sleep(1);
            }

            //          序号   proj   arenum
            timespac = timespac * 1000 * 100 * 100;
            var constval = (projectNum * 100) + areaNum;
            for (int i = 0; i < count; i++)
            {
                var temp = timespac + (i * 10000) + constval;
                result[i] = temp;
            }

            return result;
        }

        /// <summary>
        /// 仿雪花算法，获取随机数
        /// </summary>
        /// <param name="projectNum">项目代号，0~99</param>
        /// <param name="areaNum">所属分区</param>
        /// <returns></returns>
        public static long GetRandomID(int projectNum, int areaNum)
        {
            return GetRandomIDList(projectNum, areaNum, 1)[0];
        }

        #endregion


        #region 质数相关


        /// <summary>
        /// 给定的值是否质数（素数）.
        /// 注：1不是质数也不是合数
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        public static bool IsPrime(int num)
        {
            if (num <= 1)
            {
                return false;
            }

            if (num <= 3)
            {
                return true;
            }

            var loop = Math.Sqrt(num);
            for (var i = 2; i <= loop; i++)
            {
                if (num % i == 0)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 找出指定区间的全部素数返回.
        /// 注1：maxValue只支持最大2亿，如果需要超过2亿，请考虑拆分2个数组，重新实现
        /// 注2：批量查找一百万以内的素数，此方法只需要5毫秒，循环调用IsPrime要200毫秒.
        /// 2亿以内有11078937个素数，本方法耗时约4秒
        /// </summary>
        /// <param name="maxValue"></param>
        /// <param name="minValue"></param>
        /// <returns></returns>
        public static List<int> GetPrimes(int maxValue = 200000000, int minValue = 2)
        {
            if (maxValue < minValue)
            {
                throw new ArgumentException("最小值不能比最大值还大");
            }

            if (maxValue > 200000000)
            {
                throw new ArgumentException("最大值不能超过2亿，避免内存溢出");
            }

            // 从0开始，所以对result的引用，都要减1
            var result = new bool[maxValue];
            result[2 - 1] = true;
            result[3 - 1] = true;

            // 标记非2的倍数为 质数
            for (var i = 5; i <= maxValue; i += 2)
            {
                result[i - 1] = true;
            }

            var sqrt = Math.Sqrt(maxValue);
            for (var i = 3; i <= sqrt; i++)
            {
                if (result[i - 1])
                {
                    // 标记当前质数的倍数为 非质数
                    for (var inner = i + i; inner <= maxValue; inner += i)
                    {
                        result[inner - 1] = false;
                    }
                }
            }

            var ret = new List<int>();
            for (var i = minValue; i <= maxValue; i++)
            {
                if (result[i - 1])
                {
                    ret.Add(i);
                }
            }

            return ret;
        }

        #endregion


        #region N进制累算法，比如用于短url的id

        // 0为最低位，小写z为最高位
        const string _arrNums = "0123456789";
        const string _arrChUpper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        const string _arrChLower = "abcdefghijklmnopqrstuvwxyz";

        // 最长支持64进制
        const string _arrChs = _arrNums + _arrChUpper + _arrChLower;
        private static Dictionary<char, int> _arrBitNum;

        static Dictionary<char, int> ArrBitNum => _arrBitNum ?? (_arrBitNum = InitArr());

        /// <summary>
        /// 返回n进制加1结果
        /// </summary>
        /// <param name="currentNum">n进制的字符串</param>
        /// <param name="n">n进制</param>
        /// <returns></returns>
        public static string Add(string currentNum, int n = 0)
        {
            long realNum = ConvertToNum(currentNum, n) + 1;
            var ret = ConvertToStr(realNum, n);
            return ret;
        }

        /// <summary>
        /// 返回n进制相加结果
        /// </summary>
        /// <param name="currentNum1">n进制的字符串</param>
        /// <param name="currentNum2">n进制的字符串</param>
        /// <param name="n">n进制</param>
        /// <returns></returns>
        public static string Add(string currentNum1, string currentNum2, int n = 0)
        {
            long realNum = ConvertToNum(currentNum1, n) + ConvertToNum(currentNum2, n);
            var ret = ConvertToStr(realNum, n);
            return ret;
        }


        /// <summary>
        /// 把n进制转换为十进制
        /// </summary>
        /// <param name="str">n进制的字符串</param>
        /// <param name="n">n进制</param>
        /// <returns></returns>
        public static long ConvertToNum(string str, int n = 0)
        {
            if (n <= 0)
                n = _arrChs.Length;
            var ret = 0L;
            if (str == null || (str = str.Trim()).Length <= 0)
                return ret;
            //1234 = 1* 62^(4-1-0) + 1* 62^(4-1-1)
            for (int i = 0, j = str.Length; i < j; i++)
            {
                if (!ArrBitNum.TryGetValue(str[i], out var num))
                    throw new ArgumentException("Contains unknown char: " + str[i]);
                if (num > 0)
                    ret += (long) (num * Math.Pow(n, j - 1 - i));
            }

            return ret;
        }

        /// <summary>
        /// 把十进制数转换为n进制
        /// </summary>
        /// <param name="num">十进制数</param>
        /// <param name="n">要转换为几进制</param>
        /// <returns></returns>
        public static string ConvertToStr(long num, int n = 0)
        {
            if (num < 0)
                throw new ArgumentException("不支持负数转换");

            if (n <= 0)
                n = _arrChs.Length;

            var ret = new StringBuilder();
            do
            {
                var perNum = (int) (num % n);
                num = num / n;
                ret.Insert(0, _arrChs[perNum]);
            } while (num > 0);

            return ret.ToString();
        }

        static Dictionary<char, int> InitArr()
        {
            var ret = new Dictionary<char, int>();
            for (int i = 0, j = _arrChs.Length; i < j; i++)
            {
                ret.Add(_arrChs[i], i);
            }

            return ret;
        }

        #endregion
    }
}
