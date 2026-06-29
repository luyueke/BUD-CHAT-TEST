using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Game.Pet;
using GameData;
using Message;

/// <summary>
/// 漂浮宠物控制器 - 实现漂浮跟随效果
/// </summary>
public class FloatingPartController : MonoBehaviour
{
    [Header("跟随设置")]
    public Transform followTarget; // 跟随目标
    public float followDistance = 3.0f; // 普通跟随距离
    public float followSpeed = 5.0f; // 跟随速度
    public float stoppingDistance = 2.5f; // 停止跟随距离
    public float teleportDistance = 15.0f; // 传送距离
    public Vector3 offsetFromPlayer = new Vector3(1.0f, 0.3f, 0.5f); // 相对玩家的偏移
    
    [Header("漂浮设置")]
    public float floatHeight = 0.8f; // 基础漂浮高度，从1.5降低到0.8
    public float floatAmplitude = 0.2f; // 漂浮幅度
    public float floatFrequency = 1.0f; // 漂浮频率
    
    [Header("延迟跟随设置")]
    public bool useDelayedFollow = true; // 是否使用延迟跟随
    public int maxWaypoints = 20; // 最大路点数量
    public float waypointUpdateInterval = 0.05f; // 路点更新间隔
    public float waypointDelay = 0.5f; // 路点跟随延迟(秒)
    
    [Header("旋转设置")]
    public float rotationSpeed = 5.0f; // 旋转速度
    public float randomRotationChance = 0.01f; // 随机旋转几率
    public float randomRotationAngle = 30.0f; // 随机旋转角度
    
    // 内部变量
    private Vector3 targetPosition;
    private Vector3 currentVelocity;
    private float floatOffset;
    private Quaternion targetRotation;
    private PetAnimationCtrl petAnimationCtrl;
    private string playerId;
    private bool isFastRun = false;
    private bool isFollow = true;
    private bool firstTeleport = true;
    
    // 延迟跟随相关
    private LinkedList<DelayedWaypoint> waypoints = new LinkedList<DelayedWaypoint>();
    private float waypointTimer = 0.0f;

    /// <summary>
    /// 延迟路点结构
    /// </summary>
    public struct DelayedWaypoint
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 velocity;
        public float creationTime;
    }

    /// <summary>
    /// 初始化跟随目标和玩家ID
    /// </summary>
    public void InitPlayer(Transform playerTransform, string followPlayerId)
    {
        if (playerTransform != null)
        {
            followTarget = playerTransform;
        }
        
        UnRegisterEvent();
        
        playerId = followPlayerId;
        RegisterEvent(followPlayerId);
        
        // 初始化一些参数
        floatOffset = UnityEngine.Random.Range(0, 2 * Mathf.PI);
        petAnimationCtrl = GetComponentInChildren<PetAnimationCtrl>(true);
        PlayIdleAnimation();
    }

    private void RegisterEvent(string followPlayerId)
    {
        if (string.IsNullOrEmpty(followPlayerId)) return;
        StateEventManager.Inst.RegisterStateEvent<bool>(followPlayerId, StateEvent.FastRun, OnPlayerFastRun);
    }

    private void UnRegisterEvent()
    {
        if (string.IsNullOrEmpty(playerId)) return;
        StateEventManager.Inst.UnRegisterStateEvent<bool>(playerId, StateEvent.FastRun, OnPlayerFastRun);
    }
    
    private void Start()
    {
        // 确保组件引用正确
        if (petAnimationCtrl == null)
        {
            petAnimationCtrl = GetComponentInChildren<PetAnimationCtrl>(true);
        }
    }

    private void OnDestroy()
    {
        UnRegisterEvent();
    }

    private void LateUpdate()
    {
        if (followTarget == null || !isFollow) return;

        // 更新路点
        if (useDelayedFollow)
        {
            UpdateWaypoints();
        }
        
        // 执行跟随
        FollowPlayer();
    }

    private void UpdateWaypoints()
    {
        waypointTimer += Time.deltaTime;
        if (waypointTimer >= waypointUpdateInterval)
        {
            waypointTimer = 0f;
            
            // 创建新路点
            DelayedWaypoint waypoint = new DelayedWaypoint
            {
                position = followTarget.position,
                rotation = followTarget.rotation,
                velocity = Vector3.zero, // 可以根据需要获取目标速度
                creationTime = Time.time
            };
            
            waypoints.AddLast(waypoint);
            
            // 移除过期路点
            while (waypoints.Count > maxWaypoints)
            {
                waypoints.RemoveFirst();
            }
        }
    }

    private void FollowPlayer()
    {
        // 计算与目标的距离
        float distanceToTarget = Vector3.Distance(transform.position, followTarget.position);
        
        // 如果距离太远，直接传送到玩家附近
        if (distanceToTarget > teleportDistance)
        {
            TeleportToPlayer();
            return;
        }
        
        // 根据是否使用延迟跟随选择不同的跟随目标
        Vector3 targetPos;
        Quaternion targetRot;
        
        if (useDelayedFollow && waypoints.Count > 0)
        {
            // 获取延迟的路点
            DelayedWaypoint currentWaypoint = GetDelayedWaypoint();
            targetPos = currentWaypoint.position;
            targetRot = currentWaypoint.rotation;
        }
        else
        {
            // 直接跟随当前目标
            targetPos = followTarget.position;
            targetRot = followTarget.rotation;
        }
        
        // 应用偏移和漂浮效果
        // 使用固定的世界坐标系偏移
        Vector3 offsetPos = new Vector3(offsetFromPlayer.x, offsetFromPlayer.y, offsetFromPlayer.z);
        float floatY = floatAmplitude * Mathf.Sin((Time.time + floatOffset) * floatFrequency);
        Vector3 finalTargetPosition = targetPos + offsetPos + new Vector3(0, floatHeight + floatY, 0);
        
        // 检查是否需要移动（距离是否足够远）
        bool needToMove = distanceToTarget > stoppingDistance;
        
        // 应用平滑移动
        if (needToMove)
        {
            // 根据距离调整速度
            float currentSpeed = isFastRun ? followSpeed * 1.5f : followSpeed;
            
            // 使用SmoothDamp进行平滑移动
            transform.position = Vector3.SmoothDamp(
                transform.position,
                finalTargetPosition,
                ref currentVelocity,
                distanceToTarget > followDistance ? 0.1f : 0.3f,
                currentSpeed);
                
            // 播放移动动画
            if (isFastRun)
            {
                PlayFastRunAnimation();
            }
            else
            {
                PlayRunAnimation();
            }
        }
        else
        {
            // 距离近时播放闲置动画
            PlayIdleAnimation();
        }
        
        // 让宠物的朝向与跟随物体保持一致
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
    }
    
    /// <summary>
    /// 获取延迟的路点
    /// </summary>
    private DelayedWaypoint GetDelayedWaypoint()
    {
        float targetTime = Time.time - waypointDelay;
        
        // 如果延迟时间比第一个点还早，返回第一个点
        if (waypoints.First.Value.creationTime > targetTime)
        {
            return waypoints.First.Value;
        }
        
        // 找到最接近目标时间的点
        LinkedListNode<DelayedWaypoint> current = waypoints.First;
        while (current.Next != null && current.Next.Value.creationTime <= targetTime)
        {
            current = current.Next;
        }
        
        return current.Value;
    }

    /// <summary>
    /// 传送到玩家附近
    /// </summary>
    private void TeleportToPlayer(float distance = 0)
    {
        if (distance == 0)
        {
            distance = followDistance;
        }

        // 计算传送位置（玩家附近位置）
        Vector3 teleportPosition = followTarget.position + offsetFromPlayer + new Vector3(0, floatHeight, 0);
        
        // 设置位置并保持与跟随物体相同的朝向
        transform.position = teleportPosition;
        transform.rotation = followTarget.rotation;
        
        // 清除速度
        currentVelocity = Vector3.zero;
        
        // 第一次传送不播放特效
        if (!firstTeleport && petAnimationCtrl != null)
        {
            petAnimationCtrl.PlayTeleportingAni();
        }
        
        firstTeleport = false;
        
        // 清空路点
        waypoints.Clear();
    }

    /// <summary>
    /// 玩家快跑状态变化回调
    /// </summary>
    private void OnPlayerFastRun(bool isFast)
    {
        isFastRun = isFast;
    }

    /// <summary>
    /// 设置是否跟随
    /// </summary>
    public void SetIsFollow(bool value)
    {
        isFollow = value;
        
        if (!isFollow)
        {
            PlayIdleAnimation();
        }
    }
    
    #region 动画控制

    private void PlayIdleAnimation()
    {
        PlayPetAnim(PetAnim.Idle);
    }

    private void PlayRunAnimation()
    {
        if (petAnimationCtrl == null) return;
        petAnimationCtrl.ParameterAnimation.SetFloat(PlayerAniPrameter.MoveSpeed, 3);
        petAnimationCtrl.ParameterAnimation.SetFloat(PlayerAniPrameter.PressJoystickTime, 0);
        PlayPetAnim(PetAnim.Run);
    }

    private void PlayFastRunAnimation()
    {
        if (petAnimationCtrl == null) return;
        petAnimationCtrl.ParameterAnimation.SetFloat(PlayerAniPrameter.MoveSpeed, 10);
        petAnimationCtrl.ParameterAnimation.SetFloat(PlayerAniPrameter.PressJoystickTime, 10);
        PlayPetAnim(PetAnim.FastRun);
    }

    public void PlayPetAnim(PetAnim animType)
    {
        if (petAnimationCtrl == null) return;
        
        PlayerAniState aniState = PlayerAniState.Idle;
        switch (animType)
        {
            case PetAnim.Run:
            case PetAnim.FastRun:
                aniState = PlayerAniState.Run;
                break;
            case PetAnim.Idle:
                aniState = PlayerAniState.Idle;
                break;
            case PetAnim.Jump:
                aniState = PlayerAniState.Jump;
                break;
        }
        petAnimationCtrl.SetPlayerAniState(aniState);
    }

    public void SetFlowTarget(GameObject go_Fllow)
    {
        if(go_Fllow == null)
            return;
        
        this.followTarget = go_Fllow.transform;
    }

    #endregion
}
