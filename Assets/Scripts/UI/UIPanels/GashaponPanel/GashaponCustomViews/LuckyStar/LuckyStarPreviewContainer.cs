using System;
using System.Collections.Generic;
using Basic.Utils;
using Game.Store;
using GameData.PgcData;
using Product;
using UI.Manager;
using UI.UIPanels.GashaponPanel;
using UnityEngine;
using UnityEngine.UI;

public class LuckyStarPreviewContainer : MonoBehaviour {
    [SerializeField] private Sprite pgcBgSprite;
    [SerializeField] private Sprite currencySprite;
    [SerializeField] private GameObject rewardItemPrefab;

    private void Awake() {
        GameObjectEx.FindComponentByName<Button>(transform, "Mask/Bg/CloseBtn").onClick.AddListener(OnCloseClicked);
    }

    private void OnCloseClicked() {
        Destroy(gameObject);
    }

    public void SetData(GashaponData data) {
        rewardItemPrefab.SetActive(false);
        if (data == null) {
            return;
        }


        var pgcIds = new List<string>() { "11400069", "11000210", "10500026", "10500027" };
        var rewardList = new List<GashaponRewardData>();
        foreach (var rewardData in data.RewardList) {
            if (rewardData.RewardType != RewardType.RewardPgcResource) {
                continue;
            }

            if (pgcIds.Contains(rewardData.Id)) {
                rewardList.Add(rewardData);
            }
        }

        rewardList.Add(new GashaponRewardData() {
            RewardType = RewardType.RewardPurpleDreamCoin,
            Num = 60,
        });


        foreach (var rewardData in rewardList) {
            var tmpObj = Instantiate(rewardItemPrefab, rewardItemPrefab.transform.parent);
            var tmpItem = new LuckyStarPreviewItem(tmpObj);
            tmpItem.BindData(rewardData,
                rewardData.RewardType == RewardType.RewardPgcResource ? pgcBgSprite : currencySprite);
            tmpObj.gameObject.SetActive(true);
        }

        rewardItemPrefab.SetActive(false);
    }
}


public class LuckyStarPreviewItem {
    private GameObject bindObj;
    private Image bgImage;
    private Text numText;
    private Image iconImage;

    public LuckyStarPreviewItem(GameObject obj) {
        bindObj = obj;
        iconImage = GameObjectEx.FindComponentByName<Image>(obj, "Icon");
        bgImage = GameObjectEx.FindComponentByName<Image>(obj, "BG");
        numText = GameObjectEx.FindComponentByName<Text>(obj, "NumText");
    }

    public void BindData(GashaponRewardData data, Sprite bgSprite) {
        if (data.Num > 0) {
            numText.text = $"x{data.Num}";
        }

        bgImage.sprite = bgSprite;
        //货币类
        var currencyType = GameUtils.ConvertRewardType((int)data.RewardType);
        if (data.PgcDatas == null && currencyType != CurrencyType.None) {
            PgcUtils.LoadCurrencyIconAsync(currencyType, bindObj, (iconSprite) => {
                if (bindObj != null && iconSprite != null) {
                    iconImage.sprite = iconSprite;
                }
            });
        } else {
            PgcUtils.GetIconSpriteByPgcIdAsync(data.Id, bindObj, sp => {
                if (bindObj != null && sp != null) {
                    iconImage.sprite = sp;
                }
            });
        }
    }
}
