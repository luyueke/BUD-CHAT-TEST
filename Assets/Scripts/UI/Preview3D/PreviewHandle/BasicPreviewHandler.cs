using System.Collections.Generic;
using DG.Tweening;
using Es;
using UI.Preview3D.Base;
using UI.Preview3D.Bean;
using UI.Preview3D.Helper;
using UnityEngine;

namespace UI.Preview3D.PreviewHandle
{
    public abstract class BasicPreviewHandler : IPreview3DHandler
    {
        protected Preview3DData CurPreviewData;
        protected PreviewWrap CurPreviewWrap;

        private PreviewParams _previewParams;

        private Vector3 _defaultCameraLocalPos;
        private Vector3 _defaultCameraLocalEulerAngles;
        private float _defaultCameraFov;

        private Vector3 _defaultModelLocalPos;
        private Vector3 _defaultModelLocalEulerAngles;
        private Vector3 _defaultModelLocalScale;
        private const float ChangeTime = 0.5f;

        private List<Tweener> _tweeners = new List<Tweener>();

        public virtual void HandlePreview(Preview3DData data, PreviewWrap wrap)
        {
            CurPreviewData = data;
            CurPreviewWrap = wrap;
            _previewParams = GetPreviewPrams(data);

            ControlCameraOnStart(data, wrap);
            ControlModelOnStart(data, wrap);
        }

        public virtual void CancelPreview(Preview3DData data, PreviewWrap wrap)
        {
            ControlCameraOnCancel(data, wrap);
            ControlModelOnCancel(data, wrap);
            ForceStopAllTween();
        }

        protected virtual void ControlCameraOnStart(Preview3DData data, PreviewWrap wrap)
        {
            if (_previewParams == null) return;
            var camera = wrap.PreviewModelRoot.previewCamera;
            var cameraTransform = camera.transform;

            var localPosition = cameraTransform.localPosition;
            var localEulerAngles = cameraTransform.localEulerAngles;
            _defaultCameraLocalPos = new Vector3(localPosition.x, localPosition.y, localPosition.z);
            _defaultCameraLocalEulerAngles = new Vector3(localEulerAngles.x, localEulerAngles.y, localEulerAngles.z);
            _defaultCameraFov = camera.fieldOfView;

            var t1 = cameraTransform.transform.DOLocalMove(_previewParams.CameraLocalPos, ChangeTime);
            var t2 = cameraTransform.transform.DOLocalRotate(_previewParams.CameraLocalEulerAngles, ChangeTime);
            _tweeners.Add(t1);
            _tweeners.Add(t2);

            if (_previewParams.CameraFov > 0f)
            {
                camera.fieldOfView = _previewParams.CameraFov;
            }
        }

        protected virtual void ControlCameraOnCancel(Preview3DData data, PreviewWrap wrap)
        {
            if (_previewParams == null) return;
            var cameraTransform = wrap.PreviewModelRoot.previewCamera.transform;
            var t1 = cameraTransform.transform.DOLocalMove(_defaultCameraLocalPos, ChangeTime);
            var t2 = cameraTransform.transform.DOLocalRotate(_defaultCameraLocalEulerAngles, ChangeTime);
            _tweeners.Add(t1);
            _tweeners.Add(t2);
            wrap.PreviewModelRoot.previewCamera.fieldOfView = _defaultCameraFov;
        }

        protected virtual void ControlModelOnStart(Preview3DData data, PreviewWrap wrap)
        {
            if (_previewParams == null) return;
            var modelTrans = wrap.PreviewModelRoot.previewModel;

            var oldPos = modelTrans.localPosition;
            var oldEulerAngles = modelTrans.localEulerAngles;
            var oldScale = modelTrans.localScale;
            _defaultModelLocalPos = new Vector3(oldPos.x, oldPos.y, oldPos.z);
            _defaultModelLocalEulerAngles = new Vector3(oldEulerAngles.x, oldEulerAngles.y, oldEulerAngles.z);
            _defaultModelLocalScale = new Vector3(oldScale.x, oldScale.y, oldScale.z);

            var targetPos = _previewParams.ModelLocalPos;
            var targetEulerAngles = _previewParams.ModelLocalEulerAngles;
            var targetScale = _previewParams.ModelLocalScale;
            var mT1 = modelTrans.DOLocalMove(targetPos, ChangeTime);
            var mT2 = modelTrans.DOLocalRotate(targetEulerAngles, ChangeTime);
            var mT3 = modelTrans.DOScale(targetScale, ChangeTime);
            _tweeners.Add(mT1);
            _tweeners.Add(mT2);
            _tweeners.Add(mT3);
        }

        protected virtual void ControlModelOnCancel(Preview3DData data, PreviewWrap wrap)
        {
            if (_previewParams == null) return;
            var modelTrans = wrap.PreviewModelRoot.previewModel;
            var mT1 = modelTrans.DOLocalMove(_defaultModelLocalPos, ChangeTime);
            var mT2 = modelTrans.DOLocalRotate(_defaultModelLocalEulerAngles, ChangeTime);
            var mT3 = modelTrans.DOScale(_defaultModelLocalScale, ChangeTime);
            _tweeners.Add(mT1);
            _tweeners.Add(mT2);
            _tweeners.Add(mT3);
        }

        protected virtual PreviewParams GetPreviewPrams(Preview3DData data)
        {
            return data.PreviewParams ?? new PreviewParams();
        }

        public void ForceStopAllTween()
        {
            foreach (var t in _tweeners)
            {
                t?.Kill();
            }
        }
    }
}