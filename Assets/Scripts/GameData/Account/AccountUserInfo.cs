
using Newtonsoft.Json;
using System;
using Game.Avatar;
using GameData.BaseInfo;
using GameData.PgcData;
using UnityEngine;
using System.Collections.Generic;
using Game.Config;
using Sirenix.Utilities;

/// <summary>
/// 登录基本人物信息
/// </summary>
[Serializable]
public class AccountUserInfo : UserInfoProtocol, IComparable<AccountUserInfo>
{
    /// <summary>简介</summary>
    public string bio;
    /// <summary>生日，秒</summary>
    public long birthday;
    /// <summary>性别，1男 2女</summary>
    public int gender;
    public string uid { get; set; }
    public string portraitUrl { get; set; }
    public string nickname { get; set; }
    public string username { get; set; }

    /// <summary>人物形象数据</summary>
    public string avatarJson;

    /// <summary>注册时间</summary>
    public long registerTime;

    

    /// <summary>
    /// vip标识信息
    /// </summary>
    public SubScribeData subscribeData;

    public CreatorLevelInfo creatorLevelInfo;

    public IdleData idleData;
    /// <summary>
    /// 用户头衔
    /// </summary>
    public UserTitle userTitle;

    public int homepageSkin;//当前玩家个人主页皮肤id
    public List<HomepageSkinInfo> ownedHomepageSkinList;
    
    //当前头像框
    public int avatarFrame;
    //当前拥有的头像框
    public List<OwnedAvatarFrame> ownedAvatarFrameList;
    
    //当前聊天气泡
    public int chatBubbles;
    //当前拥有的聊天气泡
    public List<OwnedChatBubble> ownedChatBubblesList;
    public int isNewUser; //1是新手;

    public int nicknameFrame;
    //当前拥有的昵称列表
    public List<OwnedNicknameBg> ownedNicknameFrameList;

    public int titleId; //称号ID

    public List<OwnedTitleBg> ownedTitleList; //当前拥有称号列表
    public CreatorBadgeInfoData creatorBadgeInfo;
    
    public class UserTitle
    {
        public string titleName;
        public int titleId;//1  2  3 
        public int claimTime;//领取时间
        public int endTime;//领取时间
    }
    
    public class HomepageSkinInfo
    {
        public int skinId;
        public int ownedTime;
    }
    
    public class OwnedAvatarFrame
    {
        public int frameId;
        public long ownedTime;
        public long expireTime;
        public string leftTime;
    }
    
    public class OwnedChatBubble
    {
        public int bubbleId;
        public int ownedTime;
    }

    public class OwnedNicknameBg
    {
        public int frameId;
        public int ownedTime;
    }

    public class OwnedTitleBg
    {
        public int titleId;
        public long ownedTime;
        public long expireTime;
        public string leftTime;
    }
    public enum TitleId
    {
        NewCreator = 1,
        PopularCreator = 2,
        HotCreator = 3
    }
    public class SubScribeData
    {
        public long subscribeTime;
        public long expirationTime;
        public int vipType;
    }


    public int CompareTo(AccountUserInfo other)
    {
        bool uidEqual = this.uid == other.uid;
        bool portEqual = this.portraitUrl == other.portraitUrl;
        bool nickEqual = this.nickname == other.nickname;
        bool userNameEqual = this.username == other.username;
        bool bioEqual = this.bio == other.bio;
        bool birthEqual = this.birthday == other.birthday;
        bool genderEqual = this.gender == other.gender;
        bool avatarEqual = this.avatarJson == other.avatarJson;
        bool idleEqual = this.idleData == other.idleData;
        bool homePageSkinEqual = this.homepageSkin == other.homepageSkin;
        bool avatarFrameEqual = this.avatarFrame == other.avatarFrame;
        bool chatBubbleEqual = this.chatBubbles == other.chatBubbles;
        bool homePageOwnedEqual = this.ownedHomepageSkinList == other.ownedHomepageSkinList;
        bool avatarFrameOwnedEqual = this.ownedAvatarFrameList == other.ownedAvatarFrameList;
        bool chatBubbleOwnedEqual = this.ownedChatBubblesList == other.ownedChatBubblesList;
        bool creatorBadgeEqual = CreatorBadgeInfoDataEquals(this.creatorBadgeInfo, other.creatorBadgeInfo);

        bool isEqual = uidEqual && portEqual && nickEqual && userNameEqual && genderEqual && bioEqual && birthEqual && avatarEqual && idleEqual && homePageSkinEqual && avatarFrameEqual && chatBubbleEqual && homePageOwnedEqual && avatarFrameOwnedEqual && chatBubbleOwnedEqual && creatorBadgeEqual;
        // 只比较是否相等、不比较大小
        return isEqual ? 0 : -1;
    }

    private static bool CreatorBadgeInfoDataEquals(CreatorBadgeInfoData a, CreatorBadgeInfoData b)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }

        if (a == null || b == null)
        {
            return false;
        }

        return a.id == b.id && a.category == b.category && a.level == b.level && a.score == b.score && a.rank == b.rank;
    }

    [JsonIgnore]
    public CharacterData avatarInfo
    {
        get
        {
            if (string.IsNullOrEmpty(avatarJson))
            {
                return AvatarDataManager.Inst.GetDefaultDataByGender(1);
            }
            var tempData = CharacterData.DeserializeObject(avatarJson);
            return tempData;
        }
    }

    [JsonIgnore]
    public CharacterData otherAvatarInfo
    {
        get
        {
            var str = PlayerPrefs.GetString(GameConsts.EmoteOtherPlayerOcKey + uid, "");

            if (string.IsNullOrEmpty(str)) return avatarInfo;

            try
            {
                var tempData = CharacterData.DeserializeObject(str);
                return tempData;
            }
            catch
            {
                return avatarInfo;
            }
        }
    }

    public object Clone()
    {
        return this.MemberwiseClone();
    }
}

public class NpcUgcIdleData : UgcIdleData
{
    public string aniName;
    public override UgcIdleData Clone()
    {
        NpcUgcIdleData data = (NpcUgcIdleData)base.Clone();
        data.aniName = aniName;
        return data;
    }
}


public class UgcIdleData
{
    public string id;
    public string metaDataUrl;
    public List<AnimPropData> propList;  
    public string cover;
    
    public virtual UgcIdleData Clone()
    {
        UgcIdleData data = new UgcIdleData();
        data.id = id;
        data.metaDataUrl = metaDataUrl;
        data.cover = cover;
        data.propList = new List<AnimPropData>();
        data.propList.AddRange(propList);
        return data;
    }
}

public class IdleData
{
    public int animResType; //0:pgc 1:ugc
    public int personType; //0：各自交互 1：人和宠物交互 仅对UGC动画有效，PGC资源通过ID判断
    public string mainIdle;
    public List<string> subIdle;
    public List<UgcIdleData> ugcIdleList;
    public override bool Equals(object obj)
    {
        if (obj is IdleData)
        {
            var data = obj as IdleData;
            if (mainIdle != data.mainIdle) return false;
            if (animResType != data.animResType) return false;
            if (personType != data.personType) return false;
            if (subIdle == null && data.subIdle == null) return true;
            if (subIdle == null || data.subIdle == null) return false;
            if (ugcIdleList != data.ugcIdleList) return false;
            if (subIdle.Count != data.subIdle.Count) return false;
            for (int i = 0, C = subIdle.Count; i < C; i++)
            {
                if (subIdle[i] != data.subIdle[i]) return false;
            }

            return true;
        }
        return base.Equals(obj);
    }

    public IdleData Clone()
    {
        IdleData data = new IdleData();
        data.mainIdle = mainIdle;
        data.animResType = animResType;
        data.personType = personType;
        data.ugcIdleList = new List<UgcIdleData>();
        if(ugcIdleList != null) data.ugcIdleList.AddRange(ugcIdleList);
        data.subIdle = new();
        if (subIdle != null) data.subIdle.AddRange(subIdle);
        return data;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}

/// <summary>
/// 用户基本信息协议
/// </summary>
public interface UserInfoProtocol
{
    /// <summary>用户id</summary>
    public string uid { set; get; }

    /// <summary>头像</summary>
    public string portraitUrl { set; get; }

    /// <summary>昵称</summary>
    public string nickname { set; get; }

    /// <summary>用户名，6位唯一id</summary>
    public string username { set; get; }
}


public class CreatorLevelInfo
{
    public int titleType;
    public int levelType;
    public int point;
    public int level;
    public int nextLevelPoint;
    public long unlockTime;
}

public enum CreatorLevelType
{
    Unknown = 0,
    Level1 = 1, //1 萌新
    Level2 = 2, //2 新晋
    Level3 = 3, //3 逐光
    Level5 = 4, //4 人气
}