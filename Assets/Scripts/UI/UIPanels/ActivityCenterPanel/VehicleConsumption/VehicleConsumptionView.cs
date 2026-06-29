using System;
using System.Collections.Generic;
using System.Linq;
using Basic.Utils;
using Game.Store;
using GameData.Base;
using GameData.BaseInfo;
using GameData.Manager;
using GameData.PgcData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.BaseWidgets;
using UI.Manager;
using UI.UIPanels.RechargePanel;
using UnityEngine;
using UnityEngine.UI;


public class VehicleConsumptionView : ActivityBaseView {
    [SerializeField] private VehicleConsumptionItem rewardItemPrefab;

    [SerializeField] private Transform rewardItemParent;

    [SerializeField] private Text buyCountText;
    [SerializeField] private Text endTimeText;



    [SerializeField] private GameObject buyContainer;
    [SerializeField] private GameObject ownContainer;

    [SerializeField] private CButton previewBtn;
    [SerializeField] private LoadingButton rechargeBtn;

    [SerializeField] private CButton infoBtn;


    private List<VehicleConsumptionItem> rewardItems;

    private ActivityInfo activityInfo;

    private Dictionary<int, ActivityRewardInfo> rewardInfos;

    private bool isSending;

    private ActivityInfo oActivityInfo;
    public override void Init(ActivityInfo info) {
        base.Init(info);
        oActivityInfo = info;
        activityInfo = info;
        InitData();
        InitUI();
    }

    private void InitData() {
        rewardInfos = new Dictionary<int, ActivityRewardInfo>();
        foreach (var rewardInfo in activityInfo.rewardList) {
            rewardInfos.Add(rewardInfo.rewardId, rewardInfo);
        }

        GetDataByHttp();
    }
    private void GetDataByHttp()
    {
        ActivityCenterInfoReq req = new ActivityCenterInfoReq();
        req.idList = new List<string>
        {
            ActivityId.UGCVehicleConsume.ToString()
        };
     //   Debug.LogError("Vehicle GetDataByHttp 111");
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ActivityList, HttpMethod.POST,
            JsonConvert.SerializeObject(req), (content) =>
            {
           //     Debug.LogError("Vehicle GetDataByHttp 222 content="+ content);
                ActivityResponse activityResponse = JsonConvert.DeserializeObject<ActivityResponse>(content);
                if (activityResponse.list != null)
                {
                    OnGetActivityListSuccess(activityResponse.list);
                }
            },
            (error) =>
            {
                Debug.LogError("Vehicle GetDataByHttp 333 content=" );
            });
    }

    private void OnGetActivityListSuccess(List<ActivityInfo> activityList)
    {
        var activityInfo = activityList.Find(x => x.activityId == ActivityId.UGCVehicleConsume.ToString());
        if (activityInfo == null)
        {
            return;
        }
        //this.activityInfo = activityInfo;
        //this.activityInfo.rewardList = this.oActivityInfo.rewardList;
        //this.activityInfo.preRewardList = this.oActivityInfo.preRewardList;
        RefrashData(activityInfo);

    }


    private void InitUI() {
        rewardItems = new List<VehicleConsumptionItem>();
        foreach (var eventInfo in activityInfo.eventList) {
            var extra = JsonConvert.DeserializeObject<JObject>(eventInfo.extra);
            int rewardId = (extra["rewardId"] ?? 1).Value<int>();
            var rewardInfo = rewardInfos[rewardId];
            var item = Instantiate(rewardItemPrefab, rewardItemParent);
            item.gameObject.SetActive(true);
            item.Init(eventInfo, rewardInfo, OnClaimReward);
            rewardItems.Add(item);
        }

        rewardItemPrefab.gameObject.SetActive(false);
//        endTimeText.SetLocalText("距售卖结束还有: {0}", activityInfo.leftTime);
//        string defaultFormat = "yyyy年MM月dd日";
//#if PACKAGE_TYPE_US
//        defaultFormat = "yyyy-MM-dd";
//#endif
//        expireTimeText.SetLocalText(" ·  现在购买折扣卡，有效期至{0}。", DateTime.Now.AddDays(30).ToString(defaultFormat));
        //ownedExpireTimeText.SetLocalText("截止至{0}",DateTime.Now.AddDays(30).ToString(defaultFormat));
        //ownedExpireTimeText.SetPreferredSize();
        previewBtn.onClick.AddListener(OnPreviewClick);
        rechargeBtn.onClick.AddListener(OnRechargeClick);
        infoBtn.onClick.AddListener(OnInfoClick);
    }

    private void OnInfoClick() {
        //infoView.SetActive(true);
        UIManager.Inst.OpenPanel<GashaponRulePanel>(PanelId.GashaponRulePanel, "Assets/Loadable/UI/ActivityCenterPanel/UGCVehicleConsume/Rule.json");
    }

    private void OnClaimReward(ActivityEventInfo eventInfo) {
     
        //if ((ClaimStatus)eventInfo.eventStatus == ClaimStatus.Lock || (ClaimStatus)eventInfo.eventStatus == ClaimStatus.ErrStatus) {
        //    OnPreviewClick();
        //    return;
        //}

        if ((ClaimStatus)eventInfo.eventStatus != ClaimStatus.Unlocked) {
            var rewardInfo = activityInfo.rewardList.Find(x => x.rewardId == eventInfo.eventId);
            ShowRewardPanel(rewardInfo);
            return;
        }

        if (isSending) {
            return;
        }

        isSending = true;


        JObject jObject = new JObject() {
            ["activityId"] = activityInfo.activityId,
            ["eventId"] = eventInfo.eventId
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ClaimActivityReward,
            HttpMethod.POST,
            JsonConvert.SerializeObject(jObject),
            (content) => {
                ActivityEventClaimResponse activityEventClaimResponse =
                    JsonConvert.DeserializeObject<ActivityEventClaimResponse>(content);
                isSending = false;
                OnClaimSuccess(activityEventClaimResponse);
            },
            (error) => {
                isSending = false;
            });
    }

    public override void RefrashData(ActivityInfo info) {
        base.RefrashData(info);
        bool isOwned = info.productinfo?.isPurchased == 1;
        buyCountText.text = info.currencyAmount.ToString();
        buyContainer.SetActive(!isOwned);
        ownContainer.SetActive(isOwned);
        RefreshItems(info);
        //endTimeText.gameObject.SetActive(!isOwned);
    }

    private void ShowRewardPanel(ActivityRewardInfo eventInfo)
    {
        switch ((BUDRewardType)eventInfo.budRewardType)
        {
            //case BUDRewardType.RewardAvatarFrame:
            //    HeadCycleData headData = UserUIWidgetManager.Inst.GetHeadCycleDataById(packageData.avatarFrameType);
            //    if (headData != null)
            //    {
            //        PreviewManager.Inst.ShowAvatarFramePreview(headData.Id);
            //    }
            //    break;
            //case BUDRewardType.RewardHomepageSkin:
            //    UIManager.Inst.OpenPanel<ProfileThemePreviewPanel>(PanelId.ProfileThemePreviewPanel, packageData.homepageSkinType);
            //    break;
            //case BUDRewardType.RewardChatBubbles:
            //    GameChatBubbleData chatData = UserUIWidgetManager.Inst.GetChatDataByID(packageData.chatBubblesType);
            //    if (chatData != null)
            //    {
            //        var tem = new RewardPreviewInfo(BUDRewardType.RewardChatBubbles, CurrencyType.None, chatData.PgcId, chatData.Name, "");
            //        tem.SetTitleAndDes(chatData.Name, chatData.Desc);
            //        PreviewManager.Inst.ShowPreview(tem);
            //    }
            //    break;
            case BUDRewardType.RewardBadge:
                PreviewManager.Inst.ShowPreview(new RewardPreviewInfo(BUDRewardType.RewardBadge, CurrencyType.Badge, "", "", ""));
                break;
            case BUDRewardType.RewardLuckyCoin:
                PreviewManager.Inst.ShowPreview(new RewardPreviewInfo(BUDRewardType.RewardLuckyCoin, CurrencyType.LuckyCoin, "", "", ""));
                break;
            case BUDRewardType.RewardYouYouCoin:
                PreviewManager.Inst.ShowPreview(new RewardPreviewInfo(BUDRewardType.RewardYouYouCoin, CurrencyType.YouYouCoin, "", "", ""));
                break;
            case BUDRewardType.RewardPurpleDreamCoin:
                PreviewManager.Inst.ShowPreview(new RewardPreviewInfo(BUDRewardType.RewardPurpleDreamCoin, CurrencyType.PurpleDreamCoin, "", "", ""));
                break;
            case BUDRewardType.RewardCommunityAnimationTicket:
                PreviewManager.Inst.ShowPreview(new RewardPreviewInfo(BUDRewardType.RewardCommunityAnimationTicket, CurrencyType.CommunityAnimationTicket, "", "", ""));
                break;
            case BUDRewardType.RewardCommunityInstrumentTicket:
                PreviewManager.Inst.ShowPreview(new RewardPreviewInfo(BUDRewardType.RewardCommunityAnimationTicket, CurrencyType.CommunityInstrumentTicket, "", "", ""));
                break;
            case BUDRewardType.RewardUGCVehicleTicket:
                PreviewManager.Inst.ShowPreview(new RewardPreviewInfo(BUDRewardType.RewardUGCVehicleTicket, CurrencyType.CommunityVehicleTicket, "", "", ""));
                break;
            case BUDRewardType.RewardCommunitySkinTicket:
                PreviewManager.Inst.ShowPreview(new RewardPreviewInfo(BUDRewardType.RewardCommunitySkinTicket, CurrencyType.CommunitySkinTicket, "", "", ""));
                break;
            default:
                OnPreviewClick();
                break;
        }
    }

    public void OnPreviewClick() {

        var panel = UIManager.Inst.OpenPanel<ActivityRewardPanel>(PanelId.ActivityRewardPanel,
           Enum.Parse<ActivityId>(activityInfo.activityId));
        panel.SetPreviewData(activityInfo);

    }


    public void OnRechargeClick() {

        int num = AccountDataManager.Inst.BalanceInfo.GetAccountCount(CurrencyType.Gem);
        int price = 398;

        if (num < price)
        {
            UIManager.Inst.OpenPanel(PanelId.GetMoreGemsPanel, price - num);

        }
        else
        {

            rechargeBtn.SetLoadingVisible(true);
            isSending = true;
            JObject req = new JObject()
            {
                ["productType"] = 17,
                ["productId"] = ActivityId.UGCVehicleConsume.ToString()
            };
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.BuyProductPay, HttpMethod.POST,
                JsonConvert.SerializeObject(req), (rspValue) =>
                {
                    rechargeBtn.SetLoadingVisible(false);
                    isSending = false;
                    var rsp = JObject.Parse(rspValue);
                //if (rsp["discountCard"] != null) {
                //    var discountCardRsp = rsp["discountCard"].ToObject<DiscountCardStatusResponse>();
                //    if (discountCardRsp != null) {
                //        DiscountCardManager.Inst.SetStatus(discountCardRsp.hasDiscountCard, discountCardRsp.expireTime);
                //        OnRechargeSuccess();
                //    }
                //}

                OnRechargeSuccess();
                    GetDataByHttp();
                    TokenDataManager.Inst.GetTokenData();
                    AccountDataManager.Inst.BalanceInfo.Refresh();
                }, (_) =>
                {
                    rechargeBtn.SetLoadingVisible(false);
                    isSending = false;
                });
        }
    }

    private void OnRechargeSuccess() {

        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
    
        var rewardList = new List<CommonRewardItemData>();
        CommonRewardItemData pinkCoinData = new CommonRewardItemData() {
            IconSp = PgcUtils.LoadRewardIcon(BUDRewardType.RewardUGCVehicleTicket, gameObject),
            RewardAmount = 5,
            rewardName = PgcUtils.GetRewardName(BUDRewardType.RewardUGCVehicleTicket)
        };
        rewardList.Add(pinkCoinData);

        panel.ShowRewards(rewardList, true, true);
    }

    private void OnClaimSuccess(ActivityEventClaimResponse response) {
        if (this == null || gameObject == null) {
            return;
        }
        AccountDataManager.Inst.BalanceInfo.Refresh();
        var eventInfo = activityInfo.eventList.Find(x => x.eventId == response.eventInfo.eventId);
        var extra = JsonConvert.DeserializeObject<JObject>(eventInfo.extra);
        int rewardId = (extra["rewardId"] ?? 1).Value<int>();
        var rewardInfo = rewardInfos[rewardId];
        eventInfo.eventStatus = response.eventInfo.eventStatus;
        RefreshItems(activityInfo);
        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
        if (rewardInfo.budRewardType == (int)BUDRewardType.RewardPgcResource) {
            panel.ShowPgcRewards(new List<string>() { rewardInfo.pgcId }, rewardInfo.rewardName);
        } else if(rewardInfo.budRewardType == (int) BUDRewardType.RewardPgcBundle)
        {
            panel.ShowPgcRewards(rewardInfo.pgcId.Split(",").ToList(), rewardInfo.rewardName);
        }
        else {
            TokenDataManager.Inst.GetTokenData();
            AccountDataManager.Inst.BalanceInfo.Refresh();
            var rewardList = new List<CommonRewardItemData>();
            CommonRewardItemData commonRewardItemData = new CommonRewardItemData();
            commonRewardItemData.IconSp = PgcUtils.LoadRewardIcon((BUDRewardType)rewardInfo.budRewardType, gameObject);
            commonRewardItemData.rewardName = PgcUtils.GetRewardName((BUDRewardType)rewardInfo.budRewardType);
            commonRewardItemData.RewardAmount = response.claimAmount;
            rewardList.Add(commonRewardItemData);
            panel.ShowRewards(rewardList);
        }
    }

    private void RefreshItems(ActivityInfo activityInfo) {
        if (rewardItems.Count != activityInfo.eventList.Count) {
            LoggerUtils.LogError("[SunnyDoll] Refresh Event ItemView fail");
            return;
        }

        for (int i = 0; i < activityInfo.eventList.Count; i++) {
            rewardItems[i].Refresh(activityInfo.eventList[i]);
        }
    }
}
