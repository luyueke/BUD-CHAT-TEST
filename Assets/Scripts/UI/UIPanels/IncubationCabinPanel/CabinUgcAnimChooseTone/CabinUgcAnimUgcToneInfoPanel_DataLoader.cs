using System.Collections.Generic;
using GameData.BaseInfo;
using GameData.UGCData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Events;

public class CabinUgcAnimUgcToneInfoPanel_DataLoader : MonoBehaviour
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
        CabinToneNetManager.Inst.ResetCabinCharacterTonePublishData();
    }

    public void GetTonePublishedData(UnityAction<List<CabinToneInfo>> resultAction = null)
    {
        if (_isRequestingData)
        {
            resultAction?.Invoke(new List<CabinToneInfo>());
            return;
        }

        if (_isEnd)
        {
            resultAction?.Invoke(new List<CabinToneInfo>());
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
                (content) => { OnGetSuccessUGCInteractList(content, resultAction); }, (error) => { OnGetFail(resultAction); });
        }
        else
        {
            CabinToneNetManager.Inst.GetCabinTonePublishList((success) =>
            {
                TimerManager.Inst.Stop(_timer);
                _isRequestingData = false;
                if (success)
                {
                    _isEnd = CabinToneNetManager.Inst.GetCabinCharacterTonePublishIsEnd();
                    var subList = CabinToneNetManager.Inst.GetCabinCharacterTonePublishData();
                    if (subList == null || subList.Count == 0)
                    {
                        resultAction?.Invoke(new List<CabinToneInfo>());
                    }
                    else
                    {
                        var toneList = new List<CabinToneInfo>();
                        foreach (var item in subList)
                        {
                            if (item.characterToneInfo != null)
                            {
                                toneList.Add(item.characterToneInfo);
                            }
                        }
                        resultAction?.Invoke(toneList);
                    }
                }
                else
                {
                    resultAction?.Invoke(new List<CabinToneInfo>());
                }
            });
        }
    }

    private void OnGetSuccessUGCInteractList(string content, UnityAction<List<CabinToneInfo>> resultAction = null)
    {
        _isRequestingData = false;
        TimerManager.Inst.Stop(_timer);
        var buyedToneListRsp = JsonConvert.DeserializeObject<CabinUgcAnimBuyedToneListRsp>(content);
        this._isEnd = buyedToneListRsp.IsEnd == 1;
        this._cookie = buyedToneListRsp.cookie;

        if (buyedToneListRsp == null || buyedToneListRsp.list == null || buyedToneListRsp.list.Count == 0)
        {
            resultAction?.Invoke(new List<CabinToneInfo>());
        }
        else
        {
            var toneList = new List<CabinToneInfo>();
            foreach (var item in buyedToneListRsp.list)
            {
                if (item.ugcData != null)
                {
                    toneList.Add(item.ugcData);
                }
            }
            resultAction?.Invoke(toneList);
        }
    }

    private void OnGetFail(UnityAction<List<CabinToneInfo>> resultAction = null)
    {
        _isRequestingData = false;
        TimerManager.Inst.Stop(_timer);
        resultAction?.Invoke(new List<CabinToneInfo>());
    }

    public class CabinUgcAnimBuyedToneListRsp : HttpPageBaseData
    {
        public List<CabinCharacterToneBuyedSubData> list = new List<CabinCharacterToneBuyedSubData>();
    }
}
