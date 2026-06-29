using System;
using System.Collections;
using System.Collections.Generic;
using BUD.AnimPose;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class LoopTypeContent : MonoBehaviour
{
    public CButton Btn_ChooseType;
    public Text Txt_ChooseType;
    public CButton Btn_Loop;
    public CButton Btn_NoLoop;
    public RectTransform Rt_Arrow;

    private bool _isOpenMenu = false;
    private Action<bool> _chooseAct;

    private void Awake()
    {
        Btn_ChooseType.onClick.AddListener(OnBtnChooseTypeClick);
        Btn_Loop.onClick.AddListener(OnBtnLoopClick);
        Btn_NoLoop.onClick.AddListener(OnBtnNoLoopClick);
    }

    public void Init(Action<bool> act)
    {
        this._chooseAct = act;
        if (AnimDataManager.Inst.isLoop)
        {
            OnBtnLoopClick();
        }
        else
        {
            OnBtnNoLoopClick();
        }
    }

    private void OnBtnChooseTypeClick()
    {
        Rt_Arrow.localEulerAngles = _isOpenMenu ? Vector3.zero : new Vector3(0, 0, -90);
        this.Btn_Loop.gameObject.SetActive(!_isOpenMenu);
        this.Btn_NoLoop.gameObject.SetActive(!_isOpenMenu);

        _isOpenMenu = !_isOpenMenu;
    }

    private void OnBtnLoopClick()
    {
        this._chooseAct?.Invoke(true);
        this.Txt_ChooseType.SetText("循环预览");
        
        RestUI();
    }

    private void OnBtnNoLoopClick()
    {
        this._chooseAct?.Invoke(false);
        this.Txt_ChooseType.SetText("非循环预览");

        RestUI();
    }

    private void RestUI()
    {
        this.Btn_Loop.gameObject.SetActive(false);
        this.Btn_NoLoop.gameObject.SetActive(false);
        Rt_Arrow.localEulerAngles = Vector3.zero;
    }
    
}
