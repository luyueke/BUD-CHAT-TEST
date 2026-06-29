using System.Collections.Generic;
using System.Linq;
using Es;
using Game.Database;
using UnityEngine;


/// <summary>
/// 领取类型，对应后端状态
/// </summary>
public enum BudRewardStatus {
    ErrRewardStatus = 0,
    Lock = 1,
    Unlocked = 2,
    Claimed = 3,
}

public enum SeasonLobbyStatus
{
    ErrLobbyStatus = 0,
    Claimable = 1,
    UnClaimable = 2,
    Offline = 3,
}

public class SeasonPassListRsp
{
    public List<SeasonPassItemInfo> rewardList;
    public List<SeasonPassItemInfo> paidRewardList;

    public ProgressInfo progressInfo;

    public int currentTier;
    /// <summary>
    /// 是否解锁高级
    /// </summary>
    public int isPaid;

    /// <summary>
    /// 0 高级 1 豪华
    /// </summary>
    public int paidType;

    /// <summary>
    /// 结束时间
    /// </summary>
    public string endTime;

    public int lobbyStatus;

    /// <summary>
    /// 今日在线时长
    /// </summary>
    public int todaySyncTime;
  
    /// <summary>
    /// 赛季第一周
    /// </summary>
    public int currentWeek;

    public int ShowPrePaidRewardPopup;//是否展示通行证购买奖励结算弹窗

    public SeasonLobbyStatus seasonLobbyStatus
    {
        get
        {
            return (SeasonLobbyStatus)lobbyStatus;
        }
    }
}

public class SeasonPassRewardData
{
    public int rewardType;
    public int amount;
    public string pgcId;
    public int isReplaced;
    public List<string> pgcIdList;

    public BUDRewardType BudRewardType
    {
        get
        {
            return (BUDRewardType)rewardType;
        }
    }


    public string name;
    public Sprite IconSp;
}

public class SeasonPassRewardInfo
{
    public int rewardType;
    public List<SeasonPassRewardData> itemList;
    public List<SeasonPassRewardData> replaceRewardList;
}


public class SeasonPassItemInfo
{
    public string rewardId;
    public SeasonPassRewardInfo rewardInfo;
    public int rewardStatus = 0;

    public BUDRewardType BudRewardType
    {
        get
        {
            var rewardType = rewardInfo?.rewardType ?? 0;
            return (BUDRewardType)rewardType;
        }
    }

    public BudRewardStatus BudRewardStatus
    {
        get
        {
            return (BudRewardStatus)rewardStatus;
        }
    }

    public int RewardAmount()
    {
        var amount = 0;
        if (rewardInfo?.itemList?.Count > 0)
        {
            amount = rewardInfo.itemList.First().amount;
        }

        return amount;
    }

    public string IconName()
    {
        {
            var amount = 0;
            if (rewardInfo?.itemList?.Count > 0)
            {
                amount = rewardInfo.itemList.First().amount;
            }


            switch (BudRewardType)
            {
                case BUDRewardType.RewardAvatarFrame:
                    return "head_1";
                case BUDRewardType.ErrRewardType:
                    return null;
                case BUDRewardType.RewardBadge:
                    if (amount <= 200)
                    {
                        return "icon_7";
                    }
                    return "icon_9";
                case BUDRewardType.RewardCoin:
                    if (amount < 200)
                    {
                        return "icon_3";
                    } else if (amount >= 200 && amount < 800)
                    {
                        return "icon_4";
                    } else if (amount >= 800 && amount < 2000)
                    {
                        return "icon_10";
                    }
                    return "icon_11";
                case BUDRewardType.RewardGem:
                    return "icon_5";
                case BUDRewardType.RewardPgcResource:
                    var pgcId = rewardInfo?.itemList?.First()?.pgcIdList?.First() ?? "";
                    if (pgcId == "10400508" || pgcId == "11300369")
                    {
                        return "icon_ma";
                    }
                    if (pgcId == "10400513" || pgcId == "12100153" || pgcId == "11400090")
                    {
                        return "icon_ma1";
                    }
                    //if (pgcId == "10900498")
                    //{
                    //    return "icon_pgc";
                    //}
                    if(pgcId == "10200070")
                    {
                        return "icon_mw";
                    }
                    if(pgcId == "10900514")
                    {
                        return "icon_mw1";
                    }
                    if (pgcId == "40100533")
                    {
                        return "icon_paopao";
                    }
                    if (pgcId == "40100567")
                    {
                        return "icon_paopao1";
                    }
                    if (pgcId == "10100116")
                    {
                        return "icon_yabi2";
                    }
                    if (pgcId == "40100528")
                    {
                        return "icon_xiaolu";
                    }
                    if (pgcId == "10900486")
                    {
                        return "icon_pgc1";
                    }
                    if (pgcId == "40100494")
                    {
                        return "icon_pgc_emote";
                    }
                    if (string.IsNullOrEmpty(pgcId))
                    {
                        return "icon_ma";
                    }
                    if (pgcId == "10900477")
                    {
                        return "icon_dogH";
                    }

                    // 小恶魔头饰
                    if (pgcId == "40100494") {
                        return "headdress_39";
                    }

                    if (pgcId == "11000220")
                    {
                        return "bracelet_01_03";
                    }

                    if (pgcId == "10900439")
                    {
                        return "icon_loveLogin_pgc";
					}
                    if (pgcId == "11000140")
                    {
                        return "bracelet_286_01";
                    }
                    if (pgcId == "10100054")
                    {
                        return "S9Abandoned_bundle";
                    }
                    if (pgcId == "11000245")
                    {
                        return "bracelet_500_01";
                    }
                    return "icon_ma";
                case BUDRewardType.RewardGashaponVoucher:
                    return "icon_30";
                case BUDRewardType.RewardPinkCoin:
                    return "icon_31";
                case BUDRewardType.RewardYouYouCoin:
                    return "icon_32";
                case BUDRewardType.RewardLuckyCoin:
                    return "icon_22";
                case BUDRewardType.RewardSeasonPassRandomPack:
                    return "icon_36";
                case BUDRewardType.RewardPurpleDreamCoin:
                    return "icon_41";
                case BUDRewardType.RewardAICore:
                    return "icon_43";
                case BUDRewardType.RewardAISeasonCoin:
                    return "icon_44";
                case BUDRewardType.RewardSeasonPassCoin:
                    return "icon_45";
            }

            return null;
        }
    }
   

    public bool IsPgcCloth()
    {
        if (BudRewardType != BUDRewardType.RewardPgcResource)
        {
            return false;
        }

        var pgcId = rewardInfo?.itemList?.First()?.pgcIdList?.First() ?? "";
        if (string.IsNullOrEmpty(pgcId))
        {
            return false;
        }
        GameResData resData = Es.DataTables.GetGameResData(pgcId);
        return resData.ResourceType == (int)GameData.PgcData.ResourceType.Avatar || resData.ResourceType == (int)GameData.PgcData.ResourceType.PGCPetAvatar || resData.ResourceType == (int)GameData.PgcData.ResourceType.Emote;
     }
}

public class ProgressInfo
{
    public int currentTier;
    public int start;
    public int end;
}


public class SeasonPassClaimRsp
{
    public List<SeasonPassRewardData> rewardList;
    public ServerBagUpdateData backpackData;
    public List<GameData.Rewards.RewardInfo> replaceRewardList;
}


