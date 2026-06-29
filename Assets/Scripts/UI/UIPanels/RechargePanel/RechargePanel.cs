using System;
using System.Collections.Generic;
using System.Linq;
using Game.Event;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UI.Base;
using UI.BaseWidgets;
using UI.UIPanels.RechargePanel;
using UnityEngine;


/// <summary>
/// Author:
/// Desc:
/// Date:24-07-05 18:47:03
/// </summary>
public class RechargePanel : BasePanel<RechargePanel> {
    public static string RechargePanelAtlas = "Assets/Loadable/UI/RechargePanel/RechargePanel.spriteatlas";

    public static string ConsumptionTicketPanelAtlas = "Assets/Loadable/UI/RechargePanel/ConsumptionTicketPanel/ConsumptionTicketPanel.spriteatlas";

    public static string CumulativeRechargePanelAtlas = "Assets/Loadable/UI/RechargePanel/CumulativeRechargePanel/CumulativeRechargePanel.spriteatlas";

    public static string S4ShiYuanPackPanelAtlas = "Assets/Loadable/UI/RechargePanel/S4ShiYuanPackPanel/S4ShiYuanPackPanel.spriteatlas";

    public static string S11ShiYuanPackPanelAtlas = "Assets/Loadable/UI/UIPanel/S11ShiYuanPackgePanel/S11ShiyuanPack.spriteatlas";

    public static string PinkCoinPanelAtlas = "Assets/Loadable/UI/RechargePanel/PinkCoinPanel/PinkCoinPanel.spriteatlas";

    [SerializeField] private CButton BackBtn;

    [SerializeField] private GameObject rechargeTabObj;
    [SerializeField] private Transform tabContent;

    [SerializeField] private Transform viewContent;
    private int targetId = -1;

    private bool isRequesting = false;


    private List<RechargeId> rechargeIds = new List<RechargeId>() {
    	RechargeId.BabyShrimpGiftPack,
        RechargeId.zzzGiftPack,
        RechargeId.ShovelPack,
            RechargeId.S15SeasonRecharge,
         RechargeId.SeasonPrePack,
         RechargeId.WaCoinPack,
         RechargeId.S14SeasonRecharge,
     
         RechargeId.QianQianWanWanGift,
        RechargeId.SpringLimited,
        RechargeId.BuyReducePanel,
        RechargeId.LoverGift,
        RechargeId.FirstChargeOneYuan,
        RechargeId.MiserableNursePromise,
        RechargeId.S8CommunityCoinPack,
        RechargeId.S8TwentyYuanPackage,
        RechargeId.S12SeasonRecharge,
        RechargeId.S7PhoenixPackage,
        RechargeId.NewYearsFortune,
        RechargeId.SleepyCoi,
        RechargeId.LimitedRechargeGiftPack,
        //RechargeId.CatGiftPack,
        RechargeId.S9LimitedTimeCurrencyPack,
        RechargeId.FirstChargeSixYuan,
        RechargeId.DailyRechargePanel,
        RechargeId.ShiyuanGiftPack,
        RechargeId.LiuyuanGiftPack,
        RechargeId.FurryPack,
        RechargeId.DreamyVioletPack,
        RechargeId.PeachContiPack,
        RechargeId.VipMonthPack,
        RechargeId.MonthCard,
        RechargeId.Y2kSnowboard,
        RechargeId.GemPack,
        RechargeId.RedeemCode,
    };

    private List<RechargeId> activityTypeList = new List<RechargeId>()
    {
        RechargeId.SeasonPrePack,
        RechargeId.WaCoinPack,
        RechargeId.SpringLimited,
        RechargeId.LoverGift,
        RechargeId.QianQianWanWanGift,
        RechargeId.Y2kSnowboard,
        RechargeId.BuyReducePanel,
        RechargeId.NewYearsFortune,
        RechargeId.SleepyCoi
    };


    private List<RechargeId> activityList = new List<RechargeId>()
    {
        RechargeId.S14SeasonRecharge,
        RechargeId.S15SeasonRecharge,
        RechargeId.FirstChargeOneYuan,
        RechargeId.FirstChargeSixYuan,
        RechargeId.S12SeasonRecharge,
    };

    private Dictionary<RechargeId, PaidPackageType> rechargeTypeDic = new Dictionary<RechargeId, PaidPackageType>() {
        { RechargeId.FurryPack, PaidPackageType.FurryPack },
        { RechargeId.DreamyVioletPack, PaidPackageType.DreamyVioletPack },
        { RechargeId.PeachContiPack, PaidPackageType.PeachContiPack },
        { RechargeId.S7PhoenixPackage, PaidPackageType.S7PhoenixPackage},
        { RechargeId.S8TwentyYuanPackage, PaidPackageType.S8TwentyYuanPackage},
        { RechargeId.S8CommunityCoinPack, PaidPackageType.S8CommunityCoinPack},
        { RechargeId.S9LimitedTimeCurrencyPack, PaidPackageType.S9LimitedTimeCurrencyPack}
    };

    private Dictionary<RechargeId, TASK_ID> packageTypeDic = new Dictionary<RechargeId, TASK_ID>() {
        { RechargeId.MiserableNursePromise, TASK_ID.MiserableNursePromise},
        { RechargeId.FurryPack, TASK_ID.S6OnePack },
        { RechargeId.DreamyVioletPack, TASK_ID.S6TenPack },
        { RechargeId.PeachContiPack, TASK_ID.S6SixPack },
        { RechargeId.S7PhoenixPackage, TASK_ID.S7PhoenixPack},
        { RechargeId.S8CommunityCoinPack, TASK_ID.S8CommunityCoinPack},
    };

    private Dictionary<RechargeId, RechargeTabItem> rechargeTabItems = new Dictionary<RechargeId, RechargeTabItem>();
    private Dictionary<RechargeId, GameObject> rechargeDic = new Dictionary<RechargeId, GameObject>();

    private RechargeId currentRechargeId = RechargeId.ErrRechargeId;


    public override void OnCreate() {
        InitData();
        BackBtn.onClick.AddListener(CloseSelf);
        BusinessLiveManager.Inst.AddConfigUpdateListener(OnBusinessConfigUpdate);
        MessageHelper.AddListener(MessageName.ReddotNotice, OnRefreshNotive);
        EventCenterDataManager.Inst.SetActivityDataCallback((() =>
        {
            RefreshTabs();
        }));

    }

    public override void CloseSelf()
    {
        MessageHelper.Broadcast(MessageName.RechargePanelClose);
        base.CloseSelf();

    }

    protected override void OnDestroy() {
        base.OnDestroy();
        BusinessLiveManager.Inst.RemoveConfigUpdateListener(OnBusinessConfigUpdate);
        MessageHelper.RemoveListener(MessageName.ReddotNotice, OnRefreshNotive);
        EventCenterDataManager.Inst.ClearActivityDataCallback();
    }

    private void OnBusinessConfigUpdate(BusinessLiveConfig config) {

        RefreshTabs();
    }

    private void RefreshTabs()
    {
        List<RechargeId> curRechargeList = rechargeTabItems.Keys.ToList();
        bool isResetTab = false;
        if (curRechargeList != null && curRechargeList.Count > 0)
        {
            foreach (var rechargeId in curRechargeList) {
                // 后端配置是否下线
                if (!CheckRechargeTab(rechargeId))
                {
                    if (rechargeTabItems.TryGetValue(rechargeId, out RechargeTabItem item)) {
                        item.gameObject.SetActive(false);
                        if (currentRechargeId == rechargeId) {
                            isResetTab = true;
                        }
                    }
                }
            }
        }

        if (isResetTab) {
            var selectRechargeTabItem = rechargeTabItems.FirstOrDefault(tmp => tmp.Value.gameObject.activeSelf);
            if (selectRechargeTabItem.Value != null) {
                OnTabClick(selectRechargeTabItem.Key);
            }
        }

    }

    public void RefreshActivityData()
    {
        if (isRequesting)
        {
            return;
        }
        isRequesting = true;
        ActivityCenterInfoReq req = new ActivityCenterInfoReq();
        req.idList = activityList.Select(id => id.ToString()).ToList();
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ActivityList, HttpMethod.POST,
            JsonConvert.SerializeObject(req), (content) =>
            {
                isRequesting = false;
                ActivityResponse activityResponse = JsonConvert.DeserializeObject<ActivityResponse>(content);
                if (activityResponse.list != null)
                {
                    OnGetActivityListSuccess(activityResponse.list);
                }
            },
            (error) =>
            {
                isRequesting = false;
            });
    }

    private void OnGetActivityListSuccess(List<ActivityInfo> activityInfoList)
    {
        List<RechargeId> curRechargeList = rechargeTabItems.Keys.ToList();
        foreach (RechargeId aRechargeId in activityList)
        {
            var activityInfo = activityInfoList.Find(x => x.activityId == aRechargeId.ToString());
            if (activityInfo == null) continue;
            {
                var rechargeId = curRechargeList.Find(x => x == aRechargeId);
                if (!rechargeTabItems.TryGetValue(rechargeId, out RechargeTabItem item)) continue;
                if (activityInfo.activityStatus == 2)
                {
                    HideRechargeTab(rechargeId);
                    if (rechargeId == RechargeId.LiuyuanGiftPack) 
                    {
                        S4ShiYuanPackPanel.GetTaskList(() => {
                            item.gameObject.SetActive(true);
                        });
                    }
                    if (rechargeId == RechargeId.ShiyuanGiftPack)
                    {
                        S11ShiYuanPackPanel.GetTaskList(() =>
                        {
                            item.gameObject.SetActive(true);
                        });
                    }
                }
                else
                {
                    item.gameObject.SetActive(true);
                }
            }
        }
    }

    private bool CheckRechargeTab(RechargeId rechargeId) {
        if (activityTypeList.Contains(rechargeId) && !BusinessLiveManager.Inst.IsRechargeActivityLive(rechargeId.ToString()))
        {
            return false;
        }
        if (rechargeTypeDic.TryGetValue(rechargeId, out var paidPackageType) )
        {
            var isPaid = IAPDataManager.Inst.GetIsPaid((int)paidPackageType);
            if (!isPaid)
            {
                var isPaidPackLive = BusinessLiveManager.Inst.IsPaidPackLive((int)paidPackageType);
                if (!isPaidPackLive)
                {
                    return false;
                }
            }
            else
            {
                if (packageTypeDic.TryGetValue(rechargeId, out var taskId))
                {
                    var isTaskEnable = EventCenterDataManager.Inst.CheckTaskIsEnable(taskId);
                    if (!isTaskEnable)
                    {
                        return false;
                    }
                }
            }
        }
        
        if (rechargeId == RechargeId.DailyRechargePanel)
        {
            var businessConfig = BusinessLiveManager.Inst.GetBusinessConfig();
            if (businessConfig == null || businessConfig.recharge == null || businessConfig.recharge.activityList == null )
            {
                return false;
            }
            if (!businessConfig.recharge.activityList.Contains("56"))
            {
                return false;
            }
        }
        if(rechargeId == RechargeId.LiuyuanGiftPack)
        {
            var businessConfig = BusinessLiveManager.Inst.GetBusinessConfig();
            if (businessConfig == null || businessConfig.recharge.paidPackageList == null || !businessConfig.recharge.paidPackageList.Contains("3"))
            {
                S4ShiYuanPackPanel.GetTaskList(() =>
                {
                    if (rechargeTabItems.TryGetValue(rechargeId, out RechargeTabItem item))
                    {
                        item.gameObject.SetActive(true);
                    }
        
                });
                return false;
            }
        }

        if(rechargeId == RechargeId.CatGiftPack)
        {
           var miaoPackage =  IAPDataManager.Inst.GetMiaoCoinPackages();
            return miaoPackage != null;
        }

        if (rechargeId == RechargeId.ShovelPack)
        {
            return IAPDataManager.Inst.productRes?.chasingWavesPackageList != null;
        }


        if (rechargeId == RechargeId.ShiyuanGiftPack)
        {
            var businessConfig = BusinessLiveManager.Inst.GetBusinessConfig();
            if (businessConfig == null || businessConfig.recharge.paidPackageList == null || !businessConfig.recharge.paidPackageList.Contains("4"))
            {
                S11ShiYuanPackPanel.GetTaskList(() =>
                {
                    if (rechargeTabItems.TryGetValue(rechargeId, out RechargeTabItem item))
                    {
                        item.gameObject.SetActive(true);
                    }

                });
                return false;
            }
        }
        
        if(rechargeId == RechargeId.BabyShrimpGiftPack)
        {
            return IAPDataManager.Inst.GetXiaXiaZaiCoinPackages() != null;
        }

         if(rechargeId == RechargeId.zzzGiftPack)
        {
            return IAPDataManager.Inst.GetPhantomSoundPartyCoinPackages() != null;
        }
        if ( rechargeId == RechargeId.SpringLimited && !IAPDataManager.Inst.IsNewYearLimitedPackageLive())
        {
            return false;
        }

        if (rechargeId == RechargeId.DreamyVioletPack && !EventCenterDataManager.Inst.CheckTaskIsEnable(TASK_ID.S6TenPack))
        {
            return false;
        }
        if (rechargeId == RechargeId.S9LimitedTimeCurrencyPack && !EventCenterDataManager.Inst.CheckTaskIsEnable(TASK_ID.S9LimitedTimePack))
        {
            return false;
        }
        

        if (activityList.Contains(rechargeId) && !EventCenterDataManager.Inst.CheckActivityIsEnable(rechargeId.ToString()))
        {
            return false;
        }
        
        if (rechargeId == RechargeId.MiserableNursePromise && !EventCenterDataManager.Inst.CheckTaskIsEnable(TASK_ID.MiserableNursePromise))
        {
            return false;
        }


        if (rechargeId == RechargeId.RedeemCode)
        {
#if UNITY_IOS
            return GlobalConfigManager.Instance.IsShowAppleAudit ? false : true;
            //return IAPDataManager.Inst.productRes == null ? false : !IAPDataManager.Inst.productRes.disableRedeem;
#elif UNITY_ANDROID
            return true;
#endif
        }
        return true;
    }


    public override void OnShow(params object[] args) {
        base.OnShow(args);
        if (args.Length > 0) {
            var rechargeId = (RechargeId)args[0];
            // 检查是否有第二个参数，并且是 int (目标 skipId)
            if (args.Length > 1 && args[1] is int)
            {
                // 只有当目标主视图是 LimitedRechargeGiftPack 时，才存储 skipId
                if (rechargeId == RechargeId.LimitedRechargeGiftPack)
                {
                    targetId = (int)args[1];
                }
            }
            OnTabClick(rechargeId);
        }

        OnRefreshNotive();
    }


    private void InitData() {
        RechargeId selectId = RechargeId.ErrRechargeId;
        foreach (var rechargeId in rechargeIds) {
            var viewCfg = RechargeDataManager.Inst.GetRechargeViewConfig(rechargeId);
            if (viewCfg != null) {
                var obj = Instantiate(rechargeTabObj, tabContent);
                obj.transform.localScale = Vector3.one;
                obj.SetActive(true);
                RechargeTabItem tab = obj.GetComponent<RechargeTabItem>();
                tab.Init(viewCfg, OnTabClick);
                rechargeTabItems.Add(rechargeId, tab);
                if (CheckRechargeTab(rechargeId)) {
                    obj.SetActive(true);
                    if (selectId == RechargeId.ErrRechargeId) {
                        selectId = rechargeId;
                    }
                } else {
                    if (rechargeId == RechargeId.MonthCard)
                    {
                        obj.SetActive(true);
                    }else{
                        obj.SetActive(false);
                    }
                }


            }
        }
        OnTabClick(selectId);

        RefreshActivityData();
    }

    public void OnTabClick(RechargeId rechargeId) {
        if (currentRechargeId == rechargeId) {
            return;
        }


        if (!rechargeTabItems.ContainsKey(rechargeId)) {
            LoggerUtils.LogError("该礼包已过期 或者 未配置:" + rechargeId);
            return;
        }


        if (rechargeTabItems.TryGetValue(currentRechargeId, out var lastTabItem)) {
            lastTabItem.SetSelect(false);
        }

        if (rechargeDic.TryGetValue(currentRechargeId, out var lastGashaponView)) {
            lastGashaponView.gameObject.SetActive(false);
        }

        currentRechargeId = rechargeId;
        if (rechargeTabItems.TryGetValue(currentRechargeId, out var tabItem)) {
            tabItem.SetSelect(true);
        }
        if (!rechargeDic.TryGetValue(currentRechargeId, out var rechargeView)) {
            var viewCfg = RechargeDataManager.Inst.GetRechargeViewConfig(currentRechargeId);
            rechargeView = Loader.Load<GameObject>(viewCfg.ViewPrefab).Instantiate(viewContent);
            if (rechargeView.TryGetComponent<SeasonCumulativeView>(out var seasonCumulativeView))
                seasonCumulativeView.Initialize(currentRechargeId);
            if(!rechargeDic.ContainsKey(currentRechargeId))
                rechargeDic.Add(currentRechargeId, rechargeView);
        }
        rechargeView.SetActive(true);
        if (currentRechargeId == RechargeId.LimitedRechargeGiftPack && targetId != -1)
        {
            HandleSubViewSkip();
        }
    }


    public void HideRechargeTab(RechargeId rechargeId) {
        if (rechargeTabItems.TryGetValue(rechargeId, out RechargeTabItem item)) {
            item.gameObject.SetActive(false);
            if (currentRechargeId == rechargeId) {
                var selectRechargeTabItem = rechargeTabItems.FirstOrDefault(tmp => tmp.Value.gameObject.activeSelf);
                if (selectRechargeTabItem.Value != null) {
                    OnTabClick(selectRechargeTabItem.Key);
                }
            }
        }
    }

    private void OnRefreshNotive() {
        if (rechargeTabItems != null) {
            foreach (var keyValue in rechargeTabItems) {
                if (ReddotManagerUtils.Inst.HasRechargeRedDot((int)keyValue.Key)) {
                    keyValue.Value.SetRedDot(true);
                } else {
                    keyValue.Value.SetRedDot(false);
                }
            }
        }

    }

    // 新增：用于处理子视图跳转的辅助方法
    private void HandleSubViewSkip()
    {
        if (currentRechargeId == RechargeId.LimitedRechargeGiftPack)
        {
            if (rechargeDic.TryGetValue(currentRechargeId, out var currentViewObject))
            {
                // 获取 LimitedRechargeGiftPack 组件
                var limitedPackComponent = currentViewObject.GetComponent<LimitedRechargeGiftPack>();
                
                    limitedPackComponent._curSkipId = targetId;
            }
            else
            {
                LoggerUtils.LogError($"无法在 rechargeDic 中找到当前视图对象: {currentRechargeId}");
            }
            // 不论是否成功找到组件，都清除掉暂存的 ID，避免下次误用
            targetId = -1;
        }
    }




}

