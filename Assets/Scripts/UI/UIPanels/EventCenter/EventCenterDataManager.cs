using AIGame.Base;
using EventTracking;
using Game.AINPCStudio;
using Game.AnimationStudio;
using Game.CommunityGame;
using GameUI;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using UI.UIPanels.FittingRoom;
using UI.UIPanels.ProfilePanel;
using UI.UIPanels.RechargePanel;

namespace Game.Event
{
    public class EventCenterDataManager : GlobalInstance<EventCenterDataManager>
    {
        private Dictionary<string, Action<TaskListRsp>> OnGetTaskDataSuccess = new Dictionary<string, Action<TaskListRsp>>();
        private Dictionary<string, Action<string>> OnGetTaskDataFail = new Dictionary<string, Action<string>>();
        private Dictionary<string, bool> TaskEnableState = new Dictionary<string, bool>();
        private Action activitySucc;
        private Dictionary<string, bool> ActivityEnableState = new Dictionary<string, bool>();

        private GetTaskListReq getTaskListReq = new GetTaskListReq()
        {
            idList = new List<string>
            {
                TASK_ID.S7PhoenixPack.ToString(), TASK_ID.S6TenPack.ToString(), TASK_ID.NewbieSevenDayTask.ToString(),
                TASK_ID.Daily.ToString(), TASK_ID.Weekly.ToString(), TASK_ID.SevenDayLogin.ToString(),
                TASK_ID.OnePack.ToString(), TASK_ID.SixYuanPack.ToString(), TASK_ID.NewbieCheckIn.ToString(),
                TASK_ID.S8CommunityCoinPack.ToString(),TASK_ID.MiserableNursePromise.ToString(),
                TASK_ID.S9LimitedTimePack.ToString(),
                TASK_ID.NewbieSevenDayTaskV2.ToString(),
                TASK_ID.NewbieCheckInV2.ToString(),
                TASK_ID.NewbieSevenDayAccumulativeTask.ToString(),
                TASK_ID.NewbieDressUpTask.ToString(),
                TASK_ID.RainyRibbitPack.ToString(),
            }
        };


        private GetTaskListReq getActivityListReq = new GetTaskListReq()
        {
            idList = new List<string>
            {
                ActivityId.ZongziSummerCarnival.ToString(),
                ActivityId.AbandonedHospitalEscape.ToString(),
                ActivityId.QingmingOuting.ToString(),
                ActivityId.FirstChargeSixYuan.ToString(),
                ActivityId.FirstChargeOneYuan.ToString(),
                ActivityId.S9SeasonRecharge.ToString(),
                ActivityId.S14SeasonRecharge.ToString(),
                ActivityId.S15SeasonRecharge.ToString()
            }
        };

        public List<EventCenterTopBarConfig> TopBarConfigs = new()
        {
            new()
            {
                type = TASK_ID.SevenDayLogin,
                name = "七天登录",
                firstTitle = "7天登录任务",
                subTitle = "确保每天不要错过，否则你将回到第一天",
                panelPath = "Assets/Loadable/UI/UIPanel/EventCenter/Prefabs/Style2EventBottomPanel.prefab",
            },
            new()
            {
                type = TASK_ID.Daily,
                name = "每日任务",
                firstTitle = "每日任务",
                subTitle = "完成每日任务获得奖励",
                panelPath = "Assets/Loadable/UI/UIPanel/EventCenter/Prefabs/Style1EventBottomPanel.prefab",
            },
            new()
            {
                type = TASK_ID.Weekly,
                name = "每周任务",
                firstTitle = "每周任务",
                subTitle = "完成每周任务获得奖励",
                panelPath = "Assets/Loadable/UI/UIPanel/EventCenter/Prefabs/Style1EventBottomPanel.prefab",
            },
        };

        public void AddTaskDataCallBack(string taskId, Action<TaskListRsp> succ = null, Action<string> fail = null)
        {
            if (OnGetTaskDataSuccess.ContainsKey(taskId))
            {
                OnGetTaskDataSuccess.Remove(taskId);
            }
            OnGetTaskDataSuccess.Add(taskId, succ);

            if (OnGetTaskDataFail.ContainsKey(taskId))
            {
                OnGetTaskDataFail.Remove(taskId);
            }
            OnGetTaskDataFail.Add(taskId, fail);
        }

        public void SetActivityDataCallback(Action succ = null)
        {
            this.activitySucc = succ;
        }

        public void ClearActivityDataCallback()
        {
            this.activitySucc = null;
        }

        public void RemoveTaskDataCallBack(string taskId)
        {
            OnGetTaskDataSuccess.Remove(taskId);
            OnGetTaskDataFail.Remove(taskId);
        }

        public void SkipToTask(EventCenterSkipType skipType, params object[] args)
        {
            switch (skipType)
            {
                case EventCenterSkipType.PlayMap:
                    UIManager.Inst.OpenPanel<NewSeasonPassPanel>(PanelId.CommunityGamesPanel); //社区地图
                    break;
                case EventCenterSkipType.UseEmote:
                    UIManager.Inst.OpenPanel(PanelId.CommunityGamesPanel);
                    break;
                case EventCenterSkipType.ChangeAvatar:
                    UIManager.Inst.OpenPanel(PanelId.FittingRoomPanel);
                    break;
                case EventCenterSkipType.GetSkin:
                    UIManager.Inst.OpenPanel(PanelId.FittingRoomPanel);
                    break;
                case EventCenterSkipType.PlayGashapon:
                    UIManager.Inst.OpenPanel(PanelId.StoreMallPanel);
                    break;
                case EventCenterSkipType.ChangeNick:
                    UIManager.Inst.OpenPanel<ChangeNickNamePanel>(PanelId.ChangeNickNamePanel, AccountDataManager.Inst.UserInfo);
                    break;
                case EventCenterSkipType.EditBio:
                    if (!UIManager.Inst.TryFindPanel(WindowId.ProfileWindow, PanelId.EditDescPanel, out _))
                    {
                        UIManager.Inst.OpenPanel<EditDescPanel>(PanelId.EditDescPanel, AccountDataManager.Inst.UserInfo);
                    }
                    break;
                case EventCenterSkipType.ChangeHead:
                    UIManager.Inst.OpenPanel<ChangeHeadImgPanel>(PanelId.ChangeHeadImgPanel, AccountDataManager.Inst.UserInfo);
                    break;
                case EventCenterSkipType.InstrumentEditorDraft:
                    UIManager.Inst.OpenPanel(PanelId.MusicalInstrumentStudioPanel);
                    break;
                case EventCenterSkipType.ScoreEditorDraft:
                    UIManager.Inst.OpenPanel(PanelId.MusicScoreStudioPanel);
                    break;
                case EventCenterSkipType.AvatarBundle:
                    var fittingRoom = UIManager.Inst.SwapPanel(PanelId.FittingRoomPanel) as FittingRoomPanel;
                    fittingRoom?.JumpTo(MainTabs.Tab.Ugc, 50098);
                    break;
                case EventCenterSkipType.AvatarStudio:
                    UIManager.Inst.OpenPanel(PanelId.AvatarStudioMainPanel);
                    break;
                case EventCenterSkipType.PetFittingRoom:
                    UIManager.Inst.OpenPanel(PanelId.FittingRoomPanel, true);
                    break;
                case EventCenterSkipType.PetStudio:
                    UIManager.Inst.OpenPanel(PanelId.AvatarStudioMainPanel, CharacterStyle.Pet);
                    break;
                case EventCenterSkipType.Contest:
                    ContestEventManager.Inst.OpenContestPage();
                    break;
                case EventCenterSkipType.MusicStudio:
                    UIManager.Inst.OpenPanel(PanelId.InstrumentStudioCategoryPanel);
                    break;
                case EventCenterSkipType.GameStudio:
                    UIManager.Inst.OpenPanel(PanelId.AssetStudioCategoryPanel);
                    break;
                case EventCenterSkipType.AnimationStudio:
                    UIManager.Inst.OpenPanel(PanelId.AnimationStudioMainPanel, AnimationStudioType.Animation);
                    break;
                case EventCenterSkipType.FittingRoomAnimation:
                    var animFittingRoomPanel = UIManager.Inst.SwapPanel(PanelId.FittingRoomPanel) as FittingRoomPanel;
                    animFittingRoomPanel?.JumpTo(MainTabs.Tab.Ugc, 90100);
                    break;
                case EventCenterSkipType.PoseStudio:
                    UIManager.Inst.OpenPanel(PanelId.AnimationStudioMainPanel, AnimationStudioType.Pose);
                    break;
                case EventCenterSkipType.PropStore:
                    UIManager.Inst.OpenPanel(PanelId.PropStorePanel);
                    break;
                case EventCenterSkipType.CoinGashapon:
                    UIManager.Inst.OpenPanel(PanelId.StoreMallPanel, "lottery.coin");
                    break;
                case EventCenterSkipType.SeasonPassDailyTask:
                    var seasonPassPanel = UIManager.Inst.FindPanel<NewSeasonPassPanel>(PanelId.NewSeasonPassPanel);
                    if (seasonPassPanel == null)
                    {
                        seasonPassPanel = UIManager.Inst.OpenPanel<NewSeasonPassPanel>(PanelId.NewSeasonPassPanel);
                    }
                    var type = seasonPassPanel.GetCurSeasonPassType();
                    seasonPassPanel.SwitchView(type, "SeasonTaskView");
                    break;
                case EventCenterSkipType.ApartmentEscape:
                    AIYandereStartPanel aiYandereStartPanel = UIManager.Inst.OpenPanel<AIYandereStartPanel>(PanelId.AIYandereStartPanel);
                    break;
                case EventCenterSkipType.AIBuddyChat:
                    if (args != null && args.Length > 0)
                    {
                        UIManager.Inst.OpenPanel<AINpcChatPanel>(PanelId.AINpcChatPanel, args);
                    }
                    else
                    {
                        UIManager.Inst.OpenPanel(PanelId.AIBuddyListPanel);
                    }
                    break;
                case EventCenterSkipType.AINpcStudio:
                    UIManager.Inst.OpenPanel(PanelId.AINpcStudioMainPanel);
                    break;
                case EventCenterSkipType.AINpcStore:
                    UIManager.Inst.OpenPanel(PanelId.AINpcStorePanel);
                    break;
                case EventCenterSkipType.FriendList:
                    UIManager.Inst.OpenPanel(PanelId.IntimacySystemPanel);
                    break;
                case EventCenterSkipType.SleepKoi:
                    UIManager.Inst.OpenPanel(PanelId.RechargePanel, (int)RechargeId.SleepyCoi);
                    break;
                case EventCenterSkipType.LuckyCoinGashapon:
                    UIManager.Inst.OpenPanel(PanelId.StoreMallPanel, "lottery.newCottageCore");
                    break;
                case EventCenterSkipType.Login7Day:
                    UIManager.Inst.OpenPanel<ActivityCenterPanel>(PanelId.ActivityCenterPanel, ActivityId.SevenDaySignInGift.ToString());
                    break;

                case EventCenterSkipType.GoAIHospital:
                    UIManager.Inst.OpenPanel(PanelId.AIHospitalMainEntryPanel);
                    break;
                case EventCenterSkipType.PlayAIHospital:
                    AIHospitalUtils.Inst.EnterOfficalHospitalGame();
                    UIManager.Inst.ClosePanel(PanelId.ActivityCenterPanel);
                    break;
                case EventCenterSkipType.MaterialStore:
                    UIManager.Inst.OpenPanel<MaterialStorePanel>(PanelId.MaterialStorePanel);
                    break;
                case EventCenterSkipType.HospitalExchangeStore:
                    UIManager.Inst.OpenPanel(PanelId.AIHospitalStorePanel);
                    break;
                case EventCenterSkipType.LuckeyGashapon:
                    UIManager.Inst.OpenPanel(PanelId.StoreMallPanel, "lottery.newCottageCore");
                    break;
                case EventCenterSkipType.OCActivity:
                    OcCompetitionSystem.Inst.OpenPanel();
                    break;
                case EventCenterSkipType.PlayGashaponSeason:
                    UIManager.Inst.OpenPanel(PanelId.StoreMallPanel, "lottery.whiteDewDrowningStar");
                    break;
                case EventCenterSkipType.Jiejiele:
                    UIManager.Inst.OpenPanel(PanelId.ConnectingGamePanel);
                    break;
                case EventCenterSkipType.DumplingGashapon:
                    LoadEvent.ReportTask(165, 1);
                    UIManager.Inst.OpenPanel(PanelId.StoreMallPanel, "lottery.lionLanternsRolling");
                    break;
                case EventCenterSkipType.TakePhoto:
                    //LoadEvent.ReportTask(165, 1);
                    //UIManager.Inst.OpenPanel(PanelId.StoreMallPanel, "lottery.lionLanternsRolling");
                    break;
                case EventCenterSkipType.Album:
                    UIManager.Inst.OpenPanel(PanelId.AlbumPanel);
                    break;
                case EventCenterSkipType.SockGashapon:
                    LoadEvent.ReportTask(184, 1);
                    UIManager.Inst.OpenPanel(PanelId.StoreMallPanel, "lottery.wawaKindergartenSuit");
                    break;
                case EventCenterSkipType.SockCarGashapon:
                    LoadEvent.ReportTask(184, 2);
                    UIManager.Inst.OpenPanel(PanelId.StoreMallPanel, "lottery.littleTailCatCar");
                    break;
                case EventCenterSkipType.SockGift:
                    LoadEvent.ReportTask(184, 3);
                    UIManager.Inst.OpenPanel(PanelId.RechargePanel, (int)RechargeId.WaCoinPack);
                    break;
                case EventCenterSkipType.YunningtiantongGashapon:
                    LoadEvent.ReportTask(184, 4);
                    UIManager.Inst.OpenPanel(PanelId.StoreMallPanel, "lottery.cloudCone");
                    break;
                case EventCenterSkipType.SeasonGashapon:
                    LoadEvent.ReportTask(184, 5);
                    UIManager.Inst.OpenPanel(PanelId.StoreMallPanel, "lottery.whiteDewDrowningStar");
                    break;
                case EventCenterSkipType.TheatreStudioCategory:
                    UIManager.Inst.OpenPanel(PanelId.TheatreStudioCategoryPanel);
                    break;
                case EventCenterSkipType.FittingRoomAvatarCard:
                    var fittingRoomP = UIManager.Inst.SwapPanel(PanelId.FittingRoomPanel) as FittingRoomPanel;
                    if (fittingRoomP)
                    {
                        fittingRoomP.JumpTo(MainTabs.Tab.Ugc, GameData.PgcData.UniqueType.Get(GameData.PgcData.ResourceType.AvatarCard, (int)GameData.PgcData.UgcTheatreSubType.AvatarCard));
                    }
                    break;
                case EventCenterSkipType.FittingRoomTheatre:
                    var fittingRoomPl = UIManager.Inst.SwapPanel(PanelId.FittingRoomPanel) as FittingRoomPanel;
                    if (fittingRoomPl)
                    {
                        fittingRoomPl.JumpTo(MainTabs.Tab.Ugc, GameData.PgcData.UniqueType.Get(GameData.PgcData.ResourceType.Theatre, (int)GameData.PgcData.UgcTheatreSubType.Theatre));
                    }
                    break;
            }
        }

        public bool CheckTaskIsEnable(TASK_ID taskId)
        {
            bool isEnable = false;
            if (TaskEnableState.ContainsKey(taskId.ToString()))
            {
                isEnable = TaskEnableState[taskId.ToString()];
            }
            LoggerUtils.Log("taskID:" + taskId.ToString() + "isEnable:" + isEnable);
            return isEnable;
        }

        public bool CheckActivityIsEnable(string activityId)
        {
            bool isEnable = false;
            if (ActivityEnableState.ContainsKey(activityId))
            {
                isEnable = ActivityEnableState[activityId];
            }

            return isEnable;
        }

        public bool CheckTaskIsAllComplete(TaskInfoData taskInfoData, bool judgeTaskStatus = false)
        {
            if (taskInfoData == null || taskInfoData.eventList == null || taskInfoData.eventList.Count <= 0)
            {
                return false;
            }
            if (taskInfoData.taskStatus == 2 && judgeTaskStatus)//taskStatus==2 任务类型为2
            {
                return true;
            }
            bool isFinish = true;
            foreach (var taskItemData in taskInfoData.eventList)
            {
                if (taskItemData.eventStatus != (int)EventStatus.Finish)
                {
                    isFinish = false;
                    break;
                }
            }
            return isFinish;
        }

        public void GetTaskInfo(TASK_ID taskId = TASK_ID.Default)
        {
            var reqParam = JsonConvert.SerializeObject(getTaskListReq);
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.TaskList, HttpMethod.POST, reqParam, (content) =>
            {
                var taskListRsp = JsonConvert.DeserializeObject<TaskListRsp>(content);
                if (taskListRsp == null || taskListRsp.list == null)
                    return;

                taskListRsp.list.ForEach(x =>
                {
                    if (TaskEnableState.ContainsKey(x.taskId))
                    {
                        TaskEnableState[x.taskId] = x.taskStatus == 1;
                    }
                    else
                    {
                        TaskEnableState.Add(x.taskId, x.taskStatus == 1);
                    }
                });

                foreach (var taskCb in OnGetTaskDataSuccess.Values)
                {
                    taskCb?.Invoke(taskListRsp);
                }
            }, (error) =>
            {
                OnGetTaskDataFail.TryGetValue(taskId.ToString(), out var onFailAct);
                onFailAct?.Invoke(error);
            });
        }

        public void GetActivityInfo(ActivityId activityId = ActivityId.FirstChargeOneYuan)
        {
            var reqParam = JsonConvert.SerializeObject(getActivityListReq);
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ActivityList, HttpMethod.POST, reqParam, (content) =>
            {
                ActivityResponse activityResponse = JsonConvert.DeserializeObject<ActivityResponse>(content);

                if (activityResponse == null || activityResponse.list == null)
                    return;

                activityResponse.list.ForEach(x =>
                {
                    if (ActivityEnableState.ContainsKey(x.activityId))
                    {
                        ActivityEnableState[x.activityId] = x.activityStatus == 1;
                    }
                    else
                    {
                        ActivityEnableState.Add(x.activityId, x.activityStatus == 1);
                    }
                });

                activitySucc?.Invoke();

            }, (error) =>
            {
            });
        }

        public void CliamReward(string taskId, int eventId, int claimType, int rewardIndex = 0, Action<TaskClaimRsp> succ = null, Action fail = null)
        {
            JObject jb = new JObject
            {
                ["taskId"] = taskId,
                ["eventId"] = eventId,
                ["claimType"] = claimType,
                ["rewardIndex"] = rewardIndex
            };
            var reqParam = JsonConvert.SerializeObject(jb);
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ClaimTaskRewards, HttpMethod.POST, reqParam, (content) =>
            {
                if (string.IsNullOrEmpty(content))
                    return;

                TaskClaimRsp data = JsonConvert.DeserializeObject<TaskClaimRsp>(content);
                succ?.Invoke(data);
            }, (error) =>
            {
                fail?.Invoke();
            });
        }

        public void ReportTask(PostEventId eventId, Action<bool> success = null, Action fail = null)
        {
            JObject jObject = new JObject()
            {
                ["eventId"] = (int)eventId
            };
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.PostEvent,
                HttpMethod.POST,
                JsonConvert.SerializeObject(jObject),
                (content) =>
                {

                },
                (error) =>
                {

                });
        }

        public override void Release()
        {
            base.Release();
            TaskEnableState.Clear();
            OnGetTaskDataSuccess.Clear();
            OnGetTaskDataFail.Clear();
            ActivityEnableState.Clear();
            // OnGetActivityDataSuccess.Clear();
            // OnGetActivityDataFail.Clear();
        }

    }

    public class EventCenterTopBarConfig
    {
        public TASK_ID type;
        public string name;
        public string firstTitle;
        public string subTitle;
        public string panelPath;
    }

    public class TaskClaimRsp
    {
        public List<TaskItemData> eventList;//这个回包只有 eventId 和 eventStatus
        public int totalClaimAmount = 0;
        public List<TaskClaimRewardData> rewardList;
    }

    public class TaskClaimRewardData
    {
        public int amount;
        public int rewardType;
        public List<string> pgcIdList;
    }

    public class TaskListRsp
    {
        public List<TaskInfoData> list;
    }

    public class GetTaskListReq
    {
        public List<string> idList = new List<string>();
    }

    public class TaskInfoData
    {
        public string taskId;
        public int taskStatus;
        public List<TaskItemData> eventList;
        public string endDate;
        public TaskProgress taskProgress;
        public int value; //补签次数
    }

    public class TaskProgress
    {
        public int start;
        public int end;
    }

    public class TaskItemData
    {
        public int eventId; // 任务ItemId
        public string eventName; // 任务名称
        public int targetAmount; // 任务目标数量
        public int finishAmount; // 任务当前完成数
        public int groupId; //任务分组
        public int memberId = 0; //新手任务可用：组内的子任务
        public int eventStatus = 1;// 任务领取状态 1 不可领取 2 可领取 3 领取完成
        public int skipType;//跳转类型
        public List<string> rewardPgcList; //可能是领取一批Pgc资源
        public List<TaskClaimRewardData> rewardList;

        public int rewardNum;
        public string targetAmountList;  //阶段任务使用
        public string rewardNumList;//阶段任务使用

        public void RefreshEventStatus(int status)
        {
            this.eventStatus = status;
        }

        public void RefreshFinishAmount(int amount)
        {
            this.finishAmount = amount;
        }
    }

    public enum EventStatus
    {
        Default = 0,
        UnClaim = 1,
        Claim = 2,
        Finish = 3,
        Signed = 4,
    }

    public enum TASK_ID
    {
        Default = 0,
        NewbieSevenDayTask = 1, //新手任务
        Daily = 2, //每日任务
        Weekly = 3, //每周任务
        SevenDayLogin = 4, //七天登陆任务
        OnePack = 5,//一元礼包
        SixYuanPack = 6,//六元礼包
        CreatorTitle = 7,
        NewbieCheckIn = 8,
        RainyRibbitPack = 9,
        WasabiPack = 10,
        // NewComerCommunityCoinV2 = 11,  //s3开始的新人社区商品币任务
        HalloweenPack = 12, //万圣节6元礼包
        WeirdCorePack = 13,
        Y2KPack = 14,
        LimitedTimeCurrencyPack = 15,
        RechargeBenefits = 16, //充值福利
        SmannyPack = 17, // 十元礼包
        S5LimitedTimePack = 18, // 18元礼包
        LotteryMagicTrial = 19, //魔法试炼季扭蛋任务
        LotteryBudChronicles = 20, //Bud编年史扭蛋任务
        LotteryJingleBells = 21, //圣诞响叮当扭蛋任务
        SeasonDaily = 22,
        SeasonWeekly = 23,
        SeasonFirstWeek = 24,
        SeasonSecondWeek = 25,
        SeasonThirdWeek = 26,
        SeasonPassChallenge = 27,
        AIBuddyIntimacyTask = 36,
        S7SeasonDaily = 37, // s7 赛季日任务
        S7SeasonWeeklyActive = 38, // s7 赛季周活跃任务
        S7SeasonWeekTask = 39, // s7 赛季周任务
        S7SeasonChallenge = 40, // s7 赛季任务
        S6TenPack = 41,//s6 10元礼包
        S6OnePack = 42,
        S6SixPack = 43,
        S7PhoenixPack = 44,
        S8SeasonDaily = 45, // s8 赛季日任务
        S8SeasonWeeklyActive = 46, // s8 赛季周活跃任务
        S8SeasonWeekTask = 47, // s8 赛季周任务
        S8SeasonChallenge = 48, // s8 赛季任务
        S8TwentyYuanPackage = 49,
        S8CommunityCoinPack = 50,
        S9SeasonDaily = 51, // s9 赛季日任务
        S9SeasonWeeklyActive = 52, // s9 赛季周活跃任务
        S9SeasonWeekTask = 53, // s9 赛季周任务
        S9SeasonChallenge = 54, // s9 赛季任务
        S9AiGameSeasonWeekExperience = 55, // s9 Aigame 每日周经验任务
        S9AiGameSeasonWeekTask = 56, // s9 Aigame 每周任务
        S9AiGameSeasonChallenge = 57, // s9 AiGame 赛季任务
        S9LimitedTimePack = 58,
        MiserableNursePromise = 59,// 丧丧护士的约定
        S10SeasonDaily = 60, // s10 赛季日任务
        S10SeasonWeeklyActive = 61, // s10 赛季周活跃任务
        S10SeasonWeekTask = 62, // s10 赛季周任务
        S10SeasonChallenge = 63, // s10 赛季任务
        NewbieCheckInV2 = 64, // S10 新手签到任务
        NewbieSevenDayTaskV2 = 65, // S10 新手 7 天任务
        NewbieSevenDayTaskV3 = 66, // S10 新手 7 天任务V3
        NewbieSevenDayAccumulativeTask = 67, // S10 新手 7 天任务V3 之累计任务
        NewbieDressUpTask = 68, //破冰消费任务
        NewbieDressUpTask18 = 69, //破冰消费任务
        NewbieDressUpTask30 = 70, //破冰消费任务

        DawnSurprise = 72, // 曙光惊喜
        SummerEnjoyment = 73, // 夏日畅享
        CoolSummerEnjoyment = 74, // 酷夏尊享
        DeluxeSupplies = 75, // 豪华补给
        AnniversaryBenefits = 76, // 周年福利
        SummerDate = 77,

        S12SeasonDaily = 78, // s12 赛季日任务
        S12SeasonWeeklyActive = 79, // s12 赛季周活跃任务
        S12SeasonWeekTask = 80, // s12 赛季周任务
        S12SeasonChallenge = 81, // s12 赛季任务

        S13SeasonDaily = 82, // s12 赛季日任务
        S13SeasonWeeklyActive = 83, // s12 赛季周活跃任务
        S13SeasonWeekTask = 84, // s12 赛季周任务
        S13SeasonChallenge = 85, // s12 赛季任务
        TreePlantingDayDailyTask = 90, // 植树节每日活跃任务

        S14SeasonDaily = 91, // s14 赛季日任务
        S14SeasonWeeklyActive = 92, // s14 赛季周活跃任务
        S14SeasonWeekTask = 93, // s14 赛季周任务
        S14SeasonChallenge = 94, // s14 赛季任务

        S15SeasonDaily = 101, // s15 赛季日任务
        S15SeasonWeeklyActive = 102, // s15 赛季周活跃任务
        S15SeasonWeekTask = 103, // s15 赛季周任务
        S15SeasonChallenge = 106, // s15 赛季任务

        AlbumDailyTask = 96,// 相机每日活跃任务
        AlbumGrandTotal = 97,// 相机累计任务
        S15TheaterDaily = 98, //S15剧场活动每日任务
        S15TheaterDailyGrandTotal = 99,//S15剧场活动成就任务
    }

    public enum TaskClaimState
    {
        Default = 0,
        Unable = 1,
        Enable = 2,
        Finished = 3,
    }

    public enum EventCenterSkipType
    {
        ErrType = 0,
        Login = 1,      //登录
        PlayMap = 2,    // 地图游玩
        GetSkin = 3,    // 获取皮肤
        PlayGashapon = 4, // 扭蛋
        OnlineTime = 5, //在线时长
        ChangeAvatar = 6, // 试衣间
        UseEmote = 7, // 用表情
        ChangeNick = 8, // 换名称
        EditBio = 9, // 编辑bio
        ChangeHead = 10, // 换头像
        InstrumentEditorDraft = 11, // 乐器编辑器草稿
        ScoreEditorDraft = 12, // 乐谱编辑器草稿
        AvatarBundle = 13, // 试衣间 bundle
        AvatarStudio = 14, // 形象工作室
        PetFittingRoom = 15,//宠物试衣间
        PetStudio = 16, //宠物工作室
        Contest = 17, // 活动大赛
        MusicStudio = 18, // 音乐工作室
        GameStudio = 19, // 游戏工作室
        AnimationStudio = 20, // 动画工作室
        FittingRoomAnimation = 21, // 试衣间、动作table
        PoseStudio = 22, // 姿势编辑器
        PropStore = 23, //道具商城
        CoinGashapon = 24, // 金币扭蛋
        SeasonPassDailyTask = 25, // 赛季通行证每日任务
        ApartmentEscape = 26,//逃脱公寓
        AIBuddyChat = 27,//与伙伴聊天
        AINpcStudio = 28, // Npc工作室
        AINpcStore = 29, // Npc商城
        FriendList = 30, // 好友列表
        Login7Day = 31, // 活动-七天签到礼
        SleepKoi = 32,// 跳转瞌睡小锦鲤
        LuckyCoinGashapon = 33, //幸运币扭蛋跳转
        GoAIHospital = 34, //跳转废弃医院
        PlayAIHospital = 35, //开始游玩废弃医院
        MaterialStore = 36, //材质商城
        HospitalExchangeStore = 37,
        LuckeyGashapon = 38,//薯条扭蛋
        OCActivity = 39, //OC大赛
        PlayGashaponSeason = 40, // 赛季扭蛋
        Jiejiele = 41, // 接接乐游戏
        DumplingGashapon = 42, // 元宵扭蛋
        TakePhoto = 43, // 摄影大赛
        Album = 44,//相册
        CarGashapon = 45, // 载具扭蛋扭蛋
        SockGift = 46, // 查看袜袜幼稚园特惠礼包
        YunningtiantongGashapon = 47, // 云凝甜筒号扭蛋
        SeasonGashapon = 48, // 赛季扭蛋
        SockGashapon = 49, // 袜袜扭蛋
        SockCarGashapon = 50, // 小尾喵车扭蛋
        TheatreStudioCategory = 51, // 剧场工作室分类面板
        FittingRoomAvatarCard = 52,//形象工作室演员
        FittingRoomTheatre = 53,//形象工作室剧场
    }

    public enum PostEventId
    {
        ErrType = 0,
        EmoteInGame = 6,
        CallAIBuddyInMap = 83, // 召唤AIBuddy
        PlayApartmentEscape = 87, // 游玩公寓逃脱模拟器
        ViewEscapeSimulator = 92,//查看逃脱模拟器，客户端发送
        TakePhotoCheckIn = 115, // 拍照📷打卡
        ViewSleepyKoi = 116, // 查看瞌睡小锦鲤
        PlayAIGame = 125, // 游玩AI模拟器， 客户端上报
        UseDoubleEmoteInMap = 126, // 使用双人动作
        UnlockAIGame = 143,// 解锁AIGame解决，成功或失败
    }


}

