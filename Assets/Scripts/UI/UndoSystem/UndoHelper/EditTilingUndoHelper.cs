using System;
using Basic.UndoRedo;
using Game.Config;
using Game.ECS;
using UI.Manager;
using UI.UIPanels.GameEdit;
using UI.UIWidgets;
using UnityEngine;

/// <summary>
/// Author:Jaywill
/// Description:道具 通用 Tiling 设置undo/redo
/// Date: 2023/9/4 17:19:35
/// </summary>
///
public class EditTilingUndoData
{
    public Vector2 tile; 
    public SceneEntity targetEntity;
    public Type adapterType; 
}

public class EditTilingUndoHelper : BaseUndoHelper
{
    public override void Undo(UndoRecord record)
    {
        EditTilingUndoData helpData = record.BeginData as EditTilingUndoData;
        ExcuteData(helpData);
    }

    public override void Redo(UndoRecord record)
    {
        EditTilingUndoData helpData = record.EndData as EditTilingUndoData;
        ExcuteData(helpData);
    }
    
    private void ExcuteData(EditTilingUndoData helpData)
    {
        if (helpData.targetEntity != null && helpData.targetEntity.GetViewGo() != null)
        {
            InputHandlerManager.Inst.SelectEntity(helpData.targetEntity);
        }
        UpdatePanel(helpData);
    }
    
    protected void UpdatePanel(EditTilingUndoData helpData)
    {
        GamePropertyEditPanel propertyPanel = UIOpenUntils.RefreshGamePropertyEditPanel(helpData.adapterType, helpData.targetEntity);
        var tilingViews = propertyPanel.GetComponentsInChildren<EditTilingView>(true);
        if (tilingViews != null && tilingViews.Length > 0)
        {
            for (int i = 0; i < tilingViews.Length; i++)
            {
                tilingViews[i].SetTiling(helpData.tile);
            }
            var selectView = tilingViews[0];
            selectView.OnUndoValue(helpData.tile);

            var curSubView = selectView.GetComponentInParent<BasePropertyEditSubView>();
            if (curSubView)
            {
                propertyPanel.SelectTabItem(curSubView.GetViewIndex());
            }
        }
    }
}
