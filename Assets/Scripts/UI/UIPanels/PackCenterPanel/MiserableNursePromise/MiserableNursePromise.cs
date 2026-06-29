using System;
using System.Collections;
using System.Collections.Generic;
using Es;
using Game.Avatar;
using Game.Event;
using GameData.PgcData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;

public class MiserableNursePromise : PackBaseView
{
    public CButton Btn_GoHospital;
    public CButton Btn_Buy;
    public GameObject Go_Buy;
    public GameObject Go_Buyed;
    public Transform characterRoot;
    public AvatarCameraController avatarCameraController;
    public PackCommonItem packItem;
    public Transform taskContent;
    
    private string spriteatlasPath;
    private List<PackCommonItem> packItemNodeList = new List<PackCommonItem>();
    private bool _isPaid = false;
    private bool _isInit = false;
    
    #region TaskDaata
    private TASK_ID _curTaskIdType = TASK_ID.MiserableNursePromise;
    private string _curTaskId = "MiserableNursePromise";
    private TaskInfoData _taskInfoData;
    #endregion
    
    #region Avatar
    private CharacterWrap characterWrapper;
    private PlayerAnimationCtrl animationCtrl;
    private PlayerAnimationCtrl otherAnimationCtrl;
    private bool isInitAnimation = false;

    private List<string> pgcIds = new List<string>()
    {
        "12100008","12000007","10200019","11300214","11200026","10900306","10600061","10400305"
    };
    #endregion
    private void Start()
    {
        LoadConfig();
        InitUIComponent();
        CreateItems(packViewConfig.rewardDataList);
        InitAvatar();
        Invoke("Delay", 0.1f);
        isInitAnimation = true;
        
        RefreshTaskStatus();

        _isInit = true;
    }

    private void OnEnable()
    {
        if(_isInit)
            RefreshTaskStatus();
    }

    private void LoadConfig()
    {
        string configPath = "Assets/Loadable/UI/UIPanel/PackCenterPanel/MiserableNursePromise/MiserableNursePromise.json";
        spriteatlasPath = "Assets/Loadable/UI/UIPanel/PackCenterPanel/MiserableNursePromise/MiserableNursePromise.spriteatlas";
        var textAsset = Loader.Load<TextAsset>(configPath, gameObject);
        packViewConfig = JsonConvert.DeserializeObject<PackViewConfig>(textAsset.text);
        TaskId = _curTaskId;
    }

    #region UI相关
    private void InitUIComponent()
    {
        Btn_GoHospital.onClick.AddListener(OnBtnGoHospitalClick);
        Btn_Buy.onClick.AddListener(OnBtnBuyClick);
    }

    private void OnBtnGoHospitalClick()
    {
        UIManager.Inst.OpenPanel(PanelId.AIHospitalMainEntryPanel);
    }
    
    private void OnBtnBuyClick()
    {
        var req = new JObject()
        {
            ["productType"] = (int)BUDProductType.NursePromisePackage,
            ["productId"] = TASK_ID.MiserableNursePromise.ToString()
        };

        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.PayProductByGem,
            HttpMethod.POST,
            JsonConvert.SerializeObject(req),
            OnBuySuccess, OnBuyFail);
    }

    private void OnBuySuccess(string content)
    {
        TipPanel.ShowToast("购买成功！");
        
        //1.刷新货币余额
        AccountDataManager.Inst.BalanceInfo.Refresh();
        
        //2.刷新状态
        RefreshTaskStatus();
    }

    private void OnBuyFail(string error)
    {
        LoggerUtils.LogError("MiserableNursePromise ", "OnBuyFail");
    }
    
    private void CreateItems(List<RewardItem> rewardDataList) {
        packItemNodeList.Clear();

        for (var i = 1; i < rewardDataList.Count; i++) {
            var taskItem = Instantiate(packItem, taskContent);
            taskItem.SetAtlasPath(spriteatlasPath);
            taskItem.OnInitCreate(rewardDataList[i], true);
            packItemNodeList.Add(taskItem);
        }
    }

    private void OnBlockAction(int rewardId)
    {
        switch (rewardId)
        {
            case 1:
                var bundlePreviewPanel = UIManager.Inst.OpenPanel<RewardPreviewPanel>(PanelId.RewardPreviewPanel);
                bundlePreviewPanel.SetEventPreview(new List<string> { "12100008","12000007","10200019","11300214","11200026","10900306","10600061","10400305" }, "丧丧护士套装", "rewardIcon1", spriteatlasPath, "丧丧护士的约定","#EFC5FF", "MiserableNursePromise_Bg");
                break;
            case 2:
                var avatarFramePreviewPanel = UIManager.Inst.OpenPanel<CurrencyTipsPanel>(PanelId.CurrencyTipsPanel);
                avatarFramePreviewPanel.PreviewAvatarFrame(AvatarFrameType.AvatarFrameSangSang);
                break;
            case 3:
                var bubblePreviewPanel = UIManager.Inst.OpenPanel<CurrencyTipsPanel>(PanelId.CurrencyTipsPanel);
                bubblePreviewPanel.PreviewChatBubble(ChatBubblesType.ChatBubblesSangSang, "丧丧护士聊天气泡", "通过丧丧护士的约定完成任务获得");
                break;
        }
    }
    #endregion

    #region Avatar相关
    private void InitAvatar() {
        var saveCharacterData = AccountDataManager.Inst.UserInfo.avatarInfo;
        if (saveCharacterData == null)
            saveCharacterData = AvatarDataManager.Inst.GetDefaultDataByGender(1);
        if (saveCharacterData != null) {
            characterWrapper = AvatarController.Inst.CreateUIAvatar(saveCharacterData);
            characterWrapper.SetParent(characterRoot, true);
            animationCtrl = characterWrapper.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
            avatarCameraController.RotateTarget = characterRoot;

            var otherCharacterWrap = AvatarController.Inst.CreateUIAvatar(saveCharacterData);
            otherCharacterWrap.SetParent(characterRoot, true);
            otherAnimationCtrl = otherCharacterWrap.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
            otherCharacterWrap.Avatar.gameObject.SetActive(false);
        }
    }
    
    private void Delay() {
        WearPgcClothes(pgcIds);
    }
    
    private void WearPgcClothes(List<string> pgcIds) {
        if (pgcIds == null) {
            return;
        }

        for (int i = 0; i < pgcIds.Count; i++) {
            var pgcId = pgcIds[i];
            var config = DataTables.GetGameResData(pgcId);
            if (config == null) continue;
            switch ((ResourceType)config.ResourceType) {
                case ResourceType.Avatar:
                    TryOn(pgcId);
                    break;
                case ResourceType.Emote:
                    PreviewEmote(pgcId, (EmoteSubType)config.SubType);
                    break;
            }
        }
    }

    private void TryOn(string pgcId) {
        var pgcConfig = PgcUtils.GetPgcConfigData(pgcId);
        var config = DataTables.GetAvatarCommonData(pgcId);
        var classType = UniqueType.GetAvatar(pgcId);
        characterWrapper.ChangePart(UniqueType.GetAvatar((AvatarSubType)pgcConfig.SubType),
            pgcId);
        characterWrapper.ChangeColor(classType, config.defaultColor);
        characterWrapper.Move(classType, config.pDef);
        characterWrapper.Rotate(classType, config.rDef);
        characterWrapper.Scale(classType, config.sDef);
        characterWrapper.HVScale(classType, config.vhSDef);
        characterWrapper.SetLeftOrRight(classType, config.leftRightType);
    }

    internal void PreviewEmote(string pgcId, EmoteSubType emoteSubType) {
        avatarCameraController?.SetEmoteView(pgcId);
        animationCtrl?.ResetEmoteForUICharacter();
        otherAnimationCtrl?.gameObject.SetActive(false);
        otherAnimationCtrl?.ResetEmoteForUICharacter();
        switch (emoteSubType) {
            case EmoteSubType.Single:
            case EmoteSubType.SingleLoop:
                animationCtrl?.PlaySingleEmoteForUICharacter(pgcId, null);
                break;
            case EmoteSubType.Double:
            case EmoteSubType.DoubleLoop:
                animationCtrl?.PlayDoubleEmoteForUICharacter(pgcId, otherAnimationCtrl, null);
                break;
        }
    }
    #endregion

    #region Task刷新
    protected override void OnTaskListUpdate(TaskListRsp taskListRsp) {
        List<TaskInfoData> taskInfoDatas = taskListRsp.list;
        if (taskInfoDatas == null || taskInfoDatas.Count <= 0) {
            return;
        }

        TaskInfoData tsTaskInfoData = taskInfoDatas[0];
        if (tsTaskInfoData == null) {
            return;
        }

        this._taskInfoData = tsTaskInfoData;

        List<TaskItemData> eventList = tsTaskInfoData.eventList;
        for (int i = 0; i < eventList.Count; i++)
        {
            var rewardId = eventList[i].eventId;
            if (i == 0)
            {
                //第0个是是否购买
                _isPaid = eventList[i].eventStatus == (int)EventStatus.Finish;
            }
            else
            {
                packItemNodeList[i - 1].SetData(TaskId, eventList[i], taskItemData => { RefreshTaskStatus(); },
                    () =>
                    {
                        OnBlockAction(rewardId);
                    });
            }
        }

        Go_Buy.SetActive(!_isPaid);
        Go_Buyed.SetActive(_isPaid);
    }
    #endregion
}
