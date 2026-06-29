using GameData.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public abstract class BaseUgcToneItem<T> : BaseToneItem where T : UgcBaseInfo
{
    public GameObject Go_UnderReview;
    public Button Btn_Create;

    protected bool _isCreate = false;
    protected T _curInfo;

    protected override void Awake()
    {
        base.Awake();
        Btn_Create.onClick.AddListener(OnCreateBtnClick);
        Go_UnderReview.GetComponent<Button>().onClick.AddListener(OnUnderReviewClick);
        Go_UnderReview.SetActive(false);
    }

    protected abstract void OnCreateBtnClick();
    protected abstract void OnUnderReviewRejected();

    protected virtual void OnUnderReviewClick()
    {
        var auditResult = GetAuditResult();

        if (auditResult == AuditResult.Appealing)
        {
            TipPanel.ShowToast("该作品在申诉中，请耐心等待");
            return;
        }

        if (auditResult == AuditResult.PendingToAudit)
        {
            TipPanel.ShowToast("该作品在审核中，请耐心等待");
            return;
        }

        if (auditResult == AuditResult.Rejected)
        {
            OnUnderReviewRejected();
        }
    }

    protected virtual AuditResult GetAuditResult()
    {
        var auditResult = AuditResult.Passed;

        if (_curInfo?.auditInfo != null)
        {
            auditResult = (AuditResult)_curInfo.auditInfo.auditResult;
        }

        return auditResult;
    }

    protected void RefreshAuditStatus()
    {
        if (_isCreate)
        {
            Go_UnderReview.SetActive(false);
            return;
        }

        var auditResult = GetAuditResult();
        var txtComp = Go_UnderReview.GetComponentInChildren<Text>();
        var imageGo = GameObjectEx.FindChildByName(transform, "Go_UnderReview/Image")
            .GetComponent<Image>().gameObject;

        if (auditResult == AuditResult.Rejected)
        {
            txtComp.SetLocalText("审核失败");
            imageGo.SetActive(true);
            Go_UnderReview.SetActive(true);
        }
        else if (auditResult == AuditResult.Appealing)
        {
            txtComp.SetLocalText("申诉中");
            imageGo.SetActive(true);
            Go_UnderReview.SetActive(true);
        }
        else if (auditResult == AuditResult.PendingToAudit)
        {
            txtComp.SetLocalText("审核中");
            imageGo.SetActive(false);
            Go_UnderReview.SetActive(true);
        }
    }

    public override void SetSelectState(bool isSelect)
    {
        if (_isCreate)
            return;

        base.SetSelectState(isSelect);
    }
}
