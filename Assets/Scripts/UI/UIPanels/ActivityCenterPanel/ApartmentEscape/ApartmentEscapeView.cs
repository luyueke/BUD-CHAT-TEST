using System.Collections.Generic;
using System.Linq;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class ApartmentEscapeView : ActivityBaseView
{
    [SerializeField] private ApartmentEscapeDailyView handDailyView;

    [SerializeField] private ApartmentEscapeRewardItem rewardItemPrefab;


    [SerializeField] private Text currencyAmountText;

    [SerializeField] private CButton previewBtn;

    [SerializeField] private RawImage bgImage;

    [SerializeField] private Button giveBtn;
    [SerializeField] private Text likeProgressTxt;
    [SerializeField] private Image likeProgressImg;
    [SerializeField] private Text roseCount;
    [SerializeField] private GameObject likeObj;
    [SerializeField] private Button tipBtn;

    private readonly List<int> rewardEventIds = new List<int>() { 7, 8, 9, 10, 11 };
    private Dictionary<int, ApartmentEscapeRewardItem> rewardItems = new Dictionary<int, ApartmentEscapeRewardItem>();
    private ActivityInfo activityInfo;
    private bool isSending = false;
    private const float ProgressImgMaxWidth = 423f; // progressImg 的总长度
    private bool isConverting = false;
    

    public override void Init(ActivityInfo info)
    {
        activityInfo = info;
        base.Init(info);
        handDailyView.InitListUI(info.eventList, info.activityId, OnClaimSuccess);


        foreach (var rewardEventId in rewardEventIds)
        {
            var eventInfo = info.eventList.FirstOrDefault(tmp => rewardEventId == tmp.eventId);
            if (eventInfo == null)
            {
                continue;
            }
            var rewardData = new CommonRewardItemData()
            {
                pgcId = eventInfo.pgcId,
                RewardAmount = eventInfo.rewardNum,
                rewardName = eventInfo.eventName,
                rewardType = eventInfo.rewardType,
            };
            
            var tmpItem = Instantiate(rewardItemPrefab, rewardItemPrefab.transform.parent);
            tmpItem.gameObject.SetActive(true);
            tmpItem.Init(rewardEventId, rewardData, OnClaimCallBack);
            tmpItem.SetProcess(eventInfo.targetAmount);
            rewardItems.Add(rewardEventId, tmpItem);
        }

        rewardItemPrefab.gameObject.SetActive(false);
        previewBtn.onClick.AddListener(OnPreviewClick);

        
        giveBtn.onClick.AddListener(() =>
        {
            if (activityInfo.currencyAmount > 0)
            {
                ExchangeActivityCurrency(info.activityId, activityInfo.currencyAmount);
            }
            else
            {
                TipPanel.ShowToast("玫瑰花余额不足");
            }
        });
        
        tipBtn.onClick.AddListener(() =>
        {
            ApartmentTipPanel panel =
                UIManager.Inst.OpenPanel<ApartmentTipPanel>(PanelId.ApartmentTipPanel);
        });

        UpdateProgress(activityInfo.apartmentEscapeInfo);

    }
    
    private void ExchangeActivityCurrency(string activityId, int exchangeAmount)
    {
        if (isConverting)
        {
            return;
        }

        isConverting = true;
        JObject obj = new JObject()
        {
            ["activityId"] = activityId,
            ["exchangeAmount"] = exchangeAmount
        };

        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ExchangeActivityCurrency,
            HttpMethod.POST,
            JsonConvert.SerializeObject(obj),
            (message =>
            {
                isConverting = false;
                AccountDataManager.Inst.BalanceInfo.Refresh();

                var data = JsonConvert.DeserializeObject<ExchangeCoinPanel.ExchangeActivityRes>(message);
                int currencyAmount = data.currencyAmount;
                UpdateCurrency(currencyAmount);
                UpdateRedDot();
                // RefrashData(activityInfo);
                MessageHelper.Broadcast(MessageName.OnRefreshTaskDataAfterBack);
            }),
            (arg0 =>
            {
                isConverting = false;
            }));
    }

    private void UpdateProgress(ApartmentEscapeInfo info)
    {
        if (info != null)
        {
            int currentNum = info.currentAmount;
            int totalNum = info.totalAmount;
            likeProgressTxt.text = info.currentAmount + "/" + info.totalAmount;
            if (likeProgressImg != null)
            {
                float progressRatio = (float)currentNum / totalNum; // 计算比例
                float newWidth = progressRatio * ProgressImgMaxWidth; // 按比例计算新的宽度
                likeProgressImg.rectTransform.sizeDelta = new Vector2(newWidth, likeProgressImg.rectTransform.sizeDelta.y); // 更新宽度
            }

            likeObj.gameObject.SetActive(currentNum >= totalNum);
            giveBtn.gameObject.SetActive(currentNum < totalNum);
        }
    }

    private void OnPreviewClick()
    {
        if (activityInfo == null)
        {
            return;
        }

        //todo
        
        var panel = UIManager.Inst.OpenPanel<ActivityRewardPanel>(PanelId.ActivityRewardPanel, activityInfo.activityId);
        panel.SetEventPreview(activityInfo, "向优优妹送礼物提升好感度，可免费领取头像框！", bgImage.texture);
        
        // var panel = UIManager.Inst.OpenPanel<PaidPackRewardPanel>(PanelId.PaidPackRewardPanel, activityInfo.activityId);
        // panel.SetEventPreview(activityInfo, "向优优妹送礼物提升好感度，可免费领取头像框！", bgImage.texture);
    }

    private void OnClaimCallBack(CommonRewardItem rewardItem)
    {
        var eventInfo = activityInfo.eventList.Find(tmp => tmp.eventId == rewardItem.itemId);
        if (eventInfo == null)
        {
            return;
        }

        if (eventInfo.eventStatus != (int)ClaimStatus.Unlocked)
        {
            OnPreviewClick();
            return;
        }

        if (isSending)
        {
            return;
        }

        isSending = true;

        JObject jObject = new JObject()
        {
            ["activityId"] = activityInfo.activityId,
            ["eventId"] = eventInfo.eventId
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ClaimActivityReward,
            HttpMethod.POST,
            JsonConvert.SerializeObject(jObject),
            (content) =>
            {
                ActivityEventClaimResponse activityEventClaimResponse =
                    JsonConvert.DeserializeObject<ActivityEventClaimResponse>(content);
                isSending = false;
                OnClaimSuccess(activityEventClaimResponse, rewardItem);
            },
            (error) => { isSending = false; });
    }

    private void OnClaimSuccess(ActivityEventClaimResponse response)
    {
        if (this == null || gameObject == null)
        {
            return;
        }

        var eventInfo = activityInfo.eventList.Find(x => x.eventId == response.eventInfo.eventId);
        if (eventInfo == null)
        {
            return;
        }

        eventInfo.eventStatus = response.eventInfo.eventStatus;
        activityInfo.currencyAmount = response.currencyAmount;
        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);

        if (eventInfo.rewardType == (int)BUDRewardType.RewardPgcResource)
        {
            panel.ShowPgcRewards(new List<string>() { eventInfo.pgcId }, eventInfo.eventName);
        }
        else
        {
            AccountDataManager.Inst.BalanceInfo.Refresh();
            var rewardList = new List<CommonRewardItemData>();
            CommonRewardItemData commonRewardItemData = new CommonRewardItemData();
            commonRewardItemData.IconSp =
                PgcUtils.LoadRewardIcon((BUDRewardType)eventInfo.rewardType, panel.gameObject);
            commonRewardItemData.rewardName = PgcUtils.GetRewardName((BUDRewardType)eventInfo.rewardType);
            commonRewardItemData.RewardAmount = eventInfo.rewardNum;
            rewardList.Add(commonRewardItemData);
            panel.ShowRewards(rewardList);
        }

        UpdateRedDot();
        RefrashData(activityInfo);
    }

    private void OnClaimSuccess(ActivityEventClaimResponse response, CommonRewardItem rewardItem)
    {
        if (this == null || gameObject == null)
        {
            return;
        }

        var eventInfo = activityInfo.eventList.Find(x => x.eventId == response.eventInfo.eventId);
        if (eventInfo == null)
        {
            return;
        }

        AccountDataManager.Inst.BalanceInfo.Refresh();
        activityInfo.currencyAmount = response.currencyAmount;
        eventInfo.eventStatus = response.eventInfo.eventStatus;
        rewardItem.SetStatus((ClaimStatus)eventInfo.eventStatus);
        AccountDataManager.Inst.BalanceInfo.Refresh();
        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);

        panel.ShowRewards(new List<CommonRewardItemData>() { rewardItem.rewardData });
        UpdateRedDot();
        RefrashData(activityInfo);
        MessageHelper.Broadcast(MessageName.OnRefreshTaskDataAfterBack);
    }


    public override void RefrashData(ActivityInfo info)
    {
        base.RefrashData(info);
        activityInfo = info;
        handDailyView.RefreshListUI(info.eventList);

        foreach (var eventInfo in info.eventList)
        {
            if (rewardItems.TryGetValue(eventInfo.eventId, out var rewardItem))
            {
                rewardItem.SetStatus((ClaimStatus)eventInfo.eventStatus);
            }
        }


        UpdateCurrency(info.currencyAmount);
        UpdateProgress(info.apartmentEscapeInfo);
    }

    private void UpdateCurrency(int amount)
    {
        roseCount.SetText(amount.ToString());
        // roseCount.SetPreferredSize();
    }
}