using Es;
using Game.Database;
using GameData.Base;
using GameData.BaseInfo;
using GameData.PgcData;
using Product;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Store
{
    public class OcSkinInfo
    {
        public string skinId;
        public int isPgc;
        public string coverUrl;
        public PaymentInfo paymentInfo;
        public int isPrivateOrder;
    }

    public class OcContestHandler : AssetsDataHandler
    {
        private Dictionary<string, GoodsData> goodsDict;
        private Dictionary<string, StoreProductData> productDict;

        public override void InitData()
        {
            dict = new();
            goodsDict = new();
            productDict = new();

            var productList = AssetsDataManager.StoreData.ProductList;
            // 商城资源
            for (int i = 0, C = productList.Count; i < C; i++)
            {
                var productData = productList[i];
                switch (productData.ProductType)
                {
                    case Product.ProductType.Emote:
                        if (productData.AssetDataList.Count != 1) return;
                        productDict[productData.AssetDataList[0].PgcId] = productData;
                        break;
                    case Product.ProductType.Avatar:
                        if (productData.AssetDataList.Count != 1) return;
                        productDict[productData.AssetDataList[0].PgcId] = productData;
                        break;
                }
            }

            var creatorExchangeList = AssetsDataManager.StoreData.CreatorExchangeList;
            // 商城资源
            for (int i = 0, C = creatorExchangeList.Count; i < C; i++)
            {
                var productData = creatorExchangeList[i];
                switch (productData.ProductType)
                {
                    case Product.ProductType.Emote:
                        if (productData.AssetDataList.Count != 1) return;
                        productDict[productData.AssetDataList[0].PgcId] = productData;
                        break;
                    case Product.ProductType.Avatar:
                        if (productData.AssetDataList.Count != 1) return;
                        productDict[productData.AssetDataList[0].PgcId] = productData;
                        break;
                }
            }

            for (int i = 0, C = AssetsDataManager.FreeList.Count; i < C; i++)
            {
                var id = AssetsDataManager.FreeList[i];
                var tableData = DataTables.GetAvatarCommonData(id);
                if (tableData == null) continue;
                var uniqueType = UniqueType.Get(ResourceType.Avatar, tableData.SubType);
                var assetData = new AssetData()
                {
                    Name = "",
                    PgcId = id,
                    PurchaseType = PurchaseType.Free,
                    Price = 0
                };
                var assetsData = CreatePgcAssetsData(uniqueType, assetData, tableData, new InventoryData()
                {
                    Id = tableData.PgcId,
                    ClassType = uniqueType,
                    IsNew = false,
                    OwnedNum = 1
                });

                StoreProductData productData = new();
                CreateSingleGoodsData(productData, assetData, id, assetsData, i);
                ;
            }

            UpdatePetData();
        }

        #region PET

        public void UpdatePetData()
        {
            var productList = AssetsDataManager.StoreData.PetProductList;
            // 商城资源
            for (int i = 0, C = productList.Count; i < C; i++)
            {
                var productData = productList[i];

                switch (productData.ProductType)
                {
                    case Product.ProductType.Emote:
                        if (productData.AssetDataList.Count != 1) return;
                        productDict[productData.AssetDataList[0].PgcId] = productData;
                        break;
                    case Product.ProductType.Avatar:
                        if (productData.AssetDataList.Count != 1) return;
                        productDict[productData.AssetDataList[0].PgcId] = productData;
                        break;
                }
            }
            
            for (int i = 0, C = AssetsDataManager.FreeList.Count; i < C; i++)
            {
                var id = AssetsDataManager.FreeList[i];
                var tableData = DataTables.GetPetAvatarCommonData(id);
                if (tableData == null) continue;
                var uniqueType = UniqueType.Get(ResourceType.PGCPetAvatar, tableData.SubType);
                var assetData = new AssetData()
                {
                    Name = "",
                    PgcId = id,
                    PurchaseType = PurchaseType.Free,
                    Price = 0
                };
                var assetsData = CreatePgcAssetsData(uniqueType, assetData, tableData, new InventoryData()
                {
                    Id = tableData.PgcId,
                    ClassType = uniqueType,
                    IsNew = false,
                    OwnedNum = 1
                });

                StoreProductData productData = new();
                CreateSingleGoodsData(productData, assetData, id, assetsData, i);
            }
        }

        #endregion

        protected override void BeforeChangeInvoke(List<AssetsData> changed)
        {
            foreach (var kv in goodsDict)
            {
                var goodsData = kv.Value;
                goodsData.Update();
            }
        }

        protected override void AfterChangeInvoke(List<AssetsData> changed)
        {
        }

        public List<GoodsData> GetGoodsDataInOc(List<OcSkinInfo> skins, bool isPet)
        {
            List<GoodsData> lists = new();
            for (int i = 0, Count = skins.Count; i < Count; i++)
            {
                var skin = skins[i];

                //是私单，并且没有拥有的情况下，不展示
                if (skin.isPrivateOrder == 1 && !AssetsDataManager.IsOwned(skin.skinId))
                {
                    continue;
                }

                if (goodsDict.ContainsKey(skin.skinId))
                {
                    lists.Add(goodsDict[skin.skinId]);
                    continue;
                }

                if (skin.isPgc == 1)
                {
                    if (productDict.ContainsKey(skin.skinId))
                    {
                        var productData = productDict[skin.skinId];
                        var assetData = productData.AssetDataList[0];
                        SingleAvatarGoods(productData, assetData, i, isPet: isPet);
                        if (goodsDict.ContainsKey(skin.skinId)) lists.Add(goodsDict[skin.skinId]);
                    }
                    else
                    {
                        StoreProductData productData = new();
                        productData.AssetDataList.Add(new AssetData()
                        {
                            Name = "",
                            PgcId = skin.skinId,
                            PurchaseType = PurchaseType.ErrPurchaseType,
                            Price = 0
                        });
                        productData.SkipType = StoreSkipType.ErrSkipType;
                        SingleAvatarGoods(productData, productData.AssetDataList[0], i, isPet: isPet);
                        if (goodsDict.ContainsKey(skin.skinId)) lists.Add(goodsDict[skin.skinId]);
                    }
                }
                else
                {
                    CreateUgcGoodsData(skin.skinId, skin.paymentInfo, isPet);
                    if (goodsDict.ContainsKey(skin.skinId)) lists.Add(goodsDict[skin.skinId]);
                }
            }

            return lists;
        }

        protected void SingleAvatarGoods(Product.StoreProductData productData, Product.AssetData serverAssetsData,
            int sortIndex, bool canWear = false, bool isPet = false)
        {
            var pgcId = serverAssetsData.PgcId.ToString();
            var tableData = isPet ? DataTables.GetPetAvatarCommonData(pgcId) : DataTables.GetAvatarCommonData(pgcId);
            if (tableData == null) return;
            var uniqueType = UniqueType.Get(isPet ? ResourceType.PGCPetAvatar : ResourceType.Avatar, tableData.SubType);
            InventoryData inventoryData = BagDatabase.Inst.Select(pgcId);
            var assetsData = CreatePgcAssetsData(uniqueType, serverAssetsData, tableData, inventoryData);
            dict[tableData.PgcId] = assetsData;
            var goodsData = CreateSingleGoodsData(productData, serverAssetsData, pgcId, assetsData, sortIndex);
            goodsData.CantWear = canWear;
        }

        protected void SingleEmoteGoods(Product.StoreProductData productData, Product.AssetData serverAssetsData,
            int sortIndex)
        {
            var pgcId = serverAssetsData.PgcId.ToString();
            var tableData = DataTables.GetEmoUIConfig(pgcId);
            if (tableData == null) return;
            var uniqueType = UniqueType.Get(ResourceType.Emote, tableData.emoType);
            InventoryData inventoryData = BagDatabase.Inst.Select(pgcId);
            var assetsData = CreateEmoteAssetsData(serverAssetsData, tableData, inventoryData);
            dict[tableData.pgcId] = assetsData;
            var goodsData = CreateSingleGoodsData(productData, serverAssetsData, pgcId, assetsData, sortIndex);
            goodsData.CantWear = true;
        }

        protected GoodsData CreateSingleGoodsData(Product.StoreProductData productData,
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
            };
            goodsData.Update();
            goodsDict.Add(id, goodsData);
            return goodsData;
        }

        private GoodsData CreateUgcGoodsData(string ugcId, PaymentInfo data, bool isPet)
        {
            GoodsData goodsData;
            if (goodsDict.ContainsKey(ugcId)) return goodsDict[ugcId];

            goodsData = new GoodsData();
            goodsData.Id = ugcId;
            goodsData.GoodsType = GoodsType.SingleUgc;
            goodsData.ButtonType = ButtonType.Assets;
            goodsData.SourceData = new SourceData()
            {
                Source = Source.Ugc
            };
            goodsData.Assets = new List<AssetsData>()
            {
                CreateUgcAssetsData(ugcId, data, isPet)
            };
            goodsData.Update();
            goodsDict.Add(ugcId, goodsData);
            return goodsData;
        }

        protected Source GetSource(Product.StoreSkipType storeSkipType)
        {
            switch (storeSkipType)
            {
                case Product.StoreSkipType.Item:
                case Product.StoreSkipType.CreatorExchange:
                    return Source.Mall;
                case Product.StoreSkipType.Lottery:
                    return Source.Gashapon;
                default:
                    return Source.Jump;
            }
        }

        protected EmoteAssetsData CreateEmoteAssetsData(Product.AssetData serverAssetsData, EmoUIConfig data,
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

        protected PGCAssetsData CreatePgcAssetsData(int classType, Product.AssetData serverAssetsData,
            AvatarCommonData tableData, InventoryData inventoryData, bool isPet = false)
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
            assetsData.ResourceType = isPet ? ResourceType.PGCPetAvatar : ResourceType.Avatar;
            assetsData.AvatarSubType = UniqueType.AvatarSubType(classType);
            var specialConfig = DataTables.GetSpecialSkinConfig(pgcId);
            if (specialConfig != null) assetsData.AvatarSubType = AvatarSubType.SpecialSkin;
            assetsData.InventoryData = inventoryData;
            dict.Add(pgcId, assetsData);
            return assetsData;
        }

        /// <summary>
        ///  
        /// </summary>
        /// <param name="ugcId"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        private UGCAssetsData CreateUgcAssetsData(string ugcId, PaymentInfo data, bool isPet)
        {
            UGCAssetsData assetsData;
            if (dict.ContainsKey(ugcId)) return (UGCAssetsData)dict[ugcId];

            assetsData = new UGCAssetsData();
            assetsData.Id = ugcId;
            assetsData.ResourceType = isPet ? ResourceType.UGCPetAvatar : ResourceType.UgcAvatar;
            assetsData.InventoryData = BagDatabase.Inst.Select(ugcId);
            if (data != null)
            {
                assetsData.Value = new CurrencyData()
                {
                    CurrencyType = (CurrencyType)data.currencyType,
                    Value = data.price
                };
            }
            else
            {
                assetsData.Value = new CurrencyData()
                {
                    CurrencyType = CurrencyType.None,
                    Value = 0
                };
            }

            dict.Add(ugcId, assetsData);
            return assetsData;
        }

        public StoreProductData GetStoreItemData(string pgcId)
        {
            return productDict.ContainsKey(pgcId) ? productDict[pgcId] : null;
        }
    }
}