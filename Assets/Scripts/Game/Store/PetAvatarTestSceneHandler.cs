using System.Collections.Generic;
using Es;
using Game.Database;
using GameData.PgcData;

namespace Game.Store {
    public class PetAvatarTestSceneHandler : AvatarTestSceneHandler {
        public override void InitData()
        {
            data = new();
            dict = new();
            goodsDict = new();

            var allList = DataTables.GetPetAvatarCommonDataList();
            //// 免费资源
            //for (int i = 0, C = buys.Count; i < C; i++)
            //{
            //    var id = buys[i];
            //    var tableData = DataTables.GetAvatarCommonData(id);
            for (int i = 0, C = allList.Count; i < C; i++)
            {
                var tableData = allList[i];
                if (!tableData.PgcId.StartsWith(((int)ResourceType.PGCPetAvatar).ToString())) {
                    continue;
                }
                var uniqueType = UniqueType.Get(ResourceType.PGCPetAvatar, tableData.SubType);
                var list = data.ContainsKey(uniqueType) ? data[uniqueType] : NewList(uniqueType);
                var assetsData = CreatePetPgcAssetsData(uniqueType, tableData, new InventoryData()
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
            for (int i = 0, C = emoteList.Count; i < C; i++)
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


        protected PGCAssetsData CreatePetPgcAssetsData(int classType, AvatarCommonData data, InventoryData inventoryData)
        {
            PGCAssetsData assetsData;
            var pgcId = data.PgcId;
            if (dict.ContainsKey(pgcId)) return (PGCAssetsData)dict[pgcId];

            assetsData = new PGCAssetsData();
            assetsData.Id = pgcId;
            //assetsData.Name = data.ugcInfo.name;
            assetsData.ResourceType = ResourceType.PGCPetAvatar;
            assetsData.AvatarSubType = UniqueType.AvatarSubType(classType);
            assetsData.InventoryData = inventoryData;
            dict.Add(pgcId, assetsData);
            return assetsData;
        }
    }
}
