using System;
using System.Collections;
using System.Collections.Generic;
using Basic.Utils;
using Es;
using Game.Audio;
using Game.Avatar;
using Game.Base;
using Game.KinematicCharacter;
using Game.MapSetting;
using Game.Props.PropsBehaviours;
using Game.Props.PropsManagers;
using Game.Scene.EnterModelController;
using GameData;
using GameData.BaseInfo;
using GameData.Manager;
using GameData.UGCData;
using GameSync.Manager;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UIAgent;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AIGame.Base
{
    public class AIYandereGame : BaseAIGame
    {
        
        public class GameResultReq
        {
            public string npcId;
            public int gameId;
            public int result;
            public int duration;
            public string conversationId;
        }
        
        private AIYandereEnterModelController _yandereEnterModelController;
        private const string TAG = "AIYandereGame";
        private AIGameGuestPanel _gameGuestPanel;
        private bool _isDoorOpen;//门是否被打开
        private bool _isStartGame = false;
        public YandereStep _curStep { get;private set; }
        
        public int CurAreaIndex = 0;
        private YandereAreaType curAreaType = YandereAreaType.Unknown;
        
        private long curGameTime = 0;
        private bool IsGoodEnd_2 = false;
        private bool isPgcEnter = true;
        private AINpcInfo npcInfo;
        #region 游戏初始化与生命周期
        public override void OnInitByCreate()
        {
            base.OnInitByCreate();
            YandereDataManager.Inst.Init();
            _yandereEnterModelController = GameController.GetEnterModelController<AIYandereEnterModelController>();
        }

        public override void OnStart()
        {
            base.OnStart();
            GetSceneNodeBev();
            _gameGuestPanel = UIManager.Inst.FindPanel<AIGameGuestPanel>(WindowId.GuestWindow, PanelId.AIGameGuestPanel);
            //1.关闭相关UI
            UIManager.Inst.ClosePanel(PanelId.AIYandereStartPanel);
            //2.调整游戏状态
            npc.gameObject.SetActive(true);
            
            npcInfo = GameDataManager.Inst.mapGlobalData.GetCurInfo<AINpcInfo>();
            isPgcEnter = npcInfo.id.Equals("0");
            _gameGuestPanel.IsPgcEnter = isPgcEnter;
            OnStepChange(YandereStep.GameDesc);
            SetComputerInteractive(isPgcEnter);
        }

        public override void Restart()
        {
            base.Restart();
            GameController.ExitGame(() =>
            {
                SimpleGameDurationManager.Inst.Release();
                UIManager.Inst.ForceSetOtherWindowTransInStack(WindowId.GuestWindow, true);
                UIManager.Inst.ClosePanel(PanelId.UIOperationOnWorldPanel);
                UIManager.Inst.ClosePanel(PanelId.AIYandereGameOverPanel);
                UIManager.Inst.BackToLastWindow();
                UIManager.Inst.OpenPanel(PanelId.BlackPanel);
                var npcInfo = GameDataManager.Inst.mapGlobalData.GetCurInfo<AINpcInfo>();
                GameController.StartAIGame(npcInfo, EnterGameModel.AIYandere, true, "AIYandere");
            });
        }

        protected override void AddListener()
        {
            base.AddListener();
            MessageHelper.AddListener<AIYandereCharacterBehaviour>(MessageName.OnAINPCTouchClick, OnAINPCTouchClick);
            MessageHelper.AddListener(MessageName.PassLevelJudging, DoJudging);
        }

        protected override void RemoveListener()
        {
            base.RemoveListener();
            MessageHelper.RemoveListener<AIYandereCharacterBehaviour>(MessageName.OnAINPCTouchClick, OnAINPCTouchClick);
            MessageHelper.RemoveListener(MessageName.PassLevelJudging, DoJudging);
        }

        private IEnumerator HandelOpenMainDoor()
        {
            _gameGuestPanel.HidePanel();
            AIGameCameraUtils.Inst.SetCamToPos(this.mainDoor.camTransform, 1, 3);
            yield return new WaitForSeconds(1);
            
            this.mainDoor.HandOpenDoor();
            this._isDoorOpen = true;
            _gameGuestPanel.ChangeTarget();
            yield return new WaitForSeconds(2);
            
            AIGameCameraUtils.Inst.BackToPlayer(1);
            
            yield return new WaitForSeconds(1);
            
            //TODO 只开启操作UI
            _gameGuestPanel.ShowPanel();
        }
        #endregion
        
        #region 游戏内数据及节点
        //AINpc
        private AIYandereCharacterBehaviour npc;
        //大门
        private AIYandereMainDoorBehaviour mainDoor;
        //逃跑目标点
        private AIYandereRunTargetPointBehaviour runTargetBev;
        //目标点
        private AIYandereTargetPointBehaviour targetBev;
        private List<AIYandereTriggerAreaBehaviour> trigAreaBevs;
        //普通门
        private List<AIYandereDoorBehaviour> normalDoorBevs;
        private AIRequestChatData _sendPosGameData = new AIRequestChatData();
        private List<AIPropBaseBehaviour> _curAINodeList = new List<AIPropBaseBehaviour>();
        
        private void GetSceneNodeBev()
        {
             var light = DirLightManager.Inst.GetGlobalDirLight();
             light.enabled = false;

             var terrainMgr = GlobalNodeManager.Inst.Get<TerrainManager>();
             var terrain = terrainMgr.GetTerrain();
             terrain.gameObject.SetActive(false);
             
             var skyboxData =  DataTables.GetNormalSkyboxData("10010");
             SkyboxManager.Inst.SetNormalSkyboxTexture(skyboxData,null);
             // 设置 环境光
             RenderSettings.ambientSkyColor = Color.white;
             RenderSettings.ambientEquatorColor = new Color(1f, 0.9869f, 0.8915f);
             RenderSettings.ambientGroundColor = new Color(0.9150f, 0.6706f, 0.6431f);
             
            var npcMgr = GlobalNodeManager.Inst.Get<AIYandereCharacterManager>();
            this.npc = npcMgr.GetNpcBev();
            
            var mainDoorMgr = GlobalNodeManager.Inst.Get<AIYandereMainDoorManager>();
            this.mainDoor = mainDoorMgr.GetYandereMainDoorBev();
            this.mainDoor.SetFunctions(CheckStartGame);
            this.mainDoor.SetSendPosAct(SendPos);
            _curAINodeList.Add(this.mainDoor);
            
            var sofaMgr = GlobalNodeManager.Inst.Get<AIYandereSofaManager>();
            var sofas = sofaMgr.GetSofaBevs();
            sofas.ForEach(x =>
            {
                x.SetFunctions(CheckStartGame);
                x.SetSendPosAct(SendPos);
            });
            _curAINodeList.AddRange(sofas);
            
            var lightMgr = GlobalNodeManager.Inst.Get<AIYandereLightManager>();
            var lights = lightMgr.GetSofaBevs();
            lights.ForEach(x =>
            {
                x.SetFunctions(CheckStartGame);
            });
            _curAINodeList.AddRange(lights);
            
            var tvMgr = GlobalNodeManager.Inst.Get<AIYandereTVManager>();
            var tvBehaviour = tvMgr.GetBev();
            tvBehaviour.SetFunctions(CheckStartGame);
            _curAINodeList.Add(tvBehaviour);
            
            var cMgr = GlobalNodeManager.Inst.Get<AIYandereComputerManager>();
            var cBehaviour = cMgr.GetBev();
            cBehaviour.SetFunctions(CheckStartGame);
            cBehaviour.SetPlayComputer(PlayComputer);
            _curAINodeList.Add(cBehaviour);
            
            var bathRoomMgr = GlobalNodeManager.Inst.Get<AIYandereBathRoomManager>();
            var bathBehaviour = bathRoomMgr.GetBev();
            bathBehaviour.SetFunctions(CheckStartGame);
            bathBehaviour.SetSendPosAct(SendPos);
            _curAINodeList.Add(bathBehaviour);
            
            var normalDoorMgr = GlobalNodeManager.Inst.Get<AIYandereDoorManager>();
            normalDoorBevs = normalDoorMgr.GetDoorBevs();
            normalDoorBevs.ForEach(x =>
            {
                x.SetFunctions(CheckStartGame);
            });
            _curAINodeList.AddRange(normalDoorBevs);
            
            var runTargetMgr = GlobalNodeManager.Inst.Get<AIYandereRunTargetManager>();
            this.runTargetBev = runTargetMgr.GetRunTargetPointBev();
            this.runTargetBev.SetAction(OnTrigEnterRunTargetPoint);
            _curAINodeList.Add(runTargetBev);
            
            var targetPointMgr = GlobalNodeManager.Inst.Get<AIYandereTargetPointManager>();
            targetBev = targetPointMgr.GetargetPointBev();
            targetBev.SetAction(SuccessfullyEscaped);
            _curAINodeList.Add(targetBev);
            
            var triggerAreaMgr = GlobalNodeManager.Inst.Get<AIYandereTriggerAreaManager>();
            trigAreaBevs = triggerAreaMgr.GetTriggerAreaBevs();
            trigAreaBevs.ForEach(x =>
            {
                x.SetSendPosAct(SendPos);
            });
            _curAINodeList.AddRange(trigAreaBevs);
        }

        public void SetComputerInteractive(bool interactive)
        {
            var cMgr = GlobalNodeManager.Inst.Get<AIYandereComputerManager>();
            var cBehaviour = cMgr.GetBev();
            cBehaviour.IsCanClick = interactive;
        }

        public void SetCanInteractive(bool interactive)
        {
            for (var i = 0; i < _curAINodeList.Count; i++)
            {
                _curAINodeList[i].IsCanClick = interactive;
            }
        }

        public void PlayComputer()
        {
            var computerPanel = UIManager.Inst.OpenPanel<AIShowComputerPanel>(PanelId.AIShowComputerPanel);
            computerPanel.OnClose = () =>
            {
                var cMgr = GlobalNodeManager.Inst.Get<AIYandereComputerManager>();
                var cBehaviour = cMgr.GetBev();
                if (cBehaviour != null)
                {
                    cBehaviour.CancelEmoteOnUI();
                }
            };
        }

        private void SendPos(YandereAreaType areaType)
        {
            SendPos(areaType, 0);
        }

        private void SendPos(YandereAreaType areaType,int index)
        {
            if(!_isStartGame)
                return;

            if (areaType != YandereAreaType.OpenDoor)
            {
                int range = Random.Range(0, 10);
                if (range > 7)
                {
                    return;
                }
            }
            _sendPosGameData.location = (int) areaType;
            var gameData = JsonConvert.SerializeObject(_sendPosGameData);
            _gameGuestPanel.SendMsgToSeverForOut(gameData, null, OnSendOpToSeverFail);
            this.CurAreaIndex = index;
            this.curAreaType = areaType;
        }
        private void OnTrigEnterRunTargetPoint()
        {
            if(!_isDoorOpen)
                return;
            OnStepChange(IsGoodEnd_2? YandereStep.GoodEnd_2:YandereStep.Run);
        }
        #endregion
        
        public void OnStepChange(YandereStep gameStep)
        {
            switch (gameStep)
            {
                case YandereStep.GameDesc:
                    StartDesc();
                    break;
                case YandereStep.GameStart:
                    OnStepChange(YandereStep.Play);
                    break;
                case YandereStep.Play:
                    StartGame();
                    break;
                case YandereStep.GameNpcSelect:
                    break;
               
               
                case YandereStep.Run:
                    SimpleGameDurationManager.Inst.OnPassLevelStop();
                    _gameGuestPanel.HideUIBtnClick();
                    CoroutineManager.Inst.StartCoroutine(RunBefore(NpcStartRun));
                    break;
                case YandereStep.BadEnd_1:
                    if (!IsRunOrGameOver())
                    {
                        YandereDataManager.Inst.FinalResult = 0;
                        CoroutineManager.Inst.StartCoroutine(PlayBadEnd_1());
                    }
                    break;
                case YandereStep.BadEnd_2:
                    if (!IsRunOrGameOver())
                    {
                        YandereDataManager.Inst.FinalResult = 1;
                        CoroutineManager.Inst.StartCoroutine(PlayBadEnd_2());
                    }
                    break;
                case YandereStep.BadEnd_3:
                    if (!IsGameOver())
                    {
                        YandereDataManager.Inst.FinalResult = 2;
                        PlayBadEnd_3();
                    }

                    break;
                case YandereStep.GoodEnd_1:
                    if (!IsGameOver())
                    {
                        YandereDataManager.Inst.FinalResult = 3;
                        PlayGoodEnd_1();
                    }

                    break;
                case YandereStep.GoodEnd_2:
                    if (!IsGameOver())
                    {
                        YandereDataManager.Inst.FinalResult = 4;
                        CoroutineManager.Inst.StartCoroutine(PlayGoodEnd_2());
                    }
                    break;

            }
            _curStep = gameStep;
        }
        
        private void StartDesc(string name = "")
        {
            AIGameCameraUtils.Inst.SetCamToPos(npc.CamFollowCenter, 0, 5f);
            _isDoorOpen = false;
            npc.ReSetNpc();
            npc.PlayIdleAnim();
            npc.gameObject.SetActive(true);
            
            //隐藏各种panel
            SetSelfPlayerState(false);
            _gameGuestPanel.HidePanel();

            if (!YandereDataManager.Inst.IsTryAgain)
            {
                if (isPgcEnter)
                {
                    EnterGuidePanel();
                    return;
                }
                
                var loadingPanel = UIManager.Inst.FindPanel<AIGameLoadingPanel>(WindowId.CommonWindow,PanelId.AIGameLoadingPanel);
                if (loadingPanel != null)
                {
                    loadingPanel.OnComplete = EnterGuidePanel;
                }
            }
            else
            {
                if (isPgcEnter)
                {
                    OnGuideOverAndStartPlay();
                    return;
                }
                
                var loadingPanel = UIManager.Inst.FindPanel<AIGameLoadingPanel>(WindowId.CommonWindow,PanelId.AIGameLoadingPanel);
                if (loadingPanel != null)
                {
                    loadingPanel.OnComplete = OnGuideOverAndStartPlay;
                }
            }
        }

        private void EnterGuidePanel()
        {
            AIGameSoundUtils.Inst.StopBgm(YandereConfig.BGM_START);
            var guidePanel = UIManager.Inst.OpenPanel<AIYandereGuidePanel>(PanelId.AIYandereGuidePanel,isPgcEnter,npcInfo.name);
            guidePanel.EnterGame = OnGuideOverAndStartPlay;
        }

        //引导完成并开始游戏
        private void OnGuideOverAndStartPlay()
        {
            SetSelfPlayerState(true);
            UIManager.Inst.ClosePanel(PanelId.PGCGameGuidePanel);
            AIGameSoundUtils.Inst.PlayBgm(YandereConfig.BGM_PLAY);
            _gameGuestPanel.ShowPanel();
            OnStepChange(YandereStep.GameStart);
        }

        private void StartGame()
        {
            AIGameCameraUtils.Inst.BackToPlayer();
            
            _isStartGame = true;
            _isDoorOpen = false;
            
            npc.ReSetNpc();
            npc.PlayIdleAnim();
            _gameGuestPanel.ShowPanel();
            SimpleGameDurationManager.Inst.Release();
            SimpleGameDurationManager.Inst.SetDuration(new GameDurationData {
                duration = YandereConfig.GameTime
            });
            _gameGuestPanel.LockInput();
            SimpleGameDurationManager.Inst.CountDownStart(_gameGuestPanel.StartNpcFirstTalk);
            curGameTime =  GameUtils.GetTimeStamp();
            AIGameSoundUtils.Inst.PlaySound(YandereConfig.YE_Countdown_Start);
        }

        #region 结局处理
        private IEnumerator RunBefore(Action action)
        {
            _gameGuestPanel.m_joyStick.OnResetJoystick();
            _gameGuestPanel.m_joyStick.UpdateCharacterInputs();
            var root = _gameGuestPanel.m_joyStick.transform.parent.gameObject;
            root.SetActive(false);
            SimpleGameDurationManager.Inst.CountDownPause();
            AIGameSoundUtils.Inst.StopAllBgm();
            UIManager.Inst.OpenPanel(PanelId.BlackPanel, true);
            yield return new WaitForSeconds(0.5f);
            AIGameSoundUtils.Inst.PlaySound(YandereConfig.SOUND_KNIFE);
            npc.gameObject.SetActive(true);
            npc.SetNpcToSpawnPoint();
            AIGameCameraUtils.Inst.SetCamToPos(npc.CamFollowCenter, 0, 2f);
            string content = isPgcEnter ? "给你开门你还真敢出去？看我揍扁你！！！" : "你为什么不愿意和我一起永远呆在这里！！！";
            _gameGuestPanel.IsCreateDialog = true;
            _gameGuestPanel.SetNpcTalk(content,true,true,true);
            _gameGuestPanel.SetNpcDialogLocalPos(new Vector3(0,1.7f,0));
            npc.ChangeAttackState();
            UIManager.Inst.ClosePanel(PanelId.BlackPanel);
            string killAnimName = npcInfo.npcGender == 1 ? YandereConfig.YE_YanderekillerMan2 :  YandereConfig.YE_Yanderekiller2;
            AIGameSoundUtils.Inst.PlaySound(killAnimName); 
            yield return new WaitForSeconds(2f);
            UIManager.Inst.OpenPanel(PanelId.BlackPanel, true);
            yield return new WaitForSeconds(0.5f);
            root.SetActive(true);
            AIGameCameraUtils.Inst.BackToPlayer();
            UIManager.Inst.ClosePanel(PanelId.BlackPanel);
            yield return new WaitForSeconds(1.5f);
            action?.Invoke();
        }

    
        private IEnumerator PlayBadEnd_1()
        {
            SetSelfPlayerState(false);
            AIGameSoundUtils.Inst.StopAllBgm();
            npc.SetNpcToSpawnPoint();
            AIGameCameraUtils.Inst.SetCamToPos(npc.CamFollowCenter, 1, 2f);
            yield return new WaitForSeconds(2f);
            npc.PlayLaughAnim();
            yield return new WaitForSeconds(1.5f);
            AIGameSoundUtils.Inst.PlayBgm(YandereConfig.BGM_TOGETHER_FOREVER);
            ShowGameOverPanel(YandereStep.BadEnd_1);
        }
        
        private IEnumerator PlayBadEnd_2()
        {
            SetSelfPlayerState(false);
            _gameGuestPanel.HidePanel();
            npc.SetNpcToSpawnPoint();
            AIGameCameraUtils.Inst.SetCamToPos(npc.CamFollowCenter, 1, 2f);
            yield return new WaitForSeconds(2f);
            AIGameSoundUtils.Inst.StopAllBgm();
            AIGameSoundUtils.Inst.PlaySound(YandereConfig.SOUND_KNIFE);
            npc.PlayKillerAnim();
            yield return new WaitForSeconds(5f);
            AIGameSoundUtils.Inst.StopSound(YandereConfig.SOUND_KNIFE);
            ShowGameOverPanel(YandereStep.BadEnd_2);
        }
        private void PlayBadEnd_3()
        {
            SetSelfPlayerState(false);
            _gameGuestPanel.HidePanel();
            AIGameSoundUtils.Inst.StopSound(YandereConfig.SOUND_HEART_BEAT);
            ShowGameOverPanel(YandereStep.BadEnd_3);
        }
        private void PlayGoodEnd_1()
        {
            AIGameSoundUtils.Inst.StopSound(YandereConfig.SOUND_KNIFE);
            AIGameSoundUtils.Inst.StopSound(YandereConfig.SOUND_HEART_BEAT);
            AIGameSoundUtils.Inst.PlayBgm(YandereConfig.BGM_ESCAPED);
            SimpleGameDurationManager.Inst.RecordPlayTimeCost();
            _gameGuestPanel.HidePanel();
            
            ShowGameOverPanel(YandereStep.GoodEnd_1);
        }
        private IEnumerator PlayGoodEnd_2()
        {
            AIGameSoundUtils.Inst.PlayBgm(YandereConfig.BGM_SWEET_WAITING);
            SimpleGameDurationManager.Inst.RecordPlayTimeCost();
            _gameGuestPanel.HidePanel();
            yield return new WaitForSeconds(0.5f);
            ShowGameOverPanel(YandereStep.GoodEnd_2);
        }
        

        #endregion
        
        public void NpcStartRun()
        {
            _gameGuestPanel.SetScreenEffect(true);
            npc.StartRunAfter(20f, 1.5f, (isCatch) => OnStepChange(YandereStep.BadEnd_3));
            AIGameSoundUtils.Inst.PlaySound(YandereConfig.SOUND_HEART_BEAT);
        }

        private void ShowGameOverPanel(YandereStep yandereStep)
        {
            if (UIManager.Inst.FindPanel(PanelId.CameraModePanel))
            {
                UIManager.Inst.ClosePanel(PanelId.CameraModePanel);
            }
            ReportThinkingData(yandereStep);
            var dur = GameUtils.GetTimeStamp() - curGameTime;
            UIManager.Inst.OpenPanel(PanelId.AIYandereGameOverPanel, yandereStep, dur, _gameGuestPanel.curConversationId);
        }

        public void ReportThinkingData(YandereStep yandereStep)
        {
            Dictionary<string, object> trackData = new Dictionary<string, object>();
            if (curGameTime == 0)
            {
                Debug.LogError("Yandere GameTime == 0");
                return;
            }
            var dur = GameUtils.GetTimeStamp() - curGameTime;
            trackData.Add("userId",AccountDataManager.Inst.UserInfo?.uid);
            trackData.Add("gameMode", isPgcEnter ? "PGC" : "UGC");
            trackData.Add("npdId", npcInfo.id);
            trackData.Add("gameTime", dur);
            int result = YandereDataManager.Inst.GetResult(yandereStep);
            trackData.Add("result", result);
            trackData.Add("chat",_gameGuestPanel.ChatCount);
            trackData.Add("gameId",(int) PGCGameType.AIYandere);
            AnalyticsManager.Inst.Track(AnalyticsEventName.AIYANDERE_DATA,trackData);
        }


        public bool IsRunOrGameOver()
        {
            return _curStep == YandereStep.Run || IsGameOver();
        }
        
        public bool IsGameOver()
        {
            return _curStep == YandereStep.BadEnd_1 ||
                   _curStep == YandereStep.BadEnd_3 ||
                   _curStep == YandereStep.BadEnd_2 ||
                   _curStep == YandereStep.GoodEnd_2 ||
                   _curStep == YandereStep.GoodEnd_1;
        }
        
 

        public bool IsOpenDoor()
        {
            return _isDoorOpen;
        }

        /// <summary>
        /// 判断是否开始游戏，若为开始
        /// </summary>
        /// <returns></returns>
        public bool CheckStartGame(bool isToast = true)
        {
            if (isToast && !_isStartGame)
            {
                TipPanel.ShowToast("请等待游戏开始");
            }
            return _isStartGame;
        }
        
        private void OnSendOpToSeverFail()
        {
            LoggerUtils.LogError("AIYandereGame OnSendOpToSeverFail");
        }

        #region 游戏逻辑
        private void SetSelfPlayerState(bool state)
        {
            AvatarController.Inst.SelfStateController.gameObject.SetActive(state);
        }
        
        //打开主门
        public void OpenMainDoor()
        {
            //数据校验
            if(_isDoorOpen)
                return;
            
            CoroutineManager.Inst.StartCoroutine(HandelOpenMainDoor());
        }
        
        private void OnAINPCTouchClick(AIYandereCharacterBehaviour aiYandereCharacterBehaviour)
        {
            AIGameCameraUtils.Inst.SetCamToPos(this.npc.CamFollowCenter, 0, 2f);
        }

        private void DoJudging()
        {
            LoggerUtils.LogError("逃脱倒计时结束");

            //数据校验
            if (IsGameOver())
            {
                return;
            }

            _isStartGame = false;
            _gameGuestPanel.HidePanel();
            npc.ForceStopFollow();
            SimpleGameDurationManager.Inst.OnPassLevelStop();
            OnStepChange(YandereStep.BadEnd_1);
        }

        public void SuccessfullyEscaped()
        {
            OnStepChange(YandereStep.GoodEnd_1);
        }

        //游戏进度检测
        public void GameStepCheck()
        {
            var result = YandereDataManager.Inst.GetResult();
            if (result > 0)
            {
                npc.StopSpeakAnim();
                if (_gameGuestPanel != null)
                {
                    _gameGuestPanel.SetDoorProgressVisible(false);
                }
                var curStep = YandereStep.None;
                var curState = (YandereEndState) result;
                switch (curState)
                {
                    case YandereEndState.BadEnd_2:
                        IsGoodEnd_2 = false;
                        SetCanInteractive(false);
                        curStep = YandereStep.BadEnd_2;
                        break;
                    case YandereEndState.OpenDoor:
                        IsGoodEnd_2 = false;
                        OpenMainDoor();
                        break;
                    case YandereEndState.GoodEnd_2:
                        IsGoodEnd_2 = true;
                        OpenMainDoor();
                        break;
                }
                if (curStep != YandereStep.None)
                    OnStepChange(curStep);
            }
        }
        #endregion
    }
}