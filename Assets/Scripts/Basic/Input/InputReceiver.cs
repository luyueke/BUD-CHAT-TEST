using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class InputReceiver :InstMonoBehaviour<InputReceiver>
{
    public static bool locked { get; set; }
    private bool singleTouchLocked = false;
    private int firstTouchId;
    private int lastTouchCount;
    private float firstTouchTime;
    private InputHandler handler;
    private static readonly List<RaycastResult> RaycastResults = new List<RaycastResult>(32);

#if UNITY_EDITOR
    private readonly float shortTouchThreshold = 1f;
#else
    private readonly float shortTouchThreshold = 0.3f;
#endif

    private void Awake()
    {
        if (!DontDestroyUtils.IsContains(this.gameObject))
        {
            this.gameObject.DontDestroy();
        }
        locked = false;
        handler = null;
    }

    public void Init()
    {

    }


    public void SetHandle(InputHandler iHandler)
    {
        handler = iHandler;
    }

    void Update()
    {
        if (locked)
            return;

        if (handler == null)
            return;

        if (lastTouchCount < Input.touchCount)
        {
            HandleNewTouch();
        }

        if(Input.touchCount == 1)
        {
            HandleSingleTouch();
        }

        if(Input.touchCount > 1)
        {
            HandleMultipleTouches();
        }
        HandleAllTouches();
        lastTouchCount = Input.touchCount;


#if UNITY_EDITOR
        //  Unity鼠标滚轮模拟双指缩放,注意鼠标滑轮在Unity Game视图中才生效
        if (Input.GetAxis("Mouse ScrollWheel") != 0)
        {
            handler.OnMouseScrollWheel_Unity();
        }
#endif
    }

    void HandleNewTouch()
    {
        if (!TryGetNewTouch(out var last))
        {
            return;
        }
        if(EventSystem.current==null)return;
        /*
         * 双指操作(边移动摇杆边处理新的点击事件)时，对点击事件进行约束
         * 1.点中的是ui
         * 2.且ui不在摇杆范围内（JoyStick的预制体默认未勾选RaycastTarget,如果点击时按钮在摇杆下层，则不处理）
         * 3.未在射击状态下
         */


        if (IsPointerOverBlockingUI(last, handler))
        {
            //LoggerUtils.Log("InputReceiver IsPointerOverGameObject:" + EventSystem.current.currentInputModule);
            singleTouchLocked = true;
            return;
        }

        singleTouchLocked = false;
        if (Input.touchCount == 1)
        {
            firstTouchId = last.fingerId;
            firstTouchTime = Time.timeSinceLevelLoad;
        }

        handler.OnTouchBegin(last);
    }

    private static bool IsPointerOverBlockingUI(Touch touch, InputHandler currentHandler)
    {
        if (EventSystem.current == null) return false;
        if (!EventSystem.current.IsPointerOverGameObject(touch.fingerId)) return false;

        RaycastResults.Clear();
        var ped = new PointerEventData(EventSystem.current)
        {
            position = touch.position,
            pointerId = touch.fingerId
        };
        EventSystem.current.RaycastAll(ped, RaycastResults);

        bool isCameraMode = IsCameraModeInputHandler(currentHandler);

        // passthrough 仅在 CameraModePanel 激活时存在。
        // Follow 模式使用 EditModeHandler 但 passthrough 仍覆盖全屏，
        // 如果不提前识别，所有单指触摸都会被非 camera-mode 分支直接阻塞。
        if (!isCameraMode)
        {
            for (int i = 0; i < RaycastResults.Count; i++)
            {
                var go = RaycastResults[i].gameObject;
                if (go != null && IsPassthroughUI(go))
                {
                    isCameraMode = true;
                    break;
                }
            }
        }

        for (int i = 0; i < RaycastResults.Count; i++)
        {
            var go = RaycastResults[i].gameObject;
            if (go == null) continue;

            if (isCameraMode)
            {
                if (IsPassthroughUI(go)) return false;
                if (go.GetComponent<UnityEngine.UI.Selectable>() != null) return true;
                if (go.GetComponent<IDragHandler>() != null) return true;
            }
            else
            {
                return true;
            }
        }

        return !isCameraMode;
    }

    private static bool IsCameraModeInputHandler(InputHandler currentHandler)
    {
        if (currentHandler == null) return false;
        return string.Equals(currentHandler.GetType().Name, "CameraModeHandler", System.StringComparison.Ordinal);
    }

    private static bool IsPassthroughUI(GameObject go)
    {
        for (var t = go != null ? go.transform : null; t != null; t = t.parent)
        {
            if (t.GetComponent("UICameraModeGlobalClosePassthrough") != null) return true;
        }

        return false;
    }

    void HandleSingleTouch()
    {
        if (singleTouchLocked)
            return;
        Touch touch = Input.GetTouch(0);
        if (touch.phase == TouchPhase.Moved)
        {
            handler.OnMovementTouchStay(touch);
        }

        if (touch.fingerId == firstTouchId)
        {
            if (touch.phase == TouchPhase.Ended)
            {
                if (Time.timeSinceLevelLoad - firstTouchTime < shortTouchThreshold)
                {
                    handler.OnShortTouchEnd(touch);
                }
                else
                {
                    handler.OnLongTouchEnd(touch);
                }
            }
        }
    }

    void HandleMultipleTouches()
    {
        if (lastTouchCount <= 1)
        {
            handler.OnMultipleTouchesBegin(Input.touches);
        }
        else
        {
            handler.OnMultipleTouchesStay(Input.touches);
        }
    }

    void HandleAllTouches()
    {
        for(int i = 0; i < Input.touchCount; ++i)
        {
            handler.OnTouchStay(Input.GetTouch(i));
        }
    }

    bool TryGetNewTouch(out Touch touch)
    {
        for (int i = 0; i < Input.touchCount; ++i)
        {
            if (Input.GetTouch(i).phase == TouchPhase.Began)
            {
                touch = Input.GetTouch(i);
                return true;
            }
        }
        touch = default;
        return false;
    }

}
