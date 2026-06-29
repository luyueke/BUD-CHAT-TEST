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
using UnityEngine.Rendering;
using Game.Base;
using Game.Avatar;
public class AIParkGuide1_3 : AIParkGuideBase
{
    private AIParkDialogMgr _dialogManager;
    private AIPark_CharacterBehaviour _tiliaNpc;
    public int step = (int)GuideStep1_3.Guide_FirstInGame_Guide_1_3_None;
    public int GetCurStep(){
        return step;
    }
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



        //         string emoteID = "40100401";
        // #if UNITY_EDITOR || UNITY_STANDARD_BUILD
        //         if (MobileInterface.Instance.onClientRespose.ContainsKey(MobileInterfaceDefine.quickEmote))
        //         {
        //             MobileInterface.Instance.onClientRespose[MobileInterfaceDefine.quickEmote]?.Invoke(emoteID);
        //         }
        // #endif
        //         MessageHelper.Broadcast(MessageName.OnS11ChatEmoteClick, emoteID, "");
        //         LoggerUtils.Log("DebugInputPanel QuickEmoteClick:" + emoteID);
        //         AIParkUtils.Inst.OnSendDoubleEmoteReq();

        //         var talkState = _tiliaNpc.ForceEnterInterruptState<TalkWithPlayerState>();
        //         talkState.StartPlayNpcEmote(emoteID, true);

        TriggerNext();
    }

    public override void TriggerNext()
    {
        base.TriggerNext();
        Debug.Log("TriggerNext:" + step);
        if (step == (int)GuideStep1_3.Guide_FirstInGame_Guide_1_3_None)
        {
            step = (int)GuideStep1_3.Guide_FirstInGame_Guide_1_3_None_1;
            TimerManager.Inst.RunOnce("AIParkGuide1_3_StartTalk", 2f, () =>
            {
                MessageHelper.Broadcast<string, string, Action>(MessageName.OnParkBeginNpcTalk, _tiliaNpc.GetNpcID(), "哎呀我们不要在这站着了，我带你一起去荡秋千吧！你一定会喜欢的！", () =>
                {
                    TimerManager.Inst.RunOnce("AIParkGuide1_3_StartTalk", 1f, () =>
                    {
                        TriggerNext();
                    });
                });
            });

        }else if(step == (int)GuideStep1_3.Guide_FirstInGame_Guide_1_3_None_1){
            step = (int)GuideStep1_3.Guide_FirstInGame_Guide_1_3_1;
            var guestPanel = UIManager.Inst.FindPanel<AIParkGuestPanel>(PanelId.AIParkGuestPanel);
            if (guestPanel != null)
            {
                guestPanel.GuideEventTargetBgShow("和提莉亚去坐秋千");
                guestPanel.GuideTarget2SwingTxt();
            }
            AIParkGuideMgr.Inst.BootPanelGuide(AIGameParkConfig.EPgcGuideID.Guide2SwingTxt);
        }
        else if (step == (int)GuideStep1_3.Guide_FirstInGame_Guide_1_3_1)
        {
            //小地图出现，强引导地图
            var guestPanel = UIManager.Inst.FindPanel<AIParkGuestPanel>(PanelId.AIParkGuestPanel);
            if (guestPanel != null)
            {
                guestPanel.GuideMapPanel();
            }
            AIParkGuideMgr.Inst.BootPanelGuide(AIGameParkConfig.EPgcGuideID.CheckMapTips);
            // var guidePanel = UIManager.Inst.OpenPanel<AIParkStrongGuide>(PanelId.AIParkStrongGuidePanel, WindowId.GuestWindow, AIGameParkConfig.EPgcGuideID.CheckMapTips);
            step = (int)GuideStep1_3.Guide_FirstInGame_Guide_1_3_2;
        }
        else if (step == (int)GuideStep1_3.Guide_FirstInGame_Guide_1_3_2)
        {
            var mapPanel = UIManager.Inst.OpenPanel<AIParkGameMiniMapPanel>(PanelId.AIParkGameMiniMapPanel, WindowId.GuestWindow);
            if (mapPanel != null)
            {
                mapPanel.ShowGuideHighLightSwing(); //todo 秋千位置
            }
            step = (int)GuideStep1_3.Guide_FirstInGame_Guide_1_3_3;
        }
        else if (step == (int)GuideStep1_3.Guide_FirstInGame_Guide_1_3_3)
        {
            //关闭地图后 出现摇杆/跳跃按钮
            // GameObject.Find("CantHideRoot").transform.localScale = Vector3.one;
            var aiParkGame = AIGameController.Inst.GetCurAIGame<AIParkGame>();
            // aiParkGame.LoadEffectAndSetParent(_tiliaNpc);

            var guestPanel = UIManager.Inst.FindPanel<AIParkGuestPanel>(PanelId.AIParkGuestPanel);
            if (guestPanel != null)
            {
                guestPanel.hudPanel.bCheckRaycast = false;
                guestPanel.GuideEventTargetPanel();
                guestPanel.GuideEventTargetBgShow("和提莉亚去坐秋千");
                AIGameCameraUtils.Inst.BackToPlayer();
                guestPanel.DirectUnlockInput();

                AvatarController.Inst.SelfController.CameraTarget.eulerAngles = AvatarController.Inst.SelfController.transform.eulerAngles;
                AvatarController.Inst.SelfController.AfterCharacterMove();

                _tiliaNpc.MoveToPosition(new(32.1f, 0, 1.7f));
                // guestPanel.GuideCameraJoyStick();
                UIOperationOnWorldPanel.bBeginRaycast = false;
                //跟随_tiliaNpc
                // StartPlayerFollowTilia(); //屏蔽跟随

            }
            step = (int)GuideStep1_3.Guide_FirstInGame_Guide_1_3_4;
            TriggerNext();
        }
        else if (step == (int)GuideStep1_3.Guide_FirstInGame_Guide_1_3_4)
        {
            var guestPanel = UIManager.Inst.FindPanel<AIParkGuestPanel>(PanelId.AIParkGuestPanel);
            guestPanel.GuideCameraJoyStickRevert();
            guestPanel.canvasGroup.interactable = true;
            AIParkGuideMgr.Inst.bBanChatToNpc = false;
            AvatarController.Inst.SelfController.SetFreezeCharacter(false);
            guestPanel.guestGroup.GetComponent<CanvasGroup>().interactable = true;
            // guestPanel.DisableJoyStick();
            // AIGameCameraUtils.Inst.SetMoveCameraPosAndRotation(new(26.97f, 2.366f, 0.607f), new(6.86f, 85f, 0));
            // AIGameCameraUtils.Inst.SetCamFOV(50);
            UIOperationOnWorldPanel.bBeginRaycast = true;
            // TimerManager.Inst.RunOnce("AIParkGuide1_3_bBeginRaycast", 0.01f, () =>
            // {
            // AIParkGuideMgr.Inst.BootPanelGuide(AIGameParkConfig.EPgcGuideID.Touch_Hand_Swing);
            // });

            step = (int)GuideStep1_3.Guide_FirstInGame_Guide_1_3_5;
        }

        else if (step == (int)GuideStep1_3.Guide_FirstInGame_Guide_1_3_5)
        {
            UIOperationOnWorldPanel.bBeginRaycast = false;

            //tilia跟自己做上秋千
            var bev = GlobalNodeManager.Inst.Get<AIPark_SwingMgr>().GetBehaviour();
            AIParkPropsManager.Inst.SelfEnterProp(bev);
            AIParkPropsManager.Inst.EnterAction(_tiliaNpc, bev);
            step = (int)GuideStep1_3.Guide_FirstInGame_Guide_1_3_6;
            TriggerNext();
        }
        else if (step == (int)GuideStep1_3.Guide_FirstInGame_Guide_1_3_6)
        {
            //八音盒人偶讲的话
            var guestPanel = UIManager.Inst.FindPanel<AIParkGuestPanel>(PanelId.AIParkGuestPanel);
            if (guestPanel != null)
            {
                guestPanel.SendInput("一起荡秋千真开心呀！");
            }
            step = (int)GuideStep1_3.Guide_FirstInGame_Guide_1_3_7;
        }
        else if (step == (int)GuideStep1_3.Guide_FirstInGame_Guide_1_3_7)
        {
            step = (int)GuideStep1_3.Guide_FirstInGame_Guide_1_3_8;
            TriggerNextStep();
        }
    }

    public override void TriggerNextStep()
    {
        AIParkPropsManager.Inst.SetGuideSwinging(false);
        base.TriggerNextStep();

    }

    private BudTimer _followTiliaTimer = null;

    private void StartPlayerFollowTilia()
    {
        TimerManager.Inst.RunOnce("AIParkGuide1_3_StartPlayerFollowTilia", 1f, () =>
        {
            // 获取玩家控制器
            var playerController = AvatarController.Inst.SelfController;
            LoggerUtils.Log("StartPlayerFollowTilia: 开始跟随Tilia NPC");

            // 使用TimerManager实现实时跟随
            StartPlayerFollowWithTimer();
        });

    }

    private void StartPlayerFollowWithTimer()
    {
        var playerController = AvatarController.Inst.SelfController;
        if (playerController == null || _tiliaNpc == null) return;

        float followSpeed = 2f; // 跟随速度
        float followDistance = 1f; // 跟随距离（在Tilia后面2米）
        float maxFollowTime = 30f; // 最大跟随时间（防止卡死）
        float startTime = Time.time;

        // 使用TimerManager创建循环Timer
        _followTiliaTimer = TimerManager.Inst.Run("PlayerFollowTilia", 0f, 0.016f, () => // 约60FPS
        {
            // 检查是否超时
            if (Time.time - startTime > maxFollowTime)
            {
                LoggerUtils.Log("StartPlayerFollowWithTimer: 跟随超时，停止跟随");
                StopPlayerFollowTilia();
                return;
            }

            // 检查NPC和玩家是否还存在
            if (playerController == null || _tiliaNpc == null)
            {
                LoggerUtils.Log("StartPlayerFollowWithTimer: NPC或玩家为空，停止跟随");
                StopPlayerFollowTilia();
                return;
            }

            // 获取Tilia的当前位置
            Vector3 tiliaPosition = _tiliaNpc.transform.position;
            Vector3 playerPosition = playerController.transform.position;

            // 计算玩家应该跟随的位置（在Tilia后面followDistance米处）
            Vector3 followOffset = new Vector3(0, 0, -followDistance);
            Vector3 targetFollowPosition = tiliaPosition + followOffset;

            // 计算当前距离
            float distanceToTarget = Vector3.Distance(playerPosition, targetFollowPosition);

            // 如果距离太远，需要移动
            if (distanceToTarget > 0.5f) // 如果距离超过0.5米，开始移动
            {
                // 计算移动方向
                Vector3 direction = (targetFollowPosition - playerPosition).normalized;
                direction.y = 0; // 保持水平移动

                // 计算移动距离
                float moveDistance = followSpeed * 0.016f; // 使用固定帧时间
                Vector3 newPosition = playerPosition + direction * moveDistance;

                // 计算朝向（面向移动方向）
                Quaternion targetRotation = Quaternion.LookRotation(direction);

                // 使用KCC设置位置和旋转
                playerController.SetCalculatePos(newPosition);
                playerController.Motor.SetPositionAndRotation(newPosition, targetRotation);

                // 播放跑步动画
                var animController = playerController.PlayerAnimCtrl;
                if (animController != null)
                {
                    animController.SetPlayerAniState(PlayerAniState.Run, false);
                }

                LoggerUtils.Log($"StartPlayerFollowWithTimer: 跟随中，距离: {distanceToTarget:F2}");
            }
            else
            {
                // 距离足够近，播放待机动画
                var animController = playerController.PlayerAnimCtrl;
                if (animController != null)
                {
                    animController.SetPlayerAniState(PlayerAniState.Idle, false);
                }

                LoggerUtils.Log("StartPlayerFollowWithTimer: 到达目标位置，停止跟随");
                StopPlayerFollowTilia();
            }
        });
    }

    private void StopPlayerFollowTilia()
    {
        var playerController = AvatarController.Inst.SelfController;
        if (playerController == null)
        {
            LoggerUtils.LogError("StopPlayerFollowTilia: 玩家控制器为空");
            return;
        }

        if (_followTiliaTimer != null)
        {
            TimerManager.Inst.Stop(_followTiliaTimer);
            _followTiliaTimer = null;
        }

        LoggerUtils.Log("StopPlayerFollowTilia: 停止跟随Tilia NPC");
        TriggerNext();
    }

    public enum GuideStep1_3{
        Guide_FirstInGame_Guide_1_3_None = 0,
        Guide_FirstInGame_Guide_1_3_1 = 1,
        Guide_FirstInGame_Guide_1_3_2 = 2,
        Guide_FirstInGame_Guide_1_3_3 = 3,
        Guide_FirstInGame_Guide_1_3_4 = 4,
        Guide_FirstInGame_Guide_1_3_5 = 5,
        Guide_FirstInGame_Guide_1_3_6 = 6,
        Guide_FirstInGame_Guide_1_3_7 = 7,
        Guide_FirstInGame_Guide_1_3_8 = 8,


        Guide_FirstInGame_Guide_1_3_None_1 = 9,

    }
}

