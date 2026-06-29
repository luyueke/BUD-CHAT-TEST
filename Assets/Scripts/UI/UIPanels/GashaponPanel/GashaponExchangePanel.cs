using System;
using System.Collections;
using System.Collections.Generic;
using Basic.Utils;
using Es;
using Game.Database;
using Game.Store;
using GameData.Gashapon;
using GameData.PgcData;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.Base;
using UI.BaseWidgets;
using UI.Manager;
using UI.UIPanels.GashaponPanel;
using UnityEngine;
using UnityEngine.UI;

public class GashaponPonyExchangeItemData
{
    public List<GashaponExchangeData> exchangeList;
    public string title;
}

public class GashaponExchangeParam
{
    public string bgPath;
    public string title;
    public GashaponData gashaponData;
    public CurrencyType rewardCurrency;
    public CurrencyType rewardCurrency1 = CurrencyType.None;
    public string itemBgColor;
    public string clearTips;
    public string buyTips;
    public bool isSpecialRoot;
    public List<GashaponPonyExchangeItemData> exchangeGroupList;
    // 幻音派对扭蛋专属：开启后兑换列表按 _specialExchangeOrder 固定排序
    public bool isPhantomSoundParty;
    // 幻音派对扭蛋专属：已拥有 panelInfoList 任一载具时打折(选中 panelInfoList 载具项才 discount 显示 + 价格固定为 5)
    public bool phantomVehicleOwnedDiscount;
    // 幻音派对扭蛋专属：panelInfoList 载具的 pgcId 列表，仅选中其中一项时才触发打折表现
    public List<string> phantomVehiclePgcIds;
    // 幻音派对扭蛋专属：用户已点击过兑换 item，红点不再显示
    public bool phantomExchangeRedDotSeen;
}

public class ExchangeDataRsp
{
    public List<CommonRewardData> rewardList;
    public ServerBagUpdateData backpackData;
}

public class GashaponExchangePanel : BasePanel<GashaponExchangePanel>
{
    [SerializeField] private GashaponCharacterPreview characterPreview;
    [SerializeField] private GashaponExchangeItem itemPrefab;
    [SerializeField] private CButton BackBtn;
    [SerializeField] private Transform bgRootNode;
    [SerializeField] private Transform Content;
    [SerializeField] private AccountWidget AccountWidget;
    [SerializeField] private AccountWidget AccountWidget1;
    [SerializeField] private Text RewardName;
    [SerializeField] private Text EndTimeText;
    [SerializeField] private Text TitleText;
    [SerializeField] internal Text TipText;
    [SerializeField] private LoadingButton BuyBtn;
    [SerializeField] private Image BuyIcon;
    [SerializeField] private Text BuyText;
    [SerializeField] private CButton TryOnBtn;
    [SerializeField] private BundleExpandView bundleExpandView;
    [SerializeField] private RectTransform scrollViewRectTrans;
    [SerializeField] private Transform LeftRoot;
    [SerializeField] private Transform SpecialRoot;
    [SerializeField] private Transform SpecialContent;
    [SerializeField] private GashaponPriceItem SpecialItemPrefab;
    [SerializeField] private GameObject exchangePoolItemPrefab;
    [SerializeField] private GashaponMutipleTypeView mutipleTypeView;
    [SerializeField] private CButton previewVideoBtn;
    [SerializeField] private Image previewVideoIcon;
    [SerializeField] private GameObject discount;


    private List<GashaponExchangeItem> items = new List<GashaponExchangeItem>();
    private GashaponExchangeItem curSelectItem = null;
    private string itemBgColor = "";

    private GashaponData _gashaponData;
    GashaponExchangeData _info;

    private List<GashaponPriceItem> specialItems = new List<GashaponPriceItem>();
    private List<GashaponExchangeData> _specialDataList = new List<GashaponExchangeData>();
    private GashaponPriceItem curSpecialSelectItem;
    private bool _isSpecialRoot = false;
    private bool _isWawaji = false;
    private bool _isPhantomSoundParty = false;
    // 幻音派对：是否已拥有 panelInfoList 任一载具(打折前提)
    private bool _phantomVehicleOwnedDiscount = false;
    // 幻音派对：panelInfoList 载具 pgcId，仅选中其中一项时才显示 discount + 价格 5
    private List<string> _phantomVehiclePgcIds;
    // 幻音派对：用户已点击过兑换 item，红点不再显示
    private bool _phantomExchangeRedDotSeen = false;
    private const string PhantomVehicleDiscountPrice = "5";

    // 娃娃机多型号分组(大头 + 其变体)，仅 lottery.clawMachine 生效
    private static readonly List<string[]> WawajiTypeGroups = new List<string[]>
    {
        new[] { "160100003", "160100004", "160100005" },
        new[] { "160100006", "160100007", "160100008" },
    };

    // 视频预览配置：pgcId → (videoPath, iconPath)
    // 扩展时在此处添加新条目即可，无需改动其他逻辑
    private static readonly Dictionary<string, (string videoPath, string iconPath)> VideoPreviewConfigs =
        new Dictionary<string, (string, string)>
        {
            { "160100003", ("Assets/Loadable/Demand3D/ResVideo/wawaji1/wawaji1.mp4", "Assets/Loadable/UI/UIPanel/GashaponWawajiPanel/wawaji_video1.png") },
            { "160100004", ("Assets/Loadable/Demand3D/ResVideo/wawaji1/wawaji1.mp4", "Assets/Loadable/UI/UIPanel/GashaponWawajiPanel/wawaji_video1.png") },
            { "160100005", ("Assets/Loadable/Demand3D/ResVideo/wawaji1/wawaji1.mp4", "Assets/Loadable/UI/UIPanel/GashaponWawajiPanel/wawaji_video1.png") },
            { "160100006", ("Assets/Loadable/Demand3D/ResVideo/wawaji2/wawaji2.mp4",  "Assets/Loadable/UI/UIPanel/GashaponWawajiPanel/wawaji_video2.png") },
            { "160100007", ("Assets/Loadable/Demand3D/ResVideo/wawaji2/wawaji2.mp4",  "Assets/Loadable/UI/UIPanel/GashaponWawajiPanel/wawaji_video2.png") },
            { "160100008", ("Assets/Loadable/Demand3D/ResVideo/wawaji2/wawaji2.mp4",  "Assets/Loadable/UI/UIPanel/GashaponWawajiPanel/wawaji_video2.png") },
        };

    private string _curVideoPath;

    public Action onCloseCallback;
    public Action onSpecialItemClicked;
    public override void OnCreate()
    {
        BackBtn.onClick.AddListener(OnBackBtnClick);
        TryOnBtn?.onClick.AddListener(OnTryOnBtnClick);
        BuyBtn?.onClick.AddListener(OnBuyBtnClick);
        previewVideoBtn?.onClick.AddListener(OnPreviewVideoBtnClick);
        previewVideoBtn?.gameObject.SetActive(false);

        MessageHelper.AddListener(MessageName.OnUserStyleSuccess, OnUserInfoSuccess);
    }

    public override void OnHidden()
    {
        MessageHelper.RemoveListener(MessageName.OnUserStyleSuccess, OnUserInfoSuccess);
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        if (args != null && args.Length > 0)
        {
            var param = (GashaponExchangeParam)args[0];
            if (!string.IsNullOrEmpty(param.itemBgColor))
            {
                itemBgColor = param.itemBgColor;
            }
            _isSpecialRoot = param.isSpecialRoot;
            _isPhantomSoundParty = param.isPhantomSoundParty;
            _phantomVehicleOwnedDiscount = param.isPhantomSoundParty && param.phantomVehicleOwnedDiscount;
            _phantomVehiclePgcIds = param.phantomVehiclePgcIds;
            _phantomExchangeRedDotSeen = param.phantomExchangeRedDotSeen;
            // 初始隐藏：仅当选中 panelInfoList 载具项时才由 SelectExchangeData/OnItemClick 打开
            if (discount != null) discount.SetActive(false);
            LeftRoot.gameObject.SetActive(!param.isSpecialRoot);
            SpecialRoot.gameObject.SetActive(param.isSpecialRoot);

            SetData(param.gashaponData);
            if (param.isSpecialRoot && param.exchangeGroupList != null)
                UpdateSpecialListview(param.exchangeGroupList);
            SetupMutipleTypeView(param.gashaponData);
            SetBg(param.bgPath);
            SetTitle(param.title);
            SetAccountWidgetType(param.rewardCurrency);
            SetAccountWidgetType1(param.rewardCurrency1);

            if (!string.IsNullOrEmpty(param.clearTips))
            {
                SetTipText(param.clearTips);
            }

            if (!string.IsNullOrEmpty(param.buyTips))
            {
                EndTimeText.SetLocalText(param.buyTips);
            }

            characterPreview.SetCameraColor(DataUtil.DeSerializeColorByHex(itemBgColor + "00"));
        }
    }

    public void SetData(GashaponData gashaponData)
    {
        _gashaponData = gashaponData;
        var rewardList = GashaponDataManager.Inst.PreDealData(gashaponData.ExchangeList);
        UpdateListview(rewardList);
        Invoke("DefClickFirst", 0.2f);
    }

    private void OnUserInfoSuccess()
    {
        foreach (var itemNode in items)
        {
            var exchangeInfo = itemNode.GetBindData();
            if (exchangeInfo != null)
            {
                bool isOwned = GashaponUtils.IsOwnedReward(exchangeInfo);
                if (isOwned)
                {
                    itemNode.SetOwnedUI(true);
                    if (curSelectItem == itemNode)
                    {
                        BuyBtn.gameObject.SetActive(false);
                    }
                }
            }
        }
    }

    public void SetAnimPreviewBtnColor(Color outlineColor, Color selectColor, Color normalColor)
    {
        if (characterPreview != null)
        {
            characterPreview.SetAnimPreviewBtnColor(outlineColor, selectColor, normalColor);
        }
    }

    public void UpdateListview(List<GashaponExchangeData> dataList)
    {
        if (dataList == null || dataList.Count <= 0) return;
        for (int i = 0; i < dataList.Count; i++)
        {
            GashaponExchangeData priceData = dataList[i];
            GashaponExchangeItem itemScript = Instantiate(itemPrefab, Content);
            itemScript.Init(priceData, OnItemClick);
            if (!string.IsNullOrEmpty(itemBgColor))
            {
                itemScript.SetItemColor(itemBgColor);
            }
            items.Add(itemScript);
        }
    }

    private void UpdateSpecialListview(List<GashaponPonyExchangeItemData> groupList)
    {
        foreach (var item in specialItems) Destroy(item.gameObject);
        specialItems.Clear();
        _specialDataList.Clear();
        for (int i = SpecialContent.childCount - 1; i >= 0; i--)
            DestroyImmediate(SpecialContent.GetChild(i).gameObject);

        if (groupList == null || groupList.Count == 0) return;
        for (int i = 0; i < groupList.Count; i++)
        {
            var group = groupList[i];
            var groupGo = Instantiate(exchangePoolItemPrefab, SpecialContent);
            groupGo.SetActive(true);
            GameObjectEx.FindComponentByName<Text>(groupGo, "txt_title").text = group.title;
            var content = GameObjectEx.FindChildByName(groupGo, "Content");
            if (group.exchangeList == null) continue;
            // 该固定排序(_specialExchangeOrder)只对幻音派对扭蛋生效，娃娃机/AirVehicle 等不应被重排
            if (_isPhantomSoundParty)
                SortExchangeListByConfig(group.exchangeList);
            for (int j = 0; j < group.exchangeList.Count; j++)
            {
                var exchangeData = group.exchangeList[j];
                var rewardData = new GashaponRewardData
                {
                    Id = exchangeData.Id,
                    RewardType = exchangeData.RewardType,
                    Num = exchangeData.Num,
                    BundleId = exchangeData.BundleId,
                    PgcDatas = exchangeData.PgcDatas,
                    Name = exchangeData.Name,
                };
                var item = Instantiate(SpecialItemPrefab, content.transform);
                item.Init(rewardData, OnSpecialItemClick);
                bool showRedDot = _phantomVehicleOwnedDiscount
                    && !_phantomExchangeRedDotSeen
                    && _phantomVehiclePgcIds != null
                    && _phantomVehiclePgcIds.Contains(exchangeData.Id)
                    && !GashaponUtils.IsOwnedReward(exchangeData);
                item.SetRedDotVisible(showRedDot);
                specialItems.Add(item);
                _specialDataList.Add(exchangeData);
            }
        }
        if (specialItems.Count > 0)
            Invoke("DefSpecialClickFirst", 0.2f);
    }

    // 兑换列表按策划指定的固定顺序排序，未在配置中的 id 排到末尾并保持原有相对顺序。
    private static readonly List<string> _specialExchangeOrder = new List<string>
    {
        "160100009", "160100010", "160100011", "34700001", "10500085",
        "190000033", "180100010", "40200568", "11400092", "40100578",
        "40200566", "40200565", "40200567", "40200560", "40200561",
        "40200562", "40200563", "31600001",
    };//后端要求写死来处理排序

    private void SortExchangeListByConfig(List<GashaponExchangeData> list)
    {
        if (list == null || list.Count <= 1) return;
        list.Sort((a, b) =>
        {
            int ia = a == null ? -1 : _specialExchangeOrder.IndexOf(a.Id);
            int ib = b == null ? -1 : _specialExchangeOrder.IndexOf(b.Id);
            if (ia < 0) ia = int.MaxValue;
            if (ib < 0) ib = int.MaxValue;
            return ia.CompareTo(ib);
        });
    }

    private void DefSpecialClickFirst()
    {
        if (specialItems.Count > 0)
            specialItems[0].OnItemClick();

        RectTransform rectTransform = SpecialContent.GetComponent<RectTransform>();
        VerticalLayoutGroup verticalLayoutGroup = SpecialContent.GetComponent<VerticalLayoutGroup>();
        if (verticalLayoutGroup != null && verticalLayoutGroup.enabled)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }
    }

    private void OnSpecialItemClick(GashaponPriceItem item, GashaponRewardData info)
    {
        int idx = specialItems.IndexOf(item);
        if (idx < 0 || idx >= _specialDataList.Count) return;
        var exchangeData = _specialDataList[idx];
        SelectExchangeData(exchangeData);
        curSpecialSelectItem = item;
        foreach (var si in specialItems) si.SetSelectStatus(false);
        item.SetSelectStatus(true);
        // MutipleTypeView 的显隐与分组同步已统一在 SelectExchangeData 内处理
        onSpecialItemClicked?.Invoke();
    }

    // 仅娃娃机(lottery.clawMachine)显示多型号切换视图，其它扭蛋隐藏
    private void SetupMutipleTypeView(GashaponData gashaponData)
    {
        _isWawaji = gashaponData != null && gashaponData.Id == "lottery.clawMachine";
        if (mutipleTypeView == null) return;
        if (!_isWawaji)
        {
            mutipleTypeView.gameObject.SetActive(false);
            return;
        }

        // 从完整兑换列表按 pgcId 取每组(大头+变体)的兑换数据
        var groups = new List<List<GashaponExchangeData>>();
        foreach (var ids in WawajiTypeGroups)
        {
            var g = new List<GashaponExchangeData>();
            foreach (var id in ids)
            {
                var d = gashaponData.ExchangeList?.Find(e => e != null && e.Id == id);
                if (d != null) g.Add(d);
            }
            if (g.Count > 0) groups.Add(g);
        }

        if (groups.Count == 0)
        {
            mutipleTypeView.gameObject.SetActive(false);
            return;
        }
        mutipleTypeView.Init(groups, SelectExchangeData);
        // 初始隐藏：仅当选中多型号组成员时才显示(由 SelectExchangeData 控制)
        mutipleTypeView.gameObject.SetActive(false);
    }

    // 判断某 pgcId 是否属于娃娃机多型号组
    private static bool IsInWawajiGroup(string id)
    {
        foreach (var g in WawajiTypeGroups)
            foreach (var s in g)
                if (s == id) return true;
        return false;
    }

    // 幻音派对打折：已拥有任一载具(前提) 且 当前选中项正是 panelInfoList 载具之一
    private bool IsPhantomVehicleDiscountItem(string id)
    {
        return _phantomVehicleOwnedDiscount
               && !string.IsNullOrEmpty(id)
               && _phantomVehiclePgcIds != null
               && _phantomVehiclePgcIds.Contains(id);
    }

    // 选中某个兑换项(供 SpecialContent 点击 与 MutipleTypeView toggle 共用)
    public void SelectExchangeData(GashaponExchangeData exchangeData)
    {
        if (exchangeData == null) return;
        _info = exchangeData;
        // 娃娃机货币换名：水晶/碎片→星辉夹/泡泡夹，有覆盖名则原文直显
        var specialNameOv = PgcUtils.GetScopeCurrencyName(GameUtils.ConvertRewardType((int)exchangeData.RewardType));
        if (!string.IsNullOrEmpty(specialNameOv))
            RewardName.text = specialNameOv;
        else
            RewardName.SetLocalText(exchangeData.Name);
        // 幻音派对：仅选中 panelInfoList 载具项时打折，discount 显示且购买价固定展示为 5
        bool phantomDiscount = IsPhantomVehicleDiscountItem(exchangeData.Id);
        if (discount != null) discount.SetActive(phantomDiscount);
        BuyText.SetText(phantomDiscount ? PhantomVehicleDiscountPrice : exchangeData.Price + "");
        BuyIcon.sprite = PgcUtils.LoadCurrencyIcon(exchangeData.CurrencyType, BuyIcon.gameObject);
        BuyBtn.gameObject.SetActive(!GashaponUtils.IsOwnedReward(exchangeData));
        characterPreview.StartPreview(exchangeData, OnTryComplete);

        // 选中多型号组成员才显示 MutipleTypeView，否则隐藏
        if (_isWawaji && mutipleTypeView != null)
        {
            bool inGroup = IsInWawajiGroup(exchangeData.Id);
            mutipleTypeView.gameObject.SetActive(inGroup);
            if (inGroup) mutipleTypeView.ShowGroupContaining(exchangeData.Id);
        }

        // 视频预览按钮：有对应配置的 pgcId 才显示，并更新图标和视频路径
        UpdateVideoPreviewBtn(exchangeData.Id);
    }

    private void UpdateVideoPreviewBtn(string pgcId)
    {
        if (previewVideoBtn == null) return;
        if (VideoPreviewConfigs.TryGetValue(pgcId, out var cfg))
        {
            _curVideoPath = cfg.videoPath;
            if (previewVideoIcon != null)
                previewVideoIcon.sprite = XAssetLoaderMgr.Inst.LoadResource<Sprite>(cfg.iconPath, gameObject);
            previewVideoBtn.gameObject.SetActive(true);
        }
        else
        {
            _curVideoPath = null;
            previewVideoBtn.gameObject.SetActive(false);
        }
    }

    private void OnPreviewVideoBtnClick()
    {
        if (!string.IsNullOrEmpty(_curVideoPath))
            UIManager.Inst.OpenPanel<VideoPreviewPanel>(PanelId.VideoPreviewPanel, _curVideoPath);
    }

    public void SetEndTime(string leftTime)
    {
        EndTimeText.SetLocalText("距活动结束还有: {0}", leftTime);
    }

    public void SetTitle(string title)
    {
        TitleText.SetLocalText(title);
    }

    public void SetAccountWidgetType(CurrencyType type)
    {
        AccountWidget?.ChangeType(type);
    }

    public void SetAccountWidgetType1(CurrencyType type)
    {
        if (type == CurrencyType.None)
        {
            AccountWidget1?.gameObject.SetActive(false);
            return;
        }
        else
        {
            AccountWidget1?.gameObject.SetActive(true);
        }
        AccountWidget1?.ChangeType(type);
    }


    public void SetBg(string path)
    {
        var itemObj = Loader
            .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
            .Instantiate(bgRootNode);
        var item = itemObj.GetComponent<ActivityCenterBgItem>();

        if (string.IsNullOrEmpty(path))
        {
            var viewCfg = GashaponDataManager.Inst.GetGashaponView(_gashaponData.Id);
            if (viewCfg == null)
            {
                return;
            }
            item.InitCustomBgItem(viewCfg.BgColor, viewCfg.AtlasPath, viewCfg.BgSpriteIds);
        }
        else
        {
            item.InitCustomTextureBg(path);
        }
        item.gameObject.SetActive(true);
    }

    public void SetTipText(string tips)
    {
        TipText.SetLocalText(tips);
    }

    public void SetBundleViewBgColor(string colorStr)
    {
        bundleExpandView?.SetBgColor(colorStr);
    }


    private void DefClickFirst()
    {
        characterPreview.StopAllEmoteSound();
        if (items.Count > 0)
        {
            items[0].OnClickItem();
        }
    }

    private void OnItemClick(GashaponExchangeItem itemNode, GashaponExchangeData info)
    {
        _info = info;
        string pgcId = "";
        if (GashaponUtils.HasPGCData(info))
        {
            pgcId = info.PgcDatas[0].Id;
        }

        bool isOwned = GashaponUtils.IsOwnedReward(info);

        if (!string.IsNullOrEmpty(info.BundleId) && info.PgcDatas != null && info.PgcDatas.Count > 1)
        {
            string bundleName = PgcUtils.GetBundleName(info.BundleId);
            RewardName.SetLocalText(bundleName);
        }
        else
        {
            // 娃娃机货币换名：水晶/碎片→星辉夹/泡泡夹，有覆盖名则原文直显
            var nameOv = PgcUtils.GetScopeCurrencyName(GameUtils.ConvertRewardType((int)info.RewardType));
            if (!string.IsNullOrEmpty(nameOv))
                RewardName.text = nameOv;
            else
                RewardName.SetLocalText(info.Name);
        }

        // 幻音派对：仅选中 panelInfoList 载具项时打折，discount 显示且购买价固定展示为 5
        bool phantomDiscount = IsPhantomVehicleDiscountItem(info.Id);
        if (discount != null) discount.SetActive(phantomDiscount);
        BuyText.SetText(phantomDiscount ? PhantomVehicleDiscountPrice : info.Price + "");
        BuyBtn.gameObject.SetActive(!isOwned);
        characterPreview.StartPreview(info, OnTryComplete);
        curSelectItem = itemNode;

        BuyIcon.sprite = PgcUtils.LoadCurrencyIcon(info.CurrencyType, BuyIcon.gameObject);

        if (info.PgcDatas?.Count == 1)
        {
            scrollViewRectTrans.offsetMin = new Vector2(scrollViewRectTrans.offsetMin.x, 112);
        }
        else
        {
            scrollViewRectTrans.offsetMin = new Vector2(scrollViewRectTrans.offsetMin.x, 270);
        }

        for (int i = 0; i < items.Count; i++)
        {
            items[i].SetSelectStatus(false);
        }

        if (info.PgcDatas != null && info.PgcDatas.Count > 1)
        {
            bundleExpandView.Init(new GashaponRewardData()
            {
                BundleId = info.BundleId,
                PgcDatas = info.PgcDatas,
                Id = info.Id,
                Name = info.Name,
                RewardType = info.RewardType,
            });
            bundleExpandView.Show();
        }
        else
        {
            bundleExpandView.Hide();
        }

    }

    private void OnTryComplete(List<string> pgcIds)
    {
        if (pgcIds != null && pgcIds.Count > 0)
        {
            // if (previewListView.CurSelectItem != null)
            // {
            //     var bindData = previewListView.CurSelectItem.GetBindData();
            //     if (bindData.PgcDatas != null)
            //     {
            //         if (GashaponUtils.IsAssetsDataEqual(bindData.PgcDatas, pgcIds))
            //         {
            //             previewListView.CurSelectItem.SetLoadingVisible(false);
            //         }
            //     }
            // }
        }
    }

    private void OnBackBtnClick()
    {
        CloseSelf();
        AccountDataManager.Inst.BalanceInfo.Refresh();
        onCloseCallback?.Invoke();
    }

    private void OnBuyBtnClick()
    {
        if (_gashaponData == null) return;

        GashaponExchangeData curSelectData;
        if (_isSpecialRoot)
        {
            if (_info == null) return;
            curSelectData = _info;
        }
        else
        {
            if (curSelectItem == null || curSelectItem.GetBindData() == null) return;
            curSelectData = curSelectItem.GetBindData();
            if (curSelectData.Id == "160200002")
            {
                var pdcId = BagDatabase.Inst.Select("160100002");
                if (pdcId == null || pdcId.OwnedNum <= 0)
                {
                    TipPanel.ShowToast("需有铃兰踏踏");
                    return;
                }
            }
        }

        // 前置依赖：兑换 160100004/005 需先拥有 160100003；兑换 160100007/008 需先拥有 160100006
        string prereqId = null;
        string prereqName = null;
        if (curSelectData.Id == "160100004" || curSelectData.Id == "160100005")
        {
            prereqId = "160100003";
            prereqName = "抓抓泡泡号 晴空";
        }
        else if (curSelectData.Id == "160100007" || curSelectData.Id == "160100008")
        {
            prereqId = "160100006";
            prereqName = "抓抓星辉号 星澜";
        }
        if (!string.IsNullOrEmpty(prereqId))
        {
            var prereq = BagDatabase.Inst.Select(prereqId);
            if (prereq == null || prereq.OwnedNum <= 0)
            {
                TipPanel.ShowToast($"需先拥有{prereqName}");
                return;
            }
        }

        var lotteryId = string.IsNullOrEmpty(curSelectData.LotteryId) ? _gashaponData.Id : curSelectData.LotteryId;
        RequestExchange(lotteryId, curSelectData.ExchangeId, OnExchangeSuccess);
    }

    private void OnTryOnBtnClick()
    {

    }

    public void ShowExchangeReward(ExchangeDataRsp redeemClaimResponse)
    {
        if (redeemClaimResponse == null)
        {
            return;
        }

        List<CommonRewardData> redeemClaimRewardsItems = redeemClaimResponse.rewardList;
        var pairList = redeemClaimResponse.backpackData?.pairList;
        if ((redeemClaimRewardsItems == null || redeemClaimRewardsItems.Count <= 0) && (pairList == null || pairList.Count <= 0))
        {
            return;
        }
        GashaponDataManager.Inst.ShowGashaponReward(this.gameObject, redeemClaimRewardsItems, pairList, _info);
        //
        var panel = UIManager.Inst.FindPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
        //修改额外背景
        if (_gashaponData != null && panel != null)
        {
            if (_gashaponData.Id == "lottery.ponyVehicle")
            {
                var viewPath = GashaponUtils.ViewBasePath + "PonyGashapon/preview_bg.png";
                var sprite = XAssetLoaderMgr.Inst.LoadResource<Sprite>(viewPath, gameObject);

                UIManager.Inst.FindPanel<CommonRewardPanel>(PanelId.CommonRewardPanel).ShowAdditionalBg(sprite);
            }
            else if (_gashaponData.Id == "lottery.musicalPudding.hotAirBalloon")
            {
                var viewPath = GashaponUtils.ViewBasePath + "MusicNoteGashapon/preview_bg.png";
                var sprite = XAssetLoaderMgr.Inst.LoadResource<Sprite>(viewPath, gameObject);
                UIManager.Inst.FindPanel<CommonRewardPanel>(PanelId.CommonRewardPanel).ShowAdditionalBg(sprite);
            }
        }
    }

    #region 网络请求

    private void RequestExchange(string gashaId, int exchangeId, Action<ExchangeDataRsp> onSuccess, Action<string> onFail = null)
    {
        JObject jObject = new JObject()
        {
            ["lotteryId"] = gashaId,
            ["productId"] = exchangeId
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.GashaponReddem, HttpMethod.POST, JsonConvert.SerializeObject(jObject), (content) =>
            {
                ExchangeDataRsp response = JsonConvert.DeserializeObject<ExchangeDataRsp>(content);
                onSuccess?.Invoke(response);
            },
            (error) =>
            {
                HttpResponseRawData rsp = JsonConvert.DeserializeObject<HttpResponseRawData>(error);
                if (rsp != null && !string.IsNullOrEmpty(rsp.rmsg))
                {
                    TipPanel.ShowToast(rsp.rmsg);
                    onFail?.Invoke(rsp.rmsg);
                }
                else
                {
                    onFail?.Invoke("");
                }
            });
    }

    private void OnExchangeSuccess(ExchangeDataRsp dataRsp)
    {
        AccountDataManager.Inst.RequestAvatarFrameOrChat();
        ShowExchangeReward(dataRsp);

        foreach (var itemNode in items)
        {
            var exchangeInfo = itemNode.GetBindData();
            if (exchangeInfo != null)
            {
                bool isOwned = GashaponUtils.IsOwnedReward(exchangeInfo);
                if (isOwned)
                {
                    itemNode.SetOwnedUI(true);
                    if (curSelectItem == itemNode)
                    {
                        BuyBtn.gameObject.SetActive(false);
                    }
                }
            }
        }

        // 娃娃机特殊列表(SpecialContent)的已拥有态刷新
        for (int i = 0; i < specialItems.Count && i < _specialDataList.Count; i++)
        {
            if (GashaponUtils.IsOwnedReward(_specialDataList[i]))
                specialItems[i].SetOwnedStatus(true);
        }
        // 多型号视图锁状态刷新
        if (_isWawaji && mutipleTypeView != null) mutipleTypeView.RefreshOwned();
        // 当前选中项已拥有则隐藏购买按钮
        if (_info != null && GashaponUtils.IsOwnedReward(_info)) BuyBtn.gameObject.SetActive(false);
    }


    #endregion



}
