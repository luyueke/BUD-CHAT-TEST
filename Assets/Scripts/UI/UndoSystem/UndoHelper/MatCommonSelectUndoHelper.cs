/**
 * @ Author: Jun Zhou
 * @ Create Time: 2023-08-18 18:24:34
 * @ Modified by: Jun Zhou
 * @ Modified time: 2023-09-12 13:07:21
 * @ Description: 材质面板Undo/Redo
 */

using System;
using Basic.UndoRedo;
using Game.ECS;
using GameData;
using UI.Manager;
using UI.UIPanels.GameEdit;


public class MatCommonSelectUndoData
{
    public MaterialUnionID matId;
    public SceneEntity targetEntity; // 用于回撤时选中当前实体
    public Type adapterType; // 对于没有选择实体（设置类型）需要通过适配器类型还原出面板
}

public class MatCommonSelectUndoHelper : BaseUndoHelper
{
    public override void Undo(UndoRecord record)
    {
        MatCommonSelectUndoData helpData = record.BeginData as MatCommonSelectUndoData;
        ExcuteData(helpData);
    }

    public override void Redo(UndoRecord record)
    {
        MatCommonSelectUndoData helpData = record.EndData as MatCommonSelectUndoData;
        ExcuteData(helpData);
    }
    
    private void ExcuteData(MatCommonSelectUndoData helpData)
    {
        if (helpData.targetEntity != null && helpData.targetEntity.GetViewGo() != null)
        {
            InputHandlerManager.Inst.SelectEntity(helpData.targetEntity);
        }
        UpdatePanel(helpData);
    }

    protected void UpdatePanel(MatCommonSelectUndoData helpData)
    {
        GamePropertyEditPanel propertyPanel = UIOpenUntils.RefreshGamePropertyEditPanel(helpData.adapterType, helpData.targetEntity);

        var subView = propertyPanel.GetSubView<GameMatEditSubView>();
        if (subView != null)
        {
            propertyPanel.SelectTabItem<GameMatEditSubView>();
            subView.OnUndoSelect(helpData);
        }
        else
        {
            LoggerUtils.LogError("MatCommonSelectUndoHelper can not find GameMatEditSubView");
        }
    }
}
