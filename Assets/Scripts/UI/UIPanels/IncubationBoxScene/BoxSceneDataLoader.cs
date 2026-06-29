using System.Collections.Generic;
using GameData.Base;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Game.IncubationBoxScene
{
    /// <summary>
    /// Box 场景工作室数据加载器，封装分页拉取逻辑。
    /// 对应 AnimationStudioDataLoader，通过 cookie 游标实现下翻分页。
    /// 挂载在 BoxSceneStudioInfoPanel 的同一或子 GameObject 上，由 Inspector 绑定。
    /// </summary>
    public class BoxSceneDataLoader : MonoBehaviour
    {
        /// <summary>当前加载的列表类型（草稿 / 已发布），由外部赋值</summary>
        public StudioSubType StudioType;

        // ──────────────────────────────────────────────
        // 分页状态
        // ──────────────────────────────────────────────

        /// <summary>是否已全部加载完毕（无更多分页）</summary>
        private bool _isEnd = false;

        /// <summary>分页游标，每次请求成功后由服务端返回</summary>
        private string _cookie = "";

        /// <summary>是否正在请求中（防止并发重复请求）</summary>
        private bool _isRequestingData = false;

        /// <summary>超时保护计时器，防止请求卡死</summary>
        private BudTimer _timer;

        // ──────────────────────────────────────────────
        // 公共方法
        // ──────────────────────────────────────────────

        /// <summary>
        /// 初始化数据加载状态，重置分页游标。
        /// 每次切换 Tab 或重新进入列表时调用。
        /// </summary>
        /// <param name="type">列表类型（草稿 / 已发布）</param>
        public void InitData(StudioSubType type)
        {
            StudioType = type;
            ResetCookie();
        }

        /// <summary>
        /// 获取草稿列表的下一页数据。
        /// 若正在请求或已到末页，直接回调空列表。
        /// </summary>
        /// <param name="callback">回调，参数为本次获取到的草稿数据列表</param>
        public void GetDraftList(UnityAction<List<CharacterBoxInfo>> callback = null)
        {
            if (_isRequestingData)
            {
                callback?.Invoke(new List<CharacterBoxInfo>());
                return;
            }

            if (_isEnd)
            {
                callback?.Invoke(new List<CharacterBoxInfo>());
                return;
            }

            _isRequestingData = true;

            // 5 秒超时保护，防止请求卡死
            _timer = TimerManager.Inst.RunOnce("BoxSceneGetDraftListTimeout", 5, () =>
            {
                _isRequestingData = false;
            });

            NetworkManager.Inst.SendHttpRequest(
                HttpUrlDefine.CharacterBoxDraftList,
                HttpMethod.GET,
                BuildPagedRequest(),
                content => OnDraftGetSuccess(content, callback),
                error => OnGetFail(error, callback));
        }

        /// <summary>
        /// 获取已发布列表的下一页数据。
        /// 若正在请求或已到末页，直接回调空列表。
        /// </summary>
        /// <param name="callback">回调，参数为本次获取到的已发布数据列表</param>
        public void GetPublishedList(UnityAction<List<CharacterBoxPublishItem>> callback = null)
        {
            if (_isRequestingData)
            {
                callback?.Invoke(new List<CharacterBoxPublishItem>());
                return;
            }

            if (_isEnd)
            {
                callback?.Invoke(new List<CharacterBoxPublishItem>());
                return;
            }

            _isRequestingData = true;

            // 5 秒超时保护
            _timer = TimerManager.Inst.RunOnce("BoxSceneGetPublishedListTimeout", 5, () =>
            {
                _isRequestingData = false;
            });

            NetworkManager.Inst.SendHttpRequest(
                HttpUrlDefine.CharacterBoxPublishList,
                HttpMethod.GET,
                BuildPagedRequest(),
                content => OnPublishedGetSuccess(content, callback),
                error => OnGetFail(error, callback));
        }

        // ──────────────────────────────────────────────
        // 私有方法
        // ──────────────────────────────────────────────

        /// <summary>
        /// 构建携带分页游标的请求参数。
        /// </summary>
        /// <returns>JSON 序列化后的请求参数字符串</returns>
        private string BuildPagedRequest()
        {
            var req = new JObject
            {
                ["uid"] = AccountDataManager.Inst.Uid,
                ["cookie"] = _cookie,
            };
            return JsonConvert.SerializeObject(req);
        }

        /// <summary>
        /// 重置分页游标，下次请求将从第一页开始。
        /// </summary>
        private void ResetCookie()
        {
            _isEnd = false;
            _cookie = "";
        }

        /// <summary>
        /// 草稿列表请求成功回调，解析响应并更新分页状态。
        /// </summary>
        private void OnDraftGetSuccess(string content, UnityAction<List<CharacterBoxInfo>> callback)
        {
            _isRequestingData = false;
            TimerManager.Inst.Stop(_timer);

            var rsp = JsonConvert.DeserializeObject<CharacterBoxDraftListData>(content);

            if (rsp == null || rsp.list == null)
            {
                callback?.Invoke(new List<CharacterBoxInfo>());
                return;
            }

            // 更新分页游标
            _isEnd = rsp.isEnd == 1;
            _cookie = rsp.cookie ?? "";

            // 服务端每项为 {"characterBoxInfo":{...}} 结构，取出内层 CharacterBoxInfo
            var infos = new List<CharacterBoxInfo>(rsp.list.Count);
            foreach (var item in rsp.list)
            {
                if (item?.characterBoxInfo != null)
                {
                    infos.Add(item.characterBoxInfo);
                }
            }

            callback?.Invoke(infos);
        }

        /// <summary>
        /// 已发布列表请求成功回调，解析响应并更新分页状态。
        /// </summary>
        private void OnPublishedGetSuccess(string content, UnityAction<List<CharacterBoxPublishItem>> callback)
        {
            _isRequestingData = false;
            TimerManager.Inst.Stop(_timer);

            var rsp = JsonConvert.DeserializeObject<CharacterBoxPublishListData>(content);

            if (rsp == null || rsp.list == null)
            {
                callback?.Invoke(new List<CharacterBoxPublishItem>());
                return;
            }

            // 更新分页游标
            _isEnd = rsp.isEnd == 1;
            _cookie = rsp.cookie ?? "";

            callback?.Invoke(rsp.list);
        }

        /// <summary>
        /// 请求失败时的统一处理，解锁请求状态并回调空列表。
        /// </summary>
        private void OnGetFail<T>(string error, UnityAction<List<T>> callback)
        {
            _isRequestingData = false;
            TimerManager.Inst.Stop(_timer);
            LoggerUtils.LogError($"Box 场景列表请求失败: {error}");
            callback?.Invoke(new List<T>());
        }
    }
}
