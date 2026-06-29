using Basic.Utils;
using Es;
using Game.Database;
using Game.Store;
using GameData.Gashapon;
using GameData.Rewards;
using GameUI;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using UI.Manager;
using UnityEngine;

namespace UI.UIPanels.GashaponPanel
{
    /// <summary>
    /// 枚举值和配置表GashaponViewConfig-ViewId对应
    /// </summary>
    public enum GashaponType
    {
        Unknown = 0,
        PromiseOfBud = 1,
        PinkDream = 2,
        BirthdayParty = 3,
        Gyaru = 4,
        PurpleDream = 5,
        MusicFestival = 6,
        MaidMinty = 8,
        MochiMeow = 9,
        Y2kBunny = 10,
        RockAndRebel = 11,
        BudUniversity = 12,
        Pet = 13,
        PuffyBear = 14,
        ManekiNeko = 15,
        Vetreska = 16,
        Halloween = 17,
        CottageCore = 18,
        AnimalCarnival = 19,
        VibrantTrendy = 21,//活力潮流
        Hanabi = 22,//火花盛宴
        BlueStars = 23,//牛仔时尚
        MysteriousPrincess = 24,//灵舞部落
        DancingPuppet = 25,
        ConsumeEvent = 26, //消费狂欢节，不出现在商城
        GardenParty = 27,//S5 金币盲盒
        BudChronicles = 28,//S5 徽章盲盒
        PastelGirls = 29,//S5 创作者盲盒
        SnowElf = 30,//邂逅雪精灵
        JingleBells = 31,//圣诞响叮当
        WitchLuna = 32, // 幽喵露娜
        DemonRaven = 33, // 鸦魔瑞文
        FantasyBunny = 34, // 灵兔邦尼
        RadiantApollo = 35, // 耀光阿波罗
        LuckyStar = 39, // 新年幸运星
        Rainbow = 40,
        Coin = 41, //金币扭蛋
        NewCottageCore = 42, //翠野田园
        SnowDream = 43, //冰雪绮梦季
        WindKeeper = 36, // 风暴守护者
        Circus = 38, // 小丑
        SweetheartBall = 37, // 甜心
        WelcomeSpring = 44, // 锦时迎春季
        GodOfWealth = 45,//喜迎财神
        NewYearDrama = 46,//戏韵贺岁
        FrostKeeper = 49,//霜寒守护者
        HeavenlyMatch = 48, // 佳偶天成
        StarryNightFairyTale = 52, //星夜童话 
        CoralSeaEngagement = 53, //珊瑚海之约
        DarkShadowFeather = 54, //暗影冥羽
        FlowerCarriage = 55, //粉韵花辇
        ZongXiaFuYao = 56, //粽夏芙瑶
        MagicBoomBoomBoom = 57, //粽夏芙瑶
        FriesFun = 59,//薯条
        MidAutumn = 60, //中秋
        MusicalPuddingSkateboard = 61,//音符滑雪板
        MusicalNoteSuit = 62,//音符套装
        VioletGashapon = 64,//堇色暗香令
        Y2kArcticFever = 66,//极冻狂潮

        PonyGashapon = 67,//小马扭蛋
        UMIMusicalPudding = 69,//Umi扭蛋
        YuanxiaoJiejieling = 70,//元宵接接乐
        GashaponSweetmeat = 71,//甜品师日记
        BabyShrimpHuhu = 76,   //虾虾崽-huhu
        BabyShrimpWuwu = 77,   //虾虾崽-wuwu
        BabyShrimpRabbit = 78, //虾虾崽-兔云之座
        BabyShrimpSuit = 79, //虾虾崽-兔云之座

        ZZZPhantomParty = 81, //啧啧幻音派对

    }

    public enum GashaponCustomType
    {
        Normal = 0,
        Custom = 1,//活动扭蛋等自定义扭蛋
        Group = 2,//扭蛋组合
    }

    public class GashaponTaskRewardRsp
    {
        public List<CommonRewardData> rewardList;
        public ServerBagUpdateData backpackData;
        public List<GashaponExtraTaskInfo> taskList;
    }



    public class GashaponDataManager : GlobalInstance<GashaponDataManager>
    {
        public List<GashaponSerialInfo> AllGashaponSerialInfos = new List<GashaponSerialInfo>(); // 全部扭蛋系列配置
        private bool isTwisting = false;
        public Action RewardAc;

        public int newYearLuckyStar_show;
        public string pendingSubLotteryId;
        public override void Release()
        {
            base.Release();
            AllGashaponSerialInfos.Clear();
        }

        private GashaponStoreHandler dataHandler = AssetsDataManager.GetData<GashaponStoreHandler>();

        /// <summary>
        /// 当前时间是否为万圣节期间。 10月9日11点 - 11月4日11点
        /// </summary>
        public bool IsInHalloween
        {
            get
            {
                return LobbyInfoManager.Inst.LobbyInfo?.gameTheme == GameTheme.Halloween;
            }
        }

        public GashaponData gashaponData(String gashaId)
        {
            if (string.IsNullOrEmpty(gashaId))
            {
                return null;
            }
            return dataHandler.GetGashaponData(gashaId);
        }


        public GashaponViewConfig GetGashaponViewCfg(GashaponType viewId)
        {
            if (viewId == GashaponType.Unknown)
            {
                return null;
            }
            return DataTables.GetGashaponViewConfig((int)viewId);
        }

        public GashaponViewConfig GetGashaponView(String gashaId)
        {
            if (gashaId == "lottery.musicalPudding.dreamingGhost" || gashaId == "lottery.musicalPudding.kumoGhost" || gashaId == "lottery.musicalPudding.kuroGhost")
            {
                gashaId = "lottery.musicalNoteSuit";
            }

            if(gashaId == "lottery.musicalPudding.prank" || gashaId == "lottery.musicalPudding.umi" || gashaId == "lottery.musicalPudding.didu")
            {
                gashaId = "lottery.musicalPuddingSuit";
            }

            if (gashaId == "lottery.violetFragrance" || gashaId == "lottery.nightButterflyDream")
            {
                gashaId = "lottery.VioletGashapon";
            }

            if (gashaId == "lottery.polkaDotBerry" || gashaId == "lottery.seaSaltRabbit")
            {
                gashaId = "lottery.PolkaDotBerry";
            }

            if (gashaId == "lottery.wawaKindergarten.pinkTail" || gashaId == "lottery.wawaKindergarten.warmOrange" || gashaId == "lottery.wawaKindergartenSuit")
            {
                gashaId = "lottery.wawaKindergartenSuit";
            }

            if(gashaId == "lottery.whiteDewDrowningStar" || gashaId == "lottery.orangeRainBubbles")
            {
                gashaId = "lottery.whiteDewDrowningStar";
            }

            if (gashaId == "lottery.babyShrimp.huhu" || gashaId == "lottery.babyShrimp.wuwu" || gashaId == "lottery.babyShrimp.rabbit" || gashaId == "lottery.babyShrimpSuit")
            {
                gashaId = "lottery.babyShrimpSuit";
            }

            if (string.IsNullOrEmpty(gashaId))
            {
                return null;
            }
            var allList = DataTables.GetGashaponViewConfigList();
            var result = allList.Find(x => x.GashaId == gashaId);
            if (result == null)
            {
                LoggerUtils.LogError($"[Gashapon] Check {gashaId} config");
            }

            return result;
        }

        public List<RewardInfo> GetCurRewardPoolData(string gashaId)
        {
            if (AllGashaponSerialInfos != null && AllGashaponSerialInfos.Count > 0)
            {
                foreach (var sInfo in AllGashaponSerialInfos)
                {
                    if (sInfo.gashaponInfo.gashaponId.ToString() == gashaId)
                    {
                        return sInfo.rewardInfo;
                    }
                }
            }

            return null;
        }

        #region Data

        public void RequestGashapon(string gashaId, int times, Action<GashaponRsp> cb = null, int cardId = 0)
        {
            if (isTwisting)
            {
                TipPanel.ShowToast("点击太快了，请稍后");
                return;
            }

            LoggerUtils.Log($"RequestGashapon: gashaId:{gashaId}, times:{times}, cardId:{cardId}");

            try
            {
                GashaponReq req = new GashaponReq()
                {
                    lotteryId = gashaId,
                    times = times,
                    cardId = cardId
                };
                isTwisting = true;
                NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.toGasha, HttpMethod.POST, JsonConvert.SerializeObject(req), (response) =>
                {
                    isTwisting = false;
                    AccountDataManager.Inst.BalanceInfo.Refresh();
                    GashaponRsp rsp = JsonConvert.DeserializeObject<GashaponRsp>(response);
                    cb?.Invoke(rsp);
                    var t = rsp.popupType;
                    RewardAc = () =>
                    {
                        TimeLimitGiftSystem.Inst.SetTriggerTime(t);
                    };
                }, (fail) =>
                {
                    isTwisting = false;
                    LoggerUtils.LogError("扭蛋抽奖失败 ：" + fail);
                    cb?.Invoke(null);
                });
            }
            catch (Exception e)
            {
                LoggerUtils.LogError("扭蛋抽奖失败 ：" + e.StackTrace);
                isTwisting = false;
            }
        }




        public void RequestGashaponTask(string gashaId, int times, Action<GashaponRsp> cb = null)
        {
            if (isTwisting)
            {
                TipPanel.ShowToast("点击太快了，请稍后");
                return;
            }

            LoggerUtils.Log($"RequestGashapon: gashaId:{gashaId}, times:{times}");

            try
            {
                TaskGashaponReq req = new TaskGashaponReq()
                {
                    activityId = gashaId,
                    times = times
                };
                isTwisting = true;
                NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.TaskGacha, HttpMethod.POST, JsonConvert.SerializeObject(req), (response) =>
                {
                    isTwisting = false;
                    AccountDataManager.Inst.BalanceInfo.Refresh();
                    GashaponRsp rsp = JsonConvert.DeserializeObject<GashaponRsp>(response);
                    cb?.Invoke(rsp);
                }, (fail) =>
                {
                    isTwisting = false;
                    LoggerUtils.LogError("扭蛋抽奖失败 ：" + fail);
                });
            }
            catch (Exception e)
            {
                LoggerUtils.LogError("扭蛋抽奖失败 ：" + e.StackTrace);
                isTwisting = false;
            }
        }


        public void RequestGashaponInfo(string gashaId,Action<GashaponInfoRsp>onSuccess, Action<string>onFail = null)
        {
            if (string.IsNullOrEmpty(gashaId))
            {
                return;
            }

            if(gashaId == "lottery.musicalNoteSuit" || gashaId == "lottery.VioletGashapon" || gashaId == "lottery.musicalPuddingSuit" || gashaId == "lottery.PolkaDotBerry"
                || gashaId == "lottery.wawaKindergartenSuit" || gashaId == "lottery.babyShrimpSuit")
            {
                return;
            }

            JObject req = new JObject()
            {
                ["lotteryId"] = gashaId
            };
            var _gashaId = gashaId;
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.gashaInfo,
                HttpMethod.GET,
                JsonConvert.SerializeObject(req),
                (response) =>
                {
                    GashaponInfoRsp rsp = JsonConvert.DeserializeObject<GashaponInfoRsp>(response);
                    onSuccess?.Invoke(rsp);
                    if (_gashaId == "lottery.newYearLuckyStar")
                    {
                        var luckyStarExtraData = JsonConvert.DeserializeObject<LuckyStarExtraData>(rsp.extraData);
                        if (luckyStarExtraData != null)
                        {
                            newYearLuckyStar_show = luckyStarExtraData.randomLuckyStarNum;
                        }
                    }
                }, (fail) =>
                {
                    LoggerUtils.Log("扭蛋信息拉取失败 ：" + fail);
                    onFail?.Invoke(fail);
                }, retryCount: 3);
        }


        public void RequestClaimTaskReward(string gashaId,int eventId,Action<GashaponTaskRewardRsp>onSuccess, Action<string>onFail = null)
        {
            if (string.IsNullOrEmpty(gashaId))
            {
                return;
            }

            JObject req = new JObject()
            {
                ["lotteryId"] = gashaId,
                ["eventId"] = eventId,
            };

            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.GashaponTaskReward,
                HttpMethod.POST,
                JsonConvert.SerializeObject(req),
                (response) =>
                {
                    GashaponTaskRewardRsp rsp = JsonConvert.DeserializeObject<GashaponTaskRewardRsp>(response);
                    onSuccess?.Invoke(rsp);

                }, (fail) =>
                {
                    LoggerUtils.Log("扭蛋任务领奖失败 ：" + fail);
                    HttpResponseRawData rsp= JsonConvert.DeserializeObject<HttpResponseRawData>(fail);
                    if (rsp != null && !string.IsNullOrEmpty(rsp.rmsg))
                    {
                        TipPanel.ShowToast(rsp.rmsg);
                        onFail?.Invoke(rsp.rmsg);
                    }
                    else
                    {
                        onFail?.Invoke(fail);
                    }
                });
        }
        #endregion



        //判断是否需要预处理
        public bool NeedToDealData(List<GashaponRewardData> srcDataList)
        {
            if (srcDataList == null || srcDataList.Count <= 0) return false;
            bool result = false;
            foreach (var rewardData in srcDataList)
            {
                if (!string.IsNullOrEmpty(rewardData.BundleId))
                {
                    result = true;
                    break;
                }
            }

            return result;
        }

        public void ShowCurrencyNoEnough(int currencyType,int price)
        {
            CurrencyType bType = (CurrencyType)currencyType;
            int count = AccountDataManager.Inst.BalanceInfo.GetAccountCount(bType);
            int needNum = price - count;
            switch (currencyType)
            {
                case (int)CurrencyType.Coin:
                    ExchangeCoinPanel exchangeCoinPanel = UIManager.Inst.OpenPanel<ExchangeCoinPanel>(PanelId.ExchangeCoinPanel);
                    exchangeCoinPanel.SetData(bType, CurrencyType.Gem,needNum);
                    break;
                case (int)CurrencyType.LuckyCoin:
                case (int)CurrencyType.ChristmasCoin:
                case (int)CurrencyType.MagicCoin:
                case (int)CurrencyType.PurpleDreamCoin:
                case (int)CurrencyType.YouYouCoin:
                case (int)CurrencyType.ShrimpYuan:
                case (int)CurrencyType.ZZZCoin:
                    ExchangeCoinPanel customCoinPanel = UIManager.Inst.OpenPanel<ExchangeCoinPanel>(PanelId.ExchangeCoinPanel);
                    customCoinPanel.SetData(bType, CurrencyType.Gem, needNum);
                    break;
                case (int)CurrencyType.Badge:
                    ExchangeCoinPanel badgePanel = UIManager.Inst.OpenPanel<ExchangeCoinPanel>(PanelId.ExchangeCoinPanel);
                    badgePanel.SetData(CurrencyType.Badge, CurrencyType.Gem,needNum);
                    break;
                case (int)CurrencyType.Gem:
                    UIManager.Inst.OpenPanel(PanelId.GetMoreGemsPanel, needNum);
                    break;
                case (int)CurrencyType.KoiGachaCoin:
                    TipPanel.ShowToast("抽奖券不足");
                    break;
            }
        }


        //判断是否需要预处理
        public bool NeedToDealData(List<GashaponExchangeData> srcDataList)
        {
            if (srcDataList == null || srcDataList.Count <= 0) return false;
            bool result = false;
            foreach (var rewardData in srcDataList)
            {
                if (!string.IsNullOrEmpty(rewardData.BundleId))
                {
                    result = true;
                    break;
                }
            }

            return result;
        }

        public GashaponRewardData CreateBundleRewardData(List<GashaponRewardData> rewardList)
        {
            rewardList.Reverse();
            var firstData = rewardList[0];
            GashaponRewardData rewardData = new GashaponRewardData();
            rewardData.Id = firstData.Id;
            rewardData.Level = firstData.Level;
            rewardData.RewardType = firstData.RewardType;
            rewardData.BundleId = firstData.BundleId;
            rewardData.Num = 1;
            rewardData.PgcDatas = rewardList.SelectMany(data => data.PgcDatas).ToList();
            rewardData.RewardId = firstData.RewardId;
            return rewardData;
        }


        public GashaponExchangeData CreateBundleExchangeData(List<GashaponExchangeData> srcDataList)
        {
            srcDataList.Reverse();
            var firstData = srcDataList[0];
            GashaponExchangeData resultData = firstData.Clone();
            resultData.PgcDatas = srcDataList.SelectMany(data => data.PgcDatas).ToList();
            return resultData;
        }


        /// <summary>
        /// 通过bundleId 创建 GashaponRewardData
        /// </summary>
        /// <param name="srcDataList"></param>
        /// <param name="bundleId"></param>
        /// <returns></returns>
        public GashaponRewardData GetBundleRewardDataById(List<GashaponRewardData> srcDataList,string bundleId)
        {
            if (!NeedToDealData(srcDataList))
            {
                return null;
            }
            List<GashaponRewardData> resultDatas = new List<GashaponRewardData>();

            foreach (var rewardData in srcDataList)
            {
                if (rewardData.BundleId == bundleId)
                {
                    resultDatas.Add(rewardData);
                }
            }

            return CreateBundleRewardData(resultDatas);
        }

        /// <summary>
        /// 获取移除属于Bundle相关的数据后的数组
        /// </summary>
        /// <param name="srcDataList"></param>
        /// <returns></returns>
        public List<GashaponRewardData> RemoveBundleRewardData(List<GashaponRewardData> srcDataList)
        {
            List<GashaponRewardData> resultDatas = new List<GashaponRewardData>(srcDataList);
            resultDatas.RemoveAll(data => !string.IsNullOrEmpty(data.BundleId));
            return resultDatas;
        }


        /// <summary>
         /// 预处理奖励数据，过滤以及组合Bundle的数据
         /// </summary>
         /// <param name="srcDataList"></param>
         /// <returns></returns>
        public List<GashaponRewardData> PreDealData(List<GashaponRewardData> srcDataList)
        {
            if (!NeedToDealData(srcDataList))
            {
                return srcDataList;
            }

            Dictionary<string, List<GashaponRewardData>> bundleDict = new Dictionary<string, List<GashaponRewardData>>();
            Dictionary<string, int> bundleIndexDict = new Dictionary<string, int>();

            //记录Bundle原始位置
            for (int i = 0; i < srcDataList.Count; i++)
            {
                var resultData = srcDataList[i];
                if (!string.IsNullOrEmpty(resultData.BundleId))
                {
                    if (!bundleDict.TryGetValue(resultData.BundleId, out var bundleList))
                    {
                        bundleList = new List<GashaponRewardData>();
                        bundleDict[resultData.BundleId] = bundleList;
                        // 记录最小的下标
                        bundleIndexDict[resultData.BundleId] = i;
                    }
                    bundleList.Add(resultData);
                }
            }

            List<GashaponRewardData> resultDatas = new List<GashaponRewardData>();

            for (int i = 0; i < srcDataList.Count; i++)
            {
                var resultData = srcDataList[i];
                if (!string.IsNullOrEmpty(resultData.BundleId) && bundleIndexDict.ContainsKey(resultData.BundleId))
                {
                    // 第一次遇到此 BundleId 时，插入 Bundle 数据
                    if (bundleIndexDict[resultData.BundleId] == i)
                    {
                        var combineList = bundleDict[resultData.BundleId];
                        if (combineList != null && combineList.Count > 0)
                        {
                            var newData = CreateBundleRewardData(combineList);
                            resultDatas.Add(newData);
                        }
                    }
                    continue;
                }

                // 插入非 Bundle 项
                resultDatas.Add(resultData);
            }

            return resultDatas;
        }


        /// <summary>
        /// 预处理奖励数据，过滤以及组合Bundle的数据
        /// </summary>
        /// <param name="srcDataList"></param>
        /// <returns></returns>
        public List<GashaponExchangeData> PreDealData(List<GashaponExchangeData> srcDataList)
        {
            if (!NeedToDealData(srcDataList))
            {
                return srcDataList;
            }

            Dictionary<string, List<GashaponExchangeData>> bundleDict = new Dictionary<string, List<GashaponExchangeData>>();
            Dictionary<string, int> bundleIndexDict = new Dictionary<string, int>();

            //构造Bundle Dict +记录Bundle原始位置
            for (int i = 0; i < srcDataList.Count; i++)
            {
                var resultData = srcDataList[i];
                if (!string.IsNullOrEmpty(resultData.BundleId))
                {
                    if (!bundleDict.TryGetValue(resultData.BundleId, out var bundleList))
                    {
                        bundleList = new List<GashaponExchangeData>();
                        bundleDict[resultData.BundleId] = bundleList;
                        bundleIndexDict[resultData.BundleId] = i;// 记录最小的下标
                    }
                    bundleList.Add(resultData);
                }
            }

            List<GashaponExchangeData> resultDatas = new List<GashaponExchangeData>();

            for (int i = 0; i < srcDataList.Count; i++)
            {
                var resultData = srcDataList[i];
                if (!string.IsNullOrEmpty(resultData.BundleId) && bundleIndexDict.ContainsKey(resultData.BundleId))
                {
                    // 第一次遇到此 BundleId 时，插入 Bundle 数据
                    if (bundleIndexDict[resultData.BundleId] == i)
                    {
                        var combineList = bundleDict[resultData.BundleId];
                        if (combineList != null && combineList.Count > 0)
                        {
                            var newData = CreateBundleExchangeData(combineList);
                            resultDatas.Add(newData);
                        }
                    }
                    continue;
                }

                // 插入非 Bundle 项
                resultDatas.Add(resultData);
            }

            return resultDatas;
        }

        public void ShowGashaponReward(GameObject refGameObject,List<CommonRewardData> rewardsItems,List<ServerPairData> pairList , GashaponExchangeData info =null)
        {
            if ((rewardsItems == null || rewardsItems.Count <= 0) && (pairList == null || pairList.Count <= 0))
            {
                return;
            }
            var taskRewardDatas = new List<CommonRewardItemData>();

            if (rewardsItems != null) {
                foreach (var rewardItem in rewardsItems)
                {
                    int rewardType = rewardItem.rewardType;
                    int rewardCount = rewardItem.rewardNum;
                    var baseName = !string.IsNullOrEmpty(rewardItem.pgcId)
                        ? Es.DataTables.GetPgcNameData(rewardItem.pgcId)?.Name
                        : PgcUtils.GetRewardName((BUDRewardType)rewardType);
                    var nameOverride = PgcUtils.GetScopeCurrencyName(GameUtils.ConvertRewardType(rewardType));
                    taskRewardDatas.Add(new CommonRewardItemData() {
                        rewardType = rewardType,
                        RewardAmount = rewardCount,
                        pgcId = rewardItem.pgcId,
                        rewardName = !string.IsNullOrEmpty(nameOverride) ? nameOverride : baseName,
                        IconSp = PgcUtils.LoadRewardIcon((BUDRewardType)rewardType, refGameObject)
                    });
                }
            }


            if (pairList != null) {
                var avatarDataHandler = AssetsDataManager.GetData<AvatarBUDSceneHandler>();
                var petAvatarDataHandler = AssetsDataManager.GetData<PetAvatarBUDSceneHandler>();
                foreach (var pairData in pairList) {
                    if (pairData.list != null) {
                        foreach (var opertionData in pairData.list) {
                            var assetData = avatarDataHandler.GetAssetsData(opertionData.id);
                            if (assetData == null) {
                                assetData = petAvatarDataHandler.GetAssetsData(opertionData.id);
                            }
                            taskRewardDatas.Add(new CommonRewardItemData() {
                                rewardType = pairData.dataType,
                                IconSp = PgcUtils.GetIconSpriteByPgcId(opertionData.id, refGameObject),
                                rewardName = info == null ?assetData?.Name:info.Name,
                                RewardAmount = 1,
                            });
                        }

                    }
                }
                Message.MessageHelper.Broadcast(Message.MessageName.AvaterDatabaseCheck);
            }
            var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
            panel.ShowRewards(taskRewardDatas);
            AccountDataManager.Inst.BalanceInfo.Refresh();
        }

        public void JumpToGashapon(string gashaponId) {
            pendingSubLotteryId = gashaponId;
            UIManager.Inst.OpenPanel<StoreMallPanel>(PanelId.StoreMallPanel, gashaponId);
        }

    }
}
