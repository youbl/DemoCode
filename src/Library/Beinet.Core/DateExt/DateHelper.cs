
using System;

namespace Beinet.Core.DateExt
{
    /// <summary>
    /// 日期时间相关辅助方法
    /// </summary>
    public static class DateHelper
    {
        #region 时间戳相关函数

        /// <summary>
        /// 计算机当前时区的 UNIX 时间起始值
        /// </summary>
        public static readonly DateTime BaseTime = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));

        /// <summary>   
        /// 将unix timestamp时间戳(毫秒数) 转换为.NET的DateTime   
        /// </summary>   
        /// <param name="millisecondTimeStamp">毫秒数</param>   
        /// <returns>转换后的时间</returns>   
        public static DateTime FromUnixTime(long millisecondTimeStamp)
        {
            return BaseTime.AddMilliseconds(millisecondTimeStamp);
        }

        /// <summary>   
        /// 将unix timestamp时间戳(秒数) 转换为.NET的DateTime   
        /// </summary>   
        /// <param name="secondTimeStamp">秒数</param>   
        /// <returns>转换后的时间</returns>   
        public static DateTime FromUnixTime(int secondTimeStamp)
        {
            var ret = BaseTime;
            return AddTime(secondTimeStamp, ret);
        }

        /// <summary>   
        /// 将.NET的DateTime转换为unix timestamp时间戳，单位由参数2决定, 默认返回秒
        /// </summary>   
        /// <param name="dateTime">待转换的时间</param>   
        /// <param name="isReturnMillisecond">返回值是毫秒还是秒</param>   
        /// <returns>转换后的unix time</returns>   
        public static long GetTimeStamp(DateTime dateTime = default(DateTime), bool isReturnMillisecond = false)
        {
            if (dateTime == default(DateTime))
            {
                dateTime = DateTime.Now;
            }

            TimeSpan ret = (dateTime - BaseTime);
            if (isReturnMillisecond)
                return (long)ret.TotalMilliseconds;
            else
                return (long)ret.TotalSeconds;
        }

        /// <summary>   
        /// 将.NET的DateTime转换为unix timestamp时间戳，单位由参数2决定, 默认返回秒
        /// </summary>   
        /// <param name="dateTime">待转换的时间</param>   
        /// <returns>转换后的unix time</returns>   
        public static int GetTimeStampSecond(DateTime dateTime = default(DateTime))
        {
            return (int)GetTimeStamp(dateTime);
        }

        /// <summary>   
        /// 将.NET的DateTime转换为unix timestamp时间戳，单位由参数2决定, 默认返回秒
        /// </summary>   
        /// <param name="dateTime">待转换的时间</param>   
        /// <returns>转换后的unix time</returns>   
        public static long GetTimeStampMillisecond(DateTime dateTime = default(DateTime))
        {
            return GetTimeStamp(dateTime, true);
        }

        /// <summary>
        /// 转换成时间戳
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static long ToTimeStamp(this DateTime dateTime)
        {
            return GetTimeStamp(dateTime);
        }
        /// <summary>
        /// 转换成时间戳
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static long ToTimeStampMillisecond(this DateTime dateTime)
        {
            return GetTimeStamp(dateTime, true);
        }
        #endregion


        /// <summary>
        /// 计算2个时间段的交集分钟数
        /// </summary>
        /// <param name="dtBegin"></param>
        /// <param name="dtEnd"></param>
        /// <param name="dtCompareBegin"></param>
        /// <param name="dtCompareEnd"></param>
        /// <returns></returns>
        public static double DateDiffMinutes(DateTime dtBegin, DateTime dtEnd, DateTime dtCompareBegin, DateTime dtCompareEnd)
        {
            if (dtEnd.CompareTo(dtCompareBegin) < 0 || dtBegin.CompareTo(dtCompareEnd) > 0)
                return 0;

            if (dtBegin.CompareTo(dtCompareBegin) < 0)
                dtBegin = dtCompareBegin;

            if (dtEnd.CompareTo(dtCompareEnd) > 0)
                dtEnd = dtCompareEnd;

            return dtEnd.Subtract(dtBegin).TotalMinutes;
        }

        /// <summary>
        /// 返回指定月份第一天
        /// </summary>
        /// <param name="Year"></param>
        /// <param name="Month"></param>
        /// <returns></returns>
        public static DateTime GetFirstDayOfMonth(int Year, int Month)
        {
            return Convert.ToDateTime(Year.ToString() + "-" + Month.ToString() + "-01");
        }

        /// <summary>
        /// 返回指定月份最后一天
        /// </summary>
        /// <param name="Year"></param>
        /// <param name="Month"></param>
        /// <returns></returns>
        public static DateTime GetLastDayOfMonth(int Year, int Month)
        {
            //这里的关键就是 DateTime.DaysInMonth 获得一个月中的天数
            int Days = DateTime.DaysInMonth(Year, Month);
            return Convert.ToDateTime(Year.ToString() + "-" + Month.ToString() + "-" + Days.ToString());

        }


        /// <summary>
        /// 返回指定日期所在周的星期一
        /// </summary>
        /// <param name="day">任意一天</param>
        /// <returns></returns>
        public static DateTime GetMonday(this DateTime day)
        {
            int i = day.DayOfWeek - DayOfWeek.Monday;
            // i值 > = 0 ，因为枚举原因，Sunday排在最前，此时Sunday-Monday=-1，必须+7=6。
            if (i == -1) i = 6;
            TimeSpan ts = new TimeSpan(i, 0, 0, 0);
            return day.Subtract(ts);
        }

        /// <summary>
        /// 返回指定月份最后一天
        /// </summary>
        /// <param name="day">任意一天</param>
        /// <returns></returns>
        public static DateTime GetSunday(this DateTime day)
        {
            int i = day.DayOfWeek - DayOfWeek.Sunday;
            if (i != 0) i = 7 - i;// 因为枚举原因，Sunday排在最前，相减间隔要被7减。   
            TimeSpan ts = new TimeSpan(i, 0, 0, 0);
            return AddTime(ts, day);
        }


        /// <summary>
        /// 添加指定的秒数时间，并返回增加后的值
        /// </summary>
        /// <param name="seconds"></param>
        /// <param name="addBefore"></param>
        /// <returns></returns>
        public static DateTime AddTime(int seconds, DateTime addBefore = default(DateTime))
        {
            var ret = DateTime.MaxValue;
            if (addBefore == default(DateTime))
            {
                addBefore = DateTime.Now;
            }
            if ((ret - addBefore).TotalSeconds <= seconds)
            {
                return ret;
            }
            return addBefore.AddSeconds(seconds);
        }

        /// <summary>
        /// 添加指定的时间，并返回增加后的值
        /// </summary>
        /// <param name="ts"></param>
        /// <param name="addBefore"></param>
        /// <returns></returns>
        public static DateTime AddTime(TimeSpan ts, DateTime addBefore = default(DateTime))
        {
            var ret = DateTime.MaxValue;
            if (ts == TimeSpan.MaxValue)
            {
                return ret;
            }
            if (addBefore == default(DateTime))
            {
                addBefore = DateTime.Now;
            }
            // 避免出错：添加或减去的值产生无法表示的 DateTime。
            if ((ret - addBefore) <= ts)
            {
                return ret;
            }
            return addBefore.Add(ts);
        }

    }
}
