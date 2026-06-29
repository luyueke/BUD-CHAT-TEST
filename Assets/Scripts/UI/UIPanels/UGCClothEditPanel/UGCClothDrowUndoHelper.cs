/// <summary>
/// Author:Zhouzihan
/// Description:
/// Date: 2022/6/13 20:51:4
/// </summary>

using System;
using System.Collections;
using System.Collections.Generic;
using Basic.UndoRedo;
using Game.UGCEditor;
using UnityEngine;

public class UGCClothDrawUndoData : UGCBasicUndoData
{
    public int undoId;
    public UGCDrawMode type;
    public GridMapCanvas mapCanvas;
    public Dictionary<Vector2Int,Color> drawGridPairs;
    public Action<int> changePartAction;
    public int partId;
}
public class UGCClothDrawUndoHelper : BaseUndoHelper
{
    public override void Undo(UndoRecord record)
    {
        UGCClothDrawUndoData helpData = record.BeginData as UGCClothDrawUndoData;
        ExcuteData(helpData);
    }
    public override void Redo(UndoRecord record)
    {
        UGCClothDrawUndoData helpData = record.EndData as UGCClothDrawUndoData;
        ExcuteData(helpData);
    }
    private void ExcuteData(UGCClothDrawUndoData helpData)
    {
        helpData.changePartAction?.Invoke(helpData.partId);
        if (helpData.mapCanvas)
        {
            helpData.mapCanvas.OnUndoSetGrid(helpData.drawGridPairs);
        }
    }
    
}
