using Es;
using Game.Avatar;
using GameData.PgcData;
using Message;
using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UI.Manager;
using UI.UIPanels.GashaponPanel;
using UI.UIPanels.RechargePanel;
using UnityEngine;
using UnityEngine.UI;

public class Y2kArcticFeverView : NewDefaultGashaponView
{
    [SerializeField] private CButton jumpBtn1;
    [SerializeField] private CButton jumpBtn2;
    private Dictionary<int, CommonRewardItem> rewardItems;

    [SerializeField] private List<Button> rewardBtns;

    [SerializeField] private AvatarCameraController avatarCameraController2;
    [SerializeField] private GameObject modelRoot2; // 用于放置角色模型的根节点
    [HideInInspector] private PlayerAnimationCtrl animationCtrl;
    protected override void InitBG()
    {

    }

    private void InitCharacter()
    {

        List<string> pgcIds = new List<string>() {
        "10800123","12100143","10600128","10100113","11000263","10900494","11300355","10400494","11400082","11100017"
    };
        var saveCharacterData = AvatarDataManager.Inst.GetDefaultDataByGender(1);
        var characterWrapper = AvatarController.Inst.CreateUIAvatarWithIKController(saveCharacterData, modelRoot2.transform);
        avatarCameraController2.RotateTarget = modelRoot2.transform;
        foreach (var pgcId in pgcIds)
        {
            var pgcConfig = PgcUtils.GetPgcConfigData(pgcId);
            var config = DataTables.GetAvatarCommonData(pgcId);
            var classType = UniqueType.GetAvatar(pgcId);
            characterWrapper.ChangePart(UniqueType.GetAvatar((AvatarSubType)pgcConfig.SubType), pgcId);
            characterWrapper.ChangeColor(classType, config.defaultColor);
            if (pgcId == "11100017")
            {
                characterWrapper.Move(classType, new Vec3(-0.0458f, 0.2375f, 0));
                characterWrapper.Rotate(classType, new Vec3(-166f, -90f, 0));
                characterWrapper.Scale(classType, new Vec3(0.5898f, 0.5898f, 0.5898f));
            }
            else
            {
                characterWrapper.Move(classType, config.pDef);
                characterWrapper.Rotate(classType, config.rDef);
                characterWrapper.Scale(classType, config.sDef);
            }
            characterWrapper.HVScale(classType, config.vhSDef);
            characterWrapper.SetLeftOrRight(classType, config.leftRightType);
        }
        characterWrapper.Avatar?.GetComponentInChildren<CustomBodyTypeController>()?.ApplyBodyType(CustomBodyTypeController.BodyType.None);
        characterWrapper.ChangeColor(10022, "#FFDDDC");

        animationCtrl = characterWrapper.Avatar?.GetComponentInChildren<PlayerAnimationCtrl>();
        animationCtrl?.PlaySpecialAnimForUICharacter(GameData.SpecialAnim.Run);
    }

    public override void OnCreate(string id)
    {
        base.OnCreate(id);
        var items = GetComponentsInChildren<CommonRewardItem>(true);
        rewardItems = new Dictionary<int, CommonRewardItem>();
        for (int i = 0; i < items.Length; i++)
        {
            items[i].Init(i + 1, OnClaimRewardCallBack);
            rewardItems.Add(i + 1, items[i]);
        }

        foreach (var item in rewardBtns) {
            item.onClick.AddListener(OnPriviewBtnClick);
        }

        jumpBtn1.gameObject.SetActive(false);
        jumpBtn2.gameObject.SetActive(false);
       // jumpBtn1.gameObject.SetActive(BusinessLiveManager.Inst.IsActivityLive(((int)ActivityId.NewYearsTurntable2026).ToString()));
        //jumpBtn1.onClick.AddListener(() =>
        //{
        //    UIManager.Inst.OpenPanel(PanelId.ActivityCenterPanel, ActivityId.NewYearsTurntable2026.ToString());
        //});

        //jumpBtn2.gameObject.SetActive(IAPDataManager.Inst.IsNewYearLimitedPackageLive());
        //jumpBtn2.onClick.AddListener(() =>
        //{
        //    UIManager.Inst.OpenPanel(PanelId.RechargePanel, (int)RechargeId.SpringLimited);
        //});

        bgPath = GashaponDataManager.Inst.GetGashaponView(id).BgPath;
        exchageCurrency = CurrencyType.CollectionTicket;

        InitCharacter();
    }

    protected override void UpdateWidgetView()
    {

    }

    private void OnClaimRewardCallBack(CommonRewardItem rewardItem)
    {
        GashaponDataManager.Inst.RequestClaimTaskReward(gashaponData.Id, rewardItem.itemId, OnClaimRewardSuccess);
    }

    private void OnClaimRewardSuccess(GashaponTaskRewardRsp rsp)
    {
        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
        List<CommonRewardItemData> rewardItemDatas = new List<CommonRewardItemData>();
        foreach (var taskInfo in rsp.taskList)
        {
            if (rewardItems.TryGetValue(taskInfo.eventId, out var taskItem))
            {
                taskItem.SetStatus((ClaimStatus)taskInfo.rewardStatus);
            }
        }

        foreach (var rewardData in rsp.rewardList)
        {
            var rewardItemData = new CommonRewardItemData
            {
                rewardName = PgcUtils.GetRewardName((BUDRewardType)rewardData.rewardType),
                RewardAmount = rewardData.rewardNum,
                rewardType = rewardData.rewardType,
            };
            rewardItemDatas.Add(rewardItemData);
        }

        panel.ShowRewards(rewardItemDatas, true);
        MessageHelper.Broadcast(MessageName.ReddotNotice);
        AccountDataManager.Inst.BalanceInfo.Refresh();
        GashaponDataManager.Inst.RequestGashaponInfo(gashaponId, OnGashaponInfoUpdate);
    }

    public override void OnShow()
    {
        base.OnShow();
        foreach (var item in rewardItems)
        {
            item.Value.SetStatus(ClaimStatus.Lock);
        }
    }

    protected override void OnGashaponInfoUpdate(GameData.Gashapon.GashaponInfoRsp infoRsp)
    {
        base.OnGashaponInfoUpdate(infoRsp);

        if (infoRsp.singleDrawDiscountedPrice == infoRsp.singleDrawPrice)
        {
            GameObjectEx.FindChildByName(twistBtn.transform, "TagDiscount").gameObject.SetActive(false);
            GameObjectEx.FindChildByName(twistBtn.transform, "Content/srcNum").gameObject.SetActive(false);
            GameObjectEx.FindComponentByName<Text>(twistBtn.transform, "Content/num").SetText(infoRsp.singleDrawDiscountedPrice.ToString());
        }
        else
        {
            GameObjectEx.FindChildByName(twistBtn.transform, "TagDiscount").gameObject.SetActive(true);
            GameObjectEx.FindChildByName(twistBtn.transform, "Content/srcNum").gameObject.SetActive(true);
            GameObjectEx.FindComponentByName<Text>(twistBtn.transform, "Content/srcNum")
                .SetText(infoRsp.singleDrawPrice.ToString());
            GameObjectEx.FindComponentByName<Text>(twistBtn.transform, "Content/num")
                .SetText(infoRsp.singleDrawDiscountedPrice.ToString());
        }

        if (infoRsp.taskList != null)
        {
            foreach (var taskInfo in infoRsp.taskList)
            {
                if (rewardItems.TryGetValue(taskInfo.eventId, out var rewardItem))
                {
                    rewardItem.SetStatus((ClaimStatus)taskInfo.rewardStatus);
                }
            }
        }

        if (infoRsp.isHavingPgcOptionalBox == 1)
        {
            var rewardView = GetComponentInChildren<SelectReward>(true);
            if (rewardView != null)
            {
                rewardView.gameObject.SetActive(true);
                rewardView.Init(gashaponData, new List<string>() { "33", "34" }, () =>
                {
                    GashaponDataManager.Inst.RequestGashaponInfo(gashaponId, OnGashaponInfoUpdate);
                });
            }
        }


        int curNum = infoRsp.luckyProgressInfo?.start ?? 0;
        if (curNum == 0)
        {
            GameObjectEx.FindComponentByName<Text>(twist10Btn.transform, "TagDiscount/Text (Legacy)").SetLocalText("首次五折");
        }
        else
        {
            GameObjectEx.FindComponentByName<Text>(twist10Btn.transform, "TagDiscount/Text (Legacy)").SetLocalText("九折优惠");
        }
    }
}
