using System.Collections;
using Game.Base;
using Game.ECS;
using Game.Props.PropsManagers;
using Game.Utils;
using GameData.MapData;
using UI.BaseWidgets;
using UnityEngine;
using xasset;

namespace UI {
    public class BaseCoverView : UGCBaseStateView {
        protected GameObject coverObj = null;

        [SerializeField] protected Camera previewCamera;

        [SerializeField] protected PreviewCameraHandler previewCameraHandler;

        [SerializeField] protected RenderTexture previewRenderTexture;

        protected BasePropEditData baseEditData = null;

        public override void Awake() {
            base.Awake();
        }

        public override void Show() {
            base.Show();

            StartCoroutine(SetVerify());
        }

        public override void Hide() {
            base.Hide();
            StopAllCoroutines();
        }

        public override void SetEditData(UGCBaseEditData data) {
            base.SetEditData(data);
            baseEditData = data as BasePropEditData;
        }

        protected override void SyncEditData() {
            base.SyncEditData();
            ((LoadingButton)nextBtn).HideLoading();
            if (editData.GetInfo() == null) {
                LoggerUtils.LogError("editData.info == null:");
                return;
            }

            if (coverObj == null) {
                if (baseEditData.metaDataBytes == null && !string.IsNullOrEmpty(editData.GetInfo().metaDataUrl)) {
                    baseEditData.metaDataBytes = Asset.LoadRemoteAssetSync(editData.GetInfo().metaDataUrl);
                }
                if (baseEditData.metaDataBytes != null) {
                    coverObj = AssetPropNodeManager.Inst.CreatePropSync(editData.GetInfo().id, baseEditData.metaDataBytes);
                    if (coverObj == null) {
                        LoggerUtils.LogError("coverObj == null:");
                        return;
                    }

                    if (previewCamera == null) {
                        LoggerUtils.LogError("previewCamera == null:");
                        return;
                    }
                    coverObj.transform.SetParent(previewCamera.transform);
                    coverObj.Reset();
                    baseEditData.size ??= coverObj.GetBounds(true).size;
                    previewCameraHandler.SetTarget(coverObj);
                    previewCameraHandler.SetFocus();
                }

            }
        }



        protected virtual IEnumerator SetVerify() {
            while (coverObj == null) {
                yield return null;
            }
        }

        protected byte[] GetCoverData() {
            var lastActive = RenderTexture.active;
            RenderTexture.active = previewRenderTexture;

            var cameraData =
                previewCamera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            bool originPostProcessing = true;
            if (cameraData != null) {
                originPostProcessing = cameraData.renderPostProcessing;
                cameraData.renderPostProcessing = false;
            }

            Texture2D screenShot = new Texture2D(previewRenderTexture.width, previewRenderTexture.height,
                TextureFormat.ARGB32, false);
            screenShot.ReadPixels(new Rect(0, 0, previewRenderTexture.width, previewRenderTexture.height), 0, 0);
            var rawData = screenShot.GetRawTextureData<Color32>();
            for (int i = 0; i < rawData.Length; i++) {
                rawData[i] = rawData[i].ToGama();
            }

            screenShot.Apply();
            var coverData = screenShot.EncodeToPNG();

            if (cameraData != null) {
                cameraData.renderPostProcessing = originPostProcessing;
            }

            RenderTexture.active = lastActive;
            Destroy(screenShot);
            return coverData;
        }

        protected virtual void OnDestroy() {
            if (coverObj != null) {
                AssetPropNodeManager.Inst.DestroyProp(coverObj);
                coverObj = null;
            }
        }


    }
}
