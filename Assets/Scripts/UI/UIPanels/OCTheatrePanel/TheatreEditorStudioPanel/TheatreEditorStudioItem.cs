using System;
using GameData;
using GameData.Base;
using GameData.BaseInfo;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class TheatreEditorStudioItem : MonoBehaviour
{
    public CButton Btn_Create;
    public CButton Btn_Select;
    public GameObject publishRoot;
    public Text buyNum;
    public Text price;
    public Image currencyIcon;
    public CButton Btn_underReview;

    private DraftListItem _curData;
    private Action<DraftListItem> _onClickAct;
    private TheatreStudioSubType _curStudioSubType;

    public void InitCreateMode()
    {
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

    public void InitSelectMode(DraftListItem data, Action<DraftListItem> onSelect, TheatreStudioSubType studioSubType)
    {
        _onClickAct = onSelect;
        _curData = data;
        _curStudioSubType = studioSubType;
        Btn_Create.gameObject.SetActive(false);
        Btn_Select.gameObject.SetActive(true);

        Btn_Select.onClick.RemoveAllListeners();
        Btn_Select.onClick.AddListener(OnSelectBtnClick);

        Btn_underReview.onClick.RemoveAllListeners();
        Btn_underReview.onClick.AddListener(OnUnderReviewClick);
        Btn_underReview.gameObject.SetActive(false);

        if (studioSubType == TheatreStudioSubType.Drafts)
        {
            publishRoot.SetActive(false);
        }
        else
        {
            publishRoot.SetActive(true);
            if (_curData.theatreInfo?.paymentInfo != null)
            {
                price.SetText(_curData.theatreInfo.paymentInfo.price.ToString());
                currencyIcon.sprite =
                    PgcUtils.LoadCurrencyIcon(_curData.theatreInfo.paymentInfo.currencyType, gameObject);
            }

            if (_curData.interactInfo != null)
                buyNum.text = data.interactInfo.likeAmount.ToString();
        }

        RefreshAuditState();
    }

    private void OnCreateBtnClick()
    {
        UIManager.Inst.OpenPanel(PanelId.TheatreEditorPanel, new OCTheatreInfo());
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
        if (_curData.theatreInfo != null && _curData.theatreInfo.auditInfo != null)
        {
            auditResult = (AuditResult)_curData.theatreInfo.auditInfo.auditResult;
        }

        return auditResult;
    }
}
