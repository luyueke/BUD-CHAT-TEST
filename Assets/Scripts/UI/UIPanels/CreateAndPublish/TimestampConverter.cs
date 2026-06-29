using System;

public static class TimestampConverter
{
    
    public static string ConvertToDateTimeString(long timestampInSeconds)
    {
        // Convert timestamp to DateTime in UTC
        DateTime utcDateTime = DateTimeOffset.FromUnixTimeSeconds(timestampInSeconds).UtcDateTime;

        // Manually create China Standard Time offset
        TimeSpan chinaStandardTimeOffset = TimeSpan.FromHours(8); // China is UTC+8

        // Apply the offset to the UTC time
        DateTime chinaDateTime = utcDateTime + chinaStandardTimeOffset;

        // Format the DateTime as a string in "yyyy-MM-dd HH:mm" format
        string formattedDateTime = chinaDateTime.ToString("yyyy-MM-dd HH:mm");

        return formattedDateTime;
    }
}