using Game.Store;
using Message;
using System;
using System.Collections.Generic;

namespace GameUI
{
    public static class AssetsBuyUtils
    {
        //static bool CheckedTickUser(List<AssetsBuyParam> buyParams)
        //{
        //    switch (subType)
        //    {
        //        case 0:
        //        case 1008://姿势和乐谱不能用卷
        //            return false;
        //        case 1007: //动作卷
        //            if (TokenDataManager.Inst.Data.GetToken(CurrencyType.CommunityAnimationTicket) <= 0 || price > 200)
        //                return false;
        //            UIManager.Inst.OpenPanel(PanelId.ConsumptionTicketPanel, new ConsumptionTicketConfig
        //            {
        //                type = CurrencyType.CommunityAnimationTicket,
        //                pgcId = pgcid,
        //                ClaimCallBack = () =>
        //                {
        //                    var panel = UIManager.Inst.OpenPanel<BuySuccessTipPanel>(PanelId.BuySuccessTipPanel);
        //                    panel?.InitData(ugcInfo, "购买成功！");
        //                }
        //            });
        //            return true;
        //        case 24: //乐器卷
        //            if (TokenDataManager.Inst.Data.GetToken(CurrencyType.CommunityInstrumentTicket) <= 0 || price > 200)
        //                return false;
        //            UIManager.Inst.OpenPanel(PanelId.ConsumptionTicketPanel, new ConsumptionTicketConfig
        //            {
        //                type = CurrencyType.CommunityInstrumentTicket,
        //                pgcId = pgcid,
        //                ClaimCallBack = () =>
        //                {
        //                    var panel = UIManager.Inst.OpenPanel<BuySuccessTipPanel>(PanelId.BuySuccessTipPanel);
        //                    panel?.InitData(ugcInfo, "购买成功！");
        //                }
        //            });
        //            return true;
        //        default: //其余都算皮肤卷
        //            if (TokenDataManager.Inst.Data.GetToken(CurrencyType.CommunitySkinTicket) <= 0 || price > 60)
        //                return false;
        //            UIManager.Inst.OpenPanel(PanelId.ConsumptionTicketPanel, new ConsumptionTicketConfig
        //            {
        //                type = CurrencyType.CommunitySkinTicket,
        //                pgcId = pgcid,
        //                ClaimCallBack = () =>
        //                {
        //                    var panel = UIManager.Inst.OpenPanel<BuySuccessTipPanel>(PanelId.BuySuccessTipPanel);
        //                    panel?.InitData(ugcInfo, "购买成功！");
        //                }
        //            });
        //            return true;
        //    }
        //}

        public static void BuyUgcItem(List<AssetsBuyParam> buyParams,Action<bool> ac)
        {
            //if (CheckedTickUser(buyParams))
            //{
            //    return;
            //}

            // 捕获 buyUgcId 和其他参数
            AssetsDataManager.BuyUgc(buyParams, (success, reason, needNum) =>
            {
                if (!success)
                {
                    if (reason.Equals("余额不足"))
                    {
                        switch (buyParams[0].currencyType)
                        {
                            case CurrencyType.Coin:
                                ExchangeCoinPanel panel1 = UIManager.Inst.OpenPanel<ExchangeCoinPanel>(PanelId.ExchangeCoinPanel);
                                panel1.SetData(CurrencyType.Coin, CurrencyType.Gem, needNum);
                                break;
                            case CurrencyType.Badge:
                                ExchangeCoinPanel panel2 = UIManager.Inst.OpenPanel<ExchangeCoinPanel>(PanelId.ExchangeCoinPanel);
                                panel2.SetData(CurrencyType.Badge, CurrencyType.Gem, needNum);
                                break;
                            case CurrencyType.PinkCoin:
                                if (ExchangeCoinPanel.JudgePinkCoin(needNum))
                                {
                                    ExchangeCoinPanel pinkCoinPanel = UIManager.Inst.OpenPanel<ExchangeCoinPanel>(PanelId.ExchangeCoinPanel);
                                    pinkCoinPanel.SetData(CurrencyType.PinkCoin, CurrencyType.Gem, needNum);
                                }
                                //ExchangeCoinPanel panel3 = UIManager.Inst.OpenPanel<ExchangeCoinPanel>(PanelId.ExchangeCoinPanel);
                                //panel3.SetData(CurrencyType.PinkCoin, CurrencyType.Gem, needNum);
                                break;
                            case CurrencyType.Gem:
                                UIManager.Inst.OpenPanel(PanelId.GetMoreGemsPanel, needNum);
                                break;
                        }
                    }

                    var str = "";
                    foreach (var item in buyParams) 
                    {
                        str += item.ugcInfo.id;
                        str += "_";
                    }
                    LoggerUtils.Log($"购买UGC商品失败 [{str}]:" + reason);
                    ac?.Invoke(false);
                }
                else
                {
                    //var panel = UIManager.Inst.OpenPanel<BuySuccessTipPanel>(PanelId.BuySuccessTipPanel);
                    //panel?.InitData(ugcInfo, "购买成功！");
                    foreach (var item in buyParams) {
                        MessageHelper.Broadcast(MessageName.OnBuyUgcItemSuccess, item.ugcInfo.id);
                    }

                    //if (ugcInfo is AINpcInfo && LobbyInfoManager.Inst.LobbyInfo.enableNPCHalfPrice == 1)
                    //{
                    //    LobbyInfoManager.Inst.LobbyInfo.enableNPCHalfPrice = 0;
                    //}

                    //_isOwned = true;
                    //RefreshData();

                    ac?.Invoke(true);
                }
            });
        }




    }
}