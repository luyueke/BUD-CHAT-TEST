using Basic.Utils;
using Game.Avatar;
using Game.Base;
using Game.Props;
using Game.Props.PropsManagers;
using Game.Props.PropsManagers.AIGames.AIHospital.FSM;
using GameData.BaseInfo;
using GameData.Manager;
using Message;
using System;
using System.Collections.Generic;
using System.Linq;
using Game.Event;
using Game.MapSetting;
using UnityEngine;

namespace AIGame.Base
{
    public class AIHospitalGame : BaseAIGame
    {
        private const string TAG = "AIHospitalGame";
        public bool Is_FirstPlay = true;

        /// <summary>
        /// 当前任务的步骤id
        /// </summary>
        [SerializeField] private int _currentStep;
        public int CurrentStep
        {
            get { return _currentStep; }
            set
            {
                if (isPgcEnter)
                {
                    if (pgcTarget[_currentStep].role != HospitalNpcRoleType.Default)
                    {
                        var oldCharacterBehaviour = AIHospital_CharacterManager.Inst.GetNpc(AIGameHospitalConfig.taskTarget[_currentStep].targetRole);
                        oldCharacterBehaviour?.SetIsCurrentTarget(false);
                    }
                    LoggerUtils.Log($" stepchanged {_currentStep} = {value}");
                    _currentStep = value;
                    if (pgcTarget[_currentStep].role != HospitalNpcRoleType.Default)
                    {
                        var newCharacterBehaviour = AIHospital_CharacterManager.Inst.GetNpc(AIGameHospitalConfig.taskTarget[_currentStep].targetRole);
                        newCharacterBehaviour?.SetIsCurrentTarget(true);
                        LoadEffectAndSetParent(newCharacterBehaviour);
                        IsSpecialStep(value);
                    }
                    else
                        OnDisableEffect();
                }
                else
                {

                }
                _gameGuestPanel?.SetTarget(CurrentStep);
            }
        }

        private AIHospitalGuestPanel _gameGuestPanel;

        private bool _isStartGame = false;

        private long curGameTime = 0;

        public bool isPgcEnter = true;
        private int _costTime = 0;
        /// <summary>
        /// 当局游戏_hp
        /// </summary>
        private int _hp;

        public int Hp
        {
            get { return _hp; }
            set
            {
                LoggerUtils.Log($"尝试修改AIHospitalGame的 当局血量  当前步骤{_hp} => {value}");
                _hp = value;
            }
        }

        private int _pwd;

        public int Pwd
        {
            get { return _pwd; }
            set
            {
                LoggerUtils.Log($"尝试修改AIHospitalGame的 当局密码  当前步骤{_pwd} => {value}");
                _pwd = value;
            }
        }

        public string CurMapID { get; private set; }

        #region 特效资源
        private string _effectPath = "Assets/Loadable/Avatar/CharacterBody/CallNpcEffect/s9npcSelect/s9AINpcSelected.prefab";
        #endregion
        [SerializeField] private GameObject _effectObj;

        public GameSetting GameSetting { get; private set; }
        

        /// <summary>
        /// ugc目标
        /// </summary>
        private Dictionary<string, S9UgcTaskData> ugcTarget = new Dictionary<string, S9UgcTaskData>();

        /// <summary>
        /// pgc任务目标
        /// </summary>
        private List<S9PgcTaskData> pgcTarget = new List<S9PgcTaskData>();

        /// <summary>
        /// 游戏是否通关
        /// </summary>
        private bool _GamePassed = false;
        public override void OnStart()
        {
            //游戏开始
            base.OnStart();
            InitGameData();
            
            //两个模式都没玩过才算初次游玩
            var isFirstPlayUgc = PlayerPrefs.GetInt(AccountDataManager.Inst.Uid + "_" + "PlayS9Ugc", 0) != 1;
            var isFirstPlayPgc = PlayerPrefs.GetInt(AccountDataManager.Inst.Uid + "_" + "PlayS9Pgc", 0) != 1;
            Is_FirstPlay = isFirstPlayUgc && isFirstPlayPgc;

            var curMapInfo = GameDataManager.Inst.mapGlobalData.GetCurInfo<MapInfo>();
            GameSetting = curMapInfo.gameSetting;
            if (curMapInfo.id == "PGC")
            {
                pgcTarget = AIGameHospitalConfig.taskTarget
                    .Where(x => x is not null)
                    .Select(x => new S9PgcTaskData()
                    {
                        role = x.targetRole,
                        isFinish = false,
                    })
                    .ToList();
                isPgcEnter = true;
                //标记游玩过PGC
                PlayerPrefs.SetInt(AccountDataManager.Inst.Uid + "_" + "PlayS9Pgc", 1);
            }
            else
            {   
                
                AIGameHospitalConfig.hasTimeLimited = GameSetting.timeLimited == 1;
                AIGameHospitalConfig.gameDuration = GameSetting.limitDuration;
                AIGameHospitalConfig.defaultHp = GameSetting.limitHp == 0 ? 9999 : GameSetting.limitHp;
                AIGameHospitalConfig.ugcBgmUrl = GameSetting.bgMusicUrl;
                ugcTarget = GameSetting.aIGameConfig.hospitalNPCs
                    .Where(x => x.role == (int)HospitalNPCType.Provost) // 筛选role为7(Provost)的数据
                    .ToDictionary(
                        x => x.id, // 使用npcId作为字典key
                        x => new S9UgcTaskData()
                        {
                            npcData = x, // 使用整个NPC数据作为value
                            isFinish = false,
                        }
                    );
                isPgcEnter = false;
                CurMapID = curMapInfo.id;
                //标记游玩过PGC
                PlayerPrefs.SetInt(AccountDataManager.Inst.Uid + "_" + "PlayS9Ugc", 1);
            }

            //初始化
            AIHospital_PropsManager.Inst.InitData();
            AIHospital_PropsManager.Inst.InitPhoto(curMapInfo.gameSetting.aIGameConfig.hospitalPhotos);
            TalkStateUtils.Inst.InitData();
            if (AIHospitalUtils.Inst.HospitalGameData != null)
            {
                AIHospital_CharacterManager.Inst.InitSceneAICharacter(AIHospitalUtils.Inst.HospitalGameData.hospitalScript, isPgcEnter);
            }
            _gameGuestPanel = UIManager.Inst.FindPanel<AIHospitalGuestPanel>(WindowId.GuestWindow, PanelId.AIHospitalGuestPanel);
            _gameGuestPanel.IsPgcEnter = isPgcEnter;
            //PlayerPrefs.SetInt(AccountDataManager.Inst.Uid + AIGameHospitalConfig.pgcGuideId, -1);
            OnStepChange(S9GameState.Ready);
            CurrentStep = 0;
            LoadEffectForUgcNpc(Vector3.zero);
            if (AIGameHospitalConfig.defaultHp == AIGameHospitalConfig.maxHp)
            {
                _gameGuestPanel.HideHeart();
            }
            if (!isPgcEnter)
            {
                MessageHelper.Broadcast(MessageName.OnS9UgcMainDoorLock);
            }
            //OnGameSuccess();
        }

        public override void Restart()
        {
            base.Restart();
            AIGameSoundUtils.Inst.StopAllSound();
            var curMapId = GameDataManager.Inst.mapGlobalData.GetCurInfo<MapInfo>().id;
            GameController.ExitGame(() =>
            {
                SimpleGameDurationManager.Inst.Release();
                UIManager.Inst.ForceSetOtherWindowTransInStack(WindowId.GuestWindow, true);
                UIManager.Inst.ClosePanel(PanelId.UIOperationOnWorldPanel);
                UIManager.Inst.ClosePanel(PanelId.AIHospitalEndPanel);
                UIManager.Inst.BackToLastWindow();
                UIManager.Inst.OpenPanel(PanelId.BlackPanel);

                if (isPgcEnter)
                {
                    AIHospitalUtils.Inst.EnterOfficalHospitalGame();
                }
                else
                {
                    AIHospitalUtils.Inst.EnterUgcHospitalGame(curMapId);
                }
            });
        }

        private void InitGameData()
        {
            AIGameHospitalConfig.Reset();
            Hp = AIGameHospitalConfig.defaultHp;
            Pwd = AIGameHospitalConfig.defaultPwd;
            GameAINpcChatManager.Inst.ClearAllConversitionID();
            GameAINpcChatManager.Inst.ClearGuideCache();
            ugcTarget.Clear();
            pgcTarget.Clear();
            EventCenterDataManager.Inst.ReportTask(PostEventId.PlayAIGame);
        }

        protected override void AddListener()
        {
            base.AddListener();
            MessageHelper.AddListener<Action>(MessageName.OnS9MainDoorClick, OnMainDoorClick);
            MessageHelper.AddListener(MessageName.PassLevelJudging, DoJudging);
            MessageHelper.AddListener(MessageName.OnS9DustManChangeClothes, OnDustManChangeClothes);
            MessageHelper.AddListener(MessageName.OnS9TargetAreaTrigEnter, OnTargetAreaTrigEnter);
            MessageHelper.AddListener(MessageName.OnS9MainDoorOpen, UpdatePgcOpenDoorState);
            MessageHelper.AddListener(MessageName.OnS9UgcTargetFinished, OnS9UgcTargetFinished);
            //MessageHelper.AddListener<string, string, bool>(MessageName.OnS9EmoteWithMsg,OnS9EmoteWithMsg);
            MessageHelper.AddListener(MessageName.OnHospitalGameFail, OnHospitalGameFail);
        }

        protected override void RemoveListener()
        {
            base.RemoveListener();
            MessageHelper.RemoveListener<Action>(MessageName.OnS9MainDoorClick, OnMainDoorClick);
            MessageHelper.RemoveListener(MessageName.PassLevelJudging, DoJudging);
            MessageHelper.RemoveListener(MessageName.OnS9DustManChangeClothes, OnDustManChangeClothes);
            MessageHelper.RemoveListener(MessageName.OnS9TargetAreaTrigEnter, OnTargetAreaTrigEnter);
            MessageHelper.RemoveListener(MessageName.OnS9MainDoorOpen, UpdatePgcOpenDoorState);
            MessageHelper.RemoveListener(MessageName.OnS9UgcTargetFinished, OnS9UgcTargetFinished);
            //MessageHelper.RemoveListener<string, string, bool>(MessageName.OnS9EmoteWithMsg, OnS9EmoteWithMsg);
            MessageHelper.RemoveListener(MessageName.OnHospitalGameFail, OnHospitalGameFail);
        }

        private void OnMainDoorClick(Action callback)
        {
            if (isPgcEnter)
            {
                UIManager.Inst.OpenPanel(PanelId.AIHospitalPasswordPanel, callback);
            }
            else
                callback();
        }

        private void OnTargetAreaTrigEnter()
        {
            if (isPgcEnter)
            {
                if (CurrentStep == pgcTarget.Count - 1)
                {
                    UpdatePgcNpcState(HospitalNpcRoleType.Default, true);
                    //OnStepChange(S9GameState.Success);
                }
                else
                {
                    var npc = AIHospital_CharacterManager.Inst.GetNpc(HospitalNpcRoleType.Security);
                    if (npc)
                    {
                        OnNpcAlert(npc.GetNpcID());
                    }
                }
            }
            else
                OnStepChange(S9GameState.Success);
        }

        private void OnDustManChangeClothes()
        {
            LoggerUtils.Log("和清洁工换装完成");
            //todo 具体的换装表现可以放在这里或者清洁工behavior里面
            UpdatePgcNpcState(HospitalNpcRoleType.Dustman, true);
        }

        private void StartGame()
        {
            var loadingPanel = UIManager.Inst.FindPanel<AIGameLoadingPanel>(WindowId.CommonWindow, PanelId.AIGameLoadingPanel);
            loadingPanel?.CloseSelf();
            AIGameCameraUtils.Inst.BackToPlayer();
            var mainCameraTarget = GameObject.Find("CameraTarget");
            mainCameraTarget.transform.localEulerAngles = new Vector3(30f, 90f, 0);
            _isStartGame = true;

            _gameGuestPanel.ShowPanel();
            _gameGuestPanel.OnHpChange(AIGameHospitalConfig.defaultHp);
            SimpleGameDurationManager.Inst.Release();
            SimpleGameDurationManager.Inst.SetDuration(new GameDurationData
            {
                duration = AIGameHospitalConfig.gameDuration
            });
            _gameGuestPanel.LockInput();
            SimpleGameDurationManager.Inst.CountDownStart(() =>
            {
                _gameGuestPanel.StartNpcFirstTalk();
                OnGuideOverAndStartPlay();
            });
            curGameTime = GameUtils.GetTimeStamp();
            AIGameSoundUtils.Inst.PlaySound(YandereConfig.YE_Countdown_Start);
        }

        private void DoJudging()
        {
            LoggerUtils.Log("逃脱倒计时结束");

            //数据校验
            if (IsGameOver())
            {
                return;
            }

            _isStartGame = false;
            _gameGuestPanel.HidePanel();
            SimpleGameDurationManager.Inst.OnPassLevelStop();
            OnStepChange(S9GameState.TimeOut);
        }

        public void OnStepChange(S9GameState state)
        {
            // 记录状态变化
            LoggerUtils.Log($"游戏状态改变: {state}");

            switch (state)
            {
                case S9GameState.Ready:
                    //屏蔽道具交互
                    AIHospital_PropsManager.Inst.SetInteractEnable(false);
                    // 游戏准备阶段
                    _isStartGame = false;
                    StartDesc();
                    break;

                case S9GameState.EnterConsultingRoom:
                    if (!isPgcEnter)
                    {
                        OnStepChange(S9GameState.UGCStartEscape);
                    }
                    break;

                case S9GameState.StartEscape:
                    //1.移动镜头
                    var doorLP = new GameObject("DoorLP");
                    doorLP.transform.SetPositionAndRotation(new Vector3(-13.46f, 3.21f, -28.0f), Quaternion.Euler(new Vector3(0, 90, 0)));
                    AIGameCameraUtils.Inst.SetCamToPos(doorLP.transform, 1, 2f);
                    // AvatarController.Inst.SelfController.PlayerAnimCtrl.Wrap.Avatar.SetActive(false);
                    TimerManager.Inst.RunOnce("WaitCameraMove1", 1.5f, () =>
                    {
                        TimerManager.Inst.RunOnce("WaitCameraMove2", 2f, () =>
                        {
                            //回到玩家身上
                            // AvatarController.Inst.SelfController.PlayerAnimCtrl.Wrap.Avatar.SetActive(true);
                            AIGameCameraUtils.Inst.BackToPlayer();

                            //NPC开始巡逻
                            AIHospital_CharacterManager.Inst.StartPatrol();

                            //开启道具交互
                            AIHospital_PropsManager.Inst.SetInteractEnable(true);

                            AIHospital_PropsManager.Inst.OpenConsultingRoomDoor();

                            GameObject.Destroy(doorLP);
                        });
                    });
                    break;
                
                case S9GameState.UGCStartEscape:
                    //开门
                    AIHospital_PropsManager.Inst.OpenConsultingRoomDoor();
                    //NPC开始巡逻
                    AIHospital_CharacterManager.Inst.StartPatrol();
                    
                    //开启道具交互
                    AIHospital_PropsManager.Inst.SetInteractEnable(true);
                            
                    break;
                    
                case S9GameState.Success:
                    EventCenterDataManager.Inst.ReportTask(PostEventId.UnlockAIGame);
                    // 停止计时
                    StopGameTimer();
                    TrackData(S9GameState.Success);
                    // 游戏成功
                    _isStartGame = false;
                    // 显示成功界面
                    OnGameSuccess();
                    break;

                case S9GameState.TimeOut:
                    EventCenterDataManager.Inst.ReportTask(PostEventId.UnlockAIGame);
                    // 停止计时
                    StopGameTimer();
                    TrackData(S9GameState.TimeOut);
                    // 游戏失败
                    _isStartGame = false;
                    // 隐藏游戏界面
                    _gameGuestPanel?.HidePanel();
                    // 显示失败界面
                    OnGameTimeOut();
                    break;
                
                case S9GameState.Fail:
                    EventCenterDataManager.Inst.ReportTask(PostEventId.UnlockAIGame);
                    // 停止计时
                    StopGameTimer();
                    TrackData(S9GameState.Fail);
                    // 游戏失败
                    _isStartGame = false;
                    // 隐藏游戏界面
                    _gameGuestPanel?.HidePanel();
                    // 显示失败界面
                    OnGameFail();
                    break;

                case S9GameState.Exit:
                    // 停止计时器
                    StopGameTimer();
                    TrackData(S9GameState.Exit);
                    // 退出游戏
                    _isStartGame = false;
                    // 隐藏所有相关界面
                    _gameGuestPanel?.HidePanel();
                    break;

                default:
                    LoggerUtils.LogError($"未处理的游戏状态: {state}");
                    break;
            }
        }

        private void StopGameTimer()
        {
            SimpleGameDurationManager.Inst.RecordPlayTimeCost();
            _costTime = SimpleGameDurationManager.Inst.GetPlayPassDurationData().duration;
            SimpleGameDurationManager.Inst.OnPassLevelStop();
        }

        #region 数据埋点上报
        private void TrackData(S9GameState state)
        {
            HospitalEndType endType = HospitalEndType.Success;
            HospitalTaskType taskType = HospitalTaskType.CompleteAllMissions;
            if (state == S9GameState.Fail || state == S9GameState.Exit || state == S9GameState.TimeOut)
            {
                endType = HospitalEndType.ForceExit;

                if (state == S9GameState.Fail || state == S9GameState.TimeOut)
                {
                    endType = Hp <= 0 ? HospitalEndType.Death : HospitalEndType.TimeOut;
                }
                
                if (isPgcEnter)
                {
                    var roleType = AIGameHospitalConfig.taskTarget[CurrentStep].targetRole;
                    switch (roleType)
                    {
                        case HospitalNpcRoleType.Doctor:
                            taskType = HospitalTaskType.PersuadeDoctor;
                            break;
                        case HospitalNpcRoleType.Nurse:
                            taskType = HospitalTaskType.PersuadeNurse;
                            break;
                        case HospitalNpcRoleType.Dean:
                            taskType = HospitalTaskType.PersuadeDean;
                            break;
                        case HospitalNpcRoleType.Pharmacist:
                            taskType = HospitalTaskType.PersuadePharmacist;
                            break;
                        case HospitalNpcRoleType.Dustman:
                            var dustManBev = AIHospital_CharacterManager.Inst.GetNpc(HospitalNpcRoleType.Dustman);
                            if(dustManBev != null && dustManBev.GetDustManClickCount() == 0){
                                taskType = HospitalTaskType.AttackDustman;
                            }
                            else{
                                taskType = HospitalTaskType.ChangeDustmanClothes;
                            }
                            break;
                        case HospitalNpcRoleType.Default:
                            taskType = HospitalTaskType.LeaveHospital;
                            break;
                    }
                }
                else
                {
                    taskType = HospitalTaskType.PersuadeRegulators;
                    if (CheckAllUgcTargetFinish())
                    {
                        taskType = HospitalTaskType.LeaveHospital;
                    }
                }
            }
            AIHospitalUtils.Inst.TrackAnalyticsData(_costTime, endType, taskType, Is_FirstPlay);
        }
        #endregion

        private void StartDesc()
        {
            _gameGuestPanel.HidePanel();
            EnterGuidePanel();
        }

        private void EnterGuidePanel()
        {
            var loadingPanel = UIManager.Inst.FindPanel<AIGameLoadingPanel>(WindowId.CommonWindow, PanelId.AIGameLoadingPanel);
            loadingPanel?.CloseSelf();

            var guidePanel = UIManager.Inst.OpenPanel<AIHospitalGuidePanel>(PanelId.AIHospitalGuidePanel);
            guidePanel.SetEnterAct(StartGame);
        }

        //引导完成并开始游戏
        private void OnGuideOverAndStartPlay()
        {
            SetSelfPlayerState(true);
            UIManager.Inst.ClosePanel(PanelId.PGCGameGuidePanel);
            if (isPgcEnter)
            {
                if (PlayerPrefs.GetInt(AccountDataManager.Inst.Uid + AIGameHospitalConfig.pgcGuideId, 0) <= 0)
                {
                    UIManager.Inst.OpenPanel<AIHospitalStrongGuide>(PanelId.AIHospitalStrongGuidePanel, AIGameHospitalConfig.EPgcGuideID.TargetTips);
                }
                else
                {
                    var doctor =  AIHospital_CharacterManager.Inst.GetNpc(HospitalNpcRoleType.Doctor);
                    doctor?.MoveToTalkPosition();
                }
            }


            if (!string.IsNullOrEmpty(AIGameHospitalConfig.ugcBgmUrl))
            {
                BgMusicManager.Inst.SetUGCMusic(AIGameHospitalConfig.ugcBgmUrl);
                BgMusicManager.Inst.PlayUGCMusic();
            }
            else
            {
                AIGameSoundUtils.Inst.PlayBgm(AIHospitalConfig.MAIN_BGM);
            }
            _gameGuestPanel.ShowPanel();
            OnStepChange(S9GameState.EnterConsultingRoom);
        }

        private void SetSelfPlayerState(bool state)
        {
            AvatarController.Inst.SelfStateController.gameObject.SetActive(state);
        }

        public bool IsGameOver()
        {
            return false;
        }

        public void GameStepCheck()
        {

        }

        /// <summary>
        /// npc警戒值满了触发的行为
        /// </summary>
        public void OnNpcAlert(string npcId)
        {
            LoggerUtils.Log($"你的异常行为已经被 npcId: {npcId} 捕捉到了，这里播放对应的表现");
            _gameGuestPanel.OnHpChange(--Hp);

            bool linkState = BuddyLinkEmoteManager.Inst.IsInBuddyLinkState(AccountDataManager.Inst.Uid);
            if (linkState)
            {
                //先退出牵手状态
                BuddyLinkEmoteManager.Inst.SendExitLinkReq(AvatarController.Inst.SelfStateController.linkEmoteData);
                //等待一帧确保状态退出
                TimerManager.Inst.RunOnce("WaitExitLink", 0.1f, () =>
                {
                    if (Hp <= 0)
                    {
                        var curAlertNpc = AIHospital_CharacterManager.Inst.GetNpc(npcId);
                        curAlertNpc?.EnterInterruptState<StopPlayerEscapeOnFailState>();
                    }
                    else
                    {
                        var curAlertNpc = AIHospital_CharacterManager.Inst.GetNpc(npcId);
                        curAlertNpc?.EnterInterruptState<StopPlayerEscapeState>();
                    }
                });
            }
            else
            {
                if (Hp <= 0)
                {
                    var curAlertNpc = AIHospital_CharacterManager.Inst.GetNpc(npcId);
                    curAlertNpc?.EnterInterruptState<StopPlayerEscapeOnFailState>();
                }
                else
                {
                    var curAlertNpc = AIHospital_CharacterManager.Inst.GetNpc(npcId);
                    curAlertNpc?.EnterInterruptState<StopPlayerEscapeState>();
                }
            }
        }
        
        private void OnGameSuccess()
        {
            var param = 1;
            UIManager.Inst.OpenPanel(PanelId.CommonBlackScreenPanel, param);
            _GamePassed = true;
            _gameGuestPanel.StopTimer();
        }

        public void OnGameTimeOut()
        {
            var param = 2;
            UIManager.Inst.OpenPanel(PanelId.CommonBlackScreenPanel, param);
            _GamePassed = false;
            _gameGuestPanel.StopTimer();
        }

        private void OnGameFail()
        {
            var param = 0;
            UIManager.Inst.OpenPanel(PanelId.CommonBlackScreenPanel, param);
            _GamePassed = false;
            _gameGuestPanel.StopTimer();
        }

        private void OnHospitalGameFail()
        {
            OnStepChange(S9GameState.Fail);
        }


        /// <summary>
        /// 清洁工的交互比较特殊定制一下,必须达到特定步骤清洁工才能有特殊交互
        /// </summary>
        private void IsSpecialStep(int stepID)
        {
            var roleType = AIGameHospitalConfig.taskTarget[stepID].targetRole;
            switch (roleType)
            {
                case HospitalNpcRoleType.Nurse:
                    //走出病房游戏剧本才开始执行
                    OnStepChange(S9GameState.StartEscape);
                    break;

                case HospitalNpcRoleType.Dean:
                    //完成了护士的任务
                    var doorLP = new GameObject("DoorLP");
                    doorLP.transform.SetPositionAndRotation(new Vector3(-4.2f,3.45f,0f), Quaternion.Euler(new Vector3(0, 90,0)));
                    MessageHelper.Broadcast(MessageName.SetOperationPanelEnable, false);
                    AIGameCameraUtils.Inst.SetCamToPos(doorLP.transform, 1, 2f);
                    // AvatarController.Inst.SelfController.PlayerAnimCtrl.Wrap.Avatar.SetActive(false);

                    TimerManager.Inst.RunOnce("OnCompleteNurseTask", 1.5f, () =>
                    {
                        TipPanel.ShowToast("已得到大门密码，点击门锁输入密码后可开门！");
                        TimerManager.Inst.RunOnce("WaitCameraMove2", 2f, () =>
                        {
                            //回到玩家身上
                            // AvatarController.Inst.SelfController.PlayerAnimCtrl.Wrap.Avatar.SetActive(true);
                            AIGameCameraUtils.Inst.BackToPlayer();
                            GameObject.Destroy(doorLP);
                            MessageHelper.Broadcast(MessageName.SetOperationPanelEnable, true);
                        });
                    });
                    break;

                case HospitalNpcRoleType.Pharmacist:
                    //完成说服院长 - 镜头打到保安
                    MessageHelper.Broadcast(MessageName.SetOperationPanelEnable, false);
                    var securityLP = new GameObject("securityLP");
                    securityLP.transform.SetPositionAndRotation(new Vector3(-4.673f, 2.764f, -0.932f), Quaternion.Euler(new Vector3(0, 135, 0)));
                    AIGameCameraUtils.Inst.SetCamToPos(securityLP.transform, 1, 2f);
                    var securityBev = AIHospital_CharacterManager.Inst.GetNpc(HospitalNpcRoleType.Security);
                    securityBev.SetPositionAndRotation(new Vector3(-3.171f,1.41f,-2.31f), Quaternion.Euler(new Vector3(0, -45, 0)));

                    TimerManager.Inst.RunOnce("OnCompleteFireSecurity", 1.5f, () =>
                    {
                        securityBev.PlayAnim("40100299");
                        TimerManager.Inst.RunOnce("OnSecurityEndAnim", 2f, () =>
                        {
                            TipPanel.ShowToast("保安被任性的院长开除啦！");
                            TimerManager.Inst.RunOnce("WaitCameraMove2", 2f, () =>
                            {
                                //回到玩家身上
                                // AvatarController.Inst.SelfController.PlayerAnimCtrl.Wrap.Avatar.SetActive(true);
                                AIGameCameraUtils.Inst.BackToPlayer();
                                GameObject.Destroy(securityLP);

                                securityBev._isInitialized = false;
                                securityBev.gameObject.SetActive(false);
                                MessageHelper.Broadcast(MessageName.SetOperationPanelEnable, true);
                            });
                        });
                    });
                    break;

                case HospitalNpcRoleType.Dustman:
                    MessageHelper.Broadcast(MessageName.SetOperationPanelEnable, false);
                    //丛药剂师那里拿到了麻醉剂 - 镜头打到清洁工
                    var dustManBev = AIHospital_CharacterManager.Inst.GetNpc(HospitalNpcRoleType.Dustman);
                    var DustManLP = new GameObject("DustManLP");
                    DustManLP.transform.SetPositionAndRotation(new Vector3(-15.82f, 2.937f, 16.8f), Quaternion.Euler(new Vector3(0, 270, 0)));
                    var dustManOriPos = dustManBev.transform.position;
                    dustManBev.EnterInterruptState<TalkWithPlayerState>();
                    dustManBev.SetPositionAndRotation(new Vector3(-18.364f, 1.41f, 16.8f), Quaternion.Euler(new Vector3(0, 90, 0)));

                    AIGameCameraUtils.Inst.SetCamToPos(DustManLP.transform, 1, 2f);

                    TimerManager.Inst.RunOnce("OnCompleteNurseTask", 1.5f, () =>
                    {
                        TipPanel.ShowToast("已获得麻醉剂，对清洁工使用麻醉剂后可换上他的衣服！");
                        TimerManager.Inst.RunOnce("WaitCameraMove2", 2f, () =>
                        {
                            //回到玩家身上
                            // AvatarController.Inst.SelfController.PlayerAnimCtrl.Wrap.Avatar.SetActive(true);
                            dustManBev.SetPositionAndRotation(dustManOriPos, Quaternion.Euler(new Vector3(0, 90, 0)));
                            dustManBev.RecalculateChapter();

                            AIGameCameraUtils.Inst.BackToPlayer();
                            GameObject.Destroy(DustManLP);
                            MessageHelper.Broadcast(MessageName.SetOperationPanelEnable, true);
                        });
                    });
                    break;
                default:
                    break;
            }
        }

        #region 加载当前步骤选中特效并且可以重新设置绑定对象
        private void LoadEffectAndSetParent(AIHospital_CharacterBehaviour behaviour)
        {
            if (behaviour == null)
            {
                LoggerUtils.LogError($"加载资源异常 找不到behaviour");
                return;
            }

            if (_effectObj == null)
            {
                var prefab = Loader.Load<GameObject>(_effectPath).RetainAsset();
                if (prefab == null)
                {
                    LoggerUtils.LogError($"加载资源异常 path = {_effectPath}");
                    return;
                }
                _effectObj = GameObject.Instantiate(prefab);
            }
            _effectObj.SetActive(true);
            _effectObj.transform.localScale = Vector3.one;
            var aihospitalEffect = _effectObj.GetComponent<AIHospitalEffect>();
            aihospitalEffect.targetBone = behaviour.GetBottomBone();
        }

        private void LoadEffectForUgcNpc(Vector3 offset)
        {
            foreach (var item in ugcTarget.Values)
            {
                var behaviour = AIHospital_CharacterManager.Inst.GetNpc(item.npcData.id);
                if (behaviour)
                {
                    behaviour.SetIsCurrentTarget(true);
                    if (item.effect == null)
                    {
                        var prefab = Loader.Load<GameObject>(_effectPath).RetainAsset();
                        if (prefab == null)
                        {
                            LoggerUtils.LogError($"加载资源异常 path = {_effectPath}");
                            return;
                        }
                        item.effect = GameObject.Instantiate(prefab);
                    }
                    item.effect.SetActive(true);
                    item.effect.transform.localScale = Vector3.one;
                    var aihospitalEffect = item.effect.GetComponent<AIHospitalEffect>();
                    aihospitalEffect.targetBone = behaviour.GetBottomBone();
                }
            }
        }

        private void OnDisableEffect()
        {
            _effectObj.SetActive(false);
        }

        private void UnLoadEffectForUgcNpc(S9UgcTaskData value)
        {
            if (value != null)
            {
                // 检查并销毁特效对象
                if (value.effect != null)
                {
                    // 设置不可见
                    value.effect.SetActive(false);
                    // 解除父物体关系
                    value.effect.transform.SetParent(null);
                    // 销毁游戏对象
                    GameObject.Destroy(value.effect);
                    value.effect = null;
                }
            }
            else
            {
                LoggerUtils.LogError($"UnLoadEffectForUgcNpc: 找不到对应的npc特效对象，npcID={value.npcData.id}");
            }
        }
        #endregion

        #region 更新ugc状态
        public void UpdateUgcNpcState(string npcID, bool bFinish)
        {
            bool needCheck = false;
            if (ugcTarget.TryGetValue(npcID, out S9UgcTaskData value))
            {
                if (value.isFinish == bFinish) return;
                needCheck = true;
                value.isFinish = bFinish;
                if (bFinish)
                {
                    UnLoadEffectForUgcNpc(value);
                    var behaviour = AIHospital_CharacterManager.Inst.GetNpc(npcID);
                    if (behaviour)
                    {
                        behaviour.SetIsCurrentTarget(false);
                        LoggerUtils.LogError($"{behaviour.GetNpcName()} 被成功说服");
                    }
                }
            }
            else
            {
                LoggerUtils.LogError($"无法找到关联的npc，id={npcID}");
            }
            if (needCheck && CheckAllUgcTargetFinish())
            {
                MessageHelper.Broadcast(MessageName.OnS9UgcTargetFinished);
            }
        }

        private bool CheckAllUgcTargetFinish()
        {
            int finishCount = 0;
            foreach (var item in ugcTarget)
            {
                if (item.Value.isFinish)
                {
                    finishCount++;
                }
            }
            _gameGuestPanel.SetTarget(finishCount);
            return finishCount >= GameSetting.aIGameConfig.winRegulatorAmount;
        }
        #endregion

        #region 更新pgc状态
        public void UpdatePgcNpcState(HospitalNpcRoleType roleType, bool bFinish)
        {
            LoggerUtils.Log($"UpdatePgcNpcState: {roleType} bFinish ={bFinish}");
            int step = 0;
            bool bChanged = false; ;
            for (int i = 0; i < pgcTarget.Count; i++)
            {
                if (pgcTarget[i].role != roleType && bFinish && !pgcTarget[i].isFinish)
                {
                    break;
                }
                if (pgcTarget[i].role == roleType)
                {
                    if (pgcTarget[i].isFinish != bFinish)
                    {
                        pgcTarget[i].isFinish = bFinish;
                        bChanged = true;
                    }
                }
            }
            if (bChanged)
            {
                for (int i = 0; i < AIGameHospitalConfig.taskTarget.Count; i++)
                {
                    if (pgcTarget[i].isFinish)
                    {
                        step = i;
                    }
                }
                if (step == pgcTarget.Count - 1 && bFinish)
                {
                    OnStepChange(S9GameState.Success);
                }
                else
                {
                    CurrentStep = ++step;
                    if(isPgcEnter)
                        GameAINpcChatManager.Inst.ClearTempConversitionID(CurrentStep);
                }
            }
        }

        private void UpdatePgcOpenDoorState()
        {
            //UpdatePgcNpcState(HospitalNpcRoleType.Default,true);
        }
        #endregion

        private void OnS9UgcTargetFinished()
        {
            var mainDoorMgr = GlobalNodeManager.Inst.Get<AIHospital_MainDoorManager>();
            var mainDoorBev = mainDoorMgr.GetMainDoor();

            if (isPgcEnter)
            {
                mainDoorBev.HandOpenDoor();
                return;
            }
            
            var mainDoorLP = new GameObject();
            mainDoorLP.transform.SetPositionAndRotation(new Vector3(-4.2f, 3.523f, 0f), Quaternion.Euler(new Vector3(5, 90, 0)));
            AIGameCameraUtils.Inst.SetCamToPos(mainDoorLP.transform, 1, 2f);
            MessageHelper.Broadcast(MessageName.SetOperationPanelEnable, false);
            TimerManager.Inst.RunOnce("BackToPlayer", 1.5f, () =>
            {
                mainDoorBev.HandOpenDoor();
                TipPanel.ShowToast("大门已打开，快逃出去吧！");
            });
            
            TimerManager.Inst.RunOnce("BackToPlayer", 4f, () =>
            {
                GameObject.Destroy(mainDoorLP);
                AIGameCameraUtils.Inst.BackToPlayer();
                MessageHelper.Broadcast(MessageName.SetOperationPanelEnable, true);
            });
        }


        public long GetGameStartTime()
        {
            return curGameTime;
        }

        public bool GetGamePassState()
        {
            return _GamePassed;
        }
    }
}
