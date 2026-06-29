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
using Game.Avatar;
using Game.KinematicCharacter;
using Message;

public class AIParkGuide1_1 : AIParkGuideBase
{
    private AIPark_CharacterBehaviour _tiliaNpc;
    private AIParkDialogMgr _dialogManager;
    private bool _isDialogComplete = false;

    public override void RunGuide()
    {
        base.RunGuide();

        AIParkPropsManager.Inst.SetGuideSwinging(true);
        AIPark_CharacterManager.Inst.SceneAIDoRandomActions(); //地图npc开始行动
        // 开始引导流程
        StartGuideSequence();
    }

    private void StartGuideSequence()
    {
        AIParkGuideMgr.Inst.bBanChatToNpc = true;
        // 1. 重置玩家及八音盒到出生位置
        ResetPlayerAndNpcPositions();

        AvatarController.Inst.SelfController.SetFreezeCharacter(true);

        var guestPanel = UIManager.Inst.FindPanel<AIParkGuestPanel>(PanelId.AIParkGuestPanel);
        if (guestPanel != null)
        {
            guestPanel.hudPanel.bCheckRaycast = false;
        }

        // 2. 隐藏 AIParkGuestPanel UI
        HideAIParkGuestPanel();

        // 3. 延迟一下让位置重置完成，然后设置镜头对准八音盒
        // TimerManager.Inst.RunOnce("AIParkGuide1_1_SetCameraToTilia", 1.0f, () =>
        {
            SetCameraToTilia();
        }
        // );
    }

    private void SetCameraToTilia()
    {
        //禁止提莉亚做动作
        AIParkPropsManager.Inst.SetBanDoCustomAction(true);
        AIParkGuideMgr.Inst.SetAllNpcGuidePos();
        var trans = AvatarController.Inst.SelfController.transform;
        // 设置镜头对准提莉亚
        // AIGameCameraUtils.Inst.SetCamLookAt((trans.position+_tiliaNpc.transform.position)/2, -(-trans.forward * 2f + trans.up * 0.8f), 0, 11.0f, () =>
        {
            AIGameCameraUtils.Inst.SetMoveCameraPosAndRotation(new(4.689f, 2.7f, 20.8f), new(10f, -180f, 0));
            AIGameCameraUtils.Inst.SetCamFOV(50);
            // AIGameCameraUtils.Inst.BackToPlayer();//test
            // var mainCameraTarget = GameObject.Find("CameraTarget");
            // mainCameraTarget.transform.localEulerAngles = new Vector3(10f, 180f, 0);
            // AvatarController.Inst.SelfController.AfterCharacterMove();//test
            // 镜头对准完成后开始对话
            var tiliaTalkPos = AIPark_CharacterUtils.Inst.GetGuidePosByGuidePosType(GuidePosType.TiliaTalk_First_Park);
            if (tiliaTalkPos != null)
            {
                _tiliaNpc.MoveToPosition(tiliaTalkPos.pos, () =>
                {
                    var guestPanel = UIManager.Inst.FindPanel<AIParkGuestPanel>(PanelId.AIParkGuestPanel);

                    guestPanel.hudPanel.bCheckRaycast = true;
                    TimerManager.Inst.RunOnce("AIParkGuide1_1_StartTiliaConversation", 0.05f, () =>
                    {
                        StartTiliaConversation();
                    });
                });
            }
        }
        // );

    }

    private void ResetPlayerAndNpcPositions()
    {
        // 获取玩家出生位置
        var playerSpawnPos = AIPark_CharacterUtils.Inst.GetGuidePosByGuidePosType(GuidePosType.PlayerSpawn_First_Park);
        if (playerSpawnPos != null)
        {
            // 重置玩家位置
            var player = AIPark_CharacterManager.Inst.GetNpc(((int)ParkNpcRoleType.self).ToString());

            if (player != null)
            {
                player.SetPositionAndRotation(playerSpawnPos.pos, Quaternion.Euler(playerSpawnPos.rot));
            }
        }

        // 获取八音盒人偶（提莉亚）的出生位置
        var tiliaSpawnPos = AIPark_CharacterUtils.Inst.GetGuidePosByGuidePosType(GuidePosType.TiliaSpawn_First_Park);
        if (tiliaSpawnPos != null)
        {
            // 获取提莉亚NPC
            _tiliaNpc = AIPark_CharacterManager.Inst.GetNpc(((int)ParkNpcRoleType.Tilia).ToString());
            if (_tiliaNpc != null)
            {
                AIParkPropsManager.Inst.ExitAction(((int)ParkNpcRoleType.Tilia).ToString());
                // 重置提莉亚位置
                _tiliaNpc.SetPositionAndRotation(tiliaSpawnPos.pos, Quaternion.Euler(tiliaSpawnPos.rot));
                // 停止移动并播放站立动画
                _tiliaNpc.StopMoving();
                _tiliaNpc._npcStateController.ExitState(PlayerState.SingleEmote, false);
                // _tiliaNpc.PlayAnim(AIParkConfig.Anim_StateLy_Stand);
                // _tiliaNpc._npcAnimController.SetPlayerAniState(PlayerAniState.Idle, true);
            }
        }
    }

    private void HideAIParkGuestPanel()
    {
        // 隐藏 AIParkGuestPanel
        var guestPanel = UIManager.Inst.FindPanel<AIParkGuestPanel>(PanelId.AIParkGuestPanel);
        if (guestPanel != null)
        {
            guestPanel.GuideHidePanel();
        }
    }

    private void StartTiliaConversation()
    {
        // 获取对话管理器
        var guestPanel = UIManager.Inst.FindPanel<AIParkGuestPanel>(PanelId.AIParkGuestPanel);
        if (guestPanel != null)
        {
            _dialogManager = guestPanel.GetDialogManager();
        }

        if (_dialogManager != null && _tiliaNpc != null)
        {
            // 让提莉亚看向玩家
            _tiliaNpc.LookAtPlayer();

            // 延迟一下让提莉亚转向完成，然后开始对话
            TimerManager.Inst.RunOnce("AIParkGuide1_1_StartTiliaConversation", 0.2f, () =>
            {
                // 显示提莉亚的对话
                MessageHelper.Broadcast<string, string, Action>(MessageName.OnParkBeginNpcTalk, _tiliaNpc.GetNpcID(), "欢迎来到梦幻游乐园！我叫提莉亚，你叫什么名字呀？", () =>
                {
                    _isDialogComplete = true;
                    beginConversationBtnGuide();
                });
            });
        }
        else
        {
            LoggerUtils.LogError("AIParkGuide1_3: 无法获取对话管理器或提莉亚NPC");
        }
    }

    public void beginConversationBtnGuide()
    {
        AIParkGuideMgr.Inst.BootPanelGuide(AIGameParkConfig.EPgcGuideID.ChatBtnTips);
    }

    public override void TriggerNext()
    {
        base.TriggerNext();
    }

    public override void TriggerNextStep()
    {
        base.TriggerNextStep();
    }
}