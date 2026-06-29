using System;
using System.Collections;
using System.Collections.Generic;
using GameData.Account;
using GameData.Base;
using GameData.BaseInfo;
using UnityEngine;

namespace GameData
{
    public enum CollectType
    {
        Collect = 1,
        UnCollect = 2
    }

    [Serializable]
    public class GetUserInfoRsp
    {
        public AccountUserInfo userInfo;
        public AccountPetInfo petInfo;
        public AIBuddyInfo aibuddyInfo;
        // 大厅 AI 伙伴角色（GameData 侧复刻类型，可见性 isHidden 内置）
        public HallCharacterInfo characterInfo;
        //服务器好像没有返回载具信息，先在这里预留一个字段，防止后续需要用到
        public VehicleInfo vehicleInfo;
        public StatisticsData statistics;
        public RelationShipData relationShip;
        public AccountAmount amount;
        public List<WearingInfo> wearings;
        public List<WearingInfo> petWearings;
        public CreatorScoreInfo creatorScoreInfo;
        public DebugCfg debugCfg;
    }

    public class CreatorScoreInfo
    {
        public CreatorScoreInfoData current;
        public CreatorScoreInfoData history;
    }

    public class CreatorScoreInfoData
    {
        public int score;
        public int category;
        public int scoreFrame;//0-无，1-铁框，2-金框，3-粉金
    }

    [Serializable]
    public class DebugCfg
    {
        public string lastActiveTime;
        public string lastPaidTime;
        public string sevenDaysTotalPaid;
        public string totalPaid;
        public string channelName;
        public string country;
    }

    public enum WearingType
    {
        UGC = 0,
        PGC = 1
    }

    [Serializable]
    public class WearingInfo
    {
        public int resType;// 0:ugc 1:pgc
        public string pgcId;
        public string ugcId;
        public string bundleId;
        public string cover;
        public PaymentInfo paymentInfo;
        public int isPrivateOrder; //是否私单
    }


    [Serializable]
    public class GetPublishListRsp
    {
        public MapListRsp map;
        public SkinListRsp skin;
        public PropListRsp prop;
        public MaterialListRsp material;
        public MapListResponse musicScore;
        public MusicToneResponse musicTone;
        public MapListResponse petSkin;
        public MapListResponse anim;
        public MapListResponse petAnim;
        public MapListResponse pose;
        public MapListResponse petPose;
        public MapListResponse npc;
        public MapListResponse vehicle;
        public MapListResponse theater;
        public MapListResponse actor;
    }

    public class MusicToneResponse
    {
        public List<DraftListItem> list;
        public int isEnd;
        public string cookie;
    }

    [Serializable]
    public class MapResInfo
    {
        public MapInfo mapInfo;
        public BaseInteractInfo interactInfo;
        public BaseCreator creator;
    }

    [Serializable]
    public class SkinResInfo
    {
        public SkinInfo skinInfo;
        public BaseInteractInfo interactInfo;
        public BaseCreator creator;
    }


    [Serializable]
    public class PropResInfo
    {
        public PropInfo propInfo;
        public BaseInteractInfo interactInfo;
        public BaseCreator creator;
    }


    [Serializable]
    public class MaterialResInfo
    {
        public MaterialInfo materialInfo;
        public BaseInteractInfo interactInfo;
        public BaseCreator creator;
    }

    [Serializable]
    public class MapListRsp
    {
        public string cookie;
        public int isEnd;
        public List<MapResInfo> list;
    }

    [Serializable]
    public class SkinListRsp
    {
        public string cookie;
        public int isEnd;
        public List<SkinResInfo> list;
    }

    [Serializable]
    public class PropListRsp
    {
        public string cookie;
        public int isEnd;
        public List<PropResInfo> list;
    }

    [Serializable]
    public class MaterialListRsp
    {
        public string cookie;
        public int isEnd;
        public List<MaterialResInfo> list;
    }

    [Serializable]
    public class AIBuddyListRsp
    {
        public string cookie;
        public int isEnd;
        public List<AIBuddyInfo> list = new List<AIBuddyInfo>();
        public int totalSlot;
        public int usedSlot;
    }

    [Serializable]
    public class AIBuddySetReq
    {
        public int type;
        public AIBuddyInfo info;
    }

    [Serializable]
    public class AIBuddyInfoRsp
    {
        public AIBuddyInfo info;
        public int taskReddot;//红点数
    }
    
    [Serializable]
    public class ServerRewardInfo
    {
        public int rewardType;
        public List<string> pgcIdList;
        public int amount;
    }
}

// ===== Album (shared data) =====
// 放在 GameData 程序集中，供 UGCNetData / UI 相册等模块共用（避免跨程序集找不到类型）

public class CameraAlbumInfo
{
    public string id;
    public string cover;//缩略图
    public string coverFull;//大图
    public string creator;
    public int isDelete;//0:正常,1:删除
    public int type;//0:照片,1:视频
    public int isBan;
    public int isPublic;//0：所有相册,1:公开相册
    public locationInfo locationInfo;//位置信息
    public List<atListItem> atList;//@列表
    public int createTime;//创建时间
    public int updateTime;//更新时间
}

public class locationInfo
{
    public string mapId;
    public string locationName;//打卡点名称
    public bool isLandMark;//是否是打卡点打卡
}

public class atListItem
{
    public string uid;
    public string username;
}

public class AlbumListRes
{
    public string cookie;
    public int isEnd;
    public List<AlbumPhotoInfo> list;
    public int total;
    public int totalSlot;
}

public class SetAlbumRes
{
    public List<CameraAlbumInfo> list;
}

public class AlbumPhotoInfo
{
    public CameraAlbumInfo albumItem;
    public InteractInfo interactInfo;
    public AccountUserInfo creator;
}

public class InteractInfo
{
    public int liked;//点赞状态
    public int likeAmount;//点赞数量
    public int consumed;//消费状态
    public int consumeAmount;//消费数量
    public int collected;//收藏状态
    public int collectAmount;//收藏数量
    public int commentAmount;//评论数量
    public int rewardAmount;//打赏数量
    public int shareAmount;//分享数量
    public int heatAmount;//热度值
}
