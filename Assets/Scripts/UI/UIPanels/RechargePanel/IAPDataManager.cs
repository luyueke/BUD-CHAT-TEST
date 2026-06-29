using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Basic.Extensions;
using Game.Database;
using Game.Event;
using GameData.Rewards;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

public class ChannelProductInfo
{
    /// <summary>
    /// 商品ID， 必有字段
    /// </summary>
    public string productId;

    /// <summary>
    /// 从后段兑换字段
    /// </summary>
    public string extension;
    /// <summary>
    /// 后段返回
    /// </summary>
    public string productName;
    public string productDesc;
    public string price;
    public int coinNum;
    public string serverID;
    public string roleID;
    public string payNotifyUrl;
    public string cpOrderId;
    /// <summary>
    /// 支付类型
    /// </summary>
    public int paymentType;// 1支付宝 2微信
}

public class BuyReduceData
{
    public List<BuyDiscountData> list;
    public int discountType;
}

public class BuyDiscountData
{
    public int id;
    public int amount;
}

public class BuyRedeceRespon
{
    public List<BuyDiscountData> list;
    public string pgcId;
}

public class BuyIapReq
{
    public string productId;
    public GiftOrderData giftOrderData;
}
public class GiftOrderData
{
    public string toUid;
    public int giftType;
    public string mailId;
    public string seasonPassType;
}

/// <summary>
/// 钻石直氪信息
/// </summary>
public class ProductGemInfo
{
    public string price;
    public string productId;
    public int gemNum;
    public string name;
    public string desc;
    public LimitPackage limitPackage;

    public ChannelProductInfo toU8Info()
    {
        var fixedInfo = new ChannelProductInfo();
        fixedInfo.productId = productId;
        fixedInfo.productName = name;
        fixedInfo.productDesc = desc;
        fixedInfo.price = price;
        return fixedInfo;
    }
}

public class LimitPackage
{
    public int luckyCoinNum;
}

public class OrderCheckRes
{
    public ProductGemInfo iapProduct;
}

public class ProductOrderInfo
{
    public string budOrderId;
}

public class SubscribeStatusResponse
{
    public int vipType;
    public string activityRemainTime;
    public string remainTime;
    public int dailyRewardStatus;
    public List<ProductInfo> products;
    public SubscribeStatusReward rewards;
    public List<VipDailyRewardInfo> vipDailyReward;
    public int storageDays;
    public BaseLimitPackageData weeklyLimitPackage;
    public MonthCardData monthlyCard;
    public AICreditMonthCardData buddyMonthlyCard; //ai伙伴月卡

}

public class AICreditMonthCardData
{
    public int isValid;//1:已购买,0:未购买
    public string remainTime;//月卡剩余时间
}

public class MonthCardInfo
{
    public string remainTime;
    public string accumulationDays;
    public List<ServerRewardInfo> dailyRewards;
    public int marketDiscount;
    public ProductInfo productInfo;
}

public class MonthCardData
{
    public MonthCardInfo basicCard;
    public MonthCardInfo premiumCard;
}

public class VipDailyRewardInfo
{
    public int rewardType;
    public int amount;
}

public class DiscountCardStatusResponse
{
    public bool hasDiscountCard;
    public long expireTime;

}

public class RedeemClaimReq
{
    public string redeemCode;
}

public class RedeemClaimResponse
{
    public List<RedeemClaimRewardsItem> rewards;
    public ServerBagUpdateData backPackData;
    public int homepageSkin;
    public int avatarFrame;
    public int chatBubbles;
    public RedeemUgcItem ugcInfo;
    public int rewardType;
    public int nicknameFrame;
}

public class RedeemUgcItem
{
    public string ugcId;
    public string ugcCover;
    public string ugcName;
}

public class RedeemClaimRewardsItem
{
    public int rewardType;
    public int count;
}


public class SubscribeStatusReward
{
    /// <summary>
    /// 年卡额外奖励，为空则没有
    /// </summary>
    public List<int> month;

    /// <summary>
    /// 年卡额外奖励，为空则没有
    /// </summary>
    public List<int> year;

}


public class PaidPackageListItem
{
    public int packageType;
    public int isPaid;
    public string endDate;
    public string taskEndDate;
    public ProductInfo productInfo;
}

public enum PaidPackageType
{
    Illegal = 0,
    StartPack = 1,
    sixYuanPack = 2,
    RainyRibbitPack = 3,
    WasabiPack = 4,
    HalloweenPack = 5,
    WeirdCorePack = 6,//S4 10元礼包
    Y2KPack = 7,// S4 6元礼包
    LimitedTimeCurrencyPack = 8,//18元限时礼包
    SmannyPack = 9,//S5 10元礼包
    S5LimitedTimePack = 10,//S5 18元限时礼包
    FurryPack = 11,
    PeachContiPack = 12,
    DreamyVioletPack = 13,
    S7PhoenixPackage = 14,//S7 春节12元礼包
    S8TwentyYuanPackage = 15,// S8 限时返场礼包
    S8CommunityCoinPack = 16, // S8 限时粉币礼包
    S9LimitedTimeCurrencyPack = 17, // S9 限时粉币礼包

    // 周年庆礼包
    DawnSurprise = 18, // 曙光惊喜
    SummerEnjoyment = 19, // 夏日畅享
    CoolSummerEnjoyment = 20, // 酷夏尊享
    DeluxeSupplies = 21, // 豪华补给

}


public class ProductInfo
{
    public string productId;
    public string productName;
    public string productDesc;
    public string price;
    public int type;

    public ChannelProductInfo toU8Info()
    {
        var fixedInfo = new ChannelProductInfo();
        fixedInfo.productId = productId;
        fixedInfo.productName = productName;
        fixedInfo.productDesc = productDesc;
        fixedInfo.price = price;
        return fixedInfo;
    }
}

public class PriceInfoResponse
{
    public int resultType;
    public List<PriceLocalInfo> priceInfos;
}

public class SubscribeRewardReq
{
    public int type;
}

public enum SubscribeRewardReqType
{
    dailyReward = 1,
    silverMonthCardReward = 2, //银卡每日奖励
    goldMonthCardReward = 3,//金卡每日奖励
}

public enum SubscribeVipType
{
    None = 0,
    MonthCard = 1,
    YearCard = 2
}

public class ProductRes
{
    public List<ProductGemInfo> gemRechargeList;
    public List<PaidPackageListItem> paidPackageList;
    public bool disableRedeem = false;
    //累充福利
    public RechargeBenifits rechargeBenefits;
    public LimitPackageData limitPackageData;

    public Y2KSkateboardingPackage y2KSkateboardingPackage;

    public NewYearLuckyLottery newYearLuckyLottery;
    public NewYearLuckyLottery sleepyKoiLuckyLottery;
    public List<NewYearLimitedPackageListItem> newYearLimitedPackageList;
    public List<PopupPackageList> popupPackageList;
    public List<MiaoCoinPackageData> miaoCoinPackage;
    public List<BuyReducePackageData> buyReducePackageDatas;
    public List<Y2KSkateboardingPackage> lanternFestivalPackageList;
    public List<Y2KSkateboardingPackage> loveFestivalPackageList;
    public List<Y2KSkateboardingPackage> qianqianWanwanPackageList;
    public List<Y2KSkateboardingPackage> treePlantingDayPackageList;
    public List<Y2KSkateboardingPackage> seasonPrePackPackageList;
    public List<MiaoCoinPackageData> waCoinPackage;
    public List<Y2KSkateboardingPackage> babyShrimpPackageList;
    public List<Y2KSkateboardingPackage> zzzPackageList;//幻音派对礼包
    public List<ChasingWavesPackage> chasingWavesPackageList;
}

public class ChasingWavesPackage
{
    public string productId;
    public string productName;
    public string productDesc;
    public string price;
    public string discount;
    public int isPaid;
}

public class Y2KSkateboardingPackage
{
    public string price;
    public string discount;
    public string productId;
    public int isPaid;
    public string productName;
    public string productDesc;
    public int gemNum;

    public override string ToString()
    {
        return $"Y2KSkateboardingPackage name={productName},price={price},productid={productId},desc={productDesc},ispaid={isPaid},discount={discount}";
    }
}

public class BuyReducePackageData
{
    public string price;
    public string name;
    public string productId;
    public string discount;
    public string desc;
    public int buyCount;
    public int buyLimitCount;
}

public class MiaoCoinPackageData
{
    public string price;
    public string name;
    public string productId;
    public int gemNum;
    public string desc;
    public string discount;
    public int isPaid;
}

public class PopupPackageList
{
    public ProductInfo productInfo;
    public int isPaid;

}

#region 累充福利需求 - 2024/11/12

public class RechargeBenifits
{
    public int totalRecharged;
    public List<RechargeLevelData> rechargeLevelList;
}

public class RechargeLevelData
{
    public int id;
    public int rechargeNum;//当前档位需要的充值金额(元)
    public int rewardType;
    public string itemName;
    public int pgcId;
    public int isOptionalReward;
    public List<int> optionalPgcList;
    public int rewardStatus;
}

#endregion


public class NewYearLimitedPackageListItem
{
    public string productId;
    public string productName;
    public int price;
    public int hasDiscount;
    public int discountPrice;
    public string discountEndDate;
    public string endDate;
    public int isPaid;
    public int purchasedNum;
}

public class NewYearLuckyLottery
{
    public ProductInfo productInfo;
    public int isPaid;
    public List<string> lotteryList;
    public string firstPrizeUserNick;
    public int rewardStatus;
}

/// <summary>
/// BUD 商品购买类型
/// </summary>
public enum BUDProductType
{
    ErrProductType = 0,
    Avatar = 1,
    Emote = 2,
    SeasonPass = 3, // 高级通行证
    AdvancedSeasonPassTier = 4, // 通行证跳级
    AIHospitalSlot = 5,
    PrivateOrderPublish = 6, // 发布私单
    LimitPackage = 7, // 非直氪限购礼包
    InstrumentAndAnimationDiscountCard = 8, // 乐器/动作打折卡
    VipMonthPackage = 9,
    NewYearLimitedPackage = 10, // 新年限定礼包
    FlySword = 11, // 御剑飞行
    AILimitProducts = 12,// ai 限额商品
    NursePromisePackage = 13, // 丧丧护士礼包
    SunnyDollBundle = 14, //晴天娃娃套装
    NewbieResigning = 15, // 新手补签
    Vehicle = 16,//载具
}

public class BillingResultResponse
{
    public int resultType;
    public string productId;
    public string channelName;
}

public class GetChannelIdResponse
{
    public int channelId;
}

public enum BillingResultType
{

    UserPaySuccess = 1,
    UserPayPending = 2,
    RechargeSuccess = 3,
    RechargeFail = 4
}

public class RewardItem
{
    public string rewardId;
    public int groupId;
    public string rewardIcon1;
    public string rewardIcon2;
    public string rewardName1;
    public string rewardName2;
    public int rewardType1;
    public int rewardType2;
    public int rewardNum1;
    public int rewardNum2;
    public int num;
    public int rewardNum;
    public string taskIcon;
    public string title;
    public int progress;
    public int hasPreview;
    public string pgcId;
    public int rewardType;
    public List<string> pgcIds;
}

public class IapTrackData
{
    public string price;
    public string item;
    public string channel;
}

public class GetAvailableFreeSpaceResponse
{
    public Int64 size;
}

public class PriceLocalInfo
{
    public string productId;
    public string priceLocal; //带标签本地化价格 如US$9.99
    public string price; //不带标签本地化价格 如9.99
}

public class PriceDefaultInfo
{
    public int gems;
    public int bonus;
    public string price;
}

public enum ProductIdType
{
    product_60budgem,
    product_320budgem,
    product_650budgem,
    product_1320budgem,
    product_3320budgem,
    product_6680budgem,
    product_budvip,
    product_budsvip,
    product_budvaluepack,
    product_budpremiumvaluepack,
    product_fudai6,
    product_fudai12,
    product_fudai30,
    product_fudai68,
    product_fudai98,
    product_fudai128,
    product_budxinnian1,
    product_budlove7,
    product_LimitedTimeComeback,
    product_budlimiteds8,
    product_budxinnian2026,
}

public class DiscountRes
{
    public List<DiscountItemInfo> list;
}

public class DiscountItemInfo
{
    public int id;
    public int rewardType;
    public int amount;
    public List<string> pgcIds;
    public int originalPrice;
    public int discountedPrice;
    public string name;
    public string icon;
    public string desc;
    public int avatarFrameType;
    public int homepageSkinType;
    public int chatBubblesType;
    public int total;
    public int purchasedNum;
    public string bundleId;
    //public List<string> GetPgcIds()
    //{
    //    List<string> strList = JsonConvert.DeserializeObject<List<string>>(pgdIds);
    //    return strList;
    //}
}

public class IAPDataManager : GlobalInstance<IAPDataManager>
{
    private ProductRes _productRes;
    public ProductRes productRes
    {
        get
        {
            return _productRes;
        }
    }

    private SubscribeStatusResponse _subscribeStatusRsp;
    public SubscribeStatusResponse subscribeStatusRsp
    {
        get
        {
            return _subscribeStatusRsp;
        }
    }

    private DiscountCardStatusResponse _discountCardStatusRsp;

    public DiscountCardStatusResponse discountCardStatusRsp
    {
        get
        {
            return _discountCardStatusRsp;
        }
    }

    public int channelId = 0;

    private List<PriceLocalInfo> mobilePriceInfoList;

    public List<PriceDefaultInfo> defaultShowList_US;

    private List<PriceLocalInfo> mobilePriceInfoListLocal;

    public enum ChannelIdEnum
    {
        Official = 2,
        Vivo = 3,
        Oppo = 4,
        Honor = 5,
        Huawei = 6,
        Tencent = 7,
        BiliBili = 8,
        GameCenter = 9,
        Xiaomi = 10,
        Douyin = 11,
        KuaiShou = 12,
        ChongChong = 13,
        TapTap = 14,
        HaoyouKuaiBao = 15,
        Unknown = 0
    }

    public IAPDataManager()
    {
        InitConfig();
    }

    private void InitConfig()
    {
        defaultShowList_US = new List<PriceDefaultInfo>();
        defaultShowList_US.Add(new PriceDefaultInfo { gems = 60, price = "$0.99", bonus = 0 });
        defaultShowList_US.Add(new PriceDefaultInfo { gems = 320, price = "$4.99", bonus = 18 });
        defaultShowList_US.Add(new PriceDefaultInfo { gems = 650, price = "$9.99", bonus = 45 });
        defaultShowList_US.Add(new PriceDefaultInfo { gems = 1320, price = "$19.99", bonus = 108 });
        defaultShowList_US.Add(new PriceDefaultInfo { gems = 3320, price = "$49.99", bonus = 290 });
        defaultShowList_US.Add(new PriceDefaultInfo { gems = 6680, price = "$99.99", bonus = 620 });

        mobilePriceInfoListLocal = new List<PriceLocalInfo>();
        mobilePriceInfoListLocal.Add(new PriceLocalInfo { productId = GetProductId(ProductIdType.product_60budgem), priceLocal = "$0.99", price = "0.99" });
        mobilePriceInfoListLocal.Add(new PriceLocalInfo { productId = GetProductId(ProductIdType.product_320budgem), priceLocal = "$4.99", price = "4.99" });
        mobilePriceInfoListLocal.Add(new PriceLocalInfo { productId = GetProductId(ProductIdType.product_650budgem), priceLocal = "$9.99", price = "9.99" });
        mobilePriceInfoListLocal.Add(new PriceLocalInfo { productId = GetProductId(ProductIdType.product_1320budgem), priceLocal = "$19.99", price = "19.99" });
        mobilePriceInfoListLocal.Add(new PriceLocalInfo { productId = GetProductId(ProductIdType.product_3320budgem), priceLocal = "$49.99", price = "49.99" });
        mobilePriceInfoListLocal.Add(new PriceLocalInfo { productId = GetProductId(ProductIdType.product_6680budgem), priceLocal = "$99.99", price = "99.99" });
        mobilePriceInfoListLocal.Add(new PriceLocalInfo { productId = GetProductId(ProductIdType.product_budvip), priceLocal = "$5.99", price = "5.99" });
        mobilePriceInfoListLocal.Add(new PriceLocalInfo { productId = GetProductId(ProductIdType.product_budsvip), priceLocal = "$59.99", price = "59.99" });
        mobilePriceInfoListLocal.Add(new PriceLocalInfo { productId = GetProductId(ProductIdType.product_budvaluepack), priceLocal = "$2.99", price = "2.99" });
        mobilePriceInfoListLocal.Add(new PriceLocalInfo { productId = GetProductId(ProductIdType.product_budpremiumvaluepack), priceLocal = "$5.99", price = "5.99" });
        mobilePriceInfoListLocal.Add(new PriceLocalInfo { productId = GetProductId(ProductIdType.product_fudai6), priceLocal = "$6", price = "6" });
        mobilePriceInfoListLocal.Add(new PriceLocalInfo { productId = GetProductId(ProductIdType.product_fudai12), priceLocal = "$12", price = "12" });
        mobilePriceInfoListLocal.Add(new PriceLocalInfo { productId = GetProductId(ProductIdType.product_fudai30), priceLocal = "$30", price = "30" });
        mobilePriceInfoListLocal.Add(new PriceLocalInfo { productId = GetProductId(ProductIdType.product_fudai68), priceLocal = "$68", price = "68" });
        mobilePriceInfoListLocal.Add(new PriceLocalInfo { productId = GetProductId(ProductIdType.product_fudai98), priceLocal = "$98", price = "98" });
        mobilePriceInfoListLocal.Add(new PriceLocalInfo { productId = GetProductId(ProductIdType.product_fudai128), priceLocal = "$128", price = "128" });
        mobilePriceInfoListLocal.Add(new PriceLocalInfo { productId = GetProductId(ProductIdType.product_budxinnian1), priceLocal = "$12", price = "12" });
        mobilePriceInfoListLocal.Add(new PriceLocalInfo { productId = GetProductId(ProductIdType.product_budlove7), priceLocal = "$20", price = "20" });
        mobilePriceInfoListLocal.Add(new PriceLocalInfo { productId = GetProductId(ProductIdType.product_LimitedTimeComeback), priceLocal = "$20", price = "20" });
        mobilePriceInfoListLocal.Add(new PriceLocalInfo { productId = GetProductId(ProductIdType.product_budlimiteds8), priceLocal = "$18", price = "18" });
        mobilePriceInfoListLocal.Add(new PriceLocalInfo { productId = GetProductId(ProductIdType.product_budxinnian2026), priceLocal = "$12", price = "12" });
    }

    public string GetProductId(ProductIdType productIdType)
    {
        string productId = "";
#if UNITY_ANDROID
        switch (productIdType)
        {
            case ProductIdType.product_60budgem:
                productId = "android_60budgem";
                break;
            case ProductIdType.product_320budgem:
                productId = "android_320budgem";
                break;
            case ProductIdType.product_650budgem:
                productId = "android_650budgem";
                break;
            case ProductIdType.product_1320budgem:
                productId = "android_1320budgem";
                break;
            case ProductIdType.product_3320budgem:
                productId = "android_3320budgem";
                break;
            case ProductIdType.product_6680budgem:
                productId = "android_6680budgem";
                break;
            case ProductIdType.product_budvip:
                productId = "android_budvip";
                break;
            case ProductIdType.product_budsvip:
                productId = "android_budsvip";
                break;
            case ProductIdType.product_budvaluepack:
                productId = "android_budvaluepack";
                break;
            case ProductIdType.product_budpremiumvaluepack:
                productId = "android_budpremiumvaluepack";
                break;
            case ProductIdType.product_fudai6:
                productId = "android_fudai6";
                break;
            case ProductIdType.product_fudai12:
                productId = "android_fudai12";
                break;
            case ProductIdType.product_fudai30:
                productId = "android_fudai30";
                break;
            case ProductIdType.product_fudai68:
                productId = "android_fudai68";
                break;
            case ProductIdType.product_fudai98:
                productId = "android_fudai98";
                break;
            case ProductIdType.product_fudai128:
                productId = "android_fudai128";
                break;
            case ProductIdType.product_budxinnian1:
                productId = "android_budxinnian1";
                break;
            case ProductIdType.product_budlove7:
                productId = "android_budlove7";
                break;
            case ProductIdType.product_LimitedTimeComeback:
                productId = "android_LimitedTimeComeback";
                break;
            case ProductIdType.product_budlimiteds8:
                productId = "android_budlimiteds8";
                break;
            case ProductIdType.product_budxinnian2026:
                productId = "android_budxinnian2026";
                break;
        }
#else
        switch (productIdType)
        {
            case ProductIdType.product_60budgem:
                productId = "ios_60budgem";
                break;
            case ProductIdType.product_320budgem:
                productId = "ios_320budgem";
                break;
            case ProductIdType.product_650budgem:
                productId = "ios_650budgem";
                break;
            case ProductIdType.product_1320budgem:
                productId = "ios_1320budgem";
                break;
            case ProductIdType.product_3320budgem:
                productId = "ios_3320budgem";
                break;
            case ProductIdType.product_6680budgem:
                productId = "ios_6680budgem";
                break;
            case ProductIdType.product_budvip:
                productId = "ios_budvip";
                break;
            case ProductIdType.product_budsvip:
                productId = "ios_budsvip";
                break;
            case ProductIdType.product_budvaluepack:
                productId = "ios_budvaluepack";
                break;
            case ProductIdType.product_budpremiumvaluepack:
                productId = "ios_budpremiumvaluepack";
                break;
            case ProductIdType.product_fudai6:
                productId = "ios_fudai6";
                break;
            case ProductIdType.product_fudai12:
                productId = "ios_fudai12";
                break;
            case ProductIdType.product_fudai30:
                productId = "ios_fudai30";
                break;
            case ProductIdType.product_fudai68:
                productId = "ios_fudai68";
                break;
            case ProductIdType.product_fudai98:
                productId = "ios_fudai98";
                break;
            case ProductIdType.product_fudai128:
                productId = "ios_fudai128";
                break;
            case ProductIdType.product_budxinnian1:
                productId = "ios_budxinnian1";
                break;
            case ProductIdType.product_budlove7:
                productId = "ios_budlove7";
                break;
            case ProductIdType.product_LimitedTimeComeback:
                productId = "ios_LimitedTimeComeback";
                break;
            case ProductIdType.product_budlimiteds8:
                productId = "ios_budlimiteds8";
                break;
            case ProductIdType.product_budxinnian2026:
                productId = "ios_budxinnian2026";
                break;
        }

#endif

        return productId;
    }

    public bool IsOfficialChannel()
    {
        return channelId == 2 || channelId == 14 || channelId == 15;
    }

    /// <summary>
    /// 获取直氪Gem列表
    /// </summary>
    /// <returns></returns>
    public List<ProductGemInfo> GetGemPackProducts()
    {
        return _productRes?.gemRechargeList;
    }

    public List<MiaoCoinPackageData> GetMiaoCoinPackages()
    {
        return _productRes?.miaoCoinPackage;
    }

    public List<Y2KSkateboardingPackage> GetXiaXiaZaiCoinPackages()
    {
        return _productRes?.babyShrimpPackageList;
    }

    public List<Y2KSkateboardingPackage> GetPhantomSoundPartyCoinPackages()
    {
        return _productRes?.zzzPackageList;
    }

    public List<MiaoCoinPackageData> GetWaCoinPackages()
    {
        return _productRes?.waCoinPackage;
    }

    /// <summary>
    /// 获取累充Datas
    /// </summary>
    /// <returns></returns>
    public RechargeBenifits GetCumulativeRechargeData()
    {
        return _productRes?.rechargeBenefits;
    }

    public LimitPackageData GetLimitedRechargeData()
    {
        return _productRes?.limitPackageData;
    }

    public List<PaidPackageListItem> GetPaidPackageList()
    {
        return _productRes?.paidPackageList;
    }

    public bool GetIsPaid(int packageType)
    {
        var packageListItems = GetPaidPackageList();
        if (packageListItems == null)
        {
            return false;
        }
        var packageListItem =
             packageListItems.Find(x => x.packageType == packageType);
        if (packageListItem != null)
        {
            return packageListItem.isPaid == 1;
        }

        return false;
    }

    public NewYearLuckyLottery GetNewYearLuckyLottery()
    {
        return _productRes?.newYearLuckyLottery;
    }

    public List<NewYearLimitedPackageListItem> GetNewYearLimitedPackageList()
    {
        return _productRes?.newYearLimitedPackageList;
    }


    public int GetNewYearLimitedPackagePrice(string productId, int originalPrice)
    {
        if (_productRes?.newYearLimitedPackageList == null)
            return originalPrice;

        var package = _productRes.newYearLimitedPackageList
            .FirstOrDefault(x => x.productId == productId);

        if (package == null)
            return originalPrice;

        return package.hasDiscount == 1 ? package.discountPrice : package.price;
    }

    public bool HasNewYearLimitedDiscount(string productId)
    {
        if (_productRes?.newYearLimitedPackageList == null)
            return false;

        var package = _productRes.newYearLimitedPackageList
            .FirstOrDefault(x => x.productId == productId);

        if (package == null)
            return false;

        return package.hasDiscount == 1;
    }

    public bool IsNewYearLimitedPackageLive()
    {
        return _productRes?.newYearLimitedPackageList?.Count > 0;
    }

    public void Refresh(Action<ProductRes> callback = null)
    {
#if PACKAGE_TYPE_US
        GetMobilePriceInfos();//获取Native价格
#endif
        GetProductInfo(callback);
    }

    public void GetProductInfo(Action<ProductRes> onSuccess = null,
        Action onFail = null)
    {
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ProductList,
            HttpMethod.GET,
            "",
            onReceive: arg0 =>
            {
                ProductRes response = JsonConvert.DeserializeObject<ProductRes>(arg0);
                //Debug.LogError("rechargeBenefits=" + JsonConvert.SerializeObject(response.rechargeBenefits));
                //if(response != null && response.rechargeBenefits != null)
                //{
                //    Dictionary<string, object> superProperties = AnalyticsManager.Inst.GetSuperProperties();
                //    superProperties["charge_amount"] = response.rechargeBenefits.totalRecharged;
                //    AnalyticsManager.Inst.SetSuperProperties(superProperties);  //上报累计充值金额
                //}


                if (response != null)
                {
                    _productRes = response;
                }
                onSuccess?.Invoke(response);
            }, onFail: arg0 =>
            {
                onFail?.Invoke();
            }, retryCount: 3);
    }

    public void GetPayDiscountInfo(Action<DiscountRes> onSuccess = null, Action onFail = null)
    {
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.DiscountList,
            HttpMethod.GET,
            "",
            onReceive: arg0 =>
            {
                DiscountRes response = JsonConvert.DeserializeObject<DiscountRes>(arg0);
                onSuccess?.Invoke(response);
            }, onFail: arg0 =>
            {
                onFail?.Invoke();
            }, retryCount: 3);
    }

    public void BuyDiscountListItem(int discountType, List<BuyDiscountData> list, Action<BuyRedeceRespon> onSuccess = null, Action onFail = null)
    {
        BuyReduceData req = new BuyReduceData();
        req.list = new List<BuyDiscountData>();
        req.discountType = discountType;
        for (int i = 0; i < list.Count; i++)
        {
            req.list.Add(list[i]);
        }
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.BuyDiscountItem,
            HttpMethod.POST,
            JsonConvert.SerializeObject(req),
            onReceive: arg0 =>
            {
                BuyRedeceRespon response = JsonConvert.DeserializeObject<BuyRedeceRespon>(arg0);
                onSuccess?.Invoke(response);
            }, onFail: arg0 =>
            {
                onFail?.Invoke();
            }, retryCount: 3);
    }

    public void GetProductOrderId(string productId, GiftOrderData giftOrderData, Action<bool, ProductOrderInfo> resultAction = null)
    {
        if (string.IsNullOrEmpty(productId))
        {
            resultAction?.Invoke(false, null);
            return;
        }

        BuyIapReq buyIapReq = new BuyIapReq();
        buyIapReq.productId = productId;
        if (giftOrderData != null)
        {
            buyIapReq.giftOrderData = giftOrderData;
        }

        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ProductIdExchange,
            HttpMethod.POST,
            JsonConvert.SerializeObject(buyIapReq),
            onReceive: arg0 =>
            {
                ProductOrderInfo response = JsonConvert.DeserializeObject<ProductOrderInfo>(arg0);
                resultAction?.Invoke(true, response);
            }, onFail: arg0 =>
            {
                resultAction?.Invoke(false, null);
            }, retryCount: 3);
    }

    public void GetPayOrderStatus(string budOrderId, Action<bool, OrderCheckRes?> resultAction = null)
    {
        if (string.IsNullOrEmpty(budOrderId))
        {
            resultAction?.Invoke(false, null);
            return;
        }

        JObject req = new JObject
        {
            ["budOrderId"] = budOrderId,
        };

        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.PayOrderStatus,
            HttpMethod.GET,
            JsonConvert.SerializeObject(req),
            onReceive: arg0 =>
            {
                OrderCheckRes response = JsonConvert.DeserializeObject<OrderCheckRes>(arg0);
                resultAction?.Invoke(true, response);
            }, onFail: arg0 =>
            {
                resultAction?.Invoke(false, null);
            });
    }

    public void GetDiscountCardStatus(Action<bool, DiscountCardStatusResponse> resultAction = null)
    {
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.DiscountCardStatus,
            HttpMethod.GET,
            "",
            onReceive: arg0 =>
            {
                DiscountCardStatusResponse response = JsonConvert.DeserializeObject<DiscountCardStatusResponse>(arg0);
                if (response != null)
                {
                    _discountCardStatusRsp = response;
                }

                resultAction?.Invoke(true, response);
            }, onFail: arg0 =>
            {
                resultAction?.Invoke(false, null);
            }, retryCount: 3);
    }

    public void GetSubscribeStatus(Action<bool, SubscribeStatusResponse> resultAction = null)
    {
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.SubscribeStatus,
            HttpMethod.GET,
            "",
            onReceive: arg0 =>
            {
                SubscribeStatusResponse response = JsonConvert.DeserializeObject<SubscribeStatusResponse>(arg0);
                if (response != null)
                {
                    _subscribeStatusRsp = response;
                }

                resultAction?.Invoke(true, response);
            }, onFail: arg0 =>
            {
                resultAction?.Invoke(false, null);
            }, retryCount: 3);
    }


    public void GetTaskList(string taskId, Action<bool, TaskListRsp> resultAction = null)
    {


        GetTaskListReq getTaskListReq = new GetTaskListReq()
        {
            idList = new List<string> { taskId }
        };

        var reqParam = JsonConvert.SerializeObject(getTaskListReq);


        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.TaskList, HttpMethod.POST, reqParam, content =>
        {
            var taskListRsp = JsonConvert.DeserializeObject<TaskListRsp>(content);
            resultAction?.Invoke(true, taskListRsp);
        }, failMessage =>
        {
            resultAction?.Invoke(false, null);
        });
    }

    public void GetTaskList(List<string> taskIds, Action<bool, TaskListRsp> resultAction = null)
    {


        GetTaskListReq getTaskListReq = new GetTaskListReq()
        {
            idList = taskIds
        };

        var reqParam = JsonConvert.SerializeObject(getTaskListReq);


        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.TaskList, HttpMethod.POST, reqParam, content =>
        {
            var taskListRsp = JsonConvert.DeserializeObject<TaskListRsp>(content);
            resultAction?.Invoke(true, taskListRsp);
        }, failMessage =>
        {
            resultAction?.Invoke(false, null);
        });
    }


    public void GetSubScribeReward(Action<bool> resultAction = null)
    {
        SubscribeRewardReq subscribeRewardReq = new SubscribeRewardReq()
        {
            type = (int)SubscribeRewardReqType.dailyReward
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.SubScribeReward,
            HttpMethod.POST,
            JsonConvert.SerializeObject(subscribeRewardReq),
            onReceive: arg0 =>
            {
                resultAction?.Invoke(true);
            }, onFail: arg0 =>
            {
                resultAction?.Invoke(false);
            }, retryCount: 3);
    }

    public static ProductInfo GetVipProductInfo(SubscribeVipType subscribeVipType,
        SubscribeStatusResponse subscribeStatusResponse)
    {
        if (subscribeStatusResponse == null)
        {
            return null;
        }

        List<ProductInfo> productInfos = subscribeStatusResponse.products;
        if (productInfos.Count == 0)
        {
            return null;
        }
        ProductInfo productInfo = productInfos.Find(x => x.type == (int)subscribeVipType);
        return productInfo;
    }

    #region Mock

    private List<ProductGemInfo> MockGems()
    {
        return new List<ProductGemInfo>()
        {
            new ProductGemInfo()
            {
                productId = "diamond60_m",
                price = "6 元",
                gemNum = 60,
                name = "测试60钻"
            },
            new ProductGemInfo()
            {
                productId = "diamond60_m",
                price = "6元",
                gemNum = 61,
                name = "测试60钻"
            },
            new ProductGemInfo()
            {
                productId = "diamond60_m",
                price = "6元",
                gemNum = 62,
                name = "测试60钻"
            },
            new ProductGemInfo()
            {
                productId = "diamond60_m",
                price = "6元",
                gemNum = 63,
                name = "测试60钻"
            },
            new ProductGemInfo()
            {
                productId = "diamond60_m",
                price = "6元",
                gemNum = 64,
                name = "测试60钻"
            },
            new ProductGemInfo()
            {
                productId = "diamond60_m",
                price = "6元",
                gemNum = 65,
                name = "测试60钻"
            }
        };
    }

    #endregion


    #region 直氪 - App 内购买

    /// <summary>
    /// 使用Gem购买商品（非pgc资源和ugc资源）
    /// </summary>
    /// <param name="productType"></param>
    /// <param name="productId"></param>
    /// <param name="resultHandler"></param>
    public void PayByGem(BUDProductType productType, string productId, Action<bool> resultHandler, int amount = 0)
    {
        var req = new JObject()
        {
            ["productType"] = (int)productType,
            ["productId"] = productId
        };

        if (amount > 0)
        {
            req["purchaseAmount"] = amount;
        }

        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.PayProductByGem,
            HttpMethod.POST,
            JsonConvert.SerializeObject(req),
            onReceive: msg =>
            {
                AccountDataManager.Inst.BalanceInfo.Refresh();
                resultHandler?.Invoke(true);
            }, onFail: arg0 =>
            {
                resultHandler?.Invoke(false);
            });
    }

    /// <summary>
    /// 购买SeasonPass
    /// </summary>
    /// <param name="productId"> 赛季ID </param>
    /// <param name="isLuxury"></param>
    /// <param name="resultHandler"></param>
    public void PaySeasonPassByGem(string productId, bool isLuxury, Action<bool> resultHandler)
    {
        var req = new JObject()
        {
            ["productType"] = (int)BUDProductType.SeasonPass,
            ["productId"] = productId,
            ["seasonPassInfo"] = new JObject()
            {
                ["buyType"] = isLuxury ? 1 : 0,
            }
        };

        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.PayProductByGem,
            HttpMethod.POST,
            JsonConvert.SerializeObject(req),
            onReceive: msg =>
            {
                AccountDataManager.Inst.BalanceInfo.Refresh();
                resultHandler?.Invoke(true);
            }, onFail: arg0 =>
            {
                resultHandler?.Invoke(false);
            });
    }




    #endregion


    #region 海外配置

    public List<PriceDefaultInfo> GetPriceDefaultInfos()
    {
        return defaultShowList_US;
    }

    public PriceLocalInfo GetPriceInfo(string productId)
    {
        PriceLocalInfo result = null;
        if (mobilePriceInfoList != null)
        {
            foreach (var info in mobilePriceInfoList)
            {
                if (info.productId == productId)
                {
                    result = info;
                    break;
                }
            }
        }

        //用本地配置兜底
        if (result == null)
        {
            foreach (var info in mobilePriceInfoListLocal)
            {
                if (info.productId == productId)
                {
                    result = info;
                    break;
                }
            }
        }


        return result;
    }

    public PriceLocalInfo GetPriceInfo(ProductIdType productIdType)
    {
        string productId = GetProductId(productIdType);
        return GetPriceInfo(productId);
    }

    public void GetPriceInfoSync(string productId, Action<PriceLocalInfo> callback)
    {
        if (mobilePriceInfoList != null)
        {
            PriceLocalInfo result = null;
            foreach (var info in mobilePriceInfoList)
            {
                if (info.productId == productId)
                {
                    result = info;
                    break;
                }
            }
            callback(result);
            return;
        }

        GetMobilePriceInfos((PriceInfoResponse) =>
        {
            if (PriceInfoResponse.priceInfos != null)
            {
                PriceLocalInfo result = null;
                foreach (var info in PriceInfoResponse.priceInfos)
                {
                    if (info.productId == productId)
                    {
                        result = info;
                        break;
                    }
                }
                callback(result);
            }
            else
            {
                callback(null);
            }
        }, () =>
        {
            callback(null);
        });
    }



    public List<string> GetProductIdList()
    {
        // var productIds = new List<string>();
        // if (productRes != null) {
        //     if (productRes.paidPackageList != null) {
        //         productIds.AddRange(productRes.paidPackageList.Select(tmp => tmp.productInfo.productId));
        //     }
        //     if (productRes.gemRechargeList != null) {
        //         productIds.AddRange(productRes.gemRechargeList.Select(tmp => tmp.productId));
        //     }
        // }
        // return productIds;
        var productIds = new List<string>();
        if (mobilePriceInfoListLocal != null)
        {
            productIds.AddRange(mobilePriceInfoListLocal.Select(tmp => tmp.productId));
        }
        return productIds;
    }

    /**
	 * 获取价格信息
	 */
    public void GetMobilePriceInfos(Action<PriceInfoResponse> onSuccess = null, Action onFail = null)
    {
        List<string> productIds = GetProductIdList();
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.getPriceList,
            (response) => OnMobilePriceInfoSuccess(response, onSuccess));
        MobileInterface.Instance.AddClientFail(MobileInterfaceDefine.getPriceList,
            (response) => OnMobileResponseFail(response, onFail));

        JObject jo = new JObject
        {
            ["productIds"] = new JArray(productIds)
        };
        MobileInterface.Instance.SendMessage(MobileInterfaceDefine.getPriceList, JsonConvert.SerializeObject(jo));
    }

    private void OnMobilePriceInfoSuccess(string response, Action<PriceInfoResponse> onSuccess = null,
        Action onFail = null)
    {
        //LoggerUtils.Log($"InAppPurchaseManager:OnMobilePriceInfoSuccess response={response}");
        MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.getPriceList);
        PriceInfoResponse priceInfoResponse = JsonConvert.DeserializeObject<PriceInfoResponse>(response);

        if (priceInfoResponse != null)
        {
            onSuccess?.Invoke(priceInfoResponse);
            mobilePriceInfoList = priceInfoResponse.priceInfos;
        }
        else
        {
            LoggerUtils.LogError("InAppPurchaseManager:OnMobilePriceInfoSuccess Decode Json Fail.");
            onFail?.Invoke();
        }
    }

    private void OnMobileResponseFail(string response, Action onFail = null)
    {
        LoggerUtils.LogError($"InAppPurchaseManager:OnMobileFail response={response}");

        onFail?.Invoke();
    }


    #endregion

}
