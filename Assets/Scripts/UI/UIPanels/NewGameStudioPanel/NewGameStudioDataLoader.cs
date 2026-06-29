using System.Collections;
using System.Collections.Generic;
using GameData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Events;

public class NewGameStudioDataLoader : MonoBehaviour
{
    private bool _isEnd;
    private string _cookie = "";
    private string _reqHead = HttpUrlDefine.createList;
    private StudioSubType _studioSubType = StudioSubType.Drafts;
    private UgcType _dataType;
    private UnityAction<List<DraftListItem>> _resultAction = null;

    public void InitData(StudioSubType subType, UgcType dataType)
    {
        ResetCookie();
        _studioSubType = subType;
        _dataType = dataType;
        
        switch (dataType)
        {
            case UgcType.Map:
                _reqHead = _studioSubType == StudioSubType.Drafts ? HttpUrlDefine.createList : HttpUrlDefine.publishList;
                break;
        }
    }
    
    public void ResetCookie()
    {
        _cookie = "";
        _isEnd = false;
    }

    public void SetCallBack(UnityAction<List<DraftListItem>> cb)
    {
        _resultAction = cb;
    }

    public void GetDatas()
    {
        if (_isEnd)
        {
            InvokeNullData();
            return;
        }
        
        var req = new JObject()
        {
            ["cookie"] = _cookie,
            ["uid"] = AccountDataManager.Inst.Uid,
            ["ugcType"] = (int)_dataType
        };
        
        NetworkManager.Inst.SendHttpRequest(_reqHead, HttpMethod.GET, JsonConvert.SerializeObject(req), (content) =>
            {
                if (this.gameObject == null)
                {
                    InvokeNullData();
                    return;
                }
                
                MapListResponse mapListResponse = JsonConvert.DeserializeObject<MapListResponse>(content);
                this._isEnd = mapListResponse.isEnd == 1;
                this._cookie = mapListResponse.cookie;

                if (mapListResponse.list == null)
                {
                    mapListResponse.list = new List<DraftListItem>();
                    LoggerUtils.LogError(_reqHead + " Rsp mapListResponse.list is Null");
                }
                _resultAction?.Invoke(mapListResponse.list);
            },
            InvokeNullData);
    }

    private void InvokeNullData(string content = "")
    {
        _resultAction?.Invoke(new List<DraftListItem>());
    }
}
