using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    /// <summary>
    /// 通讯录
    /// </summary>
    public class ChatTXPanel : MonoBehaviour
    {
        public GameObject contentGo;
        public ChatTXItem chatTXItem;           //通讯录item模板
        public ScrollRect scrollRect;
        public ChatTXEmptyNode emptyNode;
        public ChatCallTXDetailNode chatCallTXDetailNode;
        public ChatMainPanel chatMainPanel;
        public ChatCallPopNode chatCallPopNode;
        public ChatVideoCallNode chatVideoCallNode; //处于通话中 点击call按钮恢复通话界面

        private readonly List<GameObject> _items = new List<GameObject>();

        private void OnEnable()
        {
            chatCallTXDetailNode?.gameObject.SetActive(false);
            LoadPublishList();
        }

        public void LoadPublishList()
        {
            CabinNetManager.Inst.GetNetCabinCharacterPublishList(CabinPurchasedType.All, OnPublishListReceived);
        }

        private void OnPublishListReceived(bool success, List<CabinPublishData> list)
        {
            if (this == null) return;
            ClearItems();

            bool hasItems = success && list != null && list.Count > 0;
            contentGo?.SetActive(hasItems);
            emptyNode?.gameObject.SetActive(!hasItems);
            if (!hasItems) return;

            var container = scrollRect != null ? scrollRect.content : (Transform)contentGo?.transform;
            chatTXItem.gameObject.SetActive(false);

            foreach (var data in list)
            {
                var go = Instantiate(chatTXItem.gameObject, container);
                go.SetActive(true);
                var item = go.GetComponent<ChatTXItem>();
                item?.Init(data, OnItemClicked);
                _items.Add(go);
            }
        }

        private void OnItemClicked(CabinPublishData data)
        {
            if (chatCallTXDetailNode == null) return;
            chatCallTXDetailNode.gameObject.SetActive(true);
            chatCallTXDetailNode.Init(data,
                (characterId) =>
                {
                    chatMainPanel.SwitchTab(0);
                    chatCallTXDetailNode.gameObject.SetActive(false);
                    chatMainPanel?.BeginCharacterChat(characterId,data.characterInfo.name, data.characterInfo.characterPortraitUrl); 
                },
                (characterId) =>
                {
                    if (chatCallPopNode != null)
                    {
                        chatCallPopNode.gameObject.SetActive(true);
                        chatCallPopNode.Init(data, () => chatCallPopNode.gameObject.SetActive(false));
                    }
                }
            );
        }

        private void ClearItems()
        {
            foreach (var go in _items)
                if (go != null) Destroy(go);
            _items.Clear();
            chatTXItem?.gameObject.SetActive(false);
        }

        [Button("测试数据")]
        void testData()
        {
            var list = new List<CabinPublishData>
            {
                new CabinPublishData(new CabinCharacterUgcInfo { name = "小雅", characterPortraitUrl = "", id = "c1",
                    createTime = new DateTimeOffset(2025, 1, 10, 0, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds() }),
                new CabinPublishData(new CabinCharacterUgcInfo { name = "阿凯", characterPortraitUrl = "", id = "c2",
                    createTime = new DateTimeOffset(2025, 3, 22, 0, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds() }),
                new CabinPublishData(new CabinCharacterUgcInfo { name = "Luna", characterPortraitUrl = "", id = "c3",
                    createTime = new DateTimeOffset(2024, 11, 5, 0, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds() }),
            };
            OnPublishListReceived(true, list);
        }
    }
}
