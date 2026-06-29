using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;

namespace Game.Event
{
    public class BudNewbieTaskPanel : BasePanel<BudNewbieTaskPanel>
    {
        public Transform topTaskContent;
        public Transform bottomTaskContent;
        public NewbieTaskItem newbieTaskItem;
        public NewbieTaskBottomItem newbieTaskBottomItem;

        private Transform _transBG;
        private CButton _btnBack;
        private GameObject _goLoading;
        private string bottomPanelPath;
        private string _taskId = "NewbieSevenDayTask";
        private bool isInit = false;
        private TaskInfoData _taskInfoData;

        private List<RewardItem> _rewardItems = new List<RewardItem>();

        private List<NewbieTaskItem> newbieTaskItems = new List<NewbieTaskItem>();
        private List<NewbieTaskBottomItem> newbieTaskBottomItems = new List<NewbieTaskBottomItem>();
        
        private List<TaskItemData> eventList;

        public override void OnCreate()
        {
            base.OnCreate();
            _transBG = GameObjectEx.FindChildByName(this.transform, "Trans_BG");
            _btnBack = GameObjectEx.FindChildByName(this.transform, "BackButton").GetComponent<CButton>();
            _btnBack.onClick.AddListener(CloseSelf);
            InitUI();

            GenerateContent();

            RefreshTaskStatus();
        }


        private void GenerateContent()
        {
            if (isInit)
            {
                return;
            }

            isInit = true;
            string jsonPath = "Assets/Loadable/UI/RechargePanel/config/NewbieTaskConfig.json";
            var ugcAsset =
                Loader.Load<TextAsset>(
                    jsonPath, this.gameObject);
            _rewardItems = JsonConvert.DeserializeObject<List<RewardItem>>(ugcAsset.text);

            // 只取前 7 个任务
            var limitedRewardItems = _rewardItems.Take(7).ToList();
            newbieTaskItems.Clear();
            foreach (Transform child in topTaskContent)
            {
                Destroy(child.gameObject);
            }

            for (var i = 0; i < limitedRewardItems.Count; i++)
            {
                var taskItem = Instantiate(newbieTaskItem, topTaskContent);
                taskItem.OnInitCreate(limitedRewardItems[i]);
                newbieTaskItems.Add(taskItem);
            }
            
            
        }

        private void RefreshTaskStatus()
        {
            IAPDataManager.Inst.GetTaskList(_taskId, (b, taskInfoResponse) =>
            {
                if (taskInfoResponse == null)
                {
                    return;
                }

                List<TaskInfoData> taskInfoDatas = taskInfoResponse.list;
                if (taskInfoDatas == null || taskInfoDatas.Count <= 0)
                {
                    return;
                }

                topTaskContent.gameObject.SetActive(true);

                TaskInfoData tsTaskInfoData = taskInfoDatas[0];
                if (tsTaskInfoData == null)
                {
                    return;
                }

                this._taskInfoData = tsTaskInfoData;
                GenerateContent();

                List<TaskItemData> eventList = tsTaskInfoData.eventList;
                this.eventList = eventList;
                for (int i = 0; i < newbieTaskItems.Count; i++)
                {
                    newbieTaskItems[i].SetData(tsTaskInfoData.taskId, eventList[i],
                        taskItemData => { RefreshTaskStatus(); },
                        rewardItem => { GenerateBottomData(rewardItem.rewardId); });
                    if(eventList[i].eventStatus == (int)EventStatus.Default)
                    {
                        GenerateBottomData(eventList[i].eventId.ToString());
                    }
                }
            });
        }
        private void GenerateBottomData(string rewardId)
        {
            // 检查newbieTaskBottomItem是否为空
            if (newbieTaskBottomItem == null)
            {
                Debug.LogError("BudNewbieTaskPanel: newbieTaskBottomItem is null! Please assign it in the inspector.");
                return;
            }

            // 检查bottomTaskContent是否为空
            if (bottomTaskContent == null)
            {
                Debug.LogError("BudNewbieTaskPanel: bottomTaskContent is null! Please assign it in the inspector.");
                return;
            }

            // 获取从第8个开始的数据，每次取3个
            var startIndex = 7 + (Convert.ToInt32(rewardId) - 1) * 3;
            var segmentItems = _rewardItems.Skip(startIndex).Take(3).ToList();

            var segmentTaskItems = this.eventList.Skip(startIndex).Take(3).ToList();

            // 清空底部任务项
            newbieTaskBottomItems.Clear();
            foreach (Transform child in bottomTaskContent)
            {
                Destroy(child.gameObject);
            }

            // 创建任务项
            for (var i = 0; i < segmentItems.Count; i++)
            {
                var taskItem = Instantiate(newbieTaskBottomItem, bottomTaskContent);
                taskItem.OnInitCreate(segmentItems[i]);
                taskItem.SetData(segmentTaskItems[i], item =>
                {
                    NewbieTaskSkipManager.Inst.HandleSkip(item.rewardId,true);
                    CloseSelf();
                });
                newbieTaskBottomItems.Add(taskItem);
            }
        }


        private void InitUI()
        {
            if (_transBG == null)
            {
                return;
            }

            string atlasPath = "Assets/Loadable/UI/UIPanel/CommonBgPanel/CommonBgIcon.spriteatlas";
            var itemObj = Loader
                .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
                .Instantiate(_transBG);
            var item = itemObj.GetComponent<ActivityCenterBgItem>();
            item.InitCustomBgItem("FFFFFF", atlasPath, new List<string>()
            {
                "color_bg_icon_newbie_01", "color_bg_icon_newbie_02", "color_bg_icon_newbie_03"
            });
            item.gameObject.SetActive(true);
        }

        public override void OnWindowBeFocused()
        {
            EventCenterDataManager.Inst.GetTaskInfo();
        }
    }
}