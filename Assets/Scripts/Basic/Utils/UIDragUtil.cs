using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIDragUtil : MonoBehaviour, IDragHandler
{
    public Transform RotateTarget { set; get; }
    public Transform RotateTarget_Double { set; get; }
    public float cameraUpDownSpeed { set; get; } = 0.3f;
    private bool singleTouchLocked = false;
    private int firstTouchId;
    private int lastTouchCount;
    private float firstTouchTime;
    private readonly float shortTouchThreshold = 0.2f;

    public void OnDrag(PointerEventData eventData)
    {
        if (RotateTarget != null)
        {
            var rot = RotateTarget.transform.localEulerAngles;
            rot.y -= eventData.delta.x * cameraUpDownSpeed;
            RotateTarget.transform.localEulerAngles = rot;
        }
        
        if (RotateTarget_Double != null)
        {
            var rot = RotateTarget_Double.transform.localEulerAngles;
            rot.y -= eventData.delta.x * cameraUpDownSpeed;
            RotateTarget_Double.transform.localEulerAngles = rot;
        }
    }

    #region 缩放和拖动
    void Update()
    {

#if UNITY_EDITOR
        //  Unity鼠标滚轮模拟双指缩放,注意鼠标滑轮在Unity Game视图中才生效
        if (Input.GetAxis("Mouse ScrollWheel") != 0)
        {
            OnMouseScrollWheel_Unity();
            return;
        }
#endif

        GameObject selectObj = EventSystem.current.currentSelectedGameObject;
        if (selectObj != gameObject)
        {
            return;
        }

        if (lastTouchCount < Input.touchCount)
        {
            HandleNewTouch();
        }

        if (Input.touchCount == 1)
        {
            HandleSingleTouch();
        }

        if (Input.touchCount > 1)
        {
            HandleMultipleTouches();
        }
        HandleAllTouches();
        lastTouchCount = Input.touchCount;
    }

    void HandleNewTouch()
    {
        Touch last = GetNewTouch();
        singleTouchLocked = false;
        if (Input.touchCount == 1)
        {
            firstTouchId = last.fingerId;
            firstTouchTime = Time.timeSinceLevelLoad;
        }

        OnTouchBegin(last);
    }

    void HandleSingleTouch()
    {
        if (singleTouchLocked)
            return;
        Touch touch = Input.GetTouch(0);
        if (touch.phase == TouchPhase.Moved)
        {
            OnMovementTouchStay(touch);
        }

        if (touch.fingerId == firstTouchId)
        {
            if (touch.phase == TouchPhase.Ended)
            {
                if (Time.timeSinceLevelLoad - firstTouchTime < shortTouchThreshold)
                {
                    OnShortTouchEnd(touch);
                }
                else
                {
                    OnLongTouchEnd(touch);
                }
            }
        }
    }

    void HandleMultipleTouches()
    {
        if (lastTouchCount <= 1)
        {
            OnMultipleTouchesBegin(Input.touches);
        }
        else
        {
            OnMultipleTouchesStay(Input.touches);
        }
    }

    void HandleAllTouches()
    {
        for (int i = 0; i < Input.touchCount; ++i)
        {
            OnTouchStay(Input.GetTouch(i));
        }
    }

    Touch GetNewTouch()
    {
        for (int i = 0; i < Input.touchCount; ++i)
        {
            if (Input.GetTouch(i).phase == TouchPhase.Began)
            {
                return Input.GetTouch(i);
            }
        }
        return default;
    }
    #endregion

    public enum MultiGesture
    {
        MoreFingers,
        TwoFingers,
        Span,
        Move
    }

    public virtual void OnTouchBegin(Touch touch) { }

    public virtual void OnShortTouchEnd(Touch touch) { }

    public virtual void OnTouchStay(Touch touch) { }

    public virtual void OnLongTouchEnd(Touch touch) { }

    public virtual void OnMovementTouchStay(Touch touch) { }

    public virtual void OnMultipleTouchesBegin(Touch[] touches) { }

    public virtual void OnMultipleTouchesStay(Touch[] touches) { }

    public virtual bool OnDragJoyStick(Touch touche)
    {
        return false;
    }

    public virtual void OnMouseScrollWheel_Unity() { }
}