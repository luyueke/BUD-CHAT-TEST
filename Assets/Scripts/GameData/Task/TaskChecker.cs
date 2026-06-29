using GameData.Task;
using Message;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameData.Task
{
    public class TaskInteractInfo
    {
        public string npcID;
        public int propID;
        public int favorability;
    }
    public class TaskChecker
    {
        public TaskChecker()
        {
            AddListener();   
        }

        public void AddListener()
        {
            RemoveListener();
            MessageHelper.AddListener<EConditionType,TaskInteractInfo>(MessageName.OnPropInteract, OnUpdate);
            MessageHelper.AddListener<EConditionType, TaskInteractInfo>(MessageName.OnPropInteractWithNpc, OnUpdate);
        }

        public void RemoveListener()
        {
            MessageHelper.RemoveListener<EConditionType, TaskInteractInfo>(MessageName.OnPropInteract, OnUpdate);
            MessageHelper.RemoveListener<EConditionType, TaskInteractInfo>(MessageName.OnPropInteractWithNpc, OnUpdate);
        }

        // 按条件类型分类存储
        private Dictionary<EConditionType, List<ITaskCondition>> _conditionGroups = new();

        // 条件到任务的映射
        private Dictionary<ITaskCondition, TaskData> _conditionTaskMap = new();

        // 任务到条件的映射
        private Dictionary<TaskData, ITaskCondition> _taskConditionMap = new();

        public void AddCondition(TaskData taskData, ITaskCondition condition)
        {
            var type = condition.GetConditionType();
            if (!_conditionGroups.ContainsKey(type))
            {
                _conditionGroups[type] = new List<ITaskCondition>();
            }
            _conditionGroups[type].Add(condition);
            _conditionTaskMap[condition] = taskData;
            _taskConditionMap[taskData] = condition;

            condition.Init(taskData);
        }

        public void RemoveCondition(ITaskCondition condition)
        {
            var type = condition.GetConditionType();
            if (_conditionGroups.ContainsKey(type))
            {
                _conditionGroups[type].Remove(condition);
            }
            
            if (_conditionTaskMap.TryGetValue(condition, out var taskData))
            {
                _taskConditionMap.Remove(taskData);
                _conditionTaskMap.Remove(condition);
            }
        }

        /// <summary>
        /// 获取条件对应的任务数据
        /// </summary>
        public TaskData GetTaskData(ITaskCondition condition)
        {
            return condition != null && _conditionTaskMap.TryGetValue(condition, out var taskData) 
                ? taskData 
                : null;
        }

        /// <summary>
        /// 获取任务数据对应的条件
        /// </summary>
        public ITaskCondition GetCondition(TaskData taskData)
        {
            return taskData != null && _taskConditionMap.TryGetValue(taskData, out var condition)
                ? condition
                : null;
        }

        // 按类型更新，提高性能
        public void OnUpdate(EConditionType type, TaskInteractInfo info = null)
        {
            if (_conditionGroups.TryGetValue(type, out var conditions))
            {
                foreach (var condition in conditions)
                {
                    condition.UpdateTaskState(info);
                }
            }
        }
    }
}