using System.Collections.Generic;
using Com.TheFallenGames.OSA.Util.IO;
using Game.Store;
using GameData.PgcData;
using Message;
using Newtonsoft.Json;
using Product;
using UI.Base;
using UI.Manager;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class SendGiftFriendPanel : BasePanel<SendGiftFriendPanel>
{
    public Button backButton;
    public Text assetName;
    public Image assetsIcon;
    public Text productPrice;
    public Button searchInputButton;
    public Text searchText;
    public Button searchButton;
    public Button closeSearchButton;
    public GameObject friendBg;
    public GameObject followBg;
    public Button friendButton;
    public Button followButton;
    public Text emptyTxt;
    public GameObject loadingView;
    public Sprite budSprite;
    public RemoteImageBehaviour remoteAssetsIcon;
    public Image priceIcon;

    public RemoteImageBehaviour RemoteImage => remoteAssetsIcon;

    public SendGiftFriendListEntry SendGiftFriendListEntry;

    private GoodsData mData;
    private int giftType = 0;
    private string url;
    private string searchContent = "";
    private int relationShipType;

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        if (args.Length >= 1)
        {
            mData = (GoodsData)args[0];
            giftType = (int)args[1];
        }

        SetupUI();
    }


    public override void OnCreate()
    {
        base.OnCreate();
        relationShipType = (int)RelationShipType.Friend;
        SendGiftFriendListEntry.AddClickListener(OnScrollViewClick);

        backButton.onClick.AddListener(() => { CloseSelf(); });

        friendButton.onClick.AddListener(() =>
        {
            friendBg.gameObject.SetActive(true);
            followBg.gameObject.SetActive(false);
            relationShipType = (int)RelationShipType.Friend;
            loadingView.gameObject.SetActive(true);
            SendGiftFriendListEntry.GetFirstPageFriendDatas((int)RelationShipType.Friend, OnHasFriends);
        });

        followButton.onClick.AddListener(() =>
        {
            friendBg.gameObject.SetActive(false);
            followBg.gameObject.SetActive(true);
            relationShipType = (int)RelationShipType.Follow;
            loadingView.gameObject.SetActive(true);
            SendGiftFriendListEntry.GetFirstPageFriendDatas((int)RelationShipType.Follow, OnHasFriends);
        });

        searchInputButton.onClick.AddListener(() => { OnSearchBtnClick(); });

        searchButton.onClick.AddListener(() => { Search(); });
        closeSearchButton.onClick.AddListener(() =>
        {
            this.searchText.color = DataUtil.DeSerializeColorCheckHash("#9E9E9E");
            closeSearchButton.gameObject.SetActive(false);
            searchText.SetLocalText("请输入玩家ID或昵称");
            loadingView.gameObject.SetActive(true);
            SendGiftFriendListEntry.GetFirstPageFriendDatas(relationShipType, OnHasFriends);
        });

        MessageHelper.AddListener(MessageName.SendGiftSuccess, SendGiftSuccess);
    }

    private void SendGiftSuccess()
    {
        CloseSelf();
    }

    private void OnHasFriends(bool hasFriends)
    {
        loadingView.gameObject.SetActive(false);
        emptyTxt.text = relationShipType == (int)RelationShipType.Friend ? "你还没有好友" : "你还没有关注的人";
        emptyTxt.gameObject.SetActive(!hasFriends);
    }

    private void OnScrollViewClick(PointerEventData data)
    {
    }

    private void SetupUI()
    {
        assetName.text = mData.Name;
        switch (mData.GoodsType)
        {
            case GoodsType.SinglePgc:
            case GoodsType.SingleUgc:
                SingleItemUpdate(mData.Assets);
                break;
            case GoodsType.BundlePgc:
                BundlePgcItemUpdate(mData.Id);
                break;
            case GoodsType.BundleUgc:
                BundleItemUpdate();
                break;
            case GoodsType.ToolProduct:
                ToolsItemUpdate();
                break;
        }

        SendGiftFriendListEntry.SetGoodsData(mData, giftType);
        loadingView.gameObject.SetActive(true);
        SendGiftFriendListEntry.GetFirstPageFriendDatas((int)RelationShipType.Friend, OnHasFriends);

        if (mData.GiftType == GiftType.PremiumSeasonPass || mData.GiftType == GiftType.DeluxeSeasonPass ||
            mData.GiftType == GiftType.AdvancedSeasonPassTier || mData.GiftType == GiftType.MonthlyVip)
        {
            priceIcon.gameObject.SetActive(false);
            productPrice.text = "\u00a5" + mData.Price.Value;

            return;
        }

        productPrice.text = mData.IsOwned ? mData.OriginalPrice.Value.ToString():mData.Price.Value.ToString();
        priceIcon.gameObject.SetActive(true);
        priceIcon.sprite =
            PgcUtils.LoadCurrencyIcon((CurrencyType)mData.Price.CurrencyType, gameObject);
    }

    private void BundlePgcItemUpdate(string bundleId)
    {
        url = null;
        remoteAssetsIcon.gameObject.SetActive(false);
        assetsIcon.gameObject.SetActive(true);
        var sprite = PgcUtils.LoadBundleIcon(bundleId, gameObject);
        if (sprite != null) assetsIcon.sprite = sprite;
        else assetsIcon.sprite = budSprite;
    }
    
    private void BundleItemUpdate()
    {
        remoteAssetsIcon.gameObject.SetActive(false);
        assetsIcon.gameObject.SetActive(true);
        assetsIcon.sprite = budSprite;
        if (mData.UgcBundleInfo != null && mData.UgcBundleInfo.skinInfo != null)
        {
            RefreshCover(mData.UgcBundleInfo.skinInfo.cover);
        }
    }

    private void ToolsItemUpdate()
    {
        var spriteName = "icon_premium_pass";
        if (mData.Id.Contains("giftgaojipass"))
        {
            spriteName = "icon_premium_pass";
        }
        else if (mData.Id.Contains("gifthaohuapass"))
        {
            spriteName = "icon_deluxe_pass";
        }
        else if (mData.Id.Contains("gifthaohuauppass"))
        {
            spriteName = "icon_deluxe_pass_upgrade";
        }
        else if (mData.Id.Contains("giftvip"))
        {
            spriteName = "icon_vip_month";
        } else if (mData.Id.Contains("newYearLimitedPack1"))
        {
            spriteName = "icon_newYearLimitedPack1";
        } else if (mData.Id.Contains("newYearLimitedPack2"))
        {
            spriteName = "icon_newYearLimitedPack2";
        }
        else if (mData.Id.Contains("LaborDay"))
        {
            spriteName = "newLaborPack2";
        }else if (mData.Id.Contains("newYearLimitedDancingPack"))
        {
            spriteName = "icon_newYearLimitedDancingPack";
        }
        

        assetsIcon.gameObject.SetActive(true);
        var spriteatlasPath = "Assets/Loadable/UI/UIPanel/FittingRoomPanel/FittingRoomPanel.spriteatlas";
        var sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, spriteName, gameObject);
        if (sprite != null) assetsIcon.sprite = sprite;
        else assetsIcon.sprite = budSprite;
    }

    private void SingleItemUpdate(List<AssetsData> assetsData)
    {
        if (assetsData == null) return;
        if (assetsData.Count != 1) return;
        var assets = assetsData[0];
        // 资源类型
        switch (assets.ResourceType)
        {
            case ResourceType.Avatar:
                PgcItem(assets);
                break;
            case ResourceType.UgcAvatar:
                UgcItem((UGCAssetsData)assets);
                break;
            case ResourceType.Emote:
                EmoteItem((EmoteAssetsData)assets);
                break;
            case ResourceType.MusicScore:
                MusicScoreItem((MusicScoreAssetsData)assets);
                break;
            case ResourceType.PGCPetAvatar:
                PetPgcItem(assets);
                break;
            case ResourceType.UGCPetAvatar:
                PetUgcItem((UGCAssetsData)assets);
                break;
            case ResourceType.UgcPose:
                PetUgcPoseItem((UgcPoseAssetsData)assets);
                break;
            case ResourceType.UgcEmote:
                PetUgcAnimItem((UgcAnimAssetsData)assets);
                break;
            case ResourceType.UgcVehicle:
                VehicleItem((UgcVehicleAssetsData)assets);
                break;
        }
    }

    private void PgcItem(AssetsData assetsData)
    {
        url = null;
        remoteAssetsIcon.gameObject.SetActive(false);
        assetsIcon.gameObject.SetActive(true);
        var sprite = PgcUtils.GetIconSpriteByPgcId(assetsData.Id, gameObject);
        if (sprite != null) assetsIcon.sprite = sprite;
        else assetsIcon.sprite = budSprite;
    }

    private void PetPgcItem(AssetsData assetsData)
    {
        url = null;
        remoteAssetsIcon.gameObject.SetActive(false);
        assetsIcon.gameObject.SetActive(true);
        var sprite = PgcUtils.GetIconSpriteByPgcId(assetsData.Id, gameObject);

        if (sprite != null) assetsIcon.sprite = sprite;
        else assetsIcon.sprite = budSprite;
    }

    private void EmoteIdleItem()
    {
        url = null;
        remoteAssetsIcon.gameObject.SetActive(false);
        assetsIcon.gameObject.SetActive(true);
        var atlas = XAssetLoaderMgr.Inst.GetSpriteAltasPath(SpriteAtlasType.PgcEmoteSprite);
        string spriteName = mData.Id;
        if (mData.ProductId != 0)
        {
            spriteName = mData.Id + mData.ProductId;
        }

        var sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlas, spriteName, gameObject);
        if (sprite != null) assetsIcon.sprite = sprite;
        else assetsIcon.sprite = budSprite;
    }


    private void EmoteItem(EmoteAssetsData assetsData)
    {
        url = null;
        remoteAssetsIcon.gameObject.SetActive(false);
        assetsIcon.gameObject.SetActive(true);
        var sprite = PgcUtils.GetIconSpriteByPgcId(assetsData.Id, gameObject);
        if (sprite != null) assetsIcon.sprite = sprite;
        else assetsIcon.sprite = budSprite;
    }

    private void UgcItem(UGCAssetsData assetsData)
    {
        remoteAssetsIcon.gameObject.SetActive(false);
        assetsIcon.gameObject.SetActive(true);
        assetsIcon.sprite = budSprite;
        if (assetsData.UgcInfo != null)
        {
            RefreshCover(assetsData.UgcInfo.UgcInfo.cover);
        }
    }

    private void PetUgcItem(UGCAssetsData assetsData)
    {
        remoteAssetsIcon.gameObject.SetActive(false);
        assetsIcon.gameObject.SetActive(true);
        assetsIcon.sprite = budSprite;
        if (assetsData.UgcInfo != null)
        {
            RefreshCover(assetsData.UgcInfo.UgcInfo.cover);
        }
    }


    private void PetUgcPoseItem(UgcPoseAssetsData assetsData)
    {
        remoteAssetsIcon.gameObject.SetActive(false);
        assetsIcon.gameObject.SetActive(true);
        assetsIcon.sprite = budSprite;
        if (assetsData.UgcInfo != null)
        {
            RefreshCover(assetsData.UgcInfo.UgcInfo.cover);
        }
    }

    private void PetUgcAnimItem(UgcAnimAssetsData assetsData)
    {
        remoteAssetsIcon.gameObject.SetActive(false);
        assetsIcon.gameObject.SetActive(true);
        assetsIcon.sprite = budSprite;
        if (assetsData.UgcInfo != null)
        {
            RefreshCover(assetsData.UgcInfo.UgcInfo.cover);
            if (assetName != null)
            {
                assetName.text = assetsData.UgcInfo.UgcInfo.name;
            }
        }
    }
    private void VehicleItem(UgcVehicleAssetsData assetsData)
    {
        remoteAssetsIcon.gameObject.SetActive(false);
        assetsIcon.gameObject.SetActive(true);
        assetsIcon.sprite = budSprite;
        if (assetsData.UgcInfo != null)
        {
            RefreshCover(assetsData.UgcInfo.vehicleInfo.cover);

            if (assetName != null)
            {
                assetName.text = assetsData.UgcInfo.vehicleInfo.name;
            }
        }

    }
    private void MusicScoreItem(MusicScoreAssetsData assetsData)
    {
        remoteAssetsIcon.gameObject.SetActive(false);
        assetsIcon.gameObject.SetActive(true);
        assetsIcon.sprite = budSprite;
        if (assetsData.UgcInfo != null)
        {
            RefreshCover(assetsData.UgcInfo.musicScoreInfo.cover);
        }
    }

    private void RefreshCover(string cover)
    {
        url = cover;
        remoteAssetsIcon.Load(cover, onCompleted: (bool fromCache, bool success) =>
        {
            if (this == null || url != cover) return;
            assetsIcon.gameObject.SetActive(false);
            remoteAssetsIcon.gameObject.SetActive(true);
        });
    }

    void OnSearchBtnClick()
    {
        KeyBoardInfo keyBoardInfo = new KeyBoardInfo
        {
            type = 0,
            placeHolder = LocalizationManager.Inst.GetLocalizedText("搜索"),
            inputMode = 2,
            maxLength = 60,
            inputFlag = 0,
            textSecurity = 1,
            lengthTips = LocalizationManager.Inst.GetLocalizedText("字数超出限制"),
            defaultText = "",
            returnKeyType = (int)ReturnType.Done
        };
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, KeyboardReturn);
        MobileInterface.Instance.ShowKeyboard(JsonConvert.SerializeObject(keyBoardInfo));
    }

    void KeyboardReturn(string str)
    {
        MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);
        if (string.IsNullOrEmpty(str))
        {
            return;
        }

        this.searchContent = str;
        this.searchText.text = str;
        this.searchText.color = DataUtil.DeSerializeColorCheckHash("#000000");
        Search();
    }

    private void Search()
    {
        if (string.IsNullOrEmpty(this.searchContent))
        {
            return;
        }

        closeSearchButton.gameObject.SetActive(true);

        SendGiftFriendListEntry.SearchGiftFriendDatas(relationShipType, this.searchContent, OnHasFriends);
    }
}