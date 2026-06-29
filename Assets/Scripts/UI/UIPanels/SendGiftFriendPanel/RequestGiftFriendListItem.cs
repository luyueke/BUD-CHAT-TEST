
using System;
using Game.Store;
using UI.BaseWidgets;
using UI.UIPanels.ProfilePanel;
using UnityEngine;

public class RequestGiftFriendListItem : MonoBehaviour
{
    [SerializeField] private CText userNameTxt;
    [SerializeField] private CText userIdTxt;
    [SerializeField] private GameObject onlineImg;
    [SerializeField] private CButton headButton;
    [SerializeField] private CButton sendGiftFriendButton;
    [SerializeField] private CButton ownedButton;
    [SerializeField] private HeadViewWidget HeadViewWidget;
        
    private MyFriendsInfo myFriendsInfo;
    private GoodsData goodsData;
    private int giftType = 0;
    private Action<GoodsData, MyFriendsInfo> clickAction;

    private void Start()
    {
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
        
        sendGiftFriendButton.onClick.AddListener(() =>
        {
            clickAction?.Invoke(goodsData, myFriendsInfo);
        });
        
        ownedButton.onClick.AddListener(() =>
        {
            
        });
    }

    public void Init()
    {
    }

    public void SetData(MyFriendsInfo info, GoodsData goodsData, int giftType,Action<GoodsData, MyFriendsInfo> clickAction)
    {
        if (info == null)
        {
            return;
        }

        this.clickAction = clickAction;
        myFriendsInfo = info;
        this.goodsData = goodsData;
        this.giftType = giftType;
        var userInfo = info.userInfo;
        userNameTxt.text = userInfo.nickname;
        onlineImg.gameObject.SetActive(info.isOnline == 1);
        userIdTxt.text = "ID:" + userInfo.username;
        
        HeadViewWidget.InitHeadCycle(info.userInfo);
    }
    


}