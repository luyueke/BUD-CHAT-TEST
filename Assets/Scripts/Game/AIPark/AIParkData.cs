using Game.Props.PropsManagers.AIGames.AIPark.FSM;
using Game.Props.PropsManagers.AIGames.AIHospital.FSM;
using System.Collections;
using System.Collections.Generic;
using GameData.BaseInfo;
using UnityEngine;
using Pb.Game;
using Newtonsoft.Json.Linq;

/// <summary>
/// message 里面gamedata对应的就是S11ChatReq 的json
/// </summary>
public class S11AIMessageReq
{
    public int gameId;
    public string conversationId;
    public string gameData;
    public string mapId;
    public string npcId;
    public string sessionId;
}

public class S11AIMessageRsp
{
    //public int result;
    //public string rmsg;
    //public string requestId;
    public string conversationId;
    public string gameData;//S11AImssageGDataRsp
    public int isEnd;
    public int remainingChatCnt;
    // public History history;

    //下面3个字段通过gameData解出来
    public string reply;                  //回复的文本内容
    public int replyIsEnd;                //replayIsEnd = 1 回复结束，后续会有emote，replayIsEnd =2 对话流关闭
    public string emoteId;                  //回复中可能携带的emote
    public void ParseGameData()
    {
        JObject jObject = JObject.Parse(gameData);
        if (jObject["reply"] != null)
        {
            reply = jObject["reply"].ToString();
        }
        if (jObject["replyIsEnd"] != null)
        {
            replyIsEnd = int.Parse(jObject["replyIsEnd"].ToString());
        }
        if (jObject["emoteId"] != null)
        {
            emoteId = jObject["emoteId"].ToString();
        }
    }
}



/// <summary>
/// AI聊天请求数据格式
/// 请求发上去的时候需要content/emote 两者有一个不是空的，pgc可以携带role和npcid,ugc只有npcid
/// </summary>
public class S11ChatReq
{
    public string content;
    public string emote;
    public string npcId;
}

/// <summary>
/// AI聊天回复详情
/// </summary>
public class S11ChatRsp
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

public class AIGameParkConfig
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

    public static string ParkThemeColor = "#68CCBE";

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

    public static string GetEmoteName(string emoteID, bool isEnter = true)
    {
        if (emoteMsg.TryGetValue(emoteID, out var info))
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
    public static List<S11AIGameStepTarget> taskTarget = new()
    {
        // new S11AIGameStepTarget(ParkNpcRoleType.Doctor,"逃离病房：", "和医生对话，说服医生打开病房门让你离开"),
        // new S11AIGameStepTarget(ParkNpcRoleType.Nurse,"逃脱准备：", "和护士对话，想办法打听到大门密码"),
        // new S11AIGameStepTarget(ParkNpcRoleType.Dean,"逃脱准备：", "和院长对话，说服院长开除保安"),
        // new S11AIGameStepTarget(ParkNpcRoleType.Pharmacist,"逃脱准备：", "和药剂师对话，想办法骗他给你麻醉针"),
        // new S11AIGameStepTarget(ParkNpcRoleType.Dustman,"逃脱准备：", "对清洁工使用麻醉针，弄晕他后换上他的衣服"),
        // new S11AIGameStepTarget(ParkNpcRoleType.Default,"离开医院：", "去大门输入密码开门，然后从医院离开"),
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
        TillaSpeak = 100,
        TargetTips = 0,
        ChatBtnTips = 1,
        SendMsgTips = 2,
        SendEmoteMsgTips = 3,
        ProgressTips = 4,
        CheckMapTips = 5,
        LinkBtnTips = 6,


        Event_1 = 7,
        Event_2 = 8,
        Event_3 = 9,

        Event_1_1 = 10,
        Event_1_2 = 11,
        Event_2_1_1 = 15,
        Event_2_1_2 = 16,
        Event_2_2_1 = 17,
        Touch_Hand_Swing = 18,
        Event_2_3_1 = 19,
        Event_2_3_2 = 20,
        Event_2_3_3 = 21,
        Event_3_1_1 = 22,

        Guide2SwingTxt = 23,

    }

    public enum EGuideAction
    {
        None = 0,
        ShowNpcContent = 1,
        ShowHeartTips = 2,
    }

    /// <summary>
    /// 本地存储的引导ID
    /// </summary>
    public static string pgcGuideId = "pgcGuideId";
}

/// <summary>
/// S11游戏步骤目标
/// </summary>
public class S11AIGameStepTarget
{
    public ParkNpcRoleType targetRole;
    public string targetRoleId;
    public string targetTitle;
    public string targetDesc;
    public bool bFinish = false;

    public S11AIGameStepTarget(ParkNpcRoleType _role, string _roleId, string _title, string _desc)
    {
        targetRole = _role;
        targetRoleId = _roleId;
        targetTitle = _title;
        targetDesc = _desc;
    }
}

public enum S11GuideStep
{
    None = -1,
    Guide_FirstInGame_Guide_1_1 = 103,
    Guide_FirstInGame_Guide_1_2 = 104,
    Guide_FirstInGame_Guide_1_3 = 105,
    Guide_FirstInGame_Guide_1_4 = 106,
    Guide_FirstInGame_Guide_1_5 = 107,
    Guide_FirstInGame_Guide_1_6 = 108,

    Guide_FirstInGame_Guide_2_1 = 109,
    Guide_FirstInGame_Guide_2_2 = 110,
    Guide_FirstInGame_Guide_2_3 = 111,
    Guide_FirstInGame_Guide_3_1 = 112,

}

public enum S11GameState
{
    Ready_QuickEv_1 = -1,
    Ready = 0,
    Event_Os = 1, //旁白
    NpcDiscuss = 2,
    NpcDiscussEnd = 3,
    Event_1_Begin = 4,
    Event_1_End = 5,

    Event_2_Begin = 6,
    Event_2_End = 7,
    Event_3_Begin = 8,
    Event_3_End = 9,
    Event_Summary = 101,
    SummaryDiscuss = 102,






    Success = 11,
    Fail = 12,
    Exit = 13,
    UGCStartEscape = 14,
    TimeOut = 15,
}

public class S11UgcTaskData
{
    public ParkNPCData npcData;
    public bool isFinish;
    public GameObject effect;
}

public class S11PgcTaskData
{
    public ParkNpcRoleType role;
    public bool isFinish;
}

public class S11GameReport
{
    public int gameId;
    public int result;
    public int duration;
    public string conversationId;
    public string npcId;
    public string mapId;
}


public enum GuidePosType
{
    PlayerSpawn_First_Park = 0,
    TiliaSpawn_First_Park = 1,
    TiliaTalk_First_Park = 2,


    PlayerSpawn_Park = 3,
    TiliaSpawn_Park = 4,
    TiliaTalk_Park = 5,
    PlayerSpawn_Stage = 6,
    TiliaTalk_Stage = 7,
    TiliaSpawn_Stage = 8,
    PlayerSpawn_Fountain = 9,
    TiliaSpawn_Fountain = 10,
    TiliaTalk_Fountain = 11,
    Guide_FirstInGame_Npc_1 = 12,
    Guide_FirstInGame_Self = 13,
}
public enum GuideStepType
{
    Guide_FirstInGame_Big_Step_1 = 1, //初始进入游戏引导的大步骤
    Guide_FirstInGame_Big_Step_2 = 2, //事件到结局 大步骤
    Guide_FirstInGame_Over = -1, //步骤完结
}