using Game.Config;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UGCAnimTimeLineGestureHandler : UIDragUtil
{
    public ScrollRect TimeLineSc;
    public TimeLineController TimeLineCtr;
    [SerializeField] internal float maxScale = 0.2f;
    [SerializeField] internal float minScale = 1f;
    [SerializeField] internal float zoomSpeed = 3f;

    private Canvas uiCanvas;
    private float lastTouchSpan;
    protected MultiGesture _gestureType;
    private int gestureDir = 1;

    protected virtual void Awake()
    {
        uiCanvas = GameObject.Find("Canvas").GetComponent<Canvas>();
    }

    private bool CheckCanTouch()
    {
        return true;
        
        GameObject selectObj = EventSystem.current.currentSelectedGameObject;
        if (selectObj != null && selectObj.name == "ClickArea")
        {
            return true;
        }

        return false;
    }

    public override void OnTouchBegin(Touch touch)
    {
        base.OnTouchBegin(touch);
        TimeLineSc.enabled = true;
    }

    public override void OnMultipleTouchesBegin(Touch[] touches)
    {
        if (!CheckCanTouch())
            return;
        
        TimeLineSc.enabled = false;
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
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(uiCanvas.transform as RectTransform, touchPos,
                    uiCanvas.worldCamera, out outVec))
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
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(uiCanvas.transform as RectTransform, touchPos,
                    uiCanvas.worldCamera, out outVec))
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
        LoggerUtils.LogError(zoom);

        bool isZoomIn = zoom < 0;
        TimeLineCtr.ZoomTimeLine(isZoomIn);
        // var oriScale = roleCamera.orthographicSize;
        // var finalScale = oriScale + zoom;
        // finalScale = Mathf.Clamp(finalScale, maxScale * ResolutionAutoFit.CameraScale,
        //     minScale * ResolutionAutoFit.CameraScale);
    }
}