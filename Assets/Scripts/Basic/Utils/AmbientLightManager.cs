using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AmbientLightSetting
{
    public Color ambientLight;
    public Color ambientSkyColor;
    public Color ambientEquatorColor;
    public Color ambientGroundColor;
    public float reflectionIntensity;
}

public enum LightAbility
{
    Hall,
    Game,
    Preview,
}

public class PreviewLightRecordData
{
    public bool SrcHallLightVisible;
    public bool SrcGameSceneLightVisible;
    public bool SrcPreviewSceneLightVisible;
    public AmbientLightSetting SrcLightSetting;
}

public class AmbientLightManager : GlobalInstance<AmbientLightManager>
{
    private AmbientLightSetting _srcLightSetting;
    private GameObject _hallDirectionLight;
    
    private GameObject _gameSceneDirectionLight;
    private GameObject _previewPanelDirectionLight;
    
    //禁用列表
    private Dictionary<LightAbility, List<string>> noAbilityDict = new Dictionary<LightAbility, List<string>>();

    private AmbientLightSetting uiDefaultSetting = new AmbientLightSetting
    {
        ambientLight = new Color(0.82f, 0.82f, 0.82f),
        ambientSkyColor = new Color(0.82f, 0.82f, 0.82f),
        ambientEquatorColor = new Color(0.94f, 0.94f, 0.94f),
        ambientGroundColor = new Color(1f, 0.57f, 0.54f),
        reflectionIntensity = 0
    };

    public static AmbientLightSetting EditLightSetting = new AmbientLightSetting
    {
        ambientLight = new Color(0.722f, 0.8f, 0.922f),
        ambientSkyColor = new Color(0.722f, 0.8f, 0.922f),
        ambientEquatorColor = new Color(0.82f, 0.8f, 0.769f),
        ambientGroundColor = new Color(0.039f, 0.369f, 0.639f),
        reflectionIntensity = 0.5f
    };

    public void DisableLight(LightAbility light,string callKey)
    {
        if (!noAbilityDict.ContainsKey(light))
        {
            noAbilityDict.Add(light,new List<string>());
        }
        noAbilityDict[light].Add(callKey);
        RefreshLight(light);
    }

    public void EnableLight(LightAbility light, string callKey)
    {
        if (noAbilityDict.ContainsKey(light))
        {
            noAbilityDict[light].Remove(callKey);
            RefreshLight(light);
        }
    }

    private void RefreshLight(LightAbility light)
    {
        int count = 0;
        if (noAbilityDict.ContainsKey(light) && noAbilityDict[light] != null)
        {
            count = noAbilityDict[light].Count;
        }

        switch (light)
        {
            case LightAbility.Hall:
                var hallLight = GetHallDirectionLight();
                if (hallLight)
                {
                    hallLight.SetActive(count <= 0);
                }
                break;
            case LightAbility.Game:
                break;
            case LightAbility.Preview:
                break;
        }
    }


    /// <summary>
    /// 打开UI 灯光
    /// </summary>
    /// <param name="customSetting">自定义灯光参数</param>
    /// <returns>原灯光</returns>
    public AmbientLightSetting OpenUILight(AmbientLightSetting customSetting = null)
    {
        if (customSetting == null)
        {
            customSetting = uiDefaultSetting;
        }

        AmbientLightSetting srcSetting = new AmbientLightSetting();
        srcSetting.ambientLight = RenderSettings.ambientLight;
        srcSetting.ambientSkyColor = RenderSettings.ambientSkyColor;
        srcSetting.ambientEquatorColor = RenderSettings.ambientEquatorColor;
        srcSetting.ambientGroundColor = RenderSettings.ambientGroundColor;
        srcSetting.reflectionIntensity = RenderSettings.reflectionIntensity;
        
        _srcLightSetting = srcSetting;
        
        RenderSettings.ambientLight = customSetting.ambientLight;
        RenderSettings.ambientSkyColor = customSetting.ambientSkyColor;
        RenderSettings.ambientEquatorColor = customSetting.ambientEquatorColor;
        RenderSettings.ambientGroundColor = customSetting.ambientGroundColor;
        RenderSettings.reflectionIntensity = customSetting.reflectionIntensity;
        
        return _srcLightSetting;
    }
    
    /// <summary>
    /// 关闭UI灯光且回复原灯光
    /// </summary>
    /// <param name="srcSetting"></param>
    public void CloseUILight(AmbientLightSetting srcSetting = null)
    {
        if (srcSetting == null && _srcLightSetting == null)
        {
            return;
        }
        
        if (srcSetting == null)
        {
            srcSetting = _srcLightSetting;
        }

        RenderSettings.ambientLight = srcSetting.ambientLight;
        RenderSettings.ambientSkyColor = srcSetting.ambientSkyColor;
        RenderSettings.ambientEquatorColor = srcSetting.ambientEquatorColor;
        RenderSettings.ambientGroundColor = srcSetting.ambientGroundColor;
        RenderSettings.reflectionIntensity = srcSetting.reflectionIntensity;
        
        _srcLightSetting = null;
    }


    public GameObject GetHallDirectionLight()
    {
        if (_hallDirectionLight == null)
        {
            _hallDirectionLight = GameObject.Find("HallDirectionalLight");
        }

        return _hallDirectionLight;
    }
    
    public GameObject GetGameSceneDirectionLight()
    {
        if (_gameSceneDirectionLight == null)
        {
            _gameSceneDirectionLight = GameObject.Find("MapSceneDirectional Light");
        }

        return _gameSceneDirectionLight;
    }

    private void SetHallLightVisible(bool value)
    {
        var hallLight = GetHallDirectionLight();
        if (hallLight != null)
        {
            hallLight.SetActive(value);
        }
    }
    
    //返回原大厅灯光状态
    public bool HideHallLight()
    {
        bool srcActive = false;
        var light = GetHallDirectionLight();
        if (light != null)
        {
            srcActive = light.activeInHierarchy;
        }
        SetHallLightVisible(false);
        return srcActive;
    }

    public void RevertHallLight(bool value)
    {
        SetHallLightVisible(value);
    }
    
    //返回原游戏场景灯光状态
    public bool HideGameSceneLight()
    {
        bool srcActive = false;
        var light = GetGameSceneDirectionLight();
        if (light != null)
        {
            srcActive = light.activeInHierarchy;
        }
        SetGameSceneLightVisible(false);
        return srcActive;
    }

    public void RevertGameSceneLight(bool value)
    {
        SetGameSceneLightVisible(value);
    }
    
    private void SetGameSceneLightVisible(bool value)
    {
        var gameSceneLight = GetGameSceneDirectionLight();
        if (gameSceneLight != null)
        {
            gameSceneLight.SetActive(value);
        }
    }

    //显示预览直射灯 - 关闭当前场景直射灯
    public bool ShowPreviewDirLight()
    {
        bool srcActive = false;

        if (_previewPanelDirectionLight != null)
        {
            srcActive = _previewPanelDirectionLight.activeInHierarchy;
        }
        else
        {
            _previewPanelDirectionLight = Loader.Load<GameObject>("Assets/Loadable/UI/UIPanel/PropStorePanel/PreviewPanelDirectionalLight.prefab").Instantiate();
        }
        _previewPanelDirectionLight.SetActive(true);
        
        return srcActive;
    }
    
    //显示预览直射灯 - 关闭当前场景直射灯
    public bool HidePreviewDirLight()
    {
        bool srcActive = false;

        if (_previewPanelDirectionLight != null)
        {
            srcActive = _previewPanelDirectionLight.activeInHierarchy;
            _previewPanelDirectionLight.SetActive(false);
        }
        return srcActive;
    }

    public void RevertPreviewLight(bool value)
    {
        if (_previewPanelDirectionLight != null)
        {
            _previewPanelDirectionLight.gameObject.SetActive(value);
        }
    }

    public PreviewLightRecordData OpenPreviewDirLight(AmbientLightSetting customSetting = null)
    {
        PreviewLightRecordData data = new PreviewLightRecordData();

        data.SrcPreviewSceneLightVisible = ShowPreviewDirLight();
        data.SrcLightSetting = OpenUILight(customSetting);
        data.SrcHallLightVisible = HideHallLight();
        data.SrcGameSceneLightVisible = HideGameSceneLight();
        
        return data;
    }

    public void ClosePreviewDirLight(PreviewLightRecordData data)
    {
        AmbientLightManager.Inst.CloseUILight(data.SrcLightSetting);
        AmbientLightManager.Inst.RevertHallLight(data.SrcHallLightVisible);
        AmbientLightManager.Inst.RevertGameSceneLight(data.SrcGameSceneLightVisible);
        AmbientLightManager.Inst.RevertPreviewLight(data.SrcPreviewSceneLightVisible);
    }
}
