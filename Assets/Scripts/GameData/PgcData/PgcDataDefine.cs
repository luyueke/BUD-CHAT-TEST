using Es;
using System;

namespace GameData.PgcData
{
    /// <summary>
    /// 包含PGC资源大类型和子类型的定义，枚举值和后端保持一致
    /// </summary>
    public enum ResourceType
    {
        ErrResourceType = 0,
        Avatar = 1,         // 衣服部件
        GameProp = 2,       // 游戏内道具
        Currency = 3,       // 货币
        Emote = 4,          // 表情
        UgcAvatar = 5,      // UGC衣服部件
        MusicScore = 6,     // 乐谱

        PGCPetAvatar = 7,    // 宠物 PGC 皮肤

        UGCPetAvatar = 8,    // 宠物 UGC 皮肤

        UgcEmote = 9, //UGC Emote
        Pose = 10, // 官方姿势
        UgcPose = 11,//UGC Pose
        ChatBubble = 12, //s6新增 聊天气泡
        AvatarFrame = 13, //s6新增 头像框
        Vehicle = 16, // s13新增，载具
        UgcVehicle = 17, //s13新增，ugc载具
        AvatarCard = 20, //s15新增，OC剧场角色卡
        Theatre = 21, //s15新增，OC剧场剧本
        CabinCharacter = 22, //s15新增，伙伴角色
        PartnerTone = 23,    //伙伴音调（UGC 动作音调）
        PartnerPack = 24,    //伙伴拓展包（皮肤包）
        UGCBoxScene = 97,   //UGCBOX场景
        CameraSelfiePose = 98, //s14新增，相机自拍
        Other = 99,         // 其他类型
    }


    public static class UniqueType
    {
        public static int Get(int resourceType, int subType)
        {
            return resourceType * 10000 + subType;
        }

        public static ResourceType ResourceType(int type)
        {
            return (ResourceType)(type / 10000);
        }

        public static AvatarSubType AvatarSubType(int type)
        {
            return (AvatarSubType)(type % 10000);
        }

        public static EmoteSubType EmoteSubType(int type)
        {
            return (EmoteSubType)(type % 10000);
        }

        public static UgcAnimSubType UgcEmoteSubType(int type)
        {
            return (UgcAnimSubType)(type % 10000);
        }

        public static UgcPoseSubType UgcPoseSubType(int type)
        {
            return (UgcPoseSubType)(type % 10000);
        }

        public static VehicleSubType VehicleSubType(int type)
        {
            return (VehicleSubType)(type % 10000);
        }

        public static bool UgcAvatarEnable(int type)
        {
            var resourceType = ResourceType(type);

            if (resourceType == PgcData.ResourceType.PGCPetAvatar || resourceType == PgcData.ResourceType.UGCPetAvatar) {
                return Enum.IsDefined(typeof(PetUGCAvatarEnable), type % 10000);
            } else if (resourceType == PgcData.ResourceType.UgcAvatar || resourceType == PgcData.ResourceType.Avatar ) {
                return Enum.IsDefined(typeof(UgcAvatarEnable), type % 10000);
            }
            return false;
        }



        public static int Get(ResourceType resourceType, int subType)
        {
            return Get((int)resourceType, subType);
        }

        public static int GetAvatar(AvatarSubType type)
        {
            return Get(PgcData.ResourceType.Avatar, (int)type);
        }
       
        public static int GetAvatar(string pgcId)
        {
            var config = DataTables.GetAvatarCommonData(pgcId);
            if (config == null) return 0;
            var classType = GetAvatar((AvatarSubType)config.SubType);
            var specialConfig = DataTables.GetSpecialSkinConfig(pgcId);
            if (specialConfig != null) classType = UniqueType.GetAvatar(PgcData.AvatarSubType.SpecialSkin);
            return classType;
        }

        public static int GetUgcAvatar(AvatarSubType type)
        {
            return Get(PgcData.ResourceType.UgcAvatar, (int)type);
        }

        public static int GetPGCPetAvatar(AvatarSubType type) {
            return Get(PgcData.ResourceType.PGCPetAvatar, (int)type);
        }

        public static int GetUGCPetAvatar(AvatarSubType type) {
            return Get(PgcData.ResourceType.UGCPetAvatar, (int)type);
        }

        public static PgcData.ResourceType GetPgcResType(string id) {
            if (string.IsNullOrEmpty(id)) {
                return PgcData.ResourceType.ErrResourceType;
            }

            if (!int.TryParse(id, out var value) || value < 10000000) {
                return PgcData.ResourceType.ErrResourceType;
            }
            return (PgcData.ResourceType) (value / 10000000);
        }

        public static int GetUgcVehicle(VehicleSubType type)
        {
            return Get(PgcData.ResourceType.UgcVehicle, (int)type);
        }

        public static int GetVehicle(VehicleSubType type)
        {
            return Get(PgcData.ResourceType.Vehicle, (int)type);
        }

        /// <summary>
        /// 判断一个ID 是否是 PGC类型，UGC 类型 是数字字母组合 例如:”2lBRzq7dnAnOa5BsYUx0TZb5Tto“， pgc类型是纯数字 例如: "40200053"
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static bool IsPgc(string id) {
            if (string.IsNullOrEmpty(id)) {
                return false;
            }
            if (id == "leisure" || id == "default")
                return true;
            if (!int.TryParse(id, out var value) || value < 10000000) {
                return false;
            }
            return true;
        }



    }

    /// <summary>
    /// PGC道具在哪些业务场景售卖，存字符串，多个场景以,隔开
    /// </summary>
    public static class PgcSceneType
    {
        public const string ScenesTypeEnumFree = "0";
        public const string ScenesTypeEnumTopPick = "1";
        public const string ScenesTypeEnumStore = "2";
        public const string ScenesTypeEnumGashapon = "3";
    }

    /// <summary>
    /// Avatar子类型枚举值
    /// </summary>
    public enum AvatarSubType
    {
        ErrAvatarSubType = 0,
        Scarf = 1,        // 颈部饰品
        Belt = 2,         // 腰部饰品
        Brow = 3,         // 眉毛
        Clothes = 4,      // 衣服
        Effect = 5,       // 特殊饰品
        Eyes = 6,         // 眼睛
        Glasses = 7,      // 眼镜
        Hair = 8,         // 头发
        Hats = 9,         // 帽子
        Hand = 10,        // 手部饰品
        Mouth = 11,       // 嘴巴
        FacePaint = 12,   // 脸绘
        Shoe = 13,        // 鞋子
        Backpack = 14,    // 背包
        Cape = 15,        // 披风
        Crossbody = 16,   // 斜挎包
        Earring = 17,     // 耳部饰品
        Nose = 18,        // 鼻子
        Blush = 19,       // 腮红
        Glove = 20,       // 手套
        Visor = 21,       // 面罩
        Skin = 22,        // 皮肤颜色
        Head = 23,        // 头型
        MusicalInstrument = 24, //乐器

        Ear = 25, // 耳朵
        Tail = 26, // 尾巴
        Body = 27, //身体

        SpecialSkin = 28, // 特殊走跑跳道具
        Promotion = 29,//促销

        Shape = 30,//体型
        Vehicle = 31,//载具

        Size = 97,//宠物大小Size
        Bundle = 98, // 捆绑包
        All = 99, //全部
        NewBie = 100 //新手tag
    }

    public enum PetUGCAvatarEnable {
        ErrAvatarSubType = 0,
        Skin = 22,
        Clothes = 4,
        Ear = 25,
        Hair = 8,
        Hats = 9,
        Scarf = 1,
        Glasses = 7,
        Eyes = 6,
        Mouth = 11,
        FacePaint = 12,
        Tail = 26,
        Backpack = 14,
        Shoe = 13,
        Bundle = 98, // 捆绑包
    }

    public enum PetPGCAvatarEnable {
        ErrAvatarSubType = 0,
        Skin = 22,
        Clothes = 4,
        Ear = 25,
        Hairs = 8,
        Hats = 9,
        Scarf = 1,
        Glasses = 7,
        Eyes = 6,
        Mouth = 11,
        FacePaint = 12,
        Tail = 26,
        Backpack = 14,
        Shoe = 13,
    }



    // 激活的ugc部位
    public enum UgcAvatarEnable
    {
        ErrUgcAvatarSubType = 0,
        Clothes = 4,     // UGC衣服
        Backpack = 14,    // UGC背包
        Hand = 10,        // UGC手部饰品
        Hats = 9,         // UGC帽子
        Shoe = 13,        // UGC鞋子
        Eyes = 6,         // UGC眼睛
        Glasses = 7,      // UGC眼镜
        Mouth = 11,      // UGC嘴巴
        FacePaint = 12,      // UGC脸绘
        MusicalInstrument = 24, //UGC乐器
        Hair = 8,    // UGC 头发
        Bundle = 98, // 捆绑包
    }



    /// <summary>
    /// 表情枚举
    /// </summary>
    public enum EmoteSubType
    {
        ErrEmoteSubType = 0,
        Single = 1,
        SingleLoop = 2,
        Double = 3,
        DoubleLoop = 4,
        PetSingle = 5,
        PetSingleLoop = 6,
        PetWithPlayer = 7,
        PetWithPlayerLoop = 8,
        LinkEmote = 9,
        SelfiePose = 10, //拍照页面专用，商城等FittingRoom不应该显示出来

        SingleAll = 100,
        DoubleAll = 200,
        PetSingleAll = 300,
        PetWithPlayerAll = 400,
    }

    /// <summary>
    /// 载具枚举
    /// </summary>
    public enum VehicleSubType
    {
        ErrVehicleSubType = 0,
        SingleVehicle = 1,
        DoubleVehicle = 2,
        AllVehicle = 100,

        FittingRoomVehicle = 0, // 在商城展示的载具
    }

    public enum PartnerSubType
    {
        ErrPartnerSubType = 0,
        DefaultPartnerSubType = 1
    }

    public enum PartnerToneSubType
    {
        ErrPartnerToneSubType = 0,
        DefaultPartnerToneSubType = 1
    }

    public enum PartnerPackSubType
    {
        ErrPartnerPackSubType = 0,
        DefaultPartnerPackSubType = 1
    }

    public static class EmoteSubTypeExtension {
        public static bool IsPet(this EmoteSubType subType) {
            return subType == EmoteSubType.PetSingle || subType == EmoteSubType.PetSingleLoop || subType == EmoteSubType.PetWithPlayer || subType == EmoteSubType.PetWithPlayerLoop || subType == EmoteSubType.PetSingleAll || subType == EmoteSubType.PetWithPlayerAll;
        }

        public static bool IsLoop(this EmoteSubType subType) {
            return subType == EmoteSubType.SingleLoop || subType == EmoteSubType.DoubleLoop || subType == EmoteSubType.PetSingleLoop || subType == EmoteSubType.PetWithPlayerLoop;
        }

        public static bool IsDouble(this EmoteSubType subType) {
            return subType == EmoteSubType.Double || subType == EmoteSubType.DoubleLoop || subType == EmoteSubType.DoubleAll || subType == EmoteSubType.PetWithPlayer || subType == EmoteSubType.PetWithPlayerLoop || subType == EmoteSubType.PetWithPlayerAll;
        }

    }


    /// <summary>
    /// 游戏内道具子分类
    /// </summary>
    public enum GamePropSubType
    {
        ErrGamePropSubType = 0,
        Default = 1,
        PgcGameProp = 2, //PGC游戏道具
    }

    /// <summary>
    /// Other大类下的子类型定义
    /// </summary>
    public enum OtherPgcSubType
    {
        ErrPgcSubType = 0,
        UGCMaterialTemplate = 1, //UGC材质模板配置
        UGCItemInfoTemplate = 2, //UGC素材模板配置
    }

    public enum MusicScoreSubType
    {
        ErrMusicScoreSubType = 0,
        GeneralMusicScore = 1,
    }

    public enum UgcPoseSubType
    {
        ErrPoseSubType = 0,
        Single = 1,
        Double = 3,
        PetSingle = 5,
        PetWithPlayer = 7,

        PeopleAll = 100,
        PetAll = 200,
    }

    public enum UgcAnimSubType
    {
        ErrAnimSubType = 0,
        Single = 1,
        Double = 3,
        PetSingle = 5,
        PetWithPlayer = 7,
        LinkEmote = 8,

        PeopleAll = 100,
        PetAll = 200,
    }

    public enum UgcTheatreSubType
    {
        ErrTheatreSubType = 0,
        AvatarCard = 1,
        Theatre = 2
    }

    public enum CabinCharacterSubType
    {
        ErrCabinCharacterSubType = 0,
        CabinCharacter = 1
    }
}
