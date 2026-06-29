using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.U2D;


public enum SpriteAtlasType
{
    Common,
    MatIconSprite,
    EditorPropSprite,
    PgcPropSprite,
    GashaponIconSprite,
    ColorBackground,
    PgcEmoteSprite,
    ClassSprite,
    RewardAtlas,
    Bundle,
}
public class XAssetLoaderMgr : GlobalInstance<XAssetLoaderMgr>
{
    public Dictionary<SpriteAtlasType, string> AtlasPathDict = new Dictionary<SpriteAtlasType, string>()
    {
        {SpriteAtlasType.MatIconSprite,"Assets/Loadable/UI/SpriteAltas/BaseMatIcon.spriteatlas"},
        {SpriteAtlasType.EditorPropSprite,"Assets/Loadable/UI/SpriteAltas/EditorPropIcon.spriteatlas"},
        {SpriteAtlasType.PgcPropSprite ,"Assets/Loadable/UI/SpriteAltas/PgcPropIcon.spriteatlas"},
        {SpriteAtlasType.GashaponIconSprite ,"Assets/Loadable/UI/SpriteAltas/GashaponIcon.spriteatlas"},
        {SpriteAtlasType.ColorBackground ,"Assets/Loadable/UI/SpriteAltas/ColorBgIcon.spriteatlas"},
        {SpriteAtlasType.PgcEmoteSprite ,"Assets/Loadable/UI/SpriteAltas/PgcEmoteIcon.spriteatlas"},
        {SpriteAtlasType.Common, "Assets/Loadable/UI/UIPanel/CommonSprite/CommonSprite.spriteatlas"},
        {SpriteAtlasType.ClassSprite ,"Assets/Loadable/UI/SpriteAltas/ClassIcon.spriteatlas"},
        {SpriteAtlasType.RewardAtlas ,"Assets/Loadable/UI/UIPanel/CommonBgPanel/CommonBgIcon.spriteatlas"},
        {SpriteAtlasType.Bundle ,"Assets/Loadable/UI/SpriteAltas/Bundle.spriteatlas"}
    };

    public T LoadResource<T>(string path, GameObject refObj) where T : UnityEngine.Object
    {
        var pack = Loader.Load<T>(path);
        if (pack != null && refObj != null)
        {
            return pack.RetainAsset(refObj);
        }
        LoggerUtils.Log("XAssetLoaderMgr Res Not Found  " + typeof(T) + "|| Path = " + path);
        return null;
    }

    public Sprite LoadSpriteInAltas(string atlasPath, string spriteName, GameObject refObj)
    {
        var atlas = LoadResource<SpriteAtlas>(atlasPath, refObj);
        if (atlas != null)
        {
            var sp = atlas.GetSprite(spriteName);
            if (sp == null) {
                LoggerUtils.LogError("XAssetLoaderMg LoadSpriteInAltas Not Found || Name = " + spriteName);
            }
            return sp;
        }
        LoggerUtils.LogError("XAssetLoaderMg LoadSpriteInAltas Not Found || Name = " + spriteName);
        return null;
    }

    public Sprite LoadSpriteInAltas(SpriteAtlasType type, string spriteName, GameObject refObj)
    {
        AtlasPathDict.TryGetValue(type, out string atlasPath);
        var atlas = LoadResource<SpriteAtlas>(atlasPath, refObj);
        if (atlas != null)
        {
            var sp = atlas.GetSprite(spriteName);
            if (sp == null) {
                LoggerUtils.LogError("XAssetLoaderMg LoadSpriteInAltas Not Found || Name = " + spriteName);
            }
            return sp;
        }
        LoggerUtils.LogError("XAssetLoaderMg LoadSpriteInAltas Not Found || Name = " + spriteName);
        return null;
    }

    public string GetSpriteAltasPath(SpriteAtlasType type)
    {
        if (AtlasPathDict.ContainsKey(type))
        {
            return AtlasPathDict[type];
        }
        throw new MissingReferenceException("XAssetLoaderMg GetSpriteAltasPath Not Found || Type = " + type );
    }

    public void LoadSpriteInAltasAsync(string atlasPath, string spriteName, GameObject refObj, Action<Sprite> onComplete)
    {
        Loader.LoadAsync<SpriteAtlas>(atlasPath, (isSuc, warpper) =>
        {
            if (isSuc && warpper != null)
            {
                if (refObj == null)
                {
                    LoggerUtils.Log("LoadSpriteInAltasAsync RefObj Is Null");
                    onComplete?.Invoke(null);
                    return;
                }

                var atlas = warpper.RetainAsset(refObj);
                var sp = atlas.GetSprite(spriteName);
                onComplete?.Invoke(sp);
            }
            else
            {
                if (refObj == null)
                {
                    LoggerUtils.LogError("LoadSpriteInAltasAsync RefObj Is Null");
                }
                onComplete?.Invoke(null);
            }
        });
    }
}
