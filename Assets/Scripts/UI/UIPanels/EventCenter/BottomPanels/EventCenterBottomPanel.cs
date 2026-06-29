using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Game.Event
{
    public class EventCenterBottomPanel : MonoBehaviour
    {
        public Transform ItemContent;
        protected TASK_ID _taskId;
        protected const string atlasPath = "Assets/Loadable/UI/UIPanel/EventCenter/EventCenter.spriteatlas";
        protected TaskInfoData _taskInfoData;
        protected List<BaseEventItem> _baseEventItems = new List<BaseEventItem>();


        private void OnDestroy()
        {
            EventCenterDataManager.Inst.RemoveTaskDataCallBack(this._taskId.ToString());
        }

        public void InitData(TASK_ID taskId)
        {
            EventCenterDataManager.Inst.AddTaskDataCallBack(taskId.ToString(), OnGetDataSuccess, OnGetDataFail);   
            this._taskId = taskId;
            BindData();
            BindUI();
            //创建Item
            InitItems(this._taskInfoData);
            //刷新数据
            RefreshItems(this._taskInfoData);

            EventCenterDataManager.Inst.GetTaskInfo(taskId);
        }

        private void BindData()
        {
            string path = "Assets/Loadable/UI/UIPanel/EventCenter/Configs/DailyTaskConfig.json";
            switch (this._taskId)
            {
                case TASK_ID.SevenDayLogin:
                    path = "Assets/Loadable/UI/UIPanel/EventCenter/Configs/SevenDayTaskConfig.json";
                    break;
                case TASK_ID.Daily:
                    path = "Assets/Loadable/UI/UIPanel/EventCenter/Configs/DailyTaskConfig.json";
                    break;
                case TASK_ID.Weekly:
                    path = "Assets/Loadable/UI/UIPanel/EventCenter/Configs/WeeklyTaskConfig.json";
                    break;
                case TASK_ID.NewbieSevenDayTask:
                    path = "Assets/Loadable/UI/UIPanel/EventCenter/Configs/BudNewbieConfig.json";
                    break;
            }

            var textAsset = Loader.Load<TextAsset>(path, this.gameObject);
            this._taskInfoData = JsonConvert.DeserializeObject<TaskInfoData>(textAsset.text);
        }
        
        //UI数据绑定
        public virtual void BindUI()
        {
            
        }

        //初始化Items
        public virtual void InitItems(TaskInfoData infoData)
        {
            
        }
        
        //更新数据
        public virtual void RefreshItems(TaskInfoData syncTaskData)
        {
            if (syncTaskData == null || syncTaskData.taskStatus == 0)
            {
                LoggerUtils.LogError("syncTaskData任务数据 有误");
                return;
            }
            //刷新数据
            this._taskInfoData.eventList.ForEach(curData =>
            {
                var curTaskItemData = syncTaskData.eventList.Find(syncData => syncData.eventId == curData.eventId);
                if (curTaskItemData != null)
                {
                    var eventStatus = curTaskItemData.eventStatus;
                    var finishAmount = curTaskItemData.finishAmount;
                    curData.RefreshEventStatus(eventStatus);
                    curData.RefreshFinishAmount(finishAmount);
                }
            });
        }
        
        public virtual void ShowPanel()
        {
            gameObject.SetActive(true);
        }
        
        public virtual void HidePanel()
        {
            gameObject.SetActive(false);
        }
        
        public void OnGetDataSuccess(TaskListRsp taskListRsp)
        {
            var syncData = taskListRsp.list.Find(x => x.taskId == this._taskId.ToString());
            RefreshItems(syncData);
        }

        public void OnGetDataFail(string error = "")
        {
            
        }
    }
}
