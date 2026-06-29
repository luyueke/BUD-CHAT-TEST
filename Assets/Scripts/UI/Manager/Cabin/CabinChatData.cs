using Game.Avatar;
using Game.Store;
using GameData.BaseInfo;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UI.UIPanels.IncubationCabin;
using UnityEngine;
using UnityEngine.Networking;

public class createBotStreamReq
{
    public List<CabinChatCreateBotRoleContent> messages;
    public string sessionId;
}

public class boxchatStreamReq
{
    public List<CabinChatCreateBotRoleContent> messages;
    public string characterId;
}


public class CabinChatCreateBotRoleContent
{
    public string role; //ai: assistant   用户:user
    public string content; //聊天内容
    public int timestamp; //时间戳
    public string audioUrl; //语音id
    public int audioDuration; //毫秒
    public string msgId; //消息id
}


/// <summary>
/// http sse
/// </summary>
public class CabinChatCreateBotChoiceData
{
    public string index;
    public CabinChatCreateBotRoleContent delta;
    public string finish_reason;
}

public class CabinChatCreateBotData
{
    public string id;
    public string created; //时间戳
    public List<CabinChatCreateBotChoiceData> choices;
    public CabinChatCreateBotProfileData botProfile;

    public TextChatOptions textChatOptions;
    public List<BotMatchTags> botMatchTags; //标签
    public string stage; //startExtract  aiReply  outputExtract
}

public class TextChatOptions
{
    public int audioDuration; //tts预估毫秒数
    public string msgId; //消息id
    public bool insufficient; //下一条或者当前余额不足(ai能量币)
}

/// <summary>
/// 标签信息 
/// </summary>
public class BotMatchTags
{
    public string categoryName; //性别/年龄/特质
    public int matchedTagId;//标签id
    public string matchedTag;//标签id
    public string reason;
}

public class CreateBotTagsResponse
{
    public List<BotMatchTags> botMatchTags;
}



public class CabinChatCreateBotProfileData
{
    public string name;
    public string path;
    public string ip_source;
    public string inspired_by_ip;
    public string persona;
    public string world;
    public string one_line_note;
    public List<string> greeting_candidates;
    public string characterName;
    public string characterDesc;
    public string toneId;
    /// <summary>AI 匹配的标签列表，由外层 CabinChatCreateBotData 写入</summary>
    public List<BotMatchTags> botMatchTags;
}


/// <summary>
/// 文字聊天历史记录
/// </summary>
public class CabinChatTextHistoryData
{
    public int isEnd; //0否 1是
    public List<CabinChatTextHistory> history;
}

public class CabinChatTextHistory
{
    public string role; //ai: assistant   用户:user
    public string content; //聊天内容
    public int timestamp; //时间戳
    public string audioUrl; //聊天内容tts语音 地址
    public int audioDuration; //语音预估的毫秒数
    public string msgId; //消息id
}

/// <summary>
/// 文字聊天会话列表
/// </summary>
public class CabinChatSessionList
{
    public int isEnd; //0否 1是
    public List<CabinChatSessionData> sessionList;
}

public class CabinChatSessionData
{
    public CabinChatTextHistory lastContent; //最近一条消息
    public string portraitUrl; //头像
    public string sessionId;
    public int timestamp;   //最近一条消息时间戳
    public string name;//角色名字
}

/// <summary>
/// 语音通话历史记录
/// </summary>
public class CabinAudioHistoryData
{
    public int isEnd; //0否 1是
    public List<CabinAudioHistory> history;
}

public class CabinAudioHistory
{
    public int startTime; //通话开始时间戳(秒)
    public int duration;  //通话时长(秒)
    public string sessionId;
    public string name;
    public string portraitUrl;
    public int timestamp;
}

/// <summary>
/// AI返回的选择题结构（当 delta.content 为此 JSON 时触发选择UI）
/// </summary>
public class ChatSelectionData
{
    public string reply;
    public List<ChatSelectionChip> chips;
    public string selection_mode; // "single" 或 "multi"
    public int multi_max;
}

public class ChatSelectionChip
{
    public string label;
    public string value;
}

