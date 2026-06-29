using System;
using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UnityEngine;

public class OcActivityTaskButton : MonoBehaviour
{
    public GameObject Go_Reddot;
    public CButton Btn_Click;
    public GameObject Go_Select;
    public CameraActivityTaskType CurTaskType;
    private Action<CameraActivityTaskType> _onBtnClick;

    private void Awake()
    {
        Btn_Click.onClick.AddListener(OnBtnClick);
    }

    public void InitData(Action<CameraActivityTaskType> act)
    {
        this._onBtnClick = act;
    }

    private void OnBtnClick()
    {
        this._onBtnClick?.Invoke(this.CurTaskType);
    }

    public void SetSelectState(bool isSelect)
    {
        Go_Select?.SetActive(isSelect);
    }

    public void SetReddotEnable(bool enable)
    {
        Go_Reddot?.SetActive(enable);
    }
}
