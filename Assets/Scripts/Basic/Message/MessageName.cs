// @Author: YangJie
// @Description:
// @Date:  2023/07/12
// @Modify:

namespace Message
{
    public class MessageName
    {
        public const string StartEnterGame = "StartEnterGame";
        public const string OverEnterGame = "OverEnterGame";

        public const string TcpLoginSuccess = "TcpLoginSuccess";

        /// <summary>
        /// 用户登录成功
        /// </summary>
        public const string LoginSuccess = "LoginSuccess";

        /// <summary>
        /// 开始下载远端 URL 元数据
        /// </summary>
        public const string StartLoadRemoteMetadata = "StartLoadRemoteMetadata";

        /// <summary>
        /// 结束加载远端 URL 元数据
        /// </summary>
        public const string OverLoadRemoteMetadata = "OverLoadRemoteMetadata";

        /// <summary>
        /// 开始构建地图
        /// </summary>
        public const string StartBuildMap = "StartBuildMap";

        /// <summary>
        /// 新手引导结束
        /// </summary>
        public const string GetCurrencyAnimation = "GetCurrencyAnimation";
        
        /// <summary>
        /// 播放用户称号
        /// </summary>
        public const string PlayUserTitle = "PlayUserTitle";

        /// <summary>
        /// 结束构建地图
        /// </summary>
        public const string OverBuildMap = "OverBuildMap";


        /// <summary>
        /// 开始准备退出游戏
        /// </summary>
        public const string StartExitGame = "StartExitGame";

        /// <summary>
        /// 退出游戏
        /// </summary>
        public const string OverExitGame = "OverExitGame";
        /// <summary>
        /// 衣服编辑器内发布成功
        /// </summary>
        public const string ClothPublishInEditSuccess = "ClothPublishInEditSuccess";

        /// <summary>
        /// UndoRedo 更新UI面板
        /// </summary>
        public const string UpdateUndoView = "UpdateUndoView";

        /// <summary>
        /// 通关图：通知进行通关条件判断
        /// </summary>
        public const string PassLevelJudging = "PassLevelJudging";

        /// <summary>
        /// 通关倒计时暂停通知
        /// </summary>
        public const string CountDownPauseAndRecord = "CountDownPauseAndRecord";
        public const string CountDownContinue = "CountDownContinue";

        /// <summary>
        /// 增删红点
        /// </summary>
        public const string AddRedDot = "AddRedDot";
        public const string RemoveRedDot = "RemoveRedDot";

        /// <summary>
        /// 修改用户数据
        /// </summary>
        public const string EditUserInfo = "EditUserInfo";

        public const string OnUserStyleSuccess = "OnUserStyleSuccess";

        /// <summary>
        /// 通知刷新用户Token数据
        /// </summary>
        public const string TokenUpdate = "TokenUpdate";

        /// <summary>
        /// 红点通知
        /// </summary>
        public const string ReddotNotice = "ReddotNotice";

        /// <summary>
        /// 首次打开体型
        /// </summary>
        public const string FirstShapeOpenNotice = "FirstShapeOpenNotice";

        public const string SpecialProps = "SpecialProps";
        /// <summary>
        /// 自己取消Emote (移动或使用跳跃取消Emote）
        /// </summary>
        public const string SelfCancelEmote = "SelfCancelEmote";

        /// <summary>
        /// 自己取消Emote (移动或使用跳跃取消Emote）,仅限AI游戏
        /// </summary>
        public const string AICancelEmote = "AICancelEmote";

        public const string AOEForceSelfEmote = "AOEForceSelfEmote";

        /// <summary>
        /// 同步互斥系统状态
        /// </summary>
        public const string SyncPlayerStatus = "SyncPlayerStatus";

        /// <summary>
        /// 同步地图信息完成
        /// </summary>
        public const string SyncMapInfoComplete = "SyncMapInfoComplete";

        /// <summary>
        /// 进房批量创建玩家完成
        /// </summary>
        public const string BatchCreatePlayers = "BatchCreatePlayers";

        /// <summary>
        /// 背包数据消息
        /// </summary>
        /// 数据准备就绪
        public const string AvaterDatabaseIsReady = "AvaterDatabaseIsReady";
        /// 检查数据
        public const string AvaterDatabaseCheck = "AvaterDatabaseCheck";
        /// 数据发生改变
        public const string AvaterDatabaseOnDataChange = "AvaterDatabaseOnDataChange";
        /// 数据与服务端对不上
        public const string AvaterDatabaseOnDataRebuild = "AvaterDatabaseOnDataRebuild";
        /// 服务器触发数据更新
        public const string AvaterDatabaseUpdate = "AvaterDatabaseUpdate";
        // 异步加载成功
        public const string OnAvatarPutOnSuccess = "OnAvatarPutOnSuccess";
        
        /// <summary>
        /// 玩家退出
        /// </summary>
        public const string PlayerLeave = "PlayerLeave";

        /// <summary>
        /// 玩家进入房间（被创建后）
        /// </summary>
        public const string PlayerEnter = "PlayerEnter";

        /// <summary>
        /// 交互板上板子
        /// </summary>
        public const string InteractiveOnBoard = "InteractiveOnBoard";

        /// <summary>
        /// 交互板下绊子
        /// </summary>
        public const string InteracriveDownBoard = "InteracriveDownBoard";
        /// <summary>
        /// 交互板被打断
        /// </summary>
        public const string InteracriveInterrupt = "InteracriveInterrupt";

        /// <summary>
        /// 用户余额改变
        /// </summary>
        public const string OnPlayerInfoAccountChange = "OnPlayerInfoAccountChange";

        /// <summary>
        /// 加好友
        /// </summary>
        public const string AddFriendSuccess = "AddFriendSuccess";

        /// <summary>
        /// 关注
        /// </summary>
        public const string FollowSuccess = "FollowSuccess";

        /// <summary>
        /// 购买vip成功
        /// </summary>
        public const string BuyVipSuccess = "BuyVipSuccess";

        /// <summary>
        /// 购买Oc
        /// </summary>
        public const string BuySlotResult = "BuySlotResult";

        /// <summary>
        /// 购买一元礼包
        /// </summary>
        public const string BuyStartPack = "BuyStartPack";

        public const string UICameraMode = "UICameraMode";

        public const string SelfieMode = "SelfieMode";
        public const string SelfieChangePose = "SelfieChangePose";
        public const string LensMode = "LensMode";
        // 请求切换自拍模式（true=进入自拍，false=退出自拍）
        public const string ShowSelfieSticker = "ShowSelfieSticker";
        public const string CameraLandMarkTrigEnter = "CameraLandMarkTrigEnter";
        public const string UICameraModeSelfieRequest = "UICameraModeSelfieRequest";
        public const string UICameraModeHideUI = "UICameraModeHideUI";
        public const string UICameraModeCloseCurrent = "UICameraModeCloseCurrent";

        public const string UICameraModeCloseLensControl = "UICameraModeCloseLensControl";
        public const string UICameraModeOpenLensControl = "UICameraModeOpenLensControl";

        public const string UICameraModeGlobalClose = "UICameraModeGlobalClose";

        public const string UICameraModeReset = "UICameraModeReset";
        public const string UICameraModeFovChanged = "UICameraModeFovChanged";
        // 请求在 CameraMode 下打开 NPC（AI 伙伴）子菜单（世界中点击伙伴"互动选项"时改走此处而非独立 OptionPanel）
        public const string UICameraModeOpenNpc = "UICameraModeOpenNpc";

        public const string OnBuyUgcItemSuccess = "OnBuyUgcItemSuccess";

        public const string FootSound = "FootSound";

        public const string GameSound = "GameSound";
        public const string StopGameSound = "StopGameSound";

        public const string ShapeItemEvent = "ShapeItemEvent";
        public const string ShapeEmoteItemEvent = "ShapeEmoteItemEvent";

        public const string BuyShapeRefresh = "BuyShapeRefresh";

        //
        // /// <summary>
        // /// 获取渠道id
        // /// </summary>
        // public const string getChannelId = "getChannelId";

        // Profiler 信息更新
        public const string UpgradeProfilerInfo = "UpgradeProfilerInfo";

        //TCP通知刷新好友列表
        public const string OnTcpNotifyRefreshFriendList = "OnTcpNotifyRefreshFriendList";

        /// <summary>
        /// 已打开衣服编辑器
        /// </summary>
        public const string DidOpenUgcCloseEidtPage = "DidOpenUgcCloseEidtPage";


        /// <summary>
        /// 聊天消息
        /// </summary>
        public const string ChatMessage = "ChatMessage";

        public const string BusinessLiveConfigUpdate = "BusinessLiveConfigUpdate";

        public const string ShowToast = "ShowToast";

        public const string SelectAssetItem = "SelectAssetItem";

        public const string UpdateHallTask = "UpdateHallTask";

        public const string ChatMutePopup = "ChatMutePopup"; //禁言

        public const string AccountSuspension = "AccountSuspension"; //封号

        #region UGC乐器
        public const string OnUgcTonePublishedListChange = "OnUgcTonePublishedListChange";
        public const string OnUgcInstrumentDraftsListChange = "OnUgcInstrumentDraftsListChange";
        public const string OnUgcInstrumentPublishedListChange = "OnUgcInstrumentPublishedListChange";
        public const string OnGetPlayerHoldInstrument = "OnGetPlayerHoldInstrument";
        public const string UgcInstrumentDidPublishedNew = "UgcInstrumentDidPublishedNew";
        public const string OnExitPlayInstrumentAnim = "OnExitPlayInstrumentAnim";
        #endregion

        #region UGC乐谱
        public const string OnUgcMusicScorePublishedListChange = "OnUgcMusicScorePublishedListChange";
        public const string OnUgcMusicScoreDraftsListChange = "OnUgcMusicScoreDraftsListChange";
        public const string OnMusicScorePlay = "OnMusicScorePlay";
        public const string UgcMusicScoreDidPublishedNew = "UgcMusicScoreDidPublishedNew";
        public const string UgcToneDidPublishedNew = "UgcToneDidPublishedNew";
        #endregion

        public const string OnTryListenMIChange = "OnTryListenMIChange";
        public const string OnTryListenMSChange = "OnTryListenMSChange";

        public const string OnRefreshTaskDataAfterBack = "OnRefreshTaskDataAfterBack";
        public const string OnMusicScorePreviewSelect = "OnMusicScorePreviewSelect";

        #region 背景音乐控制

        /// <summary>
        /// 压低背景音乐
        /// </summary>
        public const string ReduceMusic = "ReduceMusic";

        /// <summary>
        /// 恢复背景音乐
        /// </summary>
        public const string RestoreMusic = "RestoreMusic";
        #endregion

        public const string OnAssetDelete = "OnAssetDelete";

        public const string NewComerCommunityCoin = "NewComerCommunityCoin";

        /// <summary>
        /// 切换系统语言
        /// </summary>
        public const string OnSwitchLanguage = "OnSwitchLanguage";

        #region UGC动作工作室
        public const string OnUgcAnimStudioDraftListChange = "OnUgcAnimStudioDraftListChange";
        public const string OnUgcAnimStudioPublishedListChange = "OnUgcAnimStudioPublishedListChange";
        public const string OnUgcAnimDidPublishedNew = "OnUgcAnimDidPublishedNew";

        #endregion

        public const string OnPurchaseLimitedPackageSuccess = "OnPurchaseLimitedPackageSuccess";
        public const string RefreshCumData = "RefreshCumData";
        public const string SendGiftSuccess = "SendGiftSuccess";
        public const string OnPurchaseNewYearPackageSuccess = "OnPurchaseNewYearPackageSuccess";

        #region 组队消费
        public const string RefreshGroupConsumeApplyData = "RefreshGroupConsumeApplyData";
        #endregion
        //双人表情状态变化
        public const string LinkEmoteStateChange = "LinkEmoteStateChange";
        public const string BuddyLinkEmoteStateChange = "BuddyLinkEmoteStateChange";
        public const string LinkEmoteShowUnlink = "LinkEmoteShowUnlink";
        // 其他玩家召唤 Cabin AI 伙伴（含待机数据 + 本次唤醒动作）；GameUI 层据此驱动其他端 buddy 待机/动作
        public const string OnRecvBuddySummonSync = "OnRecvBuddySummonSync";
        // 其他玩家触发口令互动；GameUI 层据此驱动其他端 buddy 播动作+语音
        public const string OnRecvBuddyInteractSync = "OnRecvBuddyInteractSync";
        // 口令互动的聊天显示（发起侧本地 + 接收侧广播）：参数 (playerId, InteractSyncData)
        // 驱动 buddy 头顶气泡(台词) + 聊天 Chat 分类(玩家@伙伴:指令) + Emote 分类(伙伴:动作名)
        public const string OnBuddyCommandChat = "OnBuddyCommandChat";
        // AI 伙伴乘坐/离开载具状态变化：参数 (driverUid, bool isOnVehicle)
        // 用于暂停/恢复伙伴待机、隐藏/恢复头顶交互按钮
        public const string OnBuddyVehicleStateChange = "OnBuddyVehicleStateChange";
        // 地图放置的 AI 伙伴（AIBuddyInMapBehaviour）需要 UI 侧拉取 Cabin 数据并装配：参数 (AIBuddyInMapBehaviour behaviour)
        // Game 行为层在运行态广播，UI 侧编排器（AIBuddyInMapUIManager）监听后拉取数据回调装配
        public const string OnAIBuddyInMapNeedSetup = "OnAIBuddyInMapNeedSetup";
        // 地图 AI 伙伴行为层销毁前广播，UI 侧清理对应缓存/盒子：参数 (AIBuddyInMapBehaviour behaviour)
        public const string OnAIBuddyInMapRemoved = "OnAIBuddyInMapRemoved";
        #region SeasonPass

        public const string RefreshSeasonPass = "RefreshSeasonPass";
        public const string RefreshSeasonPassTask = "RefreshSeasonPassTask";

        public const string SeasonPassDataUpdate = "SeasonPassDataUpdate";
        #endregion


        public const string OnChangeHeadCycleSuccess = "OnChangeHeadCycleSuccess";

        #region IncubationCabin
        public const string OnCabinDraftListChange = "OnCabinDraftListChange";
        public const string OnCabinPublishListChange = "OnCabinPublishListChange";
        /// <summary>角色创建或复制成功，参数：(info: CabinCharacterUgcInfo)</summary>
        public const string OnCabinDraftCharacterAdded = "OnCabinDraftCharacterAdded";
        /// <summary>角色属性编辑成功，参数：(info: CabinCharacterUgcInfo)</summary>
        public const string OnCabinDraftCharacterEdited = "OnCabinDraftCharacterEdited";
        /// <summary>角色上架成功，参数：(info: CabinCharacterUgcInfo，ugcclass 已变为 Published)</summary>
        public const string OnCabinDraftCharacterPublished = "OnCabinDraftCharacterPublished";
        /// <summary>角色删除成功，参数：(id: string)</summary>
        public const string OnCabinDraftCharacterDeleted = "OnCabinDraftCharacterDeleted";        
        /// <summary>角色下架成功，参数：(id: string)</summary>
        public const string OnCabinDraftCharacterUnPublished = "OnCabinDraftCharacterUnPublished";
        /// <summary>BOX角色局部数据变更，参数：(characterId: string, updateType: CabinCharacterUpdateType)</summary>
        public const string OnCabinCharacterUpdated = "OnCabinCharacterUpdated";
        /// <summary>IncubationCabinPanel 关闭时皮肤包选择发生变更，需重新截图更新封面</summary>
        public const string OnCabinSkinChanged = "OnCabinSkinChanged";
        /// <summary>编辑器姿势选中，参数：(poseData: string)</summary>
        public const string OnCabinEditorPoseSelected = "OnCabinEditorPoseSelected";
        /// <summary>编辑器缩放变更，参数：(scale: float)</summary>
        public const string OnCabinEditorScaleChanged = "OnCabinEditorScaleChanged";
        /// <summary>编辑器横向位移变更，参数：(posX: float)</summary>
        public const string OnCabinEditorPosXChanged = "OnCabinEditorPosXChanged";
        /// <summary>编辑器纵向位移变更，参数：(posY: float)</summary>
        public const string OnCabinEditorPosYChanged = "OnCabinEditorPosYChanged";
        /// <summary>扩展包创建成功，参数：(packInfo: CabinCharacterPackInfo)</summary>
        public const string OnCabinExtPackCreated = "OnCabinExtPackCreated";
        /// <summary>扩展包编辑保存成功，参数：(packInfo: CabinCharacterPackInfo)</summary>
        public const string OnCabinExtPackUpdated = "OnCabinExtPackUpdated";
        /// <summary>扩展包编辑删除成功，参数：(packInfo: CabinCharacterPackInfo)</summary>
        public const string OnCabinExtPackDelect = "OnCabinExtPackDelect";
        /// <summary>刷新皮肤列表</summary>
        public const string OnCabinRefreshSkinRoleList = "OnCabinRefreshSkinRoleList";
        /// <summary>更新预览 Avatar，参数：(characterData: CharacterData)</summary>
        public const string OnCabinUpdateAvatar = "OnCabinUpdateAvatar";
        /// <summary>触发重新拍照，参数：(callback: Action&lt;bool, string&gt;)</summary>
        public const string OnCabinTakeMatchPhoto = "OnCabinTakeMatchPhoto";
        /// <summary>刷新基础信息</summary>
        public const string OnCabinRefreshBaseMsg = "OnCabinRefreshBaseMsg";
        /// <summary>刷新互动内容</summary>
        public const string OnCabinRefreshInteractContent = "OnCabinRefreshInteractContent";
        /// <summary>音色变更后刷新唤醒动作（Node2）和口令互动（Node3）的 Item 数据</summary>
        public const string OnCabinRefreshInteractNode2And3 = "OnCabinRefreshInteractNode2And3";
        /// <summary>开始预览唤醒动作，参数：(interaction: characterInteraction)</summary>
        public const string OnCabinBeginPreviewActivation = "OnCabinBeginPreviewActivation";
        /// <summary>开始预览口令互动，参数：(voiceCommands: voiceCommands)</summary>
        public const string OnCabinBeginPreviewVoiceCommands = "OnCabinBeginPreviewVoiceCommands";
        /// <summary>创建发布角色音色</summary>
        public const string OnCreatToneInfo = "OnCreatToneInfo";
        /// <summary>养成舱角色点赞数变更，参数：(likeAmount: int)</summary>
        public const string OnCabinCharacterLikeChange = "OnCabinCharacterLikeChange";
        #endregion
        #region budBox
        /// <summary>budbox列表初始化完成</summary>
        public const string OnBudBoxListInit = "OnBudBoxListInit";
        /// <summary>连接MQTT服务器成功</summary>
        public const string OnLinkMqttBrokerFinish = "OnLinkMqttBrokerFinish";
        /// <summary>导入角色完成</summary>
        public const string OnImportBoxCharacterFinish = "OnImportBoxCharacterFinish";
        /// <summary>删除角色完成</summary>
        public const string OnDelectBoxCharacterFinish = "OnDelectBoxCharacterFinish";
        /// <summary>BoxData数据变更检查结果通知</summary>
        public const string OnDetectionChange = "OnDetectionChange";
        /// <summary>Box 硬件在线/离线状态变更，参数：(deviceId: string)</summary>
        public const string OnBoxDeviceStateChanged = "OnBoxDeviceStateChanged";
        /// <summary>Box 硬件名称修改：(deviceId: string)</summary>
        public const string OnBudBoxNameChange = "OnBudBoxNameChange";

        /// <summary>Box 硬件休眠状态改变：(deviceId: string)</summary>
        public const string OnBudBoxActiveChange = "OnBudBoxActiveChange";

        /// <summary>Box 硬件通话状态改变：(deviceId: string)</summary>
        public const string OnBudBoxCallChange = "OnBudBoxCallChange";

        /// <summary>Box 屏幕亮屏/熄屏状态改变：(deviceId: string)</summary>
        public const string OnBudBoxScreenChange = "OnBudBoxScreenChange";

        /// <summary>Box 角色（导入或皮肤切换）发生变更，参数：(deviceId: string)</summary>
        public const string OnBoxCharacterChanged = "OnBoxCharacterChanged";

        /// <summary>Box 硬件基础属性还原</summary>
        public const string OnBudBoxBaseDataRest = "OnBudBoxBaseDataRest";


        /// <summary>Box 硬件恢复出厂设置</summary>
        public const string OnBudBoxRestInit = "OnBudBoxRestInit";
        /// <summary>Box 硬件升级结果</summary>
        public const string OnBudBoxUpgradeResult = "OnBudBoxUpgradeResult";
        /// <summary>硬件侧响应 sw2hw_askBleOpen，蓝牙/WiFi 已就绪，参数：(deviceId: string)</summary>
        public const string OnSw2HwBleOpenReady = "OnSw2HwBleOpenReady";
        /// <summary>Box 场景同步结果广播，参数 bool：true=成功</summary>
        public const string OnBoxSceneSyncResult = "OnBoxSceneSyncResult";
        /// <summary>当前活跃 Box 切换完成，_budBoxData 已更新；用于面板已在 window 中时主动刷新 UI</summary>
        public const string OnActiveBudBoxChanged = "OnActiveBudBoxChanged";
        #endregion

        #region AINpc
        public const string OnAINpcStudioDraftListChange = "OnAINpcStudioDraftListChange";
        public const string OnAINpcStudioPublishedListChange = "OnAINpcStudioPublishedListChange";
        public const string OnAINpcDidPublishedNew = "OnAINpcDidPublishedNew";
        public const string OnAIBuddyInfoUpdated = "OnAIBuddyInfoUpdated";
        public const string OnAINpcChatPanelClose = "OnAINpcChatPanelClose";
        #endregion

        #region AI游戏相关
        // ai 游戏中点击 npc
        public const string OnAINPCTouchClick = "OnAINPCTouchClick";
        public const string TypeData = "TypeData";
        #endregion

        public const string ResumeActivityEmote = "ResumeActivityEmote";
        public const string OnStoreMallPanelClose = "OnStoreMallPanelClose";

        #region AI_Hospital
        public const string OnS9MainDoorClick = "OnS9MainDoorClick";
        public const string OnS9DustManChangeClothes = "OnS9DustManChangeClothes";
        public const string OnS9ChatEmoteClick = "OnS9ChatEmoteClick";
        public const string OnS9TargetAreaTrigEnter = "OnS9TargetAreaTrigEnter";
        public const string OnS9UgcTargetFinished = "OnS9UgcTargetFinished";
        public const string OnS9MainDoorOpen = "OnS9MainDoorOpen";
        public const string OnS9UgcMainDoorLock = "OnS9UgcMainDoorLock";
        public const string OnS9HeatRankItemClick = "OnS9HeatRankItemClick";
        public const string OnS9UpdateGiftState = "OnS9UpdateGiftState";
        public const string OnS9GameFinish = "OnS9GameFinish";
        public const string OnS9StoreTips = "OnS9StoreTips";
        public const string OnS9StoreItemClick = "OnS9StoreItemClick";
        public const string OnHospitalGameFail = "OnHospitalGameFail";
        public const string OnS9GuideStepClick = "OnS9GuideStepClick";
        public const string OnS9GuideStepAction = "OnS9GuideStepAction";

        //park
        public const string OnS11GuideStepClick = "OnS11GuideStepClick";

        public const string OnS11GuideStepAction = "OnS11GuideStepAction";
        public const string OnS11EmoteWithMsg = "OnS11EmoteWithMsg";
        public const string OnS11ChatEmoteClick = "OnS11ChatEmoteClick";
        public const string OnParkGameFail = "OnParkGameFail";
        public const string OnParkBeginNpcTalk = "OnParkBeginNpcTalk"; //第一幕npc讨论的对话
        public const string OnSyncNpcLocation = "OnSyncNpcLocation"; //npc位置同步


        public const string Park_GetNextSceneData = "Park_GetNextSceneData";
        public const string Park_CountDownEnd = "Park_CountDownEnd";
        
        /// <summary>
        /// 点击Emote时发消息显示在左上角聊天框
        /// </summary>
        public const string OnS9EmoteWithMsg = "OnS9EmoteWithMsg";
        #endregion

        public const string SetOperationPanelEnable = "SetOperationPanelEnable";
        public const string ParkTriggerNextHistory = "ParkTriggerNextHistory";
        public const string ParkNpcTalkOver = "ParkNpcTalkOver";
        /// <summary>
        /// 红点状态变化
        /// </summary>
        public const string OnHospitalSeasonRedDotStateChanged = "OnHospitalSeasonRedDotStateChanged";

        /// <summary>
        /// 载具编辑器点击
        /// </summary>
        public const string OnTouchForVehiclePerson = "OnTouchForVehiclePerson";

        public const string OnUgcVehicleDraftsListChange = "OnUgcVehicleDraftsListChange";

        public const string OnUgcVehiclePublishedListChange = "OnUgcVehiclePublishedListChange";

        public const string UgcVehicleDidPublishedNew = "UgcVehicleDidPublishedNew";

        public const string VehicleEditCloseRefresh = "VehicleEditCloseRefresh";

        #region 游园卡,道具交互
        public const string OnPropInteract = "OnPropInteract";
        public const string OnPropInteractWithNpc = "OnPropInteractWithNpc";
        public const string OnTaskConditionComplete = "OnTaskConditionComplete";
        #endregion


        #region 载具游戏
        public const string OnSelfVehicleCreated = "OnSelfVehicleCreated"; // 玩家创建载具成功了
        public const string OnSelfPgcVehicleCreated = "OnSelfPgcVehicleCreated"; // 玩家创建Pgc载具成功了
        public const string OnSelfGetInVehicle = "OnSelfGetInVehicle"; // 玩家上车成功了
        public const string OnVehicleTryHonking = "OnVehicleTryHonking"; // 玩家尝试鸣笛
        public const string OnVehicleHonkingStart = "OnVehicleHonkingStart"; // 载具鸣笛开始
        public const string OnVehicleHonkingStop = "OnVehicleHonkingStop"; // 载具鸣笛停止
        public const string OnVehicleMovingSoundStart = "OnVehicleMovingSoundStart"; // 载具移动声音开始
        public const string OnVehicleMovingSoundStop = "OnVehicleMovingSoundStop"; // 载具移动声音停止
        public const string OnPlayerTryGetOutVehicle = "OnPlayerTryGetOutVehicle"; // 玩家尝试下车
        public const string OnPlayerGetOutVehicle = "OnPlayerGetOutVehicle"; // 玩家下车成功了
        public const string OnPlayerControlVehicle = "OnPlayerControlVehicle"; // 玩家控制载具, 用于PGC可以下座的载具
        public const string OnVehicleEditDriverAudioPlay = "OnVehicleEditDriverAudioPlay";
        public const string OnVehicleEditDriverAudioStop = "OnVehicleEditDriverAudioStop";
        // 娃娃机抓人玩法
        public const string OnWawajiTryGrab = "OnWawajiTryGrab"; // A的娃娃机选定目标B(string driverUid, string targetUid) → GameVehicleManager广播op=20
        public const string OnSelfBoundStateChange = "OnSelfBoundStateChange"; // 本人进入/退出被束缚状态(bool isBound, string captorUid) → UI显示挣脱按钮+QTE
        public const string OnSelfCapturedStateChange = "OnSelfCapturedStateChange"; // 本人进入/退出被劫持状态(bool isCaptured) → UI隐藏挣脱按钮
        public const string OnSelfStruggleTap = "OnSelfStruggleTap"; // 本人点击一次挣脱按钮 → BoundState 播一次 struggle 动作
        public const string OnWawajiStruggleTap = "OnWawajiStruggleTap"; // 远端同步：某被抓玩家挣扎一次(string uid) → 该玩家远端副本播 struggle
        public const string OnWawajiSelfLanded = "OnWawajiSelfLanded"; // 本人下落落地(string uid) → GameVehicleManager广播op=24，让远端退出Falling
        public const string OnWawajiClawLocked = "OnWawajiClawLocked"; // 爪子IK拉满锁定被抓玩家(string targetUid) → GameVehicleManager让该玩家进Bound（替代0.6s墙钟）
        public const string OnPlayerAudioBanChanged = "OnPlayerAudioBanChanged";//玩家闭麦状态变化（uid, isAudioBanned）

        public const string OnPlayerVehicleShow = "OnPlayerVehicleShow"; // 控制自己的横幅展示
        public const string OnPlayerVehicleShowName = "OnPlayerVehicleShowName"; // 控制自己的横幅展示内容
        #endregion

        public const string OnPlayerUseSkill = "OnPlayerUseSkill"; // 玩家使用技能 (载具技能等、以后可以扩张)

        /// <summary>
        /// 本地网络同步
        /// </summary>
        public const string LocalNetworkSync = "LocalNetworkSync";

        public const string OnJoystickChange = "OnJoystickChange";
        public const string OnJoystickChange_new = "OnJoystickChange_new";

        #region 按键策略相关，目前以载具为主，后续可以扩展到其他地方
        public const string OnJumpInputCoolDown = "OnJumpInputCoolDown";
        public const string OnSkill1CoolDown = "OnSkill1CoolDown";
        public const string OnSkill2CoolDown = "OnSkill2CoolDown";
        public const string OnHonkingCoolDown = "OnHonkingCoolDown";
        public const string OnSkill5CoolDown = "OnSkill5CoolDown"; // 娃娃机抓取(Skill5)冷却(float秒) → 冷却图标 fill 1→0
        public const string OnJumpRemainingTimes = "OnJumpRemainingTimes";
        public const string OnSkill1RemainingTimes = "OnSkill1RemainingTimes";
        public const string OnSkill2RemainingTimes = "OnSkill2RemainingTimes";
        #endregion


        public const string OnDropdownTrigger = "OnDropdownTrigger";

        public const string OnConfirmPanel = "OnConfirmPanel";

        public const string RefreshOcTimes = "RefreshOcTimes";

        public const string RefreshOcList = "RefreshOcList";
        public const string AnniversaryPanel_TabChange = "AnniversaryPanel_TabChange";
        public const string AnniversaryPanel_PackRedDot = "AnniversaryPanel_PackRedDot";
        public const string UpdateAnniversaryLimitPack = "UpdateAnniversaryLimitPack";
        public const string UpdateProductInfo = "UpdateProductInfo";

        public const string TcpTimeUpdate = "TcpTimeUpdate";
        public const string S11ParkReceiveNewChat = "S11ParkReceiveNewChat"; //乐园收到新的聊天 通知聊天内容
        public const string UpdateVivoPriState = "UpdateVivoPriState"; //更新vivo特权状态

        public const string UpadateHalloweenLimitPack = "UpadateHalloweenLimitPack";


        //赛季兑换
        public const string SeasonExchangeGiftUpdate = "SeasonExchangeGiftUpdate";

        public const string CloseGashaponPreviewPanel = "CloseGashaponPreviewPanel";

        /// <summary>
        /// 相册照片数据发生变化（删除/上传/公开状态等）
        /// </summary>
        public const string OnAlbumPhotoDataChanged = "OnAlbumPhotoDataChanged";

        /// <summary>
        /// 创作者赛季：类型选择变化（CreatorSelectType）
        /// </summary>
        public const string OnCreatorSelectTypeChanged = "OnCreatorSelectTypeChanged";

        /// <summary>
        /// 创作者中心数据刷新（初始化/领取奖励后），用于外部同步任务红点等
        /// </summary>
        public const string OnCreatorCenterDataRefreshed = "OnCreatorCenterDataRefreshed";

        public const string PlantTreeUpdate = "PlantTreeUpdate";

        public const string ChatMessageLives = "ChatMessageLives";//当前聊天显示条数，对于单个用户

        public const string RechargePanelClose = "RechargePanelClose";//热销面板关闭

        /// <summary>AI能量余额变更</summary>
        public const string OnAICreditChange = "OnAICreditChange";
        
        #region OC剧场-演员工作室
        public const string OnActorStudioDraftListChange = "OnActorStudioDraftListChange";
        public const string OnActorStudioPublishedListChange = "OnActorStudioPublishedListChange";
        #endregion

        #region OC剧场-剧本工作室
        public const string OnTheatreStudioDraftListChange = "OnTheatreStudioDraftListChange";
        public const string OnTheatreStudioPublishedListChange = "OnTheatreStudioPublishedListChange";
        public const string TheatreInfoPanelClose = "TheatreInfoPanelClose";
        public const string OnTheatreActorLineupChanged = "OnTheatreActorLineupChanged";
        public const string TheatreRoomPlayersChanged = "TheatreRoomPlayersChanged";
        public const string TheatreRoomHostLeft = "TheatreRoomHostLeft";
        public const string TheatreRoomActorAssignmentChanged = "TheatreRoomActorAssignmentChanged";
        #endregion

        #region OC剧场-角色编辑器

        public const string ActorCharacterOpenFittingRoomPanel = "ActorCharacterOpenFittingRoomPanel";// 立绘界面 打开 fitting room
        public const string ActorWardrobeViewOpenFittingRoomPanel = "ActorWardrobeViewOpenFittingRoomPanel";// 衣柜界面 打开 fitting room
        public const string ActorCardInfoUpdate = "ActorCardInfoUpdate";// 刷新演员卡信息
        public const string ActorNormalIconUpdate = "ActorNormalIconUpdate";// 刷新演员卡icon（常态贴图）

        public const string ActorExpressionItemDeleted = "ActorExpressionItemDeleted";// 立绘表情item删除
        public const string ActorWardrobeItemAdded = "ActorWardrobeItemAdded";// 衣柜item添加
        public const string ActorWardrobeItemSelected = "ActorWardrobeItemSelected";// 衣柜item选中（null表示取消选中）
        public const string ActorCardWardrobeItemClicked = "ActorCardWardrobeItemClicked";// 演员卡衣柜tab点击item，用于切换3D预览
        public const string OnSliderValueChanged = "OnSliderValueChanged";// 颜色改变

        #endregion






        public const string OnPhantomSoundPartyBigRewardToggleChanged = "OnPhantomSoundPartyBigRewardToggleChanged"; //幻音派对惊喜大奖tog切换，参数：当前选中的载具pgcId(int)

        public const string ClickEmpty = "ClickEmpty";  //点空了
        public const string RoomWsConnectStateChange = "RoomWsConnectStateChange";  //房间websocket连接状态变化
        public const string RoomConnectStateChange = "RoomConnectStateChange";  //livekit房间连接状态变化
        public const string NewAddChatMessage = "NewAddChatMessage";
        public const string LivekitRoomSoundChange = "LivekitRoomSoundChange"; //livekit房间麦克风状态变化
    }
}
