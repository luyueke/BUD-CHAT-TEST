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
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 草稿列表item
/// </summary>
public class DraftsItem : MonoBehaviour {
    public CButton draftsBtn;
    public CButton createBtn;
    public GameObject UpLoading;
    public GameObject UpLoadFail;
    public GameObject Go_UnderReview;
    public GameObject Go_Visted;
    public Text Txt_VistedNum;

    //Data
    public DraftListItem _curData;
    private Action<DraftListItem> _onClickAct;
    private StudioSubType _curStudioSubType;

    public virtual void Init(Action<DraftListItem> onSelect, Action<DraftListItem, DraftsItem> uploadAction, DraftListItem data, StudioSubType studioSubType) {
        _onClickAct = onSelect;
        _curData = data;
        _curStudioSubType = studioSubType;

        //说明是CreateBtn
        bool isCreate = (data == null) || GameStudioUtils.GetBaseInfo(data) == null;
        createBtn.gameObject.SetActive(isCreate);
        draftsBtn.gameObject.SetActive(!isCreate);

        if (isCreate) {
            createBtn.onClick.RemoveAllListeners();
            createBtn.onClick.AddListener(CreateNewMap);
            return;
        }

        draftsBtn.onClick.RemoveAllListeners();
        draftsBtn.onClick.AddListener(OnDraftsBtnClick);

        var underReviewBtn = Go_UnderReview.GetComponent<Button>();
        underReviewBtn.onClick.RemoveAllListeners();
        underReviewBtn.onClick.AddListener(OnUnderReviewBtnClick);
        RefreshAuditState();
        RefreshUIInfo();
    }

    private void OnUnderReviewBtnClick() {
        if (GetAuditState() == AuditResult.Appealing) {
            TipPanel.ShowToast("该作品在申诉中，请耐心等待");
            return;
        }
    }

    private void OnDraftsBtnClick() {
        if (_curData == null) {
            return;
        }
        _onClickAct?.Invoke(_curData);
    }

    public void SetUpLoadState(UploadStatus state) {
        switch (state) {
            case UploadStatus.Uploading:
                UpLoading.gameObject.SetActive(true);
                UpLoadFail.gameObject.SetActive(false);
                break;

            case UploadStatus.UploadFail:
                UpLoading.gameObject.SetActive(false);
                UpLoadFail.gameObject.SetActive(true);
                break;

            default:
                UpLoading.gameObject.SetActive(false);
                UpLoadFail.gameObject.SetActive(false);
                break;
        }
    }

    protected virtual void CreateNewMap() {
        LoggerUtils.Log("CreateNewMap");

        DateTime currentDate = DateTime.Now;
        string formattedDate = currentDate.ToString("yyyy-MM-dd");
        string mapName = LocalizationManager.Inst.GetLocalizedText("未命名") + "-" + formattedDate;

        LoggerUtils.Log("地图名字：" + mapName);

        List<MapTemplate> mapTemplates = TemplateManager.Inst.GetAllMapTemplates();
        var createMapInfo = new MapInfo() {
            name = mapName,
            templateId = mapTemplates[0].Id
        };
        var p = UIManager.Inst.OpenPanel<UgcLoadingPanel>(PanelId.UgcLoadingPanel);
        p.Init(createMapInfo, LoadingType.Map);
        GameController.StartGame(EnterGameModel.CreateEmptyScene, createMapInfo);
    }

    private void RefreshAuditState() {
        if (_curStudioSubType == StudioSubType.Drafts)
            return;

        var auditResult = GetAuditState();
        if (auditResult == AuditResult.PendingToAudit) {
            Go_UnderReview.GetComponentInChildren<Text>(true).SetLocalText("审核中");;
            Go_UnderReview.SetActive(true);
        } else if (auditResult == AuditResult.Appealing) {
            Go_UnderReview.GetComponentInChildren<Text>(true).SetLocalText("申诉中");
            Go_UnderReview.SetActive(true);
        }
    }


    private AuditResult GetAuditState() {
        var auditResult = AuditResult.Passed;
        if (_curData.mapInfo != null && _curData.mapInfo.auditInfo != null) {
            auditResult = (AuditResult)_curData.mapInfo.auditInfo.auditResult;
        }
        return auditResult;
    }

    private void RefreshUIInfo() {
        switch (_curStudioSubType) {
            case StudioSubType.Drafts:
                Go_Visted.SetActive(false);
                break;

            case StudioSubType.Published:
                Go_Visted.SetActive(true);
                if (_curData != null && _curData.interactInfo != null) {
                    Txt_VistedNum.text = _curData.interactInfo.consumeAmount.ToString();
                }
                break;
        }
    }
}
