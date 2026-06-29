using System;
using System.Collections.Generic;
using Basic.Utils;
using Es;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Product;
using UI.Manager;
using UnityEngine;
using Object = UnityEngine.Object;


public enum SeasonPassType {
    ErrSeasonPassType = 0,
    PurpleDreams = 1, //紫梦如初
    SummerWave = 2, //夏浪音乐节
    CutePetsOuting = 3, //萌宠郊游
    AnimationStudioSP = 4, //动画工作室
    MagicTrial = 5, //魔法试炼季
    NewYearLogin = 6, //跨年登录礼
    S6SeasonPass = 7, // s6通行证
    S7SeasonPass = 8, // S7 赛季通行证
    LoveLogin = 9, // S7 情人节小通行证
    S8SeasonPass = 10, // S8赛季通行证
    S9SeasonPass = 11,//S9通行证
    S9AbandonedHospitalSeasonPass = 12, //s9 废弃医院通行证
    LaborDay = 13, // 五一劳动节通行证
    S10SeasonPass = 14, //S10通行证
    S12SeasonPass = 15 ,//S12通行证
    HorseYearLogin = 16, //马年通行证
    S13SeasonPass = 17,//S13通行证
    S14SeasonPass = 18,//S14通行证
    S15SeasonPass = 19,//S15通行证
}



public class SeasonPassDataManager : GlobalInstance<SeasonPassDataManager>
{

    public SeasonPassType CurrentSeasonPassType = SeasonPassType.S15SeasonPass;
    

    public delegate void OnSeasonPassDataChange(SeasonPassType type, SeasonPassListRsp rsp);
    public event OnSeasonPassDataChange OnSeasonPassDataChangeHandler;

    public int PremiumSeasonPassGem = 300;

    public int LuxurySeasonPassGem = 600;

    private Dictionary<SeasonPassType, SeasonPassConfig> _seasonPassConfigs = new Dictionary<SeasonPassType, SeasonPassConfig>();
    private Dictionary<SeasonPassType, SeasonPassListRsp> _seasonPassListRsps = new Dictionary<SeasonPassType, SeasonPassListRsp>();
    private bool isClaiming = false;

    public List<PaidPackRewardData> GetSeasonPassRewardDatas(SeasonPassType type, bool isPaid)
    {
        List<PaidPackRewardData> paidPackRewardDatas = new List<PaidPackRewardData>();
        switch (type)
        {
            case SeasonPassType.S9SeasonPass:
                paidPackRewardDatas.Add(new PaidPackRewardData()
                {
                    pgcIds = new List<string>() { "11300345", "10700129", "10400469" },
                    name = "潜水蛙人套装",
                    isBundle = true,
                    bundleId = "75"
                });
                paidPackRewardDatas.Add(new PaidPackRewardData()
                {
                    pgcIds = new List<string>() { "40100473" },
                    name = "吹海螺",
                    isBundle = false,
                    bundleId = "998"
                });
                paidPackRewardDatas.Add(new PaidPackRewardData()
                {
                    pgcIds = new List<string>() { "10400470" },
                    name = "蜜桃泳衣",
                    isBundle = false,
                    bundleId = "998"
                });
                break;
            
            case SeasonPassType.S9AbandonedHospitalSeasonPass:
                if (isPaid) {
                paidPackRewardDatas.Add(new PaidPackRewardData()
                {
                    pgcIds = new List<string>()
                    {
                        "10100054",
                        "10400306",
                        "11000109",
                        "10900307",
                        "11300215",
                        "10200020"
                    },
                    name = "薰衣草苏琪套装",
                    isBundle = true,
                        bundleId = "81"
                    });
                }
                else 
                {
                    paidPackRewardDatas.Add(new PaidPackRewardData()
                    {
                        pgcIds = new List<string>() { "11000245" },
                        name = "医生针管",
                        isBundle = false,
                    });
                }
                break;
            case SeasonPassType.S12SeasonPass:
                if (isPaid)
                {
                    paidPackRewardDatas.Add(new PaidPackRewardData()
                    {
                        pgcIds = new List<string>()
                    {
                        "10400497",
                        "10900498",
                    },
                        name = "亚比雪人套装",
                        isBundle = true,
                        bundleId = "199"
                    });
                    paidPackRewardDatas.Add(new PaidPackRewardData()
                    {
                        pgcIds = new List<string>() { "10100116" },
                        name = "亚比雪人项圈",
                        isBundle = false,
                    });
                    paidPackRewardDatas.Add(new PaidPackRewardData()
                    {
                        pgcIds = new List<string>()
                        {
                        "40100528"
                        },
                        name = "喂食小鹿",
                        isBundle = false
                    });
                }
                else
                {
                    paidPackRewardDatas.Add(new PaidPackRewardData()
                    {
                        pgcIds = new List<string>() { "10100116" },
                        name = "亚比雪人项圈",
                        isBundle = false,
                    });
                }
                break;
            case SeasonPassType.S14SeasonPass:
                if (isPaid)
                {
                    paidPackRewardDatas.Add(new PaidPackRewardData()
                    {
                        pgcIds = new List<string>() { "10400508", "11300369" },
                        name = "焦糖布丁喵套装",
                        isBundle = true,
                        bundleId = "211"
                    });
                    paidPackRewardDatas.Add(new PaidPackRewardData()
                    {
                        pgcIds = new List<string>() { "10200070" },
                        name = "焦糖布丁喵尾巴",
                        isBundle = false,
                    });
                    paidPackRewardDatas.Add(new PaidPackRewardData()
                    {
                        pgcIds = new List<string>()  { "40100533" },
                        name = "吃蛋糕",
                        isBundle = false
                    });
                }
                else
                {
                    paidPackRewardDatas.Add(new PaidPackRewardData()
                    {
                        pgcIds = new List<string>() { "10200070" },
                        name = "焦糖布丁喵尾巴",
                        isBundle = false,
                    });
                }
                break;
            case SeasonPassType.S15SeasonPass:
                if (isPaid)
                {
                    paidPackRewardDatas.Add(new PaidPackRewardData()
                    {
                        pgcIds = new List<string>() { "10400513", "12100153","11400090" },
                        name = "天使小羊套装",
                        isBundle = true,
                        bundleId = "140"
                    });
                    paidPackRewardDatas.Add(new PaidPackRewardData()
                    {
                        pgcIds = new List<string>() { "10900514" },
                        name = "天使小羊头套",
                        isBundle = false,
                    });
                    paidPackRewardDatas.Add(new PaidPackRewardData()
                    {
                        pgcIds = new List<string>() { "40100567" },
                        name = "小羊步摇",
                        isBundle = false
                    });
                }
                else
                {
                    paidPackRewardDatas.Add(new PaidPackRewardData()
                    {
                        pgcIds = new List<string>() { "10900514" },
                        name = "天使小羊头套",
                        isBundle = false,
                    });
                }
                break;
        }

        return paidPackRewardDatas;
    }

    public SeasonPassConfig GetSeasonPassConfig(SeasonPassType type, GameObject gameObject)
    {
        if (!_seasonPassConfigs.ContainsKey(type))
        {
            //获取配置
            string enumName = Enum.GetName(typeof(SeasonPassType), type);
            var configPath = "Assets/Loadable/UI/UIPanel/SeasonPassPanel/" + enumName + "/SeasonPassConfig.json";
            var textAsset = Loader.Load<TextAsset>(configPath, gameObject);
            if (textAsset != null)
            {
                SeasonPassConfig config = JsonConvert.DeserializeObject<SeasonPassConfig>(textAsset.text);
                _seasonPassConfigs[type] = config;
            }
        }
        
        return _seasonPassConfigs[type];
    }

    public string GetPgcName(string pgcId)
    {
        return Es.DataTables.GetPgcNameData(pgcId)?.Name;
    }

    public string GetSeasonPassRewardAvatarName(string pgcId)
    {
        if (pgcId == "11300345")
        {
            return "潜水蛙人套装";
        }
        else if (pgcId == "10100054")
        {
            return "薰衣草苏琪套装";
        }
        else if (pgcId == "11300350")
        {
            return "奶芙熊熊套装";
        }
        else if (pgcId == "40100494")
        {
            return "画家装扮";
        }
        else if (pgcId == "10400497")
        {
            return "亚比雪人套装";
        }
        else if (pgcId == "10900498")
        {
            return "亚比雪人套装";
        }
        else if (pgcId == "40100494")
        {
            return "喂食小鹿";
        }
        else if(pgcId == "10400508" || pgcId == "11300369")
        {
            return "焦糖布丁喵套装";
        }
        else if (pgcId == "40100533")
        {
            return "吃蛋糕";
        }
        else if (pgcId == "10400513" || pgcId == "12100153" || pgcId == "11400090")
        {
            return "天使小羊套装";
        }
        else if (pgcId == "10900514")
        {
            return "天使小羊头套";
        }

        return "通行证随机礼包";
    }
    
    public Dictionary<string, string> GetSeasonPassPgcNames(SeasonPassType type)
    {
        switch (type)
        {
            case SeasonPassType.S9AbandonedHospitalSeasonPass:
                return new Dictionary<string, string>()
                {
                    { "10900318", "金喜新年礼帽" },
                    { "10500035", "金喜新年头饰" },
                    { "10900056", "小恶魔头饰" },
                    { "40300436", "送熊熊" }
                };
                break;

            default:
            case SeasonPassType.S9SeasonPass:
                return new Dictionary<string, string>()
                {
                    { "10900318", "金喜新年礼帽" },
                    { "10500035", "金喜新年头饰" },
                    { "10900056", "小恶魔头饰" },
                    { "40300436", "送熊熊" }
                };
                break;
        }
    }

    public SeasonPassListRsp GetSeasonPassList(SeasonPassType type)
    {
        if (_seasonPassListRsps.ContainsKey(type))
        {
            return _seasonPassListRsps[type];
        }

        return null;
    }

    public SeasonPassListRsp GetCurSeasonData()
    {
        return GetSeasonPassList(CurrentSeasonPassType);
    }

    public string GetSeasonPassName(int giftType)
    {
 
        switch (giftType)
        {
            case (int)GiftType.LaborDay:
                Debug.LogError(Enum.GetName(typeof(SeasonPassType), SeasonPassType.LaborDay));
                return Enum.GetName(typeof(SeasonPassType), SeasonPassType.LaborDay);
                break;
            default:
                return GetSeasonPassName(CurrentSeasonPassType);

        }
    }
    public string GetSeasonPassName(SeasonPassType type)
    {
        return Enum.GetName(typeof(SeasonPassType), type);
    }

    public bool GetSeasonPassIsPaid(SeasonPassType type = SeasonPassType.S9SeasonPass)
    {
        if (_seasonPassListRsps.ContainsKey(type))
        {
            return _seasonPassListRsps[type].isPaid == 1;
        }

        return false;
    }

    public int GetSeasonPassPaidType(SeasonPassType type = SeasonPassType.S9SeasonPass)
    {
        /// 0解锁高级通行证 1 解锁豪华通行证
        if (_seasonPassListRsps.ContainsKey(type))
        {
            return _seasonPassListRsps[type].paidType;
        }

        return 0;
    }
    
    public void GetSeasonPassLists(Action<bool, SeasonPassListRsp> resultAction = null)
    {
        GetSeasonPassList(CurrentSeasonPassType, resultAction);
    }

    public void ReqCurSeasonData(Action<bool, SeasonPassListRsp> resultAction) {
        GetSeasonPassList(CurrentSeasonPassType, resultAction);
    }

    public void GetSeasonPassList(SeasonPassType type, Action<bool, SeasonPassListRsp> resultAction)
    {
        //var tem = new SeasonPassListRsp();
        //tem.progressInfo = new ProgressInfo();
        //tem.rewardList = new List<SeasonPassItemInfo>();
        //tem.paidRewardList = new List<SeasonPassItemInfo>();
        //var v = new SeasonPassItemInfo();
        //v.rewardId = "1";
        //v.rewardInfo = new SeasonPassRewardInfo();
        //v.rewardInfo.rewardType = 1;
        //v.rewardInfo.itemList = new List<SeasonPassRewardData>();
        //v.rewardInfo.itemList.Add(new SeasonPassRewardData() { rewardType = 1,amount = 10, });
        //for (int i = 0; i < 100; i++)
        //{
        //    tem.rewardList.Add(v);
        //    tem.paidRewardList.Add(v);
        //}
        //tem.progressInfo.start = 0;
        //tem.progressInfo.end = 100;
        //tem.progressInfo.currentTier = 0;
        //tem.isPaid = 0;
        //tem.paidType = 0;
        //_seasonPassListRsps[type] = tem;
        //
        //if (_seasonPassListRsps.ContainsKey(type))
        //{
        //    resultAction?.Invoke(true, _seasonPassListRsps[type]);
        //}
        //return;
        
        var req = new JObject()
        {
            ["seasonPassType"] = (int)type
        };

        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.SeasonPassInfo,
            HttpMethod.GET,
            JsonConvert.SerializeObject(req),
            onReceive: msg =>
            {
                SeasonPassListRsp seasonPassRsp = JsonConvert.DeserializeObject<SeasonPassListRsp>(msg);
                // GameUtils.WriteJsonToFile(msg);
                if (seasonPassRsp == null)
                {
                    LoggerUtils.LogError("OnGetSeasonPassDetailSuccess seasonPassRsp is null");
                    return;
                }
                _seasonPassListRsps[type] = seasonPassRsp;
                OnSeasonPassDataChangeHandler?.Invoke(type, seasonPassRsp);
                resultAction?.Invoke(true, seasonPassRsp);
            }, onFail: arg0 =>
            {
                resultAction?.Invoke(false, null);
            }, retryCount:2);
    }

    public void Claim(SeasonPassType type, SeasonPassItemInfo data, Action<bool, SeasonPassClaimRsp> resultAction = null)
    {
        if (isClaiming)
        {
            return;
        }

        isClaiming = true;
        var req = new JObject()
        {
            ["rewardId"] = data.rewardId,
            ["seasonPassType"] = (int)type
        };

        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.SeasonPassClaim,
            HttpMethod.POST,
            JsonConvert.SerializeObject(req),
            onReceive: msg =>
            {
                var rsp = JsonConvert.DeserializeObject<SeasonPassClaimRsp>(msg);
                resultAction?.Invoke(true, rsp);
                isClaiming = false;
            }, onFail: arg0 =>
            {
                resultAction?.Invoke(false, null);
                isClaiming = false;
            });
    }

    public void ClaimAll(SeasonPassType type, Action<bool, SeasonPassClaimRsp> resultAction = null)
    {
        if (isClaiming)
        {
            return;
        }

        isClaiming = true;
        var req = new JObject()
        {
            ["seasonPassType"] = (int)type,
            ["isAll"] = 1
        };

        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.SeasonPassClaim,
            HttpMethod.POST,
            JsonConvert.SerializeObject(req),
            onReceive: msg =>
            {
                var rsp = JsonConvert.DeserializeObject<SeasonPassClaimRsp>(msg);
                resultAction?.Invoke(true, rsp);
                isClaiming = false;
            }, onFail: arg0 =>
            {
                resultAction?.Invoke(false, null);
                isClaiming = false;
            });
    }

    public int GetCurDay(List<SeasonPassItemInfo> rewardInfos)
    {
        int curDay = 0;
        if (rewardInfos == null)
        {
            return curDay;
        }

        foreach (var info in rewardInfos)
        {
            if (info.BudRewardStatus != BudRewardStatus.Lock)
                curDay++;
        }
        curDay = Math.Max(1, curDay);
        curDay = Math.Min(curDay, 50);
        return curDay;
    }

    public bool FinishAllProgress(List<SeasonPassItemInfo> rewardInfos)
    {
        if (rewardInfos == null)
            return false;
        return rewardInfos.Count == GetCurDay(rewardInfos);
    }

    public List<CommonRewardItemData> OnPaidSeasonPassAndGetRewardData(SeasonPassType seasonPassType, bool isPaid, GameObject gameObject)
    {
        var rewardList = new List<CommonRewardItemData>();
        if (isPaid)
        {
            switch (seasonPassType)
            {
                case SeasonPassType.S15SeasonPass:
                case SeasonPassType.S14SeasonPass:
                case SeasonPassType.S9SeasonPass:
                    rewardList.Add(new CommonRewardItemData()
                    {
                        IconSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(SpriteAtlasType.Common, "SlotIcon", gameObject),
                        RewardAmount = 1,
                        rewardName = "皮肤设子位"
                    });
                    rewardList.Add(new CommonRewardItemData()
                    {
                        IconSp = PgcUtils.LoadRewardIcon(BUDRewardType.RewardYouYouCoin, gameObject),
                        RewardAmount = 15,
                        rewardName = "优优币"
                    });
                    rewardList.Add(new CommonRewardItemData()
                    {
                        IconSp = PgcUtils.LoadRewardIcon(BUDRewardType.RewardExperience, gameObject),
                        RewardAmount = 1000,
                        rewardName = "通行证经验"
                    });
                    break;
                case SeasonPassType.S9AbandonedHospitalSeasonPass:
                    rewardList.Add(new CommonRewardItemData()
                    {
                        IconSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(SpriteAtlasType.Common, "SlotIcon", gameObject),
                        RewardAmount = 2,
                        rewardName = "皮肤设子位"
                    });
                    rewardList.Add(new CommonRewardItemData()
                    {
                        IconSp = PgcUtils.LoadRewardIcon(BUDRewardType.RewardAICore, gameObject),
                        RewardAmount = 68,
                        rewardName = "AI核心"
                    });
                    rewardList.Add(new CommonRewardItemData()
                    {
                        IconSp = PgcUtils.LoadRewardIcon(BUDRewardType.AbandonedRewardExperience, gameObject),
                        RewardAmount = 1000,
                        rewardName = "通行证经验"
                    });
                    break;
            }
        }
        else
        {
            switch (seasonPassType)
            {
                case SeasonPassType.S15SeasonPass:
                case SeasonPassType.S14SeasonPass:
                case SeasonPassType.S9SeasonPass:
                    rewardList.Add(new CommonRewardItemData()
                    {
                        IconSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(SpriteAtlasType.Common, "SlotIcon", gameObject),
                        RewardAmount = 2,
                        rewardName = "皮肤设子位"
                    });
                    rewardList.Add(new CommonRewardItemData()
                    {
                        IconSp = PgcUtils.LoadRewardIcon(BUDRewardType.RewardYouYouCoin, gameObject),
                        RewardAmount = 15,
                        rewardName = "优优币"
                    });
                    rewardList.Add(new CommonRewardItemData()
                    {
                        IconSp = PgcUtils.LoadRewardIcon(BUDRewardType.RewardExperience, gameObject),
                        RewardAmount = 1000,
                        rewardName = "通行证经验"
                    });
                    break;
                case SeasonPassType.S9AbandonedHospitalSeasonPass:
                    rewardList.Add(new CommonRewardItemData()
                    {
                        IconSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(SpriteAtlasType.Common, "SlotIcon", gameObject),
                        RewardAmount = 2,
                        rewardName = "皮肤设子位"
                    });
                    rewardList.Add(new CommonRewardItemData()
                    {
                        IconSp = PgcUtils.LoadRewardIcon(BUDRewardType.RewardAICore, gameObject),
                        RewardAmount = 68,
                        rewardName = "AI核心"
                    });
                    rewardList.Add(new CommonRewardItemData()
                    {
                        IconSp = PgcUtils.LoadRewardIcon(BUDRewardType.AbandonedRewardExperience, gameObject),
                        RewardAmount = 1000,
                        rewardName = "通行证经验"
                    });
                    break;
            }
        }

        return rewardList;
    }
}
