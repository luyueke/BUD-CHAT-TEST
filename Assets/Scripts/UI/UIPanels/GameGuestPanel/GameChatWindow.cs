using System.Collections;
using Es;
using Game.Utils;
using GameData.GameSync;
using Message;
using Pb.Game;
using UI.BaseWidgets;
using UnityEngine;

namespace UI.UIPanels
{
    public class GameChatWindow : MonoBehaviour
    {
        [SerializeField]private GameChatOSAEntry dataEntry;
        [SerializeField]private TabView tabView;
        [SerializeField]private GameObject[] redDotObjs;

        ChatType curSelectChat;

        void Awake()
        {
            tabView.AddItemSelectCallBack(OnChatTypeSelect);
        }

        void Start()
        {
            NetSyncManager.Inst.AddBroadcastListener(SubCmdType.Chat, OnChatRecv);
            ChatDispatcherUtils.Inst.AddChatListener(OnChatDispatch);
            GameAIBuddyChatManager.Inst.AddLocalBuddyChatMsgListener(HandleLocalBuddyChat);
            MessageHelper.AddListener<string, InteractSyncData>(MessageName.OnBuddyCommandChat, OnBuddyCommandChat);
            FirstSelect();
        }

        void OnDestroy()
        {
            NetSyncManager.Inst.RemoveBroadcastListener(SubCmdType.Chat, OnChatRecv);
            ChatDispatcherUtils.Inst.RemoveChatListener(OnChatDispatch);
            GameAIBuddyChatManager.Inst.RemoveLocalBuddyChatMsgListener(HandleLocalBuddyChat);
            MessageHelper.RemoveListener<string, InteractSyncData>(MessageName.OnBuddyCommandChat, OnBuddyCommandChat);
        }

        // AI 伙伴口令互动：Chat 分类显示玩家发出的指令，Emote 分类显示伙伴做的动作名
        void OnBuddyCommandChat(string playerId, InteractSyncData data)
        {
            if (data == null) return;

            if (!string.IsNullOrEmpty(data.Command))
            {
                var content = $"@{data.BuddyName} {data.Command}";
                dataEntry.AddPlayerMessage(content, playerId, ChatType.Chat);
                ShowRedDot(ChatType.Chat);
            }

            var emoteName = GetEmoteName(data);
            if (!string.IsNullOrEmpty(emoteName))
            {
                dataEntry.AddBuddyEmoteMessage(playerId, data.BuddyName, emoteName);
                ShowRedDot(ChatType.Emote);
            }
        }

        // 取 PGC 动作的本地化名称（与玩家做表情时 Emote 分类一致）；UGC 动作无配置名返回空
        string GetEmoteName(InteractSyncData data)
        {
            if (data.IsPgc != 1 || string.IsNullOrEmpty(data.EmoteId)) return string.Empty;
            var emoteConfig = DataTables.GetEmoUIConfig(data.EmoteId);
            if (emoteConfig == null) return string.Empty;
            var pgcData = DataTables.GetPgcNameData(data.EmoteId);
            return LocalizationManager.Inst.GetLocalizedText(pgcData != null ? pgcData.Name : emoteConfig.name);
        }

        void OnChatRecv(CommonSyncClientData netData)
        {
            var senderId = netData.PalyerId;
            var chatNetData = (ChatNetData)netData.Body;

            if (string.IsNullOrEmpty(chatNetData.BuddyId))
            {
                HandlePlayerChat(senderId, chatNetData);
            }
            else
            {
                HandleBuddyChat(senderId, chatNetData);
            }
        }

        private void HandlePlayerChat(string senderId, ChatNetData chatNetData)
        {
            dataEntry.AddPlayerMessage(chatNetData.Content, senderId, ChatType.Chat);
            ShowRedDot(ChatType.Chat);
        }

        private void HandleBuddyChat(string senderId, ChatNetData chatNetData)
        {
            dataEntry.AddBuddyMessage(senderId, chatNetData);
            ShowRedDot(ChatType.Chat);
        }

        private void HandleLocalBuddyChat(string senderId, string buddyName, string content)
        {
            var chatNetData = new ChatNetData
            {
                Content = content,
                BuddyId = senderId,
                BuddyName = buddyName
            };
            dataEntry.AddBuddyMessage(senderId, chatNetData);
            ShowRedDot(ChatType.Chat);
        }

        void OnChatDispatch(string msg, ChatType chatType, string playerId)
        {
            if (string.IsNullOrEmpty(playerId))
            {
                dataEntry.AddMessage(msg, chatType);
                ShowRedDot(chatType);
            }
            else
            {
                dataEntry.AddPlayerMessage(msg, playerId, chatType);
                ShowRedDot(chatType);
            }
            
        }

        void FirstSelect()
        {
            var chatType = (ChatType)0;
            curSelectChat = chatType;
            dataEntry.FilterData(chatType);
            HideRedDot(chatType);
        }

        void OnChatTypeSelect(TabItem item, int index)
        {
            var chatType = (ChatType)index;
            curSelectChat = chatType;
            dataEntry.FilterData(chatType);
            HideRedDot(chatType);
        }

        void ShowRedDot(ChatType chatType)
        {
            if (curSelectChat != chatType)
            {
                var index = (int)chatType;
                if (redDotObjs.Length > index)
                {
                    redDotObjs[index].SetActive(true);
                }
            }
        }

        void HideRedDot(ChatType chatType)
        {
            var index = (int)chatType;
            if (redDotObjs.Length > index)
            {
                redDotObjs[index].SetActive(false);
            }
        }
    }
}
