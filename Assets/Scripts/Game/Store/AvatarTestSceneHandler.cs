using Es;
using Game.Database;
using GameData.PgcData;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Store
{
    public class AvatarTestSceneHandler : AssetsDataHandler
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
                var assetsData = CreatePgcAssetsData(uniqueType, tableData, new InventoryData()
                {
                    Id = tableData.PgcId,
                    ClassType = uniqueType,
                    IsNew = false,
                    OwnedNum = 1
                });
                var specialData = DataTables.GetSpecialSkinConfig(tableData.PgcId);
                if (specialData != null) uniqueType = UniqueType.GetAvatar(AvatarSubType.SpecialSkin);
                var list = data.ContainsKey(uniqueType) ? data[uniqueType] : NewList(uniqueType);
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
            //goodsData.Name = data.ugcInfo.name;
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
    }
}
