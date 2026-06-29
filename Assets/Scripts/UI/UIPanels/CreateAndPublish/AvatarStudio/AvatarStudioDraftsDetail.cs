using System;
using System.Collections;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.Util.IO;
using Es;
using Game.Base;
using Game.Config;
using Game.ECS;
using Game.Props.PropsManagers;
using GameData.Base;
using GameData.BaseInfo;
using GameData.UGCData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UGCAsset;
using UGCAsset.Draft;
using UI;
using UI.BaseWidgets;
using UI.Manager;
using UIAgent;
using UnityEngine;
using UnityEngine.UI;

public class AvatarStudioDraftsDetail : MonoBehaviour {
    private SuperTextMesh detailName;
    private CButton detailRenameBtn;
    private CButton detailCopyBtn;
    private CButton detailDeleteBtn;
    private CButton detailPublishBtn;
    private CButton detailEditBtn;
    private CButton detailChangeTypeBtn;
    private Text lastEditText;
    private RawImage detailCover;
    private CButton detailCoverBtn;
    private Image detailSkinImageBg;
    private CButton detailHideButton;
    private RemoteImageBehaviour detailSkinRBehaviour;
    private GameObject draftDetailGo;
    private MapInfo currentGameStudioMapInfo;
    private string mDraftPath = "";
    private GameObject PropSyncingBg;
    private GameObject PropRetrySavingBg;
    private Button PropRetrySavingBtn;
    private GameObject uiMask;
    private DraftListItem curInfo;
    private Action<DraftListItem> editAction;
    private Action<DraftListItem> editInfoAction;

    private Action copyGameAction;
    private Action<DraftListItem> deleteGameAction;
    private Action<bool> updateGameAction;
    private Action<DraftListItem> reuploadAction;
    private Action<bool> publishGameAction;
    private Action<DraftListItem> retrySaveAction;

    public void InitUI() {
        InitUIComponent();
        AddBtnListener();
    }

    public void InitAction(Action<bool> publishGameAction, Action<DraftListItem> editAction,
        Action<DraftListItem> editInfoAction, Action copyGameAction, Action<DraftListItem> deleteGameAction, Action<DraftListItem> retrySaveAction) {
        this.publishGameAction = publishGameAction;
        this.editAction = editAction;
        this.editInfoAction = editInfoAction;
        this.copyGameAction = copyGameAction;
        this.deleteGameAction = deleteGameAction;
        this.retrySaveAction = retrySaveAction;

    }

    private void InitUIComponent() {
        detailName = GameObjectEx.FindChildByName(this.transform, "DetailTitle").GetComponent<SuperTextMesh>();
        lastEditText = GameObjectEx.FindChildByName(this.transform, "DetailLastEdit").GetComponent<Text>();
        detailRenameBtn = GameObjectEx.FindChildByName(this.transform, "DraftsRename").GetComponent<CButton>();
        detailCopyBtn = GameObjectEx.FindChildByName(this.transform, "DraftsDuplicate").GetComponent<CButton>();
        detailDeleteBtn = GameObjectEx.FindChildByName(this.transform, "DraftsDelete").GetComponent<CButton>();
        detailPublishBtn = GameObjectEx.FindChildByName(this.transform, "DraftsPublish").GetComponent<CButton>();
        detailEditBtn = GameObjectEx.FindChildByName(this.transform, "DraftsEdit").GetComponent<CButton>();
        detailChangeTypeBtn = GameObjectEx.FindChildByName(this.transform, "DraftsChangeType").GetComponent<CButton>();
        detailCoverBtn = GameObjectEx.FindChildByName(this.transform, "DetailCover").GetComponent<CButton>();
        detailSkinImageBg = GameObjectEx.FindChildByName(this.transform, "DetailPropBg").GetComponent<Image>();
        detailSkinRBehaviour = GameObjectEx.FindChildByName(this.transform, "DetailPropImage")
            .GetComponent<RemoteImageBehaviour>();

        detailHideButton = transform.GetComponent<CButton>();
        PropSyncingBg = GameObjectEx.FindChildByName(this.transform, "PropSyncingBg").gameObject;
        PropRetrySavingBg = GameObjectEx.FindChildByName(this.transform, "PropRetrySavingBg").gameObject;
        PropRetrySavingBtn = PropRetrySavingBg.GetComponent<CButton>();

        detailChangeTypeBtn.gameObject.SetActive(false);
    }

    private void AddBtnListener() {
        detailEditBtn.onClick.AddListener(OnEditBtnClick);
        detailDeleteBtn.onClick.AddListener(OnDeleteBtnClick);
        detailPublishBtn.onClick.AddListener(OnPublishBtnClick);
        detailCopyBtn.onClick.AddListener(OnCopyBtnClick);
        detailRenameBtn.onClick.AddListener(OnRenameClick);
        detailCoverBtn.onClick.AddListener(OnDetailsBtnClick);
        detailHideButton.onClick.AddListener(Hide);
        PropRetrySavingBtn.onClick.AddListener(OnRetrySaveBtnClick);
        detailChangeTypeBtn.onClick.AddListener(OnChangeTypeBtnClick);
    }

    public void UpdateInfo(DraftListItem draftListItem) {
        gameObject.SetActive(true);
        var skinInfo = draftListItem.skinInfo;
        if (skinInfo == null || string.IsNullOrEmpty(skinInfo.id)) {
            return;
        }

        curInfo = draftListItem;
        detailName.SetText(skinInfo.name);
        detailSkinImageBg.gameObject.SetActive(false);
        if (skinInfo.cover != null) {
            detailSkinRBehaviour.Load(skinInfo.cover, true, (from, success) => {
                if (detailSkinImageBg != null) {
                    detailSkinImageBg.gameObject.SetActive(true);
                }
            });
        }

        lastEditText.SetText(
            TimestampConverter.ConvertToDateTimeString(GameStudioUtils.GetBaseInfo(draftListItem).updateTime));

        UploadStatus uploadStatus = GameStudioUtils.GetUploadStatus(curInfo);

        detailRenameBtn.gameObject.SetActive(true);
        detailDeleteBtn.gameObject.SetActive(true);
        detailCopyBtn.gameObject.SetActive(true);
        detailPublishBtn.gameObject.SetActive(true);
        switch (uploadStatus) {
            case UploadStatus.NotUpload:
            case UploadStatus.UploadSuccess:
                PropSyncingBg.SetActive(false);
                PropRetrySavingBg.SetActive(false);
                detailEditBtn.gameObject.SetActive(true);
                break;
            case UploadStatus.Uploading:
                PropSyncingBg.SetActive(true);
                PropRetrySavingBg.SetActive(false);
                detailEditBtn.gameObject.SetActive(false);
                break;
            case UploadStatus.UploadFail:
                PropSyncingBg.SetActive(false);
                PropRetrySavingBg.SetActive(true);
                detailEditBtn.gameObject.SetActive(false);
                break;
        }

        bool isLoadingOrFail = uploadStatus == UploadStatus.Uploading || uploadStatus == UploadStatus.UploadFail;
        detailRenameBtn.gameObject.SetActive(!isLoadingOrFail);
        detailCopyBtn.gameObject.SetActive(!isLoadingOrFail);
        detailDeleteBtn.gameObject.SetActive(!isLoadingOrFail);
        detailPublishBtn.gameObject.SetActive(!isLoadingOrFail);

        bool isProp = GameStudioUtils.GetIsProp(draftListItem);
        detailChangeTypeBtn.gameObject.SetActive(isProp);
    }

    private void OnEditBtnClick() {
        editAction?.Invoke(curInfo);
        Hide();
    }

    private void OnDeleteBtnClick() {
        CommonConfirmPanel commonConfirmPanel =
            UIManager.Inst.OpenPanel<CommonConfirmPanel>(PanelId.CommonConfirmPanel);
        commonConfirmPanel.SetLocalText("确认删除", "你确定要删除该草稿吗？\n 一旦删除就无法找回", "删除", "取消");
        commonConfirmPanel.SetOnClickAction(ConfirmClick, CancelClick);
    }


    private void ConfirmClick() {
        if (curInfo != null) {
            var req = new SetSkinInfoReq {
                skinInfo = curInfo.skinInfo,
                setType = (int)SetType.Delete
            };

            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.SetSkin, HttpMethod.POST,
                JsonConvert.SerializeObject(req),
                OnDeleteSuccess, OnDeleteFail);
        }
    }

    private void CancelClick() {
    }

    private void OnDeleteSuccess(string msg) {
        deleteGameAction?.Invoke(curInfo);
        Hide();
    }

    private void OnDeleteFail(string failMsg) {
    }

    /// <summary>
    /// 修改信息
    /// </summary>
    private void OnRenameClick() {
        var name = GameStudioUtils.GetBaseInfo(curInfo).name;
        var panel = UIManager.Inst.OpenPanel<EditNamePanel>(PanelId.EditNamePanel, "修改名字", name, "确定");
        panel.SetOnClickAction((content) => {
            ConfirmEditName(curInfo, content);
        });
    }

    public void ConfirmEditName(DraftListItem item, string name) {
        if (item != null) {
            item.skinInfo.name = name;
            var req = new SetSkinInfoReq {
                skinInfo = item.skinInfo,
                setType = (int)SetType.Edit
            };
            item.skinInfo.name = name;
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.SetSkin, HttpMethod.POST,
                JsonConvert.SerializeObject(req),
                OnEditSuccess, OnEditFail);
        }
    }

    /// <summary>
    /// 修改名字成功
    /// </summary>
    /// <param name="msg"></param>
    private void OnEditSuccess(string msg) {
        UIManager.Inst.ClosePanel(PanelId.EditNamePanel);
        editInfoAction.Invoke(curInfo);
    }

    /// <summary>
    /// 修改名字失败
    /// </summary>
    /// <param name="failMsg"></param>
    private void OnEditFail(string failMsg) {
        UIManager.Inst.ClosePanel(PanelId.EditNamePanel);
        HttpResponseFailDataStruct repData = JsonConvert.DeserializeObject<HttpResponseFailDataStruct>(failMsg);
        string errormessage = repData.rmsg;
        if (!String.IsNullOrEmpty(errormessage)) {
            TipPanel.ShowToast(errormessage);
        }
    }

    /// <summary>
    /// 复制
    /// </summary>
    private void OnCopyBtnClick() {
        if (curInfo != null) {
            var req = new SetSkinInfoReq {
                skinInfo = curInfo.skinInfo,
                setType = (int)SetType.Copy
            };

            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.SetSkin, HttpMethod.POST,
                JsonConvert.SerializeObject(req),
                CopySuccess, CopyFail);
        }
    }

    private void CopySuccess(string msg) {
        copyGameAction?.Invoke();
    }

    private void CopyFail(string failMsg) {
    }

    private void OnPublishBtnClick() {
        // PublishCurrencyPanel publishCurrencyPanel = UIManager.Inst.OpenPanel<PublishCurrencyPanel>(PanelId.PublishCurrencyPanel);
        // publishCurrencyPanel.SetCallback((type =>
        // {
        //     GoToPublishView(type);
        // }));
        GoToPublishView(CurrencyType.PinkCoin);
    }
    private void OnRetrySaveBtnClick() {
        retrySaveAction?.Invoke(curInfo);
        Hide();
    }


    private void OnDetailsBtnClick() {
        // PublishCurrencyPanel publishCurrencyPanel = UIManager.Inst.OpenPanel<PublishCurrencyPanel>(PanelId.PublishCurrencyPanel);
        // publishCurrencyPanel.SetCallback((type =>
        // {
        //     GoToPublishView(type);
        // }));
        GoToPublishView(CurrencyType.PinkCoin);
    }

    private void OnChangeTypeBtnClick()
    {
        if (curInfo != null)
        {
            UIManager.Inst.OpenPanel(PanelId.ChangeUGCTypePanel,curInfo);
        }
        Hide();
    }


    private void GoToPublishView(CurrencyType currencyType) {
        if (curInfo == null)  { return; }
        var tmpSkinInfo = curInfo?.skinInfo.Clone();
        if (tmpSkinInfo == null)  { return; }

        bool isVip = VipDataManager.Inst.isVip;
        if (tmpSkinInfo.isProp)
        {
            var detailInfo = tmpSkinInfo.skinDetailInfo;
            if (detailInfo != null && !GameProfilerManager.Inst.CheckStatisticInfoIsPass(LimitType.Prop, detailInfo))
            {
                UIManager.Inst.OpenPanel<PublishWarningPanel>(PanelId.PublishWarningPanel, LimitType.Prop, detailInfo);
                return;
            }
        }

        var publishMachine = new UGCPublishStateMachine();
        var stateList = new List<UGCPublishStateBase>() {
        };

        if (tmpSkinInfo.imgs != null && tmpSkinInfo.imgs.Length > 0 && !isVip)
        {
            string titleStr = LocalizationManager.Inst.GetLocalizedText("您正在使用的VIP功能：添加手机相册图片");
            var panel = UIManager.Inst.OpenPanel<JoinVipPanel>(PanelId.JoinVipPanel,titleStr,new List<JoinVipType>
            {
                JoinVipType.Image
            });
            return;
        }

        switch ((SkinType)tmpSkinInfo.skinType)
        {
            case SkinType.Pet:
                if (tmpSkinInfo.isProp) {
                    if (curInfo.skinInfo.coverAutoSaved != CoverSaveStatus.ManualSaved) {
                        stateList.Add(new UGCPublishStateBase(UGCPublishState.SetPropSkinCover));
                    }
                    stateList.Add(new UGCPublishStateBase(UGCPublishState.SetPropSkinAnchor));
                    stateList.Add(new UGCPublishStateBase(UGCPublishState.SetPetSkinAdjust));
                    if (tmpSkinInfo.skinDetailInfo == null) {
                        var oriConfigData = AvatarCommonData.From(tmpSkinInfo);
                        var detailInfo = new SkinDetailInfo();
                        detailInfo.pDef = oriConfigData.pDef;
                        detailInfo.rDef = oriConfigData.rDef;
                        detailInfo.sDef = oriConfigData.sDef;
                        tmpSkinInfo.skinDetailInfo = detailInfo;
                    }
                }
                bool isFace = GameConsts.TransparentPart.Contains(tmpSkinInfo.templateId);
                if (isFace)
                {
                    stateList.Add(new UGCPublishStateBase(UGCPublishState.SetPetSkinAdjust));
                    if (tmpSkinInfo.skinDetailInfo==null)
                    {
                        var oriConfigData = Es.DataTables.GetPetAvatarCommonData(tmpSkinInfo.templateId);
                        var detailInfo = new SkinDetailInfo();
                        detailInfo.pDef = oriConfigData.pDef;
                        detailInfo.rDef = oriConfigData.rDef;
                        detailInfo.sDef = oriConfigData.sDef;
                        tmpSkinInfo.skinDetailInfo = detailInfo;
                    }
                }
                break;
            
            default:
            case SkinType.Avatar:
                if (tmpSkinInfo.isProp) {
                    if (curInfo.skinInfo.coverAutoSaved != CoverSaveStatus.ManualSaved) {
                        stateList.Add(new UGCPublishStateBase(UGCPublishState.SetPropSkinCover));
                    }
                    stateList.Add(new UGCPublishStateBase(UGCPublishState.SetPropSkinAnchor));
                    stateList.Add(new UGCPublishStateBase(UGCPublishState.SetPropSkinAdjust));
                    if (tmpSkinInfo.skinDetailInfo == null) {
                        var oriConfigData = AvatarCommonData.From(tmpSkinInfo);
                        var detailInfo = new SkinDetailInfo();
                        detailInfo.pDef = oriConfigData.pDef;
                        detailInfo.rDef = oriConfigData.rDef;
                        detailInfo.sDef = oriConfigData.sDef;
                        tmpSkinInfo.skinDetailInfo = detailInfo;
                    }
                }
                isFace = GameConsts.TransparentPart.Contains(tmpSkinInfo.templateId);
                if (isFace)
                {
                    stateList.Add(new UGCPublishStateBase(UGCPublishState.SetPropSkinAdjust));
                    if (tmpSkinInfo.skinDetailInfo==null)
                    {
                        var oriConfigData = Es.DataTables.GetAvatarCommonData(tmpSkinInfo.templateId);
                        var detailInfo = new SkinDetailInfo();
                        detailInfo.pDef = oriConfigData.pDef;
                        detailInfo.rDef = oriConfigData.rDef;
                        detailInfo.sDef = oriConfigData.sDef;
                        tmpSkinInfo.skinDetailInfo = detailInfo;
                    }
                }
                break;
        }
        
        stateList.Add(new UGCSkinDetailState());
        publishMachine.SetStates(stateList);
        var draftInfo = SkinAssetManager.Inst.GetOrCreateDraftInfo(tmpSkinInfo);
        if (draftInfo == null)
        {
            return;
        }

        publishMachine.SetEditData(new SkinEditData() {
            draftInfo = draftInfo,
            currencyType = currencyType
        });
        publishMachine.SetCancelCallBack(() => {
        });
        publishMachine.SetFinishCallBack(() => {
            publishGameAction?.Invoke(true);
            Hide();
        });

        publishMachine.Start();
    }

    public void Hide() {
        gameObject.SetActive(false);
    }
}
