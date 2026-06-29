using Es;
using Game.Event;
using GameData;
using GameData.Base;
using GameData.BaseInfo;
using GameUI;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlantTreeSystem : GlobalInstance<PlantTreeSystem>
{
    public TaskListRsp DailyInfo;

    public ActivityInfo RechargeInfo;

    public ActivityInfo WaterInfo;

    public List<GamePropData> Seeds_pgc = new List<GamePropData>();

    List<DraftListItem> seeds = new List<DraftListItem>();
    public List<DraftListItem> Seeds { get { return seeds; } }

    public MapListResponse MapListResponse;

    List<PlantUserInfo> friendsInfos = new List<PlantUserInfo>();
    public List<PlantUserInfo> FriendsInfos { get { return friendsInfos; } }

    public PlantUserRsp FriendsListResData;

    public string searchStr;

    //public List<string> productIds = new List<string>();

    public override void Initialize()
    {
        base.Initialize();
//#if UNITY_ANDROID
//        productIds.Add("android_2026zhishujieshuidi6");
//        productIds.Add("android_2026zhishujiefeiliao30");
//        productIds.Add("android_2026zhishujiezhuyepifu168");
//#else
//        productIds.Add("ios_2026zhishujieshuidi6");
//        productIds.Add("ios_2026zhishujiefeiliao30");
//        productIds.Add("ios_2026zhishujiezhuyepifu168");
//#endif

        foreach (var item in Es.DataTables.GetGamePropDataList())
        {
            if (item.BannerType == -1 && item.ModelType == 19)
            {
                Seeds_pgc.Add(item);
            }
        }
    }


    #region Web
    public void TaskReq() {
       var   reqParam = new GetTaskListReq()
        {
            idList = new List<string>() {
                TASK_ID.TreePlantingDayDailyTask.ToString()
            }
        };

        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.TaskList, HttpMethod.POST,
            JsonConvert.SerializeObject(reqParam), (content) =>
            {
                DailyInfo = JsonConvert.DeserializeObject<TaskListRsp>(content);
                if (DailyInfo == null || DailyInfo.list == null)
                    return;
                MessageHelper.Broadcast(MessageName.PlantTreeUpdate);

            }, (error) =>
            {
                LoggerUtils.Log($"TaskReq: {error}");
            });
    }
    public void ActivityReq()
    {
        ActivityCenterInfoReq req = new ActivityCenterInfoReq();
        req.idList = new List<string>();
        req.idList.Add(ActivityId.TreePlantingDayDailyRecharge.ToString());
        req.idList.Add(ActivityId.TreePlantingDayWateringActivity.ToString());
        TaskReq();
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ActivityList, HttpMethod.POST, JsonConvert.SerializeObject(req), (content) =>
        {
            LoggerUtils.Log($"收到活动请求，idList: {content}");
            ActivityResponse activityResponse = JsonConvert.DeserializeObject<ActivityResponse>(content);
            if (activityResponse.list != null)
            {
                foreach (var item in activityResponse.list)
                {
                    if (Enum.TryParse(item.activityId, out ActivityId type))
                    {
                        switch (type)
                        {
                            case ActivityId.TreePlantingDayDailyRecharge:
                                RechargeInfo = item;
                                break;
                            case ActivityId.TreePlantingDayWateringActivity:
                                WaterInfo = item;
                                break;
                        }
                    }
                }
                MessageHelper.Broadcast(MessageName.PlantTreeUpdate);
                MessageHelper.Broadcast(MessageName.ReddotNotice);
            }

            //客服数据
            if (!string.IsNullOrEmpty(activityResponse.customerServiceUrl))
            {

            }
        },
        (error) =>
        {
            LoggerUtils.Log($"ActivityReq，idList: {error}");
        });

    }
    public void RankListReq(Action<ContestRankListReq> resultAction = null)
    {
        var jb = new JObject()
        {
            ["listType"] = 3,
            ["cookie"] = "",
            ["contestId"] = "0000000000-10",
        };
        var url = HttpUrlDefine.ContestEntryList;

        var now = TcpTimeSystem.Inst.ServerDataTime;
        bool isMarch = now.Year == 2026 && now.Month == 3 && (now.Day == 28 || now.Day == 29);
        if (isMarch)
        {
            url = HttpUrlDefine.ContestRankingList;

            jb = new JObject()
            {
                ["cookie"] = "",
                ["contestId"] = "0000000000-10",
            };
        }

        var paramStr = JsonConvert.SerializeObject(jb);
        NetworkManager.Inst.SendHttpRequest(url,
            HttpMethod.GET,
            paramStr,
            (content) =>
            {
                ContestRankListReq response = JsonConvert.DeserializeObject<ContestRankListReq>(content);

                resultAction?.Invoke(response);
            },
            (error) =>
            {
                Debug.LogError("RankListReq " + error);
            });
    }
    public void RequestFriendList(string str,Action<List<PlantUserInfo>> onResult)
    {
        if (str != searchStr)
        {
            FriendsListResData = null;
            friendsInfos.Clear();
        }

        if (str == searchStr && FriendsListResData != null && FriendsListResData.isEnd == 1)
        {
            onResult?.Invoke(FriendsListResData.list);
            return;
        }

        searchStr = str;

        var req = new JObject()
        {
            ["searchWord"] = str,
            ["cookie"] = FriendsListResData?.cookie,
        };

        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.PlantUserList, HttpMethod.GET, JsonConvert.SerializeObject(req),
        onReceive: msg =>
        {
            FriendsListResData = JsonConvert.DeserializeObject<PlantUserRsp>(msg);
            if (FriendsListResData.list != null)
            {
                friendsInfos.AddRange(FriendsListResData.list);
            }

            onResult?.Invoke(FriendsListResData.list);

        }, onFail: arg0 => { Debug.LogError("RequestFriendList " + arg0); });
    }
    public void SeedInfoReq(Action<List<DraftListItem>> resultAction)
    {
        if (MapListResponse != null && MapListResponse.isEnd == 1)
        {
            resultAction?.Invoke(MapListResponse.list);
            return;
        }

        var req = new MapListReq
        {
            cookie = MapListResponse?.cookie,
            uid = AccountDataManager.Inst.Uid,
            //ugcType = ugcType
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.propPublishList, HttpMethod.GET, JsonConvert.SerializeObject(req), (content) =>
        {
            MapListResponse = JsonConvert.DeserializeObject<MapListResponse>(content);
            if (MapListResponse.list == null)
            {
                MapListResponse.list = new List<DraftListItem>();
            }

            seeds.AddRange(MapListResponse.list);
            resultAction?.Invoke(MapListResponse.list);
        },
        (error) =>
        {
            Debug.LogError("SeedInfoReq " + error);
        });

    }
    public void RequestWater(string uid, int count, int type,Action ac = null)
    {
        var req = new JObject()
        {
            ["toUid"] = uid,
            ["count"] = count,
            ["type"] = type
        };

        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.PlantWater, HttpMethod.POST, JsonConvert.SerializeObject(req),
        onReceive: msg =>
        {
            AccountDataManager.Inst.BalanceInfo.Refresh();
            PlantTreeSystem.Inst.ActivityReq();
            ac?.Invoke();
        }, onFail: arg0 => { Debug.LogError("RequestWater " + arg0); });
    }
    #endregion



}

public class PlantUserRsp
{
    public int isEnd;
    public string cookie;
    public List<PlantUserInfo> list;
}
public class PlantUserInfo
{
    public AccountUserInfo userInfo;
    public PlantUserDayInfo treePlantingDayInfo;
}

public class PlantUserDayInfo
{
    public string plantingCover;
    public int growthValue;
    public int growthRank;
    public int isEndVote; //是否结束浇水周期
    public bool isVoted;
}