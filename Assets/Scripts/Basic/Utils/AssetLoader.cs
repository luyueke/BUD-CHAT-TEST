using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class AssetLoader
{
    
    public class CallBack<T> {
        public Action<T> onComplete;
        public Action<long> onError;


    }
    
    private static Downloader downloader;

    private Dictionary<string, CallBack<byte[]>> byteLoadingDic =
        new Dictionary<string, CallBack<byte[]>>();
    
    private Dictionary<string, CallBack<string>> stringLoadingDic =
        new Dictionary<string, CallBack<string>>();
    
    public AssetLoader()
    {
        if (downloader == null)
        {
            var download = new GameObject("Download");
            downloader = download.AddComponent<Downloader>();
        }
    }
    
    

    public void DownloadAsset(string url, Action<string> onComplete, Action<long> onError)
    {
        if (string.IsNullOrEmpty(url)) {
            LoggerUtils.LogError("DownloadAsset: url 为 null");
            onError?.Invoke(404);
            return;
        }

        if (stringLoadingDic.ContainsKey(url)) {
            stringLoadingDic[url].onComplete += onComplete;
            stringLoadingDic[url].onError += onError;
            return;
        }
        
        stringLoadingDic.Add(url, new CallBack<string>() {
            onComplete = onComplete,
            onError = onError
        });
        
        downloader.DownloadAsset(url, (string data) => {
            if (!stringLoadingDic.TryGetValue(url, out var callBack)) return;
            callBack.onComplete?.Invoke(data);
            stringLoadingDic.Remove(url);


        }, (err) => {
            if (!stringLoadingDic.TryGetValue(url, out var callBack)) return;
            callBack.onError?.Invoke(err);
            stringLoadingDic.Remove(url);
        });
    }
    
    public void DownloadAsset(string url, Action<byte[]> onComplete, Action<long> onError)
    {
        if (string.IsNullOrEmpty(url)) {
            LoggerUtils.LogError("DownloadAsset: url 为 null");
            onError?.Invoke(404);
            return;
        }

        if (byteLoadingDic.ContainsKey(url)) {
            byteLoadingDic[url].onComplete += onComplete;
            byteLoadingDic[url].onError += onError;
            return;
        }
        
        byteLoadingDic.Add(url, new CallBack<byte[]>() {
            onComplete = onComplete,
            onError = onError
        });
        
        downloader.DownloadAsset(url, (byte[] byteData) => {
            if (!byteLoadingDic.TryGetValue(url, out var callBack)) return;
            callBack.onComplete?.Invoke(byteData);
            byteLoadingDic.Remove(url);


        }, (err) => {
            if (!byteLoadingDic.TryGetValue(url, out var callBack)) return;
            callBack.onError?.Invoke(err);
            byteLoadingDic.Remove(url);
        });
    }

    public void Clear() {
        byteLoadingDic.Clear();
    }



}