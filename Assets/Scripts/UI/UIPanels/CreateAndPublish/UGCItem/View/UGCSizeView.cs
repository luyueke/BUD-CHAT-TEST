using System;
using System.Collections;
using Basic;
using Game;
using Game.Base;
using Game.Config;
using Game.Props.PropsManagers;
using Game.Utils;
using GameData.BaseInfo;
using GameData.MapData;
using GameData.UGCData;
using RTG;
using UI.Manager;
using UnityEngine;

namespace UI {
    public class UGCSizeView : UGCBaseStateView {
        [SerializeField] private Camera previewCamera;

        [SerializeField]
        private GameObject sizeTarget;
        private GameObject ugcObj;

        [SerializeField] private PreviewCameraHandler previewCameraHandler;


        public override void Awake() {
            base.Awake();
            GlobalCameraManager.Inst.InsertLast(previewCamera);
        }


        public void Update() {
            if (scaleGizmo != null) {
                scaleGizmo.RefreshPositionAndRotation();
            }
        }


        public override void Show() {
            base.Show();
            InitGizmo();
            if (editData is PropEditData itemEditData && itemEditData.draftInfo != null && ugcObj == null) {
                if (!string.IsNullOrEmpty(itemEditData.draftInfo.GetMetadataUrl())) {
                    ugcObj = AssetPropNodeManager.Inst.CreatePropSync(itemEditData.GetInfo().id, itemEditData.metaDataBytes);
                    ugcObj.transform.SetParent(sizeTarget.transform);
                    ugcObj.Reset();
                }
            }

            if (editData.GetInfo() is PropInfo itemInfo) {
                if (itemInfo.detailInfo == null) {
                    itemInfo.detailInfo = new DetailInfo();
                }
                sizeTarget.transform.localScale = itemInfo.detailInfo.scale;
            } else {
                sizeTarget.transform.localScale = Vector3.one;
            }

            scaleGizmo.SetTargetObject(ugcObj);

            previewCameraHandler.SetTarget(ugcObj);
            previewCameraHandler.SetFocus();
            // anchor 与 BaseAnchorView 中 anchorTarget.localPosition 同属 objRoot 局部坐标系，
            // 在 UGCSizeView 中等价于 ugcObj 局部坐标。用 CustomWorldPivot 固定 gizmo 世界坐标，
            // 确保缩放过程中 gizmo 不随 scale 变化而漂移
            if (ugcObj != null && editData.GetInfo() is PropInfo anchorProp && anchorProp.detailInfo != null) {
               var gizmoPivotWorld = ugcObj.transform.TransformPoint(anchorProp.detailInfo.anchor);
               scaleGizmo.SetCustomWorldPivot(gizmoPivotWorld);
               scaleGizmo.SetTransformPivot(GizmoObjectTransformPivot.CustomWorldPivot);
            }
        }


        public override void Hide() {
            base.Hide();
            ReleaseGizmo();
        }

        protected override void OnNextBtnClick() {
            base.OnNextBtnClick();
            if (editData.GetInfo() is PropInfo itemInfo) {
                if (itemInfo.detailInfo == null) {
                    itemInfo.detailInfo = new DetailInfo();
                }
                itemInfo.detailInfo.scale = sizeTarget.transform.localScale;
            }
        }


        private void OnDestroy() {
            GlobalCameraManager.Inst.Remove(previewCamera);
            if (ugcObj != null) {
                AssetPropNodeManager.Inst.DestroyProp(ugcObj);
                ugcObj = null;
            }
        }

        private bool isNewRTG = false;
        private ObjectTransformGizmo scaleGizmo;
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

            scaleGizmo = RTGizmosEngine.Get.CreateObjectScaleGizmo();
            scaleGizmo.SetTransformSpace(GizmoSpace.Local);
            scaleGizmo.SetSnapEnabled(true);
            scaleGizmo.SetRit();
            scaleGizmo.Gizmo.PostDragBegin += (Gizmo giz, int handle) =>
            {
                InputReceiver.locked = true;
            };
            scaleGizmo.Gizmo.PostDragEnd += (Gizmo giz, int handle) =>
            {
                InputReceiver.locked = false;
            };

        }

        protected void ReleaseGizmo() {
            var curCamera = RTFocusCamera.Get.TargetCamera;
            RTFocusCamera.Get.SetTargetCamera(GameCameraUtils.Inst.GetMainCamera());
            if (curCamera != null) {
                RTGizmosEngine.Get.RemoveRenderCamera(curCamera);
            }

            RTGizmosEngine.Get.AddRenderCamera(GameCameraUtils.Inst.GetMainCamera());
            RTScene.Get.IgnoreUIElementHovered = false;
            RTGizmosEngine.Get.RemoveGizmo(scaleGizmo.Gizmo);
            if (isNewRTG) {
                GameObject.DestroyImmediate(RTGApp.Get.gameObject);
            }

            InputReceiver.locked = false;
        }

    }
}
