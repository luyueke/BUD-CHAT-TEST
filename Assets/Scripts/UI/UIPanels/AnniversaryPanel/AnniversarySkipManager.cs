using Game.Event;
using GameUI;
using Message;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnniversarySkipManager : GlobalInstance<AnniversarySkipManager>
{

    public void Skip(int id)
    {
        switch (id)
        {
            case 1: OcCompetitionSystem.Inst.OpenPanel(); break;
            case 2:
                MessageHelper.Broadcast(MessageName.AnniversaryPanel_TabChange, (int)(ActivityId.AnniversaryCelebrationSummer));
                break;
            case 3:
                MessageHelper.Broadcast(MessageName.AnniversaryPanel_TabChange, (int)(ActivityId.S11CelebrationStore));
                break;
            case 4:
                MessageHelper.Broadcast(MessageName.AnniversaryPanel_TabChange, (int)(ActivityId.AnniversaryCelebrationGift));
                break;
            case 5:
                UIManager.Inst.OpenPanel(PanelId.StoreMallPanel, "lottery.magicTrial.witchLuna");
                break;
            case 6:
                MessageHelper.Broadcast(MessageName.AnniversaryPanel_TabChange, (int)(ActivityId.S11GroupConsume));
                break;
            case 7:
                MessageHelper.Broadcast(MessageName.AnniversaryPanel_TabChange, (int)(ActivityId.AnniversaryCelebrationMonth));
                break;
            case 8:
                var businessConfig = BusinessLiveManager.Inst.GetBusinessConfig();
                if (businessConfig == null || businessConfig.recharge.paidPackageList == null || !businessConfig.recharge.paidPackageList.Contains("3"))
                {
                    return;
                }
                var rechargepanel = UIManager.Inst.OpenPanel<RechargePanel>(PanelId.RechargePanel);
                rechargepanel.OnTabClick(UI.UIPanels.RechargePanel.RechargeId.LiuyuanGiftPack);
                break;
            case 9:
                UIManager.Inst.OpenPanel(PanelId.StoreMallPanel, "lottery.sweetheartParty");
                break;
            case 10:
                MessageHelper.Broadcast(MessageName.AnniversaryPanel_TabChange, (int)(ActivityId.AnniversaryLuckyKoi));
                break;
            case 11:
                UIManager.Inst.OpenPanel(PanelId.ActivityCenterPanel);
                break;
            case 12:
                UIManager.Inst.OpenPanel(PanelId.StoreMallPanel, "lottery.starryFairyTale");
                break;
            case 13:
                UIManager.Inst.OpenPanel(PanelId.StoreMallPanel, "lottery.friesFun");
                break;
            case 14:
                var businessConfig1 = BusinessLiveManager.Inst.GetBusinessConfig();
                if (businessConfig1 == null || businessConfig1.recharge.paidPackageList == null || !businessConfig1.recharge.paidPackageList.Contains("4"))
                {
                    return;
                }
                var rechargepanel1 = UIManager.Inst.OpenPanel<RechargePanel>(PanelId.RechargePanel);
                rechargepanel1.OnTabClick(UI.UIPanels.RechargePanel.RechargeId.ShiyuanGiftPack);
                break;
            case 15:
                UIManager.Inst.OpenPanel(PanelId.StoreMallPanel, "lottery.joyfulCircus");
                break;
            default:
                break;
        }

    }

}
