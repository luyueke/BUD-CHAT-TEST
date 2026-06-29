using System;
using System.Collections.Generic;
using System.Linq;
using Es;
using EventTracking;
using Message;
using Newtonsoft.Json;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Event
{
    public class BudNewbieTaskV3Panel : BasePanel<BudNewbieTaskV2Panel>
    {
        public Transform topTaskContent;
        public Transform bottomTaskContent;
        public Transform accumulativeTaskContent;
        public Text fireTex;
        public Slider slider;
        public NewbieV3TaskItem newbieTaskItem;
        public NewbieV3TaskBottomItem newbieTaskBottomItem;
        public NewBieV3AccumulativeTaskItem newBieV3AccumulativeTaskItem;
        public NewBieV3AccumulativeTaskItem newBieV3AccumulativeTaskItemBig;
        private Transform _transBG;
        private CButton _btnBack;
        public CButton _tryOnBtn;
        public CButton _rewardBtn;
        public CButton _grayBtn;
        public RawImage bgRawTex;
        private GameObject _goLoading;
        private string _taskId = "NewbieSevenDayTaskV3";
        private string _accumulativeTaskId = "NewbieSevenDayAccumulativeTask";
        private bool isInit = false;
        public RewardPreviewPanel bundleShowpanel;
        private List<RewardItem> _rewardItems = new List<RewardItem>();
        private List<TaskConfig> _taskConfigs = new List<TaskConfig>();
        private List<TaskConfig> _taskaccumulativeConfigs = new List<TaskConfig>();
        private List<TaskItemData> _serverEventList;
        private List<TaskItemData> _serverAccumulativeEventList;

        private List<NewBieV3AccumulativeTaskItem> newBieAccumulativeTaskItems = new List<NewBieV3AccumulativeTaskItem>();
        private List<NewbieV3TaskItem> newbieTaskItems = new List<NewbieV3TaskItem>();
        private List<NewbieV3TaskBottomItem> newbieTaskBottomItems = new List<NewbieV3TaskBottomItem>();
        private List<int> targetAmonut = new List<int> { 0, 220, 180, 160, 140,120,100,100 };
        // 累积任务的奖励值列表
        private readonly List<int> rewardValues = new List<int> { 0, 100, 160, 200, 260, 400, 600, 700 };
        private int _currentSelectedDay = 0;
        
        public NewBieSkipView _skipView;
        public static bool bExitPopWindow = false;
        public override void OnCreate()
        {
            base.OnCreate();
            _transBG = GameObjectEx.FindChildByName(this.transform, "Trans_BG");
            _goLoading = GameObjectEx.FindChildByName(this.transform, "Loading")?.gameObject;
            _btnBack = GameObjectEx.FindChildByName(this.transform, "BackButton").GetComponent<CButton>();
            _btnBack.onClick.AddListener(CloseSelf);
            _tryOnBtn.onClick.AddListener(ClickTryOn);
            _rewardBtn.onClick.AddListener(ClickGetAllReward);
            InitUI();
            string key = "FirstOpenNewBieTask_" + AccountDataManager.Inst.Uid;
            //上报展示埋点
            if (!PlayerPrefs.HasKey(key))
            {
                LoadEvent.ReportTaskUV(new TaskUVInfo
                {
                    taskId = _taskId,
                    type = 0
                });
                PlayerPrefs.SetInt(key , 1);
                PlayerPrefs.Save();
            }
            MessageHelper.AddListener(MessageName.UpdateHallTask, RefreshTaskStatus);
            RefreshTaskStatus();
        }

        private void ClickTryOn()
        {
            bundleShowpanel.SetEventPreview(new List<string>() {
                    "11300354",
                    "11000261",
                    "10900492",
                    "10400492"}, "童心甜梦套装", "Bundle_103", XAssetLoaderMgr.Inst.GetSpriteAltasPath(SpriteAtlasType.Bundle), "新年组队消费 领新年好礼", "#BF8DFF",
            bgRawTex.texture);
            bundleShowpanel.gameObject.SetActive(true);
        }

        private void ClickGetAllReward()
        {
            EventCenterDataManager.Inst.CliamReward(this._taskId, 0, 2, 0, (claimRspData) =>
            {
                List<TaskClaimRewardData> rewardList = claimRspData.rewardList;
                var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
                List<CommonRewardItemData> items = new List<CommonRewardItemData>();

                MessageHelper.Broadcast(MessageName.UpdateHallTask);
                foreach (var reward in rewardList)
                {
                    CommonRewardItemData item;
                    MessageHelper.Broadcast(MessageName.OnPlayerInfoAccountChange, (CurrencyType)reward.rewardType);
                    if (reward.rewardType != 77)
                    {//77是活跃度
                        if (reward.rewardType != 5)
                        {
                            item = new CommonRewardItemData()
                            {
                                RewardAmount = reward.amount,
                                rewardType = reward.rewardType,
                                rewardName = UI.Manager.PgcUtils.GetRewardName((BUDRewardType)reward.rewardType)
                            };
                            items.Add(item);
                        }
                        else
                        {
                            if (reward.pgcIdList != null && reward.pgcIdList.Count > 0)
                            {
                                for (int i = 0; i < reward.pgcIdList.Count; i++)
                                {
                                    var pgcItem = new CommonRewardItemData()
                                    {
                                        RewardAmount = reward.amount,
                                        rewardType = reward.rewardType,
                                        pgcId = reward.pgcIdList[i],
                                        //      rewardName = PgcUtils.GetRewardName((BUDRewardType)reward.rewardType)
                                    };
                                    items.Add(pgcItem);
                                }

                            }
                        }
                    }
                    else
                    {
                        continue;
                    }

                    //items.Add(item);


                }
                panel.ShowRewards(items);
                panel.SetCloseAct(() => {
                    MarketReviewManager.Inst.CheckTaskV3IsShowMarketPointPanel();
                });
                AccountDataManager.Inst.BalanceInfo.Refresh();
                VipDataManager.Inst.UpdateVipStatus();
                ReddotManagerUtils.Inst.RefreshRedDot();
            });
        }

        public override void CloseSelf()
        {
            base.CloseSelf();
            var gameHallpanel = UIManager.Inst.FindPanel<GameHallPanel>(WindowId.GameHallWindow, PanelId.GameHallPanel);
            if(gameHallpanel._isTopBtnClose)
            {
                gameHallpanel.ChangBtAlpha(1, "BtnNewbieV2");
            }
            
            // 清理数据状态
            isInit = false;
            _serverEventList = null;
            _serverAccumulativeEventList = null;
            _currentSelectedDay = 0;
            
            // 清理UI列表
            newbieTaskItems.Clear();
            newBieAccumulativeTaskItems.Clear();
            newbieTaskBottomItems.Clear();

            if(bExitPopWindow)
            {
                bExitPopWindow = false;
            }
        }

        private void LoadConfigIfNeeded()
        {
            if (isInit) return;
            isInit = true;
            string jsonPath = "Assets/Loadable/UI/RechargePanel/config/NewbieTaskV2Config.json";
            var ugcAsset = Loader.Load<TextAsset>(jsonPath, this.gameObject);
            _rewardItems = JsonConvert.DeserializeObject<List<RewardItem>>(ugcAsset.text);
            var configs = Es.DataTables.GetTaskConfigList();
            _taskConfigs = configs.Where(config => config.taskId == _taskId).ToList();
            _taskaccumulativeConfigs = configs.Where(config => config.taskId == _accumulativeTaskId).ToList();
        }

        private void RefreshTaskStatus()
        {
            _goLoading?.SetActive(true);
            bool haveReward = false;
           
            IAPDataManager.Inst.GetTaskList(_taskId, (b, taskInfoResponse) =>
            {
                if (this == null || gameObject == null)
                {
                    return;
                }

                _goLoading?.SetActive(false);
                if (taskInfoResponse?.list == null || taskInfoResponse.list.Count <= 0) return;

                TaskInfoData tsTaskInfoData = taskInfoResponse.list[0];
                if (tsTaskInfoData?.eventList == null) return;

                this._serverEventList = tsTaskInfoData.eventList;
                int sum = 0;
                foreach(var eventlst in _serverEventList)
                {
                    if(eventlst.eventStatus == (int)EventStatus.Finish)
                    {
                        var targetConfig = _taskConfigs.FirstOrDefault(x => x.eventId == eventlst.eventId);
                        if (targetConfig == null) continue;
                        
                        foreach(string key in targetConfig.rewardNum)
                        {
                            var rewards = key.Split(",");
                            if (rewards[0] == "7")
                            {
                                sum += int.Parse(rewards[1]);
                                break;
                            }
                        }
                    }
                    if(eventlst.eventStatus == (int)EventStatus.Claim)
                    {
                        haveReward = true;
                    }
                  //  Debug.LogError($"event ={eventlst.eventName},event.id={eventlst.eventId},statues={eventlst.eventStatus},haveReward={haveReward}");
                }
                fireTex.text = sum.ToString();
                MarketReviewManager.Inst.CheckNewBieSevenDayV3TaskData(sum);
                slider.value = CalculateProgressValue(sum);
                LoadConfigIfNeeded();

                newbieTaskItems.Clear();
                foreach (Transform child in topTaskContent) Destroy(child.gameObject);

                var dailyRewardStatuses = _serverEventList.Where(s => s.memberId == 0).OrderBy(s => s.groupId).ToList();

                foreach (var serverStatus in dailyRewardStatuses)
                {
                    var localData = _rewardItems.FirstOrDefault(r => r.groupId == serverStatus.groupId && r.rewardId == serverStatus.memberId.ToString());
                    if (localData == null) continue;
                    int cnt = serverStatus.eventId;
                    var taskItem = Instantiate(newbieTaskItem, topTaskContent);
                    newbieTaskItems.Add(taskItem);
                    taskItem.SetData(serverStatus,
                        (cnt) => {
                            _currentSelectedDay = cnt;
                            GenerateBottomData(_currentSelectedDay);
                            HighlightSelectedDay();
                            RefreshTaskStatus();
                        },
                        cnt
                    );

                }
                if (_currentSelectedDay <= 0) {
                    _currentSelectedDay = FindFirstUncompletedDay();
                }

                GenerateBottomData(_currentSelectedDay);
                HighlightSelectedDay();

                _rewardBtn.gameObject.SetActive(haveReward);
                _grayBtn.gameObject.SetActive(!haveReward);

            });
            IAPDataManager.Inst.GetTaskList(_accumulativeTaskId, (b, taskInfoResponse) =>
            {
                if (this == null || gameObject == null)
                {
                    return;
                }

                _goLoading?.SetActive(false);
                if (taskInfoResponse?.list == null || taskInfoResponse.list.Count <= 0) return;

                TaskInfoData tsTaskInfoData = taskInfoResponse.list[0];
                if (tsTaskInfoData?.eventList == null) return;
                this._serverAccumulativeEventList = tsTaskInfoData.eventList;
                LoadConfigIfNeeded();

                newBieAccumulativeTaskItems.Clear();
                foreach (Transform child in accumulativeTaskContent) Destroy(child.gameObject);
                foreach (var serverStatus in _serverAccumulativeEventList)
                {
                    int cnt = serverStatus.eventId;
                    NewBieV3AccumulativeTaskItem taskItem;
                    if (cnt == 7)
                    {
                        taskItem = Instantiate(newBieV3AccumulativeTaskItemBig, accumulativeTaskContent);
                    }
                    else
                    {
                        taskItem = Instantiate(newBieV3AccumulativeTaskItem, accumulativeTaskContent);
                    }
                    if (serverStatus.eventStatus == (int)EventStatus.Claim)
                    {
                        haveReward = true;
                    }
                    //Debug.LogError($"event ={serverStatus.eventName},event.id={serverStatus.eventId},statues={serverStatus.eventStatus},haveReward={haveReward}");
                    newBieAccumulativeTaskItems.Add(taskItem);
                    taskItem.SetData(_accumulativeTaskId,
                        RefreshAccountTaskStatusClaim,
                        serverStatus,
                        (cnt) => {
                            _currentSelectedDay = cnt;
                            GenerateBottomData(_currentSelectedDay);
                            HighlightSelectedDay();
                            RefreshTaskStatus();
                        },
                        cnt,
                        SetSelectAccumulativeItems
                    );
                }

                _rewardBtn.gameObject.SetActive(haveReward);
                _grayBtn.gameObject.SetActive(!haveReward);

            });

       
        }
        void SetSelectAccumulativeItems(int i)
        {
            foreach (var a in newBieAccumulativeTaskItems)
            {
                if (a._curCount == i)
                {
                    a.SetSelect(true);
                }
                else
                {
                    a.SetSelect(false);
                }
            }
            
        }
        void RefreshAccountTaskStatusClaim(TaskClaimRsp taskInfoResponse) {
            if (this == null || gameObject == null)
            {
                return;
            }

            _goLoading?.SetActive(false);
            if (taskInfoResponse?.eventList == null || taskInfoResponse.eventList.Count <= 0) return;

            this._serverAccumulativeEventList = taskInfoResponse.eventList;
            LoadConfigIfNeeded();

            newBieAccumulativeTaskItems.Clear();
            foreach (Transform child in accumulativeTaskContent) Destroy(child.gameObject);
            foreach (var serverStatus in _serverAccumulativeEventList)
            {
                int cnt = serverStatus.eventId;
                NewBieV3AccumulativeTaskItem taskItem;
                if (cnt == 7)
                {
                    taskItem = Instantiate(newBieV3AccumulativeTaskItemBig, accumulativeTaskContent);
                }
                else
                {
                    taskItem = Instantiate(newBieV3AccumulativeTaskItem, accumulativeTaskContent);
                }
                newBieAccumulativeTaskItems.Add(taskItem);
                taskItem.SetData(_accumulativeTaskId,
                    RefreshAccountTaskStatusClaim,
                    serverStatus,
                    (cnt) => {
                        _currentSelectedDay = cnt;
                        GenerateBottomData(_currentSelectedDay);
                        HighlightSelectedDay();
                        RefreshTaskStatus();
                    },
                    cnt,
                    SetSelectAccumulativeItems
                );
            }
        }
        void RefreshTaskStatusClaim(TaskClaimRsp tsTaskInfoData) {
            if (tsTaskInfoData?.eventList == null) return;

            this._serverEventList = tsTaskInfoData.eventList;
            int sum = 0;
            foreach (var eventlst in _serverEventList)
            {
                if (eventlst.eventStatus == (int)EventStatus.Finish)
                {
                    var targetConfig = _taskConfigs.FirstOrDefault(x => x.eventId == eventlst.eventId);
                    if (targetConfig == null) continue;
                    
                    foreach (string key in targetConfig.rewardNum)
                    {
                        var rewards = key.Split(",");
                        if (rewards[0] == "7")
                        {
                            sum += int.Parse(rewards[1]);
                            break;
                        }
                    }
                }
            }
            fireTex.text = sum.ToString();
            MarketReviewManager.Inst.CheckNewBieSevenDayV3TaskData(sum);
            slider.value = CalculateProgressValue(sum);
            LoadConfigIfNeeded();

            newbieTaskItems.Clear();
            foreach (Transform child in topTaskContent) Destroy(child.gameObject);

            var dailyRewardStatuses = _serverEventList.Where(s => s.memberId == 0).OrderBy(s => s.groupId).ToList();

            foreach (var serverStatus in dailyRewardStatuses)
            {
                var localData = _rewardItems.FirstOrDefault(r => r.groupId == serverStatus.groupId && r.rewardId == serverStatus.memberId.ToString());
                if (localData == null) continue;
                int cnt = serverStatus.eventId;
                var taskItem = Instantiate(newbieTaskItem, topTaskContent);

                taskItem.SetData(serverStatus,
                    (cnt) => {
                        _currentSelectedDay = cnt;
                        GenerateBottomData(_currentSelectedDay);
                        HighlightSelectedDay();
                        RefreshTaskStatus();
                    },
                    cnt
                );
            }
            IAPDataManager.Inst.GetTaskList(_accumulativeTaskId, (b, taskInfoResponse) =>
            {
                if (this == null || gameObject == null)
                {
                    return;
                }

                _goLoading?.SetActive(false);
                if (taskInfoResponse?.list == null || taskInfoResponse.list.Count <= 0) return;

                TaskInfoData tsTaskInfoData = taskInfoResponse.list[0];
                if (tsTaskInfoData?.eventList == null) return;
                this._serverAccumulativeEventList = tsTaskInfoData.eventList;
                LoadConfigIfNeeded();

                newBieAccumulativeTaskItems.Clear();
                foreach (Transform child in accumulativeTaskContent) Destroy(child.gameObject);
                foreach (var serverStatus in _serverAccumulativeEventList)
                {
                    int cnt = serverStatus.eventId;
                    NewBieV3AccumulativeTaskItem taskItem;
                    if (cnt == 7)
                    {
                        taskItem = Instantiate(newBieV3AccumulativeTaskItemBig, accumulativeTaskContent);
                    }
                    else
                    {
                        taskItem = Instantiate(newBieV3AccumulativeTaskItem, accumulativeTaskContent);
                    }
                    newBieAccumulativeTaskItems.Add(taskItem);
                    taskItem.SetData(_accumulativeTaskId,
                        RefreshAccountTaskStatusClaim,
                        serverStatus,
                        (cnt) => {
                            _currentSelectedDay = cnt;
                            GenerateBottomData(_currentSelectedDay);
                            HighlightSelectedDay();
                            RefreshTaskStatus();
                        },
                        cnt,
                        SetSelectAccumulativeItems
                    );
                }
            });
        }
        
        private void GenerateBottomData(int groupId)
        {
            string key = "FirstShowTask_" +groupId + "_" + AccountDataManager.Inst.Uid;
            //上报展示埋点
            if (!PlayerPrefs.HasKey(key))
            {
                //上报展示当天任务埋点
                LoadEvent.ReportTaskUV(new TaskUVInfo
                {
                    taskId = _taskId,
                    type = 1,
                    groupId = groupId
                });
                PlayerPrefs.SetInt(key, 1);
                PlayerPrefs.Save();
            }
            
            newbieTaskBottomItems.Clear();
            
            foreach (Transform child in bottomTaskContent) Destroy(child.gameObject);
            foreach (var task in newbieTaskItems)
            {
               task.SetSelect((task._curCount == groupId));
            }
            if (_serverEventList == null) return;

            var subTaskStatuses = _serverEventList
                .Where(s => s.groupId == groupId && s.memberId != 0)
                .OrderBy(s => s.memberId)
                .ToList();
            foreach (var serverStatus in subTaskStatuses)
            {   if(serverStatus.eventStatus != (int)EventStatus.Claim)//先创建可以领取的
                {
                    continue;
                }
                var localData = _taskConfigs.FirstOrDefault(r => r.eventId == serverStatus.eventId);
                if (localData == null)
                {
                    continue;
                }

                var taskItem = Instantiate(newbieTaskBottomItem, bottomTaskContent);
                taskItem.OnInitCreate(localData);
                taskItem.SetData(serverStatus, item =>
                { 
                } , RefreshTaskStatusClaim, _taskId , newbieTaskItems[groupId - 1]._taskItemData.eventStatus == (int)EventStatus.UnClaim);
                newbieTaskBottomItems.Add(taskItem);
            }
            foreach (var serverStatus in subTaskStatuses)
            {
                if (serverStatus.eventStatus != (int)EventStatus.UnClaim)//再创建完成中的
                {
                    continue;
                }
                var localData = _taskConfigs.FirstOrDefault(r => r.eventId == serverStatus.eventId);
                if (localData == null)
                {
                    continue;
                }

                var taskItem = Instantiate(newbieTaskBottomItem, bottomTaskContent);
                taskItem.OnInitCreate(localData);
                taskItem.SetData(serverStatus, item =>
                {
                    if (item.progress == null || item.progress.Count == 0)
                    {
                        Debug.LogWarning("任务进度列表为空");
                        return;
                    }
                    
                    if (item.progress.Count == 1)
                    {
                        NewbieTaskSkipManager.Inst.HandleSkip(item.progress[0]);
                    }
                    else
                    {
                        _skipView.SetData(item.progress , taskItem.transform);
                        _skipView.gameObject.SetActive(true);
                    }
                    //上报展示当天任务点击埋点
                    string key = "FirstClickTask_" + serverStatus.eventId + "_" + AccountDataManager.Inst.Uid;
                    if (!PlayerPrefs.HasKey(key))
                    {
                        LoadEvent.ReportTaskUV(new TaskUVInfo
                        {
                            taskId = _taskId,
                            type = 1,
                            groupId = groupId,
                            memberIds = new List<int> { serverStatus.memberId }
                        }
                        );
                        PlayerPrefs.SetInt(key, 1);
                        PlayerPrefs.Save();
                    }
                }, RefreshTaskStatusClaim,
                _taskId,
                newbieTaskItems[groupId-1]._taskItemData.eventStatus == (int)EventStatus.UnClaim);
                newbieTaskBottomItems.Add(taskItem);
            }
            foreach (var serverStatus in subTaskStatuses)
            {
                if (serverStatus.eventStatus != (int)EventStatus.Default)//创建无法完成的
                {
                    continue;
                }
                var localData = _taskConfigs.FirstOrDefault(r => r.eventId == serverStatus.eventId);
                if (localData == null)
                {
                    continue;
                }

                var taskItem = Instantiate(newbieTaskBottomItem, bottomTaskContent);
                taskItem.OnInitCreate(localData);
                taskItem.SetData(serverStatus, item =>
                {
                }, RefreshTaskStatusClaim, _taskId , newbieTaskItems[groupId - 1]._taskItemData.eventStatus == (int)EventStatus.UnClaim);
                newbieTaskBottomItems.Add(taskItem);
            }
            foreach (var serverStatus in subTaskStatuses)
            {   
                if(serverStatus.eventStatus != (int)EventStatus.Finish)//把完成的任务创建在列表最下方
                {
                    continue;
                }
                var localData = _taskConfigs.FirstOrDefault(r => r.eventId == serverStatus.eventId);
                if (localData == null)
                {
                    continue;
                }

                var taskItem = Instantiate(newbieTaskBottomItem, bottomTaskContent);
                taskItem.OnInitCreate(localData);
                taskItem.SetData(serverStatus, item =>
                {
                }, RefreshTaskStatusClaim, _taskId , newbieTaskItems[groupId - 1]._taskItemData.eventStatus == (int)EventStatus.UnClaim);
                newbieTaskBottomItems.Add(taskItem);
            }
        }

        private int FindFirstUncompletedDay()
        {
            int currId = -1;
            if (_serverEventList == null) return 1;

            // 获取前7天的任务状态（eventId 1-7）
            var dailyStatuses = _serverEventList
                .Where(s => s.eventId >= 1 && s.eventId <= 7)
                .OrderBy(s => s.eventId)
                .ToList();

            // 找到第一个状态是0的天数
            foreach (var status in dailyStatuses)
            {
                if (status.eventStatus == 0)
                {
                    currId = status.eventId;
                }
            }
            if (currId == -1)
            {
                // 找到最后一个状态不是1的天数
                foreach (var status in dailyStatuses)
                {
                    if (status.eventStatus != 1)
                    {
                        currId = status.eventId;
                    }
                }
            }
            // 如果所有天数都完成了，返回最后一天
            return currId != -1 ? currId : 1;
        }

        private void HighlightSelectedDay()
        {
            foreach (var item in newbieTaskItems)
            {
                //item.SetSelected(item.GetGroupId() == _currentSelectedDay);
            }
        }

        private void InitUI()
        {
            if (_transBG == null) return;
            string atlasPath = "Assets/Loadable/UI/UIPanel/CommonBgPanel/CommonBgIcon.spriteatlas";
            var itemObj = Loader.Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab").Instantiate(_transBG);
            var item = itemObj.GetComponent<ActivityCenterBgItem>();
            item.InitCustomBgItem("FFFFFF", atlasPath, new List<string> { "color_bg_icon_newbie_01", "color_bg_icon_newbie_02", "color_bg_icon_newbie_03" });
            item.gameObject.SetActive(true);
        }

        public override void OnWindowBeFocused()
        {
            RefreshTaskStatus();
            EventCenterDataManager.Inst.GetTaskInfo();
        }
        public override void OnShow(params object[] args)
        {
            base.OnShow(args);
            // 每次显示时重新加载数据
            isInit = false;
            LoadConfigIfNeeded();
            RefreshTaskStatus();
        }
        private void OnDestroy()
        {
            MessageHelper.RemoveListener(MessageName.UpdateHallTask, RefreshTaskStatus);
            
            // 清理数据状态
            isInit = false;
            _serverEventList = null;
            _serverAccumulativeEventList = null;
            _currentSelectedDay = 0;
            
            // 清理UI列表
            newbieTaskItems.Clear();
            newBieAccumulativeTaskItems.Clear();
            newbieTaskBottomItems.Clear();
        }

        // 根据当前累积值计算进度条位置
        private float CalculateProgressValue(int currentSum)
        {
            if (currentSum <= 0) return 0f;
            if (currentSum >= 700) return 1f;

            // 找到当前累积值所在的区间
            for (int i = 1; i < rewardValues.Count; i++)
            {
                if (currentSum <= rewardValues[i])
                {
                    // 计算在当前区间内的相对进度
                    float segmentProgress = (currentSum - rewardValues[i - 1]) / (float)(rewardValues[i] - rewardValues[i - 1]);
                    // 计算总体进度
                    return ((i - 1) + segmentProgress) / (rewardValues.Count - 1);
                }
            }
            return 1f;
        }
    }
}