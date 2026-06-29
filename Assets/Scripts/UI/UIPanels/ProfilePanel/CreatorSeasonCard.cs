using System;
using ChocDino.UIFX;
using GameData;
using UI.BaseWidgets;
using UI.UIPanels.ProfilePanel;
using UnityEngine;
using UnityEngine.UI;

public class CreatorSeasonCard : BaseCard
{
    private const string CreatorSeasonAtlasPath = "Assets/Loadable/UI/UIPanel/CreatorSeasonPanel/CreatorSeasonPanel.spriteatlas";

    [SerializeField] private CButton HistoryScoreBtn;
    [SerializeField] private Text HistoryScore;
    [SerializeField] private Image HistoryScoreImg;
    [SerializeField] private GameObject HistorySourceAni;
    [SerializeField] private GameObject HistoryScoreDetail;
    [SerializeField] private Text HistoryDetailScore;
    [SerializeField] private Text HistoryDetailDesc;
    [SerializeField] private CButton NowScoreBtn;
    [SerializeField] private CButton BageBtn;
    [SerializeField] private Text NowScore;
    [SerializeField] private Image NowScoreImg;
    [SerializeField] private GameObject NowSourceAni;
    [SerializeField] private GameObject NowScoreDetail;
    [SerializeField] private Text NowDetailScore;
    [SerializeField] private Text NowDetailDesc;
    [SerializeField] private Image bg1;
    [SerializeField] private Image bg2;
    [SerializeField] private Image bg3;
    [SerializeField] private Image bg4;
    [SerializeField] private Image bg0;
    [SerializeField] private Transform titleObj;
    [SerializeField] private Text _noTitle;
    [SerializeField] private OutlineFilter _noTitleOutline;
    [SerializeField] private UserBadge _userBadge;

    private AccountUserInfo _accountUserInfo;
    private string _uid;

    public void SetUserInfo(AccountUserInfo userInfo)
    {
        _accountUserInfo = userInfo;
        SetUserTitleId(userInfo);
    }

    private void SetUserTitleId(AccountUserInfo userInfo)
    {
        for (int i = titleObj.childCount - 1; i >= 0; i--)
            DestroyImmediate(titleObj.GetChild(i).gameObject);

        if (userInfo.titleId > 0)
        {
            var config = UserUIWidgetManager.Inst.GetTitleData(userInfo.titleId);
            if (config != null && titleObj.childCount <= 0)
            {
                var o = Loader.Load<GameObject>(config.Prefab, gameObject);
                Instantiate(o, titleObj);
            }
            _noTitle.gameObject.SetActive(false);
        }
        else
        {
            _noTitle.gameObject.SetActive(false);
            _noTitle.gameObject.SetActive(true);
        }

        if (_userBadge != null)
        {
            var selfUid = AccountDataManager.Inst?.UserInfo?.uid;
            if (!string.IsNullOrEmpty(selfUid) && userInfo?.uid == selfUid
                && CreatorRequestCtrl.Inst != null)
            {
                // 自己的主页：用本周列表接口验证"使用中"徽章是否本周仍有效
                // UserInfo.creatorBadgeInfo 记录用户设置的使用中 category，但 settleTime 不可靠
                // 在本周列表中找到同 category 则说明未过期，找不到则隐藏
                var inUseCategory = AccountDataManager.Inst.UserInfo?.creatorBadgeInfo?.category ?? -1;
                CreatorRequestCtrl.Inst.RequestCreatorBadgeListInfo(0, res =>
                {
                    if (!this) return;
                    var cur = inUseCategory >= 0
                        ? res?.list?.Find(b => b.category == inUseCategory)
                        : null;
                    if (cur != null)
                        _userBadge.SetData(cur.category, cur.level);
                    else
                        _userBadge.Hide();
                }, _ => { if (this) _userBadge.Hide(); });
            }
            else
            {
                // 他人主页：用 publicProfile 下发的 creatorBadgeInfo；settleTime=0 表示已过期
                var badgeInfo = userInfo?.creatorBadgeInfo;
                if (badgeInfo != null)
                {
                    long serverTime = TcpTimeSystem.Inst.ServerTime;
                    bool isExpiredByWeek = badgeInfo.settleTime == 0
                                           || TimeTools.IsNewWeek(badgeInfo.settleTime, serverTime);
                    if (!isExpiredByWeek)
                        _userBadge.SetData(badgeInfo.category, badgeInfo.level);
                    else
                        _userBadge.Hide();
                }
            }
        }
    }

    public override void OnCreate(ProfilePanel profilePanel)
    {
        base.OnCreate(profilePanel);
        cardBgType = ProfileCardBgType.Bg5;
        HistoryScoreBtn.onClick.AddListener(OnClickHistoryScore);
        NowScoreBtn.onClick.AddListener(OnClickNowScore);
        BageBtn.onClick.AddListener(OnClickBadge);
    }

    public override void OnUpdateTheme(ProfileThemeInfo themeInfo)
    {
        base.OnUpdateTheme(themeInfo);
        if (themeInfo == null || themeInfo.colorInfo == null) return;
        if (!string.IsNullOrEmpty(themeInfo.colorInfo.profileCardColor) && TitleBg != null)
        {
            TitleBg.color = DataUtil.DeSerializeColorCheckHash(themeInfo.colorInfo.profileCardColor);
        }
        if (!string.IsNullOrEmpty(themeInfo.colorInfo.gridColor))
        {
            var gridColor = DataUtil.DeSerializeColorCheckHash(themeInfo.colorInfo.gridColor);
            if (bg1 != null) bg1.color = gridColor;
            if (bg2 != null) bg2.color = gridColor;
            if (bg3 != null) bg3.color = gridColor;
            if (bg4 != null) bg4.color = gridColor;
        }
        if (!string.IsNullOrEmpty(themeInfo.colorInfo.mainBgColor))
        {
            var mainBgColor = DataUtil.DeSerializeColorCheckHash(themeInfo.colorInfo.mainBgColor);
            if (bg0 != null) bg0.color = mainBgColor;
        }
        if (!string.IsNullOrEmpty(themeInfo.colorInfo.noTitleColor) && _noTitle != null)
        {
            _noTitle.color = DataUtil.DeSerializeColorCheckHash(themeInfo.colorInfo.noTitleColor);
        }
        if (!string.IsNullOrEmpty(themeInfo.colorInfo.noTitleBorderColor) && _noTitleOutline != null)
        {
            _noTitleOutline.Color = DataUtil.DeSerializeColorCheckHash(themeInfo.colorInfo.noTitleBorderColor);
        }
        if (_noTitle != null && _noTitle.gameObject.activeSelf)
        {
            _noTitle.gameObject.SetActive(false);
            _noTitle.gameObject.SetActive(true);
        }
    }

    public override void OnShow(string uid)
    {
        base.OnShow(uid);
        _uid = uid;
        Show(true);
        ClearScoreView();
        ClearTitleView();
        AccountDataManager.Inst.RemoveUserInfoChangeListener(OnUserInfoChange);
        if (AccountDataManager.Inst.IsMySelf(uid))
            AccountDataManager.Inst.AddUserInfoChangeListener(OnUserInfoChange);
    }

    private void OnUserInfoChange(AccountUserInfo userInfo)
    {
        _accountUserInfo = userInfo;
        SetUserTitleId(userInfo);
    }

    private void OnDestroy()
    {
        AccountDataManager.Inst?.RemoveUserInfoChangeListener(OnUserInfoChange);
    }

    int currentHistoryIndex = 0;
    int currentCurrentIndex = 0;

    public void SetCreatorScoreInfo(CreatorScoreInfo creatorScoreInfo)
    {
        var history = creatorScoreInfo?.history;
        var current = creatorScoreInfo?.current;

        if (creatorScoreInfo == null)
        {
            ClearScoreView();
            return;
        }
        currentHistoryIndex = history.category;
        currentCurrentIndex = current.category;
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

    private void ClearScoreView()
    {
        if (HistoryScore != null) HistoryScore.text = "";
        if (NowScore != null) NowScore.text = "";
        if (HistoryDetailScore != null) HistoryDetailScore.text = "";
        if (NowDetailScore != null) NowDetailScore.text = "";
        if (HistoryDetailDesc != null) HistoryDetailDesc.text = string.Empty;
        if (NowDetailDesc != null) NowDetailDesc.text = string.Empty;
        //if (HistoryScoreImg != null) HistoryScoreImg.enabled = false;
        //if (NowScoreImg != null) NowScoreImg.enabled = false;
        if (HistorySourceAni != null) HistorySourceAni.SetActive(false);
        if (NowSourceAni != null) NowSourceAni.SetActive(false);
    }

    private void ClearTitleView()
    {
        if (titleObj != null)
        {
            for (int i = titleObj.childCount - 1; i >= 0; i--)
                DestroyImmediate(titleObj.GetChild(i).gameObject);
        }

        if (_noTitle != null)
        {
            _noTitle.gameObject.SetActive(false);
            _noTitle.gameObject.SetActive(true);
        }
    }

    private void OnClickHistoryScore()
    {
        //HistoryScoreDetail.SetActive(!HistoryScoreDetail.activeSelf);
        //NowScoreDetail.SetActive(false);
        //return;//TODO 等接口
        CreatorRequestCtrl.Inst.RequestCreatorUserScoreInfo(_uid, (res) =>
        {
            UIManager.Inst.OpenPanel(PanelId.ScoreDetailedRulesPanel, res.list,currentHistoryIndex, true);

        }, (error) =>
        {
            Debug.LogError("RequestScoreRankInfo error: " + error);
        },"s14-0");
    }

    private void OnClickNowScore()
    {
        //HistoryScoreDetail.SetActive(false);
        //NowScoreDetail.SetActive(!NowScoreDetail.activeSelf);
        CreatorRequestCtrl.Inst.RequestCreatorUserScoreInfo(_uid, (res) =>
        {
            UIManager.Inst.OpenPanel(PanelId.ScoreDetailedRulesPanel, res.list,currentCurrentIndex, false);

        }, (error) =>
        {
            Debug.LogError("RequestScoreRankInfo error: " + error);
        });
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

    private static string GetCreatorScoreFrameSpriteName(int scoreFrame) => scoreFrame switch
    {
        1 => "sourceKuang2",
        2 => "sourceKuang1",
        3 => "sourceKuang0",
        4 => "sourceKuang3",
        _ => "defaultSourceKuang",
    };

    private static string GetCreatorScoreDetailDesc(int category) => category switch
    {
        0 => "总榜积分",
        1 => "2D皮肤积分",
        2 => "3D皮肤积分",
        3 => "动作积分",
        4 => "地图积分",
        5 => "工具积分",
        _ => "",
    };

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
            img.rectTransform.sizeDelta = new Vector2(160, 160);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[CreatorSeasonCard] LoadSpriteInAltas failed. atlas={atlasPath}, sprite={spriteName}, err={e.Message}");
            if (hideWhenMissing)
            {
                img.enabled = false;
            }
        }
    }

    private void OnClickBadge()
    {
        if (_accountUserInfo == null || AccountDataManager.Inst == null || !AccountDataManager.Inst.IsMySelf(_accountUserInfo.uid))
            {
                return;
            }

            UIManager.Inst.OpenPanel(PanelId.BadgeScorePanel, _accountUserInfo.uid);
    }

// #if UNITY_EDITOR
//     // 右键 CreatorSeasonCard 组件 → 选对应菜单即可触发
//     [ContextMenu("TEST - 有徽章（本周）")]
//     private void Test_BadgeValid()
//     {
//         var badge = new CreatorBadgeInfoData
//         {
//             category = 0,
//             level = 2,
//             feature = Network.Http.HeaderDefine.CUR_FEATURE,
//             settleTime = TcpTimeSystem.Inst.ServerTime, // 当前服务器时间 = 本周
//         };
//         ApplyBadge(badge);
//         Debug.Log("[TEST] 有徽章（本周）category=0 level=2");
//     }

//     [ContextMenu("TEST - 没有徽章")]
//     private void Test_NoBadge()
//     {
//         if (_userBadge != null) _userBadge.Hide();
//         Debug.Log("[TEST] 没有徽章");
//     }

//     [ContextMenu("TEST - 徽章已过期（上周）")]
//     private void Test_BadgeExpired()
//     {
//         var badge = new CreatorBadgeInfoData
//         {
//             category = 0,
//             level = 2,
//             feature = Network.Http.HeaderDefine.CUR_FEATURE,
//             settleTime = TcpTimeSystem.Inst.ServerTime - 7 * 24 * 3600, // 往前推7天 = 上周
//         };
//         ApplyBadge(badge);
//         Debug.Log("[TEST] 徽章已过期（上周结算）");
//     }

//     private void ApplyBadge(CreatorBadgeInfoData badgeInfo)
//     {
//         if (_userBadge == null) return;
//         bool isCurFeature = string.IsNullOrEmpty(badgeInfo.feature)
//                             || badgeInfo.feature == Network.Http.HeaderDefine.CUR_FEATURE;
//         bool isExpiredByWeek = badgeInfo.settleTime > 0
//                                && TimeTools.IsNewWeek(badgeInfo.settleTime, TcpTimeSystem.Inst.ServerTime);
//         if (isCurFeature && !isExpiredByWeek)
//             _userBadge.SetData(badgeInfo.category, badgeInfo.level);
//         else
//             _userBadge.Hide();
//     }
// #endif
}
