using Game.COSXML;
using GameData;
using System;
using System.Collections;
using System.IO;
using UnityEngine;

/// <summary>
/// Author:
/// Desc: 养成舱卡面封面截图工具类，封装从 RenderTexture 读取像素并上传 COS 的通用流程。
///       避免各界面重复实现相同的截图 + 上传逻辑。
/// Date: 26-06-03
/// </summary>
public static class CabinCoverPhotoHelper
{
    /// <summary>
    /// 等待当前帧渲染结束后，从 RenderTexture 截图并上传到 COS，通过 onComplete 回调返回封面 URL。
    /// 调用方须以 StartCoroutine 启动此协程，并通过 yield return 等待结果。
    /// </summary>
    /// <param name="rt">已分配并已由相机渲染的 RenderTexture</param>
    /// <param name="onComplete">上传完成后的回调：上传成功传入封面 URL，失败传入 null</param>
    public static IEnumerator UploadFromRenderTexture(RenderTexture rt, Action<string> onComplete)
    {
        // 等待当前帧渲染结束，确保 RenderTexture 内容已更新
        yield return new WaitForEndOfFrame();

        // 从 RenderTexture 读取像素到内存，使用 ARGB32 保留 Alpha 通道
        byte[] bytes = null;
        bool readSuccess = false;

        try
        {
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.ARGB32, false);
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            bytes = tex.EncodeToPNG();
            UnityEngine.Object.Destroy(tex);
            readSuccess = true;
        }
        catch (Exception e)
        {
            LoggerUtils.LogError("CabinCoverPhotoHelper - 截图读取失败: " + e.Message);
        }

        if (!readSuccess)
        {
            onComplete?.Invoke(null);
            yield break;
        }

        // 保存到本地临时文件，准备上传
        string fileName = LocalDataUtils.Inst.SaveImgRes(bytes);
        var uri = $"IncubationCabin/characterInfo/{AccountDataManager.Inst.Uid}/{Path.GetFileName(fileName)}";

        // 异步上传到 COS，等待结果
        bool done = false;
        string resultUrl = null;

        CosXmlUploadManager.UploadFile(uri, fileName, (url, err) =>
        {
            // 无论成功失败，先清理本地临时文件
            File.Delete(fileName);

            if (!string.IsNullOrEmpty(err))
            {
                LoggerUtils.LogError("CabinCoverPhotoHelper - 封面上传失败: " + err);
            }
            else
            {
                resultUrl = url;
            }

            done = true;
        });

        while (!done)
        {
            yield return null;
        }

        // 将 URL 或 null（失败）回传给调用方
        onComplete?.Invoke(resultUrl);
    }
}
