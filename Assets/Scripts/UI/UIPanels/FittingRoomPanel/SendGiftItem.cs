using Basic.Utils;
using Com.TheFallenGames.OSA.Util.IO;
using Game.Store;
using GameData.PgcData;
using System;
using System.Collections.Generic;
using ChocDino.UIFX;
using GameData.Base;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;
using GameData.BaseInfo;
using Product;

namespace UI.UIPanels.FittingRoom
{
    public class SendGiftItem : MonoBehaviour
    {
        [Header("颜色")]
        [SerializeField] Image background;
        [SerializeField] Image colorBackground;

        [SerializeField] GameObject assetRoot;
        [SerializeField] GameObject takeOffRoot;
        [SerializeField] GameObject designRoot;
        [SerializeField] GameObject banRoot;
        [Header("节点")]
        [SerializeField] Image selectedImage;
        [SerializeField] GameObject infoRoot;
        [SerializeField] Image iconImage;
        [SerializeField] Text text;
        [SerializeField] Image assetsIcon;
        [SerializeField] RemoteImageBehaviour remoteAssetsIcon;
        [SerializeField] BlurFilter iconBlurFilter;
        [SerializeField] Sprite budSprite;
        [SerializeField] Color selectedColor;
        [SerializeField] Font numFont;
        [SerializeField] Font textFont;
        [SerializeField] GameObject loadingRoot;
        [SerializeField] GameObject extInfoRoot;
        [SerializeField] Text extInfoText;
        [SerializeField] Text designText;
        [SerializeField] Text assetName;
        [SerializeField] Text toolsName;
        [SerializeField] GameObject discountTag;
        [SerializeField] Image iconLight;

        [Header("下架")]
        [SerializeField] Button banRewardBtn;
        [SerializeField] Text banRewardText;

        [Header("红点")]
        [SerializeField] GameObject redDotRoot;

        private Action<GoodsData> onItemSelected;
        private GoodsData mData;
        private string url;

        /// <summary>
        /// 确定是否是试衣间Panel显示，需要判断Item来源，需要单独处理
        /// </summary>
        private bool isFittingRoomPanel;


        [Header("限时物品")]
        [SerializeField] GameObject timeLimitRoot;
        [SerializeField] Text timeLimitText;

        public RemoteImageBehaviour RemoteImage => remoteAssetsIcon;

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(OnItemClick);
        }

        public void OnItemClick()
        {
            if (mData != null && mData.GoodsType == GoodsType.SingleUgc)
            {
                var assetData = mData.GetFirstAsset<AssetsData>();
                if (assetData == null || assetData.UgcInfo == null || assetData.UgcInfo.UgcInfo == null) return;
                var updateState = (ForceUpdate)mData.GetFirstAsset<AssetsData>().UgcInfo.UgcInfo.forceUpdate;
                if (updateState != ForceUpdate.Default)
                {
                    UIManager.Inst.OpenPanel(PanelId.UpdateTipsPanel, updateState);
                    return;
                }
            } else if (isFittingRoomPanel && mData != null && mData.GoodsType == GoodsType.SinglePgc && !mData.IsOwned) {
                if (mData.SourceData.Source != Source.Mall && mData.EndTime > 0) {
                    DateTime endTime = GameUtils.GetDataTimeStamp(mData.EndTime);
                    if (endTime <= DateTime.Now) {
                        TipPanel.ShowToast("该商品为限时活动商品，该活动已结束");
                        return;
                    }
                }
            }
            onItemSelected?.Invoke(mData);
        }

        private void ResetAllUI()
        {
            if (iconBlurFilter) iconBlurFilter.Blur = 0;
            assetRoot.SetActive(false);
            takeOffRoot.SetActive(false);
            designRoot.SetActive(false);
            banRoot.SetActive(false);
            infoRoot.SetActive(false);
            if (redDotRoot) redDotRoot.SetActive(false);
            if (extInfoRoot) extInfoRoot.SetActive(false);
        }

        public void SetStyle(Color color1, Color color2)
        {
            colorBackground.color = color1;
            selectedColor = color2;
        }

        public void UpdateViews(GoodsData goodsData, bool isFittingRoom, Action<GoodsData> action) {
            isFittingRoomPanel = isFittingRoom;
            mData = goodsData;
            onItemSelected = action;
            ResetAllUI();

            SetSelected(goodsData.Selected);

            if (mData.ButtonType == ButtonType.TakeOff)
            {
                takeOffRoot.SetActive(true);
                return;
            }
            else if (mData.ButtonType == ButtonType.Design)
            {
                designRoot.SetActive(true);
                if (designText != null && !string.IsNullOrEmpty(goodsData.AddTips)) designText.SetLocalText(goodsData.AddTips);
                return;
            }
            else if (mData.ButtonType == ButtonType.EmoteIdle)
            {
                assetRoot.SetActive(true);
                EmoteIdleItem();
                return;
            }

            assetRoot.SetActive(true);
            toolsName.gameObject.SetActive(false);
            iconLight.gameObject.SetActive(false);
            // 商品类型 单品或者捆绑包
            switch (goodsData.GoodsType)
            {
                case GoodsType.SinglePgc:
                case GoodsType.SingleUgc:
                    SingleItemUpdate(goodsData.Assets);
                    break;
                case GoodsType.BundlePgc:
                    BundlePgcItemUpdate(goodsData.Id);
                    break;
                case GoodsType.BundleUgc:
                    BundleItemUpdate();
                    break;
                case GoodsType.ToolProduct:
                    ToolsItemUpdate();
                    break;
            }

            mData.LoadingAction = OnLoadingAction;
            loadingRoot.SetActive(mData.IsLoading);

            if (mData.IsBagScene)
            {
                if (redDotRoot) redDotRoot.SetActive(mData.IsNew);
                infoRoot.SetActive(false);
                return;
            }

            infoRoot.SetActive(true);
            SetSelected(goodsData.Selected);
            iconImage.gameObject.SetActive(false);
            text.gameObject.SetActive(true);
            text.font = textFont;
            if (timeLimitRoot != null) {
                timeLimitRoot.SetActive(false);
            }
            
            if (mData.IsOwned)
            {
                text.SetLocalText("已拥有");
                return;
            }
            
            if (mData.Price == null)
            {
                text.SetLocalText("加载中");
                return;
            }

            if (timeLimitRoot != null && isFittingRoomPanel) {
                // 试衣间特有逻辑
                if (mData.EndTime > 0 && !mData.IsOwned) {
                    timeLimitRoot.SetActive(true);

                    DateTime endTime = GameUtils.GetDataTimeStamp(mData.EndTime);
                    if (endTime > DateTime.Now) {
                        TimeSpan span = endTime - DateTime.Now;
                        if (span.TotalDays > 1d) {
                            timeLimitText.SetLocalText("距结束:{0}", LocalizationManager.Inst.GetLocalizedText("{0}天{1}小时",Math.Floor(span.TotalDays), span.Hours));
                        } else {
                            timeLimitText.SetLocalText("距结束:{0}", LocalizationManager.Inst.GetLocalizedText("{0}小时{1}分",Math.Floor(span.TotalHours), span.Minutes));
                        }
                    } else {
                        timeLimitText.SetLocalText("活动已结束");
                    }
                } else {
                    timeLimitRoot.SetActive(false);
                }
            }
            if (mData.Price.Value == 0) {
                text.SetLocalText("免费");
            } else {
                if (mData.GiftType == GiftType.PremiumSeasonPass || mData.GiftType == GiftType.DeluxeSeasonPass || mData.GiftType == GiftType.AdvancedSeasonPassTier || mData.GiftType == GiftType.MonthlyVip)
                {
                    iconImage.gameObject.SetActive(false);
                    text.font = numFont;
                    text.text = $"\u00a5{mData.Price.Value}";
                    return;
                }

                if (mData.GiftType == GiftType.NewYearLimitedPackage)
                {
                    int price = IAPDataManager.Inst.GetNewYearLimitedPackagePrice(mData.Id, (int)(Mathf.Ceil(mData.Price.Value)));
                    mData.Price.Value = price;
                }

                iconImage.gameObject.SetActive(true);
                iconImage.sprite =
                    PgcUtils.LoadCurrencyIcon((CurrencyType)mData.Price.CurrencyType, gameObject);
                text.font = numFont;
                text.text = $"{mData.Price.Value}";
            }
        }

        private void OnLoadingAction(bool loading, GoodsData goodsData)
        {
            if (this == null || goodsData != mData || loadingRoot == null) return;
            loadingRoot.SetActive(loading);
        }

        private void ToolsItemUpdate()
        {
            
            toolsName.gameObject.SetActive(true);
            iconLight.gameObject.SetActive(true);
            discountTag.SetActive(false);
            toolsName.text = mData.Name;

            var spriteName = "icon_premium_pass";
            if (mData.Id.Contains("giftgaojipass"))
            {
                spriteName = "icon_premium_pass";
                colorBackground.color = DataUtil.DeSerializeColorByHex("#f9dcac");
            } else if (mData.Id.Contains("gifthaohuapass"))
            {
                spriteName = "icon_deluxe_pass";
                colorBackground.color = DataUtil.DeSerializeColorByHex("#f2aa62");
            } else if (mData.Id.Contains("gifthaohuauppass"))
            {
                spriteName = "icon_deluxe_pass_upgrade";
                colorBackground.color = DataUtil.DeSerializeColorByHex("#ee7aba");
            } else if (mData.Id.Contains("giftvip"))
            {
                spriteName = "icon_vip_month";
                colorBackground.color = DataUtil.DeSerializeColorByHex("#9e4af6");
            } else if (mData.Id.Contains("newYearLimitedPack1"))
            {
                spriteName = "icon_newYearLimitedPack1";
                colorBackground.color = DataUtil.DeSerializeColorByHex("#A71338");
                bool hasDiscount = IAPDataManager.Inst.HasNewYearLimitedDiscount(mData.Id);
                discountTag.SetActive(hasDiscount);
            } else if (mData.Id.Contains("newYearLimitedPack3"))
            {
                spriteName = "icon_newYearLimitedPack3";
                colorBackground.color = DataUtil.DeSerializeColorByHex("#FF9636");
                bool hasDiscount = IAPDataManager.Inst.HasNewYearLimitedDiscount(mData.Id);
                discountTag.SetActive(hasDiscount);
            }
            else if (mData.Id.Contains("newYearLimitedDancingPack"))
            {
                spriteName = "icon_newYearLimitedDancingPack";
                colorBackground.color = DataUtil.DeSerializeColorByHex("#EF624A");
                bool hasDiscount = IAPDataManager.Inst.HasNewYearLimitedDiscount(mData.Id);
                discountTag.SetActive(hasDiscount);
            }
            else if (mData.Id.Contains("LaborDay"))
            {
                assetsIcon.rectTransform.sizeDelta = new Vector2(132,138);
                spriteName = "newLaborPack2";
                colorBackground.color = DataUtil.DeSerializeColorByHex("#B8E231");
                bool hasDiscount = IAPDataManager.Inst.HasNewYearLimitedDiscount(mData.Id);
                discountTag.SetActive(hasDiscount);
            }
            assetsIcon.gameObject.SetActive(true);
            remoteAssetsIcon.gameObject.SetActive(false);
            var spriteatlasPath = "Assets/Loadable/UI/UIPanel/FittingRoomPanel/FittingRoomPanel.spriteatlas";
            var sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, spriteName, gameObject);
            if (sprite != null) assetsIcon.sprite = sprite;
            else assetsIcon.sprite = budSprite;
        }
        

        // 单品
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

            //if (!mData.IsBagScene) return;

            CheckUgcIsBan(assetsData);
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

            //if (!mData.IsBagScene) return;

            CheckUgcIsBan(assetsData);
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
            CheckUgcIsBan(assetsData);
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
            CheckUgcIsBan(assetsData);
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

        private void PetDefalutUgcAnimItem(UgcAnimAssetsData assetsData)
        {
            remoteAssetsIcon.gameObject.SetActive(false);
            assetsIcon.gameObject.SetActive(true);
            assetsIcon.sprite = budSprite;
            if (assetsData.UgcInfo != null)
            {
                RefreshCover(assetsData.UgcInfo.UgcInfo.cover);
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
                extInfoRoot.SetActive(true);
                extInfoText.SetLocalText(assetsData.UgcInfo.musicScoreInfo.toneType == (int)ToneType.Fifteen ? "15音" : "22音");
            }

            //if (!mData.IsBagScene) return;

            CheckUgcIsBan(assetsData);
        }

        private void CheckUgcIsBan(UGCAssetsData assetsData)
        {
            banRewardBtn.onClick.RemoveAllListeners();
            AssetsDataManager.GetUgcInfo(assetsData.Id, (serverData) =>
            {
                if (serverData?.skinInfo == null)
                {
                    return;
                }
                assetsData.AvatarSubType = (AvatarSubType)serverData.skinInfo.subType;
                assetsData.UgcInfo = serverData;
                assetsData.Name = serverData.skinInfo.name;

                if (this == null) return;
                if (mData.Id != serverData.UgcInfo.id) return;
                RefreshCover(serverData.UgcInfo.cover);
                if (!mData.IsBagScene) return;
                Ban(serverData.skinInfo.isBan, serverData.skinInfo.paymentInfo);
            });
        }

        private void CheckUgcIsBan(MusicScoreAssetsData assetsData)
        {
            banRewardBtn.onClick.RemoveAllListeners();
            AssetsDataManager.GetMusicScoreInfo(assetsData.Id, (isSuccess, serverData) =>
            {
                if (!isSuccess) return;

                assetsData.UgcInfo = serverData;
                assetsData.Name = serverData.musicScoreInfo.name;
                if (this == null) return;
                if (mData.Id != serverData.musicScoreInfo.id) return;

                RefreshCover(serverData.musicScoreInfo.cover);
                extInfoRoot.SetActive(true);
                extInfoText.SetLocalText(assetsData.UgcInfo.musicScoreInfo.toneType == (int)ToneType.Fifteen ? "15音" : "22音");
                if (!mData.IsBagScene) return;
                Ban(serverData.musicScoreInfo.isBan, serverData.musicScoreInfo.paymentInfo);
            });
        }

        private void CheckUgcIsBan(UgcPoseAssetsData assetsData)
        {
            banRewardBtn.onClick.RemoveAllListeners();
            AssetsDataManager.GetPoseInfo(assetsData.Id, (isSuccess, serverData) =>
            {
                if (!isSuccess) return;

                assetsData.UgcInfo = serverData;
                assetsData.Name = serverData.poseInfo.name;
                if (this == null) return;
                if (mData.Id != serverData.poseInfo.id) return;

                RefreshCover(serverData.poseInfo.cover);
                if (!mData.IsBagScene) return;
                Ban(serverData.poseInfo.isBan, serverData.poseInfo.paymentInfo);
            });
        }

        private void CheckUgcIsBan(UgcAnimAssetsData assetsData)
        {
            banRewardBtn.onClick.RemoveAllListeners();
            AssetsDataManager.GetUgcAnimInfo(assetsData.Id, (isSuccess, serverData) =>
            {
                if (!isSuccess) return;

                assetsData.UgcInfo = serverData;
                assetsData.Name = serverData.animInfo.name;

                if (this == null) return;
                if (mData.Id != serverData.animInfo.id) return;

                RefreshCover(serverData.animInfo.cover);
                if (assetName != null)
                {
                    assetName.text = serverData.animInfo.name;
                }
                if (!mData.IsBagScene) return;
                Ban(serverData.animInfo.isBan, serverData.animInfo.paymentInfo);
            });
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

        private void Ban(int isBan, PaymentInfo paymentInfo)
        {
            if (isBan >= 1)
            {
                iconBlurFilter.Blur = 10;
                banRoot.SetActive(true);
                banRewardText.SetLocalText(isBan == 1 ? "领取补偿" : "确认");
                var haveReward = isBan == 1 && paymentInfo != null;
                banRewardBtn.onClick.RemoveAllListeners();
                banRewardBtn.onClick.AddListener(() =>
                {
                    banRewardBtn.interactable = false;
                    AssetsDataManager.GetBanReward(mData.Id, (success) =>
                    {
                        if (banRewardBtn != null) banRewardBtn.interactable = !success;

                        if (success && haveReward)
                        {
                            var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
                            CommonRewardItemData commonRewardItemData = new CommonRewardItemData();
                            commonRewardItemData.IconSp = PgcUtils.LoadCurrencyIcon(CurrencyType.Badge, gameObject);
                            commonRewardItemData.rewardName = PgcUtils.GetTokenName(CurrencyType.Badge);
                            commonRewardItemData.RewardAmount = paymentInfo.price;
                            panel.ShowRewards(new List<CommonRewardItemData>() { commonRewardItemData });
                        }
                    });
                });
            }
        }

        private void SetSelected(bool flag)
        {
            selectedImage.gameObject.SetActive(false);
            background.color = Color.white;

            if (!flag) return;
            if (infoRoot.activeSelf)
            {
                background.color = selectedColor;
            }
            else
            {
                selectedImage.gameObject.SetActive(true);
            }

            if (mData.IsBagScene) AssetsDataManager.ClearNewTip(mData);
        }
    }
}
