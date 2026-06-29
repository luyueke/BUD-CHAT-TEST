// TimeUtils.cs
// Create by xiaojl Mar/15/2023
// 时间工具类

using System;

namespace Network.Tcp.Core
{
    internal static class TimeUtils
    {

        private static readonly int OneMinuteTimeStamp = 60;
        private static readonly int OneHourTimeStamp = 60 * OneMinuteTimeStamp;
        private static readonly int OneDayTimeStamp = 24 * OneHourTimeStamp;
        private static readonly DateTime DT_UTC_2018 = new DateTime(2018, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // 自2018到现在所经过的秒数
        public static int NowSeconds()
        {
            return (int)((DateTime.UtcNow - DT_UTC_2018).TotalSeconds);
        }

        /// <summary>
        /// 将时间戳转换为 HH:MM 的形式
        /// </summary>
        /// withSplit : 是否带分割符返回
        public static string FormatTimeHAM(int time, string withSplit = ":")
        {
            int timeStamp = time;
            int hour = timeStamp / OneHourTimeStamp;
            timeStamp = (timeStamp - hour * OneHourTimeStamp);
            int minute = (int)Math.Ceiling((double)timeStamp / OneMinuteTimeStamp);
            return $"{hour.ToString("00")}{withSplit}{minute.ToString("00")}";
        }

        /// <summary>
        /// 将时间戳转换为 0d 0h 0m 的形式
        /// </summary>
        public static string FormatTimeDHM(int time)
        {
            int timeStamp = time;
            int day = timeStamp / OneDayTimeStamp; // 天数
            timeStamp = (timeStamp - day * OneDayTimeStamp);
            int hour =  timeStamp / OneHourTimeStamp; // 小时
            timeStamp = (timeStamp - hour * OneHourTimeStamp);
            int minute = (int)Math.Ceiling((double)timeStamp / OneMinuteTimeStamp);

            return $"{day}d {hour}h {minute}m";
        }

        /// <summary>
        /// 将时间戳转换为 0d 0h 0m 的形式
        /// </summary>
        public static string FormatTimeHM(int time)
        {
            int timeStamp = time;
            int hour =  timeStamp / OneHourTimeStamp; // 小时
            timeStamp = (timeStamp - hour * OneHourTimeStamp);
            int minute = (int)Math.Ceiling((double)timeStamp / OneMinuteTimeStamp);

            return $"{hour}h {minute}m";
        }
    }
}
