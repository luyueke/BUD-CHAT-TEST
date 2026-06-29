using System;
using System.Collections;
using System.Collections.Generic;
using BUD.AnimPose;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class KeyFrameItem : MonoBehaviour
{
    public RectTransform RectTrans;
    public Text Txt_Lable;
    public CButton Btn_Frame;
    public Image Img_Outline;
    public Image Img_Bg;
    public Image Img_Line;
    public RectTransform Rect_Line;
    
    private Action<KeyFrameItem> _onClickAct;
    private Action<KeyFrameItem> _onDeleteAct;
    private Action<KeyFrameItem> _onAddAct;

    private bool _isLastFrame = false;
    private int _frameIndex;
    private KeyFrameData _curData;
    private Color _selectedColor = Color.yellow;
    private Color _addedColor = Color.green;
    private Color _emptyColor = Color.gray;
    private const int MaxZoomLevel = 18;
    private const int MinZoomLevel = 1;
    private const int OriWidth = 74;
    private int CurWidth = 74;
    private const int DeltZoomW = 20;
    private int _curZoomLevel = 1;
    private int _frameFrequency = 10;
    private float _spacing = 10.00f;
    

    private void Awake()
    {
        Btn_Frame.onClick.AddListener(OnBtnFrameClick);
    }

    public void InitKeyFrameItem(int index, Action<KeyFrameItem> onClickAct)
    {
        this._frameIndex = index;
        SetTime(this._frameIndex);
        this._onClickAct = onClickAct;
    }
    
    //设置帧号
    private void SetTime(int index)
    {
        if (index % _frameFrequency == 0)
        {
            Txt_Lable.text = string.Format("0:{0:00}", index / _frameFrequency);
        }
        else
        {
            Txt_Lable.text = ".";
        }
    }
    
    //数据绑定
    public void SetData(KeyFrameData data, bool isLastFrame = false)
    {
        _isLastFrame = isLastFrame;
        this._curData = data;
        SetLinePadding(CurWidth);
    }
    
    public void AddFrameData(KeyFrameData data)
    {
        this._curData = data;
        this._curData.isEmptySlot = 0;
    }
    
    private void OnBtnFrameClick()
    {
        _onClickAct?.Invoke(this);
    }

    public void RefreshItemBg(FrameSelectMode mode)
    {
        switch (mode)
        {
            case FrameSelectMode.EmptyFrameData:
                Img_Line.gameObject.SetActive(false);
                Img_Outline.gameObject.SetActive(false);
                break;
            
            case FrameSelectMode.EmptySlot:
                Img_Line.gameObject.SetActive(true);
                Img_Outline.gameObject.SetActive(true);
                
                Img_Outline.color = _emptyColor;
                Img_Bg.color = _emptyColor;
                break;

            case FrameSelectMode.AddedFrameData:
                Img_Line.gameObject.SetActive(true);
                Img_Outline.gameObject.SetActive(true);
                
                Img_Outline.color = _addedColor;
                Img_Bg.color = _addedColor;
                break;

            case FrameSelectMode.Selected:
                Img_Line.gameObject.SetActive(true);
                Img_Outline.gameObject.SetActive(true);
                
                Img_Outline.color = Color.white;
                Img_Bg.color = _selectedColor;
                break;
        }
    }

    public int GetIndex()
    {
        return _frameIndex;
    }

    public float GetContentWidth()
    {
        //包含间距 10
        var width = RectTrans.sizeDelta.x;
        return width + _spacing;
    }

    public void ZoomOut() 
    {
        if(_curZoomLevel <= MinZoomLevel)
            return;
        
        _curZoomLevel--;
        CurWidth = OriWidth + (_curZoomLevel - 1) * DeltZoomW;
        this.RectTrans.sizeDelta = new Vector2(CurWidth, this.RectTrans.sizeDelta.y);
        SetLinePadding(CurWidth);
    }

    public void ZoomIn()
    {
        if(_curZoomLevel >= MaxZoomLevel)
            return;
        
        _curZoomLevel++;
        CurWidth = OriWidth + (_curZoomLevel - 1) * DeltZoomW;
        this.RectTrans.sizeDelta = new Vector2(CurWidth, this.RectTrans.sizeDelta.y);
        SetLinePadding(CurWidth);
    }

    public void ZoomByGesture(float zoomScale)
    {
        var newWidth = OriWidth + zoomScale * DeltZoomW;
        this.RectTrans.sizeDelta = new Vector2(newWidth, this.RectTrans.sizeDelta.y);
    }
    
    public KeyFrameData GetData()
    {
        return this._curData;
    }
    
    public int GetZoomLevel()
    {
        return _curZoomLevel;
    }

    private void SetLinePadding(int width)
    {
        if (GetIndex() == 0)
        {
            Rect_Line.offsetMin = new Vector2(width/2.0f, Rect_Line.offsetMin.y);
        }

        if (_isLastFrame)
        {
            Rect_Line.offsetMax = new Vector2(-width/2.0f, Rect_Line.offsetMax.y);
        }
        else
        {
            Rect_Line.offsetMax = new Vector2(width * 0.07f, Rect_Line.offsetMax.y);
        }
    }
}

public enum FrameSelectMode
{
    EmptyFrameData = 0,// 有槽位，但是没有帧数据
    EmptySlot,  // 空槽
    AddedFrameData, // 已添加帧数据
    Selected, //被选中
}
