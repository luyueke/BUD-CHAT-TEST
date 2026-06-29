using System;
using System.Collections.Generic;
using UnityEngine;

// 需要替换的动画片段类型
public enum AnimClipType
{
    idle,
    Collect,
    Discard,
    Run,
    Fast_Run,
    Jump,
    pvp_attack,
    pvp_runattack,
    pvp_beattack,
    pvp_beattackback,
    mutual_run_left,
    mutual_run_right,
    Run_Jump,
    selfiestick_start,
    selfiestick_centre,
    selfiestick_end,
    BouncePlank,
    pvp_runbeattackback,
    trapbox_hit,
    wing_fly,
    vehicle_up,
    vehicle_down,
    swimming,
    swimming_idle,
    swimming_up,
    swimming_walk,
    swimming_slow,
}
public class StateName
{
    public const string Idle = "idle";
    // BaseState
    public const string Jump = "jump";
    public const string RunFast = "run_fast";
    public const string Run = "run";
    
    // 排行榜人物获奖
    public const string PodiumAwrad_FirstStart = "firstplace_start";
    public const string PodiumAwrad_FirstCentre = "firstplace_centre";
    public const string PodiumAwrad_SecondStart = "secondplace_start";
    public const string PodiumAwrad_SecondCentre = "secondplace_centre";
    public const string PodiumAwrad_ThirdStart = "thirdplace_start";
    public const string PodiumAwrad_ThirdCentre = "thirdplace_centre";

    //需要替换的动画类型和动画名称对应的 dict
    public static Dictionary<AnimClipType, string> AnimTypeAndNameDict = new Dictionary<AnimClipType, string>()
    {
        {AnimClipType.idle, Idle},
        {AnimClipType.Run, Run},
        {AnimClipType.Fast_Run, RunFast},
        {AnimClipType.Jump, Jump},
    };
}
[Serializable]
public class ClipItem
{
    public AnimClipType clipKey;
    public string name;
}

