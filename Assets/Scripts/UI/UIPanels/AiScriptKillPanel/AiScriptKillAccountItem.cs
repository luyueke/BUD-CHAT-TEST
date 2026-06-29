using Game.Event;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class AiScriptKillAccountItem : MonoBehaviour
{
    //UI组件
    public Image Icon;
    public Image Icon5;
    public Text targetNum;
    public Text num;
    public GameObject overObj;
    public GameObject lockObj;
    public GameObject claimObj;
    public Button claimBtn;

    public RawImage bgRawTex;
    ActivityEventInfo _localData;
    ActivityEventInfo _serverData;
    Action<ActivityEventClaimResponse> _claimAction;
    string rewardSpritAlxs = "";
    string _activityId;
    List<int> width = new List<int>{ 100, 60, 100, 60,100 };
    private void Start()
    {
        claimBtn.onClick.AddListener(
            () =>
            {
                if (_serverData.eventStatus != (int)EventStatus.Claim) {
                    ShowPreview(_localData.rewardType);
                }
                else
                {
                    Claim();
                }

            });
    }


    public void SetData(ActivityEventInfo localData, ActivityEventInfo serverData,string activityId, Action<ActivityEventClaimResponse> claimAction)
    {
        _localData = localData;
        _serverData = serverData;
        _activityId = activityId;
        _claimAction = claimAction;
        num.text ="x" +  localData.rewardNum.ToString();
        if (localData.rewardType != 1001)
        {
            Icon.sprite = PgcUtils.LoadRewardIcon((BUDRewardType)localData.rewardType, Icon.gameObject);
        }
        else
        {
            Icon.gameObject.SetActive(false);
            Icon5.gameObject.SetActive(true);
        }
        RectTransform rectTransform = GetComponent<RectTransform>();
        // 只修改宽度，保持原有高度
        float currentHeight = rectTransform.sizeDelta.y;
        rectTransform.sizeDelta = new Vector2(width[localData.eventId - 8], currentHeight);
        targetNum.text = localData.targetAmount.ToString();
        SetStatus(serverData.eventStatus);
    }


    void SetStatus(int statu)
    {
        switch (statu)
        {
            case (int)EventStatus.UnClaim:
                claimObj.SetActive(false);
                lockObj.SetActive(true);
                overObj.SetActive(false);
                break;
            case (int)EventStatus.Claim:
                claimObj.SetActive(true);
                lockObj.SetActive(false);
                overObj.SetActive(false);
                break;
            case (int)EventStatus.Finish:
                claimObj.SetActive(false);
                lockObj.SetActive(false);
                overObj.SetActive(true);
                break;
        }
    }

    void Claim()
    {
        JObject jObject = new JObject()
        {
            ["activityId"] = _activityId,
            ["eventId"] = _serverData.eventId
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ClaimActivityReward,
            HttpMethod.POST,
            JsonConvert.SerializeObject(jObject),
            (content) =>
            {
                ActivityEventClaimResponse avtivityEventClaimResponse = JsonConvert.DeserializeObject<ActivityEventClaimResponse>(content);
                OnClaimSuccess(avtivityEventClaimResponse);
            },
            (error) =>
            {
            });
    }

    private void OnClaimSuccess(ActivityEventClaimResponse avtivityEventClaimResponse)
    {
        this._claimAction?.Invoke(avtivityEventClaimResponse);
        SetStatus(avtivityEventClaimResponse.eventInfo.eventStatus);
        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
        List<CommonRewardItemData> items = new List<CommonRewardItemData>();
        MessageHelper.Broadcast(MessageName.UpdateHallTask);
        
        CommonRewardItemData item;
        MessageHelper.Broadcast(MessageName.OnPlayerInfoAccountChange, (CurrencyType)_localData.rewardType);  
        if(_localData.rewardType != 1001)
        {
            item = new CommonRewardItemData()
            {
                RewardAmount = _localData.rewardNum,
                rewardType = _localData.rewardType,
                rewardName = PgcUtils.GetRewardName((BUDRewardType)_localData.rewardType)
            };
        }
        else{
            item = new CommonRewardItemData()
            {
                RewardAmount = _localData.rewardNum,
                IconSp = Icon5.sprite,
                rewardName = "泰迪套装"
            };
        }
        
        items.Add(item);
        panel.ShowRewards(items);
        AccountDataManager.Inst.BalanceInfo.Refresh();
        VipDataManager.Inst.UpdateVipStatus();
        ReddotManagerUtils.Inst.RefreshRedDot();
    }
    void ShowPreview(int type)
    {   
        if(type!= 1001)
        {
            //这么处理是因为有些货币类型和物品类型对应的枚举值不同
            if (type == 22) type = 9;
            if (type == 7) type = 5;

            UIManager.Inst.OpenPanel<CurrencyTipsPanel>(PanelId.CurrencyTipsPanel, (CurrencyType)type);
        }
        else
        {
            var bundleShowpanel = UIManager.Inst.OpenPanel<RewardPreviewPanel>(PanelId.RewardPreviewPanel);
            bundleShowpanel.SetEventPreview(new List<string>() {
                    "10400490",
                    "10900490"}, "泰迪套装",Icon5.sprite, "#BF8DFF",bgRawTex.texture);
        }
        
    }
}
