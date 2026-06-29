public enum PlayerAniPrameter
{
    /// <summary>
    /// 切换动画状态
    /// </summary>
    StateOn,
    /// <summary>
    /// 当前人物状态
    /// </summary>
    CurPlayerState,
    /// <summary>
    /// 之前人物状态
    /// </summary>
    PrePlayerState,
    /// <summary>
    /// 当前动画状态
    /// </summary>
    CurAniState,
    /// <summary>
    /// 之前动画状态
    /// </summary>
    PreAniState,
    /// <summary>
    /// 当前子动画状态
    /// </summary>
    CurChildAniState,
    /// <summary>
    /// 之前子动画状态
    /// </summary>
    PreChildAniState,
    /// <summary>
    /// 是否在地面
    /// </summary>
    IsGround,


    /// <summary>
    /// XZ移动速度
    /// </summary>
    MoveSpeed,

    /// <summary>
    /// Y轴移动速度
    /// </summary>
    YMoveSpeed,
    /// <summary>
    /// 移动时间
    /// </summary>
    PressJoystickTime,
    /// <summary>
    /// 环境 - 水, 冰, 雪
    /// </summary>
    Environment,

    /// <summary>
    /// 当当前穿戴特殊PGC ID
    /// </summary>
    SpecialAnimPGC,
    
    /// <summary>
    /// 联动表情（双人牵手等），0：发起者， 1：跟随者
    /// </summary>
    PlayerABType,

    LandOn, // 落地

}

public enum PlayerAniState
{
    TempClip = 9999,

    // Base
    Idle = 1000,
    Run = 1001,
    Jump = 1002,
    Land = 1003,

    Preview_Idle = 2000,
    Preview_Run = 2001,
    Preview_Jump = 2002,
    Preview_Land = 2003,
    // Other
    Baqi = 1004,
    GashaponOneTime = 1005,
    GashaponTenTime = 1006,
}

public enum PlayerChildAniState
{
    None,
    Enter = 1,
    Exit,
    Left,
    Right,
    Up,
}
