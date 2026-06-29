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

public class SeasonPassBaseView : MonoBehaviour {
    [Header("人物形象")] 
    [SerializeField] internal Transform characterRoot;
    [SerializeField] internal UIDragUtil avatarCameraController1;
    [SerializeField] internal UIDragUtil avatarCameraController2;
    [SerializeField] internal UIDragUtil avatarCameraController3;

    [SerializeField] private DailyTaskView _dailView;
    [SerializeField] private SeasonTaskSubView _seasonSubView;
    [SerializeField] private WeeklyTaskView _weeklyView;
    
    internal CharacterWrap characterWrap;
    internal PlayerAnimationCtrl animationCtrl;
    Dictionary<string, BaseSeasonView> seasonViews = new Dictionary<string, BaseSeasonView>();
    
    [HideInInspector] public BaseSeasonView currentSeasonView;
    private string atlasPath = "Assets/Loadable/UI/UIPanel/SeasonPassPanel/SeasonPassPanel.spriteatlas";
    public Action BackAction;
    private SeasonPassListRsp seasonPassRsp;
    private TaskListRsp taskListRsp;
    private GameObject rewardRedDotObj;
    private GameObject taskRedDotObj;
    
    public SeasonPassConfig _curSeasonPassConfig;
    private Button btn_Back;

    public virtual void InitData(SeasonPassConfig config)
    {
        this._curSeasonPassConfig = config;
        
        _dailView.InitData(config);
        _seasonSubView.InitData(config);
        _weeklyView.InitData(config);
        
        MessageHelper.AddListener(MessageName.RefreshSeasonPass, RequestSeasonPassData);
        MessageHelper.AddListener(MessageName.RefreshSeasonPassTask, RequestTaskInfo);
        
        InitUI();
        //InitAvatar();
        
        OnSessionToggleClick("SeasonRewardView");
        RequestSeasonPassData();
        RequestTaskInfo();
    }
    
    public virtual void OnDestroy()
    {
        MessageHelper.RemoveListener(MessageName.RefreshSeasonPass, RequestSeasonPassData);
        MessageHelper.RemoveListener(MessageName.RefreshSeasonPassTask, RequestTaskInfo);
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
        }

        if (SeasonPassDataManager.Inst.GetSeasonPassList(SeasonPassDataManager.Inst.CurrentSeasonPassType) != null)
        {
            var paidRewardList = SeasonPassDataManager.Inst.GetSeasonPassList(SeasonPassDataManager.Inst.CurrentSeasonPassType);
            paidRewardList.paidRewardList.ForEach(r =>
            {
                r.rewardInfo.itemList.ForEach(i =>
                {
                    if (i.pgcIdList != null)
                        i.pgcIdList.ForEach(p => OnWearAvatar(p));
                });
            });
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
        btn_Back = GameObjectEx.FindChildByName(this.transform, "BaseLayout2D/Btn_Back").GetComponent<Button>();
        btn_Back.onClick.AddListener(OnBackClick);
        foreach (var tmpSeasonView in transform.GetComponentsInChildren<BaseSeasonView>(true)) {
            tmpSeasonView.OnCreate();
            seasonViews.Add(tmpSeasonView.gameObject.name, tmpSeasonView);
            tmpSeasonView.gameObject.SetActive(false);
        }

        var toggleGroup = GameObjectEx.FindComponentByName<ToggleGroup>(transform, "BaseLayout2D/TopSessionContent");
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
    }

    public void OnSessionToggleClick(string toggleName) {
        if (!seasonViews.TryGetValue(toggleName, out var seasonView)) return;

        if(toggleName == "SeasonPurchaseView")
        {
            seasonView.gameObject.SetActive(true);
            seasonView.OnShow();
            if (seasonPassRsp != null)
            {
                seasonView.RefreshData(seasonPassRsp);
                seasonView.RefreshData(taskListRsp);
            }
            return;
        }


        if (currentSeasonView != null ) {
            currentSeasonView.gameObject.SetActive(false);
        }
        seasonView.gameObject.SetActive(true);
        currentSeasonView = seasonView;
        currentSeasonView.OnShow();
        if (seasonPassRsp != null) {
            currentSeasonView.RefreshData(seasonPassRsp);
            currentSeasonView.RefreshData(taskListRsp);
        }

    }

    private void RequestSeasonPassData() {
        SeasonPassDataManager.Inst.GetSeasonPassList(SeasonPassDataManager.Inst.CurrentSeasonPassType,(isSuccess, rsp) => {
            if (this == null || gameObject == null) {
                return;
            }
            if (isSuccess) {
                seasonPassRsp = rsp;
                if (currentSeasonView != null) {
                    currentSeasonView.RefreshData(rsp);
                }

                if(rsp.ShowPrePaidRewardPopup == 1)
                {
                    ShowSeasonReward();
                }

                if (rsp == null || rsp.paidRewardList == null || rsp.rewardList == null)
                {
                    return;
                }
                bool isHasRedDot = rsp.rewardList.Exists(tmp => tmp.BudRewardStatus == BudRewardStatus.Unlocked) || rsp.paidRewardList.Exists(tmp => tmp.BudRewardStatus == BudRewardStatus.Unlocked);
                rewardRedDotObj.SetActive(isHasRedDot);
                MessageHelper.Broadcast(MessageName.SeasonPassDataUpdate);
            }
        });
    }
    private void ShowSeasonReward()
    {
        var rewardItemDatas = new List<CommonRewardItemData>();

        string spriteatlasPath = "Assets/Arts/UI/UIPanel/RechargePanel/SeasonPrePack/SeasonPrePackAtlas.spriteatlas";
        var itemData2 = new CommonRewardItemData()
        {
            IconSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, "season", gameObject) ,
            RewardAmount = 1,
            rewardName = "高级通行证",
        };
        rewardItemDatas.Add(itemData2);
        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
        panel.ShowRewards(rewardItemDatas);
    }

    private void RequestTaskInfo()
    {
        if (this == null || gameObject == null)
        {
            return;
        }

        var reqParam = new GetTaskListReq();
        reqParam.idList = _curSeasonPassConfig.TaskIdList;
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.TaskList, HttpMethod.POST,
            JsonConvert.SerializeObject(reqParam), (content) =>
            {
                taskListRsp = JsonConvert.DeserializeObject<TaskListRsp>(content);
                if (taskListRsp == null || taskListRsp.list == null)
                    return;
                if (currentSeasonView != null)
                {
                    currentSeasonView.RefreshData(taskListRsp);
                }

                bool isHasRedDot = taskListRsp.list.Exists(tmp =>
                    tmp.eventList != null && tmp.eventList.Exists(tmpEvent =>
                        tmpEvent != null && tmpEvent.eventStatus == (int)ClaimStatus.Unlocked));
                taskRedDotObj.SetActive(isHasRedDot);


            }, (error) => { });
    }
}
