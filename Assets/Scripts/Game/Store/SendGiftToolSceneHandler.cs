using Es;
using Game.Database;
using GameData.PgcData;

using System.Collections.Generic;
using Product;

namespace Game.Store
{
    public class SendGiftToolSceneHandler : AssetsDataHandler
    {
        protected Dictionary<int, List<GoodsData>> data;
        protected Dictionary<string, GoodsData> goodsDict;
        public override void InitData()
        {
            data = new();
            dict = new();
            goodsDict = new();

            var allList = DataTables.GetAvatarCommonDataList();
            for (int i = allList.Count - 1; i >= 0; i--)
            {
                var tableData = allList[i];
                if (!tableData.PgcId.StartsWith("1")) continue;
                var uniqueType = UniqueType.Get(ResourceType.Avatar, tableData.SubType);
                var list = data.ContainsKey(uniqueType) ? data[uniqueType] : NewList(uniqueType);
                var assetsData = CreatePgcAssetsData(uniqueType, tableData, new InventoryData()
                {
                    Id = tableData.PgcId,
                    ClassType = uniqueType,
                    IsNew = false,
                    OwnedNum = 1
                });
                dict[tableData.PgcId] = assetsData;
                list.Add(CreateGoodsData(uniqueType, tableData.PgcId, assetsData));
                data[uniqueType] = list;
            }

            var emoteList = DataTables.GetEmoUIConfigList();
            for (int i = emoteList.Count - 1; i >= 0; i--)
            {
                var tableData = emoteList[i];
                var uniqueType = UniqueType.Get(ResourceType.Emote, tableData.emoType);
                var list = data.ContainsKey(uniqueType) ? data[uniqueType] : NewList(uniqueType);
                var assetsData = CreateEmoteAssetsData(uniqueType, tableData, new InventoryData()
                {
                    Id = tableData.pgcId,
                    ClassType = uniqueType,
                    IsNew = false,
                    OwnedNum = 1
                });
                dict[tableData.pgcId] = assetsData;
                list.Add(CreateGoodsData(uniqueType, tableData.pgcId, assetsData));
                data[uniqueType] = list;
            }
        }

        protected GoodsData CreateGoodsData(int classType, string id, AssetsData assetsData)
        {
            GoodsData goodsData;
            if (goodsDict.ContainsKey(id)) return goodsDict[id];

            goodsData = new GoodsData();
            goodsData.Id = id;
            goodsData.GoodsType = GoodsType.SinglePgc;
            goodsData.ButtonType = ButtonType.Assets;
            goodsData.IsGiftScene = true;
            goodsData.Assets = new List<AssetsData>()
            {
                assetsData
            };
            goodsData.CantWear = true;
            goodsData.IsOwned = assetsData.InventoryData?.OwnedNum > 0;
            goodsDict.Add(id, goodsData);
            return goodsData;
        }

        protected PGCAssetsData CreatePgcAssetsData(int classType, AvatarCommonData data, InventoryData inventoryData)
        {
            PGCAssetsData assetsData;
            var pgcId = data.PgcId;
            if (dict.ContainsKey(pgcId)) return (PGCAssetsData)dict[pgcId];

            assetsData = new PGCAssetsData();
            assetsData.Id = pgcId;
            //assetsData.Name = data.ugcInfo.name;
            assetsData.ResourceType = ResourceType.Avatar;
            assetsData.AvatarSubType = UniqueType.AvatarSubType(classType);
            var specialConfig = DataTables.GetSpecialSkinConfig(pgcId);
            if (specialConfig != null) assetsData.AvatarSubType = AvatarSubType.SpecialSkin;
            assetsData.InventoryData = inventoryData;
            dict.Add(pgcId, assetsData);
            return assetsData;
        }

        protected EmoteAssetsData CreateEmoteAssetsData(int classType, EmoUIConfig data, InventoryData inventoryData)
        {
            EmoteAssetsData assetsData;
            var pgcId = data.pgcId;
            if (dict.ContainsKey(pgcId)) return (EmoteAssetsData)dict[pgcId];

            assetsData = new EmoteAssetsData();
            assetsData.Id = pgcId;
            //assetsData.Name = data.ugcInfo.name;
            assetsData.ResourceType = ResourceType.Emote;
            assetsData.EmoteSubType = (EmoteSubType)data.emoType;
            assetsData.InventoryData = inventoryData;
            dict.Add(pgcId, assetsData);
            return assetsData;
        }

        public List<GoodsData> NewList(int uniqueType)
        {
            var list = new List<GoodsData>();

            data[uniqueType] = list;

            return list;
        }

        public List<GoodsData> GetGoodsData(int uniqueType)
        {
            return data.ContainsKey(uniqueType) ? data[uniqueType] : NewList(uniqueType);
        }
        
        public List<GoodsData> GetToolsGoodsData(List<GoodsData> list)
        {
            
            var goodsInfoDatas = new List<GoodsData>();

#if UNITY_IOS
      //goodsInfoDatas.Add(new GoodsData()
      //      {
      //          Id = "ios_giftgaojipass",
      //          Name = "高级版通行证",
      //          GoodsType = GoodsType.ToolProduct,
      //          Price = new CurrencyData()
      //          {
      //              CurrencyType = CurrencyType.Gem,
      //              Value = 30
      //          },
      //          GiftType = GiftType.PremiumSeasonPass
      //      });

      //      goodsInfoDatas.Add(new GoodsData()
      //      {
      //          Id = "ios_gifthaohuapass",
      //          Name = "豪华版通行证",
      //          GoodsType = GoodsType.ToolProduct,
      //          Price = new CurrencyData()
      //          {
      //              CurrencyType = CurrencyType.Gem,
      //              Value = 60
      //          },
      //          GiftType = GiftType.DeluxeSeasonPass
      //      });
            
      //      goodsInfoDatas.Add(new GoodsData()
      //      {
      //          Id = "ios_gifthaohuauppass",
      //          Name = "豪华版通行证升级包",
      //          GoodsType = GoodsType.ToolProduct,
      //          Price = new CurrencyData()
      //          {
      //              CurrencyType = CurrencyType.Gem,
      //              Value = 30
      //          },
      //          GiftType = GiftType.AdvancedSeasonPassTier
      //      });
            
            goodsInfoDatas.Add(new GoodsData()
            {
                Id = "ios_giftvip",
                Name = "VIP月卡",
                GoodsType = GoodsType.ToolProduct,
                Price = new CurrencyData()
                {
                    CurrencyType = CurrencyType.Gem,
                    Value = 20
                },
                GiftType = GiftType.MonthlyVip
            });
            //if (isNewYearLive)
            //{
            //    goodsInfoDatas.Add(new GoodsData()
            //    {
            //        Id = "ios_newYearLimitedPack1",
            //        Name = "辞岁烟花礼包",
            //        GoodsType = GoodsType.ToolProduct,
            //        Price = new CurrencyData()
            //        {
            //            CurrencyType = CurrencyType.Gem,
            //            Value = 1080
            //        },
            //        GiftType = GiftType.NewYearLimitedPackage
            //    });
            //    goodsInfoDatas.Add(new GoodsData()
            //    {
            //        Id = "ios_newYearLimitedPack2",
            //        Name = "金蛇贺岁礼包",
            //        GoodsType = GoodsType.ToolProduct,
            //        Price = new CurrencyData()
            //        {
            //            CurrencyType = CurrencyType.Gem,
            //            Value = 1080
            //        },
            //        GiftType = GiftType.NewYearLimitedPackage
            //    });
            //}
            if (list != null && list.Count > 0)
            {
                goodsInfoDatas.AddRange(list);

            }

#elif UNITY_ANDROID
            //goodsInfoDatas.Add(new GoodsData()
            //{
            //    Id = "android_giftgaojipass",
            //    Name = "高级版通行证",
            //    GoodsType = GoodsType.ToolProduct,
            //    Price = new CurrencyData()
            //    {
            //        CurrencyType = CurrencyType.Gem,
            //        Value = 30
            //    },
            //    GiftType = GiftType.PremiumSeasonPass
            //});

            
            //goodsInfoDatas.Add(new GoodsData()
            //{
            //    Id = "android_gifthaohuapass",
            //    Name = "豪华版通行证",
            //    GoodsType = GoodsType.ToolProduct,
            //    Price = new CurrencyData()
            //    {
            //        CurrencyType = CurrencyType.Gem,
            //        Value = 60
            //    },
            //    GiftType = GiftType.DeluxeSeasonPass
            //});
            
            //goodsInfoDatas.Add(new GoodsData()
            //{
            //    Id = "android_gifthaohuauppass",
            //    Name = "豪华版通行证升级包",
            //    GoodsType = GoodsType.ToolProduct,
            //    Price = new CurrencyData()
            //    {
            //        CurrencyType = CurrencyType.Gem,
            //        Value = 30
            //    },
            //    GiftType = GiftType.AdvancedSeasonPassTier
            //});
            
            goodsInfoDatas.Add(new GoodsData()
            {
                Id = "android_giftvip",
                Name = "VIP月卡",
                GoodsType = GoodsType.ToolProduct,
                Price = new CurrencyData()
                {
                    CurrencyType = CurrencyType.Gem,
                    Value = 20
                },
                GiftType = GiftType.MonthlyVip
            });
            if (list != null && list.Count > 0)
            {
                goodsInfoDatas.AddRange(list);
            }
            //    goodsInfoDatas.Add(new GoodsData()           
            //    {
            //        Id = "android_newYearLimitedPack1",
            //        Name = "辞岁烟花礼包",
            //        GoodsType = GoodsType.ToolProduct,
            //        Price = new CurrencyData()
            //        {
            //            CurrencyType = CurrencyType.Gem,
            //            Value = 1080
            //        },
            //        GiftType = GiftType.NewYearLimitedPackage
            //    });
            //    goodsInfoDatas.Add(new GoodsData()
            //    {
            //        Id = "android_newYearLimitedPack2",
            //        Name = "金蛇贺岁礼包",
            //        GoodsType = GoodsType.ToolProduct,
            //        Price = new CurrencyData()
            //        {
            //            CurrencyType = CurrencyType.Gem,
            //            Value = 1080
            //        },
            //        GiftType = GiftType.NewYearLimitedPackage
            //    });
            //}

#endif


                return goodsInfoDatas;
        }

    }
}
