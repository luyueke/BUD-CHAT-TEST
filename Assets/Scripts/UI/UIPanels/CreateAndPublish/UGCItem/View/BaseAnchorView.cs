using Basic;
using Game;
using Game.Base;
using Game.Props.PropsManagers;
using Game.Utils;
using GameData.BaseInfo;
using RTG;
using UI.Manager;
using UnityEngine;
using xasset;

namespace UI {
    public class BaseAnchorView : UGCBaseStateView {
        protected BasePropEditData baseEditData = null;


        [SerializeField] protected Camera previewCamera;

        [SerializeField] protected GameObject anchorTarget;


        [SerializeField] protected GameObject objRoot;

        [SerializeField] protected PreviewCameraHandler previewCameraHandler;

        protected GameObject ugcObj;
        protected bool isLockGizmo = false;

        public override void Awake() {
            base.Awake();
            GlobalCameraManager.Inst.InsertLast(previewCamera);
        }

        public override void Show() {
            base.Show();
            InitGizmo();
            SetPropAndGizmo();
        }


        public override void SetEditData(UGCBaseEditData data) {
            base.SetEditData(data);
            baseEditData = data as BasePropEditData;
        }

        protected virtual void SetPropAndGizmo() {
            moveGizmo.SetTargetObject(anchorTarget);

            if (ugcObj == null) {

                if (baseEditData.metaDataBytes == null && !string.IsNullOrEmpty(editData.GetInfo().metaDataUrl)) {
                    baseEditData.metaDataBytes = Asset.LoadRemoteAssetSync(editData.GetInfo().metaDataUrl);
                }
                if (baseEditData.metaDataBytes != null) {
                    ugcObj = AssetPropNodeManager.Inst.CreatePropSync(editData.GetInfo().id, baseEditData.metaDataBytes);
                    ugcObj.transform.SetParent(objRoot.transform);
                    ugcObj.Reset();
                    baseEditData.size ??= ugcObj.GetBounds(true).size;
                }
            }
            previewCameraHandler.SetTarget(objRoot);
            previewCameraHandler.SetFocus();
        }

        public virtual void Update() {
            if (moveGizmo != null) {
                moveGizmo.RefreshPositionAndRotation();
            }
        }

        public override void Hide() {
            base.Hide();
            ReleaseGizmo();
        }

        protected virtual void OnDestroy() {
            GlobalCameraManager.Inst.Remove(previewCamera);
            if (ugcObj != null) {
                AssetPropNodeManager.Inst.DestroyProp(ugcObj);
                ugcObj = null;
            }
        }

        private bool isNewRTG = false;
        private ObjectTransformGizmo moveGizmo;
        protected void InitGizmo() {
            var rtg = RTGApp.Get;
            if (rtg == null) {
                var rtgAppWrapper = Loader.Load<GameObject>(GizmoManager.RtgPrefabPath);
                var rtgObj = rtgAppWrapper.Instantiate();
                rtgObj.GetComponentInChildren<RTFocusCamera>(true).SetTargetCamera(previewCamera);
                rtgObj.gameObject.SetActive(true);
                isNewRTG = true;
            } else {
                RTFocusCamera.Get.SetTargetCamera(previewCamera);
            }


            RTGizmosEngine.Get.RemoveRenderCamera(GameCameraUtils.Inst.GetMainCamera());
            RTGizmosEngine.Get.AddRenderCamera(previewCamera);
            RTScene.Get.IgnoreUIElementHovered = true;

            moveGizmo = RTGizmosEngine.Get.CreateObjectMoveGizmo();
            moveGizmo.SetTransformSpace(GizmoSpace.Local);
            moveGizmo.SetRit();
            moveGizmo.Gizmo.PostDragBegin += (Gizmo giz, int handle) =>
            {
                InputReceiver.locked = true;
            };
            moveGizmo.Gizmo.PostDragEnd += (Gizmo giz, int handle) =>
            {
                InputReceiver.locked = false;
            };


        }

        protected void ReleaseGizmo() {
            var rtgCameraScript = RTGApp.Get.GetComponentInChildren<RTFocusCamera>();
            var curCamera = rtgCameraScript.TargetCamera;
            rtgCameraScript.SetTargetCamera(GameCameraUtils.Inst.GetMainCamera());
            if (curCamera != null) {
                RTGizmosEngine.Get.RemoveRenderCamera(curCamera);
            }
            RTGizmosEngine.Get.AddRenderCamera(GameCameraUtils.Inst.GetMainCamera());
            RTScene.Get.IgnoreUIElementHovered = false;
            RTGizmosEngine.Get.RemoveGizmo(moveGizmo.Gizmo);
            if (isNewRTG) {
                GameObject.DestroyImmediate(RTGApp.Get.gameObject);
            }
            InputReceiver.locked = false;
        }


    }
}
