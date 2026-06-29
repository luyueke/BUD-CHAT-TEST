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
using System.Collections.Generic;
using Game.Avatar;
using Message;

//第一个事件
public class AIParkGuide2_3 : AIParkGuideBase
{
    private AIParkDialogMgr _dialogManager;
    private AIPark_CharacterBehaviour _tiliaNpc;
    int step = 1;

    public override void RunGuide()
    {
        base.RunGuide();

        AIParkPropsManager.Inst.bBanTillaAction = true;

        // 获取对话管理器
        var guestPanel = UIManager.Inst.FindPanel<AIParkGuestPanel>(PanelId.AIParkGuestPanel);
        if (guestPanel != null)
        {
            _dialogManager = guestPanel.GetDialogManager();
        }

        // 获取八音盒人偶（提莉亚）
        _tiliaNpc = AIPark_CharacterManager.Inst.GetNpc(((int)ParkNpcRoleType.Tilia).ToString());
        ResetPlayerAndNpcPositions();

        SetCameraToTilia();


        MessageHelper.Broadcast<string, string, Action>(MessageName.OnParkBeginNpcTalk, _tiliaNpc.GetNpcID(), "再不找出这些事情背后的真相大家的关系就真无法挽回了...你找到捣蛋鬼是谁了吗？如果找到的话记得去选择你认为的捣蛋鬼噢！", () =>
        {
            LoggerUtils.Log("AIParkGuide2_1: 提莉亚对话完成");
            TimerManager.Inst.RunOnce("AIParkGuide2_3_1", 0.5f, () =>
            {
                // 对话完成后恢复提莉亚的朝向
                //todo tilia做自己的事情
                AIParkPropsManager.Inst.bBanTillaAction = false;
                ShowAllUI();

                _tiliaNpc.ForceEnterCurHistory();

                TriggerNext();
            });



        });
    }

    private void ResetPlayerAndNpcPositions()
    {
        var locationType = AIParkUtils.Inst.DiscussLocationType;
        // 获取玩家出生位置
        var playerSpawnPos = AIPark_CharacterUtils.Inst.GetGuidePosByGuidePosType(true, locationType);
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
        var tiliaSpawnPos = AIPark_CharacterUtils.Inst.GetGuidePosByGuidePosType(false, locationType);
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
                _tiliaNpc.PlayAnim(AIParkConfig.Anim_StateLy_Stand);
            }
        }
    }


    private void SetCameraToTilia()
    {
        //禁止提莉亚做动作
        // AIParkGuideMgr.Inst.SetAllNpcGuidePos();
        var trans = AvatarController.Inst.SelfController.transform;
        // 设置镜头对准提莉亚
        // AIGameCameraUtils.Inst.SetCamLookAt((trans.position+_tiliaNpc.transform.position)/2, -(-trans.forward * 2f + trans.up * 0.8f), 0, 11.0f, () =>
        {

            if (AIParkUtils.Inst.DiscussLocationType == LocationType.Park)
            {
                AIGameCameraUtils.Inst.SetMoveCameraPosAndRotation(new(23.52f, 2.62f, 5.22f), new(10, 111f, -0.875f));
            }
            else if (AIParkUtils.Inst.DiscussLocationType == LocationType.Stage)
            {
                AIGameCameraUtils.Inst.SetMoveCameraPosAndRotation(new(-2f, 3.275f, -4.325f), new(18.45f, 114, 0));
            }
            else if (AIParkUtils.Inst.DiscussLocationType == LocationType.Fountain)
            {
                AIGameCameraUtils.Inst.SetMoveCameraPosAndRotation(new(-4.5f, 3.4f, 9.84f), new(8.6f, 180f, 0));
            }
            AIGameCameraUtils.Inst.SetCamFOV(50);
            // AIGameCameraUtils.Inst.BackToPlayer();//test
            // var mainCameraTarget = GameObject.Find("CameraTarget");
            // mainCameraTarget.transform.localEulerAngles = new Vector3(10f, 180f, 0);
            // AvatarController.Inst.SelfController.AfterCharacterMove();//test
            // 镜头对准完成后开始对话

            // var tiliaTalkPos = AIPark_CharacterUtils.Inst.GetGuidePosByGuidePosType(GuidePosType.TiliaTalk_First_Park);
            // if (tiliaTalkPos != null)
            // {
            //     _tiliaNpc.MoveToPosition(tiliaTalkPos.pos, () =>
            //     {
            //         var guestPanel = UIManager.Inst.FindPanel<AIParkGuestPanel>(PanelId.AIParkGuestPanel);

            //         guestPanel.hudPanel.bCheckRaycast = true;
            //         TimerManager.Inst.RunOnce("AIParkGuide1_1_StartTiliaConversation", 0.05f, () =>
            //         {
            //             StartTiliaConversation();
            //         });
            //     });
            // }
        }
        // );

    }

    /// <summary>
    /// 显示所有UI
    /// </summary>
    private void ShowAllUI()
    {
        // 显示 AIParkGuestPanel
        var guestPanel = UIManager.Inst.FindPanel<AIParkGuestPanel>(PanelId.AIParkGuestPanel);
        if (guestPanel != null)
        {
            AvatarController.Inst.SelfController.SetFreezeCharacter(false);
            guestPanel.ShowPanel();
            guestPanel.GuideRevertPanel();
            guestPanel.DirectUnlockInput();
            AIGameCameraUtils.Inst.BackToPlayerFromMoveCam();
        }

        LoggerUtils.Log("AIParkGuide2_1: 显示所有UI完成");
    }

    public override void TriggerNext()
    {
        base.TriggerNext();
        if (step == 1)
        {
            AIParkGuideMgr.Inst._aiGame.ShowSelectEvent();
            step++;
            TriggerNext();
        }
        else if (step == 2)
        {
            AIParkGuideMgr.Inst.BootPanelGuide(AIGameParkConfig.EPgcGuideID.Event_2_3_2);
            step++;
        }
        else if (step == 3)
        {
            AIParkGuideMgr.Inst.BootPanelGuide(AIGameParkConfig.EPgcGuideID.Event_2_3_3);
            step++;
        }
        else if (step == 4)
        {
            BootDataManager.Inst.SetParkInGame(2);
            TriggerNextStep();
        }

    }

    public override void TriggerNextStep()
    {
        base.TriggerNextStep();
    }
}