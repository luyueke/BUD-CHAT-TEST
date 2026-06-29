using System.Collections;
using System.Collections.Generic;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;

public class AIHospitalFirstPlayGiftPanel : BasePanel<AIHospitalFirstPlayGiftPanel>
{
    [SerializeField] private CButton _closeBtn;
    [SerializeField] private CButton _okBtn;
    [SerializeField] private CButton _giftPreviewBtn;

    public override void OnShow(params object[] args)
    {
        base.OnCreate();
        InitUI();
    }
    public void InitUI()
    {
       AddListener();             
    }

    public void AddListener()
    {
        _closeBtn.onClick.AddListener(ClosePanel);
        _okBtn.onClick.AddListener(ClosePanel);
        _giftPreviewBtn.onClick.AddListener(ClosePanel);
    }

    public void RemoveListener()
    {
        _closeBtn.onClick.RemoveAllListeners();
        _okBtn.onClick.RemoveAllListeners();
        _giftPreviewBtn.onClick.RemoveAllListeners();   
    }

    public void ClosePanel()
    {
        CloseSelf();
    }

    public void OnGiftPreviewClick()
    {
        //todo 预览奖励
    }
}
