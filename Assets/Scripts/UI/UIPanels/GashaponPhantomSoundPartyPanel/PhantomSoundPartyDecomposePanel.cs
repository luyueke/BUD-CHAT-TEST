using System.Collections;
using System.Collections.Generic;
using Basic.Utils;
using UI.Base;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class PhantomSoundPartyDecomposePanel : BasePanel<PonyDecomponsePanel>
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
        var currencyType1 = GameUtils.ConvertRewardType((int)BUDRewardType.RewardTypeZZZPhantomCrystal);
        var currencyType2 = GameUtils.ConvertRewardType((int)BUDRewardType.RewardTypeZZZPhantomCrystalShards);
        var currencySprite1 = PgcUtils.LoadCurrencyIcon(currencyType1, gameObject);
        var currencySprite2 = PgcUtils.LoadCurrencyIcon(currencyType2, gameObject);
        var currencySpriteBadge = PgcUtils.LoadCurrencyIcon(CurrencyType.Badge, gameObject);
        var dataList = new List<(MusicNoteDecomponseItemData, MusicNoteDecomponseItemData)>() {
            // 载具自选盒 → 12 绒天使水晶
            (new MusicNoteDecomponseItemData() {
                 iconSprite = PgcUtils.LoadRewardIcon(BUDRewardType.RewardPgcOptionalBox, gameObject),
            }, new MusicNoteDecomponseItemData() {
                iconSprite = currencySprite1,
                count = 12
            }),
            // 星愿天使 10500085 → 150 绒天使碎片
            (new MusicNoteDecomponseItemData() {
                iconSprite = PgcUtils.LoadAvatarIcon("10500085", gameObject),
            }, new MusicNoteDecomponseItemData() {
                iconSprite = currencySprite2,
                count = 150
            }),
            // 绒天使称号 190000033 → 150 绒天使碎片
            (new MusicNoteDecomponseItemData() {
                iconSprite = UserUIWidgetManager.Inst?.GetTitleBg(33, gameObject),
            }, new MusicNoteDecomponseItemData() {
                iconSprite = currencySprite2,
                count = 150
            }),
            // 绒天使昵称框 180100010 → 100 绒天使碎片
            (new MusicNoteDecomponseItemData() {
                iconSprite = UserUIWidgetManager.Inst?.GetNicknameBg(10, gameObject),
            }, new MusicNoteDecomponseItemData() {
                iconSprite = currencySprite2,
                count = 100
            }),
            // 心动发条 40200568 → 100 绒天使碎片
            (new MusicNoteDecomponseItemData() {
                iconSprite = PgcUtils.LoadEmoteIcon("40200568", gameObject),
            }, new MusicNoteDecomponseItemData() {
                iconSprite = currencySprite2,
                count = 100
            }),
            // 绒绒天使 40100578 → 50 绒天使碎片
            (new MusicNoteDecomponseItemData() {
                iconSprite = PgcUtils.LoadEmoteIcon("40100578", gameObject),
            }, new MusicNoteDecomponseItemData() {
                iconSprite = currencySprite2,
                count = 50
            }),
            // 绒天使背包 11400092 → 50 绒天使碎片
            (new MusicNoteDecomponseItemData() {
                iconSprite = PgcUtils.LoadAvatarIcon("11400092", gameObject),
            }, new MusicNoteDecomponseItemData() {
                iconSprite = currencySprite2,
                count = 50
            }),
            // 困困小兔 40200566 → 150 徽章
            (new MusicNoteDecomponseItemData() {
                iconSprite = PgcUtils.LoadEmoteIcon("40200566", gameObject),
            }, new MusicNoteDecomponseItemData() {
                iconSprite = currencySpriteBadge,
                count = 150
            }),
            // 幸运小兔 40200565 → 150 徽章
            (new MusicNoteDecomponseItemData() {
                iconSprite = PgcUtils.LoadEmoteIcon("40200565", gameObject),
            }, new MusicNoteDecomponseItemData() {
                iconSprite = currencySpriteBadge,
                count = 150
            }),
            // 泡泡小兔 40200567 → 150 徽章
            (new MusicNoteDecomponseItemData() {
                iconSprite = PgcUtils.LoadEmoteIcon("40200567", gameObject),
            }, new MusicNoteDecomponseItemData() {
                iconSprite = currencySpriteBadge,
                count = 150
            }),
            // 天使小兔 40200562 → 100 徽章
            (new MusicNoteDecomponseItemData() {
                iconSprite = PgcUtils.LoadEmoteIcon("40200562", gameObject),
            }, new MusicNoteDecomponseItemData() {
                iconSprite = currencySpriteBadge,
                count = 100
            }),
            // 女仆小兔 40200563 → 100 徽章
            (new MusicNoteDecomponseItemData() {
                iconSprite = PgcUtils.LoadEmoteIcon("40200563", gameObject),
            }, new MusicNoteDecomponseItemData() {
                iconSprite = currencySpriteBadge,
                count = 100
            }),
            // JK小兔 40200560 → 50 徽章
            (new MusicNoteDecomponseItemData() {
                iconSprite = PgcUtils.LoadEmoteIcon("40200560", gameObject),
            }, new MusicNoteDecomponseItemData() {
                iconSprite = currencySpriteBadge,
                count = 50
            }),
            // 辣妹小兔 40200561 → 50 徽章
            (new MusicNoteDecomponseItemData() {
                iconSprite = PgcUtils.LoadEmoteIcon("40200561", gameObject),
            }, new MusicNoteDecomponseItemData() {
                iconSprite = currencySpriteBadge,
                count = 50
            }),
        };

        for(int i = 0; i < dataList.Count; i++)
        {
            var item = Instantiate(contentItemGo, contentRootGo.transform);
            item.SetActive(true);
            GameObjectEx.FindChildByName(item, "icon1").GetComponent<Image>().sprite = dataList[i].Item1.iconSprite;
            GameObjectEx.FindChildByName(item, "icon2").GetComponent<Image>().sprite = dataList[i].Item2.iconSprite;
            GameObjectEx.FindChildByName(item, "txt").GetComponent<Text>().text = dataList[i].Item2.count.ToString();
            if(i == 2)
            {
                GameObjectEx.FindChildByName(item, "icon1").GetComponent<Image>().SetNativeSize();
                GameObjectEx.FindChildByName(item, "icon1").GetComponent<Image>().transform.localScale *= 0.3f;
            }
             if(i == 3)
            {
                GameObjectEx.FindChildByName(item, "icon1").GetComponent<Image>().SetNativeSize();
                GameObjectEx.FindChildByName(item, "icon1").GetComponent<Image>().transform.localScale *= 0.2f;
            }
            if(i == 5 || i == 6)
            {
                GameObjectEx.FindChildByName(item, "icon1").GetComponent<Image>().SetNativeSize();
                GameObjectEx.FindChildByName(item, "icon1").GetComponent<Image>().transform.localScale *= 0.2f;
            }
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRootGo.GetComponent<RectTransform>());
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRootGo.GetComponent<RectTransform>());
    }


}
