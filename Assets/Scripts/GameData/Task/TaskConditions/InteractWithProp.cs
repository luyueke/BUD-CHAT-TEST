using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameData.Task
{
    /// <summary>
    /// 和道具交互
    /// </summary>
    public class InteractProp :TaskBaseCondition
    {
        public override void UpdateTaskState(TaskInteractInfo info)
        {
            if (info == null) return;
            base.UpdateTaskState(info);
            if (_selfData.nTaskTargetPropID == info.propID)
            {
                TaskManager.Inst.RemoveNode(this);
            }
        }
    }

    /// <summary>
    /// npc好感度
    /// </summary>
    public class NpcFavorability : TaskBaseCondition
    {
        public override void UpdateTaskState(TaskInteractInfo info)
        {
            if (info == null) return;
            base.UpdateTaskState(info);
            _selfData.nTaskProgress = info.favorability;
            if (_selfData.nTargetValue <= info.favorability)
            {
                TaskManager.Inst.RemoveNode(this);
            }
        }
    }

    /// <summary>
    /// 双人一起交互道具
    /// </summary>
    public class InteractPropWithNpc : TaskBaseCondition
    {
        public override void UpdateTaskState(TaskInteractInfo info)
        {
            if (info == null) return;
            base.UpdateTaskState(info);
            bool isTargetNpc = string.IsNullOrEmpty(_selfData.strTaskTargetNpcID)||_selfData.strTaskTargetNpcID == info.npcID ;
            if (_selfData.nTaskTargetPropID == info.propID && isTargetNpc)
            {
                TaskManager.Inst.RemoveNode(this);
            }
        }
    }
}