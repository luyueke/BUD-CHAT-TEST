using System;
using System.Collections.Generic;
using Game.MusicalInstrument;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class TimbreCommunityPublishDataLoader : MonoBehaviour
{
    protected bool _isEnd = false;
    protected string _cookie = "";
    private BudTimer _timer;
    public bool _isRequestingData = false;

    public void ResetCookie()
    {
        _isEnd = false;
        _cookie = "";
    }

    /// <summary>
    /// 获取草稿或发布数据
    /// </summary>
    /// <param name="isFirstRequest"></param>
    /// <param name="resultAction"></param>
    public void GetTonePublishedData(Action<List<ToneItemData>> resultAction = null)
    {
        if (_isRequestingData)
        {
            resultAction?.Invoke(new List<ToneItemData>());
            return;
        }

        if (_isEnd)
        {
            resultAction?.Invoke(new List<ToneItemData>());
            return;
        }

        _isRequestingData = true;
        _timer = TimerManager.Inst.RunOnce("GetPublishToneListRspKey", 5, () => { _isRequestingData = false; });

        JObject jb = new JObject
        {
            ["cookie"] = _cookie,
            ["uid"] = AccountDataManager.Inst.Uid,
        };
        var reqParam = JsonConvert.SerializeObject(jb);
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.UGCTonePublishList, HttpMethod.GET, reqParam,
            (content) => { OnGetSuccess(content, resultAction); }, (error) => { OnGetFail(error, resultAction); });
    }

    private void OnGetSuccess(string content, Action<List<ToneItemData>> resultAction = null)
    {
        _isRequestingData = false;
        TimerManager.Inst.Stop(_timer);
        var publishToneListRsp = JsonConvert.DeserializeObject<PublishToneListRsp>(content);
        this._isEnd = publishToneListRsp.IsEnd == 1;
        this._cookie = publishToneListRsp.cookie;

        if (publishToneListRsp == null || publishToneListRsp.list == null || publishToneListRsp.list.Count == null)
        {
            resultAction?.Invoke(new List<ToneItemData>());
        }
        else
        {
            resultAction?.Invoke(publishToneListRsp.list);
        }
    }

    private void OnGetFail(string error, Action<List<ToneItemData>> resultAction = null)
    {
        _isRequestingData = false;
        TimerManager.Inst.Stop(_timer);
        resultAction?.Invoke(new List<ToneItemData>());
    }
}
