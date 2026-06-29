using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
public class ClickEventListener : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    Action<GameObject, PointerEventData> mClickedHandler = null;
    Action<GameObject, PointerEventData> mOnPointerDownHandler = null;
    Action<GameObject, PointerEventData> mOnPointerUpHandler = null;
    Action<GameObject, PointerEventData> mOnPointerEnterHandler = null;
    Action<GameObject, PointerEventData> mOnPointerExitHandler = null;

    bool mIsPressed = false;
    public bool IsPressd
    {
        get { return mIsPressed; }
    }

    public void AddClickEventHandler(Action<GameObject, PointerEventData> handler)
    {
        mClickedHandler += handler;
    }

    public void AddPointerDownHandler(Action<GameObject, PointerEventData> handler)
    {
        mOnPointerDownHandler += handler;
    }

    public void AddPointerUpHandler(Action<GameObject, PointerEventData> handler)
    {
        mOnPointerUpHandler += handler;
    }

    public void AddPointerEnterHandler(Action<GameObject, PointerEventData> handler)
    {
        mOnPointerEnterHandler += handler;
    }

    public void AddPointerExitHandler(Action<GameObject, PointerEventData> handler)
    {
        mOnPointerExitHandler += handler;
    }


    public void OnPointerClick(PointerEventData eventData)
    {
        if (mClickedHandler != null)
        {
            mClickedHandler(gameObject, eventData);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        mIsPressed = true;
        if (mOnPointerDownHandler != null)
        {
            mOnPointerDownHandler(gameObject, eventData);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        mIsPressed = false;
        if (mOnPointerUpHandler != null)
        {
            mOnPointerUpHandler(gameObject, eventData);
        }
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (mOnPointerEnterHandler != null)
        {
            mOnPointerEnterHandler(gameObject, eventData);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (mOnPointerExitHandler != null)
        {
            mOnPointerExitHandler(gameObject, eventData);
        }
    }

    protected virtual void OnDestroy()
    {
        mClickedHandler = null;
        mOnPointerDownHandler = null;
        mOnPointerUpHandler = null;
        mOnPointerEnterHandler = null;
        mOnPointerExitHandler = null;
    }

}