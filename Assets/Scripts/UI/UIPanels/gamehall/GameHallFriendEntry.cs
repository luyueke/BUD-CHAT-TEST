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
    public class GameHallFriendEntry : MonoBehaviour
    {
        public GameHallFriendAdapter adapter;
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
                if (this == null || gameObject == null)
                {
                    Debug.LogError("GameHallFriendEntry 已被销毁");
                    return;
                }
                if (adapter == null)
                {
                    Debug.LogError("adapter 为空！");
                    return;
                }
                if (adapter.Data == null)
                {
                    Debug.LogError("adapter.Data 为空！");
                    return;
                }
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
                GetFirstPageFriendDatas(hasFriendAct);
            }
        }


        public void RequestFriendList(Action<List<MyFriendsInfo>> onResult)
        {
            httpRequest.relationShip = 2;
            httpRequest.relationStatus = 3;
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


        public void OnApplicationPause(bool pauseStatus)
        {
            //处理IOS端，切后台的时候，Http请求发不出去的问题
#if UNITY_IOS
			if (!pauseStatus)
			{
				TimerManager.Inst.RunOnce("FriendViewTimer", 1f, () =>
				{
					if (adapter.Data.Count <= 0)
					{
						RequestFriendList(OnReceivedNewModelsForInsert);
					}
				});
			}
#endif
        }
    }
}
