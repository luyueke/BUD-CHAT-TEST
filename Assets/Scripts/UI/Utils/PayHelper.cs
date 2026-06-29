using System;
using GameData.Gashapon;
using GameData.Manager;

namespace UI.Utils
{
    public static class PayHelper
    {
        /// <summary>
        /// 进行消费，内含货币不足判断
        /// </summary>
        public static void Pay(CurrencyType targetCurrencyType, int price, Action<bool> cb)
        {
            var haveCurrencyNum = TokenDataManager.Inst.Data.GetToken(targetCurrencyType);
            if (haveCurrencyNum < price)
            {
                cb?.Invoke(false);

                // TODO:待完成各界面表现再开放
                TipPanel.ShowToast("货币不足");

                //货币不足,根据不同货币类型弹不同UI
                // switch (targetCurrencyType)
                // {
                //     case TokenType.Coin:
                //         UIManager.Inst.OpenPanel(PanelId.NotEnoughTipPanel);
                //         break;
                //     case TokenType.Badge:
                //         UIManager.Inst.OpenPanel(PanelId.GetMoreBadgesPanel);
                //         break;
                //     case TokenType.Gem:
                //         UIManager.Inst.OpenPanel(PanelId.GetMoreGemsPanel);
                //         break;
                // }
            }
            else
            {
                cb?.Invoke(true);
            }
        }
    }
}
