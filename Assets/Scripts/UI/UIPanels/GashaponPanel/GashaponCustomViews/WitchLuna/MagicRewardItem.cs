using System;
using System.Collections.Generic;
using Basic.Utils;
using Game.Store;
using GameData.PgcData;
using UI.BaseWidgets;
using UI.Manager;
using UI.UIPanels.GashaponPanel;
using UnityEngine;
using UnityEngine.UI;

public class MagicRewardItem : MonoBehaviour {
    private GameObject previewObj;
    private Image iconImage;
    private Image bgImage;
    private Image currencyIcon;
    private Text countText;
    private GameObject downMarkObj;
    private GashaponRewardData rewardData;
    private Action previewCallBack = null;
    private Text nameText;



    private void Awake() {
        previewObj = transform.Find("PreviewBtn").gameObject;
        iconImage = transform.Find("RewardIcon").GetComponent<Image>();
        currencyIcon = transform.Find("CurrencyIcon").GetComponent<Image>();
        countText = transform.Find("RewardNum").GetComponent<Text>();
        downMarkObj = transform.Find("DoneBg").gameObject;
        previewObj = transform.Find("PreviewBtn").gameObject;
        nameText = GameObjectEx.FindComponentByName<Text>(transform,"RewardName");
        bgImage = transform.GetComponent<Image>();
        transform.GetComponent<CButton>().onClick.AddListener(OnPreviewClick);
    }

    public void SetData(GashaponRewardData data, Action callBack) {
        rewardData = data;
        InitSprite();
        countText.text = data.Num > 1 ? "x" + data.Num : "";
        countText.gameObject.SetActive(true);
        downMarkObj.SetActive(GashaponUtils.IsOwnedReward(data));
        previewCallBack = callBack;
        if (nameText != null) {
            if (!string.IsNullOrEmpty(rewardData.BundleId)) {
                nameText.SetLocalText(PgcUtils.GetBundleName(rewardData.BundleId));
                nameText.gameObject.SetActive(true);
            }
        }
    }

    public GashaponRewardData GetRewardData() {
        return rewardData;
    }

    private void OnPreviewClick() {
        previewCallBack?.Invoke();
    }


    private void InitSprite() {
        iconImage.gameObject.SetActive(false);
        currencyIcon.gameObject.SetActive(false);
        previewObj.gameObject.SetActive(false);
        // PGCBundle
        if (!string.IsNullOrEmpty(rewardData.BundleId)) {
            iconImage.gameObject.SetActive(true);
            previewObj.gameObject.SetActive(true);
            iconImage.sprite = PgcUtils.LoadBundleIcon(rewardData.BundleId, gameObject);
            if (iconImage.sprite == null) {
                LoggerUtils.LogError("rewardData.BundleId:" + rewardData.BundleId);
            }
            return;
        }


        //货币类
        var currencyType = GameUtils.ConvertRewardType((int)rewardData.RewardType);
        if ((rewardData.PgcDatas == null || rewardData.PgcDatas.Count == 0) && GameUtils.IsCurrencyType((int)currencyType)) {
            bgImage.color = DataUtil.DeSerializeColorByHex("#92BEFF");
            PgcUtils.LoadCurrencyIconAsync(currencyType, gameObject, (iconSprite) => {
                if (this != null && currencyIcon != null && iconSprite != null) {
                    currencyIcon.gameObject.SetActive(true);
                    currencyIcon.sprite = iconSprite;
                }
            });
        } else if (rewardData.PgcDatas[0] != null && rewardData.PgcDatas[0].ResourceType == ResourceType.Avatar) {
            //皮肤

            PgcUtils.LoadAvatarIconAsync(rewardData.Id, gameObject, (iconSprite) => {
                if (this != null && iconImage != null && iconSprite != null) {
                    iconImage.gameObject.SetActive(true);
                    previewObj.gameObject.SetActive(true);
                    iconImage.sprite = iconSprite;
                }
            });
        } else if (rewardData.PgcDatas[0] != null && rewardData.PgcDatas[0].ResourceType == ResourceType.PGCPetAvatar) {
            //Pet皮肤

            PgcUtils.LoadPetAvatarIconAsync(rewardData.Id, gameObject, (iconSprite) => {
                if (this != null && iconImage != null && iconSprite != null) {
                    iconImage.gameObject.SetActive(true);
                    previewObj.gameObject.SetActive(true);
                    iconImage.sprite = iconSprite;
                }
            });
        } else if (rewardData.PgcDatas[0] != null && rewardData.PgcDatas[0].ResourceType == ResourceType.Emote) {
            //表情
            PgcUtils.LoadEmoteIconAsync(rewardData.Id, gameObject, (iconSprite) => {
                if (this != null && iconImage != null && iconSprite != null) {
                    iconImage.gameObject.SetActive(true);
                    previewObj.gameObject.SetActive(true);
                    iconImage.sprite = iconSprite;
                }
            });
        }
    }

    public void SetDrawnStatus(bool isDrawn) {
        downMarkObj.SetActive(isDrawn);
    }
}
