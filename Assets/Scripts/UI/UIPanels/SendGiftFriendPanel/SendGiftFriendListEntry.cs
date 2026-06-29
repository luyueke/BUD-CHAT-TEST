using System;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using Game.GameHall.View;
using Game.Store;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.EventSystems;


public class SendGiftFriendListEntry : MonoBehaviour
{
    public SendGiftFriendListAdater adapter;

    private string cookie = "";
    private bool isEnd = false;

    private string searchCookie = "";
    private bool searchIsEnd = false;
    private Action<bool> hasFriendAct;
    private GoodsData _goodsData;
    private int giftType = 0;
    private int relationShip = 2;


    protected void Start()
    {
        //需要动态拉取数据必须要做的初始化操作
        PullToRefreshBehaviour refreshController = adapter.GetComponent<PullToRefreshBehaviour>();
        refreshController.OnRefreshWithSign.AddListener(OnPullReleased);
        adapter.OnItemsUpdated.AddListener(refreshController.HideGizmo);
    }


    public void GetFirstPageFriendDatas(int relationShip, Action<bool> act)
    {
        
        isEnd = false;
        cookie = "";
        hasFriendAct = act;
        this.relationShip = relationShip;

        RequestFriendList(relationShip, datas =>
        {
            adapter.OnItemsUpdated?.Invoke();
            adapter.Data.ResetItems(datas, false);
            hasFriendAct?.Invoke(adapter.Data.Count > 0);
        });
    }
    
    public void SearchGiftFriendDatas(int relationShip, string searchWord, Action<bool> act)
    {
        searchIsEnd = false;
        searchCookie = "";
        hasFriendAct = act;
        
        SearchGiftUserList(relationShip,searchWord, datas =>
        {
            adapter.OnItemsUpdated?.Invoke();
            adapter.Data.ResetItems(datas, false);
            hasFriendAct?.Invoke(adapter.Data.Count > 0);
        });
    }

    public void SetGoodsData(GoodsData goodsData, int giftType)
    {
        this._goodsData = goodsData;
        this.giftType = giftType;
        adapter.SetGoodsData(goodsData, giftType);
    }
    
    private void OnPullReleased(float sign)
    {
        if (sign < 0)
        {
            RequestFriendList(relationShip, OnReceivedNewModelsForInsert);
        }
        else if (sign > 0)
        {
            GetFirstPageFriendDatas(relationShip, hasFriendAct);
        }
    }

    private void SearchGiftUserList(int relationShip, string searchWord, Action<List<MyFriendsInfo>> onResult)
    {
        GiftUserListReq httpRequest = new GiftUserListReq
        {
            giftId = _goodsData.Id,
            giftType = this.giftType,
            relationShip = relationShip,
            cookie = searchCookie,
            opType = 1,
            searchWord = searchWord,
            seasonPassType = SeasonPassDataManager.Inst.GetSeasonPassName(giftType)
        };

        if (searchIsEnd)
        {
            onResult?.Invoke(new List<MyFriendsInfo>());
            return;
        }

        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.GiftSearchUser,
            HttpMethod.GET,
            JsonConvert.SerializeObject(httpRequest),
            onReceive: msg =>
            {
                GiftUserListResponse resourceInfo =
                    JsonConvert.DeserializeObject<GiftUserListResponse>(msg);
                searchIsEnd = resourceInfo.isEnd == 1;
                searchCookie = resourceInfo.cookie;
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


    private void RequestFriendList(int relationShip, Action<List<MyFriendsInfo>> onResult)
    {
        GiftUserListReq httpRequest = new GiftUserListReq
        {
            giftId = _goodsData.Id,
            giftType = this.giftType,
            relationShip = relationShip,
            cookie = cookie,
            opType = 1,
            seasonPassType = SeasonPassDataManager.Inst.GetSeasonPassName(giftType)
        };
        
        if (isEnd)
        {
            onResult?.Invoke(new List<MyFriendsInfo>());
            return;
        }
        
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.GiftUserList,
            HttpMethod.GET,
            JsonConvert.SerializeObject(httpRequest),
            onReceive: msg =>
            {
                GiftUserListResponse resourceInfo =
                    JsonConvert.DeserializeObject<GiftUserListResponse>(msg);
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