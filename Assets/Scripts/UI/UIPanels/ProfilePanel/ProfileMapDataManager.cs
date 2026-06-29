using System.Collections.Generic;
using GameData.UGCData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Events;

public enum Category
{
    Map = 0,
    Prop = 1,
    Material = 2,
    Avatar = 3
}

public enum SubCategory
{
    Published = 0,
    Liked = 1,
    Collected = 2,
    Played = 3,
    Owned = 4
}

public class ProfileMapDataManager : MonoBehaviour
{
    private bool isEnd = false;

    private string cookie = "";

    private string reqUrl = HttpUrlDefine.publishList;
    public bool isRequestingData = false;

    [SerializeField]private Category _category;
    [SerializeField]private SubCategory _subCategory;

    private string _currentUid;

    /// <summary>
    /// 配置请求链接
    /// </summary>
    public void SetData(Category category, SubCategory subCategory)
    {
        _category = category;
        _subCategory = subCategory;

        if (category == Category.Map)
        {
            switch (subCategory)
            {
                case SubCategory.Published:
                    reqUrl = HttpUrlDefine.publishList;
                    break;
                case SubCategory.Liked:
                    reqUrl = HttpUrlDefine.UGCInteractList;
                    break;
                case SubCategory.Collected:
                    reqUrl = HttpUrlDefine.UGCInteractList;
                    break;
                case SubCategory.Played:
                    reqUrl = HttpUrlDefine.UGCInteractList;
                    break;
            }
        }
        else if (category == Category.Prop)
        {
            switch (subCategory)
            {
                case SubCategory.Published:
                    reqUrl = HttpUrlDefine.propPublishList;
                    break;
                case SubCategory.Liked:
                    reqUrl = HttpUrlDefine.UGCInteractList;
                    break;
                case SubCategory.Owned:
                    reqUrl = HttpUrlDefine.UGCInteractList;
                    break;
            }
        }
        else if (category == Category.Material)
        {
            switch (subCategory)
            {
                case SubCategory.Published:
                    reqUrl = HttpUrlDefine.MaterialPublishList;
                    break;
                case SubCategory.Liked:
                    reqUrl = HttpUrlDefine.UGCInteractList;
                    break;
                case SubCategory.Owned:
                    reqUrl = HttpUrlDefine.UGCInteractList;
                    break;
            }
        }
        else if (category == Category.Avatar)
        {
            switch (subCategory)
            {
                case SubCategory.Published:
                    reqUrl = HttpUrlDefine.clothPublishList;
                    break;
                case SubCategory.Liked:
                    reqUrl = HttpUrlDefine.UGCInteractList;
                    break;
                case SubCategory.Owned:
                    reqUrl = HttpUrlDefine.UGCInteractList;
                    break;
            }
        }
    }

    public void ResetCookie()
    {
        cookie = "";
        isEnd = false;
    }

    public void SetCookie(string ck)
    {
        cookie = ck;
    }

    public void SetIsEnd(int intEnd)
    {
        isEnd = intEnd == 1;
    }

    public void GetProfileGameList(bool isFirstRequest, string uid, UnityAction<List<ResInfo>, bool> resultAction = null)
    {
        _currentUid = uid;
        if (isRequestingData)
        {
            resultAction?.Invoke(new List<ResInfo>(), false);
            return;
        }

        if (isEnd)
        {
            resultAction?.Invoke(new List<ResInfo>(), false);
            return;
        }

        isRequestingData = true;
        var timer = TimerManager.Inst.RunOnce("GetGameStudioDraftsData", 5, () => { isRequestingData = false; });
        GetRemoteDraftsOrPublishData(uid, (results, isPrivate) =>
        {
            List<ResInfo> allDraftsList = new List<ResInfo>();
            allDraftsList.AddRange(results);
            isRequestingData = false;
            TimerManager.Inst.Stop(timer);
            resultAction?.Invoke(allDraftsList, isPrivate);
        });
    }

    public void GetRemoteDraftsOrPublishData(string uid, UnityAction<List<ResInfo>, bool> resultAction = null)
    {
        if (_subCategory == SubCategory.Published)
        {
            var req = new MapListReq
            {
                cookie = cookie,
                uid = uid,
                ugcType = 2
            };
            NetworkManager.Inst.SendHttpRequest(reqUrl, HttpMethod.GET,
                JsonConvert.SerializeObject(req), (content) =>
                {
                    MapListResponse mapListResponse = JsonConvert.DeserializeObject<MapListResponse>(content);
                    this.isEnd = mapListResponse.isEnd == 1;
                    this.cookie = mapListResponse.cookie;

                    if (mapListResponse.list == null)
                    {
                        mapListResponse.list = new List<DraftListItem>();
                    }

                    List<ResInfo> resInfos = ProfileMapUtils.GetResInfos(mapListResponse.list);
                    resultAction?.Invoke(resInfos, false);
                },
                (error) => { resultAction?.Invoke(new List<ResInfo>(), false); });
        }
        else if (_subCategory == SubCategory.Liked || _subCategory == SubCategory.Collected ||
                 _subCategory == SubCategory.Played || _subCategory == SubCategory.Owned)
        {
            UGCInteractType ugcInteractType = UGCInteractType.None;
            switch (_subCategory)
            {
                case SubCategory.Liked:
                    if (_category == Category.Map)
                    {
                        ugcInteractType = UGCInteractType.LikeMap;
                    }
                    else if (_category == Category.Prop)
                    {
                        ugcInteractType = UGCInteractType.LikeUGCItem;
                    }
                    else if (_category == Category.Material)
                    {
                        ugcInteractType = UGCInteractType.LikeMaterial;
                    }
                    else if (_category == Category.Avatar)
                    {
                        ugcInteractType = UGCInteractType.LikeSkin;
                    }

                    break;
                case SubCategory.Collected:
                    if (_category == Category.Map)
                    {
                        ugcInteractType = UGCInteractType.CollectMap;
                    }

                    break;
                case SubCategory.Played:
                    if (_category == Category.Map)
                    {
                        ugcInteractType = UGCInteractType.ExperienceMap;
                    }

                    break;
                case SubCategory.Owned:
                    if (_category == Category.Prop)
                    {
                        ugcInteractType = UGCInteractType.BuyUGCItem;
                    }
                    else if (_category == Category.Material)
                    {
                        ugcInteractType = UGCInteractType.BuyMaterial;
                    }
                    else if (_category == Category.Avatar)
                    {
                        ugcInteractType = UGCInteractType.BuySkin;
                    }

                    break;
            }

            var jb = new JObject
            {
                ["interactType"] = (int)ugcInteractType,
                ["cookie"] = cookie,
                ["targetUid"] = uid
            };

            NetworkManager.Inst.SendHttpRequest(reqUrl, HttpMethod.GET,
                JsonConvert.SerializeObject(jb), (content) =>
                {
                    ResInfoList serverData = JsonConvert.DeserializeObject<ResInfoList>(content);
                    this.isEnd = serverData.isEnd == 1;
                    this.cookie = serverData.cookie;
                    bool isPrivate = serverData.permissionType == 1;
                    if (serverData.list == null)
                    {
                        serverData.list = new List<ResInfo>();
                    }

                    resultAction?.Invoke(serverData.list, isPrivate);
                },
                (error) => { resultAction?.Invoke(new List<ResInfo>(), false); });
        }
    }
}