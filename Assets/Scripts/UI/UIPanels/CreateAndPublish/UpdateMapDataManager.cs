using System.Collections.Generic;
using GameData.BaseInfo;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UGCAsset;
using UGCAsset.Draft;
using UnityEngine.Events;


public class UpdateMapDataManager : GlobalInstance<UpdateMapDataManager>
{
    private bool isEnd = false;

    private string cookie = "";

    private string reqHead = HttpUrlDefine.createList;
    public bool isRequestingData = false;

    private StudioSubType _studioSubType = StudioSubType.Drafts;

    public void SetSubType(StudioSubType subType)
    {
        _studioSubType = subType;
        reqHead = subType == StudioSubType.Drafts ? HttpUrlDefine.createList : HttpUrlDefine.publishList;
    }

    public void ResetCookie()
    {
        cookie = "";
        isEnd = false;
    }


    /// <summary>
    /// 获取草稿或发布数据
    /// </summary>
    /// <param name="isFirstRequest"></param>
    /// <param name="resultAction"></param>
    public void GetGameStudioDraftsData(bool isFirstRequest, UnityAction<List<DraftListItem>> resultAction = null)
    {
        if (isRequestingData)
        {
            resultAction?.Invoke(new List<DraftListItem>());
            return;
        }

        if (isEnd)
        {
            resultAction?.Invoke(new List<DraftListItem>());
            return;
        }

        isRequestingData = true;
        var timer = TimerManager.Inst.RunOnce("GetGameStudioDraftsData", 5, () => { isRequestingData = false; });
        GetRemoteDraftsOrPublishData((results) =>
        {
            List<DraftListItem> allDraftsList = new List<DraftListItem>();
            if (isFirstRequest && _studioSubType == StudioSubType.Drafts)
            {
                //先加草稿数据再加入远端数据
                List<DraftListItem> localDraftsList = GetLocalDraftsData();
                if (localDraftsList != null && localDraftsList.Count > 0)
                {
                    foreach (var tmpDraft in localDraftsList)
                    {
                        var resultDraftItem = results.Find((item) => item.mapInfo.id == tmpDraft.mapInfo.id);
                        // 服务器无草稿或 服务器草稿版本低于本地草稿版本，使用本地草稿
                        if (resultDraftItem == null || resultDraftItem.mapInfo.draftVersion < tmpDraft.mapInfo.draftVersion)
                        {
                            MapAssetManager.Inst.UploadDraftInfo(tmpDraft.mapInfo.id);
                            results.Remove(resultDraftItem);
                            allDraftsList.Add(tmpDraft);
                        }
                        else
                        {
                            //服务器草稿高于本地草稿，删除本地草稿
                            MapAssetManager.Inst.DeleteDraftInfo(tmpDraft.mapInfo.id);
                        }
                    }
                }

                allDraftsList.AddRange(results);
            }
            else
            {
                allDraftsList.AddRange(results);
            }
            isRequestingData = false;
            TimerManager.Inst.Stop(timer);
            resultAction?.Invoke(allDraftsList);
        });
    }

    /// <summary>
    /// 获取本地草稿数据
    /// </summary>
    private List<DraftListItem> GetLocalDraftsData()
    {
        MapDraftInfo[] mapDraftInfos = MapAssetManager.Inst.GetDraftInfos();
        List<DraftListItem> localDraftsList = new List<DraftListItem>();
        foreach (var mapDraftInfo in mapDraftInfos)
        {
            DraftListItem draftListItem = new DraftListItem();
            MapInfo mapInfo = mapDraftInfo.baseInfo;
            mapInfo.draftVersion = mapDraftInfo.draftVersion;
            mapInfo.metaDataUrl = mapDraftInfo.GetMetadataUrl();
            mapInfo.cover = mapDraftInfo.GetCoverUrl();
            mapInfo.updateTime = mapDraftInfo.updateTime;
            mapInfo.isLocal = true;
            draftListItem.mapInfo = mapInfo;
            localDraftsList.Add(draftListItem);
        }
        return localDraftsList;
    }


    /// <summary>
    /// 获取远端草稿or已发布数据
    /// </summary>
    /// <param name="resultAction"></param>
    public void GetRemoteDraftsOrPublishData(UnityAction<List<DraftListItem>> resultAction = null)
    {

        var req = new MapListReq
        {
            cookie = cookie,
            uid = AccountDataManager.Inst.Uid
        };

        NetworkManager.Inst.SendHttpRequest(reqHead, HttpMethod.GET, JsonConvert.SerializeObject(req), (content) =>
            {
                MapListResponse mapListResponse = JsonConvert.DeserializeObject<MapListResponse>(content);
                this.isEnd = mapListResponse.isEnd == 1;
                this.cookie = mapListResponse.cookie;

                if (mapListResponse.list == null)
                {
                    mapListResponse.list = new List<DraftListItem>();
                }

                resultAction?.Invoke(mapListResponse.list);
            },
            (error) => { resultAction?.Invoke(new List<DraftListItem>()); });

    }


}
