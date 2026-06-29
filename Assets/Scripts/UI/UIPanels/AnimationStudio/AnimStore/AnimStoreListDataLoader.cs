using System;
using System.Collections.Generic;
using Game.Store;
using GameData;
using GameData.Base;
using GameData.BaseInfo;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.Events;

/// <summary>
/// 动作商店数据加载器，支持三种模式：
///   1. LoadBySection  —— sectionInfoV2，用于「社区UGC」tab
///   2. LoadCreated    —— animPublishList / posePublishList，用于「我创作的」
///   3. LoadPurchased  —— UGCInteractList，用于「我购买的」
/// 外部把返回的 List&lt;RecommendItemData&gt; 合并后直接塞进 AnimStoreListAdapter.Data。
/// </summary>
public class AnimStoreListDataLoader
{
    // TODO: 与服务端确认 UGC 动画/姿势的 interactType 值
    // AnimMusic 购买对应 interactType=27；动画/姿势的值待确认
    private const int InteractTypeUgcAnim = 0;

    private bool _isEnd = false;
    private string _cookie = "";
    private string _sectionId = "";
    private int _currencyType = 0;

    // ──────────────────────────────────────────────
    // 1. 社区 UGC — Section 模式
    // ──────────────────────────────────────────────

    public void RefreshSection(string sectionId, int currencyType = 0)
    {
        _sectionId = sectionId;
        _currencyType = currencyType;
        _cookie = "";
        _isEnd = false;
    }

    public void LoadBySection(UnityAction<List<RecommendItemData>> onComplete)
    {
        if (_isEnd)
        {
            onComplete?.Invoke(new List<RecommendItemData>());
            return;
        }

        var jb = new JObject
        {
            ["cookie"] = _cookie,
            ["sectionId"] = _sectionId,
            ["currencyType"] = _currencyType
        };
        NetworkManager.Inst.SendHttpRequest(
            HttpUrlDefine.sectionInfoV2,
            HttpMethod.GET,
            JsonConvert.SerializeObject(jb),
            content =>
            {
                var rsp = JsonConvert.DeserializeObject<UgcAvatarPageUseData>(content);
                _isEnd = rsp?.isEnd == 1;
                _cookie = rsp?.cookie ?? "";
                onComplete?.Invoke(rsp?.list ?? new List<RecommendItemData>());
            },
            error =>
            {
                LoggerUtils.LogError("AnimStoreListDataLoader.LoadBySection error: " + error);
                onComplete?.Invoke(new List<RecommendItemData>());
            });
    }

    // ──────────────────────────────────────────────
    // 2. 我创作的 UGC — animPublishList / posePublishList
    // ──────────────────────────────────────────────

    private bool _createdIsEnd = false;
    private string _createdCookie = "";

    public void ResetCreated()
    {
        _createdIsEnd = false;
        _createdCookie = "";
    }

    /// <param name="animType">0=全部, 1=单人, 3=双人（对应 UgcAnimSubType/UgcPoseSubType）</param>
    /// <param name="isAnim">true=加载 Anim（动画），false=加载 Pose（姿势）</param>
    public void LoadCreated(int animType, bool isAnim, UnityAction<List<RecommendItemData>> onComplete)
    {
        if (_createdIsEnd)
        {
            onComplete?.Invoke(new List<RecommendItemData>());
            return;
        }

        var url = isAnim ? HttpUrlDefine.animPublishList : HttpUrlDefine.posePublishList;
        var jb = new JObject
        {
            ["cookie"] = _createdCookie,
            ["uid"] = AccountDataManager.Inst.Uid,
            ["animType"] = animType,
            ["poseType"] = animType,
            ["skinType"] = 0
        };

        NetworkManager.Inst.SendHttpRequest(
            url,
            HttpMethod.GET,
            JsonConvert.SerializeObject(jb),
            content =>
            {
                var rsp = JsonConvert.DeserializeObject<MapListResponse>(content);
                _createdIsEnd = rsp?.isEnd == 1;
                _createdCookie = rsp?.cookie ?? "";
                onComplete?.Invoke(ConvertDraftItems(rsp?.list, isAnim));
            },
            error =>
            {
                LoggerUtils.LogError("AnimStoreListDataLoader.LoadCreated error: " + error);
                onComplete?.Invoke(new List<RecommendItemData>());
            });
    }

    // ──────────────────────────────────────────────
    // 3. 我购买的 UGC — UGCInteractList
    // ──────────────────────────────────────────────

    private bool _purchasedIsEnd = false;
    private string _purchasedCookie = "";

    public void ResetPurchased()
    {
        _purchasedIsEnd = false;
        _purchasedCookie = "";
    }

    /// <param name="skinType">0=人物动画，1=宠物动画</param>
    public void LoadPurchased(int skinType, UnityAction<List<RecommendItemData>> onComplete)
    {
        if (_purchasedIsEnd)
        {
            onComplete?.Invoke(new List<RecommendItemData>());
            return;
        }

        var jb = new JObject
        {
            ["cookie"] = _purchasedCookie,
            ["targetUid"] = AccountDataManager.Inst.Uid,
            ["interactType"] = InteractTypeUgcAnim, // TODO: 确认值
            ["noPublish"] = 1,
            ["skinType"] = skinType
        };

        NetworkManager.Inst.SendHttpRequest(
            HttpUrlDefine.UGCInteractList,
            HttpMethod.GET,
            JsonConvert.SerializeObject(jb),
            content =>
            {
                var rsp = JsonConvert.DeserializeObject<UgcAvatarPageUseData>(content);
                _purchasedIsEnd = rsp?.isEnd == 1;
                _purchasedCookie = rsp?.cookie ?? "";
                onComplete?.Invoke(rsp?.list ?? new List<RecommendItemData>());
            },
            error =>
            {
                LoggerUtils.LogError("AnimStoreListDataLoader.LoadPurchased error: " + error);
                onComplete?.Invoke(new List<RecommendItemData>());
            });
    }

    // ──────────────────────────────────────────────
    // 转换：DraftListItem → RecommendItemData
    // ──────────────────────────────────────────────

    private static List<RecommendItemData> ConvertDraftItems(List<DraftListItem> items, bool isAnim)
    {
        var result = new List<RecommendItemData>();
        if (items == null) return result;
        foreach (var item in items)
        {
            var data = ConvertDraftItem(item, isAnim);
            if (data != null) result.Add(data);
        }
        return result;
    }

    private static RecommendItemData ConvertDraftItem(DraftListItem item, bool isAnim)
    {
        if (isAnim)
        {
            var info = item.animInfo;
            if (info == null) return null;
            return new RecommendItemData
            {
                ugcId = info.id,
                ugcType = UgcType.Anim,
                ugcData = JsonConvert.SerializeObject(info),
                interactInfo = item.interactInfo ?? new BaseInteractInfo { consumed = 1 },
                creatorInfo = item.creator
            };
        }
        else
        {
            var info = item.poseInfo;
            if (info == null) return null;
            return new RecommendItemData
            {
                ugcId = info.id,
                ugcType = UgcType.Pose,
                ugcData = JsonConvert.SerializeObject(info),
                interactInfo = item.interactInfo ?? new BaseInteractInfo { consumed = 1 },
                creatorInfo = item.creator
            };
        }
    }
}
