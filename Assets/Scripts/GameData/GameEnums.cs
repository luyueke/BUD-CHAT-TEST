// @Author: YangJie
// @Description:
// @Date:  2023/07/12
// @Modify:

namespace GameData
{
    public enum EnterGameModel
    {
        CreateEmptyScene = 1, // 创建空场景
        GuestScene = 2, // 游玩模式
        ContinueEditScene = 3, // 继续编辑模式
        PublishTest = 4, //地图作者发布自测模式
        UpdatePublishTest = 5, //地图作者覆盖发布自测模式

        UgcSkinEmpty = 101, //新建UGC衣服
        UgcSkinContinueEdit = 102, //继续编辑UGC衣服

        UgcPropEmpty = 201, //新建UGC素材
        UgcPropContinueEdit = 202, //继续编辑UGC素材

        UgcMaterialEmpty = 301, //新建UGC材质
        UgcMaterialContinueEdit = 302, //继续编辑UGC材质

        UgcMusicalInstrumentEmpty = 401, //新建UGC乐器
        UgcMusicalInstrumentContinueEdit = 402, //继续编辑UGC乐器

        UgcMusicScoreEmpty = 501, //新建UGC乐谱
        UgcMusicScoreContinueEdit = 502, //继续编辑UGC乐谱

        UgcAnimEmpty = 701,//新建UGC动画
        UgcAnimContinueEdit = 702,//继续编辑UGC动画

        AnimPoseEmpty = 601, //新建Pose
        AnimPoseContinueEdit = 602, //继续编辑Pose

        UgcVehicleEmpty = 801,//新建载具
        UgcVehicleEmptyContinueEdit = 802,//继续编辑载具
        UgcVehicleTryPlay = 803,//载具试玩（进图后打开 VehiclePlayPanel，不进编辑器）

        OCActorCreaterEmpty = 901,//OC剧场演员创建
        OCActorContinueEdit = 902,
        
        AIYandere = 1000, //AI病娇

        AIHospital = 2000, //AI医院
        AIPark = 3000,
    }

    // 交互类型 1 体验地图 2 点赞地图 3 购买皮肤 4 点赞皮肤 5 收藏设子(此接口无效) 6 购买素材 7 点赞素材 8 收藏皮肤 9 购买材质 10 点赞材质
    public enum UgcInteractType
    {
        PurchasedMaterial = 9,
        LikedMaterial = 10,
    }

    // 1:地图 2:衣服 3:素材 4：材质
    public enum UgcType
    {
        Map = 1,
        Clothes = 2,
        Prop = 3,
        Material = 4,
        MusicTone = 5,
        MusicScore = 6,
        Anim = 7, //动画
        Pose = 8, //姿势
        AnimMusic = 9, //动画音效
        AINpc = 10,
        UgcVehicle = 11,//载具

        AIPartner = 12,//AI伙伴
        PartnerMusic = 13,//伙伴音色

        ActorCard = 14,//演员
        Theatre = 15,//剧场

        AIPartnerBundle = 16,//AI伙伴捆绑包
        PartnerBox = 17,//伙伴盒子

    }

    public enum GameMode
    {
        Play,
        Guest,
        Edit,
        AIGuset,
    }

    public enum PropModelShape
    {
        Cube, //正方体
        Cylinder //圆柱体
    }

    public enum WaterSpeed
    {
        Slow,
        Mid,
        Fast
    }

    /// <summary>
    /// 对应EmoUIConfig配置表
    /// 1 = 单人表情动作
    /// 2 = 双人表情动作
    /// </summary>
    public enum UIEmoteType
    {
        SinglePlayer = 1,
        DoublePlayer = 2,
        PetSingle = 3,
        PetWithPlayer = 4,
        LinkEmote = 5,
        Vehicle = 6,
        CameraSelfiePose = 7,
        Theatre = 8,
        MusicalInstrument = 9,
    }

    public enum CharacterFootType {
        Jump, // 跳起
        Landed, // 落下
        Run, // 跑
        FastRun, // 快跑,
        Idle
    }


    public enum SpecialAnim {
        Idle,
        Run,
        FastRun,
        Jump,
        JumpRun,
        JumpFastRun,
        Landed,
        PreviewJump,
        PreviewRun,
        PreviewFastRun,
        PreviewIdle,
        ExhibitIdle, // 特殊待机
    }

    public static class SpecialAnimExtension {
        public static string GetString(this SpecialAnim anim) {
            switch(anim) {
                case SpecialAnim.Idle:
                    return "idle";
                case SpecialAnim.Run:
                    return "run";
                case SpecialAnim.FastRun:
                    return "run_fast";
                case SpecialAnim.Jump:
                    return "jump_idle";
                case SpecialAnim.JumpRun:
                    return "jump";
                case SpecialAnim.JumpFastRun:
                    return "jump_run";
                case SpecialAnim.Landed:
                    return "jump_down";
                case SpecialAnim.PreviewJump:
                    return "preview_jump";
                case SpecialAnim.PreviewRun:
                    return "preview_run";
                case SpecialAnim.PreviewIdle:
                    return "leisure_idle";
                case SpecialAnim.PreviewFastRun:
                    return "preview_fastRun";
                case SpecialAnim.ExhibitIdle:
                    return "idle_exhibit";
                default:
                    return "idle";
            }
        }
    }

    public enum VehicleAnim{
        Start,
        Idle,
        Run,
        FastRun,
        Jump,
        Fall,
        Land,
        Skill1Start,
        Skill1Loop,
        Skill1End,
        Skill2Start,
        Skill2Loop,
        Skill2End,
    }

    public static class VehicleAnimExtension {
        public static string GetString(this VehicleAnim anim) {
            switch(anim) {
                case VehicleAnim.Start:
                    return "start";
                case VehicleAnim.Idle:
                    return "idle";
                case VehicleAnim.Run:
                    return "run";
                case VehicleAnim.FastRun:
                    return "runFast";
                case VehicleAnim.Jump:
                    return "jump";
                case VehicleAnim.Fall:
                    return "fall";
                case VehicleAnim.Land:
                    return "land";
                case VehicleAnim.Skill1Start:
                    return "skill1Start";
                case VehicleAnim.Skill1Loop:
                    return "skill1Loop";
                case VehicleAnim.Skill1End:
                    return "skill1End";
                case VehicleAnim.Skill2Start:
                    return "skill2Start";
                case VehicleAnim.Skill2Loop:
                    return "skill2Loop";
                case VehicleAnim.Skill2End:
                    return "skill2End";
                default:
                    return "idle";
            }
        }
    }


}
