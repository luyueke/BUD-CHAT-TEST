using UnityEngine;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using System;
using System.Text;


/// <summary>
/// 子线程写日志
/// </summary>
public class LoggerThread
{
    private FileStream _fileStream;
    private StreamWriter _streamWriter;

    private bool _isAbort = false;
    private string _logFile;
    private object _lockObj = new object();
    private List<LogData> _logList = new List<LogData>();
    private uint _maxLogDayCount;
    private bool _isCheckMaxDayCount;
    public void Start(string logFile, uint maxLogDayCount)
    {
        if (_isAbort)
            return;
        _logFile = logFile;
        _maxLogDayCount = maxLogDayCount;
        _isCheckMaxDayCount = true;
        _isAbort = false;
        string dirPath = Path.GetDirectoryName(_logFile);
        if (!Directory.Exists(dirPath))
            Directory.CreateDirectory(dirPath);
        _fileStream = new FileStream(_logFile, FileMode.Append, FileAccess.Write, FileShare.Read);
        _streamWriter = new StreamWriter(_fileStream, Encoding.UTF8);
        Thread th = new Thread(ThreadMethod); //创建线程                     
        th.Start();
    }

    public void Log(DateTime time, LogType type, string str)
    {
        lock (_lockObj)
        {
            _logList.Add(new LogData(time, type, str));
        }
    }
    public void LogRepeat(string str)
    {
        lock (_lockObj)
        {
            var d = new LogData(default(DateTime), LogType.Log, str);
            d.isRepeat = true;
            _logList.Add(d);
        }
    }

    public void Flush()
    {
        WriteLog();
    }

    public void Dispose()
    {
        _isAbort = true;
        WriteLog();
        if (_streamWriter != null)
        {
            _streamWriter.Flush();
            _streamWriter.Close();
            _streamWriter.Dispose();
        }
        _streamWriter = null;
        if (_fileStream != null)
        {
            _fileStream.Close();
            _fileStream.Dispose();
        }
        _fileStream = null;
    }

    private void WriteLog()
    {
        lock (_lockObj)
        {
            for (int i = 0, n = _logList.Count; i < n; i++)
            {
                var logData = _logList[i];
                if (logData.isRepeat)
                {
                    _streamWriter.WriteLine(logData.message);
                }
                else
                {
                    var typeSrt = "";
                    if (logData.type == LogType.Log)
                        typeSrt = "|L|";
                    else if (logData.type == LogType.Warning)
                        typeSrt = "|W|";
                    else if (logData.type == LogType.Error)
                        typeSrt = "|E|";
                    else if (logData.type == LogType.Assert)
                        typeSrt = "|A|";
                    else if (logData.type == LogType.Exception)
                        typeSrt = "|Ex|";
                    _streamWriter.WriteLine(logData.time.ToString("HH:mm:ss.fff") + typeSrt + logData.message);
                }
            }
            _streamWriter.Flush();
            _fileStream.Flush();
            _logList.Clear();
        }
    }
    private void CheckMaxDayCount()
    {
        _isCheckMaxDayCount = false;
        var currentFileName = Path.GetFileName(_logFile);
        var files = LogHandler.GetAllLogFiles();

        var arr = new List<string>();
        for (var i = 0; i < _maxLogDayCount; i++)
        {
            var date = DateTime.Today.AddDays(-i);
            arr.Add(date.ToString(LogHandler.dateFormat));
        }
        for (var i = 0; i < files.Length; i++)
        {
            var file = files[i];
            var fileName = Path.GetFileName(file);
            if (fileName != currentFileName)
            {
                var del = true;
                for (var j = 0; j < arr.Count; j++)
                {
                    if (fileName.IndexOf(arr[j]) != -1)
                    {
                        del = false;
                        break;
                    }
                }
                if (del)
                {
                    File.Delete(file);
                }
            }

        }
    }
    private void ThreadMethod()
    {
        while (!_isAbort)
        {
            WriteLog();
            if (_isCheckMaxDayCount)
                CheckMaxDayCount();
            Thread.Sleep(60);
        }
    }

    struct LogData
    {
        public DateTime time;
        public LogType type;
        public string message;
        public bool isRepeat;

        public LogData(DateTime time, LogType type, string message)
        {
            this.time = time;
            this.type = type;
            this.message = message;
            isRepeat = false;
        }
    }

}


