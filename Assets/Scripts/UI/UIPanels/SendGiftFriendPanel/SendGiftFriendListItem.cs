
using Game.Store;
using GameData.Base;
using UI.BaseWidgets;
using UI.UIPanels.ProfilePanel;
using UnityEngine;

public class SendGiftFriendListItem : MonoBehaviour
{
    [SerializeField] private HeadViewWidget headViewWidget;
    [SerializeField] private CText userNameTxt;
    [SerializeField] private CText userIdTxt;
    [SerializeField] private GameObject onlineImg;
    [SerializeField] private CButton headButton;
    [SerializeField] private CButton sendGiftFriendButton;
    [SerializeField] private CButton ownedButton;
    
    private MyFriendsInfo myFriendsInfo;
    private GoodsData goodsData;
    private int giftType = 0;
    

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
            UIManager.Inst.OpenPanel(PanelId.EditMailPanel,myFriendsInfo, goodsData, null, giftType);
        });
        
        ownedButton.onClick.AddListener(() =>
        {
            
        });
    }

    public void Init()
    {
    }

    public void SetData(MyFriendsInfo info, GoodsData goodsData, int giftType)
    {
        if (info == null)
        {
            return;
        }

        headViewWidget?.InitHeadCycle(info.userInfo);
        myFriendsInfo = info;
        this.goodsData = goodsData;
        this.giftType = giftType;
        var userInfo = info.userInfo;
        userNameTxt.text = userInfo.nickname;
        onlineImg.gameObject.SetActive(info.isOnline == 1);
        userIdTxt.text = "ID:" + userInfo.username;

        BaseInteractInfo interactInfo = info.interactInfo;
        if(interactInfo != null)
        {
            int consumed = interactInfo.consumed;
            ownedButton.gameObject.SetActive(consumed > 0);
            sendGiftFriendButton.gameObject.SetActive(consumed <= 0);
        }
    }
    


}