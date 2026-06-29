using Game.Base;
using Game.GameSync;
using GameData;
using GameData.UGCData;
using UI.BaseWidgets;
using UI.UIPanels.ProfilePanel;
using UnityEngine;
using UnityEngine.UI;

public class SearchFriendItem : MonoBehaviour
{
    [SerializeField] private HeadViewWidget headViewWidget;
    [SerializeField] private SuperTextMesh userNameTxt;
    [SerializeField] private GameObject onlineImg;
    [SerializeField] private CButton joinButton;
    [SerializeField] private CText mapName;
    [SerializeField] private CButton headButton;
    [SerializeField] private RectTransform roomName;
    [SerializeField] private CButton chatBtn;

    private MyFriendsInfo myFriendsInfo;

    private void Start()
    {
        joinButton.onClick.AddListener(() =>
        {
            if (myFriendsInfo == null)
            {
                return;
            }

            RoomInfo roomInfo = myFriendsInfo.roomInfo;
            if (roomInfo != null)
            {
                string roomCode = roomInfo.roomCode;
                GameSyncHttpHelper.Inst.EnterRoomByRoomCode(roomCode, OnEnterRoomSuccess, OnEnterRoomFail);
            }
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
        
                
        chatBtn.onClick.AddListener(() =>
        {
            OnEnterChat();
        });


    }
    
    private void OnEnterChat()
    {
        if (myFriendsInfo != null)
        {
            UIManager.Inst.OpenPanel<GameHallChatPanel>(PanelId.GameHallChatPanel, myFriendsInfo.userInfo.uid);
        }
    }

    private void OnEnterRoomSuccess(UgcInfoRsp rspData)
    {
        if(this == null)
            return;

        var p = UIManager.Inst.OpenPanel<UgcLoadingPanel>(PanelId.UgcLoadingPanel);
        p.Init(rspData.mapInfo, rspData.creator, LoadingType.Map);
        GameController.StartGame(EnterGameModel.GuestScene, rspData.mapInfo);
        // Btn_Confirm.HideLoading();
    }

    private void OnEnterRoomFail()
    {
        if(this == null)
            return;

        // Btn_Confirm.HideLoading();
    }

    public void Init()
    {

    }

    public void SetData(MyFriendsInfo info)
    {

        if (info == null)
        {
            return;
        }

        myFriendsInfo = info;
        var userInfo = info.userInfo;
        if (headViewWidget != null)
        {
            headViewWidget.InitHeadCycle(userInfo);
        }
        userNameTxt.text = userInfo.nickname;
        onlineImg.gameObject.SetActive(info.isOnline == 1);

        RoomInfo roomInfo = info.roomInfo;
        if (roomInfo != null)
        {
            joinButton.gameObject.SetActive(true);
            mapName.text = roomInfo.mapName;
            LayoutRebuilder.ForceRebuildLayoutImmediate(roomName);
        }
        else
        {
            joinButton.gameObject.SetActive(false);
            mapName.text = "";
        }
    }

}
