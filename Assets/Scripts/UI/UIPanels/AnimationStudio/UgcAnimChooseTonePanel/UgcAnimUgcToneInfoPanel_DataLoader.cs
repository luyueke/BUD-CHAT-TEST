using System.Collections;
using System.Collections.Generic;
using Game.MusicalInstrument;
using GameData.BaseInfo;
using GameData.UGCData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Events;

public class UgcAnimUgcToneInfoPanel_DataLoader : MonoBehaviour
{
    protected bool _isEnd = false;
    protected string _cookie = "";
    private BudTimer _timer;
    public bool IsOwnedList;
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
    public void GetTonePublishedData(UnityAction<List<AnimMusicListRspData>> resultAction = null)
    {
        if (_isRequestingData)
        {
            resultAction?.Invoke(new List<AnimMusicListRspData>());
            return;
        }

        if (_isEnd)
        {
            resultAction?.Invoke(new List<AnimMusicListRspData>());
            return;
        }

        _isRequestingData = true;
        _timer = TimerManager.Inst.RunOnce("GetPublishToneListRsp", 5, () => { _isRequestingData = false; });

        if (IsOwnedList)
        {
            JObject jb = new JObject
            {
                ["cookie"] = _cookie,
                ["targetUid"] = AccountDataManager.Inst.Uid,
                ["interactType"] = (int)UGCInteractType.CharacterTone,
                ["noPublish"] = 1
            };
            var reqParam = JsonConvert.SerializeObject(jb);

            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.UGCInteractList, HttpMethod.GET, reqParam,
                (content) => { OnGetSuccessUGCInteractList(content, resultAction); }, (error) => { OnGetFail(error, resultAction); });
        }
        else
        {
            JObject jb = new JObject
            {
                ["cookie"] = _cookie,
                ["uid"] = AccountDataManager.Inst.Uid,
            };
            var reqParam = JsonConvert.SerializeObject(jb);
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.animMusicPublishList, HttpMethod.GET, reqParam,
                (content) => { OnGetSuccess(content, resultAction); }, (error) => { OnGetFail(error, resultAction); });
        }
    }

    private void OnGetSuccess(string content, UnityAction<List<AnimMusicListRspData>> resultAction = null)
    {
        _isRequestingData = false;
        TimerManager.Inst.Stop(_timer);
        var publishToneListRsp = JsonConvert.DeserializeObject<UgcAnimPublishToneListRsp>(content);
        this._isEnd = publishToneListRsp.IsEnd == 1;
        this._cookie = publishToneListRsp.cookie;

        if (publishToneListRsp == null || publishToneListRsp.list == null || publishToneListRsp.list.Count == null)
        {
            resultAction?.Invoke(new List<AnimMusicListRspData>());
        }
        else
        {
            resultAction?.Invoke(publishToneListRsp.list);
        }
    }
    
    private void OnGetSuccessUGCInteractList(string content, UnityAction<List<AnimMusicListRspData>> resultAction = null)
    {
        _isRequestingData = false;
        TimerManager.Inst.Stop(_timer);
        var publishToneListRsp = JsonConvert.DeserializeObject<UgcAnimPublishToneListRsp>(content);
        this._isEnd = publishToneListRsp.IsEnd == 1;
        this._cookie = publishToneListRsp.cookie;

        if (publishToneListRsp == null || publishToneListRsp.list == null || publishToneListRsp.list.Count == null)
        {
            resultAction?.Invoke(new List<AnimMusicListRspData>());
        }
        else
        {
            resultAction?.Invoke(publishToneListRsp.list);
        }
    }

    private void OnGetFail(string error, UnityAction<List<AnimMusicListRspData>> resultAction = null)
    {
        _isRequestingData = false;
        TimerManager.Inst.Stop(_timer);
        resultAction?.Invoke(new List<AnimMusicListRspData>());
    }
    
    public class UgcAnimPublishToneListRsp : HttpPageBaseData
    {
        public List<AnimMusicListRspData> list = new List<AnimMusicListRspData>();
    }
}

public class AnimMusicListRspData
{
    public AnimMusicInfo animMusicInfo;
    public AnimMusicInfo ugcInfo;
}


