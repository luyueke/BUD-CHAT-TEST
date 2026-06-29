using System.Collections.Generic;
using System.Linq;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Network;
using Network.Http;
using Basic.Utils;
using EventTracking;
using System;
using Game.Event;
using UI.UIPanels.ProfilePanel;
using UI.Manager;
using GameData.Manager;
using GameUI;
using Message;


public class AnniversarySummerMgr : IActivity
{


    public static readonly TASK_ID TaskId = TASK_ID.SummerDate;
    public static readonly string ConfigPath = "Assets/Loadable/UI/UIPanel/AnniversarySummer/AnniversarySummer.json";
    public static readonly string ViewPrefabPath = "Assets/Loadable/UI/UIPanel/AnniversarySummer/AnniversarySummer.prefab";
    private static AnniversarySummerMgr _instance;
    public static AnniversarySummerMgr Inst
    {
        get
        {
            if (_instance == null)
            {
                _instance = new AnniversarySummerMgr();
                ActivityManager.Inst.AddActivity(ActivityId.AnniversaryCelebrationSummer, _instance);
                RedDotSystemNew.Inst.AddReddotType(ReddotType.AnniversaryCelebrationSummer, _instance.IsEntryRedDot);
            }
            return _instance;
        }
    }

    public TaskInfoData taskInfo;

    public ActivityInfo activityInfo;

    public int currentDay = 0;
    public AnniversarySummerView view;

    public static readonly DateTime ACTIVITY_START_TIME = new DateTime(2025, 7, 25, 11, 0, 0);
    public static readonly DateTime ACTIVITY_END_TIME = new DateTime(2025, 8, 31, 23, 59, 59);


    //(day,index)
    public Dictionary<int, RewardPreviewInfo[]> RewardTypeList;

    public bool AnniversaryGiftClaimed = true; //周年庆礼包是否领取

    AnniversarySummerMgr()
    {
        RewardTypeList ??= new();
        RewardTypeList.Clear();
        RewardTypeList.Add(1, new RewardPreviewInfo[] { new RewardPreviewInfo(BUDRewardType.RewardLuckyCoin, CurrencyType.LuckyCoin, "", "", ""), new RewardPreviewInfo(BUDRewardType.RewardPgcResource, CurrencyType.None, "11000262", "周年庆小马气球", "") });
        RewardTypeList.Add(2, new RewardPreviewInfo[] { new RewardPreviewInfo(BUDRewardType.RewardYouYouCoin, CurrencyType.YouYouCoin, "", "", ""), new RewardPreviewInfo(BUDRewardType.RewardPgcResource, CurrencyType.None, "40300500", "跷跷板", "") });
        RewardTypeList.Add(3, new RewardPreviewInfo[] { new RewardPreviewInfo(BUDRewardType.RewardCoin, CurrencyType.Coin, "", "", ""), new RewardPreviewInfo(BUDRewardType.RewardCommunityAnimationTicket, CurrencyType.CommunityAnimationTicket, "", "", "") });
        RewardTypeList.Add(4, new RewardPreviewInfo[] { new RewardPreviewInfo(BUDRewardType.RewardYouYouCoin, CurrencyType.YouYouCoin, "", "", ""), new RewardPreviewInfo(BUDRewardType.RewardPgcResource, CurrencyType.None, "40300501", "滑滑梯", "") });
        RewardTypeList.Add(5, new RewardPreviewInfo[] { new RewardPreviewInfo(BUDRewardType.RewardCoin, CurrencyType.Coin, "", "", ""), new RewardPreviewInfo(BUDRewardType.RewardCommunityInstrumentTicket, CurrencyType.CommunityInstrumentTicket, "", "", "") });
        RewardTypeList.Add(6, new RewardPreviewInfo[] { new RewardPreviewInfo(BUDRewardType.RewardLuckyCoin, CurrencyType.LuckyCoin, "", "", ""), new RewardPreviewInfo(BUDRewardType.RewardAvatarFrame, CurrencyType.None, "130100029", "", "") });
        RewardTypeList.Add(7, new RewardPreviewInfo[] { new RewardPreviewInfo(BUDRewardType.RewardPurpleDreamCoin, CurrencyType.PurpleDreamCoin, "", "", ""), new RewardPreviewInfo(BUDRewardType.RewardPgcResource, CurrencyType.None, "40100503", "跳楼机", "") });
    }

    public void Init(GameObject go, AnniversarySummerView view)
    {
        InitActivityInfo(go);
        this.view = view;
    }

    void InitActivityInfo(GameObject go)
    {
        string activityConfigPath = "Assets/Loadable/UI/UIPanel/AnniversarySummer/AnniversarySummer.json";
        var textAsset = Loader.Load<TextAsset>(activityConfigPath, go);
        ActivityInfo info = JsonConvert.DeserializeObject<ActivityInfo>(textAsset.text);
        this.activityInfo = info;
    }

    public void GetTaskData()
    {
        var reqParam = new GetTaskListReq()
        {
            idList = new List<string>() { TaskId.ToString() }
        };
        Debug.Log("AnniversarySummerMgr请求活动信息: " + JsonConvert.SerializeObject(reqParam));
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.TaskList, HttpMethod.POST, JsonConvert.SerializeObject(reqParam), (content) =>
        {
            Debug.Log("AnniversarySummerMgr请求活动信息成功 success: " + content);
            OnGetTaskDataSuccess(JsonConvert.DeserializeObject<TaskListRsp>(content));
            if (this.view != null)
            {
                this.view.RefreshUI();
            }
        }, (error) =>
        {
            LoggerUtils.Log("AnniversarySummerMgr GetTaskData failed: " + error);
        });
    }
    private void OnGetTaskDataSuccess(TaskListRsp taskListRsp)
    {
        if (taskListRsp.list.Count == 0)
        {
            return;
        }
        taskInfo = taskListRsp.list[0];
        LoggerUtils.Log("AnniversarySummerMgr OnGetTaskDataSuccess: " + taskInfo.taskId);

        for (int i = taskInfo.eventList.Count - 1; i >= 0; i--)
        {
            var eventInfo = taskInfo.eventList[i];
            if (eventInfo.eventStatus == 2 || eventInfo.eventStatus == 3)
            {
                currentDay = i + 1;
                break;
            }
        }
    }

    public void RefreshTaskData(TaskItemData taskItemData)
    {
        for (int i = taskInfo.eventList.Count - 1; i >= 0; i--)
        {
            var eventInfo = taskInfo.eventList[i];
            if (eventInfo.eventId == taskItemData.eventId)
            {
                eventInfo.eventStatus = taskItemData.eventStatus;
                break;
            }
        }
    }

    public string GetPgcIdBy(int day, int rewardType)
    {
        List<TaskClaimRewardData> rewardList = new();
        foreach (var reward in RewardTypeList[day])
        {
            if ((int)reward.rewardType == rewardType)
            {
                return reward.pgcId;
            }
        }
        return "";
    }

    public void Testt(int ev)
    {
        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
        List<CommonRewardItemData> items = new List<CommonRewardItemData>();

        var list = RewardTypeList[ev];
        foreach (var reward in list)
        {
            CommonRewardItemData item;
            item = new CommonRewardItemData()
            {
                RewardAmount = 1,
                rewardType = (int)reward.rewardType,
                rewardName = PgcUtils.GetRewardName((BUDRewardType)reward.rewardType),
                pgcId = GetPgcIdBy(ev, (int)reward.rewardType)
            };
            items.Add(item);
        }
        panel.ShowRewards(items);
        panel.ShowCommonBtn("确定", null);
    }

    public void ClaimTaskReward(int eventId, Action<TaskClaimRsp> success, Action<string> fail)
    {
        Debug.Log("AnniversarySummerMgr 请求当天奖励领取:" + eventId);
        EventCenterDataManager.Inst.CliamReward(TaskId.ToString(), eventId, 1, 0, (claimRspData) =>
        {
            Debug.Log("AnniversarySummerMgr 请求当天奖励领取成功:" + eventId + "  " + JsonConvert.SerializeObject(claimRspData));
            List<TaskClaimRewardData> rewardList = claimRspData.rewardList;
            var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
            List<CommonRewardItemData> items = new List<CommonRewardItemData>();
            foreach (var reward in rewardList)
            {
                CommonRewardItemData item;
                item = new CommonRewardItemData()
                {
                    RewardAmount = reward.amount,
                    rewardType = reward.rewardType,
                    rewardName = PgcUtils.GetRewardName((BUDRewardType)reward.rewardType),
                    pgcId = GetPgcIdBy(eventId, reward.rewardType)
                };
                items.Add(item);
            }
            panel.ShowRewards(items);
            panel.ShowCommonBtn("确定", null);

            claimRspData.eventList.ForEach(eventInfo =>
            {
                RefreshTaskData(eventInfo);
            });
            success?.Invoke(claimRspData);
            if (this.view != null)
            {
                this.view.RefreshUI();
            }
            MessageHelper.Broadcast(MessageName.ReddotNotice);
            TokenDataManager.Inst.GetTokenData();
            AccountDataManager.Inst.BalanceInfo.Refresh();
            // VipDataManager.Inst.UpdateVipStatus();
            // ReddotManagerUtils.Inst.RefreshRedDot();
        });
    }

    public void ClaimActivityReward()
    {
        Debug.Log("AnniversarySummerMgr 请求周年赠礼领取:" + "AnniversaryGift");

        JObject jObject = new JObject()
        {
            ["activityId"] = "AnniversaryGift",
            ["eventId"] = 1,
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ClaimActivityReward,
            HttpMethod.POST,
            JsonConvert.SerializeObject(jObject),
            (content) =>
            {
                Debug.Log("AnniversarySummerMgr 请求周年赠礼领取成功:" + "AnniversaryGift" + "  " + content);
                ActivityEventClaimResponse activityEventClaimResponse = JsonConvert.DeserializeObject<ActivityEventClaimResponse>(content);
                AnniversaryGiftClaimed = true;
                var giftView = UIManager.Inst.FindPanel<AnniversaryGiftView>(PanelId.AnniversaryGiftView);
                if (giftView != null)
                {
                    giftView.RefreshUI();
                }
                var rewardList = activityEventClaimResponse.rewardList;
                var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
                List<CommonRewardItemData> items = new List<CommonRewardItemData>();
                for (int i = 0; i < rewardList.Count; i++)
                {
                    CommonRewardItemData item;
                    item = new CommonRewardItemData()
                    {
                        RewardAmount = rewardList[i].amount,
                        rewardType = rewardList[i].rewardType,
                        rewardName = PgcUtils.GetRewardName((BUDRewardType)rewardList[i].rewardType),
                    };
                    if (rewardList[i].pgcIdList?.Count > 0)
                    {
                        item.pgcId = rewardList[i].pgcIdList[0];
                    }
                    items.Add(item);
                }
                //补气泡
                CommonRewardItemData item2;
                item2 = new CommonRewardItemData()
                {
                    RewardAmount = 1,
                    rewardType = 44,
                    rewardName = PgcUtils.GetRewardName((BUDRewardType)44),
                    pgcId = "120100016"
                };
                items.Add(item2);
                //
                panel.ShowRewards(items);
                panel.ShowCommonBtn("确定", null);
                MessageHelper.Broadcast(MessageName.ReddotNotice);
                TokenDataManager.Inst.GetTokenData();
                AccountDataManager.Inst.BalanceInfo.Refresh();

            },
            (error) =>
            {
            });
    }


    public void GetActivityDataByHttp()
    {
        Debug.Log("AnniversarySummerMgr 请求周年赠礼状态:" + "AnniversaryGift");

        ActivityCenterInfoReq req = new ActivityCenterInfoReq();
        req.idList = new List<string>
        {
            "AnniversaryGift"
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ActivityList, HttpMethod.POST,
            JsonConvert.SerializeObject(req), (content) =>
            {
                ActivityResponse activityResponse = JsonConvert.DeserializeObject<ActivityResponse>(content);
                Debug.Log("AnniversarySummerMgr 请求周年赠礼状态成功:" + JsonConvert.SerializeObject(activityResponse));
                if (activityResponse.list?.Count > 0)
                {
                    var activityInfo = activityResponse.list[0];
                    AnniversaryGiftClaimed = activityInfo.activityStatus == 2;
                    Debug.Log("AnniversarySummerMgr 周年赠礼领取状态:" + activityInfo.activityStatus);
                }
            },
            (error) =>
            {
            });
    }


    public bool IsDuringActivity()
    {
        DateTime now = DateTime.Now;
        return now >= ACTIVITY_START_TIME && now <= ACTIVITY_END_TIME;
    }

    public void ShowPanel()
    {

    }

    public List<ReddotType> GetReddotTypes()
    {
        return new List<ReddotType>() { ReddotType.AnniversaryCelebrationSummer };
    }

    public void LoginActivityInfo(ActivityInfo activityInfo)
    {

    }

    public bool IsEntryOpen()
    {
        return true;
    }

    public bool IsOpen()
    {
        return true;
    }

    public bool IsEntryRedDot(string param)
    {
        if (!IsDuringActivity())
        {
            return false;
        }
        if (!AnniversaryGiftClaimed)
        {
            return true;
        }
        if(taskInfo == null)
        {
            return false;
        }
        for (int i = taskInfo.eventList.Count - 1; i >= 0; i--)
        {
            var eventInfo = taskInfo.eventList[i];
            if (eventInfo.eventStatus == 2)
            {
                return true;
            }
        }
        return false;
    }
}

