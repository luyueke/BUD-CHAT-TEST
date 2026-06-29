using System;
using System.Collections.Generic;
using System.Linq;
using Basic.Utils;
using Game.Store;
using GameData.Base;
using GameData.BaseInfo;
using GameData.Manager;
using GameData.PgcData;


namespace UI.UIPanels.RechargePanel {
    public class DiscountCardManager : GlobalInstance<DiscountCardManager> {


        public bool hasDiscountCard;
        public long expireTime;

        public void UpdateStatus()
        {
            IAPDataManager.Inst.GetDiscountCardStatus((b, rsp) =>
            {
                if (rsp == null)
                {
                    return;
                }

                hasDiscountCard = rsp.hasDiscountCard;
                expireTime = rsp.expireTime;
            });
        }

        public void SetStatus(bool isOwned, long expire) {
            hasDiscountCard = isOwned;
            expireTime = expire;
        }
    }

    public static class DiscountCardUtils
    {
        /// <summary>
        /// 折扣卡打 6折
        /// </summary>
        /// <returns></returns>
        public static float GetDiscount()
        {
            return 0.6f;
        }

        /// <summary>
        /// 是否开启折扣卡
        /// </summary>
        /// <returns></returns>
        public static bool IsEnableDiscountCard()
        {
            return BusinessLiveManager.Inst.IsActivityLive(((int)ActivityId.MusicAndDanceCommunity).ToString());
        }

        /// <summary>
        /// 拥有折扣卡 且在有效期内
        /// </summary>
        /// <returns></returns>
        public static bool IsOwnedDiscountCard()
        {
            if (!DiscountCardManager.Inst.hasDiscountCard)
            {
                return false;
            }

            var expireTimeDate = GameUtils.GetDataTimeStamp(DiscountCardManager.Inst.expireTime);
            if (expireTimeDate < DateTime.Now)
            {
                return false;
            }

            return true;
        }

        public static bool IsSupportDiscountCard(UgcBaseInfo ugcInfo)
        {
            if (ugcInfo == null)
            {
                return false;
            }
            if (!IsEnableDiscountCard())
            {
                return false;
            }

            if (ugcInfo is AnimInfo)
            {
                return true;
            }
            else if (ugcInfo is SkinInfo skinInfo && skinInfo.isPrivateOrder == 0 && skinInfo.subType == (int)AvatarSubType.MusicalInstrument)
            {
                return true;
            }
            return false;
        }

        public static bool IsSupportDiscountCard(GoodsData data)
        {
            if (!IsEnableDiscountCard())
            {
                return false;
            }


            if (data.Assets != null && data.Assets.Count > 0)
            {
                var asset = data.Assets[0];
                if (asset.ResourceType == ResourceType.UgcEmote)
                {
                    return true;
                }

                if (asset.ResourceType == ResourceType.UgcAvatar)
                {
                    var skinInfo = asset.UgcInfo?.skinInfo;
                    if (skinInfo != null && skinInfo.isPrivateOrder == 1)
                    {
                        return false;
                    }
                    if (asset is AvatarAssetsData avatarAsset &&
                        avatarAsset.AvatarSubType == AvatarSubType.MusicalInstrument)
                    {
                        return true;
                    }
                }

                return false;
            }

            return false;
        }
    }
}
