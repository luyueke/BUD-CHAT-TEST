using System.Collections.Generic;
using GameData.Base;
using GameData.BaseInfo;
using GameData.UGCData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Box 场景列表数据加载器，通过 /ugc/interactList 接口（interactType=54）拉取用户已拥有的 BoxScene 列表。
/// 参考 CabinUgcAnimUgcToneInfoPanel_DataLoader 模式实现，支持分页游标拉取。
/// </summary>
public class BoxScene_DataLoader : MonoBehaviour
{
    private bool _isEnd = false;
    private string _cookie = "";
    private BudTimer _timer;
    private bool _isRequestingData = false;

    /// <summary>
    /// 重置分页 cookie，下次调用 GetBoxSceneList 将从第一页重新拉取
    /// </summary>
    public void ResetCookie()
    {
        _isEnd = false;
        _cookie = "";
    }

    /// <summary>
    /// 请求用户已拥有的 BoxScene 列表，结果通过 resultAction 回调返回。
    /// 请求进行中或已拉取完毕时直接回调空列表。
    /// </summary>
    /// <param name="resultAction">回调，参数为本次返回的 BoxSceneInfo 列表</param>
    public void GetBoxSceneList(UnityAction<List<BoxSceneInfo>> resultAction = null)
    {
        if (_isRequestingData)
        {
            resultAction?.Invoke(new List<BoxSceneInfo>());
            return;
        }

        if (_isEnd)
        {
            resultAction?.Invoke(new List<BoxSceneInfo>());
            return;
        }

        _isRequestingData = true;
        // 5 秒超时保护，防止请求卡死导致列表无法再次加载
        _timer = TimerManager.Inst.RunOnce("GetBoxSceneListRsp", 5, () => { _isRequestingData = false; });

        JObject jb = new JObject
        {
            ["cookie"] = _cookie,
            ["targetUid"] = AccountDataManager.Inst.Uid,
            ["interactType"] = (int)UGCInteractType.BoxScene,
            ["noPublish"] = 0
        };
        var reqParam = JsonConvert.SerializeObject(jb);

        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.UGCInteractList, HttpMethod.GET, reqParam,
            (content) => { OnGetSuccess(content, resultAction); },
            (error) => { OnGetFail(resultAction); });
    }

    /// <summary>
    /// 请求成功回调：反序列化响应，更新分页状态，提取 ugcInfo 列表回调给调用方
    /// </summary>
    private void OnGetSuccess(string content, UnityAction<List<BoxSceneInfo>> resultAction = null)
    {
        _isRequestingData = false;
        TimerManager.Inst.Stop(_timer);

        var rsp = JsonConvert.DeserializeObject<BoxSceneInteractListRsp>(content);

        if (rsp == null)
        {
            LoggerUtils.LogError("[BoxScene_DataLoader] 反序列化 BoxSceneInteractListRsp 失败，content=" + content);
            resultAction?.Invoke(new List<BoxSceneInfo>());
            return;
        }

        // 更新分页游标，供下次上拉加载使用
        this._isEnd = rsp.IsEnd == 1;
        this._cookie = rsp.cookie;

        if (rsp.list == null || rsp.list.Count == 0)
        {
            resultAction?.Invoke(new List<BoxSceneInfo>());
            return;
        }

        var sceneList = new List<BoxSceneInfo>();
        foreach (var item in rsp.list)
        {
            if (item.ugcInfo != null)
            {
                sceneList.Add(item.ugcInfo);
            }
        }
        resultAction?.Invoke(sceneList);
    }

    /// <summary>
    /// 请求失败回调：清理请求锁，通过回调返回空列表
    /// </summary>
    private void OnGetFail(UnityAction<List<BoxSceneInfo>> resultAction = null)
    {
        _isRequestingData = false;
        TimerManager.Inst.Stop(_timer);
        LoggerUtils.LogError("[BoxScene_DataLoader] 请求 BoxScene 列表失败");
        resultAction?.Invoke(new List<BoxSceneInfo>());
    }

    /// <summary>BoxScene 互动列表接口响应体</summary>
    public class BoxSceneInteractListRsp : HttpPageBaseData
    {
        public List<BoxSceneSubData> list = new List<BoxSceneSubData>();
    }

    /// <summary>BoxScene 互动列表单条数据</summary>
    public class BoxSceneSubData
    {
        /// <summary>BoxScene 场景元数据</summary>
        public BoxSceneInfo ugcInfo;
        /// <summary>互动状态（点赞、收藏等）</summary>
        public BaseInteractInfo interactInfo;
    }
}
