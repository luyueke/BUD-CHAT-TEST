using Basic.Utils;
using GameData.Base;
using GameData.BaseInfo;
using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// BOX角色数据局部变更类型，配合 MessageName.OnCabinCharacterUpdated 使用
/// </summary>
public enum CabinCharacterUpdateType
{
    All,                //刷新全部
    Cover,           // 封面图 URL（info.cover）
    CardColor,       // 卡片背景色（cabinCoverInfo.detail.color）
    CardDesc,        // 封面台词（cabinCoverInfo.desc）
    CharacterDetail, // 角色位置/缩放（编辑器 Slider 调整）
}

public enum UGCClass
{
    None = 0,          // 无
    Draft = 1,       // 草稿
    Published = 2,   // 已发布
    Unpublished = 3,  // 已下架
    Buy = 4           //已购买
}

public enum CabinPurchasedType
{
    All = 1,         //1：所有列表，
    Published = 2,   //2：我创作的，
    UGCShop = 3,   //3：社区购买列表
    PGCShop = 4   //4:官方商城
}

/// <summary>
/// 进入BOX编辑器的入口类型，用于区分创建新角色还是编辑已有草稿
/// </summary>
public enum CabinEntryType
{
    Create = 0,  // 创建新角色
    Edit = 1,  // 编辑已有草稿
}

/// <summary>
/// 养成舱网络数据
/// </summary>
public class CabinPublishListData   //角色发布列表信息
{
    public List<CabinPublishData> list;
}

/// <summary>
/// 养成舱网络数据
/// </summary>
public class CabinSearchListData   //角色发布列表信息
{
    public List<CabinPublishData_Search> list;

    public List<CabinPublishData> GetData()
    {
        List<CabinPublishData> tList = new List<CabinPublishData>();
        if (list == null)
        {
            return tList;
        }

        foreach (var item in list)
        {
            tList.Add(new CabinPublishData(item.ugcInfo)
            {
                creatorInfo = item.creatorInfo,
                interactInfo = item.interactInfo,
            });
        }
        return tList;
    }
}

/// <summary>
/// 角色发布列表分页响应。继承 HttpPageUseData，cookie/isEnd/isFirst 由 HttpPageRequestHandle 自动读取与回填，
/// 用于服务端真分页拉取角色发布列表（替代一次性返回的 CabinPublishListData）。
/// </summary>
public class CabinPublishPageData : Game.Store.HttpPageUseData
{
    public List<CabinPublishData> list;
}

/// <summary>
/// 角色搜索分页响应。继承 HttpPageUseData，list 为搜索专用结构 CabinPublishData_Search，
/// 通过 GetData() 统一转换为 CabinPublishData 供列表使用（与 CabinSearchListData.GetData 逻辑一致）。
/// </summary>
public class CabinSearchPageData : Game.Store.HttpPageUseData
{
    public List<CabinPublishData_Search> list;

    /// <summary>将搜索结果结构转换为通用 CabinPublishData 列表，便于与发布列表共用同一刷新流程。</summary>
    public List<CabinPublishData> GetData()
    {
        List<CabinPublishData> tList = new List<CabinPublishData>();
        if (list == null)
        {
            return tList;
        }

        foreach (var item in list)
        {
            tList.Add(new CabinPublishData(item.ugcInfo)
            {
                creatorInfo = item.creatorInfo,
                interactInfo = item.interactInfo,
            });
        }
        return tList;
    }
}

/// <summary>
/// 角色草稿列表分页响应。继承 HttpPageUseData，cookie/isEnd/isFirst 由 HttpPageRequestHandle 自动读取与回填，
/// 用于服务端真分页拉取草稿列表（替代一次性返回的 CabinPublishListData）。每个状态页签独立分页。
/// </summary>
public class CabinDraftPageData : Game.Store.HttpPageUseData
{
    public List<CabinPublishData> list;
}

public class SetCabinCharacterInfoData
{
    public SetType setType;
    public CabinCharacterUgcInfo characterInfo;
}

public class SetCabinCharacterInfoRspData
{
    public CabinCharacterUgcInfo characterInfo;
}

public class SetCabinCharacterPackInfoData
{
    public SetType setType;
    public CabinCharacterPackInfo characterPackInfo;
}
public class ReqToneBatchPreview
{
    public string voiceId;
    public List<string> texts;
    public int languageType;
}


public class SetCabinCharacterPackInfoRspData
{
    public CabinCharacterPackInfo characterPackInfo;
}


public class CabinPublishData
{
    public CabinCharacterUgcInfo characterInfo; //BOX角色
    public BaseInteractInfo interactInfo; //互动信息(用户ugc交互)
    public AccountUserInfo creatorInfo; //角色创作者信息，由调用方按需填充，可为 null

    public CabinPublishData(CabinCharacterUgcInfo characterInfo)
    {
        this.characterInfo = characterInfo;
    }
}

public class CabinPublishData_Search
{
    public CabinCharacterUgcInfo ugcInfo; //BOX角色
    public BaseInteractInfo interactInfo; //互动信息(用户ugc交互)
    public AccountUserInfo creatorInfo; //角色创作者信息，由调用方按需填充，可为 null
}

public class CabinCharacterSetData
{
    public int setType; //1 创建草稿2 编辑草稿3 复制草稿4 发布草稿5 更新发布 7 删除
    public CabinCharacterUgcInfo characterInfo;
}


public class CabinCoverInfo
{
    public string desc;   //封面描述
    public string detail; //封面坐标颜色等数据（CabinCoverDetail 序列化的 JSON）
    public CabinPropInfo poseInfo;
    /// <summary>将 detail JSON 字符串解析为 CabinCoverDetail，字段缺失或格式非法时返回默认实例</summary>
    public CabinCoverDetail GetDetail()
    {
        if (!string.IsNullOrEmpty(detail))
        {
            try
            {
                var result = JsonConvert.DeserializeObject<CabinCoverDetail>(detail);
                if (result != null) return result;
            }
            catch { }
        }
        return new CabinCoverDetail();
    }

    /// <summary>将 CabinCoverDetail 序列化后写入 detail 字段</summary>
    public void SetDetail(CabinCoverDetail coverDetail)
    {
        detail = JsonConvert.SerializeObject(coverDetail);
    }

}

/// <summary>卡片封面的详细参数，序列化后存储在 CabinCoverInfo.detail</summary>
public class CabinCoverDetail
{
    public int colorId; //卡片背景色配置 ID（对应 DraftBoxCardColorConfig.Id）
    public string poseId;           //当前封面使用的姿势资源 ID（官方为 config ID，UGC 为 AssetsData.Id）
    public int poseResourceType;   //姿势资源类型（ResourceType 枚举值：Pose=10 官方，UgcPose=11 UGC）
    public Vector3 sizeVec3;
    public Vector3 posVec3;
}

public enum emUnpublished
{
    None = 0,
    delist = 1,
    isBan = 2,
    All = 3,
}




/// <summary>
/// 皮肤包信息
/// </summary>
public class SkinPackInfo
{
    public string packId;
    public string cover;
    public string avatarJson;
    public int isDefault;
}

/// <summary>
/// 动作包信息
/// </summary>
public class EmotePackInfo
{
    public List<EmoteData> emoteList;
    public List<EmoteData> loopEmoteList;
}

public class UgcEmoteData
{
    public string id;
    public string metaDataUrl;
    public string cover;
    public List<CabinPropInfo> propList;
}
public class CabinPropInfo
{
    public int index;
    public int bindIndex;
    public string id;
    public string metaDataUrl;
    public string cover;
}


public class EmoteData
{
    public string emoteId;
    public UgcEmoteData ugcEmoteData;
    public int isPgc;
}


/// <summary>
/// 语音包信息
/// </summary>
public class DialogueData
{
    public string id; //语音包id
    public string text;
    public string toneId;
    public int isPgc;
    public string audioUrl; //语音包对应的音频
}



/// <summary>
/// 待机动作信息
/// </summary>
public class PendingEmoteData
{
    public List<pEmoteData> emoteList = new List<pEmoteData>();
    public List<pEmoteData> loopEmoteList = new List<pEmoteData>();
}


public class pEmoteData
{
    public string emoteId;
    public UgcIdleData ugcData;
}




/// <summary>
/// 角色互动基础数据，包含动作、音频及状态的公共字段。
/// 供 characterInteraction 和 voiceCommands 共同继承使用。
/// </summary>
public class BaseInteractionData
{
    public string emoteId;        // 动作id
    public int isMute;            // 是否静音
    public string text;           // 语音包id
    public int delaySecond;       // 延迟时间
    public int isPgc;             // 是否是pgc
    public int languageType;      // 中文：0，英语：1，日语：2
    public string audioUrl;       // 音频url
    public int disabled;          // 1:禁用 0:可用
    public UgcIdleData ugcData;
}

/// <summary>
/// 唤醒动作信息
/// </summary>
public class characterInteraction : BaseInteractionData
{
}

/// <summary>
/// 口令互动信息
/// </summary>
public class voiceCommands : BaseInteractionData
{
    public string command; // 口令
}




public class dialogueCommands
{
    public string moodId; //情绪id
    public List<string> emoteIds; //动作id列表
}



public class CabinDoubaoBatchPreviewData
{
    public List<CabinDoubaoBatchPreviewSubData> list;
}
public class CabinDoubaoBatchPreviewSubData
{
    public string text; //文本及对应音频
    public string url; //文本的url
}




/// <summary>
/// BOX角色详情信息
/// </summary> <summary>
/// 
/// </summary>
public class CabinCharacterDetailData
{
    public CabinCharacterUgcInfo characterInfo; //BOX角色
    public AccountUserInfo creator;
    public BaseInteractInfo interactInfo; //互动信息(用户ugc交互)
    public RelationShipInfo relationShipInfo;
}


/// <summary>
/// BOX角色音色详情信息
/// </summary> <summary>
/// 
/// </summary>
public class CabinCharacterToneDetailData
{
    public CabinToneInfo characterToneInfo; //BOX角色
    public AccountUserInfo creator;
    public BaseInteractInfo interactInfo; //互动信息(用户ugc交互)
    public RelationShipInfo relationShipInfo;
}





#region 音色

public class CabinCharacterToneInfo
{
    public int setType; //1 创建草稿2 编辑草稿3 复制草稿4 发布草稿5 更新发布 7 删除
    public CabinToneInfo characterToneInfo;
}



public class CabinTonePublishData
{
    public string cookie;
    public int isEnd; //是否结束 0 否，1 是
    public List<CabinCharacterTonePublishSubData> list; //BOX角色
}

public class CabinCharacterToneBuyedSubData
{
    [JsonConverter(typeof(StringObjectConverter))]
    public CabinToneInfo ugcData;
    public BaseInteractInfo interactInfo; //互动信息(用户ugc交互)
}


public class CabinCharacterTonePublishSubData
{
    public CabinToneInfo characterToneInfo;
    public BaseInteractInfo interactInfo; //互动信息(用户ugc交互)
}

public class CabinCharacterToneSearchSubData
{
    public CabinToneInfo ugcInfo;
    public BaseInteractInfo interactInfo; //互动信息(用户ugc交互)
}

// AI伴侣音色标签
public class CharacterToneTagItem
{
    public int tagId;
    public string tagName;
}

public class CharacterToneTagData
{
    public int groupId;
    public string groupName;
    public List<CharacterToneTagItem> tagList;
}

public class CabinToneTagsRsp
{
    public List<CharacterToneTagData> list;
}

// 搜索AI伴侣音色（分页）
public class SearchCabinToneRsp : Network.Http.HttpPageBaseData
{
    public List<CabinCharacterToneSearchSubData> list;
}
#endregion

public class CabinUgcBaseInfo : UgcBaseInfo
{
    public int isDeleted; //是否被删除 0 否，1 是
    public PaymentInfo paymentInfo;         //发布信息
    public int ugcclass;               //UGC 类型：1=草稿  2=已发布 3=已下架 4=已购买

    public int isBan;       //是否被运营下架 0未下架  1下架
    public int delist;      // 是否自己下架 0未下架  1下架
    public emUnpublished IsUnpublished()
    {
        if (delist == 1 && isBan == 1)
        {
            return emUnpublished.All;
        }
        if (delist == 1)
        {
            return emUnpublished.delist;
        }
        if (isBan == 1)
        {
            return emUnpublished.isBan;
        }
        return emUnpublished.None;
    }
}



public class CabinCharacterBaseInfo : CabinUgcBaseInfo
{
    public List<SkinPackInfo> skinPack = new List<SkinPackInfo>(); //皮肤包
    public PendingEmoteData pendingEmote = new PendingEmoteData();           // 待机动作
    public List<characterInteraction> activation = new List<characterInteraction>();  // 唤醒动作
    public List<voiceCommands> voiceCommands = new List<voiceCommands>();       // 口令互动
    public CabinCoverInfo coverInfo = new CabinCoverInfo();   //角色封面详情数据
}

public class CabinCharacterUgcInfo : CabinCharacterBaseInfo
{
    public string toneId; //音色id
    public List<string> extensionPackList = new List<string>(); //角色拓展包id 列表
    public string pubName;             //发布名称
    public string pubDesc;              //发布描述
    public string targetUgcId;          //商品原始UGCid
    public string stockUgcId;           //库存原始UGCid ，自己的草稿列表才会用得到
    public string characterPortraitUrl; //头像的URL地址
    public PendingEmoteData usingEmote = new PendingEmoteData();           // 使用的待机动作
    public List<characterInteraction> usingActivation = new List<characterInteraction>();  // 唤醒动作
    public List<voiceCommands> usingVoiceCommands = new List<voiceCommands>();       // 口令互动
    public string botProfile;           // ai创建最后的整个botProfilejson string

    public string GetName()
    {
        return base.name ?? string.Empty;
    }
}


public class CabinCharacterPackInfo : CabinCharacterBaseInfo
{
    public string characterId;                      // 绑定的角色id
}

public class CabinExtensionPackBatchRspData
{
    public List<CabinExtensionPackBatchItem> characterPackList;
}

public class CabinExtensionPackBatchItem
{
    public CabinCharacterPackInfo characterPackInfo;
}
