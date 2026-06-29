using GameData.Base;
using GameData.BaseInfo;
using Newtonsoft.Json;
using System.Collections.Generic;

public enum SetType : int
{
    Create = 1,
    Edit = 2,
    Copy = 3,
    Publish = 4,
    Update = 5,
    Replace = 6,
    Delete = 7,
    ForcePublish = 8,
    Unpublish = 10,
}


public class BaseUgcInfoReq
{
    public int setType;
}

public class SetMapInfoReq : BaseUgcInfoReq
{
    public MapInfo mapInfo;
    public string overwriteId;
}

public class SetPropInfoReq : BaseUgcInfoReq
{
    public PropInfo propInfo;
}

public class SetMaterialInfoReq : BaseUgcInfoReq
{
    public MaterialInfo materialInfo;
}
public class SetMusicScoreInfoReq : BaseUgcInfoReq
{
    public MusicScoreInfo musicScoreInfo;
}

public class SetVehicleReq : BaseUgcInfoReq
{
    public VehicleInfo vehicleInfo;
}

public class SetSkinInfoReq : BaseUgcInfoReq
{
    public SkinInfo skinInfo;
    public SkinActionInfo SkinActionInfo;
}

public class SetAnimInfoReq : BaseUgcInfoReq
{
    public AnimInfo animInfo;
    public PoseInfo poseInfo;
}

public class SetUgcAnimMusicReq : BaseUgcInfoReq
{
    public AnimMusicInfo animMusicInfo;
}

public class SetAINpcInfoReq : BaseUgcInfoReq
{
    public AINpcInfo npc;
}

public class SetAINpcInfoRep
{
    public AINpcInfo npc;
}

public class SetActorInfoReq : BaseUgcInfoReq
{
    public OCTheatreAvatarInfo actorInfo;
}

public class SetTheatreInfoReq : BaseUgcInfoReq
{
    public OCTheatreInfo theaterInfo;
}

public class MapInfoReq
{
    public string id;
}

public class MapListReq
{
    public string uid;
    public string cookie;
    public string isEnd;
    public int ugcType;
    public int gameType;//0 普通地图，1 ai game 地图
    public int gameId;
}

public class SkinListReq
{
    public string uid;
    public string cookie;
    public string isEnd;
    public int subType;
}

public class MaterialListReq
{
    public string uid;
    public string cookie;
    public string isEnd;
}

public class DraftListItem
{
    public MapInfo mapInfo;
    public SkinInfo skinInfo;
    public PropInfo propInfo;
    public MaterialInfo materialInfo;
    public MusicScoreInfo musicScoreInfo;
    public BaseCreator creator;
    public BaseInteractInfo interactInfo;
    public SkinActionInfo skinActionInfo;
    public ToneInfo musicToneInfo;

    #region UGCAnimInfo
    public AnimInfo animInfo;
    public PoseInfo poseInfo;
    public AnimMusicInfo animMusicInfo;
    #endregion
    public AINpcInfo npc;
    public VehicleInfo vehicleInfo;

    public OCTheatreAvatarInfo actorInfo;
    [JsonProperty("theaterInfo")]
    public OCTheatreInfo theatreInfo;

    public T Get<T>() where T : UgcBaseInfo
    {
        if (typeof(T) == typeof(MapInfo))
            return mapInfo as T;
        else if (typeof(T) == typeof(SkinInfo))
            return skinInfo as T;
        else if (typeof(T) == typeof(PropInfo))
            return propInfo as T;
        else if (typeof(T) == typeof(MaterialInfo))
            return materialInfo as T;
        else if (typeof(T) == typeof(MusicScoreInfo))
            return musicScoreInfo as T;
        else if (typeof(T) == typeof(ToneInfo))
            return musicToneInfo as T;
        else if (typeof(T) == typeof(AnimInfo))
            return animInfo as T;
        else if (typeof(T) == typeof(PoseInfo))
            return poseInfo as T;
        else if (typeof(T) == typeof(AnimMusicInfo))
            return animMusicInfo as T;
        else if (typeof(T) == typeof(AINpcInfo))
            return npc as T;
        else if (typeof(T) == typeof(VehicleInfo))
            return vehicleInfo as T;
        else if (typeof(T) == typeof(OCTheatreAvatarInfo))
            return actorInfo as T;
        else if (typeof(T) == typeof(OCTheatreInfo))
            return theatreInfo as T;
        else
            throw new System.Exception("not support type");
    }

    public bool selected;
}


public class MapListResponse
{
    public List<DraftListItem> list;
    public int isEnd;
    public string cookie;
}
public class AvatarStudioListReq
{
    public string uid;
    public string cookie;
    public string isEnd;
    public int subType;//1 衣服，99全部
    public int skinType;
    public int currencyType;
}

public class GetBannedUgcsResponse
{
    public List<UgcBaseInfo> list;
}

