using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    /// <summary>
    /// 通话界面
    /// </summary>
    public class ChatCallPanel : MonoBehaviour
    {
        public GameObject contentGo;           //有聊天记录
        public ScrollRect scrollRect;
        public GameObject chatCallRecordItemGo; //带有ChatCallRecordItem组件的预制件
        public ChatCallEmptyNode emptyNode;     //无聊天记录
        public ChatCallDetailNode chatCallDetailNode;

        private readonly List<GameObject> _recordItems = new List<GameObject>();

        private void OnEnable()
        {
            chatCallDetailNode?.gameObject.SetActive(false);
            LoadSessionList();
        }

        public void LoadSessionList()
        {
            CabinChatManager.Inst.GetBoxAudioHistory("", OnAudioHistoryReceived,
                (err) => Debug.LogError("GetBoxAudioHistory error: " + err));
        }

        private void OnAudioHistoryReceived(CabinAudioHistoryData data)
        {
            if (this == null) return;
            ClearItems();

            bool hasItems = data?.history != null && data.history.Count > 0;
            contentGo?.SetActive(hasItems);
            emptyNode?.gameObject.SetActive(!hasItems);
            if (!hasItems)
            {
                if (emptyNode != null) emptyNode.ShowEmpty();
                return;
            }

            var container = scrollRect != null ? scrollRect.content : contentGo?.transform;
            foreach (var record in data.history)
            {
                var go = Instantiate(chatCallRecordItemGo, container);
                if (go.TryGetComponent<ChatCallRecordItem>(out var item)) item.Init(record, OnRecordItemClicked);
                _recordItems.Add(go);
            }
        }

        private void OnRecordItemClicked(CabinAudioHistory record)
        {
            if (chatCallDetailNode == null) return;
            chatCallDetailNode.gameObject.SetActive(true);
            chatCallDetailNode.Init(record);
        }

        private void ClearItems()
        {
            foreach (var go in _recordItems)
                if (go != null) Destroy(go);
            _recordItems.Clear();
        }

        [Button("测试数据")]
        void testData()
        {
            int now = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var mockData = new CabinAudioHistoryData
            {
                history = new List<CabinAudioHistory>
                {
                    new CabinAudioHistory { sessionId = "s1", name = "小雅",  startTime = now - 300,       duration = 75,  timestamp = now - 300       },
                    new CabinAudioHistory { sessionId = "s2", name = "阿凯",  startTime = now - 86400,     duration = 182, timestamp = now - 86400     },
                    new CabinAudioHistory { sessionId = "s3", name = "Luna",  startTime = now - 86400 * 5, duration = 30,  timestamp = now - 86400 * 5 },
                }
            };
            OnAudioHistoryReceived(mockData);
        }
    }
}
