using Game.Event;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class SunnyDollGiftItem : MonoBehaviour
{
    [SerializeField] private Text rewardNum;
    [SerializeField] private Text eventName;
    [SerializeField] private LoadingButton claimButton;
    [SerializeField] protected CButton goButton;
    [SerializeField] private GameObject claimedObj;
    [SerializeField] private Text progressText;

    [HideInInspector] public ActivityEventInfo _info;

    private bool isSending;

    public int EventId
    {
        get
        {
            return _info?.eventId ?? 0;
        }
    }

    private SunnyDollGift _root;

    private void Awake()
    {
        claimButton.onClick.AddListener(ClaimButtonClick);
        goButton.onClick.AddListener(GoButtonClick);
    }

    public void Init(ActivityEventInfo data, SunnyDollGift root)
    {
        _info = data;
        _root = root;

        RefreshData(data);
        ClaimCallBack();

        if (_info.eventSkipType == (int)EventCenterSkipType.Login || _info.eventSkipType == (int)EventCenterSkipType.OnlineTime)
        {
            goButton.GetComponentInChildren<Text>().text = "进行中";
        }
    }


    public void RefreshData(ActivityEventInfo info)
    {
        _info = info;
        rewardNum.SetText(info.rewardNum.ToString());
        eventName.SetText(info.eventName);
        if (info.targetAmount >= 1)
        {
            progressText.gameObject.SetActive(true);
            progressText.text = $"（{info.finishAmount}/{info.targetAmount}）";
        }
        else
        {
            progressText.gameObject.SetActive(false);
        }

        goButton.gameObject.SetActive(info.eventStatus == (int)ClaimStatus.Lock);
        claimButton.gameObject.SetActive(info.eventStatus == (int)ClaimStatus.Unlocked);
        claimedObj.gameObject.SetActive(info.eventStatus == (int)ClaimStatus.Claimed);
    }

    protected virtual void GoButtonClick()
    {
        EventCenterDataManager.Inst.SkipToTask((EventCenterSkipType)_info.eventSkipType);
    }

    private void ClaimButtonClick()
    {
        if (_info == null)
        {
            return;
        }

        // 发送领取奖励请求
        if (isSending) return;

        isSending = true;
        ClaimStart();
        JObject jObject = new JObject()
        {
            ["activityId"] = _root.activityInfo.activityId,
            ["eventId"] = _info.eventId,
            ["isAll"] = 0,
            ["expireClaim"] = 0
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ClaimActivityReward,
            HttpMethod.POST,
            JsonConvert.SerializeObject(jObject),
            (content) =>
            {
                ClaimCallBack();
                ActivityEventClaimResponse activityEventClaimResponse = JsonConvert.DeserializeObject<ActivityEventClaimResponse>(content);
                isSending = false;
                OnClaimSuccess(activityEventClaimResponse);
            },
            (error) =>
            {
                ClaimCallBack();
                LoggerUtils.LogError($"发送奖励失败！！，失败原因{error}");
                isSending = false;
            });
    }

    private void OnClaimSuccess(ActivityEventClaimResponse response)
    {
        if (this == null || gameObject == null)
        {
            return;
        }

        var eventInfo = _root.activityInfo.eventList.Find(x => x.eventId == response.eventInfo.eventId);
        if (eventInfo == null)
        {
            return;
        }

        eventInfo.eventStatus = response.eventInfo.eventStatus;
        //activityInfo.currencyAmount = response.currencyAmount;
        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);

        //AccountDataManager.Inst.BalanceInfo.Refresh();
        //CommonRewardItemData commonRewardItemData = new CommonRewardItemData();
        //commonRewardItemData.IconSp = PgcUtils.LoadRewardIcon((BUDRewardType)eventInfo.rewardType, panel.gameObject);
        //commonRewardItemData.rewardName = PgcUtils.GetRewardName((BUDRewardType)eventInfo.rewardType);
        //commonRewardItemData.RewardAmount = eventInfo.rewardNum;
        TaskRewardData reward = new TaskRewardData();
        reward.rewardType = eventInfo.rewardType;
        reward.num = eventInfo.rewardNum;
        panel.ShowRewards(reward, "恭喜你！醒狮汤圆已降价");

        _root.RefreshRedDot();
        _root.RefrashData(_root.activityInfo);
        MessageHelper.Broadcast(MessageName.OnRefreshTaskDataAfterBack);
    }

    public void ClaimStart()
    {
        claimButton.ShowLoading();
    }

    public void ClaimCallBack()
    {
        claimButton.HideLoading();
    }
}