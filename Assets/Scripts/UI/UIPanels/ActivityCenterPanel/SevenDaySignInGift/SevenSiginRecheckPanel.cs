using System;
using System.Collections;
using System.Collections.Generic;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;

public class SevenSiginRecheckPanel : BasePanel<SevenSiginRecheckPanel>
{
    public CButton Btn_Close;
    public CButton Btn_Cancel;
    public CButton Btn_Confirm;

    private Action _OnConfirm;
    public override void OnCreate()
    {
        base.OnCreate();
        Btn_Close.onClick.AddListener(CloseSelf);
        Btn_Cancel.onClick.AddListener(CloseSelf);
        Btn_Confirm.onClick.AddListener(OnBtnConfirmClick);
    }

    public void SetAction(Action act)
    {
        this._OnConfirm = act;
    }

    private void OnBtnConfirmClick()
    {
        this._OnConfirm?.Invoke();
        CloseSelf();
    }
    
}
