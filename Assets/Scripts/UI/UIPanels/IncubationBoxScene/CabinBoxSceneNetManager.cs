using GameData.UGCData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

/// <summary>
/// AI 伙伴盒子网络管理器。
/// 封装与服务端通信的6个 HTTP 接口，包括草稿列表、发布列表、设置、详情、批量详情和搜索。
/// 作为全局单例使用，通过 CabinBoxSceneNetManager.Inst 访问。
/// </summary>
public class CabinBoxSceneNetManager : GlobalInstance<CabinBoxSceneNetManager>
{
    public const string modelPath = "Assets/Loadable/Avatar/UGCRolePart/ModelPrefab/UGCBoxScene/qiye_ugcBreedingFarm_1_3d.prefab";
    public const string meshPath = "Assets/Loadable/Avatar/UGCRolePart/ModelPrefab/UGCBoxScene/qiye_ugcBreedingFarm_1_mesh.prefab";

    /// <summary>默认盒子 Prefab 路径，metaDataUrl 为空时展示该外观（路径待填写）</summary>
    public const string defaultModelPath = "Assets/Arts/EachScene/Box_Scene/ArtScence/BreedingFarm_fbx_box2.prefab";
    public CabinBoxSceneNetManager() { }

    // ──────────────────────────────────────────────
    // 草稿列表
    // ──────────────────────────────────────────────

    /// <summary>
    /// 获取当前用户的 AI 伙伴盒子草稿列表。
    /// GET /ugc/characterBox/draftList
    /// </summary>
    /// <param name="callback">回调：(是否成功, 草稿列表)</param>
    public void GetCharacterBoxDraftList(Action<bool, List<CharacterBoxInfo>> callback = null)
    {
        var req = new JObject()
        {
            ["uid"] = AccountDataManager.Inst.Uid,
        };

        NetworkManager.Inst.SendHttpRequest(
            HttpUrlDefine.CharacterBoxDraftList,
            HttpMethod.GET,
            JsonConvert.SerializeObject(req),
            rspStr =>
            {
                // 反序列化响应体
                CharacterBoxDraftListData rsp = JsonConvert.DeserializeObject<CharacterBoxDraftListData>(rspStr);

                if (rsp == null)
                {
                    LoggerUtils.LogError("获取AI伙伴盒子草稿列表失败: 响应为空");
                    callback?.Invoke(false, null);
                    return;
                }

                // rsp.list 为 List<CharacterBoxPublishItem>，取出内层 CharacterBoxInfo
                var list = rsp.list;
                var infos = new List<CharacterBoxInfo>(list != null ? list.Count : 0);
                if (list != null)
                {
                    foreach (var item in list)
                    {
                        if (item?.characterBoxInfo != null)
                        {
                            infos.Add(item.characterBoxInfo);
                        }
                    }
                }

                callback?.Invoke(true, infos);
            },
            errRspStr =>
            {
                LoggerUtils.LogError("获取AI伙伴盒子草稿列表失败: " + errRspStr);
                callback?.Invoke(false, null);
            });
    }

    // ──────────────────────────────────────────────
    // 发布列表
    // ──────────────────────────────────────────────

    /// <summary>
    /// 获取当前用户的 AI 伙伴盒子发布列表。
    /// GET /ugc/characterBox/publishList
    /// </summary>
    /// <param name="callback">回调：(是否成功, 发布列表，每项含互动数据)</param>
    public void GetCharacterBoxPublishList(Action<bool, List<CharacterBoxPublishItem>> callback = null)
    {
        var req = new JObject()
        {
            ["uid"] = AccountDataManager.Inst.Uid,
        };

        NetworkManager.Inst.SendHttpRequest(
            HttpUrlDefine.CharacterBoxPublishList,
            HttpMethod.GET,
            JsonConvert.SerializeObject(req),
            rspStr =>
            {
                // 反序列化响应体
                CharacterBoxPublishListData rsp = JsonConvert.DeserializeObject<CharacterBoxPublishListData>(rspStr);

                if (rsp == null)
                {
                    LoggerUtils.LogError("获取AI伙伴盒子发布列表失败: 响应为空");
                    callback?.Invoke(false, null);
                    return;
                }

                callback?.Invoke(true, rsp.list ?? new List<CharacterBoxPublishItem>());
            },
            errRspStr =>
            {
                LoggerUtils.LogError("获取AI伙伴盒子发布列表失败: " + errRspStr);
                callback?.Invoke(false, null);
            });
    }

    // ──────────────────────────────────────────────
    // 设置（创建 / 编辑 / 发布 / 删除）
    // ──────────────────────────────────────────────

    /// <summary>
    /// 设置 AI 伙伴盒子（创建草稿、编辑草稿、复制草稿、发布草稿、更新发布、删除）。
    /// POST /ugc/characterBox/set
    /// </summary>
    /// <param name="setType">
    /// 操作类型：Create=1 创建草稿, Edit=2 编辑草稿, Copy=3 复制草稿,
    /// Publish=4 发布草稿, UpdatePublish=5 更新发布, Delete=7 删除
    /// </param>
    /// <param name="info">待设置的盒子信息</param>
    /// <param name="callback">回调：(是否成功, 服务端返回的最新盒子信息)</param>
    public void SetCharacterBox(SetType setType, CharacterBoxInfo info, Action<bool, CharacterBoxInfo> callback = null)
    {
        var req = new CharacterBoxSetRequestData()
        {
            setType = setType,
            characterBoxInfo = info,
        };

        NetworkManager.Inst.SendHttpRequest(
            HttpUrlDefine.CharacterBoxSet,
            HttpMethod.POST,
            JsonConvert.SerializeObject(req),
            rspStr =>
            {
                // 反序列化响应体
                CharacterBoxSetRspData rsp = JsonConvert.DeserializeObject<CharacterBoxSetRspData>(rspStr);

                if (rsp == null)
                {
                    LoggerUtils.LogError("AI伙伴盒子设置失败: 响应为空");
                    callback?.Invoke(false, null);
                    return;
                }

                callback?.Invoke(true, rsp.characterBoxInfo);
            },
            errRspStr =>
            {
                LoggerUtils.LogError("AI伙伴盒子设置失败: " + errRspStr);
                callback?.Invoke(false, null);
            });
    }

    /// <summary>
    /// 设置 AI 伙伴盒子，同时暴露原始失败响应字符串（供调用方处理 501 审核拒绝等特殊错误码）。
    /// POST /ugc/characterBox/set
    /// </summary>
    /// <param name="setType">操作类型</param>
    /// <param name="info">待设置的盒子信息</param>
    /// <param name="callback">成功回调：(是否成功, 服务端返回的最新盒子信息)</param>
    /// <param name="onFail">失败回调：原始错误响应 JSON 字符串，供调用方自行解析错误码</param>
    public void SetCharacterBox(SetType setType, CharacterBoxInfo info,
        Action<bool, CharacterBoxInfo> callback, Action<string> onFail)
    {
        var req = new CharacterBoxSetRequestData()
        {
            setType = setType,
            characterBoxInfo = info,
        };

        NetworkManager.Inst.SendHttpRequest(
            HttpUrlDefine.CharacterBoxSet,
            HttpMethod.POST,
            JsonConvert.SerializeObject(req),
            rspStr =>
            {
                // 反序列化响应体
                CharacterBoxSetRspData rsp = JsonConvert.DeserializeObject<CharacterBoxSetRspData>(rspStr);

                if (rsp == null)
                {
                    LoggerUtils.LogError("[CabinBoxSceneNetManager] SetCharacterBox 失败: 响应为空");
                    callback?.Invoke(false, null);
                    return;
                }

                callback?.Invoke(true, rsp.characterBoxInfo);
            },
            errRspStr =>
            {
                LoggerUtils.LogError($"[CabinBoxSceneNetManager] SetCharacterBox 失败: {errRspStr}");
                onFail?.Invoke(errRspStr);
            });
    }

    // ──────────────────────────────────────────────
    // 详情
    // ──────────────────────────────────────────────

    /// <summary>
    /// 获取单个 AI 伙伴盒子的详细信息，包含创作者信息和互动数据。
    /// GET /ugc/characterBox/info
    /// </summary>
    /// <param name="id">盒子 ID</param>
    /// <param name="callback">回调：(是否成功, 详情数据)</param>
    public void GetCharacterBoxDetail(string id, Action<bool, CharacterBoxDetailData> callback = null)
    {
        var req = new JObject()
        {
            ["id"] = id,
        };

        NetworkManager.Inst.SendHttpRequest(
            HttpUrlDefine.CharacterBoxInfoUrl,
            HttpMethod.GET,
            JsonConvert.SerializeObject(req),
            rspStr =>
            {
                // 反序列化响应体
                CharacterBoxDetailData rsp = JsonConvert.DeserializeObject<CharacterBoxDetailData>(rspStr);

                if (rsp == null)
                {
                    LoggerUtils.LogError("获取AI伙伴盒子详情失败: 响应为空, id=" + id);
                    callback?.Invoke(false, null);
                    return;
                }

                callback?.Invoke(true, rsp);
            },
            errRspStr =>
            {
                LoggerUtils.LogError("获取AI伙伴盒子详情失败: " + errRspStr + ", id=" + id);
                callback?.Invoke(false, null);
            });
    }

    // ──────────────────────────────────────────────
    // 批量详情
    // ──────────────────────────────────────────────

    /// <summary>
    /// 批量获取 AI 伙伴盒子详情。
    /// GET /ugc/characterBox/batchInfo
    /// </summary>
    /// <param name="idList">逗号分隔的盒子 ID 字符串，例如 "1111,2222,3333"</param>
    /// <param name="callback">回调：(是否成功, 盒子列表)</param>
    public void GetCharacterBoxBatchInfo(string idList, Action<bool, List<CharacterBoxInfo>> callback = null)
    {
        var req = new JObject()
        {
            ["idList"] = idList,
        };

        NetworkManager.Inst.SendHttpRequest(
            HttpUrlDefine.CharacterBoxBatchInfo,
            HttpMethod.GET,
            JsonConvert.SerializeObject(req),
            rspStr =>
            {
                // 反序列化响应体
                CharacterBoxBatchRspData rsp = JsonConvert.DeserializeObject<CharacterBoxBatchRspData>(rspStr);

                if (rsp == null)
                {
                    LoggerUtils.LogError("批量获取AI伙伴盒子详情失败: 响应为空, idList=" + idList);
                    callback?.Invoke(false, null);
                    return;
                }

                callback?.Invoke(true, rsp.characterBoxList ?? new List<CharacterBoxInfo>());
            },
            errRspStr =>
            {
                LoggerUtils.LogError("批量获取AI伙伴盒子详情失败: " + errRspStr);
                callback?.Invoke(false, null);
            });
    }

    // ──────────────────────────────────────────────
    // 搜索
    // ──────────────────────────────────────────────

    /// <summary>
    /// 搜索 AI 伙伴盒子。
    /// GET /search/characterBox
    /// </summary>
    /// <param name="searchWord">搜索关键词</param>
    /// <param name="callback">回调：(是否成功, 搜索结果列表)</param>
    public void SearchCharacterBox(string searchWord, Action<bool, List<CharacterBoxSearchItem>> callback = null)
    {
        var req = new JObject()
        {
            ["searchWord"] = searchWord ?? string.Empty,
        };

        NetworkManager.Inst.SendHttpRequest(
            HttpUrlDefine.SearchCharacterBox,
            HttpMethod.GET,
            JsonConvert.SerializeObject(req),
            rspStr =>
            {
                // 反序列化响应体
                CharacterBoxSearchRspData rsp = JsonConvert.DeserializeObject<CharacterBoxSearchRspData>(rspStr);

                if (rsp == null)
                {
                    LoggerUtils.LogError("搜索AI伙伴盒子失败: 响应为空, keyword=" + searchWord);
                    callback?.Invoke(false, null);
                    return;
                }

                callback?.Invoke(true, rsp.list ?? new List<CharacterBoxSearchItem>());
            },
            errRspStr =>
            {
                LoggerUtils.LogError("搜索AI伙伴盒子失败: " + errRspStr);
                callback?.Invoke(false, null);
            });
    }
}
