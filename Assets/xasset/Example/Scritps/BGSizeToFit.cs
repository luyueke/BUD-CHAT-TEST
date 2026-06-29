using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

public class BGSizeToFit : MonoBehaviour
{
    public RawImage customBgImg;

    private void Start()
    {
        Resize();
    }
    private float ScreenRatio()
    {
#if UNITY_EDITOR
        return (float)Screen.width / Screen.height;
#endif
        float portraitW = Math.Max(Screen.currentResolution.width, Screen.currentResolution.height);
        float portraitH = Math.Min(Screen.currentResolution.width, Screen.currentResolution.height);
        return portraitW / portraitH;
    }

    [Button("resize")]
    public void SetBGSizeToFit()
    {
        Resize();
    }

    public void Resize()
    {
        if (customBgImg == null || customBgImg.texture == null)
        {
            return;
        }
        // view 的 宽高比
        float screenRatio = ScreenRatio();
        // 图片的宽高比
        int texW = customBgImg.texture.width;
        int texH = customBgImg.texture.height;
        if (texH == 0)
        {
            return;
        }
        float texRatio = ((float)texW) / texH;
        Debug.LogFormat($"[Resize] screenRatio: {screenRatio}, texRatio: {texRatio}");
        var transSize = this.GetComponent<RectTransform>().rect;

        float screenWidth = Math.Max(transSize.width, transSize.height);
        float screenHeight = Math.Min(transSize.width, transSize.height);

        float fixedSizeX = screenWidth;
        float fixedSizeY = screenHeight;
        if (texRatio >= screenRatio)
        {
            // 以屏幕高度为最高值
            fixedSizeY = screenHeight;
            fixedSizeX = texRatio * fixedSizeY;
        }
        else
        {
            fixedSizeX = screenWidth;
            fixedSizeY = fixedSizeX / texRatio;
        }
        Debug.LogFormat($"[Resize] fixedSizeX: {fixedSizeX}, fixedSizeY: {fixedSizeY}");
        customBgImg.rectTransform.sizeDelta = new Vector2(fixedSizeX, fixedSizeY);
    }

}
