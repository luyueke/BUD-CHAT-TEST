using System;
using Basic.UndoRedo;
using Game.Config;
using Game.ECS;
using UI.Manager;
using UI.UIPanels.GameEdit;
using UnityEngine;

/// <summary>
/// Author:Jaywill
/// Description:PGC 通用 Undo/Redo 管理
/// Date: 2023/8/18 15:10:05
/// </summary>
///
public class PGCCommonSelectUndoData
{
    public string pgcId;
    public int viewIndex; // 可能存在多个View，记录一下View的索引
    public SceneEntity targetEntity;
    public Type adapterType; // 对于没有选择实体（设置类型）需要通过适配器类型还原出面板
}

public class PGCCommonSelectUndoHelper : BaseUndoHelper
{
    public override void Undo(UndoRecord record)
    {
        PGCCommonSelectUndoData helpData = record.BeginData as PGCCommonSelectUndoData;
        ExcuteData(helpData);
    }

    public override void Redo(UndoRecord record)
    {
        PGCCommonSelectUndoData helpData = record.EndData as PGCCommonSelectUndoData;
        ExcuteData(helpData);
    }
    
    private void ExcuteData(PGCCommonSelectUndoData helpData)
    {
        if (helpData.targetEntity != null && helpData.targetEntity.GetViewGo() != null)
        {
            InputHandlerManager.Inst.SelectEntity(helpData.targetEntity);
        }
        UpdatePanel(helpData);
    }

    protected void UpdatePanel(PGCCommonSelectUndoData helpData)
    {
        GamePropertyEditPanel propertyPanel = UIOpenUntils.RefreshGamePropertyEditPanel(helpData.adapterType, helpData.targetEntity);
        var subView = propertyPanel.GetSubView<PgcListEditSubView>(helpData.viewIndex);
        if (subView != null)
        {
            propertyPanel.SelectTabItem(helpData.viewIndex);
            subView.OnUndoSelect(helpData.pgcId);
        }
        else
        {
            LoggerUtils.LogError("PGCCommonSelectUndoHelper can not find PgcListEditSubView");
        }
    }
}
