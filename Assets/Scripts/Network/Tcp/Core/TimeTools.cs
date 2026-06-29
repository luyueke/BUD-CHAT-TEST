using System;
using System.Collections;
using UnityEngine;


public static class TimeTools
{


    /// <summary>
    /// 获取时间戳对应的周几
    /// </summary>
    /// <param name="timestamp">Unix时间戳（秒）</param>
    /// <returns>1=周一, 2=周二, ..., 7=周日</returns>
    public static int GetDayOfWeek(long timestamp)
    {
        DateTime dateTime = SecondsToDateTime(timestamp);
        int dayOfWeek = (int)dateTime.DayOfWeek;
        return dayOfWeek == 0 ? 7 : dayOfWeek;
    }

    /// <summary>
    /// 获取时间戳对应的周几名称
    /// </summary>
    /// <param name="timestamp">Unix时间戳（秒）</param>
    /// <returns>周几的中文名称</returns>
    public static string GetDayOfWeekName(long timestamp)
    {
        int dayOfWeek = GetDayOfWeek(timestamp);
        string[] weekNames = { "", "周一", "周二", "周三", "周四", "周五", "周六", "周日" };
        return weekNames[dayOfWeek];
    }

    /// <summary>
    /// 获取今天的周几
    /// </summary>
    /// <returns>1=周一, 2=周二, ..., 7=周日</returns>
    public static int GetTodayDayOfWeek()
    {
        int dayOfWeek = (int)DateTime.Now.DayOfWeek;
        return dayOfWeek == 0 ? 7 : dayOfWeek;
    }

    /// <summary>
    /// 获取今天的周几名称
    /// </summary>
    /// <returns>今天周几的中文名称</returns>
    public static string GetTodayDayOfWeekName()
    {
        int dayOfWeek = GetTodayDayOfWeek();
        string[] weekNames = { "", "周一", "周二", "周三", "周四", "周五", "周六", "周日" };
        return weekNames[dayOfWeek];
    }

    /// <summary>
    /// 判断时间戳是否为今天
    /// </summary>
    /// <param name="timestamp">Unix时间戳（秒）</param>
    /// <returns>是否为今天</returns>
    public static bool IsToday(long timestamp)
    {
        DateTime dateTime = DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;
        DateTime today = DateTime.Today;
        return dateTime.Date == today;
    }

    /// <summary>
    /// 判断时间戳是否为本周
    /// </summary>
    /// <param name="timestamp">Unix时间戳（秒）</param>
    /// <returns>是否为本周</returns>
    public static bool IsThisWeek(long timestamp)
    {
        DateTime dateTime = DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;
        DateTime today = DateTime.Today;
        DateTime startOfWeek = today.AddDays(-(int)today.DayOfWeek);
        DateTime endOfWeek = startOfWeek.AddDays(6);

        return dateTime.Date >= startOfWeek && dateTime.Date <= endOfWeek;
    }

    /// <summary>
    /// 获取时间戳所在周的起始日 0 点（周日）的时间戳（秒，本地时间）
    /// </summary>
    public static DateTime GetWeekStartTimestamp(long timestamp)
    {
        DateTime dateTime = SecondsToDateTime(timestamp);
        var week = GetDayOfWeek(timestamp) - 1;
        DateTime startOfWeek = dateTime.Date.AddDays(- week);
        return startOfWeek;
    }

    /// <summary>
    /// 判断当前时间是否相对上次记录已进入新的一周
    /// </summary>
    /// <param name="lastTimestamp">上次记录的时间戳（秒），如上次领周奖励的时间；≤0 视为无记录，返回 true</param>
    /// <param name="currentTimestamp">当前时间戳（秒），不传则用当前时间</param>
    /// <returns>若当前所在周与 lastTimestamp 所在周不同则为 true（新的一周）</returns>
    public static bool IsNewWeek(long lastTimestamp, long currentTimestamp = 0)
    {
        var lastWeekStart = GetWeekStartTimestamp(lastTimestamp);
        var currWeekStart = GetWeekStartTimestamp(currentTimestamp);
        return currWeekStart.Date > lastWeekStart.Date;
    }

    public static long TicksToMilliseconds(long ticks)
    {
        return ticks / 10000;
    }

    // Ticks转秒
    public static long TicksToSeconds(long ticks)
    {
        return ticks / 10000000;
    }

    // Ticks转Unix时间戳（秒）
    public static long TicksToUnixTimestamp(long ticks)
    {
        // Unix时间戳从1970年开始，需要减去1970年的Ticks
        long unixTicks = ticks - new DateTime(1970, 1, 1).Ticks;
        return unixTicks / 10000000;
    }

    // Ticks转Unix时间戳（毫秒）
    public static long TicksToUnixTimestampMs(long ticks)
    {
        long unixTicks = ticks - new DateTime(1970, 1, 1).Ticks;
        return unixTicks / 10000;
    }

    /// <summary>
    /// 秒时间戳转DateTime
    /// </summary>
    /// <param name="timestamp">Unix时间戳（秒）</param>
    /// <returns>DateTime对象</returns>
    public static DateTime SecondsToDateTime(long timestamp)
    {
        return DateTimeOffset.FromUnixTimeSeconds(timestamp).LocalDateTime;
    }

    /// <summary>
    /// 毫秒时间戳转DateTime
    /// </summary>
    /// <param name="timestamp">Unix时间戳（毫秒）</param>
    /// <returns>DateTime对象</returns>
    public static DateTime MillisecondsToDateTime(long timestamp)
    {
        return DateTimeOffset.FromUnixTimeMilliseconds(timestamp).DateTime;
    }

    /// <summary>
    /// DateTime转秒时间戳
    /// </summary>
    /// <param name="dateTime">DateTime对象</param>
    /// <returns>Unix时间戳（秒）</returns>
    public static long DateTimeToSeconds(DateTime dateTime)
    {
        return new DateTimeOffset(dateTime).ToUnixTimeSeconds();
    }

    /// <summary>
    /// DateTime转毫秒时间戳
    /// </summary>
    /// <param name="dateTime">DateTime对象</param>
    /// <returns>Unix时间戳（毫秒）</returns>
    public static long DateTimeToMilliseconds(DateTime dateTime)
    {
        return new DateTimeOffset(dateTime).ToUnixTimeMilliseconds();
    }

    /// <summary>
    /// 获取当前时间戳（秒）
    /// </summary>
    /// <returns>当前Unix时间戳（秒）</returns>
    public static long GetCurrentTimestamp()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    /// <summary>
    /// 获取当前时间戳（毫秒）
    /// </summary>
    /// <returns>当前Unix时间戳（毫秒）</returns>
    public static long GetCurrentTimestampMs()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    /// <summary>
    /// 时间戳转格式化的字符串
    /// </summary>
    /// <param name="timestamp">Unix时间戳（秒）</param>
    /// <param name="format">格式化字符串</param>
    /// <returns>格式化的时间字符串</returns>
    public static string TimestampToString(long timestamp, string format = "yyyy-MM-dd HH:mm:ss")
    {
        DateTime dateTime = SecondsToDateTime(timestamp);
        return dateTime.ToString(format);
    }

    /// <summary>
    /// 时间戳转相对时间字符串
    /// </summary>
    /// <param name="timestamp">Unix时间戳（秒）</param>
    /// <returns>相对时间字符串</returns>
    public static string TimestampToRelativeTime(long timestamp)
    {
        DateTime targetTime = SecondsToDateTime(timestamp);
        DateTime now = DateTime.Now;
        TimeSpan timeSpan = now - targetTime;

        if (timeSpan.TotalDays >= 1)
        {
            return $"{(int)timeSpan.TotalDays}天前";
        }
        else if (timeSpan.TotalHours >= 1)
        {
            return $"{(int)timeSpan.TotalHours}小时前";
        }
        else if (timeSpan.TotalMinutes >= 1)
        {
            return $"{(int)timeSpan.TotalMinutes}分钟前";
        }
        else
        {
            return "刚刚";
        }
    }

    /// <summary>
    /// 将秒数转换为时:分:秒格式
    /// </summary>
    /// <param name="totalSeconds">总秒数</param>
    /// <returns>格式化的时间字符串</returns>
    public static string SecondsToTimeString(int totalSeconds)
    {
        int hours = totalSeconds / 3600;
        int minutes = (totalSeconds % 3600) / 60;
        int seconds = totalSeconds % 60;

        return $"{hours:D2}:{minutes:D2}:{seconds:D2}";
    }

    /// <summary>
    /// 将秒数转换为中文格式（几时几分几秒）
    /// </summary>
    /// <param name="totalSeconds">总秒数</param>
    /// <returns>中文格式的时间字符串</returns>
    public static string SecondsToChineseTimeString(int totalSeconds)
    {
        int hours = totalSeconds / 3600;
        int minutes = (totalSeconds % 3600) / 60;
        int seconds = totalSeconds % 60;

        string result = "";

        if (hours > 0)
            result += $"{hours}时";
        if (minutes > 0)
            result += $"{minutes}分";
        if (seconds > 0 || result == "")
            result += $"{seconds}秒";

        return result;
    }

    /// <summary>
    /// 将秒数转换为详细的中文格式
    /// </summary>
    /// <param name="totalSeconds">总秒数</param>
    /// <returns>详细的中文格式时间字符串</returns>
    public static string SecondsToDetailedChineseTimeString(int totalSeconds)
    {
        int hours = totalSeconds / 3600;
        int minutes = (totalSeconds % 3600) / 60;
        int seconds = totalSeconds % 60;

        return $"{hours}小时{minutes}分钟{seconds}秒";
    }
}
