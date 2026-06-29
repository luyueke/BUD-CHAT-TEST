using System.Collections.Generic;
using System.Linq;
using Es;
using Game.Avatar;
using Game.Store;
using GameData;
using GameData.PgcData;
using UI.BaseWidgets;
using UI.Manager;
using UI.UIPanels.GashaponPanel;
using UI.UIPanels.RechargePanel;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class HeavenlyMatchView : NewDefaultGashaponView {
    [SerializeField] private CButton jumpBtn1;
    [SerializeField] private CButton jumpBtn2;
    private Dictionary<int, CommonRewardItem> rewardItems;

    [SerializeField] private Transform leftCharacterRoot;

    [SerializeField] private AvatarCameraController leftAvatarCameraController;
    private PlayerAnimationCtrl leftAnimationCtrl;

    [SerializeField] private Transform rightCharacterRoot;

    [SerializeField] private AvatarCameraController rightAvatarCameraController;
    private PlayerAnimationCtrl rightAnimationCtrl;

    protected override void InitBG()
    {

    }



    private void InitAvatar()
    {
        var leftSaveCharacterData = AccountDataManager.Inst.UserInfo.avatarInfo.Clone();
        var leftCharacterWrapper = AvatarController.Inst.CreateUIAvatarWithIKController(leftSaveCharacterData, leftCharacterRoot);
        var leftRewardData =  GashaponDataManager.Inst.GetBundleRewardDataById(gashaponData.RewardList, "58");
        foreach (var assetsData in leftRewardData.PgcDatas)
        {
            string pgcId = assetsData.Id;
            var pgcConfig = PgcUtils.GetPgcConfigData(pgcId);
            if (pgcConfig == null)
            {
                continue;
            }

            if (pgcConfig.ResourceType != (int)ResourceType.Avatar)
            {
                continue;
            }

            var config = DataTables.GetAvatarCommonData(pgcId);
            var classType = UniqueType.GetAvatar(pgcId);
            leftCharacterWrapper.ChangePart(UniqueType.GetAvatar((AvatarSubType)pgcConfig.SubType), pgcId);
            leftCharacterWrapper.ChangeColor(classType, config.defaultColor);
            leftCharacterWrapper.Move(classType, config.pDef);
            leftCharacterWrapper.Rotate(classType, config.rDef);
            leftCharacterWrapper.Scale(classType, config.sDef);
            leftCharacterWrapper.HVScale(classType, config.vhSDef);
            leftCharacterWrapper.SetLeftOrRight(classType, config.leftRightType);
        }

        leftCharacterWrapper.SetParent(leftCharacterRoot, true);
        leftAnimationCtrl = leftCharacterWrapper.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
        leftAnimationCtrl.CheckAndOverrideSpecialAnim();
        leftAnimationCtrl.PlaySpecialAnimForUICharacter(SpecialAnim.Idle);
        leftAvatarCameraController.RotateTarget = leftCharacterRoot;
        leftAvatarCameraController.isMoveEnabled = false;
        leftAvatarCameraController.isZoomEnabled = false;

        var rightSaveCharacterData = AccountDataManager.Inst.UserInfo.avatarInfo.Clone();
        var rightCharacterWrapper = AvatarController.Inst.CreateUIAvatarWithIKController(rightSaveCharacterData, rightCharacterRoot);
        var rightRewardData =  GashaponDataManager.Inst.GetBundleRewardDataById(gashaponData.RewardList, "57");
        foreach (var assetsData in rightRewardData.PgcDatas)
        {
            string pgcId = assetsData.Id;

            var pgcConfig = PgcUtils.GetPgcConfigData(pgcId);
            if (pgcConfig == null)
            {
                continue;
            }

            if (pgcConfig.ResourceType != (int)ResourceType.Avatar)
            {
                continue;
            }

            var config = DataTables.GetAvatarCommonData(pgcId);
            var classType = UniqueType.GetAvatar(pgcId);
            rightCharacterWrapper.ChangePart(UniqueType.GetAvatar((AvatarSubType)pgcConfig.SubType), pgcId);
            rightCharacterWrapper.ChangeColor(classType, config.defaultColor);
            rightCharacterWrapper.Move(classType, config.pDef);
            rightCharacterWrapper.Rotate(classType, config.rDef);
            rightCharacterWrapper.Scale(classType, config.sDef);
            rightCharacterWrapper.HVScale(classType, config.vhSDef);
            rightCharacterWrapper.SetLeftOrRight(classType, config.leftRightType);
        }

        rightCharacterWrapper.SetParent(rightCharacterRoot, true);
        rightAnimationCtrl = rightCharacterWrapper.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
        rightAnimationCtrl.PlaySpecialAnimForUICharacter(SpecialAnim.Idle);
        rightAnimationCtrl.CheckAndOverrideSpecialAnim();
        rightAvatarCameraController.RotateTarget = rightCharacterRoot;
        rightAvatarCameraController.isMoveEnabled = false;
        rightAvatarCameraController.isZoomEnabled = false;


    }


    public override void OnCreate(string id)
    {
        base.OnCreate(id);
        InitAvatar();
        var items = GetComponentsInChildren<CommonRewardItem>(true);
        rewardItems = new Dictionary<int, CommonRewardItem>();
        for (int i = 0; i < items.Length; i++)
        {
            items[i].Init(i + 1, OnClaimRewardCallBack);
            rewardItems.Add(i + 1, items[i]);
        }

        jumpBtn1.gameObject.SetActive(BusinessLiveManager.Inst.IsActivityLive(((int)ActivityId.NewYearsTurntable2026).ToString()));
        jumpBtn1.onClick.AddListener(()=>
        {
            UIManager.Inst.OpenPanel(PanelId.ActivityCenterPanel, ActivityId.NewYearsTurntable2026.ToString());
        });

        jumpBtn2.gameObject.SetActive(IAPDataManager.Inst.IsNewYearLimitedPackageLive());
        jumpBtn2.onClick.AddListener(() =>
        {
            UIManager.Inst.OpenPanel(PanelId.RechargePanel, (int)RechargeId.LimitedRechargeGiftPack);
        });

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
                rewardView.Init(gashaponData, new List<string>() { "57", "58" }, null);
            }
        }
    }
}
