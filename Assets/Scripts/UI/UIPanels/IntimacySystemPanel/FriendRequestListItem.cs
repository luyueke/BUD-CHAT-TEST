using Network;
using Network.Http;
using Newtonsoft.Json;
using UI.BaseWidgets;
using UI.UIPanels.ProfilePanel;
using UnityEngine;

public class FriendRequestListItem : MonoBehaviour
{
    [SerializeField] private CText userNameTxt;
    [SerializeField] private CButton accpetBtn;
    [SerializeField] private CText acceptTxt;
    [SerializeField] private GameObject onlineImg;
    [SerializeField] private CButton headButton;
    [SerializeField] private HeadViewWidget headViewWidget;
    
    private MyFriendsInfo myFriendsInfo;

    private void Start()
    {
        accpetBtn.onClick.AddListener(() =>
        {
            if (myFriendsInfo == null || myFriendsInfo.userInfo == null || myFriendsInfo.userInfo.uid == null)
            {
                return;
            }

            SetRealtionParams setRelationReq = new SetRealtionParams();
            setRelationReq.setType = (int)SetRelationType.Set;
            setRelationReq.relationship = (int)RelationShipType.Friend;
            setRelationReq.targetUid = myFriendsInfo.userInfo.uid;
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.setRelation, HttpMethod.POST,
                JsonConvert.SerializeObject(setRelationReq),
                OnAddSuccess, OnAddFailed);
        });

        headButton.onClick.AddListener(() =>
        {
            if (myFriendsInfo == null)
            {
                return;
            }

            string uid = myFriendsInfo.userInfo.uid;
            if (string.IsNullOrEmpty(uid))
            {
                return;
            }

            UIManager.Inst.OpenPanel<ProfilePanel>(PanelId.ProfilePanel, uid);
        });
    }

    private void OnAddSuccess(string message)
    {
        acceptTxt.SetLocalText("好友");
        accpetBtn.enabled = false;
    }

    private void OnAddFailed(string msg)
    {
        LoggerUtils.LogError(msg);
    }


    public void SetData(MyFriendsInfo info)
    {
        if (info == null)
        {
            return;
        }

        myFriendsInfo = info;
        var userInfo = info.userInfo;
        userNameTxt.text = userInfo.nickname;
        onlineImg.gameObject.SetActive(info.isOnline == 1);

        headViewWidget.InitHeadCycle(info.userInfo);
    }
}
