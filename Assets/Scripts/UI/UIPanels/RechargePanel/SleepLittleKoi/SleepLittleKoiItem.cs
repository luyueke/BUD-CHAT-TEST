using System;
using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UnityEngine;

public class SleepLittleKoiItem : MonoBehaviour
{
    public CurrencyType _CurrencyType;
    public bool _IsVip;
    private CButton _CButton;

    private void Awake()
    {
        _CButton = this.GetComponent<CButton>();
        _CButton.onClick.AddListener(OnBtnClick);
    }

    private void OnBtnClick()
    {
        if (!_IsVip)
        {
            UIManager.Inst.OpenPanel(PanelId.CurrencyTipsPanel, _CurrencyType);
        }
        else
        {
            var panel = UIManager.Inst.OpenPanel<CurrencyTipsPanel>(PanelId.CurrencyTipsPanel);
            if (panel != null)
            {
                panel.SetVipContent();
            }
        }
    }
}
