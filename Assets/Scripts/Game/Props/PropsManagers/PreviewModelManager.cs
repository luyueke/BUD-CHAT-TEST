using Game.Base;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;
using Game.Scene.ModeController;
using Game.Utils;
using GameData.BaseInfo;
using GameData.Config;
using GameData.Manager;
using Pb.Map;
using UnityEngine;
using xasset;

namespace Game.Props.PropsManagers {
    [NodeBehaviourAttribute(typeof(PreviewModelBehaviour))]
    public class PreviewModelManager : BaseNodeManager {
        private const string PreviewModelPropId = "20100067";
        private PreviewModelBehaviour previewModelBehaviour;

        protected override void OnNotifyCreateInEdit(NodeBaseBehaviour nodeBehaviour) {
            previewModelBehaviour = nodeBehaviour as PreviewModelBehaviour;
            CreateModel();
        }

        protected override void OnNotifyCreateInBuild(NodeBaseBehaviour nodeBehaviour) {
            previewModelBehaviour = nodeBehaviour as PreviewModelBehaviour;
            CreateModel();
        }

        public void CheckModel() {
            if (entities.Count < 1) {
                var comp = new PreviewModelComponent() {
                    color = Color.white
                };
                var componentData = comp.Write();
                var pNodeData = new PNodeData {
                    PropId = GamePropDataHelper.GetPropIdByNodeModelType(NodeModelType.PreviewModel).Id,
                    Pos = Vector3.zero.ToPB(),
                    Scale = Vector3.one.ToPB(),
                    Rotation = Vector3.zero.ToPB(),
                    Attrs = { componentData }
                };
                GamePropNodeManager.Inst.CreateSceneNodeByData(pNodeData);
            }
        }

        public void CheckPersonModel()
        {
            var vehicleInfo = GameDataManager.Inst.mapGlobalData?.GetCurInfo<VehicleInfo>();
            if (entities.Count >= vehicleInfo.vehicleType)
            {
                return;
            }
            var comp = new PreviewModelComponent()
            {
                color = Color.white
            };
            var componentData = comp.Write();
            var pNodeData = new PNodeData
            {
                PropId = GamePropDataHelper.GetPropIdByNodeModelType(NodeModelType.PreviewModel).Id,
                Pos = Vector3.zero.ToPB(),
                Scale = Vector3.one.ToPB(),
                Rotation = Vector3.zero.ToPB(),
                Attrs = { componentData }
            };
            GamePropNodeManager.Inst.CreateSceneNodeByData(pNodeData);
        } 

        private void CreateModel() {
            if (!this.IsSkinEdit() && !this.IsMusicInstrumentEdit() && !this.IsVehicleEdit()) {
                return;
            }
            string prefabPath = "Assets/Loadable/Model3D/Editor_Props/PreviewModel/gdgt_Character_PREFAB.prefab";
            if (this.IsSkinEdit()) {
                var skinInfo = GameDataManager.Inst.mapGlobalData.GetCurInfo<SkinInfo>();
                if ((SkinType)skinInfo.skinType == SkinType.Pet) {
                    prefabPath = "Assets/Loadable/Model3D/Editor_Props/PreviewModel/gdgt_Pet_PREFAB.prefab";
                }
            }


            GameObject go = null;
            var wrapper = Loader.Load<GameObject>(prefabPath);
            go = wrapper.Instantiate(previewModelBehaviour.transform);
            go.name = wrapper.GetAssetName();
            go.transform.localRotation = Quaternion.Euler(0, 180, 0);
            go.transform.localPosition = Vector3.zero;
            previewModelBehaviour.RefreshColor();
        }
    }
}
