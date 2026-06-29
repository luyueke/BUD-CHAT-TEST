using System;
using System.Collections.Generic;
using Game.Avatar;
using GameData.BaseInfo;
using GameData.PgcData;
using Newtonsoft.Json;

namespace Game.Avatar {

    /// <summary>
    /// 禁止直接通过Newtonsoft.Json 序列化和反序列化，需要使用内部方法DeserializeObject
    /// </summary>
    public class PetData : BaseAvatarData
    {
        public List<CharacterPartData> partDatas;

        public int bodyType = 0;//体型
        public PetData Clone() {
            PetData data = new PetData();
            data.partDatas = new List<CharacterPartData>();
            if (partDatas != null) {
                foreach (var partData in partDatas) {
                    data.partDatas.Add(partData.Clone() as CharacterPartData);
                }
            }
            return data;
        }
        
        public void ChangeSkinData(SkinInfo skinInfo) {
            var resData = Es.DataTables.GetGameResData(skinInfo.templateId);
            var ugc = UniqueType.GetUGCPetAvatar((AvatarSubType)resData.SubType);
            var pgc = UniqueType.GetPGCPetAvatar((AvatarSubType)resData.SubType);
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




        public override CharacterPartData GetPartData(int subType) {
            if (partDatas != null) {
                foreach (var partData in partDatas) {
                    if (partData.Type == subType) {
                        return partData;
                    }
                }
            }
            return null;
        }


        public static PetData DeserializeObject(string avatarJson) {
            var tempData = JsonConvert.DeserializeObject<PetData>(avatarJson);
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
                        LoggerUtils.LogError($"找不到部件Pgc配置:{partData.Id}");
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
                    int pEnum = UniqueType.GetPGCPetAvatar(partValue);
                    if (!partEnumList.Contains(pEnum))
                    {
                        CharacterPartData partData = new CharacterPartData()
                        {
                            Id = "0",
                            Type = pEnum
                        };

                        switch (partValue) {
                            case AvatarSubType.Skin:
                                partData.Id = "72200001";
                                partData.Cr = "#C192FF";
                                break;
                            case AvatarSubType.Size:
                                partData.Id =  "79700001";
                                partData.Sca = new Vec3(1, 1, 1);
                                break;
                            default:
                                break;
                        }

                        tempData.partDatas.Add(partData);
                    }
                }
            }



            return tempData;
        }

        public static string SerializeObject(PetData chaData) {
            string jsonContent = string.Empty;
            if (chaData != null && chaData.partDatas != null) {
                int count = chaData.partDatas.Count;
                for (int i = count - 1; i >= 0; i--) {
                    var data = chaData.partDatas[i];
                    if (string.IsNullOrEmpty(data.Id) || data.Id.Equals("0")) {
                        chaData.partDatas.RemoveAt(i);
                    }
                }

                JsonSerializerSettings setting = new JsonSerializerSettings();
                setting.DefaultValueHandling = DefaultValueHandling.Ignore;
                setting.NullValueHandling = NullValueHandling.Ignore;
                jsonContent = JsonConvert.SerializeObject(chaData, setting);
            }

            return jsonContent;
        }
    }


}
