using System;
using System.Collections;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.Util.IO;
using Game.Base;
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
using UnityEngine;
using UnityEngine.UI;

public class AssetRightMenuView : MonoBehaviour
{
    private Text detailName;
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
    private GameObject uiMask;
    private DraftListItem curInfo;
    private Action<DraftListItem> editAction;
    private Action<DraftListItem> editInfoAction;

    private Action copyGameAction;
    private Action<DraftListItem> deleteGameAction;
    private Action<bool> updateGameAction;
    private Action<DraftListItem> reuploadAction;
    private Action<bool> publishGameAction;

    public void InitUI()
    {
        InitUIComponent();
        AddBtnListener();
    }
    public void InitAction(Action<bool> publishGameAction,Action<DraftListItem> editAction,Action<DraftListItem> editInfoAction,Action copyGameAction,Action<DraftListItem> deleteGameAction)
    {
        this.publishGameAction = publishGameAction;
        this.editAction = editAction;
        this.editInfoAction = editInfoAction;
        this.copyGameAction = copyGameAction;
        this.deleteGameAction = deleteGameAction;

    }
    private void InitUIComponent()
    {
        detailName = GameObjectEx.FindChildByName(this.transform, "DetailTitle").GetComponent<Text>();
        lastEditText = GameObjectEx.FindChildByName(this.transform, "DetailLastEdit").GetComponent<Text>();
        detailRenameBtn = GameObjectEx.FindChildByName(this.transform, "DraftsRename").GetComponent<CButton>();
        detailCopyBtn = GameObjectEx.FindChildByName(this.transform, "DraftsDuplicate").GetComponent<CButton>();
        detailDeleteBtn = GameObjectEx.FindChildByName(this.transform, "DraftsDelete").GetComponent<CButton>();
        detailPublishBtn = GameObjectEx.FindChildByName(this.transform, "DraftsPublish").GetComponent<CButton>();
        detailEditBtn = GameObjectEx.FindChildByName(this.transform, "DraftsEdit").GetComponent<CButton>();
        detailCoverBtn = GameObjectEx.FindChildByName(this.transform, "DetailCover").GetComponent<CButton>();
        detailChangeTypeBtn = GameObjectEx.FindChildByName(this.transform, "DraftsChangeType").GetComponent<CButton>();
        detailSkinImageBg = GameObjectEx.FindChildByName(this.transform, "DetailPropBg").GetComponent<Image>();
        detailSkinRBehaviour = GameObjectEx.FindChildByName(this.transform, "DetailPropImage").GetComponent<RemoteImageBehaviour>();

        detailHideButton = transform.GetComponent<CButton>();
        PropSyncingBg = GameObjectEx.FindChildByName(this.transform, "PropSyncingBg").gameObject;
        PropRetrySavingBg = GameObjectEx.FindChildByName(this.transform, "PropRetrySavingBg").gameObject;

        detailChangeTypeBtn.gameObject.SetActive(false);
    }

    private void AddBtnListener()
    {
        detailEditBtn.onClick.AddListener(OnEditBtnClick);
        detailDeleteBtn.onClick.AddListener(OnDeleteBtnClick);
        detailPublishBtn.onClick.AddListener(OnPublishBtnClick);
        detailCopyBtn.onClick.AddListener(OnCopyBtnClick);
        detailRenameBtn.onClick.AddListener( OnRenameClick);
        detailCoverBtn.onClick.AddListener(OnDetailsBtnClick);
        detailHideButton.onClick.AddListener(Hide);
        detailChangeTypeBtn.onClick.AddListener(OnChangeTypeBtnClick);
    }
    public void UpdateInfo(DraftListItem draftListItem)
    {
        var baseInfo = GameStudioUtils.GetBaseInfo(draftListItem);
        if (baseInfo == null)
        {
            return;
        }
        gameObject.SetActive(true);

        if (string.IsNullOrEmpty(baseInfo.id))
        {
            return;
        }
        curInfo = draftListItem;
        detailName.SetText(baseInfo.name);
        detailSkinImageBg.gameObject.SetActive(false);
        if (baseInfo.cover != null)
        {
            detailSkinRBehaviour.Load(baseInfo.cover, true, (from, success) =>
            {
                if (detailSkinImageBg != null)
                {
                    detailSkinImageBg.gameObject.SetActive(true);
                }
            });
        }
        lastEditText.SetText(TimestampConverter.ConvertToDateTimeString(GameStudioUtils.GetBaseInfo(draftListItem).updateTime));

        UploadStatus uploadStatus = GameStudioUtils.GetUploadStatus(curInfo);

        detailRenameBtn.gameObject.SetActive(true);
        detailDeleteBtn.gameObject.SetActive(true);
        detailCopyBtn.gameObject.SetActive(true);
        detailPublishBtn.gameObject.SetActive(true);
        switch (uploadStatus)
        {
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
        detailRenameBtn.gameObject.SetActive(uploadStatus ==UploadStatus.NotUpload ||
                                             uploadStatus == UploadStatus.UploadSuccess);
        detailCopyBtn.gameObject.SetActive(uploadStatus ==UploadStatus.NotUpload ||
                                           uploadStatus == UploadStatus.UploadSuccess);
        detailDeleteBtn.gameObject.SetActive(uploadStatus ==UploadStatus.NotUpload ||
                                             uploadStatus == UploadStatus.UploadSuccess);
        detailPublishBtn.gameObject.SetActive(uploadStatus ==UploadStatus.NotUpload ||
                                              uploadStatus ==UploadStatus.UploadSuccess);

        bool isProp = GameStudioUtils.GetIsProp(draftListItem);
        detailChangeTypeBtn.gameObject.SetActive(isProp);
    }

    private void OnEditBtnClick()
    {
        editAction?.Invoke(curInfo);
        Hide();
    }

    private void OnDeleteBtnClick()
    {
        CommonConfirmPanel commonConfirmPanel =
            UIManager.Inst.OpenPanel<CommonConfirmPanel>(PanelId.CommonConfirmPanel);
        commonConfirmPanel.SetLocalText("确认删除","你确定要删除该草稿吗？\n 一旦删除就无法找回", "删除", "取消");
        commonConfirmPanel.SetOnClickAction(ConfirmClick, CancelClick);
    }


    private void ConfirmClick()
    {
        if (curInfo == null)
        {
            return;
        }

        var viewType = GameStudioUtils.GetMainViewType(curInfo);
        if (viewType == MainViewType.Prop)
        {
            var req = new SetPropInfoReq()
            {
                propInfo = curInfo.propInfo,
                setType = (int)SetType.Delete
            };

            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.setProp, HttpMethod.POST,
                JsonConvert.SerializeObject(req),
                OnDeleteSuccess, OnDeleteFail);
        }
        else if (viewType == MainViewType.Material)
        {
            var req = new SetMaterialInfoReq()
            {
                materialInfo = curInfo.materialInfo,
                setType = (int)SetType.Delete
            };

            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.SetMaterial, HttpMethod.POST,
                JsonConvert.SerializeObject(req),
                OnDeleteSuccess, OnDeleteFail);
        }
    }

    private void CancelClick()
    {
    }

    private void OnDeleteSuccess(string msg)
    {
        deleteGameAction?.Invoke(curInfo);
        Hide();
    }

    private void OnDeleteFail(string failMsg)
    {
    }
    /// <summary>
    /// 修改信息
    /// </summary>
    private void OnRenameClick()
    {
        var name = GameStudioUtils.GetBaseInfo(curInfo).name;
        var panel = UIManager.Inst.OpenPanel<EditNamePanel>(PanelId.EditNamePanel, "修改名字", name, "确定");
        panel.SetOnClickAction((content)=>
        {
            ConfirmEditName(curInfo,content);
        });
    }
    public void ConfirmEditName(DraftListItem item, string name)
    {
        if (item == null)
        {
            return;
        }

        var viewType = GameStudioUtils.GetMainViewType(item);

        if (viewType == MainViewType.Prop)
        {
            item.propInfo.name = name;
            var req = new SetPropInfoReq()
            {
                propInfo = item.propInfo,
                setType = (int)SetType.Edit
            };

            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.setProp, HttpMethod.POST,
                JsonConvert.SerializeObject(req),
                OnEditSuccess, OnEditFail);
        }
        else if (viewType == MainViewType.Material)
        {
            item.materialInfo.name = name;
            var req = new SetMaterialInfoReq()
            {
                materialInfo = item.materialInfo,
                setType = (int)SetType.Edit
            };

            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.SetMaterial, HttpMethod.POST,
                JsonConvert.SerializeObject(req),
                OnEditSuccess, OnEditFail);
        }
    }
    /// <summary>
    /// 修改名字成功
    /// </summary>
    /// <param name="msg"></param>
    private void OnEditSuccess(string msg)
    {
        UIManager.Inst.ClosePanel(PanelId.EditNamePanel);
        editInfoAction.Invoke(curInfo);
    }

    /// <summary>
    /// 修改名字失败
    /// </summary>
    /// <param name="failMsg"></param>
    private void OnEditFail(string failMsg)
    {
        UIManager.Inst.ClosePanel(PanelId.EditNamePanel);
        HttpResponseFailDataStruct repData = JsonConvert.DeserializeObject<HttpResponseFailDataStruct>(failMsg);
        string errormessage = repData.rmsg;
        if (!String.IsNullOrEmpty(errormessage))
        {
            TipPanel.ShowToast(errormessage);
        }
    }

    /// <summary>
    /// 复制
    /// </summary>
    private void OnCopyBtnClick()
    {
        if (curInfo == null)
        {
            return;
        }

        var viewType = GameStudioUtils.GetMainViewType(curInfo);
        if (viewType == MainViewType.Prop)
        {
            var req = new SetPropInfoReq()
            {
                propInfo = curInfo.propInfo.Clone(),
                setType = (int)SetType.Copy
            };
            req.propInfo.coverAutoSaved = CoverSaveStatus.AutoSaved;

            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.setProp, HttpMethod.POST,
                JsonConvert.SerializeObject(req),
                CopySuccess, CopyFail);
        }
        else if (viewType == MainViewType.Material)
        {
            var req = new SetMaterialInfoReq()
            {
                materialInfo = curInfo.materialInfo,
                setType = (int)SetType.Copy
            };

            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.SetMaterial, HttpMethod.POST,
                JsonConvert.SerializeObject(req),
                CopySuccess, CopyFail);
        }
    }

    private void CopySuccess(string msg)
    {
        copyGameAction?.Invoke();
    }

    private void CopyFail(string failMsg)
    {
    }

    private void OnPublishBtnClick()
    {
        // PublishCurrencyPanel publishCurrencyPanel = UIManager.Inst.OpenPanel<PublishCurrencyPanel>(PanelId.PublishCurrencyPanel);
        // publishCurrencyPanel.SetCallback((type =>
        // {
        //     GoToPublishView(type);
        // }));
        GoToPublishView(CurrencyType.PinkCoin);
    }

    private void OnDetailsBtnClick()
    {
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

    private void GoToPublishView(CurrencyType currencyType)
    {
        if (curInfo == null)
        {
            return;
        }

        var viewType = GameStudioUtils.GetMainViewType(curInfo);

        #region 判断是否通过顶点数校验
        if (viewType == MainViewType.Prop)
        {
            var detailInfo = curInfo.propInfo.detailInfo;

            if (detailInfo != null && !GameProfilerManager.Inst.CheckStatisticInfoIsPass(LimitType.Prop, detailInfo))
            {
                UIManager.Inst.OpenPanel<PublishWarningPanel>(PanelId.PublishWarningPanel, LimitType.Prop, detailInfo);
                return;
            }
        }
        #endregion

        var publishMachine = new UGCPublishStateMachine();

        if (viewType == MainViewType.Material)
        {
            var tmpMaterialInfo = curInfo.materialInfo.Clone();
            bool isVip = VipDataManager.Inst.isVip;
            if (tmpMaterialInfo.imgs != null && tmpMaterialInfo.imgs.Length > 0 && !isVip)
            {
                string titleStr = LocalizationManager.Inst.GetLocalizedText("您正在使用的VIP功能：添加手机相册图片");
                var panel = UIManager.Inst.OpenPanel<JoinVipPanel>(PanelId.JoinVipPanel,titleStr, new List<JoinVipType>
                {
                    JoinVipType.Image
                });
                return;
            }

            publishMachine.SetStates(new List<UGCPublishStateBase>() {
                new UGCMaterialDetailState()
            });

            var tempMaterialInfo = curInfo.materialInfo.Clone();
            var draftInfo = MaterialAssetManager.Inst.GetDraftInfo(tempMaterialInfo);
            if (draftInfo == null)
            {
                draftInfo = new MaterialDraftInfo(tempMaterialInfo);
            }

            publishMachine.SetEditData(new MaterialEditData()
            {
                draftInfo = draftInfo,
                currencyType = currencyType
            });

            publishMachine.SetFinishCallBack(() => {
                publishGameAction?.Invoke(true);
                Hide();
            });
            publishMachine.Start();


        } else if (viewType == MainViewType.Prop)
        {

            var tmpPropInfo = curInfo.propInfo.Clone();
            var states = new List<UGCPublishStateBase>() {
                new UGCAnchorState(),
                new UGCPublishSizeState(),
                new UGCPropDetailState()
            };

            if (tmpPropInfo.coverAutoSaved != CoverSaveStatus.ManualSaved) {
                states.Insert(0, new PropCoverState());
            }

            publishMachine.SetStates(states);

            var draftInfo = PropAssetManager.Inst.GetDraftInfo(tmpPropInfo);
            if (draftInfo == null)
            {
                draftInfo = new PropDraftInfo(tmpPropInfo);
            }

            publishMachine.SetEditData(new PropEditData()
            {
                draftInfo = draftInfo,
                currencyType = currencyType
            });
            publishMachine.SetCancelCallBack(() =>
            {
            });
            publishMachine.SetFinishCallBack(() => {
                publishGameAction?.Invoke(true);
                Hide();
            });
            publishMachine.Start();
        }
    }
    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
