using System;
using System.Collections;
using System.Collections.Generic;
using AIGame.Base;
using DG.Tweening;
using Message;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class AIParkChatMgr
{
    private static AIParkChatMgr _instance;
    public static AIParkChatMgr Inst
    {
        get
        {
            return _instance ?? (_instance = new AIParkChatMgr());
        }
    }

    List<AIParkChatInfo> _chatList;

    int _curReadIndex = 0;
    bool _lockUnReadState = false; // 锁定为未读状态 收到聊天一般是直接已读 这里锁定为未读状态

    public AIParkChatMgr()
    {
    }

    public void Init()
    {
        _chatList ??= new();
        _chatList.Clear();
    }

    public List<AIParkChatInfo> GetChatList()
    {
        return _chatList;
    }

    public void SetCurReadIndex(int index)
    {
        if (index > _curReadIndex)
        {
            for (int i = _curReadIndex; i < index; i++)
            {
                _chatList[i].isRead = true;
            }
        }
        _curReadIndex = index;
    }

    public int GetCurReadIndex()
    {
        for (int i = 0; i < _chatList.Count; i++)
        {
            if (_chatList[i].isRead == false)
            {

            }
        }
        return 0;
    }


    public void SetLockUnReadState(bool isUnReadState)
    {
        _lockUnReadState = isUnReadState;
    }

    public void InsertChat(string npcName, string content)
    {
        string npcId = AIPark_NpcUtil.GetNpcId(npcName);
        AIParkChatInfo chatInfo = new AIParkChatInfo() { npcId = npcId, npcName = npcName, content = content, chatId = _chatList.Count };
        if (_lockUnReadState)
        {
            chatInfo.isRead = false;
            _chatList.Add(chatInfo);
        }
        else
        {
            chatInfo.isRead = true;
            _chatList.Add(chatInfo);
        }
        MessageHelper.Broadcast(MessageName.S11ParkReceiveNewChat, chatInfo);
    }

    public void SetAllChatRead()
    {
        foreach (var chat in _chatList)
        {
            chat.isRead = true;
        }
    }

    public int GetUnReadCount()
    {
        int count = 0;
        foreach (var chat in _chatList)
        {
            if (chat.isRead == false)
            {
                count++;
            }
        }
        return count;
    }

    public int GetFirstUnReadChatId()
    {
        for (int i = _chatList.Count - 1; i >= 0; i--)
        {
            if (_chatList[i].isRead && i < _chatList.Count - 1)
            {
                return _chatList[i + 1].chatId;
            }
        }
        return -1;
    }


    public void TestAddChat()
    {
        InsertChat("1", "test");
        InsertChat("2", "test2");
        InsertChat("3", "test3");
        InsertChat("4", "test4");
        InsertChat("5", "test5");
        InsertChat("6", "test6");
        InsertChat("7", "test7");
    }
}

public class AIParkChatInfo
{
    public int chatId; //客户端自己维护的id
    public string npcId;
    public string npcName;
    public string content;
    public bool isRead = false;
}

