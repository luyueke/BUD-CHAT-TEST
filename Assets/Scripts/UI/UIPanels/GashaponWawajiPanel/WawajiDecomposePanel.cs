using System.Collections.Generic;
using Basic.Utils;
using UI.Base;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class WawajiDecomposePanel : BasePanel<WawajiDecomposePanel>
{
    [SerializeField] private CButton closeBtn;
    [SerializeField] private GameObject contentRootGo;
    [SerializeField] private GameObject contentItemGo;

    public override void OnCreate()
    {
        InitUI();
    }

    private void InitUI()
    {
        closeBtn.onClick.AddListener(() => UIManager.Inst.ClosePanel(this));

        // 娃娃机分解返还的三种货币
        var badgeSprite = PgcUtils.LoadCurrencyIcon(
            GameUtils.ConvertRewardType((int)BUDRewardType.RewardBadge), gameObject);          // 徽章
        var shardsSprite = PgcUtils.LoadCurrencyIcon(
            GameUtils.ConvertRewardType((int)BUDRewardType.RewardCrystalShards), gameObject);   // 泡泡夹(水晶碎片)
        var crystalSprite = PgcUtils.LoadCurrencyIcon(
            GameUtils.ConvertRewardType((int)BUDRewardType.RewardCrystal), gameObject);          // 星辉夹(水晶)

        // 左 = 被分解物品图标，右 = 返还货币图标 + 数量
        var dataList = new List<(MusicNoteDecomponseItemData, MusicNoteDecomponseItemData)>()
        {
            // —— 转换徽章 ——
            (new MusicNoteDecomponseItemData() { iconSprite = PgcUtils.LoadAvatarIcon("10800096", gameObject) },
             new MusicNoteDecomponseItemData() { iconSprite = badgeSprite, count = 50 }),
            (new MusicNoteDecomponseItemData() { iconSprite = PgcUtils.LoadAvatarIcon("10100006", gameObject) },
             new MusicNoteDecomponseItemData() { iconSprite = badgeSprite, count = 50 }),
            (new MusicNoteDecomponseItemData() { iconSprite = PgcUtils.LoadAvatarIcon("10700036", gameObject) },
             new MusicNoteDecomponseItemData() { iconSprite = badgeSprite, count = 50 }),
            (new MusicNoteDecomponseItemData() { iconSprite = PgcUtils.LoadEmoteIcon("40100529", gameObject) },
             new MusicNoteDecomponseItemData() { iconSprite = badgeSprite, count = 150 }),
            (new MusicNoteDecomponseItemData() { iconSprite = PgcUtils.LoadEmoteIcon("40100362", gameObject) },
             new MusicNoteDecomponseItemData() { iconSprite = badgeSprite, count = 150 }),
            (new MusicNoteDecomponseItemData() { iconSprite = PgcUtils.LoadEmoteIcon("40100534", gameObject) },
             new MusicNoteDecomponseItemData() { iconSprite = badgeSprite, count = 150 }),
            (new MusicNoteDecomponseItemData() { iconSprite = PgcUtils.LoadEmoteIcon("40100320", gameObject) },
             new MusicNoteDecomponseItemData() { iconSprite = badgeSprite, count = 150 }),
            (new MusicNoteDecomponseItemData() { iconSprite = PgcUtils.LoadEmoteIcon("40100329", gameObject) },
             new MusicNoteDecomponseItemData() { iconSprite = badgeSprite, count = 150 }),
            (new MusicNoteDecomponseItemData() { iconSprite = PgcUtils.LoadEmoteIcon("40100490", gameObject) },
             new MusicNoteDecomponseItemData() { iconSprite = badgeSprite, count = 150 }),
            // 套装(返还徽章)
            (new MusicNoteDecomponseItemData() { iconSprite = PgcUtils.LoadBundleIcon("145", gameObject) },
             new MusicNoteDecomponseItemData() { iconSprite = badgeSprite, count = 100 }),
            (new MusicNoteDecomponseItemData() { iconSprite = PgcUtils.LoadBundleIcon("146", gameObject) },
             new MusicNoteDecomponseItemData() { iconSprite = badgeSprite, count = 100 }),
            (new MusicNoteDecomponseItemData() { iconSprite = PgcUtils.LoadBundleIcon("107", gameObject) },
             new MusicNoteDecomponseItemData() { iconSprite = badgeSprite, count = 100 }),

            // —— 转换泡泡夹 ——
            (new MusicNoteDecomponseItemData() { iconSprite = PgcUtils.LoadBundleIcon("147", gameObject) },
             new MusicNoteDecomponseItemData() { iconSprite = shardsSprite, count = 100 }),
            (new MusicNoteDecomponseItemData() { iconSprite = PgcUtils.LoadAvatarIcon("10500015", gameObject) },
             new MusicNoteDecomponseItemData() { iconSprite = shardsSprite, count = 50 }),
            (new MusicNoteDecomponseItemData() { iconSprite = PgcUtils.LoadEmoteIcon("40100527", gameObject) },
             new MusicNoteDecomponseItemData() { iconSprite = shardsSprite, count = 50 }),
            (new MusicNoteDecomponseItemData() { iconSprite = PgcUtils.LoadEmoteIcon("40300330", gameObject) },
             new MusicNoteDecomponseItemData() { iconSprite = shardsSprite, count = 100 }),
            (new MusicNoteDecomponseItemData() { iconSprite = PgcUtils.LoadEmoteIcon("40300127", gameObject) },
             new MusicNoteDecomponseItemData() { iconSprite = shardsSprite, count = 100 }),
            (new MusicNoteDecomponseItemData() { iconSprite = PgcUtils.LoadAvatarIcon("10500074", gameObject) },
             new MusicNoteDecomponseItemData() { iconSprite = shardsSprite, count = 150 }),
            (new MusicNoteDecomponseItemData() { iconSprite = PgcUtils.LoadAvatarIcon("10500059", gameObject) },
             new MusicNoteDecomponseItemData() { iconSprite = shardsSprite, count = 150 }),

            // —— 转换星辉夹 ——
            (new MusicNoteDecomponseItemData() { iconSprite = PgcUtils.LoadVehicleIcon("160100003", gameObject) },
             new MusicNoteDecomponseItemData() { iconSprite = crystalSprite, count = 6 }),
            (new MusicNoteDecomponseItemData() { iconSprite = PgcUtils.LoadVehicleIcon("160100006", gameObject) },
             new MusicNoteDecomponseItemData() { iconSprite = crystalSprite, count = 9 }),
        };

        // 按价值排序：星辉夹(Crystal) > 泡泡夹(CrystalShards) > 徽章(Badge)，同币种内按数量降序
        int RankOf(MusicNoteDecomponseItemData cur)
        {
            if (cur.iconSprite == crystalSprite) return 0;
            if (cur.iconSprite == shardsSprite) return 1;
            return 2; // badge
        }
        dataList.Sort((a, b) =>
        {
            int ra = RankOf(a.Item2), rb = RankOf(b.Item2);
            if (ra != rb) return ra.CompareTo(rb);
            return b.Item2.count.CompareTo(a.Item2.count); // 同币种数量降序
        });

        for (int i = 0; i < dataList.Count; i++)
        {
            var item = Instantiate(contentItemGo, contentRootGo.transform);
            item.SetActive(true);
            GameObjectEx.FindChildByName(item, "icon1").GetComponent<Image>().sprite = dataList[i].Item1.iconSprite;
            GameObjectEx.FindChildByName(item, "icon2").GetComponent<Image>().sprite = dataList[i].Item2.iconSprite;
            GameObjectEx.FindChildByName(item, "txt").GetComponent<Text>().text = dataList[i].Item2.count.ToString();
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRootGo.GetComponent<RectTransform>());
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRootGo.GetComponent<RectTransform>());
    }
}
