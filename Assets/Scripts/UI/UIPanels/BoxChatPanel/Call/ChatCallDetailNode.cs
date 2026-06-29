using Com.TheFallenGames.OSA.Util.IO;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    /// <summary>
    /// 通话详情界面
    /// </summary>
    public class ChatCallDetailNode : MonoBehaviour
    {
        public RemoteImageBehaviour bigRemoteImageBehaviour;   //头像(遮罩底)
        public RemoteImageBehaviour smallRemoteImageBehaviour; //头像
        public ChatCallRecordDetailItem chatCallRecordDetailItem; //通话记录item模板

        public GameObject callDetailBg;
        public Button closeBtn;

        private readonly List<ChatCallRecordDetailItem> _detailItems = new List<ChatCallRecordDetailItem>();

        void Awake()
        {
            closeBtn.onClick.AddListener(Close);
        }
        public void OnEnable()
        {
            callDetailBg?.SetActive(true);
        }

        void OnDisable()
        {
            callDetailBg?.SetActive(false);
        }

        void Close()
        {
            gameObject.SetActive(false);
        }

        public void Init(CabinAudioHistory data)
        {
            if (!string.IsNullOrEmpty(data.portraitUrl))
            {
                bigRemoteImageBehaviour?.Load(data.portraitUrl);
                smallRemoteImageBehaviour?.Load(data.portraitUrl);
            }

            OnHistoryReceived(new CabinAudioHistoryData
            {
                history = new List<CabinAudioHistory> { data }
            });
        }

        private void OnHistoryReceived(CabinAudioHistoryData data)
        {
            if (this == null) return;
            ClearItems();

            if (chatCallRecordDetailItem == null) return;

            if (data?.history == null || data.history.Count == 0)
            {
                chatCallRecordDetailItem.gameObject.SetActive(false);
                return;
            }

            var container = chatCallRecordDetailItem.transform.parent;
            chatCallRecordDetailItem.gameObject.SetActive(false);

            foreach (var h in data.history)
            {
                var go = Instantiate(chatCallRecordDetailItem.gameObject, container);
                go.SetActive(true);
                var item = go.GetComponent<ChatCallRecordDetailItem>();
                item?.Init(h);
                _detailItems.Add(item);
            }
        }

        private void ClearItems()
        {
            foreach (var item in _detailItems)
                if (item != null) Destroy(item.gameObject);
            _detailItems.Clear();

            chatCallRecordDetailItem?.gameObject.SetActive(false);
        }

        [Button("测试数据")]
        void testData()
        {
            int now = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var mockHistory = new CabinAudioHistoryData
            {
                isEnd = 1,
                history = new List<CabinAudioHistory>
                {
                    new CabinAudioHistory { startTime = now - 1800,       duration = 125  },
                    new CabinAudioHistory { startTime = now - 86400,      duration = 305  },
                    new CabinAudioHistory { startTime = now - 86400 * 3,  duration = 62   },
                    new CabinAudioHistory { startTime = now - 86400 * 10, duration = 1830 },
                }
            };
            OnHistoryReceived(mockHistory);
        }
    }
}
