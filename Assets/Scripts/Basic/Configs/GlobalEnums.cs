using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 通用的全局枚举，跟后端保持一致
/// </summary>
/// <summary>
/// 奖励类型，对应后端统一奖励
/// </summary>
public enum BUDRewardType
{
    ErrRewardType = 0,
    RewardCoin = 1,
    RewardBadge = 2,
    RewardGem = 3,
    RewardGashaponVoucher = 4, //弃用
    RewardPgcResource = 5,
    RewardBalloon = 6, // 气球币(活动已结束)
    RewardPinkCoin = 7,
    RewardPianoCoin = 8, // 钢琴币
                     // 9 - 12 为创作者任务抽象奖励，
    RewardCreatorTitle1 = 9,  //创作者称号1
    RewardCreatorTitle2 = 10, //创作者称号2
    RewardCreatorTitle3 = 11, //创作者称号3
    RewardCreatorPubByGem = 12, //创作者能发布gem ugc
    RewardDressCoin = 13, //裙币
    RewardFrogCoin = 14, //🐸币
    RewardCrystalBall = 15, //🔮币
    RewardEnergyCoin = 16, // 能量币
    RewardMovieCoin = 17, // 电影币
    ConsumeTwist = 18, // 消费狂欢抽奖卷
    WinterCarnival = 19, // 冬日狂欢
    RewardChristmasCoin = 20, // s5 新增 圣诞币
    RewardMagicCoin = 21, // s5新增 魔法币
    RewardLuckyCoin = 22, // s5新增 幸运币
    RewardLuckyTicket = 23, // s5新增 幸运券
    RewardChristmasTicket = 24, //s5专属 铃铛券
    RewardCoinTicket = 25, //s5新增 金币券
    AnimeShoppingFestival = 26, //s5新增 购物车🛒币,
    RewardPgcBundle = 27, // s5新增 仅后端使用
    RewardHomepageSkin = 28, //s5新增 个人主页皮肤
    RewardGiftUgc = 29,
    RewardGiftPremiumSeasonPass = 30, //赠送高级版通行证
    RewardGiftDeluxeSeasonPass = 31, //赠送豪华版通行证
    RewardGiftAdvancedSeasonPassTier = 32, //赠送豪华版通行证升级包
    RewardGiftMonthlyVip = 33, //赠送月卡vip
    RewardGiftYearlyVip = 34, //赠送年卡vip
    RewardSeasonGachaCoin = 35, //s6新增，赛季扭蛋币，废弃
    RewardSkinSlot = 36, //皮肤卡位
    RewardSeasonPassRandomPack = 37, //通行证随机礼包
    RewardActive = 38, // 日活跃度
    RewardExperience = 39, // 赛季经验值
    RewardYouYouCoin = 40,  // s6 新增，优优币
    RewardPurpleDreamCoin = 41, //s6 新增，紫梦币
    RewardYuanJing = 42, // 缘晶💎
    RewardAvatarFrame = 43, //头像框
    RewardChatBubbles = 44, //聊天气泡
    RewardVipFreeTrail = 45, // vip 免费试用
    RewardYouYouCoinNewYearPack = 46, // s6新增 优优币新年礼盒
    RewardLuckyStar = 47, //s6新增 幸运星
    RewardPopularityTicket = 48, //人气券 s6
    RewardGiftTicket = 49, // 礼品券
    RewardRose = 50, // 玫瑰花
    RewardCreatorPoint = 51, //s7 创作者积分
    RewardKey = 52, // 钥匙🔑
    RewardPgcResourceFreeTrail = 53, // pgc试用
    RewardPurpleDreamTicket = 54, // 紫梦券
    RewardFireCracker = 55, // 爆竹
    RewardGiftNewYearLimitedPackage = 56, // 赠送2025新年限定礼包(辞岁烟花/金蛇贺岁)
    RewardAIBuddyIntimacyRate = 57,
    RewardCreatorLevelTitle = 58, //创作者等级头衔
    RewardBuddySummoningEffects = 59, //AIBuddy 召唤特效
    RewardPgcOptionalBox = 60, //s7-0 Pgc自选礼盒
    RewardSweetieTicket = 61, //s7-0 甜心券
    RewardAiBuddySlot = 62, //s7 伙伴卡位
    RewardUgcTemplateResource = 63, //s7 现在只有宠物模板
    RewardNightCap = 64, // 瞌睡帽
    RewardLotusLeaf = 65, // 荷叶🪷
    RewardCarrot = 66, // 胡萝卜🥕
    RewardUgcResource = 67, //ugc商品
    RewardRechargeDailyPackage = 68, // 每日充值幸运礼盒
    RewardAISeasonCoin = 69,//ai赛季币
    RewardAICore = 70, // AI 核心
    RewardLuckyBag = 71, // Lucky Bag//福袋 随机 【幸运币*1 优优币*1 徽章*10 金币*100】
    RewardAIKey = 72, // AI 钥匙
    AbandonedRewardExperience = 73, // 赛季经验值
    RewardLaborDaySeasonPass = 74, //五一通行证
    RewardZongzi = 75, // 粽子
    RewardGemReduction = 76, // 可降价的钻石数
    RewardNewBieTaskActive = 77, // 新手7天任务活跃度
    RewardCreatorCoin = 78, // 创作者币
    RewardParadiseBalloons = 79, // 乐园气球🎈
    RewardCommunityInstrumentTicket = 80, // 社区乐器兑换券
    RewardCommunitySkinTicket = 81, // 社区皮肤兑换券
    RewardCommunityAnimationTicket = 82, // 社区动作兑换券
    RewardCelebrationCoin = 83, //庆典币奖励
    RewardAnniversaryBox = 84, //周年庆宝箱
    RewardTypeUserTitle = 85, // 称号奖励
    RewardTypeEnterEffect = 86, // 进房播报
    RewardTypeFries = 87, // 薯条🍟
    RewardLuckyKoiTicket = 89, // 幸运锦鲤抽奖券
    RewardSeasonPassCoin = 90, // 通行证币
    RewardSeasonPassRedeemPackage = 91, // 赛季通行证兑换皮肤动作礼包
    RewardCollectionTicket = 92, // 典藏券
    RewardTypeMiaoCoin = 93, // 喵币
    RewardUGCVehicleTicket = 94,//UGC载具兑换券
    RewardCrystal = 95,//水晶
    RewardCrystalShards = 96,//水晶碎片
    RewardMusicNoteCrystal = 97,//乐符水晶
    RewardMusicNoteCrystalShards = 98,//乐符水晶碎片
    RewardGiftNewYearLimitedLotteryPackage = 99, // 赠送御剑飞行礼包
    RewardTypeNicknameFrame = 100, //昵称框
    AlbumExpansion = 101,// 相册扩容
    RewardTypeTitle = 102, // 新的称号类型
    TreePlantingDayWater = 103, //植树节水滴
    TreePlantingDayFertilizer = 104, //植树节肥料
    CreationBadge = 105, // 创作者赛季积分制徽章
    RewardAlbumTaskActive = 106, //相册活动活跃度
    RewardAlbumSelfieEmote = 107, // 相册自拍动作
    RewardTypeSockCoin = 108,  //袜袜币
    RewardTypeSockTailTicket = 109, // 粉绒小尾券
    RewardTypeSockYunyunTicket = 110, //暖橙晕晕券
    RewardBib = 111,// //小围兜
    RewardTypeShovel = 112, // 铲子（挖宝工具）
    RewardTheaterActivity = 113,//剧场活动活跃度
    RewardCommunityTheaterTicket = 114,// 社区剧本兑换券
    RewardBabyShrimp = 115,//虾元
     RewardTypeZZZCoin = 116,//啧币
    RewardTypeZZZPhantomCrystal = 117,//啧啧幻音水晶
    RewardTypeZZZPhantomCrystalShards = 118, //啧啧幻音碎片

    RewardTypeCameraPose = 1000,//自拍姿势，客户端预览用
    RewardTypeSelfDefine = 1001,//自定义，客户端预览用，从配置中取icon和预览图  目前为相册容量 
}
public enum RewardTypeUserTitleID
{
    Image_PurpleDesigner = 4,
    Image_StarDesigner = 5,
    Image_DreamDesigner = 6,
}

public enum RewardTypeEnterEffect
{
    Image_PurpleDesigner = 1004,
    Image_StarDesigner = 1005,
    Image_DreamDesigner = 1006,
}

public enum CurrencyType
{
    TotalRecharged = -3,
    FirstRecharge = -2,
    Free = -1,
    None = 0,
    Coin = 1,
    Badge = 2,
    Gem = 3,
    Ticket = 4, // 兑换券
    PinkCoin = 5, // 社区商品币
    GreenCoin = 6, // 创作者币
    Points = 7,
    EnergyCoin = 8,
    LuckyCoin = 9, //幸运币
    ChristmasCoin = 10, // 圣诞币 s5专属
    MagicCoin = 11, //魔法币 s5专属
    LuckyTicket = 12, //幸运券  s5新增
    ChristmasTicket = 13, //铃铛券 s5专属
    CoinTicket = 14, //金币券 s5新增
    SeasonGachaCoin = 15, // s6 赛季扭蛋币
    YouYouCoin = 16, // 优优币
    PurpleDreamCoin = 17, // s6 紫梦币
    LuckyStar = 18,//幸运星 s6
    PopularityTicket = 19, //人气券 s6
    GiftTicket = 20, // 礼品券
    SweetieTicket = 21, // 甜心券
    PurpleDreamTicket = 22, // 紫梦券
    AISeasonCoin = 23, // AI 赛季币
    AICore = 24, // AI 核心
    CelebrationCoin = 25, //庆典币
    CommunityInstrumentTicket = 26, // 社区乐器兑换券
    CommunitySkinTicket = 27, // 社区皮肤兑换券
    CommunityAnimationTicket = 28, // 社区动作兑换券
    KoiGachaCoin = 29, // 幸运锦鲤抽奖券
    SeasonPassCoin = 30, // 通行证币
    CollectionTicket = 31, // 典藏券
    MiaoCoin = 32, //喵币
    CommunityVehicleTicket = 33, //社区载具兑换券
    Crystal = 34, //水晶
    CrystalShards = 35, //水晶碎片
    MusicNoteCrystal = 36, //乐符水晶
    MusicNoteCrystalShards = 37, //乐符水晶碎片
    TreePlantingWater = 38, //水滴
    TreePlantingFertilizer = 39, //肥料
    SockCoin = 40, //袜币
    SockTailTicket = 41, //粉绒小尾券
    SockYunyunTicket = 42, //暖橙晕晕券
    Shovel=43,//逐浪沙沙限定货币,铲子
    CommunityTheaterTicket=44,//社区剧场兑换券
    ShrimpYuan = 45,//虾元
    ZZZCoin = 46,//绒币
    ZZZPhantomCrystal = 47, //啧啧幻音水晶
    ZZZPhantomCrystalShards = 48,//啧啧幻音碎片
}

public enum SlotType
{
    None = 0,
    OutfitSlot = 1,
    PetOutfitSlot = 2,
    PostureSlot = 3, // 单人快捷姿势卡位
    PoseDoubleSlot = 4,// 双人快捷姿势卡位
    PetPoseSingleSlot = 5, // 宠物单人快捷姿势卡位
    PetPoseDoubleSlot = 6,
    AIBuddySlot = 7,// aibuddy 卡位
}

//public enum AvatarFrameType
//{//头像框
//    AvatarFrameDefault = 0, //默认的头像框
//    AvatarFrameS6Recharge = 1,  // s6赛季累充头像框
//    AvatarFrameVip = 2,  // Vip 专属
//    ApartmentEscape = 3,  // 公寓逃脱模拟器预告活动头像框
//    AvatarFrame2025NewYear = 4,  //2025新年
//    AvatarFrameSnowFrame = 5,  //s6赛季扭蛋
//    AvatarFrameNewYearLimitedPack = 6,  //2025新年限定礼包-金蛇贺岁
//    NewYearFirecrackers = 7,  // 新年爆竹🧨头像框
//    AvatarFrameGroupConsumeActivity = 8,  // 组队消费活动头像框
//    AvatarFrameS7Recharge = 9,  // s7赛季累充头像框
//    AvatarFrameAIBuddyIntimacy = 10,  //  ai buddy 亲密度任务 头像框
//    AvatarFrameSweetheartParty = 11, // s7扭蛋 甜心舞会头像框
//    AvatarFrameLoveLogin = 12, // s7 情人节登录头像框
//    AvatarFrameS7AncientStyle = 13, //s7 赛季扭蛋古风头像框
//    AvatarFrameS7LevelNewCreator = 14, //s7 新晋创作者头像框
//    AvatarFrameS7LevelLightChaserCreator = 15, //s7 逐光创作者头像框
//    AvatarFrameS7LotteryGodOfWealth = 16, //s7扭蛋 喜迎财神春节小氪头像框
//    AvatarFrameSleepy = 17, // 瞌睡头像框
//    AvatarFrameS8StarryFairyTale = 18, //s8扭蛋 星夜童话季 萌喵彩虹头像框
//    AvatarFrameS8Recharge = 19, // S8 赛季累充头像框
//    AvatarFrameS9CoralSeaPromise = 20, //s9扭蛋 珊瑚海之约 海盗绯焰头像框
//    AvatarFrameCoralSea = 21 ,// s9 赛季累充珊瑚海之约
//    AvatarFrameUmi = 22 // UMI
//}

public enum AvatarFrameType
{//头像框
    AvatarFrameDefault = 0, //默认的头像框
    AvatarFrameS6Recharge = 1, // s6赛季累充头像框
    AvatarFrameVip = 2, // Vip 专属
    ApartmentEscape = 3, // 公寓逃脱模拟器预告活动头像框
    AvatarFrame2025NewYear = 4, //2025新年
    AvatarFrameSnowFrame = 5, //s6赛季扭蛋
    AvatarFrameNewYearLimitedPack = 6, //2025新年限定礼包-金蛇贺岁
    NewYearFirecrackers = 7, // 新年爆竹🧨头像框
    AvatarFrameGroupConsumeActivity = 8, // 组队消费活动头像框
    AvatarFrameS7Recharge = 9, // s7赛季累充头像框
    AvatarFrameAIBuddyIntimacy = 10, //  ai buddy 亲密度任务 头像框
    AvatarFrameSweetheartParty = 11, // s7扭蛋 甜心舞会头像框
    AvatarFrameLoveLogin = 12, // s7 情人节登录头像框
    AvatarFrameS7AncientStyle = 13, //s7 赛季扭蛋古风头像框
    AvatarFrameS7LevelNewCreator = 14, //s7 新晋创作者头像框
    AvatarFrameS7LevelLightChaserCreator = 15, //s7 逐光创作者头像框
    AvatarFrameS7LotteryGodOfWealth = 16, //s7扭蛋 喜迎财神春节小氪头像框
    AvatarFrameSleepy = 17, // 瞌睡头像框
    AvatarFrameS8StarryFairyTale = 18, //s8扭蛋 星夜童话季 萌喵彩虹头像框
    AvatarFrameS8Recharge = 19, // S8 赛季累充头像框
    AvatarFrameS9CoralSeaPromise = 20, //s9扭蛋 珊瑚海之约 海盗绯焰头像框
    AvatarFrameCoralSea = 21, // s9 赛季累充珊瑚海之约
    AvatarFrameGreenKiteDream = 22, // 绿野鸢梦 s9 五一劳动节头像框
    AvatarFrameSangSang = 23, // 丧丧护士头像框
    AvatarFrameMayDayCarnival = 24, //五一通关狂欢头像框
    AvatarFrameS9DragonBoatLottery = 25,// s9 粽夏芙瑶头像框
    AvatarFrameTypeS10Magic = 26, //S10魔法砰砰砰头像框
    AvatarFrameS10NewBie = 27, //S10新手头像框
    AvatarFrameScarletEleg = 28, //猩红挽歌头像框
    AvatarFrameAnniversaryYear = 29, //周年庆头像框
    AvaratFrameSweetheartBowe = 30, //破冰消费头像框
    AvatarFrameS12Recharge = 31, //s12 赛季累充头像框
    AvatarFrameMusicalPudding = 32, // 音符布丁头像框
    AvatarFrameChristmas = 33, // 圣诞派对头像框
    AvatarFrame2026NewYear = 34,// 2026新年
    AvatarFrameJinSeAnXiangLing = 35,//堇色暗香令头像框
    AvatarFrameUmi = 36,//Umi头像框
    AvatarFrameDhzy = 37,//灯华织页头像框
    AvatarFrameYxxd = 38,//元宵限定头像框
    AvatarFrameMgxf = 39,//玫瑰信筏头像框
    AvatarFrameXdmg = 40,//心动玫瑰头像框
    AvatarFrameQianqianWanwan = 41,//千千万万头像框
    AvatarFramePlantTree= 42,//春日森屿头像框
}
//public enum ChatBubblesType
//{//气泡
//    ChatBubblesDefault = 0,  //默认的头像框
//    ChatBubblesS6 = 1,  // s6赛季累充气泡框-冰雪
//    ChatBubblesVip = 2,  // 未使用
//    ChatBubbles2025NewYear = 3,  //2025新年
//    ChatBubbles2025NewYearLimitedPack = 4,  //2025新年限定礼包-金蛇贺岁
//    ChatBubblesAIBuddyIntimacy = 5,  // ai buddy 亲密度任务 气泡
//}

public enum AIResType
{
    AIChat = 1,
    AIYandere = 2
}


public enum AIResFreeState
{
    Free = 1,
    Buy = 2
}
