using System;
using UnityEngine;

public class ActivityTimeUtils
{
     /// <summary>
    /// 解析剩余时间格式并转换为结束时间戳
    /// </summary>
    /// <param name="remainingTimeStr">剩余时间字符串，格式如"5天5小时"</param>
    /// <returns>结束时间戳（秒）</returns>
    public static long ParseRemainingTimeToEndTimestamp(string remainingTimeStr)
    {
        if (string.IsNullOrEmpty(remainingTimeStr))
        {
            return 0;
        }

        try
        {
            int days = 0;
            int hours = 0;
            int minutes = 0;
            int seconds = 0;

            // 解析天数
            var dayMatch = System.Text.RegularExpressions.Regex.Match(remainingTimeStr, @"(\d+)天");
            if (dayMatch.Success)
            {
                days = int.Parse(dayMatch.Groups[1].Value);
            }

            // 解析小时
            var hourMatch = System.Text.RegularExpressions.Regex.Match(remainingTimeStr, @"(\d+)小时");
            if (hourMatch.Success)
            {
                hours = int.Parse(hourMatch.Groups[1].Value);
            }

            // 解析分钟
            var minuteMatch = System.Text.RegularExpressions.Regex.Match(remainingTimeStr, @"(\d+)分钟");
            if (minuteMatch.Success)
            {
                minutes = int.Parse(minuteMatch.Groups[1].Value);
            }

            // 解析秒数
            var secondMatch = System.Text.RegularExpressions.Regex.Match(remainingTimeStr, @"(\d+)秒");
            if (secondMatch.Success)
            {
                seconds = int.Parse(secondMatch.Groups[1].Value);
            }

            // 计算结束时间
            DateTime endTime = TcpTimeSystem.Inst.ServerDataTime.AddDays(days).AddHours(hours).AddMinutes(minutes).AddSeconds(seconds);

            // 转换为时间戳（使用UTC时间）
            return ((DateTimeOffset)endTime.ToUniversalTime()).ToUnixTimeSeconds();
        }
        catch (Exception ex)
        {
            Debug.LogError($"解析剩余时间失败: {remainingTimeStr}, 错误: {ex.Message}");
            return 0;
        }
    }

    /// <summary>
    /// 将时间戳转换为DateTime（本地时间）
    /// </summary>
    /// <param name="timestamp">Unix时间戳（秒）</param>
    /// <returns>DateTime对象（本地时间）</returns>
    public static DateTime TimestampToDateTime(long timestamp)
    {
        return DateTimeOffset.FromUnixTimeSeconds(timestamp).LocalDateTime;
    }

    /// <summary>
    /// 将DateTime转换为时间戳
    /// </summary>
    /// <param name="dateTime">DateTime对象</param>
    /// <returns>Unix时间戳（秒）</returns>
    public static long DateTimeToTimestamp(DateTime dateTime)
    {
        return ((DateTimeOffset)dateTime.ToUniversalTime()).ToUnixTimeSeconds();
    }
}