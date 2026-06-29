
using System;
using System.Collections.Generic;
using Game.Event;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.CreaterRewardPanel
{
    public class TaskBoardView : MonoBehaviour
    {
        [SerializeField] internal CButton backButton;
        [SerializeField] internal TaskBoardProgress taskBoardProgress;
        [SerializeField] internal Text taskBoardFanNum;
        [SerializeField] internal Transform taskContent;
        [SerializeField] internal List<TaskBoardItem> taskBoardItems;
        private TaskInfoData taskInfoData;


        private Action<bool> _backAction;
        private Action<bool> _redDotAction;
        private string taskId = "CreatorTitle";

        private void Awake()
        {
        }
        
        public void OnInitCreated(Action<bool> backAction, Action<bool> redDotAction)
        {
            this._backAction = backAction;
            this._redDotAction = redDotAction;
            backButton.onClick.AddListener(() =>
            {
                _backAction?.Invoke(true);
            });
            
            GetTaskList();
        }
        
        public void GetTaskList()
        {
            taskContent.gameObject.SetActive(false);
            GetTaskListReq getTaskListReq = new GetTaskListReq()
            {
                idList = new List<string>{ taskId}
            };
        
            var reqParam = JsonConvert.SerializeObject(getTaskListReq);
        
        
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.TaskList, HttpMethod.POST, reqParam, content =>
            {
                var taskInfoResponse = JsonConvert.DeserializeObject<TaskListRsp>(content);
                if (taskInfoResponse == null)
                {
                    return;
                }

                taskContent.gameObject.SetActive(true);
                List<TaskInfoData> taskInfoDatas = taskInfoResponse.list;
                if (taskInfoDatas == null || taskInfoDatas.Count <= 0)
                {
                    return;
                }

                TaskInfoData tsTaskInfoData = taskInfoDatas[0];
                if (tsTaskInfoData == null)
                {
                    return;
                }

                this.taskInfoData = tsTaskInfoData;
                
                List<TaskItemData> eventList = tsTaskInfoData.eventList;
                for (int i = 0; i < taskBoardItems.Count; i++)
                {
                    taskBoardItems[i].SetData(taskId, eventList[i], taskItemData =>
                    {
                        GetTaskList();
                    });
                }

                if (eventList != null && eventList.Count > 0)
                {
                    int fansNum = eventList[0].finishAmount;
                    taskBoardProgress.InitProgress(fansNum);

                    taskBoardFanNum.text = fansNum.ToString();
                }
                var taskInfoData = taskInfoDatas.Find(x => x.taskId == taskId);
                if (taskInfoData != null)
                {
                    var unClaimData = taskInfoData.eventList.Find(x => x.eventStatus == (int)TaskClaimState.Enable);
                    _redDotAction?.Invoke(unClaimData != null);
                }
            } , failMessage =>
            {
            });
        }
        
    }
    
}