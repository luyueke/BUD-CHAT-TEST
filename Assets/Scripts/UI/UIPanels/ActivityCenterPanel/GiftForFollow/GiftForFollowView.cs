using System.Collections;
using System.Collections.Generic;
using Game.Event;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class GiftForFollowView : ActivityBaseView
{
    [SerializeField] private List<GiftForFollowItemView> _itemViews;

    private bool isSending;
    private ActivityInfo _info;
    [SerializeField] private Text Txt_LeftTime;
    public override string Path => "Assets/Loadable/UI/ActivityCenterPanel/GiftForFollow/GiftForFollow.spriteatlas";
    public override void Init(ActivityInfo info)
    {
        _info = info;
        var bgParent = GameObjectEx.FindChildByName(transform, "BG");
        
        InitBg(bgParent, "#EE50AF",new List<string>()
        {
            "GiftForFollow_1",
            "GiftForFollow_2",
            "GiftForFollow_3",
            "GiftForFollow_4"
        });
        InitEventBaseInfo(info);
        if (!string.IsNullOrEmpty(info.leftTime))
        {
            Txt_LeftTime.text = "距活动结束还有：" + info.leftTime;
        }
    }

    public override void RefrashData(ActivityInfo info)
    {
        base.RefrashData(info);
        _info = info;
        RefreshItems();
    }
    
    //先用本地数据显示任务信息
    private void InitEventBaseInfo(ActivityInfo info)
    {
        if (_itemViews.Count != info.eventList.Count)
        {
            LoggerUtils.LogError("[SunnyDoll] init Event ItemView fail");
            return;
        }

        for (int i = 0; i < info.eventList.Count; i++)
        {
            var data = info.eventList[i];
            _itemViews[i].Init(data, ClaimReward);
            _itemViews[i].FollowAction = SkipToFollow;

            if (data.eventId == 4)
            {
                bool isShowTaptap = IAPDataManager.Inst.IsOfficialChannel();
#if UNITY_IPHONE
                isShowTaptap = true;
#endif
                _itemViews[i].gameObject.SetActive(isShowTaptap);
            }
        }
    }

    public void RefreshItems()
    {
        if (_itemViews.Count != _info.eventList.Count)
        {
            LoggerUtils.LogError("[SunnyDoll] Refresh Event ItemView fail");
            return;
        }
        
        for (int i = 0; i < _info.eventList.Count; i++)
        {
            _itemViews[i].Refresh(_info.eventList[i]);
        }
    }

    private void SkipToFollow(ActivityEventInfo info)
    {
        if (string.IsNullOrEmpty(info.extra))
        {
            return;
        }
        OnOpenFollow(info, false);
    }
    
    private void ClaimReward(ActivityEventInfo info, TaskClaimState state)
    {
        if (state == TaskClaimState.Unable && !string.IsNullOrEmpty(info.extra))
        {
            OnOpenFollow(info);
            return;
        }

        if (state == TaskClaimState.Enable)
        {
            onClaimDirect(info); 
        }
    }

    private void OnOpenFollow(ActivityEventInfo info, bool autoReport = true)
    {
        int eventId = info.eventId;
        if (eventId == 4)
        {
            string taptapLink = "https://www.taptap.cn/app/226341";
#if UNITY_IOS
                        taptapLink = "https://www.taptap.cn/app/226341?os=ios";
#elif UNITY_ANDROID
            taptapLink = "https://www.taptap.cn/app/226341";
#endif
            Application.OpenURL(taptapLink);
            
            if (autoReport)
            {
                onReportTaskFinsh(info);
            }
            
            return;
        }
        var link = info.extra;
        if (string.IsNullOrEmpty(link))
        {
            return;
        }
        
        Application.OpenURL(link);
        if (autoReport)
        {
            onReportTaskFinsh(info);
        }
    }

    private void onClaimDirect(ActivityEventInfo info)
    {
        if (isSending)
        {
            return;
        }
        isSending = true;

        JObject jObject = new JObject()
        {
            ["activityId"] = _info.activityId,
            ["eventId"] = info.eventId
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ClaimActivityReward,
            HttpMethod.POST, 
            JsonConvert.SerializeObject(jObject), 
            (content) =>
            {
                ActivityEventClaimResponse avtivityEventClaimResponse = JsonConvert.DeserializeObject<ActivityEventClaimResponse>(content);
                isSending = false;
                OnClaimSuccess(avtivityEventClaimResponse);
            },
            (error) =>
            {
                isSending = false;
            });
    }
    
    private void OnClaimSuccess(ActivityEventClaimResponse response)
    {
        var eventInfo = _info.eventList.Find(x => x.eventId == response.eventInfo.eventId);
        if (eventInfo==null)
        {
            return;
        }

        eventInfo.eventStatus = response.eventInfo.eventStatus;
        RefreshItems();
        
        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);

        if (eventInfo.rewardType == (int)BUDRewardType.RewardPgcResource)
        {
            panel.ShowPgcRewards(new List<string>() {eventInfo.pgcId}, eventInfo.eventName);
        }
        else
        {
            AccountDataManager.Inst.BalanceInfo.Refresh();
            var rewardList = new List<CommonRewardItemData>();
            CommonRewardItemData commonRewardItemData = new CommonRewardItemData();
            commonRewardItemData.IconSp = PgcUtils.LoadRewardIcon((BUDRewardType)eventInfo.rewardType, gameObject);
            commonRewardItemData.rewardName = PgcUtils.GetRewardName((BUDRewardType)eventInfo.rewardType);
            commonRewardItemData.RewardAmount = response.claimAmount;
            rewardList.Add(commonRewardItemData);
        
            panel.ShowRewards(rewardList);
        }
        
        UpdateRedDot();
    }
    
    private void onReportTaskFinsh(ActivityEventInfo info)
    {
        JObject jObject = new JObject()
        {
            // ["bizId"] = _info.activityId,
            ["eventId"] = (info.eventId + 19)
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.PostEvent,
            HttpMethod.POST, 
            JsonConvert.SerializeObject(jObject), 
            (content) =>
            {

                if (this == null)
                {
                    return;
                }
                
                var eventInfo = _info.eventList.Find(x => x.eventId == info.eventId);
                if (eventInfo==null)
                {
                    return;
                }

                eventInfo.eventStatus = (int)TaskClaimState.Enable;
                RefreshItems();
            },
            (error) =>
            {

            });
    }

    
}
