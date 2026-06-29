using System;
using System.Collections;
using System.Collections.Generic;
using Es;
using Game.Audio;
using Game.Avatar;
using Game.Base;
using Game.MapSetting;
using Game.Props.PropsComponents;
using GameData.Base;
using GameData.BaseInfo;
using UI.Base;
using UI.BaseWidgets;
using UI.UIPanels.GameEdit.SettingView;
//using UIAgent;
using UnityEngine;
using UnityEngine.UI;

public class VehicleAudioPanel : BasePanel<VehicleAudioPanel>
{
    public CButton Btn_Close;
    public CButton Btn_Confirm;
    public Toggle Tog_Pgc;
    public Toggle Tog_Created;
    public Toggle Tog_Owned;

    public VehiclePgcToneInfoPanel PgcTonePanel;
    public UgcAnimUgcToneInfoPanel CreatedPanel;
    public UgcAnimUgcToneInfoPanel OwnedPanel;

    private AnimMusicInfo _curChooseBgmMusicInfo = new AnimMusicInfo();
    //private ChooseUgcAnimBgmData panelData;
    private VehicleAudioType _audioType;
    private VehicleInfo _vehicleInfo;
    private VehicleAudioData _curChooseAudioData;
    private GameObject _globalSoundObj;

    public  Transform vipObj1;
    public  Transform vipObj2;
    public enum ToneStudioType
    {
        PGC = 0,
        Created = 1,
        Owned = 2,
    }
    public override void OnCreate()
    {
        base.OnCreate();
        AddListener();
        _globalSoundObj = GameObject.Find("GlobalMainCamera");
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        _vehicleInfo = (VehicleInfo)args[0];
        _audioType = (VehicleAudioType)args[1];
        Tog_Pgc.isOn = true;
        OnSelectView(ToneStudioType.PGC);
        PgcTonePanel.InitPanel(_audioType);
        vipObj1.gameObject.SetActive(_audioType == VehicleAudioType.Horn);
        vipObj2.gameObject.SetActive(_audioType == VehicleAudioType.Horn);
    }

    private void AddListener()
    {
        PgcTonePanel.SetOnToneItemSelectAct(OnMusicItemClick);
        CreatedPanel.SetOnToneItemSelectAct(OnCreateOrOwnMusicItemClick);
        OwnedPanel.SetOnToneItemSelectAct(OnCreateOrOwnMusicItemClick);
        
        Btn_Close.onClick.AddListener(()=>{
            StopPlayAudio();
            CloseSelf();
        });
        Btn_Confirm.onClick.AddListener(OnBtnConfirmClick);
        Tog_Pgc.onValueChanged.AddListener((isOn) =>
        {
            if (isOn)
            {
                StopPlayAudio();
                OnSelectView(ToneStudioType.PGC);
            }
        });
        Tog_Created.onValueChanged.AddListener((isOn) =>
        {
            if (isOn)
            {
                StopPlayAudio();
                OnSelectView(ToneStudioType.Created);
            }
        });
        Tog_Owned.onValueChanged.AddListener((isOn) =>
        {
            if (isOn)
            {
                StopPlayAudio();
                OnSelectView(ToneStudioType.Owned);
            }
        });
    }

    private void OnCreateOrOwnMusicItemClick(AnimMusicInfo info)
    {
        if (_audioType == VehicleAudioType.Horn)
        {
            if (CanUseVip() == false)
            {
                return;
            }
        }
        OnMusicItemClick(info);
    }
    private void OnMusicItemClick(AnimMusicInfo info)
    {

        _curChooseBgmMusicInfo = info;

        // 将 AnimMusicInfo 统一转换为 VehicleAudioData，后续确认逻辑只使用 _curChooseAudioData
        _curChooseAudioData = ConvertAnimMusicInfoToVehicleAudioData(info);
        
        if(_curChooseBgmMusicInfo != null)
            Btn_Confirm.gameObject.SetActive(true);
    }

    private void OnMusicItemClick(VehicleAudioData info)
    {
        _curChooseAudioData = info;
        if(_curChooseAudioData != null)
        {
            Btn_Confirm.gameObject.SetActive(true);
        }
    }

    private void OnBtnConfirmClick()
    {
        if (_vehicleInfo == null)
        {
            return;
        }
        if (_vehicleInfo.vehicleAudio == null)
        {
            _vehicleInfo.vehicleAudio = new VehicleAudioDetailInfo();
        }
        if (_curChooseAudioData == null)
        {
            return;
        }

        // UGC（URL）与 PGC（Wwise）二选一：写入一种时清理另一种，避免逻辑侧判断歧义
        var hasUgcUrl = _curChooseAudioData.isUGC && !string.IsNullOrEmpty(_curChooseAudioData.MusicUrl);
        switch (_audioType)
        {
            case VehicleAudioType.Star:
                if (hasUgcUrl)
                {
                    _vehicleInfo.vehicleAudio.starUrl = _curChooseAudioData.MusicUrl;
                    _vehicleInfo.vehicleAudio.starWwise = null;
                }
                else
                {
                    _vehicleInfo.vehicleAudio.starUrl = "";
                    _vehicleInfo.vehicleAudio.starWwise = _curChooseAudioData.wwiseInfo;
                }
                break;
            case VehicleAudioType.Drive:
                if (hasUgcUrl)
                {
                    _vehicleInfo.vehicleAudio.driveUrl = _curChooseAudioData.MusicUrl;
                    _vehicleInfo.vehicleAudio.driveWwise = null;
                }
                else
                {
                    _vehicleInfo.vehicleAudio.driveUrl = "";
                    _vehicleInfo.vehicleAudio.driveWwise = _curChooseAudioData.wwiseInfo;
                }
                break;
            case VehicleAudioType.Horn:
                if (hasUgcUrl)
                {
                    _vehicleInfo.vehicleAudio.hornUrl = _curChooseAudioData.MusicUrl;
                    _vehicleInfo.vehicleAudio.hornWwise = null;
                }
                else
                {
                    _vehicleInfo.vehicleAudio.hornUrl = "";
                    _vehicleInfo.vehicleAudio.hornWwise = _curChooseAudioData.wwiseInfo;
                }
                break;
        }
        StopPlayAudio();
        CloseSelf();
    }

    private VehicleAudioData ConvertAnimMusicInfoToVehicleAudioData(AnimMusicInfo info)
    {
        if (info == null)
        {
            return null;
        }

        var type = _audioType switch
        {
            VehicleAudioType.Star => 0,
            VehicleAudioType.Drive => 1,
            VehicleAudioType.Horn => 2,
            _ => 0
        };

        // 约定：AnimMusicInfo（Created/Owned）走 URL 音频；PgcTonePanel 走 Wwise 音效
        return new VehicleAudioData
        {
            name = info.name,
            type = type,
            isUGC = info.isPgc != 1,
            wwiseInfo = null,
            MusicUrl = info.metaDataUrl,
        };
    }

    private void OnSelectView(ToneStudioType studioType)
    {
        switch (studioType)
        {
            case ToneStudioType.PGC:
                PgcTonePanel.gameObject.SetActive(true);
                CreatedPanel.gameObject.SetActive(false);
                OwnedPanel.gameObject.SetActive(false);
                break;
            
            case ToneStudioType.Created:
                PgcTonePanel.gameObject.SetActive(false);
                CreatedPanel.gameObject.SetActive(true);
                OwnedPanel.gameObject.SetActive(false);
                CreatedPanel.GetPublishedData();
                break;
            
            case ToneStudioType.Owned:
                PgcTonePanel.gameObject.SetActive(false);
                CreatedPanel.gameObject.SetActive(false);
                OwnedPanel.gameObject.SetActive(true);
                OwnedPanel.GetPublishedData();
                break;

        }
    }

    private void StopPlayAudio()
    {
        AkSoundManager.Inst.StopGameMusicNode();
        if(_curChooseAudioData != null && _curChooseAudioData.wwiseInfo != null)
        {
            AkSoundManager.Inst.StopSound(_curChooseAudioData.wwiseInfo.stopWwise, _globalSoundObj);
        }
    }

    public void RefreshOwnedList()
    {
        if (Tog_Owned.isOn)
        {
            OnSelectView(ToneStudioType.Owned);
        }
        else
        {
            Tog_Owned.isOn = true;
        }
    }

    public string GetCurToneId()
    {
        return _curChooseBgmMusicInfo?.id;
    }
    // [SerializeField] private Transform StartAudioRoot;

    // [SerializeField] private Transform RunAudioRoot;

    // [SerializeField] private Transform HornAudioRoot;

    // [SerializeField] private GameObject VehicleAudioItems;

    // public CButton CloseBtn;

    // public CButton EnterBtn;

    // private readonly List<VehicleAudioItem> StarAudioList = new List<VehicleAudioItem>();

    // private readonly List<VehicleAudioItem> RunAudioList = new List<VehicleAudioItem>();

    // private readonly List<VehicleAudioItem> HornAudioList = new List<VehicleAudioItem>();

    // private int _audioLimit = 60;

    // private VehicleAudioItem selectStarItem;

    // private VehicleAudioItem selectDriveItem;

    // private VehicleAudioItem selectHornItem;

    // private VehicleInfo vehicleInfo;

    // private GameObject _globalSoundObj;

    // #region 音效

    // private readonly string StarGroupStr = "Vehicle_Start";

    // private readonly string DriveGroupStr = "Vehicle_Drive";

    // private readonly string HornGroupStr = "Vehicle_Whistle";

    // private readonly List<string> WwiseStar = new List<string>() { "Play_Start_1P" , "Play_Start_3P" };

    // private readonly List<string> WwiseDrive = new List<string>() { "Play_Drive_1P" , "Play_Drive_3P" };

    // private readonly List<string> WwiseHorn = new List<string>() { "Play_Whistle_1P", "Play_Whistle_3P" };

    // private readonly List<string> WwiseStop = new List<string>() { "Stop_Drive_1P", "Stop_Drive_3P", "Stop_Whistle_1P", "Stop_Whistle_3P" };


    // private readonly List<string> StarSwitchList = new List<string>() { "Car_Start01", "Car_Start02", "Car_Start03", "Car_Start04", "Car_Start05" };

    // private readonly List<string> DriveSwitchList = new List<string>() { "Car_Drive01", "Car_Drive02", "Car_Drive03", "Car_Drive04", "Car_Drive05" };

    // private readonly List<string> HornSwitchList = new List<string>() { "Car_Whistle01", "Car_Whistle02", "Car_Whistle03", "Car_Whistle04", "Car_Whistle05" };

    // private readonly List<string> StarNameList = new List<string>() { "汽车启动音效01", "汽车启动音效02", "火车启动音效", "飞机启动音效", "飞毯启动音效" };

    // private readonly List<string> DriveNameList = new List<string>() { "汽车行驶音效01", "汽车行驶音效02", "火车行驶音效", "飞机行驶音效", "飞毯行驶音效" };

    // private readonly List<string> HornNameList = new List<string>() { "喇叭音效01", "喇叭音效02", "喇叭音效03", "喇叭音效04", "喇叭音效05" };

    // #endregion

    // protected override void Awake()
    // {
    //     base.Awake();
    //     CloseBtn.onClick.AddListener(EnterAudioEvent);
    //     EnterBtn.onClick.AddListener(EnterAudioEvent);

    //     _globalSoundObj = GameObject.Find("GlobalMainCamera");

    //     var audioItem = Instantiate(VehicleAudioItems, StartAudioRoot).GetComponent<VehicleAudioItem>();
    //     var vehicleAudioData = new VehicleAudioData();
    //     vehicleAudioData.isUGC = true;
    //     vehicleAudioData.type = 0;
    //     audioItem.SetData(vehicleAudioData, OnItemSelected);
    //     audioItem.SetDeleteUGCMusic(OnDeleteUGCMusic);
    //     StarAudioList.Add(audioItem);

    //     for(int i = 0; i < StarSwitchList.Count; i++)
    //     {
    //         var item = Instantiate(VehicleAudioItems, StartAudioRoot);
    //         var vehicleItem = item.GetComponent<VehicleAudioItem>();
    //         var ugcAudioData = new VehicleAudioData();
    //         ugcAudioData.wwiseInfo = new VehicleWwiseInfo();
    //         ugcAudioData.wwiseInfo.group = StarGroupStr;
    //         ugcAudioData.wwiseInfo.switchs = StarSwitchList[i];
    //         ugcAudioData.wwiseInfo.wwise = WwiseStar[0];
    //         ugcAudioData.wwiseInfo.wwise3P = WwiseStar[1];
    //         ugcAudioData.name = StarNameList[i];
    //         ugcAudioData.isUGC = false;
    //         ugcAudioData.type = 0;
    //         vehicleItem.SetData(ugcAudioData, OnItemSelected);
    //         StarAudioList.Add(vehicleItem);
    //     }

    //     var driveAudioItem = Instantiate(VehicleAudioItems, RunAudioRoot).GetComponent<VehicleAudioItem>();
    //     var driveAudioData = new VehicleAudioData();
    //     driveAudioData.isUGC = true;
    //     driveAudioData.type = 1;
    //     driveAudioItem.SetData(driveAudioData, OnItemSelected);
    //     driveAudioItem.SetDeleteUGCMusic(OnDeleteUGCMusic);
    //     RunAudioList.Add(driveAudioItem);

    //     for(int i = 0; i < DriveSwitchList.Count; i++)
    //     {
    //         var item = Instantiate(VehicleAudioItems, RunAudioRoot);
    //         var vehicleItem = item.GetComponent<VehicleAudioItem>();
    //         var ugcAudioData = new VehicleAudioData();
    //         ugcAudioData.wwiseInfo = new VehicleWwiseInfo();
    //         ugcAudioData.wwiseInfo.group = DriveGroupStr;
    //         ugcAudioData.wwiseInfo.switchs = DriveSwitchList[i];
    //         ugcAudioData.wwiseInfo.wwise = WwiseDrive[0];
    //         ugcAudioData.wwiseInfo.wwise3P = WwiseDrive[1];
    //         ugcAudioData.wwiseInfo.stopWwise = WwiseStop[0];
    //         ugcAudioData.wwiseInfo.stopWwise3P = WwiseStop[1];
    //         ugcAudioData.name = DriveNameList[i];
    //         ugcAudioData.isUGC = false;
    //         ugcAudioData.type = 1;
    //         vehicleItem.SetData(ugcAudioData, OnItemSelected);
    //         RunAudioList.Add(vehicleItem);
    //     }

    //     var hornAudioItem = Instantiate(VehicleAudioItems, HornAudioRoot).GetComponent<VehicleAudioItem>();
    //     var hornAudioData = new VehicleAudioData();
    //     hornAudioData.isUGC = true;
    //     hornAudioData.type = 2;
    //     hornAudioItem.SetData(hornAudioData, OnItemSelected,true);
    //     hornAudioItem.SetDeleteUGCMusic(OnDeleteUGCMusic);
    //     HornAudioList.Add(hornAudioItem);

    //     for(int i = 0; i < HornSwitchList.Count; i++)
    //     {
    //         var item = Instantiate(VehicleAudioItems, HornAudioRoot);
    //         var vehicleItem = item.GetComponent<VehicleAudioItem>();
    //         var ugcAudioData = new VehicleAudioData();
    //         ugcAudioData.wwiseInfo = new VehicleWwiseInfo();
    //         ugcAudioData.wwiseInfo.group = HornGroupStr;
    //         ugcAudioData.wwiseInfo.switchs = HornSwitchList[i];
    //         ugcAudioData.wwiseInfo.wwise = WwiseHorn[0];
    //         ugcAudioData.wwiseInfo.wwise3P = WwiseHorn[1];
    //         ugcAudioData.wwiseInfo.stopWwise = WwiseStop[2];
    //         ugcAudioData.wwiseInfo.stopWwise3P = WwiseStop[3];
    //         ugcAudioData.name = HornNameList[i];
    //         ugcAudioData.isUGC = false;
    //         ugcAudioData.type = 2;
    //         vehicleItem.SetData(ugcAudioData, OnItemSelected);
    //         HornAudioList.Add(vehicleItem);
    //     }
    // }

    // public override void OnShow(params object[] args)
    // {
    //     if (args != null && args.Length > 0)
    //     {
    //         vehicleInfo = (VehicleInfo)args[0];
    //         if(vehicleInfo.vehicleAudio == null)
    //         {
    //             vehicleInfo.vehicleAudio = new VehicleAudioDetailInfo();
    //         }

    //         ResetAllSelectionVisuals();
    //         InitSelectionByVehicleInfo();
    //     }
    // }

    // private void ResetAllSelectionVisuals()
    // {
    //     foreach (var item in StarAudioList)
    //     {
    //         item.Setselect(false, false);
    //     }
    //     foreach (var item in RunAudioList)
    //     {
    //         item.Setselect(false, false);
    //     }
    //     foreach (var item in HornAudioList)
    //     {
    //         item.Setselect(false, false);
    //     }

    //     selectStarItem = null;
    //     selectDriveItem = null;
    //     selectHornItem = null;
    // }

    // private void InitSelectionByVehicleInfo()
    // {
    //     if (vehicleInfo == null || vehicleInfo.vehicleAudio == null)
    //     {
    //         return;
    //     }

    //     // 启动音效
    //     if (!string.IsNullOrEmpty(vehicleInfo.vehicleAudio.starUrl))
    //     {
    //         var ugcItem = StarAudioList[0];
    //         ugcItem.GetData().MusicUrl = vehicleInfo.vehicleAudio.starUrl;
    //         ugcItem.RefreshData();
    //         selectStarItem = ugcItem;
    //         selectStarItem.Setselect(true);
    //     }
    //     else
    //     {
    //         StarAudioList[0].GetData().MusicUrl = null;
    //         StarAudioList[0].RefreshData();

    //         selectStarItem = FindWwiseMatchOrDefault(StarAudioList, vehicleInfo.vehicleAudio.starWwise);
    //         selectStarItem.Setselect(true);
    //         if (vehicleInfo.vehicleAudio.starWwise == null)
    //         {
    //             vehicleInfo.vehicleAudio.starWwise = selectStarItem.GetData().wwiseInfo;
    //         }
    //     }

    //     // 行驶音效
    //     if (!string.IsNullOrEmpty(vehicleInfo.vehicleAudio.driveUrl))
    //     {
    //         var ugcItem = RunAudioList[0];
    //         ugcItem.GetData().MusicUrl = vehicleInfo.vehicleAudio.driveUrl;
    //         ugcItem.RefreshData();
    //         selectDriveItem = ugcItem;
    //         selectDriveItem.Setselect(true);
    //     }
    //     else
    //     {
    //         RunAudioList[0].GetData().MusicUrl = null;
    //         RunAudioList[0].RefreshData();

    //         selectDriveItem = FindWwiseMatchOrDefault(RunAudioList, vehicleInfo.vehicleAudio.driveWwise);
    //         selectDriveItem.Setselect(true);
    //         if (vehicleInfo.vehicleAudio.driveWwise == null)
    //         {
    //             vehicleInfo.vehicleAudio.driveWwise = selectDriveItem.GetData().wwiseInfo;
    //         }
    //     }

    //     // 喇叭音效
    //     if (!string.IsNullOrEmpty(vehicleInfo.vehicleAudio.hornUrl))
    //     {
    //         var ugcItem = HornAudioList[0];
    //         ugcItem.GetData().MusicUrl = vehicleInfo.vehicleAudio.hornUrl;
    //         ugcItem.RefreshData();
    //         selectHornItem = ugcItem;
    //         selectHornItem.Setselect(true);
    //     }
    //     else
    //     {
    //         HornAudioList[0].GetData().MusicUrl = null;
    //         HornAudioList[0].RefreshData();

    //         selectHornItem = FindWwiseMatchOrDefault(HornAudioList, vehicleInfo.vehicleAudio.hornWwise);
    //         selectHornItem.Setselect(true);
    //         if (vehicleInfo.vehicleAudio.hornWwise == null)
    //         {
    //             vehicleInfo.vehicleAudio.hornWwise = selectHornItem.GetData().wwiseInfo;
    //         }
    //     }
    // }

    // private VehicleAudioItem FindWwiseMatchOrDefault(List<VehicleAudioItem> list, VehicleWwiseInfo wwise)
    // {
    //     int defaultIndex = list.Count > 1 ? 1 : 0;
    //     if (wwise == null)
    //     {
    //         return list[defaultIndex];
    //     }

    //     for (int i = 1; i < list.Count; i++)
    //     {
    //         var data = list[i].GetData();
    //         if (data?.wwiseInfo != null && data.wwiseInfo.switchs == wwise.switchs)
    //         {
    //             return list[i];
    //         }
    //     }
    //     return list[defaultIndex];
    // }

    // public void OnDeleteUGCMusic(VehicleAudioItem item)
    // {
    //     var audioData = item.GetData();
    //     //audioData.isUGC = false;
    //     switch (audioData.type)
    //     {
    //         case 0:
    //             selectStarItem = StarAudioList.Count > 1 ? StarAudioList[1] : StarAudioList[0];
    //             SetSelectItemStatus(selectStarItem);
    //             if (vehicleInfo?.vehicleAudio != null)
    //             {
    //                 vehicleInfo.vehicleAudio.starUrl = "";
    //                 vehicleInfo.vehicleAudio.starWwise = selectStarItem.GetData().wwiseInfo;
    //             }
    //             break;
    //         case 1:
    //             selectDriveItem = RunAudioList.Count > 1 ? RunAudioList[1] : RunAudioList[0];
    //             SetSelectItemStatus(selectDriveItem);
    //             if (vehicleInfo?.vehicleAudio != null)
    //             {
    //                 vehicleInfo.vehicleAudio.driveUrl = "";
    //                 vehicleInfo.vehicleAudio.driveWwise = selectDriveItem.GetData().wwiseInfo;
    //             }
    //             break;
    //         case 2:
    //             selectHornItem = HornAudioList.Count > 1 ? HornAudioList[1] : HornAudioList[0];
    //             SetSelectItemStatus(selectHornItem);
    //             if (vehicleInfo?.vehicleAudio != null)
    //             {
    //                 vehicleInfo.vehicleAudio.hornUrl = "";
    //                 vehicleInfo.vehicleAudio.hornWwise = selectHornItem.GetData().wwiseInfo;
    //             }
    //             break;
    //     }
    // }

    public bool CanUseVip()
    {
        if (VipDataManager.Inst.isVip)
            return true;

        var joinVipType = new List<JoinVipType>() { JoinVipType.VIP_Multi_Clip };
        var joinVipTitle = "您正在使用的VIP功能：" + "添加自定义喇叭声音";
        if (joinVipType.Count > 0)
        {
            UIManager.Inst.OpenPanel<JoinVipPanel>(PanelId.JoinVipPanel, joinVipTitle, joinVipType);
        }

        return false;
    }

    // public void OnItemSelected(VehicleAudioItem item)
    // {
    //     var needVip = item?.NeedVip;
    //     if(needVip.Value )
    //     {
    //         if(CanUseVip() == false)
    //         {
    //             return;
    //         }
    //     }
    //     var audioData = item.GetData();
    //     if (audioData.isUGC)
    //     {
    //         if (string.IsNullOrEmpty(audioData.MusicUrl))
    //         {
    //             bool isCancel = false;
    //             Action onCancel = () => {
    //                 isCancel = true;
    //             };

    //             UIManager.Inst.OpenPanel<BgMusicUploadingPanel>(PanelId.BgMusicUploadingPanel, onCancel);
    //             int musicLoudness = 0;
    //             AlbumUtils.Inst.UploadMusic(_audioLimit, (remoteUrl) => {
    //                 if (isCancel)
    //                 {
    //                     return;
    //                 }
    //                 if (!string.IsNullOrEmpty(remoteUrl))
    //                 {
    //                     //SetSelectItemStatus(item);
    //                     audioData.MusicUrl = remoteUrl;
    //                     item.RefreshData();
    //                     SetSelectItemStatus(item);
    //                     //SaveUGCAudioToVehicleInfo(audioData.type, remoteUrl);
    //                     AkSoundManager.Inst.PlayUGCAudioByUrl(remoteUrl,false,gameObject);
    //                 }
    //                 UIManager.Inst.ClosePanel(PanelId.BgMusicUploadingPanel);
    //             }, err => {
    //                 UIManager.Inst.ClosePanel(PanelId.BgMusicUploadingPanel);
    //             }, null, loudness => {
    //                 musicLoudness = loudness;
    //             });
    //             return;
    //         }

    //         SetSelectItemStatus(item);
    //         AkSoundManager.Inst.PlayUGCAudioByUrl(audioData.MusicUrl,false,gameObject);
    //         return;
    //     }
    //     SetSelectItemStatus(item);
    //     AkSoundManager.Inst.PlaySound(audioData.wwiseInfo.group, audioData.wwiseInfo.switchs, audioData.wwiseInfo.wwise, _globalSoundObj);
    // }

    // private void SetSelectItemStatus(VehicleAudioItem item)
    // {
    //     var audioData = item.GetData();
    //     selectStarItem?.Setselect(true, false);
    //     selectDriveItem?.Setselect(true, false);
    //     selectHornItem?.Setselect(true, false);
    //     switch (audioData.type)
    //     {
    //         case 0:
    //             selectStarItem?.Setselect(false);
    //             StopPlayAudio();
    //             selectStarItem = item;
    //             selectStarItem.Setselect(true, true);
    //             break;
    //         case 1:
    //             selectDriveItem?.Setselect(false);
    //             StopPlayAudio();
    //             selectDriveItem = item;
    //             selectDriveItem.Setselect(true, true);
    //             break;
    //         case 2:
    //             selectHornItem?.Setselect(false);
    //             StopPlayAudio();
    //             selectHornItem = item;
    //             selectHornItem.Setselect(true, true);
    //             break;
    //     }
    // }

    // private void StopPlayAudio()
    // {
    //     AkSoundManager.Inst.StopGameMusicNode();

    //     if (selectDriveItem != null)
    //     {
    //         var audioData = selectDriveItem.GetData();
    //         if(audioData.wwiseInfo != null)
    //         {
    //             AkSoundManager.Inst.StopSound(audioData.wwiseInfo.stopWwise, _globalSoundObj);
    //         }
    //     }
    //     if (selectHornItem != null)
    //     {
    //         var audioHornData = selectHornItem.GetData();
    //         if(audioHornData.wwiseInfo !=null)
    //         {
    //             AkSoundManager.Inst.StopSound(audioHornData.wwiseInfo.stopWwise, _globalSoundObj);
    //         }
    //     }
    // }

    // private void SaveUGCAudioToVehicleInfo(int type, string url)
    // {
    //     if (vehicleInfo?.vehicleAudio == null)
    //     {
    //         return;
    //     }
    //     switch (type)
    //     {
    //         case 0:
    //             vehicleInfo.vehicleAudio.starUrl = url;
    //             break;
    //         case 1:
    //             vehicleInfo.vehicleAudio.driveUrl = url;
    //             break;
    //         case 2:
    //             vehicleInfo.vehicleAudio.hornUrl = url;
    //             break;
    //     }
    // }

    // private void EnterAudioEvent()
    // {
    //     var starInfo = selectStarItem.GetData();
    //     if(string.IsNullOrEmpty(starInfo.MusicUrl))
    //     {
    //         vehicleInfo.vehicleAudio.starUrl = "";
    //         vehicleInfo.vehicleAudio.starWwise = starInfo.wwiseInfo;
    //     }
    //     else
    //     {
    //         vehicleInfo.vehicleAudio.starUrl = starInfo.MusicUrl;
    //         vehicleInfo.vehicleAudio.starWwise = null;
    //     }

    //     var driveInfo = selectDriveItem.GetData();
    //     if (string.IsNullOrEmpty(driveInfo.MusicUrl))
    //     {
    //         vehicleInfo.vehicleAudio.driveUrl = "";
    //         vehicleInfo.vehicleAudio.driveWwise = driveInfo.wwiseInfo;
    //     }
    //     else
    //     {
    //         vehicleInfo.vehicleAudio.driveUrl = driveInfo.MusicUrl;
    //         vehicleInfo.vehicleAudio.driveWwise = null;
    //     }

    //     var hornInfo = selectHornItem.GetData();
    //     if (string.IsNullOrEmpty(hornInfo.MusicUrl))
    //     {
    //         vehicleInfo.vehicleAudio.hornUrl = "";
    //         vehicleInfo.vehicleAudio.hornWwise = hornInfo.wwiseInfo;
    //     }
    //     else
    //     {
    //         vehicleInfo.vehicleAudio.hornUrl = hornInfo.MusicUrl;
    //         vehicleInfo.vehicleAudio.hornWwise = null;
    //     }
    //     StopPlayAudio();
    //     CloseSelf();
    // }
}

public class VehicleAudioData
{
    public string name;

    public int type;//0启动，1.运行，2.喇叭

    public bool isUGC;

    public VehicleWwiseInfo wwiseInfo;

    public string MusicUrl;
}