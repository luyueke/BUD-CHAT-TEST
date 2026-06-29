using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIHospitalConfig
{
    public const string GUIDE_BGM = "Bgm_S9Hosp_Login";
    public const string MAIN_BGM = "Bgm_S9Hosp_MainScene";

    #region 道具交互
    public const string SOUND_OPEN_DOOR = "Hosp_DoorOpen";//开门音效
    public const string SOUND_CLOSE_DOOR = "Hosp_DoorClose";//关门音效

    public const string SOUND_LIGHT_OPEN = "Hosp_Light_TurnOn"; //开灯音效
    public const string SOUND_LIGHT_CLOSE = "Hosp_Light_TurnOff"; //关灯音效
    
    public const string BROKE_LIGHT_LOOP = "Hosp_LightFailure_Loop"; //故障灯循环

    public const string SOUND_MED_OPEN_DOOR = "Hosp_MedicineCabinet_Open"; //药柜打开音效
    public const string SOUND_MED_CLOSE_DOOR = "Hosp_MedicineCabinet_Close"; //药柜关闭音效
    #endregion
}
