using UnityEngine;
using UI.UIPanels.FittingRoom;
using UI.UIPanels.GashaponPanel;
using Game.AnimationStudio;
using Newbie;
using GameUI;

public class NewbieTaskSkipManager : GlobalInstance<NewbieTaskSkipManager>
{
    
    public void HandleSkip(string rewardId,bool isV1 = false)
    {
        switch (rewardId)
        {

            //第一天
            case "1":
                var budFittingRoomPanel = UIManager.Inst.SwapPanel(PanelId.FittingRoomPanel) as FittingRoomPanel;
                budFittingRoomPanel?.JumpTo(MainTabs.Tab.BUD); //官方商城
                break;
            case "2":
                var bagFittingRoomPanel = UIManager.Inst.SwapPanel(PanelId.FittingRoomPanel) as FittingRoomPanel;
                bagFittingRoomPanel?.JumpTo(MainTabs.Tab.Bag); //商城背包
                break;
            case "3":
                UIManager.Inst.OpenPanel(PanelId.AvatarStudioMainPanel); //形象编辑
                break;
            case "4":
                var ugcFittingRoomPanel = UIManager.Inst.SwapPanel(PanelId.FittingRoomPanel) as FittingRoomPanel;
                ugcFittingRoomPanel?.JumpTo(MainTabs.Tab.Ugc); //社区商城
                break;
            case "5":
                UIManager.Inst.OpenPanel<FittingRoomPanel>(PanelId.FittingRoomPanel).JumpTo(MainTabs.Tab.Ugc , 50024);
                break;
            case "6":
                var ugcFittingRoomPanel2 = UIManager.Inst.SwapPanel(PanelId.FittingRoomPanel) as FittingRoomPanel;
                ugcFittingRoomPanel2?.JumpTo(MainTabs.Tab.Ugc , 50004); //社区商城 - 服装
                break;
            case "7":
                var Rechargepanel = UIManager.Inst.OpenPanel<RechargePanel>(PanelId.RechargePanel);//热销
                Rechargepanel.OnTabClick(UI.UIPanels.RechargePanel.RechargeId.LimitedRechargeGiftPack);
                break;
            case "8":
                if (isV1)
                {
                    UIManager.Inst.OpenPanel<NewSeasonPassPanel>(PanelId.CommunityGamesPanel); //社区地图
                } else
                {
                    var panel = UIManager.Inst.OpenPanel(PanelId.StoreMallPanel);//扭蛋
                }         
                break;
            case "9":
                UIManager.Inst.OpenPanel(PanelId.StoreMallPanel, "lottery.coin");//金币扭蛋
                break;
            case "10":
                UIManager.Inst.OpenPanel<NewSeasonPassPanel>(PanelId.CommunityGamesPanel); //社区地图
                break;
            
            //第二天
            case "11":
                UIManager.Inst.OpenPanel(PanelId.FittingRoomPanel);
                break;
            case "12":
                UIManager.Inst.OpenPanel(PanelId.IntimacySystemPanel);//好友
                break;
            case "13":
                UIManager.Inst.OpenPanel<NewSeasonPassPanel>(PanelId.NewSeasonPassPanel);
                break;
            case "15":
                UIManager.Inst.OpenPanel<NewSeasonPassPanel>(PanelId.AvatarStudioMainPanel);
                break;
            case "16":
                BootPanel.isPlaying = true;
                TimerManager.Inst.RunOnce("NoPlayBoot", 1f, () =>
                {
                    BootPanel.isPlaying = false;
                });
                UIManager.Inst.OpenPanel<AvatarStudioMainPanel>(PanelId.AvatarStudioMainPanel).OpenView(AvatarStudioViewEnum.Drafts);//形象工作室 - 草稿箱
                break;
            
            //第四天
            case "17":
                var animFittingRoomPanel = UIManager.Inst.SwapPanel(PanelId.FittingRoomPanel) as FittingRoomPanel;
                animFittingRoomPanel?.JumpTo(MainTabs.Tab.Action, 90100); 
                break;
            case "18":
                UIManager.Inst.OpenPanel<AnimationStudioMainPanel>(PanelId.AnimationStudioMainPanel).OnSelectView(StudioSubType.Drafts); //动画工作室 - 草稿箱
                break;
            case "19":
                var gameHallPanel = UIManager.Inst.FindPanel<GameHallPanel>(WindowId.GameHallWindow, PanelId.GameHallPanel);
                if (!HallCharacterManager.IsHidden) {
                    var idleAnimPanel = UIManager.Inst.OpenPanel<UI.UIPanels.LobbyCharacterIdlePanel.LobbyNpcIdlePanel>(PanelId.LobbyNpcIdlePanel);
                    idleAnimPanel.UpdateHallAnim = gameHallPanel.OnIdleChange;
                } else {
                    var idleAnimPanel = UIManager.Inst.OpenPanel<UI.UIPanels.LobbyCharacterIdlePanel.LobbyCharacterIdlePanel>(PanelId.LobbyCharacterIdlePanel);
                    idleAnimPanel.UpdateHallAnim = gameHallPanel.OnIdleChange;
                }
                break;
            
            case "21":
                UIManager.Inst.OpenPanel(PanelId.AINpcStorePanel);//NPC商城
                break;
            case "22":
                UIManager.Inst.OpenPanel<NewSeasonPassPanel>(PanelId.AINpcStudioMainPanel);//NPC工作室
                break;
            
            case "25":
                UIManager.Inst.OpenPanel<NewSeasonPassPanel>(PanelId.MusicalInstrumentStudioPanel);//音乐工作室
                break;
            
            
            //第七天
            case "26":
                //ContestEventManager.Inst.OpenContestPage();
                OcCompetitionSystem.Inst.OpenPanel();
                break;

            case "27":
                UIManager.Inst.OpenPanel(PanelId.AnimationStudioCategoryPanel);
                break;
            //第四天
            case "29":
                var budFittingRoomPanel1 = UIManager.Inst.SwapPanel(PanelId.FittingRoomPanel) as FittingRoomPanel;
                budFittingRoomPanel1?.JumpTo(MainTabs.Tab.BUD, 2); //官方商城
                break;
            case "28":
                UIManager.Inst.OpenPanel(PanelId.CreatorCenterPanel);//创作者中心
                break;
                
            case "30":
                UIManager.Inst.OpenPanel<GameStudioPanel>(PanelId.GameStudioPanel);
                break;
            case "31":
                UIManager.Inst.OpenPanel<AnimationStudioMainPanel>(PanelId.AnimationStudioMainPanel); //动画工作室
                break;
            //第四天
            case "32":
                var ugcAnimFittingRoomPanel = UIManager.Inst.SwapPanel(PanelId.FittingRoomPanel) as FittingRoomPanel;
                ugcAnimFittingRoomPanel?.JumpTo(MainTabs.Tab.Ugc, 90100);
                break;
            case "35":
                UIManager.Inst.OpenPanel(PanelId.AIHospitalMainEntryPanel);
                break;
            case "36":
                UIManager.Inst.OpenPanel(PanelId.CommunityGamesPanel, WindowId.RecommendWindow);
            //    UIManager.Inst.OpenPanel(PanelId.GameEntryMainPanel); //ai剧本杀游玩大厅
                break;
            case "37":
                GameEntrySystem.Inst.OpenParkUgcPanel(); //剧本创作
                break;
            case "38":
                GameEntrySystem.Inst.OpenParkGroupPanel(); //剧本杀广场 
                break;
            case "39":
                UIManager.Inst.OpenPanel(PanelId.StoreMallPanel , ""); //薯条扭蛋
                break;
            case "40":
                var RechargepanelInfo = UIManager.Inst.OpenPanel<RechargePanel>(PanelId.RechargePanel);//热销VIP月卡
                RechargepanelInfo.OnTabClick(UI.UIPanels.RechargePanel.RechargeId.VipMonthPack);
                break;
            case "41":
                UIManager.Inst.OpenPanel<BuyAlbumVolumePanel>(PanelId.BuyAlbumVolumePanel);
                break;
            default:
                Debug.LogError("NodFindID:" + rewardId);
                break;
        }
    }
    
}

