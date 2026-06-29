using System.Collections.Generic;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UGCAsset;
using UGCAsset.Draft;
using UnityEngine;
using UnityEngine.Events;
using GameData;
using Newtonsoft.Json.Linq;

public class ContestRequester : MonoBehaviour
{
    private ContestPageType _pageType;
    private string contestId;
    private string searchKey;

    private bool isSearchMode = false;

    private string itemBgColorValue = "";
    private string itemThemeColorValue = "";

    public bool isInSearch
    {
        get
        {
            return isSearchMode;
        }
    }
    
    public void Init(ContestPageType pageType, string cId)
    {
        isRequestingData = false;
        isSearchMode = false;
        _pageType = pageType;
        contestId = cId;

       var list = ContestDataManager.Inst.GetContestInfo(cId)?.themeColorList;
       if (list != null && list.Count > 1)
       {
           itemBgColorValue = list[0];
           itemThemeColorValue = list[1];
       }
    }

    public void EnterSearch(string searchKey)
    {
        isRequestingData = false;
        isSearchMode = true;
        this.searchKey = searchKey;
    }

    public void LeaveSearch()
    {
        isRequestingData = false;
        isSearchMode = false;
        this.searchKey = null;
    }
    
    private string reqPath
    {
        get
        {
            return isSearchMode ? HttpUrlDefine.ContestSearch : HttpUrlDefine.ContestEntryList;
        }
    }
    
    private bool isEnd = false;
    private string cookie = "";
    public bool isRequestingData = false;

    public void ResetCookie()
    {
        cookie = "";
        isEnd = false;
    }
    
    public void GetData(bool isFirstRequest, UnityAction<List<ContestEntryInfo>> resultAction = null)
    {
        if (isFirstRequest)
        {
            ResetCookie();
        }
        
        if (isRequestingData)
        {
            resultAction?.Invoke(new List<ContestEntryInfo>());
            return;
        }

        if (isEnd)
        {
            resultAction?.Invoke(new List<ContestEntryInfo>());
            return;
        }

        isRequestingData = true;
        var timer = TimerManager.Inst.RunOnce("OnceKey", 5, () => { isRequestingData = false; });
        GetRemoteDraftsOrPublishData((results) =>
        {
            List<ContestEntryInfo> allDraftsList = new List<ContestEntryInfo>();
            allDraftsList.AddRange(results);
            isRequestingData = false;
            TimerManager.Inst.Stop(timer);
            resultAction?.Invoke(allDraftsList);
        });
    }


    /// <summary>
    /// 获取远端草稿or已发布数据 地图 素材 材质
    /// </summary>
    /// <param name="resultAction"></param>
    private void GetRemoteDraftsOrPublishData(UnityAction<List<ContestEntryInfo>> resultAction = null)
    {
        var paramStr = "";
        if (isSearchMode)
        {
            var jb = new JObject()
            {
                ["searchWord"] = searchKey,
                ["contestId"] = contestId,
                ["cookie"] = cookie
            };
            paramStr = JsonConvert.SerializeObject(jb);
        }
        else
        {
            var jb = new JObject()
            {
                ["listType"] = (int)_pageType,
                ["contestId"] = contestId,
                ["cookie"] = cookie
                
            };
            paramStr = JsonConvert.SerializeObject(jb);
        }
        
        NetworkManager.Inst.SendHttpRequest(reqPath, HttpMethod.GET, paramStr, (content) =>
            {
                ContestEntryListReq response = JsonConvert.DeserializeObject<ContestEntryListReq>(content);
                this.isEnd = response.isEnd == 1;
                this.cookie = response.cookie;

                var fixedItems = response.list ?? new List<ContestEntryInfo>();
                if (fixedItems.Count > 0)
                {
                    foreach (var contestEntryInfo in fixedItems)
                    {
                        contestEntryInfo.creationInfo.bgColor = itemBgColorValue;
                        contestEntryInfo.creationInfo.themeColor = itemThemeColorValue;
                    }
                }

                resultAction?.Invoke(fixedItems);
            },
            (error) =>
            {
                resultAction?.Invoke(new List<ContestEntryInfo>());
            });
    }
}
