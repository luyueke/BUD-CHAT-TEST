using System.Collections;
using System.Collections.Generic;
using BUD.MailBox;
using GameData.PgcData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Events;

public class VehicleStudioDataLoader : GlobalInstance<VehicleStudioDataLoader>
{
    protected StudioSubType _curStudioSubType;
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

    public void InitData(StudioSubType type)
    {
        _curStudioSubType = type;
        _reqHead = type == StudioSubType.Drafts
            ? HttpUrlDefine.VehicleCreatList
            : HttpUrlDefine.VehiclePublishList;
        ResetCookie();
    }

    /// <summary>
    /// 获取草稿或发布数据
    /// </summary>
    /// <param name="isFirstRequest"></param>
    /// <param name="resultAction"></param>
    public void GetInstrumentStudioList(UnityAction<List<DraftListItem>> resultAction = null)
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
        _timer = TimerManager.Inst.RunOnce("GetInstrumentStudioListRsp", 5, () =>
        {
            _isRequestingData = false;
        });

        JObject jb = new JObject
        {
            ["cookie"] = _cookie,
            ["uid"] = AccountDataManager.Inst.Uid,
            //["subType"] = (int)AvatarSubType.MusicalInstrument
        };
        var reqParam = JsonConvert.SerializeObject(jb);
        NetworkManager.Inst.SendHttpRequest(_reqHead, HttpMethod.GET, reqParam, (content) =>
        {
            OnGetSuccess(content, resultAction);
        }, (error) =>
        {
            OnGetSuccess(error, resultAction);
        });
    }

    private void OnGetSuccess(string content, UnityAction<List<DraftListItem>> resultAction = null)
    {
        //#region 假数据测试
        //var draftsJson = "{\r\n  \"cookie\": \"\",\r\n  \"isEnd\": 1,\r\n  \"list\": [\r\n    {\r\n      \"skinInfo\": {\r\n        \"id\": \"2zrRObTF5CSRBMnZuOT4kYvEtyy\",\r\n        \"name\": \"a a\",\r\n        \"desc\": \"\",\r\n        \"cover\": \"https://cdn.budapp.cn/UGCMusicInstrumentCover/Template/1.png?imageMogr2/thumbnail/256x256/UGCMusicInstrumentCover/Template/1.png\",\r\n        \"metaDataUrl\": \"\",\r\n        \"creator\": \"1944689833637244928\",\r\n        \"createTime\": 1752487174,\r\n        \"updateTime\": 1752487174,\r\n        \"isDelete\": 0,\r\n        \"isBan\": 0,\r\n        \"ugcClass\": 1,\r\n        \"templateId\": \"52400001\",\r\n        \"draftVersion\": 0,\r\n        \"auditInfo\": null,\r\n        \"coverAutoSaved\": 0,\r\n        \"designCode\": \"\",\r\n        \"forceUpdate\": 0,\r\n        \"sectionInfo\": null,\r\n        \"subType\": 24,\r\n        \"isProp\": true,\r\n        \"skinType\": 0\r\n      },\r\n      \"skinActionInfo\": {\r\n        \"instrumentInfo\": {\r\n          \"moveId\": \"1008\",\r\n          \"toneId\": \"pgcTone_1\",\r\n          \"isPgc\": 1,\r\n          \"toneInfo\": {\r\n            \"id\": \"pgcTone_1\",\r\n            \"name\": \"\",\r\n            \"desc\": \"\",\r\n            \"cover\": \"\",\r\n            \"metaDataUrl\": \"\",\r\n            \"creator\": \"\",\r\n            \"createTime\": 0,\r\n            \"updateTime\": 0,\r\n            \"isDelete\": 0,\r\n            \"isBan\": 0,\r\n            \"ugcClass\": 0,\r\n            \"templateId\": \"\",\r\n            \"draftVersion\": 0,\r\n            \"auditInfo\": null,\r\n            \"coverAutoSaved\": 0,\r\n            \"designCode\": \"\",\r\n            \"forceUpdate\": 0,\r\n            \"sectionInfo\": null,\r\n            \"toneType\": 0,\r\n            \"playType\": 0,\r\n            \"toneDict\": null,\r\n            \"isPgc\": 1\r\n          },\r\n          \"animDetailInfo\": {\r\n            \"pDef\": \"0.0000,0.0000,0.0000\",\r\n            \"rDef\": \"0.0000,0.0000,0.0000\",\r\n            \"sDef\": \"0.5882,0.5882,0.5882\"\r\n          }\r\n        }\r\n      }\r\n    }\r\n  ]\r\n}";

        //MapListResponse draftsData = JsonConvert.DeserializeObject<MapListResponse>(draftsJson);

        //MapListResponse publishData = new MapListResponse();

        //_isRequestingData = false;
        //TimerManager.Inst.Stop(_timer);
        //this._isEnd = draftsData.isEnd == 1;
        //this._cookie = draftsData.cookie;

        //if (draftsData == null || draftsData.list == null || draftsData.list.Count == null)
        //{
        //    resultAction?.Invoke(new List<DraftListItem>());
        //}
        //else
        //{
        //    resultAction?.Invoke(draftsData.list);
        //}
        //#endregion
        //return;
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
