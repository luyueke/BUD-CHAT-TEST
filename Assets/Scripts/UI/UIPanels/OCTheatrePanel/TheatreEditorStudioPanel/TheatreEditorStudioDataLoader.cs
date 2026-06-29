using System.Collections.Generic;
using GameData.Base;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Events;

public class TheatreEditorStudioDataLoader : MonoBehaviour
{
    protected TheatreStudioSubType _curStudioSubType;
    protected bool _isEnd = false;
    protected string _cookie = "";
    private BudTimer _timer;
    private string _reqHead = "";
    private bool _isRequestingData = false;

    private void ResetCookie()
    {
        _isEnd = false;
        _cookie = "";
    }

    public void InitData(TheatreStudioSubType type)
    {
        _curStudioSubType = type;
        _reqHead = type == TheatreStudioSubType.Drafts
            ? HttpUrlDefine.TheatreDraftList
            : HttpUrlDefine.TheatrePublishList;
        ResetCookie();
    }

    public void GetTheatreStudioList(UnityAction<List<DraftListItem>> resultAction = null)
    {
        if (_isRequestingData)
        {
            resultAction?.Invoke(new List<DraftListItem>());
            return;
        }

        if (_isEnd)
        {
            resultAction?.Invoke(new List<DraftListItem>());
            return;
        }

        _isRequestingData = true;
        _timer = TimerManager.Inst.RunOnce("GetTheatreStudioListRsp", 5, () =>
        {
            _isRequestingData = false;
        });

        JObject jb = new JObject
        {
            ["cookie"] = _cookie,
            ["uid"] = AccountDataManager.Inst.Uid,
        };
        var reqParam = JsonConvert.SerializeObject(jb);
        NetworkManager.Inst.SendHttpRequest(_reqHead, HttpMethod.GET, reqParam, (content) =>
        {
            OnGetSuccess(content, resultAction);
        }, (error) =>
        {
            OnGetFail(error, resultAction);
        });
    }

    private void OnGetSuccess(string content, UnityAction<List<DraftListItem>> resultAction = null)
    {
        _isRequestingData = false;
        TimerManager.Inst.Stop(_timer);
        MapListResponse mapListResponse = JsonConvert.DeserializeObject<MapListResponse>(content);
        this._isEnd = mapListResponse.isEnd == 1;
        this._cookie = mapListResponse.cookie;

        if (mapListResponse == null || mapListResponse.list == null || mapListResponse.list.Count == null)
        {
            resultAction?.Invoke(new List<DraftListItem>());
        }
        else
        {
            resultAction?.Invoke(mapListResponse.list);
        }
    }

    private void OnGetFail(string error, UnityAction<List<DraftListItem>> resultAction = null)
    {
        _isRequestingData = false;
        TimerManager.Inst.Stop(_timer);
        resultAction?.Invoke(new List<DraftListItem>());
    }
}
