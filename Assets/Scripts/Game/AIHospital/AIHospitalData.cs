using Game.Props.PropsManagers.AIGames.AIHospital.FSM;
using System.Collections;
using System.Collections.Generic;
using GameData.BaseInfo;
using UnityEngine;

/// <summary>
/// message 里面gamedata对应的就是s9ChatReq 的json
/// </summary>
public class S9AIMessageReq
{
    public int gameId;
    public string conversationId;
    public string gameData;
    public string mapId;
    public string npcId;
    public string sessionId;
}

public class S9AIMessageRsp
{
    //public int result;
    //public string rmsg;
    //public string requestId;
    public string conversationId;
    public string gameData;
    public int remainingChatCnt;
}

/// <summary>
/// AI聊天请求数据格式
/// 请求发上去的时候需要content/emote 两者有一个不是空的，pgc可以携带role和npcid,ugc只有npcid
/// </summary>
public class S9ChatReq
{
    public string content;
    public string emote;
    public int hospitalNPCRole;
    //public string npcId;
}

/// <summary>
/// AI聊天回复详情
/// </summary>
public class S9ChatRsp
{
    public string reply;                  //回复的文本内容
    public int replyIsEnd;                //replayIsEnd = 1 回复结束，后续会有emote，replayIsEnd =2 对话流关闭
    public string emote;                  //回复中可能携带的emote
    public int decisionRate;              //npc决策值，需要体现在npc脚底，圈圈表示具体进度
    public int npcAlertness;              //npc警惕值，客户端实际上无表现
    public int persuadeSuccess;           //ugc 说服npc任务完成标记
    public int answerSuccess;             //ugc 回答npc任务完成标记
    public int code;                      //pgc密码  
    public string taskAnswer;         //pgc回复
}

public class AIGameHospitalConfig
{
    //是否限时
    public static bool hasTimeLimited = false;
    //游戏时长
    public static int gameDuration = 900;
    //最大决策值
    public static float maxDecisionNum = 100;
    //默认决策值
    public static int defaultDecision = 20;
    //最大警惕值
    public static int maxAlertNum = 100;
    //生命值
    public static int defaultHp = 3;
    //UGCBGM
    public static string ugcBgmUrl = "";

    public static int maxHp = 9999;

    public static string guideKeyName = "StrongGuide";

    //分享需要的版本
    public static string shareNeedVersion = "1.0.9";
    /// <summary>
    /// 四位数的密码给个五位默认值，无法跳过直接通关
    /// </summary>
    public static int defaultPwd = 99999;

    public static string firstPlayMapIDKey = "_firstPlayMapIDKey";

    public static string firstPassMapIDKey = "_firstPassMapIDKey";

    public static string hospitalThemeColor = "#68CCBE";

    public class EmoteInfo
    {
        public string emoteName;
        public string enterName;
        public string exitName;
        public EmoteInfo(string name1, string name2, string name3)
        {
            emoteName = name1;
            enterName = name2;
            exitName = name3;
        }
    }

    public static Dictionary<string, EmoteInfo> emoteMsg = new Dictionary<string, EmoteInfo>()
    {
        { "40900001", new EmoteInfo("普通牵手", "(紧紧牵住了你的手)", "(松开了你的手)")},
        { "40900002", new EmoteInfo("推婴儿车", "(拉着你上了推车)", "(拉着你下了推车)")},
        { "40900003", new EmoteInfo("御剑飞行", "(带着你御剑飞行)", "(带着你从御剑下来)")},
        { "40900004", new EmoteInfo("筋斗云", "(带着你坐上了筋斗云)", "(带着你从筋斗云下来)")},
        { "40900489", new EmoteInfo("拉花车", "(拉着你上了推车)", "(拉着你下了推车)")},
    };

    /// <summary>
    /// 牵手状态骨骼有变化，给特效加偏移
    /// </summary>
    public static Dictionary<string, Vector3> _selectEffectOffset = new Dictionary<string, Vector3>()
    {
        { "40900001", new Vector3(0,0,0)},
        { "40900002", new Vector3(0,0,-0.5f)},
        { "40900003", new Vector3(0,0,1.3f)},
        { "40900004", new Vector3(0,0,1.3f)},
        { "40900489", new Vector3(0,0,0)},
    };

    public static string GetEmoteName(string emoteID,bool isEnter = true)
    {
        if(emoteMsg.TryGetValue(emoteID, out var info))
        {
            return isEnter ? info.enterName : info.exitName;
        }
        LoggerUtils.LogError("没有找到对应的emoteID:" + emoteID);  
        return null;    
    }

    public void SetData(MapInfo mapInfo)
    {
        
    }

    public static string ugcTargetDesc = "说服{0}名监管者逃离医院({1}/{0})";

    /// <summary>
    /// 任务目标文本
    /// </summary>
    public static List<S9AIGameStepTarget> taskTarget = new()
    {
        new S9AIGameStepTarget(HospitalNpcRoleType.Doctor,"逃离病房：", "和医生对话，说服医生打开病房门让你离开"),
        new S9AIGameStepTarget(HospitalNpcRoleType.Nurse,"逃脱准备：", "和护士对话，想办法打听到大门密码"),
        new S9AIGameStepTarget(HospitalNpcRoleType.Dean,"逃脱准备：", "和院长对话，说服院长开除保安"),
        new S9AIGameStepTarget(HospitalNpcRoleType.Pharmacist,"逃脱准备：", "和药剂师对话，想办法骗他给你麻醉针"),
        new S9AIGameStepTarget(HospitalNpcRoleType.Dustman,"逃脱准备：", "对清洁工使用麻醉针，弄晕他后换上他的衣服"),
        new S9AIGameStepTarget(HospitalNpcRoleType.Default,"离开医院：", "去大门输入密码开门，然后从医院离开"),
    };

    public static void Reset()
    {
        hasTimeLimited = true;
        gameDuration = 900;
        maxDecisionNum = 100;
        defaultDecision = 20;
        maxAlertNum = 100;
        defaultHp = 3;
        ugcBgmUrl = "";
    }

    public enum EPgcGuideID
    {
        Nonoe = -1,
        TargetTips = 0,
        ChatBtnTips = 1,
        SendMsgTips = 2,
        SendEmoteMsgTips = 3,
        ProgressTips = 4,
        HeartTips = 5,
    }

    public enum EGuideAction
    {
        None = 0,
        ShowDoctorContent = 1,
        ShowHeartTips = 2,
    }

    /// <summary>
    /// 本地存储的引导ID
    /// </summary>
    public static string pgcGuideId = "pgcGuideId";
}

/// <summary>
/// s9游戏步骤目标
/// </summary>
public class S9AIGameStepTarget
{
    public HospitalNpcRoleType targetRole;
    public string targetTitle;
    public string targetDesc;
    public bool bFinish =false;

    public S9AIGameStepTarget(HospitalNpcRoleType _role, string _title, string _desc)
    {
        targetRole = _role;
        targetTitle = _title;
        targetDesc = _desc;
    }
}

public enum S9GameState
{
    Ready = 0,
    EnterConsultingRoom = 1, //进入候诊室
    StartEscape = 2, //开始逃脱
    Success = 3,
    Fail = 4,
    Exit = 5,
    UGCStartEscape = 6,
    TimeOut = 7,
}

public class S9UgcTaskData
{
    public HospitalNPCData npcData;
    public bool isFinish;
    public GameObject effect;
}

public class S9PgcTaskData
{
    public HospitalNpcRoleType role;
    public bool isFinish;
}

public class S9GameReport
{
    public int gameId;
    public int result;
    public int duration;
    public string conversationId;
    public string npcId;
    public string mapId;
}

