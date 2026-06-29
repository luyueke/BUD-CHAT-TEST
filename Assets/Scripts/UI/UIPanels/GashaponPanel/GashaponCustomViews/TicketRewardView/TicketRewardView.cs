using Message;
using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UI.Manager;
using UI.UIPanels.GashaponPanel;
using UI.UIPanels.RechargePanel;
using UnityEngine;
using UnityEngine.UI;

public class TicketRewardView : NewDefaultGashaponView
{
    [SerializeField] private CButton jumpBtn1;
    [SerializeField] private CButton jumpBtn2;
    private Dictionary<int, CommonRewardItem> rewardItems;
    protected override void InitBG()
    {

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

        jumpBtn1.gameObject.SetActive(BusinessLiveManager.Inst.IsActivityLive(((int)ActivityId.NewYearsTurntable2026).ToString()));
        jumpBtn1.onClick.AddListener(()=>
        {
            UIManager.Inst.OpenPanel(PanelId.ActivityCenterPanel, ActivityId.NewYearsTurntable2026.ToString());
        });

        jumpBtn2.gameObject.SetActive(IAPDataManager.Inst.IsNewYearLimitedPackageLive());
        jumpBtn2.onClick.AddListener(() =>
        {
            UIManager.Inst.OpenPanel(PanelId.RechargePanel, (int)RechargeId.SpringLimited);
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
                rewardView.Init(gashaponData, new List<string>() { "33", "34" } ,()=> {
                    GashaponDataManager.Inst.RequestGashaponInfo(gashaponId, OnGashaponInfoUpdate);
                });
            }
        }
    }
}
