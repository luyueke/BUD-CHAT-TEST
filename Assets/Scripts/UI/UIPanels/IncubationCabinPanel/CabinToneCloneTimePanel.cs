using System.Collections.Generic;
using Sirenix.OdinInspector;
using UI.Base;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.IncubationCabin
{
    /// <summary>
    /// 声音克隆次数购买弹窗
    /// - 右上角显示当前剩余克隆次数
    /// - 购买月卡：跳转到 AI 能量商城
    /// - 购买克隆次数：1次 / 3次，走 IAP 流程（由 AICreditPurchaseHelper 处理）
    /// </summary>
    public class CabinToneCloneTimePanel : BasePanel<CabinToneCloneTimePanel>
    {
        public Text txt_leftCnt;   // 当前剩余克隆次数，如 "2次"
        public Button buyBtn;        // 去购买月卡 → 跳转能量商城
        public Button buy3TimeBtn;   // 购买 3 次克隆
        public Button buy1TimeBtn;   // 购买 1 次克隆

        public Button closeBtn;

        private AICreditPurchaseHelper _purchaseHelper;
        public Sprite toneCloneSprite;

        public override void OnCreate()
        {
            base.OnCreate();
            _purchaseHelper = new AICreditPurchaseHelper(this);
            closeBtn.onClick.AddListener(CloseSelf);
            buyBtn.onClick.AddListener(OnBuyBtnClick);
            buy1TimeBtn.onClick.AddListener(() =>
                _purchaseHelper.BuyIAP(AICreditPurchaseHelper.Clone1GemInfo, () => OnClonePurchaseSuccess(1)));
            buy3TimeBtn.onClick.AddListener(() =>
                _purchaseHelper.BuyIAP(AICreditPurchaseHelper.Clone3GemInfo, () => OnClonePurchaseSuccess(3)));
        }

        public override void OnShow(params object[] args)
        {
            base.OnShow(args);
            RefreshCloneCount();
        }

        private void RefreshCloneCount()
        {
            int cloneAmount = AccountDataManager.Inst.BalanceInfo.AiCredit.cloneAmount;
            if (txt_leftCnt != null)
                txt_leftCnt.text = $"{cloneAmount}次";
        }

        private void OnBuyBtnClick()
        {
            CloseSelf();
            UIManager.Inst.OpenPanel(PanelId.AICreditShopPanel);
        }

        private void OnClonePurchaseSuccess(int count)
        {
            RefreshCloneCount();
            var rewardPanel = UIManager.Inst.OpenPanel<CommonBoxRewardPanel>(PanelId.CommonBoxRewardPanel);
            rewardPanel.ShowReward(new List<BoxRewardItemData>
            {
                new()
                {
                    itemType   = BoxRewardItemData.ItemType.Common,
                    commonData = new CommonRewardItemData
                    {
                        IconSp        = toneCloneSprite,
                        rewardSpecial = $"×{count}",
                        rewardName    = "声音克隆次数",
                    }
                }
            });
        }

        [Button("展示奖励")]
        void testOnClonePurchaseSuccess()
        {
            OnClonePurchaseSuccess(3);
        }

    }
}
