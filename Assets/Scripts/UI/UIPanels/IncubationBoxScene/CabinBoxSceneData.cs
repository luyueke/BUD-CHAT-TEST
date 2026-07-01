using GameData.Base;
using GameData.BaseInfo;
using System.Collections.Generic;

/// <summary>
/// AI 伙伴盒子（CharacterBox）相关数据结构定义。
/// 供 CabinBoxSceneNetManager 和各 UI 面板使用。
/// </summary>

// ──────────────────────────────────────────────
// 核心数据模型
// ──────────────────────────────────────────────

/// <summary>
/// AI 伙伴盒子主数据模型，涵盖草稿与发布共用字段。
/// </summary>
public class CharacterBoxInfo :UgcBaseInfo
{
    /// <summary>是否已删除：0=否，1=是</summary>
    public int isDelete;

    /// <summary>付费信息（price / currencyType）</summary>
    public PaymentInfo paymentInfo;

    /// <summary>
    /// UGC 状态：1=草稿，2=已发布，3=已下架，4=已购买
    /// </summary>
    public int ugcclass;

    /// <summary>per-user 消费状态：1=已拥有，0=未拥有（由请求层从 interactInfo 回填）</summary>
    public int consumed;
}

// ──────────────────────────────────────────────
// 草稿列表
// ──────────────────────────────────────────────

/// <summary>
/// 草稿列表接口响应数据包装（GET /ugc/characterBox/draftList）。
/// </summary>
public class CharacterBoxDraftListData
{
    /// <summary>
    /// 草稿列表。服务端每项结构与发布列表相同，均为 {"characterBoxInfo":{...}} 包装，
    /// 因此复用 CharacterBoxPublishItem（interactInfo 在草稿场景下为 null）。
    /// </summary>
    public List<CharacterBoxPublishItem> list;

    /// <summary>是否已到最后一页：1=无更多数据，0=仍有更多</summary>
    public int isEnd;

    /// <summary>分页游标，下一次请求时传入以获取下一页</summary>
    public string cookie;
}

// ──────────────────────────────────────────────
// 发布列表
// ──────────────────────────────────────────────

/// <summary>
/// 发布列表中的单条记录，包含盒子信息与互动数据。
/// </summary>
public class CharacterBoxPublishItem
{
    /// <summary>AI 伙伴盒子信息</summary>
    public CharacterBoxInfo characterBoxInfo;

    /// <summary>互动信息（点赞、收藏、评论等）</summary>
    public BaseInteractInfo interactInfo;
}

/// <summary>
/// 发布列表接口响应数据包装（GET /ugc/characterBox/publishList）。
/// </summary>
public class CharacterBoxPublishListData
{
    /// <summary>发布列表</summary>
    public List<CharacterBoxPublishItem> list;

    /// <summary>是否已到最后一页：1=无更多数据，0=仍有更多</summary>
    public int isEnd;

    /// <summary>分页游标，下一次请求时传入以获取下一页</summary>
    public string cookie;
}

// ──────────────────────────────────────────────
// 设置（创建 / 编辑 / 发布 / 删除）
// ──────────────────────────────────────────────

/// <summary>
/// 盒子设置接口请求体（POST /ugc/characterBox/set）。
/// setType 使用全局枚举 SetType：
///   Create=1  创建草稿
///   Edit=2    编辑草稿
///   Copy=3    复制草稿
///   Publish=4 发布草稿
///   UpdatePublish=5 更新发布
///   Delete=7  删除
/// </summary>
public class CharacterBoxSetRequestData
{
    /// <summary>操作类型，参见 SetType 枚举</summary>
    public SetType setType;

    /// <summary>盒子信息</summary>
    public CharacterBoxInfo characterBoxInfo;
}

/// <summary>
/// 盒子设置接口响应数据（POST /ugc/characterBox/set）。
/// </summary>
public class CharacterBoxSetRspData
{
    /// <summary>服务端返回的最新盒子信息</summary>
    public CharacterBoxInfo characterBoxInfo;
}

// ──────────────────────────────────────────────
// 详情
// ──────────────────────────────────────────────

/// <summary>
/// 盒子详情接口响应数据（GET /ugc/characterBox/info）。
/// </summary>
public class CharacterBoxDetailData
{
    /// <summary>AI 伙伴盒子详细信息</summary>
    public CharacterBoxInfo characterBoxInfo;

    /// <summary>创作者账号信息</summary>
    public AccountUserInfo creator;

    /// <summary>互动信息（点赞、收藏、评论等）</summary>
    public BaseInteractInfo interactInfo;

    /// <summary>关系信息</summary>
    public RelationShipInfo relationShipInfo;
}

// ──────────────────────────────────────────────
// 批量详情
// ──────────────────────────────────────────────

/// <summary>
/// 批量盒子详情接口响应数据（GET /ugc/characterBox/batchInfo）。
/// 请求时传入逗号分隔的 idList，如 "1111,2222,3333"。
/// </summary>
public class CharacterBoxBatchRspData
{
    /// <summary>批量查询结果列表</summary>
    public List<CharacterBoxInfo> characterBoxList;
}

// ──────────────────────────────────────────────
// 搜索
// ──────────────────────────────────────────────

/// <summary>
/// 搜索结果中的单条记录，包含盒子信息、创作者信息和互动数据。
/// </summary>
public class CharacterBoxSearchItem
{
    /// <summary>AI 伙伴盒子信息</summary>
    public CharacterBoxInfo characterBoxInfo;

    /// <summary>创作者账号信息</summary>
    public AccountUserInfo creatorInfo;

    /// <summary>互动信息</summary>
    public BaseInteractInfo interactInfo;
}

/// <summary>
/// 搜索接口响应数据包装（GET /search/characterBox）。
/// </summary>
public class CharacterBoxSearchRspData
{
    /// <summary>搜索结果列表</summary>
    public List<CharacterBoxSearchItem> list;
}
