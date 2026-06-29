using System;
using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UnityEngine;

public class ReportPanelItem : MonoBehaviour
{
    public CText Txt_Title;
    public CButton Btn_Click;
    public GameObject Go_IsSelected;

    private Action<ErrReportType> _onSelected;
    private ErrReportType _curReportType;

    public void InitData(ErrReportType curReportType, string title, Action<ErrReportType> act)
    {
        Go_IsSelected.SetActive(false);
        this._curReportType = curReportType;
        Btn_Click.onClick.RemoveAllListeners();
        Btn_Click.onClick.AddListener(() =>
        {
            act?.Invoke(this._curReportType);
            Go_IsSelected.SetActive(true);
        });
        Txt_Title.SetLocalText(title);
    }
}
