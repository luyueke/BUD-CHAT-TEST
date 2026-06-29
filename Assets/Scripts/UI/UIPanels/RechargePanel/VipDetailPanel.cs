using System.Collections.Generic;
using System.Globalization;
using Game.Event;
using Message;
using Newtonsoft.Json;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;


public class VipDetailPanel : BasePanel<VipDetailPanel>
{
    public CButton buyVipBtn;
    public CButton buySVipBtn;
    public CButton backBtn;
    public Text vipContinueTxt;
    public Text svipContinueTxt;
    public Text vipPriceText;//vip价格
    public Text vipPriceUnit;//单位
    public Text vipGemText;//vip获得gem数
    public Text vipTipsText;//vip获得gem提示
    public Text svipPriceText;
    public Text svipPriceUnit;
    public Text svipSrcPriceText;//折前价格
    public Text svipSrcPriceUnit;
    public Text svipGemText;
    public Text SvipTipsText;
	public Transform BG;
    public long svipEndTime;
    public Text svipEndTimeText;

    public SubscribeStatusResponse _subscribeStatusResponse;
    private SubscribeVipType currentSubscribeVipType;
    private string budOrderId = "";

    private string spriteatlasPath = RechargePanel.RechargePanelAtlas;
    private IapTrackData _iapTrackData = new IapTrackData();


    public override void OnCreate()
    {
        base.OnCreate();

        InitBg();

        buyVipBtn.onClick.AddListener(() => { ContinueVip(SubscribeVipType.MonthCard); });

        buySVipBtn.onClick.AddListener(() => { ContinueVip(SubscribeVipType.YearCard); });

        backBtn.onClick.AddListener(() => { CloseSelf(); });

        if (svipEndTime > 0)
        {
            string endTimeTips = DataUtil.ToLocalCountDownTime(svipEndTime);
            string tips = LocalizationManager.Inst.GetLocalizedText("年卡售卖结束时间：");
            svipEndTimeText.SetText(tips + endTimeTips);
        }

    }

    private void InitBg()
    {
        if (BG == null)
        {
            LoggerUtils.LogError("[BG] Check GenderSelectPanel Bg Object");
            return;
        }




        var spriteatlasPath = RechargePanel.RechargePanelAtlas;
        var itemObj = Loader
            .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
            .Instantiate(BG);
        var item = itemObj.GetComponent<ActivityCenterBgItem>();
        item.InitCustomBgItem("#00000000", spriteatlasPath,
            new List<string>() { "vip_reward_bg1"});
        item.gameObject.SetActive(true);

#if PACKAGE_TYPE_US
        AdapterPackageUI();
#endif


    }

    public override void OnShow(params object[] args) {
        base.OnShow(args);
        if (args.Length > 0) {
            bool isNeedRefresh = (bool)args[0];
            if (isNeedRefresh) {
                RefreshVipStatus();
            }
        }
    }


    protected override void OnDestroy()
    {
        base.OnDestroy();
    }

    public void SetData(SubscribeStatusResponse subscribeStatusResponse)
    {
        if (subscribeStatusResponse != null)
        {
            this._subscribeStatusResponse = subscribeStatusResponse;
            SetUI();
        }
        else
        {
            RefreshVipStatus();
        }
    }

    public void SetUI()
    {
        if (_subscribeStatusResponse == null)
        {
            return;
        }

        bool isVip = _subscribeStatusResponse.vipType == (int)SubscribeVipType.MonthCard ||
                     _subscribeStatusResponse.vipType == (int)SubscribeVipType.YearCard;
        if (isVip)
        {
            vipContinueTxt.SetLocalText("续订");
            svipContinueTxt.SetLocalText("续订");
        }
        else
        {
            vipContinueTxt.SetLocalText("订阅");
            svipContinueTxt.SetLocalText("订阅");
        }
    }


    private void RefreshVipStatus()
    {
        IAPDataManager.Inst.GetSubscribeStatus((b, subscribeStatusResponse) =>
        {
            if (subscribeStatusResponse == null)
            {
                return;
            }

            this._subscribeStatusResponse = subscribeStatusResponse;
            SetUI();
        });
    }

    private void TrackEvent(BillingResultResponse billingResultResponse)
    {
        Dictionary<string, object> trackData = new Dictionary<string, object>();
        float result;
        if (float.TryParse(_iapTrackData.price, NumberStyles.Float, CultureInfo.InvariantCulture,out result))
        {
            trackData.Add("priceNum", result);
        }

        trackData.Add("item", _iapTrackData.item);
        bool isUploadData = !AnalyticsManager.Inst.ContainOrderID(budOrderId);
        if (isUploadData)
        {
            trackData.Add("budOrderId", budOrderId ?? "");
            trackData.Add("method", "VipDetailPanel");
            AnalyticsManager.Inst.Track(AnalyticsEventName.TOP_UP_SUCCESS, trackData);
        }
    }



    private void ContinueVip(SubscribeVipType subscribeVipType)
    {
        if (_subscribeStatusResponse == null)
        {
            return;
        }

        if (IAPDataManager.Inst.IsOfficialChannel())
        {
            ProductInfo productInfo = IAPDataManager.GetVipProductInfo(subscribeVipType, _subscribeStatusResponse);
            ConfirmPaymentPanel panel = UIManager.Inst.OpenPanel<ConfirmPaymentPanel>(PanelId.ConfirmPaymentPanel, productInfo.price);
            panel.SetCallback(paymentType =>
            {
                BuyVip(subscribeVipType, paymentType);
            });
            return;
        }

        BuyVip(subscribeVipType, ConfirmPaymentPanel.PaymentType.Default);
    }

    private void BuyVip(SubscribeVipType subscribeVipType, ConfirmPaymentPanel.PaymentType paymentType)
    {
        this.currentSubscribeVipType = subscribeVipType;
        ProductInfo productInfo = IAPDataManager.GetVipProductInfo(subscribeVipType, _subscribeStatusResponse);
        if (productInfo == null)
        {
            return;
        }

        ShowPurchaseLoading();
        ChannelProductInfo channelProductInfo = productInfo.toU8Info();
        var productId = channelProductInfo.productId;
        if (string.IsNullOrEmpty(productId))
        {
            return;
        }

        _iapTrackData.price = channelProductInfo.price;
        _iapTrackData.item = subscribeVipType == SubscribeVipType.MonthCard ? "VIP" : "SVIP";
        _iapTrackData.channel = "";

        IAPDataManager.Inst.GetProductOrderId(productId,null, (b, info) =>
        {
            budOrderId = info?.budOrderId;
            if (!b || string.IsNullOrEmpty(budOrderId))
            {
                HidePurchaseLoading();
                return;
            }

            channelProductInfo.extension = JsonConvert.SerializeObject(info);
            channelProductInfo.paymentType = (int)paymentType;
            MobileInterface.Instance.SendMessage(MobileInterfaceDefine.startBillingFlow,
                JsonConvert.SerializeObject(channelProductInfo));
        });
    }

    private void StartBillingFlow(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        BillingResultResponse billingResultResponse = JsonConvert.DeserializeObject<BillingResultResponse>(message);
        if (billingResultResponse.resultType == (int)BillingResultType.UserPaySuccess)
        {
            StartLooping();
            TrackEvent(billingResultResponse);
        }
        else if (billingResultResponse.resultType == (int)BillingResultType.RechargeFail)
        {
            HidePurchaseLoading();
            PurchaseStatusManager.Inst.StopLoop();
        }
    }

    private void StartLooping()
    {
        if (string.IsNullOrEmpty(budOrderId))
        {
            return;
        }

        if (UIManager.Inst.TryFindPanel<PurchaseProcessingPanel>(PanelId.PurchaseProcessingPanel, out var panel))
        {
            panel.StartTimer(60, () =>
            {
                PurchaseStatusManager.Inst.StopLoop();
                MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.startBillingFlow);
            });
        }

        PurchaseStatusManager.Inst.StartLoop(budOrderId, (orderResult, productGemInfo) =>
        {
            if (this == null)
            {
                return;
            }

            if (!orderResult)
            {
                return;
            }

            AccountDataManager.Inst.BalanceInfo.Refresh();
            HidePurchaseLoading();
            ShowVipRewards();
            _subscribeStatusResponse.vipType = (int)currentSubscribeVipType;
            SetUI();
            VipDataManager.Inst.UpdateVipStatus();
            MessageHelper.Broadcast(MessageName.BuyVipSuccess);
        });
    }

    private void ShowPurchaseLoading()
    {
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.startBillingFlow, StartBillingFlow);
        PurchaseProcessingPanel processingPanel =
            UIManager.Inst.OpenPanel<PurchaseProcessingPanel>(PanelId.PurchaseProcessingPanel);
    }

    private void HidePurchaseLoading()
    {
        MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.startBillingFlow);
        if (UIManager.Inst.TryFindPanel<PurchaseProcessingPanel>(PanelId.PurchaseProcessingPanel, out var panel))
        {
            UIManager.Inst.ClosePanel(panel);
        }
    }

    private void ShowVipRewards()
    {
        var spriteatlasPath = RechargePanel.RechargePanelAtlas;
        SubscribeStatusReward subscribeStatusReward = _subscribeStatusResponse.rewards;
        if (currentSubscribeVipType == SubscribeVipType.MonthCard)
        {
            if (subscribeStatusReward != null)
            {
                Message.MessageHelper.Broadcast(Message.MessageName.AvaterDatabaseCheck);
                Sprite monthSp =
                    XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, "vip_reward_vip", gameObject);
                var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
                panel.ShowRewards(new List<CommonRewardItemData>()
                {
                    new CommonRewardItemData()
                    {
                        IconSp = monthSp,
                        RewardAmount = 1,
                        rewardName = "VIP月卡"
                    }
                });

                List<int> monthReward = subscribeStatusReward.month;
                if (monthReward != null && monthReward.Count > 0)
                {
                    Sprite reward1 =
                        XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, "vipland_gem", gameObject);
                    var rewardpanel1 = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
                    rewardpanel1.ShowRewards(new List<CommonRewardItemData>()
                    {
                        new CommonRewardItemData()
                        {
                            IconSp = reward1,
                            RewardAmount = 210,
                            rewardName = "BUD钻"
                        },
                    });
                }
            }
        }
        else if (currentSubscribeVipType == SubscribeVipType.YearCard)
        {
            Message.MessageHelper.Broadcast(Message.MessageName.AvaterDatabaseCheck);
            Sprite rewardSp =
                XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, "vip_reward_svip", gameObject);
            var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
            panel.ShowRewards(new List<CommonRewardItemData>()
            {
                new CommonRewardItemData()
                {
                    IconSp = rewardSp,
                    RewardAmount = 1,
                    rewardName = "SVIP年卡"
                }
            });

            List<int> yearReward = subscribeStatusReward.year;
            if (yearReward != null && yearReward.Count > 0)
            {
                Sprite reward1 = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, "vipland_gem", gameObject);
                var rewardpanel1 = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
                rewardpanel1.ShowRewards(new List<CommonRewardItemData>()
                {
                    new CommonRewardItemData()
                    {
                        IconSp = reward1,
                        RewardAmount = 1970,
                        rewardName = "BUD钻"
                    },
                });
            }
        }
    }

    private void AdapterPackageUI()
    {
        var vipPriceInfo = IAPDataManager.Inst.GetPriceInfo(ProductIdType.product_budvip);
        var svipPriceInfo = IAPDataManager.Inst.GetPriceInfo(ProductIdType.product_budsvip);
        vipPriceText.SetText(vipPriceInfo.priceLocal + "/");//vip价格
        vipGemText.SetText("x"+RechargeConfig.VipGem);//vip获得gem数
        vipTipsText.SetLocalText("立刻领取{0}钻",RechargeConfig.VipGem);//vip获得gem提示

        svipPriceText.SetText(svipPriceInfo.priceLocal+"/");
        svipSrcPriceText.SetText(RechargeConfig.SVipSrcPrice);//折前价格
        svipGemText.SetText("x"+RechargeConfig.SVipGem);
        SvipTipsText.SetLocalText("立刻领取{0}钻",RechargeConfig.SVipGem);
        vipPriceUnit.gameObject.SetActive(false);//单位
        vipContinueTxt.gameObject.SetActive(false);
        svipPriceUnit.gameObject.SetActive(false);
        svipSrcPriceUnit.gameObject.SetActive(false);
        svipContinueTxt.gameObject.SetActive(false);
    }
}
