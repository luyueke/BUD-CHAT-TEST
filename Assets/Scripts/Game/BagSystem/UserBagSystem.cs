using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EasySpreadsheet;
using Game.Avatar;
using GameData.PgcData;
using Message;
using UnityEngine;


namespace Game.BagSystem
{
    public class BaseUserBagData
    {
        public string id;

        public BaseUserBagData(string _id)
        {
            id = _id;
        }
    }


    public class UserCountBagData : BaseUserBagData
    {
        public int count;

        public UserCountBagData(string _id) : base(_id)
        {
        }
    }

public class UserBagSystem: GlobalInstance<UserBagSystem>
{
    public Dictionary<int,List<BaseUserBagData>> allBagDatas;
    
        public void Initialize()
        {
            allBagDatas = new Dictionary<int, List<BaseUserBagData>>();
            var itemDatas = Es.DataTables.GetAvatarCommonDataList();
            
            //TODO:fsc 背包改造--判断ResourceType，支持不同种类资源
            //TODO:fsc 背包改造--维护免费+已购买的资源
            
            foreach (var commonData in itemDatas)
            {
                var bagData = new BaseUserBagData(commonData.PgcId);
                if (!allBagDatas.ContainsKey(commonData.SubType))
                {
                    allBagDatas.Add(commonData.SubType,new List<BaseUserBagData>());
                }
                allBagDatas[commonData.SubType].Add(bagData);
            }

            //UI分类中首个非资源
            var canNullList = Es.DataTables.GetUIAvatarNullDataList();
            List<AvatarSubType> canNullDic = new List<AvatarSubType>();
            foreach (var data in canNullList)
            {
                if (data.isNull == 1)
                {
                    canNullDic.Add((AvatarSubType)data.ResType);
                }
            }
            foreach (var key in allBagDatas.Keys)
            {
                var partEnum = (AvatarSubType) key;
                if (canNullDic.Contains(partEnum))
                {
                    allBagDatas[key].Insert(0,new BaseUserBagData("0"));
                }
            }
            
            // var scarfList = Es.DataTables.GetAvatarDataScarfList();
            // var scarfDatas = scarfList.Select(x => new BaseUserBagData(x.Id)).ToList();
            // allBagDatas.Add((int) AvatarPartEnum.Scarf, scarfDatas);
            //
            // var beltList = Es.DataTables.GetAvatarDataBeltList();
            // var beltDatas = beltList.Select(x => new BaseUserBagData(x.Id)).ToList();
            // allBagDatas.Add((int) AvatarPartEnum.Belt, beltDatas);
            //
            // var browList = Es.DataTables.GetAvatarDataBrowList();
            // var browDatas = browList.Select(x => new BaseUserBagData(x.Id)).ToList();
            // allBagDatas.Add((int) AvatarPartEnum.Brow, browDatas);
            //
            // var clothesList = Es.DataTables.GetAvatarDataClothesList();
            // var clothesDatas = clothesList.Select(x => new BaseUserBagData(x.Id)).ToList();
            // allBagDatas.Add((int) AvatarPartEnum.Clothes, clothesDatas);
            //
            // var effectList = Es.DataTables.GetAvatarDataEffectList();
            // var effectDatas = effectList.Select(x => new BaseUserBagData(x.Id)).ToList();
            // allBagDatas.Add((int) AvatarPartEnum.Effect, effectDatas);
            //
            // var eyesList = Es.DataTables.GetAvatarDataEyeList();
            // var eyesDatas = eyesList.Select(x => new BaseUserBagData(x.Id)).ToList();
            // allBagDatas.Add((int) AvatarPartEnum.Eyes, eyesDatas);
            //
            // var glassesList = Es.DataTables.GetAvatarDataGlassesList();
            // var glassesDatas = glassesList.Select(x => new BaseUserBagData(x.Id)).ToList();
            // allBagDatas.Add((int) AvatarPartEnum.Glasses, glassesDatas);
            //
            // var hairList = Es.DataTables.GetAvatarDataHairList();
            // var hairDatas = hairList.Select(x => new BaseUserBagData(x.Id)).ToList();
            // allBagDatas.Add((int) AvatarPartEnum.Hair, hairDatas);
            //
            // var hatsList = Es.DataTables.GetAvatarDataHatsList();
            // var hatsDatas = hatsList.Select(x => new BaseUserBagData(x.Id)).ToList();
            // allBagDatas.Add((int) AvatarPartEnum.Hats, hatsDatas);
            //
            // var gloveList = Es.DataTables.GetAvatarDataHandList();
            // var gloveDatas = gloveList.Select(x => new BaseUserBagData(x.Id)).ToList();
            // allBagDatas.Add((int) AvatarPartEnum.Glove, gloveDatas);
            //
            // var mouseList = Es.DataTables.GetAvatarDataMouseList();
            // var mouseDatas = mouseList.Select(x => new BaseUserBagData(x.Id)).ToList();
            // allBagDatas.Add((int) AvatarPartEnum.Mouse, mouseDatas);
            //
            // var facePaintList = Es.DataTables.GetAvatarDataFacePaintList();
            // var facePaintDatas = facePaintList.Select(x => new BaseUserBagData(x.Id)).ToList();
            // allBagDatas.Add((int) AvatarPartEnum.FacePaint, facePaintDatas);
            //
            // var shoeList = Es.DataTables.GetAvatarDataShoeList();
            // var shoeDatas = shoeList.Select(x => new BaseUserBagData(x.Id)).ToList();
            // allBagDatas.Add((int) AvatarPartEnum.Shoe, shoeDatas);
            //
            // var bagList = Es.DataTables.GetAvatarDataBagList();
            // var bagDatas = bagList.Select(x => new BaseUserBagData(x.Id)).ToList();
            // allBagDatas.Add((int) AvatarPartEnum.Backpack, bagDatas);
            //
            // var capeList = Es.DataTables.GetAvatarDataCapeList();
            // var capeDatas = capeList.Select(x => new BaseUserBagData(x.Id)).ToList();
            // allBagDatas.Add((int) AvatarPartEnum.Cape, capeDatas);
            //
            // var crossbodyList = Es.DataTables.GetAvatarDataCrossbodyList();
            // var crossbodyDatas = crossbodyList.Select(x => new BaseUserBagData(x.Id)).ToList();
            // allBagDatas.Add((int) AvatarPartEnum.Crossbody, crossbodyDatas);
            //
            // var earList = Es.DataTables.GetAvatarDataEarList();
            // var earDatas = earList.Select(x => new BaseUserBagData(x.Id)).ToList();
            // allBagDatas.Add((int) AvatarPartEnum.Earring, earDatas);
            //
            // var noseList = Es.DataTables.GetAvatarDataNoseList();
            // var noseDatas = noseList.Select(x => new BaseUserBagData(x.Id)).ToList();
            // allBagDatas.Add((int) AvatarPartEnum.Nose, noseDatas);
            //
            // var blushList = Es.DataTables.GetAvatarDataBlushList();
            // var blushDatas = blushList.Select(x => new BaseUserBagData(x.Id)).ToList();
            // allBagDatas.Add((int) AvatarPartEnum.Blush, blushDatas);
        }

        public List<BaseUserBagData> GetAvatarBagDatasByType<T>(int resType) where T : BaseUserBagData
        {
            var subType = (int)UniqueType.AvatarSubType(resType);
            return allBagDatas.ContainsKey(subType) ?allBagDatas[resType] : null;
        }


        public void AddBagData<T>(T data, bool isInsertFirst = true) where T : BaseUserBagData
        {
            var cData = Es.DataTables.GetGameResData(data.id);
            if (!allBagDatas.ContainsKey(cData.SubType))
            {
                allBagDatas.Add(cData.SubType, new List<BaseUserBagData>());
            }

            List<BaseUserBagData> bagDatas = allBagDatas[cData.SubType];
            if (bagDatas.FindIndex(x => x.id == data.id) >= 0)
            {
                Debug.LogError($"已经存在当前物品Id{data.id}，重复添加");
                return;
            }

            if (isInsertFirst)
            {
                bagDatas.Insert(0, data);
            }
            else
            {
                bagDatas.Add(data);
            }

            MessageHelper.Broadcast(MessageName.AddRedDot, data.id, cData.SubType);
        }

        public void RemoveDataById<T>(string id) where T : BaseUserBagData
        {
            foreach (var keyValue in allBagDatas)
            {
                bool isExist = false;
                BaseUserBagData rmvData = null;
                foreach (var bagData in keyValue.Value)
                {
                    if (bagData.id.Equals(id))
                    {
                        isExist = true;
                        rmvData = bagData;
                    }
                }

                if (isExist)
                {
                    keyValue.Value.Remove(rmvData);
                    MessageHelper.Broadcast(MessageName.RemoveRedDot, keyValue.Key, id);
                }
            }
        }

        public void AddUGCBagData<T>(T data, int resType, bool isInsertFirst = true) where T : BaseUserBagData
        {
            if (!allBagDatas.ContainsKey(resType))
            {
                allBagDatas.Add(resType, new List<BaseUserBagData>());
            }

            List<BaseUserBagData> bagDatas = allBagDatas[resType];
            if (bagDatas.FindIndex(x => x.id == data.id) >= 0)
            {
                Debug.LogError($"已经存在当前物品Id{data.id}，重复添加");
                return;
            }

            if (isInsertFirst)
            {
                bagDatas.Insert(0, data);
            }
            else
            {
                bagDatas.Add(data);
            }

            MessageHelper.Broadcast(MessageName.AddRedDot, data.id, resType, 1);
        }

        public void RemoveUGCDataById<T>(string id, int resType) where T : BaseUserBagData
        {
            allBagDatas.TryGetValue(resType, out var bagDatas);

            if (bagDatas != null)
            {
                var bagData = bagDatas.Find(bagData => bagData.id == id);
                if (bagData != null)
                {
                    bagDatas.Remove(bagData);
                    MessageHelper.Broadcast(MessageName.RemoveRedDot, resType, id);
                }
            }

        }

        public T GetDataById<T>(string id) where T : BaseUserBagData
        {
            foreach (var keyValue in allBagDatas)
            {
                foreach (var bagData in keyValue.Value)
                {
                    if (bagData.id.Equals(id))
                    {
                        return (T) bagData;
                    }
                }
            }

            return null;
        }

        public void AddBagDataByCount<T>(T data, bool isInsertFirst = true) where T : UserCountBagData
        {
            var cData = Es.DataTables.GetGameResData(data.id);
            if (!allBagDatas.ContainsKey(cData.SubType))
            {
                allBagDatas.Add(cData.SubType, new List<BaseUserBagData>());
            }

            List<BaseUserBagData> bagDatas = allBagDatas[cData.SubType];

            var userBagData = bagDatas.Find(x => x.id == data.id);
            if (userBagData != null)
            {
                var uData = userBagData as UserCountBagData;
                uData.count += data.count;
            }
            else
            {
                if (isInsertFirst)
                {
                    bagDatas.Insert(0, data);
                }
                else
                {
                    bagDatas.Add(data);
                }
            }
        }

        public void RemoveCountDataById<T>(string id, int useCount) where T : BaseUserBagData
        {
            foreach (var keyValue in allBagDatas)
            {
                bool isExist = false;
                UserCountBagData rmvData = null;
                foreach (var bagData in keyValue.Value)
                {
                    if (bagData.id.Equals(id))
                    {
                        isExist = true;
                        rmvData = (UserCountBagData) bagData;
                        if (rmvData.count < useCount)
                        {
                            Debug.LogError($"{id}物品消耗数量异常,当前数量{rmvData.count}，需要消耗{useCount}");
                        }
                        else
                        {
                            rmvData.count -= useCount;
                        }
                    }
                }

            }
        }
    }
}
