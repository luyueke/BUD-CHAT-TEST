using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Author:Jaywill
/// 分辨率适配器,需要结合CanvasScaler 使用
/// 仅给启动也用，其他地方禁用
/// </summary>
public class AppLanuchAutoFit : MonoBehaviour
{
    private CanvasScaler canvasScaler;

    void Awake()
    {
        canvasScaler = transform.GetComponentInChildren<CanvasScaler>(true);
        
        int screenWidth = Screen.width;
        int screenHeight = Screen.height;
        
        float screenRatio = (float)screenWidth / screenHeight;//当前屏幕宽高比
        float resulutionRation = canvasScaler.referenceResolution.x / canvasScaler.referenceResolution.y;//设计分辨率宽高比
        
        // 如果比设计分辨率宽度还长，则高度适配，否则宽度适配，保持纵横比
        if(screenRatio >= resulutionRation)
        {
            canvasScaler.matchWidthOrHeight = 1f;
        }
        else
        {
            canvasScaler.matchWidthOrHeight = 0f;
        }
    }
}

