using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HistoricalRanking : MonoBehaviour
{



     public string seasonId = "14";//默认显示赛季14的数据,有新增的话这里改就好

    //有新增的话这里加就好
    private readonly Dictionary<string, string> _seasonId_time = new() { { "14", "2026.3.27-2026.5.22" } };


    public Button btn_close;
    public Transform contentRoot;
    public GameObject itemPrefab;
    public List<Toggle> togs;

    private List<UserRankData> _rankList;

    public Transform sv_togs;
    public GameObject togs_season;
   

    void Awake()
    {
        btn_close.onClick.AddListener(() => gameObject.SetActive(false));

        foreach (var child in _seasonId_time.Keys)
        {
            var tog = Instantiate(togs_season, sv_togs);
            tog.name = child;
            var seasonId = child;
            GameObjectEx.FindComponentByName<Text>(tog, "Label").text = "S" + child + "赛季\n" + _seasonId_time[child];
            GameObjectEx.FindComponentByName<Text>(tog, "Background").text = "S" + child + "赛季\n" + _seasonId_time[child];
            tog.GetComponent<Toggle>().group = sv_togs.GetComponent<ToggleGroup>();
            tog.GetComponent<Toggle>().onValueChanged.AddListener(isOn =>
            {
                if (isOn)
                {
                    for (int i = 0; i < togs.Count; i++)
                    {
                        int index = i;
                        togs[i].onValueChanged.RemoveAllListeners();
                        togs[i].onValueChanged.AddListener(isOn =>
                        {
                            if (isOn)
                            {
                                OnRankTypeChanged(seasonId, index);
                            }
                        });
                    }
                    togs[0].onValueChanged.Invoke(true);
                }
            });
            
            
        }
        GameObjectEx.FindComponentByName<Toggle>(sv_togs, seasonId).onValueChanged.Invoke(true);
    }

    private void OnRankTypeChanged(string seasonId, int index)
    {
// #if UNITY_EDITOR
//         _badgeList = BuildTestBadgeList();
//         _rankList = BuildTestRankList(index);
//         TryRefreshItems();
// #else
        CreatorRequestCtrl.Inst.RequestScoreRankInfo(3, index, "s" + seasonId + "-0", res =>
        {
            _rankList = res?.list;
            TryRefreshItems();
        }, error => Debug.LogError("RequestScoreRankInfo error: " + error));
//#endif
    }

// #if UNITY_EDITOR
//     private static readonly string[][] _testNames =
//     {
//         new[] { "创作大神A", "星光少女B", "像素艺术家C", "音符布丁D", "冒险者E", "彩虹工坊F", "梦境设计师G", "银河探索者H", "花田守护者I", "光影魔术手J" },
//         new[] { "2D皮肤第一名", "2D皮肤第二名", "2D皮肤第三名", "2D皮肤第四名", "2D皮肤第五名" },
//         new[] { "3D皮肤第一名", "3D皮肤第二名", "3D皮肤第三名", "3D皮肤第四名", "3D皮肤第五名", "3D皮肤第六名" },
//         new[] { "动作第一名", "动作第二名", "动作第三名" },
//         new[] { "地图第一名", "地图第二名", "地图第三名", "地图第四名", "地图第五名", "地图第六名", "地图第七名" },
//         new[] { "工具第一名", "工具第二名", "工具第三名", "工具第四名" },
//     };

//     private List<UserRankData> BuildTestRankList(int index)
//     {
//         var names = index < _testNames.Length ? _testNames[index] : _testNames[0];
//         var list = new List<UserRankData>();
//         int baseScore = 10000 - index * 500;
//         for (int i = 0; i < names.Length; i++)
//         {
//             list.Add(new UserRankData
//             {
//                 userInfo = new AccountUserInfo { nickname = names[i] },
//                 userRank = new CreatorUserRankInfo { rank = i + 1, score = baseScore - i * 600 }
//             });
//         }
//         return list;
//     }

//     private List<CreatorBadgeInfoData> BuildTestBadgeList()
//     {
//         var list = new List<CreatorBadgeInfoData>();
//         for (int i = 0; i < 10; i++)
//         {
//             list.Add(new CreatorBadgeInfoData { category = 1, level = (i < 3 ? 3 : i < 6 ? 2 : 1), rank = i + 1 });
//         }
//         return list;
//     }
// #endif
    private void TryRefreshItems()
    {
        if (_rankList == null) return;

        contentRoot.ClearChildren();

        foreach (var rankData in _rankList)
        {
            var item = Instantiate(itemPrefab, contentRoot);
            SetItemData(item, rankData);
        }
    }

    private void SetItemData(GameObject item, UserRankData data)
    {
        
        GameObjectEx.FindChildByName(item.transform, "img_ranking1").gameObject.SetActive(data.userRank.rank == 1);
        GameObjectEx.FindChildByName(item.transform, "img_ranking2").gameObject.SetActive(data.userRank.rank == 2);
        GameObjectEx.FindChildByName(item.transform, "img_ranking3").gameObject.SetActive(data.userRank.rank == 3);
        GameObjectEx.FindChildByName(item.transform, "img_ranking").gameObject.SetActive(data.userRank.rank > 3);
        var rankText = GameObjectEx.FindComponentByName<Text>(item.transform, "rank");
        if (rankText != null)
        {
            if (data.userRank.rank > 3)
            {
               rankText.text = data.userRank.rank.ToString();
            }
        }
        var nicknameText = GameObjectEx.FindComponentByName<Text>(item.transform, "nickName");
        if (nicknameText != null && data.userInfo != null)
            nicknameText.text = data.userInfo.nickname;

        var scoreText = GameObjectEx.FindComponentByName<Text>(item.transform, "score");
        if (scoreText != null)
            scoreText.text = data.userRank?.score.ToString() ?? "0";

        var headInfo = GameObjectEx.FindComponentByName<HeadViewWidget>(item.transform, "HeadViewWidget");
        if (headInfo != null && data.userInfo != null)
            headInfo.InitHeadCycle(data.userInfo);

        var userBadge = GameObjectEx.FindComponentByName<UserBadge>(item.transform, "UserBadge");
        if (userBadge != null)
        {
            var badge = data.userInfo?.creatorBadgeInfo;
            userBadge.gameObject.SetActive(badge != null);
            if (badge != null)
                userBadge.SetData(badge.category, badge.level);
        }
    }
}
