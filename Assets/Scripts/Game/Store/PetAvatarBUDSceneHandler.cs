using System.Collections.Generic;
using Es;
using Game.Database;
using GameData.PgcData;
using Google.Protobuf.Collections;
using Product;

namespace Game.Store {
    public class PetAvatarBUDSceneHandler : AvatarBUDSceneHandler {
        public override void InitData()
        {
            data = new();
            dict = new();
            goodsDict = new();
            sectionsDict = new();
            seriesList = new();
            shapePropDict = new();

            var sList = AssetsDataManager.StoreData.PetSeriesList;
            for (int i = 0, C = sList.Count; i < C; i++)
            {
                seriesList.Add(sList[i].Id, new BUDSeriesData()
                {
                    ServerData = sList[i],
                    GoodsList = new()
                });
            }

            var productList = AssetsDataManager.StoreData.PetProductList;
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
                        SingleAvatarGoods(productData, assetData, i);
                        break;
                    case Product.ProductType.Emote:
                        if (productData.AssetDataList.Count != 1) return;
                        assetData = productData.AssetDataList[0];
                        SingleEmoteGoods(productData, assetData, i);
                        break;
                }
            }
        }

        protected override void SingleAvatarGoods(StoreProductData productData, AssetData serverAssetsData, int sortIndex, bool canWear = false) {
            var pgcId = serverAssetsData.PgcId.ToString();
            var tableData = DataTables.GetPetAvatarCommonData(pgcId);
            if (tableData == null) return;
            var uniqueType = UniqueType.Get(ResourceType.PGCPetAvatar, tableData.SubType);
            var list = data.ContainsKey(uniqueType) ? data[uniqueType] : NewList(uniqueType);
            if (list.ContainsKey(pgcId))
            {
                // 列表中已经有相同物体
                return;
            }
            InventoryData inventoryData = BagDatabase.Inst.Select(pgcId);
            var assetsData = CreatePgcAssetsData(uniqueType, serverAssetsData, tableData, inventoryData);
            dict[tableData.PgcId] = assetsData;
            var goodsData = CreateSingleGoodsData(productData, serverAssetsData, pgcId, assetsData, sortIndex);
            goodsData.CantWear = canWear;
            list.Add(pgcId, goodsData);
            if (productData.SeriesId > 0)
            {
                if (seriesList.TryGetValue(productData.SeriesId, out var seriesData))
                {
                    seriesData.GoodsList.Add(goodsData);
                }
            }
            data[uniqueType] = list;
        }

        protected override PGCAssetsData CreatePgcAssetsData(int classType, AssetData serverAssetsData, AvatarCommonData tableData,
            InventoryData inventoryData) {
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
            assetsData.ResourceType = ResourceType.PGCPetAvatar;
            assetsData.AvatarSubType = UniqueType.AvatarSubType(classType);
            assetsData.InventoryData = inventoryData;
            dict.Add(pgcId, assetsData);
            return assetsData;
        }
    }
}
