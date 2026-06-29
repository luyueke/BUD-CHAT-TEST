using Basic.Utils;
using Es;
using Game.Database;
using GameData.PgcData;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Basic.Extensions;
using Product;
using UnityEngine;

namespace Game.Store
{
    public class BUDSeriesData
    {
        public Product.StoreSeriesData ServerData;
        public List<GoodsData> GoodsList;
    }

    public class BUDShapePropData
    {
        public ShapeThemeProp ServerData;
        public List<GoodsData> GoodsList;
    }

    public class BudGashaponData
    {
        public string lotteryId;
    }


    public class GiftMallHandler : AvatarBUDSceneHandler
    {
        public Dictionary<string, GoodsData> bundleDataList;

        public override void InitData()
        {
            data = new();
            dict = new();
            goodsDict = new();
            sectionsDict = new();
            seriesList = new();
            bundleDataList = new();
            shapePropDict = new();
            var sList = AssetsDataManager.StoreData.SeriesList;
            for (int i = 0, C = sList.Count; i < C; i++)
            {
                seriesList.Add(sList[i].Id, new BUDSeriesData()
                {
                    ServerData = sList[i],
                    GoodsList = new()
                });
            }

            var productList = AssetsDataManager.StoreData.GiftList;
            // 商城资源
            for (int i = 0, C = productList.Count; i < C; i++)
            {
                var productData = productList[i];
                Product.AssetData assetData;
                switch (productData.ProductType)
                {
                    case Product.ProductType.Avatar:
                        if (productData.AssetDataList.Count != 1) return;
                        assetData = productData.AssetDataList[0];
                        SingleAvatarGoods(productData, assetData, i, true);
                        break;
                    case Product.ProductType.Emote:
                        if (productData.AssetDataList.Count != 1) return;
                        assetData = productData.AssetDataList[0];
                        SingleEmoteGoods(productData, assetData, i);
                        break;
                }
            }
        }

        protected override void AddBundleList(Product.StoreGiftData productData, AssetsData assetData)
        {
            if (!bundleDataList.ContainsKey(productData.BundleId))
            {
                bundleDataList.Add(productData.BundleId, new GoodsData
                {
                    Id = productData.BundleId,
                    GoodsType = GoodsType.BundlePgc,
                    ButtonType = ButtonType.Assets,
                    Name = productData.AssetDataList.IsNullOrEmpty() ? "" : productData.AssetDataList[0].Name,
                    Assets = new List<AssetsData>() { },
                    GiftType = productData.GiftType,
                    Price = new CurrencyData()
                    {
                        CurrencyType = productData.AssetDataList.IsNullOrEmpty() ? CurrencyType.Gem : (CurrencyType)productData.AssetDataList[0].PurchaseType,
                        Value = productData.AssetDataList.IsNullOrEmpty() ? 0: (int)productData.AssetDataList[0].Price
                    }
                });
            }

            var bundleGoodsData = bundleDataList[productData.BundleId];
            bundleGoodsData.Assets.Add(assetData);

            var uniqueType = UniqueType.GetAvatar(AvatarSubType.Bundle);
            var list = data.ContainsKey(uniqueType) ? data[uniqueType] : NewList(uniqueType);
            if (!list.ContainsKey(bundleGoodsData.Id)) list.Add(bundleGoodsData.Id, bundleGoodsData);
            data[uniqueType] = list;
        }

        public string GetPgcName(string pgcId)
        {
            if (dict == null)
            {
                return "";
            }

            if (!dict.TryGetValue(pgcId, out var assetData)) return "";
            return assetData != null ? assetData.Name : "";
        }
    }
    
    
    public class AIBuddyGiftHandler : AvatarBUDSceneHandler
    {
        protected Dictionary<string, GoodsData> giftDict;
        public override void InitData()
        {
            data = new();
            dict = new();
            goodsDict = new();
            sectionsDict = new();
            seriesList = new();
            shapePropDict = new();
            giftDict = new Dictionary<string, GoodsData>();

            var productList = AssetsDataManager.StoreData.BuddyGiftList;
            // 商城资源
            for (int i = 0, C = productList.Count; i < C; i++)
            {
                var productData = productList[i];
                AssetData assetData;
                switch (productData.ProductType)
                {
                    case ProductType.Emote:
                        if (productData.AssetDataList.Count != 1) return;
                        assetData = productData.AssetDataList[0];
                        SingleEmoteGoods(productData, assetData, i);
                        break;
                }
            }
        }
        
        protected virtual void SingleEmoteGoods(StoreBuddyGiftData productData, AssetData serverAssetsData,
            int sortIndex)
        {
            var pgcId = serverAssetsData.PgcId.ToString();
            var tableData = DataTables.GetEmoUIConfig(pgcId);
            if (tableData == null) return;
            var uniqueType = UniqueType.Get(ResourceType.Emote, (int)EmoteSubType.SingleAll);
            var list = data.ContainsKey(uniqueType) ? data[uniqueType] : NewList(uniqueType);
            if (list.ContainsKey(pgcId))
            {
                // 列表中已经有相同物体
                return;
            }

            InventoryData inventoryData = BagDatabase.Inst.Select(pgcId);
            var assetsData = CreateEmoteAssetsData(serverAssetsData, tableData, inventoryData);
            dict[tableData.pgcId] = assetsData;
            var goodsData = CreateSingleGoodsData(productData, serverAssetsData, pgcId, assetsData, sortIndex);
            goodsData.CantWear = true;
            list.Add(pgcId, goodsData);
            data[uniqueType] = list;
        }
        
        protected virtual GoodsData CreateSingleGoodsData(StoreBuddyGiftData productData,
            AssetData serverAssetsData, string id, AssetsData assetsData, int sortIndex)
        {
            GoodsData goodsData;
            if (giftDict.ContainsKey(id)) return giftDict[id];
            goodsData = new GoodsData
            {
                Id = productData.ProductId,
                SortIndex = sortIndex,
                GoodsType = GoodsType.SinglePgc,
                ButtonType = ButtonType.Assets,
                Name = serverAssetsData.Name,
                Assets = new List<AssetsData>() { assetsData },
                GiftType = productData.GiftType,
                Discount = productData.Discount,
                OriginalPrice = new CurrencyData()
                {
                    CurrencyType = (CurrencyType)serverAssetsData.PurchaseType,
                    Value = (int)serverAssetsData.Price
                },
                Price = new CurrencyData()
                {
                    CurrencyType = (CurrencyType)serverAssetsData.PurchaseType,
                    Value = (int)serverAssetsData.Price
                }
            };
            giftDict.Add(id, goodsData);
            return goodsData;
        }
        
        public List<GoodsData> GetAIBuddyGiftList()
        {
            var uniqueType = UniqueType.Get(ResourceType.Emote,  (int)EmoteSubType.SingleAll);
            var srcList = data.ContainsKey(uniqueType) ? data[uniqueType].Values.ToList() : NewList(uniqueType).Values.ToList();
            return srcList;
        }
    }


    public class CreatorRewardHandler : AvatarBUDSceneHandler
    {
        public override void InitData()
        {
            data = new();
            dict = new();
            goodsDict = new();
            sectionsDict = new();
            seriesList = new();
            shapePropDict = new();
            var sList = AssetsDataManager.StoreData.SeriesList;
            for (int i = 0, C = sList.Count; i < C; i++)
            {
                seriesList.Add(sList[i].Id, new BUDSeriesData()
                {
                    ServerData = sList[i],
                    GoodsList = new()
                });
            }

            var productList = AssetsDataManager.StoreData.CreatorExchangeList;
            // 商城资源
            for (int i = 0, C = productList.Count; i < C; i++)
            {
                var productData = productList[i];
                Product.AssetData assetData;
                switch (productData.ProductType)
                {
                    case Product.ProductType.Avatar:
                        if (productData.AssetDataList.Count != 1) return;
                        assetData = productData.AssetDataList[0];
                        SingleAvatarGoods(productData, assetData, i, true);
                        break;
                    case Product.ProductType.Emote:
                        if (productData.AssetDataList.Count != 1) return;
                        assetData = productData.AssetDataList[0];
                        SingleEmoteGoods(productData, assetData, i);
                        break;
                }
            }
        }
    }

    public class AvatarBUDSceneHandler : AssetsDataHandler
    {
        protected Dictionary<int, Dictionary<string, GoodsData>> data;
        protected Dictionary<int, List<UgcSectionData>> sectionsDict;
        protected Dictionary<string, GoodsData> goodsDict;
        protected Dictionary<int, BUDSeriesData> seriesList;
        protected Dictionary<int, BUDShapePropData> shapePropDict;

        public override void InitData()
        {
            data = new();
            dict = new();
            goodsDict = new();
            sectionsDict = new();
            seriesList = new();
            shapePropDict = new();

            var sList = AssetsDataManager.StoreData.SeriesList;
            for (int i = 0, C = sList.Count; i < C; i++)
            {
                seriesList.Add(sList[i].Id, new BUDSeriesData()
                {
                    ServerData = sList[i],
                    GoodsList = new()
                });
            }

            var shapeList = AssetsDataManager.ShapeThemePropList;
            if(shapeList != null)
            {
                for (int i = 0, C = shapeList.Count; i < C; i++)
                {
                    shapePropDict.Add(shapeList[i].themeId, new BUDShapePropData()
                    {
                        ServerData = shapeList[i],
                        GoodsList = new()
                    });
                }
            }

            var productList = AssetsDataManager.StoreData.ProductList;
            // 商城资源
            for (int i = 0, C = productList.Count; i < C; i++)
            {
                var productData = productList[i];
                Product.AssetData assetData;
                switch (productData.ProductType)
                {
                    case Product.ProductType.Avatar:
                        if (productData.AssetDataList.Count != 1) return;
                        assetData = productData.AssetDataList[0];
                        SingleAvatarGoods(productData, assetData, i, true);
                        break;
                    case Product.ProductType.Emote:
                        if (productData.AssetDataList.Count != 1) return;
                        assetData = productData.AssetDataList[0];
                        SingleEmoteGoods(productData, assetData, i);
                        break;
                    case (ProductType)ResourceType.Vehicle:
                        if (productData.AssetDataList.Count != 1) return;
                        assetData = productData.AssetDataList[0];
                        SingleVehicleGoods(productData, assetData, i);
                        break;
                }
            }
        }

        protected virtual void SingleAvatarGoods(Product.StoreGiftData productData, Product.AssetData serverAssetsData,
            int sortIndex, bool canWear = false)
        {
            var pgcId = serverAssetsData.PgcId.ToString();
            var tableData = DataTables.GetAvatarCommonData(pgcId);
            if (tableData == null) return;
            var uniqueType = UniqueType.Get(ResourceType.Avatar, tableData.SubType);
            var list = data.ContainsKey(uniqueType) ? data[uniqueType] : NewList(uniqueType);
            if (list.ContainsKey(pgcId))
            {
                // 列表中已经有相同物体
                return;
            }

            InventoryData inventoryData = BagDatabase.Inst.Select(pgcId);
            var assetsData = CreatePgcAssetsData(uniqueType, serverAssetsData, tableData, inventoryData);
            if (string.IsNullOrEmpty(productData.BundleId))
            {
                dict[tableData.PgcId] = assetsData;
                var goodsData = CreateSingleGoodsData(productData, serverAssetsData, pgcId, assetsData, sortIndex);
                goodsData.CantWear = canWear;
                list.Add(pgcId, goodsData);
                data[uniqueType] = list;
            }

            if (!string.IsNullOrEmpty(productData.BundleId))
            {
                AddBundleList(productData, assetsData);
            }
        }

        protected virtual void AddBundleList(Product.StoreGiftData productData, AssetsData data)
        {
        }

        protected virtual void SingleAvatarGoods(Product.StoreProductData productData,
            Product.AssetData serverAssetsData, int sortIndex, bool canWear = false)
        {
            var pgcId = serverAssetsData.PgcId.ToString();
            var tableData = DataTables.GetAvatarCommonData(pgcId);
            if (tableData == null) return;

            var uniqueType = UniqueType.Get(ResourceType.Avatar, tableData.SubType);

            var specialData = DataTables.GetSpecialSkinConfig(pgcId);
            GoodsData goodsData;
            GoodsData createData()
            {
                InventoryData inventoryData = BagDatabase.Inst.Select(pgcId);
                var assetsData = CreatePgcAssetsData(uniqueType, serverAssetsData, tableData, inventoryData);
                dict[tableData.PgcId] = assetsData;
                var goodsData = CreateSingleGoodsData(productData, serverAssetsData, pgcId, assetsData, sortIndex);
                goodsData.CantWear = canWear;
                return goodsData;
            }
            if (specialData != null)
            {
                var specialUniqueType = UniqueType.GetAvatar(AvatarSubType.SpecialSkin);
                var list1 = data.ContainsKey(specialUniqueType) ? data[specialUniqueType] : NewList(specialUniqueType);
                if (list1.ContainsKey(pgcId))
                {
                    // 列表中已经有相同物体
                    return;
                }
                goodsData = createData();
                list1.Add(pgcId, goodsData);
                data[specialUniqueType] = list1;
            }
            else
            {
                var list = data.ContainsKey(uniqueType) ? data[uniqueType] : NewList(uniqueType);
                if (list.ContainsKey(pgcId))
                {
                    // 列表中已经有相同物体
                    return;
                }
                goodsData = createData();
                list.Add(pgcId, goodsData);
                data[uniqueType] = list;
            }

            if (goodsData != null && productData.SeriesId > 0)
            {
                if (seriesList.TryGetValue(productData.SeriesId, out var seriesData))
                {
                    seriesData.GoodsList.Add(goodsData);
                }
            }

            if(goodsData != null && shapePropDict != null)
            {
                foreach(var item in shapePropDict)
                {
                    for(int i = 0 ; i < item.Value.ServerData.products.Count; i++)
                    {
                        if (item.Value.ServerData.products[i].ToString() == productData.AssetDataList[0].PgcId)
                        {
                            item.Value.GoodsList.Add(goodsData);
                        }
                    }
                }
            }
        }

        protected virtual void SingleEmoteGoods(Product.StoreGiftData productData, Product.AssetData serverAssetsData,
            int sortIndex)
        {
            var pgcId = serverAssetsData.PgcId.ToString();
            var tableData = DataTables.GetEmoUIConfig(pgcId);
            if (tableData == null) return;
            var uniqueType = UniqueType.Get(ResourceType.Emote, tableData.emoType);
            var list = data.ContainsKey(uniqueType) ? data[uniqueType] : NewList(uniqueType);
            if (list.ContainsKey(pgcId))
            {
                // 列表中已经有相同物体
                return;
            }

            InventoryData inventoryData = BagDatabase.Inst.Select(pgcId);
            var assetsData = CreateEmoteAssetsData(serverAssetsData, tableData, inventoryData);
            dict[tableData.pgcId] = assetsData;
            var goodsData = CreateSingleGoodsData(productData, serverAssetsData, pgcId, assetsData, sortIndex);
            goodsData.CantWear = true;
            list.Add(pgcId, goodsData);
            data[uniqueType] = list;

            var allUniqueType = UniqueType.Get(ResourceType.Emote, Mathf.CeilToInt(tableData.emoType / 2f) * 100);
            var allList = data.ContainsKey(allUniqueType) ? data[allUniqueType] : NewList(allUniqueType);
            allList.Add(pgcId, goodsData);
            data[allUniqueType] = allList;
        }

        protected virtual void SingleEmoteGoods(Product.StoreProductData productData,
            Product.AssetData serverAssetsData, int sortIndex)
        {
            var pgcId = serverAssetsData.PgcId.ToString();
            var tableData = DataTables.GetEmoUIConfig(pgcId);
            if (tableData == null) return;
            var uniqueType = UniqueType.Get(ResourceType.Emote, tableData.emoType);
            var list = data.ContainsKey(uniqueType) ? data[uniqueType] : NewList(uniqueType);
            if (list.ContainsKey(pgcId))
            {
                // 列表中已经有相同物体
                return;
            }

            InventoryData inventoryData = BagDatabase.Inst.Select(pgcId);
            var assetsData = CreateEmoteAssetsData(serverAssetsData, tableData, inventoryData);
            dict[tableData.pgcId] = assetsData;
            var goodsData = CreateSingleGoodsData(productData, serverAssetsData, pgcId, assetsData, sortIndex);
            goodsData.CantWear = true;
            list.Add(pgcId, goodsData);
            data[uniqueType] = list;

            var allUniqueType = UniqueType.Get(ResourceType.Emote, Mathf.CeilToInt(tableData.emoType / 2f) * 100);
            var allList = data.ContainsKey(allUniqueType) ? data[allUniqueType] : NewList(allUniqueType);
            allList.Add(pgcId, goodsData);
            data[allUniqueType] = allList;

            if (goodsData != null && shapePropDict != null)
            {
                foreach (var item in shapePropDict)
                {
                    for (int i = 0; i < item.Value.ServerData.products.Count; i++)
                    {
                        if (item.Value.ServerData.products[i].ToString() == productData.AssetDataList[0].PgcId)
                        {
                            item.Value.GoodsList.Add(goodsData);
                        }
                    }
                }
            }

        }

        protected virtual void SingleVehicleGoods(Product.StoreProductData productData, Product.AssetData serverAssetsData, int sortIndex)
        {
            var pgcId = serverAssetsData.PgcId.ToString();
            var tableData = DataTables.GetVehicleUIConfig(pgcId);
            if (tableData == null) return;
            var uniqueType = UniqueType.Get(ResourceType.Vehicle, tableData.vehicleType);
            var list = data.ContainsKey(uniqueType) ? data[uniqueType] : NewList(uniqueType);
            if (list.ContainsKey(pgcId))
            {
                // 列表中已经有相同物体
                return;
            }
            InventoryData inventoryData = BagDatabase.Inst.Select(pgcId);
            var assetsData = CreateVehicleAssetsData(serverAssetsData, tableData, inventoryData);
            dict[tableData.pgcId] = assetsData;
            var goodsData = CreateSingleGoodsData(productData, serverAssetsData, pgcId, assetsData, sortIndex);
          //  goodsData.SourceData.Source = Source.Mall;
            goodsData.CantWear = false;
            list.Add(pgcId, goodsData);
            data[uniqueType] = list;
        }

        protected virtual VehicleAssetsData CreateVehicleAssetsData(Product.AssetData serverAssetsData, VehicleUIConfig data,
            InventoryData inventoryData)
        {
            VehicleAssetsData assetsData;
            var pgcId = data.pgcId.ToString();
            if (dict.ContainsKey(pgcId)) return (VehicleAssetsData)dict[pgcId];
            assetsData = new VehicleAssetsData();
            assetsData.Id = pgcId;
            assetsData.Name = serverAssetsData.Name;
            assetsData.ResourceType = ResourceType.Vehicle;
            assetsData.VehicleSubType = (VehicleSubType)data.vehicleType;
            assetsData.InventoryData = inventoryData;
            assetsData.Value = new CurrencyData()
            {
                CurrencyType = (CurrencyType)serverAssetsData.PurchaseType,
                Value = (int)serverAssetsData.Price
            };
            dict.Add(pgcId, assetsData);
            return assetsData;
        }

        protected virtual EmoteAssetsData CreateEmoteAssetsData(Product.AssetData serverAssetsData, EmoUIConfig data,
            InventoryData inventoryData)
        {
            EmoteAssetsData assetsData;
            var pgcId = data.pgcId.ToString();
            if (dict.ContainsKey(pgcId)) return (EmoteAssetsData)dict[pgcId];

            assetsData = new EmoteAssetsData();
            assetsData.Id = pgcId;
            assetsData.Name = serverAssetsData.Name;
            assetsData.ResourceType = ResourceType.Emote;
            assetsData.EmoteSubType = (EmoteSubType)data.emoType;
            assetsData.InventoryData = inventoryData;
            assetsData.Value = new CurrencyData()
            {
                CurrencyType = (CurrencyType)serverAssetsData.PurchaseType,
                Value = (int)serverAssetsData.Price
            };
            dict.Add(pgcId, assetsData);
            return assetsData;
        }

        protected virtual GoodsData CreateSingleGoodsData(Product.StoreGiftData productData,
            Product.AssetData serverAssetsData, string id, AssetsData assetsData, int sortIndex)
        {
            GoodsData goodsData;
            if (goodsDict.ContainsKey(id)) return goodsDict[id];
            goodsData = new GoodsData
            {
                Id = productData.ProductId,
                SortIndex = sortIndex,
                GoodsType = GoodsType.SinglePgc,
                ButtonType = ButtonType.Assets,
                Name = serverAssetsData.Name,
                Assets = new List<AssetsData>() { assetsData },
                GiftType = productData.GiftType,
            };
            goodsData.Update();
            goodsDict.Add(id, goodsData);
            return goodsData;
        }

        protected virtual GoodsData CreateSingleGoodsData(Product.StoreProductData productData,
            Product.AssetData serverAssetsData, string id, AssetsData assetsData, int sortIndex)
        {
            GoodsData goodsData;
            if (goodsDict.ContainsKey(id)) return goodsDict[id];
            goodsData = new GoodsData
            {
                Id = id,
                SortIndex = sortIndex,
                ProductId = productData.ProductId,
                GoodsType = GoodsType.SinglePgc,
                ButtonType = ButtonType.Assets,
                Name = serverAssetsData.Name,
                SourceData = new SourceData() { Source = GetSource(productData.SkipType), Id = productData.SkipData },
                Assets = new List<AssetsData>() { assetsData },
                EndTime = productData.EndTime,
            };
            goodsData.Update();
            goodsDict.Add(id, goodsData);
            return goodsData;
        }

        protected Source GetSource(Product.StoreSkipType storeSkipType)
        {
            switch (storeSkipType)
            {
                case Product.StoreSkipType.Item:
                    return Source.Mall;
                case Product.StoreSkipType.CreatorExchange:
                    return Source.CreatorReward;
                case Product.StoreSkipType.Lottery:
                    return Source.Gashapon;
                case Product.StoreSkipType.SeasonPass:
                    return Source.SeasonPass;
                case Product.StoreSkipType.GiftPack:
                    return Source.Gift;
                case Product.StoreSkipType.VipReward:
                    return Source.VIP;
                case Product.StoreSkipType.StarterQuest:
                    return Source.BeginnerTask;
                case Product.StoreSkipType.BalloonActivity:
                case Product.StoreSkipType.LimitedEvents:
                    return Source.Activity;
                case Product.StoreSkipType.Task:
                    return Source.Task;
                case StoreSkipType.HotSales:
                    return Source.HotSales;
                default:
                    return Source.Jump;
            }
        }

        protected override void BeforeChangeInvoke(List<AssetsData> changed)
        {
            try
            {
                // 先收集所有的键，避免在遍历时修改集合
                var keys = goodsDict.Keys.ToList();
                
                foreach (var key in keys)
                {
                    if (!goodsDict.ContainsKey(key)) continue;
                    
                    var goodsData = goodsDict[key];
                    goodsData.Update();
                }
            }
            catch (Exception e)
            {
                LoggerUtils.LogError($"BeforeChangeInvoke error: {e.Message}");
            }
        }


        protected override void AfterChangeInvoke(List<AssetsData> changed)
        {
            try
            {
                // 先收集所有的键，避免在遍历时修改集合
                var keys = data.Keys.ToList();
                
                foreach (var uniqueType in keys)
                {
                    if (!data.ContainsKey(uniqueType)) continue;
                    
                    var goodsList = data[uniqueType].Values.ToList();
                    goodsList.Sort((a, b) =>
                    {
                        var ao = a.IsOwned ? 1 : 0;
                        var bo = b.IsOwned ? 1 : 0;
                        return ao == bo
                            ? (a.SortIndex == b.SortIndex ? 0 : a.SortIndex > b.SortIndex ? 1 : -1)
                            : (ao > bo ? 1 : -1);
                    });
                    
                    // 重新构建字典以保持排序
                    var sortedDict = new Dictionary<string, GoodsData>();
                    foreach (var goods in goodsList)
                    {
                        sortedDict[goods.Id] = goods;
                    }
                    data[uniqueType] = sortedDict;
                }
            }
            catch (Exception e)
            {
                LoggerUtils.LogError($"AfterChangeInvoke error: {e.Message}");
            }
        }

        protected virtual PGCAssetsData CreatePgcAssetsData(int classType, Product.AssetData serverAssetsData,
            AvatarCommonData tableData, InventoryData inventoryData)
        {
            PGCAssetsData assetsData;
            var pgcId = tableData.PgcId;
            if (dict.ContainsKey(pgcId)) return (PGCAssetsData)dict[pgcId];

            assetsData = new PGCAssetsData();
            assetsData.Id = pgcId;
            assetsData.Name = serverAssetsData.Name;
            assetsData.Value = new CurrencyData()
            {
                CurrencyType = (CurrencyType)serverAssetsData.PurchaseType,
                Value = (int)serverAssetsData.Price,
            };
            assetsData.ResourceType = ResourceType.Avatar;
            assetsData.AvatarSubType = UniqueType.AvatarSubType(classType);
            var specialConfig = DataTables.GetSpecialSkinConfig(pgcId);
            if (specialConfig != null) assetsData.AvatarSubType = AvatarSubType.SpecialSkin;
            assetsData.InventoryData = inventoryData;
            dict.Add(pgcId, assetsData);
            return assetsData;
        }

        public Dictionary<string, GoodsData> NewList(int uniqueType)
        {
            var list = new Dictionary<string, GoodsData>();

            data[uniqueType] = list;

            return list;
        }

        public BUDShapePropData GetShapeGoods(int themeId)
        {
            return shapePropDict.ContainsKey(themeId) ? shapePropDict[themeId] : null;
        }

        public List<GoodsData> GetGiftMallGoods(int uniqueType)
        {
            var result = data.ContainsKey(uniqueType) ? data[uniqueType].Values.ToList() : NewList(uniqueType).Values.ToList();
            // result = FilterGoodsLive(result);
            return result;
        }


        public List<GoodsData> GetGoodsData(int uniqueType)
        {
            // Vehicle 的 AllVehicle 在构建商品列表时没有像 Emote 那样同步写入 allList，
            // 因此这里需要动态把各 VehicleSubType 的数据合并出来。
            if (uniqueType == UniqueType.Get(ResourceType.Vehicle, (int)VehicleSubType.AllVehicle))
            {
                var merged = new Dictionary<string, GoodsData>();

                void MergeType(int type)
                {
                    var dict = data.ContainsKey(type) ? data[type] : NewList(type);
                    foreach (var kv in dict)
                    {
                        var key = kv.Key;
                        if (string.IsNullOrEmpty(key)) key = kv.Value?.Id;
                        if (string.IsNullOrEmpty(key)) continue;
                        if (!merged.ContainsKey(key)) merged.Add(key, kv.Value);
                    }
                }

                // 如果 AllVehicle 自己已经有数据，也一并纳入（兼容未来可能的写入方式）
                MergeType(uniqueType);
                MergeType(UniqueType.Get(ResourceType.Vehicle, (int)VehicleSubType.SingleVehicle));
                MergeType(UniqueType.Get(ResourceType.Vehicle, (int)VehicleSubType.DoubleVehicle));
                var resultAll = merged.Values.ToList();
                // 与 AfterChangeInvoke 的排序逻辑保持一致：未拥有优先，其次 SortIndex 升序
                resultAll.Sort((a, b) =>
                {
                    var ao = a.IsOwned ? 1 : 0;
                    var bo = b.IsOwned ? 1 : 0;
                    return ao == bo
                        ? (a.SortIndex == b.SortIndex ? 0 : a.SortIndex > b.SortIndex ? 1 : -1)
                        : (ao > bo ? 1 : -1);
                });
                return FilterGoodsLive(resultAll);
            }

            var result = data.ContainsKey(uniqueType) ? data[uniqueType].Values.ToList() : NewList(uniqueType).Values.ToList();
            result = FilterGoodsLive(result);
            return result;
        }

        private List<GoodsData> FilterGoodsLive(List<GoodsData> srcList)
        {
            //不影响源数据，拷贝一份
            List<GoodsData> resultList = new List<GoodsData>(srcList);
            for (int i = resultList.Count - 1; i >= 0; i--)
            {
                var goodsData = resultList[i];
                if (goodsData.SourceData.Source == Source.Gashapon)
                {
                    if (!BusinessLiveManager.Inst.IsGashaponLive(goodsData.SourceData.Id))
                    {
                        resultList.Remove(goodsData);
                        // LoggerUtils.Log("###过滤扭蛋："+goodsData.SourceData.Id);
                    }
                }
                else if (goodsData.SourceData.Source == Source.Gift)
                {
                    if (!BusinessLiveManager.Inst.IsPaidPackLive(goodsData.SourceData.Id))
                    {
                        resultList.Remove(goodsData);
                        // LoggerUtils.Log("###过滤礼包："+goodsData.SourceData.Id);
                    }
                }
                else if (goodsData.SourceData.Source == Source.Activity)
                {
                    if (!BusinessLiveManager.Inst.IsActivityLive(goodsData.SourceData.Id))
                    {
                        resultList.Remove(goodsData);
                        // LoggerUtils.Log("###过滤活动："+goodsData.SourceData.Id);
                    }
                }
                else if (goodsData.SourceData.Source == Source.CreatorReward)
                {
                    if (!BusinessLiveManager.Inst.IsSeriesLive(goodsData.SourceData.Id))
                    {
                        resultList.Remove(goodsData);
                        // LoggerUtils.Log("###过滤创作者系列："+goodsData.SourceData.Id);
                    }
                }
            }

            return resultList;
        }

        public List<UgcSectionData> GetGiftMallSections(int classType)
        {
            if (sectionsDict.ContainsKey(classType)) return sectionsDict[classType];

            var list = new List<UgcSectionData>();
            list.Add(new UgcSectionData() { sectionId = "All", sectionName = "全部" });

            sectionsDict.Add(classType, list);
            var goods = GetGiftMallGoods(classType);
            if (goods == null) return list;
            HashSet<CurrencyType> currencyTypeHash = new();
            goods.ForEach(g => currencyTypeHash.Add(g.Price.CurrencyType));
            if (currencyTypeHash.Contains(CurrencyType.Gem))
                list.Add(new UgcSectionData() { sectionId = "Gem", sectionName = "钻石" });
            if (currencyTypeHash.Contains(CurrencyType.Badge))
                list.Add(new UgcSectionData() { sectionId = "Badge", sectionName = "徽章" });
            if (currencyTypeHash.Contains(CurrencyType.Coin))
                list.Add(new UgcSectionData() { sectionId = "Coin", sectionName = "金币" });

            return list;
        }

        public List<UgcSectionData> GetSections(int classType)
        {
            if (sectionsDict.ContainsKey(classType)) return sectionsDict[classType];

            var list = new List<UgcSectionData>();
            list.Add(new UgcSectionData() { sectionId = "All", sectionName = "全部" });

            sectionsDict.Add(classType, list);
            var goods = GetGoodsData(classType);
            if (goods == null) return list;
            HashSet<CurrencyType> currencyTypeHash = new();
            goods.ForEach(g => currencyTypeHash.Add(g.Price.CurrencyType));
            if (currencyTypeHash.Contains(CurrencyType.Gem))
                list.Add(new UgcSectionData() { sectionId = "Gem", sectionName = "钻石" });
            if (goods.Exists(tmp => tmp.SourceData.Source != Source.Mall))
            {
                // 包含非直氪商品，则添加特殊商品栏
                list.Add(new UgcSectionData() { sectionId = "Special", sectionName = "特殊奖品" });
            }

            if (currencyTypeHash.Contains(CurrencyType.Badge))
                list.Add(new UgcSectionData() { sectionId = "Badge", sectionName = "徽章" });
            if (currencyTypeHash.Contains(CurrencyType.Coin))
                list.Add(new UgcSectionData() { sectionId = "Coin", sectionName = "金币" });

            return list;
        }

        public List<BUDSeriesData> GetSeriesList(Product.SeriesType seriesType)
        {
            var list = new List<BUDSeriesData>();
            foreach (var kv in seriesList)
            {
                if (kv.Value.ServerData.SeriesType == seriesType)
                {
                    list.Add(kv.Value);
                  //  LoggerUtils.LogError($"Found series: {kv.Value.ServerData.Id}, Type: {kv.Value.ServerData.SeriesType}");
                }
            }
            list = FilterSeriesLive(list);
           // LoggerUtils.LogError($"After filter: {list.Count} series for type {seriesType}");
            return list;
        }

        private List<BUDSeriesData> FilterSeriesLive(List<BUDSeriesData> list)
        {
            if (list == null || list.Count <= 0) return list;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var series = list[i];
                if (!BusinessLiveManager.Inst.IsSeriesLive(series.ServerData.Id.ToString()))
                {
                    list.Remove(series);
                    LoggerUtils.Log("###过滤系列：" + series.ServerData.Id);
                }
            }

            return list;
        }
    }
}