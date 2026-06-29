using System;
using System.Collections;
using System.Collections.Generic;
using Message;
using NetCoreServer;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Events;


public class AccountBalanceInfo
{
    private Dictionary<CurrencyType, int> allAccountBalance = new Dictionary<CurrencyType, int>();
    public AICredit AiCredit { get; set; } = new AICredit();

    public class UserBalanceData
    {
        public int totalRecharged;
        public int firstRecharge;
        public int coin;
        public int gem;
        public int badge;
        public Voucher voucher = new Voucher();
        public int communityCoin;
        public int creatorCoin;
        public int creatorPoints;
        public int energyCoin;
        public int luckyCoin;
        public int christmasCoin;
        public int magicCoin;
        public int luckyTicket;
        public int christmasTicket;
        public int coinTicket;
        public int purpleDreamCoin;
        public int youYouCoin;
        public int giftTicket;
        public int popularityTicket;
        public int purpleDreamTicket;
        public int sweetieTicket;
        public int aISeasonCoin;
        public int aICore;

        public int koiGachaCoin; //幸运锦鲤抽奖券
        public int seasonPassCoin; //通行证币
        public int miaoCoin;//喵币

        public int collectionTicket; // 收藏券
        public int communitySkinTicket;//社区皮肤券
        public int communityVehicleTicket; //载具兑换券
        public int crystal; //水晶
        public int crystalShards; //水晶碎片
        public int musicalNoteCrystal; //乐符水晶
        public int musicalNoteCrystalShards; //乐符水晶碎片

        public int treePlantingWater; //水滴
        public int treePlantingFertilizer; //肥料

        public int waCoin; // 袜币
        public int pinkTailTicket; // 小尾券
        public int warmOrangeTicket; // 晕晕券
        public int communityTheaterTicket;//社区剧本兑换券
        public int shovel; //铲子

        public int shrimpYuan; //虾元
        public AICredit aiCredit;
        public int zzzCoin; //绒币
        public int zzzPhantomCrystal; //啧啧幻音水晶
        public int zzzPhantomCrystalShards; //啧啧幻音碎片
    }
    
    public class AICredit
    {
        //消耗顺序 每日-赠送-永久
        public int permanentAmount; //永久ai能量 购买能量包的能量，不清空
        public int expiringAmount;//赠送ai能量  购买伙伴赠送的能量，自购买伙伴七天后清空
        public int dailyAmount;//每日ai能量  每日零点自动刷新的能量，每日零点会清空前一天没用完的
        public int cloneAmount;//AI伙伴音色克隆次数
    }

    public class Voucher
    {
        public int avatarVoucher;
        public int gashaponVoucher;
    }

    private void Start()
    {
    }

    public void Refresh(Action callback = null)
    {
        RefreshData(successAction: data =>
        {
            SyncBalance(CurrencyType.TotalRecharged, data.totalRecharged);
            SyncBalance(CurrencyType.FirstRecharge, data.firstRecharge);
            SyncBalance(CurrencyType.Coin, data.coin);
            SyncBalance(CurrencyType.Gem, data.gem);
            SyncBalance(CurrencyType.Badge, data.badge);
            SyncBalance(CurrencyType.PinkCoin, data.communityCoin);
            SyncBalance(CurrencyType.GreenCoin, data.creatorCoin);
            SyncBalance(CurrencyType.Points, data.creatorPoints);
            SyncBalance(CurrencyType.EnergyCoin, data.energyCoin);
            SyncBalance(CurrencyType.LuckyCoin, data.luckyCoin);
            SyncBalance(CurrencyType.ChristmasCoin, data.christmasCoin);
            SyncBalance(CurrencyType.MagicCoin, data.magicCoin);
            SyncBalance(CurrencyType.LuckyTicket, data.luckyTicket);
            SyncBalance(CurrencyType.ChristmasTicket, data.christmasTicket);
            SyncBalance(CurrencyType.CoinTicket, data.coinTicket);
            SyncBalance(CurrencyType.PurpleDreamCoin, data.purpleDreamCoin);
            SyncBalance(CurrencyType.YouYouCoin, data.youYouCoin);
            SyncBalance(CurrencyType.GiftTicket, data.giftTicket);
            SyncBalance(CurrencyType.PopularityTicket, data.popularityTicket);
            SyncBalance(CurrencyType.PurpleDreamTicket, data.purpleDreamTicket);
            SyncBalance(CurrencyType.SweetieTicket, data.sweetieTicket);
            SyncBalance(CurrencyType.AISeasonCoin, data.aISeasonCoin);
            SyncBalance(CurrencyType.AICore, data.aICore);
            SyncBalance(CurrencyType.KoiGachaCoin, data.koiGachaCoin);
            SyncBalance(CurrencyType.SeasonPassCoin, data.seasonPassCoin);
            SyncBalance(CurrencyType.MiaoCoin, data.miaoCoin);
            SyncBalance(CurrencyType.CollectionTicket, data.collectionTicket);
            SyncBalance(CurrencyType.CommunitySkinTicket, data.communitySkinTicket);
            SyncBalance(CurrencyType.CommunityVehicleTicket, data.communityVehicleTicket);
            SyncBalance(CurrencyType.Crystal, data.crystal);
            SyncBalance(CurrencyType.CrystalShards, data.crystalShards);
            SyncBalance(CurrencyType.MusicNoteCrystal, data.musicalNoteCrystal);
            SyncBalance(CurrencyType.MusicNoteCrystalShards, data.musicalNoteCrystalShards);
            SyncBalance(CurrencyType.TreePlantingFertilizer, data.treePlantingFertilizer);
            SyncBalance(CurrencyType.TreePlantingWater, data.treePlantingWater);
            SyncBalance(CurrencyType.SockCoin, data.waCoin);
            SyncBalance(CurrencyType.SockTailTicket, data.pinkTailTicket);
            SyncBalance(CurrencyType.SockYunyunTicket, data.warmOrangeTicket);
            SyncBalance(CurrencyType.CommunityTheaterTicket, data.communityTheaterTicket);
            SyncBalance(CurrencyType.Shovel, data.shovel);
            SyncBalance(CurrencyType.ShrimpYuan, data.shrimpYuan);
            SyncBalance(CurrencyType.ZZZCoin, data.zzzCoin);
            SyncBalance(CurrencyType.ZZZPhantomCrystal, data.zzzPhantomCrystal);
            SyncBalance(CurrencyType.ZZZPhantomCrystalShards, data.zzzPhantomCrystalShards);
            AiCredit = data.aiCredit ?? new AICredit();
            MessageHelper.Broadcast(MessageName.OnAICreditChange);
            SetEventPublicData();
            callback?.Invoke();

        });
    }
    private void SetEventPublicData()
    {

        var balanceInfo = AccountDataManager.Inst.BalanceInfo;
        var _userInfo = AccountDataManager.Inst.UserInfo;
        Dictionary<string, object> superProperties = AnalyticsManager.Inst.GetSuperProperties();
        superProperties["open_id"] = AccountDataManager.Inst.accountUnionid;//UID 
        superProperties["role_id"] = _userInfo.username;//UID
        superProperties["role_name"] = _userInfo.nickname;//名字
        superProperties["diamond_amount"] = balanceInfo.GetAccountCount(CurrencyType.Gem);//剩余钻石数量
        superProperties["pinkcoin_amount"] = balanceInfo.GetAccountCount(CurrencyType.PinkCoin);//剩余粉币数量
        superProperties["bluecoin_amount"] = balanceInfo.GetAccountCount(CurrencyType.Badge);//剩余徽章数量
        superProperties["coin_amount"] = balanceInfo.GetAccountCount(CurrencyType.Coin);//剩余金币数量
        superProperties["yoyocoin_amount"] = balanceInfo.GetAccountCount(CurrencyType.YouYouCoin);
        superProperties["purplecoin_amount"] = balanceInfo.GetAccountCount(CurrencyType.PurpleDreamCoin);
        superProperties["luckycoin_amount"] = balanceInfo.GetAccountCount(CurrencyType.LuckyCoin);
        superProperties["creatorcoin_amount"] = balanceInfo.GetAccountCount(CurrencyType.GreenCoin);
        superProperties["creator_energy_amount"] = balanceInfo.GetAccountCount(CurrencyType.EnergyCoin);
        superProperties["passcoin_amount"] = balanceInfo.GetAccountCount(CurrencyType.SeasonPassCoin);
        superProperties["charge_amount"] = balanceInfo.GetAccountCount(CurrencyType.TotalRecharged);//累计充值金额
        superProperties["first_charge_amount"] = balanceInfo.GetAccountCount(CurrencyType.FirstRecharge);//首充金额
        AnalyticsManager.Inst.SetSuperProperties(superProperties);//设置公共事件属性

    }
    /// <summary>
    /// 刷新余额接口
    /// </summary>
    public void RefreshData(Action<UserBalanceData> successAction = null, Action failAction = null)
    {
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.userBalance,
            HttpMethod.GET,
            "",
            onReceive: arg0 =>
            {
                UserBalanceData resData = JsonConvert.DeserializeObject<UserBalanceData>(arg0);
                if (resData == null)
                {
                    failAction?.Invoke();
                    return;
                }

                successAction?.Invoke(resData);
            },
            onFail: arg0 =>
            {
                Debug.LogError("刷新余额接口失败，msg：" + arg0);
                failAction?.Invoke();
            },
            retryCount:2);
    }

    public void Clear()
    {
        foreach (CurrencyType fruit in System.Enum.GetValues(typeof(CurrencyType)))
        {
            if (fruit != CurrencyType.None)
            {
                SyncBalance(fruit, 0);
            }
        }
    }

    /// <summary>
    /// 本地数据同步，不准确，用于金币收集等逻辑
    /// </summary>
    /// <param name="type"></param>
    /// <param name="num"></param>
    public void SyncBalance(CurrencyType type, int num)
    {
        if (num < 0)
        {
            return;
        }

        allAccountBalance[type] = num;
        MessageHelper.Broadcast(MessageName.OnPlayerInfoAccountChange, type);
    }


    /// <summary>
    /// 获取本地余额数据
    /// </summary>
    /// <param name="accountType"></param>
    /// <returns></returns>
    public int GetAccountCount(CurrencyType accountType)
    {
        if (allAccountBalance.TryGetValue(accountType, out var bInfo))
        {
            return bInfo;
        }
        return 0;
    }

}
