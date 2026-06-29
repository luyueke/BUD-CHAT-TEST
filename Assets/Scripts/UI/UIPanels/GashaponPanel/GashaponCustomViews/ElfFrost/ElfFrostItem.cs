using System;
using Basic.Utils;
using Es;
using Game.BagSystem;
using Game.Database;
using Game.Store;
using GameData.PgcData;
using Product;
using UI.BaseWidgets;
using UI.Manager;
using UI.UIPanels.GashaponPanel;
using UnityEngine;
using UnityEngine.UI;

public class ElfFrostItem : MonoBehaviour {
    [SerializeField] private Image iconImage;

    [SerializeField] private Image bgImage;

    [SerializeField] private GameObject specialMarkObj;

    [SerializeField] private Sprite defaultSprite;

    [SerializeField] private Sprite specialSprite;

    [SerializeField] private GameObject ownedMarkObj;
    [SerializeField] private GameObject specialOwnedMarkObj;

    private GashaponRewardData rewardInfo;
    private Action<string> onClickCallBack;


    private void Awake() {
        GetComponent<CButton>().onClick.AddListener(OnItemClicked);
    }

    public string GetRewardId() {
        return rewardInfo.Id;
    }


    public void Init(GashaponRewardData info, Action<string> callBack = null) {
        rewardInfo = info;

        InitSprite(info);
        //string atlasPath = "Assets/Loadable/UI/UIPanel/ActivityCenterPanel/ActivityCenterPanel.spriteatlas";
        //var cfg = PgcUtils.GetPgcConfigData(rewardInfo.Id);
        //if (cfg != null) {
        //    var spriteName = PgcUtils.GetIconSpriteNameByPgcId(rewardInfo.Id);
        //    iconImage.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlasPath, spriteName, gameObject);
        //} else {
        //    LoggerUtils.LogError("获取不到对应的 PGC 物品:" + rewardInfo.Id);
        //}

        var skinConfig = DataTables.GetSpecialSkinConfig(rewardInfo.Id);
        if (skinConfig != null) {

            bgImage.sprite = specialSprite;
            specialMarkObj.SetActive(true);
        } else {
            bgImage.sprite = defaultSprite;
            specialMarkObj.SetActive(false);
        }

        bool isOwned = AssetsDataManager.IsOwned(rewardInfo.Id);
      //  Debug.LogError("ElffrostItem init isowned=" + isOwned+",skinconfig="+(skinConfig == null));
        ownedMarkObj.SetActive(isOwned && (skinConfig == null));
        specialOwnedMarkObj.SetActive(isOwned && (skinConfig != null));
        onClickCallBack = callBack;
    }

    public void InitSprite(GashaponRewardData data)
    {
        //pgcIcon.gameObject.SetActive(false);
        //currencyIcon.gameObject.SetActive(false);
        //货币类
        var currencyType = GameUtils.ConvertRewardType((int)data.RewardType);
        if (data.PgcDatas == null && currencyType != CurrencyType.None)
        {
            PgcUtils.LoadCurrencyIconAsync(currencyType, gameObject, (iconSprite) =>
            {
                if (this != null && iconImage != null && iconSprite != null)
                {
                    //currencyIcon.gameObject.SetActive(true);
                    iconImage.sprite = iconSprite;
                }
            });
        }
        else if (GashaponUtils.HasPGCData(data) && data.PgcDatas[0]?.ResourceType == ResourceType.Avatar)
        {//皮肤
            if (!string.IsNullOrEmpty(data.BundleId))
            {
                PgcUtils.LoadBundleIconAsync(data.BundleId, gameObject, (iconSprite) =>
                {
                    if (this != null && iconImage != null && iconSprite != null)
                    {
                        //pgcIcon.gameObject.SetActive(true);
                        iconImage.sprite = iconSprite;
                    }
                });
            }
            else
            {
                PgcUtils.LoadAvatarIconAsync(data.Id, gameObject, (iconSprite) =>
                {
                    if (this != null && iconImage != null && iconSprite != null)
                    {
                        //pgcIcon.gameObject.SetActive(true);
                        iconImage.sprite = iconSprite;
                    }
                });
            }
        }
        else if (GashaponUtils.HasPGCData(data) && data.PgcDatas[0]?.ResourceType == ResourceType.PGCPetAvatar)
        {//Pet皮肤
            PgcUtils.LoadPetAvatarIconAsync(data.Id, gameObject, (iconSprite) =>
            {
                if (this != null && iconImage != null && iconSprite != null)
                {
                    //pgcIcon.gameObject.SetActive(true);
                    iconImage.sprite = iconSprite;
                }
            });
        }
        else if (GashaponUtils.HasPGCData(data) && data.PgcDatas[0]?.ResourceType == ResourceType.Emote)
        { //表情
            PgcUtils.LoadEmoteIconAsync(data.Id, gameObject, (iconSprite) =>
            {
                if (this != null && iconImage != null && iconSprite != null)
                {
                    //pgcIcon.gameObject.SetActive(true);
                    iconImage.sprite = iconSprite;
                }
            });
        }
        else if (data.RewardType == RewardType.RewardAvatarFrame)
        { //头像框
            UserUIWidgetManager.Inst.GetHeadCycleImgByPgcIdAsync(data.Id, gameObject, (iconSprite) =>
            {
                if (this != null && iconImage != null && iconSprite != null)
                {
                    //pgcIcon.gameObject.SetActive(true);
                    iconImage.sprite = iconSprite;
                }
            });
        }
        else if (data.RewardType == RewardType.RewardChatBubbles)
        { //聊天气泡
            UserUIWidgetManager.Inst.GetChatBubbleIconByPgcIdAsync(data.Id, gameObject, (iconSprite) =>
            {
                if (this != null && iconImage != null && iconSprite != null)
                {
                    //pgcIcon.gameObject.SetActive(true);
                    iconImage.sprite = iconSprite;
                }
            });
        }
        else if ((int)data.RewardType == (int)BUDRewardType.RewardUgcTemplateResource)
        { //宠物模版
            PgcUtils.LoadPetUGCTemplateAsync(data.Id, gameObject, (iconSprite) =>
            {
                if (this != null && iconImage != null && iconSprite != null)
                {
                    //pgcIcon.gameObject.SetActive(true);
                    iconImage.sprite = iconSprite;
                }
            });
        }
        else if ((int)data.RewardType == (int)BUDRewardType.RewardHomepageSkin)
        {   //主页皮肤
            var sprite = ProfileThemeManager.Inst.LoadThemeIcon(data.Id, gameObject);
            if (sprite != null)
            {
                //pgcIcon.gameObject.SetActive(true);
                iconImage.sprite = sprite;
            }
        }
        else
        {
            var sprite1 = PgcUtils.LoadRewardIcon((BUDRewardType)data.RewardType, gameObject);
            if (sprite1 != null)
            {
                //pgcIcon.gameObject.SetActive(true);
                iconImage.sprite = sprite1;
            }
        }
    }



    public void RefreshOwnedMark() {
        bool isOwned = AssetsDataManager.IsOwned(rewardInfo.Id);
        var skinConfig = DataTables.GetSpecialSkinConfig(rewardInfo.Id);
        ownedMarkObj.SetActive(isOwned && (skinConfig == null));
        specialOwnedMarkObj.SetActive(isOwned && (skinConfig != null));
    }

    private void OnItemClicked() {
        onClickCallBack?.Invoke(rewardInfo.Id);
    }

}
