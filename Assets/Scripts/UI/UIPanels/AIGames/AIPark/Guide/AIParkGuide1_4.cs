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
using Message;
using Game.Avatar;

public class AIParkGuide1_4 : AIParkGuideBase
{
    private AIParkDialogMgr _dialogManager;
    int step = 1;
    private AIPark_CharacterBehaviour _tiliaNpc;

    public override void RunGuide()
    {
        base.RunGuide();

        // 获取对话管理器
        var guestPanel = UIManager.Inst.FindPanel<AIParkGuestPanel>(PanelId.AIParkGuestPanel);
        if (guestPanel != null)
        {
            _dialogManager = guestPanel.GetDialogManager();
        }
        _tiliaNpc = AIPark_CharacterManager.Inst.GetNpc(((int)ParkNpcRoleType.Tilia).ToString());

        TimerManager.Inst.RunOnce("AIParkGuide1_4_StartTalk", 3f, () =>
        {
            MessageHelper.Broadcast<string, string, Action>(MessageName.OnParkBeginNpcTalk, _tiliaNpc.GetNpcID(), "很高兴认识你！我们这儿除了我，还有很多小伙伴可以一起玩呢～", () =>
            {
                TimerManager.Inst.RunOnce("AIParkGuide1_4_CameraToAllNpc", 2.6f, () =>
                {
                    //运镜,查看场景其他npc
                    CameraToAllNpc();
                });
            });
        });

    }

    public void CameraToAllNpc()
    {
        var npcDic = AIPark_CharacterManager.Inst.GetNpcDic();
        var npcList = new List<string>();
        foreach (var npc in npcDic)
        {
            npcList.Add(npc.Key);
        }
        CoroutineManager.Inst.StartCoroutine(CameraToAllNpcCoroutine(npcList));
    }

    private IEnumerator CameraToAllNpcCoroutine(List<string> npcList)
    {
        foreach (var npcStr in npcList)
        {
            var npc = AIPark_CharacterManager.Inst.GetNpc(npcStr);
            if (npc.GetNpcID() == ((int)ParkNpcRoleType.Tilia).ToString() || npc.GetNpcID() == ((int)ParkNpcRoleType.self).ToString())
            {
                continue;
            }

            // 获取NPC的位置和朝向
            Vector3 npcPosition = npc.transform.position;
            Vector3 npcForward = npc.transform.forward;
            Vector3 npcUp = npc.transform.up;

            // 使用AIGameCameraUtils设置相机朝向NPC，过渡时间1秒，距离3米
            AIGameCameraUtils.Inst.SetCamLookAt(npcPosition + new Vector3(0, 0.5f, 0), -(npcForward * 2.5f + npcUp), 1f, 5f);

            // 等待2秒让玩家观察NPC
            yield return new WaitForSeconds(2f);
        }

        AIGameCameraUtils.Inst.SetMoveCameraPosAndRotation(new(26.97f, 2.366f, 0.607f), new(6.86f, 85f, 0));
        AIGameCameraUtils.Inst.SetCamFOV(50);

        // 查看完所有NPC后，回到玩家位置
        // AIGameCameraUtils.Inst.BackToPlayer(1f);

        // ResetPlayerAndNpcPositions();
        SetCameraToTilia();

        TriggerNextStep();


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
            // );
        }
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
                _tiliaNpc.PlayAnim(AIParkConfig.Anim_StateLy_Stand);
                _tiliaNpc._npcAnimController.SetPlayerAniState(PlayerAniState.Idle, true);
            }
        }
    }

    public override void TriggerNextStep()
    {
        base.TriggerNextStep();

    }
}