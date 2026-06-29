using Basic.UndoRedo;
using Game.Base;
using Game.UGCEditor;
using Game.Utils;
using UndoSystem;
using UnityEngine;
public class UGCClothesCreateDestroyUndoData : UGCBasicUndoData
{
    public GameObject targetNode;
    public int createUndoMode;
    public int type;
}
public class UGCClothesCreateDestroyUndoHelper : BaseUndoHelper
{
    public override void Undo(UndoRecord record)
    {
        UGCClothesCreateDestroyUndoData beginData = record.BeginData as UGCClothesCreateDestroyUndoData;
        UGCClothesCreateDestroyUndoData endData = record.EndData as UGCClothesCreateDestroyUndoData;
        //删除操作
        if (IsDestroyMode(beginData))
        {
            ExcuteCreate(beginData);
        }
        else if (IsCreateMode(endData))//创建或复制操作
        {
            ExcuteDestroy(endData);
        }
    }

    public override void Redo(UndoRecord record)
    {
        UGCClothesCreateDestroyUndoData beginData = record.BeginData as UGCClothesCreateDestroyUndoData;
        UGCClothesCreateDestroyUndoData endData = record.EndData as UGCClothesCreateDestroyUndoData;
        //删除操作
        if (IsDestroyMode(beginData))
        {
            ExcuteDestroy(beginData);
        }
        else if (IsCreateMode(endData))
        {
            ExcuteCreate(endData);
        }
    }

    //在redo中的创建操作，在undo的删除操作 被清除时需要真正销毁节点
    public override void OnRemoveFromPool(UndoRecord record, int fromType)
    {
        base.OnRemoveFromPool(record, fromType);
        LoggerUtils.Log("CreateDestroyUndoHelper OnRemoveFromPool fromType:" + fromType);
        UGCClothesCreateDestroyUndoData beginData = record.BeginData as UGCClothesCreateDestroyUndoData;
        UGCClothesCreateDestroyUndoData endData = record.EndData as UGCClothesCreateDestroyUndoData;
        //删除操作
        if (IsDestroyMode(beginData))
        {
            if (fromType == (int)UndoRedoType.Undo && GamePropNodeManager.Inst.IsContainsSecondCache(beginData.targetNode))
            {
                LoggerUtils.Log("undo真正销毁节点:" + beginData.targetNode.transform.name);
                GamePropNodeManager.Inst.RemoveItemFromSecondCache(beginData.targetNode);
                GamePropNodeManager.Inst.DestroyNode(beginData.targetNode);
            }
        }
        else if (IsCreateMode(endData))
        {
            if (fromType == (int)UndoRedoType.Redo && GamePropNodeManager.Inst.IsContainsSecondCache(endData.targetNode))
            {
                GamePropNodeManager.Inst.RemoveItemFromSecondCache(endData.targetNode);
                LoggerUtils.Log("redo真正销毁节点:" + endData.targetNode.transform.name);
                GamePropNodeManager.Inst.DestroyNode(endData.targetNode);
            }
        }
    }

    private void ExcuteCreate(UGCClothesCreateDestroyUndoData helpData)
    {
        helpData.DoSwitchPart();
        GamePropNodeManager.Inst.RevertNodeFromSecondCache(helpData.targetNode);
        GameObject gameObject = helpData.targetNode;
        if (gameObject)
        {
            GamePropNodeManager.Inst.RevertNodeFromSecondCache(gameObject);
            if(TransformInteractorController.Inst.InterActor)
            {
                var behav = gameObject.GetComponent<ElementBaseBehaviour>();
                if (behav)
                {
                    behav.RedoInfo(helpData.selectPartIndex);
                    TransformInteractorController.Inst.InterActor.Show(behav.rectTrans, behav.OnTransformChange, behav.Init);
                }
            }
            ExcuteMirObj(gameObject, true);
        }
        else
        {
            LoggerUtils.Log("该节点已被回收，操作无效");
        }
    }

    private void ExcuteDestroy(UGCClothesCreateDestroyUndoData helpData)
    {
        helpData.DoSwitchPart();
        GameObject go = helpData.targetNode;
        ExcuteMirObj(go, false);
        GamePropNodeManager.Inst.DestroyNodeToSecondCache(go);
        var behav = go.GetComponent<ElementBaseBehaviour>();
        if (behav)
        {
            behav.UndoInfo(helpData.selectPartIndex);
            TransformInteractorController.Inst.InterActor.ResetInfo();
            UGCImportTextManager.Inst.CurrentSelectBehaviour = null;
            UGCImportPhotoManager.Inst.CurrentSelectBehaviour = null;
        }
    }

    private void ExcuteMirObj(GameObject obj,bool isRevert)
    {
        var tbehav = obj.GetComponent<ElementBaseBehaviour>();
        GameObject mirObj = tbehav.ExcuteMirObj();
        if (mirObj)
        {
            if (isRevert)
            {
                GamePropNodeManager.Inst.RevertNodeFromSecondCache(mirObj);
                tbehav.OnTransformChange();
            }
            else
            {
                GamePropNodeManager.Inst.DestroyNodeToSecondCache(mirObj);
            }
        }
    }

    private bool IsCreateMode(UGCClothesCreateDestroyUndoData helpData)
    {
        return (helpData.createUndoMode == (int)UndoRedoConfig.CreateUndoMode.Create && helpData.targetNode != null);
    }

    private bool IsDestroyMode(UGCClothesCreateDestroyUndoData helpData)
    {
        return (helpData.createUndoMode == (int)UndoRedoConfig.CreateUndoMode.Destroy && helpData.targetNode != null);
    }
}
