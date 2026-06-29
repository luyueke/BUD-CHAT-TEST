// @Author: YangJie
// @Description:
// @Date:  2023/07/28
// @Modify:

using Game.COSXML.Network;
using GameData.Base;
using GameData.BaseInfo;
using GameData.UGCData;
using Network.Http;
using Newtonsoft.Json;
using System.Collections.Generic;
using UGCAsset.Draft;

namespace UGCAsset {
    public class UGCSetRequest {
        public UGCOperationType setType;
        public MapInfo mapInfo;
        public SkinInfo skinInfo;
        public PropInfo propInfo;
        public MaterialInfo materialInfo;
        public MusicScoreInfo musicScoreInfo;
        public PoseInfo poseInfo;
        public ToneInfo musicToneInfo;
        public AnimInfo animInfo;
        public AnimMusicInfo animMusicInfo;
        public AINpcInfo npc;
        public VehicleInfo vehicleInfo;
        public OCTheatreAvatarInfo actorInfo;
        public OCTheatreInfo theaterInfo;
        /// <summary>
        /// 仅仅地图发布时有该字段，需要传入被覆盖的地图id
        /// </summary>
        public string overwriteId;

        /// <summary>
        /// 仅申诉时，有该字段
        /// </summary>
        public string appealReason;

        public string GetSetUrl() {
            if (mapInfo != null) {
                return HttpUrlDefine.setMap;
            } else if (skinInfo != null) {
                return HttpUrlDefine.SetSkin;
            } else if (propInfo != null) {
                return HttpUrlDefine.setProp;
            } else if (materialInfo != null) {
                return HttpUrlDefine.SetMaterial;
            } else if (musicScoreInfo != null) {
                return HttpUrlDefine.SetMusicScore;
            } else if (musicToneInfo != null)
            {
                return HttpUrlDefine.SetUGCTone;
            }
            else if (animInfo != null)
            {
                return HttpUrlDefine.setAnim;
            }
            else if (poseInfo != null)
            {
                return HttpUrlDefine.SetPose;
            }
            else if (npc != null)
            {
                return HttpUrlDefine.NpcSet;
            }
            else if(vehicleInfo != null)
            {
                return HttpUrlDefine.SetVehicle;
            }
            else if (actorInfo != null)
            {
                return HttpUrlDefine.ActorSet;
            }
            else if (theaterInfo != null)
            {
                return HttpUrlDefine.TheatreSet;
            }
            else {
                throw new System.Exception("not support type");
            }
        }

        public UGCSetRequest(UgcBaseInfo info, UGCOperationType type) {
            setType = type;
            switch (info) {
                case MapInfo mapInfoValue:
                    mapInfo = mapInfoValue;
                    break;
                case SkinInfo skinInfoValue:
                    skinInfo = skinInfoValue;
                    break;
                case PropInfo propInfoValue:
                    propInfo = propInfoValue;
                    break;
                case MaterialInfo materialInfoValue:
                    materialInfo = materialInfoValue;
                    break;
                case MusicScoreInfo musicScoreInfoValue:
                    musicScoreInfo = musicScoreInfoValue;
                    break;
                case ToneInfo toneInfo:
                    musicToneInfo = toneInfo;
                    break;
                case AnimInfo ugcAnimInfo:
                    animInfo = ugcAnimInfo;
                    break;
                case PoseInfo poseInfoValue:
                    poseInfo = poseInfoValue;
                    break;
                case AnimMusicInfo musicInfo:
                    animMusicInfo = musicInfo;
                    break;
                case AINpcInfo npcInfo:
                    npc = npcInfo;
                    break;
                case VehicleInfo vehicleInfoValue:
                    vehicleInfo = vehicleInfoValue;
                    break;
                case OCTheatreAvatarInfo actorInfoValue:
                    actorInfo = actorInfoValue;
                    break;
                case OCTheatreInfo theatreInfoValue:
                    theaterInfo = theatreInfoValue;
                    break;
                default:
                    throw new System.Exception("not support type");
            }
        }
    }



    public class UGCSkinActionSetRequest : UGCSetRequest {
        public SkinActionInfo skinActionInfo;

        public UGCSkinActionSetRequest(SkinInfo info, SkinActionInfo actionInfo, UGCOperationType type) : base(info, type) {
            this.skinActionInfo = actionInfo;
        }
    }

    public class UGCVehicleSetRequest : UGCSetRequest
    {
        public UGCVehicleSetRequest(VehicleInfo info, UGCOperationType type) : base(info, type)
        {

        }
    }

    public class BaseSetResponse {
    }

    public class BaseSetRequest {
        public UGCOperationType setType;

        /// <summary>
        /// 覆盖发布地图时，需要传入被覆盖的地图id
        /// </summary>
        public string overwriteId;

        public string ToJson() {
            var settings = new JsonSerializerSettings {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore,
            };
            return JsonConvert.SerializeObject(this, settings);
        }
    }


    public class CollectStatusRequest {
        public List<string> idList;
        public int statusType;
    }

    public class PGCInfos {
        public List<PGCCollectInfo> pgcInfos;
    }

    public class PGCCollectInfo : PGCInfo {
        public string collectStatus;
    }

    public abstract class BaseUgcInfoResponse<T> {
        public RelationShipInfo relationShipInfo;
        public BaseCreator creator;
        public BaseInteractInfo interactInfo;
        public SkinActionInfo skinActionInfo;

        public abstract T GetUgcInfo();
    }


    public class SkinSetRequest : BaseSetRequest {
        public SkinInfo skinInfo;
    }

    public class PropSetRequest : BaseSetRequest {
        public PropInfo propInfo;
    }



    public class MaterialSetRequest : BaseSetRequest {
        public MaterialInfo materialInfo;
    }


    public class SkinInfoGetResponse : BaseUgcInfoResponse<SkinInfo> {
        public SkinInfo skinInfo;

        public override SkinInfo GetUgcInfo() {
            return skinInfo;
        }
    }

    public class UGCMaterialGetResponse : BaseUgcInfoResponse<MaterialInfo> {
        public MaterialInfo materialInfo;

        public override MaterialInfo GetUgcInfo() {
            return materialInfo;
        }
    }

    public class UGCPropGetResponse : BaseUgcInfoResponse<PropInfo> {
        public PropInfo propInfo;

        public override PropInfo GetUgcInfo() {
            return propInfo;
        }
    }

    public class UGCAnimGetResponse : BaseUgcInfoResponse<AnimInfo> {
        public AnimInfo animInfo;

        public override AnimInfo GetUgcInfo() {
            return animInfo;
        }
    }

    public class UGCActorGetResponse : BaseUgcInfoResponse<OCTheatreAvatarInfo> {
        public OCTheatreAvatarInfo actorInfo;

        public override OCTheatreAvatarInfo GetUgcInfo() {
            return actorInfo;
        }
    }

}
