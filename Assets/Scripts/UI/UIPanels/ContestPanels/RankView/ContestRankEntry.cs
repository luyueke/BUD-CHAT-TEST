
using System;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using GameData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Events;

public class ContestRankEntry : MonoBehaviour
{
    public ContestRankAdapter adapter;
    private ContestRankRequester requester = new ContestRankRequester();

    protected void Start()
    {
        PullToRefreshBehaviour refreshController = adapter.GetComponent<PullToRefreshBehaviour>();
        refreshController.OnRefreshWithSign.AddListener(OnPullReleased);
        adapter.OnItemsUpdatedAct.AddListener(refreshController.HideGizmo);
    }

    public void ResetAdpater()
    {
        if (adapter != null && adapter.IsInitialized)
        {
            adapter.ResetItems(0);
            adapter.ClearPool();
        }
    }

    public void SetActions(String contestId, Action<ContestEntryInfo> onSelectItemAct,  Action isEmptyAction)
    {
        requester.Init(contestId);
        adapter.OnSelectItemAct = onSelectItemAct;
        adapter.EmptyDataAct = isEmptyAction;
    }

    public void GetFirstPageDatas(Action<List<ContestEntryInfo>, ContestEntryInfo> complete)
    {
        requester.ResetCookie();
        requester.GetData(true, resultAction: (b, datas) =>
        {
            var items = datas.list;
            if (items == null)
            {
                items = new List<ContestEntryInfo>();
            }

            var selfItem = datas.userRankData;
            complete?.Invoke(items, selfItem);
            ResetAdpater();
            adapter.OnItemsUpdatedAct?.Invoke();
            
            if (datas != null && items.Count > 0)
            {
                adapter.Data.ResetItems(items);
            }
        });
    }
    
    public void RemoveSingleItem(string cId)
    {
        adapter.RemoveSingleItem(cId);
    }

    public void UpdateSingleItem(ContestEntryInfo data)
    {
        adapter.UpdateSingleItem(data);
    }

    public void OnPullReleased(float sign)
    {
        if (sign < 0)
        {
            requester.GetData(false, OnReceivedNewModelsForInsert);
        }
    }

    void OnReceivedNewModelsForInsert(bool result, ContestRankListReq rsp)
    {
        var items = rsp?.list;
        if (items == null)
        {
            items = new List<ContestEntryInfo>();
        }
        
        if (items.Count == 0)
        {
            adapter.OnItemsUpdatedAct?.Invoke();
            return;
        }

        adapter.Data.InsertItems(adapter.GetItemsCount(), items);
        adapter.OnItemsUpdatedAct?.Invoke();
    }
  
}

public class ContestRankRequester
{
    private string contestId;
    private string searchKey;
    public void Init(string contestId)
    {
        this.contestId = contestId;
    }
    private bool isEnd = false;
    private string cookie = "";
    public bool isRequestingData = false;

    public void ResetCookie()
    {
        cookie = "";
        isEnd = false;
        isRequestingData = false;
    }
    
    public void GetData(bool isFirstRequest, UnityAction<bool, ContestRankListReq> resultAction = null)
    {
        if (isFirstRequest)
        {
            ResetCookie();
        }
        
        if (isRequestingData)
        {
            resultAction?.Invoke(false, null);
            return;
        }

        if (isEnd)
        {
            resultAction?.Invoke(false, null);
            return;
        }

        isRequestingData = true;
        var timer = TimerManager.Inst.RunOnce("OnceKey", 5, () => { isRequestingData = false; });
        GetRemoteDraftsOrPublishData((results, req) =>
        {
            isRequestingData = false;
            TimerManager.Inst.Stop(timer);
            resultAction?.Invoke(results, req);
        });
    }

    private void GetRemoteDraftsOrPublishData(UnityAction<bool, ContestRankListReq> resultAction = null)
    {
        var jb = new JObject()
        {
            ["cookie"] = cookie,
            ["contestId"] = contestId,
        };
        var paramStr = JsonConvert.SerializeObject(jb);
        
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ContestRankingList,
            HttpMethod.GET, 
            paramStr, 
            (content) =>
            {
                ContestRankListReq response = JsonConvert.DeserializeObject<ContestRankListReq>(content);
                this.isEnd = response.isEnd == 1;
                this.cookie = response.cookie;

                resultAction?.Invoke(true, response);
            },
            (error) =>
            {
                resultAction?.Invoke(false, null);
            });
    }
}
