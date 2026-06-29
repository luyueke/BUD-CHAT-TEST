using Es;
using Game.Database;
using Game.Store;
using GameData.PgcData;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Store
{
    public class GashaponStoreHandler : AssetsDataHandler
    {
        private Dictionary<string, GashaponData> gashaponDict;

        public override void InitData()
        {
            dict = new();
            gashaponDict = new();

            var gashaponList = AssetsDataManager.StoreData.GashaponList;
            // 商城资源
            for (int i = 0, C = gashaponList.Count; i < C; i++)
            {
                var gashaponServerData = gashaponList[i];
                var gashaponData = GetGashaponData(gashaponServerData.Id);
                gashaponData.Name = gashaponServerData.Name;
                gashaponData.SinglePrice = gashaponServerData.SinglePrice;
                gashaponData.TenDrawPrice = gashaponServerData.TenDrawPrice;
                gashaponData.Discount = gashaponServerData.Discount;
                gashaponData.CurrencyType = (CurrencyType)gashaponServerData.CurrencyType;
                gashaponData.RewardList.Clear();
                for (int j = 0, L = gashaponServerData.RewardList.Count; j < L; j++)
                {
                    var rewardServerData = gashaponServerData.RewardList[j];
                    var rewardData = CreateGashaponRewardData(rewardServerData);
                    gashaponData.RewardList.Add(rewardData);
                }

                //兑换列表
                for (int k = 0;k < gashaponServerData.ExchangeList.Count; k++)
                {
                    var exchangeServerData = gashaponServerData.ExchangeList[k];
                    var localData = CreateGashaponExchangeData(exchangeServerData, gashaponServerData.Id);
                    gashaponData.ExchangeList.Add(localData);
                }
            }
        }

        public List<GashaponData> GetGashaponList()
        {
            return gashaponDict.Values.ToList();
        }

        public GashaponData GetGashaponData(string id)
        {
            if (gashaponDict.ContainsKey(id)) return gashaponDict[id];

            GashaponData gashaponData = new GashaponData();
            gashaponData.Id = id;
            gashaponData.RewardList = new();
            gashaponData.ExchangeList = new();
            gashaponDict.Add(id, gashaponData);
            return gashaponData;
        }

        private GashaponRewardData CreateGashaponRewardData(Product.StoreGashaponData.Types.RewardData serverData)
        {
            var id = (serverData.RewardType == Product.RewardType.RewardPgcResource
                      || serverData.RewardType == Product.RewardType.RewardAvatarFrame
                      || serverData.RewardType == Product.RewardType.RewardChatBubbles
                      || (int)serverData.RewardType == (int)BUDRewardType.RewardTypeNicknameFrame
                      || (int)serverData.RewardType == (int)BUDRewardType.RewardTypeTitle
                      || (int)serverData.RewardType == (int)BUDRewardType.RewardUgcTemplateResource) ? serverData.PgcId : $"{serverData.RewardType}_{serverData.Num}";

            GashaponRewardData rewardData;
            rewardData = new GashaponRewardData();
            rewardData.Id = id;

            rewardData.RewardType = serverData.RewardType;
            rewardData.Level = serverData.Level;
            rewardData.Num = serverData.Num;
            rewardData.BundleId = serverData.BundleId;
            rewardData.RewardId = serverData.RewardId;
            rewardData.Name = serverData.Name;

            if (serverData.RewardType == Product.RewardType.RewardPgcResource)
            {
                var assetData = CreatePgcAssetsData(serverData.PgcId, serverData.Name, BagDatabase.Inst.Select(serverData.PgcId));
                rewardData.PgcDatas = new List<AssetsData>();
                rewardData.PgcDatas.Add(assetData);
            }
            return rewardData;
        }

        private GashaponExchangeData CreateGashaponExchangeData(Product.StoreGashaponData.Types.ExchangeData serverData, string lotteryId)
        {
            var id = (serverData.RewardType == Product.RewardType.RewardPgcResource
                      || serverData.RewardType == Product.RewardType.RewardAvatarFrame
                      || (int)serverData.RewardType == (int)BUDRewardType.RewardTypeNicknameFrame
                      || (int)serverData.RewardType == (int)BUDRewardType.RewardTypeTitle
                      || serverData.RewardType == Product.RewardType.RewardChatBubbles
                      || (int)serverData.RewardType == (int)BUDRewardType.RewardTypeZZZPhantomCrystal) ? serverData.PgcId : $"{serverData.RewardType}_{serverData.RewardNum}";

            GashaponExchangeData localData;
            localData = new GashaponExchangeData();
            localData.Id = id;
            localData.ExchangeId = serverData.ExchangeId;
            localData.RewardType = serverData.RewardType;
            localData.Num = serverData.RewardNum;
            localData.Name = serverData.Name;
            localData.BundleId = serverData.BundleId;
            localData.Price = serverData.Price;
            localData.CurrencyType = (CurrencyType)serverData.CurrencyType;
            localData.LotteryId = lotteryId;

            if (serverData.RewardType == Product.RewardType.RewardPgcResource)
            {
                var assetData = CreatePgcAssetsData(serverData.PgcId, serverData.Name, BagDatabase.Inst.Select(serverData.PgcId));
                localData.PgcDatas = new List<AssetsData>();
                localData.PgcDatas.Add(assetData);
            }
            return localData;
        }

        private AssetsData CreatePgcAssetsData(string pgcId, string name, InventoryData inventoryData)
        {
            if (dict.ContainsKey(pgcId)) return dict[pgcId];

            AssetsData assetsData;
            var config = Es.DataTables.GetGameResData(pgcId);
            if (config == null) config = new();
            switch (config.ResourceType)
            {
                case (int)ResourceType.Avatar:
                case (int)ResourceType.PGCPetAvatar:
                    var pgcAssetsData = new PGCAssetsData();
                    pgcAssetsData.ResourceType = (ResourceType)config.ResourceType;
                    pgcAssetsData.AvatarSubType = (AvatarSubType)config.SubType;
                    var specialConfig = DataTables.GetSpecialSkinConfig(pgcId);
                    if (specialConfig != null) pgcAssetsData.AvatarSubType = AvatarSubType.SpecialSkin;
                    assetsData = pgcAssetsData;
                    break;
                case (int)ResourceType.Emote:
                    var emoteAssetsData = new EmoteAssetsData();
                    emoteAssetsData.ResourceType = (ResourceType)config.ResourceType;
                    emoteAssetsData.EmoteSubType = (EmoteSubType)config.SubType;
                    assetsData = emoteAssetsData;
                    break;
                case (int)ResourceType.AvatarFrame:
                    var afAssetsData = new AssetsData();
                    afAssetsData.ResourceType = (ResourceType)config.ResourceType;
                    assetsData = afAssetsData;
                    break;
                case (int)ResourceType.ChatBubble:
                    var cbAssetsData = new AssetsData();
                    cbAssetsData.ResourceType = (ResourceType)config.ResourceType;
                    assetsData = cbAssetsData;
                    break;
                case (int)ResourceType.Vehicle:
                    var vehicleAssetsData = new AssetsData();
                    vehicleAssetsData.ResourceType = (ResourceType)config.ResourceType;
                    assetsData = vehicleAssetsData;
                    break;
                default:
                    assetsData = new AssetsData();
                    break;
            }

            assetsData.Id = pgcId;
            assetsData.Name = name;
            assetsData.InventoryData = inventoryData;
            dict.Add(pgcId, assetsData);
            return assetsData;
        }
    }

    public class SeriesData
    {
        public int Id;
        public string Name;
        public string TextColor;        // 文字颜色
        public string StrokeColor;      // 描边颜色
        public string BackgroundColor;  // 背景颜色
    }

    public class GashaponData
    {
        public string Id;
        public string Name;
        public SeriesData Series;
        public int SinglePrice;
        public int singleDrawPrice;
        public int TenDrawPrice;
        public int Discount;            // 十连抽折扣
        public CurrencyType CurrencyType;
        public List<GashaponRewardData> RewardList;
        public List<GashaponExchangeData> ExchangeList;
    }


    public class GashaponBaseData
    {
        public string Id;
        public int Num;
        public string BundleId;
        public Product.RewardType RewardType;


        //客户端业务字段：
        public List<AssetsData> PgcDatas; // 如果奖励是pgc 这里就有值
        public Sprite iconSprite; //如果是特殊类型，需要记录sprite方便展示
    }

    // 扭蛋奖品数据
    public class GashaponRewardData:GashaponBaseData
    {
        public Product.Level Level;
        public string Name;
        public long RewardId;
    }



    public class GashaponExchangeData:GashaponBaseData
    {
        public int ExchangeId;
        public string Name;
        public long Price;
        public CurrencyType CurrencyType;
        public string LotteryId;

        public GashaponExchangeData Clone()
        {
            return new GashaponExchangeData
            {
                ExchangeId = this.ExchangeId,
                RewardType = this.RewardType,
                Num = this.Num,
                Id = this.Id,
                Name = this.Name,
                BundleId = this.BundleId,
                Price = this.Price,
                CurrencyType = this.CurrencyType,
                LotteryId = this.LotteryId,
                PgcDatas = new List<AssetsData>(this.PgcDatas)
            };
        }
        public GashaponExchangeData()
        {
 
        }
        public GashaponExchangeData(int id, string name, long price, CurrencyType curType)
        {
            ExchangeId = id;
            Name = name;
            Price = price;
            CurrencyType = curType;
        }
    }
}
