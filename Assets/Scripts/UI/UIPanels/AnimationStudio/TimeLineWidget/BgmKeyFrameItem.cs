using System;
using System.Collections;
using System.Collections.Generic;
using GameData.BaseInfo;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BgmKeyFrameItem : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler, IBeginDragHandler, IEndDragHandler, IInitializePotentialDragHandler
{
    public CButton Btn_Select;
    public Text Txt_Title;
    public GameObject Go_Selected;
    public RectTransform RectTrans;
    public Image Img_Bg;

    public RectTransform RectChild_1;
    public RectTransform RectChild_2;
    public RectTransform RectChild_3;
    [HideInInspector]
    public int trackId;
    [HideInInspector]
    public AnimMusicInfo animMusicInfo;
    
    private List<KeyFrameItem> _curKeyFrameItems = new List<KeyFrameItem>();
    private float pressStartTime;
    private bool isDragging = false;
    private bool isLongPress = false;
    private bool isTouch = false;
    private Action<BgmKeyFrameItem> _onStopDraggingAct;
    private Action<BgmKeyFrameItem> _onSelectAct;
    
    // 长按检测时间
    private float longPressDuration = 0.5f;

    // 是否被选中
    private bool isSelected = false;

    // 父级 ScrollRect
    private ScrollRect parentScrollRect;
    private Action<PointerEventData> _onScvDragAct;
    private Action<PointerEventData> _onEndDragAct;
    
    //ZoomData
    private const float OriPreSecondWidth = 74f;
    private float PreSecondWidth = 74f;
    private const float OriChildPaddingWidth = 37f;
    private float ChildPaddingWidth = 37f;
    private float Spacing = 10f;
    private const int MaxZoomLevel = 18;
    private const int MinZoomLevel = 1;
    private const int DeltZoomW = 20;
    private const int DeltZoomPadding = 10;
    private int _curZoomLevel = 1;

    // 新增：拖动时的偏移量
    private Vector2 dragOffset;

    private void Awake()
    {
        Btn_Select.onClick.AddListener(OnSelect);
    }
    
    public void SetAction(Action<BgmKeyFrameItem> onStopDraggingAct, Action<BgmKeyFrameItem> onSelectAct)
    {
        this._onStopDraggingAct = onStopDraggingAct;
        this._onSelectAct = onSelectAct;
    }

    public void SetParentScvData(ScrollRect scrollRect, Action<PointerEventData> onDragAct, Action<PointerEventData> onEndDragAct)
    {
        parentScrollRect = scrollRect;
        this._onScvDragAct = onDragAct;
        this._onEndDragAct = onEndDragAct;
    }

    private void OnParentScvBeginDrag(PointerEventData eventData)
    {
        parentScrollRect.OnBeginDrag(eventData);
        this._onScvDragAct?.Invoke(eventData);
    }
    
    private void OnParentScvOnDrag(PointerEventData eventData)
    {
        parentScrollRect.OnDrag(eventData);
        this._onScvDragAct?.Invoke(eventData);
    }
    
    private void OnParentScvEndDrag(PointerEventData eventData)
    {
        parentScrollRect.OnEndDrag(eventData);
        this._onEndDragAct?.Invoke(eventData);
    }

    public void SetData(int trackId, AnimMusicInfo animMusicInfo, Color bgColor, int zoomLevel = 1)
    {
        this.trackId = trackId;
        this.animMusicInfo = animMusicInfo;
        this.Txt_Title.SetLocalText(this.animMusicInfo.name);
        this.Img_Bg.color = bgColor;
        _curZoomLevel = zoomLevel;
        PreSecondWidth = OriPreSecondWidth + (_curZoomLevel - 1) * DeltZoomW;
        ChildPaddingWidth = OriChildPaddingWidth + (_curZoomLevel - 1) * DeltZoomPadding;
        
        var length = this.animMusicInfo.frameLen;
        SetLength(length);
    }

    private void SetLength(int length)
    {
        var newWidth = PreSecondWidth * (length + 1) + (length * Spacing);
        this.RectTrans.sizeDelta = new Vector2(newWidth, this.RectTrans.sizeDelta.y);
        
        RectChild_1.offsetMin = new Vector2(ChildPaddingWidth, 0);
        RectChild_1.offsetMax = new Vector2(-ChildPaddingWidth, 0);
        RectChild_2.offsetMin = new Vector2(ChildPaddingWidth, 0);
        RectChild_2.offsetMax = new Vector2(-ChildPaddingWidth, 0);
        RectChild_3.offsetMin = new Vector2(ChildPaddingWidth, 0);
        RectChild_3.offsetMax = new Vector2(-ChildPaddingWidth, 0);
    }

    public void SetPos(Vector2 pos)
    {
        this.RectTrans.anchoredPosition = pos;
    }

    public void SetSelectState(bool isSelected)
    {
        Go_Selected.SetActive(isSelected);
        this.isSelected = isSelected;
    }
    
    private void OnSelect()
    {
        this._onSelectAct?.Invoke(this);
    }

    public void Zoom(bool IsZoomIn)
    {
        if (IsZoomIn)
        {
            ZoomIn();
        }
        else
        {     
            ZoomOut();
        }
    }
    
    private void ZoomOut() 
    {
        if(_curZoomLevel <= MinZoomLevel)
            return;
        
        _curZoomLevel--;
        PreSecondWidth = OriPreSecondWidth + (_curZoomLevel - 1) * DeltZoomW;
        ChildPaddingWidth = OriChildPaddingWidth + (_curZoomLevel - 1) * DeltZoomPadding;
        var length = this.animMusicInfo.frameLen;
        SetLength(length);
    }

    private void ZoomIn()
    {
        if(_curZoomLevel >= MaxZoomLevel)
            return;
        
        _curZoomLevel++;
        PreSecondWidth = OriPreSecondWidth + (_curZoomLevel - 1) * DeltZoomW;
        ChildPaddingWidth = OriChildPaddingWidth + (_curZoomLevel - 1) * DeltZoomPadding;
        var length = this.animMusicInfo.frameLen;
        SetLength(length);
    }

    #region 拖动相关操作

    // 按下时开始计时
    public void OnPointerDown(PointerEventData eventData)
    {
        pressStartTime = Time.time;
        isLongPress = false;
        isDragging = false;
        isTouch = true;

        // 记录开始按下的位置
        // 当未选中时，需要将事件传递给父级 ScrollRect
        if (!isSelected)
        {
            // 通知父级开始处理拖动事件
            OnParentScvBeginDrag(eventData);
        }
    }

    // 抬起时停止计时
    public void OnPointerUp(PointerEventData eventData)
    {
        isTouch = false;
        if (isLongPress)
        {
            StopDragging();
        }

        if (!isSelected)
        {
            // 通知父级结束拖动事件
            OnParentScvEndDrag(eventData);
        }
    }

    public void OnInitializePotentialDrag(PointerEventData eventData)
    {
        if (!isSelected && parentScrollRect != null)
        {
            parentScrollRect.OnInitializePotentialDrag(eventData);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!isSelected)
        {
            OnParentScvBeginDrag(eventData);
            return;
        }

        // 开始拖动
        isDragging = true;

        // 计算拖动偏移量
        Vector2 localPointerPosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(RectTrans.parent as RectTransform, eventData.position, eventData.pressEventCamera, out localPointerPosition);
        dragOffset = RectTrans.anchoredPosition - localPointerPosition;
    }

    // 拖动时移动Item
    public void OnDrag(PointerEventData eventData)
    {
        if (!isSelected)
        {
            OnParentScvOnDrag(eventData);
            return;
        }

        isDragging = true;
        Vector2 localPointerPosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(RectTrans.parent as RectTransform, eventData.position, eventData.pressEventCamera, out localPointerPosition);
        RectTrans.anchoredPosition = localPointerPosition + dragOffset;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isSelected)
        {
            OnParentScvEndDrag(eventData);
            return;
        }

        StopDragging();
    }

    private void Update()
    {
        if (!isTouch || isDragging || isLongPress || !isSelected)
            return;
        
        if (Time.time - pressStartTime > longPressDuration)
        {
            StartDragging();
        }
    }

    private void StartDragging()
    {
        isLongPress = true;
        // 可以在这里添加拖动开始时的逻辑
    }

    private void StopDragging()
    {
        isDragging = false;
        isLongPress = false;

        // 吸附到最近的关键帧
        _onStopDraggingAct?.Invoke(this);
    }

    #endregion
}
