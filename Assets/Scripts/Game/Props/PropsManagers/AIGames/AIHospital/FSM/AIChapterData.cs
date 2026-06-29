using System;
using UnityEngine;

namespace Game.Props.PropsManagers.AIGames.AIHospital.FSM
{
    //官方地图角色枚举
    public enum HospitalNpcRoleType
    {
        Default = 0,
        Doctor = 1, // 医生
        Dean = 2, // 院长 
        Patient = 3, // 病人
        Nurse = 4, // 护士
        Dustman = 5, // 清洁工
        Pharmacist = 6, // 药剂师
        Security = 7, //保安
        Max = 8,
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
        Talk= 14,//交谈
        
        TalkWithPlayer = 100         // 与玩家交谈
    }

    /// <summary>
    /// AI章节数据，定义NPC在特定时间段的行为
    /// </summary>
    [Serializable]
    public class AIHospital_ChapterData
    {
        /// <summary>
        /// 位置类型
        /// </summary>
        public int location;

        /// <summary>
        /// 动作类型
        /// </summary>
        public int action;

        /// <summary>
        /// 开始时间（秒）
        /// </summary>
        public float startTime;

        /// <summary>
        /// 结束时间（秒）
        /// </summary>
        public float endTime;
        
        /// <summary>
        /// 交互对象ID
        /// </summary>
        public string talkTo;

        public AIHospital_ChapterData(LocationType location, ActionType action, float startTime, float endTime, string talkTo = "")
        {
            this.location = (int)location;
            this.action = (int)action;
            this.startTime = startTime;
            this.endTime = endTime;
            this.talkTo = talkTo;
        }

        public override string ToString()
        {
            return $"位置: {location}, 动作: {action}, 时间: {startTime}-{endTime}, 交互对象: {talkTo}";
        }
    }
} 