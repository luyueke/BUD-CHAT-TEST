using Basic.Utils;
using Game.Avatar;
using Game.Base;
using Game.Props;
using Game.Props.PropsManagers;
using GameData.BaseInfo;
using GameData.Manager;
using Message;
using System;
using System.Collections.Generic;
using System.Linq;
using Game.Event;
using Game.MapSetting;
using UnityEngine;
using Game.Props.PropsManagers.AIGames.AIPark.FSM;
using GameSync.Manager;
using UI.Base;
using Game.Props.PropsBehaviours;
using Es;
using GameData.PgcData;
using Newtonsoft.Json;
using UI.UIPanels.ProfilePanel;
using Pb.Game;
using UIAgent;
using Network;
using Network.Http;
namespace AIGame.Base
{
    public class AIParkGame : BaseAIGame
    {
        private const string TAG = "AIParkGame";
        public bool Is_CurPlayWithEventGuide = false; //本次是否有走事件引导
        public bool Is_FirstPlay = true;
        public int CurEventStep = 1; //0 1 2 3   0代表完成

        [SerializeField] private int _currentStep;
        public int CurrentStep
        {
            get { return _currentStep; }
            set
            {
                if (isPgcEnter)
                {
                    // if (pgcTarget[_currentStep].role != ParkNpcRoleType.Default)
                    // {
                    //     // var oldCharacterBehaviour = AIPark_CharacterManager.Inst.GetNpc(AIGameParkConfig.taskTarget[_currentStep].targetRole);
                    //     // oldCharacterBehaviour?.SetIsCurrentTarget(false);
                    // }
                    // LoggerUtils.Log($" stepchanged {_currentStep} = {value}");
                    // _currentStep = value;
                    // if (pgcTarget[_currentStep].role != ParkNpcRoleType.Default)
                    // {
                    //     // var newCharacterBehaviour = AIPark_CharacterManager.Inst.GetNpc(AIGameParkConfig.taskTarget[_currentStep].targetRole);
                    //     // newCharacterBehaviour?.SetIsCurrentTarget(true);
                    //     // LoadEffectAndSetParent(newCharacterBehaviour);
                    //     // IsSpecialStep(value);
                    // }
                    // else
                    //     OnDisableEffect();
                }
                _gameGuestPanel?.SetTarget(CurrentStep);
            }
        }

        private AIParkGuestPanel _gameGuestPanel;
        private bool _isStartGame = false;
        private long curGameTime = 0;
        public bool isPgcEnter = true;
        private int _costTime = 0;
        private int _hp;
        public int Hp
        {
            get { return _hp; }
            set
            {
                LoggerUtils.Log($"尝试修改AIPartGame的 当局血量  当前步骤{_hp} => {value}");
                _hp = value;
            }
        }

        public string CurMapID { get; private set; }

        #region 特效资源
        private string _effectPath = "Assets/Loadable/Avatar/CharacterBody/CallNpcEffect/S9npcSelect/S9AINpcSelected.prefab";
        #endregion
        [SerializeField] private GameObject _effectObj;

        public GameSetting GameSetting { get; private set; }

        /// <summary>
        /// ugc目标
        /// </summary>
        private Dictionary<string, S11UgcTaskData> ugcTarget = new Dictionary<string, S11UgcTaskData>();

        /// <summary>
        /// pgc任务目标
        /// </summary>
        private List<S11PgcTaskData> pgcTarget = new List<S11PgcTaskData>();
        private bool _GamePassed = false;
        int _curEventIndex = 1;

        string _curDiscussId = "";

        Dictionary<int, bool> _sceneEndDict = new Dictionary<int, bool>();//场景是否结束

        BudTimer _npcEmoteTimer;
        public override void OnStart()
        {


            base.OnStart();
            InitGameData();

            //两个模式都没玩过才算初次游玩
            var isFirstPlayUgc = PlayerPrefs.GetInt(AccountDataManager.Inst.Uid + "_" + "PlayS11Ugc", 0) != 1;
            var isFirstPlayPgc = PlayerPrefs.GetInt(AccountDataManager.Inst.Uid + "_" + "PlayS11Pgc", 0) != 1;
            Is_FirstPlay = isFirstPlayUgc && isFirstPlayPgc;
            CurEventStep = 1;
            Is_CurPlayWithEventGuide = false;

            var curMapInfo = GameDataManager.Inst.mapGlobalData.GetCurInfo<MapInfo>();
            GameSetting = curMapInfo.gameSetting;

            if (curMapInfo.id == "PGC")
            {
                AIGameParkConfig.hasTimeLimited = GameSetting.timeLimited == 1;
                AIGameParkConfig.gameDuration = GameSetting.limitDuration;
                AIGameParkConfig.defaultHp = GameSetting.limitHp == 0 ? 9999 : GameSetting.limitHp;
                AIGameParkConfig.ugcBgmUrl = GameSetting.bgMusicUrl;
                pgcTarget = AIGameParkConfig.taskTarget
                    .Where(x => x is not null)
                    .Select(x => new S11PgcTaskData()
                    {
                        role = x.targetRole,
                        isFinish = false,
                    })
                    .ToList();
                isPgcEnter = true;
            }
            else
            {
                AIGameParkConfig.hasTimeLimited = GameSetting.timeLimited == 1;
                AIGameParkConfig.gameDuration = GameSetting.limitDuration;
                AIGameParkConfig.defaultHp = GameSetting.limitHp == 0 ? 9999 : GameSetting.limitHp;
                AIGameParkConfig.ugcBgmUrl = GameSetting.bgMusicUrl;
                ugcTarget = GameSetting.aIGameConfig.hospitalNPCs
                    .Where(x => x.role == (int)ParkNPCType.Provost)
                    .ToDictionary(
                        x => x.id,
                        x => new S11UgcTaskData()
                        {
                            npcData = new()
                            {
                                id = x.id,
                                role = x.role,
                                name = x.name,
                                cover = x.cover,
                                npcConfig = x.npcConfig,
                            },
                            isFinish = false,
                        }
                    );
                isPgcEnter = false;
                CurMapID = curMapInfo.id;
            }

            Debug.Log($"AIParkGame OnStart aICommonGameConfig =" + curMapInfo.gameSetting.aICommonGameConfig);
            //初始化
            AIParkPropsManager.Inst.InitData();
            AIParkPropsManager.Inst.InitPhoto(curMapInfo.gameSetting.AICommonGameConfig);

            TalkStateUtils.Inst.InitData();
            AIParkUtils.Inst.ParkGameData.isPgcEnter = isPgcEnter;
            AIPark_CharacterManager.Inst.IsPgcEnter = isPgcEnter;
            if (AIParkUtils.Inst.ParkGameData != null)
            {
                var allNpcIds = AIParkUtils.Inst.ParkGameData.GetAllNpcs();
                var allNpcNames = AIPark_NpcUtil.GetAllNpcNames(allNpcIds);
                var allNpcAvatars = AIPark_NpcUtil.GetAllNpcAvatars(allNpcIds);
                AIPark_CharacterManager.Inst.InitSceneAICharacter(allNpcIds, allNpcAvatars, allNpcNames);
            }
            _gameGuestPanel = UIManager.Inst.FindPanel<AIParkGuestPanel>(WindowId.GuestWindow, PanelId.AIParkGuestPanel);
            _gameGuestPanel.IsPgcEnter = isPgcEnter;

            CurrentStep = 0;
            LoadEffectForUgcNpc(Vector3.zero);
            if (AIGameParkConfig.defaultHp == AIGameParkConfig.maxHp)
            {
                _gameGuestPanel.HideHeart();
            }

            OnStepChange(S11GameState.Ready);

            AIParkNpcLocationSyncUtil.Inst.Init();

            MessageHelper.AddListener(MessageName.Park_GetNextSceneData, OnPreGetNextSceneData);
            MessageHelper.AddListener(MessageName.Park_CountDownEnd, OnCountDownEnd);
        }

        public override void Restart()
        {
            base.Restart();
            AIGameSoundUtils.Inst.StopAllSound();
            var curMapId = GameDataManager.Inst.mapGlobalData.GetCurInfo<MapInfo>().id;
            GameController.ExitGame(() =>
            {
                AIParkPropsManager.Inst.SelfEnterIdle();
                SimpleGameDurationManager.Inst.Release();
                UIManager.Inst.ForceSetOtherWindowTransInStack(WindowId.GuestWindow, true);
                UIManager.Inst.ClosePanel(PanelId.UIOperationOnWorldPanel);

                // UIManager.Inst.ClosePanel(PanelId.AIParkEndPanel);
                UIManager.Inst.BackToLastWindow();
                UIManager.Inst.OpenPanel(PanelId.BlackPanel);
                AIParkPropsManager.Inst.ExitGameFreeAllProps();
                AIParkSoundUtil.Inst.ReleaseAllSound();

                if (isPgcEnter)
                {
                    // AIParkUtils.Inst.EnterOfficalParkGame();
                }
                else
                {
                    // AIParkUtils.Inst.EnterUgcParkGame(curMapId);
                }
            });
        }

        private void InitGameData()
        {
            AIGameParkConfig.Reset();
            AIParkChatMgr.Inst.Init();
            Hp = AIGameParkConfig.defaultHp;
            GameAINpcChatManager.Inst.ClearAllConversitionID();
            GameAINpcChatManager.Inst.ClearGuideCache();
            ugcTarget.Clear();
            pgcTarget.Clear();
            AIParkTcpNetMgr.Instance.Init(); //tcp初始化
            _curEventIndex = 1;
            _sceneEndDict ??= new();
            _sceneEndDict.Clear();
            _sceneEndDict.Add(1, false);
            _sceneEndDict.Add(2, false);
            _sceneEndDict.Add(3, false);
            _sceneEndDict.Add(4, false);
            EventCenterDataManager.Inst.ReportTask(PostEventId.PlayAIGame);
        }

        protected override void AddListener()
        {
            base.AddListener();
            // MessageHelper.AddListener<Action>(MessageName.OnS11MainDoorClick, OnMainDoorClick);
            // MessageHelper.AddListener(MessageName.PassLevelJudging, DoJudging);
            // MessageHelper.AddListener(MessageName.OnS11TargetAreaTrigEnter, OnTargetAreaTrigEnter);
            // MessageHelper.AddListener(MessageName.OnS11MainDoorOpen, UpdatePgcOpenDoorState);
            // MessageHelper.AddListener(MessageName.OnS11UgcTargetFinished, OnS11UgcTargetFinished);
            MessageHelper.AddListener(MessageName.OnParkGameFail, OnParkGameFail);
        }

        protected override void RemoveListener()
        {
            base.RemoveListener();
            // MessageHelper.RemoveListener<Action>(MessageName.OnS11MainDoorClick, OnMainDoorClick);
            // MessageHelper.RemoveListener(MessageName.PassLevelJudging, DoJudging);
            // MessageHelper.RemoveListener(MessageName.OnS11TargetAreaTrigEnter, OnTargetAreaTrigEnter);
            // MessageHelper.RemoveListener(MessageName.OnS11MainDoorOpen, UpdatePgcOpenDoorState);
            // MessageHelper.RemoveListener(MessageName.OnS11UgcTargetFinished, OnS11UgcTargetFinished);
            MessageHelper.RemoveListener(MessageName.OnParkGameFail, OnParkGameFail);
        }

        private void OnMainDoorClick(Action action)
        {
            if (isPgcEnter)
            {
                if (CurrentStep == AIGameParkConfig.taskTarget.Count - 1)
                {
                    OnGameSuccess();
                }
            }
            else
            {
                if (ugcTarget.All(x => x.Value.isFinish))
                {
                    OnGameSuccess();
                }
            }
        }

        private void OnTargetAreaTrigEnter()
        {
            if (isPgcEnter)
            {
                if (CurrentStep == AIGameParkConfig.taskTarget.Count - 1)
                {
                    OnGameSuccess();
                }
            }
        }

        private void OnS11UgcTargetFinished()
        {
            if (!isPgcEnter)
            {
                if (ugcTarget.All(x => x.Value.isFinish))
                {
                    OnGameSuccess();
                }
            }
        }

        private void OnParkGameFail()
        {
            OnGameFail();
        }




        public void LoadEffectAndSetParent(AIPark_CharacterBehaviour characterBehaviour)
        {
            if (characterBehaviour == null)
            {
                LoggerUtils.Log($"加载资源异常 找不到behaviour");
                return;
            }

            if (_effectObj == null)
            {
                var prefab = Loader.Load<GameObject>(_effectPath).RetainAsset();
                if (prefab == null)
                {
                    LoggerUtils.Log($"加载资源异常 path = {_effectPath}");
                    return;
                }
                _effectObj = GameObject.Instantiate(prefab);
            }
            _effectObj.SetActive(true);
            _effectObj.transform.localScale = Vector3.one;
            // var aihospitalEffect = _effectObj.GetComponent<AIHospitalEffect>();
            // aihospitalEffect.targetBone = characterBehaviour.GetBottomBone();
            // _effectObj = Instantiate(Resources.Load<GameObject>(_effectPath));
            _effectObj.transform.SetParent(characterBehaviour.transform);
            _effectObj.transform.localPosition = Vector3.zero;
        }

        private void LoadEffectForUgcNpc(Vector3 pos)
        {
            // if (_effectObj != null)
            // {
            //     Destroy(_effectObj);
            // }
            // _effectObj = Instantiate(Resources.Load<GameObject>(_effectPath));
            // _effectObj.transform.position = pos;
        }

        public void OnDisableEffect()
        {
            if (_effectObj != null)
            {
                Destroy(_effectObj);
            }
        }

        public void ShowGlobalToast(string toast)
        {
            _gameGuestPanel.guestGroup.ShowNotice(toast);
        }
        //检查进入下一幕旁白(当计时结束时)
        public void CheckEnterNextSceneOsWhenCountDownEnd()
        {
            EnterEventEnd(); //直接进入事件结束 旁白等待
        }
        //收到下一幕数据 派发给到os界面去
        public void OnRspNextSceneData()
        {
            var guidePanel = UIManager.Inst.FindPanel<AIParkGuidePanel>(WindowId.GuestWindow, PanelId.AIParkGuidePanel);
            Debug.Log("乐园收到下一幕数据11");

            if (guidePanel != null)
            {
                Debug.Log("乐园收到下一幕数据22");

                guidePanel.ReceiveNextSceneData();
            }
        }
        /// <summary>
        /// 切换幕的时候关闭相关无用ui
        /// </summary>
        public void CloseOtherUI()
        {
            UIManager.Inst.ClosePanel(PanelId.AIParkGameBookPanel);
            UIAgentManager.Inst.ClosePanel(WindowId.GuestWindow, PanelId.AIParkGameStagePanel);
            UIAgentManager.Inst.ClosePanel(WindowId.GuestWindow, PanelId.GuestInstrumentPlayPanel);
            UIAgentManager.Inst.ClosePanel(WindowId.GuestWindow, PanelId.AIParkQuickEmotePanel);
            UIAgentManager.Inst.ClosePanel(WindowId.GuestWindow, PanelId.AIParkGameCatalogPanel);
            _gameGuestPanel.chatPopView.QuickHideView();
        }

        void EnterEventEnd()
        {
            if (AIParkUtils.Inst.ParkCustomData.SceneIndex == 1 && _sceneEndDict[1])
            {
                AIParkUtils.Inst.ParkCustomData.SceneIndex++;
                SimpleGameDurationManager.Inst.ResetAllData();
                CloseOtherUI();
                OnStepChange(S11GameState.Event_1_End);
            }
            else if (AIParkUtils.Inst.ParkCustomData.SceneIndex == 2 && _sceneEndDict[2])
            {
                AIParkUtils.Inst.ParkCustomData.SceneIndex++;
                SimpleGameDurationManager.Inst.ResetAllData();
                CloseOtherUI();
                OnStepChange(S11GameState.Event_2_End);
            }
            else if (AIParkUtils.Inst.ParkCustomData.SceneIndex == 3 && _sceneEndDict[3])
            {
                AIParkUtils.Inst.ParkCustomData.SceneIndex++;
                CloseOtherUI();
                OnStepChange(S11GameState.Event_3_End);
            }
        }

        public bool CheckHadNextSceneData()
        {
            return AIParkTcpNetMgr.Instance.CheckHadNextSceneData(AIParkTcpNetMgr.Instance.PreNextSceneIdx);
            // return AIParkTcpNetMgr.Instance.CheckHadNextSceneData(AIParkUtils.Inst.ParkCustomData.SceneIndex);
        }

        // public void CheckEnterNextScene()
        // {
        //     if (!AIParkTcpNetMgr.Instance.CheckHadNextSceneData(AIParkUtils.Inst.ParkCustomData.SceneIndex + 1))
        //     {
        //         return;
        //     }
        //     EnterEventEnd();
        // }
        //选择事件返回
        public void OnGameEndRsp()
        {
            var guidePanel = UIManager.Inst.FindPanel<AIParkGuidePanel>(WindowId.GuestWindow, PanelId.AIParkGuidePanel);
            if (guidePanel != null)
            {
                guidePanel.ReceiveNextSceneData();
            }
        }
        /// <summary>
        /// 预准备下一幕的数据
        /// </summary> <summary>
        /// 
        /// </summary>
        private void OnPreGetNextSceneData()
        {
            Debug.Log("乐园预准备下一幕的数据" + AIParkUtils.Inst.ParkCustomData.SceneIndex + "  " + AIParkUtils.Inst.ParkGameData.events.Count);
            var sceneIdx = AIParkUtils.Inst.ParkCustomData.SceneIndex;
            if (sceneIdx == 1 && AIParkUtils.Inst.ParkGameData.events.Count > 1)
            {
                AIParkTcpNetMgr.Instance.SendAIParkSyncReq_NextScene(2);
            }
            else if (sceneIdx == 2 && AIParkUtils.Inst.ParkGameData.events.Count > 2)
            {
                AIParkTcpNetMgr.Instance.SendAIParkSyncReq_NextScene(3);
            }
        }

        private void OnCountDownEnd()
        {
            AIParkGuideMgr.Inst.ExitGuide();
            var sceneIdx = AIParkUtils.Inst.ParkCustomData.SceneIndex;
            Debug.Log("乐园倒计时结束当前幕 sceneIdx = " + sceneIdx);
            if (sceneIdx == 1)
            {
                _sceneEndDict[1] = true;
                CheckEnterNextSceneOsWhenCountDownEnd();
            }
            else if (sceneIdx == 2)
            {
                _sceneEndDict[2] = true;
                CheckEnterNextSceneOsWhenCountDownEnd();
            }
            else if (sceneIdx == 3)
            {
                _sceneEndDict[3] = true;
                CheckEnterNextSceneOsWhenCountDownEnd();
            }
        }

        void CheckEventBegin(int EventIdx)
        {
            // int sceneIdx = AIParkUtils.Inst.ParkCustomData.SceneIndex;
            var eventData = AIParkUtils.Inst.ParkGameData.events[EventIdx - 1];
            SimpleGameDurationManager.Inst.SetDuration(new GameDurationData
            {
                duration = AIParkUtils.Inst.ParkGameData.events[EventIdx - 1].EventOpts[0].EventDuration
            }, DurationType.Park);
            if (eventData.EventType == 0)
            {
                //普通事件开始
                AIPark_CharacterManager.Inst.StartPatrol();
                //镜头回到自己 事件弹窗&飘字
                //todo
                AIPark_CharacterManager.Inst.ResumeAllNpcChapter();
                _gameGuestPanel.guestGroup.ShowEvent();
                UIManager.Inst.OpenPanel<AIParkGameEventPanel>(PanelId.AIParkGameEventPanel, WindowId.GuestWindow, AIParkUtils.Inst.ParkGameData.events[EventIdx - 1], null, AIParkUtils.Inst.ParkGameData.events[EventIdx - 1].EventOpts[0].EventDuration);
                AIGameCameraUtils.Inst.BackToPlayer();
                _gameGuestPanel.ShowPanel();
                SimpleGameDurationManager.Inst.CountDownStart();
                TriggerHistoryWhenCloseEventPanel();
            }
            else if (eventData.EventType == 1)
            {
                //选择事件开始
                AIPark_CharacterManager.Inst.ResumeAllNpcChapter();
                SimpleGameDurationManager.Inst.CountDownStart();
                //todo 事件3开始 选择事件
                if (AIParkGuideMgr.Inst.CheckSelectEventGuide())
                {
                    Is_CurPlayWithEventGuide = true;
                    AIParkGuideMgr.Inst.RunGuide(S11GuideStep.Guide_FirstInGame_Guide_2_3);
                }
                else
                {
                    //选择事件
                    //打开选择事件面板
                    ShowSelectEvent();
                }
            }
        }

        void CheckEventEnd(int EventIdx)
        {
            int sceneIdx = AIParkUtils.Inst.ParkCustomData.SceneIndex;
            var eventData = AIParkUtils.Inst.ParkGameData.events[EventIdx - 1];
            Debug.Log("乐园CheckEventEnd eventData.EventType = " + eventData.EventType + " sceneIdx = " + sceneIdx + "  EventIdx=" + EventIdx);
            if (eventData.EventType == 0)
            {
                //普通事件结束
                AIPark_CharacterManager.Inst.TriggerHistoryState = false;
                EnterBlackPanel(1, 0, () =>
                {
                    _gameGuestPanel.guestGroup.HideEvent();
                    UIManager.Inst.ClosePanel(PanelId.AIParkGameEventPanel);
                    ResetNpc2DiscussPoint();
                    OnStepChange(S11GameState.Event_Os);

                }, () =>
                {
                });

                bool isEnd = AIParkUtils.Inst.ParkGameData.events.Count < AIParkUtils.Inst.ParkCustomData.SceneIndex;
                if (isEnd)
                {
                    //最后一幕是普通事件 直接进入选择
                    AIParkTcpNetMgr.Instance.SendAIParkSyncReq_SelectedEvent("");
                }
                else
                {
                    CurEventStep = EventIdx + 1;
                }
            }
            else if (eventData.EventType == 1)
            {
                if (AIParkTcpNetMgr.Instance.bHadSelectEvent)
                {
                    return;
                }
                //选择事件结束
                var be = SimpleGameDurationManager.Inst.GetDurationBehaviour();
                int leftTime = be == null ? 0 : (int)be.leftTime;
                var panel = UIManager.Inst.OpenPanel<AIParkGameEventPanel>(PanelId.AIParkGameEventPanel, WindowId.GuestWindow, AIParkUtils.Inst.ParkGameData.events[sceneIdx - 2],
                    AIParkUtils.Inst.ParkGameData.events[sceneIdx - 2].EventOpts[0], leftTime);
                if (panel == null)
                {
                    panel = UIManager.Inst.FindPanel<AIParkGameEventPanel>(WindowId.GuestWindow, PanelId.AIParkGameEventPanel);
                }
                panel.ShowChoose();
            }
        }

        public void OnSendSelectEvent()
        {
            SimpleGameDurationManager.Inst.ResetAllData();
            OnCountDownEnd();
            // for (int i = 0; i < AIParkUtils.Inst.ParkGameData.events.Count; i++)
            // {
            //     if (AIParkUtils.Inst.ParkGameData.events[i].EventType == 1)
            //     {
            //         var eventData = AIParkUtils.Inst.ParkGameData.events[i];
            //         if (i == 0)
            //         {
            //             OnStepChange(S11GameState.Event_1_End);
            //         }
            //         else if (i == 1)
            //         {
            //             OnStepChange(S11GameState.Event_2_End);
            //         }
            //         else if (i == 2)
            //         {
            //             OnStepChange(S11GameState.Event_3_End);
            //         }
            //         break;
            //     }
            // }


            AIPark_CharacterManager.Inst.TriggerHistoryState = false;
            EnterBlackPanel(1, 0, () =>
            {
                _gameGuestPanel.guestGroup.HideEvent();
                UIManager.Inst.ClosePanel(PanelId.AIParkGameEventPanel);
                ResetNpc2DiscussPoint();
                int cnt = AIParkUtils.Inst.ParkGameData.events.Count;
                if (AIParkUtils.Inst.ParkGameData.events[cnt - 1].EventType == 1)
                {
                    CurEventStep = cnt;
                }
                OnStepChange(S11GameState.Event_Os);

            }, () =>
            {
                UIOperationOnWorldPanel.bBeginRaycast = false;
            });
        }

        public void OnStepChange(S11GameState state)
        {
            // 记录状态变化
            LoggerUtils.Log($"乐园游戏状态改变: {state}");
            var sceneIdx = AIParkUtils.Inst.ParkCustomData.SceneIndex;
            switch (state)
            {
                case S11GameState.Ready_QuickEv_1:
                    //屏蔽道具交互
                    AIHospital_PropsManager.Inst.SetInteractEnable(false);
                    // 游戏准备阶段
                    _isStartGame = false;
                    _gameGuestPanel.HidePanel();
                    var loadingPanel = UIManager.Inst.FindPanel<AIGameLoadingPanel>(WindowId.CommonWindow, PanelId.AIGameLoadingPanel);
                    loadingPanel?.CloseSelf();
                    AIParkUtils.Inst.ParkGameData.events[0].EventOpts[0].EventDuration = 1000;
                    StartGame();

                    UIManager.Inst.ClosePanel(PanelId.AIParkGameDialogPanel);
                    TimerManager.Inst.RunOnce("WaitNpcDoThing", 0.1f, () =>
                    {
                        _gameGuestPanel.gameObject.SetActive(true);
                        OnStepChange(S11GameState.Event_1_Begin);
                        UIManager.Inst.ClosePanel(PanelId.AIParkGameEventPanel);
                    });
                    break;
                case S11GameState.Ready:
                    AIParkNpcLocationSyncUtil.Inst.beginCheckPos = false;
                    AIPark_CharacterManager.Inst.TriggerHistoryState = false;
                    //屏蔽道具交互
                    AIHospital_PropsManager.Inst.SetInteractEnable(false);

                    // 游戏准备阶段
                    _isStartGame = false;
                    StartDesc();

                    break;
                case S11GameState.Event_Os:
                    AIParkNpcLocationSyncUtil.Inst.beginCheckPos = false;
                    string des = "";
                    bool isEnd = false;
                    if (AIParkUtils.Inst.ParkGameData.events.Count > AIParkUtils.Inst.ParkCustomData.SceneIndex - 1)
                    {
                        isEnd = false;
                        des = AIParkUtils.Inst.ParkGameData.events[AIParkUtils.Inst.ParkCustomData.SceneIndex - 1].EventOpts[0].EventDesc;
                    }
                    else
                    {
                        if (isPgcEnter)
                        {
                            int cnt = AIParkUtils.Inst.ParkGameData.EventSummaryList.Count;
                            if (cnt > 0)
                            {
                                des = AIParkUtils.Inst.ParkGameData.EventSummaryList[cnt - 1];
                            }
                        }
                        else
                        {
                            // int cnt = AIParkUtils.Inst.ParkGameData.aICommonGameConfig.endings.Count;
                            // if (cnt > 0)
                            // {
                            //     des = AIParkUtils.Inst.ParkGameData.aICommonGameConfig.endings[cnt - 1].desc;
                            // }
                            des = AIParkUtils.Inst.ParkGameData.Summary;
                        }

                        isEnd = true;
                    }
                    var guidePanel = UIManager.Inst.FindPanel<AIParkGuidePanel>(WindowId.GuestWindow, PanelId.AIParkGuidePanel);
                    if (guidePanel == null)
                    {
                        guidePanel = UIManager.Inst.OpenPanel<AIParkGuidePanel>(PanelId.AIParkGuidePanel, WindowId.GuestWindow, des);
                    }
                    else
                    {
                        guidePanel.AddCustomContent(des);
                    }
                    Debug.Log("乐园旁白时是否有下一幕数据:" + CheckHadNextSceneData());
                    guidePanel.SetWaitNextSceneDataState(!CheckHadNextSceneData());
                    guidePanel.SetEnterAct(() =>
                    {
                        guidePanel.CloseSelf();
                        if (isEnd)
                        {
                            OnStepChange(S11GameState.SummaryDiscuss);
                        }
                        else
                        {
                            OnStepChange(S11GameState.NpcDiscuss);

                        }
                    });
                    _gameGuestPanel.HidePanel();
                    UIOperationOnWorldPanel.bBeginRaycast = false;
                    AIPark_CharacterUtils.Inst.CanContinueOsDialog = true;
                    // SetAsideCamera(); //设置旁白镜头
                    // List<(AICommonGameConfig_NPC npc, string speaker, string content, string emoteId)> Items = new();
                    // Items.Add((null, "旁白", des, ""));
                    // var dialogPanel = UIManager.Inst.OpenPanel<AIParkGameDialogPanel>(PanelId.AIParkGameDialogPanel, WindowId.GuestWindow, Items);
                    // dialogPanel.PlaySound();
                    // dialogPanel.SetShowDialogAc((speaker, content, emoteId) =>
                    // {
                    //     dialogPanel.gameObject.SetActive(false);
                    // });
                    // dialogPanel.SetCloseAc(() =>
                    // {
                    //     OnStepChange(S11GameState.NpcDiscuss);
                    // });
                    break;
                case S11GameState.NpcDiscuss:
                    AIParkNpcLocationSyncUtil.Inst.beginCheckPos = false;
                    AIPark_CharacterManager.Inst.TriggerHistoryState = false;
                    StartNpcNewDiscuss();
                    break;
                case S11GameState.NpcDiscussEnd:
                    TimerManager.Inst.Stop(_npcEmoteTimer);
                    AIGameCameraUtils.Inst.BackToPlayer();
                    var npcBehaviour = AIPark_CharacterManager.Inst.GetNpc(((int)(ParkNpcRoleType.self)).ToString());
                    npcBehaviour.playerStateControllerState.ExitState(PlayerState.SingleEmote);
                    npcBehaviour._npcKccCtr.SetFreezeCharacter(false);
                    AIPark_CharacterManager.Inst.ExitAllFromDiscussPoint();
                    SceneAIDoActionWhenNpcDiscussEnd();
                    AIPark_CharacterManager.Inst.TriggerHistoryState = true;
                    if (!AIParkUtils.Inst.ParkCustomData.isSummaryScene)
                    {
                        AIParkTcpNetMgr.Instance.SendAIParkSyncReq_StartPushAction();
                    }
                    TimerManager.Inst.RunOnce("NpcDiscussEnd", 2.5f, () =>
                    {
                        EnterBlackPanel(1, 0, () =>
                        {
                        }, () =>
                        {
                            _gameGuestPanel.ShowPanel();
                            UIOperationOnWorldPanel.bBeginRaycast = true;
                            if (AIParkUtils.Inst.ParkCustomData.isSummaryScene)
                            {
                                OnStepChange(S11GameState.Event_Summary);
                                return;
                            }
                            if (AIParkUtils.Inst.ParkCustomData.SceneIndex == 1)
                            {
                                OnStepChange(S11GameState.Event_1_Begin);
                                AIParkNpcLocationSyncUtil.Inst.beginCheckPos = true;
                            }
                            else if (AIParkUtils.Inst.ParkCustomData.SceneIndex == 2)
                            {
                                OnStepChange(S11GameState.Event_2_Begin);
                                AIParkNpcLocationSyncUtil.Inst.beginCheckPos = true;
                            }
                            else if (AIParkUtils.Inst.ParkCustomData.SceneIndex == 3)
                            {
                                OnStepChange(S11GameState.Event_3_Begin);
                                AIParkNpcLocationSyncUtil.Inst.beginCheckPos = true;
                            }
                        });
                    });
                    break;
                case S11GameState.Event_1_Begin:
                    CheckEventBegin(1);
                    break;
                case S11GameState.Event_1_End:
                    CheckEventEnd(1);
                    break;
                case S11GameState.Event_2_Begin:
                    CheckEventBegin(2);
                    break;
                case S11GameState.Event_2_End:
                    CheckEventEnd(2);
                    break;
                case S11GameState.Event_3_Begin:
                    CheckEventBegin(3);
                    break;
                case S11GameState.Event_3_End:
                    CheckEventEnd(3);
                    break;
                case S11GameState.SummaryDiscuss:
                    AIPark_CharacterManager.Inst.TriggerHistoryState = false;
                    EnterBlackPanel(1, 0, () =>
                    {
                        _gameGuestPanel.guestGroup.HideEvent();
                        UIManager.Inst.ClosePanel(PanelId.AIParkGameEventPanel);
                        ResetNpc2DiscussPoint();
                    }, () =>
                    {
                        UIOperationOnWorldPanel.bBeginRaycast = false;
                        OnStepChange(S11GameState.NpcDiscuss);
                    });
                    break;
                case S11GameState.Event_Summary:
                    SimpleGameDurationManager.Inst.CountDownPause();
                    _gameGuestPanel.guestGroup.HideEvent();
                    _gameGuestPanel.guestGroup.HideChooseBtn();
                    if (AIParkGuideMgr.Inst.CheckGuideSummaryEnd())
                    {
                        AIParkGuideMgr.Inst.RunGuide(S11GuideStep.Guide_FirstInGame_Guide_3_1);
                    }
                    else
                    {
                        ShowSummaryEvent();
                    }
                    break;
                case S11GameState.Exit:
                    // 停止计时器
                    StopGameTimer();
                    TrackData();
                    // 退出游戏
                    _isStartGame = false;
                    // 隐藏所有相关界面
                    _gameGuestPanel?.HidePanel();
                    AIParkTcpNetMgr.Instance.Release();
                    AIPark_CharacterManager.Inst.Release();
                    AIParkNpcLocationSyncUtil.Inst.Release();
                    MessageHelper.RemoveListener(MessageName.Park_GetNextSceneData, OnPreGetNextSceneData);
                    MessageHelper.RemoveListener(MessageName.Park_CountDownEnd, OnCountDownEnd);
                    break;

                default:
                    LoggerUtils.LogError($"未处理的游戏状态: {state}");
                    break;
            }
        }

        private void ReportGameResult()
        {
            S11GameReport req = new()
            {
                npcId = string.Empty,
                gameId = (int)PGCGameType.AIPark,
                duration = (int)(GameUtils.GetTimeStamp() - GetGameStartTime()),
                result = (int)0,
                conversationId = string.Empty,
                mapId = CurMapID
            };
            var paramStr = JsonConvert.SerializeObject(req);
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.AIResult, HttpMethod.POST, paramStr, (content) =>
            {
                LoggerUtils.Log("S11游戏结果上报成功");
            }, (err) => { LoggerUtils.Log("S11游戏结果上报失败", err); }, null, 0, 3);
            string localKey = AccountDataManager.Inst.UserInfo.uid + AIGameParkConfig.firstPlayMapIDKey;
            if (!isPgcEnter && string.IsNullOrEmpty(PlayerPrefs.GetString(localKey, string.Empty)))
            {
                PlayerPrefs.SetString(localKey, CurMapID);
            }
        }

        public void ExitGame()
        {
            AIParkPropsManager.Inst.SelfEnterIdle();
            ReportGameResult();
            OnStepChange(S11GameState.Exit);
            GameController.ExitGame(() =>
            {
                AIGameSoundUtils.Inst.StopAllSound();
                AIParkPropsManager.Inst.ExitGameFreeAllProps();
                AIParkSoundUtil.Inst.ReleaseAllSound();
                SimpleGameDurationManager.Inst.Release();
                UIManager.Inst.ForceSetOtherWindowTransInStack(WindowId.GuestWindow, true);
                UIManager.Inst.ClosePanel(PanelId.UIOperationOnWorldPanel);
                UIManager.Inst.BackToLastWindow();
                UIManager.Inst.OpenPanel(PanelId.GameEntryParkPanel, WindowId.RecommendWindow);
            });
        }

        public void ShowSelectEvent()
        {
            var sceneIdx = AIParkUtils.Inst.ParkCustomData.SceneIndex;
            UIManager.Inst.OpenPanel<AIParkGameEventPanel>(PanelId.AIParkGameEventPanel, WindowId.GuestWindow, AIParkUtils.Inst.ParkGameData.events[sceneIdx - 1], AIParkUtils.Inst.ParkGameData.events[sceneIdx - 1].EventOpts[0], AIParkUtils.Inst.ParkGameData.events[sceneIdx - 1].EventOpts[0].EventDuration);
            TriggerHistoryWhenCloseEventPanel();
            _gameGuestPanel.guestGroup.ShowEvent();
        }

        public void ShowSummaryEvent()
        {
            CurEventStep = 0; //完成
            AIParkTcpNetMgr.Instance.parkEndType = AIParkUtils.Inst.GetCompleteEndType();
            AIParkUtils.Inst.RecordEndTime();
            UIManager.Inst.OpenPanel<AIParkGameEndingPanel>(PanelId.AIParkGameEndingPanel, WindowId.GuestWindow, AIParkUtils.Inst.ParkGameData.Summary);
        }

        void TriggerHistoryWhenCloseEventPanel()
        {
            Action<BasePanel> ac = null;
            ac = new Action<BasePanel>((basePanel) =>
            {
                if (basePanel is AIParkGameEventPanel)
                {
                    AIPark_CharacterManager.Inst.TriggerHistoryState = true;
                    UIManager.Inst.RemoveClosePanelAction(ac);
                }
            });
            UIManager.Inst.AddClosePanelAction(ac);
        }

        private void StopGameTimer()
        {
            SimpleGameDurationManager.Inst.RecordPlayTimeCost();
            _costTime = SimpleGameDurationManager.Inst.GetPlayPassDurationData().duration;
            SimpleGameDurationManager.Inst.OnPassLevelStop();
        }

        #region 数据埋点上报
        private void TrackData()
        {
            if (isPgcEnter)
            {
                PlayerPrefs.SetInt(AccountDataManager.Inst.Uid + "_" + "PlayS11Pgc", 1);
            }
            else
            {
                PlayerPrefs.SetInt(AccountDataManager.Inst.Uid + "_" + "PlayS11Ugc", 1);
            }
            AIParkUtils.Inst.TrackAnalyticsData();
            AIParkUtils.Inst.canCalcBackAppTime = false;
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

            var guidePanel = UIManager.Inst.OpenPanel<AIParkGuidePanel>(PanelId.AIParkGuidePanel);
            guidePanel.SetEnterAct(() =>
            {
                guidePanel?.CloseSelf();
                StartGame();
            });
        }

        public long enterBgTimeStamp = 0; //进入后台的时间
        public long enterBgAllTime = 0; //进入后台后的所有时间

        public void OnApplicationFocus(bool focusStatus)
        {
            if(!focusStatus){
                enterBgTimeStamp = GameUtils.GetTimeStamp();
            }else{
                if(AIParkUtils.Inst.canCalcBackAppTime && enterBgTimeStamp > 0){
                    var costTime = GameUtils.GetTimeStamp() - enterBgTimeStamp;
                    enterBgAllTime += costTime;
                    Debug.Log("乐园OnApplicationFocus 进入后台后的所有时间:"+enterBgAllTime);
                }
                enterBgTimeStamp = 0;
            }
        }


        private void StartGame()
        {
            var loadingPanel = UIManager.Inst.FindPanel<AIGameLoadingPanel>(WindowId.CommonWindow, PanelId.AIGameLoadingPanel);
            loadingPanel?.CloseSelf();
            AIPark_CharacterManager.Inst.Init8PosData();
            AIParkUtils.Inst.RecordBeginTime();
            enterBgTimeStamp = 0;
            enterBgAllTime = 0;
            AIParkUtils.Inst.canCalcBackAppTime = true;
            // ResetNpc2DiscussPoint();
            AIGameCameraUtils.Inst.BackToPlayer();
            // var mainCameraTarget = GameObject.Find("CameraTarget");
            // mainCameraTarget.transform.localEulerAngles = new Vector3(30f, -65, 0);
            _isStartGame = true;


            SimpleGameDurationManager.Inst.Release();
            SimpleGameDurationManager.Inst.SetDuration(new GameDurationData
            {
                duration = AIParkUtils.Inst.ParkGameData.events[0].EventOpts[0].EventDuration
            }, DurationType.Park);
            _gameGuestPanel.LockInput();
            // SimpleGameDurationManager.Inst.CountDownStart(() =>
            {
                _gameGuestPanel.StartNpcFirstTalk();
                OnGuideOverAndStartPlay();
            }
            curGameTime = GameUtils.GetTimeStamp();


            // OnStepChange(S11GameState.NpcDiscuss);
        }

        void SceneAIDoActionWhenNpcDiscussEnd()
        {
            // AIPark_CharacterManager.Inst.SceneAIDoActionWhenNpcDiscussEnd();
            Debug.Log("乐园SceneAIDoActionWhenNpcDiscussEnd");
            var allNpcs = AIParkUtils.Inst.ParkGameData.GetAllNpcs();
            var actions = AIParkUtils.Inst.ParkGameData.actions;
            foreach (var npc in allNpcs)
            {
                var action = actions.FindLast(x => x.Participants.Contains(npc));
                if (action != null)
                {
                    var bev = AIPark_CharacterManager.Inst.GetNpc(npc);
                    if (bev != null)
                    {
                        //重新初始化章节信息
                        Debug.Log("乐园SceneAIDoActionWhenNpcDiscussEnd action:" + JsonConvert.SerializeObject(action));
                        bev.GetChapterHandler().ResetChapterData();
                        // if (npc == ((int)ParkNpcRoleType.Tilia).ToString())
                        // {
                        //     if (AIParkPropsManager.Inst.bBanTillaAction)
                        //     {
                        //         Debug.Log("乐园禁止蒂莉动作");
                        //         continue;
                        //     }
                        // }
                        AIPark_CharacterManager.Inst.AddSceneAICharacter(action);
                    }
                }
            }
        }

        private void StartNpcNewDiscuss()
        {
            List<(AICommonGameConfig_NPC, string, string, string)> Items = new();
            var sceneIdx = AIParkUtils.Inst.ParkCustomData.SceneIndex;
            AIGameAmusementParkSyncReply.Types.StoryEvent storyEvent = null;
            Debug.Log("乐园StartNpcNewDiscuss=" + AIParkUtils.Inst.ParkCustomData.isSummaryScene);
            if (AIParkUtils.Inst.ParkCustomData.isSummaryScene)
            {
                storyEvent = AIParkUtils.Inst.ParkGameData.summaryEvent;
            }
            else
            {
                storyEvent = AIParkUtils.Inst.ParkGameData.events[sceneIdx - 1];
            }
            Debug.Log("乐园storyEvent=" + JsonConvert.SerializeObject(storyEvent));
            var discusss = storyEvent.EventDiscuss;
            for (int i = 0; i < discusss.Count; i++)
            {
                var content = discusss[i].Content;
                Items.Add((null, discusss[i].Speaker, content, discusss[i].EmoteId));
            }
            var guestPanel = UIManager.Inst.FindPanel(PanelId.AIParkGuestPanel) as AIParkGuestPanel;
            if (guestPanel != null)
            {
                guestPanel.HidePanel();
            }
            Action<AIParkGameDialogPanel, string, string, string, int> ac = (panel, speaker, content, emoteId, time) =>
            {
                panel?.gameObject?.SetActive(false);
                var transform = AIPark_CharacterManager.Inst.GetNpc(speaker).transform;
                SetDiscussCamera(transform, time, () =>
                {
                    panel?.gameObject?.SetActive(true);
                    var speakerName = AIPark_NpcUtil.GetName(speaker);

                    _gameGuestPanel.SendNpcTalk(speakerName, content);
                    var realEmoteId = AIPark_CharacterManager.Inst.GetSpeakStateWhenDiscuss(speaker, emoteId);
                    var originEmoteId = AIPark_CharacterManager.Inst.GetOriginStateWhenDiscuss(speaker);
                    var emoteData = DataTables.GetEmoUIConfigList().Find((emoData) => emoData.pgcId == realEmoteId);
                    if (emoteData != null && emoteData.emoType != (int)EmoteSubType.Double && emoteData.emoType != (int)EmoteSubType.DoubleLoop)
                    {
                        var npcBehaviour = AIPark_CharacterManager.Inst.GetNpc(speaker);
                        npcBehaviour.PlayAnim(realEmoteId);

                        TimerManager.Inst.RunAndStop(_npcEmoteTimer);
                        _npcEmoteTimer = TimerManager.Inst.RunOnce("NpcPlayAnimEnd", 3, () =>
                        {
                            // npcBehaviour.PlayAnim(originEmoteId);
                        });
                    }
                    panel?.SetEmoteId(realEmoteId);
                    panel?.PlaySound();
                });
            };
            if (Items.Count > 0 && true)
            {
                (AICommonGameConfig_NPC, string, string, string) firstItem = Items[0];
                AIParkGameDialogPanel dialogPanel = UIManager.Inst.OpenPanel<AIParkGameDialogPanel>(PanelId.AIParkGameDialogPanel, WindowId.GuestWindow, Items);
                if (dialogPanel == null)
                {
                    dialogPanel = UIManager.Inst.FindPanel<AIParkGameDialogPanel>(WindowId.GuestWindow, PanelId.AIParkGameDialogPanel);
                }
                ac(dialogPanel, firstItem.Item2, firstItem.Item3, firstItem.Item4, 0);
                dialogPanel.SetShowDialogAc((speaker, content, emoteId) =>
                {
                    ac(dialogPanel, speaker, content, emoteId, 1);
                });
                dialogPanel.SetCloseAc(() =>
                {
                    OnStepChange(S11GameState.NpcDiscussEnd);
                });
            }
            else
            {
                Debug.Log("乐园没有讨论内容");
                OnStepChange(S11GameState.NpcDiscussEnd);
            }

        }

        private void StartNpcDiscuss()
        {
            AIParkUtils.Inst.ParkGameData.events.ForEach(x =>
            {
                if (x.EventType == (int)ParkEventType.Normal && x.EventDiscuss.Count > 0)
                {
                    _curDiscussId = x.EventDiscuss[0].Speaker;
                    MessageHelper.Broadcast<string, string>(MessageName.OnParkBeginNpcTalk, x.EventDiscuss[0].Speaker, x.EventDiscuss[0].Content);
                }
            });
        }

        public void TriggerNpcNextDiscuss()
        {
            bool canTrigger = false;
            bool doTrigger = false;
            if (AIParkUtils.Inst.ParkGameData.events.Count > 0)
            {
                var eventDiscuss = AIParkUtils.Inst.ParkGameData.events[0].EventDiscuss;
                for (int i = 0; i < eventDiscuss.Count; i++)
                {
                    if (eventDiscuss[i].Speaker == _curDiscussId)
                    {
                        canTrigger = true;
                    }
                    if (canTrigger && !doTrigger)
                    {
                        if (_curDiscussId != eventDiscuss[i].Speaker)
                        {
                            _curDiscussId = eventDiscuss[i].Speaker;
                            MessageHelper.Broadcast<string, string>(MessageName.OnParkBeginNpcTalk, eventDiscuss[i].Speaker, eventDiscuss[i].Content);
                            doTrigger = true;
                        }
                    }
                }
            }
            if (!doTrigger)
            {
                // _gameGuestPanel.ShowPanel();
                OnStepChange(S11GameState.NpcDiscussEnd);

                // EnterBlackPanel(1,0,()=>{
                //     OnStepChange(S11GameState.EnterConsultingRoom);
                // });
            }
        }



        public void EnterBlackPanel(float blackTime = 1, float beginAlpha = 0, Action fadeOutAction = null, Action fadeInAction = null)
        {
            LoggerUtils.Log("乐园EnterBlackPanel 进入黑幕");
            Action ac = () =>
            {
                LoggerUtils.Log("乐园EnterBlackPanel 黑幕出来");
                fadeInAction?.Invoke();
            };
            UIManager.Inst.OpenPanel<AIParkBlackPanel>(PanelId.AIParkBlackPanel, blackTime, beginAlpha, fadeOutAction, ac);
        }

        //引导完成并开始游戏
        private void OnGuideOverAndStartPlay()
        {
            SetSelfPlayerState(true);
            if (isPgcEnter)
            {
                if (PlayerPrefs.GetInt(AccountDataManager.Inst.Uid + AIGameHospitalConfig.pgcGuideId, 0) <= 0)
                {
                    //屏蔽引导
                    // UIManager.Inst.OpenPanel<AIHospitalStrongGuide>(PanelId.AIHospitalStrongGuidePanel, AIGameHospitalConfig.EPgcGuideID.TargetTips);
                }
            }

            var ugc = GameDataManager.Inst.mapGlobalData?.curUgcBaseInfo?.gameSetting?.bgMusicUrl;
            if (!string.IsNullOrEmpty(ugc))
            {
                BgMusicManager.Inst.SetUGCMusic(ugc);
                BgMusicManager.Inst.PlayUGCMusic();
            }
            else
            {
                AIGameSoundUtils.Inst.PlayBgm(AIParkConfig.Bgm_S11Para_MainScene);
            }
            AIParkSoundUtil.Inst.PlayEvironmentSound();

            if (!isPgcEnter)
            {
                ResetNpc2DiscussPoint();
                CurEventStep = 1;
                OnStepChange(S11GameState.Event_Os);
            }
            else
            {
                int curStep = BootDataManager.Inst.GetParkInGame();
                AIParkGuideMgr.Inst.Init(this);
                if (false || curStep == 0)
                {
                    //从未开始引导
                    AIParkGuideMgr.Inst.RunGuide(S11GuideStep.Guide_FirstInGame_Guide_1_1);
                }
                else
                {
                    ResetNpc2DiscussPoint();
                    CurEventStep = 1;
                    OnStepChange(S11GameState.Event_Os);
                }
            }
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
            OnStepChange(S11GameState.Fail);
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
            // if (ugcTarget.TryGetValue(npcID, out S9UgcTaskData value))
            // {
            //     if (value.isFinish == bFinish) return;
            //     needCheck = true;
            //     value.isFinish = bFinish;
            //     if (bFinish)
            //     {
            //         UnLoadEffectForUgcNpc(value);
            //         var behaviour = AIHospital_CharacterManager.Inst.GetNpc(npcID);
            //         if (behaviour)
            //         {
            //             behaviour.SetIsCurrentTarget(false);
            //             LoggerUtils.Log($"{behaviour.GetNpcName()} 被成功说服");
            //         }
            //     }
            // }
            // else
            // {
            //     LoggerUtils.Log($"无法找到关联的npc，id={npcID}");
            // }
            // if (needCheck && CheckAllUgcTargetFinish())
            // {
            //     MessageHelper.Broadcast(MessageName.OnS9UgcTargetFinished);
            // }
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
        public void UpdatePgcNpcState(ParkNpcRoleType roleType, bool bFinish)
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
                    OnStepChange(S11GameState.Success);
                }
                else
                {
                    CurrentStep = ++step;
                    if (isPgcEnter)
                        GameAINpcChatManager.Inst.ClearTempConversitionID(CurrentStep);
                }
            }
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



        public string GetNpcName(string npcId)
        {
            return AIPark_NpcUtil.GetName(npcId);
        }


        public LocationType GetLocationTypeBy()
        {
            LocationType locationType = AIParkUtils.Inst.DiscussLocationType;
            if (AIParkUtils.Inst.ParkCustomData.SceneIndex == 1)
            {
                locationType = LocationType.Park;
            }
            else if (AIParkUtils.Inst.ParkCustomData.SceneIndex == 2)
            {
                locationType = LocationType.TrojanHorse;
            }
            else if (AIParkUtils.Inst.ParkCustomData.SceneIndex == 3)
            {
                locationType = LocationType.Park;
            }
            return locationType;
        }

        public List<ActionType> GetAvailableActionTypes(LocationType locationType)
        {
            List<ActionType> actionTypes = new List<ActionType>();
            actionTypes.Add(ActionType.Swinging);
            actionTypes.Add(ActionType.SeeSaw);
            actionTypes.Add(ActionType.SlideSlides);
            actionTypes.Add(ActionType.Idle);
            actionTypes.Add(ActionType.SitOnBench);
            return actionTypes; //暂时不限制
            if (locationType == LocationType.Park)
            {
                actionTypes.Add(ActionType.Swinging);
                actionTypes.Add(ActionType.SeeSaw);
                actionTypes.Add(ActionType.SlideSlides);
                actionTypes.Add(ActionType.Idle);
                actionTypes.Add(ActionType.SitOnBench);
            }
            else if (locationType == LocationType.TrojanHorse)
            {
                actionTypes.Add(ActionType.Idle);
                actionTypes.Add(ActionType.TrojanHorse);
                actionTypes.Add(ActionType.SitOnBench);
            }
            else
            {
                actionTypes.Add(ActionType.Idle);
                actionTypes.Add(ActionType.SitOnBench);
            }
            return actionTypes;
        }

        public void ResetNpc2DiscussPoint()
        {
            LocationType locationType = AIParkUtils.Inst.DiscussLocationType;
            var random = UnityEngine.Random.Range(1, 3);
            if (random == 1)
            {
                locationType = LocationType.Park;
            }
            else if (random == 2)
            {
                locationType = LocationType.Stage;
            }
            else
            {
                locationType = LocationType.Fountain;
            }

            // if (AIParkUtils.Inst.ParkCustomData.SceneIndex == 1)
            // {
            //     locationType = LocationType.Park;
            // }
            // else if (AIParkUtils.Inst.ParkCustomData.SceneIndex == 2)
            // {
            //     locationType = LocationType.TrojanHorse;
            // }
            // else if (AIParkUtils.Inst.ParkCustomData.SceneIndex == 3)
            // {
            //     locationType = LocationType.Park;
            // }
            // else
            // {
            //     locationType = LocationType.Park;
            // }
            AIParkUtils.Inst.DiscussLocationType = locationType;
            AIParkPropsManager.Inst.SetAvailableActionTypes(GetAvailableActionTypes(locationType));
            //
            var selfSpawnPoint = AIPark_CharacterUtils.Inst.GetSelfSpawnPointByLocationType(locationType);
            AvatarController.Inst.SelfController.Motor.SetPositionAndRotation(selfSpawnPoint.pos, Quaternion.Euler(selfSpawnPoint.rot));
            //重置章节行为为idle
            AIPark_CharacterManager.Inst.ResetAllHistoryState();
            AIPark_CharacterManager.Inst.ResetNpc2Idle();
            AIParkPropsManager.Inst.SelfEnterIdle();
            AIPark_CharacterManager.Inst.ExitInterruptState();
            AIPark_CharacterManager.Inst.PauseAllNpcChapter();
            // AIPark_CharacterManager.Inst.ResetNpc2DiscussPointOnCircle(locationType, 2.2f);
            AIPark_CharacterManager.Inst.NewResetNpc2DiscussPoint(locationType);

            Debug.Log("乐园重置了行为");
        }
        //设置旁白镜头
        public void SetAsideCamera()
        {
            var (cameraBeginPosition, cameraEndPosition, direction) = GetAsideCameraBeginEndDirectPos();
            AIGameCameraUtils.Inst.SetCamLookAt(cameraEndPosition, direction, 0, 6, null);
            // 
            AIPark_CharacterUtils.Inst.CanContinueOsDialog = false;
            // 设置相机
            TimerManager.Inst.RunOnce("SetAsideCameraMoveEndPos", 1f, () =>
            {
                AIPark_CharacterUtils.Inst.CanContinueOsDialog = true;
                AIGameCameraUtils.Inst.SetCamLookAt(cameraEndPosition, direction, 1.2f, 0.01f, null);
            });
        }


        public (Vector3, Vector3, Vector3) GetAsideCameraBeginEndDirectPos()
        {
            float cameraHeightOffset = 0.9f;  // 相机高度偏移
            float forwardDistance = 2.41f;    // 前向距离
            float upDistance = 1.44f;         // 上向距离
            float rightDistance = 0f;         // 右向距离
            float cameraDistance = 3f;      // 相机距离
                                            // 计算相机位置：NPC位置 + 高度偏移
            var npcTransform = AvatarController.Inst.SelfController.transform;
            Vector3 cameraPosition = npcTransform.position + Vector3.up * cameraHeightOffset;

            // 计算相机朝向：基于NPC的本地坐标系
            Vector3 cameraForward = -(npcTransform.forward * forwardDistance +
                                    npcTransform.up * upDistance +
                                    npcTransform.right * rightDistance -
                                    npcTransform.up * cameraHeightOffset);
            // AIGameCameraUtils.Inst.SetCamLookAt(cameraPosition, cameraForward, time, cameraDistance, onComplete);


            Vector3 cameraEndPosition = cameraPosition - cameraForward.normalized * cameraDistance;
            Vector3 direction = (cameraPosition - cameraEndPosition).normalized;
            Vector3 cameraBeginPosition = cameraEndPosition + direction * 3;
            return (cameraBeginPosition, cameraEndPosition, direction);
        }

        public void SetDiscussCamera(Transform npcTransform, int time = 1, Action onComplete = null)
        {
            float cameraHeightOffset = 0.9f;  // 相机高度偏移
            float forwardDistance = 3.41f;    // 前向距离
            float upDistance = 1.44f;         // 上向距离
            float rightDistance = 0f;         // 右向距离
            float cameraDistance = 3f;      // 相机距离
                                            // 计算相机位置：NPC位置 + 高度偏移
            Vector3 cameraPosition = npcTransform.position + Vector3.up * cameraHeightOffset;

            // 计算相机朝向：基于NPC的本地坐标系
            Vector3 cameraForward = -(npcTransform.forward * forwardDistance +
                                    npcTransform.up * upDistance +
                                    npcTransform.right * rightDistance -
                                    npcTransform.up * cameraHeightOffset);

            // 设置相机
            AIGameCameraUtils.Inst.SetCamLookAt(cameraPosition, cameraForward, time, cameraDistance, onComplete);
        }

    }
}
