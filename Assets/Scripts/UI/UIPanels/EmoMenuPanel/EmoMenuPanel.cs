using System;
using Game.Database;
using Game.Store;
using GameData;
using GameData.PgcData;
using System.Collections.Generic;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;
using Es;
using UI.UIPanels.FittingRoom;

public class EmoMenuPanel : BasePanel<EmoMenuPanel>
{
    [SerializeField] private Button closeBtn;
    [SerializeField] private Toggle singlePlayerToggle;
    [SerializeField] private Toggle doublePlayerToggle;
    [SerializeField] private Toggle singlePetToggle;
    [SerializeField] private Toggle petWithPlayerToggle;
    [SerializeField] private Toggle linkEmoteToggle;
    [SerializeField] private Toggle vehicleToggle;
    [SerializeField] private Toggle theatreToggle;
    [SerializeField] private Toggle musicalInstrumentToggle;
    [SerializeField] private GameObject emoContentPrefab;
    [SerializeField] private GameObject vehicleContentPrefab;
    [SerializeField] private Transform emoContent;
    [SerializeField] private Text emptyTip;
    [SerializeField] private MISource animMISource;
    [SerializeField] private GameObject pgcEmoteView;
    [SerializeField] private UgcEmoView ugcEmoteView;
    [SerializeField] private UgcGameVehicleView ugcGameVehicleView;
    [SerializeField] private MusicalInstrumentEmoView musicalInstrumentView;
    [SerializeField] private GameObject tabobj;
    [SerializeField] private TheatreTriggerTheatreInfo theatreTriggerTheatreInfo;
    private UIEmoteType curEmoType = UIEmoteType.SinglePlayer;
    private UgcAnimSubType curUgcEmoType = UgcAnimSubType.Single;
    private VehicleSubType curVehicleSubType = VehicleSubType.SingleVehicle;
    private List<EmoContentItem> avtiveItemList = new List<EmoContentItem>();
    private List<EmoContentItem> unActivceItemList = new List<EmoContentItem>();
    private List<VehicleContentItem> avtiveVehicleItemList = new List<VehicleContentItem>();
    private List<VehicleContentItem> unActivceVehicleItemList = new List<VehicleContentItem>();
    public Action CloseAction { private get; set; }

    private MISource.Source curSource;

    public override void OnCreate()
    {
        base.OnCreate();
        closeBtn.onClick.AddListener(OnCloseBtnClick);
        singlePlayerToggle.onValueChanged.AddListener(OnSinglePlayerChanged);
        doublePlayerToggle.onValueChanged.AddListener(OnDoublePlayerEmoChanged);
        singlePetToggle.onValueChanged.AddListener(OnSinglePetChanged);
        petWithPlayerToggle.onValueChanged.AddListener(OnPetWithPlayerChanged);
        linkEmoteToggle.onValueChanged.AddListener(OnLinkEmoteChanged);
        vehicleToggle.onValueChanged.AddListener(OnVehicleChanged);
        theatreToggle.onValueChanged.AddListener(OnTheatreChanged);
        musicalInstrumentToggle.onValueChanged.AddListener(OnMusicalInstrumentChanged);
        //theatreToggle.gameObject.SetActive(DeviceInfoManager.Inst.Environment != GameEnvironment.PROD);
        var petIsHidden = AccountDataManager.Inst.PetInfo.isGameHidden == 1;// AccountDataManager.Inst.PetInfo.isHidden == 1;
        singlePetToggle.gameObject.SetActive(!petIsHidden);
        petWithPlayerToggle.gameObject.SetActive(!petIsHidden);
        animMISource.SetCallback(OnValueChange);
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        ugcEmoteView.OnStart(curUgcEmoType,OnCloseBtnClick);
        ugcEmoteView.gameObject.SetActive(false);
        ugcGameVehicleView.OnStart(curVehicleSubType,OnCloseBtnClick);
        ugcGameVehicleView.gameObject.SetActive(false);
        musicalInstrumentView.OnStart(OnCloseBtnClick);
        musicalInstrumentView.gameObject.SetActive(false);
        theatreTriggerTheatreInfo.gameObject.SetActive(false);
        ShowEmoConent();
        if ((args != null && args.Length > 0) && args[0] is bool bHideTab)
        {
            tabobj.SetActive(!bHideTab);
        }
    }

    private void OnValueChange(MISource.Source source)
    {
        curSource = source;
        ShowViewStatus();
    }

    private void ShowViewStatus()
    {
        if (curEmoType == UIEmoteType.Theatre)
        {
            pgcEmoteView.gameObject.SetActive(false);
            ugcEmoteView.gameObject.SetActive(false);
            ugcGameVehicleView.gameObject.SetActive(false);
            musicalInstrumentView.gameObject.SetActive(false);
            return;
        }

        pgcEmoteView.gameObject.SetActive(curSource == MISource.Source.Bud && curEmoType != UIEmoteType.MusicalInstrument);
        linkEmoteToggle.gameObject.SetActive(curSource == MISource.Source.Bud );//双人牵手只有PGC的
        ugcEmoteView.gameObject.SetActive(curSource != MISource.Source.Bud && curEmoType != UIEmoteType.Vehicle && curEmoType != UIEmoteType.MusicalInstrument);

        ugcEmoteView.resMISource.gameObject.SetActive(curSource != MISource.Source.Bud);
        ugcEmoteView.SetDefaultMISource(curEmoType);
        ugcGameVehicleView.gameObject.SetActive(curSource != MISource.Source.Bud && curEmoType == UIEmoteType.Vehicle);
        //ugcGameVehicleView.resMISource.gameObject.SetActive(curSource != MISource.Source.Bud);
        ugcGameVehicleView.SetDefaultMISource(curEmoType);

        bool showMusicalInstrument = curEmoType == UIEmoteType.MusicalInstrument;
        musicalInstrumentView.gameObject.SetActive(showMusicalInstrument);
        if (showMusicalInstrument) musicalInstrumentView.SetOuterSource(curSource);
    }


    private void OnCloseBtnClick()
    {
        CloseAction?.Invoke();
        CloseSelf();
    }

    private void OnSinglePlayerChanged(bool isOn)
    {
        ShowCheckedIcon(isOn, singlePlayerToggle.transform);

        if (isOn)
        {
            curEmoType = UIEmoteType.SinglePlayer;
            curUgcEmoType = UgcAnimSubType.Single;
            ShowEmoConent();
            ShowViewStatus();
        }
    }

    private void OnSinglePetChanged(bool isOn)
    {
        ShowCheckedIcon(isOn, singlePetToggle.transform);

        if (isOn)
        {
            curEmoType = UIEmoteType.PetSingle;
            curUgcEmoType = UgcAnimSubType.PetSingle;
            ShowEmoConent();
            ShowViewStatus();
        }
    }

    private void OnDoublePlayerEmoChanged(bool isOn)
    {
        ShowCheckedIcon(isOn, doublePlayerToggle.transform);

        if (isOn)
        {
            curEmoType = UIEmoteType.DoublePlayer;
            curUgcEmoType = UgcAnimSubType.Double;
            ShowEmoConent();
            ShowViewStatus();
        }
    }

    private void OnPetWithPlayerChanged(bool isOn)
    {
        ShowCheckedIcon(isOn, petWithPlayerToggle.transform);

        if (isOn)
        {
            curEmoType = UIEmoteType.PetWithPlayer;
            curUgcEmoType = UgcAnimSubType.PetWithPlayer;
            ShowEmoConent();
            ShowViewStatus();
        }
    }

    private void OnLinkEmoteChanged(bool isOn)
    {
        ShowCheckedIcon(isOn, linkEmoteToggle.transform);

        if (isOn)
        {
            curEmoType = UIEmoteType.LinkEmote;
            curUgcEmoType = UgcAnimSubType.LinkEmote;
            ShowEmoConent();
            ShowViewStatus();
        }
    }

    private void OnVehicleChanged(bool isOn)
    {
        ShowCheckedIcon(isOn, vehicleToggle.transform);

        if (isOn)
        {
            curEmoType = UIEmoteType.Vehicle;
            curVehicleSubType = VehicleSubType.SingleVehicle;
            ShowOwnedVehicle();
            ShowViewStatus();
        }
    }

    private void OnTheatreChanged(bool isOn)
    {
        ShowCheckedIcon(isOn, theatreToggle.transform);
        if (isOn)
        {
            curEmoType = UIEmoteType.Theatre;
            theatreTriggerTheatreInfo.gameObject.SetActive(true);
            theatreTriggerTheatreInfo.GetData();
            theatreTriggerTheatreInfo.SetOnSelectAct(OnTheatreItemSelected);
            ShowViewStatus();
        }
        else
        {
            theatreTriggerTheatreInfo.gameObject.SetActive(false);
        }
    }

    private void OnMusicalInstrumentChanged(bool isOn)
    {
        ShowCheckedIcon(isOn, musicalInstrumentToggle.transform);
        if (isOn)
        {
            curEmoType = UIEmoteType.MusicalInstrument;
            SetAllItemUnActive();
            SetAllIVehicletemUnActive();
            emptyTip.gameObject.SetActive(false);
            ShowViewStatus();
        }
    }

    private void OnTheatreItemSelected(DraftListItem item)
    {
        if (item?.theatreInfo == null) return;
        OnCloseBtnClick();
        UIManager.Inst.OpenPanel(PanelId.TheatreGameRoomPanel, item.theatreInfo);
    }

    private void ShowCheckedIcon(bool isChecked, Transform toggle)
    {
        var iconChecked = toggle.Find("Background/iconChecked").gameObject;
        var iconUnChecked = toggle.Find("Background/iconUnChecked").gameObject;

        iconChecked.SetActive(isChecked);
        iconUnChecked.SetActive(!isChecked);
        
        //双人牵手和剧场不显示"官方/社区"
        if ((toggle == linkEmoteToggle.transform || toggle == theatreToggle.transform) && isChecked)
        {
            animMISource.gameObject.SetActive(false);
        }
        else
        {
            animMISource.gameObject.SetActive(true);
        }
    }

    private void ShowEmoConent()
    {
        SetAllItemUnActive();
        SetAllIVehicletemUnActive();
        List<InventoryData> owned = new();
        List<EmoUIConfig> emoteDataList = new();
        switch (curEmoType)
        {
            case UIEmoteType.SinglePlayer:
                owned.AddRange(BagDatabase.Inst.SelectAll(UniqueType.Get(ResourceType.Emote, (int)EmoteSubType.Single)));
                owned.AddRange(BagDatabase.Inst.SelectAll(UniqueType.Get(ResourceType.Emote, (int)EmoteSubType.SingleLoop)));
                emoteDataList.AddRange(Es.DataTables.GetEmoUIConfigList().FindAll((emoData) => emoData.emoType == (int)EmoteSubType.Single && AssetsDataManager.IsFreeAssets(emoData.pgcId)));
                emoteDataList.AddRange(Es.DataTables.GetEmoUIConfigList().FindAll((emoData) => emoData.emoType == (int)EmoteSubType.SingleLoop && AssetsDataManager.IsFreeAssets(emoData.pgcId)));
                break;
            case UIEmoteType.DoublePlayer:
                owned.AddRange(BagDatabase.Inst.SelectAll(UniqueType.Get(ResourceType.Emote, (int)EmoteSubType.Double)));
                owned.AddRange(BagDatabase.Inst.SelectAll(UniqueType.Get(ResourceType.Emote, (int)EmoteSubType.DoubleLoop)));
                emoteDataList.AddRange(Es.DataTables.GetEmoUIConfigList().FindAll((emoData) => emoData.emoType == (int)EmoteSubType.Double && AssetsDataManager.IsFreeAssets(emoData.pgcId)));
                emoteDataList.AddRange(Es.DataTables.GetEmoUIConfigList().FindAll((emoData) => emoData.emoType == (int)EmoteSubType.DoubleLoop && AssetsDataManager.IsFreeAssets(emoData.pgcId)));
                break;
            case UIEmoteType.PetSingle:
                owned.AddRange(BagDatabase.Inst.SelectAll(UniqueType.Get(ResourceType.Emote, (int)EmoteSubType.PetSingle)));
                owned.AddRange(BagDatabase.Inst.SelectAll(UniqueType.Get(ResourceType.Emote, (int)EmoteSubType.PetSingleLoop)));
                emoteDataList.AddRange(Es.DataTables.GetEmoUIConfigList().FindAll((emoData) => emoData.emoType == (int)EmoteSubType.PetSingle && AssetsDataManager.IsFreeAssets(emoData.pgcId)));
                emoteDataList.AddRange(Es.DataTables.GetEmoUIConfigList().FindAll((emoData) => emoData.emoType == (int)EmoteSubType.PetSingleLoop && AssetsDataManager.IsFreeAssets(emoData.pgcId)));
                break;
            case UIEmoteType.PetWithPlayer:
                owned.AddRange(BagDatabase.Inst.SelectAll(UniqueType.Get(ResourceType.Emote, (int)EmoteSubType.PetWithPlayer)));
                owned.AddRange(BagDatabase.Inst.SelectAll(UniqueType.Get(ResourceType.Emote, (int)EmoteSubType.PetWithPlayerLoop)));
                emoteDataList.AddRange(Es.DataTables.GetEmoUIConfigList().FindAll((emoData) => emoData.emoType == (int)EmoteSubType.PetWithPlayer && AssetsDataManager.IsFreeAssets(emoData.pgcId)));
                emoteDataList.AddRange(Es.DataTables.GetEmoUIConfigList().FindAll((emoData) => emoData.emoType == (int)EmoteSubType.PetWithPlayerLoop && AssetsDataManager.IsFreeAssets(emoData.pgcId)));
                break;
            case UIEmoteType.LinkEmote:
                owned.AddRange(BagDatabase.Inst.SelectAll(UniqueType.Get(ResourceType.Emote, (int)EmoteSubType.LinkEmote)));
                emoteDataList.AddRange(Es.DataTables.GetEmoUIConfigList().FindAll((emoData) => emoData.emoType == (int)EmoteSubType.LinkEmote && AssetsDataManager.IsFreeAssets(emoData.pgcId)));
                break;
        }
        ugcEmoteView.ChangeAnimType(curUgcEmoType);
        for (int i = owned.Count - 1; i >= 0; i--)
        {
            var config = Es.DataTables.GetEmoUIConfig(owned[i].Id);
            if (config != null)
            {
                emoteDataList.Insert(0, config);
            }
        }
        emoteDataList.Sort((a, b) =>
        {
            var aData = BagDatabase.Inst.Select(a.pgcId);
            long aTime = (aData == null) ? 0 : aData.Timestamp;
            
            var bData = BagDatabase.Inst.Select(b.pgcId);
            long bTime = (bData == null) ? 0 : bData.Timestamp;
            
            // 降序排序
            return bTime.CompareTo(aTime);
        });

        for (int i = 0; i < emoteDataList.Count; i++)
        {
            var emoContentItem = GetEmoItem();
            emoContentItem.transform.SetSiblingIndex(i);
            emoContentItem.InitData(emoteDataList[i], OnCloseBtnClick);
        }

        emptyTip.gameObject.SetActive(emoteDataList.Count <= 0);
        if (emoteDataList.Count <= 0)
        {
            emptyTip.SetLocalText(curEmoType switch
                {
                    UIEmoteType.SinglePlayer => "无单人动作，可前往商城获取",
                    UIEmoteType.DoublePlayer => "无双人动作，可前往商城获取",
                    UIEmoteType.PetSingle => "无宠物动作，可前往商城获取",
                    UIEmoteType.PetWithPlayer => "无宠物与人交互动作，可前往商城获取",
                    UIEmoteType.LinkEmote => "无牵手动作，可前往商城获取",
                    UIEmoteType.Vehicle => "无载具，可前往商城获取",
                    _ => "无动作，可前往商城获取"
                }
            );
        }
    }

    private void SetAllItemUnActive()
    {
        foreach (var emoData in avtiveItemList)
        {
            unActivceItemList.Add(emoData);
            emoData.gameObject.SetActive(false);
        }

        avtiveItemList.Clear();
    }

    private EmoContentItem GetEmoItem()
    {
        EmoContentItem emoContentItem;

        if (unActivceItemList.Count == 0)
        {
            emoContentItem = GameObject.Instantiate(emoContentPrefab, emoContent).GetComponent<EmoContentItem>();
        }
        else
        {
            emoContentItem = unActivceItemList[0];
            emoContentItem.gameObject.SetActive(true);

            unActivceItemList.RemoveAt(0);
        }

        avtiveItemList.Add(emoContentItem);

        return emoContentItem;
    }

    #region 载具使用

    private void ShowOwnedVehicle(){
        SetAllItemUnActive();
        SetAllIVehicletemUnActive();
        List<InventoryData> owned = new();
        owned.AddRange(BagDatabase.Inst.SelectAll(UniqueType.Get(ResourceType.Vehicle, (int)VehicleSubType.SingleVehicle)));
        owned.AddRange(BagDatabase.Inst.SelectAll(UniqueType.Get(ResourceType.Vehicle, (int)VehicleSubType.DoubleVehicle)));
        
        for (int i = 0; i < owned.Count; i++)
        {
            var vehicleContentItem = GetVehicleItem();
            vehicleContentItem.transform.SetSiblingIndex(i);
            vehicleContentItem.InitData(owned[i], OnCloseBtnClick);
        }
        ugcGameVehicleView.ChangeAnimType(curVehicleSubType);
        emptyTip.gameObject.SetActive(owned.Count <= 0);
        if (owned.Count <= 0)
        {
            emptyTip.SetLocalText("无载具，可前往商城获取");
        }
    }

    private VehicleContentItem GetVehicleItem()
    {
        VehicleContentItem vehicleContentItem;
        if (unActivceVehicleItemList.Count == 0)
        {
            vehicleContentItem = GameObject.Instantiate(vehicleContentPrefab, emoContent).GetComponent<VehicleContentItem>();
        }
        else
        {
            vehicleContentItem = unActivceVehicleItemList[0];
            vehicleContentItem.gameObject.SetActive(true);

            unActivceVehicleItemList.RemoveAt(0);
        }

        avtiveVehicleItemList.Add(vehicleContentItem);
        return vehicleContentItem;
    }

    private void SetAllIVehicletemUnActive()
    {
        foreach (var itemData in avtiveVehicleItemList)
        {
            unActivceVehicleItemList.Add(itemData);
            itemData.gameObject.SetActive(false);
        }

        avtiveVehicleItemList.Clear();
    }

    #endregion
}