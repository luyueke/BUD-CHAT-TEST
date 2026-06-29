using System;
using GameData.Base;
using GameData.BaseInfo;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UI.BaseWidgets;
using UIAgent;
using UnityEngine;
using UnityEngine.UI;


public class UgcAnimUgcToneItem : MonoBehaviour
{
    public CButton Btn_Create;
    public CButton Btn_GoStore;
    public CButton Btn_Select;
    public GameObject Go_Selected;
    public Text Txt_Title;
    public GameObject Go_Normal;
    public GameObject Go_TryPlay;
    public GameObject Go_UnderReview;

    private bool _isOwnedList = true;
    private bool _isCreate = false;
    private bool _isGoToneStore = false;
    private AnimMusicInfo _curInfo;
    private Action<AnimMusicInfo> _onItemSelect;
    private Action<AnimMusicInfo> _onPreviewTone;
    private Action _refreshAct;

    private void Awake()
    {
        Btn_Create.onClick.AddListener(OnCreateUgcToneClick);
        Btn_GoStore.onClick.AddListener(OnGoToneStoreClick);
        Btn_Select.onClick.AddListener(OnToneItemSelect);
        Go_UnderReview.GetComponent<Button>().onClick.AddListener(OnUnderReviewClick);
        Go_Normal.SetActive(true);
        Go_TryPlay.SetActive(false);
        Go_UnderReview.SetActive(false);
    }

    private void OnUnderReviewClick()
    {
        var auditResult = GetAuditResult();
        if (auditResult == AuditResult.Appealing)
        {
            TipPanel.ShowToast("该作品在申诉中，请耐心等待");
        }
        else if (auditResult == AuditResult.PendingToAudit)
        {
            TipPanel.ShowToast("该作品在审核中，请耐心等待");
        }
        else if (auditResult == AuditResult.Rejected)
        {

            UIManager.Inst.OpenPanel<UGCAuditRejectedPanel>(PanelId.UGCAuditRejectedPanel,
                new UGCAuditRejectedPanel.RejectedAppealArgs
                {
                    req = new UGCAsset.UGCSetRequest(_curInfo, UGCAsset.UGCOperationType.Appeal),
                    onRejectedCallBack = OnAppealCallBack,
                    reason = _curInfo.auditInfo.rejectReason,
                });
        }
    }

    private void OnAppealCallBack(bool isAppeal)
    {
        if (!isAppeal)
        {
            SetUgcAnimMusicReq req = new SetUgcAnimMusicReq()
            {
                animMusicInfo = _curInfo,
                setType = (int)SetType.Delete
            };
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.setUgcAnimMusic, HttpMethod.POST,
                JsonConvert.SerializeObject(req),
                (content) =>
                {
                    this._refreshAct?.Invoke();
                }, (error) => { });
        }
        else
        {

            if (_curInfo.auditInfo != null)
            {
                _curInfo.auditInfo.auditResult = (int)AuditResult.Appealing;
            }

            RefreshAuditStatus();
        }
    }

    public void InitGoStoreMode(Action refreshAct)
    {
        this._refreshAct = refreshAct;
        _isGoToneStore = true;
        Btn_GoStore.gameObject.SetActive(true);
        Btn_Create.gameObject.SetActive(false);
        Btn_Select.gameObject.SetActive(false);
        Go_UnderReview.SetActive(false);
    }

    public void InitCreateMode()
    {
        _isCreate = true;
        Btn_Create.gameObject.SetActive(true);
        Btn_Select.gameObject.SetActive(false);
        Btn_GoStore.gameObject.SetActive(false);
        Go_UnderReview.SetActive(false);
    }

    public void InitSelectMode(bool isOwnedList, AnimMusicInfo info, Action<AnimMusicInfo> onSelectAct, Action<AnimMusicInfo> previewToneAct, Action refreshAct)
    {
        this._isOwnedList = isOwnedList;
        this._isCreate = false;
        this._curInfo = info;
        this._onItemSelect = onSelectAct;
        this._onPreviewTone = previewToneAct;
        this._refreshAct = refreshAct;
        Btn_Create.gameObject.SetActive(false);
        Btn_GoStore.gameObject.SetActive(false);
        Btn_Select.gameObject.SetActive(true);
        this.Txt_Title.SetLocalText(this._curInfo.name);
        RefreshAuditStatus();
    }

    private void OnGoToneStoreClick()
    {
        var panel =  UIManager.Inst.OpenPanel<UgcAnimToneStorePanel>(PanelId.UgcAnimToneStorePanel);
        panel.DidPurchasedAction = (pgcid) => {
            this._refreshAct?.Invoke();
        };
    }

    private void OnCreateUgcToneClick()
    {
        UIManager.Inst.OpenPanel<PublishUgcAnimTonePanel>(PanelId.PublishUgcAnimTonePanel);
    }

    private void OnToneItemSelect()
    {
        PlayMusic();
        this._onItemSelect?.Invoke(this._curInfo);
    }
    
    public void SetSelectState(bool isSelect)
    {
        if (_isCreate || _isGoToneStore)
            return;

        Go_Selected.SetActive(isSelect);

        Go_Normal.SetActive(!isSelect);
        Go_TryPlay.SetActive(isSelect);
    }

    private void PlayMusic()
    {
        this._onPreviewTone?.Invoke(_curInfo);
    }

    private void RefreshAuditStatus()
    {
        if (_isGoToneStore || _isCreate)
        {
            Go_UnderReview.SetActive(false);
            return;
        }
        
        var auditResult = GetAuditResult();
        if (auditResult == AuditResult.Rejected)
        {
            Go_UnderReview.GetComponentInChildren<Text>().text = "审核失败";
            GameObjectEx.FindChildByName(this.transform, "Go_UnderReview/Image").GetComponent<Image>().gameObject
                .SetActive(true);
            Go_UnderReview.SetActive(true);
        }
        else if (auditResult == AuditResult.Appealing)
        {
            Go_UnderReview.GetComponentInChildren<Text>().text = "申诉中";
            GameObjectEx.FindChildByName(this.transform, "Go_UnderReview/Image").GetComponent<Image>().gameObject
                .SetActive(true);
            Go_UnderReview.SetActive(true);
        }
        else if (auditResult == AuditResult.PendingToAudit)
        {
            Go_UnderReview.GetComponentInChildren<Text>().text = "审核中";
            GameObjectEx.FindChildByName(this.transform, "Go_UnderReview/Image").GetComponent<Image>().gameObject
                .SetActive(false);
            Go_UnderReview.GetComponentInChildren<Image>().gameObject.SetActive(false);
            Go_UnderReview.SetActive(true);
        }
    }

    private AuditResult GetAuditResult()
    {
        if (this._isOwnedList)
        {
            return AuditResult.Passed;
        }
        
        var auditResult = AuditResult.Passed;
        if (_curInfo.auditInfo != null)
        {
            auditResult = (AuditResult)_curInfo.auditInfo.auditResult;
        }

        return auditResult;
    }
}
