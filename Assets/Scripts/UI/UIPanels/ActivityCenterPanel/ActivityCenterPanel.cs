using Basic.Utils;
using Game.Event;
using GameData;
using GameUI;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using View.UI.PopupPanelSystem;
using View.UI.PopupPanelSystem.Base.SubSystem;

//注意这个枚举值要跟后端拿，枚举名字和值要对应上
public enum ActivityId
{
    ErrActivityId = 0,
    BalloonParty = 1,// 已结束
    SevenDayReturn = 2,// 已结束
    SunnyDoll = 3,// 已结束
    EditAvatar = 4,// 已结束
    NotesJump = 5,// 已结束 音符跳动的循环任务
    NotesJumpGrandTotal = 6,// 已结束 音符跳动的累计任务
    BundleOnline = 7, // 已结束 套装上线
    VotingFun = 8,// 已结束 投票乐翻天
    GiftForFollow = 9, // 关注有礼，这个任务随版本下架，一直放到最后面
    PetDesignContest = 10,// 已结束
    PetStudio = 11, // 已结束 宠物工作室的循环任务
    PetStudioGrandTotal = 12,// 已结束 宠物工作室的累计任务
    Halloween = 13, // 已结束 万圣节活动
    CoinAssist = 14,
    AnimationStudio = 15,
    AnimationStudioGrandTotal = 16, // 动画工作室的累计任务
    ConsumeCarnival = 17,// 消费狂欢节
    WinterCarnival = 18, // 冬日趣玩派对
    WinterCarnivalGrandTotal = 19,// 冬日趣玩派对总奖励解锁任务
    AnimeShoppingFestival = 20, // 动漫购物节
    AnimeShoppingFestivalGrandTotal = 21, //动漫购物🛍️节累计任务
    AnimeGiftCarnival = 22, // 动漫商品返利
    ChristmasCoinConsumptionRebate = 23, // 圣诞币消费返利
    MusicAndDanceCommunity = 24, // 乐舞社区折扣卡
    ElfFrost = 25, // 雪精灵
    WarmHandInHand = 26, //暖心牵手🧑‍🤝‍🧑每日任务
    WarmHandInHandGrandTotal = 27,// 暖心牵手累计类任务
    DinosaurCheckIn = 28, // 恐龙签到站
    DinosaurCheckInDaily = 29, // 恐龙每日签到
    // S6SeasonRecharge = 30, // S6 赛季累充活动
    // GroupConsume = 31, // S6 组队消费活动
    FirstChargeOneYuan = 32,//首充1元活动
    FirstChargeSixYuan = 33,//首充6元活动
    AIBuddyIntimacy = 34, // ai 伙伴的亲密度
    NewYearsTurntable = 35, // 新年福袋
    NewYearLoginAct = 36, // 新年登录礼， 特殊 activityId，实际客户端调用新年小通行证
    ApartmentEscape = 37, //公寓逃脱模拟器即将上线活动
    NewYearLoginGift = 39, // 新春登陆礼
    ApartmentEscapeChallenge = 40, //逃脱公寓挑战活动
    PurpleDreamCoinConsumptionRebate = 42,//紫梦币消费返利
    FireworkWelcomeSpring = 43,// 爆竹迎春活动
    // S8SeasonRecharge = 45, // S7 赛季累充活动
    LoveLoginAct = 46,
    SevenDaySignInGift = 47, // S8新增常驻活动-七天签到礼
    NightCap = 48, // 睡帽梦工厂
    QingmingOuting = 50, // 清明踏青行
    CuteRabbitWars = 52, // S8 萌兔大作战
    S8GroupConsume = 55, // S8 组队消费活动
    S9SeasonRecharge = 57, // S9 赛季累充消费活动
    AbandonedHospital = 58, //s9兑换商店
    AbandonedHospitalEscape = 59,
    LaborDayAct = 61, // 五一登录礼
    S9GroupConsume = 62,
    MayDayCarnival = 63, // 五一通关狂欢活动
    ZongziSummerCarnival = 64, // 粽夏狂欢
    SunnyDollEncore = 66, // 晴天娃娃特惠礼包
    S10SeasonRecharge = 67, // S10 赛季累充消费活动
    AIScriptKilling = 68, //Ai剧本杀
    AnniversaryEvent = 70,//周年庆累充
    S11CelebrationStore = 71,//周年庆商店
    S11GroupConsume = 73, // S11 组队消费活动
    FriesFun = 74,
    AnniversaryLuckyKoi = 75, //周年庆幸运锦鲤
    MidAutumnGroupConsume = 76, //中秋组队消费
    S12SeasonRecharge = 77, // S12 赛季累充活动
    ChristmasGroupConsume = 78, // S12组队消费
    NewYear2026Act = 79, //2026年登录礼
    NewYearsTurntable2026 = 80,// 2026年新年福袋
    UGCVehicleConsume = 81,//载具消费活动
    S13SeasonRecharge = 82, // S13 赛季累充活动
    NewYearLoginGiftForHorse = 83,//2026新年七天登录
    SpringFestivalGroupConsume = 84, // 新年组队消费
    LanternFestivalFunGames = 85, // 元宵接接乐
    TreePlantingDayDailyRecharge = 86, // 植树节每日充值活动
    TreePlantingDayWateringActivity = 87,// 植树节浇水次数活动任务
    CameraActivity = 88, // S14相机活动
    AlbumActivity = 89, // S14相机活动
    WaWaKindergarten = 90, // 袜袜幼稚园每日任务
    S14SeasonRecharge = 91, // S14 赛季累充活动
    LaborDayGroupConsume = 92,//五一组队消费活动
    S15SeasonRecharge = 93, // S15 赛季累充活动
    TreasureHunting = 94, // 六一寻宝活动
    S15TheaterDaily  = 97, // S15剧场活动
    S15TheaterDailyGrandTotal = 98,//S15剧场活动成就任务
    AnniversaryCelebrationCalendar = 10000, //周年庆日历
    AnniversaryCelebrationSummer = 10001, //周年庆盛夏之约
    //AnniversaryCelebrationShopping = 3, //周年庆商店   //S11CelebrationStore = 71,//周年庆商店
    AnniversaryCelebrationGift = 10002, //周年庆礼包
    AnniversaryCelebrationMonth = 10003, //周年庆月卡
    //AnniversaryCelebrationGruopPay = 6, //周年庆组队累充  //S11GroupConsume = 73, // S11 组队消费活动
    //AnniversaryCelebrationRecharge = 7, //周年庆累充   //AnniversaryEvent = 70,//周年庆累充
}
public class ActivityCenterPanel : BasePanel<ActivityCenterPanel>
{
    private Transform activityContent;
    private List<string> activityList;
    private CButton closeBtn;
    private CButton _btn_Service;
    private Dictionary<string, ActivityHandle> _avtivityHandleDic = new Dictionary<string, ActivityHandle>();

    [SerializeField] private GameObject activityTabObj;
    private Transform tabContent;

    //配置表路径
    private const string ConfigPath = "Assets/Loadable/UI/ActivityCenterPanel";

    private string curActivityId = "";
    public override void OnCreate()
    {
        base.OnCreate();
        activityContent = GameObjectEx.FindChildByName(transform, "ActivityContent");
        tabContent = GameObjectEx.FindChildByName(transform, "ActivityTabContent");
        closeBtn = GameObjectEx.FindComponentByName<CButton>(transform, "BackBtn");
        //_btn_Service = GameObjectEx.FindComponentByName<CButton>(transform, "Btn_Service");
        //_btn_Service.gameObject.SetActive(false);
        InitActivityInfo();
        InitTap();
        if (_avtivityHandleDic.Any())
        {
            OnTabClick(_avtivityHandleDic.First().Key);
        }
        else
        {
            Debug.LogError("Mingo - ActivityCenterPanel - No activities available in _avtivityHandleDic.");
        }
        closeBtn.onClick.AddListener(CloseSelf);
        AddRefreshCurrentDataListener();

        BusinessLiveManager.Inst.AddConfigUpdateListener(OnBusinessConfigUpdate);

        foreach (var w in GetComponentsInChildren<AccountWidget>(true))
            w.SetOverrideWindowId(WindowId.ActivityCenterWindow);
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        if (args is { Length: > 0 })
        {
            curActivityId = args[0] as string;
            OnTabClick(curActivityId);
        }
    }
    private void InitActivityInfo()
    {
        //该列表直接决定左侧tab活动的顺序
        activityList = new List<string>()
        {
            ActivityId.TreasureHunting.ToString(),
            ActivityId.S15TheaterDaily .ToString(),
            ActivityId.LaborDayGroupConsume.ToString(),
            ActivityId.WaWaKindergarten.ToString(),
            ActivityId.CameraActivity.ToString(),
            ActivityId.AlbumActivity .ToString(),
            ActivityId.TreePlantingDayWateringActivity.ToString(),
            ActivityId.LanternFestivalFunGames.ToString(),
            ActivityId.NewYearLoginGiftForHorse.ToString(),
            ActivityId.SpringFestivalGroupConsume.ToString(),
            ActivityId.UGCVehicleConsume.ToString(),
            ActivityId.NewYear2026Act.ToString(),
            ActivityId.NewYearsTurntable2026.ToString(),
            ActivityId.FriesFun.ToString(),
            ActivityId.AIScriptKilling.ToString(),
            ActivityId.SevenDaySignInGift.ToString(),
        };
        LoggerUtils.Log($"初始活动列表: {string.Join(", ", activityList)}");

        var liveActivityList = FilterActivityLive(activityList);
        LoggerUtils.Log($"过滤后准备构建字典的活动列表: {string.Join(", ", liveActivityList)}");

        // 审核问题，改为官服和iOS 才有
        bool isShowGift = IAPDataManager.Inst.IsOfficialChannel();
#if UNITY_IPHONE
        isShowGift = true;
#endif

        if (isShowGift)
        {
            liveActivityList.Add(ActivityId.GiftForFollow.ToString());
        }

        for (int i = 0; i < liveActivityList.Count; i++)
        {
            var activityStr = liveActivityList[i];
            if (liveActivityList[i] == ActivityId.NewYear2026Act.ToString())//特殊处理，2026年登录礼服务器另用了活动ID
            {
                activityStr = ActivityId.NewYearLoginAct.ToString();
            }
            else if (liveActivityList[i] == ActivityId.NewYearsTurntable2026.ToString())//特殊处理，2026新年福袋礼服务器另用了活动ID
            {
                activityStr = ActivityId.NewYearsTurntable.ToString();
            }
            else if (liveActivityList[i] == ActivityId.LanternFestivalFunGames.ToString())//特殊处理，2026新年福袋礼服务器另用了活动ID
            {
                activityStr = "Jiejiele";
            }
            else if (liveActivityList[i] == ActivityId.TreePlantingDayWateringActivity.ToString())//特殊处理，2026新年福袋礼服务器另用了活动ID
            {
                activityStr = "PlantTree";
            }
            else if (liveActivityList[i] == ActivityId.WaWaKindergarten.ToString())//特殊处理，2026新年福袋礼服务器另用了活动ID
            {
                activityStr = "SockTask";
            }
            else if (liveActivityList[i] == ActivityId.TreasureHunting.ToString())//特殊处理，2026新年福袋礼服务器另用了活动ID
            {
                activityStr = "TreasureHuntPanel";
            }
            else if (liveActivityList[i] == ActivityId.S15TheaterDaily.ToString())//特殊处理，2026新年福袋礼服务器另用了活动ID
            {
                activityStr = "OCActivity";
            }
            string activityConfigPath = ConfigPath + "/" + activityStr + "/" + activityStr + ".json";
            var textAsset = Loader.Load<TextAsset>(activityConfigPath, gameObject);
            if (textAsset == null)
            {
                continue;
            }
            ActivityInfo info = JsonConvert.DeserializeObject<ActivityInfo>(textAsset.text);

            ActivityHandle handle = new ActivityHandle();
            handle._viewParent = activityContent;
            handle.mainPanel = this;
            handle._info = info;     
            _avtivityHandleDic.Add(liveActivityList[i], handle);
        }

        var data = ActivitySkipSystem.Inst.ActivitySkipMsg;
        if (data != null && data.list != null)
        {
            foreach (var item in data.list)
            {
                ActivityHandle handle = new ActivityHandle();
                handle._viewParent = activityContent;
                handle.mainPanel = this;
                handle._info = new ActivityInfo() { activityId = item.id.ToString() };
                handle._info.activitySkipMsg = item;
                handle._info.activityTital = item.name;
                handle._info.viewPrefabPath = ConfigPath + "/ActivitySkipView/ActivitySkipView.prefab";
                _avtivityHandleDic.Add(item.name, handle);
            }
        }
    }

    private void InitTap()
    {
        foreach (var handle in _avtivityHandleDic.Values)
        {
            var obj = Instantiate(activityTabObj, tabContent);
            obj.transform.localScale = Vector3.one;
            obj.SetActive(true);
            ActivityTabItem tab = obj.GetComponent<ActivityTabItem>();
            tab.Init(handle._info, OnTabClick);
            handle._tab = tab;
        }
    }

    /// <summary>
    /// 过滤不在线的活动
    /// </summary>
    /// <param name="activitys"></param>
    /// <returns></returns>
    private List<string> FilterActivityLive(List<string> activitys)
    {
        LoggerUtils.Log($"过滤前的活动列表: {string.Join(", ", activitys)}");
        List<string> result = new List<string>();
        foreach (var activityId in activitys)
        {
            if (GameUtils.TryParseEnum<ActivityId>(activityId, out var parsedEnum))
            {
                int activityIntId = (int)parsedEnum;
                if (BusinessLiveManager.Inst.IsActivityLive(activityIntId.ToString()) )
                {
                    result.Add(activityId);
                }
                else
                {
                    LoggerUtils.Log($"活动{activityId}未上线，从列表中移除");
                }
            }
        }
        LoggerUtils.Log($"过滤后的活动列表: {string.Join(", ", result)}");
        return result;
    }

    private void CheckBusinessLive()
    {
        List<string> curActivityList = _avtivityHandleDic.Keys.ToList();
        if (curActivityList != null && curActivityList.Count > 0)
        {
            foreach (var activityId in curActivityList)
            {
                if (GameUtils.TryParseEnum<ActivityId>(activityId, out var parsedEnum))
                {
                    int activityIntId = (int)parsedEnum;
                    if (parsedEnum != ActivityId.GiftForFollow && !BusinessLiveManager.Inst.IsActivityLive(activityIntId.ToString()))
                    {
                        if (_avtivityHandleDic.ContainsKey(activityId))
                        {
                            LoggerUtils.Log("##过滤活动：" + activityId);
                            DelateHandle(_avtivityHandleDic[activityId]);
                        }
                    }
                }
            }
        }
    }

    private void OnBusinessConfigUpdate(BusinessLiveConfig config)
    {
        CheckBusinessLive();
    }

    public void OnTabClick(string actId)
    {
        bool foundActivity = false;

        // 先检查是否存在目标活动
        foreach (var handle in _avtivityHandleDic.Values)
        {
            if (handle._info.activityId == actId)
            {
                handle._tab.SetSelect(true);
                handle.ShowView();
                foundActivity = true;
            }
            else
            {
                handle._tab.SetSelect(false);
                handle.HideView();
            }
        }

        // 如果没找到目标活动，选择第一个活动
        if (!foundActivity && _avtivityHandleDic.Count > 0)
        {
            var firstHandle = _avtivityHandleDic.Values.First();
            firstHandle._tab.SetSelect(true);
            firstHandle.ShowView();
        }
    }

    private bool isRequest = false;
    private void GetActivityCenterInfo()
    {
        if (isRequest)
        {
            return;
        }

        isRequest = true;

        ActivityCenterInfoReq req = new ActivityCenterInfoReq();
        req.idList = _avtivityHandleDic.Keys.ToList(); // 使用已经过滤的活动列表
        LoggerUtils.Log($"发送活动请求，idList: {string.Join(", ", req.idList)}");
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ActivityList, HttpMethod.POST, JsonConvert.SerializeObject(req), (content) =>
            {
                ActivityResponse activityResponse = JsonConvert.DeserializeObject<ActivityResponse>(content);
                if (activityResponse.list != null)
                {
                    OnGetActivityListSuccess(activityResponse.list);
                }

                //客服数据
                //if (!string.IsNullOrEmpty(activityResponse.customerServiceUrl))
                //{
                //    _btn_Service.gameObject.SetActive(true);
                //    _btn_Service.onClick.RemoveAllListeners();
                //    _btn_Service.onClick.AddListener(() => { OnBtnServiceClick(activityResponse.customerServiceUrl); });
                //}

                if (this != null)
                {
                    isRequest = false;
                }
            },
            (error) =>
            {
                if (this != null)
                {
                    isRequest = false;
                }
            });
    }

    public override void OnWindowBeFocused()
    {
        base.OnWindowBeFocused();
        GetActivityCenterInfo();
    }

    //处理后端拉下详情后的数据刷新
    private void OnGetActivityListSuccess(List<ActivityInfo> activityList)
    {
        for (int i = 0; i < activityList.Count; i++)
        {
            if (!_avtivityHandleDic.ContainsKey(activityList[i].activityId))
            {
                continue;
            }
            var handle = _avtivityHandleDic[activityList[i].activityId];
            if (handle != null)
            {
                if (activityList[i].activityStatus == 0)
                {
                    DelateHandle(handle);
                    continue;
                }

                handle._info.leftTime = activityList[i].leftTime;
                // handle._tab.RefreshLeftTime(handle?._info?.leftTime);
                handle._info.currencyAmount = activityList[i].currencyAmount;
                handle._info.consumeCarnivalInfo = activityList[i]?.consumeCarnivalInfo;
                handle._info.discountCardInfo = activityList[i]?.discountCardInfo;
                handle._info.christmasCoinRebatesInfo = activityList[i]?.christmasCoinRebatesInfo;
                handle._info.groupConsumeInfo = activityList[i]?.groupConsumeInfo;
                handle._info.newYearLoginInfo = activityList[i]?.newYearLoginInfo;
                handle._info.apartmentEscapeInfo = activityList[i]?.apartmentEscapeInfo;
                handle._info.productinfo = activityList[i]?.productinfo;
                handle._info.treasureHauntingInfo = activityList[i]?.treasureHauntingInfo;
                if (activityList[i].activityId == ActivityId.TreasureHunting.ToString()
                    && handle._info.treasureHauntingInfo != null)
                {
                    handle._info.treasureHauntingInfo.levelEventList = activityList[i].eventList;
                }
                if (activityList[i].eventList != null && handle._info.eventList != null)
                {
                    for (int j = 0; j < activityList[i].eventList.Count; j++)
                    {
                        var eventInfo = handle._info.eventList.Find(x => x.eventId == activityList[i].eventList[j].eventId);
                        if (eventInfo != null)
                        {
                            eventInfo.eventStatus = activityList[i].eventList[j].eventStatus;
                            eventInfo.finishAmount = activityList[i].eventList[j].finishAmount;
                        }
                    }
                }
                if (activityList[i].rewardList != null && handle._info.rewardList != null)
                {
                    for (int j = 0; j < activityList[i].rewardList.Count; j++)
                    {
                        var rewardInfo = handle._info.rewardList.Find(x => x.rewardId == activityList[i].rewardList[j].rewardId);
                        if (rewardInfo != null)
                        {
                            rewardInfo.rewardStatus = activityList[i].rewardList[j].rewardStatus;
                            rewardInfo.rewardIcon = activityList[i].rewardList[j].rewardIcon;
                        }
                    }
                }
                if (handle.IsViewShow())
                {
                    handle.RefreshViewData();
                }
                handle.UpDateRedDot();
            }
        }
    }

    private void AddRefreshCurrentDataListener()
    {
        MessageHelper.RemoveListener(MessageName.OnRefreshTaskDataAfterBack, Refresh);
        MessageHelper.AddListener(MessageName.OnRefreshTaskDataAfterBack, Refresh);
    }

    private void Refresh()
    {
        GetActivityCenterInfo();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        MessageHelper.RemoveListener(MessageName.OnRefreshTaskDataAfterBack, Refresh);
        BusinessLiveManager.Inst.RemoveConfigUpdateListener(OnBusinessConfigUpdate);
    }

    private void DelateHandle(ActivityHandle handle)
    {
        bool isShow = handle.IsViewShow();
        handle.Release();
        _avtivityHandleDic.Remove(handle._info.activityId);
        if (isShow && _avtivityHandleDic.Count > 0)
        {
            OnTabClick(_avtivityHandleDic.First().Value._tab.actId);
        }
    }

    private void OnBtnServiceClick(string path)
    {
        LoggerUtils.LogFormat($"$[WebView] open webView: {path}");
        var jb = new JObject
        {
            ["url"] = path
        };
        MobileInterface.Instance.SendMessage(MobileInterfaceDefine.openWebview, JsonConvert.SerializeObject(jb));
    }

    public Transform GetLayout3D()
    {
        return GetBaseLayout3D();
    }
}




