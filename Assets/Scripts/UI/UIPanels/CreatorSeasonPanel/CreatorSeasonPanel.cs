using System.Collections;
using System.Collections.Generic;
using BUD.AnimPose;
using Game.Avatar;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class CreatorSeasonPanel : BasePanel<CreatorSeasonPanel>
{
    [SerializeField] private Transform characterRoot;
    [SerializeField] internal AvatarCameraController avatarCameraController;
    [SerializeField] private CButton CloseBtn;
    [SerializeField] private Text ScoreText;
    [SerializeField] private CButton TipsBtn;
    [SerializeField] private Text SelectTypeText;
    [SerializeField] private Image SelectImg;
    [SerializeField] private CButton ChangeBtn;
    [SerializeField] private CButton JoinBtn;
    [SerializeField] private CButton CreateTaskBtn;
    [SerializeField] private GameObject TaskRedPoint;
    [SerializeField] private CButton GashaponWelfareBtn;
    [SerializeField] private CButton CreateCompetitionBtn;
    [SerializeField] private CButton CreatorRoadBtn;
    [SerializeField] private GameObject RoadRedPoint;
    [SerializeField] private CButton CreateRuleBtn;
    [SerializeField] private CButton CreateRewardBtn;
    [SerializeField] private CButton CreateRankBtn;
    [SerializeField] private CButton HistoricalRankingListBtn;
    [SerializeField] private CButton CreativeProcessBtn;
    [SerializeField] private CButton ScoreInfoBtn;
    //[SerializeField] private GameObject ScoreDetailedRules;
    [SerializeField] private GameObject HistoricalRanking;
    [SerializeField] private GameObject CreativeProcess;


    private const string CreatorSelectTypeKey = "CreatorSeasonPanel_CreatorSelectType";
    private const string TitleRewardShownKey = "CreatorTitleRewardPanel_LastShown_";
    private CreatorSelectType _currentSelectType = CreatorSelectType.All;

    private List<CreatorUserScoreInfoData> _creatorUserScoreInfoList = new List<CreatorUserScoreInfoData>();

    private BaseAvatarWrapper _avatarWrapper;

    public override void OnCreate()
    {
        base.OnCreate();
        CloseBtn.onClick.AddListener(CloseSelf);
        TipsBtn.onClick.AddListener(OnTipsBtnClick);
        ChangeBtn.onClick.AddListener(OnChangeBtnClick);
        JoinBtn.onClick.AddListener(OnJoinBtnClick);
        CreateTaskBtn.onClick.AddListener(OnCreateTaskBtnClick);
        GashaponWelfareBtn.onClick.AddListener(OnGashaponWelfareBtnClick);
        CreateCompetitionBtn.onClick.AddListener(OnCreateCompetitionBtnClick);
        CreatorRoadBtn.onClick.AddListener(OnCreatorRoadBtnClick);
        CreateRuleBtn.onClick.AddListener(OnCreateRuleBtnClick);
        CreateRewardBtn.onClick.AddListener(OnCreateRewardBtnClick);
        CreateRankBtn.onClick.AddListener(OnCreateRankBtnClick);
        HistoricalRankingListBtn.onClick.AddListener(OnHistoricalRankingListBtnClick);
        CreativeProcessBtn.onClick.AddListener(OnCreativeProcessBtnClick);
        ScoreInfoBtn.onClick.AddListener(OnScoreInfoBtnClick);
        //ScoreDetailedRules.SetActive(false);
        HistoricalRanking.SetActive(false);
        CreativeProcess.SetActive(false);
        MessageHelper.AddListener<int>(MessageName.OnCreatorSelectTypeChanged, OnCreatorSelectTypeChanged);
        MessageHelper.AddListener<CreatorCenterData>(MessageName.OnCreatorCenterDataRefreshed, OnCreatorCenterDataRefreshed);
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        EnsureCharacterModel();
        LoadLocalAndApply();
        RefreshCreatorCenterRedPointsFromServer();
        RequestAndMaybeShowBadgePopup();
    }
    
    protected override void OnDestroy()
    {
        DestroyCharacterModel();
        base.OnDestroy();
        MessageHelper.RemoveListener<int>(MessageName.OnCreatorSelectTypeChanged, OnCreatorSelectTypeChanged);
        MessageHelper.RemoveListener<CreatorCenterData>(MessageName.OnCreatorCenterDataRefreshed, OnCreatorCenterDataRefreshed);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        EnsureCharacterModel();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        if (_avatarWrapper != null && _avatarWrapper.Avatar != null)
        {
            _avatarWrapper.Avatar.gameObject.SetActive(false);
        }
    }

    private void EnsureCharacterModel()
    {
        if (characterRoot == null)
        {
            return;
        }

        if (_avatarWrapper != null && _avatarWrapper.Avatar != null)
        {
            _avatarWrapper.Avatar.gameObject.SetActive(true);
            ApplyAvatarCameraForPreview();
            return;
        }

        var saveCharacterData = AccountDataManager.Inst?.UserInfo?.avatarInfo;
        if (saveCharacterData == null || AvatarController.Inst == null)
        {
            return;
        }

        _avatarWrapper = AvatarController.Inst.CreateUIAvatarWithIKController(saveCharacterData, characterRoot);
        ApplyAvatarCameraForPreview();
    }

    /// <summary>
    /// 预制体里 MoveRoot 与 characterRoot 为同一节点时，竖向拖拽会改 localPosition.y；
    /// 本面板只需水平旋转预览，关闭竖移以免模型“漂位”。
    /// </summary>
    private void ApplyAvatarCameraForPreview()
    {
        if (avatarCameraController == null || characterRoot == null)
        {
            return;
        }

        avatarCameraController.RotateTarget = characterRoot;
        avatarCameraController.isMoveEnabled = false;
        if (avatarCameraController.roleCamera != null)
        {
            //avatarCameraController.roleCamera.backgroundColor = new Color(1, 1, 1, 0);
            avatarCameraController.roleCamera.backgroundColor = new Color((float)145 / 255, (float)136 / 255, (float)231 /255, 0);
        }
    }

    private void DestroyCharacterModel()
    {
        if (_avatarWrapper != null && _avatarWrapper.Avatar != null)
        {
            Destroy(_avatarWrapper.Avatar.gameObject);
        }
        _avatarWrapper = null;
    }

    private void OnCreatorSelectTypeChanged(int type)
    {
        _currentSelectType = (CreatorSelectType)type;
        PlayerPrefs.SetInt(CreatorSelectTypeKey, type);
        PlayerPrefs.Save();
        ApplySelectTypeText();
        ApplySelectTypeIcon();
    }

    private void OnCreatorCenterDataRefreshed(CreatorCenterData data)
    {
        ApplyTaskRedPoint(CreatorCenterPanel.HasClaimableCreativeTask(data));
        ApplyRoadRedPoint(CreatorCenterPanel.HasCreatorRoadRedDot(data));
    }

    /// <summary>
    /// 拉取创作者中心数据，同步任务红点与创作者之路红点（与 CreatorCenterPanel 内 SetRedDot 逻辑一致）
    /// </summary>
    private void RefreshCreatorCenterRedPointsFromServer()
    {
        if (TaskRedPoint == null && RoadRedPoint == null)
        {
            return;
        }

        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.Center,
            HttpMethod.GET,
            null,
            content =>
            {
                var data = JsonConvert.DeserializeObject<CreatorCenterData>(content);
                ApplyTaskRedPoint(CreatorCenterPanel.HasClaimableCreativeTask(data));
                ApplyRoadRedPoint(CreatorCenterPanel.HasCreatorRoadRedDot(data));
            },
            _ => { });
    }

    private void ApplyTaskRedPoint(bool show)
    {
        if (TaskRedPoint != null)
        {
            TaskRedPoint.SetActive(show);
        }
    }

    private void ApplyRoadRedPoint(bool show)
    {
        if (RoadRedPoint != null)
        {
            RoadRedPoint.SetActive(show);
        }
    }

    private void LoadLocalAndApply()
    {
        var saved = PlayerPrefs.GetInt(CreatorSelectTypeKey, (int)CreatorSelectType.All);
        _currentSelectType = (CreatorSelectType)saved;
        ApplySelectTypeText();

// #if UNITY_EDITOR
//         // TODO: 后端接口就绪后删除此块，恢复下方网络请求
//         _creatorUserScoreInfoList = new List<CreatorUserScoreInfoData>
//         {
//             // All rank=1 → rank0（最高图标）
//             new CreatorUserScoreInfoData
//             {
//                 category = (int)CreatorSelectType.All, score = 9999, rank = 1,
//                 interactNum = 200, publishNum = 30,
//                 scoreDetails = new List<scoreDetails>
//                 {
//                     new scoreDetails { type = "0", score = "3000", maxScore = 5000 },
//                     new scoreDetails { type = "1", score = "2999", maxScore = 3000 },
//                     new scoreDetails { type = "2", score = "1500", maxScore = 2000 },
//                     new scoreDetails { type = "3", score = "2500", maxScore = 3000 },
//                 }
//             },
//             // Clothes rank=5 → cloth1（rank<=10）
//             new CreatorUserScoreInfoData
//             {
//                 category = (int)CreatorSelectType.Clothes, score = 5000, rank = 5,
//                 interactNum = 80, publishNum = 15,
//                 scoreDetails = new List<scoreDetails>
//                 {
//                     new scoreDetails { type = "0", score = "2000", maxScore = 5000 },
//                     new scoreDetails { type = "1", score = "1500", maxScore = 3000 },
//                     new scoreDetails { type = "2", score = "800", maxScore = 2000 },
//                     new scoreDetails { type = "3", score = "700", maxScore = 3000 },
//                 }
//             },
//             // Skin rank=20 score=4000 → skin2（rank>10, score>=3600）
//             new CreatorUserScoreInfoData
//             {
//                 category = (int)CreatorSelectType.Skin, score = 4000, rank = 20,
//                 interactNum = 40, publishNum = 8,
//                 scoreDetails = new List<scoreDetails>
//                 {
//                     new scoreDetails { type = "0", score = "1000", maxScore = 5000 },
//                     new scoreDetails { type = "1", score = "1200", maxScore = 3000 },
//                     new scoreDetails { type = "2", score = "800", maxScore = 2000 },
//                     new scoreDetails { type = "3", score = "1000", maxScore = 3000 },
//                 }
//             },
//             // Action rank=0 score=2000 → action3（无排名, score<3600）
//             new CreatorUserScoreInfoData
//             {
//                 category = (int)CreatorSelectType.Action, score = 2000, rank = 0,
//                 interactNum = 15, publishNum = 3,
//                 scoreDetails = new List<scoreDetails>
//                 {
//                     new scoreDetails { type = "0", score = "500", maxScore = 5000 },
//                     new scoreDetails { type = "1", score = "500", maxScore = 3000 },
//                     new scoreDetails { type = "2", score = "600", maxScore = 2000 },
//                     new scoreDetails { type = "3", score = "400", maxScore = 3000 },
//                 }
//             },
//             // Map rank=0 score=3600 → map2（无排名, score==3600）
//             new CreatorUserScoreInfoData
//             {
//                 category = (int)CreatorSelectType.Map, score = 3600, rank = 0,
//                 interactNum = 10, publishNum = 2,
//                 scoreDetails = new List<scoreDetails>
//                 {
//                     new scoreDetails { type = "0", score = "1200", maxScore = 5000 },
//                     new scoreDetails { type = "1", score = "900", maxScore = 3000 },
//                     new scoreDetails { type = "2", score = "700", maxScore = 2000 },
//                     new scoreDetails { type = "3", score = "800", maxScore = 3000 },
//                 }
//             },
//             // Tools rank=50 score=500 → tool3（rank>10, score<3600）
//             new CreatorUserScoreInfoData
//             {
//                 category = (int)CreatorSelectType.Tools, score = 500, rank = 50,
//                 interactNum = 5, publishNum = 1,
//                 scoreDetails = new List<scoreDetails>
//                 {
//                     new scoreDetails { type = "0", score = "100", maxScore = 5000 },
//                     new scoreDetails { type = "1", score = "200", maxScore = 3000 },
//                     new scoreDetails { type = "2", score = "150", maxScore = 2000 },
//                     new scoreDetails { type = "3", score = "50", maxScore = 3000 },
//                 }
//             },
//         };
//         ApplySelectTypeIcon();
// #else
        CreatorRequestCtrl.Inst.RequestCreatorUserScoreInfo(AccountDataManager.Inst.UserInfo.uid, (res) =>
        {
            _creatorUserScoreInfoList.Clear();
            foreach (var item in res.list)
            {
                _creatorUserScoreInfoList.Add(item);
            }
            ApplySelectTypeIcon();

        }, (error) =>
        {
            Debug.LogError("RequestScoreRankInfo error: " + error);
        });
//#endif
    }

    private const int CreatorScoreAdvancedThreshold = 3600;

    /// <summary>
    /// 图集子图名：前缀同品类（rank/cloth/…），后缀数字与 BadgeListItem 一致：
    /// 总榜：前1→0，前100 内(2～100)→1；名次在 100 外或无有效名次时按分数：小于 3600→3，否则→2。
    /// 其他品类：有排名时前1→0，前10(2～10)→1；否则分数：小于 3600→3，否则→2。
    /// </summary>
    private string GetSelectTypeText()
    {
        var row = _creatorUserScoreInfoList.Find(item => item.category == (int)_currentSelectType);
        var prefix = GetSelectTypeSpritePrefix(_currentSelectType);
        var suffix = GetSelectTypeSpriteSuffixDigit(_currentSelectType, row);
        return prefix + suffix;
    }

    private static string GetSelectTypeSpritePrefix(CreatorSelectType selectType)
    {
        return selectType switch
        {
            CreatorSelectType.All => "rank",
            CreatorSelectType.Clothes => "cloth",
            CreatorSelectType.Skin => "skin",
            CreatorSelectType.Action => "action",
            CreatorSelectType.Map => "map",
            CreatorSelectType.Tools => "tool",
            _ => "rank",
        };
    }

    private static int GetSelectTypeSpriteSuffixDigit(CreatorSelectType selectType, CreatorUserScoreInfoData row)
    {
        if (row == null)
        {
            return 3;
        }

        int score = row.score;
        int rank = row.rank;
        bool isOverall = selectType == CreatorSelectType.All;
        if (rank > 0)
        {
            if (rank == 1)
            {
                return 0;
            }

            if (isOverall)
            {
                if (rank <= 100)
                {
                    return 1;
                }
            }
            else
            {
                if (rank <= 10)
                {
                    return 1;
                }
            }
        }

        return score < CreatorScoreAdvancedThreshold ? 3 : 2;
    }

    private void ApplySelectTypeIcon()
    {
        if (SelectImg == null) return;

        var spriteName = GetSelectTypeText();
        if (string.IsNullOrEmpty(spriteName))
        {
            SelectImg.sprite = null;
            SelectImg.enabled = false;
            return;
        }

        // 指定从 CreatorSeasonPanel 图集加载
        const string atlasPath = "Assets/Loadable/UI/UIPanel/CreatorSeasonPanel/CreatorSeasonPanel.spriteatlas";
        var sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlasPath, spriteName, gameObject);

        SelectImg.sprite = sprite;
        SelectImg.enabled = sprite != null;
        ScoreText.text = _creatorUserScoreInfoList.Find(item => item.category == (int)_currentSelectType)?.score.ToString() ?? "0";
    }

    private void ApplySelectTypeText()
    {
        if (SelectTypeText == null) return;
        SelectTypeText.text = _currentSelectType switch
        {
            CreatorSelectType.All => "总榜",
            CreatorSelectType.Clothes => "2D皮肤",
            CreatorSelectType.Skin => "3D皮肤",
            CreatorSelectType.Action => "动作",
            CreatorSelectType.Map => "地图",
            CreatorSelectType.Tools => "工具",
            _ => "全部"
        };
    }

    private void OnTipsBtnClick()
    {
        UIManager.Inst.OpenPanel(PanelId.CreatorScoreRulePanel);
    }

    private void OnChangeBtnClick()
    {
        UIManager.Inst.OpenPanel(PanelId.CreatorTypeSelectPanel);
    }

    private void OnJoinBtnClick()
    {
        UIManager.Inst.OpenPanelTakeAni(PanelId.GameHallStudiosPanel);
    }

    private void OnCreateTaskBtnClick()
    {
        UIManager.Inst.OpenPanel(PanelId.CreatorCenterPanel, -1);//0传不过去，所以这里用-1代替
    }

    private void OnGashaponWelfareBtnClick()
    {
        UIManager.Inst.OpenPanel(PanelId.CreatorCenterPanel, 2);
    }

    private void OnCreateCompetitionBtnClick()
    {
        ContestEventManager.Inst.OpenContestPage();
    }

    private void OnCreatorRoadBtnClick()
    {
        UIManager.Inst.OpenPanel(PanelId.CreatorCenterPanel, 1);
    }

    private void OnCreateRuleBtnClick()
    {
        UIManager.Inst.OpenPanel(PanelId.CreatorRulePanel);
    }

    private void OnCreateRewardBtnClick()
    {
        UIManager.Inst.OpenPanel(PanelId.CreatorRewardInfoPanel);
    }

    private void OnCreateRankBtnClick()
    {
        UIManager.Inst.OpenPanel(PanelId.CreatorRankInfoPanel);
    }
    
    private void OnHistoricalRankingListBtnClick()
    {
        HistoricalRanking.SetActive(true);
    }

    private void OnCreativeProcessBtnClick()
    {
        CreativeProcess.SetActive(true);
    }

    private void OnScoreInfoBtnClick()
    {
         //ScoreDetailedRules.SetActive(true);
         //ScoreDetailedRules.GetComponent<ScoreDetailedRules>().InitPanel(_creatorUserScoreInfoList);
         UIManager.Inst.OpenPanel(PanelId.ScoreDetailedRulesPanel, _creatorUserScoreInfoList, (int)_currentSelectType, "分数细则");
    }
    private void RequestAndMaybeShowBadgePopup()
    {
        CreatorRequestCtrl.Inst.RequestCreatorBadgeListInfo(0, (res) =>
        {
            if (res == null || res.list == null || res.list.Count == 0)
                return;

            var uid = AccountDataManager.Inst.UserInfo.uid;
            var key = TitleRewardShownKey + uid;
            long.TryParse(PlayerPrefs.GetString(key, "0"), out long lastShown);
            long serverTime = TcpTimeSystem.Inst.ServerTime;

            if (!TimeTools.IsNewWeek(lastShown, serverTime))
                return;

            PlayerPrefs.SetString(key, serverTime.ToString());
            PlayerPrefs.Save();
            UIManager.Inst.OpenPanel(PanelId.CreatorTitleRewardPanel,res.list);
        }, _ => { });
    }
}
