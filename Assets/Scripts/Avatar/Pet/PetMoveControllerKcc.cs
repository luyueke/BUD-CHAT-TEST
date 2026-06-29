using System;
using System.Collections.Generic;
using UnityEngine;
using Game.KinematicCharacter;
using Game.Pet;
using GameData;
using Message;

public enum PetMoveState
{
    Following,
    Wait
}

public enum PetAnim
{
    Idle,
    Run,
    FastRun,
    Jump,
}

public enum JumpState
{
    None = 0,
    Up = 1,
    Down = 2,
}

public struct WayPoint
{
    public Vector3 position;
    public Quaternion rotation;
    public bool IsGround;
    public Vector3 velocity;
}

public class PetMoveControllerKcc : MonoBehaviour
{
    public Transform followPlayer;
    public float followDistance = 4.0f;//普通跟随距离，超过该距离开始加速
    public float normalSpeed = 3.2f;
    public float fastSpeed = 6;
    public float maxSpeed = 20.0f;
    public float teleportDistance = 15.0f;//闪现距离，超过该距离则闪现到玩家后面
    public float stoppingDistance = 2.5f;//停止跟随的距离
    public float waypointThreshold = 0.3f;//路点检测范围
    public float waypointUpdateInterval = 0.01f;//路点更新间隔
    public int maxWaypoints = 30;
    public float startFollowingDistance = 6f;//启动距离，超过该距离开始跟随

    public float checkHeight = 2f;
    public float jumpHeight = 1.8f;
    
    public float yVelocity = 0f; // 用于模拟重力的Y轴速度
    public float gravity = -9.8f; // 重力加速度
    public float initialUpwardVelocity = 1.5f; // 初始向上速度
    public float diffRange = 0.1f;
    
    private PetAnimationCtrl petAnimationCtrl;
    private LinkedList<WayPoint> waypoints = new LinkedList<WayPoint>();
    private float waypointTimer = 0.0f;
    private PetMoveState currentState = PetMoveState.Wait;
    private PetAnim curAnim = PetAnim.Idle;
    private KinematicCharacterController petKccControler;
    private KinematicCharacterController playerKccControler;
    private string playerId;
    private bool IsGround = true;
    private float followSpeed = 3.2f;
    private bool IsFastRun = false;

    private bool isFollow = true;
    private bool isFristTeleport = true;

    public void InitPlayer(Transform playerTransform,string followPlayerId)
    {
        
        if (playerTransform != null)
        {
            followPlayer = playerTransform;
        }
        
        UnRegisterEvent();
        
        playerId = followPlayerId;
        RegisterEvent(followPlayerId);
    }

    private void RegisterEvent(string followPlayerId)
    {
        if (string.IsNullOrEmpty(followPlayerId)) return;
        StateEventManager.Inst.RegisterStateEvent<bool>(followPlayerId, StateEvent.FastRun, OnPlayerFastRun);
        StateEventManager.Inst.RegisterStateEvent<GroundEvent>(followPlayerId, StateEvent.GroundEvent, OnGroundEvent);
    }

    private void UnRegisterEvent()
    {
        if (string.IsNullOrEmpty(playerId)) return;
        StateEventManager.Inst.UnRegisterStateEvent<bool>(playerId, StateEvent.FastRun, OnPlayerFastRun);
        StateEventManager.Inst.UnRegisterStateEvent<GroundEvent>(playerId, StateEvent.GroundEvent, OnGroundEvent);
    }
    

    private void Start()
    {
        if (followPlayer != null)
        {
            playerKccControler = followPlayer.GetComponent<KinematicCharacterController>();
            var point = CreateWayPoint();
            waypoints.AddLast(point);
        }

        petKccControler = GetComponent<KinematicCharacterController>();
        petKccControler.CurIKCController._gravity = new Vector3(0, -9.8f, 0);
        
        petAnimationCtrl = GetComponentInChildren<PetAnimationCtrl>(true);
        followSpeed = normalSpeed;
        PlayIdleAnimation();
        
        //测试代码：
        // InitPlayer(null,AccountDataManager.Inst.Uid);
    }

    private Vector3 lastPlayerPos = Vector3.zero;
    private void LateUpdate()
    {
        
        waypointTimer += Time.deltaTime;
        if (waypointTimer >= waypointUpdateInterval)
        {
            waypointTimer = 0f;
            UpdateWaypoints();
        }
        
        CheckIsGround();
        if (followPlayer != null && isFollow)
        {
            FollowPlayer();
            if (lastPlayerPos != followPlayer.transform.position)
            {
                TestCreateCube(followPlayer.transform);
            }
            lastPlayerPos = followPlayer.transform.position;
        }
    }

    private void OnDestroy()
    {
        TimerManager.Inst.Stop(delayIdleTimer);
       UnRegisterEvent();
    }


    private void SetIsOnSimulate(bool isEnable)
    {
        petKccControler.Motor.SetIsOnSimulate(isEnable);
    }


    private BudTimer delayIdleTimer = null;
    void EnterWait()
    {
        if (IsGround)
        {
            PlayIdleAnimation();
        }
        else
        {
            TimerManager.Inst.Stop(delayIdleTimer);
            delayIdleTimer = TimerManager.Inst.RunOnce("delayIdle", 0.5f, () =>
            {
                if (this && currentState == PetMoveState.Wait)
                {
                    PlayIdleAnimation();
                }
            });
        }

        ClearWaypoints();
        FacePlayer();
        currentState = PetMoveState.Wait;
        SetIsOnSimulate(true);
        
        if(!IsGround)
        {
            petKccControler.AddVelocity(new Vector3(0,2f*yVelocity,0));
        }
    }

    void EnterFollow()
    {
        StateEventManager.Inst.TriggerStateEvent<bool>(playerId, StateEvent.PetBeginFollow, true);
        CheckIsGround();
        CheckAndHandleObstacles();
        currentState = PetMoveState.Following;
        SetIsOnSimulate(false);
        
    }
    

    void FollowPlayer()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, followPlayer.position);
        
        if (distanceToPlayer > teleportDistance)
        {
            TeleportToPlayer();
            EnterWait();
            return;
        }

        if (currentState == PetMoveState.Wait)
        {
            if (distanceToPlayer > startFollowingDistance)
            {
                EnterFollow();
            }
            else
            {
                return;
            }
        }

        if (currentState == PetMoveState.Following && distanceToPlayer < stoppingDistance)
        {
            EnterWait();
            return;
        }
        
        WayPoint targetPoint = waypoints.Count > 0 ? waypoints.First.Value: CreateWayPoint();

        MoveToTarget(targetPoint);
        
        if (waypoints.Count > 0)
        {
            var currentWaypoint = waypoints.First.Value; // 获取队列中第一个路径点（不移除）
        
            // 如果到达了当前路径点，移动到下一个
            // LoggerUtils.Log("####判断移除路点："+Vector3.Distance(transform.position, currentWaypoint.position) + " 是否移除："+(Vector3.Distance(transform.position, currentWaypoint.position) < waypointThreshold));
            if (Vector3.Distance(transform.position, currentWaypoint.position) < waypointThreshold)
            {
                waypoints.RemoveFirst(); // 移除当前路径点
            }
        }


        if (!IsGround && targetPoint.velocity.y != 0)
        {
            PlayJumpAnimation();
        }
        else
        {
            if (IsFastRun || distanceToPlayer > startFollowingDistance)
            {
                PlayFastRunAnimation();
            }
            else
            {
                PlayRunAnimation();
            }
        }
    }
    

    private JumpState curJumpState = JumpState.None;
    private JumpState lastJumpState = JumpState.None;
    
    void MoveToTarget(WayPoint targetPoint)
    {
        var targetPosition = targetPoint.position;
        float distance = Vector3.Distance(transform.position, targetPosition);
        // float speed = Mathf.Lerp(1.2f * followSpeed, maxSpeed, Mathf.InverseLerp(followDistance, teleportDistance, distance));
        float speed = fastSpeed;
        Vector3 newPosition = Vector3.Lerp(transform.position, targetPosition, speed * Time.deltaTime / distance);

        // LoggerUtils.Log("####MoveToTarget:" + "targetPosition"+targetPosition +  "  newPosition:"+newPosition +"  curJumpState:"+curJumpState + "  IsGround:"+IsGround  + "  point.IsGround:"+ targetPoint.IsGround +  "  yVelocity:"+yVelocity + "  point.velocity.y:"+targetPoint.velocity.y + "  wayPoint.Count:"+waypoints.Count);
        
        if (!targetPoint.IsGround)
        {
            if (targetPoint.velocity.y > 0 && yVelocity <= 0f)
            {
                yVelocity = initialUpwardVelocity;
            }
            float targetY = targetPosition.y;
            float gravityEffect = gravity * Time.deltaTime;
            yVelocity += gravityEffect;
        
            if (targetPoint.velocity.y > 0)
            {
                yVelocity = Mathf.Min(yVelocity,targetPoint.velocity.y);
            }
            else
            {
                yVelocity = Mathf.Max(yVelocity,targetPoint.velocity.y);
            }

            var adjustY = newPosition.y + yVelocity * Time.deltaTime;
            newPosition.y = Mathf.Clamp(adjustY, targetY - diffRange, targetY + diffRange);
        }

        
        if (targetPoint.IsGround)
        {
            newPosition.y = Mathf.Max(newPosition.y, targetPosition.y);
        }

        Vector3 direction = new Vector3(targetPosition.x - transform.position.x, 0, targetPosition.z - transform.position.z);
        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        petKccControler.SetCalculatePos(newPosition);
        petKccControler.Motor.SetPositionAndRotation(newPosition,targetRotation);

        //测试代码生成cube
        TestCreateCube(transform);
    }
    
    void TestCreateCube(Transform targetTf)
    {
        // var go = Loader.Load<GameObject>("Assets/Loadable/Model3D/Editor_Props/BaseShape/gdgt_Cube_PREFAB.prefab").Instantiate(targetTf.parent);
        // go.GetComponentInChildren<MeshCollider>().enabled = false;
        // go.transform.SetPositionAndRotation(targetTf.position,targetTf.rotation);
        // if (IsGround)
        // {
        //     go.transform.localScale = new Vector3(0.4f,0.4f,0.4f);
        // }
        // else
        // {
        //     go.transform.localScale = new Vector3(0.2f,0.2f,0.2f);
        // }
        
    }


    void TeleportToPlayer(float distance = 0)
    {
        if (distance == 0)
        {
            distance = followDistance;
        }

        Vector3 teleportPosition = followPlayer.position - followPlayer.forward * distance;
        Vector3 direction = new Vector3(teleportPosition.x - transform.position.x, 0, teleportPosition.z - transform.position.z);
        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        petKccControler.SetCalculatePos(teleportPosition);
        petKccControler.Motor.SetPositionAndRotation(teleportPosition,targetRotation);
        if (!isFristTeleport) //第一次创建的传送不触发动画
        {
            petAnimationCtrl.PlayTeleportingAni(); 
        }
        petKccControler.Motor.SetRotation(targetRotation);
        isFristTeleport = false;
    }

    private WayPoint CreateWayPoint()
    {
        WayPoint wayPoint = new WayPoint
        {
            position = followPlayer.position,
            rotation = followPlayer.rotation,
            velocity = playerKccControler.Motor.Velocity,
            IsGround = playerKccControler.IsGround(),
        };
        return wayPoint;
    }

    void UpdateWaypoints()
    {
        if (followPlayer != null)
        {
            var point = CreateWayPoint();
            waypoints.AddLast(point);
            // LoggerUtils.Log("####UpdateWaypoints add ："+followPlayer.position);
            if (waypoints.Count > maxWaypoints)
            {
                var removePoint = waypoints.First.Value;
                // LoggerUtils.Log("####UpdateWaypoints remove ："+removePoint);
                waypoints.RemoveFirst();
            }
        }
    }
    
    void ClearWaypoints()
    {
        waypoints.Clear();
    }

    void FacePlayer()
    {
        Vector3 direction = new Vector3(followPlayer.position.x - transform.position.x, 0, followPlayer.position.z - transform.position.z);
        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        petKccControler.Motor.SetRotation(targetRotation);
    }

    private PetAnimationCtrl GetAnimCtr()
    {
        if (petAnimationCtrl != null)
            return petAnimationCtrl;

      
        petAnimationCtrl = GetComponentInChildren<PetAnimationCtrl>(true);
        
        return petAnimationCtrl;
    }

    
    void PlayRunAnimation()
    {
        if(petAnimationCtrl == null) return;
        petAnimationCtrl.ParameterAnimation.SetFloat(PlayerAniPrameter.MoveSpeed, 3);
        petAnimationCtrl.ParameterAnimation.SetFloat(PlayerAniPrameter.PressJoystickTime, 0);
        PlayPetAnim(PetAnim.Run);
    }

    void PlayFastRunAnimation()
    {
        if(petAnimationCtrl == null) return;
        petAnimationCtrl.ParameterAnimation.SetFloat(PlayerAniPrameter.MoveSpeed, 10);
        petAnimationCtrl.ParameterAnimation.SetFloat(PlayerAniPrameter.PressJoystickTime, 10);
        PlayPetAnim(PetAnim.FastRun);
        // LoggerUtils.Log("###播放fast run动画");
    }

    void PlayIdleAnimation()
    {
        PlayPetAnim(PetAnim.Idle);
    }

    void PlayJumpAnimation()
    {
        PlayPetAnim(PetAnim.Jump);
    }

    private bool lastGround = true;
    private void CheckIsGround()
    {
        RaycastHit hit;
        Vector3 rayOrigin = transform.position;

        float raycastLength = 0.2f;
        if (curJumpState == JumpState.Down)
        {
            raycastLength = 0.4f;
        }

        if (Physics.Raycast(rayOrigin, Vector3.down, out hit,  raycastLength))
        {
            IsGround = true;
            yVelocity = 0;
        }
        else
        {
            IsGround = false;
        }
        
        if (lastGround != IsGround)
        {
            if (IsGround)
            {
                OnPetGroundEvent(GroundEvent.Landed);
            }
            else
            {
                OnPetGroundEvent(GroundEvent.LeaveStableGround);
            }
        }
        
        lastGround = IsGround;
    }
    
    private void OnPlayerFastRun(bool isFast)
    {
        // LoggerUtils.Log("###OnPlayerFastRun："+isFast);
        followSpeed = isFast ? fastSpeed : normalSpeed;
        IsFastRun = isFast;
    }

    private void OnPetGroundEvent(GroundEvent groundEvent)
    {
        // LoggerUtils.Log("###OnPetGroundEvent："+groundEvent);
        if (groundEvent == GroundEvent.LeaveStableGround)
        {
            MessageHelper.Broadcast<CharacterFootType, GameObject, GameObject, int>(MessageName.FootSound, CharacterFootType.Jump, null, this.gameObject, 2);
        }
        else
        {
            MessageHelper.Broadcast<CharacterFootType, GameObject, GameObject, int>(MessageName.FootSound, CharacterFootType.Landed, null, this.gameObject, 2);
        }


    }

    private void OnGroundEvent(GroundEvent groundEvent)
    {
        if (followPlayer == null || playerKccControler == null) return;
        if (groundEvent == GroundEvent.Landed)
        {
            waypoints.AddLast(CreateWayPoint());
            if (currentState == PetMoveState.Wait)
            {
                // IsGround = true;
                PlayIdleAnimation();
            }
        }
        else if (groundEvent == GroundEvent.LeaveStableGround)
        {
            waypoints.AddLast(CreateWayPoint());
            if (currentState == PetMoveState.Following)
            {
                // IsGround = false;
                PlayJumpAnimation();
            }
        }
    }


    void CheckAndHandleObstacles()
    {
        float maxDistance = 3;
        float petHeight = 0.1f;
        float forceTeleportDistance = 1.5f;
        RaycastHit hit;
        Vector3 directionToPlayer = followPlayer.position - transform.position;
        
        if (Physics.Raycast(transform.position + new Vector3(0, petHeight, 0), directionToPlayer, out hit, maxDistance))
        {
            TeleportToPlayer(forceTeleportDistance);
            EnterWait();
            // if (hit.collider.transform != followPlayer)
            // {
            //     // 计算障碍物的实际高度
            //     float obstacleHeight = hit.point.y - transform.position.y;
            //     if (obstacleHeight > checkHeight)
            //     {
            //         TeleportToPlayer(forceTeleportDistance);
            //         LoggerUtils.Log("##无法跳跃障碍物1，闪现");
            //         return;
            //     }
            //     
            //     Vector3 abovePetPosition = transform.position + new Vector3(0, checkHeight, 0);
            //     if (Physics.Raycast(abovePetPosition, directionToPlayer, out RaycastHit aboveHit, maxDistance))
            //     {
            //         if (aboveHit.collider.transform != followPlayer)
            //         {
            //             TeleportToPlayer(forceTeleportDistance);
            //             LoggerUtils.Log("##无法跳跃障碍物2，闪现");
            //         }
            //         else
            //         {
            //             // AddJumpWaypoint(hit);
            //             TeleportToPlayer(forceTeleportDistance);
            //             LoggerUtils.Log("##可以跳跃障碍物1：" + obstacleHeight);
            //         }
            //     }
            //     else
            //     {
            //         // AddJumpWaypoint(hit);
            //         TeleportToPlayer(forceTeleportDistance);
            //         LoggerUtils.Log("##可以跳跃障碍物2：" + obstacleHeight);
            //     }
        // }
        }
    }
    
    // private void AddJumpWaypoint(RaycastHit hit)
    // {
    //     Vector3 jumpPosition = hit.point + new Vector3(0, jumpHeight, 0);
    //     Vector3 directionToPlayer = (followPlayer.transform.position - transform.position).normalized;
    //     Vector3 forwardOffset = directionToPlayer * 0.3f;
    //     jumpPosition += forwardOffset;
    //
    //     WayPoint wayPoint = new WayPoint
    //     {
    //         position = jumpPosition,
    //         rotation = transform.rotation,
    //         IsGround = false,
    //         velocity = new Vector3(0,5f,0)
    //     };
    //     waypoints.AddFirst(wayPoint);
    // }
    private void AddJumpWaypoint(RaycastHit hit)
    {
        Vector3 jumpPosition = hit.point + new Vector3(0, jumpHeight, 0);
        Vector3 directionToPlayer = (followPlayer.transform.position - transform.position).normalized;
        Vector3 forwardOffset = directionToPlayer * 0.3f;
        jumpPosition += forwardOffset;

        WayPoint wayPoint = new WayPoint
        {
            position = jumpPosition,
            rotation = transform.rotation,
            IsGround = false,
            velocity = new Vector3(0,5f,0)
        };
        waypoints.AddFirst(wayPoint);
    }

    #region 对外公共方法
    
    //设置是否跟随
    public void SetIsFollow(bool value)
    {
        isFollow = value;
    }
    
    public void PlayPetAnim(PetAnim animType)
    {
        if (curAnim == animType) return;
        var animCtr = GetAnimCtr();
        if (animCtr != null)
        {
            PlayerAniState aniState = PlayerAniState.Idle;
            switch (animType)
            {
                case PetAnim.Run:
                case PetAnim.FastRun:
                    aniState = PlayerAniState.Run;
                    // LoggerUtils.Log("###播放run动画");
                    break;
                case PetAnim.Idle:
                    aniState = PlayerAniState.Idle;
                    // LoggerUtils.Log("###播放Idle动画");
                    break;
                case PetAnim.Jump:
                    aniState = PlayerAniState.Jump;
                    // LoggerUtils.Log("###播放跳跃动画");
                    break;
            }
            animCtr.SetPlayerAniState(aniState);
        }
        curAnim = animType;
    }


    #endregion
}
