using System;
using System.Collections;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.Util.IO;
using Game.Store;
using GameData.PgcData;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.IncubationCabin
{

    public class GoodsDataIconUtil
    {
        public static void SetIcon(Image iconImage, RemoteImageBehaviour remoteImage, GoodsData mData, GameObject gameObject)
        {
            if (mData.ButtonType == ButtonType.TakeOff || mData.ButtonType == ButtonType.Design)
            {
                return;
            }
            if (mData.ButtonType == ButtonType.EmoteIdle)
            {
                EmoteIdleItem(iconImage, remoteImage, mData, gameObject);
                return;
            }
            switch (mData.GoodsType)
            {
                case GoodsType.SinglePgc:
                case GoodsType.SingleUgc:
                    SingleItemUpdate(mData, iconImage, remoteImage, gameObject);
                    break;
                case GoodsType.BundleUgc:
                    BundleItemUpdate(iconImage, remoteImage, mData, gameObject);
                    break;
            }
        }

        private static void EmoteIdleItem(Image iconImage, RemoteImageBehaviour remoteImage, GoodsData mData, GameObject gameObject)
        {
            remoteImage.gameObject.SetActive(false);
            iconImage.gameObject.SetActive(true);
            var atlas = XAssetLoaderMgr.Inst.GetSpriteAltasPath(SpriteAtlasType.PgcEmoteSprite);
            string spriteName = mData.Id;
            if (mData.ProductId != 0)
            {
                spriteName = mData.Id + mData.ProductId;
            }
            var sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlas, spriteName, gameObject);
            if (sprite != null) iconImage.sprite = sprite;
        }

        private static void BundleItemUpdate(Image assetsIcon, RemoteImageBehaviour remoteAssetsIcon, GoodsData mData, GameObject gameObject)
        {
            remoteAssetsIcon.gameObject.SetActive(false);
            assetsIcon.gameObject.SetActive(true);
            if (mData.UgcBundleInfo != null && mData.UgcBundleInfo.skinInfo != null)
            {
                RefreshCover(mData.UgcBundleInfo.skinInfo.cover, assetsIcon, remoteAssetsIcon);
            }

        }
        private static void SingleItemUpdate(GoodsData mData, Image iconImage, RemoteImageBehaviour remoteImage, GameObject gameObject)
        {
            List<AssetsData> assetsData = mData.Assets;
            if (assetsData == null) return;
            if (assetsData.Count != 1) return;
            var assets = assetsData[0];
            // 资源类型
            switch (assets.ResourceType)
            {
                case ResourceType.Avatar:
                    PgcItem(assets, iconImage, remoteImage, gameObject);
                    break;
                case ResourceType.UgcAvatar:
                    UgcItem((UGCAssetsData)assets, mData, iconImage, remoteImage, gameObject);
                    break;
                case ResourceType.Emote:
                    EmoteItem((EmoteAssetsData)assets, iconImage, remoteImage, gameObject);
                    break;
                case ResourceType.MusicScore:
                    MusicScoreItem((MusicScoreAssetsData)assets, mData, iconImage, remoteImage, gameObject);
                    break;
                case ResourceType.PGCPetAvatar:
                    PetPgcItem(assets, iconImage, remoteImage, gameObject);
                    break;
                case ResourceType.UGCPetAvatar:
                    PetUgcItem((UGCAssetsData)assets, mData, iconImage, remoteImage, gameObject);
                    break;
                case ResourceType.UgcPose:
                    PetUgcPoseItem((UgcPoseAssetsData)assets, mData, iconImage, remoteImage, gameObject);
                    break;
                case ResourceType.UgcEmote:
                    PetUgcAnimItem((UgcAnimAssetsData)assets, mData, iconImage, remoteImage, gameObject);
                    break;
            }
        }

        private static void EmoteItem(EmoteAssetsData assetsData, Image assetsIcon, RemoteImageBehaviour remoteAssetsIcon, GameObject gameObject)
        {
            remoteAssetsIcon.gameObject.SetActive(false);
            assetsIcon.gameObject.SetActive(true);
            var sprite = PgcUtils.GetIconSpriteByPgcId(assetsData.Id, gameObject);
            if (sprite != null) assetsIcon.sprite = sprite;


        }

        private static void PetPgcItem(AssetsData assetsData, Image assetsIcon, RemoteImageBehaviour remoteAssetsIcon, GameObject gameObject)
        {
            remoteAssetsIcon.gameObject.SetActive(false);
            assetsIcon.gameObject.SetActive(true);
            var sprite = PgcUtils.GetIconSpriteByPgcId(assetsData.Id, gameObject);

            if (sprite != null) assetsIcon.sprite = sprite;
        }

        private static void RefreshCover(string cover, Image assetsIcon, RemoteImageBehaviour remoteAssetsIcon)
        {
            string url = cover;
            remoteAssetsIcon.Load(cover, onCompleted: (bool fromCache, bool success) =>
            {
                if (assetsIcon == null || url != cover) return;
                assetsIcon.gameObject.SetActive(false);
                remoteAssetsIcon.gameObject.SetActive(true);
            });
        }
        private static void PgcItem(AssetsData assetsData, Image iconImage, RemoteImageBehaviour remoteImage, GameObject gameObject)
        {
            remoteImage.gameObject.SetActive(false);
            iconImage.gameObject.SetActive(true);
            var sprite = PgcUtils.GetIconSpriteByPgcId(assetsData.Id, gameObject);
            if (sprite != null) iconImage.sprite = sprite;
        }

        private static void UgcItem(UGCAssetsData assetsData, GoodsData mData, Image assetsIcon, RemoteImageBehaviour remoteAssetsIcon, GameObject gameObject)
        {
            remoteAssetsIcon.gameObject.SetActive(false);
            assetsIcon.gameObject.SetActive(true);
            if (assetsData.UgcInfo != null)
            {
                RefreshCover(assetsData.UgcInfo.UgcInfo.cover, assetsIcon, remoteAssetsIcon);
            }
            CheckUgcIsBan(assetsData, mData, assetsIcon, remoteAssetsIcon, gameObject);
        }

        private static void PetUgcItem(UGCAssetsData assetsData, GoodsData mData, Image assetsIcon, RemoteImageBehaviour remoteAssetsIcon, GameObject gameObject)
        {
            remoteAssetsIcon.gameObject.SetActive(false);
            assetsIcon.gameObject.SetActive(true);
            if (assetsData.UgcInfo != null)
            {
                RefreshCover(assetsData.UgcInfo.UgcInfo.cover, assetsIcon, remoteAssetsIcon);
            }

            //if (!mData.IsBagScene) return;

            CheckUgcIsBan(assetsData, mData, assetsIcon, remoteAssetsIcon, gameObject);
        }


        private static void PetUgcPoseItem(UgcPoseAssetsData assetsData, GoodsData mData, Image assetsIcon, RemoteImageBehaviour remoteAssetsIcon, GameObject gameObject)
        {
            remoteAssetsIcon.gameObject.SetActive(false);
            assetsIcon.gameObject.SetActive(true);
            if (assetsData.UgcInfo != null)
            {
                RefreshCover(assetsData.UgcInfo.UgcInfo.cover, assetsIcon, remoteAssetsIcon);
            }
            CheckUgcIsBan(assetsData, mData, assetsIcon, remoteAssetsIcon, gameObject);
        }

        private static void PetUgcAnimItem(UgcAnimAssetsData assetsData, GoodsData mData, Image assetsIcon, RemoteImageBehaviour remoteAssetsIcon, GameObject gameObject)
        {
            remoteAssetsIcon.gameObject.SetActive(false);
            assetsIcon.gameObject.SetActive(true);
            if (assetsData.UgcInfo != null)
            {
                RefreshCover(assetsData.UgcInfo.UgcInfo.cover, assetsIcon, remoteAssetsIcon);

            }
            CheckUgcIsBan(assetsData, mData, assetsIcon, remoteAssetsIcon, gameObject);
        }

        private void PetDefalutUgcAnimItem(UgcAnimAssetsData assetsData, GoodsData mData, Image assetsIcon, RemoteImageBehaviour remoteAssetsIcon, GameObject gameObject)
        {
            remoteAssetsIcon.gameObject.SetActive(false);
            assetsIcon.gameObject.SetActive(true);
            if (assetsData.UgcInfo != null)
            {
                RefreshCover(assetsData.UgcInfo.UgcInfo.cover, assetsIcon, remoteAssetsIcon);
            }
        }



        private static void MusicScoreItem(MusicScoreAssetsData assetsData, GoodsData mData, Image assetsIcon, RemoteImageBehaviour remoteAssetsIcon, GameObject gameObject)
        {
            remoteAssetsIcon.gameObject.SetActive(false);
            assetsIcon.gameObject.SetActive(true);
            if (assetsData.UgcInfo != null)
            {
                RefreshCover(assetsData.UgcInfo.musicScoreInfo.cover, assetsIcon, remoteAssetsIcon);
            }

            //if (!mData.IsBagScene) return;

            CheckUgcIsBan(assetsData, mData, assetsIcon, remoteAssetsIcon, gameObject);
        }

        private void VehiclePgcItem(VehicleAssetsData assetsData, GoodsData mData, Image assetsIcon, RemoteImageBehaviour remoteAssetsIcon, GameObject gameObject)
        {
            remoteAssetsIcon.gameObject.SetActive(false);
            assetsIcon.gameObject.SetActive(true);
            var sprite = PgcUtils.GetIconSpriteByPgcId(assetsData.Id, gameObject);
            if (sprite != null) assetsIcon.sprite = sprite;
        }

        private void VehicleUgcItem(UgcVehicleAssetsData assetsData, GoodsData mData, Image assetsIcon, RemoteImageBehaviour remoteAssetsIcon, GameObject gameObject)
        {
            remoteAssetsIcon.gameObject.SetActive(false);
            assetsIcon.gameObject.SetActive(true);
            if (assetsData.UgcInfo != null)
            {
                RefreshCover(assetsData.UgcInfo.UgcInfo.cover, assetsIcon, remoteAssetsIcon);
            }
            CheckUgcIsBan(assetsData, mData, assetsIcon, remoteAssetsIcon, gameObject);
        }

        private static void CheckUgcIsBan(UGCAssetsData assetsData, GoodsData mData, Image assetsIcon, RemoteImageBehaviour remoteAssetsIcon, GameObject gameObject)
        {
            AssetsDataManager.GetUgcInfo(assetsData.Id, (serverData) =>
            {
                assetsData.AvatarSubType = (AvatarSubType)serverData.skinInfo.subType;
                assetsData.UgcInfo = serverData;
                assetsData.Name = serverData.skinInfo.name;

                if (assetsIcon == null) return;
                if (mData.Id != serverData.UgcInfo.id) return;
                RefreshCover(serverData.UgcInfo.cover, assetsIcon, remoteAssetsIcon);
                if (!mData.IsBagScene) return;
            });
        }

        private static void CheckUgcIsBan(MusicScoreAssetsData assetsData, GoodsData mData, Image assetsIcon, RemoteImageBehaviour remoteAssetsIcon, GameObject gameObject)
        {
            AssetsDataManager.GetMusicScoreInfo(assetsData.Id, (isSuccess, serverData) =>
            {
                if (!isSuccess) return;

                assetsData.UgcInfo = serverData;
                assetsData.Name = serverData.musicScoreInfo.name;
                if (assetsIcon == null) return;
                if (mData.Id != serverData.musicScoreInfo.id) return;

                RefreshCover(serverData.musicScoreInfo.cover, assetsIcon, remoteAssetsIcon);
            });
        }

        private static void CheckUgcIsBan(UgcPoseAssetsData assetsData, GoodsData mData, Image assetsIcon, RemoteImageBehaviour remoteAssetsIcon, GameObject gameObject)
        {
            AssetsDataManager.GetPoseInfo(assetsData.Id, (isSuccess, serverData) =>
            {
                if (!isSuccess) return;

                assetsData.UgcInfo = serverData;
                assetsData.Name = serverData.poseInfo.name;
                if (assetsIcon == null) return;
                if (mData.Id != serverData.poseInfo.id) return;

                RefreshCover(serverData.poseInfo.cover, assetsIcon, remoteAssetsIcon);
            });
        }

        private static void CheckUgcIsBan(UgcAnimAssetsData assetsData, GoodsData mData, Image assetsIcon, RemoteImageBehaviour remoteAssetsIcon, GameObject gameObject)
        {
            AssetsDataManager.GetUgcAnimInfo(assetsData.Id, (isSuccess, serverData) =>
            {
                if (!isSuccess) return;

                assetsData.UgcInfo = serverData;
                assetsData.Name = serverData.animInfo.name;

                if (assetsIcon == null) return;
                if (mData.Id != serverData.animInfo.id) return;

                RefreshCover(serverData.animInfo.cover, assetsIcon, remoteAssetsIcon);

            });
        }

        private void CheckUgcIsBan(UgcVehicleAssetsData assetsData, GoodsData mData, Image assetsIcon, RemoteImageBehaviour remoteAssetsIcon, GameObject gameObject)
        {
            AssetsDataManager.GetUgcVehicleInfo(assetsData.Id, (isSuccess, serverData) =>
            {
                if (!isSuccess) return;
                assetsData.UgcInfo = serverData;
                assetsData.Name = serverData.vehicleInfo.name;
                assetsData.VehicleSubType = (VehicleSubType)serverData.vehicleInfo.vehicleType;
                if (this == null) return;
                if (mData.Id != serverData.vehicleInfo.id) return;
                RefreshCover(serverData.vehicleInfo.cover, assetsIcon, remoteAssetsIcon);
            });
        }

    }
}