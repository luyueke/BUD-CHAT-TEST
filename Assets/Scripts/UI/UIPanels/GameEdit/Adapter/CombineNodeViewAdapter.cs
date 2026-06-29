using System.Collections.Generic;
using System.Linq;
using Basic.UndoRedo;
using Game.Base;
using Game.ECS;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;
using GameData;
using UndoSystem;
using UnityEngine;

namespace UI.UIPanels.GameEdit {
    public class CombineNodeViewAdapter : BasePropertyAdapter {
        GameColorEditSubView colorSubView;
        GameMatEditSubView matSubView;
        private CombineBehaviour combineBehaviour;
        private List<SimpleShapeBehaviour> simpleShapeBehaviours;

        private bool isSetColorUndoData = false;
        private bool isSetMatUndoData = false;
        private bool isSetTilingUndoData = false;


        protected override void OnCreate() {
        }

        protected override void OnSelectEntity() {
            var combineNode = selectEntity.GetComp<GameObjectComponent>().BindGo;
            combineBehaviour = combineNode.GetComponent<CombineBehaviour>();
            if (combineNode == null)
            {
                LoggerUtils.LogError("CombineNodeViewAdapter.OnSelectEntity combineNode is null");
                return;
            }
            var behaviours = combineNode.GetComponentsInChildren<NodeBaseBehaviour>();
            if (behaviours == null)
            {
                LoggerUtils.LogError("CombineNodeViewAdapter.OnSelectEntity behaviours is null");
                return;
            }

            bool isAllSimple = behaviours.All(tmp => tmp is SimpleShapeBehaviour || tmp == combineBehaviour);

            MaterialUnionID materialUnionID = null;
            Color? color = null;
            if (isAllSimple) {
                simpleShapeBehaviours = behaviours
                    .OfType<SimpleShapeBehaviour>().ToList();
                matSubView = AddMatSubView();
                colorSubView = AddColorSubView();
                matSubView.AddMatChangeListener(OnMatChange);
                matSubView.AddTilingChangeListener(OnTilingChange);
                colorSubView.AddColorChangeListener(OnColorChange);

                foreach (var tmpBehaviour in simpleShapeBehaviours) {
                    var tmpMat = tmpBehaviour.entity.GetComp<MaterialComponent>();
                    if (materialUnionID == null) {
                        materialUnionID = tmpMat.matId;
                    } else if (materialUnionID != tmpMat.matId) {
                        materialUnionID = null;
                        break;
                    }
                }

                if (materialUnionID != null) {
                    matSubView.SetMaterialWithNoNotify(materialUnionID);
                }


                foreach (var tmpBehaviour in simpleShapeBehaviours) {
                    var tmpMat = tmpBehaviour.entity.GetComp<MaterialComponent>();
                    if (color == null) {
                        color = tmpMat.color;
                    } else if (color != tmpMat.color) {
                        color = null;
                        break;
                    }
                }

                if (color != null) {
                    colorSubView.SetColorWithNoNotify(color.Value);
                }
            }
        }

        private void OnColorChange(Color color) {
            if (color == Color.clear) {
                return;
            }
            var beforeData = GetCurCombineNodeCommonSelectUndoData(CombineNodeSelectUndoType.Color);

            foreach (var simpleBehaviour in simpleShapeBehaviours) {
                simpleBehaviour.SetColor(color);
                simpleBehaviour.entity.GetComp<MaterialComponent>().color = color;
            }

            var nowData = GetCurCombineNodeCommonSelectUndoData(CombineNodeSelectUndoType.Color);
            CheckColorUndo(beforeData, nowData);
        }

        private void OnTilingChange(float tilling) {
            var beforeData = GetCurCombineNodeCommonSelectUndoData(CombineNodeSelectUndoType.Tiling);

            foreach (var simpleBehaviour in simpleShapeBehaviours) {
                var matComponent = simpleBehaviour.entity.GetComp<MaterialComponent>();
                var newTile = matComponent.tile;
                newTile.x -= tilling;
                newTile.y -= tilling;
                if (newTile.x < 0) newTile.x = 0;
                if (newTile.y < 0) newTile.y = 0;
                simpleBehaviour.SetMaterialTiling(newTile);
                matComponent.tile = newTile;
            }
            var nowData = GetCurCombineNodeCommonSelectUndoData(CombineNodeSelectUndoType.Tiling);
            CheckTilingUndo(beforeData, nowData);
        }

        private void OnMatChange(GameMatUIData matData) {
            if (matData == null || matData.Id == null) {
                return;
            }

            var beforeData = GetCurCombineNodeCommonSelectUndoData(CombineNodeSelectUndoType.Mat);

            foreach (var simpleBehaviour in simpleShapeBehaviours) {
                simpleBehaviour.SetMaterial(matData.Id);
                simpleBehaviour.entity.GetComp<MaterialComponent>().matId = matData.Id;
            }

            var nowData = GetCurCombineNodeCommonSelectUndoData(CombineNodeSelectUndoType.Mat);

            CheckMatUndo(beforeData, nowData);
        }

        private void CheckColorUndo(CombineNodeCommonSelectUndoData beforeData, CombineNodeCommonSelectUndoData nowData) {
            if (isSetColorUndoData) {
                return;
            }
            isSetColorUndoData = true;
            AddRecord(beforeData, nowData);
        }

        private void CheckMatUndo(CombineNodeCommonSelectUndoData beforeData, CombineNodeCommonSelectUndoData nowData) {
            if (isSetMatUndoData) {
                return;
            }
            isSetMatUndoData = true;
            AddRecord(beforeData, nowData);
        }

        private void CheckTilingUndo(CombineNodeCommonSelectUndoData beforeData, CombineNodeCommonSelectUndoData nowData) {
            if (isSetTilingUndoData) {
                return;
            }
            isSetTilingUndoData = true;
            AddRecord(beforeData, nowData);
        }


        void AddRecord(CombineNodeCommonSelectUndoData beginData, CombineNodeCommonSelectUndoData endData) {
            UndoRecord record = new UndoRecord(UndoHelperName.CombineNodeSelectUndoHelper);
            record.BeginData = beginData;
            record.EndData = endData;
            UndoRecordPool.Inst.PushRecord(record);
            isSetColorUndoData = true;
        }

        CombineNodeCommonSelectUndoData GetCurCombineNodeCommonSelectUndoData(CombineNodeSelectUndoType type) {
            var undoData = new CombineNodeCommonSelectUndoData();
            undoData.targetEntity = selectEntity;
            undoData.type = type;
            foreach (var behaviour in simpleShapeBehaviours) {
                undoData.dataList.Add(new SingleCombineNodeSelectUndoData() {
                    targetEntity = behaviour.entity,
                    matId = behaviour.entity.GetComp<MaterialComponent>().matId,
                    color = behaviour.entity.GetComp<MaterialComponent>().color,
                    tiling = behaviour.entity.GetComp<MaterialComponent>().tile
                });
            }
            return undoData;
        }
    }
}
