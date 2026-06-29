using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AIGame.Base
{   
    public struct SoundGroupParam
    {   
        public string GroupName;
        public string SwitchState;
        public string SoundName; 
    }

    public class YandereConfig
    {
        /// <summary>游戏时间</summary>
        public const int GameTime = 360; 

        /// <summary>游戏第一段旁白</summary>
        public const string DescStr = "Welcome to Yandere Escape Simulator, <c=#80ff00>{0}</c>!\nYour goal: escape the apartment. The catch? You're caught in <c=#ffd540>{1}</c>'s intense affection. Engage in dialogue, use your wits, and persuade <c=#ffd540>{2}</c> to let you out—all in under 5 minutes. Ready?\n Let the escape begin!";


        /// <summary>NPC第一段对白</summary>
        public const string NpcFirstContent = "Hi {0}, morning. Are you ready to spend your life with me forever and ever?";

        public const string HiContent = "Hi {0}, ";
        
        public const string DefaultFirstContent = "morning. Are you ready to spend your life with me forever and ever?";

        /// <summary>目标文案</summary>
        public const string GoalStr = "Persuade {0} to let you exit the apartment.";
        public const string GoalStr1 = "Persuade {0} to let you exit the school.";
        public const string GoalStr2 = "Persuade {0} to let you exit the BUD burger hub.";

        /// <summary>自动跳过旁白的时间</summary>
        public const float AutoHideDescTime = 8f;

        /// <summary>自动跳过NPC第一段对话时间</summary>
        public const float AutoToPalyTime = 2f;


        //================BGM================
        public const string BGM_START = "Bgm_YE_Login";//开屏页BGM
        public const string BGM_PLAY = "Bgm_YE_MainScene";//游玩BGM
        public const string BGM_ESCAPED = "Bgm_YE_Escaped";//开门成功，成功逃脱
        public const string BGM_SWEET_WAITING = "Bgm_YE_SweetWaiting";//开门成功，甜蜜等待
        public const string BGM_TOGETHER_FOREVER= "Bgm_YE_TogetherForever";//逃脱失败，永远在一起


        //================音效================
        public const string SOUND_START_VOICE = "YE_Login_Voice";//开屏页旁边
        // public const string SOUND_CLOCK_TICK_LOOP = "Simulator_CountDown_Clock_Loop";//闹钟警告1
        // public const string SOUND_CLOCK_ALARM = "Challenge_End_Countdown";//闹钟警告2
        // public const string SOUND_TIMES_UP = "Simulator_Timesup"; //倒计时结束弹窗音效
        public const string SOUND_OPEN_DOOR = "YE_DoorOpen";//开门音效
        public const string SOUND_CLOSE_DOOR = "YE_DoorClose";//关门音效
        public const string SOUND_GAME_OVER = "YE_GameOver";
        public const string SOUND_GAME_WIN = "YE_YouWin";
        public const string SOUND_DIE = "YE_Beaten";//被砍死亡
        public const string SOUND_HEART_BEAT = "YE_Heartbeat_Loop";//楼道追杀_呼吸心跳声
        public const string SOUND_KNIFE = "YE_Mood_Change";//开门后_拿起刀情绪氛围转变

        public const string PlayOnLight = "YE_Light_TurnOn";
        public const string PlayOffLight = "YE_Light_TurnOff";
        public const string YE_TV_On = "YE_TV_On";
        public const string YE_TV_Off = "YE_TV_Off";
        
        public const string YE_Countdown_Start = "YE_Countdown_Start";

        public const string YE_Chat_Vocal = "YE_Chat_Vocal";

        public const string YE_Typing_Loop = "YE_Typing_Loop";
        
        public const string SOUND_CALL = "YE_Ringtone_Loop";

        public const string NPC_Voice_Loop = "NPC_Voice_Loop";

        public const string YE_DoorLocked = "YE_DoorLocked";

        public const string YE_Yanderekiller2 = "YE_Yanderekiller2";
        public const string YE_YanderekillerMan2 = "YE_YandereKillerMan2";
        public static SoundGroupParam SOUND_NPC_LAUGH = new SoundGroupParam{
            GroupName = "Emote_1_72",
            SwitchState = "yanderehappy",
            SoundName = "Emote_1_72_3P"
        };//NPC大笑
        
        public static SoundGroupParam SOUND_NPC_HIT = new SoundGroupParam{
            GroupName = "Emote_1_72",
            SwitchState = "yandereknock",
            SoundName = "Emote_1_72_3P"
        };//NPC被撞
        
    }
}
