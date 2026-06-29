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

public class SockDecomponsePanel : BasePanel<SockDecomponsePanel>
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
        var currencyType1 = GameUtils.ConvertRewardType((int)BUDRewardType.RewardTypeSockTailTicket);
        var currencyType2 = GameUtils.ConvertRewardType((int)BUDRewardType.RewardTypeSockYunyunTicket);
        var currencySprite = PgcUtils.LoadCurrencyIcon(currencyType, gameObject);
        var currencySprite1 = PgcUtils.LoadCurrencyIcon(currencyType1, gameObject);
        var currencySprite2 = PgcUtils.LoadCurrencyIcon(currencyType2, gameObject);
        var dataList = new List<(MusicNoteDecomponseItemData, MusicNoteDecomponseItemData)>() {
            (new MusicNoteDecomponseItemData() {
                iconSprite = PgcUtils.LoadVehicleIcon("160200005", gameObject),
            }, new MusicNoteDecomponseItemData() {
                iconSprite = currencySprite1,
                count = 12
            }),
            //(new MusicNoteDecomponseItemData() {
            //    iconSprite = PgcUtils.LoadBundleIcon("97", gameObject), //提莉亚套装
            //}, new MusicNoteDecomponseItemData() {
            //    iconSprite = currencySprite2,
            //    count = 500
            //}),
            (new MusicNoteDecomponseItemData() {
                iconSprite = PgcUtils.LoadAvatarIcon("10500081", gameObject), //铃铛狐女特效
            }, new MusicNoteDecomponseItemData() {
                iconSprite = currencySprite2,
                count = 150
            }),
            // (new MusicNoteDecomponseItemData() {
            //     iconSprite = UserUIWidgetManager.Inst.GetTitleBg(25,gameObject),  //190000025
            // }, new MusicNoteDecomponseItemData() {
            //     iconSprite = currencySprite2,
            //     count = 150
            // }),
            // (new MusicNoteDecomponseItemData() {
            //     iconSprite = UserUIWidgetManager.Inst.GetNicknameBg(4,gameObject),  //180100004
            // }, new MusicNoteDecomponseItemData() {
            //     iconSprite = currencySprite2,
            //     count = 100
            // }),
            (new MusicNoteDecomponseItemData() {
                iconSprite = PgcUtils.LoadEmoteIcon("40300511", gameObject), //不夜舞
            }, new MusicNoteDecomponseItemData() {
                iconSprite = currencySprite2,
                count = 100
            }),
            (new MusicNoteDecomponseItemData() {
                iconSprite = PgcUtils.LoadEmoteIcon("40200550", gameObject),//猫猫舔爪
            }, new MusicNoteDecomponseItemData() {
                iconSprite = currencySprite2,
                count = 50
            }),
            (new MusicNoteDecomponseItemData() {
                iconSprite = PgcUtils.LoadAvatarIcon("11000274", gameObject), //喵喵舞
            }, new MusicNoteDecomponseItemData() {
                iconSprite = currencySprite2,
                count = 50
            }),
            (new MusicNoteDecomponseItemData() {
                iconSprite = PgcUtils.LoadEmoteIcon("40200534", gameObject), //机械舞
            }, new MusicNoteDecomponseItemData() {
                iconSprite = currencySprite,
                count = 150
            }),
            (new MusicNoteDecomponseItemData() {
                iconSprite = PgcUtils.LoadEmoteIcon("40200532", gameObject), //草裙舞
            }, new MusicNoteDecomponseItemData() {
                iconSprite = currencySprite,
                count = 150
            }),
             (new MusicNoteDecomponseItemData() {
                iconSprite = PgcUtils.LoadEmoteIcon("40200533", gameObject), //兔兔太空套装
            }, new MusicNoteDecomponseItemData() {
                iconSprite = currencySprite,
                count = 150
            }),
            (new MusicNoteDecomponseItemData() {
                iconSprite = PgcUtils.LoadEmoteIcon("40200528", gameObject), //兔兔太空套装
            }, new MusicNoteDecomponseItemData() {
                iconSprite = currencySprite,
                count = 100
            }),
            (new MusicNoteDecomponseItemData() {
                iconSprite = PgcUtils.LoadEmoteIcon("40200529", gameObject), //兔兔太空套装
            }, new MusicNoteDecomponseItemData() {
                iconSprite = currencySprite,
                count = 100
            }),
            (new MusicNoteDecomponseItemData() {
                iconSprite = PgcUtils.LoadEmoteIcon("40200530", gameObject), //兔兔太空套装
            }, new MusicNoteDecomponseItemData() {
                iconSprite = currencySprite,
                count = 100
            }),
            (new MusicNoteDecomponseItemData() {
                iconSprite = PgcUtils.LoadEmoteIcon("40200531", gameObject), //兔兔太空套装
            }, new MusicNoteDecomponseItemData() {
                iconSprite = currencySprite,
                count = 100
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
