using System;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class BadgeListItem : MonoBehaviour
{
    [SerializeField] private Text TitleTxt;
    [SerializeField] private Image BadgeImg;
    [SerializeField] private Text ScoreTxt;
    [SerializeField] private Text LevelTxt;
    [SerializeField] private Text HistoryLevelTxt;
    [SerializeField] private Text HistoryScoreTxt;
    [SerializeField] private UserBadge UserBadge;
    [SerializeField] private CButton UseBtn;

    private CreatorBadgeInfoData _model;
    private bool _isHistory;

    private const string BadgeAtlasPath = "Assets/Loadable/UI/UIPanel/CreatorSeasonPanel/CreatorSeasonPanel.spriteatlas";

    private static readonly string[] BadgeCategorySpriteNameMap =
    {
        "rank",  // 0 全服
        "cloth", // 1 2D皮肤
        "skin",  // 2 3D皮肤
        "action",// 3 动作
        "map",   // 4 地图
        "tool"   // 5 工具
    };

    public void Init()
    {
        UseBtn.onClick.AddListener(OnClickUseBtn);
    }
    private void OnClickUseBtn()
    {
        if (_isHistory || _model == null || AccountDataManager.Inst == null)
        {
            return;
        }

        var myBadge = AccountDataManager.Inst.UserInfo?.creatorBadgeInfo;
        bool sameCategory = myBadge != null && myBadge.category == _model.category;
        if (sameCategory)
        {
            return;
        }

        if (UseBtn != null)
        {
            UseBtn.interactable = false;
        }

        AccountDataManager.Inst.SendSetCreatorBadgeRequest(_model, _ =>
        {
            if (!this)
            {
                return;
            }

            var adapter = GetComponentInParent<BadgeListAdapter>();
            adapter?.Refresh(keepVelocity: true);
        });
    }

    public void SetData(CreatorBadgeInfoData model, Action<CreatorBadgeInfoData> onClick, bool isSelected, bool isHistory = false)
    {
        _model = model;
        _isHistory = isHistory;

        TitleTxt.text = GetCategoryTitle(model != null ? model.category : 0);

        bool showWeek = !isHistory;

        if (ScoreTxt != null) ScoreTxt.gameObject.SetActive(showWeek);
        if (LevelTxt != null) LevelTxt.gameObject.SetActive(showWeek);
        if (HistoryLevelTxt != null) HistoryLevelTxt.gameObject.SetActive(isHistory);

        if (showWeek)
        {
            var myBadge = AccountDataManager.Inst != null ? AccountDataManager.Inst.UserInfo?.creatorBadgeInfo : null;
            bool sameCategory = myBadge != null && model != null && myBadge.category == model.category;

            if (ScoreTxt != null) ScoreTxt.text = model != null ? model.score.ToString() : "0";
            if (LevelTxt != null) LevelTxt.text = GetLevelTitle(model != null ? model.level : -1);
            if (HistoryScoreTxt != null)
            {
                HistoryScoreTxt.text = sameCategory ? "使用中" : "使用";
            }

            if (UseBtn != null)
            {
                UseBtn.interactable = !sameCategory;
            }
        }
        else
        {
            if (HistoryLevelTxt != null) HistoryLevelTxt.text = GetLevelTitle(model != null ? model.level : -1);
            if (HistoryScoreTxt != null) HistoryScoreTxt.text = model != null ? model.score.ToString() : "0";
        }

        RefreshBadgeIcon(model);
    }

    private void RefreshBadgeIcon(CreatorBadgeInfoData model)
    {
        if (UserBadge != null)
        {
            UserBadge.gameObject.SetActive(model != null);
            if (model != null)
            {
                UserBadge.SetData(model.category, model.level);
            }

            if (BadgeImg != null)
            {
                BadgeImg.gameObject.SetActive(false);
            }
            return;
        }

    }

    private static string BuildBadgeSpriteName(CreatorBadgeInfoData badgeInfo)
    {
        if (badgeInfo == null) return null;
        if (badgeInfo.category < 0 || badgeInfo.category >= BadgeCategorySpriteNameMap.Length) return null;

        var baseName = BadgeCategorySpriteNameMap[badgeInfo.category];
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

    private static string GetCategoryTitle(int category)
    {
        return category switch
        {
            0 => "创作总榜",
            1 => "2D皮肤创作",
            2 => "3D皮肤创作",
            3 => "动作创作",
            4 => "地图创作",
            5 => "工具创作",
            _ => "未知"
        };
    }

    private static string GetLevelTitle(int level)
    {
        return level switch
        {
            0 => "初级",
            1 => "高级",
            2 => "前100",
            3 => "前10",
            4 => "前1",
            _ => string.Empty
        };
    }
}
