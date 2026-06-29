using Network;
using Network.Http;
using Newtonsoft.Json;
using UI.BaseWidgets;
using UnityEngine;

namespace UI.UIPanels.ProfilePanel
{
    public class ProfileBottomView : MonoBehaviour
    {
        public CButton unfollowBtn;
        public CButton deleteFriendBtn;
        public ReportButton reportBtn;
        public CButton backBtn;

        private string targetUid;
        private RelationShipData _relationShipData;

        private void Start()
        {
            backBtn.onClick.AddListener(() =>
            {
                this.gameObject.SetActive(false);
            });
            unfollowBtn.onClick.AddListener(() =>
            {
                if (string.IsNullOrEmpty(targetUid))
                {
                    return;
                }
                SetRealtionParams setRelationReq = new SetRealtionParams();
                setRelationReq.setType = (int)SetRelationType.Cancel;
                setRelationReq.relationship = (int)RelationShipType.Follow;
                setRelationReq.targetUid = targetUid;
                NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.setRelation, HttpMethod.POST,
                    JsonConvert.SerializeObject(setRelationReq),
                    UnfollowSuccess, UnfollowFailed);
            });
            
            deleteFriendBtn.onClick.AddListener(() =>
            {
                if (string.IsNullOrEmpty(targetUid))
                {
                    return;
                }
                SetRealtionParams setRelationReq = new SetRealtionParams();
                 setRelationReq.setType = (int)SetRelationType.Cancel;
                 setRelationReq.relationship = (int)RelationShipType.Friend;
                 setRelationReq.targetUid = targetUid;
                 NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.setRelation, HttpMethod.POST,
                     JsonConvert.SerializeObject(setRelationReq),
                     UnFriendSuccess, UnFriendFailed);
            });
            
            reportBtn.SetData(targetUid, ErrReportSceneType.User);
        }

        private void UnfollowSuccess(string message)
        {
            TipPanel.ShowToast("取消关注成功");
        }

        private void UnfollowFailed(string message)
        {
           TipPanel.ShowToast(message);
        }
        
        private void UnFriendSuccess(string message)
        {
            TipPanel.ShowToast("删除好友成功");
        }

        private void UnFriendFailed(string message)
        {
            TipPanel.ShowToast(message);
        }

        public void OnInitCreated(string targetUid, RelationShipData relationShipData)
        {
            this.targetUid = targetUid;
            _relationShipData = relationShipData;
            if (relationShipData.followStatus == (int)RelationStatusType.Posi || relationShipData.followStatus == (int)RelationStatusType.Each)
            {
                unfollowBtn.gameObject.SetActive(true);
            }
            else
            {
                unfollowBtn.gameObject.SetActive(false);
            }
            
            if (relationShipData.friendStatus== (int)RelationStatusType.Posi || relationShipData.friendStatus == (int)RelationStatusType.Each)
            {
                deleteFriendBtn.gameObject.SetActive(true);
            }
            else
            {
                deleteFriendBtn.gameObject.SetActive(false);
            }
        }

    }
}
