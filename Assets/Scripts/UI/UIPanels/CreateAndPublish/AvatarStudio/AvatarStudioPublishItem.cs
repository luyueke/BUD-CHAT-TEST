using System;
using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using System;
using System.Collections.Generic;
using Es;
using Game.Base;
using GameData;
using GameData.Base;
using GameData.BaseInfo;
using GameData.Managers;
using GameData.MapData;
using UGCAsset.Draft;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class AvatarStudioPublishItem : AvatarStudioBaseItem
{
    public CButton itemBtn;
    public Text buyNum;
    public Text price;
    public Image priceIcon;

    public GameObject Go_UnderReview;

    //Data
    public DraftListItem _curData;
    private Action<DraftListItem> _onClickAct;

    public override void Init(Action<DraftListItem> onSelect, Action onCreateSelect, DraftListItem data)
    {
        _onClickAct = onSelect;
        _curData = data;
        if (data.interactInfo != null)
            buyNum.text = data.interactInfo.likeAmount.ToString();

        var skinInfo = data.Get<SkinInfo>();
        var propInfo = data.Get<PropInfo>();
        var materialInfo = data.Get<MaterialInfo>();
        if (skinInfo != null)
        {
            priceIcon?.gameObject.SetActive(true);
            if (priceIcon != null)
            {
                priceIcon.sprite = PgcUtils.LoadCurrencyIcon(skinInfo.paymentInfo.currencyType, gameObject);
            }
            price.SetText(skinInfo.paymentInfo.price.ToString());
        }
        else if (propInfo != null)
        {
            var priceValue = propInfo?.paymentInfo?.price ?? 0;
            bool isShowIcon = priceValue > 0;
            priceIcon?.gameObject.SetActive(isShowIcon);
            if (priceIcon != null)
            {
                priceIcon.sprite = PgcUtils.LoadCurrencyIcon(propInfo.paymentInfo.currencyType, gameObject);
            }
            if (isShowIcon)
            {
                price.SetText(priceValue.ToString());
            }
            else
            {
                price.SetLocalText("免费");
            }
        }
        else if (materialInfo != null)
        {
            var priceValue = materialInfo?.paymentInfo?.price ?? 0;
            bool isShowIcon = priceValue > 0;
            priceIcon?.gameObject.SetActive(isShowIcon);
            if (priceIcon != null)
            {
                var currencyType = materialInfo?.paymentInfo?.currencyType ?? CurrencyType.PinkCoin;
                priceIcon.sprite = PgcUtils.LoadCurrencyIcon(currencyType, gameObject);
            }
            if (isShowIcon)
            {
                price.SetText(priceValue.ToString());
            }
            else
            {
                price.SetLocalText("免费");
            }
        }
        else
        {
            price.SetText("0");
        }

        itemBtn.onClick.RemoveAllListeners();
        itemBtn.onClick.AddListener(OnDraftsBtnClick);

        var underReviewBtn = Go_UnderReview.GetComponent<Button>();
        underReviewBtn.onClick.RemoveAllListeners();
        underReviewBtn.onClick.AddListener(OnUnderReviewBtnClick);
        RefreshAuditState();
    }


    private void OnDraftsBtnClick()
    {
        if (_curData == null)
        {
            return;
        }

        _onClickAct?.Invoke(_curData);
    }

    private void OnUnderReviewBtnClick()
    {
        if (GetAuditState() == AuditResult.Appealing)
        {
            TipPanel.ShowToast("该作品在申诉中，请耐心等待");
            return;
        }
    }

    private void RefreshAuditState()
    {
        var auditResult = GetAuditState();

        if (auditResult == AuditResult.PendingToAudit)
        {
            buyNum.transform.parent.gameObject.SetActive(false);
            Go_UnderReview.GetComponentInChildren<Text>(true).SetLocalText("审核中");
            Go_UnderReview.SetActive(true);
        }
        else if (auditResult == AuditResult.Appealing)
        {
            buyNum.transform.parent.gameObject.SetActive(false);
            Go_UnderReview.GetComponentInChildren<Text>(true).SetLocalText("申诉中");
            Go_UnderReview.SetActive(true);
        }
    }

    private AuditResult GetAuditState()
    {
        var auditResult = AuditResult.Passed;
        if (_curData.skinInfo != null && _curData.skinInfo.auditInfo != null)
        {
            auditResult = (AuditResult)_curData.skinInfo.auditInfo.auditResult;
        }

        if (_curData.materialInfo != null && _curData.materialInfo.auditInfo != null)
        {
            auditResult = (AuditResult)_curData.materialInfo.auditInfo.auditResult;
        }

        if (_curData.propInfo != null && _curData.propInfo.auditInfo != null)
        {
            auditResult = (AuditResult)_curData.propInfo.auditInfo.auditResult;
        }

        if (_curData.mapInfo != null && _curData.mapInfo.auditInfo != null)
        {
            auditResult = (AuditResult)_curData.mapInfo.auditInfo.auditResult;
        }

        return auditResult;
    }
}
