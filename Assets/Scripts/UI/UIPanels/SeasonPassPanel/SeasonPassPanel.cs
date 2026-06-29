using System;
using System.Collections.Generic;
using Es;
using Game.Audio;
using Game.Avatar;
using Game.Event;
using GameData.PgcData;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Author:
/// Desc:
/// Date:24-07-11 18:05:58
/// </summary>
public enum Seasontype
{
    S9SeasonPass = 1,
    S9AIPass = 2,
}
public class SeasonPassPanel : BasePanel<SeasonPassPanel> {
    [Header("人物形象")] [SerializeField] internal Transform characterRoot;
    [SerializeField] internal UIDragUtil avatarCameraController1;
    [SerializeField] internal UIDragUtil avatarCameraController2;
    [SerializeField] internal UIDragUtil avatarCameraController3;
    [SerializeField] internal UIDragUtil avatarCameraController4;

    internal CharacterWrap characterWrap;
    internal PlayerAnimationCtrl animationCtrl;

    private Button btn_Back;
    public Seasontype seasonType;

    Dictionary<string, BaseSeasonView> seasonViews = new Dictionary<string, BaseSeasonView>();


    private BaseSeasonView currentSeasonView;
    private Transform BG;
    private string atlasPath = "Assets/Loadable/UI/UIPanel/SeasonPassPanel/SeasonPassPanel.spriteatlas";
    public Action BackAction;
    private SeasonPassListRsp seasonPassRsp;
    private TaskListRsp taskListRsp;
    private GameObject rewardRedDotObj;
    private GameObject taskRedDotObj;

    public override void OnCreate() {
        base.OnCreate();
        InitUI();
        InitAvatar();
        MessageHelper.AddListener(MessageName.RefreshSeasonPass, RequestSeasonPassData);
        MessageHelper.AddListener(MessageName.RefreshSeasonPassTask, RequestTaskInfo);
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        MessageHelper.RemoveListener(MessageName.RefreshSeasonPass, RequestSeasonPassData);
        MessageHelper.RemoveListener(MessageName.RefreshSeasonPassTask, RequestTaskInfo);
    }

    public override void OnShow(params object[] args) {
        base.OnShow(args);
        OnSessionToggleClick("SeasonRewardView");
        RequestSeasonPassData();
        RequestTaskInfo();
    }

    public override void OnWindowBeFocused() {
        base.OnWindowBeFocused();
        MessageHelper.Broadcast(MessageName.RefreshSeasonPassTask);
    }

    private void InitAvatar()
    {
        var saveCharacterData = AccountDataManager.Inst.UserInfo.avatarInfo;
        if (saveCharacterData == null)
            saveCharacterData = AvatarDataManager.Inst.GetDefaultDataByGender(1);
        if (saveCharacterData != null)
        {
            characterWrap = AvatarController.Inst.CreateUIAvatar(saveCharacterData);
            characterWrap.SetParent(characterRoot, true);
            animationCtrl = characterWrap.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
            avatarCameraController1.RotateTarget = characterRoot;
            avatarCameraController2.RotateTarget = characterRoot;
            avatarCameraController3.RotateTarget = characterRoot;
            avatarCameraController4.RotateTarget = characterRoot;

        }
    }

    internal void OnWearAvatar(string pgcId)
    {
        var config = DataTables.GetAvatarCommonData(pgcId);
        if (config == null) return;
        var classType = UniqueType.GetAvatar(pgcId);
        characterWrap.ChangePart(classType, pgcId);
        characterWrap.ChangeColor(classType, config.defaultColor);
        characterWrap.Move(classType, config.pDef);
        characterWrap.Rotate(classType, config.rDef);
        characterWrap.Scale(classType, config.sDef);
        characterWrap.HVScale(classType, config.vhSDef);
        characterWrap.SetLeftOrRight(classType, config.leftRightType);
    }

    private void InitUI() {
        btn_Back = GameObjectEx.FindChildByName(this.transform, "BaseLayout2D/TopBar/Btn_Back").GetComponent<Button>();
        btn_Back.onClick.AddListener(OnBackClick);
        foreach (var tmpSeasonView in transform.GetComponentsInChildren<BaseSeasonView>(true)) {
            tmpSeasonView.OnCreate();
            seasonViews.Add(tmpSeasonView.gameObject.name, tmpSeasonView);
            tmpSeasonView.gameObject.SetActive(false);
        }

        var toggleGroup = GameObjectEx.FindComponentByName<ToggleGroup>(transform, "BaseLayout2D/TopBar/TopSessionContent");
        foreach(var toggle in toggleGroup.GetComponentsInChildren<Toggle>()) {
            toggle.onValueChanged.AddListener((value) => {
                if (value) {
                    AkSoundManager.Inst.PlayUIEffectSound(UISoundType.UI_ShiftItems_B2);
                    OnSessionToggleClick(toggle.name);
                }
            });
        }

        rewardRedDotObj = GameObjectEx.FindChildByName(toggleGroup.transform, "SeasonRewardView/RedDot").gameObject;
        taskRedDotObj = GameObjectEx.FindChildByName(toggleGroup.transform, "SeasonTaskView/RedDot").gameObject;
        InitBG();
    }

    private void InitBG() {
        BG = GameObjectEx.FindChildByName(this.transform, "BG");

        if (BG == null) {
            LoggerUtils.LogError("[BG] Check GenderSelectPanel Bg Object");
            return;
        }

        //var itemObj = Loader
        //    .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
        //    .Instantiate(BG);
        //var item = itemObj.GetComponent<ActivityCenterBgItem>();
        //item.InitCustomBgItem(SeasonPassDataManager.BgColorHex, atlasPath, new List<string>() {
        //    "seasonpass_bg_1", "seasonpass_bg_2"
        //});
        //item.gameObject.SetActive(true);
    }

    public void Reload() {
        RequestSeasonPassData();
    }

    public void SwitchView(string viewName) {
        OnSessionToggleClick(viewName);
    }

    private void OnBackClick() {
        if (this == null) {
            return;
        }

        if (currentSeasonView != null && currentSeasonView.HandleBackBtnClick()){
            return;
        }
        BackAction?.Invoke();
        CloseSelf();
    }

    private void OnSessionToggleClick(string toggleName) {
        if (!seasonViews.TryGetValue(toggleName, out var seasonView)) return;
        if (currentSeasonView != null) {
            currentSeasonView.gameObject.SetActive(false);
        }
        seasonView.gameObject.SetActive(true);
        currentSeasonView = seasonView;
        currentSeasonView.OnShow();
        if (seasonPassRsp != null) {
            currentSeasonView.RefreshData(seasonPassRsp);
            currentSeasonView.RefreshData(taskListRsp);
        }

        var toggleGroup = GameObjectEx.FindChildByName(transform, "BaseLayout2D/TopBar/TopSessionContent");
        toggleGroup.gameObject.SetActive(toggleName != "SeasonPurchaseView");

    }


    private void RequestSeasonPassData() {
        // if(seasonType == Seasontype.S9SeasonPass)
        // {
        //     SeasonPassDataManager.Inst.SetType(SeasonPassType.S9SeasonPass);
        // }else if (seasonType == Seasontype.S9AIPass)
        // {
        //     SeasonPassDataManager.Inst.SetType(SeasonPassType.S9AbandonedHospitalSeasonPass);
        // }
        //
        //     SeasonPassDataManager.Inst.GetSeasonPassList((isSuccess, rsp) => {
        //     if (this == null || gameObject == null) {
        //         return;
        //     }
        //     if (isSuccess) {
        //         seasonPassRsp = rsp;
        //         if (currentSeasonView != null) {
        //             currentSeasonView.RefreshData(rsp);
        //         }
        //
        //         bool isHasRedDot = rsp.rewardList.Exists(tmp => tmp.BudRewardStatus == BudRewardStatus.Unlocked) || rsp.paidRewardList.Exists(tmp => tmp.BudRewardStatus == BudRewardStatus.Unlocked);
        //         rewardRedDotObj.SetActive(isHasRedDot);
        //     }
        // });
    }


    private void RequestTaskInfo() {
        if (this == null || gameObject == null) {
            return;
        }
        var reqParam = new GetTaskListReq();
        if(seasonType == Seasontype.S9SeasonPass)
        {
            reqParam = new GetTaskListReq()
            {
                idList = new List<string>() {
                TASK_ID.S9SeasonDaily.ToString(), TASK_ID.S9SeasonChallenge.ToString(), TASK_ID.S9SeasonWeekTask.ToString(),
                TASK_ID.S9SeasonWeeklyActive.ToString()
            }
            };
        }else if(seasonType == Seasontype.S9AIPass)
        {
            reqParam = new GetTaskListReq()
            {
                idList = new List<string>() {
                TASK_ID.S9AiGameSeasonWeekExperience.ToString(), TASK_ID.S9AiGameSeasonChallenge.ToString(),TASK_ID.S9AiGameSeasonWeekTask.ToString()
            }
            };
        }

        

        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.TaskList, HttpMethod.POST,
            JsonConvert.SerializeObject(reqParam), (content) => {
                taskListRsp = JsonConvert.DeserializeObject<TaskListRsp>(content);
                if (taskListRsp == null || taskListRsp.list == null)
                    return;
                if (currentSeasonView != null) {
                    currentSeasonView.RefreshData(taskListRsp);
                }

                bool isHasRedDot = taskListRsp.list.Exists(tmp => tmp.eventList != null && tmp.eventList.Exists(tmpEvent => tmpEvent != null && tmpEvent.eventStatus == (int)ClaimStatus.Unlocked));
                taskRedDotObj.SetActive(isHasRedDot);


            }, (error) => {
            });
    }






}

public class SeasonTaskRewardInfo {
    public string rewardName;
    public BUDRewardType rewardType;
    public int rewardAmount;
    public string pgcId;
}


public class SeasonTaskEventInfo {
    public int eventId;
    public EventCenterSkipType eventSkipType;
    public string eventName;
    public int targetAmount;
    public List<SeasonTaskRewardInfo> rewardList;
    public int spendNum;
}

