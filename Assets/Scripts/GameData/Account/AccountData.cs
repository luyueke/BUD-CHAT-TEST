using System;
using System.Collections.Generic;
using Game.Avatar;
using GameData.Account;
using GameData.BaseInfo;
using Newtonsoft.Json;
using UnityEngine;

public enum AccountPlatform
{
    Unknown = 0,
    Wechat = 1,
    Qq = 2,
    Douyin = 3,
    Apple = 4,
    Channel = 5, // 华为、小米等渠道包
    Snapchat = 6,
    Facebook = 7,
    Google = 8,
    Twitter = 9,
    TikTok = 10,
    Taptap = 11, // 国服S5 添加taptap登陆
    Tourists = 99,
    AppleAudit = 100, // apple 审核用入口
}

public enum SetUserInfoType
{
    None = 0,
    Registration = 1,
    All = 2,
    Nick = 3,
    Avatar = 4,
    Gender = 5,
    Portrait = 6,
    Bio = 7,
    Birthday = 8,
    LobbyIdle = 12,
    PetLobbyIdle = 13,
    SetPetLobbyVisible = 14, //14 包含宠物可见和动画重置
    ProfileTheme = 15,//15 修改个人主页皮肤
    SetAvatarFrame = 17,//17 设置当前个人主页皮肤
    SetChatBubbles = 19,//19 设置聊天气泡
    SetNicknameBg = 23,// 设置昵称背景
    SetTitleId = 25,// 设置称号ID
    SetCreatorBadge = 27,// 设置创作者赛季徽章展示品类
}

public class AccountAuthData
{
    public string username;
    public int platform;
    public string unionid;
    public SignChannelInfo channelInfo;
}

public class SignChannelInfo
{
    public string appID;
    public string timestamp;
    public string uid;
    public string token;
    public string sdkUsername;
    public bool newAccount;
    public string channelId;
}

public class SignInReq
{
    public string openId;
    public int provider;

    public SignChannelInfo channelInfo;
}

public class SetImageReq {
    public AccountPetInfo petInfo;
    public AccountUserInfo userInfo;
    public VehicleInfo vehicleInfo;
    /// <summary>
    /// 1 新用户注册:userInfo全部传入 2 设置全部:userInfo全部传入 3 设置昵称:userInfo 4 设置人物形象:userInfo传入avatarJson
    /// 5 设置性别:userInfo传入gender
    /// 6 设置头像:userInfo传入portraitUrl
    /// 7 设置生日:userInfo传入birthday
    /// </summary>
    public int setType;
}

public class GetImageRes
{
    public AccountUserInfo userInfo;
    public AccountPetInfo petInfo;
    public AccountAmount amount;
    public AIBuddyInfo aibuddyInfo;
    // 大厅 AI 伙伴角色（GameData 侧复刻类型，可见性 isHidden 内置，对标 aibuddyInfo）
    public HallCharacterInfo characterInfo;
    // 服务器按载具类型分字段返回：UGC 载具在 vehicleInfo，PGC 载具在 lobbyVehicle
    public VehicleInfo vehicleInfo;
    public VehicleInfo lobbyVehicle;
    public AIGamePassStatus[] aiGameStatus;
    public int showedPopupRating; //是否弹出过评分弹窗 1:弹出过 0:未弹出

    /// <summary>
    /// 取大厅展示载具：UGC(vehicleInfo)/PGC(lobbyVehicle) 二选一，优先取可见者，都不可见时取非空者
    /// </summary>
    public VehicleInfo GetLobbyVehicleInfo()
    {
        if (lobbyVehicle != null && lobbyVehicle.isHidden == 0)
        {
            return lobbyVehicle;
        }
        if (vehicleInfo != null && vehicleInfo.isHidden == 0)
        {
            return vehicleInfo;
        }
        return lobbyVehicle ?? vehicleInfo;
    }
}

public class AIGamePassStatus
{
    public int gameId;
    public bool hasFinPgc;
    public bool hasFinUgc;
    public bool hasPlayedPgc;
    public int finUGCRewardLeftTime;
    public int playedUGCRewardLeftTime;
    public bool hasDailyShare;
}

public class AccountAmount
{
    public double likeAmount;
    public double consumeAmount;
    public double followingAmount;
    public double fansAmount;
}

public class MIData
{
    public bool isPgc = true;
    public string id = "12400014";
}

public class MSData
{
    public string id = "";
}

[Serializable]
public class AccountData
{
    /// <summary>
    /// 是否为新用户
    /// </summary>
    public bool newUser;

    public string token;

    public int platform;

    public string unionid;

    public AccountUserInfo userInfo;

    public AccountPetInfo petInfo;

    public AIBuddyInfo aibuddyInfo;

    public bool isNewOpenId;

    public Dictionary<string, MIData> tryListenMIDatas = new();
    public Dictionary<string, MSData> tryListenMSDatas = new();

    /// <summary>
    /// 是否走新用户注册流程
    /// </summary>
    public bool IsNewUserRegister
    {
        get
        {
            if (newUser)
            {
                return true;
            }

            var username = userInfo?.username;
            if (string.IsNullOrEmpty(username))
            {
                return true;
            }

            return false;
        }
    }
}

[Serializable]
public class StatisticsData
{
    public int pubMapCnt;
    public int pubSkinCnt;
    public int pubPropCnt;
    public int experienceMapCnt;
    public int ownPropCnt;
    public int ownSkin;
    public int pubPostCnt;
    public int pubMaterialCnt;
    public int pubMusicScoreCnt;
    public int pubMusicToneCnt;
    public int pubAnimationCnt;
    public int pubPoseCnt;
    public int pubNpcCnt;
    public int pubVehicleCnt;
    public int pubTheaterCnt;
    public int pubActorCnt;
}

[Serializable]
public class RelationShipData
{
    public int followStatus;
    public int friendStatus;
}

//海外老用户绑定新海外服数据
[Serializable]
public class BindStatusData
{
    public bool needBind;
    public int provider;
    public AccountData accountData;
}

[Serializable]
public class CreatorBadgeInfoData
{
    public int score;
    public int level;
    public int category;
    public int id;
    /// <summary>榜单名次，服务端 /creation/badgeList 等返回；未上榜可为 0。</summary>
    public int rank;
    public string feature;
    /// <summary>徽章结算时间，Unix 秒时间戳；服务端返回，用于判断是否已过期（跨周则视为本周未获得）。</summary>
    public long settleTime;
}