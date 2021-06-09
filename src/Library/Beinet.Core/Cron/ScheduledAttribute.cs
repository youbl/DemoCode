using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Beinet.Core.Cron
{
    /// <summary>
    /// 计划任务属性，只能在方法上标记，暂不支持LWC?之类的字母
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public class ScheduledAttribute : Attribute
    {
        private string _cron;

        /// <summary>
        /// 字符串表达式定义
        /// </summary>
        public string Cron
        {
            get => _cron;
            private set
            {
                ParseCron(value);
                _cron = value;
            }
        }

        /// <summary>
        /// 是否允许启动
        /// </summary>
        public bool Run { get; set; } = true;

        /// <summary>
        /// 是否记录启动结束日志
        /// </summary>
        public bool StartLog { get; set; } = false;

        /// <summary>
        /// 可执行的秒集合
        /// </summary>
        public HashSet<int> Seconds { get; private set; }

        /// <summary>
        /// 可执行的分钟集合
        /// </summary>
        public HashSet<int> Minutes { get; private set; }

        /// <summary>
        /// 可执行的小时集合
        /// </summary>
        public HashSet<int> Hours { get; private set; }

        /// <summary>
        /// 可执行的每月几号集合
        /// </summary>
        public HashSet<int> Days { get; private set; }

        /// <summary>
        /// 可执行的月集合
        /// </summary>
        public HashSet<int> Months { get; private set; }

        /// <summary>
        /// 可执行的周几集合
        /// </summary>
        public HashSet<int> Weeks { get; private set; }

        /// <summary>
        /// 可执行的年份集合
        /// </summary>
        public HashSet<int> Years { get; private set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="cron">字符串表达式定义</param>
        public ScheduledAttribute(string cron)
        {
            Cron = cron;
        }

        /// <summary>
        /// 指定的时间，是否是要执行任务的时间
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public bool IsRunTime(DateTime time)
        {
            if (!Run)
            {
                return false;
            }

            if (Years != null && Years.Count > 0 && !Years.Contains(time.Year))
            {
                return false;
            }

            return Seconds.Contains(time.Second) &&
                   Minutes.Contains(time.Minute) &&
                   Hours.Contains(time.Hour) &&
                   Days.Contains(time.Day) &&
                   Months.Contains(time.Month) &&
                   Weeks.Contains((int) time.DayOfWeek);
        }

        #region 私有方法集

        /// <summary>
        /// 解析表达式
        /// </summary>
        /// <param name="cron"></param>
        /// <returns></returns>
        private void ParseCron(string cron)
        {
            if (cron == null || (cron = cron.Trim()).Length <= 0)
            {
                throw new ArgumentException("cron can't be empty.");
            }

            var arr = cron.Split(new char[] {' '}, StringSplitOptions.RemoveEmptyEntries);
            if (arr.Length != 6 && arr.Length != 7)
            {
                throw new ArgumentException($"cron must have 6 or 7 items: {cron}");
            }

            Seconds = ParseSecond(arr[0]);
            Minutes = ParseMinute(arr[1]);
            Hours = ParseHour(arr[2]);
            Days = ParseDay(arr[3]);
            Months = ParseMonth(arr[4]);
            Weeks = ParseWeek(arr[5]);
            if (arr.Length > 6)
            {
                Years = ParseYear(arr[6]);
            }
        }

        private static HashSet<int> ParseSecond(string itemStr)
        {
            return ParseValues(itemStr, 0, 59);
        }

        private static HashSet<int> ParseMinute(string itemStr)
        {
            return ParseValues(itemStr, 0, 59);
        }

        private static HashSet<int> ParseHour(string itemStr)
        {
            return ParseValues(itemStr, 0, 23);
        }

        private static HashSet<int> ParseDay(string itemStr)
        {
            return ParseValues(itemStr, 1, 31);
        }

        private static HashSet<int> ParseMonth(string itemStr)
        {
            return ParseValues(itemStr, 1, 12);
        }

        private static HashSet<int> ParseWeek(string itemStr)
        {
            var ret = ParseValues(itemStr, 0, 7);
            // 把星期7转换为星期0，即星期天
            if (ret.Contains(7))
            {
                ret.Remove(7);
                ret.Add(0);
            }

            return ret;
        }

        private static HashSet<int> ParseYear(string itemStr)
        {
            return ParseValues(itemStr, 1970, 2099);
        }


        /*
字段　　允许值　　允许的特殊字符 
秒     　 0-59 　　　　, - * / 
分     　 0-59　　　　 , - * / 
小时      0-23 　　　　, - * / 
日期      1-31 　　　　, - * ? / L W C 
月份      1-12 　　　　, - * / 
星期      1-7 　　　　  , - * ? / L C # 
年     1970-2099 　　, - * / 非必填
*/
        // 秒、分、时、月、年的正则
        static readonly Regex _regexSMHmY = new Regex(@"^[,\-*/\d]+$");

        // */6 这种间隔表达式
        static readonly Regex _regexDivided = new Regex(@"^\*(/\d+)?$");

        // 3-30/5 这种区间表达式
        static readonly Regex _regexRange = new Regex(@"^\d+\-\d+(/\d+)?$");

        /// <summary>
        /// 解析秒、分、时、月、年
        /// </summary>
        /// <param name="itemStr">cron项</param>
        /// <param name="minVal">数字最小范围</param>
        /// <param name="maxVal">数字最大范围</param>
        /// <returns></returns>
        private static HashSet<int> ParseValues(string itemStr, int minVal, int maxVal)
        {
            if (!_regexSMHmY.IsMatch(itemStr))
            {
                throw new ArgumentException($"cron format error: {itemStr}");
            }

            var ret = new HashSet<int>();
            foreach (var item in itemStr.Split(','))
            {
                if (item.Length == 0)
                {
                    throw new ArgumentException($"cron format error: {itemStr}, can't have two comma.");
                }

                if (int.TryParse(item, out var itemNum))
                {
                    ret.Add(itemNum);
                    continue;
                }

                if (_regexDivided.IsMatch(item))
                {
                    AddDividedVal(item, minVal, maxVal, ret);
                    continue;
                }

                if (_regexRange.IsMatch(item))
                {
                    AddRangedVal(item, minVal, maxVal, ret);
                    continue;
                }

                throw new ArgumentException($"cron format error: {itemStr}, unrecognized item: {item}");
            }

            return ret;
        }

        // 处理 * 或 */6 的方法
        private static void AddDividedVal(string item, int minVal, int maxVal, HashSet<int> arr)
        {
            int dividedNum = 0;
            if (item.IndexOf('/') >= 0)
            {
                dividedNum = int.Parse(item.Substring(item.IndexOf('/') + 1));
            }

            for (var i = minVal; i <= maxVal; i++)
            {
                if (dividedNum == 0 || i % dividedNum == 0)
                {
                    arr.Add(i);
                }
            }
        }

        // 处理 3-30 或 3-30/5 的方法
        private static void AddRangedVal(string item, int minVal, int maxVal, HashSet<int> arr)
        {
            var arrNum = item.Split('-');
            var startNum = int.Parse(arrNum[0]); // 区间起始值

            var endArr = arrNum[1].Split('/');
            var endNum = int.Parse(endArr[0]); // 区间结束值

            if (startNum < minVal || endNum > maxVal)
            {
                throw new ArgumentException(
                    $"cron format error: {item}, num must between {minVal.ToString()} and {maxVal.ToString()}");
            }

            var dividedNum = endArr.Length > 1 ? int.Parse(endArr[1]) : 0;
            for (var i = startNum; i <= endNum; i++)
            {
                if (dividedNum == 0 || (i - startNum) % dividedNum == 0)
                {
                    arr.Add(i);
                }
            }
        }

        #endregion
    }
}