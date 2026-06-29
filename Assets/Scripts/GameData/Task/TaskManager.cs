using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameData.Task
{
    public enum ETaskTag
    {
        S9 = 0,
        S11 = 1,
    }

    public enum EConditionType
    {
        /// <summary>
        /// 单人游玩设施
        /// </summary>
        EInteractProp,
        /// <summary>
        /// 双人游玩设施
        /// </summary>
        EInteractPropWithNpc,
    }

    public class TaskData
    {
        public int nTaskID;
        public string strTaskName;
        public string strTaskDesc;
        public int nTaskType;
        public int nConditionType;
        public int nTaskStatus;
        /// <summary>
        /// 是否限制必须和这个npc交互才算完成,这里可能需要多个npc或者多个道具
        /// </summary>
        public string strTaskTargetNpcID;
        /// <summary>
        /// 任务交互道具
        /// </summary>
        public int nTaskTargetPropID;
        /// <summary>
        /// 任务参数当前进度
        /// </summary>
        public int nTaskProgress;
        /// <summary>
        /// 任务参数 目标数值
        /// </summary>
        public int nTargetValue;
        /// <summary>
        /// 可以用来任务排序
        /// </summary>
        public int nTaskIndex;
    }
    public class TaskManager : GlobalInstance<TaskManager>
    {
        /// <summary>
        /// 当前存储的任务归属于那个玩法的
        /// </summary>
        public ETaskTag eTaskTag;
        /// <summary>
        /// 任务是否为线性,如果是线性则要按照顺序一个个完成
        /// </summary>
        public bool bLinkTask;
        // Start is called before the first frame update    

        /// <summary>
        /// 任务检测器
        /// </summary>
        private TaskChecker checker;

        void Init()
        {
            if (checker == null)
            {
                checker = new TaskChecker();
            }
        }

        public void AddNode(TaskData data)
        {
            //todo 这里需要按照data的nConditionType来选择new 出来的对象，考虑使用一个对象池来管理node
            checker.AddCondition(data,TaskConditionFactory.CreateCondition(data));
        }

        public void RemoveNode(ITaskCondition condition)
        {
            checker.RemoveCondition(condition);
        }

        /// <summary>
        /// 从本地拉取任务数据
        /// </summary>
        public void LoadTaskFromLocal()
        {
            
        }

        /// <summary>
        /// 从服务器拉取任务数据
        /// </summary>
        public void LoadTaskFromServer()
        {
            
        }

        public override void Release()
        {
            base.Release();
            checker.RemoveListener();
        }
    }
}