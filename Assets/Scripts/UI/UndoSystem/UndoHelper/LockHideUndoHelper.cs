using System.Collections.Generic;
using Basic.UndoRedo;
using Game.Config;
using Game.ECS;
using UI.Manager;

/// <summary>
/// Author:JayWill
/// Description:Undo/Redo实现隐藏和锁定
/// </summary>
public class LockHideUndoData
{
    public bool isLock;
    public List<SceneEntity> hideList;
    public bool activeSelf;
    public SceneEntity targetNode;
    public int LockHideType;
}
public class LockHideUndoHelper : BaseUndoHelper
{
    public override void Undo(UndoRecord record)
    {
        LockHideUndoData helpData = record.BeginData as LockHideUndoData;
        ExcuteData(helpData);
    }

    public override void Redo(UndoRecord record)
    {
        LockHideUndoData helpData = record.EndData as LockHideUndoData;
        ExcuteData(helpData);
    }

    private void ExcuteData(LockHideUndoData helpData)
    {
        switch (helpData.LockHideType)
        {
            case (int)GameGlobalEnum.LockHideType.Hide:
                LockHideManager.Inst.SetNodeVisibility(helpData.targetNode,helpData.activeSelf);
                break;
            case (int)GameGlobalEnum.LockHideType.Show:
                if (helpData.activeSelf == true)
                {
                    LockHideManager.Inst.ClearHideList();
                }
                else
                {
                    LockHideManager.Inst.SetNodesVisibility(helpData.hideList,false);
                }

                break;
            case (int)GameGlobalEnum.LockHideType.Lock:
                LockHideManager.Inst.SetLockState(helpData.targetNode,helpData.isLock);
                InputHandlerManager.Inst.SelectEntity(helpData.targetNode,true);
                if (!helpData.isLock)
                {
                    TipPanel.ShowToast("取消锁定");
                }
                else
                {
                    TipPanel.ShowToast("锁定");
                }
                break;
        }
    }
}
