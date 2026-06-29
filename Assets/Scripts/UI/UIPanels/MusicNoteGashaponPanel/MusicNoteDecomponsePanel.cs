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

public class MusicNoteDecomponseItemData
{
    public Sprite iconSprite;
    public int count;
}
public class MusicNoteDecomponsePanel : BasePanel<MusicNoteDecomponsePanel>
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
        var currencyType1 = GameUtils.ConvertRewardType((int)BUDRewardType.RewardMusicNoteCrystal);
        var currencyType2 = GameUtils.ConvertRewardType((int)BUDRewardType.RewardMusicNoteCrystalShards);
        var currencySprite = PgcUtils.LoadCurrencyIcon(currencyType, gameObject);
        var currencySprite1 = PgcUtils.LoadCurrencyIcon(currencyType1, gameObject);
        var currencySprite2 = PgcUtils.LoadCurrencyIcon(currencyType2, gameObject);
        var dataList = new List<(MusicNoteDecomponseItemData, MusicNoteDecomponseItemData)>() {
            (new MusicNoteDecomponseItemData() {
                iconSprite = PgcUtils.LoadVehicleIcon("160200003", gameObject),
            }, new MusicNoteDecomponseItemData() {
                iconSprite = currencySprite1,
                count = 12
            }),







    (new MusicNoteDecomponseItemData() {
                iconSprite = PgcUtils.LoadBundleIcon("97", gameObject), //提莉亚套装
            }, new MusicNoteDecomponseItemData() {
                iconSprite = currencySprite2,
                count = 500
            }),
       (new MusicNoteDecomponseItemData() {
                iconSprite = PgcUtils.LoadAvatarIcon("10500012", gameObject), //铃铛狐女特效
            }, new MusicNoteDecomponseItemData() {
                iconSprite = currencySprite2,
                count = 150
            }),

 (new MusicNoteDecomponseItemData() {
                iconSprite = PgcUtils.LoadAvatarIcon("10500020", gameObject), //符咒师素可纸娃娃特效
            }, new MusicNoteDecomponseItemData() {
                iconSprite = currencySprite2,
                count = 150
            }),
             (new MusicNoteDecomponseItemData() {
                iconSprite = PgcUtils.LoadAvatarIcon("10500078", gameObject), //提莉亚牵线特效
            }, new MusicNoteDecomponseItemData() {
                iconSprite = currencySprite2,
                count = 150
            }),






            (new MusicNoteDecomponseItemData() {
                iconSprite = PgcUtils.LoadEmoteIcon("40100291", gameObject), //不夜舞
            }, new MusicNoteDecomponseItemData() {
                iconSprite = currencySprite2,
                count = 100
            }),
            (new MusicNoteDecomponseItemData() {
                iconSprite = PgcUtils.LoadEmoteIcon("40200325", gameObject),//猫猫舔爪
            }, new MusicNoteDecomponseItemData() {
                iconSprite = currencySprite2,
                count = 100
            }),
            (new MusicNoteDecomponseItemData() {
                iconSprite = PgcUtils.LoadEmoteIcon("40100280", gameObject), //喵喵舞
            }, new MusicNoteDecomponseItemData() {
                iconSprite = currencySprite2,
                count = 100
            }),




             (new MusicNoteDecomponseItemData() {
                iconSprite = PgcUtils.LoadEmoteIcon("40100276", gameObject), //机械舞
            }, new MusicNoteDecomponseItemData() {
                iconSprite = currencySprite2,
                count = 50
            }),
            (new MusicNoteDecomponseItemData() {
                iconSprite = PgcUtils.LoadEmoteIcon("40100329", gameObject), //草裙舞
            }, new MusicNoteDecomponseItemData() {
                iconSprite = currencySprite2,
                count = 50
            }),
               (new MusicNoteDecomponseItemData() {
                iconSprite = PgcUtils.LoadBundleIcon("130", gameObject), //兔兔太空套装
            }, new MusicNoteDecomponseItemData() {
                iconSprite = currencySprite2,
                count = 50
            }),


  (new MusicNoteDecomponseItemData() {
                iconSprite = PgcUtils.LoadBundleIcon("127", gameObject), //丝绒破坏者套装
            }, new MusicNoteDecomponseItemData() {
                iconSprite = currencySprite,
                count = 150
            }),
              (new MusicNoteDecomponseItemData() {
                iconSprite = PgcUtils.LoadBundleIcon("128", gameObject), //夜色晚礼服套装
            }, new MusicNoteDecomponseItemData() {
                iconSprite = currencySprite,
                count = 150
            }),
              (new MusicNoteDecomponseItemData() {
                iconSprite = PgcUtils.LoadBundleIcon("129", gameObject), //暗黑维珀套装
            }, new MusicNoteDecomponseItemData() {
                iconSprite = currencySprite,
                count = 150
            }),



             (new MusicNoteDecomponseItemData() {
                iconSprite = PgcUtils.LoadBundleIcon("96", gameObject),  //糖果邦尼
                    }, new MusicNoteDecomponseItemData() {
                iconSprite = currencySprite,
                count = 100
            }),
            (new MusicNoteDecomponseItemData() {
                iconSprite = PgcUtils.LoadBundleIcon("125", gameObject), //蓝焰套装
            }, new MusicNoteDecomponseItemData() {
                iconSprite = currencySprite,
                count = 100
            }),
             (new MusicNoteDecomponseItemData() {
                iconSprite = PgcUtils.LoadBundleIcon("126", gameObject), //翡翠黛拉套装
            }, new MusicNoteDecomponseItemData() {
                iconSprite = currencySprite,
                count = 100
            }),



            (new MusicNoteDecomponseItemData() {
                iconSprite = PgcUtils.LoadAvatarIcon("11400026", gameObject), //蓬松兔
            }, new MusicNoteDecomponseItemData() {
                iconSprite = currencySprite,
                count = 50
            }),
            (new MusicNoteDecomponseItemData() {
                iconSprite = PgcUtils.LoadAvatarIcon("11600023", gameObject), //坏兔兔邦尼包
            }, new MusicNoteDecomponseItemData() {
                iconSprite = currencySprite,
                count = 50
            }),
            (new MusicNoteDecomponseItemData() {
                iconSprite = PgcUtils.LoadAvatarIcon("10900055", gameObject), //小恶魔角
                }, new MusicNoteDecomponseItemData() {
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
