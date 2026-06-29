using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Description: 动态图片适配方法
/// Author: Tee Li
/// </summary>

public static class RawImageExtensions
{
    /// <summary>
    /// 不改变原图分辨率 适配图片铺满整个窗口
    /// </summary>
    /// <param name="img">目标RawImage</param>
    /// <param name="referSize">目标窗口尺寸</param>
    public static void FitTexture(this RawImage img, Vector2 referSize)
    {
        if (img == null || img.texture == null) return;
        if (referSize.x <= 0 || referSize.y <= 0)
        {
            img.FitTexture(0);
            return;
        }
        int texW = img.texture.width;
        int texH = img.texture.height;
        if (texH <= 0 || texW <= 0)
        {
            img.FitTexture(0);
            return;
        }
        float referRatio = referSize.x / referSize.y;
        float texRatio = ((float)texW) / texH;
        if (texRatio >= referRatio) //高度适配
        {
            img.FitTexture(referSize.y, widthFirst: false);
        }
        else //宽度适配
        {
            img.FitTexture(referSize.x, widthFirst: true);
        }
    }

    /// <summary>
    /// 不改变原图分辨率 适配图片适配整个窗口
    /// </summary>
    /// <param name="img">目标RawImage</param>
    /// <param name="targetLength">目标适配长度</param>
    /// <param name="widthFirst">true-宽度适配 false-高度适配</param>
    public static void FitTexture(this RawImage img, float targetLength, bool widthFirst = false)
    {
        if (img == null || img.texture == null) return;
        Vector2 targetSize;

        int texW = img.texture.width;
        int texH = img.texture.height;
        if (texH <= 0 || texW <= 0)
        {
            targetSize = Vector2.zero;
        }
        else
        {
            float texRatio = ((float)texW) / texH;
            targetSize = widthFirst ? new Vector2(targetLength, targetLength / texRatio) : new Vector2(targetLength * texRatio, targetLength);
        }

        img.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetSize.x);
        img.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetSize.y);
    }
}
