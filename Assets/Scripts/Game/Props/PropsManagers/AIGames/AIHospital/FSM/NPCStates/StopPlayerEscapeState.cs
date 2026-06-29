using AIGame.Base;
using Game.Avatar;
using GameData.BaseInfo;
using GameData.Manager;
using Message;
using UIAgent;
using UnityEngine;

namespace Game.Props.PropsManagers.AIGames.AIHospital.FSM
{
    public class StopPlayerEscapeState : AIChapterState
    {
        private bool isPgcEnter = true;
        private AIHospital_CharacterBehaviour _curInjectionCharacter;
        public StopPlayerEscapeState(AIHospital_CharacterBehaviour character, AIHospital_ChapterData data) : base(character, data) { }

        private HospitalNpcTransData _transData = new HospitalNpcTransData()
        {
            pos = new Vector3(-0.866f, 1.41f, -0.028f),
            rot =  new Vector3(0,-90,0),
        };

        public override void OnEnter()
        {
            base.OnEnter();
            
            _characterManager.SetAllNpcEnable(false);

            var curMapInfo = GameDataManager.Inst.mapGlobalData.GetCurInfo<MapInfo>();
            if (curMapInfo.id != "PGC")
            {
                isPgcEnter = false;
            }

            LoggerUtils.Log($"[StopPlayerEscapeState] {_character.GetNpcName()} 进入阻止玩家逃跑的状态");
            //1.黑屏
            UIAgentManager.Inst.OpenPanel(PanelId.BlackPanel);
            MessageHelper.Broadcast(MessageName.SetOperationPanelEnable, false);
            
            //2.传送NPC到对应位置
            var mainDoorLP = new GameObject();
            mainDoorLP.transform.SetPositionAndRotation(new Vector3(-4.2f, 3.523f, 0f), Quaternion.Euler(new Vector3(5, 90, 0)));
            AIGameCameraUtils.Inst.SetCamToPos(mainDoorLP.transform, 0, 2f);
            
            AvatarController.Inst.SelfController.Motor.SetPositionAndRotation(new Vector3(-16.9f, 1.41f, -26.2f), Quaternion.Euler(new Vector3(0,90,0)));
            _character.SetPositionAndRotation(_transData.pos, Quaternion.Euler(_transData.rot));
            _character.gameObject.SetActive(true);
            TimerManager.Inst.RunOnce("OverTalk", 1f, () =>
            {
                UIAgentManager.Inst.ClosePanel(WindowId.CommonWindow, PanelId.BlackPanel);
                _character.SetHandNodeActive(false);
                //3.播放表情
                _character.PlayAnim("40200480");
                //4.做颜艺emoji并出现对话：警报！发现可疑人物强行逃离医院，马上抓去打镇静剂！
                _character.SetNpcTalk("警报！发现可疑人物强行逃离医院，马上抓去打镇静剂！");

                TimerManager.Inst.RunOnce("OnNpcFinishedEmote", 3f, () =>
                {
                    //1.黑屏
                    UIAgentManager.Inst.OpenPanel(PanelId.BlackPanel);
                    //2.设置打针摄像机
                    var injectTarget = new GameObject();
                    injectTarget.transform.SetPositionAndRotation(new Vector3(-15.072f,4.287f,-29.03f), Quaternion.Euler(new Vector3(21.94f,332.1f,-4.602f)));
                    AIGameCameraUtils.Inst.SetCamToPos(injectTarget.transform, 0, 2f);
                    
                    TimerManager.Inst.RunOnce("OnSendToInject", 1f, () =>
                    {
                        UIAgentManager.Inst.ClosePanel(WindowId.GuestWindow, PanelId.AIHospitalNpcChatPanel);
                        
                        TimerManager.Inst.RunOnce("PlayInject", 1f, () =>
                        {
                            GameObject.Destroy(mainDoorLP);
                            UIAgentManager.Inst.ClosePanel(WindowId.CommonWindow, PanelId.BlackPanel);

                            _curInjectionCharacter = isPgcEnter ? _characterManager.GetNpc(HospitalNpcRoleType.Doctor) : _character;
                            _curInjectionCharacter.gameObject.SetActive(true);
                            //1.打针动画
                            _curInjectionCharacter.EnterInterruptState<ForceInjectionState>();

                            TimerManager.Inst.RunOnce("SendToPlayer", 6f, () =>
                            {
                                //1.黑屏
                                var tips = "被打了镇静剂的你很快就睡着了，你并不知道自己睡了多久，直到在迷糊中再次醒来...";
                                UIAgentManager.Inst.OpenPanel(PanelId.TipsBlackPanel, tips);
                                AvatarController.Inst.SelfController.Motor.SetPositionAndRotation(new Vector3(-15.876f, 1.41f, -28), Quaternion.Euler(new Vector3(0, 90, 0)));
                                AvatarController.Inst.SelfStateController.ExitState(PlayerState.DoubleEmote);
                                AIGameCameraUtils.Inst.BackToPlayer();

                                TimerManager.Inst.RunOnce("BackToPlayer", tips.Length * 0.1f + 1, () =>
                                {
                                    GameObject.Destroy(injectTarget);
                                    //1.黑屏
                                    UIAgentManager.Inst.ClosePanel(WindowId.CommonWindow, PanelId.TipsBlackPanel);
                                    MessageHelper.Broadcast(MessageName.SetOperationPanelEnable, true);
                                    //流程结束，退出状态
                                    _curInjectionCharacter.RecalculateChapter();
                                    _character.RecalculateChapter();
                                    _character.SetHandNodeActive(true);
                                    _characterManager.SetAllNpcEnable(true);

                                    if (isPgcEnter && PlayerPrefs.GetInt(AccountDataManager.Inst.Uid + AIGameHospitalConfig.pgcGuideId) < (int)AIGameHospitalConfig.EPgcGuideID.HeartTips)
                                    {
                                        MessageHelper.Broadcast(MessageName.OnS9GuideStepAction, AIGameHospitalConfig.EGuideAction.ShowHeartTips);
                                    }
                                    if (isPgcEnter && _character._isInitialized == false)
                                    {
                                        if (_character.GetNpcRoleType() == HospitalNpcRoleType.Doctor)
                                        {
                                            var SpawnPointData = AIHospital_CharacterUtils.Inst.GetSpawnPointByRoleType(HospitalNpcRoleType.Doctor);
                                            _character.SetPositionAndRotation(SpawnPointData.pos, Quaternion.Euler(SpawnPointData.rot));
                                        }
                                    }
                                });
                            });
                        });
                    });
                });
            });
        }

        private void PlayInjectAnim()
        {
            
        }

        public override void OnExit()
        {
            base.OnExit();
            LoggerUtils.Log($"[StopPlayerEscapeState] {_character.GetNpcName()} 退出阻止玩家逃跑的状态");
            _character.SetHandNodeActive(true);
        }

        public override void OnUpdate(float deltaTime)
        {
        }
        
        
    }
} 