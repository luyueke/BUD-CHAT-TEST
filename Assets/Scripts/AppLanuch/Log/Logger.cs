using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// 可接管unity的Debug.log，把日志用子线程写到本地
/// 每次启动都重启开启一个文件写入，文件名由logFileName+日期时间+后辍组成
/// </summary>
public static class Logger
{
    private static LogHandler _currentLogHandler;
    private static bool _isOn = false;

    private static GameObject _go;


    /// <summary>
    /// 启动本地写日志
    /// </summary>
    public static void TurnOn()
    {
        if (_isOn)
            return;
        if (Application.isPlaying == false)
            return;
        _isOn = true;
        if (_currentLogHandler != null)
            _currentLogHandler.Dispose();
        _currentLogHandler = new LogHandler();
        _currentLogHandler.Start();


        if (Application.isEditor)
        {
            if (_go == null)
            {
                _go = new GameObject("Logger");
                GameObject.DontDestroyOnLoad(_go);
                _go.AddComponent<LoggerComponent>();
                _go.hideFlags = HideFlags.HideInHierarchy;
            }
        }

    }

    /// <summary>
    /// 关闭本地写日志
    /// </summary>
    public static void TurnOff()
    {
        if (_isOn == false)
            return;
        _isOn = false;
        if (_currentLogHandler != null)
            _currentLogHandler.Dispose();
        _currentLogHandler = null;
    }

    /// <summary>
    /// 是否开启本地写日志
    /// </summary>
    public static bool isOn
    {
        get { return _isOn; }
        set
        {
            if (Application.isPlaying == false)
                return;
            if (_isOn == value)
                return;
            if (value)
                TurnOn();
            else
                TurnOff();
        }
    }

    /// <summary>
    /// 存储日志的目录名, 可以设置
    /// </summary>
    public static string logDir
    {
        get { return LogHandler.logDir; }
        set { LogHandler.logDir = value; }
    }

    /// <summary>
    /// 存储日志的完整目录路径
    /// </summary>
    public static string logPath
    {
        get { return LogHandler.logPath; }
    }

    /// <summary>
    /// 当前日志文件路径，如果没开启写日志，则路径为空
    /// </summary>
    /// <value></value>
    public static string currentLogFile
    {
        get { return _currentLogHandler?.currentLogFile; }
    }

    /// <summary>
    /// 获取最后第几天的单天日志，0为当天
    /// </summary>
    /// <param name="day"></param>
    /// <returns></returns>
    public static string[] GetLastDayLogFiles(int day)
    {
        var d = DateTime.Today.AddDays(-day);

        return GetDateLogFiles(d);
    }

    /// <summary>
    /// 获取date这个日期的一天日志
    /// </summary>
    /// <param name="date"></param>
    /// <returns></returns>
    public static string[] GetDateLogFiles(DateTime date)
    {
        return LogHandler.GetDateLogFiles(date);
    }

    /// <summary>
    /// 获取所有日志文件
    /// </summary>
    /// <returns></returns>
    public static string[] GetAllLogFiles()
    {
        return LogHandler.GetAllLogFiles();
    }

    /// <summary>
    /// 日志后辍，可配置,默认.log
    /// </summary>
    public static string logExtension
    {
        get { return LogHandler.logExtension; }
        set { LogHandler.logExtension = value; }
    }

    /// <summary>
    /// 日志前辍
    /// </summary>
    public static string prefixName
    {
        get { return LogHandler.prefixName; }
        set { LogHandler.prefixName = value; }
    }


    /// <summary>
    /// 日志是否输出到终端
    /// </summary>
    public static bool isPrintToConsole
    {
        get { return LogHandler.isPrintToConsole; }
        set { LogHandler.isPrintToConsole = value; }
    }




    private class LoggerComponent : MonoBehaviour
    {
        void OnDestroy()
        {
            _isOn = false;
            if (_currentLogHandler != null)
                _currentLogHandler.Dispose();
            _currentLogHandler = null;
            _go = null;
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
                _currentLogHandler.Flush();
        }
    }

    #region 本地log管理

    public static List<string> GetLocalLogs()
    {
        if (_currentLogHandler == null)
        {
            return null;
        }
        return _currentLogHandler.GetLocalLogs();
    }

    public static void ClearLocalLogs()
    {
        if (_currentLogHandler != null)
        {
            _currentLogHandler.ClearLocalLogs();
        }
    }

    #endregion
}

