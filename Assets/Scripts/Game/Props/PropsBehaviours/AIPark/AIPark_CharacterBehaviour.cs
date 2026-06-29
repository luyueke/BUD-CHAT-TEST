using System;
using System.Collections.Generic;
using Basic.Utils;
using Es;
using Game.Avatar;
using Game.Base;
using Game.KinematicCharacter;
using Game.Props.PropsBehaviours.AIPark;
using Game.Props.PropsManagers;
using Game.Props.PropsManagers.AIGames.AIPark.FSM;
using GameData.BaseInfo;
using GameData.PgcData;
using Message;
using UIAgent;
using UnityEngine;
using UnityEngine.AI;
using UI.Catalog;
using Pb.Game;
using System.Collections;
using Game.Props.PropsBehaviours;

namespace Game.Props
{
    public class AIPark_CharacterBehaviour : NodeBaseBehaviour
    {
        #region 身份数据
        private ParkNpcRoleType _curNpcRoleType = ParkNpcRoleType.Elise;
        private ParkNPCType _curNpcType = ParkNPCType.Default;
        private string _curNpcId;

        public string _curNpcName;
        private AIPark_ChapterData _curChapterData;
        private bool PGC_ENTER = true; // 是否为官方模式

        public float _decisionRate = 0.2f;
        public bool CanShowInteractBtn = true;
        public bool IsNpcTalking = false;
        #endregion

        #region 章节行为管理
        private AIChapterHandler _npcChapterHandler;
        private float _elapsedTime;
        public bool _isInitialized = false;
        #endregion

        #region 剧情行为管理
        private LocationType _curLocationType = LocationType.None;
        private ActionType _curActionType = ActionType.None;
        #endregion

        #region 导航相关
        private bool _isAgentInit = false;
        private NavMeshAgent _npcAgent;
        private NavMeshHit _npcNavHit;
        private float _npcInitSpeed = 2.2f;
        private Action _destinationReachedCallback;
        private bool _isCheckingDestination = false;

        // 新增：移动超时处理
        private bool _isMovingWithTimeout = false;
        private float _moveStartTime = 0f;
        private float _moveTimeout = 10f; // 默认10秒超时
        private Action _timeoutCallback = null;

        public bool bIsEnterInterruptState = false;

        // 新增：KCC路径跟随器
        private KCCPathFollower _pathFollower;
        #endregion

        #region Avatar相关数据
        public KinematicCharacterController _npcKccCtr;
        public PlayerAnimationCtrl _npcAnimController;
        public SelfStateController _selfStateController;
        public OtherStateController _npcStateController;

        public PlayerStateController playerStateControllerState
        {
            get
            {
                if (_selfStateController != null)
                {
                    return _selfStateController;
                }
                if (_npcStateController != null)
                {
                    return _npcStateController;
                }
                return null;
            }
        }
        public Transform _npcCamFollowCenter;
        private string RHandName = "Bip001 R Hand";
        private string LHandName = "Bip001 L Hand";
        private string bottomBoneName = "Bone138";
        private string headBoneName = "Bip001 Head";
        private Transform RHandTrans;
        private Transform LHandTrans;
        /// <summary>
        /// 脚下的骨骼点位
        /// </summary>
        private Transform bottomBone;
        private Transform headBone;
        #endregion

        #region 记录npc新的位置行为
        private RecordNpcHistory _recordNpcHistory = new();
        #endregion

        #region 交互设置
        [Header("交互设置")]
        [SerializeField] private float detectionRadius = 2f;        // 检测半径
        [SerializeField] private float detectionAngle = 120f;      // 检测角度
        [SerializeField] private LayerMask characterLayer;         // 角色层
        [SerializeField] private float rotationSpeed = 5f;         // 旋转速度
        [SerializeField] private LineRenderer detectionLineRenderer; // 扇形区域渲染器
        [SerializeField] private int lineSegments = 30;            // 扇形线段数量
        [SerializeField] private Color lineColor = new Color(1f, 0f, 0f, 0.3f); // 扇形颜色

        private bool isInteracting = false;                        // 是否正在交互
        private AIPark_CharacterBehaviour currentInteractTarget;   // 当前交互目标
        public bool isDetectionEnabled = false;                   // 是否启用检测
        #endregion

        #region 初始化导航相关功能
        private void InitNavAgent()
        {
            if (_isAgentInit)
                return;

            _npcAgent = GetComponent<NavMeshAgent>();
            if (_npcAgent == null)
            {
                _npcAgent = gameObject.AddComponent<NavMeshAgent>();
                _npcAgent.speed = _npcInitSpeed;
                _npcAgent.angularSpeed = 5000;
                _npcAgent.radius = 0.25f;
                _npcAgent.height = 1.1f;
            }

            // 如果使用KCC路径跟随器，则禁用NavMesh Agent的位置更新
            _npcAgent.updatePosition = false;
            _npcAgent.updateRotation = false;
            _npcAgent.enabled = true;

            _isAgentInit = true;
        }

        private void InitPathFollower()
        {
            // 获取或添加KCC路径跟随器
            _pathFollower = GetComponent<PropsBehaviours.AIPark.KCCPathFollower>();
            if (_pathFollower == null)
            {
                _pathFollower = gameObject.AddComponent<PropsBehaviours.AIPark.KCCPathFollower>();

                // 设置路径跟随器参数
                _pathFollower.moveSpeed = _npcInitSpeed;
                _pathFollower.rotationSpeed = 360f;
                _pathFollower.pathRecalculationInterval = 0.5f; // 每0.5秒重新计算一次路径
                _pathFollower.maxPathDeviation = 1.0f; // 最大偏离距离1米
                _pathFollower.debugDrawPath = true; // 开启路径调试
            }
        }

        public bool IsFollowPath(){
            return _pathFollower != null && _pathFollower.IsFollowingPath;
        }

        /// <summary>
        /// 开始巡逻
        /// </summary>
        public void StartPortal()
        {
            _isInitialized = true;
        }

        /// <summary>
        /// 初始化角色行为
        /// </summary>
        public void InitData(ParkCharacterScriptData data)
        {
            // _curNpcType = (ParkNPCType)data.npcType;
            _curNpcId = data.npcId;
            _curNpcName = data.name;

            _recordNpcHistory.npcId = _curNpcId; //npc的行为

            _npcKccCtr = this.GetComponentInChildren<KinematicCharacterController>();
            _npcAnimController = this.GetComponentInChildren<PlayerAnimationCtrl>();
            _selfStateController = this.GetComponentInChildren<SelfStateController>();
            _npcStateController = this.GetComponentInChildren<OtherStateController>();


            _npcChapterHandler = new AIChapterHandler(null, this);
            _elapsedTime = 0f;


            RHandTrans = GameUtils.FindChildByName(this._npcAnimController.Wrap.Avatar.transform, RHandName);
            LHandTrans = GameUtils.FindChildByName(this._npcAnimController.Wrap.Avatar.transform, LHandName);
            bottomBone = GameUtils.FindChildByName(this._npcAnimController.Wrap.Avatar.transform, bottomBoneName);
            headBone = GameUtils.FindChildByName(this._npcAnimController.Wrap.Avatar.transform, headBoneName);

            IsCanClick = false;
            AddListener();
        }
        /// <summary>
        /// 插入章节数据 改变npc行为
        /// </summary>
        // public void InsertChapterData(AIPark_ChapterData aIPark_ChapterData){
        //     _npcChapterHandler.CheckChangeState(aIPark_ChapterData);
        // }
        public void InsertHistory(History history)
        {
            if (history == null)
            {
                //正常没有为空的情况
                LoggerUtils.LogError($"AIPark_CharacterBehaviour: {_curNpcName} 插入剧情数据为空");
                //todo 这里可能要考虑当为空的时候 npc继续上一个行为
                return;
            }

            _recordNpcHistory.InsertHistory(history);
            InsertChapterData(history);
        }

        public void ForceEnterCurHistory()
        {
            //目前只给引导tilia用
            if (GetNpcID() == ((int)ParkNpcRoleType.Tilia).ToString())
            {
                var curHistory = _recordNpcHistory.lastHistory;
                if (curHistory != null)
                {
                    InsertChapterData(curHistory);
                }
            }
        }

        public void ForceEnterLastHistory(bool bQuickEnter = false)
        {
            var lastChapterData = GetChapterHandler().GetLastChapterData();
            if (lastChapterData != null)
            {
                lastChapterData.bQuickEnter = bQuickEnter;
                _npcChapterHandler.CheckChangeState(lastChapterData);
            }
        }

        public void CheckReEnterLastChapter(bool bQuickEnter = false)
        {
            var lastChapterData = GetChapterHandler().GetLastChapterData();
            if (lastChapterData != null)
            {
                lastChapterData.bQuickEnter = bQuickEnter;
                Debug.Log(GetNpcID()+"重新回到上一个章节:" + lastChapterData.location + " 章节动作:" + lastChapterData.action);
                _npcChapterHandler.CheckChangeState(lastChapterData);
            }
            bIsEnterInterruptState = false;
        }

        public void CheckReEnterCurChapter(bool bQuickEnter = false)
        {
            var curChapterData = GetChapterHandler().GetChapterData();
            if (curChapterData != null)
            {
                curChapterData.bQuickEnter = bQuickEnter;
                Debug.Log(GetNpcID()+"重新回到当前章节:" + curChapterData.location + " 章节动作:" + curChapterData.action);
                _npcChapterHandler.CheckChangeState(curChapterData);
            }
        }

        public void InsertChapterData(History history)
        {
            if(bIsEnterInterruptState){
                //打断状态下 不插入行动剧情
                AIPark_CharacterManager.Inst.UpdateHistoryCompleteStep(_curNpcId, true);
                return;
            }
            if (AIParkPropsManager.Inst.GetBanDoCustomAction())
            {
                AIPark_CharacterManager.Inst.UpdateHistoryCompleteStep(_curNpcId, true);
                return;
            }
            if (GetNpcID() == ((int)ParkNpcRoleType.Tilia).ToString())
            {
                if (AIParkPropsManager.Inst.bBanTillaAction)
                {
                    AIPark_CharacterManager.Inst.UpdateHistoryCompleteStep(_curNpcId, true);
                    return;
                }
            }
            AIPark_CharacterManager.Inst.UpdateHistoryCompleteStep(_curNpcId, true);
            AIPark_ChapterData aIPark_ChapterData = new AIPark_ChapterData(history);
            _npcChapterHandler.CheckChangeState(aIPark_ChapterData);

        }

        public void AddListener()
        {
            //todo 监听引导步骤，实现不同步骤的表现
            MessageHelper.AddListener<int, bool>(MessageName.OnS9GuideStepClick, OnS9GuideStepClick);
        }

        public void RemoveListener()
        {
            MessageHelper.RemoveListener<int, bool>(MessageName.OnS9GuideStepClick, OnS9GuideStepClick);
        }
        #endregion


        /// <summary>
        /// 
        /// </summary>
        private readonly List<string> dustManAvatars = new List<string>()
        {
            "10400463","11000158"
        };

        /// <summary>
        /// npc点击次数，来判定具体的交互
        /// </summary>
        private int _clickCount;

        #region 生命周期
        private void Awake()
        {
            InitNavAgent();
            InitPathFollower();
            IsNpcTalking = false;
        }

        /// <summary>
        /// 在GameObject启用时执行的方法
        /// 用于确保NPC状态在隐藏后再显示时正确恢复
        /// </summary>
        private void OnEnable()
        {
            try
            {
                // 重新初始化导航组件，确保它们处于正确状态
                InitNavAgent();
                InitPathFollower();

                // 确保路径跟随器的引用有效
                if (_pathFollower != null)
                {
                    _pathFollower.InitializeComponentsPublic();
                }
                else
                {
                    LoggerUtils.Log($"AIPark_CharacterBehaviour: {_curNpcName} OnEnable时_pathFollower为null，尝试重新获取");
                    _pathFollower = GetComponent<PropsBehaviours.AIPark.KCCPathFollower>();

                    if (_pathFollower == null)
                    {
                        LoggerUtils.LogError($"AIPark_CharacterBehaviour: {_curNpcName} 无法获取KCCPathFollower组件");
                    }
                }

                // 如果NPC已经初始化过，则尝试恢复其状态
                if (_isInitialized && _npcChapterHandler != null)
                {
                    LoggerUtils.Log($"AIPark_CharacterBehaviour: {_curNpcName} OnEnable时恢复状态");

                    // 如果之前在移动中，则停止移动（避免可能的异常状态）
                    if (_isMovingWithTimeout)
                    {
                        _isMovingWithTimeout = false;

                        if (_pathFollower != null)
                        {
                            _pathFollower.StopFollowing();
                        }
                    }

                    // 恢复动画状态为默认
                    if (_npcAnimController != null)
                    {
                        _npcAnimController.SetPlayerAniState(PlayerAniState.Idle, false);
                    }

                    // 0.5秒后重新计算章节状态，给所有组件留出初始化时间
                    try
                    {
                        TimerManager.Inst.RunOnce("RecalculateChapterOnEnable_" + _curNpcName, 0.5f, () =>
                        {
                            RecalculateChapter(0);
                        });
                    }
                    catch (System.Exception ex)
                    {
                        LoggerUtils.LogError($"AIPark_CharacterBehaviour: {_curNpcName} 设置定时器时出错: {ex.Message}");
                    }
                }
            }
            catch (System.Exception ex)
            {
                LoggerUtils.LogError($"AIPark_CharacterBehaviour: {_curNpcName} OnEnable时出错: {ex.Message}");
            }
        }

        private void OnDisable()
        {
            // 当GameObject被禁用时取消所有进行中的计时器
            StopRecalculateTimer();

            // 如果正在移动，停止移动
            if (_isMovingWithTimeout && _pathFollower != null)
            {
                _pathFollower.StopFollowing();
                _isMovingWithTimeout = false;
            }
        }

        private void Update()
        {
            if (!_isInitialized)
                return;

            float deltaTime = Time.deltaTime;
            _elapsedTime += deltaTime;
            _npcChapterHandler.Update(_elapsedTime, deltaTime);

            // 如果使用的是原始NavMesh Agent方式，则需要检查是否到达目的地
            if (_isCheckingDestination && _pathFollower == null)
            {
                if (!_npcAgent.pathPending && _npcAgent.remainingDistance <= _npcAgent.stoppingDistance)
                {
                    _isCheckingDestination = false;
                    _destinationReachedCallback?.Invoke();
                }
            }

            // 检查移动超时
            CheckMoveTimeout();

            // 只有在启用检测且未交互时才检测(暂时屏蔽npc寻找npc/玩家对话)
            if (false && isDetectionEnabled && !isInteracting)
            {
                CheckForCharactersInFront();
                if (Application.isEditor)
                {
                    UpdateSectorVisualization();
                }
            }

        }

        private void OnDestroy()
        {
            RemoveListener();
            StopRecalculateTimer();
        }

        /// <summary>
        /// 检查移动是否超时
        /// </summary>
        private void CheckMoveTimeout()
        {
            if (_isMovingWithTimeout)
            {
                // 安全检查，确保_pathFollower不为空
                if (_pathFollower == null)
                {
                    LoggerUtils.LogError($"AIPark_CharacterBehaviour: {_curNpcName} CheckMoveTimeout时_pathFollower为null，重置移动状态");
                    _isMovingWithTimeout = false;
                    _timeoutCallback?.Invoke();
                    _timeoutCallback = null;
                    return;
                }

                float elapsedTime = Time.time - _moveStartTime;

                // 如果已经到达或者停止移动，重置超时标志
                if (!_pathFollower.IsFollowingPath || _pathFollower.HasReachedDestination)
                {
                    _isMovingWithTimeout = false;
                    return;
                }

                // 检查是否超时
                if (elapsedTime > _moveTimeout)
                {
                    LoggerUtils.LogError($"AIPark_CharacterBehaviour: {_curNpcName} 移动超时 ({_moveTimeout}秒)，强制触发回调并停止移动");

                    // 停止移动
                    StopMoving();

                    // 触发回调
                    _timeoutCallback?.Invoke();

                    // 重置状态
                    _isMovingWithTimeout = false;
                    _timeoutCallback = null;
                }
            }
        }

        /// <summary>
        /// 移动到指定位置，并在到达后执行回调，包含超时保护
        /// </summary>
        /// <param name="targetPosition">目标位置</param>
        /// <param name="onDestinationReached">到达目的地后的回调函数</param>
        /// <param name="moveSpeed">移动速度(可选)</param>
        /// <param name="timeout">超时时间(秒)，默认10秒</param>
        public void MoveToPositionSafe(Vector3 targetPosition, Action onDestinationReached = null, float moveSpeed = 0, float timeout = 10f)
        {
            // 设置超时保护
            _isMovingWithTimeout = true;
            _moveStartTime = Time.time;
            _moveTimeout = timeout;
            _timeoutCallback = onDestinationReached;

            // 使用常规移动方法
            MoveToPosition(targetPosition, onDestinationReached, moveSpeed);
        }


        //移动到目标附近即回调
        public void MoveToPositionNear(Vector3 targetPosition, Action<bool> onDestinationNearReached = null, float moveSpeed = 0, float checkDistance = 2f)
        {
            try
            {
                // 如果路径跟随器为null，尝试重新获取它
                if (_pathFollower == null)
                {
                    LoggerUtils.LogError($"AIPark_CharacterBehaviour: {_curNpcName} MoveToPositionNear时_pathFollower为null，尝试重新初始化");
                    InitPathFollower();

                    if (_pathFollower == null)
                    {
                        LoggerUtils.LogError($"AIPark_CharacterBehaviour: {_curNpcName} 无法初始化PathFollower，直接调用回调");
                        onDestinationNearReached?.Invoke(false);
                        return;
                    }
                }

                // 计算距离
                float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
                // float maxDistance = 50.0f;  // 超过50米取消移动  //乐园不需要这个限制

                // if (distanceToTarget > maxDistance)
                // {
                //     LoggerUtils.LogError($"AIPark_CharacterBehaviour: {_curNpcName} 移动距离过远 ({distanceToTarget:F2}米)，超过最大距离限制，直接调用回调");
                //     onDestinationNearReached?.Invoke(false);
                //     return;
                // }

                // 设置路径跟随器参数
                if (moveSpeed > 0)
                {
                    _pathFollower.moveSpeed = moveSpeed;
                }

                // 创建包装的回调，在即将到达时检查目标位置
                Action wrappedCallback = () =>
                {
                    // 在即将到达时检查目标位置是否仍然可达
                    float currentDistanceToTarget = Vector3.Distance(transform.position, targetPosition);

                    if (currentDistanceToTarget <= checkDistance)
                    {
                        StopMoving();
                        onDestinationNearReached?.Invoke(true);
                        // if (CheckCanMoveToPosition(targetPosition))
                        // {
                        //     StopMoving();
                        //     onDestinationNearReached?.Invoke(true);
                        // 目标位置可达，继续移动到目标位置
                        // LoggerUtils.Log($"AIPark_CharacterBehaviour: {_curNpcName} 目标位置可达，继续移动到最终位置");
                        // MoveToPosition(targetPosition, () =>
                        // {
                        //     StopMoving();
                        //     onDestinationNearReached?.Invoke(true);
                        // }, moveSpeed);
                        // }
                        // else
                        // {
                        //     // 目标位置不可达，停在当前位置并调用回调
                        //     LoggerUtils.Log($"AIPark_CharacterBehaviour: {_curNpcName} 目标位置不可达，停在当前位置 {transform.position}");
                        //     StopMoving();
                        //     onDestinationNearReached?.Invoke(false);
                        // }
                    }
                };

                // 开始移动到目标位置
                _pathFollower.MoveToPosition(targetPosition, () =>
                {
                    StopMoving();
                    onDestinationNearReached?.Invoke(true);
                }, wrappedCallback);
            }
            catch (System.Exception ex)
            {
                LoggerUtils.LogError($"AIPark_CharacterBehaviour: {_curNpcName} MoveToPositionNear时出错: {ex.Message}");
                onDestinationNearReached?.Invoke(false);
            }
        }


        public void MoveToPositionNearAndCheck(Vector3 targetPosition, Action<bool> onDestinationNearReached = null, float moveSpeed = 0)
        {
            try
            {
                // 如果路径跟随器为null，尝试重新获取它
                if (_pathFollower == null)
                {
                    LoggerUtils.LogError($"AIPark_CharacterBehaviour: {_curNpcName} MoveToPositionNear时_pathFollower为null，尝试重新初始化");
                    InitPathFollower();

                    if (_pathFollower == null)
                    {
                        LoggerUtils.LogError($"AIPark_CharacterBehaviour: {_curNpcName} 无法初始化PathFollower，直接调用回调");
                        onDestinationNearReached?.Invoke(false);
                        return;
                    }
                }

                // 计算距离
                float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
                float maxDistance = 50.0f;  // 超过50米取消移动

                if (distanceToTarget > maxDistance)
                {
                    LoggerUtils.LogError($"AIPark_CharacterBehaviour: {_curNpcName} 移动距离过远 ({distanceToTarget:F2}米)，超过最大距离限制，直接调用回调");
                    onDestinationNearReached?.Invoke(false);
                    return;
                }

                // 设置路径跟随器参数
                if (moveSpeed > 0)
                {
                    _pathFollower.moveSpeed = moveSpeed;
                }

                float checkDistance = 0.5f; // 当距离目标1米时进行检查
                // 创建包装的回调，在即将到达时检查目标位置
                Action wrappedCallback = () =>
                {
                    // 在即将到达时检查目标位置是否仍然可达
                    float currentDistanceToTarget = Vector3.Distance(transform.position, targetPosition);

                    if (currentDistanceToTarget <= checkDistance)
                    {
                        if (CheckCanMoveToPosition(targetPosition))
                        {
                            // 目标位置可达，继续移动到目标位置
                            LoggerUtils.Log($"AIPark_CharacterBehaviour: {_curNpcName} 目标位置可达，继续移动到最终位置");
                            MoveToPosition(targetPosition, () =>
                            {
                                StopMoving();
                                onDestinationNearReached?.Invoke(true);
                            }, moveSpeed);
                        }
                        else
                        {
                            // 目标位置不可达，停在当前位置并调用回调
                            LoggerUtils.Log($"AIPark_CharacterBehaviour: {_curNpcName} 目标位置不可达，停在当前位置 {transform.position}");
                            StopMoving();
                            onDestinationNearReached?.Invoke(false);
                        }
                    }
                };

                // 开始移动到目标位置
                _pathFollower.MoveToPosition(targetPosition, () =>
                {
                    StopMoving();
                    onDestinationNearReached?.Invoke(true);
                }, wrappedCallback);
            }
            catch (System.Exception ex)
            {
                LoggerUtils.LogError($"AIPark_CharacterBehaviour: {_curNpcName} MoveToPositionNear时出错: {ex.Message}");
                onDestinationNearReached?.Invoke(false);
            }
        }

        /// <summary>
        /// 移动到指定位置，并在到达后执行回调
        /// </summary>
        /// <param name="targetPosition">目标位置</param>
        /// <param name="onDestinationReached">到达目的地后的回调函数</param>
        /// <param name="moveSpeed">移动速度(可选)</param>
        public void MoveToPosition(Vector3 targetPosition, Action onDestinationReached = null, float moveSpeed = 0)
        {
            try
            {
                // 如果路径跟随器为null，尝试重新获取它
                if (_pathFollower == null)
                {
                    LoggerUtils.LogError($"AIPark_CharacterBehaviour: {_curNpcName} MoveToPosition时_pathFollower为null，尝试重新初始化");
                    InitPathFollower();

                    if (_pathFollower == null)
                    {
                        LoggerUtils.LogError($"AIPark_CharacterBehaviour: {_curNpcName} 无法初始化PathFollower，取消移动");
                        onDestinationReached?.Invoke();
                        return;
                    }
                }

                // 计算距离，如果距离太远，直接报警告
                float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
                float warnDistance = 20.0f; // 超过20米给出警告
                float maxDistance = 50.0f;  // 超过50米取消移动

                if (distanceToTarget > maxDistance)
                {
                    LoggerUtils.LogError($"AIPark_CharacterBehaviour: {_curNpcName} 移动距离过远 ({distanceToTarget:F2}米)，超过最大距离限制，取消移动");
                    onDestinationReached?.Invoke();
                    return;
                }
                else if (distanceToTarget > warnDistance)
                {
                    LoggerUtils.Log($"AIPark_CharacterBehaviour: {_curNpcName} 移动距离较远 ({distanceToTarget:F2}米)");
                }

                // 如果角色不在NavMesh上，尝试调整位置
                NavMeshHit navHit;
                bool onNavMesh = NavMesh.SamplePosition(transform.position, out navHit, 5.0f, NavMesh.AllAreas);
                if (!onNavMesh)
                {
                    // 记录调试信息
                    LoggerUtils.Log($"AIPark_CharacterBehaviour: 角色不在NavMesh上，当前位置 {transform.position}");

                    // 尝试将角色投影到NavMesh上
                    if (NavMesh.SamplePosition(transform.position, out navHit, 10.0f, NavMesh.AllAreas))
                    {
                        float heightDifference = Mathf.Abs(navHit.position.y - transform.position.y);
                        // 如果高度差距很大，考虑直接传送
                        if (heightDifference > 1.0f)
                        {
                            LoggerUtils.Log($"AIPark_CharacterBehaviour: 高度差过大 ({heightDifference}米)，直接调整位置到NavMesh上");
                            Vector3 adjustedPosition = navHit.position;
                            // 保持x和z，只调整y高度
                            adjustedPosition.x = transform.position.x;
                            adjustedPosition.z = transform.position.z;
                            _npcKccCtr.Motor.SetPosition(adjustedPosition);
                        }
                    }
                }

                _pathFollower.movementMethod = KCCMovementMethod.DirectPosition;
                // 使用KCC路径跟随器进行移动
                if (_pathFollower != null)
                {
                    // 如果指定了移动速度，则设置路径跟随器的速度
                    if (moveSpeed > 0)
                    {
                        _pathFollower.moveSpeed = moveSpeed;
                    }

                    // 创建回调包装，确保当到达目的地时重置移动超时标志
                    Action wrappedCallback = null;
                    if (onDestinationReached != null)
                    {
                        wrappedCallback = () =>
                        {
                            _isMovingWithTimeout = false;
                            onDestinationReached();
                        };
                    }

                    bool success = _pathFollower.MoveToPosition(targetPosition, wrappedCallback);

                    if (!success)
                    {
                        LoggerUtils.Log($"AIPark_CharacterBehaviour: 无法移动到 {targetPosition}，寻路失败，尝试备用方法");

                        // 当常规寻路失败时，尝试使用备用传送方法，但限制传送距离
                        float maxTeleportDistance = 2.0f; // 最大传送距离限制为2米

                        if (distanceToTarget > maxTeleportDistance)
                        {
                            LoggerUtils.LogError($"AIPark_CharacterBehaviour: 距离过远({distanceToTarget:F2}米)，超过最大传送距离({maxTeleportDistance}米)，不执行传送");
                            // 仍然触发回调以继续任务逻辑
                            _isMovingWithTimeout = false;
                            wrappedCallback?.Invoke();
                        }
                        else
                        {
                            // 只有在距离合理时才尝试传送
                            TryTeleportNearPosition(targetPosition, wrappedCallback);
                        }

                        // 寻路失败，重置超时标志
                        _isMovingWithTimeout = false;
                    }
                    else
                    {
                        LoggerUtils.Log($"AIPark_CharacterBehaviour: 开始移动到 {targetPosition}");
                    }
                }
            }
            catch (System.Exception ex)
            {
                LoggerUtils.LogError($"AIPark_CharacterBehaviour: {_curNpcName} MoveToPosition时出错: {ex.Message}");
                // 确保在出错时仍然调用回调，以免任务卡住
                onDestinationReached?.Invoke();
            }
        }

        public bool CheckCanMoveToPosition(Vector3 targetPosition)
        {
            try
            {
                // 1. 检查目标位置是否在NavMesh上
                NavMeshHit navHit;
                if (!NavMesh.SamplePosition(targetPosition, out navHit, 1.0f, NavMesh.AllAreas))
                {
                    LoggerUtils.Log($"AIPark_CharacterBehaviour: {_curNpcName} 目标位置不在NavMesh上，无法移动");
                    return false;
                }

                // 2. 检查目标位置是否有静态障碍物（使用OverlapSphere检测碰撞体）
                float checkRadius = 0.5f; // 检查半径，可以根据角色大小调整
                Collider[] colliders = Physics.OverlapSphere(targetPosition, checkRadius);

                foreach (Collider collider in colliders)
                {
                    // 跳过自己的碰撞体
                    if (collider.transform == transform || collider.transform.IsChildOf(transform))
                        continue;

                    // 跳过触发器
                    if (collider.isTrigger)
                        continue;

                    // 检查是否是其他角色的KinematicCharacterMotor
                    KinematicCharacterController otherKCC = collider.GetComponent<KinematicCharacterController>();
                    if (otherKCC != null)
                    {
                        LoggerUtils.Log($"AIPark_CharacterBehaviour: {_curNpcName} 目标位置有其他角色 {otherKCC.name}，无法移动");
                        return false;
                    }

                    // 检查是否是其他KinematicCharacterMotor的子对象
                    KinematicCharacterController parentKCC = collider.GetComponentInParent<KinematicCharacterController>();
                    if (parentKCC != null && parentKCC != _npcKccCtr)
                    {
                        LoggerUtils.Log($"AIPark_CharacterBehaviour: {_curNpcName} 目标位置有其他角色 {parentKCC.name}，无法移动");
                        return false;
                    }

                    // 检查是否是静态障碍物（非角色的碰撞体）
                    // 这里可以根据需要添加更多的障碍物检查逻辑
                    if (collider.gameObject.layer != LayerMask.NameToLayer("Character") &&
                        collider.gameObject.layer != LayerMask.NameToLayer("Player"))
                    {
                        // LoggerUtils.Log($"AIPark_CharacterBehaviour: {_curNpcName} 目标位置有障碍物 {collider.name}，无法移动");
                        // return false;
                    }
                }

                // 3. 使用射线检测进一步确认目标位置是否可达
                Vector3 rayStart = transform.position;
                Vector3 rayDirection = (targetPosition - rayStart).normalized;
                float rayDistance = Vector3.Distance(rayStart, targetPosition);

                // 使用LayerMask排除角色层，只检测静态障碍物
                int obstacleLayerMask = ~(1 << LayerMask.NameToLayer("Character") | 1 << LayerMask.NameToLayer("Player"));

                if (Physics.Raycast(rayStart, rayDirection, out RaycastHit hit, rayDistance, obstacleLayerMask))
                {
                    // LoggerUtils.Log($"AIPark_CharacterBehaviour: {_curNpcName} 到目标位置的路径被障碍物 {hit.collider.name} 阻挡，无法移动");
                    // return false;
                }

                LoggerUtils.Log($"AIPark_CharacterBehaviour: {_curNpcName} 目标位置检查通过，可以移动");
                return true;
            }
            catch (System.Exception ex)
            {
                LoggerUtils.LogError($"AIPark_CharacterBehaviour: {_curNpcName} CheckCanMoveToPosition时出错: {ex.Message}");
                return false;
            }
        }
        public void SetPositionAndRotation(Vector3 position, Quaternion rotation)
        {
            _npcKccCtr.Motor.SetPositionAndRotation(position, rotation);
        }

        // 添加字段保存原始朝向
        private Vector3 _originalForward;
        private bool _isLookingAtPlayer;

        public void LookAtPlayer()
        {
            var selfCharacter = AvatarController.Inst.SelfController;
            if (selfCharacter == null)
                return;

            // 保存原始前向向量
            if (!_isLookingAtPlayer)
            {
                _originalForward = _npcKccCtr.transform.forward;
                _isLookingAtPlayer = true;
            }

            Vector3 targetDirection = selfCharacter.transform.position - _npcKccCtr.transform.position;
            targetDirection.y = 0;
            targetDirection.Normalize();

            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            _npcKccCtr.Motor.SetRotation(targetRotation);
        }

        public void RevertLookAt()
        {
            if (_isLookingAtPlayer)
            {
                _originalForward.y = 0;
                _originalForward.Normalize();
                Quaternion targetRotation = Quaternion.LookRotation(_originalForward);
                _npcKccCtr.Motor.SetRotation(targetRotation);

                _isLookingAtPlayer = false;
            }
        }

        /// <summary>
        /// 停止移动
        /// </summary>
        public void StopMoving()
        {
            try
            {
                if (_pathFollower != null)
                {
                    _pathFollower.StopFollowing();
                    LoggerUtils.Log($"AIPark_CharacterBehaviour: {_curNpcName} 停止移动");
                }
                else if (_npcAgent != null)
                {
                    _npcAgent.ResetPath();
                    _isCheckingDestination = false;
                }

                if (_npcAnimController != null)
                {
                    var aniState = PlayerAniState.Idle;
                    _npcAnimController.SetPlayerAniState(aniState, false);
                }
            }
            catch (System.Exception ex)
            {
                LoggerUtils.LogError($"AIPark_CharacterBehaviour: {_curNpcName} StopMoving时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取NPC的章节处理器
        /// </summary>
        /// <returns>章节处理器</returns>
        public AIChapterHandler GetChapterHandler()
        {
            return _npcChapterHandler;
        }

        public int GetChapterLocation()
        {
            return _npcChapterHandler?.GetChapterData()?.location ?? 0;
        }

        public int GetChapterAction()
        {
            return _npcChapterHandler?.GetChapterData()?.action ?? 0;
        }

        /// <summary>
        /// 获取NPC当前状态
        /// </summary>
        /// <returns>当前状态</returns>
        public AIChapterState GetCurrentState()
        {
            if (_npcChapterHandler != null)
            {
                var stateMachine = _npcChapterHandler.GetStateMachine();
                if (stateMachine != null)
                {
                    return stateMachine.GetCurrentState();
                }
            }
            return null;
        }

        public void OnTouchDustMan()
        {
            if (_clickCount == 0)
            {
                LoggerUtils.Log("执行打针");
                // EnterInterruptState<FallDownState>();
                CanShowInteractBtn = false;
            }
            else if (_clickCount == 1)
            {
                LoggerUtils.Log("执行换装");
                AvatarController.Inst.SelfController.PlayerAnimCtrl.PlayerChangeClothesForUICharacer();
                foreach (var item in dustManAvatars)
                {
                    WearDustManClothes(item);
                }
                MessageHelper.Broadcast(MessageName.OnS9DustManChangeClothes);
                IsCanClick = false;
            }
            _clickCount++;
        }

        public int GetDustManClickCount()
        {
            return _clickCount;
        }

        private void WearDustManClothes(string pgcId)
        {
            var characterWrap = AvatarController.Inst.SelfWrap;
            var config = DataTables.GetAvatarCommonData(pgcId);
            var classType = UniqueType.GetAvatar(pgcId);
            characterWrap.ChangePart(classType, pgcId);
            characterWrap.ChangeColor(classType, config.defaultColor);
            characterWrap.Move(classType, config.pDef);
            characterWrap.Rotate(classType, config.rDef);
            characterWrap.Scale(classType, config.sDef);
            characterWrap.HVScale(classType, config.vhSDef);
            characterWrap.SetLeftOrRight(classType, config.leftRightType);
        }

        public void UpdateNpcData(float decisionRate)
        {
            this._decisionRate = decisionRate;
        }
        #endregion

        #region 开始和NPC聊天
        public TalkWithPlayerState StartTalkWithPlayer()
        {
            // 使用泛型版本的方法自动创建状态实例并进入
            if (GetCurrentState() is IdleState)
            {
                bIsEnterInterruptState = true;
                return EnterInterruptState<TalkWithPlayerState>();
            }
            return null;
        }

        public T ForceEnterInterruptState<T>() where T : AIChapterState
        {
            bIsEnterInterruptState = true;
            T state = (T)Activator.CreateInstance(typeof(T), new object[] { this, null });
            _npcChapterHandler.InterruptState(state);
            return state;
        }

        private BudTimer _recalculateChapterTimer;

        public void RecalculateChapter(float waitTime = 0)
        {
            LoggerUtils.Log(_curNpcName + " RecalculateChapter waitTime = " + waitTime);
            if (IsNpcTalking)
            {
                LoggerUtils.Log(_curNpcName + " IsNpcTalking");
                return;
            }

            //高优先级状态
            // if (GetCurrentState() is StopPlayerEscapeState || GetCurrentState() is ForceInjectionState || GetCurrentState() is StopPlayerEscapeOnFailState) 
            // {
            //     return;
            // }

            //暂时注释
            // if (UIAgentManager.Inst.FindPanel(WindowId.GuestWindow, PanelId.AIParkQuickEmotePanel))
            // {
            //     return;
            // }

            StopRecalculateTimer();

            //恢复状态
            // _recalculateChapterTimer = TimerManager.Inst.RunOnce("RecalculateChapter" + GameUtils.GetTimeStamp(), waitTime, () =>
            // {
            //     if (_npcChapterHandler != null && !_npcStateController.stateMachine.ContainsCurrentState(PlayerState.LinkEmote))
            //     {
            //         _npcChapterHandler.RecalculateChapter(_elapsedTime);
            //     }
            //     StopRecalculateTimer();
            // });
        }

        private void StopRecalculateTimer()
        {
            if (_recalculateChapterTimer != null)
            {
                TimerManager.Inst.Stop(_recalculateChapterTimer);
                _recalculateChapterTimer = null;
            }
        }


        /// <summary>
        /// 泛型版本的状态中断方法，自动创建状态实例
        /// </summary>
        /// <typeparam name="T">AIChapterState的子类类型</typeparam>
        public T EnterInterruptState<T>() where T : AIChapterState
        {
            StopRecalculateTimer();
            // 使用反射创建状态实例，传入当前对象作为参数
            T state = (T)Activator.CreateInstance(typeof(T), new object[] { this, null });
            _npcChapterHandler.InterruptState(state);
            return state;
        }

        /// <summary>
        /// 设置npc是当前目标，加载一个特效在脚底
        /// </summary>
        /// <param name="value"></param>
        public void SetIsCurrentTarget(bool value)
        {
            //todo
            if (value)
            {
                LoggerUtils.Log($"npcid = {_curNpcId} roleType ={_curNpcRoleType} 设置为当前任务目标 ");
            }
            else
            {
                LoggerUtils.Log($"npcid = {_curNpcId} roleType ={_curNpcRoleType} 取消设置为当前任务目标 ");
            }
        }

        public void SetHandNodeActive(bool isActive)
        {
            RHandTrans.gameObject.SetActive(isActive);
            LHandTrans.gameObject.SetActive(isActive);
        }

        #endregion

        #region 聊天框相关
        public void SetNpcTalk(string text)
        {
            //暂时注释
            // UIAgentManager.Inst.OpenPanel(PanelId.AIParkNpcChatPanel, _npcAnimController.Wrap.Avatar, text);
        }
        #endregion

        /// <summary>
        /// 尝试直接传送到目标位置附近的NavMesh上
        /// 这是一个备用方法，当正常导航失败时使用
        /// </summary>
        /// <param name="targetPosition">目标位置</param>
        /// <param name="onComplete">完成回调</param>
        /// <returns>是否成功传送</returns>
        private bool TryTeleportNearPosition(Vector3 targetPosition, Action onComplete = null)
        {
            try
            {
                // 注意：调用此方法前已在MoveToPosition中检查了距离，这里不再重复检查
                if (_npcKccCtr == null)
                {
                    LoggerUtils.Log($"AIPark_CharacterBehaviour: {_curNpcName} TryTeleportNearPosition时_npcKccCtr为null，取消传送");
                    onComplete?.Invoke();
                    return false;
                }

                NavMeshHit navHit;
                if (NavMesh.SamplePosition(targetPosition, out navHit, 10.0f, NavMesh.AllAreas))
                {
                    // 找到了有效的NavMesh位置
                    Vector3 validPosition = navHit.position;

                    // 获取朝向目标的方向
                    Vector3 direction = (targetPosition - validPosition).normalized;
                    direction.y = 0; // 确保水平方向
                    Quaternion rotation = Quaternion.LookRotation(direction);

                    // 使用KCC设置位置和旋转
                    _npcKccCtr.Motor.SetPositionAndRotation(validPosition, rotation);

                    LoggerUtils.Log($"AIPark_CharacterBehaviour: {_curNpcName} 使用紧急传送到达 {validPosition}，接近请求的位置 {targetPosition}");

                    // 调用完成回调
                    onComplete?.Invoke();
                    return true;
                }

                LoggerUtils.Log($"AIPark_CharacterBehaviour: {_curNpcName} 无法找到接近 {targetPosition} 的有效NavMesh位置，传送失败");
                // 仍然调用回调，让游戏逻辑可以继续
                onComplete?.Invoke();
                return false;
            }
            catch (System.Exception ex)
            {
                LoggerUtils.LogError($"AIPark_CharacterBehaviour: {_curNpcName} TryTeleportNearPosition时出错: {ex.Message}");
                onComplete?.Invoke();
                return false;
            }
        }

        #region 给外部调用的方法

        public string GetNpcName()
        {
            return _curNpcName;
        }

        public ParkNPCType GetNpcType()
        {
            return _curNpcType;
        }

        public string GetNpcID()
        {
            return _curNpcId;
        }

        public void PlayAnim(string emoteId)
        {
            Debug.Log(GetNpcID() + " " + emoteId);
            var emoAniDataList = DataTables.GetEmoAniConfigList().FindAll((emoAniData) => emoAniData.emoId == emoteId);
            if (emoAniDataList.Count > 0)
            {
                if (playerStateControllerState.CanEnterState(PlayerState.SingleEmote))
                {
                    int random = emoAniDataList[0].randomCount;
                    int randomResult = random == 0 ? 0 : UnityEngine.Random.Range(1, random + 1);
                    playerStateControllerState.EnterState(PlayerState.SingleEmote, emoteId, randomResult);
                }
            }
        }
        /// <summary>
        /// 不可用 有问题
        /// </summary>
        /// <param name="emoteId"></param>
        public void DirectPlayAnim(string emoteId)
        {
            var emoAniDataList = DataTables.GetEmoAniConfigList().FindAll((emoAniData) => emoAniData.emoId == emoteId);
            if (emoAniDataList.Count > 0)
            {
                if (playerStateControllerState.CanEnterState(PlayerState.SingleEmote))
                {
                    int random = emoAniDataList[0].randomCount;
                    int randomResult = random == 0 ? 0 : UnityEngine.Random.Range(1, random + 1);
                    playerStateControllerState.DirectIntoState(PlayerState.SingleEmote, emoteId, randomResult);
                }
            }
        }


        public Transform GetBottomBone()
        {
            return bottomBone;
        }

        public Transform GetHeadBone()
        {
            return headBone;
        }

        #endregion

        #region 引导相关
        public void OnS9GuideStepClick(int stepID, bool forceExecute = false)
        {
            //如果没有引导的时候怎么处理? 本地获取引导ID来驱动
            if (stepID == (int)AIGameParkConfig.EPgcGuideID.TargetTips)
            {
                if (this._curNpcRoleType == ParkNpcRoleType.Elise)
                {
                    MoveToTalkPosition();
                }
            }
        }

        // 添加一个可配置的距离参数
        private const float TALK_DISTANCE = 1f; // 默认1米的对话距离

        public void MoveToTalkPosition()
        {
            Vector3 playerPos = AvatarController.Inst.GetSelfAvatarPosition();
            Vector3 dirToPlayer = (transform.position - playerPos).normalized;
            // 计算NPC应该停止的位置：主角位置 + 方向 * 距离
            Vector3 targetPos = playerPos + dirToPlayer * TALK_DISTANCE;

            MoveToPosition(targetPos, () =>
            {
                //todo 把这里的消息框修改到这个behaviour ，每个npc都有一个框，确保唯一性 同时也可以
                // MessageHelper.Broadcast(MessageName.OnS9GuideStepAction, AIGameParkConfig.EGuideAction.ShowDoctorContent);                
            });
        }
        #endregion


        public void StopPathfinding()
        {
            // 停止当前寻路
            if (_npcAgent != null)
            {
                _npcAgent.isStopped = true;
                _npcAgent.velocity = Vector3.zero;
            }

            // 如果有其他寻路相关的组件，也需要停止
            // 例如：如果有自定义的寻路系统，在这里停止
        }

        public void ResumePathfinding()
        {
            // 恢复寻路
            if (_npcAgent != null)
            {
                _npcAgent.isStopped = false;
            }
            isInteracting = false;
            currentInteractTarget = null;
        }

        private void CheckForCharactersInFront()
        {
            // 获取前方所有角色
            Collider[] colliders = Physics.OverlapSphere(transform.position, detectionRadius, characterLayer);

            foreach (Collider collider in colliders)
            {
                AIPark_CharacterBehaviour otherCharacter = collider.GetComponent<AIPark_CharacterBehaviour>();
                if (otherCharacter != null && otherCharacter != this)
                {
                    // 计算方向向量
                    Vector3 directionToOther = otherCharacter.transform.position - transform.position;
                    float angle = Vector3.Angle(transform.forward, directionToOther);

                    // 检查是否在扇形范围内
                    if (angle <= detectionAngle * 0.5f)
                    {
                        // 检查是否有障碍物阻挡
                        if (!Physics.Raycast(transform.position, directionToOther.normalized, directionToOther.magnitude, ~characterLayer))
                        {
                            StartInteraction(otherCharacter);
                            break;
                        }
                    }
                }
            }
        }

        private void StartInteraction(AIPark_CharacterBehaviour otherCharacter)
        {
            if (isInteracting) return;

            isInteracting = true;
            currentInteractTarget = otherCharacter;

            // 停止双方寻路
            StopPathfinding();
            otherCharacter.StopPathfinding();

            // 开始旋转面向对方
            StartCoroutine(RotateTowardsTarget(otherCharacter));
        }

        private IEnumerator RotateTowardsTarget(AIPark_CharacterBehaviour target)
        {
            // 计算目标旋转
            Vector3 directionToTarget = target.transform.position - transform.position;
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);

            // 平滑旋转
            while (Quaternion.Angle(transform.rotation, targetRotation) > 0.1f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                yield return null;
            }

            // 确保完全面向目标
            transform.rotation = targetRotation;

            // 开始对话
            EnterInteract();
        }

        void EnterInteract()
        {

        }

        // 在Inspector中可视化检测范围
        private void OnDrawGizmosSelected()
        {
            // 绘制检测半径
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
        }

        /// <summary>
        /// 启用角色检测
        /// </summary>
        public void EnableCharacterDetection()
        {
            isDetectionEnabled = true;
        }

        /// <summary>
        /// 禁用角色检测
        /// </summary>
        public void DisableCharacterDetection()
        {
            isDetectionEnabled = false;
            // 如果正在交互，结束交互
            if (isInteracting)
            {
                EndInteraction();
            }
        }

        /// <summary>
        /// 获取当前检测状态
        /// </summary>
        public bool IsCharacterDetectionEnabled()
        {
            return isDetectionEnabled;
        }

        /// <summary>
        /// 结束当前交互
        /// </summary>
        private void EndInteraction()
        {
            if (isInteracting)
            {
                isInteracting = false;
                if (currentInteractTarget != null)
                {
                    currentInteractTarget.ResumePathfinding();
                    currentInteractTarget = null;
                }
                ResumePathfinding();
            }
        }

        private void UpdateSectorVisualization()
        {
            if (detectionLineRenderer == null)
            {
                detectionLineRenderer = gameObject.AddComponent<LineRenderer>();
                // 初始化LineRenderer属性
                detectionLineRenderer.startWidth = 0.1f;
                detectionLineRenderer.endWidth = 0.1f;
                detectionLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
                detectionLineRenderer.startColor = lineColor;
                detectionLineRenderer.endColor = lineColor;
                detectionLineRenderer.positionCount = lineSegments + 2;
                detectionLineRenderer.loop = true;
            }

            // 计算扇形的顶点
            Vector3[] points = new Vector3[lineSegments + 2];
            float angleStep = detectionAngle / (lineSegments); // 修改角度步进计算

            // 设置中心点
            points[0] = transform.position;
            // 计算扇形的边缘点
            for (int i = 0; i < lineSegments; i++)
            {
                // 计算当前角度，从-detectionAngle/2到detectionAngle/2
                float currentAngle = -detectionAngle / 2 + angleStep * i;
                // 使用当前角度旋转forward向量
                Vector3 direction = Quaternion.Euler(0, currentAngle, 0) * transform.forward;
                // 计算点位置
                points[i + 1] = transform.position + direction * detectionRadius;
            }
            points[lineSegments + 1] = transform.position;


            // 更新LineRenderer的位置
            detectionLineRenderer.SetPositions(points);
        }

    }
}
