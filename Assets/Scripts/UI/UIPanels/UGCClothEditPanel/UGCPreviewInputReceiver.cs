using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UGCPreviewInputReceiver : MonoBehaviour
{
//     public static bool locked { get; set; }
//     private bool singleTouchLocked = false;
//     private int firstTouchId;
//     private int lastTouchCount;
//     private float firstTouchTime;
//     private InputHandler handler;
//
//     private GameObject targetTouchArea;
//
// #if UNITY_EDITOR
//     private readonly float shortTouchThreshold = 1f;
// #else
//     private readonly float shortTouchThreshold = 0.3f;
// #endif
//
//     protected void Awake()
//     {
//         locked = false;
//         handler = null;
//     }
//
//     public void Init()
//     {
//     }
//
//
//     public void SetHandle(InputHandler iHandler)
//     {
//         handler = iHandler;
//     }
//
//     public void SetTouchArea(GameObject touchGo)
//     {
//         targetTouchArea = touchGo;
//     }
//
//     void Update()
//     {
//         if (locked)
//             return;
//
//         if (handler == null)
//             return;
//
//         if (lastTouchCount < Input.touchCount)
//         {
//             HandleNewTouch();
//         }
//
//         if (Input.touchCount == 1)
//         {
//             HandleSingleTouch();
//         }
//
//         if (Input.touchCount > 1)
//         {
//             HandleMultipleTouches();
//         }
//
//         HandleAllTouches();
//         lastTouchCount = Input.touchCount;
//
// #if UNITY_EDITOR
//         //  Unity鼠标滚轮模拟双指缩放,注意鼠标滑轮在Unity Game视图中才生效
//         if (Input.GetAxis("Mouse ScrollWheel") != 0)
//         {
//             handler.OnMouseScrollWheel_Unity();
//         }
// #endif
//     }
//
//     void HandleNewTouch()
//     {
//         Touch last = GetNewTouch();
//
//         
//         if (EventSystem.current == null) return;
//         
//         Debug.Log($"fsc----------------UgcClothInputHandler1----{TouchDeal(last.position)}");
//         Debug.Log($"fsc----------------UgcClothInputHandler2----{EventSystem.current.IsPointerOverGameObject(last.fingerId)}");
//
//         
//         if (EventSystem.current.IsPointerOverGameObject(last.fingerId) && !TouchDeal(last.position))
//         {
//             singleTouchLocked = true;
//             return;
//         }
//
//         singleTouchLocked = false;
//         if (Input.touchCount == 1)
//         {
//             firstTouchId = last.fingerId;
//             firstTouchTime = Time.timeSinceLevelLoad;
//         }
//
//         handler.OnTouchBegin(last);
//     }
//
//     void HandleSingleTouch()
//     {
//         if (singleTouchLocked)
//             return;
//         Touch touch = Input.GetTouch(0);
//         if (touch.phase == TouchPhase.Moved)
//         {
//             handler.OnMovementTouchStay(touch);
//         }
//
//         if (touch.fingerId == firstTouchId)
//         {
//             if (touch.phase == TouchPhase.Ended)
//             {
//                 if (Time.timeSinceLevelLoad - firstTouchTime < shortTouchThreshold)
//                 {
//                     handler.OnShortTouchEnd(touch);
//                 }
//                 else
//                 {
//                     handler.OnLongTouchEnd(touch);
//                 }
//             }
//         }
//     }
//
//     void HandleMultipleTouches()
//     {
//         if (lastTouchCount <= 1)
//         {
//             handler.OnMultipleTouchesBegin(Input.touches);
//         }
//         else
//         {
//             handler.OnMultipleTouchesStay(Input.touches);
//         }
//     }
//
//     void HandleAllTouches()
//     {
//         for (int i = 0; i < Input.touchCount; ++i)
//         {
//             handler.OnTouchStay(Input.GetTouch(i));
//         }
//     }
//
//     Touch GetNewTouch()
//     {
//         for (int i = 0; i < Input.touchCount; ++i)
//         {
//             if (Input.GetTouch(i).phase == TouchPhase.Began)
//             {
//                 return Input.GetTouch(i);
//             }
//         }
//
//         return default;
//     }
//
//
//     //碰撞到的UI判断
//     bool TouchDeal(Vector2 touchPosition)
//     {
//         PointerEventData pointer = new PointerEventData(EventSystem.current);
//         pointer.position = new Vector2(touchPosition.x, touchPosition.y);
//
//         List<RaycastResult> results = new List<RaycastResult>();
//         EventSystem.current.RaycastAll(pointer, results);
//         foreach (RaycastResult result in results)
//         {
//             if (targetTouchArea != null && result.gameObject == targetTouchArea)
//             {
//                 return true;
//             }
//         }
//
//         return false;
//     }


    public static bool locked { get; set; }
    private bool singleTouchLocked = false;
    private int firstTouchId;
    private int lastTouchCount;
    private float firstTouchTime;
    private InputHandler handler;
    private readonly float shortTouchThreshold = 0.2f;

#if UNITY_EDITOR
    // 编辑器鼠标状态追踪（用于模拟单指触摸）
    private bool _editorMouseLocked;
    private float _editorMouseDownTime;
    private Vector2 _editorLastMousePos;
#endif

    protected void Awake()
    {
        locked = false;
        handler = null;
    }

    public void SetHandle(InputHandler iHandler)
    {
        handler = null;
        handler = iHandler;
    }

    void Update()
    {
        if (locked)
            return;

#if UNITY_EDITOR
        // 鼠标左键模拟单指点击：必须先于 handler 空检查执行
        HandleEditorMouseClick();
#endif

        if (handler == null)
            return;


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
        Touch last = GetNewTouch();
        singleTouchLocked = false;
        if (Input.touchCount == 1)
        {
            firstTouchId = last.fingerId;
            firstTouchTime = Time.timeSinceLevelLoad;
        }

        handler.OnTouchBegin(last);
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
        for (int i = 0; i < Input.touchCount; ++i)
        {
            handler.OnTouchStay(Input.GetTouch(i));
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

#if UNITY_EDITOR
    /// <summary>
    /// 编辑器专用：用鼠标左键模拟单指触摸，
    /// 支持 OnTouchBegin / OnMovementTouchStay / OnTouchStay / OnShortTouchEnd / OnLongTouchEnd。
    /// 有真实触摸输入时自动跳过，避免冲突。
    /// </summary>
    private void HandleEditorMouseClick()
    {
        // 有真实触摸输入时不进行模拟，避免冲突
        if (Input.touchCount > 0)
            return;

        Vector2 mousePos = Input.mousePosition;

        if (Input.GetMouseButtonDown(0))
        {
            _editorMouseLocked = false;
            _editorMouseDownTime = Time.timeSinceLevelLoad;
            _editorLastMousePos = mousePos;

            Touch fakeTouch = new Touch();
            fakeTouch.fingerId = -1;
            fakeTouch.position = mousePos;
            fakeTouch.rawPosition = mousePos;
            fakeTouch.phase = TouchPhase.Began;
            fakeTouch.deltaPosition = Vector2.zero;
            fakeTouch.deltaTime = 0f;
            fakeTouch.tapCount = 1;
            handler?.OnTouchBegin(fakeTouch);
        }
        else if (Input.GetMouseButton(0) && !_editorMouseLocked)
        {
            Vector2 delta = mousePos - _editorLastMousePos;
            Touch fakeTouch = new Touch();
            fakeTouch.fingerId = -1;
            fakeTouch.position = mousePos;
            fakeTouch.rawPosition = mousePos;
            fakeTouch.deltaPosition = delta;
            fakeTouch.phase = delta.sqrMagnitude > 0.01f ? TouchPhase.Moved : TouchPhase.Stationary;
            fakeTouch.deltaTime = Time.deltaTime;

            if (fakeTouch.phase == TouchPhase.Moved)
            {
                handler?.OnMovementTouchStay(fakeTouch);
            }

            handler?.OnTouchStay(fakeTouch);
            _editorLastMousePos = mousePos;
        }
        else if (Input.GetMouseButtonUp(0) && !_editorMouseLocked)
        {
            Touch fakeTouch = new Touch();
            fakeTouch.fingerId = -1;
            fakeTouch.position = mousePos;
            fakeTouch.rawPosition = mousePos;
            fakeTouch.deltaPosition = mousePos - _editorLastMousePos;
            fakeTouch.phase = TouchPhase.Ended;
            fakeTouch.deltaTime = Time.deltaTime;

            if (Time.timeSinceLevelLoad - _editorMouseDownTime < shortTouchThreshold)
            {
                handler?.OnShortTouchEnd(fakeTouch);
            }
            else
            {
                handler?.OnLongTouchEnd(fakeTouch);
            }
        }
    }
#endif
}