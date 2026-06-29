using System.Collections.Generic;
using System.Linq;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Network;
using Network.Http;
using Basic.Utils;
using EventTracking;
using System;
using Game.Event;
using UI.Base;
/// <summary>
/// 酷夏尊享 礼包
/// </summary>
public class AnniversaryCelePackSubView3 : PackBaseView
{
    private PaidPackageListItem _paidPackageListItem;


    [Header("UI相关")]

    public CButton Btn_buy;

    public AnniversaryCelePackSubView3Item[] items;
    public AnniversaryCelePackView parentView;
    string productName = "酷夏尊享";

    public void Awake()
    {
        InitConfig(PaidPackageType.CoolSummerEnjoyment);
        Btn_buy.onClick.AddListener(OnBuyBtnClick);
        this._paidPackageListItem = AnniversaryCelePackMgr.Inst.PaidPackage2ItemDict[PaidPackageType.CoolSummerEnjoyment];
        if (this._paidPackageListItem == null)
        {
            AnniversaryCelePackMgr.Inst.GetProductInfo(PaidPackageType.CoolSummerEnjoyment, OnServerDataUpdate);
        }
        else
        {
            OnServerDataUpdate(this._paidPackageListItem);
        }
        RefreshUI();
    }
    void OnEnable()
    {
        RefreshUI();
    }
    private void InitConfig(PaidPackageType paidPackageType)
    {
        PaidPackageType = paidPackageType;
    }
    public override void OnServerDataUpdate(PaidPackageListItem packageListItem)
    {
        this._paidPackageListItem = packageListItem;
        RefreshUI();
        // buyNode.gameObject.SetActive(packageListItem.isPaid != 1);
        // taskEndTips.gameObject.SetActive(packageListItem.isPaid == 1);
        // endTime.gameObject.SetActive(packageListItem.isPaid != 1);
        // endTime.SetLocalText("距售卖时间结束还有: {0}",packageListItem.endDate);
    }

    private void OnBuyBtnClick()
    {
        if (_paidPackageListItem == null)
        {
            return;
        }

        if (IAPDataManager.Inst.IsOfficialChannel())
        {
            ProductInfo productInfo = _paidPackageListItem.productInfo;
            ConfirmPaymentPanel panel =
                UIManager.Inst.OpenPanel<ConfirmPaymentPanel>(PanelId.ConfirmPaymentPanel, productInfo.price);
            panel.SetCallback(paymentType => { Purchase(paymentType, productInfo, productName); });
            return;
        }

        Purchase(ConfirmPaymentPanel.PaymentType.Default, _paidPackageListItem.productInfo, productName);
    }

    protected override void OnBuySuccess(string orderId)
    {
        ShowPackReward();
        // buyNode.gameObject.SetActive(false);
        // taskEndTips.gameObject.SetActive(true);
        _paidPackageListItem.isPaid = 1;
        RefreshUI();
        ReddotManagerUtils.Inst.RefreshRedDot();
        AnniversaryCelePackMgr.Inst.GetTaskData(AnniversaryCelePackMgr.TaskId_3);
    }

    protected void ShowPackReward()
    {
        // Message.MessageHelper.Broadcast(Message.MessageName.AvaterDatabaseCheck);
        // var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
        // panel.ShowRewards(new List<CommonRewardItemData>()
        // {
        //     new CommonRewardItemData()
        //     {
        //         // IconSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, rewardIconName, gameObject),
        //         RewardAmount = 1,
        //         rewardName = packViewConfig.productName
        //     }
        // });
    }




    public void RefreshUI()
    {
        if (_paidPackageListItem == null)
        {
            Btn_buy.gameObject.SetActive(false);
            for (int i = 0; i < items.Length; i++)
            {
                items[i].RefreshUI(null,i+1);
            }
            LoggerUtils.Log("AnniversaryCelePackSubView1 RefreshUI _paidPackageListItem is null");
            return;
        }
        Btn_buy.gameObject.SetActive(_paidPackageListItem.isPaid != 1); //未购买
        var taskInfo = AnniversaryCelePackMgr.Inst.TaskId2InfoDict[AnniversaryCelePackMgr.TaskId_3];
        if (taskInfo == null)
        {
            return;
        }
        for (int i = 0; i < items.Length; i++)
        {
            items[i].RefreshUI(taskInfo.eventList[i],i+1);
        }
    }
}
