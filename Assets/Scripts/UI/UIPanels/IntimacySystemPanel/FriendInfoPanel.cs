
using Com.TheFallenGames.OSA.Util.IO;

using UI.Base;
using UI.BaseWidgets;
using UnityEngine;

namespace UI.UIPanels.FriendList
{
    public class FriendInfoPanel : BasePanel<FriendInfoPanel>
    {
        public CButton BackBtn;
        public AddFriendButton AddFriendButton;
        public HeadViewWidget HeadViewWidget;
        public CText userName;
        public CText serachtargetId;
        public CButton headButton;

        private string uid;
        public override void OnCreate()
        {
            BackBtn.onClick.AddListener(OnBackBtnClick);
            headButton.onClick.AddListener(() =>
            {
                if (string.IsNullOrEmpty(uid))
                {
                    return;
                }
                UIManager.Inst.OpenPanel<ProfilePanel.ProfilePanel>(PanelId.ProfilePanel, uid);
            });
        }

        public void SetData(string targetUserName,SearchByIdResponse searchByIdResponse)
        {
            if (searchByIdResponse == null || searchByIdResponse.userInfo == null)
            {
                return;
            }

            uid = searchByIdResponse.userInfo.uid;
            HeadViewWidget.InitHeadCycle(searchByIdResponse.userInfo);

            string username = searchByIdResponse.userInfo.nickname;
            userName.text = username;
            
            serachtargetId.text = "ID: "+ searchByIdResponse.userInfo.username;
            
            AddFriendButton.SetRelation(searchByIdResponse.userInfo.uid, searchByIdResponse.relationShipInfo);
        }

        private void OnBackBtnClick()
        {
            CloseSelf();
        }

        private void CloseSelf()
        {
            UIManager.Inst.ClosePanel(this);
        }
    }


}