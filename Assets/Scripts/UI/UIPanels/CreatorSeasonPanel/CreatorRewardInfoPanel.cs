using System.Collections;
using System.Collections.Generic;
using BudUI;
using Es;
using UI.Base;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class CreatorRewardInfoPanel : BasePanel<CreatorRewardInfoPanel>
{
    [SerializeField] private CButton CloseBtn;
    [SerializeField] private CButton RuleBtn;
    [SerializeField] private Image CurItemImg;
    [SerializeField] private Transform TitleObj;
    [SerializeField] private UserBadge UserBadge;
    [SerializeField] private GameObject SourceAni;
    [SerializeField] private Text ScoreFrameTips;
    [SerializeField] private Text WeekTips;
    [SerializeField] private Text MonthTips;
    [SerializeField] private CButton LeftBtn;
    [SerializeField] private CButton RightBtn;
    [SerializeField] private CButton ShowBtn;
    [SerializeField] private CButton AllRewardBtn;
    [SerializeField] private GameObject AllRewardSelect;
    [SerializeField] private CButton CategoryRewardBtn;
    [SerializeField] private GameObject CategoryRewardSelect;
    [SerializeField] private GameObject RewardDropDown;
    [SerializeField] private List<CToggle> DropDownItems;
    [SerializeField] private Text MyScoreText;
    [SerializeField] private List<CToggle> TopInfoItems;
    [SerializeField] private List<Text> TopInfoTexts;
    [SerializeField] private List<Image> CrownObjs;
    [SerializeField] private GameObject RightSeasonText;
    [SerializeField] private List<CToggle> RightInfoItems;
    [SerializeField] private Image Img_HeadCycle;
    [SerializeField] private Transform Trans_TopEffect;
    [SerializeField] private RawImage bgImage;


    private int _curSelectTopIndex = 0;//顶部按钮选择系数
    private int _curSelectCategoryIndex = 0;
    private int _curSelectRightIndex = 0;//右侧按钮选择系数

    private readonly List<CreatorUserScoreInfoData> _creatorUserScoreList = new List<CreatorUserScoreInfoData>();
    /// <summary>创作者各品类积分与排名缓存（与 <see cref="CreatorRequestCtrl.RequestCreatorUserScoreInfo"/> 一致），用于奖励「已激活」判断。</summary>
    private readonly List<CreatorUserScoreInfoData> _creatorBadgeList = new List<CreatorUserScoreInfoData>();

    // 当前 Top 选项对应的 Level 列表（按显示顺序）
    private readonly List<int> _topLevelOptions = new List<int>(4);
    private int _lastAppliedTitleId = -1;

    private const string CreatorSeasonAtlasPath = "Assets/Loadable/UI/UIPanel/CreatorSeasonPanel/CreatorSeasonPanel.spriteatlas";

    public override void OnCreate()
    {
        base.OnCreate();
        CloseBtn.onClick.AddListener(CloseSelf);
        RuleBtn.onClick.AddListener(OnRuleBtnClick);
        LeftBtn.onClick.AddListener(OnLeftBtnClick);
        RightBtn.onClick.AddListener(OnRightBtnClick);
        AllRewardBtn.onClick.AddListener(OnAllRewardBtnClick);
        CategoryRewardBtn.onClick.AddListener(OnCategoryRewardBtnClick);
        ShowBtn.onClick.AddListener(OnShowBtnClick);
        for (int i = 0; i < RightInfoItems.Count; i++)
        {
            int index = i;
            RightInfoItems[i].onValueChanged.AddListener((isOn) => OnRightInfoItemValueChanged(isOn, index));
        }
        for(int i = 0; i < TopInfoItems.Count; i++)
        {
            int index = i;
            TopInfoItems[i].onValueChanged.AddListener((isOn) => OnTopInfoItemValueChanged(isOn, index));
        }
        for(int i = 0; i < DropDownItems.Count; i++)
        {
            int index = i + 1;
            DropDownItems[i].onValueChanged.AddListener((isOn) => OnDropDownItemValueChanged(isOn, index));
        }
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        _curSelectTopIndex = 0;
        _curSelectCategoryIndex = 0;
        RequestMyCreatorScores();
        RequestCreatorBadgeListForActivation();
        RefreshSelectShow();
    }

    private void RefreshSelectShow()
    {
        RebuildTopLevelOptionsFromConfig();
        if (_topLevelOptions.Count == 0)
        {
            _curSelectTopIndex = 0;
        }
        else
        {
            if (_curSelectTopIndex < 0) _curSelectTopIndex = 0;
            if (_curSelectTopIndex >= _topLevelOptions.Count) _curSelectTopIndex = _topLevelOptions.Count - 1;
        }

        AllRewardSelect.gameObject.SetActive(_curSelectCategoryIndex == 0);
        CategoryRewardSelect.gameObject.SetActive(_curSelectCategoryIndex != 0);
        RewardDropDown.SetActive(_curSelectCategoryIndex != 0);

        ApplyTopInfoByOptions();

        if (_curSelectRightIndex < 0) _curSelectRightIndex = 0;
        if (RightInfoItems != null && RightInfoItems.Count > 0 && _curSelectRightIndex >= RightInfoItems.Count)
        {
            _curSelectRightIndex = 0;
        }

        // 分榜：顶部第二档（高级，_curSelectTopIndex==1）显示动作[4]、头像框[5]。
        // 总榜：两档为前100(index0) 与 第一(index1)；仅前100 显示头像框[5]；第一档不显示动作[4]（_curSelectTopIndex==1 勿与分榜第二档混用同一套 Tab 规则）。
        bool canShowActionTab = _curSelectCategoryIndex != 0 && _curSelectTopIndex == 1;
        bool canShowAvatarTab = (_curSelectCategoryIndex != 0 && _curSelectTopIndex == 1)
            || (_curSelectCategoryIndex == 0 && (_curSelectTopIndex <= 2));

        if (!canShowActionTab && _curSelectRightIndex == 4)
        {
            _curSelectRightIndex = 0;
        }

        if (!canShowAvatarTab && _curSelectRightIndex == 5)
        {
            _curSelectRightIndex = 0;
        }

        if (RightInfoItems != null && RightInfoItems.Count > 0)
        {
            RightInfoItems[_curSelectRightIndex].SetIsOnWithoutNotify(true);
        }

        if (RightInfoItems != null && RightInfoItems.Count > 5)
        {
            RightInfoItems[3].gameObject.SetActive(_curSelectCategoryIndex == 0);
            RightInfoItems[4].gameObject.SetActive(canShowActionTab);
            RightInfoItems[5].gameObject.SetActive(canShowAvatarTab);
        }

        if (RightSeasonText != null && RightInfoItems != null && RightInfoItems.Count > 5)
        {
            RightSeasonText.gameObject.SetActive(
                RightInfoItems[3].gameObject.activeSelf
                || RightInfoItems[4].gameObject.activeSelf
                || RightInfoItems[5].gameObject.activeSelf);
        }
        ScoreFrameTips.gameObject.SetActive(_curSelectRightIndex == 2);
        WeekTips.gameObject.SetActive(_curSelectRightIndex == 0 || _curSelectRightIndex == 1);
        MonthTips.gameObject.SetActive(_curSelectRightIndex == 3 || _curSelectRightIndex == 4 || _curSelectRightIndex == 5);
        ApplyCurrentConfigToView();
        RefreshMyScoreText();
    }

    /// <summary>
    /// 与 CreatorSelectType / 服务端 category 一致：0 全部，1～5 为各创作维度积分。
    /// </summary>
    private void RequestMyCreatorScores()
    {
        _creatorUserScoreList.Clear();
        RefreshMyScoreText();

        var uid = AccountDataManager.Inst?.UserInfo?.uid;
        if (string.IsNullOrEmpty(uid) || CreatorRequestCtrl.Inst == null)
        {
            return;
        }

        CreatorRequestCtrl.Inst.RequestCreatorUserScoreInfo(uid, OnMyCreatorScoresReceived, _ => { });
    }

    /// <summary>拉取各品类当周积分与排名，供激活判断。</summary>
    private void RequestCreatorBadgeListForActivation()
    {
        _creatorBadgeList.Clear();
        if (CreatorRequestCtrl.Inst == null)
        {
            return;
        }

        CreatorRequestCtrl.Inst.RequestCreatorUserScoreInfo(AccountDataManager.Inst?.UserInfo?.uid, OnCreatorBadgeListForActivationReceived, _ => { });
    }

    private void OnCreatorBadgeListForActivationReceived(CreatorUserScoreInfoList res)
    {
        if (!this)
        {
            return;
        }

        _creatorBadgeList.Clear();
        if (res?.list != null)
        {
            for (int i = 0; i < res.list.Count; i++)
            {
                var item = res.list[i];
                if (item != null)
                {
                    _creatorBadgeList.Add(item);
                }
            }
        }

        RefreshSelectShow();
    }

    private void OnMyCreatorScoresReceived(CreatorUserScoreInfoList res)
    {
        if (!this)
        {
            return;
        }

        _creatorUserScoreList.Clear();
        if (res?.list != null)
        {
            for (int i = 0; i < res.list.Count; i++)
            {
                var item = res.list[i];
                if (item != null)
                {
                    _creatorUserScoreList.Add(item);
                }
            }
        }

        RefreshMyScoreText();
    }

    private void RefreshMyScoreText()
    {
        if (MyScoreText == null)
        {
            return;
        }

        var category = _curSelectCategoryIndex;
        var found = _creatorUserScoreList.Find(x => x != null && x.category == category);
        MyScoreText.text = found != null ? found.score.ToString() : "0";
    }

    private void RebuildTopLevelOptionsFromConfig()
    {
        _topLevelOptions.Clear();

        if (_curSelectCategoryIndex == 0)
        {
            _topLevelOptions.Add(2);
            _topLevelOptions.Add(3);
            _topLevelOptions.Add(4);
            return;
        }

        var list = DataTables.GetCreatorRewardConfigList();
        if (list == null || list.Count == 0)
        {
            return;
        }

        // 收集当前 category 下的所有 level（去重）
        var levelSet = new HashSet<int>();
        for (int i = 0; i < list.Count; i++)
        {
            var cfg = list[i];
            if (cfg == null) continue;
            if (cfg.Category != _curSelectCategoryIndex) continue;
            levelSet.Add(cfg.Level);
        }

        // 按固定顺序输出（避免 UI 顺序随表变化）
        int[] order = { 0, 1, 2, 3, 4 };
        for (int i = 0; i < order.Length; i++)
        {
            var lv = order[i];
            if (levelSet.Contains(lv))
            {
                _topLevelOptions.Add(lv);
            }
        }
    }

    private void ApplyTopInfoByOptions()
    {
        int optionCount = _topLevelOptions.Count;
        int itemCount = TopInfoItems != null ? TopInfoItems.Count : 0;
        int textCount = TopInfoTexts != null ? TopInfoTexts.Count : 0;
        int crownCount = CrownObjs != null ? CrownObjs.Count : 0;

        // 显隐 + 文案 + 皇冠图（按档位 level 从图集加载）
        for (int i = 0; i < itemCount; i++)
        {
            bool active = i < optionCount;
            TopInfoItems[i].gameObject.SetActive(active);

            if (i < crownCount && CrownObjs[i] != null)
            {
                CrownObjs[i].gameObject.SetActive(active);
                if (active)
                {
                    var spriteName = GetCrownSpriteName(_topLevelOptions[i]);
                    if (!string.IsNullOrEmpty(spriteName) && XAssetLoaderMgr.Inst != null)
                    {
                        var sp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(CreatorSeasonAtlasPath, spriteName, gameObject);
                        CrownObjs[i].sprite = sp;
                        CrownObjs[i].enabled = sp != null;
                    }
                    else
                    {
                        CrownObjs[i].sprite = null;
                        CrownObjs[i].enabled = false;
                    }
                }
                else
                {
                    CrownObjs[i].sprite = null;
                    CrownObjs[i].enabled = false;
                }
            }

            if (active && i < textCount)
            {
                TopInfoTexts[i].text = GetLevelDisplayName(_topLevelOptions[i]);
            }
        }

        // 选中态
        if (optionCount > 0 && _curSelectTopIndex >= 0 && _curSelectTopIndex < itemCount)
        {
            TopInfoItems[_curSelectTopIndex].SetIsOnWithoutNotify(true);
        }
    }

    private static string GetCrownSpriteName(int level)
    {
        return level switch
        {
            0 => "crown3",
            1 => "crown2",
            2 => "crown1",
            3 => "crown1",
            4 => "crown0",
            _ => "crown3",
        };
    }

    private static string GetLevelDisplayName(int level)
    {
        return level switch
        {
            0 => "初级",
            1 => "高级",
            2 => "全服前100",
            3 => "全服前10",
            4 => "全服第一",
            _ => "未知"
        };
    }

    private bool TryGetCurrentCreatorRewardConfig(out CreatorRewardConfig cfg)
    {
        cfg = null;
        int level = (_curSelectTopIndex >= 0 && _curSelectTopIndex < _topLevelOptions.Count)
            ? _topLevelOptions[_curSelectTopIndex]
            : -1;
        if (level < 0)
        {
            return false;
        }

        var list = DataTables.GetCreatorRewardConfigList();
        if (list == null)
        {
            return false;
        }

        for (int i = 0; i < list.Count; i++)
        {
            var c = list[i];
            if (c == null) continue;
            if (c.Category == _curSelectCategoryIndex && c.Level == level)
            {
                cfg = c;
                return true;
            }
        }

        return false;
    }

    private void ApplyCurrentConfigToView()
    {
        TryGetCurrentCreatorRewardConfig(out var cfg);
        int level = (_curSelectTopIndex >= 0 && _curSelectTopIndex < _topLevelOptions.Count)
            ? _topLevelOptions[_curSelectTopIndex]
            : -1;

        if (ShowBtn != null)
        {
            var showPreview = (_curSelectRightIndex == 3 || _curSelectRightIndex == 4)
                && cfg != null
                && ((_curSelectRightIndex == 3 && cfg.ThemeId > 0)
                    || (_curSelectRightIndex == 4 && cfg.ActionId > 0));
            ShowBtn.gameObject.SetActive(showPreview);
        }

        if (CurItemImg == null) return;

        // _curSelectRightIndex==0：显示称号（TitleObj），隐藏 CurItemImg
        // _curSelectRightIndex==1：显示 badge 图
        // __curSelectRightIndex = 2: 显示积分框
        // _curSelectRightIndex==3：显示主页皮肤 icon（ThemeId 从 cfg.ThemeId 读取）
        // _curSelectRightIndex==4：显示动作 icon（pgcId 从 cfg.ActionId 读取）
        // _curSelectRightIndex==5：头像框
        bool showTitle = _curSelectRightIndex == 0;
        bool showBadge = _curSelectRightIndex == 1;
        bool showScoreFrame = _curSelectRightIndex == 2;
        bool showTheme = _curSelectRightIndex == 3;
        bool showAction = _curSelectRightIndex == 4;
        bool showAvatarFrame = _curSelectRightIndex == 5;
        CurItemImg.gameObject.SetActive(showTheme || showAction || showScoreFrame);
        if (!showAvatarFrame)
        {
            ClearAvatarFramePreview();
        }

        UserBadge.gameObject.SetActive(showBadge);
        SourceAni.SetActive(false);
        if (showBadge)
        {
            UserBadge.SetData(_curSelectCategoryIndex, level);
        }
        else if (showScoreFrame)
        {
            var sp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(CreatorSeasonAtlasPath, cfg.ScoreFrameName, gameObject);
            CurItemImg.sprite = sp;
            CurItemImg.enabled = sp != null;
            SourceAni.SetActive(_curSelectCategoryIndex == 0 || _curSelectTopIndex >= 2);
        }
        else if (showTheme)
        {
            int themeId = cfg != null ? cfg.ThemeId : 0;
            var sp = themeId > 0 ? ProfileThemeManager.Inst.LoadThemeIcon(themeId, gameObject) : null;
            CurItemImg.sprite = sp;
            CurItemImg.enabled = sp != null;

        }
        else if (showAction)
        {
            var pgcId = cfg != null && cfg.ActionId > 0 ? cfg.ActionId.ToString() : null;
            var sp = !string.IsNullOrEmpty(pgcId) ? PgcUtils.LoadEmoteIcon(pgcId, gameObject) : null;
            CurItemImg.sprite = sp;
            CurItemImg.enabled = sp != null;
        }
        else if (showTitle)
        {
            CurItemImg.sprite = null;
            CurItemImg.enabled = false;
        }
        else if (showAvatarFrame)
        {
            ApplyAvatarFramePreview(cfg != null ? cfg.HeadFrameId : 0);
        }
        else
        {
            CurItemImg.sprite = null;
            CurItemImg.enabled = false;
        }

        ApplyTitleReward(cfg != null ? cfg.TitleId : 0);
    }

    /// <summary>
    /// 头像框预览：参照 HeadViewWidget 的加载方式，显示 Img_HeadCycle 并挂载顶部特效。
    /// </summary>
    private void ApplyAvatarFramePreview(int headFrameId)
    {
        if (Img_HeadCycle == null)
        {
            return;
        }

        ClearAvatarFramePreview();
        if (headFrameId <= 0 || UserUIWidgetManager.Inst == null)
        {
            return;
        }

        var headCycleData = UserUIWidgetManager.Inst.GetHeadCycleData(headFrameId, gameObject);
        if (headCycleData == null || headCycleData.Sp_HeadCycle == null)
        {
            return;
        }

        Img_HeadCycle.sprite = headCycleData.Sp_HeadCycle;
        Img_HeadCycle.gameObject.SetActive(true);

        if (Trans_TopEffect != null && headCycleData.Effect_Top != null)
        {
            headCycleData.Effect_Top.Instantiate(Trans_TopEffect);
        }
    }

    private void ClearAvatarFramePreview()
    {
        if (Img_HeadCycle != null)
        {
            Img_HeadCycle.sprite = null;
            Img_HeadCycle.gameObject.SetActive(false);
        }

        if (Trans_TopEffect != null)
        {
            ClearChildren(Trans_TopEffect);
        }
    }


    private static CreatorUserScoreInfoData FindScoreRowForCategory(List<CreatorUserScoreInfoData> list, int category)
    {
        if (list == null)
        {
            return null;
        }

        for (int i = 0; i < list.Count; i++)
        {
            var row = list[i];
            if (row != null && row.category == category)
            {
                return row;
            }
        }

        return null;
    }

    /// <summary>各品类中取最小正排名（数字越小越靠前），作总榜无单独行时的兜底。</summary>
    private static int GetBestRankAcrossScoreList(List<CreatorUserScoreInfoData> list)
    {
        if (list == null || list.Count == 0)
        {
            return 0;
        }

        int best = int.MaxValue;
        for (int i = 0; i < list.Count; i++)
        {
            var row = list[i];
            if (row == null || row.rank <= 0)
            {
                continue;
            }

            if (row.rank < best)
            {
                best = row.rank;
            }
        }

        return best == int.MaxValue ? 0 : best;
    }

    /// <summary>总榜档位与名次：2 前100，3 前十，4 第一；0/1 总榜界面通常不出现，按未达成处理。</summary>
    private static bool MeetsOverallRankThreshold(int requiredLevel, int rank)
    {
        if (rank <= 0)
        {
            return false;
        }

        switch (requiredLevel)
        {
            case 2:
                return rank <= 100;
            case 3:
                return rank <= 10;
            case 4:
                return rank == 1;
            default:
                return false;
        }
    }

    private void ApplyTitleReward(int titleId)
    {
        bool showTitle = _curSelectRightIndex == 0;
        if (TitleObj != null)
        {
            TitleObj.gameObject.SetActive(showTitle);
        }

        if (!showTitle || TitleObj == null)
        {
            _lastAppliedTitleId = -1;
            return;
        }

        if (titleId <= 0)
        {
            ClearChildren(TitleObj);
            _lastAppliedTitleId = titleId;
            return;
        }

        // 避免重复加载同一个称号
        if (_lastAppliedTitleId == titleId && TitleObj.childCount > 0)
        {
            return;
        }

        ClearChildren(TitleObj);
        _lastAppliedTitleId = titleId;

        var config = UserUIWidgetManager.Inst != null ? UserUIWidgetManager.Inst.GetTitleData(titleId) : null;
        if (config == null || string.IsNullOrEmpty(config.Prefab))
        {
            return;
        }

        var prefab = Loader.Load<GameObject>(config.Prefab, gameObject);
        if (prefab == null)
        {
            return;
        }

        GameObject.Instantiate(prefab, TitleObj);
    }

    private static void ClearChildren(Transform parent)
    {
        if (parent == null) return;
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            var child = parent.GetChild(i);
            if (child != null)
            {
                GameObject.Destroy(child.gameObject);
            }
        }
    }

    private void OnRuleBtnClick()
    {
        UIManager.Inst.OpenPanel(PanelId.CreatorRewardRulePanel);
    }
    private void OnLeftBtnClick()
    {
        StepTopIndex(-1);
    }
    private void OnRightBtnClick()
    {
        StepTopIndex(+1);
    }

    private void StepTopIndex(int delta)
    {
        int count = _topLevelOptions != null ? _topLevelOptions.Count : 0;
        if (count <= 0) return;
        _curSelectTopIndex = (_curSelectTopIndex + delta) % count;
        if (_curSelectTopIndex < 0) _curSelectTopIndex += count;
        RefreshSelectShow();
    }
    private void OnAllRewardBtnClick()
    {
        _curSelectTopIndex = 0;
        _curSelectCategoryIndex = 0;
        _curSelectRightIndex = 0;
        RefreshSelectShow();
    }
    private void OnCategoryRewardBtnClick()
    {
        if(_curSelectCategoryIndex == 0)
        {
            _curSelectTopIndex = 0;
            _curSelectCategoryIndex = 1;
        }
        RefreshSelectShow();
    }
    private void OnShowBtnClick()
    {
        if (_curSelectRightIndex == 3)
        {
            ProfileTheme theme = ProfileTheme.qpgly;
            if(_curSelectTopIndex == 0)
            {
                theme = ProfileTheme.qpgly;
            }
            if(_curSelectTopIndex == 1 || _curSelectTopIndex == 2)
            {
                theme = ProfileTheme.xytly;
            }
           
            UIManager.Inst.OpenPanel<ProfileThemePreviewPanel>(PanelId.ProfileThemePreviewPanel, theme);
        }
        else if (_curSelectRightIndex == 4)
        {
            if (!TryGetCurrentCreatorRewardConfig(out var cfg) || cfg.ActionId <= 0)
            {
                TipPanel.ShowToast("暂无动作配置");
                return;
            }

            var pgcId = cfg.ActionId.ToString();
            var emoteName = PgcUtils.GetEmoteName(pgcId);
            if (string.IsNullOrEmpty(emoteName))
            {
                var res = DataTables.GetGameResData(pgcId);
                emoteName = res != null ? res.Name : "动作";
            }

            var panel = UIManager.Inst.OpenPanel<RewardPreviewPanel>(PanelId.RewardPreviewPanel);
            panel.SetEventPreview(new List<string> { pgcId },emoteName, "","", "创作者赛季奖励动作", "#FF9A61",bgImage.texture);
        }
    }
    private void OnRightInfoItemValueChanged(bool isOn, int index)
    {
        if (isOn)
        {
            _curSelectRightIndex = index;
            RefreshSelectShow();
        }
    }
    private void OnTopInfoItemValueChanged(bool isOn, int index)
    {
        if(isOn)
        {
            _curSelectTopIndex = index;
            RefreshSelectShow();
        }
    }
    private void OnDropDownItemValueChanged(bool isOn, int index)
    {
        if(isOn)
        {
            if(_curSelectCategoryIndex == index)
            {
                return;
            }
            _curSelectCategoryIndex = index;
            _curSelectTopIndex = 0;
            _curSelectRightIndex = 0;
            RefreshSelectShow();
        }
    }

}
