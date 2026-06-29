using System.Collections;
using System.Collections.Generic;
using Basic.Utils;
using UI.Base;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class AirDecomposePanel : BasePanel<PonyDecomponsePanel>
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
        var currencyType1 = GameUtils.ConvertRewardType((int)BUDRewardType.RewardCrystal);
        var currencyType2 = GameUtils.ConvertRewardType((int)BUDRewardType.RewardCrystalShards);
        var currencySprite = PgcUtils.LoadCurrencyIcon(currencyType, gameObject);
        var currencySprite1 = PgcUtils.LoadCurrencyIcon(currencyType1, gameObject);
        var currencySprite2 = PgcUtils.LoadCurrencyIcon(currencyType2, gameObject);
        var dataList = new List<(MusicNoteDecomponseItemData, MusicNoteDecomponseItemData)>() {
            (new MusicNoteDecomponseItemData() {
                iconSprite = PgcUtils.LoadVehicleIcon("160200004", gameObject),
            }, new MusicNoteDecomponseItemData() {
                iconSprite = currencySprite1,
                count = 12
            }),
            (new MusicNoteDecomponseItemData() {
                iconSprite = PgcUtils.LoadAvatarIcon("10500013", gameObject), 
            }, new MusicNoteDecomponseItemData() {
                iconSprite = currencySprite2,
                count = 150
            }),
            (new MusicNoteDecomponseItemData() {
                iconSprite = PgcUtils.LoadAvatarIcon("10500019", gameObject), 
            }, new MusicNoteDecomponseItemData() {
                iconSprite = currencySprite2,
                count = 150
            }),
            (new MusicNoteDecomponseItemData() {
                iconSprite = PgcUtils.LoadAvatarIcon("10500080", gameObject), 
            }, new MusicNoteDecomponseItemData() {
                iconSprite = currencySprite2,
                count = 150
            }),
            (new MusicNoteDecomponseItemData() {
                iconSprite = UserUIWidgetManager.Inst.GetTitleBg(26, gameObject),//甜梦领航员称号
            }, new MusicNoteDecomponseItemData() {
                iconSprite = currencySprite2,
                count = 150
            }),
            (new MusicNoteDecomponseItemData() {
                iconSprite = PgcUtils.LoadEmoteIcon("40100321", gameObject), 
            }, new MusicNoteDecomponseItemData() {
                iconSprite = currencySprite2,
                count = 100
            }),
            (new MusicNoteDecomponseItemData() {
                iconSprite = PgcUtils.LoadEmoteIcon("40100323", gameObject), 
            }, new MusicNoteDecomponseItemData() {
                iconSprite = currencySprite2,
                count = 100
            }),
            (new MusicNoteDecomponseItemData() {
                iconSprite = UserUIWidgetManager.Inst.GetNicknameBg(5, gameObject),//心屿甜梦昵称框
            }, new MusicNoteDecomponseItemData() {
                iconSprite = currencySprite2,
                count = 100
            }),
            (new MusicNoteDecomponseItemData() {
                iconSprite = PgcUtils.LoadAvatarIcon("11400005", gameObject),
            }, new MusicNoteDecomponseItemData() {
                iconSprite = currencySprite2,
                count = 50
            }),
            (new MusicNoteDecomponseItemData() {
                iconSprite = PgcUtils.LoadEmoteIcon("40300281", gameObject), 
            }, new MusicNoteDecomponseItemData() {
                iconSprite = currencySprite2,
                count = 50
            }),
            (new MusicNoteDecomponseItemData() {
                iconSprite = PgcUtils.LoadEmoteIcon("40400455", gameObject), 
            }, new MusicNoteDecomponseItemData() {
                iconSprite = currencySprite2,
                count = 50
            }),
             (new MusicNoteDecomponseItemData() {
                iconSprite = PgcUtils.LoadEmoteIcon("40100320", gameObject), 
            }, new MusicNoteDecomponseItemData() {
                iconSprite = currencySprite,
                count = 150
            }),
            (new MusicNoteDecomponseItemData() {
                iconSprite = PgcUtils.LoadEmoteIcon("40100058", gameObject), 
            }, new MusicNoteDecomponseItemData() {
                iconSprite = currencySprite,
                count = 150
            }),
            (new MusicNoteDecomponseItemData() {
                iconSprite = PgcUtils.LoadEmoteIcon("40200328", gameObject), 
            }, new MusicNoteDecomponseItemData() {
                iconSprite = currencySprite,
                count = 150
            }),
            (new MusicNoteDecomponseItemData() {
                iconSprite = PgcUtils.LoadEmoteIcon("40200312", gameObject), 
            }, new MusicNoteDecomponseItemData() {
                iconSprite = currencySprite,
                count = 150
            }),
            (new MusicNoteDecomponseItemData() {
                iconSprite = PgcUtils.LoadEmoteIcon("40100045", gameObject), 
            }, new MusicNoteDecomponseItemData() {
                iconSprite = currencySprite,
                count = 150
            }),
            (new MusicNoteDecomponseItemData() {
                iconSprite = PgcUtils.LoadEmoteIcon("40100534", gameObject), 
            }, new MusicNoteDecomponseItemData() {
                iconSprite = currencySprite,
                count = 150
            }),
            (new MusicNoteDecomponseItemData() {
                iconSprite = PgcUtils.LoadBundleIcon("136", gameObject), 
            }, new MusicNoteDecomponseItemData() {
                iconSprite = currencySprite,
                count = 100
            }),
            (new MusicNoteDecomponseItemData() {
                iconSprite = PgcUtils.LoadBundleIcon("137", gameObject), 
            }, new MusicNoteDecomponseItemData() {
                iconSprite = currencySprite,
                count = 100
            }),
            (new MusicNoteDecomponseItemData() {
                iconSprite = PgcUtils.LoadBundleIcon("25", gameObject), 
            }, new MusicNoteDecomponseItemData() {
                iconSprite = currencySprite,
                count = 100
            }),
            (new MusicNoteDecomponseItemData() {
                iconSprite = PgcUtils.LoadAvatarIcon("11000181", gameObject),
            }, new MusicNoteDecomponseItemData() {
                iconSprite = currencySprite,
                count = 50
            }),
            (new MusicNoteDecomponseItemData() {
                iconSprite = PgcUtils.LoadAvatarIcon("11000178", gameObject),
            }, new MusicNoteDecomponseItemData() {
                iconSprite = currencySprite,
                count = 50
            }),
            (new MusicNoteDecomponseItemData() {
                iconSprite = PgcUtils.LoadAvatarIcon("10700015", gameObject),
            }, new MusicNoteDecomponseItemData() {
                iconSprite = currencySprite,
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
            if(i == 4 || i == 7)
            {
                GameObjectEx.FindChildByName(item, "icon1").GetComponent<Image>().SetNativeSize();
                GameObjectEx.FindChildByName(item, "icon1").GetComponent<Image>().transform.localScale *= 0.2f;
            }
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRootGo.GetComponent<RectTransform>());
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRootGo.GetComponent<RectTransform>());
    }


}
