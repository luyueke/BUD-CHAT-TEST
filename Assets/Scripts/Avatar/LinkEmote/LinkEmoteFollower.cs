using Game.KinematicCharacter;
using UnityEngine;
using UnityEngine.Serialization;

public class LinkEmoteFollower : MonoBehaviour
{
    public Transform targetA; // 玩家A的Transform
    public Vector3 runPosOffset; // 相对偏移（在A的本地空间）
    public Vector3 runRotOffset = Vector3.zero; // 偏移角度（欧拉角）
    public Vector3 idlePosOffset = new Vector3(1, 0, 0);//待机时位置偏移
    public Vector3 idleRotOffset = new Vector3(0, 0, 0);//待机时角度偏移
    public Vector3 runFastPosOffset = new Vector3(1, 0, 0);//待机时位置偏移
    public Vector3 runFastRotOffset = new Vector3(0, 0, 0);//待机时角度偏移
    private PlayerAnimationCtrl playerAnimCtrl; // 动画组件
    private PlayerAnimationCtrl targetAnimCtrl;//跟随目标动画组件
    private KinematicCharacterController selfKccController; // 角色BKinematicCharacterMotor
    private KinematicCharacterController targetKccController;//玩家AKinematicCharacterMotor
    private string playerIdA;
    
    private Vector3 currentPosition; // 当前帧计算的位置
    private Quaternion currentRotation; // 当前帧计算的旋转
    public bool UseLerp = false;
    public float LerpSpeed = 5f; // 插值速度// 是否启用插值

    private bool isFollow = false;
    private void Awake()
    {
        playerAnimCtrl = GetComponentInChildren<PlayerAnimationCtrl>(true);
        selfKccController = GetComponentInChildren<KinematicCharacterController>(true);
        // 初始化当前帧的变量
        currentPosition = transform.position;
        currentRotation = transform.rotation;
    }

    // 初始化跟随关系
    public void Initialize(Transform playerA, string playerIdA)
    {
        this.playerIdA = playerIdA;
        targetA = playerA;
        var targetKccController = targetA.GetComponentInChildren<KinematicCharacterController>(true);
        if (targetKccController != null)
        {
            this.targetKccController = targetKccController;
        }
        
        var targetAnimCtrl = targetA.GetComponentInChildren<PlayerAnimationCtrl>(true);
        if (targetAnimCtrl != null)
        {
            this.targetAnimCtrl = targetAnimCtrl;
        }
    }
    
    protected void SetIsOnSimulate(bool isEnable)
    {
        if (selfKccController != null)
        {
            selfKccController.Motor.SetIsOnSimulate(isEnable);
            selfKccController.SetFreezeCharacter(!isEnable);
        }
    }

    public void StartFollow()
    {
        isFollow = true;
    }

    public void StopFollow()
    {
        isFollow = false;
    }

    public void SyncPlayerAniState()
    {
        if (targetAnimCtrl != null && playerAnimCtrl != null)
        {
            playerAnimCtrl.SetPlayerAniState(targetAnimCtrl.CurAniState);
        }
        else
        {
            playerAnimCtrl?.SetPlayerAniState(PlayerAniState.Idle);
        }
    }


    // 更新偏移
    public void UpdateRunOffset(Vector3 newLocalOffset)
    {
        runPosOffset = newLocalOffset;
    }
    
    // 更新偏移角度
    public void UpdateRunRotation(Vector3 newLocalRotation)
    {
        runRotOffset = newLocalRotation;
    }

    public void UpdateIdleOffset(Vector3 pos)
    {
        idlePosOffset = pos;
    }
    
    public void UpdateIdleRotation(Vector3 rot)
    {
        idleRotOffset = rot;
    }
    
    public void UpdateRunFastOffset(Vector3 pos)
    {
        runFastPosOffset = pos;
    }
    
    public void UpdateRunFastRotation(Vector3 rot)
    {
        runFastRotOffset = rot;
    }
    


    // 播放动画
    public void PlayAnimation(string animationName)
    {
        if (playerAnimCtrl != null)
        {
            playerAnimCtrl.Play(animationName);
        }
    }
    
    // 停止跟随
    public void Release()
    {
        targetA = null;
    }

    private void LateUpdate()
    {
        if (targetA == null || !isFollow) return;
        
        if (targetAnimCtrl != null)
        {
            float pressTime = targetAnimCtrl.GetPressJoystickTime();
            playerAnimCtrl?.SetPressJoystickTime(pressTime);
        }
        
        Vector3 offsetPos = Vector3.zero;
        Vector3 offsetRot = Vector3.zero;
        
        //Idle
        if (targetKccController != null && targetKccController.CurIKCController != null && !targetKccController.CurIKCController.IsMoving)
        {
            offsetPos = idlePosOffset;
            offsetRot = idleRotOffset;
        }
        else
        {
            //RunFast
            if (playerAnimCtrl&& playerAnimCtrl.GetPressJoystickTime() > 2.25f)
            {
                offsetPos = runFastPosOffset;
                offsetRot = runFastRotOffset;
            }
            //Run
            else
            {
                offsetPos = runPosOffset;
                offsetRot = runRotOffset;
            }
        }
        
        Vector3 targetPosition = targetA.position + targetA.rotation * offsetPos;
        // 计算全局旋转：A的全局旋转 * B的本地旋转
        Quaternion targetRotation = targetA.rotation * Quaternion.Euler(offsetRot);
        
        // 判断是否使用插值
        if (UseLerp)
        {
            currentPosition = Vector3.Lerp(currentPosition, targetPosition, LerpSpeed * Time.deltaTime);
            currentRotation = Quaternion.Lerp(currentRotation, targetRotation, LerpSpeed * Time.deltaTime);
        }
        else
        {
            currentPosition = targetPosition;
            currentRotation = targetRotation;
        }

        if (selfKccController != null)
        {
            selfKccController.SetCalculatePos(currentPosition);
            selfKccController.Motor.SetPositionAndRotation(currentPosition, currentRotation);
        }
        else
        {
            transform.SetPositionAndRotation(currentPosition, currentRotation);
        }
        
    }

}