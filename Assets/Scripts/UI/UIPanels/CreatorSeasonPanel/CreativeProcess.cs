using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CreativeProcess : MonoBehaviour
{
    public Button btn_close;
    //有新增的话这里加就好
    private readonly Dictionary<string, string> _seasonId_time = new() { { "14", "2026年3月27日-2026年5月22日"}};
    public Transform contentRoot;
    public GameObject itemPrefab;
    public GameObject itemInfoPrefab;

    private List<CreatorBadgeInfoData> _badgeList;

    void OnEnable()
    {
        btn_close.onClick.AddListener(() => gameObject.SetActive(false)); 

// #if UNITY_EDITOR
//         _badgeList = BuildTestBadgeList();
//         BuildSeasonItems();
// #else\
if(_badgeList == null)
{
        CreatorRequestCtrl.Inst.RequestCreatorBadgeListInfo(2, res =>
        {
            // 防 NPE：res 或 res.list 为 null 时取空列表，避免 FindAll/.Count 抛错。
            _badgeList = res?.list?.FindAll(b => b.feature == "s14-0") ?? new List<CreatorBadgeInfoData>();
            Debug.Log("_badgeList -- " + _badgeList.Count);
            BuildSeasonItems();
        }, error => Debug.LogError("RequestCreatorBadgeListInfo error: " + error));
}
//#endif
    }
    void OnDisable()
    {
        if(_badgeList != null)
        {
            _badgeList.Clear();
            _badgeList = null;
        }
        foreach (Transform child in contentRoot) Destroy(child.gameObject);
    }

    private void BuildSeasonItems()
    {
        foreach (var season in _seasonId_time.Keys)
        {
            var item = Instantiate(itemPrefab, contentRoot);
            item.name = "s" + season + "-0";
            GameObjectEx.FindComponentByName<Text>(item.transform, "txt_season").text = "s" + season + "赛季";
            GameObjectEx.FindComponentByName<Text>(item.transform, "txt_time").text = _seasonId_time[season];

            var itemInfo = Instantiate(itemInfoPrefab, contentRoot);
            var localTogs = new List<Toggle>(
                GameObjectEx.FindChildByName(itemInfo, "togs").GetComponentsInChildren<Toggle>());
            itemInfo.SetActive(false);

            var showBtn = GameObjectEx.FindComponentByName<Button>(item.transform, "showItemInfo");
            if (showBtn != null)
            {
                showBtn.onClick.AddListener(() =>
                {
                    bool show = !itemInfo.activeSelf;
                    itemInfo.SetActive(show);
                    if (show && localTogs.Count > 0)
                    {
                        if (!localTogs[0].isOn)
                            localTogs[0].isOn = true;
                        else
                            RequestAndFillInfo(itemInfo, 0, item.name);
                    }
                });
            }

            for (int i = 0; i < localTogs.Count; i++)
            {
                int index = i;
                string feature = item.name;
                localTogs[i].onValueChanged.AddListener(isOn =>
                {
                    if (isOn) RequestAndFillInfo(itemInfo, index, feature);
                });
            }
        }
    }

    private void RequestAndFillInfo(GameObject itemInfo, int category, string feature)
    {
        CreatorRequestCtrl.Inst.RequestScoreRankInfo(3, category, feature, res =>
        {
            var userRank = res?.userRankData?.userRank;
            var badge = _badgeList?.Find(b => b.category == category);
            FillInfo(itemInfo, category, userRank, badge);
        }, error => Debug.LogError("RequestScoreRankInfo error: " + error));
    }

    private void FillInfo(GameObject itemInfo, int category, CreatorUserRankInfo userRank, CreatorBadgeInfoData badge)
    {
        // 之前 level > 0 把初级（level=0）误判成"无徽章"，导致初级用户也看到 img_null 占位。
        // 0 是合法等级（初级），只要 badge 对象存在就当作有徽章；服务器若用 null/不返回该 category 表示"无徽章"。
        bool hasBadge = badge != null;

        var imgNull = GameObjectEx.FindChildByName(itemInfo.transform, "img_null");
        if (imgNull != null) imgNull.gameObject.SetActive(!hasBadge);

        int score = badge?.score ?? 0;
        var scoreText = GameObjectEx.FindComponentByName<Text>(itemInfo.transform, "txt_score");
        var nameText = GameObjectEx.FindChildByName(itemInfo.transform, "txt_name");
        if (scoreText != null) scoreText.gameObject.SetActive(score != 0);
        if (nameText != null) nameText.gameObject.SetActive(score == 0);
        if (scoreText != null && score != 0) scoreText.text = score.ToString();

        var rankText = GameObjectEx.FindComponentByName<Text>(itemInfo.transform, "txt_rank");
        if (rankText != null)
        {
            int rank = userRank?.rank ?? 0;
            rankText.text = rank != 0 ? $"第{rank}名" : "未上榜";
        }

        var userBadge = GameObjectEx.FindComponentByName<UserBadge>(itemInfo.transform, "UserBadge");
        if (userBadge != null)
        {
            userBadge.gameObject.SetActive(hasBadge);
            if (hasBadge)
            {
                userBadge.SetData(badge.category, badge.level);
                // var badgeImage = GameObjectEx.FindChildByName(userBadge.transform, "BadgeImage");
                // if (badgeImage != null)
                //     badgeImage.transform.localScale = Vector3.one * 3f;
            }
        }
    }

// #if UNITY_EDITOR
//     private static readonly int[] _testScores = { 9800, 7200, 6500, 4100, 8300, 3700 };

//     private UserRankData BuildTestRankData(int category)
//     {
//         int score = category < _testScores.Length ? _testScores[category] : 5000;
//         return new UserRankData
//         {
//             userInfo = new AccountUserInfo { nickname = "测试用户" },
//             userRank = new CreatorUserRankInfo { rank = category + 1, score = score }
//         };
//     }

//     private List<CreatorBadgeInfoData> BuildTestBadgeList()
//     {
//         // level 含义：0=初级 1=高级 2=top100 3=top10 4=top1
//         // category=0: level==2 → AllBadgeTop100, level==4 → AllBadgeTop1
//         // category!=0: level==3 → OtherBadgeTop10, level==4 → OtherBadgeTop1, 其他 → BadgeImage
//         return new List<CreatorBadgeInfoData>
//         {
//             new() { category = 0, level = 2 }, // 全部 top100
//             new() { category = 1, level = 4 }, // 2D皮肤 top1
//             new() { category = 2, level = 3 }, // 3D皮肤 top10
//             new() { category = 3, level = 2 }, // 动作 top100（BadgeImage）
//             new() { category = 4, level = 1 }, // 地图 高级
//             new() { category = 5, level = 0 }, // 工具 初级
//         };
//     }
// #endif
}
