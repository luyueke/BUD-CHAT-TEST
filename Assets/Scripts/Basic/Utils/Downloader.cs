using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class Downloader:MonoBehaviour
{

    public void DownloadAsset(string url, Action<string> onComplete, Action<long> onError)
    {
        StartCoroutine(LoadAsset(url,onComplete,onError));
    }

    public void DownloadAsset(string url, Action<byte[]> onComplete, Action<long> onError)
    {
        StartCoroutine(LoadAsset(url,onComplete,onError));
    }

    private IEnumerator LoadAsset(string url, Action<string> onComplete, Action<long> onError)
    {
        UnityWebRequest www = UnityWebRequest.Get(url);
        www.timeout = 30;
        yield return www.SendWebRequest();
        if (www.isNetworkError || www.isHttpError)
        {
            long responseCode = www.responseCode;
            www.Abort();

            if (onError != null)
            {
                onError(responseCode);
            }
        }
        else
        {
            if (onComplete != null)
            {
                onComplete(www.downloadHandler.text);
            }

            www.downloadHandler.Dispose();
        }
    }

    private IEnumerator LoadAsset(string url, Action<byte[]> onComplete, Action<long> onError) {
        Uri uri = null;
        try {
            uri = new Uri(url);
        } catch (Exception e) {
            LoggerUtils.LogError("LoadAsset Error: " + e.Message);
            onError?.Invoke(404);
        }
        UnityWebRequest www = UnityWebRequest.Get(uri);
        www.timeout = 30;
        yield return www.SendWebRequest();
        if (www.isNetworkError || www.isHttpError)
        {
            long responseCode = www.responseCode;
            www.Abort();

            if (onError != null)
            {
                onError(responseCode);
            }
        }
        else
        {
            onComplete?.Invoke(www.downloadHandler.data);

            www.downloadHandler.Dispose();
        }
    }
}
