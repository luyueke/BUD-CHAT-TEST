using System;
using Basic.Extensions;
using Sirenix.OdinInspector;
using UI.Preview3D.Base;
using UI.Preview3D.Gesture;
using UnityEngine;
using UnityEngine.UI;
using xasset;

namespace UI.Preview3D.Mono
{
    [Serializable]
    public struct PreviewGesturePadding
    {
        public float left;
        public float right;
        public float top;
        public float bottom;
    }
    
    [RequireComponent(typeof(RawImage))]
    public class Preview3DRawImage : MonoBehaviour
    {
        public RawImage rawImage;
        public SimplePreviewGestureHandle gestureHandle;
        public GameObject touchArea;

        /// <summary>
        /// 自定义响应区域
        /// </summary>
        [OnValueChanged("RefreshClickAreaPadding")] 
        public PreviewGesturePadding touchAreaPadding;

        private void Awake()
        {
            rawImage = GetComponent<RawImage>();
            InitTouchArea();
            Preview3DManager.Inst.Attach(this);
        }

        private void OnDisable()
        {
            Preview3DManager.Inst.CancelPreview(this);
        }

        void OnDestroy()
        {
            Preview3DManager.Inst.Detach(this);
        }

        private void InitTouchArea()
        {
            if (touchArea == null)
            {
                var touchAsset = Asset.Load(Preview3DConstant.PathTouchPrefab, typeof(GameObject));
                GameObject prefab = touchAsset.asset as GameObject;
                touchArea = Instantiate(prefab, transform);
                touchArea.name = "PreviewTouchArea";

                if (touchArea.GetComponent<SimplePreviewGestureHandle>() == null)
                    touchArea.AddComponent<SimplePreviewGestureHandle>();
            }

            RefreshClickAreaPadding();

            gestureHandle = touchArea.GetComponent<SimplePreviewGestureHandle>();
            gestureHandle.clickArea = touchArea.transform;
        }

        private void RefreshClickAreaPadding()
        {
            if (touchArea == null) return;
            RectTransform areaRect = touchArea.GetComponent<RectTransform>();
            areaRect.SetLeft(touchAreaPadding.left);
            areaRect.SetRight(touchAreaPadding.right);
            areaRect.SetTop(touchAreaPadding.top);
            areaRect.SetBottom(touchAreaPadding.bottom);
        }

        public void SetGestureEnable(bool enable)
        {
            if (gestureHandle)
            {
                gestureHandle.SetEnable(enable);
            }
        }

        public void ResetRotation()
        {
            if (gestureHandle)
            {
                gestureHandle.ResetRotation();
            }
        }
    }
}