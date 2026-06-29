using ChocDino.UIFX;
using System;
using GameData;
using UI.BaseWidgets;
using UI.UIWidgets;
using UnityEngine;
using UnityEngine.UI;
using static AccountUserInfo;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace UI.UIPanels.ProfilePanel
{
    /// <summary>
    /// 个人信息页右边卡片
    /// </summary>
    public class ProfileCard : BaseCard
    {
        [SerializeField] private UserInfoView _userInfoView;
        [SerializeField] private Text _likeNum;
        [SerializeField] private Text _followNum;
        [SerializeField] private SuperTextMesh _bioText;
        [SerializeField] private FollowButton _followButton;
        [SerializeField] private AddFriendButton _addFriendButton;
        //[SerializeField] private Image titleImage;
        [SerializeField] private Image titleInfoBg;
        //[SerializeField] private Button titleBtn;
        string titleAlxs = "Assets/Loadable/UI/UIPanel/TitlePreviewPanel/TitleAlts.spriteatlas";
        private const string CreatorSeasonAtlasPath = "Assets/Loadable/UI/UIPanel/CreatorSeasonPanel/CreatorSeasonPanel.spriteatlas";
        private AccountUserInfo _accountUserInfo;
        [SerializeField] private Text _noTitle;
        [SerializeField] private OutlineFilter _noTitleOutline;
        [SerializeField] private Transform titleObj;
        [SerializeField] private Image BadgeImg;
        [SerializeField] private UserBadge UserBadge;
        [SerializeField] private CButton BadgeBtn;
        [SerializeField] private CButton HistoryScoreBtn;
        [SerializeField] private Text HistoryScore;
        [SerializeField] private Image HistoryScoreImg;
        [SerializeField] private GameObject HistorySourceAni;
        [SerializeField] private GameObject HistoryScoreDetail;
        [SerializeField] private Text HistoryDetailScore;
        [SerializeField] private Text HistoryDetailDesc;
        [SerializeField] private CButton NowScoreBtn;
        [SerializeField] private Text NowScore;
        [SerializeField] private Image NowScoreImg;
        [SerializeField] private GameObject NowSourceAni;
        [SerializeField] private GameObject NowScoreDetail;
        [SerializeField] private Text NowDetailScore;
        [SerializeField] private Text NowDetailDesc;

        private Text _followText;
        private Text _addfrientText;

        private static readonly string[] CreatorBadgeCategorySpriteNameMap =
        {
            "rank",  // 0 全服
            "cloth", // 1 2D皮肤
            "skin",  // 2 3D皮肤
            "action",// 3 动作
            "map",   // 4 地图
            "tool"   // 5 工具
        };

        private static string BuildCreatorBadgeSpriteName(CreatorBadgeInfoData badgeInfo)
        {
            if (badgeInfo == null) return null;
            if (badgeInfo.category < 0 || badgeInfo.category >= CreatorBadgeCategorySpriteNameMap.Length) return null;

            var baseName = CreatorBadgeCategorySpriteNameMap[badgeInfo.category];
            if (string.IsNullOrEmpty(baseName)) return null;

            // 0 初级 -> 3；1 高级 -> 2；2 前100 -> 1；3 前10 -> 1；4 前1 -> 0
            var suffix = badgeInfo.level switch
            {
                0 => "3",
                1 => "2",
                2 => "1",
                3 => "1",
                4 => "0",
                _ => null
            };

            return string.IsNullOrEmpty(suffix) ? null : baseName + suffix;
        }

        private List<Text> txtFilter = new List<Text>();

        public override void OnCreate(ProfilePanel profilePanel)
        {
            base.OnCreate(profilePanel);
            InitListener();
            cardBgType = ProfileCardBgType.Bg1;
            if (titleInfoBg != null) titleInfoBg.gameObject.SetActive(false);
            _followText = _followButton.transform.Find("LoadingButton/ButtonText").GetComponent<Text>();
            _addfrientText = _addFriendButton.transform.Find("LoadingButton/ButtonText").GetComponent<Text>();
            txtFilter.Add(_followText);
            txtFilter.Add(_addfrientText);
            txtFilter.Add(HistoryDetailScore);
            txtFilter.Add(NowDetailScore);
        }

        public override void OnShow(string uid)
        {
            base.OnShow(uid);
            _bioText.SetLocalText("这个简介是空的,玩家还没有写任何东西。");
            if (AccountDataManager.Inst.IsMySelf(uid))
            {
                SetUserInfo(AccountDataManager.Inst.UserInfo, null, null);
                AccountDataManager.Inst.AddUserInfoChangeListener(OnUserInfoChange);
            }

            _userInfoView.SetCopyIdBtnVisible(true);
        }

        public override void OnUpdateTheme(ProfileThemeInfo themeInfo)
        {
          //  Debug.LogError($"OnUpdateTheme 000 id=" + themeInfo.themeId );
            base.OnUpdateTheme(themeInfo);
            if (themeInfo == null || themeInfo.colorInfo == null) return;
            
            ProfileThemeManager.Inst.ChangeTextColor(transform, themeInfo.colorInfo.textColor, themeInfo.colorInfo.textBorderColor, _userInfoView.NickText, txtFilter);
            //HistoryDetailScore.color = Color.black;
            //NowDetailScore.color = Color.black;
            if (!string.IsNullOrEmpty(themeInfo.colorInfo.profileCardColor) && TitleBg != null)
            {
                TitleBg.color = DataUtil.DeSerializeColorCheckHash(themeInfo.colorInfo.profileCardColor);
            }
            if (!string.IsNullOrEmpty(themeInfo.colorInfo.titleInfoColor) && titleInfoBg != null)
            {
                titleInfoBg.color = DataUtil.DeSerializeColorCheckHash(themeInfo.colorInfo.titleInfoColor);
             //   titleInfoBg.transform.localPosition = new Vector3(0, -17, 0);
            }
            if (!string.IsNullOrEmpty(themeInfo.colorInfo.noTitleColor) && _noTitle != null)
            {
                _noTitle.color = DataUtil.DeSerializeColorCheckHash(themeInfo.colorInfo.noTitleColor);
            }
            if (!string.IsNullOrEmpty(themeInfo.colorInfo.noTitleBorderColor) && _noTitleOutline != null)
            {
                _noTitleOutline.Color = DataUtil.DeSerializeColorCheckHash(themeInfo.colorInfo.noTitleBorderColor);
            }

            if(_noTitle.gameObject.activeSelf)
            {
                _noTitle.gameObject.SetActive(false);
                _noTitle.gameObject.SetActive(true);

            }

            _userInfoView.NameText.gameObject.SetActive(false);
            _userInfoView.NameText.gameObject.SetActive(true);



        }


        private void OnDestroy()
        {
            AccountDataManager.Inst.RemoveUserInfoChangeListener(OnUserInfoChange);
        }

        private void InitListener()
        {
            BadgeBtn.onClick.AddListener(OnClickBadge);
            HistoryScoreBtn.onClick.AddListener(OnClickHistoryScore);
            NowScoreBtn.onClick.AddListener(OnClickNowScore);
        }

        private void SetUserTitleId(AccountUserInfo userInfo)
        {
            for (int i = titleObj.childCount - 1; i >= 0; i--)
            {
                GameObject.DestroyImmediate(titleObj.GetChild(i).gameObject);
            }

            if (userInfo.titleId > 0)
            {
                var config = UserUIWidgetManager.Inst.GetTitleData(userInfo.titleId);

                if (config != null && titleObj.transform.childCount <= 0)
                {
                    var o = Loader.Load<GameObject>(config.Prefab, gameObject);
                    var titleGo = GameObject.Instantiate(o, titleObj.transform);
                }
                //_noTitle.text = "";
                _noTitle.gameObject.SetActive(false);
            }
            else
            {
                //_noTitle.text = "未使用称号";
                _noTitle.gameObject.SetActive(false);
                _noTitle.gameObject.SetActive(true);
            }
            // OnUserInfoChange 会被余额/关系/签名等不相关字段更新触发，那些 push 数据里 creatorBadgeInfo 可能是 null。
            // 直接走 Hide() 会把已显示的 Top10 误关。这里把"没带徽章信息"的更新当作"不动徽章状态"处理 —— 只有真的带 badgeInfo
            // 字段时才刷新徽章显示。打开其他玩家面板走 SetUserInfo 时这条仍生效（其他玩家的徽章数据本来就该跟着 userInfo 一起进来）。
            if (userInfo == null) return;
            var badgeInfo = userInfo.creatorBadgeInfo;
            if (badgeInfo == null) return;
            bool isCurFeature = string.IsNullOrEmpty(badgeInfo.feature)
                                || badgeInfo.feature == Network.Http.HeaderDefine.CUR_FEATURE;
            bool isExpiredByWeek = badgeInfo.settleTime > 0
                                   && TimeTools.IsNewWeek(badgeInfo.settleTime, TcpTimeSystem.Inst.ServerTime);
            // feature 为空时按旧逻辑兼容显示（publicProfile 可能不回填 feature）；
            // 一旦 feature 非空，则必须等于当前赛季 CUR_FEATURE 才展示，避免只有历史数据的用户错误地显示徽章。
            // settleTime > 0 时额外判断是否跨周：上周结算的徽章本周视为未获得。
            if (isCurFeature && !isExpiredByWeek)
            {
                UserBadge.SetData(badgeInfo.category, badgeInfo.level);
                // 有当前赛季徽章时不显示 defaultBadge 占位
                if (BadgeImg != null)
                {
                    BadgeImg.enabled = false;
                }
            }
            else
            {
                UserBadge.Hide();
                // 没有徽章时显示 defaultBadge 占位图（旧逻辑回归）
                if (BadgeImg != null)
                {
                    var sp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(CreatorSeasonAtlasPath, "defaultBadge", gameObject);
                    BadgeImg.sprite = sp;
                    BadgeImg.enabled = sp != null;
                }
            }
        }
        private void OnUserInfoChange(AccountUserInfo accountUserInfo)
        {
            _accountUserInfo = accountUserInfo;
            _userInfoView.SetData(accountUserInfo);
            _bioText.text = _accountUserInfo.bio;
            SetUserTitleId(accountUserInfo);
        }

        public void SetUserInfo(AccountUserInfo userInfo, AccountAmount accountAmount, RelationShipData relationShipData)
        {
            _accountUserInfo = userInfo;
            _userInfoView.SetData(_accountUserInfo);
            // _userInfoView.SetUserNickLengthLimit(30);

            if (accountAmount != null)
            {
                _likeNum.text = ProfileMapUtils.FormatNumber(accountAmount.likeAmount);
                _followNum.text = ProfileMapUtils.FormatNumber(accountAmount.fansAmount);
            }

            _bioText.text = _accountUserInfo.bio;

            var myUid = AccountDataManager.Inst.Uid;
            var currentUid = userInfo.uid;
            if (myUid.Equals(currentUid))
            {
                //当前是自己的个人主页
                _followButton.gameObject.SetActive(false);
                _addFriendButton.gameObject.SetActive(false);
            }
            else
            {
                //当前是别人个人主页 这次版本不处理加好友
                _followButton.gameObject.SetActive(true);
                _addFriendButton.gameObject.SetActive(true);
                RelationShipInfo relationShipInfo = new RelationShipInfo()
                {
                    relationShip = (int)RelationShipType.Follow,
                    relationStatus = relationShipData.followStatus
                };
                _followButton.SetRelation(currentUid, relationShipInfo);

                RelationShipInfo relation = new RelationShipInfo()
                {
                    relationShip = (int)RelationShipType.Friend,
                    relationStatus = relationShipData.friendStatus
                };
                _addFriendButton.SetRelation(currentUid, relation);
            }

            SetUserTitleId(userInfo);
            //设置称号
            //if (userInfo.userTitle != null)
            //{
            //    LoadTitle(userInfo.userTitle);
            //    titleImage.transform.parent.gameObject.SetActive(true);
            //}
            //titleBtn.onClick.AddListener(OnTitleClicked);
            //userInfo.titleId = 1;


        }

        private void OnClickBadge()
        {
            if (_accountUserInfo == null || AccountDataManager.Inst == null || !AccountDataManager.Inst.IsMySelf(_accountUserInfo.uid))
            {
                return;
            }

            UIManager.Inst.OpenPanel(PanelId.BadgeScorePanel, _accountUserInfo.uid);
        }

        private void OnClickHistoryScore()
        {
            HistoryScoreDetail.SetActive(!HistoryScoreDetail.activeSelf);
            NowScoreDetail.SetActive(false);
        }
        private void OnClickNowScore()
        {
            HistoryScoreDetail.SetActive(false);
            NowScoreDetail.SetActive(!NowScoreDetail.activeSelf);
        }

        private static void TrySetSpriteByName(Image img, string atlasPath, string spriteName, GameObject owner, bool hideWhenMissing = false)
        {
            if (img == null) return;

            try
            {
                var sp = (!string.IsNullOrEmpty(atlasPath) && !string.IsNullOrEmpty(spriteName) && XAssetLoaderMgr.Inst != null)
                    ? XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlasPath, spriteName, owner)
                    : null;

                img.sprite = sp;
                if (hideWhenMissing)
                {
                    img.enabled = sp != null;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ProfileCard] LoadSpriteInAltas failed. atlas={atlasPath}, sprite={spriteName}, err={e.Message}");
                if (hideWhenMissing)
                {
                    img.enabled = false;
                }
            }
        }

        public void SetCreatorScoreInfo(CreatorScoreInfo creatorScoreInfo)
        {
            var history = creatorScoreInfo?.history;
            var current = creatorScoreInfo?.current;
            if (creatorScoreInfo == null)
            {
                if (HistoryScore != null) HistoryScore.text = "";
                if (NowScore != null) NowScore.text = "";
                if (HistoryDetailScore != null) HistoryDetailScore.text = "";
                if (NowDetailScore != null) NowDetailScore.text = "";
                if (HistoryDetailDesc != null) HistoryDetailDesc.text = string.Empty;
                if (NowDetailDesc != null) NowDetailDesc.text = string.Empty;
                HistoryScoreImg.enabled = false;
                NowScoreImg.enabled = false;
                return;
            }

            var historyScore = history != null ? history.score : 0;
            var currentScore = current != null ? current.score : 0;

            if (HistoryDetailScore != null) HistoryDetailScore.text = historyScore.ToString();
            if (HistoryDetailDesc != null) HistoryDetailDesc.text = history != null ? GetCreatorScoreDetailDesc(history.category) : string.Empty;

            if (NowDetailScore != null) NowDetailScore.text = currentScore.ToString();
            if (NowDetailDesc != null) NowDetailDesc.text = current != null ? GetCreatorScoreDetailDesc(current.category) : string.Empty;

            ApplyCreatorScoreFrame(HistoryScoreImg, HistoryScore, history?.scoreFrame ?? 0, historyScore);
            ApplyCreatorScoreFrame(NowScoreImg, NowScore, current?.scoreFrame ?? 0, currentScore);
            HistorySourceAni.SetActive(history?.scoreFrame == 3);
            NowSourceAni.SetActive(current?.scoreFrame == 3);
        }

        private void ApplyCreatorScoreFrame(Image img, Text scoreText, int scoreFrame, int scoreValue)
        {
            var frame = GetCreatorScoreFrameSpriteName(scoreFrame);

            TrySetSpriteByName(img, CreatorSeasonAtlasPath, frame, gameObject);
            if (scoreText != null)
            {
                scoreText.text = frame == "defaultSourceKuang" ? string.Empty : scoreValue.ToString();
            }
        }

        private string GetCreatorScoreFrameSpriteName(int scoreFrame)
        {
            var spriteName = "";
            switch(scoreFrame)
            {
                case 0:
                    spriteName = "defaultSourceKuang";
                    break;
                case 1:
                    spriteName = "sourceKuang2";
                    break;
                case 2:
                    spriteName = "sourceKuang1";
                    break;
                case 3:
                    spriteName = "sourceKuang0";
                    break;
                case 4:
                    spriteName = "sourceKuang3";
                    break;
                default:
                    spriteName = "defaultSourceKuang";
                    break;
            }
            return spriteName;
        }

        private string GetCreatorScoreDetailDesc(int category)
        {
            switch (category)
            {
                case 0:
                    return "总榜积分";
                case 1:
                    return "2D皮肤积分";
                case 2:
                    return "3D皮肤积分";
                case 3:
                    return "动作积分";
                case 4:
                    return "地图积分";
                case 5:
                    return "工具积分";
                default:
                    return "";
            }
        }
    }





}
