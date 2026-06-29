using System;
using GameData.Base;
using GameData.BaseInfo;
using Message;
using Network;
using Network.Http;
using UIAgent;
using UnityEngine;

namespace UGCAsset.Draft
{
    public class PoseDraftInfo : BaseDraftInfo<PoseDraftInfo, PoseInfo>
    {
        public bool isQuickSave = false;
        protected override string SetUrl => HttpUrlDefine.SetPose;
        protected sealed override string CoverRemoteFolder => $"PoseCover/{uid}";
        protected sealed override string MetaDataRemoteFolder => $"PoseMetadata/{uid}";

        public PoseDraftInfo()
        {
        }

        public PoseDraftInfo(PoseInfo propInfo) : base(propInfo)
        {
        }

        public override void EditDraftToServer(Action<PoseDraftInfo, bool> callBack = null)
        {
            var ugcInfo = ToUgcInfo();
            var req = new UGCSetRequest(ugcInfo, UGCOperationType.Edit);
            NetworkManager.Inst.SendHttpRequest<DraftListItem>(isQuickSave ? HttpUrlDefine.saveQuickPose : SetUrl,
                HttpMethod.POST, req, draftListItem =>
                {
                    callBack?.Invoke(this, true);
                    isSaveDraft = true;
                    MessageHelper.Broadcast(DraftMessage.DraftSaveStatus, this);
                }, fail =>
                {
                    Debug.LogError($"保存草稿信息失败 [{ugcInfo.id}]:" + fail);
                    callBack?.Invoke(this, false);
                });
        }

        public override void PublishDraftToServer(Action<PoseInfo, bool> callBack)
        {
            var ugcInfo = ToUgcInfo();
            var req = new UGCSetRequest(ugcInfo, UGCOperationType.Publish);
            if (UIAgentManager.Inst.FindPanel(WindowId.AnimWindow, PanelId.AnimPoseEditPanel))
            {
                req = new UGCSetRequest(ugcInfo, UGCOperationType.ImmediatePublish);
            }
            
            NetworkManager.Inst.SendHttpRequest<DraftListItem>(SetUrl, HttpMethod.POST, req, draftListItem => {
                callBack(draftListItem?.Get<PoseInfo>(), true);
                isSaveDraft = true;
                MessageHelper.Broadcast(DraftMessage.DraftSaveStatus, this as PoseDraftInfo);
            }, rsp => {
                if (rsp.result == 501) {
                    // 审核失败
                    Action<bool> onAppealCallBack = (isAppeal) => {
                        // 提交申诉，视作发布成功
                        if (isAppeal) {
                            if (ugcInfo.auditInfo == null)
                            {
                                ugcInfo.auditInfo = new AuditStatus()
                                {
                                    auditResult = 4
                                };
                            }
                            else
                            {
                                ugcInfo.auditInfo.auditResult = 4;
                            }
                            callBack(ugcInfo, true);
                            isSaveDraft = true;
                            MessageHelper.Broadcast(DraftMessage.DraftSaveStatus, this as PoseDraftInfo);
                        } else {
                            // 放弃申诉，视作发布失败
                            callBack(null, false);
                        }
                    };
                    UIAgentManager.Inst.OpenPanel(PanelId.UGCAuditRejectedPanel, WindowId.None, req, rsp.rmsg, onAppealCallBack);
                } else {
                    HttpErrorCodeHandler errorCodeHandler = new Network.Http.HttpErrorCodeHandler();
                    errorCodeHandler.HandleErrorCodeResult(rsp);
                    LoggerUtils.LogError($"发布失败 [{ugcInfo.id}]:" + rsp.rmsg);
                    callBack(null, false);
                }
            }, null, 0, 0, true, false);
        }
    }

}