using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameData.Task
{
    /// <summary>
    /// 任务条件工厂类
    /// </summary>
    public static class TaskConditionFactory
    {
        /// <summary>
        /// 创建任务条件实例
        /// </summary>
        /// <param name="type">条件类型</param>
        /// <returns>任务条件实例</returns>
        public static ITaskCondition CreateCondition(EConditionType type)
        {
            return type switch
            {
                EConditionType.EInteractProp => new InteractProp(),
                EConditionType.EInteractPropWithNpc => new InteractPropWithNpc(),
                _ => throw new System.ArgumentException($"未知的任务条件类型: {type}")
            };
        }

        /// <summary>
        /// 根据任务数据创建条件实例
        /// </summary>
        /// <param name="taskData">任务数据</param>
        /// <returns>任务条件实例</returns>
        public static ITaskCondition CreateCondition(TaskData taskData)
        {
            if (taskData == null)
            {
                throw new System.ArgumentNullException(nameof(taskData));
            }

            var type = (EConditionType)taskData.nConditionType;
            var condition = CreateCondition(type);
            condition.Init(taskData);
            return condition;
        }
    }
}
