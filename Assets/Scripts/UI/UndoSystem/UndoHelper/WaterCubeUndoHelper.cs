using Basic.UndoRedo;
using Game.ECS;
using GameData;
using UI.Manager;
using UI.UIPanels.GameEdit;

/// <summary>
/// Author:Jaywill
/// Description: 水方块undo/redo
/// Date: 2023/9/45 11:15:25
/// </summary>
///
public enum WaterUndoType
{
    ModelShape,
    Speed,
    OxygenType
}

public class WaterCubeUndoData
{
    public PropModelShape ModelShape;
    public float Speed;
    public int OxygenType;
    public WaterUndoType UndoType;
    public SceneEntity targetEntity;
}

public class WaterCubeUndoHelper : BaseUndoHelper
{
    public override void Undo(UndoRecord record)
    {
        WaterCubeUndoData helpData = record.BeginData as WaterCubeUndoData;
        ExcuteData(helpData);
    }

    public override void Redo(UndoRecord record)
    {
        WaterCubeUndoData helpData = record.EndData as WaterCubeUndoData;
        ExcuteData(helpData);
    }
    
    private void ExcuteData(WaterCubeUndoData helpData)
    {
        if (helpData.targetEntity != null && helpData.targetEntity.GetViewGo() != null)
        {
            InputHandlerManager.Inst.SelectEntity(helpData.targetEntity);
        }
        UpdatePanel(helpData);
    }
    
    protected void UpdatePanel(WaterCubeUndoData helpData)
    {
        GamePropertyEditPanel propertyPanel = UIOpenUntils.RefreshGamePropertyEditPanel(null, helpData.targetEntity);
        WaterCubeViewAdapter viewAdapter = propertyPanel.GetComponentInChildren<WaterCubeViewAdapter>(true);
        if (viewAdapter != null)
        {
            viewAdapter.OnWaterUndo(helpData);
        }
    }
}
