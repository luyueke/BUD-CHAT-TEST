using System;
using System.Collections;
using System.Collections.Generic;
using Game.KinematicCharacter;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Props.PropsBehaviours.AIPark
{
    /// <summary>
    /// 移动方式
    /// </summary>
    public enum KCCMovementMethod
    {
        DirectPosition,    // 直接设置位置和旋转
        UsingKCCInputs     // 使用KCC的输入系统
    }

    /// <summary>
    /// KCC路径跟随器 - 结合NavMesh Agent的路径规划和KCC的移动控制
    /// </summary>
    public class KCCPathFollower : MonoBehaviour
    {
        #region 公开配置参数
        [Header("移动设置")]
        [Tooltip("移动方式（DirectPosition: 直接设置位置, UsingKCCInputs: 使用KCC输入系统）")]
        public KCCMovementMethod movementMethod = KCCMovementMethod.DirectPosition;

        [Tooltip("是否在狭窄区域自动切换到直接定位模式")]
        public bool autoSwitchMethodInNarrowAreas = true;

        [Tooltip("判定为狭窄区域的宽度阈值")]
        public float narrowAreaThreshold = 1.0f;

        [Tooltip("输入系统中的前瞻距离（用于优化拐角通过）")]
        public float lookAheadDistance = 1.0f;

        [Tooltip("移动速度")]
        public float moveSpeed = 3.5f;

        [Tooltip("转向速度(度/秒)")]
        public float rotationSpeed = 360f;

        [Tooltip("到达路径点的判定距离")]
        public float waypointReachedDistance = 0.3f;

        [Tooltip("到达最终目标的判定距离")]
        public float destinationReachedDistance = 0.1f;

        [Tooltip("路径重新计算间隔(秒)，0表示不自动重新计算")]
        public float pathRecalculationInterval = 0.5f;

        [Tooltip("与路径允许的最大偏移距离，超出会重新计算路径")]
        public float maxPathDeviation = 1.0f;

        [Header("路径可视化设置")]
        [Tooltip("是否在场景视图中绘制路径")]
        public bool debugDrawPath = true;

        [Tooltip("路径颜色")]
        public Color pathColor = Color.blue;

        [Tooltip("当前目标点颜色")]
        public Color currentTargetColor = Color.red;

        [Tooltip("预览路径颜色")]
        public Color previewPathColor = Color.green;

        [Tooltip("是否在场景中使用LineRenderer绘制路径")]
        public bool useLineRenderer = true;

        [Tooltip("路径线宽度")]
        public float pathLineWidth = 0.1f;
        #endregion

        #region 私有字段
        // 组件引用
        private NavMeshAgent _pathfindingAgent;  // 只用于路径查找
        private KinematicCharacterMotor _kccMotor;
        private KinematicCharacterController _kccController;
        private IKCController _kccIController;
        // private LineRenderer _pathLineRenderer;
        public PlayerAnimationCtrl _npcAnimController;

        // 路径数据
        private NavMeshPath _currentPath;
        private Vector3[] _pathPoints = new Vector3[0];
        private int _currentPathPointIndex = 0;
        private float _pathRecalculationTimer = 0f;
        private Vector3 _targetPosition;
        private bool _hasReachedDestination = false;

        // 预览路径数据
        private bool _isPreviewingPath = false;
        private Vector3[] _previewPathPoints = new Vector3[0];

        // 状态控制
        private bool _isFollowingPath = false;
        private Action _onDestinationReachedCallback;
        private Action _onMoveCallCallback; // 新增：移动时每帧调用的回调

        // 调试信息
        private float _remainingDistance;

        // 缓存变量
        private PlayerCharacterInputs _inputs = new PlayerCharacterInputs();
        private Vector3 _currentMoveDirection = Vector3.zero;

        // 碰撞处理相关
        private float _stuckDetectionTimer = 0f;
        private Vector3 _lastPosition = Vector3.zero;
        private int _stuckCount = 0;
        private float _stuckCheckInterval = 0.5f; // 每0.5秒检查一次是否卡住
        private float _stuckThreshold = 0.05f; // 如果0.5秒内移动距离小于这个值，认为可能卡住了
        private int _stuckCountThreshold = 3; // 连续卡住几次才尝试解决
        private float _unstuckForce = 2.0f; // 解除卡住时施加的力度
        #endregion

        #region Unity生命周期
        private void Awake()
        {
            InitializeComponents();
        }

        private void OnEnable()
        {
            if (_pathfindingAgent != null)
            {
                _pathfindingAgent.enabled = true;
            }

            // if (_pathLineRenderer != null && useLineRenderer)
            // {
            //     _pathLineRenderer.enabled = true;
            // }
        }

        private void OnDisable()
        {
            StopFollowing();

            if (_pathfindingAgent != null)
            {
                _pathfindingAgent.enabled = false;
            }

            // if (_pathLineRenderer != null)
            // {
            //     _pathLineRenderer.enabled = false;
            // }
        }

        private void Update()
        {
            if (_isFollowingPath)
            {
                UpdatePathFollowing();
                CheckPathDeviation();
                UpdatePathRecalculation();
            }

            UpdateVisualizations();
        }

        private void OnDrawGizmos()
        {
            if (!debugDrawPath) return;

            // 绘制当前跟随的路径
            DrawPathGizmos(_pathPoints, _currentPathPointIndex, pathColor, currentTargetColor);

            // 绘制预览路径
            if (_isPreviewingPath)
            {
                DrawPathGizmos(_previewPathPoints, -1, previewPathColor, previewPathColor);
            }
        }
        #endregion

        #region 公开方法
        /// <summary>
        /// 移动到指定位置
        /// </summary>
        /// <param name="targetPosition">目标位置</param>
        /// <param name="onDestinationReached">到达目的地后的回调函数</param>
        /// <returns>是否成功设置路径</returns>
        public bool MoveToPosition(Vector3 targetPosition, Action onDestinationReached = null, Action onMoveCall = null)
        {
            if (_pathfindingAgent == null || _kccMotor == null)
            {
                LoggerUtils.LogError("KCCPathFollower: NavMeshAgent或KCC未初始化");
                return false;
            }

            _targetPosition = targetPosition;

            float distance = Vector3.Distance(transform.position, targetPosition);
            if (distance <= 0.1f)
            { 
                _isFollowingPath = false;
                _hasReachedDestination = true;
                _onDestinationReachedCallback?.Invoke();
                return true;
            }
            // 计算路径
            if (CalculatePath(targetPosition, out NavMeshPath path))
            {
                // 停止当前跟随
                StopFollowing();

                // 保存回调
                _onDestinationReachedCallback = onDestinationReached;
                _onMoveCallCallback = onMoveCall;

                // 设置路径
                _currentPath = path;
                _pathPoints = path.corners;
                _currentPathPointIndex = 0;
                _isFollowingPath = true;
                _hasReachedDestination = false;

                // 更新可视化
                UpdateVisualizations();

                LoggerUtils.Log($"KCCPathFollower: 开始移动到 {targetPosition}，路径点数量: {_pathPoints.Length}，移动方式: {movementMethod}");
                return true;
            }
            else
            {
                LoggerUtils.Log($"KCCPathFollower: 无法找到到 {targetPosition} 的路径");
                return false;
            }
        }

        /// <summary>
        /// 预览到指定位置的路径（不开始移动）
        /// </summary>
        /// <param name="targetPosition">目标位置</param>
        /// <returns>是否成功计算路径</returns>
        public bool PreviewPath(Vector3 targetPosition)
        {
            if (_pathfindingAgent == null)
            {
                LoggerUtils.LogError("KCCPathFollower: NavMeshAgent未初始化");
                return false;
            }

            if (CalculatePath(targetPosition, out NavMeshPath path))
            {
                _previewPathPoints = path.corners;
                _isPreviewingPath = true;
                UpdateVisualizations();
                return true;
            }
            else
            {
                _isPreviewingPath = false;
                _previewPathPoints = new Vector3[0];
                UpdateVisualizations();
                LoggerUtils.Log($"KCCPathFollower: 无法计算到 {targetPosition} 的预览路径");
                return false;
            }
        }

        /// <summary>
        /// 清除预览路径
        /// </summary>
        public void ClearPathPreview()
        {
            _isPreviewingPath = false;
            _previewPathPoints = new Vector3[0];
            UpdateVisualizations();
        }

        /// <summary>
        /// 开始沿着预览的路径移动
        /// </summary>
        /// <param name="onDestinationReached">到达目的地后的回调函数</param>
        /// <param name="onMoveCall">每帧移动时调用的回调函数</param>
        /// <returns>是否成功开始移动</returns>
        public bool StartFollowingPreviewedPath(Action onDestinationReached = null, Action onMoveCall = null)
        {
            if (!_isPreviewingPath || _previewPathPoints.Length <= 1)
            {
                LoggerUtils.Log("KCCPathFollower: 没有有效的预览路径可以跟随");
                return false;
            }

            // 停止当前跟随
            StopFollowing();

            // 保存回调
            _onDestinationReachedCallback = onDestinationReached;
            _onMoveCallCallback = onMoveCall;

            // 使用预览路径
            _pathPoints = _previewPathPoints;
            _currentPathPointIndex = 0;
            _isFollowingPath = true;
            _isPreviewingPath = false;
            _hasReachedDestination = false;

            // 如果有点，设置目标为最后一个点
            if (_pathPoints.Length > 0)
            {
                _targetPosition = _pathPoints[_pathPoints.Length - 1];
            }

            // 更新可视化
            UpdateVisualizations();

            return true;
        }

        /// <summary>
        /// 停止路径跟随
        /// </summary>
        public void StopFollowing()
        {
            _isFollowingPath = false;
            _pathPoints = new Vector3[0];
            _currentPathPointIndex = 0;
            _pathRecalculationTimer = 0f;
            _onDestinationReachedCallback = null;
            _onMoveCallCallback = null;

            // 如果使用KCC输入系统，则清除输入
            if (movementMethod == KCCMovementMethod.UsingKCCInputs && _kccIController != null)
            {
                _currentMoveDirection = Vector3.zero;
                _inputs = new PlayerCharacterInputs();
                _kccIController.SetInputs(ref _inputs, Vector3.zero, Quaternion.identity, Vector3.forward);
            }

            UpdateVisualizations();
        }

        /// <summary>
        /// 强制重新计算当前路径
        /// </summary>
        /// <returns>是否成功重新计算</returns>
        public bool RecalculatePath()
        {
            if (!_isFollowingPath || _targetPosition == Vector3.zero)
            {
                return false;
            }

            return MoveToPosition(_targetPosition, _onDestinationReachedCallback, _onMoveCallCallback);
        }

        /// <summary>
        /// 获取当前是否正在跟随路径
        /// </summary>
        public bool IsFollowingPath => _isFollowingPath;

        /// <summary>
        /// 获取当前是否正在预览路径
        /// </summary>
        public bool IsPreviewingPath => _isPreviewingPath;

        /// <summary>
        /// 获取到目标的剩余距离
        /// </summary>
        public float RemainingDistance => _remainingDistance;

        /// <summary>
        /// 获取当前是否已到达目的地
        /// </summary>
        public bool HasReachedDestination => _hasReachedDestination;

        /// <summary>
        /// 公开的组件初始化方法，用于在GameObject启用时重新初始化组件
        /// </summary>
        public void InitializeComponentsPublic()
        {
            try
            {
                // 先检查组件是否已经存在
                if (_pathfindingAgent == null || _kccMotor == null || _kccController == null)
                {
                    InitializeComponents();
                }
                else
                {
                    // 只需确保NavMeshAgent是启用的
                    if (_pathfindingAgent != null)
                    {
                        _pathfindingAgent.enabled = true;
                        _pathfindingAgent.updatePosition = false;
                        _pathfindingAgent.updateRotation = false;
                        _pathfindingAgent.isStopped = true;
                    }

                    // 重新获取KCC接口控制器
                    if (_kccController != null)
                    {
                        _kccIController = _kccController.CurIKCController;
                    }

                    // 重新获取动画控制器
                    if (_npcAnimController == null)
                    {
                        _npcAnimController = this.GetComponentInChildren<PlayerAnimationCtrl>();
                    }
                }

                LoggerUtils.Log("KCCPathFollower: 组件初始化成功");
            }
            catch (System.Exception ex)
            {
                LoggerUtils.LogError($"KCCPathFollower: 初始化组件时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 切换移动方式
        /// </summary>
        /// <param name="method">新的移动方式</param>
        public void SwitchMovementMethod(KCCMovementMethod method)
        {
            if (movementMethod != method)
            {
                movementMethod = method;
                LoggerUtils.Log($"KCCPathFollower: 移动方式切换为 {method}");
            }
        }



















































        #endregion

        #region 私有方法
        private void InitializeComponents()
        {
            // 获取或添加用于路径查找的NavMeshAgent
            _pathfindingAgent = GetComponent<NavMeshAgent>();
            if (_pathfindingAgent == null)
            {
                _pathfindingAgent = gameObject.AddComponent<NavMeshAgent>();
                _pathfindingAgent.speed = moveSpeed;
                _pathfindingAgent.angularSpeed = rotationSpeed;
                _pathfindingAgent.acceleration = 8f;
                _pathfindingAgent.stoppingDistance = destinationReachedDistance;
            }

            // 关键：禁用NavMeshAgent的位置和旋转更新，只用它来计算路径
            _pathfindingAgent.updatePosition = false;
            _pathfindingAgent.updateRotation = false;
            _pathfindingAgent.isStopped = true;

            // 获取KCC组件
            _kccMotor = GetComponent<KinematicCharacterMotor>();
            _kccController = GetComponent<KinematicCharacterController>();

            _npcAnimController = this.GetComponentInChildren<PlayerAnimationCtrl>();

            // 获取KCC接口控制器（用于输入系统）
            if (_kccController != null)
            {
                _kccIController = _kccController.CurIKCController;
            }

            // 初始化路径
            _currentPath = new NavMeshPath();

            // 设置LineRenderer
            if (useLineRenderer)
            {
                SetupLineRenderer();
            }
        }

        private void SetupLineRenderer()
        {
            // 获取或添加LineRenderer组件
            // _pathLineRenderer = GetComponent<LineRenderer>();
            // if (_pathLineRenderer == null)
            // {
            //     _pathLineRenderer = gameObject.AddComponent<LineRenderer>();
            // }
            //
            // // 设置LineRenderer属性
            // _pathLineRenderer.startWidth = pathLineWidth;
            // _pathLineRenderer.endWidth = pathLineWidth;
            // _pathLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            // _pathLineRenderer.startColor = pathColor;
            // _pathLineRenderer.endColor = pathColor;
            // _pathLineRenderer.positionCount = 0;
            // _pathLineRenderer.useWorldSpace = true;
        }

        private bool CalculatePath(Vector3 targetPosition, out NavMeshPath path)
        {
            path = new NavMeshPath();

            // 确保起始点在NavMesh上
            NavMeshHit startHit;
            Vector3 validStartPos = transform.position;
            bool startOnNavMesh = NavMesh.SamplePosition(transform.position, out startHit, 5.0f, NavMesh.AllAreas);

            if (!startOnNavMesh)
            {
                LoggerUtils.Log($"KCCPathFollower: 起始点 {transform.position} 不在NavMesh上，尝试寻找最近点");
                if (!NavMesh.SamplePosition(transform.position, out startHit, 10.0f, NavMesh.AllAreas))
                {
                    LoggerUtils.LogError($"KCCPathFollower: 无法在NavMesh上找到接近 {transform.position} 的起始点");
                    return false;
                }
                validStartPos = startHit.position;
                LoggerUtils.Log($"KCCPathFollower: 起始点投影到NavMesh上的位置: {validStartPos}，高度差: {Mathf.Abs(validStartPos.y - transform.position.y)}");
            }
            else
            {
                validStartPos = startHit.position;
            }

            // 确保目标点在NavMesh上
            NavMeshHit targetHit;
            Vector3 validTargetPos = targetPosition;

            if (!NavMesh.SamplePosition(targetPosition, out targetHit, 5.0f, NavMesh.AllAreas))
            {
                LoggerUtils.Log($"KCCPathFollower: 目标点 {targetPosition} 不在NavMesh上，正在尝试寻找最近点");
                if (!NavMesh.SamplePosition(targetPosition, out targetHit, 10.0f, NavMesh.AllAreas))
                {
                    LoggerUtils.LogError($"KCCPathFollower: 无法在NavMesh上找到接近 {targetPosition} 的点");
                    return false;
                }
                validTargetPos = targetHit.position;
                LoggerUtils.Log($"KCCPathFollower: 目标点投影到NavMesh上的位置: {validTargetPos}");
            }
            else
            {
                validTargetPos = targetHit.position;
            }

            // 计算路径
            bool success = NavMesh.CalculatePath(validStartPos, validTargetPos, NavMesh.AllAreas, path);

            // 检查路径是否有效
            if (!success || path.status == NavMeshPathStatus.PathInvalid || path.corners.Length <= 1)
            {
                LoggerUtils.Log($"KCCPathFollower: 从 {validStartPos} 到 {validTargetPos} 的路径计算失败或无效，状态: {path.status}");
                return false;
            }

            return true;
        }

        private void UpdatePathFollowing()
        {
            if (!_isFollowingPath || _pathPoints.Length <= 0 || _currentPathPointIndex >= _pathPoints.Length)
            {
                return;
            }

            // 检测是否卡在障碍物中
            DetectAndHandleStuckState();

            // 获取当前目标点
            Vector3 currentTargetPoint = _pathPoints[_currentPathPointIndex];

            // 计算到当前路径点的方向和距离
            Vector3 directionToTarget = currentTargetPoint - transform.position;
            directionToTarget.y = 0; // 确保水平移动
            float distanceToTarget = directionToTarget.magnitude;




































            // 计算剩余距离
            _remainingDistance = CalculateRemainingDistance();

            // 检查是否到达当前路径点
            if (distanceToTarget <= waypointReachedDistance)
            {
                // 前进到下一个路径点
                _currentPathPointIndex++;

                // 检查是否到达最终目标
                if (_currentPathPointIndex >= _pathPoints.Length)
                {
                    OnDestinationReached();
                    return;
                }

                // 更新可视化
                UpdateVisualizations();

                // 继续处理下一个路径点
                return;
            }

            // 保存当前移动方向（用于两种移动方式）
            _currentMoveDirection = directionToTarget.normalized;

            // 检查是否在狭窄区域，如果是且启用了自动切换，则使用直接位置设置
            KCCMovementMethod currentMethod = movementMethod;
            if (autoSwitchMethodInNarrowAreas && movementMethod == KCCMovementMethod.UsingKCCInputs)
            {
                if (IsInNarrowArea())
                {
                    currentMethod = KCCMovementMethod.DirectPosition;
                }
            }

            // 根据移动方式选择适当的移动方法
            if (currentMethod == KCCMovementMethod.DirectPosition)
            {
                // 直接设置位置方式
                MoveTowardsPointDirect(currentTargetPoint, distanceToTarget);
            }
            else
            {
                // 使用KCC输入系统方式
                MoveTowardsPointUsingKCCInputs(currentTargetPoint);
            }

            // 调用移动回调（每帧移动时调用）
            if (_onMoveCallCallback != null)
            {
                try
                {
                    _onMoveCallCallback.Invoke();
                }
                catch (Exception e)
                {
                    LoggerUtils.LogError($"调用移动回调时出错: {e.Message}");
                }
            }
        }

        private void MoveTowardsPointDirect(Vector3 targetPoint, float distance)
        {
            // 计算方向和旋转
            Vector3 normalizedDirection = _currentMoveDirection;

            // 处理垂直差异 - 检查目标点与当前位置的高度差
            float heightDifference = targetPoint.y - transform.position.y;
            bool significantHeightChange = Mathf.Abs(heightDifference) > 0.5f;

            // 如果有明显的高度差，调整移动方向包含垂直分量
            if (significantHeightChange)
            {
                // 考虑垂直分量，但减少其影响以避免过大的垂直移动
                Vector3 dirWithHeight = (targetPoint - transform.position).normalized;
                // 混合水平方向和有高度的方向
                normalizedDirection = Vector3.Lerp(normalizedDirection, dirWithHeight, 0.3f);

                Debug.DrawRay(transform.position, dirWithHeight, Color.yellow);
                LoggerUtils.Log($"KCCPathFollower: 处理高度差 {heightDifference:F2}米");
            }

            // 计算目标旋转
            Quaternion targetRotation = Quaternion.LookRotation(normalizedDirection);

            // 平滑旋转 - 使用KCC设置旋转
            Quaternion newRotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );

            // 计算移动距离
            float moveAmount = Mathf.Min(moveSpeed * Time.deltaTime, distance);

            // 计算新位置
            Vector3 newPosition = transform.position + normalizedDirection * moveAmount;

            // 在处理大高度差时，使用射线检测确认新位置的有效性
            if (significantHeightChange)
            {
                // 向下射线检测，确认不会掉入空中
                RaycastHit hit;
                if (Physics.Raycast(newPosition + Vector3.up * 0.5f, Vector3.down, out hit, 5f))
                {
                    // 如果射线检测到地面，微调高度确保角色不会悬空或穿透地面
                    float groundOffset = 0.1f; // 与地面保持的偏移量
                    newPosition.y = hit.point.y + groundOffset;
                }
            }

            // 使用KCC直接设置位置和旋转
            var aniState = PlayerAniState.Run;
            _npcAnimController.SetPlayerAniState(aniState, false);
            _kccMotor.SetPositionAndRotation(newPosition, newRotation);
        }

        private void MoveTowardsPointUsingKCCInputs(Vector3 targetPoint)
        {
            // 确保有KCC接口控制器
            if (_kccIController == null)
            {
                LoggerUtils.Log("KCCPathFollower: 没有找到KCC接口控制器，无法使用输入系统");
                return;
            }

            // 优化：计算前瞻目标点，这有助于优化拐角导航
            Vector3 lookAheadTarget = CalculateLookAheadTarget();

            // 计算前瞻方向
            Vector3 lookAheadDirection = (lookAheadTarget - transform.position).normalized;
            lookAheadDirection.y = 0; // 确保水平方向

            // 计算目标旋转
            Quaternion targetRotation = Quaternion.LookRotation(lookAheadDirection);

            // 限制输入力度，在接近目标或拐角时降低速度
            float inputMultiplier = CalculateInputMultiplier(targetPoint);

            // 重要：将世界空间方向转换为相对于角色的局部空间方向
            Vector3 localMoveDir = transform.InverseTransformDirection(_currentMoveDirection);





































            // 重要：根据KCC控制器的输入要求，我们需要计算MoveAxisForward和MoveAxisRight
            // 这些值应该是局部空间的，与坐标系对齐
            float moveAxisForward = localMoveDir.z * inputMultiplier; // 前进/后退
            float moveAxisRight = localMoveDir.x * inputMultiplier;   // 左/右

            // 创建并填充输入对象
            PlayerCharacterInputs inputs = new PlayerCharacterInputs();

            // 通过反射设置输入值（因为我们不能直接访问这些字段）
            var forwardField = typeof(PlayerCharacterInputs).GetField("MoveAxisForward") ??
                              typeof(PlayerCharacterInputs).GetField("moveAxisForward");

            var rightField = typeof(PlayerCharacterInputs).GetField("MoveAxisRight") ??
                            typeof(PlayerCharacterInputs).GetField("moveAxisRight");

            if (forwardField != null) forwardField.SetValue(inputs, moveAxisForward);
            if (rightField != null) rightField.SetValue(inputs, moveAxisRight);

            // 更新内部输入缓存
            _inputs = inputs;

            try
            {
                // 使用现有的多参数SetInputs方法，同时保证内部_inputs已正确设置
                _kccIController.SetInputs(ref _inputs, _currentMoveDirection * inputMultiplier,
                    targetRotation, lookAheadDirection);
            }
            catch (Exception e)
            {
                // 如果有任何问题，回退到我们之前的方法
                LoggerUtils.Log($"设置KCC输入时出错: {e.Message}。使用备用方法。");
                _kccIController.SetInputs(ref _inputs, _currentMoveDirection * inputMultiplier,
                    targetRotation, lookAheadDirection);
            }

            // 调试信息
            Debug.DrawRay(transform.position, _currentMoveDirection * inputMultiplier, Color.green);
            Debug.DrawRay(transform.position, new Vector3(moveAxisRight, 0, moveAxisForward), Color.red);







        }

        // 计算前瞻目标点，优化拐角导航
        private Vector3 CalculateLookAheadTarget()
        {
            // 获取当前目标点
            Vector3 currentTarget = _pathPoints[_currentPathPointIndex];

            // 计算从当前位置到目标点的距离
            float distanceToTarget = Vector3.Distance(transform.position, currentTarget);

            // 如果已经很接近当前目标点，且还有下一个路径点，则考虑下一个点
            if (distanceToTarget < lookAheadDistance && _currentPathPointIndex < _pathPoints.Length - 1)
            {
                // 获取下一个路径点
                Vector3 nextTarget = _pathPoints[_currentPathPointIndex + 1];
















                // 根据距离当前目标点的远近，混合当前目标和下一个目标
                float blendFactor = 1.0f - (distanceToTarget / lookAheadDistance);
                return Vector3.Lerp(currentTarget, nextTarget, blendFactor * 0.7f);













            }

            // 如果只有一个目标点或者距离还远，直接返回当前目标点
            return currentTarget;
        }

        // 计算输入力度，在接近目标或拐角时调整速度
        private float CalculateInputMultiplier(Vector3 targetPoint)
        {
            // 基础力度
            float multiplier = 1.0f;

            // 计算到目标的距离
            float distanceToTarget = Vector3.Distance(transform.position, targetPoint);

            // 检查是否需要转弯
            bool isTurning = IsMakingTurn();

            // 如果正在转弯，降低输入力度
            if (isTurning)
            {
                multiplier *= 0.7f;
            }

            // 如果很接近目标，降低输入力度
            if (distanceToTarget < 1.0f)
            {
                multiplier *= (0.5f + 0.5f * (distanceToTarget / 1.0f));
            }

            return multiplier;
        }

        // 检查是否正在转弯
        private bool IsMakingTurn()
        {
            // 如果只有一个路径点或者已经是最后一个点，则不需要转弯
            if (_pathPoints.Length <= 1 || _currentPathPointIndex >= _pathPoints.Length - 1)
                return false;

            // 计算当前方向与下一个方向的夹角
            Vector3 currentDirection = (_pathPoints[_currentPathPointIndex] - transform.position).normalized;
            Vector3 nextDirection = (_pathPoints[_currentPathPointIndex + 1] - _pathPoints[_currentPathPointIndex]).normalized;

            // 计算方向间的夹角
            float angle = Vector3.Angle(currentDirection, nextDirection);

            // 判断是否是较大的转弯(30度以上视为转弯)
            return angle > 30f;
        }

        private float CalculateRemainingDistance()
        {
            if (_pathPoints.Length <= 0 || _currentPathPointIndex >= _pathPoints.Length)
                return 0f;

            float distance = 0f;

            // 计算当前位置到当前目标点的距离
            distance += Vector3.Distance(transform.position, _pathPoints[_currentPathPointIndex]);

            // 计算剩余路径点之间的距离
            for (int i = _currentPathPointIndex; i < _pathPoints.Length - 1; i++)
            {
                distance += Vector3.Distance(_pathPoints[i], _pathPoints[i + 1]);
            }

            return distance;
        }

        private void UpdatePathRecalculation()
        {
            // 如果设置了自动重新计算路径
            if (pathRecalculationInterval > 0)
            {
                _pathRecalculationTimer += Time.deltaTime;

                if (_pathRecalculationTimer >= pathRecalculationInterval)
                {
                    _pathRecalculationTimer = 0f;

                    // 重新计算路径
                    if (_isFollowingPath && !_hasReachedDestination)
                    {
                        RecalculatePath();
                    }
                }
            }
        }

        private void CheckPathDeviation()
        {
            // 如果没有路径或已经到达目的地，跳过检查
            if (!_isFollowingPath || _hasReachedDestination || _pathPoints.Length <= 1 || _currentPathPointIndex >= _pathPoints.Length)
            {
                return;
            }

            // 当前位置与当前路径段的距离
            Vector3 currentPos = transform.position;
            Vector3 currentTarget = _pathPoints[_currentPathPointIndex];

            // 如果有上一个点，检查与当前路径段的距离
            if (_currentPathPointIndex > 0)
            {
                Vector3 previousPoint = _pathPoints[_currentPathPointIndex - 1];
                float deviation = PointToLineDistance(currentPos, previousPoint, currentTarget);

                // 如果偏离太远，重新计算路径
                if (deviation > maxPathDeviation)
                {
                    LoggerUtils.Log($"偏离路径 {deviation}，重新计算路径");
                    RecalculatePath();
                }
            }
        }

        private float PointToLineDistance(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
        {
            // 计算点到线段的最短距离
            Vector3 lineDirection = lineEnd - lineStart;
            float lineLength = lineDirection.magnitude;
            lineDirection.Normalize();

            Vector3 pointToLineStart = point - lineStart;
            float dotProduct = Vector3.Dot(pointToLineStart, lineDirection);

            // 如果点投影在线段之外
            if (dotProduct < 0)
                return Vector3.Distance(point, lineStart);
            if (dotProduct > lineLength)
                return Vector3.Distance(point, lineEnd);

            // 点到线的垂直距离
            Vector3 projection = lineStart + lineDirection * dotProduct;
            return Vector3.Distance(point, projection);
        }

        private void UpdateVisualizations()
        {
            // 更新LineRenderer
            // if (useLineRenderer && _pathLineRenderer != null)
            // {
            //     UpdateLineRenderer();
            // }
        }

        private void UpdateLineRenderer()
        {
            // if (_pathLineRenderer == null) return;

            Vector3[] pathToRender;
            Color lineColor;

            if (_isFollowingPath && _pathPoints.Length > 1)
            {
                // 渲染当前跟随的路径
                pathToRender = _pathPoints;
                lineColor = pathColor;
            }
            else if (_isPreviewingPath && _previewPathPoints.Length > 1)
            {
                // 渲染预览路径
                pathToRender = _previewPathPoints;
                lineColor = previewPathColor;
            }
            else
            {
                // 没有路径可渲染
                // _pathLineRenderer.positionCount = 0;
                return;
            }

            // 更新LineRenderer
            // _pathLineRenderer.startColor = lineColor;
            // _pathLineRenderer.endColor = lineColor;
            // _pathLineRenderer.positionCount = pathToRender.Length;
            // _pathLineRenderer.SetPositions(pathToRender);
        }

        private void DrawPathGizmos(Vector3[] points, int currentIndex, Color pathColor, Color targetColor)
        {
            if (points == null || points.Length <= 1) return;

            Gizmos.color = pathColor;

            // 绘制路径线段
            for (int i = 0; i < points.Length - 1; i++)
            {
                Gizmos.DrawLine(points[i], points[i + 1]);
            }

            // 绘制路径点
            for (int i = 0; i < points.Length; i++)
            {
                Gizmos.DrawSphere(points[i], 0.1f);
            }

            // 绘制当前目标点
            if (currentIndex >= 0 && currentIndex < points.Length)
            {
                Gizmos.color = targetColor;
                Gizmos.DrawSphere(points[currentIndex], 0.2f);
            }
        }

        // 检查当前是否在狭窄区域
        private bool IsInNarrowArea()
        {
            // 如果当前是最后一个路径点，不做狭窄区域检查
            if (_currentPathPointIndex >= _pathPoints.Length - 1)
                return false;

            // 在当前位置进行碰撞检测，检查通道宽度
            // 使用射线检测左右两侧的障碍物距离
            float leftDistance = CheckObstacleDistance(Vector3.left);
            float rightDistance = CheckObstacleDistance(Vector3.right);
            float totalWidth = leftDistance + rightDistance;

            // 计算前向通道宽度
            Vector3 forwardDir = _currentMoveDirection;
            Vector3 leftDir = Vector3.Cross(Vector3.up, forwardDir).normalized;
            float leftForwardDistance = CheckObstacleDistance(leftDir);
            float rightForwardDistance = CheckObstacleDistance(-leftDir);
            float forwardWidth = leftForwardDistance + rightForwardDistance;

            // 如果任一宽度小于阈值，则认为是狭窄区域
            return totalWidth < narrowAreaThreshold || forwardWidth < narrowAreaThreshold;
        }

        // 检查某个方向上的障碍物距离
        private float CheckObstacleDistance(Vector3 direction)
        {
            RaycastHit hit;
            float checkDistance = 2.0f; // 最大检测距离

            if (Physics.Raycast(transform.position + Vector3.up * 0.5f, direction, out hit, checkDistance))
            {
                return hit.distance;
            }

            return checkDistance; // 如果没有检测到障碍物，返回最大检测距离
        }

        private void OnDestinationReached()
        {
            LoggerUtils.Log("乐园KCCPathFollower: 到达目的地:"+transform.name);
            _isFollowingPath = false;
            _hasReachedDestination = true;

            // 如果使用KCC输入系统，则清除输入
            if (movementMethod == KCCMovementMethod.UsingKCCInputs && _kccIController != null)
            {
                _currentMoveDirection = Vector3.zero;
                _inputs = new PlayerCharacterInputs();
                _kccIController.SetInputs(ref _inputs, Vector3.zero, Quaternion.identity, Vector3.forward);
            }

            // 调用回调
            if (_onDestinationReachedCallback != null)
            {
                try
                {
                    _onDestinationReachedCallback.Invoke();
                }
                catch (Exception e)
                {
                    LoggerUtils.LogError($"调用目的地到达回调时出错: {e.Message}");
                }
                _onDestinationReachedCallback = null;
            }

            // 清理移动回调（到达终点后不再调用）
            _onMoveCallCallback = null;

            // 更新可视化
            UpdateVisualizations();

            var aniState = PlayerAniState.Idle;
            _npcAnimController.SetPlayerAniState(aniState, false);
        }

        /// <summary>
        /// 检测并处理角色卡住的情况
        /// </summary>
        private void DetectAndHandleStuckState()
        {
            // 如果是第一次调用，初始化上一次位置
            if (_lastPosition == Vector3.zero)
            {
                _lastPosition = transform.position;
                return;
            }

            // 增加计时器
            _stuckDetectionTimer += Time.deltaTime;

            // 每隔一段时间检查一次
            if (_stuckDetectionTimer >= _stuckCheckInterval)
            {
                // 计算从上次检查到现在移动的距离
                float movedDistance = Vector3.Distance(_lastPosition, transform.position);

                // 如果移动距离小于阈值，增加卡住计数
                if (movedDistance < _stuckThreshold && _isFollowingPath)
                {
                    _stuckCount++;
                    LoggerUtils.Log($"KCCPathFollower: 可能卡住了，移动距离 {movedDistance:F3}，卡住计数 {_stuckCount}");

                    // 如果连续几次检测都卡住，尝试解除卡住状态
                    if (_stuckCount >= _stuckCountThreshold)
                    {
                        AttemptToUnstuck();
                    }
                }
                else
                {
                    // 如果这次移动正常，重置卡住计数
                    _stuckCount = 0;
                }

                // 重置计时器和位置
                _stuckDetectionTimer = 0f;
                _lastPosition = transform.position;
            }
        }

        /// <summary>
        /// 尝试解除卡住状态
        /// </summary>
        private void AttemptToUnstuck()
        {
            LoggerUtils.Log("KCCPathFollower: 尝试解除卡住状态");

            // 重置卡住计数
            _stuckCount = 0;

            // 尝试以下策略来解除卡住：

            // 1. 首先尝试找到远离障碍物的方向
            Vector3 escapeDirection = FindEscapeDirection();

            // 2. 使用该方向施加一个强制移动
            Vector3 unstuckPosition = transform.position + escapeDirection * _unstuckForce;

            // 3. 直接设置位置，绕过正常的移动逻辑
            _kccMotor.SetPosition(unstuckPosition);

            // 4. 如果仍然卡住，考虑重新计算路径
            _pathRecalculationTimer = pathRecalculationInterval; // 强制在下一帧重新计算路径

            LoggerUtils.Log($"KCCPathFollower: 应用解卡方向 {escapeDirection}，新位置 {unstuckPosition}");
        }

        /// <summary>
        /// 找到远离障碍物的方向
        /// </summary>
        private Vector3 FindEscapeDirection()
        {
            // 初始化一个远离障碍物的方向
            Vector3 escapeDirection = Vector3.zero;

            // 在多个方向上检测障碍物
            Vector3[] directions = new Vector3[]
            {
                Vector3.forward, Vector3.back, Vector3.left, Vector3.right,
                (Vector3.forward + Vector3.right).normalized,
                (Vector3.forward + Vector3.left).normalized,
                (Vector3.back + Vector3.right).normalized,
                (Vector3.back + Vector3.left).normalized
            };

            float[] distances = new float[directions.Length];
            float minDistance = float.MaxValue;

            // 对每个方向进行射线检测，找出最近的障碍物
            for (int i = 0; i < directions.Length; i++)
            {
                RaycastHit hit;
                if (Physics.Raycast(transform.position + Vector3.up * 0.5f, directions[i], out hit, 2.0f))
                {
                    distances[i] = hit.distance;
                    if (hit.distance < minDistance)
                    {
                        minDistance = hit.distance;
                    }
                }
                else
                {
                    distances[i] = 2.0f; // 如果没有检测到障碍物，设为最大检测距离
                }
            }

            // 计算远离最近障碍物的方向
            for (int i = 0; i < directions.Length; i++)
            {
                // 距离越近的方向，权重越小（即我们更倾向于远离这个方向）
                float weight = distances[i] / 2.0f;
                escapeDirection += directions[i] * weight;
            }

            // 如果所有方向都有障碍物，可能会得到零向量，此时使用当前路径方向的反方向
            if (escapeDirection.magnitude < 0.1f)
            {
                if (_currentMoveDirection != Vector3.zero)
                {
                    escapeDirection = -_currentMoveDirection;
                }
                else
                {
                    // 随机选择一个方向作为逃脱方向
                    escapeDirection = new Vector3(UnityEngine.Random.Range(-1f, 1f), 0, UnityEngine.Random.Range(-1f, 1f)).normalized;
                }
            }
            else
            {
                escapeDirection.Normalize();
            }

            // 确保水平方向
            escapeDirection.y = 0;

            return escapeDirection;
        }
        #endregion
    }
}
