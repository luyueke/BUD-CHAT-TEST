using Basic.UndoRedo;
using Game.Base;
using Game.ECS;
using Game.Utils;
using GameData.Config;
using Pb.Map;
using UI.Manager;
using UndoSystem;

/// <summary>
/// Author:JayWill
/// Description:Undo/Redo实现组合与取消组合
/// </summary>
public class CombineUndoHelper : BaseUndoHelper
{
    public override void Undo(UndoRecord record)
    {
        InputHandlerManager.Inst.UnSelectAll();
        CombineUndoData beginData = record.BeginData as CombineUndoData;
        LoggerUtils.Log("CombineUndoHelper Undo");

        if (beginData.combineUndoMode == (int)UndoRedoConfig.CombineUndoMode.Combine)//原来是组合则开始解组
        {
            LoggerUtils.Log("CombineUndoHelper undo group");
            ExcuteUnCombine(record);
            TipPanel.ShowToast("撤销组合");
        }
        else
        {
            LoggerUtils.Log("CombineUndoHelper undo ungroup");
            ExcuteCombine(record);
            TipPanel.ShowToast("撤销解除");
        }
    }

    public override void Redo(UndoRecord record)
    {
        InputHandlerManager.Inst.UnSelectAll();
        
        CombineUndoData beginData = record.BeginData as CombineUndoData;
        if (beginData.combineUndoMode == (int)UndoRedoConfig.CombineUndoMode.Combine)//原来是组合则还原组合
        {
            ExcuteCombine(record);
            TipPanel.ShowToast("重做组合");
        }
        else
        {
            ExcuteUnCombine(record);
            TipPanel.ShowToast("重做解除");
        }

    }

    private void ExcuteUnCombine(UndoRecord record)
    {
        CombineUndoData beginData = record.BeginData as CombineUndoData;
        CombineUndoData endData = record.EndData as CombineUndoData;

        CombineUndoData multiData = beginData;//组合前数据
        CombineUndoData combinedData = endData;//组合后数据

        //如果是拆解则数据相反
        if (beginData.combineUndoMode == (int)UndoRedoConfig.CombineUndoMode.UnCombine)
        {
            multiData = endData;
            combinedData = beginData;
        }

        SceneEntity combinedEntity = combinedData.combinedItem?.combineNode;
        var combineItemList = multiData.combineItemList;
        foreach (CombineUndoItem item in combineItemList)
        {
            var gComp = item.combineNode.GetComp<GameObjectComponent>();
            if ((NodeModelType)gComp.ModelType == NodeModelType.Combine)
            {
                var gameObject = GamePropNodeManager.Inst.GetNodeFromSecondCache(gComp.Uid);
                if (gameObject != null)
                {
                    GamePropNodeManager.Inst.RevertNodeFromSecondCache(gameObject);
                    var combineChildren = item.combineChildren;
                    if (combineChildren != null && combineChildren.Count > 0)
                    {
                        item.combineChildren.ForEach(child =>
                        {
                            var childGo = child.GetComp<GameObjectComponent>().BindGo;
                            childGo.transform.SetParent(gameObject.transform);
                            childGo.transform.localScale = childGo.transform.localScale.LimitVector3();
                        });
                    }
                }
                else
                {
                    //找不到原节点
                    LoggerUtils.LogError("CombineUndoHelper ExcuteUnCombine can not find the node in SecondCachePool!");
                }
            }
            else
            {
                if (combinedEntity != null)
                {
                    var combinedComp = combinedEntity.GetComp<GameObjectComponent>();
                    var combinedNode = combinedComp.BindGo;
                    var childNode = gComp.BindGo;
                    var parent = combinedNode.transform.parent;
                    if(parent == null)
                    {
                        parent = SceneBuilder.Inst.StageParent;
                    }
                    childNode.transform.SetParent(parent);
                    childNode.transform.localScale = childNode.transform.localScale.LimitVector3();
                }
            }
        }
        if (combinedEntity != null)
        {
            var combinedComp = combinedEntity.GetComp<GameObjectComponent>();
            GamePropNodeManager.Inst.DestroyNodeToSecondCache(combinedComp.BindGo);
        }
    }

    private void ExcuteCombine(UndoRecord record)
    {
        LoggerUtils.Log("CombineUndoHelper ExcuteCombine");
        CombineUndoData beginData = record.BeginData as CombineUndoData;
        CombineUndoData endData = record.EndData as CombineUndoData;

        CombineUndoData multiData = beginData;//组合前数据
        CombineUndoData combinedData = endData;//组合后数据

        //如果是拆解则数据相反
        if (beginData.combineUndoMode == (int)UndoRedoConfig.CombineUndoMode.UnCombine)
        {
            multiData = endData;
            combinedData = beginData;
        }

        SceneEntity combinedEntity = combinedData.combinedItem?.combineNode;
        if (combinedEntity == null)
        {
            //原组合SceneEntity为空
            LoggerUtils.Log("CombineUndoHelper Redo ExcuteCombine combinedEntity is null");
            return;
        }

        var combinedComp = combinedEntity.GetComp<GameObjectComponent>();
        var combinedGo = GamePropNodeManager.Inst.GetNodeFromSecondCache(combinedComp.Uid);
        if (combinedGo == null)
        {
            //原组合combinedGo为空
            LoggerUtils.Log("CombineUndoHelper Redo ExcuteCombine Comnine combinedGo is null,combinedComp.uid:" + combinedComp.Uid);
            return;
        }

        GamePropNodeManager.Inst.RevertNodeFromSecondCache(combinedGo);
        var combineItemList = multiData.combineItemList;

        foreach (CombineUndoItem item in combineItemList)
        {
            var entity = item.combineNode;
            var gComp = entity.GetComp<GameObjectComponent>();

            if ((NodeModelType)gComp.ModelType == NodeModelType.Combine)
            {
                var gameObject = gComp.BindGo;
                var combineChildren = item.combineChildren;
                if (combineChildren != null && combineChildren.Count > 0)
                {
                    item.combineChildren.ForEach(child =>
                    {
                        var childGo = child.GetComp<GameObjectComponent>().BindGo;
                        childGo.transform.SetParent(combinedGo.transform);
                        childGo.transform.localScale = childGo.transform.localScale.LimitVector3();
                    });
                }
                GamePropNodeManager.Inst.DestroyNodeToSecondCache(gComp.BindGo);
            }
            else
            {
                gComp.BindGo.transform.SetParent(combinedGo.transform);
            }
        }
    }

    //在redo中的创建操作，在undo的删除操作 被清除时需要真正销毁节点
    public override void OnRemoveFromPool(UndoRecord record, int fromType)
    {
        base.OnRemoveFromPool(record, fromType);


        CombineUndoData beginData = record.BeginData as CombineUndoData;
        CombineUndoData endData = record.EndData as CombineUndoData;

        CombineUndoData multiData = beginData;//组合前数据
        CombineUndoData combinedData = endData;//组合后数据

        LoggerUtils.Log("CombineUndoHelper OnRemoveFromPool fromType:" + fromType + "  combineUndoMode:" + beginData.combineUndoMode);

        //如果是拆解则数据相反
        if (beginData.combineUndoMode == (int)UndoRedoConfig.CombineUndoMode.UnCombine)
        {
            multiData = endData;
            combinedData = beginData;
        }

        //是undo且是组合操作
        if ((fromType == (int)UndoRedoType.Undo && beginData.combineUndoMode == (int)UndoRedoConfig.CombineUndoMode.Combine) ||
             (fromType == (int)UndoRedoType.Redo && beginData.combineUndoMode == (int)UndoRedoConfig.CombineUndoMode.UnCombine))
        {
            var combineItemList = multiData.combineItemList;
            foreach (CombineUndoItem item in combineItemList)
            {
                TryDestroyEntity(item.combineNode);
            }
        }

        if ((fromType == (int)UndoRedoType.Undo && beginData.combineUndoMode == (int)UndoRedoConfig.CombineUndoMode.UnCombine) ||
            (fromType == (int)UndoRedoType.Redo && beginData.combineUndoMode == (int)UndoRedoConfig.CombineUndoMode.Combine))
        {
            SceneEntity combinedEntity = combinedData.combinedItem?.combineNode;
            if (combinedEntity != null)
            {
                TryDestroyEntity(combinedEntity);
            }
        }
    }

    private void TryDestroyEntity(SceneEntity entity)
    {
        if (entity == null) return;
        var gComp = entity.GetComp<GameObjectComponent>();
        if (gComp == null)
        {
            LoggerUtils.LogError("TryDestroyEntity gComp IsNull");
            return;
        }
        if ((NodeModelType)gComp.ModelType == NodeModelType.Combine)
        {
            //找到要删除的组合节点
            LoggerUtils.Log("TryDestroyEntity:Destroy the CommonCombine Node");
            var gameObject = GamePropNodeManager.Inst.GetNodeFromSecondCache(gComp.Uid);
            if (gameObject != null)
            {
                GamePropNodeManager.Inst.RemoveItemFromSecondCache(gameObject);
                GamePropNodeManager.Inst.DestroyNode(gameObject);
            }
        }
        else
        {
            //非组合点，不是要删除的组合节点
            LoggerUtils.Log("TryDestroyEntity:Do not Destroy this node is not CommonCombine!");
        }
    }
}