using System.CodeDom;

namespace Network.Http
{
    public class HttpCodeDefine
    {

        /// <summary>
        /// 用户账号被封禁Code
        /// </summary>
        public const int AccountBan = 502;
    }

    public class HttpUrlDefine
    {
        public const string deregister = "/user/deregister";
        public const string login = "/user/login";
        public const string setImage = "/image/set";

        public const string hospitalCata = "/aigame/gallery/list";
        public const string ParkCata = "/aigame/gallery/list";
        public const string SetVehicleImage = "/image/vehicle/set";
        /// <summary>
        /// 设置宠物形象
        /// </summary>
        public const string SetPetImage = "/image/pet/set";
        public const string SetCharacterImage = "/image/character/set"; //设置大厅 AI 伙伴角色
        public const string getUserImage = "/image/info";
        public const string hotUpdate = "/configuration/hotUpdate";
        public const string TokenData = "/image/userBalance";

        public const string setOc = "/image/oc/set";
        public const string batchDelOc = "/image/oc/batchDel";
        public const string ocList = "/image/oc/list";
        public const string payOcList = "/pay/slotList";
        public const string creatorConsume = "/pay/creator/consume";
        public const string appSetting = "/configuration/appSetting"; //获取设备信息

        public const string getRedDot = "/configuration/resReddot"; // 获取红点
        public const string delRedDot = "/configuration/delResReddot"; // 删除红点
        public const string lobbyInfo = "/configuration/lobbyInfo"; // 大厅配置
        public const string cleanRedDot = "/notification/redDot/clean"; // 清除红点


        public const string setCollect = "/ugc/collect"; // 收藏
        public const string getCollect = "/ugc/pgcStatus"; // 收藏状态
        public const string getBusiness = "/configuration/business";//是否展示充值栏
        #region UGC 地图相关
        public const string createList = "/ugc/map/draftList"; // 地图草稿列表
        public const string publishList = "/ugc/map/publishList"; // 地图发布列表
        public const string setMap = "/ugc/map/set";
        public const string mapInfo = "/ugc/map/info";
        public const string getBannedUgcs = "/ugc/map/getBannedUgcs"; //地图发布前置校验检查
        #endregion

        #region UGC 衣服相关
        public const string SetSkin = "/ugc/skin/set";
        public const string clothCreateList = "/ugc/skin/draftList";
        public const string clothPublishList = "/ugc/skin/publishList";
        public const string GetClothesInfo = "/ugc/skin/info";
        public const string GetClothesBatchInfo = "/ugc/skin/batchInfo";
        public const string GetMusicScoreBatchInfo = "/ugc/musicScore/batchInfo";
        #endregion

        #region UGC 搜索相关
        public const string getSelectedSectionList = "/recommend/getSelectedSectionList";
        public const string setSelectedSectionList = "/recommend/setSelectedSectionList";
        public const string searchWordList = "/recommend/searchWordList";
        public const string uploadSearchWords = "/other/upload/searchWords";
        #endregion

        #region UGC 乐谱相关
        public const string SetMusicScore = "/ugc/musicScore/set";
        public const string MusicScoreCreateList = "/ugc/musicScore/draftList";
        public const string MusicScorePublishList = "/ugc/musicScore/publishList";
        public const string GetMusicScoreInfo = "/ugc/musicScore/info";
        #endregion


        #region UGC 素材相关
        public const string setProp = "/ugc/prop/set";
        public const string propDraftList = "/ugc/prop/draftList";
        public const string propPublishList = "/ugc/prop/publishList";
        public const string propInfo = "/ugc/prop/info";
        public const string batchPropInfos = "/ugc/prop/batchInfo";
        #endregion

        #region UGC 材质相关
        public const string SetMaterial = "/ugc/material/set";
        public const string MaterialCreateList = "/ugc/material/draftList";
        public const string MaterialPublishList = "/ugc/material/publishList";
        public const string GetMaterialInfo = "/ugc/material/info";
        #endregion

        #region UGC 载具相关
        public const string SetVehicle = "/ugc/vehicle/set";
        public const string VehicleCreatList = "/ugc/vehicle/draftList";
        public const string VehiclePublishList = "/ugc/vehicle/publishList";
        public const string VehicleInfo = "/ugc/vehicle/info";
        public const string batchVehicleInfo = "/ugc/vehicle/batchInfo";
        #endregion

        #region UGC 通用
        public const string UGCBuy = "/ugc/consume";
        public const string UGCLike = "/ugc/like";
        public const string UGCInteractList = "/ugc/interactList";
        public const string UGCCollect = "/ugc/collect";
        public const string UGCInteractSection = "/ugc/interactSection";

        #endregion

        #region 推荐

        public const string sectionList = "/recommend/ugc/section";
        public const string sectionInfo = "/recommend/ugc/sectionInfo";
        public const string sectionInfoV2 = "/recommend/ugc/sectionInfoV2"; //试衣间调用这个接口需要调用SearchLogicMgr.AddFilterSearchParam添加过滤参数
        public const string gameSpotlight = "/recommend/ugc/map";
        public const string GetCharacterToneTags = "/recommend/getCharacterToneTags"; //获取AI伴侣音色标签列表 GET
        #endregion

        #region 个人主页

        public const string publicProfile = "/image/publicProfile";
        public const string publicList = "/ugc/publicList";
        public const string relationStatus = "/social/relation/status";
        public const string setRelaiton = "/social/relation/set";
        public const string relationAmount = "/social/relation/amount";
        public const string setPermission = "/image/permission/set";
        public const string userBalance = "/image/userBalance";
        #endregion

        #region 红点相关
        public const string reddotNotice = "/notification/redDot/num";
        #endregion


        #region 扭蛋机

        public const string gashaponSectionList = "/gashapon/sectionList"; // 获取扭蛋系列sections
        public const string toGasha = "/lottery/gacha"; // 扭蛋抽奖接口
        public const string gashaInfo = "/lottery/info";
        public const string GashaponReddem = "/pay/lottery/redeem";//扭蛋兑换接口
        public const string GashaponTaskReward = "/lottery/claimTaskReward";//扭蛋任务领奖
        public const string GashaponSpecialButton = "/lottery/specialButton"; // 扭蛋页特殊功能按钮
        #endregion

        #region 商城

        public const string storeBanner = "/store/bannerList"; // 获取商城banner
        public const string storeProduct = "/store/productList"; // 获取商城商品列表
        public const string storeResource = "/configuration/storeResource";
        public const string BanCompensation = "/store/banCompensation";
        public const string WeekRecommendedHair = "/recommend/weeklyHair"; // 获取每周推荐发型
        #endregion

        #region 促销
        public const string storeShape = "/recommend/flashSale/themes";//促销商品主题道具
        public const string shapeBanner = "/recommend/flashSale/banners";//促销商品banner
        #endregion

        #region TopPicks

        public const string seriesList = "/store/topPick/seriesList"; // topPick系列列表
        public const string outfitList = "/store/topPick/seriesInfo"; // topPick系列详情

        #endregion

        public const string Bag = "/configuration/backpack"; // 检查背包数据
        #region 社交相关
        public const string SetComment = "/ugcmap/setComment";
        public const string friendList = "/social/relation/list";
        public const string searchFriend = "/social/relation/search";
        public const string searchByID = "/social/relation/searchUserById";
        public const string setRelation = "/social/relation/set";
        public const string batchInfo = "/image/batchInfo";
        #endregion

        #region 聊天相关
        public const string ConversationList = "/chat/conversation/list";
        public const string OfflineMsg = "/chat/conversation/offlineMsg";
        public const string SetChat = "/chat/set";
        #endregion

        #region 联机相关
        public const string GetPublicMapList = "/engine/getPublicMapList"; // 检查背包数据
        public const string GetRelationServerList = "/engine/getRelationServerList";
        public const string GetServerInfoByCode = "/engine/getServerInfoByCode";
        #endregion

        #region 邮箱相关
        public const string MailList = "/mail/list"; // 获取邮件列表
        public const string MailClaim = "/mail/claim"; // 获取邮件附件
        public const string NotificationList = "/notification/list"; // 获取通知列表
        #endregion

        #region 充值相关
        public const string exchangePay = "/pay/exchange"; // 兑换
        public const string BuyUgcPay = "/pay/buy/ugc"; // 兑换
        public const string BuyPgcPay = "/pay/buy/pgc"; // 兑换
        public const string BuyProductPay = "/pay/buy/product"; // 兑换
        public const string ExchangeActivityCurrency = "/task/exchangeActivityCurrency"; // 钻石兑换活动币
        #endregion


        #region IAP

        /// <summary>
        /// 商品充值列表
        /// </summary>
        public const string ProductList = "/pay/iapList";

        /// <summary>
        /// 商品ID 兑换，获取后段商品ID，透传给U8
        /// </summary>
        public const string ProductIdExchange = "/pay/buy/iap";

        public const string PayOrderStatus = "/pay/orderStatus";

        public const string SubscribeStatus = "/pay/subscribe/status";

        public const string SubScribeReward = "/pay/subscribe/reward";

        public const string PayProductByGem = "/pay/buy/product";

        public const string DiscountCardStatus = "/task/discountCardStatus";

        public const string DiscountList = "/pay/discountList";

        public const string BuyDiscountItem = "/pay/buy/discount";
        /// <summary>
        /// 兑换码领取奖励
        /// </summary>
        public const string redeemClaim = "/pay/redeem/claim";

        #endregion

        #region 举报相关
        public const string Report = "/audit/report";
        #endregion

        #region 审核相关
        public const string AuditImage = "/audit/image";
        public const string AuditAudio = "/audit/audio";
        public const string AuditText = "/audit/text";
        #endregion

        #region UGC 搜索
        public const string SearchMap = "/search/map";
        public const string SearchProp = "/search/prop";
        public const string SearchMaterial = "/search/material";
        public const string SearchSkin = "/search/skin";
        public const string SearchMusicScore = "/search/musicScore";
        public const string SearchUgc = "/search/ugc";
        public const string SearchBundle = "/search/bundle";
        public const string SearchMusicTone = "/search/musicTone";
        public const string SearchAnimation = "/search/animation";
        public const string SearchAnimationMusic = "/search/animationMusic";
        public const string SearchPose = "/search/pose";
        public const string SearchNpc = "/search/npc";
        public const string SearchVehicle = "/search/vehicle";
        public const string SearchTheatre = "/search/theater";
        public const string SearchActorCard = "/search/actor";
        public const string SearchCharacterTone = "/search/characterTone"; //搜索AI伴侣音色 GET
        #endregion

        #region 任务相关
        public const string TaskList = "/task/taskList"; //任务详情
        public const string ClaimTaskRewards = "/task/claimTaskRewards"; //领取任务奖励

        /// <summary>
        /// 兑换新年福运签
        /// </summary>
        public const string claimNewYearLuckyLottery = "/task/claimNewYearLuckyLottery";
        #endregion

        #region 活动相关
        public const string ActivityList = "/task/activityList"; //活动详情
        public const string ClaimActivityReward = "/task/claimActivityReward"; //领取活动奖励
        public const string ActivityRedeemReward = "/task/activityRedeemReward"; //活动兑换奖励
        public const string PostEvent = "/task/postEvent";
        public const string TaskUvEvent = "/other/upload/task";
        #endregion
        #region SeasonPass

        public const string SeasonPassInfo = "/task/seasonPassInfo";
        public const string SeasonPassClaim = "/task/claimSeasonPassReward";
        #endregion

        #region 离线渲染相关

        public const string RenderBatchInfo = "/offline/render/batchGetInfo";

        #endregion

        #region 弹窗相关

        public const string LobbyPopupList = "/recommend/lobbyPopupList";
        public const string HideLobbyPopup = "/recommend/hideLobbyPopup";

        public const string ActivityNavInfo = "/recommend/activityNavInfo";
        #endregion

        #region 创作活动

        public const string ContestInfo = "/contest/info";
        public const string ContestJoin = "/contest/join";
        public const string ContestLucky = "/contest/luckyDraw";
        public const string ContestEntryList = "/contest/entryList";
        public const string ContestSearch = "/contest/search";
        public const string ContestRankingList = "/contest/rankingList";
        public const string ContestOcInfo = "/image/oc/info";

        public const string OcEntryList = "/contest/ocEntryList";
        public const string OcVote = "/contest/vote";
        #endregion

        #region 创作者
        public const string Center = "/level/center";
        public const string ClaimReward = "/level/claimReward";
        #endregion

        #region 创作者相关
        public const string CreatorScoreRankInfo = "/creation/rankList";
        public const string CreatorUserScoreInfo = "/creation/scores";
        public const string CreatorBadgeListInfo = "/creation/badgeList";
        #endregion

        #region UGC乐器

        public const string UGCToneDraftList = "/ugc/musicTone/draftList";
        public const string UGCTonePublishList = "/ugc/musicTone/publishList";
        public const string SetUGCTone = "/ugc/musicTone/set";

        #endregion
        public const string LimitedTimeBackpack = "/configuration/limitedTimeBackpack";

        #region UGC 音色
        public const string GetMusicTone = "/ugc/musicTone/info";
        
        #endregion

        #region 创作能量币
        public const string RewardCoin = "/pay/rewardCoin"; // 能量币打赏
        #endregion


        #region UGC动作工作室
        //动画 - AnimInfo
        public const string setAnim = "/ugc/animation/set";
        public const string animDraftList = "/ugc/animation/draftList";
        public const string animPublishList = "/ugc/animation/publishList";
        public const string getAnimInfo = "/ugc/animation/info";
        public const string batchAnimInfo = "/ugc/animation/batchInfo";

        //动画音色
        public const string animMusicPublishList = "/ugc/animationMusic/publishList";
        public const string setUgcAnimMusic = "/ugc/animationMusic/set";
        public const string getUgcAnimMusic = "/ugc/animationMusic/info";

        //姿势 - PoseInfo
        public const string GetPoseInfo = "/ugc/pose/info";
        public const string SetPose = "/ugc/pose/set";
        public const string poseDraftList = "/ugc/pose/draftList";
        public const string posePublishList = "/ugc/pose/publishList";
        public const string GetPoseBatchInfo = "/ugc/pose/batchInfo";

        //快捷姿势 -

        public const string saveQuickPose = "/ugc/pose/quickSave/set";
        public const string quickPoseList = "/ugc/pose/quickSave/list";
        public const string delQuickPose = "/ugc/pose/quickSave/batchDel";
        #endregion

        #region 新海外服新增

        public const string BindStatus = "/user/bindStatus";//老海外服用户，登录新

        #endregion

        public const string TaskGacha = "/task/gacha";
        public const string ClaimRechargeBenifits = "/pay/claimRechargeBenefits";
        public const string BusinessConfig = "/configuration/business";

        #region 赠礼相关
        public const string GiveGift = "/gift/give";
        public const string RequestGift = "/gift/request";
        public const string GiftUserList = "/gift/userList";
        public const string GiftSearchUser = "/gift/searchUser";
        public const string MailClick = "/mail/clickButton";
        public const string MailRead = "/mail/read";

        #endregion


        #region 组队消费

        public const string GroupLeave = "/group/leave"; // 退出队伍
        public const string GroupLeaveList= "/group/leave/list"; // 退出队伍列表
        public const string GroupLeaveHandle = "/group/leave/handle"; // 管理队伍邀请

        public const string GroupInviteList = "/group/invite/list"; // 队伍邀请列表
        public const string GroupInvite = "/group/invite"; // 邀请成员
        public const string GroupInviteUserList = "/group/invite/userList"; //邀请好友列表
        public const string GroupInviteSearchUser = "/group/invite/searchUser"; // 搜索好友列表

        public const string GroupInviteHandle = "/group/invite/handle"; // 管理队伍邀请
        public const string GroupApplyList = "/group/apply/list"; // 队伍申请列表
        public const string GroupApplyHandle = "/group/apply/handle"; // 管理申请列表
        #endregion

        #region AINPC相关
        public const string NpcDraftList = "/ugc/npc/draftList";
        public const string NpcPublishList = "/ugc/npc/publishList";
        public const string NpcSet = "/ugc/npc/set";
        public const string NpcInfo = "/ugc/npc/info";
        public const string NpcBatchInfo = "/ugc/npc/batchInfo";
        public const string NpcSearch = "/search/npc";
        public const string NpcSelectList = "/aigame/npcSelectList";
        #endregion

        public const string AIGameRankList = "/aigame/rankList";
        #region AIYandere
        public const string AIChat = "/aigame/play/stream";
        public const string AIResult = "/aigame/uploadResult";
        public const string PreStart = "/aigame/preStart";
        public const string AIUIChat = "/aigame/chat/stream";
        #endregion

        #region AIBuddy
        public const string AIBuddyList = "/aibuddy/list";
        public const string AIBuddySet = "/aibuddy/set";
        public const string AIBuddyResort = "/aibuddy/list/resort";
        public const string AIBuddyInfo = "/aibuddy/info";
        public const string AIBuddyChat = "/aibuddy/chat/stream";
        public const string AIBuddyChatHistory = "/aibuddy/chat/history";

        #endregion


        #region AI限购
        public const string aiLimitProducts = "/pay/aiLimitProducts";
        public const string usedLimit = "/aigame/usedLimit";

        #endregion

        #region 埋点
        public const string PopupStatusUpload = "/other/upload/popup";
        #endregion

        #region ugc 分享和上报
        public const string UgcGameShare = "/ugc/share";
        #endregion

        #region 赛季兑换
        public const string RedeemStore = "/task/redeemStore"; //兑换商店
        public const string RedeemProduct = "/task/redeemSeasonPassProduct"; //兑换
        #endregion

        #region 商城评分
        public const string UgcInteractAmount = "/ugc/interactAmount";
        public const string PopupRatingSet = "/image/popupRating/set";
        #endregion



        #region 相机功能
        public const string CameraImageUpload = "/camera/image/upload";
        #endregion

        public const string PlantWater = "/task/treePlanting/growth";
        public const string PlantGetSeed = "/task/treePlanting/planting";
        public const string PlantUserList = "/task/treePlanting/userList";

        public const string AppIconInfo = "/configuration/appIcon";


        #region UGC 相机相关
        public const string AlbumList = "/ugc/album/list";
        public const string SetAlbum = "/ugc/album/set";
        public const string AlbumInfo = "/ugc/album/info";
        #endregion


        #region 养成舱
        public const string CabinCharacterPublishList = "/ugc/character/publishList";//角色发布列表get
        public const string SearchCharacter = "/search/character"; //搜索角色列表 GET
        public const string CabinCharacterSet = "/ugc/character/set"; //角色设置post
        public const string CabinCharacterInfo = "/ugc/character/info";//角色详情get
        public const string CabinCharacterDraftList = "/ugc/character/draftList";//角色草稿列表get

        public const string CabinCharacterToneClone = "/ugc/doubao/clone";//角色音色克隆get
        public const string CabinCharacterToneBatchPreview = "/ugc/doubao/batchPreview";//批量获取音色音频post




        public const string CabinCharacterDoubaoAsr = "/ugc/doubao/asr"; //语音提取文字接口

        public const string CabinCharacterToneSet = "/ugc/characterTone/set"; //角色音色设置接口  POST
        public const string CabinCharacterToneInfo = "/ugc/characterTone/info"; //角色音色详情接口 GET
        public const string CabinCharacterTonePublishList = "/ugc/characterTone/publishList"; //角色音色发布列表接口 GET
        public const string CabinBudBoxList = "/box/deviceList";//获取box列表
        public const string CabinBudBoxState  = "/box/state";  //获取box状态
        public const string CabinBoxUnbind   = "/box/unbind"; //解绑 Box POST
        public const string CabinBoxBind   = "/box/bind"; //绑定 Box POST
        public const string CabinBoxUpdate   = "/box/update"; //更新 Box POST
        public const string CabinBoxHotUpdate = "/configuration/hotUpdate/cabin"; //获取 Cabin 固件最新版本 GET

        public const string CabinCharacterPackSet = "/ugc/characterPack/set"; //设置扩展包 Get

        public const string CabinCharacterPackBatchInfo = "/ugc/characterPack/batchInfo"; //批量获取扩展包详情 Get
        #endregion

        #region AI伙伴盒子
        public const string CharacterBoxDraftList   = "/ugc/characterBox/draftList";   // 草稿列表 GET
        public const string CharacterBoxPublishList = "/ugc/characterBox/publishList"; // 发布列表 GET
        public const string CharacterBoxSet         = "/ugc/characterBox/set";         // 设置（创建/编辑/发布/删除） POST
        public const string CharacterBoxInfoUrl     = "/ugc/characterBox/info";        // 详情 GET
        public const string CharacterBoxBatchInfo   = "/ugc/characterBox/batchInfo";   // 批量详情 GET
        public const string SearchCharacterBox      = "/search/characterBox";          // 搜索 GET
        #endregion


        #region 剧场演员
        public const string ActorInfo = "/ugc/actor/info";//演员详情
        public const string ActorSet = "/ugc/actor/set";//演员设置
        public const string ActorDraftList = "/ugc/actor/draftList";//演员草稿列表
        public const string ActorPublishList = "/ugc/actor/publishList";//演员发布列表
        public const string ActorPublish = "/ugc/actor/publish";//演员发布列表
        public const string ActorBatchInfo = "/ugc/actor/batchInfo";//批量演员信息
        #endregion
        #region 剧场剧本
        public const string TheatreInfo = "/ugc/theater/info";//剧场详情
        public const string TheatreSet = "/ugc/theater/set";//剧场设置
        public const string TheatreDraftList = "/ugc/theater/draftList";//剧场草稿列表
        public const string TheatrePublishList = "/ugc/theater/publishList";//剧场发布列表
        public const string TheatrePublish = "/ugc/theater/publish";//剧场发布列表
        public const string TheatreBatchInfo = "/ugc/theater/batchInfo";//批量剧场信息

        #endregion

        #region  
        public const string createBotStream = "/boxchat/createBot/stream"; //养成舱创建角色聊天
        public const string boxchatStream = "/boxchat/stream"; //养成舱聊天
        public const string boxSessionList = "/box/sessionList"; //文字聊天会话列表 0-文字 1-语音通话
        public const string boxAudioHistory = "/box/audioHistory"; //语音通话历史记录
        public const string boxTextHistory = "/box/textHistory"; //文字聊天历史记录
        public const string boxTextAudio = "/box/textAudio"; //文字聊天内容转语音
        public const string createBotTags = "/boxchat/createBot/matchTags"; //botProfile
        public const string aiCreditDetail = "/pay/aiCredit/detail"; //ai币详细

        

        #endregion

    }
}
