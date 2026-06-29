using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UGCCanvasZoomHandler : InputHandler
{
    private float lastTouchSpan;
    private float maxScale = 2f;
    private float minScale = 1f;
    private float zoomSpeed = 1.5f;

    private Vector2 oriSize;
    private float moveSpeed = 500f;
    private Vector2 lastCenter;
    private GameObject drawBoard;
    private Canvas uiCanvas;
    private RectTransform rectComp;
    private RectTransform dragRect;
    private bool isCanZoom = false;
    private Action<float> zoomScaleAction;
    private Action zoomMoveAction;

    protected enum MultiGesture
    {
        MoreFingers,
        TwoFingers,
        Span,
        Move
    }

    protected MultiGesture gesture;

    public void SetData(Canvas canvas, GameObject obj)
    {
        drawBoard = obj;
        uiCanvas = canvas;
        rectComp = drawBoard.GetComponent<RectTransform>();
        dragRect = drawBoard.transform.parent.GetComponent<RectTransform>();
        oriSize = rectComp.sizeDelta;
    }

    public override void OnMultipleTouchesBegin(Touch[] touches)
    {
        isCanZoom = false;
        if(!CheckCanTouch(touches))
            return;
        isCanZoom = true;
        lastCenter = GetCenter(touches);
        lastTouchSpan = GetTouchSpan(touches);
        gesture = touches.Length == 2 ? MultiGesture.TwoFingers : MultiGesture.MoreFingers;
    }

    public override void OnMultipleTouchesStay(Touch[] touches)
    {
        if(!isCanZoom)
            return;
        
        if(touches.Length == 2)
        {
            float angle = DeltaAngle(touches[0], touches[1]);
            if (angle == 0f)
                return;
            gesture = angle < 90f ? MultiGesture.Move : MultiGesture.Span;
        }
        if (gesture == MultiGesture.Span)
        {
            float newTouchSpan = GetTouchSpan(touches);
            OnPinch(newTouchSpan);
        }
        else if (gesture == MultiGesture.Move)
        {
            Vector2 newCenter = GetCenter(touches);
            OnMove(newCenter);
            zoomMoveAction ?.Invoke();
        }
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
        var scaleDir = (newTouchSpan - lastTouchSpan) >= 0 ? 1 : -1;
        float zoom = scaleDir * Time.deltaTime * zoomSpeed;
        var dirScale = zoom * Vector3.one; 
        var oriScale = rectComp.localScale;
        var finalScale = oriScale + dirScale;

        if (finalScale.x > maxScale)
        {
            finalScale = Vector3.one * maxScale;
            lastTouchSpan = maxScale;
        }
        else if (finalScale.x < minScale)
        {
            finalScale = Vector3.one * minScale;
            lastTouchSpan = minScale;
        }

        rectComp.localScale = finalScale;
        lastTouchSpan = newTouchSpan;
        OnMove(lastCenter);
        zoomScaleAction?.Invoke(finalScale.x);
    }

    void OnMove(Vector2 newCenter)
    {
        Vector2 dirVec = (newCenter - lastCenter).normalized;
        var offset = dirVec * Time.deltaTime * moveSpeed;
        var oriPos = rectComp.anchoredPosition;
        var dirPos = Vector3.one * offset;
        var finalPos = oriPos + dirPos;
        finalPos = LimitedPosition(finalPos);
        rectComp.anchoredPosition = finalPos;
        lastCenter = newCenter;
    }

    private Vector3 LimitedPosition(Vector3 currentPosition)
    {
        Vector3 limited = currentPosition;
        var curMaxWidth = oriSize.x * rectComp.localScale.x - oriSize.x;
        var curMaxHeigth = oriSize.y * rectComp.localScale.y - oriSize.y;

        if (currentPosition.x >= curMaxWidth / 2)
        {
            limited.x = curMaxWidth / 2;
        }
        else if (currentPosition.x < -curMaxWidth / 2)
        {
            limited.x = -curMaxWidth / 2;
        }

        if (currentPosition.y >= curMaxHeigth / 2)
        {
            limited.y = curMaxHeigth / 2;
        }
        else if (currentPosition.y < -curMaxHeigth / 2)
        {
            limited.y = -curMaxHeigth / 2;
        }
        return limited;
    }

    public void SetZoomScale(float scale)
    {
        var finalScale = Vector3.one * scale;
        if (finalScale.x > maxScale)
        {
            finalScale = Vector3.one * maxScale;
            // lastTouchSpan = maxScale;
        }
        else if (finalScale.x < minScale)
        {
            finalScale = Vector3.one * minScale;
            // lastTouchSpan = minScale;
        }

        rectComp.localScale = finalScale;
        OnMove(lastCenter);
    }

    public void AddZoomScaleListener(Action<float> action)
    {
        zoomScaleAction += action;
    }

    public void AddZoomMoveListener(Action moveAction)
    {
        zoomMoveAction += moveAction;
    }

    public override void OnTouchBegin(Touch touch)
    {
        // Debug.Log($"fsc-----------UGCCanvasZoomHandler--------OnTouchBegin----{TouchDeal(touch.position)}");
    }

    private const string LeftClickAreaName = "ClothModelClickArea";
    
    //碰撞到的UI判断
    // bool TouchDeal(Vector2 touchPosition)
    // {
    //     PointerEventData pointer = new PointerEventData(EventSystem.current);
    //     pointer.position = new Vector2(touchPosition.x, touchPosition.y);
    //
    //     List<RaycastResult> results = new List<RaycastResult>();
    //     EventSystem.current.RaycastAll(pointer, results);
    //     foreach (RaycastResult result in results)
    //     {
    //         // Debug.Log($"fsc-------------UGCCanvasZoomHandler-------result------{result.gameObject.name}");
    //
    //         if (result.gameObject.name == LeftClickAreaName)
    //         {
    //             return false;
    //         }
    //     }
    //
    //     return true;
    // }
    
    #region 范围检查
    

    private bool CheckCanTouch(Touch[] touchs)
    {
        for (int i = 0; i < touchs.Length; i++)
        {
            if (!RectTransformUtility.RectangleContainsScreenPoint(dragRect, touchs[i].position,
                    uiCanvas.worldCamera))
            {
                return false;
            }
        }
        return true;
    }

    #endregion
}
