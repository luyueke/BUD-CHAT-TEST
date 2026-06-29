using Es;
using Game.Audio;
using Game.KinematicCharacter;
using Game.Vehicle.PGCVehicle.KVC;
using Message;
using UnityEngine;

public class HotAirBallonKVC : PGCVehicleBaseKVC
{

    private Vector3 hotAirBallonVelocity = Vector3.zero;
    private Vector3 currentBallonVelocity = Vector3.zero;
    private Quaternion currentBallonRotation = Quaternion.identity;
    private bool isRotationInitialized = false;  // 标记旋转是否已初始化

    private float speed = 2f;
    private float rotationSpeed = 35f;
    private float maxHeight = 180.5f; // 热气球的最大上升高度，不然会把人送到天花板碰撞体上方去
    private float minHeight = 1.32f; // 热气球的最小下降高度，不然会把人送到地板碰撞体下方去

    public override void OnInit(KinematicCharacterMotor motor, bool isSelf, bool isReconstruction = false)
    {
        base.OnInit(motor, isSelf, isReconstruction);
        motor.SetCapsuleDimensions(0.65f, 1.3f, -0.65f);
        Motor.ForceUnground(1f);
        Collider floor = GameObjectEx.FindComponentByName<Collider>(motor.gameObject, "floor");
        Collider wall = GameObjectEx.FindComponentByName<Collider>(motor.gameObject, "wall");
        wall.gameObject.SetActive(false);
        if(wall != null && isSelf){
            wall.gameObject.SetActive(true);
        }
        miscData.IgnoredColliders.Add(floor);
        Motor.SetPosition(motor.transform.position + new Vector3(0, 0.8f, 0));
        currentBallonRotation = motor.transform.rotation;
        isRotationInitialized = true;
    }

    public override void ApplyConfig(PgcVehicleConfig config)
    {
        base.ApplyConfig(config);
        _pgcVehicleAnimCtrl.AddStateEvent("skill2End", "on_skill2_end", 0.85f, ToIdle);
        _pgcVehicleAnimCtrl.AddStateEvent("skill1End", "on_skill1_end", 0.85f, ToIdle);
        _pgcVehicleAnimCtrl.AddStateEvent("start", "on_start_end", 0.9f, ToIdle);
        _pgcVehicleAnimCtrl.AddStateEvent("skill1Loop", "on_skill1_loop", 0f, OnSkill1Loop);
        _pgcVehicleAnimCtrl.AddStateEvent("skill2Loop", "on_skill2_loop", 0f, OnSkill2Loop);
        _pgcVehicleAnimCtrl.SetAniState(PGCVehicleAniState.Start);

    }

    private void ToIdle(){
        _pgcVehicleAnimCtrl.ParameterAnimation.SetBool(PGCVehicleAnimParameter.isGround, true);
        _pgcVehicleAnimCtrl.SetAniState(PGCVehicleAniState.Idle);
    }

    bool isSkill1LoopPlaying = false;
    bool isSkill2LoopPlaying = false;
    
    // 转向状态跟踪（独立于 Skill1/2，可以同时按下）
    private bool isSkill3Pressed = false;  // 左转向
    private bool isSkill4Pressed = false;  // 右转向

    private void OnSkill1Loop(){
        if(isSkill1LoopPlaying){
            return;
        }
        MessageHelper.Broadcast(MessageName.GameSound, "Vehicle_Skill", _pgcVehicleConfig.vSkill1LoopAnim.audio, IsSelf ?  "Play_Skill_1P" : "Play_Skill_3P", Motor.gameObject);
        isSkill1LoopPlaying = true;
    }
    private void OnSkill2Loop(){
        if(isSkill2LoopPlaying){
            return;
        }
        MessageHelper.Broadcast(MessageName.GameSound, "Vehicle_Skill", _pgcVehicleConfig.vSkill2LoopAnim.audio, IsSelf ?  "Play_Skill_1P" : "Play_Skill_3P", Motor.gameObject);
        isSkill2LoopPlaying = true;
    }

    public override void Release()
    {
        base.Release();
        isSkill1LoopPlaying = false;
        isSkill2LoopPlaying = false;
        isSkill3Pressed = false;
        isSkill4Pressed = false;
        isRotationInitialized = false;
    }


    public override void UpdateVehicleVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        base.VehicleMove(ref currentVelocity, deltaTime);

        if(Motor.transform.position.y >= maxHeight){
            currentVelocity.y = 0;
            return;
        }
        else if(Motor.transform.position.y <= minHeight){
            currentVelocity.y = 0;
            return;
        }
        //一直让热气球向上飘
        currentBallonVelocity = Vector3.Lerp(currentBallonVelocity, hotAirBallonVelocity, deltaTime * speed);
        currentVelocity.y = currentBallonVelocity.y;

    }

    public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        // 首次调用时，初始化 currentBallonRotation（如果还未初始化）
        if (!isRotationInitialized)
        {
            currentBallonRotation = currentRotation;
            isRotationInitialized = true;
        }
        
        // 处理 Skill3/4 的转向逻辑（独立于 Skill1/2）
        float turnDirection = 0f;
        if (isSkill3Pressed && !isSkill4Pressed)
        {
            // 左转向（逆时针，负角度）
            turnDirection = rotationSpeed;
        }
        else if (isSkill4Pressed && !isSkill3Pressed)
        {
            // 右转向（顺时针，正角度）
            turnDirection = -rotationSpeed;
        }
        
        // 如果同时按下 Skill3 和 Skill4，不转向（turnDirection = 0）
        
        if (turnDirection != 0f && stableMovementData.OrientationSharpness > 0f)
        {
            // 使用配置的 angleVelocity 作为旋转速度（度/秒）
            float rotationSpeed = stableMovementData.OrientationSharpness;
            float rotationAngle = turnDirection * rotationSpeed * deltaTime;
            
            // 绕 Y 轴旋转（垂直轴）
            // 使用 currentBallonRotation 作为基础，确保旋转累积正确
            Quaternion rotationDelta = Quaternion.Euler(0f, rotationAngle, 0f);
            currentRotation = currentBallonRotation * rotationDelta;
            currentBallonRotation = currentRotation;
        }
        else
        {
            // 没有转向输入时，保持当前旋转
            // 使用 currentBallonRotation 作为当前旋转，确保状态一致
            // 但如果 currentRotation 已经被其他系统修改（比如物理同步），则同步到 currentBallonRotation
            if (Quaternion.Angle(currentBallonRotation, currentRotation) > 0.1f)
            {
                // 如果旋转差异较大，说明可能被外部修改，同步 currentBallonRotation
                currentBallonRotation = currentRotation;
            }
            else
            {
                // 否则保持 currentBallonRotation 的值
                currentRotation = currentBallonRotation;
            }
        }
    }

    public override void OnSkill(int skillId, bool isPress, string extraJson = null)
    {
        // Skill3/4 处理（转向，独立于 Skill1/2）
        if (skillId == (int)SkillType.Skill3)
        {
            isSkill3Pressed = isPress;
            return;  // 直接返回，不影响 Skill1/2 的逻辑
        }
        else if (skillId == (int)SkillType.Skill4)
        {
            isSkill4Pressed = isPress;
            return;  // 直接返回，不影响 Skill1/2 的逻辑
        }

        // Skill1/2 处理（上升/下降）
        _pgcVehicleAnimCtrl.SetSkilling(isPress);

        if(!isPress){
            // 释放 Skill1/2 时，重置垂直速度
            hotAirBallonVelocity = currentBallonVelocity = Vector3.zero;
            MessageHelper.Broadcast(MessageName.StopGameSound, IsSelf ?  "Stop_Skill_1P" : "Stop_Skill_3P", Motor.gameObject);
            if(skillId == (int)SkillType.Skill1){
                MessageHelper.Broadcast(MessageName.GameSound, "Vehicle_Skill", _pgcVehicleConfig.vSkill1EndAnim.audio, IsSelf ?  "Play_Skill_1P" : "Play_Skill_3P", Motor.gameObject);
            }else if(skillId == (int)SkillType.Skill2){
                MessageHelper.Broadcast(MessageName.GameSound, "Vehicle_Skill", _pgcVehicleConfig.vSkill2EndAnim.audio, IsSelf ?  "Play_Skill_1P" : "Play_Skill_3P", Motor.gameObject);
            }
            isSkill1LoopPlaying = false;
            isSkill2LoopPlaying = false;
            return;
        }

        if(skillId == (int)SkillType.Skill1){
            hotAirBallonVelocity = new Vector3(0, _pgcVehicleConfig.skill1Value, 0);
            Motor.ForceUnground(0.1f);
            _pgcVehicleAnimCtrl.SetAniState(PGCVehicleAniState.Skill1);
            MessageHelper.Broadcast(MessageName.GameSound, "Vehicle_Skill", _pgcVehicleConfig.vSkill1StartAnim.audio, IsSelf ?  "Play_Skill_1P" : "Play_Skill_3P", Motor.gameObject);
        }else if(skillId == (int)SkillType.Skill2){
            hotAirBallonVelocity = new Vector3(0, -_pgcVehicleConfig.skill2Value, 0);
            _pgcVehicleAnimCtrl.SetAniState(PGCVehicleAniState.Skill2);
            MessageHelper.Broadcast(MessageName.GameSound, "Vehicle_Skill", _pgcVehicleConfig.vSkill2StartAnim.audio, IsSelf ?  "Play_Skill_1P" : "Play_Skill_3P", Motor.gameObject);
        }
    }

    public override void ChangeAirBanner(string strParam)
    {
        
    }
    
}