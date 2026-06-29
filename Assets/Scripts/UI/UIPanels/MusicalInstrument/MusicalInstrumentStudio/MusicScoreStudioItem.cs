using System;
using System.Collections;
using System.Collections.Generic;
using Game.Base;
using GameData;
using GameData.Base;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class MusicScoreStudioItem : MonoBehaviour {
    public CButton Btn_Create;
    public CButton Btn_Select;

    public GameObject publishRoot;
    public Text buyNum;
    public Text price;
    public CButton Go_UnderReview;
    public Image currencyIcon;

    //Data
    private DraftListItem _curData;
    private Action<DraftListItem> _onClickAct;
    private StudioSubType _curStudioSubType;

    public void InitCreateMode() {
        Btn_Create.gameObject.SetActive(true);
        Btn_Select.gameObject.SetActive(false);
        Btn_Create.onClick.RemoveAllListeners();
        Btn_Create.onClick.AddListener(OnCreateBtnClick);

    }

    private void OnUnderReviewClick() {
        var auditResult = GetAuditResult();
        if (auditResult == AuditResult.Appealing) {
            TipPanel.ShowToast("该作品在申诉中，请耐心等待");
        }
    }


    public void InitSelectMode(DraftListItem data, Action<DraftListItem> onSelect, StudioSubType studioSubType) {
        _onClickAct = onSelect;
        _curData = data;
        _curStudioSubType = studioSubType;

        Btn_Create.gameObject.SetActive(false);
        Btn_Select.gameObject.SetActive(true);

        Btn_Select.onClick.RemoveAllListeners();
        Btn_Select.onClick.AddListener(OnSelectBtnClick);

        Go_UnderReview.onClick.RemoveAllListeners();
        Go_UnderReview.onClick.AddListener(OnUnderReviewClick);
        Go_UnderReview.gameObject.SetActive(false);

        if (studioSubType == StudioSubType.Drafts)
        {
            publishRoot.SetActive(false);
        }
        else
        {
            publishRoot.SetActive(true);
            if (_curData.musicScoreInfo.paymentInfo!=null)
            {
                price.SetText(_curData.musicScoreInfo.paymentInfo.price.ToString());
                currencyIcon.sprite =
                    PgcUtils.LoadCurrencyIcon(_curData.musicScoreInfo.paymentInfo.currencyType, gameObject);
            }
            if (_curData.interactInfo != null)
                buyNum.text = data.interactInfo.likeAmount.ToString();
        }
		RefreshAuditStatus();
    }

    private void OnCreateBtnClick() {
        if (GameController.GetCurrentGameMode() == GameMode.Guest) {
            TipPanel.ShowToast("请先退出地图后再进入乐谱编辑器");
            return;
        }
        UIManager.Inst.OpenPanel(PanelId.MusicScoreEditInfoPanel, false);
    }

    private void OnSelectBtnClick() {
        if (_curData == null) {
            return;
        }
        _onClickAct?.Invoke(_curData);
    }


    private void RefreshAuditStatus() {

        var auditResult = GetAuditResult();
        if (auditResult == AuditResult.Rejected) {
            Go_UnderReview.GetComponentInChildren<Text>().SetLocalText("审核失败");
            Go_UnderReview.gameObject.SetActive(true);
            publishRoot.SetActive(false);
        } else if (auditResult == AuditResult.Appealing) {
            Go_UnderReview.GetComponentInChildren<Text>().SetLocalText("申诉中");
            Go_UnderReview.gameObject.SetActive(true);
            publishRoot.SetActive(false);
        } else {
            Go_UnderReview.gameObject.SetActive(false);
        }
    }

    private AuditResult GetAuditResult() {
        var auditResult = AuditResult.Passed;
        if (_curData.musicScoreInfo != null && _curData.musicScoreInfo.auditInfo != null) {
            auditResult = (AuditResult)_curData.musicScoreInfo.auditInfo.auditResult;
        }
        return auditResult;
    }
}
