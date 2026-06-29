using System.Collections.Generic;
using Basic.UndoRedo;
using Game.ECS;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;
using GameData;
using UI.Manager;
using UI.UIPanels.GameEdit;
using UnityEngine;


public class SingleCombineNodeSelectUndoData {
    public MaterialUnionID matId;
    public SceneEntity targetEntity; // 用于回撤时选中当前实体
    public Color color;
    public Vector2 tiling;
}


public enum CombineNodeSelectUndoType {
    Mat,
    Color,
    Tiling,
}


public class CombineNodeCommonSelectUndoData {
    public SceneEntity targetEntity;
    public List<SingleCombineNodeSelectUndoData> dataList = new List<SingleCombineNodeSelectUndoData>();
    public CombineNodeSelectUndoType type;
}


public class CombineNodeSelectUndoHelper : BaseUndoHelper {
    public override void Undo(UndoRecord record) {
        CombineNodeCommonSelectUndoData helpData = record.BeginData as CombineNodeCommonSelectUndoData;
        ExcuteData(helpData);
    }

    public override void Redo(UndoRecord record) {
        CombineNodeCommonSelectUndoData helpData = record.EndData as CombineNodeCommonSelectUndoData;
        ExcuteData(helpData);
    }

    private void ExcuteData(CombineNodeCommonSelectUndoData helpData) {
        if (helpData.targetEntity != null && helpData.targetEntity.GetViewGo() != null) {
            InputHandlerManager.Inst.SelectEntity(helpData.targetEntity);
        }

        UpdatePanel(helpData);
    }


    protected void UpdatePanel(CombineNodeCommonSelectUndoData helpData) {
        GamePropertyEditPanel propertyPanel =
            UIOpenUntils.RefreshGamePropertyEditPanel(typeof(CombineNodeViewAdapter), helpData.targetEntity);

        if (helpData.type == CombineNodeSelectUndoType.Mat) {
            var subView = propertyPanel.GetSubView<GameMatEditSubView>();
            if (subView != null) {
                propertyPanel.SelectTabItem<GameMatEditSubView>();
                MaterialUnionID materialUnionID = null;
                foreach (var undoData in helpData.dataList) {
                    var simpleBehaviour = undoData.targetEntity.GetViewGo().GetComponent<SimpleShapeBehaviour>();
                    var matComponent = undoData.targetEntity.GetComp<MaterialComponent>();
                    simpleBehaviour.SetMaterial(undoData.matId);
                    matComponent.matId = undoData.matId;
                }

                foreach (var undoData in helpData.dataList) {
                    if (materialUnionID == null) {
                        materialUnionID = undoData.matId;
                    } else if (materialUnionID != undoData.matId) {
                        materialUnionID = null;
                        break;
                    }
                }

                var matUndoData = new MatCommonSelectUndoData() {
                    matId = materialUnionID,
                };
                subView.OnUndoSelect(matUndoData);
            } else {
                LoggerUtils.LogError("CombineNodeSelectUndoHelper can not find GameMatEditSubView");
            }
        } else if (helpData.type == CombineNodeSelectUndoType.Color) {
            var subView = propertyPanel.GetSubView<GameColorEditSubView>();
            if (subView != null) {
                Color color = Color.clear;
                propertyPanel.SelectTabItem<GameColorEditSubView>();
                foreach (var undoData in helpData.dataList) {
                    var simpleBehaviour = undoData.targetEntity.GetViewGo().GetComponent<SimpleShapeBehaviour>();
                    var matComponent = undoData.targetEntity.GetComp<MaterialComponent>();
                    simpleBehaviour.SetColor(undoData.color);
                    matComponent.color = undoData.color;
                }

                foreach (var undoData in helpData.dataList) {
                    if (color == Color.clear) {
                        color = undoData.color;
                    } else if (color != undoData.color) {
                        color = Color.clear;
                        break;
                    }
                }

                var colorUndoData = new ColorCommonSelectUndoData() {
                    color = color,
                };
                subView.OnUndoSelect(colorUndoData);
            } else {
                LoggerUtils.LogError("CombineNodeSelectUndoHelper can not find GameColorEditSubView");
            }
        }
    }
}
