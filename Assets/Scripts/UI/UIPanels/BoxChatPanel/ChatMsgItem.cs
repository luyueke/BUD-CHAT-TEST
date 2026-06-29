using Com.TheFallenGames.OSA.Util.IO;
using Sirenix.OdinInspector;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    /// <summary>
    /// 聊天消息界面-消息
    /// </summary>
    public class ChatMsgItem : MonoBehaviour
    {
        public RemoteImageBehaviour remoteImageBehaviour; //头像
        public Text roleNameText;                         //聊天角色名
        public Text chatContentText;                      //聊天内容
        // 今天：显示具体时间，HH:mm，如 9:00
        // 昨天：显示「昨天」
        // 前 7 天以内（不含昨天）：显示星期几，如「周三」「周二」
        // 超过 7 天、当年以内：显示月日，如 4月3日
        // 跨年：显示完整日期，如 2024/12/25
        public Text chatMsgTime;
        public Button enterChatPanelBtn;

        private CabinChatSessionData _sessionData;
        private ChatMainPanel _chatMainPanel;

        public void SetData(CabinChatSessionData data, ChatMainPanel chatMainPanel)
        {
            _sessionData = data;
            _chatMainPanel = chatMainPanel;

            if (!string.IsNullOrEmpty(data.portraitUrl))
                remoteImageBehaviour.Load(data.portraitUrl);

            string chatContStr = data.lastContent?.content;
            if (string.IsNullOrEmpty(chatContStr))
            {
                chatContStr = "你们成为伙伴了,开始聊天吧";
            }

            roleNameText.text = data.name;
            chatContentText.text = chatContStr;
            chatMsgTime.text = FormatTimestamp(data.timestamp);

            enterChatPanelBtn.onClick.RemoveAllListeners();
            enterChatPanelBtn.onClick.AddListener(OnEnterChatPanel);
        }

        private void OnEnterChatPanel()
        {
            _chatMainPanel.chatPanel.gameObject.SetActive(true);
            _chatMainPanel.HideAll();
            _chatMainPanel.BeginCharacterChat(_sessionData.sessionId,_sessionData.name,_sessionData.portraitUrl);
        }

    

        private static string FormatTimestamp(int timestamp)
        {
            var dt = DateTimeOffset.FromUnixTimeSeconds(timestamp).LocalDateTime;
            var now = DateTime.Now;
            int diffDays = (now.Date - dt.Date).Days;

            if (diffDays < 1) return dt.ToString("H:mm");
            if (diffDays < 2) return "昨天";
            if (diffDays < 7)
            {
                string[] weeks = { "周日", "周一", "周二", "周三", "周四", "周五", "周六" };
                return weeks[(int)dt.DayOfWeek];
            }
            if (dt.Year == now.Year) return $"{dt.Month}月{dt.Day}日";
            return dt.ToString("yyyy/MM/dd");
        }

        [Button("测试聊天历史记录")]
        void testHistory()
        {
            // _chatMainPanel.chatPanel.gameObject.SetActive(true);
            // var mockData = new CabinChatTextHistoryData
            // {
            //     isEnd = 1,
            //     history = new CabinChatTextHistory
            //     {
            //         role = "assistant",
            //         content = "你好！我是你的专属助手，有什么我可以帮你的吗？",
            //         timestamp = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            //     }
            // };
            // OnHistoryReceived(mockData);
        }
    }
}
