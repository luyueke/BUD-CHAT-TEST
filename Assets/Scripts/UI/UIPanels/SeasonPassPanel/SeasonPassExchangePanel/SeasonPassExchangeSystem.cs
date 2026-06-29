using Es;
using Game.Event;
using GameData.PgcData;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RTG;
using System;
using System.Collections.Generic;
using System.Linq;
using UI.Manager;
using UnityEngine;

public class SeasonPassExchangeSystem : GlobalInstance<SeasonPassExchangeSystem>
{
    public SeasonExchangeRsp data;


    public List<SeasonExchangeConfig> rewardList = new List<SeasonExchangeConfig>()
    {
        new SeasonExchangeConfig(17,"210","暖棕萌驹皮肤动作礼包",15,80,true,new List<int>(){ 40100529,10200067, 11300364, 10400502}),
        new SeasonExchangeConfig(16,"199","亚比雪人皮肤动作礼包",15,80,true,new List<int>(){ 40100528,10900498, 10400497, 10100116}),
        new SeasonExchangeConfig(1,"53","星牛仔皮肤动作礼包",15,80,true,new List<int>(){ 40100326,10800103, 10900411,10400398,12100028,11000194,11300297,10200048,10600095}),
        new SeasonExchangeConfig(2,"201","仲夏喵喵皮肤动作礼包",15,80,true,new List<int>(){ 40200327,10800105, 10900414, 10400405, 11000200,12000024,11300300,10200049,10100089}),
        new SeasonExchangeConfig(3,"202","泡泡蛙宝皮肤动作礼包",15,80,true,new List<int>(){ 40600343,70400005, 70600010, 71200004,71100007,72200002}),
        new SeasonExchangeConfig(4,"203","陶瓷波瑟林皮肤动作礼包",15,80,true,new List<int>(){ 40200364, 10800111,10400413,10600097,11200034}),
        new SeasonExchangeConfig(5,"204","闪电沃蒂皮肤动作礼包",15,80,true,new List<int>(){ 40100364, 10700094,11000082,11300189,10400274,10800071}),
        new SeasonExchangeConfig(6,"205","铆钉凯特琳皮肤动作礼包",15,80,true,new List<int>(){ 40200398,10700110, 10200028,11300236,10400328,11600020,10100065 }),
        new SeasonExchangeConfig(7,"206","国潮锦逸皮肤动作礼包",15,80,true,new List<int>(){ 40100414,10900437, 10400428,11300319 }),
        new SeasonExchangeConfig(8,"52","毛绒蹦蹦皮肤动作礼包",15,80,true,new List<int>(){ 40100229,10900327, 11300231,10400324,10100064 }),
        new SeasonExchangeConfig(9,"208","潜水蛙人皮肤动作礼包",15,80,true,new List<int>(){ 40100473,10700129, 10400469,11300345 }),
        new SeasonExchangeConfig(10,"209","奶芙熊熊皮肤动作礼包",15,80,true,new List<int>(){ 40100494,10900481, 10400477,11300350 }),
        new SeasonExchangeConfig(11,"","金币",10,10,false,new List<SeasonExchangeConfigItem>(){ new SeasonExchangeConfigItem( BUDRewardType.RewardCoin,500,0,null) }),
        new SeasonExchangeConfig(12,"","徽章",10,10,false,new List<SeasonExchangeConfigItem>(){ new SeasonExchangeConfigItem( BUDRewardType.RewardBadge, 50,0,null) }),
        new SeasonExchangeConfig(13,"","创作者能量币",10,10,false,new List<SeasonExchangeConfigItem>(){ new SeasonExchangeConfigItem( BUDRewardType.RewardEnergyCoin, 50,0,null) }),
        new SeasonExchangeConfig(14,"","幸运币",15,10,false,new List<SeasonExchangeConfigItem>(){ new SeasonExchangeConfigItem( BUDRewardType.RewardLuckyCoin, 2,0,null) }),
        new SeasonExchangeConfig(15,"","优优币",45,2,false,new List<SeasonExchangeConfigItem>(){ new SeasonExchangeConfigItem( BUDRewardType.RewardYouYouCoin,6,0,null) }),
    };

    public override void Initialize()
    {
        if (SeasonPassDataManager.Inst.CurrentSeasonPassType == SeasonPassType.S15SeasonPass)
        {
            rewardList.Insert(0, new SeasonExchangeConfig(18, "211", "焦糖布丁喵套装动作礼包", 15, 80, true, new List<int>() {40100533, 10400508, 11300369, 10200070 }));
        }
    }

    public int GetTime(int id)
    {
        if (data != null)
        {
            foreach (var item in data.list)
            {
                if (item.id == id.ToString())
                {
                    return int.Parse(item.currentTimes);
                }
            }
        }
        return 0;
    }

    public void ReqStore(int id, int count,GameObject obj,Action ac)
    {
        JObject req = new JObject();
        req["id"] = id;
        req["seasonPassType"] = (int)SeasonPassDataManager.Inst.CurrentSeasonPassType;
        req["times"] = count;

        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.RedeemProduct, HttpMethod.POST, JsonConvert.SerializeObject(req), (content) =>
        {
            LoggerUtils.Log($"ReqProduct: {content}");
            SeasonExchangeRewardRsp rsp = JsonConvert.DeserializeObject<SeasonExchangeRewardRsp>(content);

            if (rsp != null && rsp.rewardList != null && rsp.rewardList.Count > 0)
            {
                ReqProductInfo();

                AccountDataManager.Inst.BalanceInfo.Refresh();

                var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
                var rewards =  new List<CommonRewardItemData>();

                foreach (var rewar in rsp.rewardList)
                {
                    if (rewar.rewardType == (int)BUDRewardType.RewardPgcResource)
                    {
                        foreach (var item in rewardList)
                        {
                            if (item.ExchangeId == id)
                            {
                                var reward = new CommonRewardItemData();
                                reward.IconSp = PgcUtils.LoadBundleIcon(item.BundleID, obj);
                                reward.rewardName = item.Name.Substring(0, item.Name.Length - 6) + "套装";

                                var reward2 = new CommonRewardItemData();
                                reward2.IconSp = PgcUtils.GetIconSpriteByPgcId(item.RewardList.First().PgcID.ToString(), obj);
                                reward2.rewardName = PgcUtils.GetEmoteName(item.RewardList.First().PgcID.ToString());

                                rewards.Add(reward);
                                rewards.Add(reward2);
                            }
                        }
                    }
                    else
                    {
                        var reward = new CommonRewardItemData();
                        reward.IconSp = PgcUtils.LoadRewardIcon((BUDRewardType)rewar.rewardType, obj);
                        reward.rewardName = PgcUtils.GetRewardName((BUDRewardType)rewar.rewardType);
                        reward.RewardAmount = rewar.amount;
                        rewards.Add(reward);
                    }
                }
                panel.ShowRewards(rewards);

                ac?.Invoke();
            }
            else
            {
                LoggerUtils.Log($"ReqProduct Fail");
            }

        },
        (error) =>
        {
            LoggerUtils.Log($"ReqProduct Error: {error}");
        });
    }

    public void ReqProductInfo()
    {
        JObject req = new JObject();

        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.RedeemStore, HttpMethod.GET, JsonConvert.SerializeObject(req), (content) =>
        {
            LoggerUtils.Log($"ReqStore: {content}");
            SeasonExchangeRsp rsp = JsonConvert.DeserializeObject<SeasonExchangeRsp>(content);

            data = rsp;
            MessageHelper.Broadcast(MessageName.SeasonExchangeGiftUpdate);
        },
        (error) =>
        {
            LoggerUtils.Log($"ReqStore Error: {error}");
        });
    }
}
public class SeasonExchangeRewardRsp
{
    public string id;
    public string redeemTimes;
    public List<TaskClaimRewardData> rewardList;
}
public class SeasonExchangeRsp
{
    public List<SeasonExchangeRspItem> list;
}

public class SeasonExchangeRspItem
{
    public string id;
    public string currentTimes;
}

public class SeasonExchangeConfig
{
    public int ExchangeId;
    public Sprite Icon;
    public string Name;
    public long Price;
    public int Count;
    public bool Gift;
    public string BundleID;
    public List<SeasonExchangeConfigItem> RewardList;
    public SeasonExchangeConfig()
    {

    }
    public SeasonExchangeConfig(int id, string bundleID, string name, long price, int count, bool gift, List<SeasonExchangeConfigItem> rewardList)
    {
        ExchangeId = id;
        BundleID = bundleID;
        Name = name;
        Price = price;
        Count = count;
        Gift = gift;
        RewardList = rewardList;
        if (gift)
        {
            RewardList.Add(new SeasonExchangeConfigItem(BUDRewardType.RewardExperience, 10, 0, null));
            RewardList.Add(new SeasonExchangeConfigItem(BUDRewardType.RewardExperience, 20, 0, null));
            RewardList.Add(new SeasonExchangeConfigItem(BUDRewardType.RewardLuckyCoin, 2, 0, null));
            RewardList.Add(new SeasonExchangeConfigItem(BUDRewardType.RewardYouYouCoin, 2, 0, null));
            RewardList.Add(new SeasonExchangeConfigItem(BUDRewardType.RewardCoin, 300, 0, null));
            RewardList.Add(new SeasonExchangeConfigItem(BUDRewardType.RewardBadge, 30, 0, null));
            RewardList.Add(new SeasonExchangeConfigItem(BUDRewardType.RewardEnergyCoin, 30, 0, null));
        }
    }
    public SeasonExchangeConfig(int id, string bundleID, string name, long price, int count, bool gift, List<int> rewardList)
    {
        ExchangeId = id;
        BundleID = bundleID;
        Name = name;
        Price = price;
        Count = count;
        Gift = gift;
        RewardList = new List<SeasonExchangeConfigItem>();
        foreach (var item in rewardList)
        {
            RewardList.Add(new SeasonExchangeConfigItem(BUDRewardType.RewardPgcResource, 1, item, null));
        }
        if (gift)
        {
            RewardList.Add(new SeasonExchangeConfigItem(BUDRewardType.RewardExperience, 10, 0, null));
            RewardList.Add(new SeasonExchangeConfigItem(BUDRewardType.RewardExperience, 20, 0, null));
            RewardList.Add(new SeasonExchangeConfigItem(BUDRewardType.RewardLuckyCoin, 2, 0, null));
            RewardList.Add(new SeasonExchangeConfigItem(BUDRewardType.RewardYouYouCoin, 2, 0, null));
            RewardList.Add(new SeasonExchangeConfigItem(BUDRewardType.RewardCoin, 300, 0, null));
            RewardList.Add(new SeasonExchangeConfigItem(BUDRewardType.RewardBadge, 30, 0, null));
            RewardList.Add(new SeasonExchangeConfigItem(BUDRewardType.RewardEnergyCoin, 30, 0, null));
        }
    }
}

public class SeasonExchangeConfigItem
{
    public BUDRewardType RewardType;
    public int PgcID;
    public int Count;
    public Sprite Icon;

    public SeasonExchangeConfigItem(BUDRewardType rewardType, int count, int pgcID, Sprite icon)
    {
        RewardType = rewardType;
        PgcID = pgcID;
        Count = count;
        Icon = icon;
    }
}