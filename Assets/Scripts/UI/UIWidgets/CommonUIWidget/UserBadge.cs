using UnityEngine;
using UnityEngine.UI;

public class UserBadge : MonoBehaviour
{
    [SerializeField] private GameObject AllBadgeTop1;
    [SerializeField] private GameObject AllBadgeTop10;
    [SerializeField] private GameObject AllBadgeTop100;
    [SerializeField] private Image BadgeImage;
    [SerializeField] private GameObject OtherBadgeTop1;
    [SerializeField] private GameObject OtherBadgeTop10;

    private const string CreatorSeasonAtlasPath = "Assets/Loadable/UI/UIPanel/CreatorSeasonPanel/CreatorSeasonPanel.spriteatlas";

    /// <summary>与 ProfileCard 一致：rank、cloth、skin、action、map、tool（index 对应 category 1～5）</summary>
    private static readonly string[] CategorySpriteBaseMap =
    {
        "rank",
        "cloth",
        "skin",
        "action",
        "map",
        "tool"
    };

// 0 初级
// 1 高级
// 2 top100
// 3 top10
// 4 top1
    public void Hide()
    {
        if (AllBadgeTop1 != null) AllBadgeTop1.SetActive(false);
        if (AllBadgeTop10 != null) AllBadgeTop10.SetActive(false);
        if (AllBadgeTop100 != null) AllBadgeTop100.SetActive(false);
        if (OtherBadgeTop1 != null) OtherBadgeTop1.SetActive(false);
        if (OtherBadgeTop10 != null) OtherBadgeTop10.SetActive(false);
        // 没有徽章时把 BadgeImage 切到 defaultBadge 占位图，避免 SetData 留下的 sprite/disabled 状态导致徽章位置空白
        if (BadgeImage != null && XAssetLoaderMgr.Inst != null)
        {
            var sp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(CreatorSeasonAtlasPath, "defaultBadge", gameObject);
            BadgeImage.sprite = sp;
            BadgeImage.enabled = sp != null;
        }
    }

    public void SetData(int category, int level)
    {
        if (OtherBadgeTop1 != null)
        {
            OtherBadgeTop1.SetActive(false);
        }

        if (OtherBadgeTop10 != null)
        {
            OtherBadgeTop10.SetActive(false);
        }

        if (AllBadgeTop1 != null)
        {
            AllBadgeTop1.SetActive(false);
        }

        if (AllBadgeTop10 != null)
        {
            AllBadgeTop10.SetActive(false);
        }

        if (AllBadgeTop100 != null)
        {
            AllBadgeTop100.SetActive(false);
        }

        if (category == 0)
        {
            if (AllBadgeTop1 != null)
            {
                AllBadgeTop1.SetActive(level == 4);
            }
            if (AllBadgeTop10 != null)
            {
                AllBadgeTop10.SetActive(level == 3);
            }
            if (AllBadgeTop100 != null)
            {
                AllBadgeTop100.SetActive(level == 2);
            }

            bool anyTopShowing = level == 2 || level == 3 || level == 4;
            // 总榜 level 0/1（初级/高级）走 BadgeImage 加载 rank3/rank2 sprite；top1/10/100 之外的等级原本被清成空白 Image。
            if (BadgeImage != null)
            {
                if (anyTopShowing)
                {
                    BadgeImage.sprite = null;
                    BadgeImage.enabled = false;
                }
                else if (XAssetLoaderMgr.Inst != null)
                {
                    var rankSpriteName = BuildBadgeSpriteName(category, level);
                    if (string.IsNullOrEmpty(rankSpriteName))
                    {
                        rankSpriteName = "defaultBadge";
                    }
                    var rankSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(CreatorSeasonAtlasPath, rankSpriteName, gameObject);
                    BadgeImage.sprite = rankSp;
                    BadgeImage.enabled = rankSp != null;
                    if (rankSp != null)
                    {
                        //BadgeImage.rectTransform.sizeDelta = new Vector2(75, 75);
                    }
                }
            }

            return;
        }

        if (OtherBadgeTop1 != null)
        {
            OtherBadgeTop1.SetActive(level == 4);
        }

        if (OtherBadgeTop10 != null)
        {
            OtherBadgeTop10.SetActive(level == 3);
        }

        if (BadgeImage == null || XAssetLoaderMgr.Inst == null)
        {
            return;
        }

        var spriteName = BuildBadgeSpriteName(category, level);
        if (string.IsNullOrEmpty(spriteName))
        {
            spriteName = "defaultBadge";
        }

        var sp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(CreatorSeasonAtlasPath, spriteName, gameObject);
        BadgeImage.sprite = sp;
        BadgeImage.enabled = sp != null;
        if(sp != null)
        {
            //BadgeImage.rectTransform.sizeDelta = new Vector2(75, 75);
        }
    }

    /// <summary>level 0 一律走 defaultBadge（返回 null 让上层 fallback）；level 1~4 对应后缀 2、1、1、0。</summary>
    private static string BuildBadgeSpriteName(int category, int level)
    {
        if (level == 0)
        {
            // 初级：所有 category 都用 defaultBadge 占位
            return null;
        }
        if (category <= 0 || category >= CategorySpriteBaseMap.Length)
        {
            return null;
        }

        var baseName = CategorySpriteBaseMap[category];
        if (string.IsNullOrEmpty(baseName))
        {
            return null;
        }

        var suffix = level switch
        {
            1 => "2",
            2 => "1",
            3 => "1",
            4 => "0",
            _ => null
        };

        return string.IsNullOrEmpty(suffix) ? null : baseName + suffix;
    }
}
