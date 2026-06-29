using Es;
using Game.Database;
using GameData;
using GameData.BaseInfo;
using GameData.PgcData;
using GameData.UGCData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Store
{
    public class AvatarBagSceneHandler : AssetsDataHandler
    {
        protected Dictionary<int, List<GoodsData>> data;
        protected Dictionary<string, GoodsData> goodsDict;
        public int RedDot { protected set; get; } = 0;
        public int PetRedDot { protected set; get; } = 0;
        public Dictionary<int, int> RedDotPgc { private set; get; } = new();
        public Dictionary<int, int> RedDotUgcBuy { private set; get; } = new();
        public Dictionary<int, int> RedDotUgcCreator { private set; get; } = new();
        public HashSet<int> RedDotClassHash { private set; get; } = new();

        //private string shapeKey = "FirstOpenShapePanel" + AccountDataManager.Inst.UserInfo.uid;

        public override void InitData()
        {
            data = new();
            dict = new();
            goodsDict = new();

            int ugcCount = 0;
            // 背包资源
            var bagKey = BagDatabase.Inst.Keys;
            foreach (var key in bagKey)
            {
                var ids = BagDatabase.Inst.SelectAll(key);
                if (ids == null)
                {
                    ids = new List<InventoryData>();
                }
                AddFree(key, ref ids);
                if (ids == null || ids.Count == 0) continue;
                var resourceType = UniqueType.ResourceType(key);
                // UGC 套装走专用流程：异步取 batchInfo 后展开成 BundleUgc GoodsData
                if ((resourceType == ResourceType.UgcAvatar || resourceType == ResourceType.UGCPetAvatar)
                    && UniqueType.AvatarSubType(key) == AvatarSubType.Bundle)
                {
                    AddBundleGoodsData(ids);
                    ugcCount += ids.Count;
                    continue;
                }
                switch (resourceType)
                {
                    case ResourceType.Avatar:
                    case ResourceType.PGCPetAvatar:
                        AddPgcGoodsData(ids);
                        break;
                    case ResourceType.UgcAvatar:
                    case ResourceType.UGCPetAvatar:
                        AddUgcGoodsData(ids);
                        ugcCount += ids.Count;
                        break;
                    case ResourceType.MusicScore:
                        AddMusicScoreGoodsData(ids);
                        ugcCount += ids.Count;
                        break;
                    case ResourceType.Emote:
                        AddEmoteGoodsData(ids);
                        break;
                    case ResourceType.UgcPose:
                        AddUgcPoseGoodsData(ids);
                        break;
                    case ResourceType.UgcEmote:
                        AddUgcAnimGoodsData(ids);
                        break;
                    case ResourceType.UgcVehicle:
                        AddUgcVehicleGoodsData(ids);
                        break;
                    case ResourceType.Vehicle:
                        AddVehicleGoodsData(ids);
                        break;
                    case ResourceType.Theatre:
                    case ResourceType.AvatarCard:
                        AddTheatreGoodsData(ids);
                        ugcCount += ids.Count;
                        break;
                }
            }

            LoggerUtils.Log("背包Ugc数量" + ugcCount);
        }

        protected void AddFree(int classType, ref List<InventoryData> list)
        {
            // 免费资源
            for (int i = 0, C = AssetsDataManager.FreeList.Count; i < C; i++)
            {
                var id = AssetsDataManager.FreeList[i];
                var tableData = DataTables.GetGameResData(id);
                if (tableData == null) continue;
                var uniqueType = UniqueType.Get(tableData.ResourceType, tableData.SubType);
                if (uniqueType == classType) {
                    if (!list.Exists(tmp => tmp.Id == tableData.PgcId)) {
                        list.Add(new InventoryData() { Id = tableData.PgcId, ClassType = uniqueType, IsNew = false, OwnedNum = 1 });
                    }
                }
            }
        }

        protected override void AddDataInvoke(List<InventoryData> news)
        {
            var pgcs = new List<InventoryData>();
            var ugcs = new List<InventoryData>();
            var ugcBundles = new List<InventoryData>();
            var musicScore = new List<InventoryData>();
            var emotes = new List<InventoryData>();
            var ugcPoses = new List<InventoryData>();
            var ugcAnims = new List<InventoryData>();
            var vehicles = new List<InventoryData>();
            var ugcVehicles = new List<InventoryData>();
            var theatres = new List<InventoryData>();
            foreach (var key in news)
            {
                var resourceType = UniqueType.ResourceType(key.ClassType);
                if (resourceType == ResourceType.Avatar || resourceType == ResourceType.PGCPetAvatar)
                {
                    var subType = UniqueType.AvatarSubType(key.ClassType);
                    if (subType != AvatarSubType.Bundle)
                    {
                        pgcs.Add(key.Clone());
                    }
                }
                else if (resourceType == ResourceType.UgcAvatar || resourceType == ResourceType.UGCPetAvatar)
                {
                    var subType = UniqueType.AvatarSubType(key.ClassType);
                    if (subType == AvatarSubType.Bundle)
                    {
                        ugcBundles.Add(key.Clone());
                    }
                    else
                    {
                        ugcs.Add(key.Clone());
                    }
                }
                else if (resourceType == ResourceType.MusicScore) musicScore.Add(key.Clone());
                else if (resourceType == ResourceType.Emote) emotes.Add(key.Clone());
                else if(resourceType == ResourceType.UgcPose) ugcPoses.Add(key.Clone());
                else if(resourceType == ResourceType.UgcEmote) ugcAnims.Add(key.Clone());
                else if(resourceType == ResourceType.Vehicle) vehicles.Add(key.Clone());
                else if(resourceType == ResourceType.UgcVehicle) ugcVehicles.Add(key.Clone());
                else if(resourceType == ResourceType.Theatre || resourceType == ResourceType.AvatarCard) theatres.Add(key.Clone());
            }
            AddPgcGoodsData(pgcs, true);
            AddUgcGoodsData(ugcs, true);
            AddBundleGoodsData(ugcBundles, true);
            AddMusicScoreGoodsData(musicScore, true);
            AddEmoteGoodsData(emotes, true);
            AddUgcPoseGoodsData(ugcPoses, true);
            AddUgcAnimGoodsData(ugcAnims, true);
            AddVehicleGoodsData(vehicles, true);
            AddUgcVehicleGoodsData(ugcVehicles, true);
            AddTheatreGoodsData(theatres, true);
        }

        protected virtual void AddPgcGoodsData(List<InventoryData> inventoryDatas, bool newGoods = false)
        {
            for (int i = 0, C = inventoryDatas.Count; i < C; i++)
            {
                var inventoryData = inventoryDatas[i];
                var id = inventoryData.Id;
                var tableData = DataTables.GetAvatarCommonData(id);
                if (tableData == null) tableData = DataTables.GetPetAvatarCommonData(id);
                if (tableData == null) continue;

                var uniqueType = inventoryData.ClassType;

                var specialData = DataTables.GetSpecialSkinConfig(id);
                if (specialData != null)
                {
                    var specialUniqueType = UniqueType.GetAvatar(AvatarSubType.SpecialSkin);
                    var list1 = data.ContainsKey(specialUniqueType) ? data[specialUniqueType] : NewList(specialUniqueType);
                    var assetsData = CreatePgcAssetsData(uniqueType, tableData, inventoryData);
                    if (newGoods)
                    {
                        var index = list1.FindIndex(l => l.ButtonType == ButtonType.Assets);
                        if (index == -1) index = list1.Count;
                        list1.Insert(index, CreateGoodsData(uniqueType, id, assetsData));
                    }
                    else
                    {
                        list1.Add(CreateGoodsData(uniqueType, id, assetsData));
                    }
                    data[specialUniqueType] = list1;
                }
                else
                {
                    var list = data.ContainsKey(uniqueType) ? data[uniqueType] : NewList(uniqueType);
                    var assetsData = CreatePgcAssetsData(uniqueType, tableData, inventoryData);
                    if (newGoods)
                    {
                        var index = list.FindIndex(l => l.ButtonType == ButtonType.Assets);
                        if (index == -1) index = list.Count;
                        list.Insert(index, CreateGoodsData(uniqueType, id, assetsData));
                    }
                    else
                    {
                        list.Add(CreateGoodsData(uniqueType, id, assetsData));
                    }
                    data[uniqueType] = list;
                }

            }
        }

        protected void AddEmoteGoodsData(List<InventoryData> inventoryDatas, bool newGoods = false)
        {
            for (int i = 0, C = inventoryDatas.Count; i < C; i++)
            {
                var inventoryData = inventoryDatas[i];
                var id = inventoryData.Id;
                var tableData = DataTables.GetEmoUIConfig(id);
                if (tableData == null) continue;
                var uniqueType = inventoryData.ClassType;
                var list = data.ContainsKey(uniqueType) ? data[uniqueType] : NewList(uniqueType);
                var assetsData = CreateEmoteAssetsData(uniqueType, tableData, inventoryData);
                GoodsData goodsData = CreateGoodsData(uniqueType, id, assetsData);
                if (newGoods)
                {
                    var index = list.FindIndex(l => l.ButtonType == ButtonType.Assets);
                    if (index == -1) index = list.Count;
                    list.Insert(index, goodsData);
                }
                else
                {
                    list.Add(goodsData);
                }
                data[uniqueType] = list;

                var allUniqueType = UniqueType.Get(ResourceType.Emote, Mathf.CeilToInt(tableData.emoType / 2f) * 100);
                var allList = GetGoodsData(allUniqueType);
                allList.Add(goodsData);
                data[allUniqueType] = allList;
            }
        }

        protected virtual void AddUgcGoodsData(List<InventoryData> inventoryDatas, bool newGoods = false)
        {
            for (int i = 0, C = inventoryDatas.Count; i < C; i++)
            {
                var inventoryData = inventoryDatas[i];
                var id = inventoryData.Id;
                var uniqueType = inventoryData.ClassType;
                var list = data.ContainsKey(uniqueType) ? data[uniqueType] : NewList(uniqueType);
                var assetsData = CreateUgcAssetsData(uniqueType, id, inventoryData);
                if (newGoods)
                {
                    var index = list.FindIndex(l => l.ButtonType == ButtonType.Assets);
                    if (index == -1) index = list.Count;
                    list.Insert(index, CreateGoodsData(uniqueType, id, assetsData));
                }
                else
                {
                    list.Add(CreateGoodsData(uniqueType, id, assetsData));
                }
                data[uniqueType] = list;
            }
        }

        /// <summary>
        /// 背包套装：异步调 /ugc/skin/batchInfo 取 SkinInfo（含 bundleItems），
        /// 展开成 BundleUgc GoodsData，复用商城/送礼的渲染与穿戴流程。
        /// </summary>
        protected virtual void AddBundleGoodsData(List<InventoryData> inventoryDatas, bool newGoods = false)
        {
            if (inventoryDatas == null || inventoryDatas.Count == 0) return;

            // 按 id 建索引，回调时找回对应的 InventoryData
            var invMap = new Dictionary<string, InventoryData>();
            foreach (var inv in inventoryDatas)
            {
                if (inv == null || string.IsNullOrEmpty(inv.Id)) continue;
                if (goodsDict.ContainsKey(inv.Id)) continue; // 已存在则跳过
                invMap[inv.Id] = inv;
            }
            if (invMap.Count == 0) return;

            var idList = string.Join(",", invMap.Keys);
            var req = new JObject { ["idList"] = idList };

            NetworkManager.Inst.SendHttpRequest(
                HttpUrlDefine.GetClothesBatchInfo, HttpMethod.GET,
                JsonConvert.SerializeObject(req),
                (string content) =>
                {
                    BatchDetailRsp rsp = null;
                    try { rsp = JsonConvert.DeserializeObject<BatchDetailRsp>(content); }
                    catch (Exception e)
                    {
                        LoggerUtils.LogError("Bundle batchInfo parse fail: " + e.Message);
                        return;
                    }
                    if (rsp == null || rsp.skinList == null) return;

                    var changed = new List<AssetsData>();
                    foreach (var detail in rsp.skinList)
                    {
                        if (detail == null || detail.skinInfo == null) continue;
                        var skinId = detail.skinInfo.id;
                        if (!invMap.TryGetValue(skinId, out var inv)) continue;
                        if (goodsDict.ContainsKey(skinId)) continue;

                        var classType = inv.ClassType;
                        var recommend = new RecommendItemData
                        {
                            ugcId = skinId,
                            ugcType = UgcType.Clothes,
                            UgcInfo = detail.skinInfo,
                            ugcInfo = detail.skinInfo, // 兼容老接口字段
                        };

                        var goodsData = new GoodsData();
                        goodsData.Id = skinId;
                        goodsData.Name = detail.skinInfo.name;
                        goodsData.GoodsType = GoodsType.BundleUgc;
                        goodsData.ButtonType = ButtonType.Assets;
                        goodsData.IsBagScene = true;
                        goodsData.subType = (int)AvatarSubType.Bundle;
                        goodsData.Assets = CreateUgcBundleAssetsData(classType, recommend);
                        goodsData.UgcBundleInfo = recommend;
                        goodsData.IsOwned = inv.OwnedNum > 0;

                        foreach (var asset in goodsData.Assets) asset.InventoryData = inv;

                        goodsDict[skinId] = goodsData;
                        // carrier：让 UpdateData 能通过 bundle id 找到 AssetsData，触发 BeforeChangeInvoke → UpdateRedDot，才能清掉红点
                        if (!dict.ContainsKey(skinId))
                            dict[skinId] = new UGCAssetsData { Id = skinId, InventoryData = inv };

                        var list = data.ContainsKey(classType) ? data[classType] : NewList(classType);
                        if (newGoods)
                        {
                            var index = list.FindIndex(l => l.ButtonType == ButtonType.Assets);
                            if (index == -1) index = list.Count;
                            list.Insert(index, goodsData);
                        }
                        else
                        {
                            list.Add(goodsData);
                        }
                        data[classType] = list;

                        changed.AddRange(goodsData.Assets);
                    }

                    // batchInfo 是异步的，无论是初始化还是增量都要手动触发刷新
                    if (changed.Count > 0)
                    {
                        BeforeChangeInvoke(changed);
                        OnDataChangeInvoke(changed.ToArray());
                        AfterChangeInvoke(changed);
                    }
                },
                (string failMsg) =>
                {
                    LoggerUtils.LogError("Bundle batchInfo failed: " + failMsg);
                });
        }

        /// <summary>
        /// 把套装的 bundleItems（子部件 SkinInfo JSON 串）展开成 UGCAssetsData 列表。
        /// 与商城/送礼侧 CreateUgcBundleAssetsData 同义，独立一份以保持背包侧不依赖商城 handler。
        /// </summary>
        protected virtual List<AssetsData> CreateUgcBundleAssetsData(int classType, RecommendItemData data)
        {
            var list = new List<AssetsData>();
            var info = data?.skinInfo;
            if (info == null || info.bundleItems == null) return list;
            foreach (var subInfoStr in info.bundleItems)
            {
                if (string.IsNullOrEmpty(subInfoStr)) continue;
                SkinInfo subInfo;
                try { subInfo = JsonConvert.DeserializeObject<SkinInfo>(subInfoStr); }
                catch (Exception e)
                {
                    LoggerUtils.LogError("Bundle subItem parse fail: " + e.Message);
                    continue;
                }
                if (subInfo == null || string.IsNullOrEmpty(subInfo.id)) continue;

                var recommendForSub = new RecommendItemData
                {
                    ugcId = subInfo.id,
                    ugcType = UgcType.Clothes,
                    UgcInfo = subInfo,
                    ugcInfo = subInfo, // 兼容老接口字段
                };

                // dict 里可能已有 CreateUgcAssetsData 留下的 UGCAssetsData（UgcInfo=null），补上 SkinInfo
                // 但不能复用同一对象加入 bundle assets 列表：后续 InventoryData = inv 会覆盖其 InventoryData，
                // 导致点该子部件时 op.Id = inv.Id = bundleId，反而清掉了 bundle 红点
                if (dict.TryGetValue(subInfo.id, out var existing) && existing is UGCAssetsData existingUgc)
                {
                    if (existingUgc.UgcInfo == null) existingUgc.UgcInfo = recommendForSub;
                    if (string.IsNullOrEmpty(existingUgc.Name)) existingUgc.Name = subInfo.name;
                    // bundle 侧用独立副本，不共用原对象
                    var bundleSub = new UGCAssetsData { Id = subInfo.id, Name = subInfo.name, ResourceType = existingUgc.ResourceType, AvatarSubType = existingUgc.AvatarSubType, UgcInfo = recommendForSub };
                    list.Add(bundleSub);
                    continue;
                }

                var sub = new UGCAssetsData();
                sub.Id = subInfo.id;
                sub.Name = subInfo.name;
                sub.ResourceType = ResourceType.UgcAvatar;
                sub.AvatarSubType = (AvatarSubType)subInfo.subType;
                sub.UgcInfo = recommendForSub;
                dict[subInfo.id] = sub;
                list.Add(sub);
            }
            return list;
        }

        protected virtual void AddMusicScoreGoodsData(List<InventoryData> inventoryDatas, bool newGoods = false)
        {
            for (int i = 0, C = inventoryDatas.Count; i < C; i++)
            {
                var inventoryData = inventoryDatas[i];
                var id = inventoryData.Id;
                var uniqueType = inventoryData.ClassType;
                var list = data.ContainsKey(uniqueType) ? data[uniqueType] : NewList(uniqueType);
                var assetsData = CreateMusicScoreAssetsData(uniqueType, id, inventoryData);
                if (newGoods)
                {
                    var index = list.FindIndex(l => l.ButtonType == ButtonType.Assets);
                    if (index == -1) index = list.Count;
                    list.Insert(index, CreateGoodsData(uniqueType, id, assetsData));
                }
                else
                {
                    list.Add(CreateGoodsData(uniqueType, id, assetsData));
                }
                data[uniqueType] = list;
            }
        }

        protected virtual void AddUgcPoseGoodsData(List<InventoryData> inventoryDatas, bool newGoods = false)
        {
            for (int i = 0, C = inventoryDatas.Count; i < C; i++)
            {
                var inventoryData = inventoryDatas[i];
                var id = inventoryData.Id;
                var uniqueType = inventoryData.ClassType;
                var list = data.ContainsKey(uniqueType) ? data[uniqueType] : NewList(uniqueType);
                var assetsData = CreateUgcPoseAssetsData(uniqueType, id, inventoryData);
                if (newGoods)
                {
                    var index = list.FindIndex(l => l.ButtonType == ButtonType.Assets);
                    if (index == -1) index = list.Count;
                    list.Insert(index, CreateGoodsData(uniqueType, id, assetsData));
                }
                else
                {
                    list.Add(CreateGoodsData(uniqueType, id, assetsData));
                }
                data[uniqueType] = list;
            }
        }

        protected virtual void AddUgcAnimGoodsData(List<InventoryData> inventoryDatas, bool newGoods = false)
        {
            for (int i = 0, C = inventoryDatas.Count; i < C; i++)
            {
                var inventoryData = inventoryDatas[i];
                var id = inventoryData.Id;
                var uniqueType = inventoryData.ClassType;
                var list = data.ContainsKey(uniqueType) ? data[uniqueType] : NewList(uniqueType);
                var assetsData = CreateUgcAnimAssetsData(uniqueType, id, inventoryData);
                if (newGoods)
                {
                    var index = list.FindIndex(l => l.ButtonType == ButtonType.Assets);
                    if (index == -1) index = list.Count;
                    list.Insert(index, CreateGoodsData(uniqueType, id, assetsData));
                }
                else
                {
                    list.Add(CreateGoodsData(uniqueType, id, assetsData));
                }
                data[uniqueType] = list;
            }
        }

        protected virtual void AddVehicleGoodsData(List<InventoryData> inventoryDatas, bool newGoods = false)
        {
            for (int i = 0, C = inventoryDatas.Count; i < C; i++)
            {
                var inventoryData = inventoryDatas[i];
                var id = inventoryData.Id;
                var uniqueType = inventoryData.ClassType;
                var list = data.ContainsKey(uniqueType) ? data[uniqueType] : NewList(uniqueType);
                var assetsData = CreateVehicleAssetsData(uniqueType, id, inventoryData);
                if (newGoods)
                {
                    var index = list.FindIndex(l => l.ButtonType == ButtonType.Assets);
                    if (index == -1) index = list.Count;
                    list.Insert(index, CreateGoodsData(uniqueType, id, assetsData));
                }
                else
                {
                    list.Add(CreateGoodsData(uniqueType, id, assetsData));
                }
                data[uniqueType] = list;
            }
        }

        protected virtual void AddUgcVehicleGoodsData(List<InventoryData> inventoryDatas, bool newGoods = false)
        {
            for (int i = 0, C = inventoryDatas.Count; i < C; i++)
            {
                var inventoryData = inventoryDatas[i];
                var id = inventoryData.Id;
                var uniqueType = inventoryData.ClassType;
                var list = data.ContainsKey(uniqueType) ? data[uniqueType] : NewList(uniqueType);
                var assetsData = CreateUgcVehicleAssetsData(uniqueType, id, inventoryData);
                if (newGoods)
                {
                    var index = list.FindIndex(l => l.ButtonType == ButtonType.Assets);
                    if (index == -1) index = list.Count;
                    list.Insert(index, CreateGoodsData(uniqueType, id, assetsData));
                }
                else
                {
                    list.Add(CreateGoodsData(uniqueType, id, assetsData));
                }
                data[uniqueType] = list;
            }
        }

        protected virtual void AddTheatreGoodsData(List<InventoryData> inventoryDatas, bool newGoods = false)
        {
            for (int i = 0, C = inventoryDatas.Count; i < C; i++)
            {
                var inventoryData = inventoryDatas[i];
                var id = inventoryData.Id;
                var uniqueType = inventoryData.ClassType;
                var list = data.ContainsKey(uniqueType) ? data[uniqueType] : NewList(uniqueType);
                var assetsData = CreateTheatreAssetsData(uniqueType, id, inventoryData);
                if (newGoods)
                {
                    var index = list.FindIndex(l => l.ButtonType == ButtonType.Assets);
                    if (index == -1) index = list.Count;
                    list.Insert(index, CreateGoodsData(uniqueType, id, assetsData));
                }
                else
                {
                    list.Add(CreateGoodsData(uniqueType, id, assetsData));
                }
                data[uniqueType] = list;
            }
        }

        private AssetsData CreateTheatreAssetsData(int classType, string id, InventoryData inventoryData)
        {
            if (dict.ContainsKey(id))
            {
                // 更新 InventoryData 引用（如清红点后 IsNew 变化），同时刷新 GoodsData.IsNew
                dict[id].InventoryData = inventoryData;
                if (goodsDict.ContainsKey(id)) goodsDict[id].Update();
                return dict[id];
            }
            var resourceType = UniqueType.ResourceType(classType);
            AssetsData assetsData;
            if (resourceType == ResourceType.Theatre)
            {
                assetsData = new UgcTheatreAssetsData();
                assetsData.ResourceType = ResourceType.Theatre;
            }
            else
            {
                assetsData = new UgcActorAssetsData();
                assetsData.ResourceType = ResourceType.AvatarCard;
            }
            assetsData.Id = id;
            assetsData.InventoryData = inventoryData;
            dict.Add(id, assetsData);
            return assetsData;
        }

        protected virtual GoodsData CreateGoodsData(int classType, string id, AssetsData assetsData)
        {
            GoodsData goodsData;
            if (goodsDict.ContainsKey(id)) return goodsDict[id];

            goodsData = new GoodsData();
            goodsData.Id = id;
            goodsData.GoodsType = assetsData.ResourceType switch
            {
                ResourceType.Avatar => GoodsType.SinglePgc,
                ResourceType.UgcAvatar => GoodsType.SingleUgc,
                ResourceType.MusicScore => GoodsType.SingleUgc,
                ResourceType.PGCPetAvatar => GoodsType.SinglePgc,
                ResourceType.UGCPetAvatar => GoodsType.SingleUgc,
                ResourceType.UgcPose =>GoodsType.SingleUgc,
                ResourceType.Emote =>GoodsType.SinglePgc,
                ResourceType.UgcEmote =>GoodsType.SingleUgc,
                ResourceType.UgcVehicle =>GoodsType.SingleUgc,
                ResourceType.Vehicle =>GoodsType.SinglePgc,
                ResourceType.Theatre => GoodsType.SingleUgc,
                ResourceType.AvatarCard => GoodsType.SingleUgc,
                _ => GoodsType.ErrGoodsType
            };

            // 直接从配置表中读取SubType
            var tableData = DataTables.GetAvatarCommonData(id);
            if (tableData != null)
            {
                goodsData.subType = tableData.SubType;
            }
            else
            {
                var petTableData = DataTables.GetPetAvatarCommonData(id);
                if (petTableData != null)
                {
                    goodsData.subType = petTableData.SubType;
                }
                else
                {
                    var gameResData = DataTables.GetGameResData(id);
                    if (gameResData != null)
                    {
                        goodsData.subType = gameResData.SubType;
                    }
                    else
                    {
                        goodsData.subType = 0;
                    }
                }
            }

            if (assetsData.ResourceType == ResourceType.MusicScore)
            {
                goodsData.CantWear = true;
                goodsData.UseTips = LocalizationManager.Inst.GetLocalizedText("乐谱可以在地图中配合乐器使用");
            }

            goodsData.ButtonType = ButtonType.Assets;
            goodsData.IsBagScene = true;
            //goodsData.Name = data.ugcInfo.name;
            goodsData.Assets = new List<AssetsData>()
            {
                assetsData
            };
            goodsData.IsOwned = assetsData.InventoryData?.OwnedNum > 0;
            goodsDict.Add(id, goodsData);
            return goodsData;
        }

        protected override void BeforeChangeInvoke(List<AssetsData> changed)
        {
            var remotes = new List<GoodsData>();
            foreach (var kv in goodsDict)
            {
                var goodsData = kv.Value;
                goodsData.Update();
                if (!goodsData.IsOwned) remotes.Add(goodsData);
            }

            foreach (var remote in remotes)
            {
                goodsDict.Remove(remote.Id);
                // TODO 这里先这样，但是效率有问题，后面再改
                foreach (var list in data)
                {
                    list.Value.Remove(remote);
                }
            }

            UpdateRedDot();
        }

        #region 红点
        protected virtual void UpdateRedDot()
        {
            RedDot = 0;
            PetRedDot = 0;
            RedDotClassHash.Clear();
            RedDotUgcBuy.Clear();
            RedDotUgcCreator.Clear();
            //if (!PlayerPrefs.HasKey(shapeKey))
            //{
            //    RedDot++;
            //    RedDotClassHash.Add(UniqueType.GetAvatar(AvatarSubType.Shape));
            //}
            foreach (var kv in data)
            {
                var rt = UniqueType.ResourceType(kv.Key);
                var subType = UniqueType.AvatarSubType(kv.Key);

                if (rt == ResourceType.Avatar || rt == ResourceType.PGCPetAvatar)
                {
                    var count = 0;
                    foreach (var data in kv.Value)
                    {
                        if (data.IsNew) count++;
                    }
                    if (count > 0) RedDotClassHash.Add(kv.Key);
                    RedDotPgc[kv.Key] = count;
                    if (rt == ResourceType.Avatar) RedDot += count;
                    if (rt == ResourceType.PGCPetAvatar) PetRedDot += count;
                }

                if (rt == ResourceType.UgcAvatar || rt == ResourceType.MusicScore || rt == ResourceType.UGCPetAvatar || rt == ResourceType.UgcVehicle)
                {
                    var count1 = 0;
                    var count2 = 0;
                    foreach (var data in kv.Value)
                    {
                        if (data.IsNew)
                        {
                            var ugcAssets = data.GetFirstAsset<AssetsData>();
                            if (ugcAssets.InventoryData == null) continue;
                            if (ugcAssets.InventoryData.Tag == Network.Message.BackpackTag.Creator)
                            {
                                count2++;
                            }
                            else
                            {
                                count1++;
                            }
                        }
                    }
                    if (count1 + count2 > 0) RedDotClassHash.Add(kv.Key);
                    RedDotUgcBuy[kv.Key] = count1;
                    RedDotUgcCreator[kv.Key] = count2;
                    if (rt == ResourceType.UgcAvatar || rt == ResourceType.MusicScore) RedDot += count1 + count2;
                    if (rt == ResourceType.UGCPetAvatar) PetRedDot += count1 + count2;
                }

                if (rt == ResourceType.UgcEmote || rt == ResourceType.UgcPose)
                {
                    var count1 = 0;
                    var count2 = 0;
                    foreach (var data in kv.Value)
                    {
                        if (data.IsNew)
                        {
                            var ugcAssets = data.GetFirstAsset<AssetsData>();
                            if (ugcAssets.InventoryData == null) continue;
                            if (ugcAssets.InventoryData.Tag == Network.Message.BackpackTag.Creator)
                            {
                                count2++;
                            }
                            else
                            {
                                count1++;
                            }
                        }
                    }
                    var emoteSubType = UniqueType.UgcEmoteSubType(kv.Key);
                    var classType = UniqueType.Get(rt, (emoteSubType == UgcAnimSubType.Single || emoteSubType == UgcAnimSubType.Double) ? (int)UgcAnimSubType.PeopleAll : (int)UgcAnimSubType.PetAll);
                    if (count1 + count2 > 0) RedDotClassHash.Add(classType);
                    if (!RedDotUgcBuy.ContainsKey(classType)) RedDotUgcBuy[classType] = 0;
                    RedDotUgcBuy[classType] += count1;
                    if (!RedDotUgcCreator.ContainsKey(classType)) RedDotUgcCreator[classType] = 0;
                    RedDotUgcCreator[classType] += count2;

                    if (emoteSubType == UgcAnimSubType.Single || emoteSubType == UgcAnimSubType.Double) RedDot += count1 + count2;
                    else PetRedDot += count1 + count2;
                }

                if (rt == ResourceType.Theatre || rt == ResourceType.AvatarCard)
                {
                    var count1 = 0;
                    var count2 = 0;
                    foreach (var goodsData in kv.Value)
                    {
                        if (goodsData.IsNew)
                        {
                            var asset = goodsData.GetFirstAsset<AssetsData>();
                            if (asset?.InventoryData == null) continue;
                            if (asset.InventoryData.Tag == Network.Message.BackpackTag.Creator) count2++;
                            else count1++;
                        }
                    }
                    if (count1 + count2 > 0) RedDotClassHash.Add(kv.Key);
                    RedDotUgcBuy[kv.Key] = count1;
                    RedDotUgcCreator[kv.Key] = count2;
                    RedDot += count1 + count2;
                }
            }
        }
        #endregion

        protected virtual PGCAssetsData CreatePgcAssetsData(int classType, AvatarCommonData data, InventoryData inventoryData)
        {
            PGCAssetsData assetsData;
            var pgcId = data.PgcId;
            if (dict.ContainsKey(pgcId)) return (PGCAssetsData)dict[pgcId];

            assetsData = new PGCAssetsData();
            assetsData.Id = pgcId;
            assetsData.ResourceType = UniqueType.ResourceType(classType);
            assetsData.AvatarSubType = UniqueType.AvatarSubType(classType);
            var specialConfig = DataTables.GetSpecialSkinConfig(pgcId);
            if (specialConfig != null) assetsData.AvatarSubType = AvatarSubType.SpecialSkin;
            assetsData.InventoryData = inventoryData;
            dict.Add(pgcId, assetsData);
            return assetsData;
        }

        private EmoteAssetsData CreateEmoteAssetsData(int classType, EmoUIConfig data, InventoryData inventoryData)
        {
            EmoteAssetsData assetsData;
            var pgcId = data.pgcId;
            if (dict.ContainsKey(pgcId)) return (EmoteAssetsData)dict[pgcId];

            assetsData = new EmoteAssetsData();
            assetsData.Id = pgcId;
            assetsData.Name = data.name;
            assetsData.ResourceType = UniqueType.ResourceType(classType);
            assetsData.EmoteSubType = (EmoteSubType)data.emoType;
            assetsData.InventoryData = inventoryData;
            dict.Add(pgcId, assetsData);
            return assetsData;
        }

        protected virtual UGCAssetsData CreateUgcAssetsData(int classType, string id, InventoryData inventoryData)
        {
            UGCAssetsData assetsData;
            var ugcId = id;
            if (dict.ContainsKey(ugcId)) return (UGCAssetsData)dict[ugcId];

            assetsData = new UGCAssetsData();
            assetsData.Id = ugcId;
            assetsData.ResourceType = UniqueType.ResourceType(classType);
            assetsData.AvatarSubType = UniqueType.AvatarSubType(classType);
            assetsData.InventoryData = inventoryData;
            //AssetsDataManager.GetUgcInfo(ugcId, (data) =>
            //{
            //    assetsData.AvatarSubType = (AvatarSubType)data.ugcInfo.subType;
            //    assetsData.UgcInfo = data;
            //});
            dict.Add(ugcId, assetsData);
            return assetsData;
        }

        private MusicScoreAssetsData CreateMusicScoreAssetsData(int classType, string id, InventoryData inventoryData)
        {
            MusicScoreAssetsData assetsData;
            var ugcId = id;
            if (dict.ContainsKey(ugcId)) return (MusicScoreAssetsData)dict[ugcId];

            assetsData = new MusicScoreAssetsData();
            assetsData.Id = ugcId;
            assetsData.ResourceType = ResourceType.MusicScore;
            assetsData.MusicScoreSubType = MusicScoreSubType.GeneralMusicScore;
            assetsData.InventoryData = inventoryData;
            //AssetsDataManager.GetUgcInfo(ugcId, (data) =>
            //{
            //    assetsData.AvatarSubType = (AvatarSubType)data.ugcInfo.subType;
            //    assetsData.UgcInfo = data;
            //});
            dict.Add(ugcId, assetsData);
            return assetsData;
        }

        private UgcPoseAssetsData CreateUgcPoseAssetsData(int classType, string id, InventoryData inventoryData)
        {
            UgcPoseAssetsData assetsData;
            var pgcId = id;
            if (dict.ContainsKey(pgcId)) return (UgcPoseAssetsData)dict[pgcId];

            assetsData = new UgcPoseAssetsData();
            assetsData.Id = pgcId;
            assetsData.UgcPoseSubType = UniqueType.UgcPoseSubType(classType);
            assetsData.ResourceType = UniqueType.ResourceType(classType);
            assetsData.InventoryData = inventoryData;
            dict.Add(pgcId, assetsData);
            return assetsData;
        }

        private UgcAnimAssetsData CreateUgcAnimAssetsData(int classType, string id, InventoryData inventoryData)
        {
            UgcAnimAssetsData assetsData;
            var pgcId = id;
            if (dict.ContainsKey(pgcId)) return (UgcAnimAssetsData)dict[pgcId];

            assetsData = new UgcAnimAssetsData();
            assetsData.Id = pgcId;
            assetsData.UgcAnimSubType = UniqueType.UgcEmoteSubType(classType);
            assetsData.ResourceType = UniqueType.ResourceType(classType);
            assetsData.InventoryData = inventoryData;
            dict.Add(pgcId, assetsData);
            return assetsData;
        }

        private UgcVehicleAssetsData CreateUgcVehicleAssetsData(int classType, string id, InventoryData inventoryData)
        {
            UgcVehicleAssetsData assetsData;
            var pgcId = id;
            if (dict.ContainsKey(pgcId)) return (UgcVehicleAssetsData)dict[pgcId];
            assetsData = new UgcVehicleAssetsData();
            assetsData.Id = pgcId;
            assetsData.VehicleSubType = UniqueType.VehicleSubType(classType);
            assetsData.ResourceType = UniqueType.ResourceType(classType);
            assetsData.InventoryData = inventoryData;
            dict.Add(pgcId, assetsData);
            return assetsData;
        }

        private VehicleAssetsData CreateVehicleAssetsData(int classType, string id, InventoryData inventoryData)
        {
            VehicleAssetsData assetsData;
            var pgcId = id;
            if (dict.ContainsKey(pgcId)) return (VehicleAssetsData)dict[pgcId];
            assetsData = new VehicleAssetsData();
            assetsData.Id = pgcId;
            assetsData.VehicleSubType = UniqueType.VehicleSubType(classType);
            assetsData.ResourceType = UniqueType.ResourceType(classType);
            assetsData.InventoryData = inventoryData;
            dict.Add(pgcId, assetsData);
            return assetsData;
        }


        public virtual List<GoodsData> NewList(int uniqueType)
        {
            var list = new List<GoodsData>();

            var resourceType = UniqueType.ResourceType(uniqueType);

            // 套装：不要"去创作"占位，但保留"脱下"占位让用户一键脱掉套装
            if (UniqueType.AvatarSubType(uniqueType) == AvatarSubType.Bundle)
            {
                list.Add(new GoodsData()
                {
                    Id = "0",
                    ButtonType = ButtonType.TakeOff,
                });
                return list;
            }

            if (resourceType == ResourceType.Avatar || resourceType == ResourceType.UgcAvatar)
            {
                var avatarSubType = UniqueType.AvatarSubType(uniqueType);
                var ugcAvatarEnable = UniqueType.UgcAvatarEnable(uniqueType);
                if (ugcAvatarEnable)
                {
                    list.Add(new GoodsData()
                    {
                        Id = "",
                        ButtonType = ButtonType.Design,
                        AddTips = "去创作"
                    });
                }

                if ((resourceType == ResourceType.Avatar || resourceType == ResourceType.UgcAvatar) && avatarSubType != AvatarSubType.Clothes)
                {
                    list.Add(new GoodsData()
                    {
                        Id = "0",
                        ButtonType = ButtonType.TakeOff,
                    });
                }
            }
            else if (resourceType == ResourceType.MusicScore)
            {
                list.Add(new GoodsData()
                {
                    Id = "",
                    ButtonType = ButtonType.Design,
                    AddTips = "去创作"
                });
            }
            else if (resourceType == ResourceType.Emote)
            {
                if (UniqueType.EmoteSubType(uniqueType) == EmoteSubType.SingleLoop)
                {
                    list.Add(new GoodsData()
                    {
                        Id = "leisure",
                        ButtonType = ButtonType.EmoteIdle,
                        Name="待机"
                    });
                    list.Add(new GoodsData()
                    {
                        Id = "default",
                        ButtonType = ButtonType.EmoteIdle,
                        Name="站立"
                    });
                }
                else if (UniqueType.EmoteSubType(uniqueType) == EmoteSubType.PetSingleLoop)
                {
                    list.Add(new GoodsData()
                    {
                        Id = "default",
                        ProductId = 1,
                        ButtonType = ButtonType.EmoteIdle,
                        Name = "站立"
                    });
                }
            }
            else if (resourceType == ResourceType.PGCPetAvatar || resourceType == ResourceType.UGCPetAvatar)
            {
                var avatarSubType = UniqueType.AvatarSubType(uniqueType);
                var ugcAvatarEnable = UniqueType.UgcAvatarEnable(uniqueType);
                if (ugcAvatarEnable)
                {
                    list.Add(new GoodsData()
                    {
                        Id = "",
                        ButtonType = ButtonType.Design,
                        AddTips = "去创作"
                    });
                }

                if (avatarSubType != AvatarSubType.Skin)
                {
                    list.Add(new GoodsData()
                    {
                        Id = "0",
                        ButtonType = ButtonType.TakeOff,
                    });
                }
            }
            else if (resourceType == ResourceType.UgcPose)
            {
                list.Add(new GoodsData()
                {
                    Id = "",
                    ButtonType = ButtonType.Design,
                    AddTips = "去创作"
                });
            }
            else if(resourceType == ResourceType.UgcEmote)
            {
                list.Add(new GoodsData()
                {
                    Id = "",
                    ButtonType = ButtonType.Design,
                    AddTips = "去创作"
                });
            }
            else if (resourceType == ResourceType.Theatre || resourceType == ResourceType.AvatarCard)
            {
                list.Add(new GoodsData()
                {
                    Id = "",
                    ButtonType = ButtonType.Design,
                    AddTips = "去创作"
                });
            }
            else if (uniqueType == UniqueType.GetAvatar(AvatarSubType.SpecialSkin))
            {
                list.Add(new GoodsData()
                {
                    Id = "0",
                    ButtonType = ButtonType.TakeOff,
                });
            }else if (resourceType == ResourceType.UgcVehicle) {
                list.Add(new GoodsData()
                {
                    Id = "",
                    ButtonType = ButtonType.Design,
                    AddTips = "去创作"
                });
                list.Add(new GoodsData()
                {
                    Id = "0",
                    ButtonType = ButtonType.TakeOff,
                });
            }
            else if (resourceType == ResourceType.Vehicle) {
                list.Add(new GoodsData()
                {
                    Id = "",
                    ButtonType = ButtonType.Design,
                    AddTips = "去创作"
                });
                list.Add(new GoodsData()
                {
                    Id = "0",
                    ButtonType = ButtonType.TakeOff,
                });
            }
            data[uniqueType] = list;

            return list;
        }

        public List<GoodsData> GetGoodsData(int uniqueType)
        {
            return data.ContainsKey(uniqueType) ? data[uniqueType] : NewList(uniqueType);
        }
    }
}
