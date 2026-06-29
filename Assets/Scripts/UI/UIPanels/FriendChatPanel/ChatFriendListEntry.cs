using System;
using System.Collections.Generic;
using System.Linq;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using Game.Avatar;
using Game.Base;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UnityEngine;

namespace Game.GameHall.View
{
    public class ChatFriendListEntry : MonoBehaviour
    {
        public ChatFriendListAdapter adapter;
        private string cookie = "";
        private ConversationListReqQuery httpRequest = new ConversationListReqQuery();
        private bool isEnd = false;
        private Action<bool> hasFriendAct;

        private Action<ConversationListItem> clickAction;
        private string userId;
        private string searchWord;

        protected void Start()
        {
            PullToRefreshBehaviour refreshController = adapter.GetComponent<PullToRefreshBehaviour>();
            refreshController.OnRefreshWithSign.AddListener(OnPullReleased);
            adapter.OnItemsUpdated.AddListener(refreshController.HideGizmo);
        }


        public void SetAction(Action<ConversationListItem> action)
        {
            this.clickAction = action;
            adapter.SetClickAction(clickAction);
        }

        public void GetFirstPageFriendDatas(Action<bool> act,  string userId = "", string searchWord = "")
        {
            isEnd = false;
            cookie = "";
            hasFriendAct = act;
            this.userId = userId;
            this.searchWord = searchWord;
            RequestConversationList(userId,searchWord,datas =>
            {
                if (!GameController.IsInHallScene())
                {
                    if (AIBuddyAvatarController.Inst.SelfController != null)
                    {
                        var item = AIBuddyAvatarController.Inst.GetConversationListItem();
                        datas.Insert(0, item);
                    }
                }
                
                adapter.OnItemsUpdated?.Invoke();
                adapter.Data.ResetItems(datas, false);
                hasFriendAct?.Invoke(adapter.Data.Count > 0);
            });
        }
        
        public void SetFirst(TextChatData textChatData)
        {
            string fromUid = textChatData.fromUid;

            ConversationListItem foundItem = adapter.Data.List.Find(item => item.uid == fromUid);

            if (foundItem != null)
            {
                foundItem.count += 1;
                adapter.Data.List.Remove(foundItem);
                adapter.Data.List.Insert(0, foundItem);
            }
            else
            {
                foundItem = new ConversationListItem()
                {
                    count = 1,
                    coversationId = "",
                    isOnline = 0,
                    lastUpdateTime = "",
                    nickname = textChatData.nickName,
                    portraitUrl = textChatData.portraitUrl,
                    uid = textChatData.fromUid
                };
                adapter.Data.List.Insert(0, foundItem);
            }
            
            adapter.Refresh();
        }
        
        public void MoveItemToTop(string uid)
        {
            // 找到与指定uid匹配的item
            ConversationListItem foundItem = adapter.Data.List.Find(item => item.uid == uid);

            if (foundItem != null)
            {
                // 移除该item
                adapter.Data.List.Remove(foundItem);

                // 将其插入到列表的顶部
                adapter.Data.List.Insert(0, foundItem);
                
                adapter.Refresh();
            }
        }

        public void SetIsRead(string fromUid)
        {
            List<ConversationListItem> originalList = adapter.Data.List;
            ConversationListItem foundItem = originalList.FirstOrDefault(item => item.uid == fromUid);
            foundItem.count = 0;
        }

        public void SetSelect(string fromUid)
        {
            for (int i = 0; i < adapter.Data.List.Count; i++)
            {
                if (adapter.Data.List[i].uid == fromUid)
                {
                    adapter.Data.List[i].isSelect = true;
                }
                else
                {
                    adapter.Data.List[i].isSelect = false;
                }
            }
            adapter.Refresh();
        }
        
        public void OnPullReleased(float sign)
        {
            if (sign < 0)
            {
                RequestConversationList(userId,searchWord,OnReceivedNewModelsForInsert);
            }
            else if (sign > 0)
            {
                GetFirstPageFriendDatas(hasFriendAct);
            }
        }


        public void RequestConversationList(string userId,string searchWord,Action<List<ConversationListItem>> onResult)
        {
            httpRequest.cookie = cookie;
            httpRequest.toUid = userId;
            httpRequest.searchWord = searchWord;
            if (isEnd)
            {
                onResult?.Invoke(new List<ConversationListItem>());
                return;
            }
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ConversationList,
                HttpMethod.GET,
                JsonConvert.SerializeObject(httpRequest),
                onReceive: msg =>
                {

                    ConversationListResponse resourceInfo =
                        JsonConvert.DeserializeObject<ConversationListResponse>(msg);
                    isEnd = resourceInfo.isEnd == 1;
                    cookie = resourceInfo.cookie;
                    if (resourceInfo.list != null)
                    {
                        onResult?.Invoke(resourceInfo.list);
                    }
                    else
                    {
                        onResult?.Invoke(new List<ConversationListItem>());
                    }
                }, onFail: arg0 => { onResult?.Invoke(new List<ConversationListItem>()); });
        }


        void OnReceivedNewModelsForInsert(List<ConversationListItem> newModels)
        {
            if (newModels == null || newModels.Count == 0)
            {
                adapter.OnItemsUpdated?.Invoke();
                return;
            }

            adapter.Data.InsertItems(adapter.GetItemsCount(), newModels);
            adapter.OnItemsUpdated?.Invoke();
            hasFriendAct?.Invoke(adapter.Data.Count > 0);
        }

        public void ClickFirst()
        {
            if (clickAction == null)
            {
                return;
            }

            if (adapter.Data == null || adapter.Data.List.Count <= 0)
            {
                return;
            }
            
            this.clickAction.Invoke(adapter.Data.List[0]);
        }
        
        
    }
}

