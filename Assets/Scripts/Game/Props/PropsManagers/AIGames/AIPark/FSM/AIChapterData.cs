using System;
using System.Collections.Generic;
using System.Linq;
using Game.Base;
using Pb.Game;
using UnityEngine;

namespace Game.Props.PropsManagers.AIGames.AIPark.FSM
{
    //官方地图角色枚举
    public enum ParkNpcRoleType
    {
        self = 0,
        Default = -99,
        Dean = 101,
        Nurse = 103,
        Doctor = 104,
        Pharmacist = 106,
        Dustman = 107,
        Tilia = 1, //提莉亚 八音盒人偶
        Elise = 2, //伊莉丝 牧羊女
        Casper = 3, //卡斯帕 锡兵
        Pio = 4,   //皮奥 匹诺曹
        Teddy = 5, //泰迪 破烂的小熊玩偶
        Vivien = 6, //薇薇安 鬼魂新娘人偶
        Rowland = 7, //罗兰 破碎新郎人偶

    }



    /// <summary>
    /// 位置类型枚举
    /// </summary>
    public enum LocationType
    {
        None = 0,
        ConsultingRoom = 1,   // 诊室
        DirectorsOffice = 2,  // 院长室
        WaitingRoom = 3,      // 候诊室
        Toilet = 4,           // 厕所
        Ward = 5,             // 病房
        Corridor = 6,         // 走廊
        Pharmacy = 7,         // 药房
        OutDoor = 8,          // 大门


        TrojanHorse = 101, //旋转木马
        SlideSlides = 102, //滑滑梯
        SeeSaw = 103,  //跷跷板
        Swinging = 104, //荡秋千

        Fountain = 105, //喷泉
        Stage = 106, //舞台
        Park = 107, //公园


    }

    /// <summary>
    /// 动作类型枚举
    /// </summary>
    public enum ActionType
    {
        None = 0,

        OperatingEquipment = 1,      //弄设备
        InjectionsToPatients = 2,    //给病人打针
        OrganizingDocuments = 3,     // 整理文件
        Working = 4, //办公
        Sitting = 5, //坐下
        UsingRestroom = 6, //上厕所
        Cleaning = 7, //清洁
        TakingMedication = 8, //吃药
        ConductingRounds = 9, // 查房
        LyingDown = 10, //躺下
        DispensingMedication = 11, //配药
        Stand = 12, //站立
        Sleep = 13, //睡觉
        Talk = 14,//交谈
        TalkWithPlayer = 100,         // 与玩家交谈



        TrojanHorse = 101, //玩旋转木马
        SlideSlides = 102, //玩滑滑梯
        SeeSaw = 103,  //玩跷跷板
        Swinging = 104, //玩荡秋千
        PerformOnStage = 106, //上舞台表演
        Selfie = 107, //自拍
        SitOnBench = 108, //在长椅坐下
        Idle = 0, //站立待机


    }

    /// <summary>
    /// AI章节数据，定义NPC在特定时间段的行为
    /// </summary>
    [Serializable]
    public class AIPark_ChapterData
    {
        /// <summary>
        /// 位置类型
        /// </summary>
        public int location;

        /// <summary>
        /// 动作类型
        /// </summary>
        public int action;

        public List<string> participants;

        public List<AmusementAIQuoteLine> quotes;

        public string talkTo;

        public NodeBaseBehaviour nodeBaseBehaviour;
        public bool byServer = true; //true:后端给的章节 false:本地生成

        public bool bQuickEnter = false; //是否快速进入

        public AIPark_ChapterData()
        {
        }

        public AIPark_ChapterData(History history)
        {
            this.location = history.Location;
            this.action = history.Action;
            this.participants = history.Participants.ToList();
            this.quotes = history.Quotes.ToList();
        }
    }
}