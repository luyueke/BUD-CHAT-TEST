using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI {

    [DefaultExecutionOrder(5)]
    public class PreviewCameraHandler : MonoBehaviour {
        private bool singleTouchLocked = false;
        private int firstTouchId;
        private int lastTouchCount;
        private float firstTouchTime;
        private GameObject cameraTarget;

#if UNITY_EDITOR
        private readonly float shortTouchThreshold = 1f;
#else
        private readonly float shortTouchThreshold = 0.3f;
#endif


        public void Init() {

        }


        void Update() {

            GameObject selectObj = EventSystem.current.currentSelectedGameObject;
            if (selectObj != gameObject)
            {
                return;
            }
            
            if (InputReceiver.locked)
            {
                return;
            }

            if (lastTouchCount < Input.touchCount) {
                HandleNewTouch();
            }

            if (Input.touchCount == 1) {
                HandleSingleTouch();
            }

            if (Input.touchCount > 1) {
                HandleMultipleTouches();
            }

            HandleAllTouches();
            lastTouchCount = Input.touchCount;


#if UNITY_EDITOR
            //  Unity鼠标滚轮模拟双指缩放,注意鼠标滑轮在Unity Game视图中才生效
            if (Input.GetAxis("Mouse ScrollWheel") != 0) {
                OnMouseScrollWheel_Unity();
            }
#endif
        }




        void HandleNewTouch() {
            Touch last = GetNewTouch();
            if (EventSystem.current == null) return;
            // /*
            //  * 双指操作(边移动摇杆边处理新的点击事件)时，对点击事件进行约束
            //  * 1.点中的是ui
            //  * 2.且ui不在摇杆范围内（JoyStick的预制体默认未勾选RaycastTarget,如果点击时按钮在摇杆下层，则不处理）
            //  * 3.未在射击状态下
            //  */
            // if (EventSystem.current.IsPointerOverGameObject(last.fingerId)) {
            //     singleTouchLocked = true;
            //     return;
            // }

            singleTouchLocked = false;
            if (Input.touchCount == 1) {
                firstTouchId = last.fingerId;
                firstTouchTime = Time.timeSinceLevelLoad;
            }

            OnTouchBegin(last);
        }




        void HandleSingleTouch() {
            if (singleTouchLocked)
                return;
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Moved) {
                OnMovementTouchStay(touch);
            }

            if (touch.fingerId == firstTouchId) {
                if (touch.phase == TouchPhase.Ended) {
                    if (Time.timeSinceLevelLoad - firstTouchTime < shortTouchThreshold) {
                        OnShortTouchEnd(touch);
                    } else {
                        OnLongTouchEnd(touch);
                    }
                }
            }
        }




        void HandleMultipleTouches() {
            if (lastTouchCount <= 1) {
                OnMultipleTouchesBegin(Input.touches);
            } else {
                OnMultipleTouchesStay(Input.touches);
            }
        }


        void HandleAllTouches() {
            for (int i = 0; i < Input.touchCount; ++i) {
                OnTouchStay(Input.GetTouch(i));
            }
        }




        Touch GetNewTouch() {
            for (int i = 0; i < Input.touchCount; ++i) {
                if (Input.GetTouch(i).phase == TouchPhase.Began) {
                    return Input.GetTouch(i);
                }
            }

            return default;
        }





        protected float lastTouchSpan;
        protected Vector2 lastCenter;
        protected InputHandler.MultiGesture gesture;

        public float moveSpeed = 5f;
        public float rotateSpeed = 0.02f;
        public float panSpeed = 0.005f;

        public float clampAngle = 88f;
        public float maxRange = 2f;
        public float maxDist = 10f;
        public float minDist = 0.2f;

        private Vector3 origPos;

        private Vector3 rotEuler;

        [SerializeField]
        private Vector3 focusPos = new Vector3(0, 0, 20);

        [SerializeField]
        private float focusSize = 5;




        public void SetTarget(GameObject target) {
            cameraTarget = target;
            origPos = cameraTarget.transform.position;
            rotEuler = cameraTarget.transform.eulerAngles;
        }

        public void SetFocus() {
            if (cameraTarget == null)
            {
                return;
            }
            cameraTarget.transform.localPosition = focusPos;
            var bounds = cameraTarget.GetBounds(true);
            var maxSize = Mathf.Max(bounds.size.x,  bounds.size.y, bounds.size.z);
            if (maxSize > float.Epsilon)
            {
                cameraTarget.transform.localScale = Vector3.one *(5 / maxSize);
            }
        }




        public void OnMovementTouchStay(Touch touch) {
            OnRotateTarget(touch.deltaPosition);
        }

        public void OnMultipleTouchesBegin(Touch[] touches) {
            lastCenter = GetCenter(touches);
            lastTouchSpan = GetTouchSpan(touches);
            gesture = touches.Length == 2 ? InputHandler.MultiGesture.TwoFingers : InputHandler.MultiGesture.MoreFingers;
        }

        public void OnMultipleTouchesStay(Touch[] touches) {
            if (gesture == InputHandler.MultiGesture.TwoFingers) {
                float angle = DeltaAngle(touches[0], touches[1]);
                if (angle == 0f)
                    return;
                gesture = angle < 90f ? InputHandler.MultiGesture.Move : InputHandler.MultiGesture.Span;
            }

            if (gesture == InputHandler.MultiGesture.Span) {
                float newTouchSpan = GetTouchSpan(touches);
                OnPinch(newTouchSpan);
                lastTouchSpan = newTouchSpan;
            } else if (gesture == InputHandler.MultiGesture.Move) {
                Vector2 newCenter = GetCenter(touches);
                OnMove(newCenter);
                lastCenter = newCenter;
            }
        }

        protected virtual void OnRotateTarget(Vector2 deltaPosition) {
            if (cameraTarget != null) {
                cameraTarget.transform.Rotate(-Vector3.up * deltaPosition.x * rotateSpeed, Space.World);
                cameraTarget.transform.Rotate(-Vector3.left * deltaPosition.y * rotateSpeed, Space.World);
            }
        }

        protected virtual void OnPinch(float touchSpan) {
            if (cameraTarget != null) {
                float delta = touchSpan - lastTouchSpan;
                cameraTarget.transform.localScale += Vector3.one * delta * panSpeed;
            }
        }

        protected virtual void OnMove(Vector2 center) {

        }


        private Vector2 GetCenter(Touch[] touches) {
            Vector2 mid = Vector2.zero;
            for (int i = 0; i < touches.Length; ++i) {
                mid += touches[i].position;
            }

            return mid / touches.Length;
        }

        private float GetTouchSpan(Touch[] touches) {
            Vector2 mid = GetCenter(touches);

            float dist = 0f;

            for (int i = 0; i < touches.Length; ++i) {
                dist += Vector2.Distance(mid, touches[i].position);
            }

            return dist / touches.Length;
        }

        private float DeltaAngle(Touch one, Touch other) {
            return Mathf.Abs(Vector2.SignedAngle(one.deltaPosition, other.deltaPosition));
        }

        private void OnShortTouchEnd(Touch touch) {

        }

        private void OnTouchStay(Touch touch) {

        }

        private void OnLongTouchEnd(Touch touch) {
        }

        private void OnTouchBegin(Touch last) {
        }

        private void OnMouseScrollWheel_Unity() {
        }
    }
}
