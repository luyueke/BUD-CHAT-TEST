using System.Collections.Generic;
using Game.Event;
using Message;
using UI.Base;
using UI.UIPanels.GashaponPanel;
using UnityEngine;
using UnityEngine.UI;
using SuperTreeView;
using Game.Audio;

public class StoreMallPanel : BasePanel<StoreMallPanel>
{
    [SerializeField] private Transform BG;
    [SerializeField] private Button backBtn;
    [SerializeField] private RectTransform bannerRoot;
    [SerializeField] private Transform topViewContainer;
    [SerializeField] private TreeView mTreeView;
    //private Dictionary<GashaponType, MailDefaultItem> mailItems = new Dictionary<GashaponType, MailDefaultItem>();
    private Dictionary<GashaponType, BaseGashaponView> gashaponViews = new Dictionary<GashaponType, BaseGashaponView>();

    private List<SectionsItem> sections = new List<SectionsItem>();
    private Dictionary<int, SectionsItem> sectionDic = new Dictionary<int, SectionsItem>();
    private List<LotteryItem> lotterys = new List<LotteryItem>();

    private GashaponType currentGashaponType = GashaponType.Unknown;
    private GashaponType loadingGashaponType = GashaponType.Unknown;
    public override void OnCreate()
    {
        BusinessLiveManager.Inst.AddConfigUpdateListener(OnBusinessConfigUpdate);
        backBtn.onClick.AddListener(CloseSelf);
        //InitData();
        CheckBusinessLive();
    }

    public override void OnShow(params object[] args) {
        base.OnShow(args);
      
        if (args.Length > 0) {
            var gashaponId = args[0] as string;
            var viewCfg = GashaponDataManager.Inst.GetGashaponView(gashaponId);
            if (viewCfg != null) {
                OnClickItemView(((GashaponType)viewCfg.ViewId));
            }else
            {
             //   OnClickItemView(GashaponType.MagicBoomBoomBoom);
            }
        }else
        {
          //  OnClickItemView(GashaponType.MagicBoomBoomBoom);
        }
        EventCenterDataManager.Inst.ReportTask(PostEventId.ViewSleepyKoi);
    }

    protected override void OnDestroy()
    {
        MessageHelper.Broadcast(MessageName.RefreshGroupConsumeApplyData);
        base.OnDestroy();
        BusinessLiveManager.Inst.RemoveConfigUpdateListener(OnBusinessConfigUpdate);
        MessageHelper.Broadcast(MessageName.OnStoreMallPanelClose);
        // 兜底：关闭商城时清除娃娃机图标覆盖，避免残留污染其它界面
        UI.Manager.PgcUtils.CurrencyIconScopeOverride = null;
        UI.Manager.PgcUtils.RewardIconScopeOverride = null;
        UI.Manager.PgcUtils.CurrencyNameScopeOverride = null;
    }


    private void InitData() {

        var businessConfig = BusinessLiveManager.Inst.GetBusinessConfig();
        var lottery = businessConfig.lottery;
        if (lottery == null || lottery.sections == null || lottery.sections.Count == 0)
        {
            Debug.LogError("扭蛋分类配置为空");
            return;
        }
        mTreeView.Clear();
        mTreeView.OnTreeListAddOneItem = OnTreeListAddOneItem;
        mTreeView.OnTreeListDeleteOneItem = OnTreeListDeleteOneItem;
        mTreeView.OnItemExpandBegin = OnItemExpandBegin;
        mTreeView.OnItemCollapseBegin = OnItemCollapseBegin;
        mTreeView.OnItemCustomEvent = OnItemCustomEvent;
        mTreeView.InitView();

        sections.Clear();
        lotterys.Clear();
        sectionDic.Clear();
        for (int i = 0; i < lottery.sections.Count; i++)
        {
            TreeViewItem item = mTreeView.AppendItem("ItemPrefab1");
            var sItem = item.GetComponent<SectionsItem>();
            var list = sItem.SetItemInfo(item, lottery.sections[i]);
            sections.Add(sItem);
            lotterys.AddRange(list);
            for(int j = 0;j<list.Count;j++)
            {
                if (!sectionDic.ContainsKey(list[j].gashaponData.ViewId))
                {
                    sectionDic.Add(list[j].gashaponData.ViewId, sItem);
                }

            }
        }

        //以下找到第一个打开项

        for (int i = 0; i < lottery.sections.Count; i++)
        {
            bool isFind = false;
            for (int j = 0; j < lottery.sections[i].list.Count; j++)
            {
                var lotteryId = lottery.sections[i].list[j].lotteryId.Trim();
                var _gashaponData = GashaponDataManager.Inst.GetGashaponView(lotteryId);
                if (_gashaponData !=null && BusinessLiveManager.Inst.IsGashaponLive((int)_gashaponData.ViewId))
                {
                    OnClickItemView((GashaponType)_gashaponData.ViewId);
                    isFind = true;
                    break;
                }
            }
            if(isFind) //找到第一个就跳出循环
            {
                break;
            }
        }
        //var gashaponList = new List<GashaponType>() {
        //    GashaponType.Circus,
        //    GashaponType.StarryNightFairyTale,
        //    GashaponType.SweetheartBall,
        //    GashaponType.WitchLuna,
        //    GashaponType.DemonRaven,
        //    GashaponType.FantasyBunny,
        //    GashaponType.RadiantApollo,
        //    GashaponType.MidAutumn,
        //    GashaponType.FriesFun,
        //    GashaponType.MagicBoomBoomBoom,
        //    GashaponType.ZongXiaFuYao,
        //    GashaponType.FlowerCarriage,
        //    GashaponType.Gyaru,
        //    GashaponType.DarkShadowFeather,
        //    GashaponType.FrostKeeper,
        //    GashaponType.WelcomeSpring,
        //    GashaponType.HeavenlyMatch,
        //    GashaponType.NewYearDrama,
        //    GashaponType.GodOfWealth,
        //    GashaponType.WindKeeper,
        //    GashaponType.SnowDream,
        //    GashaponType.LuckyStar,
        //    GashaponType.JingleBells,
        //    GashaponType.PurpleDream,
        //    GashaponType.MusicFestival,
        //    GashaponType.Pet,
        //    GashaponType.NewCottageCore,
        //    GashaponType.Coin,
        //};

        //GashaponType selectType = GashaponType.Unknown;
        //foreach (var type in gashaponList) {
        //    var viewCfg = GashaponDataManager.Inst.GetGashaponViewCfg(type);
        //    if (viewCfg == null)
        //    {
        //        continue;
        //    }
        //    GameObject bannerObj = null;
        //    if (string.IsNullOrEmpty(viewCfg.BannerPrefab)) {
        //        bannerObj = Loader.Load<GameObject>("Assets/Loadable/UI/UIPanel/StorePanel/MallDefaultItem.prefab").Instantiate(bannerRoot);
        //    } else {
        //        bannerObj = Loader.Load<GameObject>(viewCfg.BannerPrefab).Instantiate(bannerRoot);
        //    }

        //    var bannerItem = bannerObj.GetComponent<MailDefaultItem>();
        //    bannerItem.SetData(viewCfg, OnClickItemView);
        //    bannerItem.CheckReddot();
        //    mailItems.Add(type, bannerItem);

        //    if (!BusinessLiveManager.Inst.IsGashaponLive((int)type)) {
        //        bannerItem.gameObject.SetActive(false);
        //    }
        //    if (selectType == GashaponType.Unknown && bannerItem.gameObject.activeSelf)
        //    {
        //        selectType = type;
        //    }
        //}

    }

    private void CheckBusinessLive()
    {
        InitData();
        //foreach (var keyValue in mailItems) {
        //    if (BusinessLiveManager.Inst.IsGashaponLive((int)keyValue.Key)) {
        //        keyValue.Value.gameObject.SetActive(true);
        //    } else {
        //        keyValue.Value.gameObject.SetActive(false);
        //    }
        //}
    }

    private void OnBusinessConfigUpdate(BusinessLiveConfig config)
    {
        if (mTreeView != null && mTreeView.ItemCount > 0 && currentGashaponType != GashaponType.Unknown) //当前页已经显示了，不要切
        {
            return;
        }
            CheckBusinessLive();
    }

    private void OnClickItemView(GashaponType type) {
        if (currentGashaponType == type) {
            return;
        }


        //if (!mailItems.ContainsKey(type)) {
        //    LoggerUtils.LogError("该扭蛋当前已过期 或者 未配置:" + type);
        //    return;
        //}

        //if (mailItems.TryGetValue(currentGashaponType, out var lastMailItem)) {
        //    lastMailItem.SetSelected(false);
        //}

        for (int i = 0; i < lotterys.Count; i++)
        {
            if(lotterys[i].gashaponData.ViewId == (int)type)
            {
                lotterys[i].IsSelected = true;
            }else
            {
                lotterys[i].IsSelected = false;
            }
        }
        //for(int i=0;i<sections.Count;i++)
        //{
        //    sections[i].IsSelected = false;
        //}
        if (sectionDic.TryGetValue((int)type, out SectionsItem item))
        {
            item.IsSelected = true;
          //  item.CheckIsExpand();
        }

        if (gashaponViews.TryGetValue(currentGashaponType, out var lastGashaponView)) {
            lastGashaponView.OnHide();
            lastGashaponView.gameObject.SetActive(false);
        }

        currentGashaponType = type;
        //if (mailItems.TryGetValue(currentGashaponType, out var mailItem)) {
        //    mailItem.SetSelected(true);
        //}
        if (!gashaponViews.TryGetValue(currentGashaponType, out var gashaponView)) {
            var viewCfg = GashaponDataManager.Inst.GetGashaponViewCfg(type);
            var targetType = currentGashaponType;

            if (!string.IsNullOrEmpty(viewCfg.ViewPrefab)) {
                // 防止同一类型重复触发异步加载
                if (loadingGashaponType == targetType) return;
                loadingGashaponType = targetType;

                Loader.LoadAsyncOrSync<GameObject>(viewCfg.ViewPrefab, (isSuccess, wrapper) =>
                {
                    loadingGashaponType = GashaponType.Unknown;

                    // 加载完成时面板可能已关闭或又切到别的类型
                    if (this == null || !this.gameObject) return;
                    if (gashaponViews.ContainsKey(targetType)) return;

                    if (!isSuccess || wrapper == null)
                    {
                        Debug.LogError("资源加载失败 viewCfg.ViewPrefab=" + viewCfg.ViewPrefab);
                        return;
                    }

                    var view = wrapper.Instantiate(BG).GetComponent<BaseGashaponView>();
                    LotteryConfig conf = GetLotteryConfig(viewCfg.GashaId);
                    if (conf != null) view.SetActivityTime(conf.startTime, conf.endTime);

                    view.OnCreate(viewCfg.GashaId);
                    view.SetPanelTopContainer(topViewContainer);
                    gashaponViews.Add(targetType, view);

                    // 只有当前仍是目标类型时才激活显示
                    if (currentGashaponType == targetType)
                    {
                        view.gameObject.SetActive(true);
                        view.OnShow();
                        AkSoundManager.Inst.PlayUIEffectSound(UISoundType.UI_ShiftItems_B2);
                    }
                });
                return;
            } else {
                gashaponView = Loader.Load<GameObject>("Assets/Loadable/UI/UIPanel/GashaponPanel/DefaultView/DefaultGashaponView.prefab").Instantiate(BG).GetComponent<BaseGashaponView>();
            }
            gashaponView.OnCreate(viewCfg.GashaId);
            gashaponView.SetPanelTopContainer(topViewContainer);
            gashaponViews.Add(currentGashaponType, gashaponView);
        }
        gashaponView.gameObject.SetActive(true);
        gashaponView.OnShow();
        AkSoundManager.Inst.PlayUIEffectSound(UISoundType.UI_ShiftItems_B2);
    }

    private LotteryConfig GetLotteryConfig(string lotteryId)
    {
        var businessConfig = BusinessLiveManager.Inst.GetBusinessConfig();
        var lottery = businessConfig.lottery;
        if (lottery == null || lottery.sections == null || lottery.sections.Count == 0)
        {
            Debug.LogError("扭蛋分类配置为空");
            return null;
        }
        for (int i = 0; i < lottery.sections.Count; i++)
        {
            for (int j = 0; j < lottery.sections[i].list.Count; j++)
            {
                if (lotteryId == lottery.sections[i].list[j].lotteryId)
                {
                    return lottery.sections[i].list[j];
                }
            }
        }
        return null;
    }

    #region TreeView
    int mCurSelectedItemId = 0;

    int mNewItemCount = 0;
    void OnItemExpandBegin(TreeViewItem item)
    {
        SectionsItem st = item.GetComponent<SectionsItem>();
        st.IsSelected =true;
    }

    void OnItemCollapseBegin(TreeViewItem item)
    {
        SectionsItem st = item.GetComponent<SectionsItem>();
        //st.SetExpandStatus(false);
        st.IsSelected = false;
    }

    void OnItemCustomEvent(TreeViewItem item, CustomEvent customEvent, System.Object param)
    {

        if (mCurSelectedItemId > 0)
        {
            if (item.ItemId == mCurSelectedItemId)
            {
                return;
            }
            mCurSelectedItemId = 0;
        }
        if (customEvent == CustomEvent.ItemClicked)
        {
            LotteryItem lt = item.GetComponent<LotteryItem>();
            OnClickItemView((GashaponType)lt.gashaponData.ViewId);
        }
        //else if (customEvent == CustomEvent.MenuClicked)
        //{
        //    for (int i = 0; i < sections.Count; i++)
        //    {
        //        sections[i].IsSelected = false;
        //    }
        //    SectionsItem st = item.GetComponent<SectionsItem>();
        //    st.IsSelected = true;
        //}
        mCurSelectedItemId = item.ItemId;
    }

    void OnTreeListAddOneItem(TreeList treeList)
    {
        int count = treeList.ItemCount;
        TreeViewItem parentTreeItem = treeList.ParentTreeItem;
        if (count > 0 && parentTreeItem != null)
        {
            SectionsItem st = parentTreeItem.GetComponent<SectionsItem>();
            st.IsSelected = true;
            //st.SetExpandBtnVisible(true);
            //st.SetExpandStatus(parentTreeItem.IsExpand);
        }
    }

    void OnTreeListDeleteOneItem(TreeList treeList)
    {
        int count = treeList.ItemCount;
        TreeViewItem parentTreeItem = treeList.ParentTreeItem;
        if (count == 0 && parentTreeItem != null)
        {
            SectionsItem st = parentTreeItem.GetComponent<SectionsItem>();
            st.IsSelected = false;
            //st.SetExpandBtnVisible(false);
        }
    }

    TreeViewItem CurSelectedItem
    {
        get
        {
            if (mCurSelectedItemId <= 0)
            {
                return null;
            }
            TreeViewItem item = mTreeView.GetTreeItemById(mCurSelectedItemId);
            if (item == null)
            {
                mCurSelectedItemId = 0;
                return null;
            }
            return item;
        }
    }

    #endregion
}
