using System.Collections.Generic;
using GameData;
using GameData.BaseInfo;
using GameData.UGCData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UGCAsset;
using UGCAsset.Draft;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// game studio数据管理
/// </summary>
public enum StudioSubType
{
    Drafts,
    Published,
    Template,
}

public class GameStudioDataManager : MonoBehaviour
{
    private bool isEnd = false;

    private string cookie = "";

    private string reqHead = HttpUrlDefine.createList;
    public bool isRequestingData = false;

    private StudioSubType _studioSubType = StudioSubType.Drafts;

    private MainViewType _mainViewType;

    public void SetData(StudioSubType subType, MainViewType mainViewType = MainViewType.Game)
    {
        _studioSubType = subType;
        _mainViewType = mainViewType;
        switch (mainViewType)
        {
            case MainViewType.Game:
                reqHead = subType == StudioSubType.Drafts ? HttpUrlDefine.createList : HttpUrlDefine.publishList;
                break;
            case MainViewType.Prop:
                reqHead = subType == StudioSubType.Drafts ? HttpUrlDefine.propDraftList : HttpUrlDefine.propPublishList;
                break;
            case MainViewType.Material:
                reqHead = subType == StudioSubType.Drafts
                    ? HttpUrlDefine.MaterialCreateList
                    : HttpUrlDefine.MaterialPublishList;
                break;
            case MainViewType.Cloth:
                reqHead = subType == StudioSubType.Drafts
                    ? HttpUrlDefine.clothCreateList
                    : HttpUrlDefine.clothPublishList;
                break;
        }
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
    public void GetRemoteDraftsOrPublishData(UnityAction<List<DraftListItem>> resultAction = null)
    {
        int ugcType = 0;
        if (_mainViewType == MainViewType.Cloth)
        {
            ugcType = (int)UgcType.Clothes;
        }
        var req = new MapListReq
        {
            cookie = cookie,
            uid = AccountDataManager.Inst.Uid,
            ugcType = ugcType
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
            (error) =>
            {
                resultAction?.Invoke(new List<DraftListItem>());
            });
    }
}
