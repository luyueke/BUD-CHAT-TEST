using System;
using System.Collections.Generic;
using System.Linq;
using Basic.Extensions;
using Basic.Utils;
using Es;
using Game.Audio;
using Game.Avatar;
using Game.Event;
using GameData.PgcData;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.Manager;
using UI.UIPanels.RechargePanel;
using UnityEngine;
using UnityEngine.UI;
using EventTracking;
using UI.UIPanels.RechargePanel;

public class FirstChargeOneYuanView : MonoBehaviour
{
    [SerializeField] private FirstChargeOneYuanRewardItem rewardItemPrefab;

    [SerializeField] private Transform taskContent;

    [SerializeField] private Button oneYuanBtn;
    [SerializeField] private Button unlockBtn;
    [SerializeField] private Button otherAmountBtn;

    private List<FirstChargeOneYuanRewardItem> firstChargeOneYuanRewardItems = new List<FirstChargeOneYuanRewardItem>();

    [SerializeField] private RawImage bgImage;

    [SerializeField] private AvatarCameraController avatarCameraController;
    [SerializeField] private Transform characterRoot;

    private readonly List<int> rewardEventIds = new List<int>() { 1, 2, 3, 4 };
    private ActivityInfo activityInfo;
    private bool isSending = false;
    private List<RewardItem> _rewardItems = new List<RewardItem>();

    private string spriteatlasPath = "Assets/Loadable/UI/ActivityCenterPanel/FirstChargeOneYuan/FirstChargeOneYuan.spriteatlas";
    
    internal CharacterWrap characterWrapper;
    internal PlayerAnimationCtrl animationCtrl;
    internal PlayerAnimationCtrl otherAnimationCtrl;
    private List<string> pgcIds = new List<string>() { "40200261","11300161", "10400237", "10900252" };
    // private List<string> emotePgcIds = new List<string>() { "40200261" };
    private bool isInit = false;
    private bool isInitAnimation = false;
    internal CharacterData saveCharacterData;



    private void Start()
    {   
        //上报埋点
        LoadEvent.ReportPopupStatus(RechargeId.FirstChargeOneYuan.ToString());

        saveCharacterData = AccountDataManager.Inst.UserInfo.avatarInfo;
        if (saveCharacterData == null)
            saveCharacterData = AvatarDataManager.Inst.GetDefaultDataByGender(1);
        GenerateContent();

        GetDataByHttp();

        oneYuanBtn.onClick.AddListener(() =>
        {
            if (UIManager.Inst.TryFindPanel<RechargePanel>(PanelId.RechargePanel, out var panel))
            {
                panel.OnTabClick(RechargeId.LimitedRechargeGiftPack);
            }
        });

        otherAmountBtn.onClick.AddListener(() =>
        {
            if (UIManager.Inst.TryFindPanel<RechargePanel>(PanelId.RechargePanel, out var panel))
            {
                panel.OnTabClick(RechargeId.GemPack);
            }
        });

        InitAvatar();
        Invoke("Delay", 0.1f);
        isInitAnimation = true;
        
        MessageHelper.AddListener(MessageName.ResumeActivityEmote, ResumeActivityEmote);
        
    }

    private void ResumeActivityEmote()
    {
        if (isInitAnimation && this.gameObject.activeSelf)
        {
            Invoke("Delay", 0.1f);
        }
    }
    
    internal void CancelTryOn()
    {
        characterWrapper.RefreshAvatar(saveCharacterData);
    }
    
    private void OnEnable()
    {
        if (isInitAnimation)
        {
            Invoke("Delay", 0.1f);
        }
    }
    
    private void GetDataByHttp(bool isClaimSuccess = false)
    {
        ActivityCenterInfoReq req = new ActivityCenterInfoReq();
        req.idList = new List<string>
        {
            ActivityId.FirstChargeOneYuan.ToString()
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ActivityList, HttpMethod.POST,
            JsonConvert.SerializeObject(req), (content) =>
            {
                ActivityResponse activityResponse = JsonConvert.DeserializeObject<ActivityResponse>(content);
                if (activityResponse.list != null)
                {
                    OnGetActivityListSuccess(activityResponse.list, isClaimSuccess);
                }
            },
            (error) =>
            {
            });
    }
    
    private void OnGetActivityListSuccess(List<ActivityInfo> activityList, bool isClaimSuccess)
    {
        var activityInfo = activityList.Find(x => x.activityId == ActivityId.FirstChargeOneYuan.ToString());
        if (activityInfo == null)
        {
            return;
        }
        
        this.activityInfo = activityInfo;
        SetupUI(activityInfo);

        if (isClaimSuccess)
        {
            if (activityInfo.activityStatus != 2) return;
            EventCenterDataManager.Inst.GetActivityInfo();
        }
    }
    
    private void OnDestroy()
    {
        animationCtrl?.ResetEmoteForUICharacter();
        otherAnimationCtrl?.gameObject.SetActive(false);
        otherAnimationCtrl?.ResetEmoteForUICharacter();
        
        MessageHelper.RemoveListener(MessageName.ResumeActivityEmote, ResumeActivityEmote);

    }

    private void SetupUI(ActivityInfo activityInfo)
    {
        for (int i = 0; i < rewardEventIds.Count; i++)
        {
            var eventInfo = activityInfo.eventList.FirstOrDefault(tmp => rewardEventIds[i] == tmp.eventId);
            if (eventInfo == null)
            {
                continue;
            }

            firstChargeOneYuanRewardItems[i].SetProgress(eventInfo.finishAmount);
            firstChargeOneYuanRewardItems[i].Init(eventInfo, OnClaimCallBack);
        }
        
        UpdateCurrency(activityInfo.currencyAmount);
    }

    private void InitAvatar()
    {
        
        var saveCharacterData = AccountDataManager.Inst.UserInfo.avatarInfo;
        if (saveCharacterData == null)
            saveCharacterData = AvatarDataManager.Inst.GetDefaultDataByGender(1);
        if (saveCharacterData != null)
        {
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
    
    private void WearPgcClothes(List<string> pgcIds)
    {
        CancelTryOn();
        if (pgcIds == null)
        {
            return;
        }

        for (int i = 0; i < pgcIds.Count; i++)
        {
            var pgcId = pgcIds[i];
            var config = DataTables.GetGameResData(pgcId);
            if (config == null) continue;
            switch ((ResourceType)config.ResourceType)
            {
                case ResourceType.Avatar:
                    TryOn(pgcId);
                    break;
                case ResourceType.Emote:
                    PreviewEmote(pgcId, (EmoteSubType)config.SubType);
                    break;
  
            }
        }
    }
    
    private void Delay()
    {
        
        WearPgcClothes(pgcIds);
    }

    private void TryOn(string pgcId)
    {
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
    
    internal void PreviewEmote(string pgcId, EmoteSubType emoteSubType)
    {
        avatarCameraController?.SetEmoteView(pgcId);
        animationCtrl?.ResetEmoteForUICharacter();
        otherAnimationCtrl?.gameObject.SetActive(false);
        otherAnimationCtrl?.ResetEmoteForUICharacter();
        switch (emoteSubType)
        {
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
    
    private void GenerateContent()
    {
        if (isInit)
        {
            return;
        }

        isInit = true;
        string jsonPath =
            "Assets/Loadable/UI/ActivityCenterPanel/FirstChargeOneYuan/FirstChargeOneYuanData.json";
        var ugcAsset =
            Loader.Load<TextAsset>(
                jsonPath, this.gameObject);
        _rewardItems = JsonConvert.DeserializeObject<List<RewardItem>>(ugcAsset.text);

        for (var i = 0; i < _rewardItems.Count; i++)
        {
            var taskItem = Instantiate(rewardItemPrefab, taskContent);
            taskItem.transform.localScale = Vector3.one;
            taskItem.gameObject.SetActive(true);
            taskItem.SetAtlasPath(spriteatlasPath);
            taskItem.OnInitCreate(_rewardItems[i]);

            firstChargeOneYuanRewardItems.Add(taskItem);
        }
    }

    private void OnClaimCallBack(ActivityEventInfo eventInfo, RewardItem rewardItem)
    {
        if (eventInfo.eventStatus != (int)ClaimStatus.Unlocked)
        {
            int rewardType = rewardItem.rewardType;
            if (rewardType == (int)BUDRewardType.RewardPgcResource || rewardType== (int)BUDRewardType.RewardVipFreeTrail)
            {
                List<string> pgcIds = rewardItem.pgcIds;
                if (!pgcIds.IsNullOrEmpty())
                {
                    CancelEmote();
                    var panel = UIManager.Inst.OpenPanel<RewardPreviewPanel>(PanelId.RewardPreviewPanel);
                    panel.SetEventPreview(pgcIds, rewardItem.rewardName1, "firstcharge_oneyuan", spriteatlasPath,"充值任意金额送仙女棒动作+水手服套装","#3586FF", bgImage.texture);
                }
                return;
            }
            CurrencyType currencyType = GameUtils.ConvertRewardType(rewardType);
            UIManager.Inst.OpenPanel(PanelId.CurrencyTipsPanel, currencyType);
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
                ActivityEventClaimResponse activityEventClaimResponse =
                    JsonConvert.DeserializeObject<ActivityEventClaimResponse>(content);
                isSending = false;
                OnClaimSuccess(activityEventClaimResponse, rewardItem);
            },
            (error) => { isSending = false; });
    }
    
    internal void CancelEmote()
    {
        avatarCameraController.ResetEmoteView();
        animationCtrl.ResetEmoteForUICharacter();
        otherAnimationCtrl.gameObject.SetActive(false);
        otherAnimationCtrl.ResetEmoteForUICharacter();
    }

    private void OnClaimSuccess(ActivityEventClaimResponse response, RewardItem rewardItem)
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
        AccountDataManager.Inst.BalanceInfo.Refresh();
        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
        panel.ShowRewards(new List<CommonRewardItemData>()
        {
            new CommonRewardItemData()
            {
                IconSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, rewardItem.rewardIcon1,
                    gameObject),
                RewardAmount = rewardItem.num,
                rewardName = rewardItem.rewardName1
            }
        });


        GetDataByHttp(true);
        ReddotManagerUtils.Inst.RefreshRedDot();
    }

    

    private void UpdateCurrency(int amount)
    {
        bool hasAmount = amount >= 1;
        unlockBtn.gameObject.SetActive(hasAmount);
        oneYuanBtn.gameObject.SetActive(!hasAmount);
        otherAmountBtn.gameObject.SetActive(!hasAmount);
    }
}