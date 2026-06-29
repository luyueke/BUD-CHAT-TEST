using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Game.Config;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Avatar
{

    [Serializable]
    public class AvatarGestureConfig
    {
        public int gestureType;
        public int gestureDir;
        public float maxScale;
        public float minScale;
        public float zoomSpeed;
        public float vecMoveSpeed;
        public float MaxYK;
        public float MaxYB;
        public float MinYK;
        public float MinYB;
        public int standardZ;
    }

    public enum GestureType
    {
        FittingRoom,
    }

    public enum GestureDirType
    {
        Front = 1,
        Back = -1,
    }

    public class CommonAvatarGestureHandler : UIDragUtil
    {
        public Camera roleCamera;
        public Transform MoveRoot;

        [SerializeField] internal float maxScale = 0.2f;
        [SerializeField] internal float minScale = 1f;
        [SerializeField] internal float zoomSpeed = 3f;
        [SerializeField] internal float vecMoveSpeed = 0.001f;
        [SerializeField] internal float minY = -0.5f;
        [SerializeField] internal float maxY = 0.5f;

        private Canvas uiCanvas;
        private float lastTouchSpan;
        protected MultiGesture _gestureType;
        private int gestureDir = 1;

        public bool isZoomEnabled = true;
        public bool isMoveEnabled = true;

        private float scaleProgress => Mathf.Max(0, Mathf.Min(1, minScale) * ResolutionAutoFit.CameraScale - roleCamera.orthographicSize) / ((Mathf.Min(1, minScale) - maxScale) * ResolutionAutoFit.CameraScale);

        protected virtual void Awake()
        {
            uiCanvas = GameObject.Find("Canvas").GetComponent<Canvas>();
            roleCamera.orthographicSize *= ResolutionAutoFit.CameraScale;
        }

        public void OnRelease()
        {

        }

        private bool CheckCanTouch()
        {
            GameObject selectObj = EventSystem.current.currentSelectedGameObject;
            if (roleCamera != null && selectObj != null && selectObj.name == "ClickArea")
            {
                return true;
            }
            return false;
        }

        public override void OnMovementTouchStay(Touch touch)
        {
            if (!CheckCanTouch())
                return;

            //处理上下移动
            OnDragAndVerticalMove(touch);
        }

        void OnDragAndVerticalMove(Touch touch)
        {
            if (!isMoveEnabled) {
                return;
            }
            //移动差值
            Vector2 move = vecMoveSpeed * touch.deltaPosition;
            //原始位置
            var oriPos = MoveRoot.localPosition;
            var targetPosY = oriPos.y - move.y;
            var limitedy = Mathf.Clamp(targetPosY, minY * scaleProgress, maxY * scaleProgress);
            MoveRoot.localPosition = new Vector3(MoveRoot.localPosition.x, limitedy, MoveRoot.localPosition.z);
        }

        public override void OnMultipleTouchesBegin(Touch[] touches)
        {
            if (!CheckCanTouch())
                return;

            lastTouchSpan = GetTouchSpan(touches);
            _gestureType = touches.Length == 2 ? MultiGesture.TwoFingers : MultiGesture.MoreFingers;
        }

        public override void OnMultipleTouchesStay(Touch[] touches)
        {
            if (!CheckCanTouch())
                return;

            if (touches.Length == 2)
            {
                float angle = DeltaAngle(touches[0], touches[1]);
                if (angle == 0f)
                    return;
                _gestureType = angle < 90f ? MultiGesture.Move : MultiGesture.Span;
            }
            if (_gestureType == MultiGesture.Span)
            {
                float newTouchSpan = GetTouchSpan(touches);
                OnPinch(newTouchSpan);
            }
        }

        public override void OnMouseScrollWheel_Unity()
        {
            _gestureType = MultiGesture.TwoFingers;
            var zoom = (Input.GetAxis("Mouse ScrollWheel") > 0 ? 1f : -1f) * zoomSpeed * GameConsts.TimeScale;
            OnZoom(zoom);
        }

        Vector2 GetCenter(Touch[] touches)
        {
            Vector2 mid = Vector2.zero;
            for (int i = 0; i < touches.Length; ++i)
            {
                var touchPos = touches[i].position;
                Vector2 outVec;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(uiCanvas.transform as RectTransform, touchPos, uiCanvas.worldCamera, out outVec))
                {
                    mid += outVec;
                }
            }
            return mid / touches.Length;
        }

        float GetTouchSpan(Touch[] touches)
        {
            Vector2 mid = GetCenter(touches);

            float dist = 0f;

            for (int i = 0; i < touches.Length; ++i)
            {
                var touchPos = touches[i].position;
                Vector2 outVec;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(uiCanvas.transform as RectTransform, touchPos, uiCanvas.worldCamera, out outVec))
                {
                    dist += Vector2.Distance(mid, outVec);
                }
            }
            return dist / touches.Length;
        }

        private float DeltaAngle(Touch one, Touch other)
        {
            return Mathf.Abs(Vector2.SignedAngle(one.deltaPosition, other.deltaPosition));
        }

        void OnPinch(float newTouchSpan)
        {
            LoggerUtils.Log(newTouchSpan);
            var scaleDir = (newTouchSpan - lastTouchSpan) >= 0 ? gestureDir * -1 : gestureDir * 1;
            float zoom = scaleDir * Time.deltaTime * zoomSpeed;
            lastTouchSpan = newTouchSpan;

            OnZoom(zoom);
        }

        void OnZoom(float zoom)
        {
            if (!isZoomEnabled) {
                return;
            }
            var oriScale = roleCamera.orthographicSize;
            var finalScale = oriScale + zoom;
            finalScale = Mathf.Clamp(finalScale, maxScale * ResolutionAutoFit.CameraScale, minScale * ResolutionAutoFit.CameraScale);
            roleCamera.orthographicSize = finalScale;

            if (!isMoveEnabled)
            {
                return;
            }

            var limitedy = Mathf.Clamp(MoveRoot.localPosition.y, minY * scaleProgress, maxY * scaleProgress);
            MoveRoot.localPosition = new Vector3(MoveRoot.localPosition.x, limitedy, MoveRoot.localPosition.z);
        }
    }
}
