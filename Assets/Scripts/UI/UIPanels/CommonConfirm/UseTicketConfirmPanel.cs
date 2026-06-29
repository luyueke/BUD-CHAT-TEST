using System;
using System.Collections;
using System.Collections.Generic;
using Game.Store;
using GameData.Gashapon;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class UseTicketConfirmPanel : BasePanel<UseTicketConfirmPanel>
{
    public CText Txt_TopTitle;
    public CText Txt_Desc;
    public CText Txt_Balance;
    public Image Img_Icon;
    public CButton Btn_Confirm;
    public CButton Btn_Cancel;
    public CButton Btn_Bg;

    private CurrencyType _CurCurrencyType;
    private Action _onConfirmAct;

    public override void OnCreate()
    {
        base.OnCreate();
        
        Btn_Confirm.onClick.AddListener(OnBtnConfirmClick);
        Btn_Cancel.onClick.AddListener(CloseSelf);
        Btn_Bg.onClick.AddListener(CloseSelf);
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);

        _CurCurrencyType = (CurrencyType)args[0];
        InitUI();
    }

    public void SetConfimAction(Action act)
    {
        _onConfirmAct = act;
    }

    private void InitUI()
    {
        //switch (_CurCurrencyType)
        //{
        //    case CurrencyType.UGCSkinTicket:
        //        Txt_TopTitle.text = "使用社区皮肤劵";
        //        Img_Icon.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(SpriteAtlasType.Common, "Ticket_Skin", gameObject);
        //        Txt_Desc.text = "当前拥有社区皮肤劵：";
        //        Txt_Balance.text = AccountDataManager.Inst.BalanceInfo.GetAccountCount(BalanceType.UGCSkinTicket).ToString();
        //        break;
        //    case CurrencyType.GashaponTicket:
        //        Txt_TopTitle.text = "使用扭蛋劵";
        //        Img_Icon.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(SpriteAtlasType.Common, "Ticket_Gashapon", gameObject);
        //        Txt_Desc.text = "当前拥有扭蛋劵：";
        //        Txt_Balance.text = AccountDataManager.Inst.BalanceInfo.GetAccountCount(BalanceType.GashaponTicket).ToString();
        //        break;
        //}
    }

    private void OnBtnConfirmClick()
    {
        _onConfirmAct?.Invoke();
    }
}
