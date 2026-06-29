using System.Collections;
using System.Collections.Generic;
using ChocDino.UIFX;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;

public class ProfileThemeManager : GlobalInstance<ProfileThemeManager>
{
    private const string basePath = "Assets/Loadable/UI/UIPanel/ProfileTheme/";
    private string confgPath = basePath + "ProfileThemeConfig.json";
    private string themeBgPath = basePath + "Themes/";
    public string ThemeAtlasPath = basePath + "ProfileTheme.spriteatlas";

    
    private ProfileThemeConfig _profileThemeConfig;

    public ProfileThemeConfig ProfileThemeConfig
    {
        get
        {
            if (_profileThemeConfig == null)
            {
                var UIRoot = GameObject.Find("UIRoot");
                TextAsset textAsset = XAssetLoaderMgr.Inst.LoadResource<TextAsset>(confgPath, UIRoot);
                _profileThemeConfig = JsonConvert.DeserializeObject<ProfileThemeConfig>(textAsset.text);
            }

            return _profileThemeConfig;
        }
    }

    public ProfileThemeInfo GetThemeInfo(int themeId)
    {
        if (ProfileThemeConfig.configs == null) return null;
        var result = ProfileThemeConfig.configs.Find(x => x.themeId == themeId);
        return result;
    }

    public string GetThemeBgPath(int themeId)
    {
        string path = themeBgPath + "Theme_" + themeId + "/" + "ThemeBg.prefab";
        return path;
    }
    public string GetThemeEffPath(int themeId)
    {
        string path = themeBgPath + "Theme_" + themeId + "/" + "ThemeEff.prefab";
        return path;
    }

    public bool IsNeedCardEffect(int themeId)
    {
        var result = ProfileThemeConfig.configs.Find(x => x.themeId == themeId);

        return result.hasEffect == 1;
    }
    public Sprite LoadThemeIcon(int themeId, GameObject refObj)
    {
        string spriteName = "theme_icon_" + themeId;
        return XAssetLoaderMgr.Inst.LoadSpriteInAltas(ThemeAtlasPath, spriteName, refObj);
    }
    public Sprite LoadThemeIcon(string pgcId, GameObject refObj)
    {
        string spriteName;
        switch (pgcId)
        {
            case "RewardHomepageSkin_1":
                spriteName = "theme_icon_9_show";
                break;
            default:

                spriteName = "";
                break;
        }
        
        return XAssetLoaderMgr.Inst.LoadSpriteInAltas(ThemeAtlasPath, spriteName, refObj);
    }

    public void ChangeTextColor(Transform root,string textColorStr,string borderColorStr = "", SuperTextMesh filter = null,List<Text> textfilters = null)
    {
        if (root == null) return;

        if (!string.IsNullOrEmpty(textColorStr))
        {
            Color textColor = DataUtil.DeSerializeColorCheckHash(textColorStr);
            var textList = root.GetComponentsInChildren<Text>(true);
            if (textList != null)
            {
                foreach (var text in textList)
                {
                    if(textfilters != null && textfilters.Contains(text))
                    {
                        continue;
                    }
                    text.color = textColor;
                }
            }
                
            var textMeshList = root.GetComponentsInChildren<SuperTextMesh>(true);
            if (textMeshList != null)
            {
                foreach (var text in textMeshList)
                {
                    if (text == filter)
                    {
                        continue;
                    }
                    text.color = textColor;
                }
            }
        }

        if (!string.IsNullOrEmpty(borderColorStr))
        {
            Color color = DataUtil.DeSerializeColorCheckHash(borderColorStr);
            var outLines = root.GetComponentsInChildren<OutlineFilter>(true);
            if (outLines != null)
            {
               
                foreach (var outline in outLines)
                {
                    bool exist = false;
                    if (textfilters != null)
                    {
                        for (int i = 0; i < textfilters.Count; i++)
                        {
                            if (outline.gameObject == textfilters[i].gameObject)
                            {
                                exist = true;
                                continue;
                            }
                        }
                    }
                    if (exist)
                    {
                        continue;
                    }
                    outline.Color = color;
                }
            }
            
            var shadows = root.GetComponentsInChildren<DropShadowFilter>(true);
            if (shadows != null)
            {
                foreach (var shadow in shadows)
                {
                    bool exist = false;
                    if (textfilters != null)
                    {
                        for (int i = 0; i < textfilters.Count; i++)
                        {
                            if (shadow.gameObject == textfilters[i].gameObject)
                            {
                                exist = true;
                                continue;
                            }
                        }
                    }
                    if(exist)
                    {
                        continue;
                    }
                    shadow.Color = color;
                }
            }
        }


    }
}
