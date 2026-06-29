using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameData.Base;
using UnityEngine;

namespace GameData
{
    public enum ContestPageType
    {
        All = 1, // 所有作品
        MyEntry = 2, // 我的作品
        Top100 = 3, // top 100
        WinningEntries = 4, // 获奖作品
    }
    
    public enum ContestStatus
    {
        NotStart = 0,
        InProgress = 1,
        Completed = 2,
        Outdated = 3,
        Submission = 4,
    }
    
    public enum BUDContestType
    {
        Unknown = 0,
        Skin = 1, // 皮肤大赛
        OC = 2, // OC大赛
        Instrument = 3, // 乐器
        MusicScore = 4, // 乐谱
        Bundle = 5, // 皮肤套装创作
        PetSkin = 6, // 宠物皮肤
        PetOC = 7, // 宠物设子
        PetBundle = 8, // 宠物套装
        Vehicle = 9,//载具
        Camera = 11,//摄影大赛
    }
    
    [Serializable]
    public class ContestPrizeInfo
    {
        public string title;
        public string pgcId;
        public string desc;
        public int num;
        public string name;
        public int rewardType;

        // 老字端 待删除
        public int classifyType;
        public int level;
        public int currencyType;//1gem 2 coin 3 badge
        public int productType;
        public int source;
    }


    [Serializable]
    public class ContestInfo
    {
        public string contestId;
        public string bannerUrl;
        public string background;
        public string contestName;
        public int status; //0 未开始 1 进行中 2 已完成 3 已过期 同ContestStatus 代码别写1234了求求了
        public int contestType; // 1:皮肤创作大赛，2: OC大赛 3 乐器大赛 4 乐谱大赛
        public long startTime;
        public long endTime;
        public string leftTime;
        public long expireTime;
        public int hasLuckyDraw;
        public int isShowBanner;
        public string streamerUrl;
        public string rewardUrl;
        public List<ContestPrizeInfo> prizes;
        public List<string> templateIdList; // 可参加皮肤大赛活动的模版ID，不配置所有模版都可以参加
        public List<string> backgroundIconUrlList;
        public List<string> themeColorList;
        public string backgroundColor;
        public string rule;
        public int isAnimeItemOnly;
        //设子大赛
        public string backgroundUrl;
        public string posterImageUrl;
        public int changeTimes;
        public int voteTimes;
        public string lastOCContestId;
        public string fittingRoomIconUrl;

        /// <summary>
        /// 当前大赛类型
        /// </summary>
        public BUDContestType CurrentContestType
        {
            get
            {
                bool IsDefined = Enum.IsDefined(typeof(BUDContestType), contestType);
                if (!IsDefined)
                {
                    return BUDContestType.Unknown;
                }

                return (BUDContestType)contestType;
            }
        }

        public bool IsPetSkinContest
        {
            get
            {
                return CurrentContestType == BUDContestType.PetSkin;
            }
        }
        
        /// <summary>
        /// 当前模版能否参加皮肤大赛
        /// </summary>
        /// <param name="templateId"></param>
        /// <returns></returns>
        public bool IsCanJoinSkin(string templateId)
        {
            if (string.IsNullOrEmpty(templateId))
            {
                return false;
            }

            // 非创作大赛返回
            if (CurrentContestType != BUDContestType.Skin && CurrentContestType != BUDContestType.PetSkin) 
            {
                return false;
            }
            
            if (status != (int)ContestStatus.InProgress)
            {
                return false;
            }

            var templateIds = templateIdList;
            if (templateIds == null)
            {
                return true;
            }

            return templateIds.Contains(templateId);
        }
    }
    
    public class ContextReqData
    {
        public ContestInfo contestInfo;
    }
    
    public class JoinContestInfo
    {
        public string creationId;
        public List<string> idList;
    }
    public class ContestCreationInfo
    {
        public string creationId;
        public string cover;
        public int creationType; // 1:皮肤创作，2:oc 创作
        public long createTime;
        public string creationName;
        public PaymentInfo paymentInfo;
        
        /// <summary>
        /// 业务字段
        /// </summary>
        public string bgColor;
        public string themeColor;

        public int price
        {
            get
            {
                return paymentInfo?.price ?? 0;
            }
        }
    }
    
    public class ContestCreatorInfo
    {
        public string uid;
        public string nickname;
        public string portraitUrl;
        public string userName;
        public int avatarFrame;
    }
    
    public class ScoreInfo
    {
        public int rank;  // 作品排名
        public int score; // 作品分数
    }
    
    public class ContestEntryInfo
    {
        public ContestCreationInfo creationInfo;
        public ScoreInfo scoreInfo;
        public ContestCreatorInfo creator;
    }
    
    public class ContestEntryListReq
    {
        public string cookie { get; set; }
        public int IsEnd { get; set; }
        public int isEnd { get; set; } //后端的循环列表，目前有IsEnd 和 isEnd， 我都补上避免循环列表无限循环
        // public bool IsBEnd => IsEnd == 1 || isEnd == 1;
        public bool IsFirst { get; set; }
        public List<ContestEntryInfo> list;
    }
    public class ContestLuckyDrawRespData
    {
        public string contestId;
        public List<string> userNameList;
    }

    public class ContestRankListReq
    {
        public string cookie { get; set; }
        public int isEnd { get; set; }
        public List<ContestEntryInfo> list;
        public ContestEntryInfo userRankData;
    }
}
