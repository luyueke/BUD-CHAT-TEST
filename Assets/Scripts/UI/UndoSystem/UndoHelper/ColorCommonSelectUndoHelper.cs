/**
 * @ Author: Jun Zhou
 * @ Create Time: 2023-08-18 18:24:34
 * @ Modified by: Jun Zhou
 * @ Modified time: 2023-08-25 15:16:13
 * @ Description: 颜色面板Undo/Redo
 */

using System;
using Basic.UndoRedo;
using Game.ECS;
using UI.Manager;
using UI.UIPanels.GameEdit;
using UnityEngine;


public class ColorCommonSelectUndoData
{
    public Color color;
    public SceneEntity targetEntity; // 用于回撤时选中当前实体
    public Type adapterType; // 对于没有选择实体（设置类型）需要通过适配器类型还原出面板
}

public class ColorCommonSelectUndoHelper : BaseUndoHelper
{
    public override void Undo(UndoRecord record)
    {
        ColorCommonSelectUndoData helpData = record.BeginData as ColorCommonSelectUndoData;
        ExcuteData(helpData);
    }

    public override void Redo(UndoRecord record)
    {
        ColorCommonSelectUndoData helpData = record.EndData as ColorCommonSelectUndoData;
        ExcuteData(helpData);
    }
    
    private void ExcuteData(ColorCommonSelectUndoData helpData)
    {
        if (helpData.targetEntity != null && helpData.targetEntity.GetViewGo() != null)
        {
            InputHandlerManager.Inst.SelectEntity(helpData.targetEntity);
        }
        UpdatePanel(helpData);
    }

    protected void UpdatePanel(ColorCommonSelectUndoData helpData)
    {
        GamePropertyEditPanel propertyPanel = UIOpenUntils.RefreshGamePropertyEditPanel(helpData.adapterType, helpData.targetEntity);

        var subView = propertyPanel.GetSubView<GameColorEditSubView>();
        if (subView != null)
        {
            propertyPanel.SelectTabItem<GameColorEditSubView>();
            subView.OnUndoSelect(helpData);
        }
        else
        {
            LoggerUtils.LogError("ColorCommonSelectUndoHelper can not find GameColorEditSubView");
        }
    }
}
