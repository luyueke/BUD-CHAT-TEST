using Basic.Utils;
using Es;
using Game.Avatar;
using GameData;
using GameData.PgcData;
using GameUI;
using GroupConsume;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using UI.Manager;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.UI;

namespace GroupConsume
{
    public class GroupInfo
    {
        public string groupId;
        public GroupStatus status;
    }


    public class MemberInfo
    {
        public AccountUserInfo userInfo;
        public GroupRole groupRole;
        public int amount;
    }

    public enum GroupRole
    {
        Error,
        Captain, // 队长
        Member // 成员
    }

    public enum GroupStatus
    {
        Waiting = 1, //组队期
        Finished = 2, //组队完成
    }
}



public class GroupConsumeInfo
{
    public GroupConsume.GroupInfo groupInfo;
    public List<GroupConsume.MemberInfo> memberList;
    public int amount;
    public int redDot;
    public string captainId;
}

public static class ActivityTool
{
    public static void FillActivityData(this ActivityInfo _info, ActivityInfo activity)
    {
        _info.leftTime = activity.leftTime;
        _info.currencyAmount = activity.currencyAmount;
        _info.consumeCarnivalInfo = activity.consumeCarnivalInfo;
        _info.discountCardInfo = activity.discountCardInfo;
        _info.christmasCoinRebatesInfo = activity.christmasCoinRebatesInfo;
        _info.groupConsumeInfo = activity.groupConsumeInfo;
        _info.newYearLoginInfo = activity.newYearLoginInfo;
        _info.apartmentEscapeInfo = activity.apartmentEscapeInfo;
        if (activity.eventList != null && _info.eventList != null)
        {
            for (int j = 0; j < activity.eventList.Count; j++)
            {
                var eventInfo = _info.eventList.Find(x => x.eventId == activity.eventList[j].eventId);
                if (eventInfo != null)
                {
                    eventInfo.eventStatus = activity.eventList[j].eventStatus;
                    eventInfo.finishAmount = activity.eventList[j].finishAmount;
                }
            }
        }
        if (activity.rewardList != null && _info.rewardList != null)
        {
            for (int j = 0; j < activity.rewardList.Count; j++)
            {
                var rewardInfo = _info.rewardList.Find(x => x.rewardId == activity.rewardList[j].rewardId);
                if (rewardInfo != null)
                {
                    rewardInfo.rewardStatus = activity.rewardList[j].rewardStatus;
                    rewardInfo.rewardIcon = activity.rewardList[j].rewardIcon;
                }
            }
        }
    }
}
public class GroupConsumeView : ActivityBaseView
{
    public HeadViewWidget headViewWidget;
    public Image Fill;
    [SerializeField] private List<GroupConsumeRewardItemView> rewardItemView;

    [SerializeField] private GroupConsumeMemberView memberView;
    [SerializeField] private Text consumedText;
    [SerializeField] private Button fittingBtn;
    [SerializeField] private Button storeBtn;
    [SerializeField] private Button groupBtn;
    [SerializeField] private Button applyBtn;
    [SerializeField] private Button giftBtn;
    [SerializeField] private Button inviteBtn;
    [SerializeField] private GameObject waitTip;
    [SerializeField] private Button exitBtn;
    [SerializeField] private Button ruleBtn;
    [SerializeField] private RawImage bgRawTex;
    [SerializeField] private AvatarCameraController avatarCameraController;
    [SerializeField] private Transform characterRoot;

    [SerializeField] private GameObject waittingObj;
    [SerializeField] private GameObject finishObj;
    private Dictionary<int, GroupConsumeRewardItemView> itemViews;
    private List<GroupConsumeMemberView> memberViews = new List<GroupConsumeMemberView>();

    private ActivityInfo activityInfo;
    private bool isSending = false;
    private string iconAtlasPath = "Assets/Loadable/UI/UIPanel/Activity/GroupConsume/GroupConsume.spriteatlas";
    private List<string> pgcIds = new List<string>() { "40300213" };

    internal CharacterWrap characterWrapper;
    internal PlayerAnimationCtrl animationCtrl;
    private void Awake()
    {
        Fill.gameObject.SetActive(false);
        ReqInfo();
    }

    void ReqInfo()
    {
        ActivityCenterInfoReq req = new ActivityCenterInfoReq();
        req.idList = new List<string>
        {
            ActivityId.LaborDayGroupConsume.ToString()
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ActivityList, HttpMethod.POST,
            JsonConvert.SerializeObject(req), (content) =>
            {
                ActivityResponse activityResponse = JsonConvert.DeserializeObject<ActivityResponse>(content);
                if (activityResponse.list == null)
                {
                    activityResponse.list = new List<ActivityInfo>();
                }

                string activityConfigPath = "Assets/Loadable/UI/ActivityCenterPanel/LaborDayGroupConsume/LaborDayGroupConsume.json";
                var textAsset = Loader.Load<TextAsset>(activityConfigPath, gameObject);
                ActivityInfo info = JsonConvert.DeserializeObject<ActivityInfo>(textAsset.text);
                var activityInfo = activityResponse.list.Find(x => x.activityId == ActivityId.LaborDayGroupConsume.ToString());
                if (activityInfo != null)
                {
                    info.FillActivityData(activityInfo);
                }
                Init(info);
            },
            (error) =>
            {
                Debug.LogError("GroupConsumeView   " + error);
            });
    }

    public override void Init(ActivityInfo info)
    {
        base.Init(info);
        activityInfo = info;
        InitUI();
        InitCharacter();
        GroupConsumeSystem.Inst.ActivityInfo = activityInfo;
        MessageHelper.Broadcast(MessageName.AnniversaryPanel_PackRedDot, ActivityId.LaborDayGroupConsume);
        MessageHelper.RemoveListener(MessageName.RefreshGroupConsumeApplyData, OnRefreshGroupConsumeAppleData);
        MessageHelper.AddListener(MessageName.RefreshGroupConsumeApplyData, OnRefreshGroupConsumeAppleData);
        MessageHelper.RemoveListener(MessageName.OnRefreshTaskDataAfterBack, OnRefreshGroupConsumeAppleData);
        MessageHelper.AddListener(MessageName.OnRefreshTaskDataAfterBack, OnRefreshGroupConsumeAppleData);
    }

    private void OnDestroy()
    {
        MessageHelper.RemoveListener(MessageName.RefreshGroupConsumeApplyData, OnRefreshGroupConsumeAppleData);
        MessageHelper.RemoveListener(MessageName.OnRefreshTaskDataAfterBack, OnRefreshGroupConsumeAppleData);
    }

    private void OnRefreshGroupConsumeAppleData()
    {
        if (this == null)
        {
            return;
        }

        ReqInfo();
    }

    private void InitUI()
    {
        itemViews = new Dictionary<int, GroupConsumeRewardItemView>();
        for (int i = 0; i < activityInfo.rewardList.Count; i++)
        {
            if (i < rewardItemView.Count)
            {
                var rewardInfo = activityInfo.rewardList[i];
                var itemView = rewardItemView[i];
                JObject extra = JObject.FromObject(rewardInfo.extra);
                int eventId = extra["eventId"]?.Value<int>() ?? 1;
                CommonRewardItemData commonRewardItemData = new CommonRewardItemData();
                commonRewardItemData.RewardAmount = rewardInfo.rewardNum;
                commonRewardItemData.rewardName = rewardInfo.rewardName;
                commonRewardItemData.rewardType = rewardInfo.budRewardType;
                commonRewardItemData.pgcId = rewardInfo.pgcId;
                //if (rewardInfo.extra != null)
                //{
                //    JObject extraObj = JObject.FromObject(rewardInfo.extra);
                //    if (extraObj.ContainsKey("bundleId") && extraObj.TryGetValue("bundleId", out var bundleId))
                //    {
                //        commonRewardItemData.bundleId = bundleId.Value<string>();
                //    }
                //    if (rewardInfo.budRewardType == (int)BUDRewardType.RewardChatBubbles && !string.IsNullOrEmpty(rewardInfo.pgcId))
                //    {
                //        UserUIWidgetManager.Inst.GetChatBubbleIconByPgcIdAsync(rewardInfo.pgcId, gameObject, sp =>
                //        {
                //            commonRewardItemData.IconSp = sp;
                //        });
                //    }
                //    else if (rewardInfo.budRewardType == (int)BUDRewardType.RewardAvatarFrame)
                //    {
                //        UserUIWidgetManager.Inst.GetHeadCycleImgByPgcIdAsync(rewardInfo.pgcId, gameObject, sp =>
                //        {
                //            commonRewardItemData.IconSp = sp;
                //        });
                //    }
                //    else if (rewardInfo.budRewardType == (int)BUDRewardType.RewardChatBubbles && !string.IsNullOrEmpty(rewardInfo.pgcId))
                //    {
                //        UserUIWidgetManager.Inst.GetChatBubbleIconByPgcIdAsync(rewardInfo.pgcId, gameObject, sp =>
                //        {
                //            commonRewardItemData.IconSp = sp;
                //        });
                //    }
                //    else if (rewardInfo.budRewardType == (int)BUDRewardType.RewardHomepageSkin && !string.IsNullOrEmpty(rewardInfo.pgcId))
                //    {   //主页皮肤
                //        var sp = ProfileThemeManager.Inst.LoadThemeIcon(int.Parse(rewardInfo.pgcId), gameObject);
                //        if (sp != null)
                //        {
                //            commonRewardItemData.IconSp = sp;
                //        }
                //    }
                //    else if (extraObj.ContainsKey("iconName") && extraObj.TryGetValue("iconName", out var iconName))
                //    {
                //        commonRewardItemData.IconSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(iconAtlasPath, iconName.Value<string>(), gameObject);
                //    }
                //}
                itemView.Init(eventId, commonRewardItemData, OnClaimCallBack);
                itemView.SetProcess(rewardInfo.spendNum);
                itemViews.Add(eventId, itemView);

            }
        }

        fittingBtn.onClick.AddListener(OnOpenFittingClicked);
        storeBtn.onClick.AddListener(OnOpenStoreClicked);
        groupBtn.onClick.AddListener(OnInviteClicked);
        inviteBtn.onClick.AddListener(OnInviteClicked);
        applyBtn.onClick.AddListener(OnApplyClicked);
        giftBtn.onClick.AddListener(OnGiftClicked);
        exitBtn.onClick.AddListener(OnExitClicked);
        ruleBtn.onClick.AddListener(OnRuleClicked);
        if (memberViews.Count <= 0)
        {
            for (int i = 0; i < 4; i++)
            {
                var itemView = Instantiate(memberView, memberView.transform.parent);
                MemberInfo memberInfo = null;
                if (i == 0)
                {
                    memberInfo = new MemberInfo()
                    {
                        userInfo = AccountDataManager.Inst.UserInfo,
                        groupRole = GroupRole.Captain,
                    };
                }

                GroupStatus groupStatus = GroupStatus.Waiting;
                if (activityInfo.groupConsumeInfo != null && activityInfo.groupConsumeInfo.groupInfo != null)
                {
                    groupStatus = activityInfo.groupConsumeInfo.groupInfo.status;
                }
                waittingObj.SetActive(groupStatus == GroupStatus.Waiting);
                finishObj.SetActive(groupStatus == GroupStatus.Finished);
                itemView.SetMemberInfo(i + 1, groupStatus, memberInfo, OnInviteClicked);
                memberViews.Add(itemView);

                itemView.gameObject.SetActive(true);
            }
            memberView.gameObject.SetActive(false);
        }

        RefrashData(activityInfo);
    }


    private void InitCharacter()
    {
        //headViewWidget.InitHeadCycle(48);
        return;
        if (characterWrapper != null)
        {
            return;
        }
        var saveCharacterData = AccountDataManager.Inst.UserInfo.avatarInfo.Clone();
        characterWrapper = AvatarController.Inst.CreateUIAvatarWithIKController(saveCharacterData, characterRoot);
        //var pgcIds = new List<string>() {
        //    "11300013","10900067",
        //            "10800006","10400029",
        //            "10100005",
        //};
        //foreach (var pgcId in pgcIds) {
        //    var pgcConfig = PgcUtils.GetPgcConfigData(pgcId);
        //    var config = DataTables.GetAvatarCommonData(pgcId);
        //    var classType = UniqueType.GetAvatar(pgcId);
        //    characterWrapper.ChangePart(UniqueType.GetAvatar((AvatarSubType)pgcConfig.SubType), pgcId);
        //    characterWrapper.ChangeColor(classType, config.defaultColor);
        //    characterWrapper.Move(classType, config.pDef);
        //    characterWrapper.Rotate(classType, config.rDef);
        //    characterWrapper.Scale(classType, config.sDef);
        //    characterWrapper.HVScale(classType, config.vhSDef);
        //    characterWrapper.SetLeftOrRight(classType, config.leftRightType);
        //}
        characterWrapper.SetParent(characterRoot, true);

        animationCtrl = characterWrapper.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
        avatarCameraController.RotateTarget = characterRoot;
        avatarCameraController.isMoveEnabled = false;
        avatarCameraController.isZoomEnabled = false;

        var cfg = Es.DataTables.GetGameResData("40100259");
        if (cfg != null && cfg.ResourceType == (int)ResourceType.Emote)
        {
            var pgcId = cfg.PgcId;
            animationCtrl.ResetEmoteForUICharacter();
            avatarCameraController.SetEmoteView(pgcId);
            switch ((EmoteSubType)cfg.SubType)
            {
                case EmoteSubType.Single:
                case EmoteSubType.SingleLoop:
                    animationCtrl.PlaySingleEmoteForUICharacter(pgcId, null);
                    break;
                case EmoteSubType.Double:
                case EmoteSubType.DoubleLoop:
                    //animationCtrl.PlayDoubleEmoteForUICharacter(pgcId, otherAnimationCtrl, null);
                    break;
                case EmoteSubType.LinkEmote:
                    //animationCtrl.PlayLinkEmoteForUICharacter(pgcId, SpecialAnim.Idle, otherAnimationCtrl);
                    break;
            }
            return;
        }
    }

    private void OnRuleClicked()
    {
        UIManager.Inst.OpenPanel<GashaponRulePanel>(PanelId.GashaponRulePanel,
            "Assets/Loadable/UI/UIPanel/Activity/GroupConsume/Rule.json");
    }

    private void OnExitClicked()
    {
        CommonConfirmPanel commonConfirmPanel =
            UIManager.Inst.OpenPanel<CommonConfirmPanel>(PanelId.CommonConfirmPanel);
        commonConfirmPanel.SetIsCloseSelf(true);
        commonConfirmPanel.SetLocalText("退出队伍", "您确定要退出当前队伍吗？", "确定", "取消");
        commonConfirmPanel.SetOnClickAction(RequestExitGroup);
    }
    private void OnGiftClicked()
    {
        //var panel = UIManager.Inst.OpenPanel<RechargePanel>(PanelId.RechargePanel);
        //panel.OnTabClick(UI.UIPanels.RechargePanel.RechargeId.S9LimitedTimeCurrencyPack);
        AnniversarySkipManager.Inst.Skip(4);
    }
    private void OnApplyClicked()
    {
        UIManager.Inst.OpenPanel<GroupConsumeApplyPanel>(PanelId.GroupConsumeApplyPanel, new GroupConsumeApplyArgs()
        {
            activityId = activityInfo.activityId,
            groupConsumeInfo = activityInfo.groupConsumeInfo,
            applyType = GroupConsumeApplyType.Invited,
            backCallBack = OnApplyCallBack
        });
    }
    private void ShowHomepageSkin()
    {
        UIManager.Inst.OpenPanel<ProfileThemePreviewPanel>(PanelId.ProfileThemePreviewPanel, ProfileTheme.ThinChocolateSweetheart);
    }

    private void OnInviteClicked()
    {
        UIManager.Inst.OpenPanel<GroupConsumeApplyPanel>(PanelId.GroupConsumeApplyPanel, new GroupConsumeApplyArgs()
        {
            activityId = activityInfo.activityId,
            groupConsumeInfo = activityInfo.groupConsumeInfo,
            applyType = GroupConsumeApplyType.Friends,
            backCallBack = OnApplyCallBack
        });
    }
    void OnGroupClicked()
    {
        UIManager.Inst.OpenPanel<FittingRoomPanel>(PanelId.GroupConsumeApplyPanel);
    }
    void OnOpenStoreClicked()
    {
        UIManager.Inst.OpenPanel<FittingRoomPanel>(PanelId.StoreMallPanel);
    }
    private void OnOpenFittingClicked()
    {
        UIManager.Inst.OpenPanel<FittingRoomPanel>(PanelId.FittingRoomPanel);
    }


    private void OnPreviewClick(CommonRewardItem rewardItem)
    {
        if (activityInfo == null)
        {
            return;
        }

        if (rewardItem == null)
        {
            return;
        }
        else
        {
            int rewardType = rewardItem.rewardData.rewardType;
            if (rewardType == (int)BUDRewardType.RewardPgcResource)
            {
                var pgcids = new List<string>();
                pgcids.Add(rewardItem.rewardData.pgcId);
                ShowPgcPreview(pgcids, rewardItem.rewardData.rewardName);
                return;
            }
            if (rewardType == (int)BUDRewardType.RewardHomepageSkin)
            {
                ShowHomepageSkin();
                return;
            }
            if (rewardType == (int)BUDRewardType.RewardAvatarFrame)
            {
                ShowAvatarFrame(rewardItem.rewardData);
                return;
            }
            if (rewardType == (int)BUDRewardType.RewardChatBubbles)
            {
                ShowChatBubble();
                return;
            }
            if (rewardType == (int)BUDRewardType.RewardYouYouCoinNewYearPack)
            {
                ShowYouYouCoinNewYearPack();
                return;
            }

            if (rewardType == (int)BUDRewardType.RewardVipFreeTrail)
            {
                ShowVipPreview();
                return;
            }

            if (rewardType == (int)BUDRewardType.RewardPgcBundle)
            {
                ShowBundlePreview();
                return;
            }


            CurrencyType currencyType = GameUtils.ConvertRewardType(rewardType);
            UIManager.Inst.OpenPanel(PanelId.CurrencyTipsPanel, currencyType);
        }
    }

    private void ShowVipPreview()
    {
        var panel = UIManager.Inst.OpenPanel<CurrencyTipsPanel>(PanelId.CurrencyTipsPanel);
        panel.UpdateUI(PgcUtils.LoadRewardIcon(BUDRewardType.RewardVipFreeTrail, panel.gameObject), "VIP体验卡1天", null, "特殊权益，领取后VIP月卡有效期增加1天");
    }

    private void ShowPgcPreview(List<string> pgcIds, string name)
    {
        //if (pgcIds.Count == 1)
        //{
        //    var cfg = Es.DataTables.GetGameResData(pgcIds[0]);
        //    if (cfg != null && cfg.ResourceType == (int)ResourceType.Emote)
        //    {
        //        var pgcId = cfg.PgcId;
        //        animationCtrl.ResetEmoteForUICharacter();
        //        avatarCameraController.SetEmoteView(pgcId);
        //        switch ((EmoteSubType)cfg.SubType)
        //        {
        //            case EmoteSubType.Single:
        //            case EmoteSubType.SingleLoop:
        //                animationCtrl.PlaySingleEmoteForUICharacter(pgcId, null);
        //                break;
        //            case EmoteSubType.Double:
        //            case EmoteSubType.DoubleLoop:
        //                //animationCtrl.PlayDoubleEmoteForUICharacter(pgcId, otherAnimationCtrl, null);
        //                break;
        //            case EmoteSubType.LinkEmote:
        //                //animationCtrl.PlayLinkEmoteForUICharacter(pgcId, SpecialAnim.Idle, otherAnimationCtrl);
        //                break;
        //        }
        //        return;
        //    }
        //}

        var panel = UIManager.Inst.OpenPanel<RewardPreviewPanel>(PanelId.RewardPreviewPanel);
        panel.SetEventPreview(pgcIds, name, "", "", "", "#BF8DFF", bgRawTex.texture);
    }

    private void ShowAvatarFrame(CommonRewardItemData data)
    {
        PreviewManager.Inst.ShowPreview(new RewardPreviewInfo(BUDRewardType.RewardAvatarFrame, CurrencyType.None, data.pgcId, data.rewardName, ""));

        //string userWidgetAtlas = UserUIWidgetManager.Inst._userHeadAtlas;
        //var panel = UIManager.Inst.OpenPanel<RewardPreviewPanel>(PanelId.RewardPreviewPanel);
        //panel.SetEventPreview(new List<string>(), "圣诞派对头像框", "HeadCycle_8", userWidgetAtlas, "新年组队消费 领新年好礼", "#BF8DFF",
        //    bgRawTex.texture);
    }

    private void ShowChatBubble()
    {

        GameChatBubbleData chatData = UserUIWidgetManager.Inst.GetChatDataByID((int)ChatBubblesType.ChatBubblesDhzy);
        if (chatData != null)
        {
            var tem = new RewardPreviewInfo(BUDRewardType.RewardChatBubbles, CurrencyType.None, chatData.PgcId, chatData.Name, "");
            tem.SetTitleAndDes(chatData.Name, chatData.Desc);
            PreviewManager.Inst.ShowPreview(tem);
        }
    }

    private void ShowBundlePreview()
    {
        var panel = UIManager.Inst.OpenPanel<RewardPreviewPanel>(PanelId.RewardPreviewPanel);
        panel.SetEventPreview(new List<string>() {
                    "11300013","10900067",
                    "10800006","10400029",
                    "10100005",}, "布兰奇特套装", "Bundle_100", XAssetLoaderMgr.Inst.GetSpriteAltasPath(SpriteAtlasType.Bundle), "新年组队消费 领新年好礼", "#BF8DFF",
            bgRawTex.texture);
    }

    private void ShowYouYouCoinNewYearPack()
    {
        var panel = UIManager.Inst.OpenPanel<CurrencyTipsPanel>(PanelId.CurrencyTipsPanel);
        panel.UpdateUI(PgcUtils.LoadRewardIcon(BUDRewardType.RewardYouYouCoinNewYearPack, panel.gameObject), "优优币礼盒", null, "开启礼盒后根据概率获得优优币， 55%概率获得15个；24%概率获得18个；15%概率获得24个 ；6%概率获得36个");
    }

    private void OnClaimCallBack(CommonRewardItem rewardItem)
    {

        var eventInfo = activityInfo.eventList.Find(tmp => tmp.eventId == rewardItem.itemId);
        if (eventInfo == null)
        {
            return;
        }

        if (eventInfo.eventStatus != (int)ClaimStatus.Unlocked)
        {
            OnPreviewClick(rewardItem);
            return;
        }

        if (isSending)
        {
            return;
        }
        isSending = true;

        JObject jObject = new JObject()
        {
            ["activityId"] = activityInfo.activityId,
            ["eventId"] = eventInfo.eventId
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ClaimActivityReward,
            HttpMethod.POST,
            JsonConvert.SerializeObject(jObject),
            (content) =>
            {
                ActivityEventClaimResponse activityEventClaimResponse = JsonConvert.DeserializeObject<ActivityEventClaimResponse>(content);
                isSending = false;
                OnClaimSuccess(activityEventClaimResponse, rewardItem);
                AccountDataManager.Inst.RefreshUserInfo(); //刷新UserInfo
            },
            (error) =>
            {
                isSending = false;
            });

    }

    private void OnClaimSuccess(ActivityEventClaimResponse response, CommonRewardItem rewardItem)
    {
        if (this == null || gameObject == null)
        {
            return;
        }
        var eventInfo = activityInfo.eventList.Find(x => x.eventId == response.eventInfo.eventId);
        if (eventInfo == null)
        {
            return;
        }
        eventInfo.eventStatus = response.eventInfo.eventStatus;
        rewardItem.SetStatus((ClaimStatus)eventInfo.eventStatus);
        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
        List<CommonRewardItemData> rewardItemList = new List<CommonRewardItemData>();

        if (rewardItem.rewardData.rewardType == (int)BUDRewardType.RewardAvatarFrame)
        {
            rewardItemList.Add(new CommonRewardItemData()
            {
                rewardType = (int)BUDRewardType.RewardAvatarFrame,
                rewardName = "薄巧甜心头像框",
                IconSp = rewardItem.iconImage.sprite,
                RewardAmount = 1,
            });
        }
        else if (rewardItem.rewardData.rewardType == (int)BUDRewardType.RewardHomepageSkin)
        {
            rewardItemList.Add(new CommonRewardItemData()
            {
                rewardType = (int)BUDRewardType.RewardHomepageSkin,
                rewardName = "薄巧甜心主页皮肤",
                IconSp = rewardItem.iconImage.sprite,
                RewardAmount = 1,
            });
        }
        else if (rewardItem.rewardData.rewardType != (int)BUDRewardType.RewardYouYouCoinNewYearPack)
        {
            rewardItemList.Add(rewardItem.rewardData);
        }
        else
        {
            foreach (var rewardInfo in response.rewardList)
            {
                rewardItemList.Add(new CommonRewardItemData()
                {
                    rewardType = rewardInfo.rewardType,
                    rewardName = PgcUtils.GetRewardName((BUDRewardType)rewardInfo.rewardType),
                    IconSp = PgcUtils.LoadRewardIcon((BUDRewardType)rewardInfo.rewardType, panel.gameObject),
                    RewardAmount = rewardInfo.amount,
                });
            }
        }
        panel.ShowRewards(rewardItemList);
        UpdateRedDot();
        RefrashData(activityInfo);
        AccountDataManager.Inst.BalanceInfo.Refresh();
        MessageHelper.Broadcast(MessageName.RefreshSeasonPass);
        MessageHelper.Broadcast(MessageName.ReddotNotice);
    }

    public override void RefrashData(ActivityInfo info)
    {
        base.RefrashData(info);
    
        activityInfo = info;
        foreach (var eventInfo in info.eventList)
        {
            if (itemViews.ContainsKey(eventInfo.eventId))
            {
                itemViews[eventInfo.eventId].SetStatus((ClaimStatus)eventInfo.eventStatus);
            }
        }
        if (info.groupConsumeInfo != null)
        {
            Fill.gameObject.SetActive(true);
            var cur = info.groupConsumeInfo.amount;
            var ls = new List<int> { 0, 380, 880, 1880, 3880, 6880, 9880, 12880 };
            var lsF = new List<float> { 0, 0.07f, 0.23f, 0.38f, 0.53f, 0.69f, 0.84f, 1 };
            ls.Reverse();
            lsF.Reverse();
            for (int i = 0; i < ls.Count; i++)
            {
                if (cur >= ls[i])
                {
                    if (i == 0)
                    {
                        Fill.fillAmount = 1;
                    }
                    else
                    {
                        Fill.fillAmount = lsF[i] + (lsF[i - 1] - lsF[i]) * (cur - ls[i]) / (ls[i - 1] - ls[i]);
                    }
                    break;
                }
            }

            consumedText.SetText(info.groupConsumeInfo.amount.ToString());
            if (info.groupConsumeInfo.groupInfo != null)
            {
                waitTip.SetActive(false);
                exitBtn.gameObject.SetActive(true);
                if (info.groupConsumeInfo.groupInfo.status == GroupStatus.Finished)
                {
                    exitBtn.gameObject.SetActive(false);
                    finishObj.SetActive(true);
                    waittingObj.SetActive(false);
                }
                else if (info.groupConsumeInfo.memberList != null)
                {
                    finishObj.SetActive(false);
                    waittingObj.SetActive(true);
                    if (info.groupConsumeInfo.memberList.Count == 4)
                    {
                        waittingObj.SetActive(false);
                        waitTip.SetActive(true);
                    }

                }
                RefreshMemberInfo();
                consumedText.SetText(info.groupConsumeInfo.amount.ToString());
                GameObjectEx.FindChildByName(applyBtn.transform, "Reddot").gameObject.SetActive(info.groupConsumeInfo.redDot > 0);
            }
        }
    }


    private void RefreshMemberInfo()
    {

        if (activityInfo == null || activityInfo.groupConsumeInfo == null || activityInfo.groupConsumeInfo.memberList == null)
        {
            LoggerUtils.LogError("RefreshMemberInfo Data Is Error");
            return;
        }


        var memberList = new List<MemberInfo>(activityInfo.groupConsumeInfo.memberList);
        if (memberList.Count == 0)
        {
            memberList.Add(new MemberInfo()
            {
                userInfo = AccountDataManager.Inst.UserInfo,
                groupRole = GroupRole.Captain,
            });
        }

        for (int i = 0; i < memberList.Count; i++)
        {
            var memberInfo = memberList[i];
            memberViews[i].gameObject.SetActive(true);
            memberViews[i].SetMemberInfo(i + 1, activityInfo.groupConsumeInfo.groupInfo.status, memberInfo);

        }

        if (activityInfo.groupConsumeInfo.groupInfo.status == GroupStatus.Waiting)
        {
            for (int i = memberList.Count; i < memberViews.Count; i++)
            {
                memberViews[i].gameObject.SetActive(true);
                memberViews[i].SetMemberInfo(i + 1, activityInfo.groupConsumeInfo.groupInfo.status, null, OnInviteClicked);
            }
        }
        // else {
        //     for (int i = memberList.Count; i < memberViews.Count; i++) {
        //         memberViews[i].gameObject.SetActive(false);
        //     }
        // }
        activityInfo.groupConsumeInfo.captainId = memberList.FirstOrDefault(tmp => tmp.groupRole == GroupRole.Captain)?.userInfo?.uid;
        if (activityInfo.groupConsumeInfo.captainId != AccountDataManager.Inst.UserInfo.uid && activityInfo.groupConsumeInfo.groupInfo.status == GroupStatus.Waiting)
        {
            // 非队长，可以退出
            exitBtn.gameObject.SetActive(true);
        }
        else
        {
            exitBtn.gameObject.SetActive(false);
        }
    }

    private void RequestExitGroup()
    {
        if (isSending)
        {
            return;
        }

        isSending = true;
        JObject jObject = new JObject()
        {
            ["groupId"] = activityInfo.groupConsumeInfo?.groupInfo?.groupId,
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.GroupLeave,
            HttpMethod.POST,
            JsonConvert.SerializeObject(jObject),
            (content) =>
            {
                isSending = false;
                // 退出队伍之后，刷新数据
                OnRefreshGroupConsumeAppleData();
                MessageHelper.Broadcast(MessageName.OnRefreshTaskDataAfterBack);
            },
            (error) =>
            {
                isSending = false;
            });
    }

    private void OnApplyCallBack()
    {
        OnRefreshGroupConsumeAppleData();
        MessageHelper.Broadcast(MessageName.OnRefreshTaskDataAfterBack);
    }

}
