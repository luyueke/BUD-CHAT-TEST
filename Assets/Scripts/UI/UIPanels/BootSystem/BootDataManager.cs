using Basic.Utils;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewbieTipsInfo
{
    public int id { get; set; }
    public Vec2 pos { get; set; }

    public List<TipsItemData> tipsItems;
    public bool hasEff = false;
    public bool hasCycleEff = false;
    public bool hasCudeEff = false;
    public bool hasHandEff = false;
}
public class TipsItemData
{
    public List<int> icon = null;
    public string text;
}
public class BootMaskInfo
{
    public int id { get; set; }
    public Vector2 pos { get; set; }

    public Vector2 size { get; set; }

    public int isClickClose = 0; //0为只能点击高亮区域，1为点击任意区域

    public int isRayTarget = 1; //0为不能穿透，1为可以穿透高亮区域

    public string maskColor = "";
}
public class BootMaskConfig
{
    public List<BootMaskInfo> configs;
}
public class NewbieTipsConfig
{
    public List<NewbieTipsInfo> configs;
}

public class BootDataManager : GlobalInstance<BootDataManager>
{
    private string maskConfgPath = "Assets/Loadable/UI/UIPanel/BootSystem/BootMaskConfig.json";
    private string tipsConfgPath = "Assets/Loadable/UI/UIPanel/BootSystem/NewBieTipsConfig.json";
    NewbieTipsConfig _newbieTipsConfigs;
    BootMaskConfig _bootMaskConfigs;

    public NewbieTipsConfig NewbieTipsConfigs
    {
        get
        {
            if (_newbieTipsConfigs == null)
            {
                var UIRoot = GameObject.Find("UIRoot");
                TextAsset textAsset = XAssetLoaderMgr.Inst.LoadResource<TextAsset>(tipsConfgPath, UIRoot);
                _newbieTipsConfigs = JsonConvert.DeserializeObject<NewbieTipsConfig>(textAsset.text);
            }

            return _newbieTipsConfigs;
        }
    }
    public BootMaskConfig BootMaskConfigs
    {
        get
        {
            if (_bootMaskConfigs == null)
            {
                var UIRoot = GameObject.Find("UIRoot");
                TextAsset textAsset = XAssetLoaderMgr.Inst.LoadResource<TextAsset>(maskConfgPath, UIRoot);
                _bootMaskConfigs = JsonConvert.DeserializeObject<BootMaskConfig>(textAsset.text);
            }

            return _bootMaskConfigs;
        }
    }

    public NewbieTipsInfo GetNewbieTipsInfo(int _id)
    {
        if (NewbieTipsConfigs.configs == null) return null;
        var result = NewbieTipsConfigs.configs.Find(x => x.id == _id);
        return result;
    }
    public BootMaskInfo GetBootMaskInfo(int _id)
    {

        if (BootMaskConfigs.configs == null) return null;
        var result = BootMaskConfigs.configs.Find(x => x.id == _id);
        return result;
    }
    public void CreateBoot(int id)
    {

    }

    public void SetPlayerIsOld()
    {
        PlayerPrefs.SetInt("firsOpenGame" + AccountDataManager.Inst.UserInfo.uid, 1);
        PlayerPrefs.SetInt("FirstClickFittingRoomItem" + AccountDataManager.Inst.Uid, 1);
        PlayerPrefs.SetInt("FirstOpenFittingRoomPanel" + AccountDataManager.Inst.UserInfo.uid, 1);
        PlayerPrefs.Save();
    }

    //局内引导是否进度
    public void SetParkInGame(int step) 
    {
        SaveGameUtil.Inst.SetIntByPlayerPrefs(SaveGameUtil.GameParkGuild,step);
    }
    public int GetParkInGame() 
    {
        return SaveGameUtil.Inst.GetIntByPlayerPrefs(SaveGameUtil.GameParkGuild);
    }
    //局外入口引导
    public void SetParkEntry(int bo)
    {
        SaveGameUtil.Inst.SetIntByPlayerPrefs(SaveGameUtil.GameParkEntry, bo);
    }
    public int GetParkEntry()
    {
        //return 1;
        return SaveGameUtil.Inst.GetIntByPlayerPrefs(SaveGameUtil.GameParkEntry);
    }
    //游戏开始入口引导
    public void SetParkStart(int bo)
    {
        SaveGameUtil.Inst.SetIntByPlayerPrefs(SaveGameUtil.GameParkStart, bo);
    }
    public int GetParkStart()
    {
        //return 1;
        return SaveGameUtil.Inst.GetIntByPlayerPrefs(SaveGameUtil.GameParkStart);
    }
}
