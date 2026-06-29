using GameData.PgcData;
using Network.Message;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Game.Database
{
    /// <summary>
    /// 背包内存数据类
    /// 实现背包数据缓存
    /// 增删改查
    /// 持有AvatarPbUpdate更新类和AvatarPbFileIO读写类
    /// </summary>
    public class BagDatabase : GlobalInstance<BagDatabase>
    {
        public bool IsInitialized { private set; get; } = false;
        public bool IsReady { private set; get; } = false;

        public HashSet<int> Keys { get; private set; } = new();

        private readonly Dictionary<int, Tuple<PbCookie, Network.Message.UserBackpack>> caches = new();
        // 分类数据
        private readonly Dictionary<int, List<string>> mClassData = new();
        // 数据字典
        private readonly Dictionary<string, InventoryData> mData = new();

        private BagPbUpdate avatarPbUpdate;

        public void Initialize(string userId)
        {
            try
            {
                if (IsInitialized && BagPbFileIO.CurUserId.Equals(userId)) return;

                #region Initialize
                // 第一次初始化执行
                if (!IsInitialized)
                {
                    AddListener();
                    // 每个部位的key用枚举名  这里只有人物服装相关的Key
                    foreach (AvatarSubType subType in Enum.GetValues(typeof(AvatarSubType)))
                    {
                        if (subType == AvatarSubType.ErrAvatarSubType) continue;
                        if (subType == AvatarSubType.All) continue;
                        if (subType == AvatarSubType.Bundle) continue;
                        Keys.Add(UniqueType.GetAvatar(subType));

                    }



                    foreach (UgcAvatarEnable subType in Enum.GetValues(typeof(UgcAvatarEnable)))
                    {
                        if (subType == UgcAvatarEnable.ErrUgcAvatarSubType) continue;
                        Keys.Add(UniqueType.GetUgcAvatar((AvatarSubType)subType));
                    }


                    foreach (PetPGCAvatarEnable subType in Enum.GetValues(typeof(PetPGCAvatarEnable)))
                    {
                        if (subType == PetPGCAvatarEnable.ErrAvatarSubType) continue;
                        Keys.Add(UniqueType.GetPGCPetAvatar((AvatarSubType)subType));
                    }


                    foreach (PetUGCAvatarEnable subType in Enum.GetValues(typeof(PetUGCAvatarEnable))) {
                        if (subType == PetUGCAvatarEnable.ErrAvatarSubType) continue;
                        Keys.Add(UniqueType.GetUGCPetAvatar((AvatarSubType)subType));
                    }

                    // 下面补其他类型的Key
                    // 动作、各种道具、鲜花、药水
                    foreach (EmoteSubType subType in Enum.GetValues(typeof(EmoteSubType)))
                    {
                        if (subType == EmoteSubType.ErrEmoteSubType) continue;
                        if (subType == EmoteSubType.SingleAll) continue;
                        if (subType == EmoteSubType.DoubleAll) continue;
                        if (subType == EmoteSubType.PetSingleAll) continue;
                        if (subType == EmoteSubType.PetWithPlayerAll) continue;
                        Keys.Add(UniqueType.Get(ResourceType.Emote, (int)subType));
                    }

                    foreach (MusicScoreSubType subType in Enum.GetValues(typeof(MusicScoreSubType)))
                    {
                        if (subType == MusicScoreSubType.ErrMusicScoreSubType) continue;
                        Keys.Add(UniqueType.Get(ResourceType.MusicScore, (int)subType));
                    }

                    foreach (UgcPoseSubType subType in Enum.GetValues(typeof(UgcPoseSubType)))
                    {
                        if (subType == UgcPoseSubType.ErrPoseSubType) continue;
                        Keys.Add(UniqueType.Get(ResourceType.UgcPose, (int)subType));
                    }

                    foreach (UgcAnimSubType subType in Enum.GetValues(typeof(UgcAnimSubType)))
                    {
                        if (subType == UgcAnimSubType.ErrAnimSubType) continue;
                        Keys.Add(UniqueType.Get(ResourceType.UgcEmote, (int)subType));
                    }

                    foreach (VehicleSubType subType in Enum.GetValues(typeof(VehicleSubType)))
                    {
                        if (subType == VehicleSubType.ErrVehicleSubType || subType == VehicleSubType.AllVehicle) continue;
                        Keys.Add(UniqueType.Get(ResourceType.Vehicle, (int)subType));
                    }

                    foreach (VehicleSubType subType in Enum.GetValues(typeof(VehicleSubType)))
                    {
                        if (subType == VehicleSubType.ErrVehicleSubType || subType == VehicleSubType.AllVehicle) continue;
                        Keys.Add(UniqueType.Get(ResourceType.UgcVehicle, (int)subType));
                    }

                    foreach (UgcTheatreSubType subType in Enum.GetValues(typeof(UgcTheatreSubType)))
                    {
                        if (subType == UgcTheatreSubType.ErrTheatreSubType) continue;
                        Keys.Add(UniqueType.Get(ResourceType.Theatre, (int)subType));
                    }

                    // OC剧场 UgcAvatarCard 类型（UGC演员卡）
                    foreach (UgcTheatreSubType subType in Enum.GetValues(typeof(UgcTheatreSubType)))
                    {
                        if (subType == UgcTheatreSubType.ErrTheatreSubType) continue;
                        Keys.Add(UniqueType.Get(ResourceType.AvatarCard, (int)subType));
                    }

                    // 伙伴角色
                    foreach (CabinCharacterSubType subType in Enum.GetValues(typeof(CabinCharacterSubType)))
                    {
                        if (subType == CabinCharacterSubType.ErrCabinCharacterSubType) continue;
                        Keys.Add(UniqueType.Get(ResourceType.CabinCharacter, (int)subType));
                    }

                    // 伙伴拓展包（皮肤包）
                    foreach (PartnerPackSubType subType in Enum.GetValues(typeof(PartnerPackSubType)))
                    {
                        if (subType == PartnerPackSubType.ErrPartnerPackSubType) continue;
                        Keys.Add(UniqueType.Get(ResourceType.PartnerPack, (int)subType));
                    }

                    // 伙伴音调（UGC 动作音调）
                    foreach (PartnerToneSubType subType in Enum.GetValues(typeof(PartnerToneSubType)))
                    {
                        if (subType == PartnerToneSubType.ErrPartnerToneSubType) continue;
                        Keys.Add(UniqueType.Get(ResourceType.PartnerTone, (int)subType));
                    }
                }

                // 重复判断用户Id是否一致，不一致就重新读取pb文件，一致就不用动
                if (!BagPbFileIO.CurUserId.Equals(userId))
                {
                    Clear();
                    avatarPbUpdate?.Dispose();
                    avatarPbUpdate = new();
                    BagPbFileIO.CurUserId = userId;
                    foreach (var key in Keys)
                    {
                        var cookie = BagPbFileIO.GetPdCookie(key);
                        var bytes = BagPbFileIO.GetPdFile(key);
                        // 版本信息为空或者版本信息的MD5和pb文件不符合，重置版本号
                        if (cookie == null || (cookie != null && bytes != null && !cookie.md5.Equals(BagPbFileIO.ComputeHash(bytes))))
                        {
                            cookie = new PbCookie()
                            {
                                cookie = 0,
                                md5 = "",
                            };
                            bytes = null;
                        }

                        if (bytes == null)
                        {
                            Network.Message.UserBackpack userBackpack = new Network.Message.UserBackpack();
                            userBackpack.DataType = key;
                            caches.Add(key, new(cookie, userBackpack));
                            mClassData.Add(key, new());
                        }
                        else
                        {
                            UpdateData(key, cookie, bytes);
                        }
                    }
                }

                IsInitialized = true;

                // 检查版本号
                CheckVersion(true, (success, change) =>
                {
                    // 成功后抛出初始化完成事件
                    if (success && change) Message.MessageHelper.Broadcast(Message.MessageName.AvaterDatabaseIsReady);

                    if (!success)
                    {
                        // 重试逻辑
                    }
                });

                #endregion
            }
            catch (Exception e)
            {
                LoggerUtils.LogError(e.Message + e.StackTrace);
            }
        }

        internal void AddListener()
        {
            Message.MessageHelper.AddListener(Message.MessageName.AvaterDatabaseCheck, OnAvaterDatabaseCheck);
            Message.MessageHelper.AddListener<string>(Message.MessageName.AvaterDatabaseUpdate, OnAvaterDatabaseUpdate);
        }

        public enum Operation
        {
            Delete,
            Add,
            Modify
        }

        public class OperationData
        {
            public Operation Operation;
            public InventoryData InventoryData;
        }

        internal void OnAvaterDatabaseUpdate(string data)
        {
            serverRawData rsp = JsonConvert.DeserializeObject<serverRawData>(data);

            if (rsp == null || rsp.data == null || rsp.data.backpackData == null || rsp.data.backpackData.pairList == null) return;
            // try
            // {
                List<InventoryData> change = new();
                rsp.data.backpackData.pairList.ForEach(pair =>
                {
                    List<OperationData> operations = new List<OperationData>();
                    pair.list.ForEach(op =>
                    {
                        operations.Add(new OperationData()
                        {
                            Operation = (Operation)op.isAdd,
                            InventoryData = new InventoryData()
                            {
                                Id = op.id,
                                ClassType = pair.dataType,
                                IsNew = (Operation)op.isAdd == Operation.Add,
                                OwnedNum = (Operation)op.isAdd == Operation.Delete ? 0 : 1,
                                Timestamp = op.timestamp,
                                Tag = op.tag,
                                Loop = op.loop
                            }
                        });
                    });

                    var _change = OnAvaterDatabaseOperation(operations);
                    if (_change.Count > 0)
                    {
                        change.AddRange(_change);
                        // 优先本地表现，但是不改版本号，后续冷起刷新数据 跳版本号 不写入版本号
                        if (IsReady && caches.ContainsKey(pair.dataType) && pair.version == caches[pair.dataType].Item1.cookie + 1)
                        {
                            caches[pair.dataType].Item1.cookie = pair.version;
                        }
                        BagPbFileIO.SetPbFile(pair.dataType, caches[pair.dataType].Item1.cookie, Google.Protobuf.MessageExtensions.ToByteArray(caches[pair.dataType].Item2));
                    }
                });

                if (change.Count > 0) Message.MessageHelper.Broadcast(Message.MessageName.AvaterDatabaseOnDataChange, change);
            // }
            // catch (Exception e)
            // {
            //     LoggerUtils.LogError(e.Message + e.StackTrace);
            // }
        }
        
        public void ClientOperation(List<OperationData> operations)
        {
            var _change = OnAvaterDatabaseOperation(operations);
            HashSet<int> saved = new();
            for (int i = 0, C = _change.Count; i < C; i++)
            {
                var dataType = _change[i].ClassType;
                if (saved.Contains(dataType)) continue;
                BagPbFileIO.SetPbFile(dataType, caches[dataType].Item1.cookie, Google.Protobuf.MessageExtensions.ToByteArray(caches[dataType].Item2));
                saved.Add(dataType);
            }
            if (_change.Count > 0) Message.MessageHelper.Broadcast(Message.MessageName.AvaterDatabaseOnDataChange, _change);
        }

        internal List<InventoryData> OnAvaterDatabaseOperation(List<OperationData> operationDatas)
        {
            List<InventoryData> change = new();
            for (int i = 0, C = operationDatas.Count; i < C; i++)
            {
                var operationData = operationDatas[i];
                var inventoryData = operationData.InventoryData;

                if (!caches.ContainsKey(inventoryData.ClassType))
                {
                    LoggerUtils.LogError($"[BagDatabase] 未知的背包类型 {inventoryData.ClassType}，跳过该操作");
                    continue;
                }

                switch (operationData.Operation)
                {
                    case Operation.Add:
                        if (Add(inventoryData))
                        {
                            
                            // 防止重复添加
                            var items = caches[inventoryData.ClassType].Item2.List.Where(i => i.Id == inventoryData.Id);
                            if (items != null) foreach (var item in items) caches[inventoryData.ClassType].Item2.List.Remove(item);
                            caches[inventoryData.ClassType].Item2.List.Insert(0, new Network.Message.UserBackpack.Types.data()
                            {
                                Id = operationData.InventoryData.Id,
                                RedDot = inventoryData.IsNew ? 1 : 0,
                                Tag = inventoryData.Tag,
                                Loop = inventoryData.Loop
                            });
                            change.Add(inventoryData);
                        }
                        break;
                    case Operation.Delete:
                        if (Delete(inventoryData))
                        {
                            var item = caches[inventoryData.ClassType].Item2.List.Single(i => i.Id == inventoryData.Id);
                            caches[inventoryData.ClassType].Item2.List.Remove(item);
                            change.Add(inventoryData);
                        }
                        break;
                    case Operation.Modify:
                        if (Modify(inventoryData))
                        {
                            // Bundle 等异步加载的类型 pb 缓存列表可能不含该 id，用 FirstOrDefault 避免 Single 抛异常导致 change 为空
                            var item = caches[inventoryData.ClassType].Item2.List.FirstOrDefault(i => i.Id == inventoryData.Id);
                            if (item != null)
                            {
                                item.RedDot = inventoryData.IsNew ? 1 : 0;
                                item.Tag = inventoryData.Tag;
                                item.Loop = inventoryData.Loop;
                            }
                            change.Add(inventoryData);
                        }
                        break;
                }
            }

            return change;
        }

        internal void OnAvaterDatabaseCheck()
        {
            CheckVersion(false, (success, change) =>
            {
                if (success && change) Message.MessageHelper.Broadcast(Message.MessageName.AvaterDatabaseOnDataRebuild);

                if (!success)
                {
                    // 重试逻辑
                }
            });
        }

        internal void CheckVersion(bool forceRefreash, Action<bool, bool> callback = null)
        {
            if (!IsInitialized)
            {
                LoggerUtils.LogError("[AvatarDataBase] 初始化还未完成，不能调用检查更新");
                callback.Invoke(false, false);
                return;
            }
            IsReady = false;

            var data = new BagCheckData() { pairList = new() };
            foreach (var kv in caches) data.pairList.Add(new PairData() { dataType = kv.Key, version = forceRefreash ? 0 : kv.Value.Item1.cookie });

            avatarPbUpdate.Check(data, (success, change) =>
            {
                if (success) IsReady = true;
                callback?.Invoke(success, change);
            });
        }

        // Pb更新更新该种类的背包数据
        internal void UpdateData(int key, PbCookie pbCookie, byte[] bytes)
        {
            caches.Remove(key);
            if (mClassData.ContainsKey(key))
            {
                mClassData[key].ForEach(id =>
                {
                    mData.Remove(id);
                });
            }
            mClassData.Remove(key);

            // 解析协议
            var bag = Network.Message.UserBackpack.Parser.ParseFrom(bytes);
            // 赋值
            for (int i = bag.List.Count - 1; i >= 0; i--)
            {
                var item = bag.List[i];
                Add(new InventoryData() { Id = item.Id, IsNew = item.RedDot > 0, OwnedNum = 1, ClassType = bag.DataType, Tag = item.Tag, Timestamp = item.Timestamp,Loop = item.Loop});
            }

            caches.Add(bag.DataType, Tuple.Create(pbCookie, bag));
        }

        internal void Clear()
        {
            IsReady = false;
            caches.Clear();
            mClassData.Clear();
            mData.Clear();
        }

        internal bool Add(InventoryData data)
        {
            if (mData == null) return false;

            if (mData.ContainsKey(data.Id))
            {
                return Modify(data);
            }
            else
            {
                mData.Add(data.Id, data);
                AddClassData(data.ClassType, data.Id);
                return true;
            }
        }

        private void AddClassData(int key, string id)
        {
            if (mClassData == null) return;

            if (mClassData.ContainsKey(key))
            {
                mClassData[key].Remove(id);
                mClassData[key].Insert(0, id);
            }
            else
            {
                mClassData.Add(key, new List<string>() { id });
            }
        }

        internal bool Delete(InventoryData data)
        {
            if (mData == null) return false;

            var success = mData.Remove(data.Id);
            if (success) DeleteClassData(data.ClassType, data.Id);
            return success;
        }

        private void DeleteClassData(int key, string id)
        {
            if (mClassData == null) return;

            if (mClassData.ContainsKey(key))
            {
                mClassData[key].Remove(id);
            }
        }

        internal bool Modify(InventoryData data)
        {
            if (mData == null) return false;

            if (!mData.ContainsKey(data.Id)) return false;
            var change = mData[data.Id].ValueCopy(data);
            return change;
        }

        /// <summary>
        /// 查找某个Id的背包数据
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public InventoryData Select(string id)
        {
            //if (!IsReady) return null;

            if (mData == null) return null;

            mData.TryGetValue(id, out InventoryData data);
            return data != null ? data.Clone() : null;
        }

        /// <summary>
        /// 获取某一类背包数据
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public List<InventoryData> SelectAll(int key)
        {
            //if (!IsReady) return null;

            if (mData == null) return null;

            if (mClassData == null || !mClassData.ContainsKey(key)) return null;

            List<InventoryData> list = new();
            var classData = mClassData[key];
            for (int i = 0, C = classData.Count; i < C; i++)
            {
                var id = classData[i];
                if (mData.ContainsKey(id)) list.Add(mData[id].Clone());
            }

            return list;
        }

        /// <summary>
        /// 效率略低，但自由度高
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public List<InventoryData> SelectAll(Func<InventoryData, bool> predicate)
        {
            //if (!IsReady) return null;

            if (mData == null) return null;

            List<InventoryData> list = new();
            foreach (var kv in mData)
            {
                if (predicate(kv.Value)) list.Add(kv.Value.Clone());
            }
            return list;
        }
    }

    [Serializable]
    public class serverRawData
    {
        public ServerBagUpdateDataRsp data;
    }

    [Serializable]
    public class ServerBagUpdateDataRsp
    {
        public ServerBagUpdateData backpackData;
        public int popupType;
    }

    [Serializable]
    public class ServerBagUpdateData
    {
        public List<ServerPairData> pairList;
    }

    [Serializable]
    public class ServerPairData
    {
        public int dataType;
        public int version;
        public List<ServerOpertionData> list;
    }

    [Serializable]
    public class ServerOpertionData
    {
        public string id;
        public int isAdd;
        public int loop;
        public BackpackTag tag;
        public long timestamp;
    }


    public class InventoryData
    {
        // 唯一Id
        public string Id;
        public int ClassType;
        // 拥有数量
        public int OwnedNum;
        // 红点标识
        public bool IsNew;
        public BackpackTag Tag;
        public int Loop;
        public long Timestamp;

        internal bool ValueCopy(InventoryData obj)
        {
            var equal = true;
            if (ClassType != obj.ClassType)
            {
                equal = false;
                ClassType = obj.ClassType;
            }

            if (OwnedNum != obj.OwnedNum)
            {
                equal = false;
                OwnedNum = obj.OwnedNum;
            }

            if (IsNew != obj.IsNew)
            {
                equal = false;
                IsNew = obj.IsNew;
            }

            if (Timestamp != obj.Timestamp)
            {
                equal = false;
                Timestamp = obj.Timestamp;
            }

            if (Loop != obj.Loop)
            {
                equal = false;
                Loop = obj.Loop;
            }

            return !equal;
        }

        public InventoryData Clone()
        {
            return (InventoryData)MemberwiseClone();
        }

        public override bool Equals(object obj)
        {
            if (obj is InventoryData)
            {
                var data = obj as InventoryData;
                return data.OwnedNum == OwnedNum && data.IsNew == IsNew;
            }
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
