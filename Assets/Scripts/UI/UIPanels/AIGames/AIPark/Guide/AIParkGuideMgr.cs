using System;
using System.Collections.Generic;
using UnityEngine;
using Basic.Utils;
using Game.Props;
using Message;
using Game.Utils;
using Game.Props.PropsManagers;
using Game.Props.PropsManagers.AIGames.AIPark.FSM;
using AIGame.Base;
using Game.Avatar;
using Game.KinematicCharacter;
using UI.UIPanels.ProfilePanel;
using System.Linq;
using Game.Props.PropsManagers;
using Newbie;
using Game.Props.PropsBehaviours;
public class AIParkGuideMgr : GlobalInstance<AIParkGuideMgr>
{
    public AIParkGame _aiGame;

    public bool isGuide = false;
    public AIParkGuideBase curGuide;
    public bool bBanChatToNpc = false;
    public S11GuideStep curStep;
    public void Init(AIParkGame aiGame)
    {
        _aiGame = aiGame;
        MessageHelper.AddListener<int, bool>(MessageName.OnS11GuideStepClick, OnStepClick);
    }

    private void OnStepClick(int state, bool isAuto)
    {
        if (state == (int)AIGameParkConfig.EPgcGuideID.ChatBtnTips)
        {
            if (AvatarController.Inst.SelfStateController.IsMainState(PlayerState.DoubleEmote))
            {
                AvatarController.Inst.SelfStateController.ExitState(PlayerState.DoubleEmote);
            }
            var _character = AIPark_CharacterManager.Inst.GetNpc(((int)ParkNpcRoleType.Tilia).ToString());
            _character.StartTalkWithPlayer();
            var playerStateController = _character.GetComponentInChildren<KinematicCharacterController>();
            AIBuddyAvatarController.Inst.CurrentInteractNpcID = playerStateController.PlayerID;
            GameAINpcChatManager_Park.Inst.StartChatToAIBuddy(true, playerStateController.PlayerID);
            return;
        }
        else if (state == (int)AIGameParkConfig.EPgcGuideID.LinkBtnTips)
        {
            if (curGuide is AIParkGuide1_2)
            {
                curGuide.TriggerNextStep();
            }
        }
        else if (state == (int)AIGameParkConfig.EPgcGuideID.CheckMapTips)
        {
            if (curGuide is AIParkGuide1_3)
            {
                curGuide.TriggerNext();
            }
        }
        else if (state == (int)AIGameParkConfig.EPgcGuideID.Event_1)
        {
            if (curGuide is AIParkGuide2_1)
            {
                curGuide.TriggerNext();
            }
        }
        else if (state == (int)AIGameParkConfig.EPgcGuideID.Event_2_1_1 || state == (int)AIGameParkConfig.EPgcGuideID.Event_2_1_2)
        {
            if (curGuide is AIParkGuide2_1)
            {
                curGuide.TriggerNext();
            }
        }
        else if (state == (int)AIGameParkConfig.EPgcGuideID.Event_2_2_1)
        {
            if (curGuide is AIParkGuide2_2)
            {
                curGuide.TriggerNext();
            }
        }
        else if (state == (int)AIGameParkConfig.EPgcGuideID.Event_2_3_1 || state == (int)AIGameParkConfig.EPgcGuideID.Event_2_3_2 || state == (int)AIGameParkConfig.EPgcGuideID.Event_2_3_3)
        {
            if (curGuide is AIParkGuide2_3)
            {
                curGuide.TriggerNext();
            }
        }
        else if (state == (int)AIGameParkConfig.EPgcGuideID.Event_3_1_1)
        {
            if (curGuide is AIParkGuide3_1)
            {
                curGuide.TriggerNext();
            }
        }
        else if (state == (int)AIGameParkConfig.EPgcGuideID.Touch_Hand_Swing)
        {
            if (curGuide is AIParkGuide1_3)
            {
                curGuide.TriggerNext();
            }
        }
        else if (state == (int)AIGameParkConfig.EPgcGuideID.Guide2SwingTxt)
        {
            if (curGuide is AIParkGuide1_3)
            {
                curGuide.TriggerNext();
            }
        }
    }

    public void CancelChatGuide()
    {
        if (isGuide && curGuide is AIParkGuide1_1)
        {
            (curGuide as AIParkGuide1_1).beginConversationBtnGuide();
        }
    }

    public void GuideOver()
    {
        isGuide = false;
        curGuide = null;
    }

    public void SpeakBeginTriggerNext()
    {
        if (isGuide && curGuide != null)
        {
            var quickEmotePanel = UIManager.Inst.FindPanel<AIParkQuickEmotePanel>(PanelId.AIParkQuickEmotePanel);
            if (quickEmotePanel != null)
            {
                quickEmotePanel.CloseSelf();
            }
        }
    }
    public void SpeakOverTriggerNext()
    {
        if (isGuide && curGuide != null)
        {
            if (curStep == S11GuideStep.Guide_FirstInGame_Guide_1_3)
            {
                if ((curGuide as AIParkGuide1_3).step == 4)
                {
                    return;
                }
            }
            TriggerNextStep();
        }
    }

    public void TriggerNextStepWithCheck(int step)
    {
        if (isGuide && curGuide != null)
        {
            if (curStep == S11GuideStep.Guide_FirstInGame_Guide_1_3)
            {
                if ((curGuide as AIParkGuide1_3).GetCurStep() == step)
                {
                    curGuide.TriggerNext();
                }
            }
        }
    }
    public void TriggerNextStep()
    {
        if (isGuide && curGuide != null)
        {
            if (curStep == S11GuideStep.Guide_FirstInGame_Guide_1_1)
            {
                curGuide.TriggerNextStep();
            }
            else if (curStep == S11GuideStep.Guide_FirstInGame_Guide_1_2)
            {
                curGuide.TriggerNextStep();
            }
            else if (curStep == S11GuideStep.Guide_FirstInGame_Guide_1_3)
            {
                curGuide.TriggerNext();
            }
        }
    }


    public void RunGuide(S11GuideStep state)
    {
        isGuide = true;
        OnStepChange(state);
    }

    public void HideGuestPanel()
    {
        var guestPanel = UIManager.Inst.FindPanel<AIParkGuestPanel>(PanelId.AIParkGuestPanel);
        if (guestPanel != null)
        {
            guestPanel.HidePanel();

            guestPanel.GetHubPanel()?.gameObject.SetActive(true);
        }
    }

    public void RevertGuestPanel()
    {

    }

    public void OnStepChange(S11GuideStep state)
    {
        Debug.Log("Guide OnStepChange:" + state);
        curStep = state;
        switch (state)
        {
            case S11GuideStep.Guide_FirstInGame_Guide_1_1:
                var guide1_1 = new AIParkGuide1_1();
                guide1_1.SetNextStep(S11GuideStep.Guide_FirstInGame_Guide_1_4);   //进入下一步运镜
                guide1_1.RunGuide();
                curGuide = guide1_1;
                break;
            case S11GuideStep.Guide_FirstInGame_Guide_1_2:
                var guide1_2 = new AIParkGuide1_2();
                guide1_2.SetNextStep(S11GuideStep.Guide_FirstInGame_Guide_1_3); //先跳过第二步
                guide1_2.RunGuide();
                curGuide = guide1_2;
                break;
            case S11GuideStep.Guide_FirstInGame_Guide_1_3:
                var guide1_3 = new AIParkGuide1_3();
                guide1_3.SetNextStep(S11GuideStep.Guide_FirstInGame_Guide_1_5);
                guide1_3.RunGuide();
                curGuide = guide1_3;
                break;
            case S11GuideStep.Guide_FirstInGame_Guide_1_4:
                var guide1_4 = new AIParkGuide1_4();
                guide1_4.SetNextStep(S11GuideStep.Guide_FirstInGame_Guide_1_3);
                guide1_4.RunGuide();
                curGuide = guide1_4;
                break;
            case S11GuideStep.Guide_FirstInGame_Guide_1_5:
                var guide1_5 = new AIParkGuide1_5();
                guide1_5.SetNextStep(S11GuideStep.Guide_FirstInGame_Guide_1_6);
                guide1_5.RunGuide();
                curGuide = guide1_5;
                break;
            case S11GuideStep.Guide_FirstInGame_Guide_1_6:
                BootDataManager.Inst.SetParkInGame(1); //完成大引导
                curStep = S11GuideStep.None;
                var guestPanel = UIManager.Inst.FindPanel<AIParkGuestPanel>(PanelId.AIParkGuestPanel);
                guestPanel.hudPanel.bCheckRaycast = true;
                guestPanel.ShowPanel();
                guestPanel.GuideRevertPanel();
                guestPanel.DirectUnlockInput();
                AvatarController.Inst.SelfController.SetFreezeCharacter(false);
                _aiGame.EnterBlackPanel(1, 0, () =>
                {
                    AIParkPropsManager.Inst.SetBanDoCustomAction(false);
                    _aiGame.ExitGame();
                    // _aiGame.ResetNpc2DiscussPoint();
                    
                }, () =>
                {
                    // AIParkPropsManager.Inst.SetBanDoCustomAction(false);
                    // _aiGame.OnStepChange(S11GameState.Event_Os);

                });
                break;
            case S11GuideStep.Guide_FirstInGame_Guide_2_1:
                var guide2_1 = new AIParkGuide2_1();
                guide2_1.RunGuide();
                curGuide = guide2_1;
                break;
            case S11GuideStep.Guide_FirstInGame_Guide_2_2:
                var guide2_2 = new AIParkGuide2_2();
                guide2_2.RunGuide();
                curGuide = guide2_2;
                break;
            case S11GuideStep.Guide_FirstInGame_Guide_2_3:
                var guide2_3 = new AIParkGuide2_3();
                guide2_3.RunGuide();
                curGuide = guide2_3;
                break;
            case S11GuideStep.Guide_FirstInGame_Guide_3_1:
                var guide3_1 = new AIParkGuide3_1();
                guide3_1.RunGuide();
                curGuide = guide3_1;
                break;
            case S11GuideStep.None:
                GuideOver();
                break;
        }
    }
    /// <summary>
    /// 是否处于引导第一次进pgc的大步骤流程
    /// </summary>
    /// <returns></returns>
    public bool IsDuringGuideFirstInGame()
    {
        if (!isGuide || curStep == S11GuideStep.None)
        {
            return false;
        }
        if (curStep == S11GuideStep.Guide_FirstInGame_Guide_1_1 || curStep == S11GuideStep.Guide_FirstInGame_Guide_1_2 || curStep == S11GuideStep.Guide_FirstInGame_Guide_1_3 || curStep == S11GuideStep.Guide_FirstInGame_Guide_1_4 || curStep == S11GuideStep.Guide_FirstInGame_Guide_1_5)
        {
            return true;
        }
        return false;
    }

    public bool IsDuringGuideEvent()
    {
        return curStep == S11GuideStep.Guide_FirstInGame_Guide_2_1 || curStep == S11GuideStep.Guide_FirstInGame_Guide_2_2 || curStep == S11GuideStep.Guide_FirstInGame_Guide_2_3;
    }
    /// <summary>
    /// 是否引导中 只显示秋千
    /// </summary>
    /// <param name="propName"></param>
    /// <returns></returns> <summary>
    /// 
    /// </summary>
    /// <param name="propName"></param>
    /// <returns></returns>
    public bool IsGuideJustShowSwingProp()
    {
        if (isGuide && curGuide is AIParkGuide1_3)
        {
            return true;
        }
        return false;
    }



    public void SetAllNpcGuidePos()
    {
        //         卡斯帕：跷跷板
        // 罗兰&薇薇安：坐在长椅上
        // 伊莉丝：舞台上表演
        // 皮奥&泰迪：旋转木马
        var allNpc = AIPark_CharacterManager.Inst.GetNpcDic().Values.ToList();
        int cnt = allNpc.Count;
        Dictionary<string, (ParkNpcTransData, string)> npc2PosDict = new();
        // npc2PosDict.Add(ParkNpcRoleType.Casper.ToString(), (new ParkNpcTransData(new Vector3(31.08f, 0.466f, 6.426f), new Vector3(0, 180, 0)), AIParkConfig.Anim_StateLy_Sit));
        npc2PosDict.Add(((int)ParkNpcRoleType.Elise).ToString(), (new ParkNpcTransData { pos = new Vector3(3.622f, 0.3f, -10.871f), rot = new Vector3(0, 0, 0) }, AIParkConfig.Anim_FeetOut_Sit));
        // npc2PosDict.Add(ParkNpcRoleType.Pio.ToString(), (new ParkNpcTransData(new Vector3(0, 0, 0), new Vector3(0, 0, 0)),""));
        // npc2PosDict.Add(ParkNpcRoleType.Teddy.ToString(), (new ParkNpcTransData(new Vector3(0, 0, 0), new Vector3(0, 0, 0)),""));
        npc2PosDict.Add(((int)ParkNpcRoleType.Vivien).ToString(), (new ParkNpcTransData { pos = new Vector3(5.529f, -0.21f, -1.923f), rot = new Vector3(0, 180, 0) }, AIParkConfig.Anim_StateLy_Sit));
        npc2PosDict.Add(((int)ParkNpcRoleType.Rowland).ToString(), (new ParkNpcTransData { pos = new Vector3(4.634f, -0.21f, -1.923f), rot = new Vector3(0, 180, 0) }, AIParkConfig.Anim_StateLy_Sit));
        for (int i = 0; i < cnt; i++)
        {
            var npc = allNpc[i];
            var id = npc.GetNpcID();
            if (npc2PosDict.ContainsKey(id))
            {
                var (transData, animName) = npc2PosDict[id];
                allNpc[i]._npcKccCtr.Motor.SetPositionAndRotation(transData.pos, Quaternion.Euler(transData.rot));
                allNpc[i]._npcKccCtr.Motor.enabled = false;
                allNpc[i]._npcKccCtr.Motor.GetComponent<Rigidbody>().isKinematic = true;
                allNpc[i].playerStateControllerState.EnterState(PlayerState.SingleEmote, animName, 0);
            }
        }
        //插入章节
        AIPark_CharacterManager.Inst.GetNpc(((int)ParkNpcRoleType.Casper).ToString()).GetChapterHandler().GuideForceQuickEnter(LocationType.SeeSaw, ActionType.SeeSaw);
        AIPark_CharacterManager.Inst.GetNpc(((int)ParkNpcRoleType.Pio).ToString()).GetChapterHandler().GuideForceQuickEnter(LocationType.TrojanHorse, ActionType.TrojanHorse);
        AIPark_CharacterManager.Inst.GetNpc(((int)ParkNpcRoleType.Pio).ToString()).GetChapterHandler().GuideForceQuickEnter(LocationType.TrojanHorse, ActionType.TrojanHorse);
        // AIPark_CharacterManager.Inst.GetNpc(((int)ParkNpcRoleType.Teddy).ToString()).GetChapterHandler().GuideForceQuickEnter(LocationType.Stage, ActionType.PerformOnStage);//test
    }

    public bool CheckNeedEventGuide()
    {
        bool isPgcGame = AIGameController.Inst.GetCurAIGame<AIParkGame>().isPgcEnter;
        if (!isPgcGame)
        {
            return false;
        }
        int curStep = BootDataManager.Inst.GetParkInGame();
        if (curStep != 1)
        {
            return false; //test
        }
        return true;
    }

    //事件引导部分 普通事件
    public void CheckNormalEventGuide()
    {
        bool isPgcGame = AIGameController.Inst.GetCurAIGame<AIParkGame>().isPgcEnter;
        if (!isPgcGame)
        {
            return;
        }
        var sceneIdx = AIParkUtils.Inst.ParkCustomData.SceneIndex;
        int curStep = BootDataManager.Inst.GetParkInGame();
        if (curStep != 1)
        {
            return; //test
        }
        if (sceneIdx == 1)
        {
            if (AIParkUtils.Inst.ParkGameData.events[sceneIdx - 1].EventType == 0)
            {
                AIGameController.Inst.GetCurAIGame<AIParkGame>().Is_CurPlayWithEventGuide = true;
                OnStepChange(S11GuideStep.Guide_FirstInGame_Guide_2_1);
            }
        }
        else if (sceneIdx == 2)
        {
            if (AIParkUtils.Inst.ParkGameData.events[sceneIdx - 1].EventType == 0)
            {
                AIGameController.Inst.GetCurAIGame<AIParkGame>().Is_CurPlayWithEventGuide = true;
                OnStepChange(S11GuideStep.Guide_FirstInGame_Guide_2_2);
            }
        }
    }
    //事件引导部分 选择事件
    public bool CheckSelectEventGuide()
    {
        bool isPgcGame = AIGameController.Inst.GetCurAIGame<AIParkGame>().isPgcEnter;
        if (!isPgcGame)
        {
            return false;
        }
        var sceneIdx = AIParkUtils.Inst.ParkCustomData.SceneIndex;
        int curStep = BootDataManager.Inst.GetParkInGame();

        if (curStep != 1)
        {
            return false; //test
        }
        if (sceneIdx == 3)
        {
            if (AIParkUtils.Inst.ParkGameData.events[sceneIdx - 1].EventType == 1)
            {
                return true;
            }
        }
        return false;
    }
    /// <summary>
    /// 是否引导结局
    /// </summary>
    /// <returns></returns>

    public bool CheckGuideSummaryEnd()
    {
        bool isPgcGame = AIGameController.Inst.GetCurAIGame<AIParkGame>().isPgcEnter;
        if (!isPgcGame)
        {
            return false;
        }
        var sceneIdx = AIParkUtils.Inst.ParkCustomData.SceneIndex;
        int curStep = BootDataManager.Inst.GetParkInGame();
        if (curStep != 2)
        {
            return false;  //test
        }
        return true;
    }



    //bootpanel
    public Dictionary<int, string> _guideBindPath = new()
    {
        {(int)AIGameParkConfig.EPgcGuideID.ChatBtnTips,"UIRoot/Canvas/GuestWindow/AIParkGuestPanel(Clone)/AIHospitalHUDPanel/AIHospitalNpcHudItem(Clone)/NormalView/Btn_ChatToNpc" },
        {(int)AIGameParkConfig.EPgcGuideID.CheckMapTips,"MiniMapBg"},
        {(int)AIGameParkConfig.EPgcGuideID.Touch_Hand_Swing,"UIRoot/Canvas/GuestWindow/UIOperationOnWorldPanel(Clone)/Panel/Content/TouchHandButton(Clone)" },
        {(int)AIGameParkConfig.EPgcGuideID.Event_2_1_1,"UIRoot/Canvas/GuestWindow/AIParkGuestPanel(Clone)/ParkGroup/TogGroup/CurEventBg" },
        {(int)AIGameParkConfig.EPgcGuideID.Event_2_1_2,"UIRoot/Canvas/GuestWindow/AIParkGuestPanel(Clone)/ParkGroup/RightGroup/EventTargetBg" },
        {(int)AIGameParkConfig.EPgcGuideID.Event_2_2_1,"UIRoot/Canvas/GuestWindow/AIParkGuestPanel(Clone)/ParkGroup/RightGroup/EventTargetBg" },
        {(int)AIGameParkConfig.EPgcGuideID.Event_2_3_1,"UIRoot/Canvas/GuestWindow/AIParkGameEventPanel(Clone)/BaseLayout2D/Bg/Choose/ToggleGroup" },  //暂时不用
        {(int)AIGameParkConfig.EPgcGuideID.Event_2_3_2,"UIRoot/Canvas/GuestWindow/AIParkGameEventPanel(Clone)/BaseLayout2D/Bg/Choose/ToggleGroup" },
        {(int)AIGameParkConfig.EPgcGuideID.Event_2_3_3,"UIRoot/Canvas/GuestWindow/AIParkGameEventPanel(Clone)/BaseLayout2D/Bg/Choose/ConfirmBtn" },
        {(int)AIGameParkConfig.EPgcGuideID.Event_3_1_1,"UIRoot/Canvas/GuestWindow/AIParkGameEndingPanel(Clone)/BaseLayout2D/Bg/DescTxt" },
        {(int)AIGameParkConfig.EPgcGuideID.Guide2SwingTxt,"UIRoot/Canvas/GuestWindow/AIParkGuestPanel(Clone)/ParkGroup/RightGroup/EventTargetBg" },
    };

    public DoubleSearchDictionary<AIGameParkConfig.EPgcGuideID, int> _guideConfig = new()
    {
        {AIGameParkConfig.EPgcGuideID.ChatBtnTips,130},
        {AIGameParkConfig.EPgcGuideID.CheckMapTips,131},
        {AIGameParkConfig.EPgcGuideID.Event_2_1_1,132},
        {AIGameParkConfig.EPgcGuideID.Event_2_1_2,133},
        {AIGameParkConfig.EPgcGuideID.Event_2_2_1,134},
        {AIGameParkConfig.EPgcGuideID.Touch_Hand_Swing,137},
        // {AIGameParkConfig.EPgcGuideID.Event_2_3_1,135}, //暂时不要
        {AIGameParkConfig.EPgcGuideID.Event_2_3_2,135},
        {AIGameParkConfig.EPgcGuideID.Event_2_3_3,136},
        {AIGameParkConfig.EPgcGuideID.Event_3_1_1,139},
        {AIGameParkConfig.EPgcGuideID.Guide2SwingTxt,140},
        
    };

    BootMaskMono lastBootMaskMono;
    public void BootPanelGuide(AIGameParkConfig.EPgcGuideID guideID)
    {
        if (_guideBindPath.TryGetValue((int)guideID, out string path))
        {
            var targetObj = GameObject.Find(path);
            if (targetObj != null)
            {
                var com = targetObj.GetComponent<BootMaskMono>();
                if (com == null)
                {
                    com = targetObj.AddComponent<BootMaskMono>();
                }
                lastBootMaskMono = com;
                int id = _guideConfig.TryGet(guideID, out int id1) ? id1 : 0;
                com.id = new() { id };
                UIManager.Inst.OpenPanel<BootPanel>(PanelId.BootPanel, id);
            }
        }

    }
    public void ClickGuide(int id)
    {
        // if(lastBootMaskMono != null)
        // {
        //     GameObject.Destroy(lastBootMaskMono);
        //     lastBootMaskMono = null;
        // }
        _guideConfig.TryGet(id, out AIGameParkConfig.EPgcGuideID guideID);
        MessageHelper.Broadcast(MessageName.OnS11GuideStepClick, (int)guideID, false);

    }

    public void ExitGuide()
    {
        if (isGuide && curGuide != null)
        {
            if (!CheckSelectEventGuide())
            {
                UIManager.Inst.ClosePanel(PanelId.BootPanel);
                OnStepChange(S11GuideStep.None);
            }

        }
    }
}
