using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.IO;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using GameData.Base;
using GameUI;
using Message;
using Newtonsoft.Json;
using System.Collections;
using UI.Base;
using UI.BaseWidgets;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

public class PlantInfoPanel : BasePanel<PlantInfoPanel>
{
    public CButton CloseBtn;

    public Toggle Toggle;

    public Toggle Toggle2;

    public GameObject InfoGroup;

    public GameObject FriendGroup;

    public RemoteImageBehaviour RemoteImage;

    public Image Icon;

    public CButton SeedBtn;

    public CButton WaterBtn;

    public Text FlowerCount;
    public Text FlowerRank;

    public Text WaterCount;

    public Text SeedCount;

    public PlantInfoPanelAdpter SelectAdpter;

    public PullToRefreshBehaviour PullToRefreshBehaviour;

    public CButton SearchBtn;

    public CButton Btn_InputGameName;

    public InputField Txt_MapName;

    private KeyBoardInfo _nameKBInfo;

    PlantTreeSystem data => PlantTreeSystem.Inst;
    public override void OnCreate()
    {
        base.OnCreate();

        CloseBtn.onClick.AddListener(CloseSelf);

        Btn_InputGameName.onClick.AddListener(OnBtnInputGameNameClick);

        Toggle.onValueChanged.AddListener(OnTog);

        Toggle2.onValueChanged.AddListener(OnTog2);

        WaterBtn.onClick.AddListener(() => { UIManager.Inst.OpenPanel(PanelId.PlantWaterPanel,AccountDataManager.Inst.Uid,true);});

        SeedBtn.onClick.AddListener(() => { UIManager.Inst.OpenPanel(PanelId.PlantWaterPanel, AccountDataManager.Inst.Uid,false);});

        SearchBtn.onClick.AddListener(OnSearch);

        _nameKBInfo = new KeyBoardInfo
        {
            type = 0,
            placeHolder = "请输入名称",
            inputMode = 0,
            maxLength = 14,
            inputFlag = 0,
            textSecurity = 1,
            lengthTips = LocalizationManager.Inst.GetLocalizedText("字数超出限制"),
            returnKeyType = (int)ReturnType.Return
        };

        PullToRefreshBehaviour.OnRefreshWithSlideUp.AddListener(OnPullRefresh);
        SelectAdpter.OnItemSelected = OnItemSelected;
        SelectAdpter.Data = new LazyDataHelper<PlantUserInfo>(SelectAdpter, GetMapInfo);

        MessageHelper.AddListener<CurrencyType>(MessageName.OnPlayerInfoAccountChange, OnPlayerInfoAccountChange);
        MessageHelper.AddListener(MessageName.PlantTreeUpdate, RefreshView);
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);

        if (!SelectAdpter.IsInitialized)
        {
            SelectAdpter.Init();
        }
        FriendGroup.gameObject.SetActive(false);
        InfoGroup.gameObject.SetActive(false);
        Toggle.isOn = true;

        WaterCount.text = AccountDataManager.Inst.BalanceInfo.GetAccountCount(CurrencyType.TreePlantingWater).ToString();
        SeedCount.text = AccountDataManager.Inst.BalanceInfo.GetAccountCount(CurrencyType.TreePlantingFertilizer).ToString();

        if (PlantTreeSystem.Inst.WaterInfo == null)
        {
            return;
        }

        FlowerCount.text = PlantTreeSystem.Inst.WaterInfo.treePlantingDayInfo.growthValue.ToString();
        FlowerRank.text = PlantTreeSystem.Inst.WaterInfo.treePlantingDayInfo.growthRank.ToString();
        var url = PlantTreeSystem.Inst.WaterInfo.treePlantingDayInfo.plantingCover;
        if (url.StartsWith("http"))
        {
            RemoteImage.gameObject.SetActive(true);
            Icon.gameObject.SetActive(false);
            RemoteImage.Load(url);
        }
        else
        {
            RemoteImage.gameObject.SetActive(false);
            Icon.gameObject.SetActive(true);
            var atlasPath = XAssetLoaderMgr.Inst.GetSpriteAltasPath(SpriteAtlasType.PgcPropSprite);
            var spriteAtlas = XAssetLoaderMgr.Inst.LoadResource<SpriteAtlas>(atlasPath, gameObject);
            Icon.sprite = spriteAtlas.GetSprite(url);
        }
    }

    void RefreshView() {
        if (PlantTreeSystem.Inst.WaterInfo == null)
        {
            return;
        }

        FlowerCount.text = PlantTreeSystem.Inst.WaterInfo.treePlantingDayInfo.growthValue.ToString();
        FlowerRank.text = PlantTreeSystem.Inst.WaterInfo.treePlantingDayInfo.growthRank.ToString();
    }

    protected override void OnDestroy()
    {
        MessageHelper.RemoveListener(MessageName.PlantTreeUpdate, RefreshView);
        MessageHelper.RemoveListener<CurrencyType>(MessageName.OnPlayerInfoAccountChange, OnPlayerInfoAccountChange);
        data.FriendsListResData = null;
        data.FriendsInfos.Clear();
        base.OnDestroy();
    }

    void OnPlayerInfoAccountChange(CurrencyType currencyType)
    {
        WaterCount.text = AccountDataManager.Inst.BalanceInfo.GetAccountCount(CurrencyType.TreePlantingWater).ToString();
        SeedCount.text = AccountDataManager.Inst.BalanceInfo.GetAccountCount(CurrencyType.TreePlantingFertilizer).ToString();
    }

    private void OnBtnInputGameNameClick()
    {
        _nameKBInfo.defaultText = Txt_MapName.text;
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, OnGetNameFormNative);
        MobileInterface.Instance.ShowKeyboard(JsonConvert.SerializeObject(_nameKBInfo));
    }

    private void OnGetNameFormNative(string value)
    {
        MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);
        Txt_MapName.text = value;
        data.RequestFriendList(value, (items) =>
        {
            PullToRefreshBehaviour.HideGizmo();
            SelectAdpter.Data.ResetItems(data.FriendsInfos.Count);
            SelectAdpter.Refresh();
        });
    }

    void OnSearch() 
    {
        if (!string.IsNullOrEmpty(Txt_MapName.text))
        {
            data.RequestFriendList(Txt_MapName.text, (items) =>
            {
                PullToRefreshBehaviour.HideGizmo();
                SelectAdpter.Data.ResetItems(data.FriendsInfos.Count);
                SelectAdpter.Refresh();
            });
        }
        else
        {
            data.RequestFriendList("", (items) =>
            {
                PullToRefreshBehaviour.HideGizmo();
                SelectAdpter.Data.ResetItems(data.FriendsInfos.Count);
                SelectAdpter.Refresh();
            });
        }
    }

    private void OnTog(bool bo)
    {
        InfoGroup.gameObject.SetActive(bo);
        if (bo)
        {

        }
    }

    private void OnTog2(bool bo)
    {
        FriendGroup.gameObject.SetActive(bo);
        if (bo)
        {
            if (!string.IsNullOrEmpty(Txt_MapName.text))
            {
                data.RequestFriendList(Txt_MapName.text, (items) =>
                {
                    PullToRefreshBehaviour.HideGizmo();
                    SelectAdpter.Data.ResetItems(data.FriendsInfos.Count);
                    SelectAdpter.Refresh();
                });
            }
            else
            {
                data.RequestFriendList("",(items) =>
                {
                    PullToRefreshBehaviour.HideGizmo();
                    SelectAdpter.Data.ResetItems(data.FriendsInfos.Count);
                    SelectAdpter.Refresh();
                });
            }
        }
    }

    private PlantUserInfo GetMapInfo(int index)
    {
        return data.FriendsInfos[index];
    }

    private void OnItemSelected(PlantUserInfo mapInfo)
    {

    }

    private void OnPullRefresh()
    {
        if (!string.IsNullOrEmpty(Txt_MapName.text))
        {
            data.RequestFriendList(Txt_MapName.text, (items) =>
            {
                PullToRefreshBehaviour.HideGizmo();
                SelectAdpter.Data.ResetItems(data.FriendsInfos.Count);
                SelectAdpter.Refresh();
            });
        }
        else
        {
            data.RequestFriendList("", (items) =>
            {
                PullToRefreshBehaviour.HideGizmo();
                SelectAdpter.Data.ResetItems(data.FriendsInfos.Count);
                SelectAdpter.Refresh();
            });
        }
    }
}