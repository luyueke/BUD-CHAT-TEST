using System.Collections.Generic;
using GameData.Base;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Events;

public class TheatreTriggerActorDataLoader : MonoBehaviour
{
    private bool _isEnd;
    private string _cookie = "";
    private BudTimer _timer;
    private bool _isRequestingData;

    public void Init()
    {
        _isEnd = false;
        _cookie = "";
        _isRequestingData = false;
        TimerManager.Inst.Stop(_timer);
    }

    public void GetData(UnityAction<List<DraftListItem>> resultAction = null)
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
        _timer = TimerManager.Inst.RunOnce("TriggerActorListRsp", 5, () => { _isRequestingData = false; });

        var jb = new JObject
        {
            ["cookie"] = _cookie,
            ["uid"] = AccountDataManager.Inst.Uid,
        };
        NetworkManager.Inst.SendHttpRequest(
            HttpUrlDefine.ActorPublishList,
            HttpMethod.GET,
            JsonConvert.SerializeObject(jb),
            content => OnSuccess(content, resultAction),
            error => OnFail(error, resultAction));
    }

    private void OnSuccess(string content, UnityAction<List<DraftListItem>> resultAction)
    {
        _isRequestingData = false;
        TimerManager.Inst.Stop(_timer);
        var resp = JsonConvert.DeserializeObject<MapListResponse>(content);
        _isEnd = resp != null && resp.isEnd == 1;
        _cookie = resp?.cookie ?? "";
        resultAction?.Invoke(resp?.list ?? new List<DraftListItem>());
    }

    private void OnFail(string error, UnityAction<List<DraftListItem>> resultAction)
    {
        _isRequestingData = false;
        TimerManager.Inst.Stop(_timer);
        resultAction?.Invoke(new List<DraftListItem>());
    }
}
