

using Basic.UndoRedo;
public class WinConditionUndoData
{
    public int wId; //通关条件id
}

/// <summary>
/// Author:shaocheng
/// Description:通关条件undo
/// Date: 2022-9-2 15:36:23
/// </summary>
public class WinConditionUndoHelper : BaseUndoHelper
{
    public override void Undo(UndoRecord record)
    {
        var data = record.BeginData as WinConditionUndoData;
        ExecuteData(data);
    }
    
    public override void Redo(UndoRecord record)
    {
        var data = record.EndData as WinConditionUndoData;
        ExecuteData(data);
    }
    
    private void ExecuteData(WinConditionUndoData data)
    {
        WinConditionManager.Inst.SetWinCondition(data.wId);
    }
}