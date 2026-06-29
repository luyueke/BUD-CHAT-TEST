using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;

/// <summary>
/// 接管unity的日声打印
/// </summary>
public class LogHandler : ILogHandler
{
    private static ILogHandler _defaultLogHandler = Debug.unityLogger.logHandler;

    public static string logDir = "Logs";
    public static string logExtension = ".log";
    public static string prefixName = "";
    public static bool isPrintToConsole = true;
    public static uint maxLogDayCount = 5; // 最多保留 几天的日志
    public static string dateTimeFormat = "yyyyMMddHHmmss";
    public static string dateFormat = "yyyyMMdd";
    private static string _logPath;
    private string _logFile;

    private string _lastLog;
    private int _repeat = 0;
    private LoggerThread _thread;

    private List<string> _logs = new List<string>();//用于给外部获取使用的日志列表
    private int _logsCount = 5000;//日志的长度

    private static string persistentDataPath;
    public LogHandler()
    {
        persistentDataPath = Application.persistentDataPath;//UnityException: get_persistentDataPath can only Ibe called from the main thread.
        Debug.unityLogger.logHandler = this;
    }

    /// <summary>
    /// 当前日志路径
    /// </summary>
    /// <value></value>
    public string currentLogFile
    {
        get { return _logFile; }
    }


    //存储日志的路径
    public static string logPath
    {
        get
        {
            if (_logPath == null)
            {
                _logPath = logDir;
                if (Application.isEditor == false)
                    _logPath = Path.Combine(persistentDataPath, _logPath);
            }
            return _logPath;
        }
    }
    /// <summary>
    /// 获取date这个日期的一天日志
    /// </summary>
    /// <param name="date"></param>
    /// <returns></returns>
    public static string[] GetDateLogFiles(DateTime date)
    {
        var dateStr = prefixName + date.ToString(dateFormat);
        var reg = new Regex(dateStr);
        return GetAllLogFiles().Where(file => reg.IsMatch(file)).ToArray();

    }
    /// <summary>
    /// 获取所有日志文件
    /// </summary>
    /// <returns></returns>
    public static string[] GetAllLogFiles()
    {
        var files = Directory.GetFiles(logPath, "*" + logExtension);
        var l = dateTimeFormat.Length;
        var dateStr = prefixName + @"[0-9]{" + l + "}";
        var reg = new Regex(dateStr);

        return files.Where(file => reg.IsMatch(file)).ToArray();
    }

    public void Start()
    {
        Stop();
        _logFile = Path.Combine(logPath, prefixName + System.DateTime.Now.ToString(dateTimeFormat) + logExtension);
        _thread = new LoggerThread();
        _thread.Start(_logFile, maxLogDayCount);
    }
    public void Stop()
    {
        LogRepeat();
        if (_thread != null)
        {
            _thread.Dispose();
            _thread = null;
        }
        _lastLog = string.Empty;
    }



    public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
    {
        if (Debug.unityLogger.IsLogTypeAllowed(logType))
        {
            if (format == "{0}")
                Log(logType, args[0].ToString());
            else
                Log(logType, string.Format(format, args));
            if (isPrintToConsole)
                _defaultLogHandler.LogFormat(logType, context, format, args);

        }
    }

    public void LogException(Exception exception, UnityEngine.Object context)
    {
        Log(LogType.Exception, exception.Message + "\n" + exception.StackTrace);
        if (isPrintToConsole)
            _defaultLogHandler.LogException(exception, context);
    }

    public void Dispose()
    {
        Stop();
        Debug.unityLogger.logHandler = _defaultLogHandler;
    }

    public void Flush()
    {
        LogRepeat();
        if (_thread != null)
        {
            _thread.Flush();
        }
    }

    public List<string> GetLocalLogs()
    {
        return _logs;
    }

    public void ClearLocalLogs()
    {
        _logs.Clear();
    }

    private void Log(LogType type, string str)
    {
        if (str == _lastLog)
        {
            _repeat++;
            if (_repeat > 2)
                return;
        }
        else
        {
            LogRepeat();
        }

        _lastLog = str;
        if (_thread != null)
            _thread.Log(System.DateTime.Now, type, str);

        string lstr = string.Format("[{0}] LogType : {1}---{2}]", System.DateTime.Now, type, str);
        _logs.Add(lstr);
        if (_logs.Count > _logsCount)
        {
            _logs.RemoveAt(0);
        }
    }

    private void LogRepeat()
    {
        if (_repeat > 0)
        {
            if (_thread != null)
                _thread.Log(DateTime.Now, LogType.Log, "[Repeat: " + _repeat + "]");
            _repeat = 0;
        }
    }
}

