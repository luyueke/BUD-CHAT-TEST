using System;
using System.Collections.Generic;
using Game.Avatar;
using Game.Database;
using GameData;
using GameData.UGCData;
using Message;
using Newtonsoft.Json;
using UI.Base;
using UI.BaseWidgets;
using UI.UIPanels.IncubationCabin;
using UnityEngine;
using UnityEngine.UI;
using xasset;

public class BuyBoxPartnerItemsPanel : BasePanel<BuyBoxPartnerItemsPanel>
{
    [SerializeField] private CButton CloseBtn;
    [SerializeField] private CButton BuyBtn;
    [SerializeField] private Text PriceTxt;
    [SerializeField] private Transform BoxModelRoot;
    [SerializeField] private AvatarCameraController AvatarCameraController;
    // 盒子 3D 预览组件，统一负责盒子模型生成、纹理加载与资源释放（放置在 BoxModelRoot 节点下）
    [SerializeField] private BudBoxModel BudBoxModel;

    private CharacterBoxInfo _data;
    private Action _onSuccess;
    private Action _onClose;

    public override void OnCreate()
    {
        base.OnCreate();
        CloseBtn.onClick.AddListener(CloseSelf);
        BuyBtn.onClick.AddListener(OnBuyBtnClick);
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        _data = args?.Length > 0 ? args[0] as CharacterBoxInfo : null;
        if (_data == null)
        {
            Debug.LogError("BuyBoxPartnerItemsPanel: 盒子数据为空");
            return;
        }
        _onSuccess = args?.Length > 1 ? args[1] as Action : null;
        _onClose  = args?.Length > 2 ? args[2] as Action : null;

        PriceTxt.text = _data.paymentInfo?.price.ToString() ?? "0";

        // 旋转目标仍指向 BoxModelRoot（BudBoxModel 节点置于其下），交由 BudBoxModel 加载盒子模型
        AvatarCameraController.RotateTarget = BoxModelRoot;

        if (BudBoxModel != null)
        {
            BudBoxModel.LoadBoxScene(_data.metaDataUrl);
        }
    }

    public override void OnHidden()
    {
        base.OnHidden();
        _onClose?.Invoke();
        _onClose = null;

        // 释放盒子模型及其动态纹理，防止内存泄漏
        if (BudBoxModel != null)
        {
            BudBoxModel.Clear();
        }
    }

    private void OnBuyBtnClick()
    {
        if (_data == null) return;

        if (!HasEnoughBalance(out int needNum))
        {
            OpenExchangePanel(needNum);
            return;
        }

        AIPartnerShopRequestCtrl.Inst.RequestBuyPartner(_data.id,
            () =>
            {
                AccountDataManager.Inst.BalanceInfo.Refresh();
                // 广播购买成功消息，通知 CabinControllScenePanel 刷新已拥有的盒子场景列表
                MessageHelper.Broadcast(MessageName.OnBuyUgcItemSuccess, _data.id);
                CloseSelf();
                var rewards = new List<BoxRewardItemData>
                {
                    new()
                    {
                        itemType = BoxRewardItemData.ItemType.Common,
                        commonData = new CommonRewardItemData
                        {
                            UgcCover = _data.cover,
                            rewardName = _data.name,
                            RewardAmount = 1
                        }
                    }
                };
                UIManager.Inst.OpenPanel<CommonBoxRewardPanel>(PanelId.CommonBoxRewardPanel)
                    .ShowReward(rewards, OnRewardPanelClose);
            },
            err => Debug.LogError("购买盒子失败: " + err));
    }

    private bool HasEnoughBalance(out int needNum)
    {
        int price = _data.paymentInfo?.price ?? 0;
        var currencyType = _data.paymentInfo?.currencyType ?? CurrencyType.None;
        var balance = AccountDataManager.Inst.BalanceInfo.GetAccountCount(currencyType);
        needNum = Mathf.Max(0, price - balance);
        return needNum == 0;
    }

    private void OnRewardPanelClose()
    {
        if (!string.IsNullOrEmpty(CabinBoxManager.Inst.GetCurrentDeviceId()))
            UIManager.Inst.OpenPanel(PanelId.BuyTipsPanel);
        else
            _onSuccess?.Invoke();
    }

    private void OpenExchangePanel(int needNum)
    {
        var currencyType = _data.paymentInfo?.currencyType ?? CurrencyType.None;
        switch (currencyType)
        {
            case CurrencyType.Coin:
                UIManager.Inst.OpenPanel<ExchangeCoinPanel>(PanelId.ExchangeCoinPanel)
                    .SetData(CurrencyType.Coin, CurrencyType.Gem, needNum);
                break;
            case CurrencyType.Badge:
                UIManager.Inst.OpenPanel<ExchangeCoinPanel>(PanelId.ExchangeCoinPanel)
                    .SetData(CurrencyType.Badge, CurrencyType.Gem, needNum);
                break;
            case CurrencyType.PinkCoin:
                if (ExchangeCoinPanel.JudgePinkCoin(needNum))
                    UIManager.Inst.OpenPanel<ExchangeCoinPanel>(PanelId.ExchangeCoinPanel)
                        .SetData(CurrencyType.PinkCoin, CurrencyType.Gem, needNum);
                break;
            case CurrencyType.Gem:
                UIManager.Inst.OpenPanel(PanelId.GetMoreGemsPanel, needNum);
                break;
            default:
                UIManager.Inst.OpenPanel<ExchangeCoinPanel>(PanelId.ExchangeCoinPanel)
                    .SetData(currencyType, CurrencyType.Gem, needNum);
                break;
        }
    }
}
