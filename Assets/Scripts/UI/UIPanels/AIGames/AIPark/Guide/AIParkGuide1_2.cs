using System;
using System.Collections;
using Game.Props.PropsManagers;
using Game.Props.PropsManagers.AIGames.AIPark.FSM;
using Game.Utils;
using UI;
using UI.Base;
using UnityEngine;
using Game.Props;
using AIGame.Base;
using Game.Props.PropsBehaviours;
using Message;
public class AIParkGuide1_2 : AIParkGuideBase
{
    private AIParkDialogMgr _dialogManager;
    public override void RunGuide()
    {
        base.RunGuide();

        // 获取对话管理器
        var guestPanel = UIManager.Inst.FindPanel<AIParkGuestPanel>(PanelId.AIParkGuestPanel);
        if (guestPanel != null)
        {
            _dialogManager = guestPanel.GetDialogManager();
        }
        var _tiliaNpc = AIPark_CharacterManager.Inst.GetNpc(((int)ParkNpcRoleType.Tilia).ToString());

      

        TimerManager.Inst.RunOnce("AIParkGuide1_2_ShowDialog", 2f, () =>
        {
            MessageHelper.Broadcast<string, string, Action>(MessageName.OnParkBeginNpcTalk, _tiliaNpc.GetNpcID(), "我带你在这里转转吧,你可以牵着我的手吗？", () =>

            // _dialogManager.ShowNpcDialog(_tiliaNpc, "我带你在这里转转吧,你可以牵着我的手吗？", true, true, true, () =>
            {
                var guidePanel = UIManager.Inst.OpenPanel<AIParkStrongGuide>(PanelId.AIParkStrongGuidePanel, WindowId.GuestWindow, AIGameParkConfig.EPgcGuideID.LinkBtnTips);
            });
        });

    }

    public override void TriggerNextStep()
    {
        base.TriggerNextStep();
    }
}

