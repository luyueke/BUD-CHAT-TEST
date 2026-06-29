using GameData;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.ProfilePanel
{
    public class AvatarCard : BaseCard
    {
        [SerializeField] private RawImage previewRawImg;
        [SerializeField] private CButton shareBtn;
        [SerializeField] private CButton giftBtn;
        [SerializeField] private CButton userInfoBtn;
        [SerializeField] private CButton editUserInfoBtn;
        [SerializeField] private Text whiteListContent;
        [SerializeField] private CButton whiteListClose;
        [SerializeField] private CButton whiteListShow;
        [SerializeField] private GameObject whiteListInfo;
        [SerializeField] private CButton sendCoinBtn;

        //服务器房间信息
        [SerializeField] private GameObject serverInfoView;
        [SerializeField] private CButton joinBtn;
        [SerializeField] private Text serverLocationText;

        //3D形象手势
        [SerializeField] private UIDragUtil dragUtil;

        // private AccountUserInfo _accountUserInfo;
        private string toUid;


        public override void OnCreate(ProfilePanel profilePanel)
        {
            base.OnCreate(profilePanel);
            shareBtn.onClick.AddListener(OnShareBtnClick);
            giftBtn.onClick.AddListener(OnGiftBtnClick);
            userInfoBtn.onClick.AddListener(OnUserInfoBtnClick);
            editUserInfoBtn.onClick.AddListener(OnEditUserInfoBtnClick);
            joinBtn.onClick.AddListener(OnJoinBtnClick);
            whiteListClose.onClick.AddListener(() =>
            {
                whiteListInfo.gameObject.SetActive(false);
                whiteListShow.gameObject.SetActive(true);

            });

            whiteListShow.onClick.AddListener(() =>
            {
                whiteListInfo.gameObject.SetActive(true);
                whiteListShow.gameObject.SetActive(false);
            });
            
            sendCoinBtn.onClick.AddListener(() =>
            {
                var panel = UIManager.Inst.OpenPanel<SendCoinPanel>(PanelId.SendCoinPanel, toUid);
            });

            //sendCoinBtn.onClick.AddListener(() =>
            //{
            //    var panel = UIManager.Inst.OpenPanel<SendCoinPanel>(PanelId.SendCoinPanel, toUid);
            //});

            serverInfoView.SetActive(false);
        }

        public override void OnShow(string uid)
        {
            base.OnShow(uid);
            this.toUid = uid;
            bool isMyself = AccountDataManager.Inst.IsMySelf(uid);
            editUserInfoBtn.gameObject.SetActive(isMyself);
            giftBtn.gameObject.SetActive(false);
            userInfoBtn.gameObject.SetActive(false);
            sendCoinBtn.gameObject.SetActive(!isMyself);
        }

        public void SetWhiteListData(DebugCfg debugCfg)
        {
            whiteListShow.gameObject.SetActive(debugCfg != null);
            if (debugCfg == null)
            {
                return;
            }

            string whiteListContent = "上次活跃时间：<color=\"#FFBF04\">" + debugCfg.lastActiveTime +
                                      "</color>\n上次付费时间：<color=\"#FFBF04\">" + debugCfg.lastPaidTime +
                                      "</color>\n近7天累充金额：<color=\"#FFBF04\">" + debugCfg.sevenDaysTotalPaid +
                                      "</color>\n累充金额：<color=\"#FFBF04\">" + debugCfg.totalPaid + "</color>\n";

#if PACKAGE_TYPE_US
            whiteListContent += "国家：<color=\"#FFBF04\">" + debugCfg.country + "</color>";
            #else
            whiteListContent += "渠道：<color=\"#FFBF04\">" + debugCfg.channelName + "</color>";
#endif


            this.whiteListContent.text = whiteListContent;
        }


        public void ShowRoleVisible(bool isVisible)
        {
            //TODO:设置人物形象显隐以及控制动画的播放
        }

        public void SetRoleTarget(Transform target)
        {
            dragUtil.RotateTarget = target;
        }


        private void OnShareBtnClick()
        {
            _profilePanel.SetBottomViewVisible(true);
        }


        private void OnGiftBtnClick()
        {
        }

        private void OnUserInfoBtnClick()
        {
        }

        private void OnEditUserInfoBtnClick()
        {
            _profilePanel.SetEditViewVisible(true);
        }

        private void OnJoinBtnClick()
        {
        }
    }
}
