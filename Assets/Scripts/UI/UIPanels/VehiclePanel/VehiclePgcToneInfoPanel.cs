using System;
using System.Collections;
using System.Collections.Generic;
using GameData.BaseInfo;
using UnityEngine;

public class VehiclePgcToneInfoPanel : MonoBehaviour
{
    #region 音效

    private readonly string StarGroupStr = "Vehicle_Start";

    private readonly string DriveGroupStr = "Vehicle_Drive";

    private readonly string HornGroupStr = "Vehicle_Whistle";

    private readonly List<string> WwiseStar = new List<string>() { "Play_Start_1P" , "Play_Start_3P" };

    private readonly List<string> WwiseDrive = new List<string>() { "Play_Drive_1P" , "Play_Drive_3P" };

    private readonly List<string> WwiseHorn = new List<string>() { "Play_Whistle_1P", "Play_Whistle_3P" };

    private readonly List<string> WwiseStop = new List<string>() { "Stop_Drive_1P", "Stop_Drive_3P", "Stop_Whistle_1P", "Stop_Whistle_3P" };


    private readonly List<string> StarSwitchList = new List<string>() { "Car_Start01", "Car_Start02", "Car_Start03", "Car_Start04", "Car_Start05" };

    private readonly List<string> DriveSwitchList = new List<string>() { "Car_Drive01", "Car_Drive02", "Car_Drive03", "Car_Drive04", "Car_Drive05" };

    private readonly List<string> HornSwitchList = new List<string>() { "Car_Whistle01", "Car_Whistle02", "Car_Whistle03", "Car_Whistle04", "Car_Whistle05" };

    private readonly List<string> StarNameList = new List<string>() { "汽车启动音效01", "汽车启动音效02", "火车启动音效", "飞机启动音效", "飞毯启动音效" };

    private readonly List<string> DriveNameList = new List<string>() { "汽车行驶音效01", "汽车行驶音效02", "火车行驶音效", "飞机行驶音效", "飞毯行驶音效" };

    private readonly List<string> HornNameList = new List<string>() { "喇叭音效01", "喇叭音效02", "喇叭音效03", "喇叭音效04", "喇叭音效05" };

    #endregion
    public Transform PgcToneContentParent;
    public VehiclePgcToneItem Prefab_PgcToneItem;
    
    private List<VehiclePgcToneItem> _pgcToneItems = new List<VehiclePgcToneItem>();
    private Action<VehicleAudioData> _onItemSelect;
    private bool isInit = false;

    private List<VehicleAudioData> _pgcToneDataList = new List<VehicleAudioData>();
    // private void Awake()
    // {
    //     InitPanel();
    // }

    public void OnSelectPanel()
    {
        if(!isInit)
            return;

        SetCurToneItemSelectState();
    }

    public void SetOnToneItemSelectAct(Action<VehicleAudioData> act)
    {
        this._onItemSelect = act;
    }

    private void OnToneItemSelectAct(VehicleAudioData toneInfo)
    {
        this._onItemSelect?.Invoke(toneInfo);
        _pgcToneItems.ForEach(x=>x.SetSelectState(false));
    }
    
    public string GetCurToneId()
    {
        string curToneId = null;
        var studioPanel = UIManager.Inst.FindPanel<VehicleAudioPanel>(PanelId.VehicleAudioPanel);
        if (studioPanel != null)
        {
            curToneId = studioPanel.GetCurToneId();
        }
        return curToneId;
    }

    public void InitPanel(VehicleAudioType audioType)
    {
        switch (audioType)
        {
            case VehicleAudioType.Star:
                InitStarAudioData();
                break;
            case VehicleAudioType.Drive:
                InitDriveAudioData();
                break;
            case VehicleAudioType.Horn:
                InitHornAudioData();
                break;
        }

        _pgcToneItems.Clear();
        foreach (var data in _pgcToneDataList)
        {
            VehiclePgcToneItem item = GameObject.Instantiate(Prefab_PgcToneItem, PgcToneContentParent);
            item.InitSelectMode(data, OnToneItemSelectAct);
            _pgcToneItems.Add(item);
        }

        SetCurToneItemSelectState();
        isInit = true;
    }

    private void InitStarAudioData()
    {
        _pgcToneDataList.Clear();
        for(int i = 0; i < StarSwitchList.Count; i++)
        {
            var ugcAudioData = new VehicleAudioData();
            ugcAudioData.wwiseInfo = new VehicleWwiseInfo();
            ugcAudioData.wwiseInfo.group = StarGroupStr;
            ugcAudioData.wwiseInfo.switchs = StarSwitchList[i];
            ugcAudioData.wwiseInfo.wwise = WwiseStar[0];
            ugcAudioData.wwiseInfo.wwise3P = WwiseStar[1];
            ugcAudioData.name = StarNameList[i];
            ugcAudioData.isUGC = false;
            ugcAudioData.type = 0;
            _pgcToneDataList.Add(ugcAudioData);
        }
    }

    private void InitDriveAudioData()
    {
        _pgcToneDataList.Clear();
        for(int i = 0; i < DriveSwitchList.Count; i++)
        {
            var ugcAudioData = new VehicleAudioData();
            ugcAudioData.wwiseInfo = new VehicleWwiseInfo();
            ugcAudioData.wwiseInfo.group = DriveGroupStr;
            ugcAudioData.wwiseInfo.switchs = DriveSwitchList[i];
            ugcAudioData.wwiseInfo.wwise = WwiseDrive[0];
            ugcAudioData.wwiseInfo.wwise3P = WwiseDrive[1];
            ugcAudioData.wwiseInfo.stopWwise = WwiseStop[0];
            ugcAudioData.wwiseInfo.stopWwise3P = WwiseStop[1];
            ugcAudioData.name = DriveNameList[i];
            ugcAudioData.isUGC = false;
            ugcAudioData.type = 1;
            _pgcToneDataList.Add(ugcAudioData);
        }
    }

    private void InitHornAudioData()
    {
        _pgcToneDataList.Clear();
        for(int i = 0; i < HornSwitchList.Count; i++)
        {
            var ugcAudioData = new VehicleAudioData();
            ugcAudioData.wwiseInfo = new VehicleWwiseInfo();
            ugcAudioData.wwiseInfo.group = HornGroupStr;
            ugcAudioData.wwiseInfo.switchs = HornSwitchList[i];
            ugcAudioData.wwiseInfo.wwise = WwiseHorn[0];
            ugcAudioData.wwiseInfo.wwise3P = WwiseHorn[1];
            ugcAudioData.wwiseInfo.stopWwise = WwiseStop[2];
            ugcAudioData.wwiseInfo.stopWwise3P = WwiseStop[3];
            ugcAudioData.name = HornNameList[i];
            ugcAudioData.isUGC = false;
            ugcAudioData.type = 2;
            _pgcToneDataList.Add(ugcAudioData);
        }
    }

    private void SetCurToneItemSelectState()
    {
        _pgcToneItems.ForEach(x =>
        {
            //x.SetSelectState(x.GetPgcId() == GetCurToneId(), false);
        });
    }

    private void OnDisable()
    {

    }
}
