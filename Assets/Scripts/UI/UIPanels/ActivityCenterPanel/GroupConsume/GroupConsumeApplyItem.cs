using System;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.Util.IO;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.BaseWidgets;
using UI.UIPanels.ProfilePanel;
using UnityEngine;
using UnityEngine.UI;

public class GroupConsumeApplyItem : MonoBehaviour {
    [HideInInspector]
    public RemoteImageBehaviour avatarImage;


    private HeadViewWidget headCycle;

    private GroupConsumeApplyType applyType;
    private GroupConsumeBaseInfo baseInfo;

    private CButton closeBtn;
    private CButton refuseBtn;
    private CButton agreeBtn;
    private CButton deleteBtn;
    private CButton inviteBtn;
    private CButton headButton;
    private GameObject invitedObj;
    private CButton joinBtn;



    private Text inviteText;
    private Text idText;



    private void Awake() {
        avatarImage = GameObjectEx.FindComponentByName<RemoteImageBehaviour>(transform, "Avatar/RawImage");
        headCycle = GameObjectEx.FindComponentByName<HeadViewWidget>(transform, "Avatar/HeadViewWidget");
        inviteText = GameObjectEx.FindComponentByName<Text>(transform, "InviteText");
        idText = GameObjectEx.FindComponentByName<Text>(transform, "IDText");
        closeBtn = GameObjectEx.FindComponentByName<CButton>(transform, "CloseBtn");
        closeBtn.onClick.AddListener(OnCloseBtnClicked);

        refuseBtn = GameObjectEx.FindComponentByName<CButton>(transform, "BtnContainer/RefuseBtn");
        refuseBtn.onClick.AddListener(OnRefuseBtnClicked);

        agreeBtn = GameObjectEx.FindComponentByName<CButton>(transform, "BtnContainer/AgreeBtn");
        agreeBtn.onClick.AddListener(OnAgreeBtnClicked);

        deleteBtn = GameObjectEx.FindComponentByName<CButton>(transform, "BtnContainer/DeleteBtn");
        deleteBtn.onClick.AddListener(OnDeleteBtnClicked);

        joinBtn = GameObjectEx.FindComponentByName<CButton>(transform, "BtnContainer/JoinBtn");
        joinBtn.onClick.AddListener(OnJoinBtnClicked);

        inviteBtn = GameObjectEx.FindComponentByName<CButton>(transform, "BtnContainer/InviteBtn");
        inviteBtn.onClick.AddListener(OnInviteBtnClicked);
        invitedObj = GameObjectEx.FindChildByName(transform, "BtnContainer/InvitedBtn").gameObject;

        headButton = GameObjectEx.FindComponentByName<CButton>(transform, "Avatar/HeadViewWidget/HeadBtn");
        headButton.onClick.AddListener(() =>
        {
            if (baseInfo.userInfo == null)
            {
                return;
            }

            string uid = baseInfo.userInfo.uid;
            if (string.IsNullOrEmpty(uid))
            {
                return;
            }

            UIManager.Inst.OpenPanel<ProfilePanel>(PanelId.ProfilePanel, uid);
        });
    }




    public void SetData(GroupConsumeBaseInfo model, GroupConsumeApplyType type) {
        baseInfo = model;
        applyType = type;
        closeBtn.gameObject.SetActive(false);
        refuseBtn.gameObject.SetActive(false);
        agreeBtn.gameObject.SetActive(false);
        deleteBtn.gameObject.SetActive(false);
        joinBtn.gameObject.SetActive(false);
        inviteBtn.gameObject.SetActive(false);
        invitedObj.SetActive(false);
        avatarImage.Load(model.userInfo.portraitUrl);
        idText.SetText($"ID:{model.userInfo.username}");
        var cycleId = model.userInfo.avatarFrame;
        //var headCycleData = UserUIWidgetManager.Inst.GetHeadCycleData(cycleId, this.gameObject);
        //headCycle.sprite = headCycleData.Sp_PreviewCycle;
        headCycle.InitHeadCycle(cycleId);
        switch (applyType) {
            case GroupConsumeApplyType.Friends:
                HandleFriend();
                break;
            case GroupConsumeApplyType.Invited:
                HandleInvited();
                break;
            case GroupConsumeApplyType.Applied:
                HandleApply();
                break;
            case GroupConsumeApplyType.Leaved:
                HandleLeave();
                break;
        }
    }


    void HandleFriend() {
        inviteText.SetText(baseInfo.userInfo.nickname);
        var info = baseInfo as GroupConsumeApplyPanel.GroupInviteUserInfo;
        if (info == null) {
            return;
        }
        if (info.isInvited == 1 || info.isInGroup == 1) {
            invitedObj.SetActive(true);
        } else {
            inviteBtn.gameObject.SetActive(true);
        }
    }

    private void HandleInvited() {
        inviteText.SetLocalText("{0}邀请你加入TA的队伍", baseInfo.userInfo.nickname);
        refuseBtn.gameObject.SetActive(true);
        joinBtn.gameObject.SetActive(true);
        closeBtn.gameObject.SetActive(true);

    }

    private void HandleApply() {
        inviteText.SetLocalText("{0}申请加入你的队伍", baseInfo.userInfo.nickname);
        refuseBtn.gameObject.SetActive(true);
        agreeBtn.gameObject.SetActive(true);
    }

    private void HandleLeave() {
        inviteText.SetLocalText("{0}退出你的队伍", baseInfo.userInfo.nickname);
        deleteBtn.gameObject.SetActive(true);
    }



    private void OnCloseBtnClicked() {
        var info = baseInfo as GroupConsumeApplyPanel.GroupConsumeApplyInfo;
        if (info == null) {
            return;
        }
        CommonReq(HttpUrlDefine.GroupInviteHandle, info.recordId, HandleType.Delete);

    }

    private void OnRefuseBtnClicked() {
        var info = baseInfo as GroupConsumeApplyPanel.GroupConsumeApplyInfo;
        if (info == null) {
            return;
        }

        if (applyType == GroupConsumeApplyType.Invited) {
            CommonReq(HttpUrlDefine.GroupInviteHandle, info.recordId, HandleType.Refuse);
        } else if (applyType == GroupConsumeApplyType.Applied) {
            CommonReq(HttpUrlDefine.GroupApplyHandle, info.recordId, HandleType.Refuse);
        }

    }

    private void OnAgreeBtnClicked() {
        var info = baseInfo as GroupConsumeApplyPanel.GroupConsumeApplyInfo;
        if (info == null) {
            return;
        }
        CommonReq(HttpUrlDefine.GroupApplyHandle, info.recordId, HandleType.Agree);

    }
    private void OnDeleteBtnClicked() {
        var info = baseInfo as GroupConsumeApplyPanel.GroupConsumeApplyInfo;
        if (info == null) {
            return;
        }
        CommonReq(HttpUrlDefine.GroupLeaveHandle, info.recordId, HandleType.Delete);
    }
    private void OnJoinBtnClicked() {
        var info = baseInfo as GroupConsumeApplyPanel.GroupConsumeApplyInfo;
        if (info == null) {
            return;
        }
        CommonReq(HttpUrlDefine.GroupInviteHandle, info.recordId, HandleType.Agree);
    }

    private void OnInviteBtnClicked() {
        var info = baseInfo as GroupConsumeApplyPanel.GroupInviteUserInfo;
        if (info == null) {
            return;
        }
        var req = new JObject() {
            ["businessId"] = ActivityId.LaborDayGroupConsume.ToString(),
            ["uid"] = baseInfo.userInfo.uid,
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.GroupInvite,
            HttpMethod.POST,
            JsonConvert.SerializeObject(req),
            onReceive: msg => {
                MessageHelper.Broadcast(MessageName.RefreshGroupConsumeApplyData);
            }, onFail: arg0 => {
                LoggerUtils.LogError("请求失败");
            });
    }


    private void CommonReq(string url, string recordId, HandleType handleType) {
        var req = new JObject() {
            ["recordId"] = recordId,
            ["handleType"] = (int)handleType
        };
        NetworkManager.Inst.SendHttpRequest(url,
            HttpMethod.POST,
            JsonConvert.SerializeObject(req),
            onReceive: msg => {
                MessageHelper.Broadcast(MessageName.RefreshGroupConsumeApplyData);
            }, onFail: arg0 => {
                LoggerUtils.LogError("请求失败");
            });

    }


    public enum HandleType {
        None = 0,
        Agree = 1,
        Refuse = 2,
        Delete = 3
    }

}
