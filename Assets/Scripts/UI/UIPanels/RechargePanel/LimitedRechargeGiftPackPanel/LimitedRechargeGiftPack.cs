using System.Collections;
using System.Collections.Generic;
using Message;
using UI.UIPanels.RechargePanel;
using UnityEngine;

public class LimitedRechargeGiftPack : MonoBehaviour
{
    [SerializeField] private Transform BG;

    [SerializeField] private Transform scrollContent;
    [SerializeField] private LogicToggleGroupItem toggleGroupItem;
    [SerializeField] private TabScrollGroup _scrollGroup;
    [SerializeField] private GameObject ValentineTabItem;

    public BaseLimitedPackageItem WinPackage;
    public BaseLimitedPackageItem ValentinePackage;
    public BaseLimitedPackageItem NewYearPackage;
    public BaseLimitedPackageItem MustBuyPackage;
    public BaseLimitedPackageItem YouyouCoinPackage;
    public BaseLimitedPackageItem DailyDiscountPackage;
    public BaseLimitedPackageItem DailyHotSalePackage;
    public BaseLimitedPackageItem WeeklyPackage;
    public BaseLimitedPackageItem MonthlyPackage;
    public BaseLimitedPackageItem MagicCoinPackage;
    public BaseLimitedPackageItem ChirstmasCoinPackage;
    public BaseLimitedPackageItem LuckyCoinPackage;

    private LimitPackageData limitPackageData;
    public int _curSkipId = 0;
    
    private void Start()
    {
        
        InitBgView();
        //后期上线的礼包默认不可见
        ChirstmasCoinPackage.gameObject.SetActive(false);
        LuckyCoinPackage.gameObject.SetActive(false);

        
        toggleGroupItem.AddListenerUIAll(((tabName, isOn) =>
        {   

            LoggerUtils.Log("toggle invoked");
            ShowHideAllPackage(false);
            switch (tabName)
            {
                case "AllPack":
                    ShowHideAllPackage(true);
                    // CheckBusinessLive();
                    NewYearPackage.CheckPackLive();
                    WinPackage.CheckPackLive();
                    ValentinePackage.CheckPackLive();
                    DailyDiscountPackage.CheckPackLive();
                    LuckyCoinPackage.CheckPackLive();
                    ChirstmasCoinPackage.CheckPackLive();
                    MagicCoinPackage.CheckPackLive();
                    break;
                case "NewYearPack":
                    ShowHideAllPackage(false);
                    NewYearPackage.CheckPackLive();
                    break;
                case "ValentinePack":
                    ShowHideAllPackage(false);
                    ValentinePackage.CheckPackLive();
                    break;
                case "MustBuy":
                    ShowHideAllPackage(false);
                    MustBuyPackage.gameObject.SetActive(true);
                    break;
                case "SeasonGiftPack":
                    ShowHideAllPackage(false);
                    YouyouCoinPackage.gameObject.SetActive(true);
                    LuckyCoinPackage.CheckPackLive();
                    //ChirstmasCoinPackage.CheckPackLive();
                    //MagicCoinPackage.CheckPackLive();
                    break;
                case "DailyGiftPack":
                    ShowHideAllPackage(false);
                    DailyDiscountPackage.CheckPackLive();
                    DailyHotSalePackage.gameObject.SetActive(true);
                    break;
                case "WeekilyGiftPack":
                    ShowHideAllPackage(false);
                    WeeklyPackage.gameObject.SetActive(true);
                    break;
                case "MonthlyGiftPack":
                    ShowHideAllPackage(false);
                    MonthlyPackage.gameObject.SetActive(true);
                    break;
            }
        }));
        RefreshViewByLocal();
        InitScrollGroup();
        StartCoroutine(DisableScrollSyncCoroutine());
        MessageHelper.AddListener(MessageName.OnPurchaseLimitedPackageSuccess, GetDataByHttp);
        BusinessLiveManager.Inst.AddConfigUpdateListener(OnBusinessConfigUpdate);
        
        GetDataByHttp();
        WinPackage.CheckPackLive();
        EventTracking.LoadEvent.ReportPopupStatus(RechargeId.LimitedRechargeGiftPack.ToString());
    }

    IEnumerator DisableScrollSyncCoroutine()
    {
        // 等待当前帧结束
        yield return new WaitForEndOfFrame();

        // 空值检查
        if (_scrollGroup != null && _scrollGroup.uScrollView != null)
        {
            // 移除所有监听器
            _scrollGroup.uScrollView.onValueChanged.RemoveAllListeners();
        }
    }
    private void ShowHideAllPackage(bool isShow)
    {
        if (!isShow)
        {
            NewYearPackage.gameObject.SetActive(isShow);
            ValentinePackage.gameObject.SetActive(isShow);
            MagicCoinPackage.gameObject.SetActive(isShow);
            ChirstmasCoinPackage.gameObject.SetActive(isShow);
            LuckyCoinPackage.gameObject.SetActive(isShow);
        }
        DailyDiscountPackage.gameObject.SetActive(isShow);
        WinPackage.gameObject.SetActive(isShow);
        //ValentinePackage.gameObject.SetActive(isShow);
        MustBuyPackage.gameObject.SetActive(isShow);
        YouyouCoinPackage.gameObject.SetActive(isShow);
        DailyHotSalePackage.gameObject.SetActive(isShow);
        WeeklyPackage.gameObject.SetActive(isShow);
        MonthlyPackage.gameObject.SetActive(isShow);
    }

    public void OnInitCreated()
    {
        GetDataByHttp();
    }

    public void OnDestroy()
    {
        MessageHelper.RemoveListener(MessageName.OnPurchaseLimitedPackageSuccess, GetDataByHttp);
        BusinessLiveManager.Inst.RemoveConfigUpdateListener(OnBusinessConfigUpdate);
    }
    

    private void GetDataByHttp()
    {
        IAPDataManager.Inst.GetProductInfo(res =>
        {
            this.limitPackageData = res.limitPackageData;
            if (limitPackageData == null)
            {
                LoggerUtils.LogError("LimitedRechargeGiftPackPanel limitPackageData is NUll");
                return;
            }

            InitByData(this.limitPackageData);
        });
    }

    private void OnBusinessConfigUpdate(BusinessLiveConfig config)
    {
        CheckBusinessLive();
    }

    private void RefreshViewByLocal()
    {
        var packageData = IAPDataManager.Inst.GetLimitedRechargeData();
        if (packageData != null)
        {
            Debug.Log("packageData.maNianPackage=" + Newtonsoft.Json.JsonConvert.SerializeObject(packageData.maNianPackage));
            InitByData(packageData);
        }
    }

    private void InitByData(LimitPackageData packageData)
    {
        NewYearPackage.InitData(packageData.maNianPackage);
        ValentinePackage.InitData(packageData.lovePackage);
        MustBuyPackage.InitData(packageData.valuePackPackage);
        YouyouCoinPackage.InitData(packageData.youYouCoinPackage);
        DailyDiscountPackage.InitData(packageData.dailyDiscountPackage);
        DailyHotSalePackage.InitData(packageData.dailyHotSalePackage);
        WeeklyPackage.InitData(packageData.weeklyPackage);
        MonthlyPackage.InitData(packageData.monthlyPackage);
        MagicCoinPackage.InitData(packageData.magicCoinPackage);
        ChirstmasCoinPackage.InitData(packageData.christmasCoinPackage);
        LuckyCoinPackage.InitData(packageData.luckyCoinPackage);
        WinPackage.InitData(packageData.wingPackage);
        CheckBusinessLive();
        SkipToPage(_curSkipId);
        CheckFreePackReddot();
    }

    //只有每日免费领的礼包有红点要求
    private void CheckFreePackReddot()
    {
        var canClaim = ReddotManagerUtils.Inst.HasRechargeRedDot((int)RechargeId.LimitedRechargeGiftPack);
        toggleGroupItem.SetToggleReddot("DailyGiftPack", canClaim);
    }

    private void CheckBusinessLive()
    {
        NewYearPackage.CheckPackLive();
        ValentinePackage.CheckPackLive();
        ChirstmasCoinPackage.CheckPackLive();
        LuckyCoinPackage.CheckPackLive();
        InitScrollGroup();
    }

    private void InitBgView()
    {
        if (BG == null)
        {
            return;
        }

        string atlasPath = RechargePanel.RechargePanelAtlas;
        var itemObj = Loader
            .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
            .Instantiate(BG);
        var item = itemObj.GetComponent<ActivityCenterBgItem>();
        item.InitCustomBgItem("#9859FF", atlasPath, new List<string>()
        {
            "s4_limit_bg_1", "s4_limit_bg_2", "s4_limit_bg_3"
        });
        item.gameObject.SetActive(true);
    }

    private void InitScrollGroup()
    {

        // 添加上下滚动联动
        toggleGroupItem.InitOnAwake();
        _scrollGroup.ReListener();
        
        bool isValentinePackLive = ValentinePackage.GetPackLive();
        ValentineTabItem.gameObject.SetActive(isValentinePackLive);

        bool newYearPackLive = NewYearPackage.GetPackLive();
       // NewYearPackage.gameObject.SetActive(newYearPackLive);
        toggleGroupItem.transform.Find("NewYearPack").gameObject.SetActive(newYearPackLive);
    }
    

    public void SkipToPage(int skipId)
    {
        if ((skipId < 0) || (skipId >= 4))
            return;
        _scrollGroup.SetGroup(skipId);
    }
}

#region 每日每周每月礼包

public class LimitPackageData
{
    public List<BaseLimitPackageData> maNianPackage;
    public List<BaseLimitPackageData> lovePackage;
    public List<BaseLimitPackageData> valuePackPackage;
    public List<BaseLimitPackageData> youYouCoinPackage;
    public List<BaseLimitPackageData> dailyDiscountPackage;
    public List<BaseLimitPackageData> dailyHotSalePackage;
    public List<BaseLimitPackageData> weeklyPackage;
    public List<BaseLimitPackageData> monthlyPackage;
    public List<BaseLimitPackageData> magicCoinPackage;
    public List<BaseLimitPackageData> christmasCoinPackage;
    public List<BaseLimitPackageData> luckyCoinPackage;
    public List<BaseLimitPackageData> wingPackage;
    public List<BaseLimitPackageData> celebrationPackage;
}

public enum LimitPackType
{
    YouYouCoinPackage = 8
}

public class BaseLimitPackageData
{
    public string productId;
    public string name;
    public int packageType;
    public int frequencyType;
    public int price;
    public int limitPackageCurrencyType;
    public string refreshTime;
    public int isPurchase;
    public string discount;
    public List<LimitPackageRewardData> rewardList;
    public int orderType;
    public int leftPurchaseTimes;
    public int totalPurchaseTimes;
}

public class LimitPackageRewardData
{
    public int rewardType;
    public int amount;
    public int isRandomAmount;
    public int homepageSkinType;
    public int chatBubblesType;
    public int avatarFrameType;
    public List<string> pgcIdList;
}

public enum FrequencyType
{
    DailyLimit = 1,
    WeeklyLimit = 2,
    MonthlyLimit = 3,
    SeasonLimit = 4,
    DailyUnlockLimit = 5,// 每日解锁，s6 优优币礼包，每日解锁一次购买次数，最多购买7次
    OnceOnlyLimit = 6//限购一次
}

public class BuyProductResult{
    public List<LimitPackageRewardData> rewardList;
}
#endregion