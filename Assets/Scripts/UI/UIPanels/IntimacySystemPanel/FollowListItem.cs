using Basic.Utils;
using UI.BaseWidgets;
using UI.UIPanels.ProfilePanel;
using UnityEngine;

public class FollowListItem : MonoBehaviour
{
    [SerializeField] private CText userNameTxt;
    [SerializeField] private CText fanNum;
    [SerializeField] private GameObject onlineImg;
    [SerializeField] private CButton headButton;
    [SerializeField] private HeadViewWidget headViewWidget;

    private MyFriendsInfo myFriendsInfo;

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

        headViewWidget?.InitHeadCycle(info.userInfo);
        myFriendsInfo = info;
        var userInfo = info.userInfo;
        userNameTxt.text = userInfo.nickname;
        onlineImg.gameObject.SetActive(info.isOnline == 1);
        var fansAmountTxt = GameUtils.ToBudCommonNumString(info.amount.fansAmount);
        fanNum.SetLocalText("{0}粉丝",fansAmountTxt);
    }
    


}