using System.Collections.Generic;
using GameData.Base;
using GameData.BaseInfo;
using Newtonsoft.Json;

namespace GameData.UGCData
{
    public class PGCInfo
    {
        public string id;
    }

    public class UgcInfoRsp
    {
        public MapInfo mapInfo;
        public BaseCreator creator;
        public BaseInteractInfo interactInfo;
        public RelationShipInfo relationShipInfo;
        public List<AlbumPhotoInfo> albumList;
    }
    public class MapLikePhotoInfo
    {

    }
    public class SkinInfoRsp
    {
        public SkinInfo skinInfo;
        public BaseCreator creator;
        public BaseInteractInfo interactInfo;
    }
    public class ResInfo
    {
        public UgcBaseInfo ugcInfo;
        public BaseCreator creator;
        public int isPgc;
        public PGCInfo pgcInfo;
        public BaseInteractInfo interactInfo;
    }

    public class ResInfo<T> where T : UgcBaseInfo
    {
        public T ugcInfo;
        public BaseCreator creator;
        public int isPgc;
        public PGCInfo pgcInfo;
        public BaseInteractInfo interactInfo;
    }

    public class ResInfoList<T> where T : UgcBaseInfo
    {
        public string cookie;
        public int isEnd;
        public List<ResInfo<T>> list;
    }

    public class ResInfoList
    {
        public string cookie;
        public int isEnd;
        public List<ResInfo> list;
        public int permissionType;
    }

    public class DetailRsp
    {
        public MaterialInfo materialInfo;
        public PropInfo propInfo;
        public SkinInfo skinInfo;
        public BaseCreator creator;
        public BaseInteractInfo interactInfo;
        public RelationShipInfo relationShipInfo;
        public MusicScoreInfo musicScoreInfo;
        public ToneInfo musicToneInfo;
        public SkinActionInfo skinActionInfo;
        public PoseInfo poseInfo;
        public AnimInfo animInfo;
        public AnimMusicInfo animMusicInfo;
        public AINpcInfo npc;
        public VehicleInfo vehicleInfo;
        public OCTheatreAvatarInfo actorInfo;
        public OCTheatreInfo theaterInfo;
    }

    public class BatchDetailRsp
    {
        public List<DetailRsp> skinList;
    }

    public class BatchMusicScoreDetailRsp
    {
        public List<DetailRsp> musicScoreList;
    }
    
    public class BatchPoseDetailRsp
    {
        public List<DetailRsp> poseList;
    }
    
    public class BatchUgcAnimDetailRsp
    {
        public List<DetailRsp> animationList;
    }

    public class BatchUgcVehicleDetailRsp
    {
        public List<DetailRsp> vehicleList;
    }

    public class BatchActorDetailRsp
    {
        public List<DetailRsp> actorList;
    }

    public class BatchTheatreDetailRsp
    {
        [JsonProperty("theaterList")]
        public List<DetailRsp> theatreList;
    }
}