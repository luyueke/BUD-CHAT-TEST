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
    public class BudNewbieTaskV2Panel : BasePanel<BudNewbieTaskV2Panel>
    {
        public Transform topTaskContent;
        public Transform bottomTaskContent;
        public NewbieV2TaskItem newbieTaskItem;
        public NewbieV2TaskBottomItem newbieTaskBottomItem;
        public Text finishNumTex;
        private Transform _transBG;
        private CButton _btnBack;
        private CButton _tryOnBtn;
        private GameObject _goLoading;
        private string _taskId = "NewbieSevenDayTaskV2";
        private bool isInit = false;

        private List<RewardItem> _rewardItems = new List<RewardItem>();
        private List<TaskConfig> _taskConfigs = new List<TaskConfig>();
        private List<TaskItemData> _serverEventList;

        private List<NewbieV2TaskItem> newbieTaskItems = new List<NewbieV2TaskItem>();
        private List<NewbieV2TaskBottomItem> newbieTaskBottomItems = new List<NewbieV2TaskBottomItem>();
        private List<int> targetAmonut = new List<int> { 0, 220, 180, 160, 140,120,100,100 };
        private int _currentSelectedDay = 0;

        public override void OnCreate()
        {
            base.OnCreate();
            _transBG = GameObjectEx.FindChildByName(this.transform, "Trans_BG");
            _goLoading = GameObjectEx.FindChildByName(this.transform, "Loading")?.gameObject;
            _btnBack = GameObjectEx.FindChildByName(this.transform, "BackButton").GetComponent<CButton>();
            _btnBack.onClick.AddListener(CloseSelf);
            //_tryOnBtn.onClick.AddListener(ClickTryOn);
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
            RefreshTaskStatus();
        }

        private void ClickTryOn()
        {   
            /*
            var panel = UIManager.Inst.OpenPanel<RewardPreviewPanel>(PanelId.RewardPreviewPanel);
            panel.SetEventPreview(new List<string>() {
                    "11300347",
                    "11000252",
                    "10900478",
                    "10600122",
                    "10400472"}, "花精灵套装", "Bundle_82", XAssetLoaderMgr.Inst.GetSpriteAltasPath(SpriteAtlasType.Bundle), "新年组队消费 领新年好礼", "#BF8DFF",
                bgRawTex.texture);
            */
        }

        public override void CloseSelf()
        {
            base.CloseSelf();
            var gameHallpanel = UIManager.Inst.FindPanel<GameHallPanel>(WindowId.GameHallWindow, PanelId.GameHallPanel);
            if(gameHallpanel._isTopBtnClose)
            {
                gameHallpanel.ChangBtAlpha(1, "BtnNewbieV2");
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
        }

        private void RefreshTaskStatus()
        {
            
            _goLoading?.SetActive(true);
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
                LoadConfigIfNeeded();

                newbieTaskItems.Clear();
                foreach (Transform child in topTaskContent) Destroy(child.gameObject);

                var dailyRewardStatuses = _serverEventList.Where(s => s.memberId == 0).OrderBy(s => s.groupId).ToList();

                foreach (var serverStatus in dailyRewardStatuses)
                {
                    var localData = _rewardItems.FirstOrDefault(r => r.groupId == serverStatus.groupId && r.rewardId == serverStatus.memberId.ToString());
                    if (localData == null) continue;

                    var taskItem = Instantiate(newbieTaskItem, topTaskContent);

                    taskItem.OnInitCreate(localData);

                    taskItem.SetData(tsTaskInfoData.taskId, serverStatus,
                            RefreshTaskStatusClaim
                            ,
                        (rewardItem) => {
                            _currentSelectedDay = rewardItem.groupId;
                            GenerateBottomData(_currentSelectedDay);
                            HighlightSelectedDay();
                            RefreshTaskStatus();
                        }
                    );
                    newbieTaskItems.Add(taskItem);
                }
                if (_currentSelectedDay <= 0) {
                    _currentSelectedDay = FindFirstUncompletedDay();
                }

                GenerateBottomData(_currentSelectedDay);
                HighlightSelectedDay();
            });
        }

        void RefreshTaskStatusClaim(TaskClaimRsp tsTaskInfoData) {
            
            if (tsTaskInfoData?.eventList == null) return;

            this._serverEventList = tsTaskInfoData.eventList;
            LoadConfigIfNeeded();

            newbieTaskItems.Clear();
            foreach (Transform child in topTaskContent) Destroy(child.gameObject);

            var dailyRewardStatuses = _serverEventList.Where(s => s.memberId == 0).OrderBy(s => s.groupId).ToList();

            foreach (var serverStatus in dailyRewardStatuses)
            {
                var localData = _rewardItems.FirstOrDefault(r => r.groupId == serverStatus.groupId && r.rewardId == serverStatus.memberId.ToString());
                if (localData == null) continue;

                var taskItem = Instantiate(newbieTaskItem, topTaskContent);

                taskItem.OnInitCreate(localData);

                taskItem.SetData(_taskId, serverStatus,
                        RefreshTaskStatusClaim
                        ,
                    (rewardItem) => {
                        _currentSelectedDay = rewardItem.groupId;
                        GenerateBottomData(_currentSelectedDay);
                        HighlightSelectedDay();
                        RefreshTaskStatus();
                    }
                );
                newbieTaskItems.Add(taskItem);
            }
            if (_currentSelectedDay <= 0)
            {
                _currentSelectedDay = FindFirstUncompletedDay();
            }

            GenerateBottomData(_currentSelectedDay);
            HighlightSelectedDay();

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
                task.SetSelect((task._taskItemData.eventId == groupId));
            }
            if (_serverEventList == null) return;

            var subTaskStatuses = _serverEventList
                .Where(s => s.groupId == groupId && s.memberId != 0)
                .OrderBy(s => s.memberId)
                .ToList();
            if (finishNumTex != null)
            {
                int totalTasks = targetAmonut[groupId];
                int finishedTasks = _serverEventList.Find(s =>
                    s.eventId == groupId
                ).finishAmount;

                finishNumTex.text = $"{finishedTasks}/{totalTasks}";
                LayoutRebuilder.ForceRebuildLayoutImmediate(finishNumTex.transform.parent.GetComponent<RectTransform>());
            }
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
                } , RefreshTaskStatusClaim, _taskId);
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
                    if (item.progress.Count <= 1)
                    {
                        NewbieTaskSkipManager.Inst.HandleSkip(item.progress[0]);
                    }
                    else
                    {

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
                }, RefreshTaskStatusClaim, _taskId);
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
                }, RefreshTaskStatusClaim, _taskId);
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
                }, RefreshTaskStatusClaim, _taskId);
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
    }
}