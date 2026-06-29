using System.Collections.Generic;
using System.Linq;
using Game.Store;
using GameData.PgcData;
using GameData.UGCData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 分页加载玩家已拥有的剧场列表（发布 + 购买）。
/// 从本地背包读取全部已拥有的 Theatre ID（key = Theatre+AvatarCard=210001），
/// 按页切片后批量调用 TheatreBatchInfo 获取完整信息。
/// </summary>
public class TheatreTriggerTheatreDataLoader : MonoBehaviour
{
    private const int PageSize = 20;

    private List<string> _allOwnedIds = new List<string>();
    private int _loadedIndex;
    private bool _isEnd;
    private BudTimer _timer;
    private bool _isRequestingData;

    public void Init()
    {
        var handler = AssetsDataManager.GetData<AvatarBagSceneHandler>();
        var goods = handler?.GetGoodsData(
            UniqueType.Get(ResourceType.Theatre, (int)UgcTheatreSubType.AvatarCard));

        _allOwnedIds = goods != null
            ? goods.Where(g => !string.IsNullOrEmpty(g.Id)).Select(g => g.Id).ToList()
            : new List<string>();

        _loadedIndex = 0;
        _isEnd = _allOwnedIds.Count == 0;
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

        var batch = _allOwnedIds.Skip(_loadedIndex).Take(PageSize).ToList();
        if (batch.Count == 0)
        {
            _isEnd = true;
            resultAction?.Invoke(new List<DraftListItem>());
            return;
        }

        _isRequestingData = true;
        _timer = TimerManager.Inst.RunOnce("TriggerTheatreListRsp", 5f, () => { _isRequestingData = false; });

        var jb = new JObject { ["idList"] = string.Join(",", batch) };
        NetworkManager.Inst.SendHttpRequest(
            HttpUrlDefine.TheatreBatchInfo,
            HttpMethod.GET,
            JsonConvert.SerializeObject(jb),
            content => OnSuccess(content, batch.Count, resultAction),
            error => OnFail(error, resultAction));
    }

    private void OnSuccess(string content, int batchCount, UnityAction<List<DraftListItem>> resultAction)
    {
        _isRequestingData = false;
        TimerManager.Inst.Stop(_timer);

        _loadedIndex += batchCount;
        _isEnd = _loadedIndex >= _allOwnedIds.Count;

        var rsp = JsonConvert.DeserializeObject<BatchTheatreDetailRsp>(content);
        var result = new List<DraftListItem>();

        if (rsp?.theatreList != null)
        {
            foreach (var item in rsp.theatreList)
            {
                if (item?.theaterInfo == null) continue;
                result.Add(new DraftListItem
                {
                    theatreInfo  = item.theaterInfo,
                    creator      = item.creator,
                    interactInfo = item.interactInfo,
                });
            }
        }

        resultAction?.Invoke(result);
    }

    private void OnFail(string error, UnityAction<List<DraftListItem>> resultAction)
    {
        _isRequestingData = false;
        TimerManager.Inst.Stop(_timer);
        resultAction?.Invoke(new List<DraftListItem>());
    }
}
