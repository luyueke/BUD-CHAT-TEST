using System;
using System.Collections;
using System.Collections.Generic;
using Game.Avatar;
using GameData.Account;
using GameData.BaseInfo;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UnityEngine;

public class GameAIBuddyChatManager : GlobalInstance<GameAIBuddyChatManager>
{
    private bool isLockInput = false;

    private AINpcInfo currentBuddyInfo;
    private string currentLocalBuddyID;
    private string curConversationId;
    private Action<string, string, string> onRevLocalAIChatMsg;
    public void StartChatToAIBuddy(bool withEmote = false, string npcID = "")
    {
        if (isLockInput)
            return;

        isLockInput = true;
        KeyBoardInfo keyBoardInfo = new KeyBoardInfo
        {
            type = 0,
            placeHolder = LocalizationManager.Inst.GetLocalizedText("请输入文字"),
            inputMode = 2,
            maxLength = 250,
            inputFlag = 0,
            lengthTips = LocalizationManager.Inst.GetLocalizedText("不能超过250字符"),
            defaultText = "",
            returnKeyType = (int)ReturnType.Send
        };
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, OnShowKeyBoard);
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.hideKeyboard, OnHideKeyBoard);
        if (withEmote)
        {
            MobileInterface.Instance.ShowEmoteKeyboard(JsonUtility.ToJson(keyBoardInfo), npcID);
        }
        else
            MobileInterface.Instance.ShowKeyboard(JsonUtility.ToJson(keyBoardInfo));
    }
    
    private void OnHideKeyBoard(string str) {
        isLockInput = false;
    }

    private void OnShowKeyBoard(string str)
    {
        MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);
        if (string.IsNullOrEmpty(str))
        {
            return;
        }

        var buddyInfo = AIBuddyAvatarController.Inst.SelfAIBuddyInfo;
        TextChatData textChatData = new TextChatData()
        {
            fromUid = AccountDataManager.Inst.Uid,
            toUid = buddyInfo?.uid,
            message = str,
            nickName = AccountDataManager.Inst.UserInfo.nickname,
            portraitUrl = AccountDataManager.Inst.UserInfo.portraitUrl,
            chatBubbles = AccountDataManager.Inst.UserInfo.chatBubbles,
            avatarFrame = AccountDataManager.Inst.UserInfo.avatarFrame,
            nicknameFrame = AccountDataManager.Inst.UserInfo.nicknameFrame
        };

        SendAIBuddyChat(str);
    }
    
    public void StartChatToLocalAIBuddy(AINpcInfo buddyInfo, string id, bool withEmote = false)
    {
        if (isLockInput)
            return;

        isLockInput = true;
        currentBuddyInfo = buddyInfo;
        currentLocalBuddyID = id;
        KeyBoardInfo keyBoardInfo = new KeyBoardInfo
        {
            type = 0,
            placeHolder = LocalizationManager.Inst.GetLocalizedText("请输入文字"),
            inputMode = 2,
            maxLength = 250,
            inputFlag = 0,
            lengthTips = LocalizationManager.Inst.GetLocalizedText("不能超过250字符"),
            defaultText = "",
            returnKeyType = (int)ReturnType.Send
        };
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, OnShowKeyBoardLocalChat);
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.hideKeyboard, OnHideKeyBoardLocalChat);
        if (withEmote)
        {
            MobileInterface.Instance.ShowEmoteKeyboard(JsonUtility.ToJson(keyBoardInfo), buddyInfo?.id);
        }
        else
            MobileInterface.Instance.ShowKeyboard(JsonUtility.ToJson(keyBoardInfo));
    }

    private void OnHideKeyBoardLocalChat(string str)
    {
        isLockInput = false;
    }

    private void OnShowKeyBoardLocalChat(string str)
    {
        MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);
        if (string.IsNullOrEmpty(str))
        {
            isLockInput = false;
            return;
        }

        if (currentBuddyInfo != null)
        {
            SendLocalAIBuddyChat(currentBuddyInfo, currentLocalBuddyID, str);
        }
    }


    private void SendAIBuddyChat(string input = null)
    {

        GameAIBuddyManager.Inst.ChatToAIBuddyAndBroadcast(input);
        var buddyInfo = AIBuddyAvatarController.Inst.SelfAIBuddyInfo;
        AIBuddyChatReq reqData = new AIBuddyChatReq()
        {
            id = buddyInfo?.id,
            message = new AIBuddyChatMessage()
            {
                role = 1,
                content = input,
            }
        };

        string allContent = null;
        bool isAllEnd = false;
        NetworkManager.Inst.SendHttpRequestOnStream(HttpUrlDefine.AIBuddyChat, HttpMethod.POST,
            JsonConvert.SerializeObject(reqData), (content) =>
            {
                if (string.IsNullOrEmpty(content))
                {
                    isLockInput = false;
                    return;
                }

                var responseData = JsonConvert.DeserializeObject<AIResposeData>(content);
                if (string.IsNullOrEmpty(responseData?.message))
                {
                    isLockInput = false;
                    return;
                }

                bool isEnd = responseData.isEnd == 1;
                if (string.IsNullOrEmpty(responseData?.message))
                {
                    isLockInput = false;
                    return;
                }

                var msgDataRsp = JsonConvert.DeserializeObject<AIBuddyChatMessage>(responseData.message);
                allContent += msgDataRsp.content;
                if (isEnd)
                {
                    isLockInput = false;
                    isAllEnd = true;
                    OnAIBuddyGuestSceneBuddyRsp(buddyInfo?.npc?.id);
                    GameAIBuddyManager.Inst.RcvAIBuddyChatAndBroadcast(buddyInfo, allContent);
                }
            }, () =>
            {
                isLockInput = false;
            });
    }
    

    private void SendLocalAIBuddyChat(AINpcInfo buddyInfo, string id, string input = null)
    {

        AINpcChatReq reqData = new AINpcChatReq() {
            npcId = buddyInfo.id,
            conversationId = curConversationId,
            query = input,
        };
        string allContent = null;
        bool isAllEnd = false;
        string currentID = id;
        string currentBuddyName = buddyInfo?.npcName;
        
        onRevLocalAIChatMsg?.Invoke(AccountDataManager.Inst.Uid, AccountDataManager.Inst.UserInfo.nickname, input);

        NetworkManager.Inst.SendHttpRequestOnStream(HttpUrlDefine.AIUIChat, HttpMethod.POST,
            JsonConvert.SerializeObject(reqData), (content) =>
            {
                if (string.IsNullOrEmpty(content))
                {
                    isLockInput = false;
                    currentBuddyInfo = null;
                    currentLocalBuddyID = null;
                    return;
                }

                var responseData = JsonConvert.DeserializeObject<AIResposeData>(content);
                if (string.IsNullOrEmpty(responseData?.message))
                {
                    isLockInput = false;
                    currentBuddyInfo = null;
                    currentLocalBuddyID = null;
                    return;
                }

                bool isEnd = responseData.isEnd == 1;

                var msgDataRsp = JsonConvert.DeserializeObject<AINpcChatRsp>(responseData.message);
                allContent += msgDataRsp.reply;
                if (isEnd)
                {
                    onRevLocalAIChatMsg?.Invoke(currentID, currentBuddyName, allContent);
                    isLockInput = false;
                    currentBuddyInfo = null;
                    currentLocalBuddyID = null;
                    isAllEnd = true;
                    OnAIBuddyGuestSceneBuddyRsp(buddyInfo?.id);
                }
            }, () =>
            {
                isLockInput = false;
                currentBuddyInfo = null;
                currentLocalBuddyID = null;
            });
    }

    public void AddLocalBuddyChatMsgListener(Action<string, string, string> act)
    {
        onRevLocalAIChatMsg += act;
    }

    public void RemoveLocalBuddyChatMsgListener(Action<string, string, string> act)
    {
        onRevLocalAIChatMsg -= act;
    }

    private class AIBuddyChatMessage
    {
        public string content;

        // 1 为自己， 2 为 aiBuddy
        public int role;
        public string pgcEmote;
        public string ugcEmote;
    }

    private class AIBuddyChatReq
    {
        public string id;
        public AIBuddyChatMessage message;
    }

    private class AINpcChatReq {
        public string conversationId;
        public string npcId;
        public string query;
    }

    private class AINpcChatRsp {
        public string conversationId;
        public string reply;
        public string pgcEmote;
        public string ugcEmote;
        public int remainingChatCnt;
    }

    #region 埋点上报
    private Dictionary<string, int> _aiBuddyBuddyProfilePageChatCount = new Dictionary<string, int>();
    private Dictionary<string, int> _aiBuddyGuestSceneChatCount = new Dictionary<string, int>();
    private Dictionary<string, int> _aiBuddyNpcStoreChatCount = new Dictionary<string, int>();

    public void OnAIBuddyProfilePageBuddyRsp(string npcId)
    {
        if (!_aiBuddyBuddyProfilePageChatCount.ContainsKey(npcId))
        {
            _aiBuddyBuddyProfilePageChatCount.Add(npcId, 0);
        }

        _aiBuddyBuddyProfilePageChatCount[npcId]++;
    }
    
    public void OnAIBuddyGuestSceneBuddyRsp(string npcId)
    {
        if (!_aiBuddyGuestSceneChatCount.ContainsKey(npcId))
        {
            _aiBuddyGuestSceneChatCount.Add(npcId, 0);
        }

        _aiBuddyGuestSceneChatCount[npcId]++;
    }

    public void OnAIBuddyNpcStoreBuddyRsp(string npcId)
    {
        if (!_aiBuddyNpcStoreChatCount.ContainsKey(npcId))
        {
            _aiBuddyNpcStoreChatCount.Add(npcId, 0);
        }

        _aiBuddyNpcStoreChatCount[npcId]++;
    }

    public void LogAIBuddyChatInfo()
    {
        if (_aiBuddyBuddyProfilePageChatCount != null)
        {
            foreach (var buddyChatData in _aiBuddyBuddyProfilePageChatCount)
            {
                Dictionary<string, object> trackData = new Dictionary<string, object>();
                trackData.Add("uid", AccountDataManager.Inst.Uid);
                trackData.Add("NPC_ID", buddyChatData.Key);
                trackData.Add("NPC_ChatCount", buddyChatData.Value);
                trackData.Add("Scene", (int)NpcChatLogType.BuddyProfilePage);
                AnalyticsManager.Inst.Track(AnalyticsEventName.AIBUDDY_CHAT, trackData);
            }
            _aiBuddyBuddyProfilePageChatCount.Clear();
        }
        
        if (_aiBuddyGuestSceneChatCount != null)
        {
            foreach (var buddyChatData in _aiBuddyGuestSceneChatCount)
            {
                Dictionary<string, object> trackData = new Dictionary<string, object>();
                trackData.Add("uid", AccountDataManager.Inst.Uid);
                trackData.Add("NPC_ID", buddyChatData.Key);
                trackData.Add("NPC_ChatCount", buddyChatData.Value);
                trackData.Add("Scene", (int)NpcChatLogType.GuestScene);
                AnalyticsManager.Inst.Track(AnalyticsEventName.AIBUDDY_CHAT, trackData);
            }
            _aiBuddyGuestSceneChatCount.Clear();
        }
        
        if (_aiBuddyNpcStoreChatCount != null)
        {
            foreach (var buddyChatData in _aiBuddyNpcStoreChatCount)
            {
                Dictionary<string, object> trackData = new Dictionary<string, object>();
                trackData.Add("uid", AccountDataManager.Inst.Uid);
                trackData.Add("NPC_ID", buddyChatData.Key);
                trackData.Add("NPC_ChatCount", buddyChatData.Value);
                trackData.Add("Scene", (int)NpcChatLogType.NpcStore);
                AnalyticsManager.Inst.Track(AnalyticsEventName.AIBUDDY_CHAT, trackData);
            }
            _aiBuddyNpcStoreChatCount.Clear();
        }
    }

    #endregion
}

public enum NpcChatLogType
{
    BuddyProfilePage = 1,
    GuestScene = 2,
    NpcStore = 3,
}
