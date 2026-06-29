using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UI.Manager;
using UIAgent;
using UnityEngine;
using UnityEngine.UI;

public class TreasureHuntLevelConfig
{
    public int Id;
    public int MapId;
    public List<int> TreasureIds;
    public List<TreasureVec3> Position;
    public List<TreasureVec3> Rotation;
    public List<List<int>> RewardIndex;
    public string Title;
}

public class TreasureVec3
{
    public float x;
    public float y;
    public float z;
}

public class TreasureHuntPanel : ActivityBaseView
{
    [SerializeField] private Text TitleText;
    [SerializeField] private GameObject TipsObj;
    [SerializeField] private List<Image> TreasureItemIcons;
    [SerializeField] private List<Text> TreasureItemNums;
    [SerializeField] private CButton PreviewBtn;
    [SerializeField] private CButton RuleBtn;
    [SerializeField] private CButton HelpGiftBtn;
    [SerializeField] private CButton GetItemBtn;
    [SerializeField] private GameObject GetItemBtnRedPoint;
    [SerializeField] private Image RewardBgSprite;

    [SerializeField] private List<TreasureHuntMap> Map;
    [SerializeField] private List<TreasureHuntLevelItem> TreasureHuntLevelItems;
    [SerializeField] private Animator PlayAnim;
    [SerializeField] private GameObject EffShazi;


    private static readonly string[] LevelRewardIcons =
    {
        "Coin", "YouYouCoin", "11300374", "Badge",     "EnergyCoin",
        "11400091", "LuckyCoin", "Badge", "40300516",  "YouYouCoin",
        "PurpleDreamCoin", "180100008",  "LuckyCoin", "Badge",    "190000031"
    };
    private static readonly int[] LevelRewardNums =
    {
        500, 6, 1, 100, 50,
        1,   6, 150, 1, 6,
        6, 1,  12, 300,  1
    };
    private static readonly string[] ChineseNums =
    {
        "一", "二", "三", "四", "五", "六", "七", "八", "九", "十", "十一", "十二", "十三", "十四", "十五"
    };

    private const string ConfigPath = "Assets/Loadable/UI/ActivityCenterPanel/TreasureHuntPanel/TreasureHuntConfig.json";
    private const string AtlasPath = "Assets/Loadable/UI/ActivityCenterPanel/TreasureHuntPanel/TreasureHuntPanel.spriteatlas";
    private List<TreasureHuntLevelConfig> _configs;
    private TreasureHuntLevelConfig _currentConfig;
    private List<int> _showedGrids;
    private TreasureHuntMap _currentMap;
    private bool _isSending;
    private int _currentLevelIndex;
    private ActivityInfo _info;

    public override void Init(ActivityInfo info)
    {
        base.Init(info);
        var configAsset = Loader.Load<TextAsset>(ConfigPath, gameObject);
        if (configAsset != null)
            _configs = JsonConvert.DeserializeObject<List<TreasureHuntLevelConfig>>(configAsset.text);
        if (PreviewBtn != null) PreviewBtn.onClick.AddListener(OnPreviewClick);
        RuleBtn.onClick.AddListener(OnRuleClick);
        GetItemBtn.onClick.AddListener(OnGetItemBtnClick);
        HelpGiftBtn.onClick.AddListener(OnHelpGiftBtnClick);
    }

    private void OnHelpGiftBtnClick()
    {
        UIAgentManager.Inst.OpenPanel(PanelId.TreasureHuntGiftPanel, _info);
    }

    private void OnGetItemBtnClick()
    {
        UIAgentManager.Inst.OpenPanel(PanelId.TreasureHuntTaskPanel, _info);
    }

    private void OnRuleClick()
    {
        UIAgentManager.Inst.OpenPanel(PanelId.TreasureHuntRulePanel);
    }

    private void OnPreviewClick()
    {
        if (_info == null) return;
        var previewInfo = new ActivityInfo
        {
            activityId = _info.activityId,
            activityTital = _info.activityTital,
            leftTime = _info.leftTime,
            rewardPanelCfg = _info.rewardPanelCfg,
            rewardList = BuildPreviewRewardList()
        };
        var panel = UIManager.Inst.OpenPanel<ActivityRewardPanel>(PanelId.ActivityRewardPanel, ActivityId.TreasureHunting);
        panel.SetPreviewData(previewInfo);
        panel.SetPreviewBg(RewardBgSprite.sprite);
    }

    private List<ActivityRewardInfo> BuildPreviewRewardList()
    {
        var list = new List<ActivityRewardInfo>();
        string[] SortIcons =
        {
            "190000031", "180100008", "40300516", "11400091", "11300374", "Coin", "YouYouCoin", "Badge", "EnergyCoin",
            "LuckyCoin", "Badge", "YouYouCoin", "PurpleDreamCoin", "LuckyCoin", "Badge"
        };
        int[] SortRewardNums =
        {
            1, 1, 1, 1, 1, 500, 6, 100, 50,
            6, 150, 6, 6, 12, 300
        };
        for (int i = 0; i < SortIcons.Length; i++)
        {
            string iconId = SortIcons[i];
            var item = new ActivityRewardInfo { rewardId = i + 1, rewardNum = SortRewardNums[i] };
            if (long.TryParse(iconId, out _))
            {
                if (iconId.StartsWith("1801"))
                {
                    item.budRewardType = (int)BUDRewardType.RewardTypeNicknameFrame;
                    item.pgcId = iconId;
                    item.rewardName = "逐浪沙沙昵称框";
                }
                else if (iconId.StartsWith("1900"))
                {
                    item.budRewardType = (int)BUDRewardType.RewardTypeTitle;
                    item.pgcId = iconId;
                    item.rewardName = "椰风漫夏称号";
                }
                else
                {
                    item.budRewardType = (int)BUDRewardType.RewardPgcResource;
                    item.pgcId = iconId;
                    var pgcName = Es.DataTables.GetPgcNameData(iconId);
                    item.rewardName = (pgcName != null && !string.IsNullOrEmpty(pgcName.Name)) ? pgcName.Name : iconId;
                }
            }
            else if (Enum.TryParse<CurrencyType>(iconId, out var ct))
            {
                item.budRewardType = CurrencyToBudRewardType(ct);
                item.rewardName = PgcUtils.CurrencyName.TryGetValue(ct, out var name) ? name : iconId;
            }
            list.Add(item);
        }
        return list;
    }

    private int CurrencyToBudRewardType(CurrencyType ct) => ct switch
    {
        CurrencyType.Coin => (int)BUDRewardType.RewardCoin,
        CurrencyType.YouYouCoin => (int)BUDRewardType.RewardYouYouCoin,
        CurrencyType.Badge => (int)BUDRewardType.RewardBadge,
        CurrencyType.LuckyCoin => (int)BUDRewardType.RewardLuckyCoin,
        CurrencyType.Shovel => (int)BUDRewardType.RewardTypeShovel,
        CurrencyType.EnergyCoin => (int)BUDRewardType.RewardEnergyCoin,
        CurrencyType.PurpleDreamCoin => (int)BUDRewardType.RewardPurpleDreamCoin,
        _ => (int)BUDRewardType.RewardCoin
    };

    public override void RefrashData(ActivityInfo info)
    {
        if (info == null || info.treasureHauntingInfo == null) return;
        _info = info;
        _currentConfig = _configs?.Find(x => x.Id == info.treasureHauntingInfo.currentLevel);
        if (_currentConfig == null) return;
        _showedGrids = new List<int>(info.treasureHauntingInfo.showedGrids ?? new List<int>());
        var levelEventList = info.treasureHauntingInfo.levelEventList;
        _currentLevelIndex = GetCurrentLevelIndex(levelEventList);
        TitleText.text = BuildLevelTitle();
        bool allLevelsDone = _currentLevelIndex >= ChineseNums.Length;
        for (int i = 0; i < Map.Count; i++)
        {
            bool show = i == _currentConfig.MapId;
            Map[i].gameObject.SetActive(show);
            if (show)
            {
                _currentMap = Map[i];
                _currentMap.Setup(_currentConfig.Position, _currentConfig.Rotation,
                                  _currentConfig.TreasureIds.Count, _showedGrids, OnGridClick,
                                  _currentConfig.TreasureIds, AtlasPath);
                if (allLevelsDone)
                    _currentMap.HideAllGridBtns();
            }
        }

        for (int i = 0; i < TreasureItemIcons.Count; i++)
        {
            bool active = i < _currentConfig.TreasureIds.Count;
            TreasureItemIcons[i].gameObject.SetActive(active);
            if (active)
            {
                string spriteName = $"ItemSmallImg{_currentConfig.TreasureIds[i]}";
                var sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(AtlasPath, spriteName, gameObject);
                if (sprite != null) TreasureItemIcons[i].sprite = sprite;
            }
        }

        for (int i = 0; i < TreasureHuntLevelItems.Count; i++)
        {
            int status = (levelEventList != null && i < levelEventList.Count)
                ? levelEventList[i].eventStatus
                : (int)ClaimStatus.Lock;

            string icon = i < LevelRewardIcons.Length ? LevelRewardIcons[i] : "";
            int eventId = i + 1;
            TreasureHuntLevelItems[i].Setup(icon, status, () => OnLevelGetClick(eventId), OnPreviewClick);
        }

        RefreshTreasureItemNums();
        RequestTaskStatus();
    }

    private void OnLevelGetClick(int eventId)
    {
        var req = new JObject
        {
            ["activityId"] = ActivityId.TreasureHunting.ToString(),
            ["eventId"] = eventId
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ClaimActivityReward, HttpMethod.POST,
            JsonConvert.SerializeObject(req),
            content =>
            {
                var response = JsonConvert.DeserializeObject<ActivityEventClaimResponse>(content);
                AccountDataManager.Inst.BalanceInfo.Refresh();
                int itemIndex = eventId - 1;
                if (itemIndex >= 0 && itemIndex < TreasureHuntLevelItems.Count)
                    TreasureHuntLevelItems[itemIndex].RefreshStatus((int)ClaimStatus.Claimed);
                if (response?.rewardList == null || response.rewardList.Count == 0) return;
                var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
                var serverReward = response.rewardList[0];
                var serverRewardType = (BUDRewardType)serverReward.rewardType;
                if (serverRewardType == BUDRewardType.RewardPgcResource)
                {
                    string pgcId = eventId <= LevelRewardIcons.Length ? LevelRewardIcons[eventId - 1] : string.Empty;
                    panel.ShowPgcRewards(new List<string> { pgcId }, GetPgcRewardName(pgcId));
                }
                else if (serverRewardType == BUDRewardType.RewardTypeNicknameFrame ||
                         serverRewardType == BUDRewardType.RewardTypeTitle)
                {
                    string pgcId = eventId <= LevelRewardIcons.Length ? LevelRewardIcons[eventId - 1] : string.Empty;
                    panel.ShowRewards(new List<CommonRewardItemData>
                    {
                        new()
                        {
                            rewardType = (int)serverRewardType,
                            pgcId = pgcId,
                            rewardName = GetPgcRewardName(pgcId),
                            RewardAmount = 1
                        }
                    });
                }
                else
                {
                    var rewardList = new List<CommonRewardItemData>
                    {
                        new()
                        {
                            IconSp = PgcUtils.LoadRewardIcon(serverRewardType, panel.gameObject),
                            rewardName = PgcUtils.GetRewardName(serverRewardType),
                            RewardAmount = serverReward.amount
                        }
                    };
                    panel.ShowRewards(rewardList);
                }
            },
            error => { });
    }

    private void OnGridClick(int gridId)
    {
        if (_isSending) return;
        if (AccountDataManager.Inst.BalanceInfo.GetAccountCount(CurrencyType.Shovel) <= 0)
        {
            if (TreasureHuntGiftPanel.IsAllPacksPurchased())
            {
                var exchangePanel = UIManager.Inst.OpenPanel<ExchangeCoinPanel>(PanelId.ExchangeCoinPanel);
                exchangePanel.SetData(CurrencyType.Shovel);
            }
            else
            {
                UIManager.Inst.OpenPanel<TreasureHuntGiftPanel>(PanelId.TreasureHuntGiftPanel);
            }
            return;
        }
        _isSending = true;
        if (PlayAnim != null && _currentMap != null)
        {
            var gridTransform = _currentMap.GetGridTransform(gridId);
            if (gridTransform != null)
            {
                PlayAnim.transform.position = gridTransform.position;
                if (EffShazi != null)
                {
                    EffShazi.transform.position = gridTransform.position;
                    foreach (var ps in EffShazi.GetComponentsInChildren<ParticleSystem>())
                        ps.Play();
                }
            }
            PlayAnim.gameObject.SetActive(true);
            PlayAnim.Rebind();
        }
        CoroutineManager.Inst.StartCoroutine(PlayAnimThenSend(gridId));
    }

    private IEnumerator PlayAnimThenSend(int gridId)
    {
        if (PlayAnim != null)
        {
            yield return new WaitForSeconds(1f);
            PlayAnim.gameObject.SetActive(false);
        }
        var req = new JObject
        {
            ["activityId"] = ActivityId.TreasureHunting.ToString(),
            ["rewardId"] = gridId
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ActivityRedeemReward, HttpMethod.POST,
            JsonConvert.SerializeObject(req),
            content =>
            {
                _isSending = false;
                AccountDataManager.Inst.BalanceInfo.Refresh();
                _showedGrids.Add(gridId);
                if (_currentMap != null) _currentMap.HideGrid(gridId);
                RefreshTreasureItemNums();
                if (IsAllRevealed())
                    SendPassEvent();
            },
            error => { _isSending = false; });
    }

    private bool IsAllRevealed()
    {
        return _currentConfig != null &&
               _currentConfig.RewardIndex.TrueForAll(group => group.TrueForAll(id => _showedGrids.Contains(id)));
    }

    private void SendPassEvent()
    {
        var req = new JObject { ["eventId"] = 186 };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.PostEvent, HttpMethod.POST,
            JsonConvert.SerializeObject(req),
            content => { OpenPassPanel(); },
            error => { });
    }

    private void OpenPassPanel()
    {
        string numStr = _currentLevelIndex <= ChineseNums.Length ? ChineseNums[_currentLevelIndex] : _currentLevelIndex.ToString();
        string iconId = _currentLevelIndex < LevelRewardIcons.Length ? LevelRewardIcons[_currentLevelIndex] : "";
        int rewardNum = _currentLevelIndex < LevelRewardNums.Length ? LevelRewardNums[_currentLevelIndex] : 1;
        UIAgentManager.Inst.OpenPanel(PanelId.TreasureHuntPassPanel, iconId, $"第{numStr}关通关", rewardNum);
    }

    private int GetCurrentLevelIndex(List<ActivityEventInfo> eventList)
    {
        if (eventList == null || eventList.Count == 0) return 0;
        int levelIndex = 0;
        for (int i = 0; i < eventList.Count; i++)
        {
            if (eventList[i].eventStatus != (int)ClaimStatus.Lock)
                levelIndex = i + 1;//因为关卡奖励是下一关的，所以要+2，当前关是0，下一关是1，通关后是2，以此类推
        }
        return levelIndex;
    }

    private string BuildLevelTitle()
    {
        string numStr = _currentLevelIndex < ChineseNums.Length ? ChineseNums[_currentLevelIndex] : _currentLevelIndex.ToString();
        string gridSize = _currentLevelIndex == 0 ? "3x3" : _currentLevelIndex == 1 ? "4x4" : _currentLevelIndex == 2 ? "5x5" : "6x6";
        return $"第{numStr}关 {gridSize}";
    }

    private string GetPgcRewardName(string pgcId)
    {
        if (pgcId.StartsWith("1801")) return "逐浪沙沙昵称框";
        if (pgcId.StartsWith("1900")) return "椰风漫夏称号";
        var pgcName = Es.DataTables.GetPgcNameData(pgcId);
        return (pgcName != null && !string.IsNullOrEmpty(pgcName.Name)) ? pgcName.Name : pgcId;
    }

    private bool _isRefreshingRedPoint;

    public void RequestTaskStatus()
    {
        if (_isRefreshingRedPoint) return;
        _isRefreshingRedPoint = true;
        var req = new ActivityCenterInfoReq { idList = new List<string> { "TreasureHuntingDaily" } };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ActivityList, HttpMethod.POST,
            JsonConvert.SerializeObject(req),
            content =>
            {
                _isRefreshingRedPoint = false;
                if (this == null || GetItemBtnRedPoint == null) return;
                var response = JsonConvert.DeserializeObject<ActivityResponse>(content);
                var eventList = response?.list?.Count > 0 ? response.list[0].eventList : null;
                bool hasClaimable = eventList != null && eventList.Exists(e => e.eventStatus == (int)ClaimStatus.Unlocked);
                GetItemBtnRedPoint.SetActive(hasClaimable);
            },
            error => { _isRefreshingRedPoint = false; });
    }

    public void RefreshRedPoint()
    {
        if (GetItemBtnRedPoint == null) return;
        var taskPanel = FindObjectOfType<TreasureHuntTaskPanel>();
        bool hasClaimable = taskPanel != null
            ? taskPanel.HasAnyClaimable()
            : _info?.eventList != null && _info.eventList.Exists(e => e.eventStatus == (int)ClaimStatus.Unlocked);
        GetItemBtnRedPoint.SetActive(hasClaimable);
    }

    private void RefreshTreasureItemNums()
    {
        for (int j = 0; j < TreasureItemNums.Count; j++)
        {
            bool active = j < _currentConfig.RewardIndex.Count;
            TreasureItemNums[j].gameObject.SetActive(active);
            if (active)
            {
                bool allRevealed = _currentConfig.RewardIndex[j].TrueForAll(id => _showedGrids.Contains(id));
                TreasureItemNums[j].text = allRevealed ? "1/1" : "0/1";
            }
        }
    }
}
