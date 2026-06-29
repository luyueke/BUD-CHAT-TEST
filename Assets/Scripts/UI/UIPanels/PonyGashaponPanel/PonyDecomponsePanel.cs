using System;
using System.Collections.Generic;
using Basic.Utils;
using Com.TheFallenGames.OSA.Util.IO;
using Game.GameSetting;
using GameData.Account;
using UI.Base;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class PonyDecomponseItemData
{
    public Sprite iconSprite;
    public int count;
}
public class PonyDecomponsePanel : BasePanel<PonyDecomponsePanel>
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
        var currencyType = GameUtils.ConvertRewardType((int)BUDRewardType.RewardBadge);
        var currencyType1 = GameUtils.ConvertRewardType((int)BUDRewardType.RewardCrystalShards);
        var currencyType2 = GameUtils.ConvertRewardType((int)BUDRewardType.RewardCrystal);
        var currencySprite = PgcUtils.LoadCurrencyIcon(currencyType, gameObject);
        var currencySprite1 = PgcUtils.LoadCurrencyIcon(currencyType1, gameObject);
        var currencySprite2 = PgcUtils.LoadCurrencyIcon(currencyType2, gameObject);
        var dataList = new List<(PonyDecomponseItemData, PonyDecomponseItemData)>() {
            (new PonyDecomponseItemData() {
                iconSprite = PgcUtils.LoadVehicleIcon("160200002", gameObject),
            }, new PonyDecomponseItemData() {
                iconSprite = currencySprite2,
                count = 9
            }),
            (new PonyDecomponseItemData() {
                iconSprite = PgcUtils.LoadVehicleIcon("160100002", gameObject),
            }, new PonyDecomponseItemData() {
                iconSprite = currencySprite2,
                count = 6
            }),
            (new PonyDecomponseItemData() {
                iconSprite = PgcUtils.LoadAvatarIcon("10500019", gameObject),
            }, new PonyDecomponseItemData() {
                iconSprite = currencySprite1,
                count = 150
            }),
            (new PonyDecomponseItemData() {
                iconSprite = PgcUtils.LoadAvatarIcon("10500013", gameObject),
            }, new PonyDecomponseItemData() {
                iconSprite = currencySprite1,
                count = 150
            }),
            //奖池5
               (new PonyDecomponseItemData() {
                iconSprite = PgcUtils.LoadEmoteIcon("40100323", gameObject),
            }, new PonyDecomponseItemData() {
                iconSprite = currencySprite1,
                count = 100
            }),
            (new PonyDecomponseItemData() {
                iconSprite = PgcUtils.LoadEmoteIcon("40100321", gameObject),
            }, new PonyDecomponseItemData() {
                iconSprite = currencySprite1,
                count = 100
            }),
            (new PonyDecomponseItemData() {
                iconSprite = PgcUtils.LoadEmoteIcon("40300281", gameObject),
            }, new PonyDecomponseItemData() {
                iconSprite = currencySprite1,
                count = 50
            }),
            (new PonyDecomponseItemData() {
                iconSprite = PgcUtils.LoadAvatarIcon("11400005", gameObject),
            }, new PonyDecomponseItemData() {
                iconSprite = currencySprite1,
                count = 50
            }),
            (new PonyDecomponseItemData() {
                iconSprite = PgcUtils.LoadEmoteIcon("40100045", gameObject),
            }, new PonyDecomponseItemData() {
                iconSprite = currencySprite,
                count = 150
            }),
            (new PonyDecomponseItemData() {
                iconSprite = PgcUtils.LoadEmoteIcon("40200312", gameObject),
            }, new PonyDecomponseItemData() {
                iconSprite = currencySprite,
                count = 150
            }),
            (new PonyDecomponseItemData() {
                iconSprite = PgcUtils.LoadEmoteIcon("40200328", gameObject),
            }, new PonyDecomponseItemData() {
                iconSprite = currencySprite,
                count = 150
            }),
            (new PonyDecomponseItemData() {
                iconSprite = PgcUtils.LoadEmoteIcon("40100058", gameObject),
            }, new PonyDecomponseItemData() {
                iconSprite = currencySprite,
                count = 150
            }),
               (new PonyDecomponseItemData() {
                iconSprite = PgcUtils.LoadEmoteIcon("40100320", gameObject),
            }, new PonyDecomponseItemData() {
                iconSprite = currencySprite,
                count = 150
            }),
             (new PonyDecomponseItemData() {
                iconSprite = PgcUtils.LoadBundleIcon("25", gameObject),
            }, new PonyDecomponseItemData() {
                iconSprite = currencySprite,
                count = 100
            }),
            (new PonyDecomponseItemData() {
                iconSprite = PgcUtils.LoadBundleIcon("119", gameObject),
            }, new PonyDecomponseItemData() {
                iconSprite = currencySprite,
                count = 100
            }),
             (new PonyDecomponseItemData() {
                iconSprite = PgcUtils.LoadBundleIcon("118", gameObject),
            }, new PonyDecomponseItemData() {
                iconSprite = currencySprite,
                count = 100
            }),
            (new PonyDecomponseItemData() {
                iconSprite = PgcUtils.LoadAvatarIcon("10900050", gameObject), //绿色熊熊头套
            }, new PonyDecomponseItemData() {
                iconSprite = currencySprite,
                count = 50
            }),
            (new PonyDecomponseItemData() {
                iconSprite = PgcUtils.LoadAvatarIcon("10900049", gameObject), //黄色熊熊头套
            }, new PonyDecomponseItemData() {
                iconSprite = currencySprite,
                count = 50
            }),
            (new PonyDecomponseItemData() {
                iconSprite = PgcUtils.LoadAvatarIcon("10900046", gameObject), //蓝色熊熊头套
            }, new PonyDecomponseItemData() {
                iconSprite = currencySprite,
                count = 50
            }),
        };

        foreach (var data in dataList)
        {
            var item = Instantiate(contentItemGo, contentRootGo.transform);
            item.SetActive(true);
            GameObjectEx.FindChildByName(item, "icon1").GetComponent<Image>().sprite = data.Item1.iconSprite;
            GameObjectEx.FindChildByName(item, "icon2").GetComponent<Image>().sprite = data.Item2.iconSprite;
            GameObjectEx.FindChildByName(item, "txt").GetComponent<Text>().text = data.Item2.count.ToString();
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRootGo.GetComponent<RectTransform>());
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRootGo.GetComponent<RectTransform>());
    }


}
