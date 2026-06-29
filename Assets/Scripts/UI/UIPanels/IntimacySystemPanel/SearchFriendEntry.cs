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
    public class SearchFriendEntry : MonoBehaviour
    {
        public SearchFriendAdapter adapter;
        private List<MyFriendsInfo> allModels = new List<MyFriendsInfo>();
        private string cookie = "";
        private SearchFriendParams searchFriendParams = new SearchFriendParams();
        private bool isEnd = false;
        private Action<bool> hasFriendAct;
        private string searchContent;

        private RelationShipType relationShipType;

        protected void Start()
        {
            //需要动态拉取数据必须要做的初始化操作
            PullToRefreshBehaviour refreshController = adapter.GetComponent<PullToRefreshBehaviour>();
            refreshController.OnRefreshWithSign.AddListener(OnPullReleased);
            adapter.OnItemsUpdated.AddListener(refreshController.HideGizmo);
        }


        public void GetFirstPageFriendDatas(RelationShipType relationShipType,string searchContent, Action<bool> act)
        {
            isEnd = false;
            cookie = "";
            hasFriendAct = act;
            this.searchContent = searchContent;
            this.relationShipType = relationShipType;
            RequestFriendList(datas =>
            {
                adapter.OnItemsUpdated?.Invoke();
                adapter.Data.ResetItems(datas, false);
                hasFriendAct?.Invoke(adapter.Data.Count > 0);
            });
        }

        public void OnPullReleased(float sign)
        {
            if (sign < 0)
            {
                RequestFriendList(OnReceivedNewModelsForInsert);
            }
            else if (sign > 0)
            {
                GetFirstPageFriendDatas(relationShipType,searchContent, hasFriendAct);
            }
        }

        public void RequestFriendList(Action<List<MyFriendsInfo>> onResult)
        {
            searchFriendParams.searchWord = this.searchContent;
            searchFriendParams.cookie = cookie;
            searchFriendParams.relationStatus = relationShipType == RelationShipType.Friend ?(int)RelationStatusType.Each : (int)RelationStatusType.Posi;
            searchFriendParams.relationShip = (int)relationShipType;
            
            if (isEnd || searchFriendParams.relationShip == 0) //没有0这种关系，防止服务器报错
            {
                onResult?.Invoke(new List<MyFriendsInfo>());
                return;
            }

            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.searchFriend,
                HttpMethod.GET,
                JsonConvert.SerializeObject(searchFriendParams),
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