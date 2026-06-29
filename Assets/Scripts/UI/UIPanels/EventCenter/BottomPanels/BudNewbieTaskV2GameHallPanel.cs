using System;
using System.Collections.Generic;
using System.Linq;
using Es;
using Message;
using Newtonsoft.Json;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Event
{
    public class BudNewbieTaskV2GameHallPanel : MonoBehaviour
    {
        public Transform bottomTaskContent;
        public NewbieV2TaskGameHallBottomItem newbieTaskBottomItem;

        public string _taskId;
        private bool isInit = false;
        public CButton CloseBt;
        public CButton SkipBt;
        private List<TaskConfig> _taskConfigs = new List<TaskConfig>();
        private List<TaskItemData> _serverEventList;
        public NewBieSkipView _skipView;
        private List<NewbieV2TaskGameHallBottomItem> newbieTaskBottomItems = new List<NewbieV2TaskGameHallBottomItem>();
        private List<int> targetAmonut = new List<int> { 0, 220, 180, 160, 140,120,100,100 };
        private int _currentSelectedDay = 1;
        
        public void Start()
        {
            RefreshTaskStatus();
            CloseBt.onClick.AddListener(OnCloseBtClick);
            SkipBt.onClick.AddListener(() =>
            {   
                if(_taskId == "NewbieSevenDayTaskV2")
                {
                    UIManager.Inst.OpenPanel(PanelId.BudNewbieTaskV2Panel);
                }
                else if(_taskId == "NewbieSevenDayTaskV3")
                {
                    UIManager.Inst.OpenPanel(PanelId.NewBieSevenDayV3TaskPanel);
                }

            });
            MessageHelper.AddListener(MessageName.UpdateHallTask, RefreshTaskStatus);
        }
        void OnCloseBtClick()
        {
            bottomTaskContent.gameObject.SetActive(!bottomTaskContent.gameObject.active);
            
        }
        private void OnEnable()
        {
            RefreshTaskStatus();
        }
        private void LoadConfigIfNeeded()
        {
            if (isInit) return;
            isInit = true;
            var configs = Es.DataTables.GetTaskConfigList();
            _taskConfigs = configs.Where(config => config.taskId == _taskId).ToList();
        }

        public void RefreshTaskStatus()
        {
            
            IAPDataManager.Inst.GetTaskList(_taskId, (b, taskInfoResponse) =>
            {
                if (this == null || gameObject == null)
                {
                    return;
                }

                if (taskInfoResponse?.list == null || taskInfoResponse.list.Count <= 0) {
                    return;
                }
                
                TaskInfoData tsTaskInfoData = taskInfoResponse.list[0];
                if (tsTaskInfoData?.eventList == null) return;
                this._serverEventList = tsTaskInfoData.eventList;
                LoadConfigIfNeeded();
                if (_currentSelectedDay <= 1) {
                    _currentSelectedDay = FindFirstUncompletedDay();
                }
                GenerateBottomData(_currentSelectedDay);
            });
        }

        private void GenerateBottomData(int groupId)
        {
            newbieTaskBottomItems.Clear();
            foreach (Transform child in bottomTaskContent) Destroy(child.gameObject);

            if (_serverEventList == null) return;

            int cnt = 0;
            var subTaskStatuses = _serverEventList
                .Where(s => s.groupId == groupId && s.memberId != 0)
                .OrderBy(s => s.memberId)
                .ToList();
            foreach (var serverStatus in subTaskStatuses)
            {
                if (cnt >= 3) return;
                var localData = _taskConfigs.FirstOrDefault(r => r.eventId == serverStatus.eventId); 
                if (localData == null )
                {
                    continue;
                }
                if (serverStatus.eventStatus != (int)EventStatus.Claim) {
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
                        _skipView.SetData(item.progress, taskItem.transform);
                        _skipView.gameObject.SetActive(true);
                    }
                },
                () =>
                {
                    TimerManager.Inst.RunOnce("title", 0.4f, () => {
                        RefreshTaskStatus();
                    });

                }, _taskId
                );
                newbieTaskBottomItems.Add(taskItem);
                cnt++;
            }
            foreach (var serverStatus in subTaskStatuses)
            {
                if (cnt >= 3) return;
                var localData = _taskConfigs.FirstOrDefault(r => r.eventId == serverStatus.eventId);
                if (localData == null )
                {
                    continue;
                }
                if( serverStatus.eventStatus != (int)EventStatus.UnClaim)
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
                        _skipView.SetData(item.progress, taskItem.transform , new Vec3(3,0,0));
                        _skipView.gameObject.SetActive(true);
                    }
                },
                () =>
                {
                    TimerManager.Inst.RunOnce("title", 0.4f, () => {
                        RefreshTaskStatus();
                    });

                }, _taskId
                );
                newbieTaskBottomItems.Add(taskItem);
                cnt++;
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
            return currId!=-1 ? currId : 1;
        }

        private void OnDestroy()
        {
            MessageHelper.RemoveListener(MessageName.UpdateHallTask, RefreshTaskStatus);
        }
    }
}