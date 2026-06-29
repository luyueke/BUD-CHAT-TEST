using System;
using System.Collections;
using System.Collections.Generic;
using GameData.Base;
using GameData.BaseInfo;
using Network;
using Network.Http;
using UGCAsset;
using UI.Base;
using UI.BaseWidgets;
using UI.UIWidgets;
using UnityEngine;
using UnityEngine.UI;

public class UGCAuditRejectedPanel : BasePanel<UGCAuditRejectedPanel> {

    #region 审核不通过UI
    [Header("审核不通过UI")]
    [SerializeField]
    private GameObject rejectedContainer;

    [SerializeField]
    private CButton rejectedCloseBtn;

    [SerializeField]
    private CButton rejectedAppealBtn;

    [SerializeField]
    private Text rejectedTipText;
    #endregion

    #region 申诉UI
    [Header("申诉UI")]
    [SerializeField]
    private GameObject appealContainer;
    [SerializeField]
    private CButton appealCloseBtn;

    [SerializeField]
    private CButton appealCommitBtn;

    [SerializeField]
    private Button appealContentBtn;


    [SerializeField]
    private TextInputView appealInputView;


    #endregion

    public class RejectedAppealArgs {
        public UGCSetRequest req;
        public string reason;
        public Action<bool> onRejectedCallBack;
    }

    private RejectedAppealArgs appealArgs;

    // private

    public override void OnCreate() {
        base.OnCreate();
        rejectedCloseBtn.onClick.AddListener(OnCloseBtnClick);
        rejectedAppealBtn.onClick.AddListener(OnAppealBtnClick);
        appealCommitBtn.onClick.AddListener(OnAppealCommitBtnClick);
        appealCloseBtn.onClick.AddListener(OnCloseBtnClick);
    }



    public override void OnShow(params object[] args) {
        base.OnShow(args);

        if (args.Length == 1) {
            appealArgs = args[0] as RejectedAppealArgs;
        } else {
            appealArgs = new RejectedAppealArgs() {
                req = args[0] as UGCSetRequest,
                reason = (string)args[1],
                onRejectedCallBack = (Action<bool>)args[2]
            };
        }


        rejectedTipText.text = appealArgs.reason;
        rejectedContainer.SetActive(true);
        appealContainer.SetActive(false);
    }

    private void OnCloseBtnClick() {
        appealArgs.onRejectedCallBack?.Invoke(false);
        CloseSelf();
    }

    private void OnAppealBtnClick() {
        rejectedContainer.SetActive(false);
        appealContainer.SetActive(true);
    }

    private void OnAppealCommitBtnClick() {
        appealArgs.req.setType = UGCOperationType.Appeal;
        appealArgs.req.appealReason = appealInputView.Input;
        NetworkManager.Inst.SendHttpRequest<DraftListItem>(appealArgs.req.GetSetUrl(), HttpMethod.POST, appealArgs.req, draftListItem => {
            appealArgs.onRejectedCallBack?.Invoke(true);
            CloseSelf();
            TipPanel.ShowToast("申诉已提交");
        }, fail => {
            appealArgs.onRejectedCallBack?.Invoke(true);
            CloseSelf();
            TipPanel.ShowToast("申诉已提交");
        });
    }


}
