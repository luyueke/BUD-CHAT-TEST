using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Es;
using Game.Avatar;
using Game.Store;
using GameData.PgcData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.Base;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class HalloweenRewardPanel : BasePanel<HalloweenRewardPanel>
{
    [SerializeField] private Transform BG;
    [SerializeField] private Button BackBtn;
    [SerializeField] private Transform Content;
    [SerializeField] private Text RewardName;

    [Header("人物形象")] [SerializeField] internal Transform characterRoot;
    [SerializeField] internal AvatarCameraController avatarCameraController;
    [SerializeField] internal GameObject AvatarRootObj;
    [SerializeField] internal Image CurrencyObj;
    
    [SerializeField] private Text EndTimeText;

    [SerializeField] private CButton wearBtn;
    [SerializeField] private LoadingButton payBtn;
    [SerializeField] private CButton tryOnBtn;

    [SerializeField] private EventCurrencyView currencyView;
    [SerializeField] private HalloweenRewardItemView _item;
    private List<HalloweenRewardItemView> itemViews = new List<HalloweenRewardItemView>();

    internal CharacterWrap characterWrap;
    internal PlayerAnimationCtrl animationCtrl;
    internal PlayerAnimationCtrl otherAnimationCtrl;

    internal CharacterData saveCharacterData;

    private AmbientLightSetting _srcLightSetting;
    private bool _srcHallLightVisible;
    private bool _srcGameSceneLightVisible;
    private bool _srcPreviewSceneLightVisible;

    private string pgcId;

    private static int allConsume;

    private Action<int> balanceChange;

    public override void OnCreate()
    {
        BackBtn.onClick.AddListener(OnBack);

        currencyView.balanceChangeAction = i => { balanceChange?.Invoke(i); };

        tryOnBtn?.onClick.AddListener(OnClickTryPlay);
        payBtn?.onClick.AddListener(OnClickPayItem);
        wearBtn?.onClick.AddListener(OnClickWear);
    }

    private void OnBack()
    {
        CloseSelf();
        AccountDataManager.Inst.BalanceInfo.Refresh();
    }

    public override void OnShow(params object[] args)
    {
        InitUI();
        _srcPreviewSceneLightVisible = AmbientLightManager.Inst.ShowPreviewDirLight();
        _srcLightSetting = AmbientLightManager.Inst.OpenUILight();
        _srcHallLightVisible = AmbientLightManager.Inst.HideHallLight();
        _srcGameSceneLightVisible = AmbientLightManager.Inst.HideGameSceneLight();

        InitRewardView();
        AccountDataManager.Inst.BalanceInfo.Refresh();
    }

    private void InitUI()
    {
        if (BG == null)
        {
            return;
        }

        string atlasPath = "Assets/Loadable/UI/UIPanel/ActivityCenterPanel/ActivityCenterPanel.spriteatlas";
        var itemObj = Loader
            .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
            .Instantiate(BG);
        var item = itemObj.GetComponent<ActivityCenterBgItem>();
        item.InitCustomBgItem("#463F40", atlasPath, new List<string>()
        {
            "Halloween_1",
            "Halloween_2",
            "Halloween_3"
        });
        item.gameObject.SetActive(true);
    }

    private void InitRewardView()
    {
        saveCharacterData = AccountDataManager.Inst.UserInfo.avatarInfo;
        if (saveCharacterData == null)
            saveCharacterData = AvatarDataManager.Inst.GetDefaultDataByGender(1);
        if (saveCharacterData != null)
        {
            characterWrap = AvatarController.Inst.CreateUIAvatar(saveCharacterData);
            characterWrap.SetParent(characterRoot, true);
            animationCtrl = characterWrap.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
            avatarCameraController.RotateTarget = characterRoot;

            var otherCharacterWrap = AvatarController.Inst.CreateUIAvatar(saveCharacterData);
            otherCharacterWrap.SetParent(characterRoot, true);
            otherAnimationCtrl = otherCharacterWrap.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
            otherCharacterWrap.Avatar.gameObject.SetActive(false);
        }
    }

    private ActivityInfo activityInfo;

    public void SetData(ActivityInfo activityInfo, int balanceValaue, Action<int> balanceChange)
    {
        this.activityInfo = activityInfo;
        this.balanceChange = balanceChange;
        var leftTime = activityInfo.leftTime;
        if (!string.IsNullOrEmpty(activityInfo.leftTime))
        {
            EndTimeText.text = $"距活动结束还有: {activityInfo.leftTime}";
        }

        currencyView.UpdateCurrency(balanceValaue);

        var rewardList = activityInfo.rewardList;
        if (rewardList != null)
        {
            foreach (var element in rewardList)
            {
                var item = GameObject.Instantiate(_item, Content);
                item.gameObject.SetActive(true);
                item.SetData(element, OnClickItem);
                itemViews.Add(item);
            }

            if (rewardList.Count > 0)
            {
                OnClickItem(rewardList[0]);
            }
        }
    }

    private ActivityRewardInfo activeData;

    private void OnClickItem(ActivityRewardInfo data)
    {
        activeData = data;
        foreach (var element in itemViews)
        {
            element.SetSelect(element.RewardId == data.rewardId);
        }

        var isOwned = false;
        var budRewardType = data.budRewardType;
        AvatarRootObj.SetActive(budRewardType == (int)BUDRewardType.RewardPgcResource);
        CurrencyObj.gameObject.SetActive(budRewardType == (int)BUDRewardType.RewardPinkCoin);
        var rewardName = data.rewardName;
        if (budRewardType == (int)BUDRewardType.RewardPgcResource)
        {
            var pgcId = data.pgcId;
            if (!string.IsNullOrEmpty(pgcId))
            {
                TryOn(pgcId, OnTryOnSuccess);
                isOwned = AssetsDataManager.IsOwned(pgcId);
            }
        }
        else if (budRewardType == (int)BUDRewardType.RewardPinkCoin)
        {
            isOwned = data.rewardStatus == 1;
            CancelTryOn();
            rewardName = $"{data.rewardNum} {rewardName}";
        }

        RewardName.text = rewardName;
        payBtn.gameObject.SetActive(!isOwned);
        var PriceText = GameObjectEx.FindChildByName(payBtn.gameObject, "Text").GetComponent<Text>();
        PriceText.text = data.spendNum.ToString();
    }

    private void OnTryOnSuccess()
    {
        itemViews.ForEach(x=>x.SetLoadingState(false));
    }

    private void OnClickWear()
    {
    }

    private void OnClickPayItem()
    {
        if (activeData == null)
        {
            return;
        }

        if (currencyView.balance < activeData.spendNum)
        {
            currencyView.OnExchange();
            return;
        }

        if (isSending)
        {
            return;
        }

        isSending = true;
        payBtn.ShowLoading();
        ConvertReward(activityInfo.activityId, activeData.rewardId, (b, response) =>
        {
            if (this == null)
            {
                return;
            }

            payBtn.HideLoading();
            isSending = false;

            if (b)
            {
                payBtn.gameObject.SetActive(false);
                OnConvertSuccess(response);
            }
        });
    }

    private void OnClickTryPlay()
    {
        UIManager.Inst.SwapPanel(PanelId.TryMusicalInstrumentPanel, characterWrap.ChaData.Clone());
    }

    private bool isSending = false;

    private void ConvertReward(string activityId, int rewardId,
        Action<bool, ActivityRewardConvertResponse> resultAction)
    {
        if (string.IsNullOrEmpty(activityId))
        {
            resultAction?.Invoke(false, null);
            return;
        }

        JObject jObject = new JObject()
        {
            ["activityId"] = activityId,
            ["rewardId"] = rewardId
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ActivityRedeemReward,
            HttpMethod.POST,
            JsonConvert.SerializeObject(jObject), (content) =>
            {
                ActivityRewardConvertResponse response =
                    JsonConvert.DeserializeObject<ActivityRewardConvertResponse>(content);
                isSending = false;

                resultAction?.Invoke(true, response);
            },
            (error) => { resultAction?.Invoke(false, null); });
    }

    private void OnConvertSuccess(ActivityRewardConvertResponse response)
    {
        if (response == null)
        {
            return;
        }

        currencyView.UpdateCurrency(response.currencyAmount);
        balanceChange?.Invoke(response.currencyAmount);
        var itemView = itemViews.Find(x => x.RewardId == response.rewardId);
        if (itemView != null)
        {
            itemView.SetOwnedUI();
        }

        ShowRewardPopupView(response);
    }

    private void ShowRewardPopupView(ActivityRewardConvertResponse response)
    {
        var rewardInfo = activityInfo.rewardList.Find(x => x.rewardId == response.rewardId);
        if (rewardInfo == null)
        {
            return;
        }

        var rewardList = new List<CommonRewardItemData>();
        
        var fixedReward = response.replaceReward;
        if (fixedReward == null)
        {
            var budRewardType = rewardInfo.budRewardType;
            if (budRewardType == (int)BUDRewardType.RewardPgcResource)
            {
                var pgcId = rewardInfo.pgcId;
                CommonRewardItemData commonRewardItemData = new CommonRewardItemData();
                commonRewardItemData.IconSp = PgcUtils.GetIconSpriteByPgcId(pgcId, gameObject);
                commonRewardItemData.rewardName = rewardInfo.rewardName;
                commonRewardItemData.RewardAmount = 1;
                rewardList.Add(commonRewardItemData);
            }
            else if (budRewardType == (int)BUDRewardType.RewardPinkCoin)
            {
                rewardInfo.rewardStatus = 1;
                CommonRewardItemData commonRewardItemData = new CommonRewardItemData();
                commonRewardItemData.IconSp = PgcUtils.LoadCurrencyIcon(CurrencyType.PinkCoin, gameObject);
                commonRewardItemData.rewardName = rewardInfo.rewardName;
                commonRewardItemData.RewardAmount = rewardInfo.rewardNum;
                rewardList.Add(commonRewardItemData);
            }
        }
        else
        {
            CommonRewardItemData commonRewardItemData = new CommonRewardItemData();
            commonRewardItemData.IconSp = PgcUtils.LoadRewardIcon((BUDRewardType)fixedReward.rewardType, gameObject);
            commonRewardItemData.rewardName = PgcUtils.GetRewardName((BUDRewardType)fixedReward.rewardType);
            commonRewardItemData.RewardAmount = fixedReward.amount;
            rewardList.Add(commonRewardItemData);
        }

        if (rewardList.Count == 0)
        {
            return;
        }
        AccountDataManager.Inst.BalanceInfo.Refresh();
        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
        panel.ShowRewards(rewardList);
    }

    internal void CancelTryOn()
    {
        characterWrap.RefreshAvatar(saveCharacterData);
    }

    /// <summary>
    /// 试穿 重置参数
    /// </summary>
    /// <param name="goodsData"></param>
    internal void TryOn(string pgcId, Action onTryOnSuccess)
    {
        CancelTryOn();

        avatarCameraController.ResetEmoteView();
        animationCtrl.ResetEmoteForUICharacter();
        otherAnimationCtrl.gameObject.SetActive(false);
        otherAnimationCtrl.ResetEmoteForUICharacter();

        GameResData config = Es.DataTables.GetGameResData(pgcId);
        if (config == null)
        {
            return;
        }

        switch ((ResourceType)config.ResourceType)
        {
            case ResourceType.Avatar:
                OnWearAvatar(pgcId, onTryOnSuccess);
                break;
            case ResourceType.Emote:
                PreviewEmote(pgcId, (EmoteSubType)config.SubType, onTryOnSuccess);
                break;
        }
    }

    internal void OnWearAvatar(string pgcId, Action onTryOnSuccess)
    {
        var config = DataTables.GetAvatarCommonData(pgcId);
        var classType = UniqueType.GetAvatar(pgcId);
        characterWrap.ChangePart(classType, pgcId, onTryOnSuccess);
        characterWrap.ChangeColor(classType, config.defaultColor);
        characterWrap.Move(classType, config.pDef);
        characterWrap.Rotate(classType, config.rDef);
        characterWrap.Scale(classType, config.sDef);
        characterWrap.HVScale(classType, config.vhSDef);
        characterWrap.SetLeftOrRight(classType, config.leftRightType);
    }

    internal void PreviewEmote(string pgcId, EmoteSubType emoteSubType, Action onTryOnSuccess)
    {
        animationCtrl.ResetEmoteForUICharacter();
        otherAnimationCtrl.gameObject.SetActive(false);
        otherAnimationCtrl.ResetEmoteForUICharacter();
        avatarCameraController.SetEmoteView(pgcId);
        switch (emoteSubType)
        {
            case EmoteSubType.Single:
            case EmoteSubType.SingleLoop:
                animationCtrl.PlaySingleEmoteForUICharacter(pgcId, onTryOnSuccess);
                break;
            case EmoteSubType.Double:
            case EmoteSubType.DoubleLoop:
                animationCtrl.PlayDoubleEmoteForUICharacter(pgcId, otherAnimationCtrl, onTryOnSuccess);
                break;
        }
    }
}