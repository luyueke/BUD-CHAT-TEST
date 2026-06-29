using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    /// <summary>
    /// 聊天消息界面
    /// </summary>
    public class ChatMsgPanel : MonoBehaviour
    {
        public GameObject ChatMsgItemGo; //带有ChatMsgItem组件
        public GameObject ScrollViewContentGo;
        public GameObject EmptyContentGo; //空会话
        public ChatMainPanel chatMainPanel;

        public Button btnToTX;

        private readonly List<GameObject> _sessionItems = new List<GameObject>();

        void OnEnable()
        {
            CabinChatManager.Inst.GetBoxSessionList(0, OnSessionListReceived);
            if(btnToTX != null)
            {
                btnToTX.onClick.AddListener(onBtnToTXClick);
            }
        }

        void onBtnToTXClick()
        {
            
        }

        private void OnSessionListReceived(CabinChatSessionList data)
        {
            if(this == null) return;
            ClearItems();

            bool hasItems = data?.sessionList != null && data.sessionList.Count > 0;
            ScrollViewContentGo.SetActive(hasItems);
            EmptyContentGo.SetActive(!hasItems);

            if (!hasItems) return;

            foreach (var session in data.sessionList)
            {
                var go = Instantiate(ChatMsgItemGo, ScrollViewContentGo.transform);
                var item = go.GetComponent<ChatMsgItem>();
                if (item == null) continue;

                item.SetData(session, chatMainPanel);
                _sessionItems.Add(go);
            }
        }

        private void ClearItems()
        {
            foreach (var go in _sessionItems)
            {
                if (go != null) Destroy(go);
            }
            _sessionItems.Clear();
        }

        [Button("生成几条测试消息")]
        void TestMsg()
        {
            int now = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            int yesterday = now - 86400;
            int threeDaysAgo = now - 86400 * 3;
            int fifteenDaysAgo = now - 86400 * 15;
            int crossYear = (int)new DateTimeOffset(2024, 12, 25, 10, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds();

            var mockData = new CabinChatSessionList
            {
                sessionList = new List<CabinChatSessionData>
                {
                    new CabinChatSessionData { sessionId = "s1", portraitUrl = "", timestamp = now,           lastContent = new CabinChatTextHistory { role = "assistant", content = "今天的消息，刚刚说的" } },
                    new CabinChatSessionData { sessionId = "s2", portraitUrl = "", timestamp = yesterday,     lastContent = new CabinChatTextHistory { role = "user",      content = "昨天发来的消息" } },
                    new CabinChatSessionData { sessionId = "s3", portraitUrl = "", timestamp = threeDaysAgo,  lastContent = new CabinChatTextHistory { role = "assistant", content = "3天前的消息，显示周几" } },
                    new CabinChatSessionData { sessionId = "s4", portraitUrl = "", timestamp = fifteenDaysAgo,lastContent = new CabinChatTextHistory { role = "user",      content = "半个月前，显示月日" } },
                    new CabinChatSessionData { sessionId = "s5", portraitUrl = "", timestamp = crossYear,     lastContent = new CabinChatTextHistory { role = "assistant", content = "跨年消息，显示完整日期" } },
                }
            };

            OnSessionListReceived(mockData);
        }
    }
}