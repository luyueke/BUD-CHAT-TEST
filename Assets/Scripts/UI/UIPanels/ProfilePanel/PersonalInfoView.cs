using Com.TheFallenGames.OSA.Util.IO;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.ProfilePanel
{
    /// <summary>
    /// 个人信息卡片
    /// </summary>
    public class PersonalInfoView : MonoBehaviour
    {
        private RemoteImageBehaviour _avatar;
        private Button _avatarBtn;
        private BUD_Text _userNameText;
        private Text _userIdText;
        private Image _officialVerity;

        private string _userId;
        private bool _isProfilePage = false;

        private bool isInit = false;


        public void InitUI(AccountUserInfo accountUserInfo, bool isProfilePage)
        {
            isInit = true;
            _avatar = GameObjectEx.FindChildByName(transform, "Avatar").GetComponent<RemoteImageBehaviour>();
            _avatarBtn = GameObjectEx.FindChildByName(transform, "AvatarArea").GetComponent<Button>();
            _userNameText = GameObjectEx.FindChildByName(transform, "UserName").GetComponent<BUD_Text>();
            _userIdText = GameObjectEx.FindChildByName(transform, "UserId").GetComponent<Text>();
            _officialVerity = GameObjectEx.FindChildByName(transform, "OfficialVerity").GetComponent<Image>();

            this._isProfilePage = isProfilePage;

            RefreshData(accountUserInfo);
        }

        public void RefreshData(AccountUserInfo accountUserInfo)
        {
            if (!isInit)
            {
                return;
            }

            if (!string.IsNullOrEmpty(accountUserInfo.portraitUrl))
            {
                _avatar.Load(accountUserInfo.portraitUrl);
            }

            if (!string.IsNullOrEmpty(accountUserInfo.nickname))
            {
                _userNameText.text = accountUserInfo.nickname;
            }

            if (!string.IsNullOrEmpty(accountUserInfo.username))
            {
                _userIdText.text = "ID:" + accountUserInfo.username;
            }

            this._userId = accountUserInfo.uid;

            _officialVerity.gameObject.SetActive(false);

            _avatarBtn.onClick.AddListener(OnAvatarBtnClick);
        }

        private void OnAvatarBtnClick()
        {
            if (_isProfilePage)
            {
                return;
            }
        }
    }
}