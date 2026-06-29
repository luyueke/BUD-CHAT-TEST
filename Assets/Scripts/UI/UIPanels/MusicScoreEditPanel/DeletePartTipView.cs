using System;
using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UnityEngine;

public class DeletePartTipView : MonoBehaviour
{
    public CButton closeViewBtn;
    public CButton yesBtn;
    public CButton noBtn;
    public Action deleteAction;
    public void Init()
    {
        closeViewBtn.onClick.AddListener(Close);
        yesBtn.onClick.AddListener(OnYesBtnClick);
        noBtn.onClick.AddListener(Close);
    }

    public void Show(Action deleteAction)
    {
        gameObject.SetActive(true);
        this.deleteAction = deleteAction;
    }
    public void Close()
    {
        gameObject.SetActive(false);
    }

    public void OnYesBtnClick()
    {
        deleteAction?.Invoke();
        Close();
    }
}
