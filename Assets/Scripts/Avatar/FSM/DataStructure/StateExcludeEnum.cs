
    public enum StateEvent
    {
        MoveJoystick,
        JumpBtn,
        FastRun,
        SkiAniChange,
        PlayerAniState,
        SyncStatus,
        MusicInstrumentPlay,
        GroundEvent,//起跳或者下地
        PetBeginFollow,
        Teleport,//传送点、返回出生点等
        PetSingleEmote
    }

    /// <summary>
    /// 人物所有状态
    /// </summary>
    public enum PlayerState
    {
        None = 0,
        Default = 1,
        SingleEmote = 2,
        WinningFlag = 3,
        CollectedStar = 4,
        ChangeClothes = 5,
        ChangeClothesAni = 6,
        Skate = 7,
        Ski = 8,
        Swim = 9,
        DoubleEmote = 10,
        InteractiveBoard = 11,
        CameraMode = 12,
        MusicInstrumentPlay = 13,
        Leisure = 14,                       // 大厅休闲状态
        UgcEmote = 15,
        UgcDoubleEmote = 16,
        LinkEmote = 17,//联动表情：双人牵手等
        LinkEmoteStart = 18,//联动表情：双人牵手等，发起动作
        IdleExhibit = 19,
        ChangeVehicleAni = 20,
        PGCVehicle = 21,//PGC载具
        Passenger = 22,//乘客
        Bound = 23,//被束缚（被娃娃机爪子抓住，可QTE挣扎）
        Captured = 24,//被劫持（挣扎失败，完全被束缚，仅车主放车才能解除）
        Falling = 25,//下落（挣脱或被放下后，从空中坠落到落地）
    }

    /// <summary>
    /// 状态互斥类型
    /// </summary>
    public enum StateExcludeType
    {
        [Label("None")]
        None,
        [Label("√1")]
        DirectIntoState, // 不打断状态 - 不打断动画 - 如有道具，直接加载道具 ✅1
        [Label("√2")]
        Coexist, // 不打断状态 - 但打断动画，播放新状态的动画 ✅2
        [Label("X*")]
        CacheState, // 缓存当前状态，进入新状态 ❌*
        [Label("X")]
        Interrupt, // 中断当前状态, 进入新状态 ❌
        [Label("Ø")]
        Ignore, // 不打断状态 - 忽略新状态 🚫
    }

    public enum PlayerStateType
    {
        [Label("人物默认状态")]
        PlayerDefault,
        [Label("人物功能属性")]
        PlayerFeature,
        [Label("环境")]
        PlayerEnvironment,
        [Label("触发属性")]
        PlayerTrigger,
    }

    /// <summary>
    /// 人物默认状态
    /// </summary>
    public enum PlayerDefault
    {
        [Label("None")]
        None,
        [Label("初始状态")]
        Default,
    }

    /// <summary>
    /// 人物功能属性
    /// </summary>
    public enum PlayerFeature
    {
        [Label("None")]
        None,
        [Label("坐骑")]
        Mount,
        [Label("手持")]
        Handheld,
        [Label("背部")]
        BackOrnament,
        [Label("头戴")]
        Headgear,
        [Label("外挂")]
        Pendant,
        [Label("超能力")]
        Ability,
        [Label("变形")]
        Transform,
        [Label("Emote")]
        Emote,
        [Label("播放动画")]
        PlayAni,
        [Label("牵手")]
        HoldingHands,
    }

    /// <summary>
    /// 环境
    /// </summary>
    public enum PlayerEnvironment
    {
        [Label("None")]
        None,
        [Label("水")]
        Water,
        [Label("冰")]
        Ice,
        [Label("雪")]
        Snow,
    }

    /// <summary>
    /// 触发属性
    /// </summary>
    public enum PlayerTrigger
    {
        [Label("None")]
        None,
        [Label("主动非循环")]
        InitativeNoLoop,
        [Label("主动循环")]
        InitativeLoop,
        [Label("被动")]
        Passive,
        [Label("主动必进")]
        InitativeEnter,
    }

    //起跳或者下地
    public enum GroundEvent
    {
        Landed,
        LeaveStableGround,
    }

    //联动表情，双人牵手等
    public enum PlayerABType
    {
        PlayerA,//发起者
        PlayerB,//跟随者
    }

    public enum SkillType{
        Skill1 = 10001,
        Skill2 = 10002,
        Skill3 = 10003,
        Skill4 = 10004,
        Skill5 = 10005,
        Skill6 = 10006,
    }