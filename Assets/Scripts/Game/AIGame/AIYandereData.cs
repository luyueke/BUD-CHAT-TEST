using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum YandereAreaType
{
    Unknown = 0,
    Bathroom = 10000,//浴室
    LivingRoom = 10001,//大厅
    Bedroom = 10002,//卧室
    SitSofa = 10003,//NPC坐沙发
    
    OpenDoor = 20000,//开门
    Closestool = 20001,//马桶
    Bathtub = 20002,//浴缸
    Sofa = 20003,//沙发
    Bed = 20004,//床

    
}

public enum MoodOption
{
    Normal = 0,
    Happy = 1,
    Sad = 2,
    Angry = 3,
    Exasperated = 4,
    Surprised = 5,
    Killer = 6,
    Nock = 9
}

public enum YandereStep
{
    None = 0,
    GameStart,
    GameNpcSelect,//游戏npc选择
    GameDesc,//游戏旁白
    Play,//操作
    Run,//逃跑
    BadEnd_1 ,
    BadEnd_2,
    BadEnd_3,
    GoodEnd_1,
    GoodEnd_2,

    // 等待游戏开始
    WaitingForStart,

    GameModeSelect,

    ConnectServer,
}


public enum YandereEndState
{
    BadEnd_2 = 2,
    OpenDoor = 3,
    GoodEnd_2 = 5
}

#region 后端交互数据结构

public class AIResposeChatData
{
    public string reply;
    // public int happinessRate = -1;//开心值
    // public int upsetRate = -1;//伤心值
    // public int angerRate = -1;// 愤怒值
    public int result;//2,3,5结局
    public int npcLocation;
    public int followPlayer;
    public int replyIsEnd;
    public int moodOption = -1;
    public int decisionRate = -1;//开门决策值
    public int currencyState = 0;
}
public class AIResultRespose
{
    // public int happinessRate = -1;//开心值
    // public int upsetRate = -1;//伤心值
    // public int angerRate = -1;// 愤怒值
    // public int decisionRate = -1;//决策值
    public int followPlayer;
    public int npcLocation;
    public int moodOption = -1;
    public int result;//2,3,5结局
    public int decisionRate = -1;//开门决策值
    public int currencyState = 0;//当前情绪  1  高兴， 2 生气 3 难过 4 任性
    public void ClearData()
    {
        // this.happinessRate = -1; //开心值
        // this.upsetRate = -1; //伤心值
        // this.angerRate = -1; // 愤怒值
        this.decisionRate = -1;//决策值
        this.followPlayer = 0;
        this.npcLocation = -1;
        this.moodOption = -1;
        this.currencyState = 0;
        this.result = 0;
    }
}


public class AIResposeMessageData
{
    public string conversationId;
    public string gameData;
    public int remainingChatCnt;
}

public class AIResposeData
{
    public string message;
    public int isEnd;
}

public class AIRequestChatData
{
    public string content;
    public int location;
}

public class AIRequestMessageData
{
    public int gameId;
    public string conversationId;
    public string gameData;
    public string npcId;
}

#endregion
