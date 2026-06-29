using System;
using System.Collections;
using System.Collections.Generic;
using GameData;
using GameData.Base;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class TheatreActorDraftsItem : MonoBehaviour
{
    public CButton Btn_Create;
    public CButton Btn_Select;
    public GameObject publishRoot;
    public Text buyNum;
    public Text price;
    public Image currencyIcon;

    public CButton Btn_underReview;

    //Data
    private DraftListItem _curData;
    private Action<DraftListItem> _onClickAct;
    private ActorSubType _curActorSubType;
    private EnterGameModel _enterGameModel;

    public void InitCreateMode(EnterGameModel enterGameModel)
    {
        _enterGameModel = enterGameModel;
        Btn_Create.gameObject.SetActive(true);
        Btn_Select.gameObject.SetActive(false);
        Btn_Create.onClick.RemoveAllListeners();
        Btn_Create.onClick.AddListener(OnCreateBtnClick);
    }

    private void OnUnderReviewClick()
    {
        var auditResult = GetAuditResult();
        if (auditResult == AuditResult.Appealing)
        {
            TipPanel.ShowToast("该作品在申诉中，请耐心等待");
        }
    }


    public void InitSelectMode(DraftListItem data, Action<DraftListItem> onSelect, ActorSubType actorSubType)
    {
        _onClickAct = onSelect;
        _curData = data;
        _curActorSubType = actorSubType;
        Btn_Create.gameObject.SetActive(false);
        Btn_Select.gameObject.SetActive(true);

        Btn_Select.onClick.RemoveAllListeners();
        Btn_Select.onClick.AddListener(OnSelectBtnClick);


        Btn_underReview.onClick.RemoveAllListeners();
        Btn_underReview.onClick.AddListener(OnUnderReviewClick);
        Btn_underReview.gameObject.SetActive(false);


        if (actorSubType == ActorSubType.Drafts)
        {
            publishRoot.SetActive(false);
        }
        else
        {
            publishRoot.SetActive(true);
            if (_curData.actorInfo?.paymentInfo != null)
            {
                price.SetText(_curData.actorInfo.paymentInfo.price.ToString());
                currencyIcon.sprite =
                    PgcUtils.LoadCurrencyIcon(_curData.actorInfo.paymentInfo.currencyType, gameObject);
            }

            if (_curData.interactInfo != null)
                buyNum.text = data.interactInfo.likeAmount.ToString();
        }

        RefreshAuditState();
    }

    private void OnCreateBtnClick()
    {
        if (_enterGameModel == EnterGameModel.OCActorCreaterEmpty)
        {
            UIManager.Inst.OpenPanel(PanelId.TheatreCreaterActorPanel, _enterGameModel);
        }
        else if (_enterGameModel == EnterGameModel.OCActorContinueEdit)
        {
            //UIManager.Inst.OpenPanel(PanelId.CreateVehiclePanel, _enterGameModel);
        }
    }

    private void OnSelectBtnClick()
    {
        if (_curData == null)
        {
            return;
        }

        _onClickAct?.Invoke(_curData);
    }


    private void RefreshAuditState()
    {
        var auditResult = GetAuditResult();
        if (auditResult == AuditResult.PendingToAudit)
        {
            Btn_underReview.GetComponentInChildren<Text>(true).SetLocalText("审核中");
            Btn_underReview.gameObject.SetActive(true);
            publishRoot.SetActive(false);
        }
        else if (auditResult == AuditResult.Appealing)
        {
            Btn_underReview.GetComponentInChildren<Text>(true).SetLocalText("申诉中");
            Btn_underReview.gameObject.SetActive(true);
            publishRoot.SetActive(false);
        }
        else
        {
            Btn_underReview.gameObject.SetActive(false);
        }
    }

    private AuditResult GetAuditResult()
    {
        var auditResult = AuditResult.Passed;
        if (_curData.actorInfo != null && _curData.actorInfo.auditInfo != null)
        {
            auditResult = (AuditResult)_curData.actorInfo.auditInfo.auditResult;
        }

        return auditResult;
    }
}

