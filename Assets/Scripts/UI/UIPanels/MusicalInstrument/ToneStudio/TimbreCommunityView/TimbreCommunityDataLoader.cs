using System;
using System.Collections.Generic;
using GameData.Base;
using GameData.BaseInfo;
using GameData.UGCData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class TimbreCommunityDataLoader : MonoBehaviour
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
    public void GetToneData(Action<List<TimbrePurchasedInfo>> resultAction = null)
    {
        if (_isRequestingData)
        {
            resultAction?.Invoke(new List<TimbrePurchasedInfo>());
            return;
        }

        if (_isEnd)
        {
            resultAction?.Invoke(new List<TimbrePurchasedInfo>());
            return;
        }

        _isRequestingData = true;
        _timer = TimerManager.Inst.RunOnce("GetToneListRsp", 5, () => { _isRequestingData = false; });

        JObject jb = new JObject
        {
            ["cookie"] = _cookie,
            ["targetUid"] = AccountDataManager.Inst.Uid,
            ["interactType"] = 14
        };
        var reqParam = JsonConvert.SerializeObject(jb);

        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.UGCInteractList, HttpMethod.GET, reqParam,
            (content) => { OnGetSuccess(content, resultAction); }, (error) => { OnGetFail(error, resultAction); });
    }

    private void OnGetSuccess(string content, Action<List<TimbrePurchasedInfo>> resultAction = null)
    {
        _isRequestingData = false;
        TimerManager.Inst.Stop(_timer);
        var publishToneListRsp = JsonConvert.DeserializeObject<TimbrePurchasedInfoList>(content);
        this._isEnd = publishToneListRsp.isEnd == 1;
        this._cookie = publishToneListRsp.cookie;

        var items = publishToneListRsp?.list;
        if (items == null || items.Count == 0)
        {
            resultAction?.Invoke(new List<TimbrePurchasedInfo>());
        }
        else
        {
            resultAction?.Invoke(items);
        }
    }

    private void OnGetFail(string error, Action<List<TimbrePurchasedInfo>> resultAction = null)
    {
        _isRequestingData = false;
        TimerManager.Inst.Stop(_timer);
        resultAction?.Invoke(new List<TimbrePurchasedInfo>());
    }

    public class TimbrePurchasedInfoList
    {
        public string cookie;
        public int isEnd;
        public List<TimbrePurchasedInfo> list;
        public int permissionType;
    }

    public class TimbrePurchasedInfo
    {
        public ToneInfo ugcInfo;
        public BaseCreator creator;
        public int isPgc;
        public PGCInfo pgcInfo;
        public BaseInteractInfo interactInfo;
    }
}