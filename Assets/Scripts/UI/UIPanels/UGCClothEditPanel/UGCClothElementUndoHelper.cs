using System;
using System.Collections;
using System.Collections.Generic;
using Basic.UndoRedo;
using Game.UGCEditor;
using UnityEngine;

//UGC衣服Undo/Redo数据区分
public enum UGCElementType
{
    Trans,
    Color,
    DetailTrans,
}

public class UGCBasicUndoData
{
    public int selectPartIndex;

    public UGCBasicUndoData()
    {
        var editPanel = UIManager.Inst.FindPanel<UGCResourceEditPanel>(WindowId.UGCResourceEditWindow, PanelId.UGCResourceEditPanel);
        if (editPanel) selectPartIndex = editPanel.CurrentPartIndex;
    }

    // 撤销其他部位内容时，自动切到被修改的部位
    public void DoSwitchPart()
    {
        var editPanel = UIManager.Inst.FindPanel<UGCResourceEditPanel>(WindowId.UGCResourceEditWindow, PanelId.UGCResourceEditPanel);
        if (editPanel != null && selectPartIndex != editPanel.CurrentPartIndex)
        {
            editPanel.SwitchPart(selectPartIndex);
        }
    }
}

public class ElementUndoData : UGCBasicUndoData
{
    public Vector2 postion;
    public Vector3 eulerAngles;
    public Vector2 sizeDelta;
    public RectTransform targetNode;
    public string color;
    public int transformType;
    public int colorType;
    public Texture tex;
    public string url;
}
public class UGCClothElementUndoHelper : BaseUndoHelper
{
    public override void Undo(UndoRecord record)
    {
        ElementUndoData helpData = record.BeginData as ElementUndoData;
        ExcuteData(helpData);
    }

    public override void Redo(UndoRecord record)
    {
        ElementUndoData helpData = record.EndData as ElementUndoData;
        ExcuteData(helpData);
    }

    private void ExcuteData(ElementUndoData helpData)
    {
        if (helpData != null)
        {
            var interActor = TransformInteractorController.Inst.InterActor;
            helpData.DoSwitchPart();

            if (helpData.transformType == (int)UGCElementType.Trans)
            {
                interActor.SetTransUndo(helpData.targetNode, helpData.postion, helpData.eulerAngles, helpData.sizeDelta);
                UGCImportPhotoManager.Inst.SetTextureUndo(helpData.targetNode, helpData.tex, helpData.url);
            }
            else if (helpData.transformType == (int)UGCElementType.DetailTrans)
            {
                interActor.SetTransUndo(helpData.targetNode, helpData.postion, helpData.eulerAngles, helpData.sizeDelta);
                UGCImportPhotoManager.Inst.SetTextureUndo(helpData.targetNode, helpData.tex, helpData.url);
            }
            else if (helpData.transformType == (int)UGCElementType.Color)
            {
                UGCImportTextManager.Inst.SetColorUndo(helpData.targetNode, DataUtil.DeSerializeColor(helpData.color));
            }
        
            interActor.SetUndoRedoAct?.Invoke();
        }
    }
}
