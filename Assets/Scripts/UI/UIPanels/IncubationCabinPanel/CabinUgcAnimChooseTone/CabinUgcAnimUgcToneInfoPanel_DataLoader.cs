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
                        // 兜底：服务端可能把同一条音色返回多次，按 id 去重后再回调，防止背包出现重复
                        resultAction?.Invoke(DedupById(toneList));
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
            // 兜底：服务端可能把同一条音色返回多次，按 id 去重后再回调，防止背包出现重复
            resultAction?.Invoke(DedupById(toneList));
        }
    }

    /// <summary>
    /// 按 <see cref="CabinToneInfo.id"/> 保序去重，保留首次出现的条目。
    /// id 为空的条目不参与去重判断、原样保留（占位项由面板层另行插入，这里返回的均为真实数据）。
    /// 用于兜底服务端把同一条音色返回多次、导致背包出现完全相同重复条目的情况。
    /// </summary>
    /// <param name="source">原始音色列表</param>
    /// <returns>去重后的新列表（source 为 null 时返回空列表）</returns>
    private List<CabinToneInfo> DedupById(List<CabinToneInfo> source)
    {
        if (source == null)
            return new List<CabinToneInfo>();

        var result = new List<CabinToneInfo>(source.Count);
        var seenIds = new HashSet<string>();
        foreach (var item in source)
        {
            if (item == null)
                continue;

            // id 为空的条目不参与去重，原样保留
            if (string.IsNullOrEmpty(item.id))
            {
                result.Add(item);
                continue;
            }

            // HashSet.Add 返回 false 说明该 id 已出现过，跳过重复项
            if (seenIds.Add(item.id))
            {
                result.Add(item);
            }
            else
            {
                LoggerUtils.Log($"[CabinUgcAnimUgcToneInfoPanel_DataLoader] 发现重复音色 id={item.id}，已去重");
            }
        }

        return result;
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
