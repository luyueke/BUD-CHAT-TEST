using Es;
using Game.Audio;
using Game.Avatar;
using Game.Event;
using GameData.PgcData;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class SeasonTaskView : BaseSeasonView
{
    private Text txt_CurDay;
    private Text txt_CurProgress;
    private Scrollbar TopProgressScrollBar;
    private GameObject TopProgressContent;
    private Dictionary<string, Toggle> tabToggles = new Dictionary<string, Toggle>();
    private Dictionary<string, BaseTaskSubView> taskSubViews = new Dictionary<string, BaseTaskSubView>();
    private Dictionary<string, GameObject> redDots = new Dictionary<string, GameObject>();
    private Toggle currentTab;
    private BaseTaskSubView currentSubView;
    private SeasonPassListRsp seasonPassRsp;
    private TaskListRsp taskListRsp;
    private CButton claimAllBtn;
    private CButton buyBtn;
    private CButton infoBtn;
    public Button btn_TopTimeContent;
    private Text txt_Title;
    public SeasonPassTipsView _tipsView;
    public CButton dayContainer;
    public CButton weekContainer;
    public GameObject fireView;
    public AvatarCameraController avatarCameraController;
    public GameObject modelRoot;
    public override void OnCreate()
    {
        base.OnCreate();

        buyBtn = GameObjectEx.FindChildByName(this.transform, "Free").GetComponent<CButton>();
        txt_Title = GameObjectEx.FindChildByName(this.transform, "Txt_Title").GetComponent<Text>();
        txt_CurProgress = GameObjectEx.FindChildByName(this.transform, "Txt_CurProgress").GetComponent<Text>();
        txt_CurDay = GameObjectEx.FindChildByName(this.transform, "Txt_CurDay").GetComponent<Text>();
        TopProgressScrollBar =
            GameObjectEx.FindChildByName(this.transform, "TopProgressScrollBar").GetComponent<Scrollbar>();
        TopProgressContent = GameObjectEx.FindChildByName(this.transform, "TopProgressContent").gameObject;
        infoBtn = GameObjectEx.FindComponentByName<CButton>(transform, "InfoBtn");
        var taskTabContent = GameObjectEx.FindChildByName(transform, "LeftContent/TabContent");
        foreach (var tabToggle in taskTabContent.GetComponentsInChildren<Toggle>(true))
        {
            tabToggles.Add(tabToggle.gameObject.name, tabToggle);
            tabToggle.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    AkSoundManager.Inst.PlayUIEffectSound(UISoundType.UI_ShiftItems_B2);
                    OnToggleSelected(tabToggle.gameObject.name);
                }
            });
            redDots.Add(tabToggle.gameObject.name, GameObjectEx.FindChildByName(tabToggle.gameObject, "RedDot").gameObject);
        }

        foreach (var taskSubView in transform.GetComponentsInChildren<BaseTaskSubView>(true))
        {
            taskSubViews.Add(taskSubView.taskKey, taskSubView);
            taskSubView.gameObject.SetActive(false);
        }
        txt_Title.SetLocalText(SeasonPassDataManager.Inst.GetSeasonPassConfig(SeasonPassDataManager.Inst.CurrentSeasonPassType, this.gameObject).Title);
        buyBtn.onClick.AddListener(OnBuySeasonPass);
        claimAllBtn = GameObjectEx.FindComponentByName<CButton>(transform, "ClaimAllBtn");
        claimAllBtn.onClick.AddListener(ClaimAllRewards);
        infoBtn.onClick.AddListener(OnRuleBtnClick);
        btn_TopTimeContent.onClick.AddListener(() =>
        {
            if (SeasonPassDataManager.Inst.FinishAllProgress(seasonPassRsp?.rewardList))
            {
                return;
            }

            var leftTime = seasonPassRsp?.progressInfo?.end - seasonPassRsp?.progressInfo?.start ?? 0;
            _tipsView.gameObject.SetActive(true);
            _tipsView.SetData(seasonPassRsp.progressInfo.start, leftTime);
        });
        dayContainer.onClick.AddListener(OnClickFire);
        weekContainer.onClick.AddListener(OnClickFire);

        List<string> pgcIds = SeasonPassDataManager.Inst.CurrentSeasonPassType switch
        {
            SeasonPassType.S14SeasonPass => new List<string>() { "10400508", "11300369", "10200070" },
            SeasonPassType.S15SeasonPass => new List<string>() { "10400513", "12100153", "10900514" },
            _ => new List<string>(),
        };
        var saveCharacterData = AccountDataManager.Inst.UserInfo.avatarInfo.Clone();
        var characterWrapper = AvatarController.Inst.CreateUIAvatarWithIKController(saveCharacterData, modelRoot.transform);
        avatarCameraController.RotateTarget = modelRoot.transform;
        foreach (var pgcId in pgcIds)
        {
            var pgcConfig = PgcUtils.GetPgcConfigData(pgcId);
            var config = DataTables.GetAvatarCommonData(pgcId);
            if (pgcConfig == null || config == null) continue;
            var classType = UniqueType.GetAvatar(pgcId);
            characterWrapper.ChangePart(UniqueType.GetAvatar((AvatarSubType)pgcConfig.SubType), pgcId);
            characterWrapper.ChangeColor(classType, config.defaultColor);
            characterWrapper.Move(classType, config.pDef);
            characterWrapper.Rotate(classType, config.rDef);
            characterWrapper.Scale(classType, config.sDef);
            characterWrapper.HVScale(classType, config.vhSDef);
            characterWrapper.SetLeftOrRight(classType, config.leftRightType);
        }
    }

    public override void OnShow()
    {
        base.OnShow();
        if (tabToggles.TryGetValue("DailyTask", out Toggle dailyTaskToggle))
        {
            if (dailyTaskToggle.isOn == false)
            {
                dailyTaskToggle.isOn = true;
            }
            else
            {
                OnToggleSelected("DailyTask");
            }
        }
    }


    void OnClickFire()
    {
        fireView.SetActive(true);
    }

    private void OnBuySeasonPass()
    {
        if (SeasonPassDataManager.Inst.GetSeasonPassIsPaid(SeasonPassDataManager.Inst.CurrentSeasonPassType) && SeasonPassDataManager.Inst.GetSeasonPassPaidType(SeasonPassDataManager.Inst.CurrentSeasonPassType) == 1)
        {
            return;
        }

        var panel = UIManager.Inst.FindPanel<NewSeasonPassPanel>(PanelId.NewSeasonPassPanel);
        if (panel != null)
        {
            panel.SwitchView(SeasonPassDataManager.Inst.CurrentSeasonPassType, "SeasonPurchaseView");
        }
    }


    private void OnRuleBtnClick()
    {
        UIManager.Inst.OpenPanel<GashaponRulePanel>(PanelId.GashaponRulePanel, "Assets/Loadable/UI/UIPanel/SeasonPassPanel/SeasonPassRule.json");
    }
    public override void RefreshData(SeasonPassListRsp rsp)
    {
        base.RefreshData(rsp);
        seasonPassRsp = rsp;
        if (seasonPassRsp.isPaid == 1)
        {
            //高级通行证时，显示升级通行证
            if (seasonPassRsp.paidType == 0)
            {
                buyBtn.gameObject.SetActive(true);
                var tex = GameObjectEx.FindChildByName(buyBtn.transform, "Txt_BtnTitle").GetComponent<Text>();
                GameObjectEx.FindChildByName(buyBtn.transform, "BuyToGetTip").gameObject.SetActive(false);
                tex.text = "升级豪华通行证";
            }
            else
            {
                //如果已经是豪华通行证，需要隐藏
                buyBtn.gameObject.SetActive(false);
            }
        }
        else
        {
            //如果没有买要把按钮打开
            buyBtn.gameObject.SetActive(true);
        }
        if (seasonPassRsp.progressInfo != null)
        {
            txt_CurProgress.text = seasonPassRsp.progressInfo.start.ToString() + "/" +
                                   LocalizationManager.Inst.GetLocalizedText("{0}经验", seasonPassRsp.progressInfo.end);
            TopProgressScrollBar.size = (float)seasonPassRsp.progressInfo.start / seasonPassRsp.progressInfo.end;
            txt_CurDay.SetLocalText("{0}级", seasonPassRsp.progressInfo.currentTier);
            TopProgressContent.SetActive(true);
        }

        if (currentSubView != null)
        {
            currentSubView.RefreshData(seasonPassRsp);
        }
    }

    public override void RefreshData(TaskListRsp rsp)
    {
        base.RefreshData(rsp);

        if (rsp == null)
        {
            return;
        }
        taskListRsp = rsp;
        if (currentSubView != null)
        {
            currentSubView.RefreshData(taskListRsp);
        }

        bool canClaimReward = false;
        if (rsp.list != null)
        {
            redDots["DailyTask"].SetActive(false);
            redDots["WeeklyTask"].SetActive(false);
            redDots["SeasonTask"].SetActive(false);
            foreach (var taskInfo in rsp.list)
            {
                //日
                if (taskInfo.taskId == TASK_ID.S14SeasonDaily.ToString() || taskInfo.taskId == TASK_ID.S15SeasonDaily.ToString())
                {
                    bool isHasRedDot = redDots["DailyTask"].activeSelf || taskInfo.eventList.Exists(tmp =>
                        tmp != null && tmp.eventStatus == (int)ClaimStatus.Unlocked);
                    redDots["DailyTask"].SetActive(isHasRedDot);
                    canClaimReward = canClaimReward || isHasRedDot;
                }
                else if (taskInfo.taskId == TASK_ID.S9AiGameSeasonWeekExperience.ToString())
                {
                    bool isHasRedDot = redDots["DailyTask"].activeSelf || taskInfo.eventList.Exists(tmp =>
                       tmp != null && tmp.eventStatus == (int)ClaimStatus.Unlocked);
                    redDots["DailyTask"].SetActive(isHasRedDot);
                    canClaimReward = canClaimReward || isHasRedDot;
                }
                //周
                if (taskInfo.taskId == TASK_ID.S14SeasonWeekTask.ToString() || taskInfo.taskId == TASK_ID.S15SeasonWeekTask.ToString())
                {
                    bool isHasRedDot = redDots["WeeklyTask"].activeSelf || taskInfo.eventList.Exists(tmp =>
                        tmp != null && tmp.eventStatus == (int)ClaimStatus.Unlocked);
                    redDots["WeeklyTask"].SetActive(isHasRedDot);
                    canClaimReward = canClaimReward || isHasRedDot;
                }
                else if (taskInfo.taskId == TASK_ID.S9AiGameSeasonWeekTask.ToString())
                {
                    bool isHasRedDot = redDots["WeeklyTask"].activeSelf || taskInfo.eventList.Exists(tmp =>
                       tmp != null && tmp.eventStatus == (int)ClaimStatus.Unlocked);
                    redDots["WeeklyTask"].SetActive(isHasRedDot);
                    canClaimReward = canClaimReward || isHasRedDot;
                }

                //赛季
                if (taskInfo.taskId == TASK_ID.S14SeasonChallenge.ToString() || taskInfo.taskId == TASK_ID.S15SeasonChallenge.ToString())
                {
                    bool isHasRedDot = redDots["SeasonTask"].activeSelf || taskInfo.eventList.Exists(tmp =>
                        tmp != null && tmp.eventStatus == (int)ClaimStatus.Unlocked);
                    redDots["SeasonTask"].SetActive(isHasRedDot);
                    canClaimReward = canClaimReward || isHasRedDot;
                }
                else if (taskInfo.taskId == TASK_ID.S9AiGameSeasonChallenge.ToString())
                {
                    bool isHasRedDot = redDots["SeasonTask"].activeSelf || taskInfo.eventList.Exists(tmp =>
                       tmp != null && tmp.eventStatus == (int)ClaimStatus.Unlocked);
                    redDots["SeasonTask"].SetActive(isHasRedDot);
                    canClaimReward = canClaimReward || isHasRedDot;
                }

            }
        }

        claimAllBtn.interactable = canClaimReward;



    }


    private void ClaimAllRewards()
    {
        var claimTaskId = TASK_ID.S15SeasonChallenge.ToString();
        switch (SeasonPassDataManager.Inst.CurrentSeasonPassType)
        {
            case SeasonPassType.S12SeasonPass:
                claimTaskId = TASK_ID.S12SeasonChallenge.ToString();
                break;
            case SeasonPassType.S14SeasonPass:
                claimTaskId = TASK_ID.S14SeasonChallenge.ToString();
                break;
            case SeasonPassType.S15SeasonPass:
                claimTaskId = TASK_ID.S15SeasonChallenge.ToString();
                break;
            case SeasonPassType.S9AbandonedHospitalSeasonPass:
                claimTaskId = TASK_ID.S9AiGameSeasonChallenge.ToString();
                break;
        }
        EventCenterDataManager.Inst.CliamReward(claimTaskId, 1, 2, 0, (rsp) =>
        {
            if (rsp.rewardList != null && rsp.rewardList.Count > 0)
            {
                OnClaimedSuccess(rsp);
            }
        });
    }



    private void OnClaimedSuccess(TaskClaimRsp rsp)
    {

        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
        var rewardList = new List<CommonRewardItemData>() { };
        foreach (var rewardInfo in rsp.rewardList)
        {
            var rewardItemData = new CommonRewardItemData();
            rewardItemData.rewardType = rewardInfo.rewardType;
            rewardItemData.RewardAmount = rewardInfo.amount;
            rewardItemData.rewardName = PgcUtils.GetRewardName((BUDRewardType)rewardInfo.rewardType);
            rewardList.Add(rewardItemData);
        }
        panel.ShowRewards(rewardList);
        AccountDataManager.Inst.BalanceInfo.Refresh();
        MessageHelper.Broadcast(MessageName.RefreshSeasonPass);
        MessageHelper.Broadcast(MessageName.RefreshSeasonPassTask);
    }


    private void OnToggleSelected(string toggleName)
    {
        if (!tabToggles.TryGetValue(toggleName, out var toggle)) return;
        if (currentTab != null)
        {
            currentTab.GetComponentInChildren<Text>(true).color = Color.white;
        }

        currentTab = toggle;
        currentTab.GetComponentInChildren<Text>(true).color = new Color32(255, 211, 54, 255);

        if (!taskSubViews.TryGetValue(toggleName, out var subView))
        {
            return;
        }

        if (currentSubView != null)
        {
            currentSubView.gameObject.SetActive(false);
        }

        currentSubView = subView;
        currentSubView.gameObject.SetActive(true);
        currentSubView.RefreshData(seasonPassRsp);
        if (taskListRsp != null)
        {
            currentSubView.RefreshData(taskListRsp);
        }

        currentSubView.OnShow();
    }


}


