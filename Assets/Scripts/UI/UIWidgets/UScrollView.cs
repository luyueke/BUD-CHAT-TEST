/**
 * @ Author: Jun Zhou
 * @ Create Time: 2023-02-15 17:10:43
 * @ Modified by: Jun Zhou
 * @ Modified time: 2023-12-12 11:13:33
 * @ Description: 滑动组件扩展
 */

using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using System.Collections;

public class UScrollView : ScrollRect, IPointerClickHandler {
    RectTransform mScrollTransform;
    Tweener moveAnimationT;
    Action<PointerEventData> mClickedHandler = null;
    //竖直方向移动到列表尾
    Action<PointerEventData> mMoveEndHandler = null;
    public bool IsDraging {get;private set;} = false;
    public bool IsAnimMoving {get;private set;} = false;

    protected override void Awake()
    {
        mScrollTransform = GetComponent<RectTransform>();
        base.Awake();
    }

    protected override void OnDestroy()
    {
        moveAnimationT?.Kill();
        base.OnDestroy();
    }

    public void AddClickListener(Action<PointerEventData> clickAction)
    {
        mClickedHandler = clickAction;
    }
    public void AddMoveEndListener(Action<PointerEventData> clickAction)
    {
        mMoveEndHandler = clickAction;
    }

    public override void OnBeginDrag(PointerEventData data)
    {
        base.OnBeginDrag(data);
        IsDraging = true;
    }

    public override void OnDrag(PointerEventData data)
    {
        base.OnDrag(data);
    }

    public override void OnEndDrag(PointerEventData data)
    {
        base.OnEndDrag(data);
        IsDraging = false;
        if (verticalNormalizedPosition < 0.25f)
        {
            mMoveEndHandler?.Invoke(data);
        }
    }

    public void OnPointerClick(PointerEventData data)
    {
        if (!IsDraging)
        {
            Debug.Log("They started OnPointerClick " + this.name);

            mClickedHandler?.Invoke(data);
        }
    }

    public void MoveTo(int index, float normalizedOffsetFromViewportStart = 0, float normalizedPositionOfItemPivotToUse = 0)
    {
        _MoveTo(index, normalizedOffsetFromViewportStart, normalizedPositionOfItemPivotToUse, false);
    }

    public void SmoothMoveTo(int index, float duration, float normalizedOffsetFromViewportStart = 0, float normalizedPositionOfItemPivotToUse = 0)
    {
        _MoveTo(index, normalizedOffsetFromViewportStart, normalizedPositionOfItemPivotToUse, true, duration);
    }

    void _MoveTo(int index, float normalizedOffsetFromViewportStart, float normalizedPositionOfItemPivotToUse, bool isAnimation, float duration = 0)
    {
        if (index >= content.childCount) return;

        var child = content.GetChild(index);
        var childTF = child.GetComponent<RectTransform>();

        var offsetFromViewportStartPivot = new Vector2(0.5f, 0.5f);
        var positionOfItemPivot = new Vector2(0.5f, 0.5f);
        if (horizontal)
        {
            offsetFromViewportStartPivot.x = normalizedOffsetFromViewportStart;
            positionOfItemPivot.x = normalizedPositionOfItemPivotToUse;
        }
        if (vertical)
        {
            offsetFromViewportStartPivot.y = normalizedOffsetFromViewportStart;
            positionOfItemPivot.y = normalizedPositionOfItemPivotToUse;
        }
        
        var itemCenterPositionInScroll = GetWorldPointInWidget(content, GetWidgetWorldPoint(childTF, positionOfItemPivot)); // 目标Item的位置
        var targetPositionInScroll = GetWorldPointInWidget(content, GetWidgetWorldPoint(viewport, offsetFromViewportStartPivot));

        var difference = targetPositionInScroll - itemCenterPositionInScroll;
        difference.z = 0f;

        if (!horizontal)
        {
            difference.x = 0f;
        }
        if (!vertical)
        {
            difference.y = 0f;
        }

        var normalizedDifference = new Vector2(
            difference.x / (content.rect.size.x - mScrollTransform.rect.size.x),
            difference.y / (content.rect.size.y - mScrollTransform.rect.size.y));

        var newNormalizedPosition = normalizedPosition - normalizedDifference;
        if (movementType != MovementType.Unrestricted)
        {
            newNormalizedPosition.x = Mathf.Clamp01(newNormalizedPosition.x);
            newNormalizedPosition.y = Mathf.Clamp01(newNormalizedPosition.y);
        }

        moveAnimationT?.Kill();
        if (isAnimation)
        {
            IsAnimMoving = true;
            moveAnimationT = DOTween.To(() => normalizedPosition, x => normalizedPosition = x, newNormalizedPosition, duration);
            moveAnimationT.OnComplete(()=>{ StartCoroutine(SetEndAnimFlag()); });
        } else {
            normalizedPosition = newNormalizedPosition;     
        }
    }

    private IEnumerator SetEndAnimFlag()
    {
        yield return new WaitForEndOfFrame();
        IsAnimMoving = false;
    } 

    Vector3 GetWidgetWorldPoint(RectTransform target, Vector2 pivot)
    {
        var pivotOffset = new Vector3(
            (pivot.x - target.pivot.x) * target.rect.size.x,
            (pivot.y - target.pivot.y) * target.rect.size.y,
            0f);
        var localPosition = target.localPosition + pivotOffset;
        return target.parent.TransformPoint(localPosition);
    }
        
    Vector3 GetWorldPointInWidget(RectTransform target, Vector3 worldPoint)
    {
        return target.InverseTransformPoint(worldPoint);
    }
}