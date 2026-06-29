using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Author: 熊昭
/// Description: unity-log日志管理器
/// Date: 2021-11-29 15:41:48
/// </summary>
public class LoggerUtils
{
    public enum LogLevel
    {
        ALL,   // 最低等级的，用于打开所有日志记录；
        TRACE, // 比DEBUG更详细信息事件，很低的日志级别，一般不会使用；
        DEBUG, // 主要用于开发过程中打印一些运行信息；
        INFO,  // 打印一些你感兴趣的或者重要的信息，这个可以用于生产环境中输出程序运行的一些重要信息；
        WARN,  // 表明会出现潜在错误的情形，给开发人员一些提示；
        ERROR, // 虽然发生错误事件，但仍然不影响系统的继续运行，打印错误和异常信息；
        FATAL, // 指出每个严重的错误事件将会导致应用程序的退出，重大错误，可以直接停止程序；
        OFF,   // 最高等级的，用于关闭所有日志记录；
    }
    
    private static string tag = "UnityMsgLog--";
    private static StringBuilder _sb;
    public static bool IsDebug = true;
    public static bool enableLogUpload = false;

    public static void Log(object message, Color color)
    {
        Log($"<color=#{ColorUtility.ToHtmlStringRGBA(color)}>{message}</color>");
    }

    // public static void Log(object message, string colorHtml)
    // {
    //     Log($"<color=#{colorHtml}>{message}</color>");
    // }
    //
    #region Debug.Log
    public static void Log(object message)
    {
        LogData(LogLevel.DEBUG, message);
    }

    public static void Log(object message1, object message2)
    {
        LogData(LogLevel.DEBUG, message1, message2);
    }
    public static void Log(object message1, object message2, object message3)
    {
        LogData(LogLevel.DEBUG, message1, message2, message3);
    }
    
    public static void Log(object message1, object message2, object message3, object message4)
    {
        LogData(LogLevel.DEBUG, message1, message2, message3, message4);
    }
    
    public static void Log(object message1, object message2, object message3, object message4,object message5)
    {
        LogData(LogLevel.DEBUG, message1, message2, message3, message4, message5);
    }
    
    public static void LogFormat(string format, params object[] args)
    {
        if (!IsDebug) 
            return;
        
        Debug.LogFormat(format, args);
    }
    #endregion
    
    #region Debug.LogError
    public static void LogError(object message)
    {
        LogData(LogLevel.ERROR, message);
    }
    
    public static void LogError(object message1, object message2)
    {
        LogData(LogLevel.ERROR, message1, message2);
    }
    public static void LogError(object message1, object message2, object message3)
    {
        LogData(LogLevel.ERROR, message1, message2, message3);
    }
    
    public static void LogError(object message1, object message2, object message3, object message4)
    {
        LogData(LogLevel.ERROR, message1, message2, message3, message4);
    }
    
    public static void LogError(object message1, object message2, object message3, object message4,object message5)
    {
        LogData(LogLevel.ERROR, message1, message2, message3, message4, message5);
    }
    
    public static void LogErrorFormat(string format, params object[] args)
    {
        if (!IsDebug) 
            return;
        
        Debug.LogErrorFormat(format, args);
    }
    #endregion
    
    private static void LogData(LogLevel logLevel,params object[] args)
    {
        if (!IsDebug && !enableLogUpload)
        {
            return;
        }

        switch (logLevel)
        {
            case LogLevel.DEBUG:
                Debug.Log(GetObjectsMsg(args));
                break;
            
            case LogLevel.ERROR:
                Debug.LogError(GetObjectsMsg(args));
                break;
        }

        // if (Debugger.isSaveLog)
        //     Debugger.LogNormal(GetObjectsMsg(args));
    }
    

    public static void LogReport(object message, string eveName = "")
    {
        // if (IsDebug || !ExceptionReport.IsReportLog) return;
        // string eve = string.IsNullOrEmpty(eveName) ?"": string.Format("[{0}]", eveName);
        // string curTime = GameUtils.GetCurTimeStr();
        // Debug.Log(string.Format("[log]{0}{1}[t]{2}", eve, message,curTime));
    }
    
    private static string GetObjectsMsg(object[] args)
    {
        _sb ??= new StringBuilder();
        _sb.Clear();
        _sb.Append(tag);
        foreach (var arg in args)
        {
            _sb.Append(arg);
        }
        return _sb.ToString();
    }
}