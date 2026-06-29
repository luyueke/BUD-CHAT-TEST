using Message;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameData.Task
{
    public interface ITaskCondition
    {
        public void Init(TaskData data);

        public EConditionType GetConditionType();

        public void UpdateTaskState(TaskInteractInfo info);
    }


    public class TaskBaseCondition : ITaskCondition
    {
        protected TaskData _selfData;
        public virtual void Init(TaskData data)
        {
            _selfData = data;
        }

        public  EConditionType GetConditionType() 
        {
            return (EConditionType)_selfData.nConditionType;
        }

        public virtual void UpdateTaskState(TaskInteractInfo info)
        {
            LoggerUtils.Log($"更新任务 {_selfData.strTaskName} 完成状态:{_selfData.strTaskDesc}");
            //throw new System.NotImplementedException();
            //todo 发送事件更新UI表现，如果是进度更新则更新进度，如果是任务完成则需要下一个任务
            MessageHelper.Broadcast(MessageName.OnTaskConditionComplete, _selfData.nTaskID);
        }
    }

}

