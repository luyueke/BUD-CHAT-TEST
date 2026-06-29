using System.Collections.Generic;
using System.Globalization;
using EventTracking;
using Message;
using Newtonsoft.Json;
using UI.Base;
using UI.BaseWidgets;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.UI;

public class GetMorePinkCoinPanel : BasePanel<GetMorePinkCoinPanel>
{
    [SerializeField] private CButton closeBtn;
    [SerializeField] private CButton rechargeBtn;
    [SerializeField] private CButton monthBtn;
    [SerializeField] private Text tipText;
    [SerializeField] private Text monthText;
    [SerializeField] private List<LimitPackageItem> limitPackageItems;

    private LimitPackageData limitPackageData;
    private float needNum;

    private const string TipFormat = "当前社区币<color=#FFF0C6>{0}</color> 所需<color=#FFF0C6>{1}</color> 差{2}币";

    public override void OnCreate()
    {
        base.OnCreate();

        MessageHelper.AddListener(MessageName.OnPurchaseLimitedPackageSuccess, Refresh);

        closeBtn.onClick.AddListener(CloseSelf);
        rechargeBtn.onClick.AddListener(RechargeClick);
        monthBtn.onClick.AddListener(MonthClick);

        IAPDataManager.Inst.GetProductInfo(res =>
        {
            this.limitPackageData = res.limitPackageData;
            if (limitPackageData == null)
            {
                LoggerUtils.LogError("LimitedRechargeGiftPackPanel limitPackageData is NUll");
                return;
            }

            Init(this.limitPackageData);
        });

        InitByLocalData();
        if (FittingRoomPanel.curTab == MainTabs.Tab.Ugc)
        {
            //上报点击事件
            LoadEvent.ReportPopupStatus("HotSaleClicked", "ClickedHotSale");
        }
        tipText.gameObject.SetActive(false);
        rechargeBtn.gameObject.SetActive(false);
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        if(args != null && args.Length >0)
        {
            needNum = args[0] is float ? Mathf.Ceil((float)args[0]) : (int)args[0];
            //var gemNum = AccountDataManager.Inst.BalanceInfo.GetAccountCount(CurrencyType.Gem);
            var pinkNum = AccountDataManager.Inst.BalanceInfo.GetAccountCount(CurrencyType.PinkCoin);
            tipText.gameObject.SetActive(true);
            rechargeBtn.gameObject.SetActive(true);
            tipText.text = string.Format(TipFormat, pinkNum, needNum + pinkNum, needNum);

            bool gotoRecharge1 = false;
            bool gotoRecharge2 = false;
            var packageData = IAPDataManager.Inst.GetLimitedRechargeData();
            if (packageData == null)
            {
                return;
            }
            var data = packageData.dailyDiscountPackage.Find(p => p.name == "一元粉币礼包");
            if (data != null)
            {
                gotoRecharge1 = data.isPurchase == 1;
            }

            var data1 = packageData.dailyHotSalePackage.Find(p => p.name == "社区商品币礼包");
            if (data1 != null)
            {
                gotoRecharge2 = data.isPurchase == 1;
            }
      
            if (gotoRecharge1 && gotoRecharge2) //如果已经买完超级热卖，直接跳转到去充值
            {
                CloseSelf();
                UIManager.Inst.OpenPanel<GetMoreGemsPanel>(PanelId.GetMoreGemsPanel, (int)needNum);
            }
        }
        
    }

    protected override void OnDestroy()
    {
        MessageHelper.RemoveListener(MessageName.OnPurchaseLimitedPackageSuccess, Refresh);
    }

    private void Refresh()
    {
        IAPDataManager.Inst.GetProductInfo((_)=>
        {
            Init(IAPDataManager.Inst?.GetLimitedRechargeData());
        });
    }

    private void InitByLocalData()
    {
        var packageData = IAPDataManager.Inst.GetLimitedRechargeData();
        if (packageData != null)
        {
            Init(packageData);
        }
    }

    private void Init(LimitPackageData packageData)
    {
        var data = packageData.dailyDiscountPackage.Find(p => p.name == "一元粉币礼包");
        if (data != null)
        {
            limitPackageItems[0].InitData(data);
            limitPackageItems[0].SetTimeOutContent(data);
        }

        var data1 = packageData.dailyHotSalePackage.Find(p => p.name == "社区商品币礼包");
        if (data1 != null)
        {
            limitPackageItems[1].InitData(data1);
            limitPackageItems[1].SetTimeOutContent(data1);
        }
        var subscribeStatusRsp = AnniversaryMonthCardMgr.Inst.subscribeStatusRsp;
        if (subscribeStatusRsp == null)
        {
            AnniversaryMonthCardMgr.Inst.GetMonthCardInfo(RefeshMonthUI);
        }else
        {
            RefeshMonthUI();
        }

    }

    private void RefeshMonthUI()
    {
        if (AnniversaryMonthCardMgr.Inst.CheckHasReward(AnniversaryMonthCardMgr.MonthCardType.Silver))
        {
            monthText.text = "续费";
        }
        else
        {
            monthText.text = "￥30";
        }
    }
    private void MonthClick()
    {
       
        var subscribeStatusRsp = AnniversaryMonthCardMgr.Inst.subscribeStatusRsp;
        if (subscribeStatusRsp == null)
        {
            AnniversaryMonthCardMgr.Inst.GetMonthCardInfo(ToBuyMonth);
        }
        else
        {
            ToBuyMonth();
        }

    }

    private void ToBuyMonth()
    {
        //购买 or 续订
        AnniversaryMonthCardMgr.Inst.ContinueVip(SubscribeVipType.MonthCard, AnniversaryMonthCardMgr.MonthCardType.Silver,()=> {
            RefeshMonthUI();
        });
    }

    private void RechargeClick()
    {
        UIManager.Inst.OpenPanel<GetMoreGemsPanel>(PanelId.GetMoreGemsPanel,(int) needNum);

    }
}