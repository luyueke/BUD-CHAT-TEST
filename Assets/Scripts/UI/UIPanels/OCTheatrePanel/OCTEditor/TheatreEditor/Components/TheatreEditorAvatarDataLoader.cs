using System.Collections.Generic;
using System.Linq;
using Game.Store;
using GameData.BaseInfo;
using GameData.PgcData;
using GameData.UGCData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 分页加载玩家已拥有的演员列表（发布 + 购买）。
/// 从本地背包读取全部已拥有的 ActorCard ID，按页切片后批量调用 ActorBatchInfo 获取完整信息。
/// 拉到的数据会写入 DataCenter.AvatarInfoCache。
/// </summary>
public class TheatreEditorAvatarDataLoader : MonoBehaviour
{
    private const int PageSize = 20;

    private List<string> _allOwnedIds = new List<string>();
    private int _loadedIndex;
    private bool _isEnd;
    private bool _isRequestingData;
    private BudTimer _timer;
    private TheatreEditorDataCenter _dataCenter;

    public bool IsEnd => _isEnd;

    public void Init(TheatreEditorDataCenter dataCenter)
    {
        _dataCenter = dataCenter;
        ResetPaging();
    }

    public void ResetPaging()
    {
        var handler = AssetsDataManager.GetData<AvatarBagSceneHandler>();
        var goods = handler?.GetGoodsData(
            UniqueType.Get(ResourceType.AvatarCard, (int)UgcTheatreSubType.AvatarCard));

        var bagIds = goods != null
            ? goods.Where(g => !string.IsNullOrEmpty(g.Id)).Select(g => g.Id)
            : Enumerable.Empty<string>();

        var theatreIds = _dataCenter?.SelectedAvatarIds ?? Enumerable.Empty<string>();

        _allOwnedIds = bagIds.Union(theatreIds)
            .Where(id => !string.IsNullOrEmpty(id))
            .ToList();

        _loadedIndex = 0;
        _isEnd = _allOwnedIds.Count == 0;
    }

    /// <summary>拉取下一页已拥有演员。</summary>
    public void GetAvatarList(UnityAction<List<OCTheatreAvatarInfo>> resultAction = null)
    {
        if (_isRequestingData)
        {
            resultAction?.Invoke(new List<OCTheatreAvatarInfo>());
            return;
        }

        if (_isEnd)
        {
            resultAction?.Invoke(new List<OCTheatreAvatarInfo>());
            return;
        }

        var batch = _allOwnedIds.Skip(_loadedIndex).Take(PageSize).ToList();
        if (batch.Count == 0)
        {
            _isEnd = true;
            resultAction?.Invoke(new List<OCTheatreAvatarInfo>());
            return;
        }

        _isRequestingData = true;
        _timer = TimerManager.Inst.RunOnce("GetTheatreEditorAvatarListRsp", 5f, () =>
        {
            _isRequestingData = false;
        });

        var idList = string.Join(",", batch);
        var jb = new JObject { ["idList"] = idList };

        NetworkManager.Inst.SendHttpRequest(
            HttpUrlDefine.ActorBatchInfo,
            HttpMethod.GET,
            JsonConvert.SerializeObject(jb),
            content => OnGetSuccess(content, batch.Count, resultAction),
            error => OnGetFail(error, resultAction)
        );
    }

    private void OnGetSuccess(string content, int batchCount,
        UnityAction<List<OCTheatreAvatarInfo>> resultAction)
    {
        _isRequestingData = false;
        TimerManager.Inst.Stop(_timer);

        _loadedIndex += batchCount;
        _isEnd = _loadedIndex >= _allOwnedIds.Count;

        var rsp = JsonConvert.DeserializeObject<BatchActorDetailRsp>(content);
        var avatarList = new List<OCTheatreAvatarInfo>();

        if (rsp?.actorList != null)
        {
            foreach (var item in rsp.actorList)
            {
                var info = item?.actorInfo;
                if (info == null) continue;
                avatarList.Add(info);
                if (_dataCenter != null && !string.IsNullOrEmpty(info.id))
                    _dataCenter.AvatarInfoCache[info.id] = info;
            }
        }

        resultAction?.Invoke(avatarList);
    }

    private void OnGetFail(string error, UnityAction<List<OCTheatreAvatarInfo>> resultAction)
    {
        _isRequestingData = false;
        TimerManager.Inst.Stop(_timer);
        resultAction?.Invoke(new List<OCTheatreAvatarInfo>());
    }
}
