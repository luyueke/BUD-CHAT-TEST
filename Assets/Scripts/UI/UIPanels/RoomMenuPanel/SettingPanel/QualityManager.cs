using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using ShadowQuality = UnityEngine.ShadowQuality;

public class QualityManager : GlobalInstance<QualityManager>
{
    public enum QualityLevel
    {
        Low,
        High,
        None,
    }

    [Serializable]
    public class FpsInfo
    {
        public string mapId;
        public float avgFps;
        public int count;
        public QualityLevel quality;
    }

    [Serializable]
    public class QualityInfo
    {
        public string mapId;
        public QualityLevel quality;
    }
    
    private QualityLevel nowQualityLevel = QualityLevel.High;
    private List<FpsInfo> fpsInfos;
    private List<QualityInfo> qualityInfos;
    
    public static bool IsQualityLow() {
#if UNITY_EDITOR
        return false;
#endif

        var memory = SystemInfo.systemMemorySize / 1000f;
#if UNITY_ANDROID
        return memory < 6;
#else
        return memory < 3;
#endif
    }
    
    public void SetFps(int fps)
    {
        Application.targetFrameRate = fps;
    }
    
    public void SetTargetQualityShadow(bool isOpenShadow)
    {
      
        QualitySettings.shadows = isOpenShadow ? ShadowQuality.All:ShadowQuality.Disable;

        var pipelineAsset = GraphicsSettings.renderPipelineAsset as UniversalRenderPipelineAsset;
        pipelineAsset.shadowDistance = isOpenShadow ? 25f : 0f;
        GraphicsSettings.renderPipelineAsset = pipelineAsset;
    }
    
}
