using System;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.GameHall.View
{
    public class FollowListEntry : MonoBehaviour
    {
        public FollowListAdapter adapter;
        private List<MyFriendsInfo> allModels = new List<MyFriendsInfo>();
        private string cookie = "";
        private MyFriendsListReqQuerry httpRequest = new MyFriendsListReqQuerry();
        private bool isEnd = false;
        private Action<bool> hasFriendAct;

        protected void Start()
        {
            //需要动态拉取数据必须要做的初始化操作
            PullToRefreshBehaviour refreshController = adapter.GetComponent<PullToRefreshBehaviour>();
            refreshController.OnRefreshWithSign.AddListener(OnPullReleased);
            adapter.OnItemsUpdated.AddListener(refreshController.HideGizmo);
        }


        public void GetFirstPageFriendDatas(Action<bool> act)
        {
            isEnd = false;
            cookie = "";
            hasFriendAct = act;
            RequestFriendList(datas =>
            {
                adapter.OnItemsUpdated?.Invoke();
                adapter.Data.ResetItems(datas, false);
                hasFriendAct?.Invoke(adapter.Data.Count > 0);
            });
        }

        /// <summary>
        /// 可以调整单元格数据内容,实现单元格大小分类致等特殊需求
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private MyFriendsInfo CreateNewModel(int index)
        {
            if (index >= 0 && index < allModels.Count)
            {
                return allModels[index];
            }

            return new MyFriendsInfo();
        }

        public void OnPullReleased(float sign)
        {
            if (sign < 0)
            {
                RequestFriendList(OnReceivedNewModelsForInsert);
            }
            else if (sign > 0)
            {
                GetFirstPageFriendDatas(hasFriendAct);
            }
        }


        public void RequestFriendList(Action<List<MyFriendsInfo>> onResult)
        {
            httpRequest.relationShip = 1;
            httpRequest.relationStatus = 1;
            httpRequest.cookie = cookie;
            if (isEnd)
            {
                onResult?.Invoke(new List<MyFriendsInfo>());
                return;
            }
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.friendList,
                HttpMethod.GET,
                JsonConvert.SerializeObject(httpRequest),
                onReceive: msg =>
                {

                    MyFriendsListResData resourceInfo =
                        JsonConvert.DeserializeObject<MyFriendsListResData>(msg);
                    isEnd = resourceInfo.isEnd == 1;
                    cookie = resourceInfo.cookie;
                    if (resourceInfo.list != null)
                    {
                        onResult?.Invoke(resourceInfo.list);
                    }
                    else
                    {
                        onResult?.Invoke(new List<MyFriendsInfo>());
                    }
                }, onFail: arg0 => { onResult?.Invoke(new List<MyFriendsInfo>()); });
        }


        void OnReceivedNewModelsForInsert(List<MyFriendsInfo> newModels)
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


        public void AddClickListener(Action<PointerEventData> act)
        {
            adapter.AddClickListener(act);
        }
        
    }
}

