using System;
using System.Collections.Generic;
using Basic.Utils;
using Es;
using Game.Store;
using GameData.Gashapon;
using GameData.PgcData;
using Product;
using UnityEngine;

namespace UI.UIPanels.GashaponPanel
{
    public class GashaponUtils
    {
        public static string ViewBasePath = "Assets/Loadable/UI/UIPanel/GashaponPanel/";
        public static Sprite LoadGashaponSprite(string spriteName, GameObject refObj)
        {
            var atlas = XAssetLoaderMgr.Inst.GetSpriteAltasPath(SpriteAtlasType.GashaponIconSprite);
            var sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlas, spriteName, refObj);
            return sprite;
        }

        public static bool HasPGCData(GashaponBaseData rewardData)
        {
            return (rewardData.PgcDatas != null && rewardData.PgcDatas.Count > 0);
        }

        public static bool IsAssetsDataEqual(List<AssetsData> list1, List<string> list2)
        {
            if (list1 == null) return false;
            List<string> listStr = ToPgcIdList(list1);
            return GameUtils.IsListEqual(listStr,list2);
        }

        public static List<string> ToPgcIdList(List<AssetsData> list1)
        {
            List<string> listStr = new List<string>();
            foreach (var assetData in list1)
            {
                listStr.Add(assetData.Id);
            }

            return listStr;
        }

        public static bool IsOwnedReward(GashaponBaseData data)
        {
            bool isOwned = false;
            if (data.PgcDatas != null && data.PgcDatas.Count > 0)
            {
                isOwned = true;
                foreach (var assetData in data.PgcDatas)
                {
                    if (!IsOwnedAsset(assetData))
                    {
                        isOwned = false;
                        break;
                    }
                }
            }

            if (data.RewardType == RewardType.RewardChatBubbles)
            {
                return UserUIWidgetManager.Inst.CheckIsOwnedBubblePgc(data.Id);
            }
            else if (data.RewardType == RewardType.RewardAvatarFrame)
            {
                return UserUIWidgetManager.Inst.CheckIsOwnedAvatarFramePgc(data.Id);
            }
            else if (data.RewardType == RewardType.RewardHomepageSkin)
            {
                return UserUIWidgetManager.Inst.CheckIsOwnedHomeSkipPgc(data.Id);
            }
            else if ((int)data.RewardType == (int)BUDRewardType.RewardUgcTemplateResource)
            {
                return LimitTimePropManager.Inst.CheckLimitPetProp(data.Id);
            }
            else if((int)data.RewardType == (int)BUDRewardType.RewardTypeNicknameFrame)
            {
                return UserUIWidgetManager.Inst.CheckIsOwnedNicknamePgc(data.Id);
            }
            else if((int)data.RewardType == (int)BUDRewardType.RewardTypeTitle)
            {
                return UserUIWidgetManager.Inst.CheckIsOwnedTitlePgc(data.Id);
            }
            return isOwned;
        }

        public static bool IsOwnedAsset(AssetsData data)
        {
            return data != null && AssetsDataManager.IsOwned(data.Id);
        }

        public static bool CurrencyIsEnough(int currencyType,int price)
        {
            bool canConvert = Enum.IsDefined(typeof(CurrencyType), currencyType);
            if (canConvert)
            {
                CurrencyType bType = (CurrencyType)currencyType;
                int value = AccountDataManager.Inst.BalanceInfo.GetAccountCount(bType);
                return value >= price;
            }
            return false;
        }

        public static GashaponExtraTaskInfo GetExtraServerInfo(List<GashaponExtraTaskInfo> taskList,int id)
        {
            if (taskList == null || taskList.Count <= 0) return null;
            return taskList.Find(x => x.eventId == id);
        }

        public static int GetRealTenPrice(GashaponData data)
        {
            return (int)((100 - data.Discount) * 0.01 * data.TenDrawPrice);
        }
    }

    public enum GashaponSpecialButton {
        ErrType,
        LuckyStar,
        PgcOptionalBox
    }

}
