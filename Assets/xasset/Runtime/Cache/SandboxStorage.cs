using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using xasset;

public class SandboxStorage
{
    public static string SandboxPath { get; internal set; } = $"{Application.persistentDataPath}/RemoteCache";
    public static int _refCount { get; internal set; } = 0;
    // UGC缓存大小限制
    public static ulong LimitSize { get; internal set; } = 200 * 1024 * 1024;
    // UGC缓存满了清理后保留的大小
    public static ulong ReduceSize { get; internal set; } = 100 * 1024 * 1024;

    public static ulong CurSize { get; internal set; } = 0;

    public static void Init()
    {
        // 1.80.0版本清除一次图片缓存
        if (Assets.Versions.versionCode == "1.80.0" && PlayerPrefs.GetInt("SandboxClear1.80.0", 0) == 0)
        {
            ClearAllCache();
        }

        if (!Directory.Exists(SandboxPath))
            Directory.CreateDirectory(SandboxPath);


        CurSize = GetDirectorySize(SandboxPath);
    }

    public static void DownLoadFinish(ulong size, string path, byte[] bytes)
    {
        var request = new SaveCacheRequest(size, path, bytes);
        request.completed += (req) =>
        {
            if (req.result == Request.Result.Success)
            {
                CheckSize();
                CurSize += ((SaveCacheRequest)req)._size;
            }
        };
        request.SendRequest();
    }

    public static void DeleteFile(string filename)
    {
        try
        {
            var info = new FileInfo(filename);
            if (info.Exists)
            {
                File.Delete(filename);
                CurSize -= (ulong)info.Length;
            }
        }
        catch { }
    }

    public static string GetSandboxPath(string filename)
    {
        return $"{SandboxPath}/{filename}";
    }

    public static ulong GetDirectorySize(string path)
    {
        ulong size = 0;
        DirectoryInfo dir = new DirectoryInfo(path);

        foreach (FileInfo file in dir.GetFiles())
        {
            size += (ulong)file.Length;
        }

        foreach (DirectoryInfo subDir in dir.GetDirectories())
        {
            size += GetDirectorySize(subDir.FullName);
        }

        return size;
    }

    public static void CheckSize()
    {
        if (CurSize < LimitSize) return;
        DirectoryInfo dir = new DirectoryInfo(SandboxPath);
        var files = dir.GetFiles();
        var filesList = files.ToList();
        filesList.Sort((a, b) => a.LastWriteTimeUtc > b.LastWriteTimeUtc ? -1 : 1);
        var list = new List<FileInfo>();
        foreach (FileInfo file in dir.GetFiles())
        {
            try
            {
                File.Delete(file.FullName);
                CurSize -= (ulong)file.Length;
                if (CurSize < ReduceSize) return;
            }
            catch
            {
                continue;
            }
        }
    }

    internal static void ClearAllCache()
    {
        try
        {
            if (Directory.Exists(SandboxPath))
                Directory.Delete(SandboxPath, true);
        }
        catch(Exception E) {
            Debug.LogError(E.Message);
        }
        PlayerPrefs.SetInt("SandboxClear1.80.0", 1);
    }
}

public class SaveCacheRequest : Request
{
    public ulong _size { get; private set; }
    private string _path;
    private byte[] _bytes;

    public SaveCacheRequest(ulong size, string path, byte[] bytes)
    {
        _size = size;
        _path = path;
        _bytes = bytes;
    }

    protected override void OnStart()
    {
        try
        {
            var info = new FileInfo(_path);
            if (info.Exists)
            {
                if (info.Length == _bytes.Length)
                {
                    SetResult(Result.Failed);
                    return;
                }
                else
                {
                    File.Delete(_path);
                }
            }
            
            File.WriteAllBytes(_path, _bytes);
            SetResult(Result.Success);
        }
        catch (Exception exception)
        {
            UnityEngine.Debug.LogError("SaveCacheRequest:" + _path + "," + exception.Message);
            SetResult(Result.Failed);
        }
    }
}