using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Es;
using GameData.BaseInfo;
using GameData.PgcData;
using Newtonsoft.Json;
using UnityEngine;

namespace Game.Avatar
{


    public class CharacterPartData: ICloneable
    {
        [JsonProperty(NullValueHandling=NullValueHandling.Ignore)]
        public string Id;   // PgcId or UgcTemplateId
        [JsonIgnore]
        public int Type;
        [JsonProperty(DefaultValueHandling= DefaultValueHandling.Ignore)]
        public int LRType;  // 左右手
        [JsonProperty(NullValueHandling=NullValueHandling.Ignore)]
        public string Cr;   // 颜色
        [JsonProperty(NullValueHandling=NullValueHandling.Ignore)]
        public Vec3 Pos;    // 位置
        [JsonProperty(NullValueHandling=NullValueHandling.Ignore)]
        public Vec3 Rot;    // 旋转
        [JsonProperty(NullValueHandling=NullValueHandling.Ignore)]
        public Vec3 Sca;    // 缩放
        [JsonProperty(NullValueHandling=NullValueHandling.Ignore)]
        public Vec3 CSca;   // 子节点缩放
        [JsonProperty(NullValueHandling=NullValueHandling.Ignore)]
        public string UId;  // UGCId
        [JsonProperty(NullValueHandling=NullValueHandling.Ignore)]
        public string Url;  // UGC资源远程路径
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Vec3 CAnchor; // 子节点锚点
        [JsonProperty(DefaultValueHandling= DefaultValueHandling.Ignore)]
        public int UgcStyle;  // template模版shader风格 0：默认 1：动漫风格
        public bool IsNull()
        {
            return string.IsNullOrEmpty(Id) || Id.Equals("0");
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }

    }

    public class FeatureItem
    {
        public int type;
        public string id;
    }
    /// <summary>
    /// 禁止直接通过Newtonsoft.Json 序列化和反序列化，需要使用内部方法DeserializeObject
    /// </summary>
    public class CharacterData : BaseAvatarData
    {
        public List<CharacterPartData> partDatas;
        public List<FeatureItem> featureItems;

        public int ver = 0;

        public int bodyType = 0;//体型

        public VehicleInfo vehicleData;

        public CharacterData Clone()
        {
            CharacterData data = new CharacterData();
            data.partDatas = new List<CharacterPartData>();
            if (partDatas != null)
            {
                foreach (var partData in partDatas)
                {
                    data.partDatas.Add(partData.Clone() as CharacterPartData);
                }
            }
            data.bodyType = bodyType;
            data.vehicleData = vehicleData;
            return data;
        }

        public void ChangeSkinData(SkinInfo skinInfo) {
            var resData = Es.DataTables.GetGameResData(skinInfo.templateId);
            //TODO:低版本看高版本会报错
            if (resData == null)
            {
                return;
            }
            var ugc = UniqueType.GetUgcAvatar((AvatarSubType)resData.SubType);
            var pgc = UniqueType.GetAvatar((AvatarSubType)resData.SubType);
            var parData = partDatas.Find(x => x.Type == ugc);
            if (parData == null) {
                parData = new CharacterPartData();
                parData.Type = ugc;
                partDatas.Add(parData);
            }
            parData.Id = skinInfo.templateId;
            parData.UId = skinInfo.id;
            if (skinInfo.isProp) {
                parData.Url = skinInfo.metaDataUrl;
            } else {
                parData.Url = skinInfo.clothesUrl;
            }
            parData.UgcStyle = skinInfo.ugcStyle;

            if (skinInfo.skinDetailInfo != null) {
                parData.CAnchor = skinInfo.skinDetailInfo.anchor;
                parData.Pos = skinInfo.skinDetailInfo.pDef;
                parData.Rot = skinInfo.skinDetailInfo.rDef;
                parData.Sca = skinInfo.skinDetailInfo.sDef;
            }

            var pgcData = partDatas.Find(x => x.Type == pgc);
            if (pgcData != null)
            {
                partDatas.Remove(pgcData);
            }
        }

        public void AddVehicleData(VehicleInfo vehicleInfo)
        {
            vehicleData = vehicleInfo;
            var type = UniqueType.GetUgcVehicle((VehicleSubType)vehicleInfo.vehicleType);
            var parData = partDatas.Find(x => x.Type == type);

            if (parData == null)
            {
                parData = new CharacterPartData();
                parData.Type = type;
                partDatas.Add(parData);
            }
            parData.Id = vehicleInfo.templateId;
            parData.UId = vehicleInfo.id;
            parData.Url = vehicleInfo.metaDataUrl;
            //parData.UgcStyle = vehicleInfo.ugcStyle;
            if (vehicleInfo.detailInfo != null)
            {
                parData.CAnchor = vehicleInfo.detailInfo.anchor;
                parData.Pos = vehicleInfo.detailInfo.pDef;
                parData.Rot = vehicleInfo.detailInfo.rDef;
                parData.Sca = vehicleInfo.detailInfo.sDef;
            }

            parData.Url = vehicleInfo.metaDataUrl;
        }

        public void RemoveVehicleData()
        {
            vehicleData = null;
            var type = UniqueType.GetUgcVehicle(VehicleSubType.SingleVehicle);
            var doubleType = UniqueType.GetUgcVehicle(VehicleSubType.DoubleVehicle);
            var parData = partDatas.Find(x => x.Type == type);
            if (parData != null)
            {
                partDatas.Remove(parData);
            }
            var doublePartData = partDatas.Find(x => x.Type == doubleType);
            if (doublePartData != null)
            {
                partDatas.Remove(doublePartData);
            }
        }

        public override CharacterPartData GetPartData(int subType)
        {
            if (partDatas != null)
            {
                foreach (var partData in partDatas)
                {
                    if (partData.Type == subType)
                    {
                        return partData;
                    }
                }
            }
            return null;
        }


        public static CharacterData DeserializeObject(string avatarJson)
        {
            var tempData = JsonConvert.DeserializeObject<CharacterData>(avatarJson);
            List<int> partEnumList = new List<int>();
            if (tempData != null && tempData.partDatas != null)
            {
                var tempList = new List<CharacterPartData>();
                tempList.AddRange(tempData.partDatas);
                foreach (var partData in tempList)
                {
                    var partPgcCfg = Es.DataTables.GetGameResData(partData.Id);
                    if (partPgcCfg == null)
                    {
                        tempData.partDatas.Remove(partData);
                        if(partData.Id != "0")
                            LoggerUtils.LogError($"找不到部件Pgc配置:{partData.Id}");
                        continue;
                    }

                    // 特殊部位，需要做个兼容逻辑，把之前部位的东西放到特殊部位
                    var specialSkin = Es.DataTables.GetSpecialSkinConfig(partData.Id);
                    if (specialSkin != null)
                    {
                        var specialSkinPartType = UniqueType.Get(partPgcCfg.ResourceType, (int)AvatarSubType.SpecialSkin);
                        partData.Type = specialSkinPartType;
                        partEnumList.Add(specialSkinPartType);
                        continue;
                    }

                    var partType = UniqueType.Get(partPgcCfg.ResourceType, partPgcCfg.SubType);
                    partData.Type = partType;
                    partEnumList.Add(partType);
                }

                var allPartEnum = Enum.GetValues(typeof(AvatarSubType));
                foreach (AvatarSubType partValue in allPartEnum)
                {
                    if (partValue == AvatarSubType.ErrAvatarSubType) continue;
                    int pEnum = UniqueType.GetAvatar(partValue);
                    if (!partEnumList.Contains(pEnum))
                    {
                        CharacterPartData data = new CharacterPartData()
                        {
                            // 老版本看新版本衣服，需要显示默认紫色衣服
                            Id = partValue == AvatarSubType.Clothes ? "10400001" : "0",
                            Type = pEnum
                        };

#if PACKAGE_TYPE_US


                        // 头部默认值
                        if (tempData.ver == 0 && partValue == AvatarSubType.Head) {
                            data.Id = "12300001";
                        }
#endif
                        // 皮肤默认值
                        if (partValue == AvatarSubType.Skin) {
                            data.Id = "12200000";
                            data.Cr = "#C192FF";
                        }

                        tempData.partDatas.Add(data);
                    }
                }
            }



            return tempData;
        }

        public string GetSpecialSkinId() {
            var specialSkinPartType = UniqueType.Get(ResourceType.Avatar, (int)AvatarSubType.SpecialSkin);
            return partDatas.FirstOrDefault(tmp => tmp.Type == specialSkinPartType)?.Id;
        }

        public CharacterPartData GetSpecialSkinPartData()
        {
            var specialSkinPartType = UniqueType.Get(ResourceType.Avatar, (int)AvatarSubType.SpecialSkin);
            return partDatas.FirstOrDefault(tmp => tmp.Type == specialSkinPartType);
        }

        public static string SerializeObject(CharacterData chaData)
        {
            string jsonContent = string.Empty;
            if (chaData != null && chaData.partDatas != null)
            {
                int count = chaData.partDatas.Count;
                for (int i = count - 1; i >= 0; i--)
                {
                    var data = chaData.partDatas[i];
                    if (string.IsNullOrEmpty(data.Id) || data.Id.Equals("0"))
                    {
                        chaData.partDatas.RemoveAt(i);
                    }
                }

                JsonSerializerSettings setting = new JsonSerializerSettings();
                setting.DefaultValueHandling = DefaultValueHandling.Ignore;
                setting.NullValueHandling = NullValueHandling.Ignore;
                chaData.ver = AvatarDataManager.CharacterVersion;
                jsonContent = JsonConvert.SerializeObject(chaData, setting);
            }

            return jsonContent;
        }

        /// <summary>
        /// 处理角色数据的特殊行为
        /// </summary>
        public static CharacterData ProcessSpecialBehavior(CharacterData data)
        {
            if (data == null) return null;
            
            List<int> partEnumList = new List<int>();

            if (data.partDatas != null)
            {
                var tempList = new List<CharacterPartData>();
                tempList.AddRange(data.partDatas);
                
                // 处理现有部件数据
                foreach (var partData in tempList)
                {
                    var partPgcCfg = Es.DataTables.GetGameResData(partData.Id);
                    if (partPgcCfg == null)
                    {
                        data.partDatas.Remove(partData);
                        if(partData.Id != "0")
                            LoggerUtils.LogError($"找不到部件Pgc配置:{partData.Id}");
                        continue;
                    }

                    // 特殊皮肤兼容处理
                    var specialSkin = Es.DataTables.GetSpecialSkinConfig(partData.Id);
                    if (specialSkin != null)
                    {
                        var specialSkinPartType = UniqueType.Get(partPgcCfg.ResourceType, (int)AvatarSubType.SpecialSkin);
                        partData.Type = specialSkinPartType;
                        partEnumList.Add(specialSkinPartType);
                        continue;
                    }

                    var partType = UniqueType.Get(partPgcCfg.ResourceType, partPgcCfg.SubType);
                    partData.Type = partType;
                    partEnumList.Add(partType);
                }

                // 处理默认值
                var allPartEnum = Enum.GetValues(typeof(AvatarSubType));
                foreach (AvatarSubType partValue in allPartEnum)
                {
                    if (partValue == AvatarSubType.ErrAvatarSubType) continue;
                    int pEnum = UniqueType.GetAvatar(partValue);
                    if (!partEnumList.Contains(pEnum))
                    {
                        CharacterPartData newData = new CharacterPartData()
                        {
                            Id = partValue == AvatarSubType.Clothes ? "10400001" : "0",
                            Type = pEnum
                        };

#if PACKAGE_TYPE_US
                        // 头部默认值
                        if (data.ver == 0 && partValue == AvatarSubType.Head) {
                            newData.Id = "12300001";
                        }
#endif
                        // 皮肤默认值
                        if (partValue == AvatarSubType.Skin) {
                            newData.Id = "12200000";
                            newData.Cr = "#C192FF";
                        }

                        data.partDatas.Add(newData);
                    }
                }
            }

            return data;
        }

    }


    public class AvatarDataManager : GlobalInstance<AvatarDataManager>
    {
        public const int CharacterVersion = 1;//人物数据版本号，兼容海外数据
        public CharacterData SelfCharacterData { get; set; }

        public PetData SelfPetData { get; set; }

        /// <summary>
        /// UGC 部位映射
        /// </summary>
        public Dictionary<AvatarSubType, AvatarSubType> ugcPartMap = new Dictionary<AvatarSubType, AvatarSubType>()
        {
            //{AvatarSubType.UGCClothes, AvatarSubType.Clothes},
            {AvatarSubType.Clothes, AvatarSubType.Clothes }
        };

        public List<AvatarSubType> ugcPartList = new List<AvatarSubType>() {AvatarSubType.Clothes };

        public void InitSelfData(int gender)
        {
            SelfCharacterData = GetDefaultDataByGender(gender);
        }


        public CharacterData GetDefaultDataByGender(int gender)
        {
            var menData = Es.DataTables.GetInitialAvatar(gender.ToString());
            var data = CharacterData.DeserializeObject(menData.Content);
            return data;
        }



    }

    [SerializeField]
    public class CharacterDressData
    {
        public CharacterData characterData;
        public string coverUrl;
    }
}
