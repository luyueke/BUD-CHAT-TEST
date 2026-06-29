using Game.Store;
using Game.Utils;
using GameData.PgcData;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.FittingRoom
{
    public class ItemInfo : MonoBehaviour
    {
        [SerializeField] Text itemName;
        [SerializeField] Text useTips;
        [SerializeField] private HeadViewWidget HeadViewWidget;
        [SerializeField] Button ugcHeadRoot;

        private GoodsData mData;
        private AssetDetailType detailType = AssetDetailType.Skin;

        public Action OnHeadClick;

        private void Awake()
        {
            ugcHeadRoot.onClick.AddListener(() =>
            {
                OnHeadClick?.Invoke();
                int ugcStyle = AssetsDataManager.GetUgcStyle(mData);
                UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel, detailType, mData.Id,ugcStyle);
            });
        }

        public void ResetAllUI()
        {
            HeadViewWidget.gameObject.SetActive(false);
        }

        public void SetTarget(GoodsData goodsData)
        {
            mData = goodsData;
            ResetAllUI();

            if (goodsData.CantWear && goodsData.IsOwned)
            {
                if (useTips) useTips.text = goodsData.UseTips;
            }
            else
            {
                if (useTips) useTips.text = "";
            }

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
                    UgcBundleItemUpdate(goodsData);
                    break;
                case GoodsType.ToolProduct:
                    ToolsItemUpdate();
                    break;
            }
        }
        
        
        private void BundlePgcItemUpdate(string bundleId)
        {
            itemName.text = PgcUtils.GetBundleName(bundleId);
        }

        private void ToolsItemUpdate()
        {
            itemName.text = mData.Name;
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
                case ResourceType.PGCPetAvatar:
                    PgcItem(assets as PGCAssetsData);
                    break;
                case ResourceType.UgcAvatar:
                case ResourceType.UGCPetAvatar:
                    var avatarAssetsData = assets as AvatarAssetsData;
                    detailType = avatarAssetsData.AvatarSubType == AvatarSubType.MusicalInstrument ? AssetDetailType.Instrument : AssetDetailType.Skin;
                    UgcItem(assets);
                    break;
                case ResourceType.MusicScore:
                    detailType = AssetDetailType.MusicScore;
                    UgcItem(assets);
                    break;
                case ResourceType.UgcPose:
                    detailType = AssetDetailType.UgcPose;
                    UgcItem(assets);
                    break;
                case ResourceType.UgcEmote:
                    detailType = AssetDetailType.UgcAnim;
                    UgcItem(assets);
                    break;
                case ResourceType.Emote:
                    EmoteItem(assets as EmoteAssetsData);
                    break;
                case ResourceType.UgcVehicle:
                    detailType = AssetDetailType.Vehicle;
                    UgcItem(assets);
                    break;
                case ResourceType.Vehicle:
                    PgcItem(assets as PGCAssetsData);
                    break;
                case ResourceType.AvatarCard:
                    detailType = AssetDetailType.Actor;
                    UgcItem(assets);
                    break;
                case ResourceType.Theatre:
                    detailType = AssetDetailType.Theatre;
                    UgcItem(assets);
                    break;
            }
        }

        private void UgcBundleItemUpdate(GoodsData goodsData)
        {
            if (goodsData.IsGiftScene)
            {
                HeadViewWidget.gameObject.SetActive(false);
                return;
            }
            detailType = AssetDetailType.UgcBundle;
            itemName.text = mData.Name;
            if (goodsData.UgcBundleInfo == null || goodsData.UgcBundleInfo.creatorInfo == null)
            {
                HeadViewWidget.gameObject.SetActive(false);
                return;
            }
            HeadViewWidget.gameObject.SetActive(true);
            HeadViewWidget.InitHeadCycle(goodsData.UgcBundleInfo.creatorInfo);
        }

        private void PgcItem(PGCAssetsData assetsData)
        {
            itemName.text = mData.Name;
        }

        private void EmoteItem(EmoteAssetsData assetsData)
        {
            itemName.text = mData.Name;
        }

        private void UgcItem(AssetsData assetsData)
        {
            if (mData.IsGiftScene)
            {
                HeadViewWidget.gameObject.SetActive(false);
                return;
            }
            itemName.text = mData.Name;
            if (assetsData.UgcInfo == null || assetsData.UgcInfo.creatorInfo == null)
            {
                HeadViewWidget.gameObject.SetActive(false);
                return;
            }
            HeadViewWidget.gameObject.SetActive(true);
            HeadViewWidget.InitHeadCycle(assetsData.UgcInfo.creatorInfo);
        }
    }
}
