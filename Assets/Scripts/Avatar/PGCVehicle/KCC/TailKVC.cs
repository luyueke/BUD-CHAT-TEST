using Es;
using Game.Audio;
using Game.Avatar;
using Game.Config;
using Game.KinematicCharacter;
using Game.Vehicle.PGCVehicle.KVC;
using Message;
using UnityEngine;

public class TailKVC : PGCVehicleBaseKVC
{
    AnimationClip startAni;
    AnimationClip loopAni;
    AnimationClip endAni;

    bool isSkill1LoopPlaying = false;
    private bool _isSkill1Ending = false;
    private float _normalMoveSpeed;
    private float _normalAccelSpeed;
    private float _skill1PressTime;
    private bool _skill1PendingRelease;
    KinematicCharacterMotor _moter;
    public override void OnInit(KinematicCharacterMotor motor, bool isSelf, bool isReconstruction = false)
    {
        base.OnInit(motor, isSelf, isReconstruction);

        startAni = Loader.Load<AnimationClip>("Assets/Loadable/Animations/Vehicle/XiaoweiCar/runTofastrun.anim", motor.gameObject);
        loopAni = Loader.Load<AnimationClip>("Assets/Loadable/Animations/Vehicle/XiaoweiCar/fastrun.anim", motor.gameObject);
        endAni = Loader.Load<AnimationClip>("Assets/Loadable/Animations/Vehicle/XiaoweiCar/runToidle.anim", motor.gameObject);
    }

    public override void ApplyConfig(PgcVehicleConfig config)
    {
        base.ApplyConfig(config);
        _normalMoveSpeed = stableMovementData.MaxStableMoveSpeed;
        _normalAccelSpeed = airMovementData.AirAccelerationSpeed;
        _pgcVehicleAnimCtrl.SetAniState(PGCVehicleAniState.Start);
    }

    private Transform Seat_0;
    private Transform Seat_1;

    protected override void OnStateChange(PGCVehicleAniState state)
    {
        if(Seat_0 == null || Seat_1 == null)
        {
            Seat_0 = GameObjectEx.FindComponentByName<Transform>(Motor.gameObject, "Seat_0");
            Seat_1 = GameObjectEx.FindComponentByName<Transform>(Motor.gameObject, "Seat_1");
            if(Seat_0 != null && Seat_1 != null)
            {
                Seat_0.localPosition /= 1.7f;
                Seat_1.localPosition /= 1.7f;
            }
        }

        MessageHelper.Broadcast(MessageName.StopGameSound, IsSelf ? "Stop_Drive_1P" : "Stop_Drive_3P", Motor.gameObject);

        if (state == PGCVehicleAniState.Start)
        {
        }
        else if (state == PGCVehicleAniState.Idle)
        {
        }
        else if (state == PGCVehicleAniState.Run)
        {
            MessageHelper.Broadcast(MessageName.GameSound, "Vehicle_Drive", "Xiaowei_Run", IsSelf ? "Play_Drive_1P" : "Play_Drive_3P", Motor.gameObject);
        }
        else if (state == PGCVehicleAniState.Jump)
        {
            MessageHelper.Broadcast(MessageName.GameSound, "Vehicle_Jump", "Xiaowei_Jump", IsSelf ? "Play_Jump_1P" : "Play_Jump_3P", Motor.gameObject);
        }
        else if (state == PGCVehicleAniState.Land)
        {
        } 
    }

    private void ToIdle()
    {
        isSkill1LoopPlaying = false;
        _isSkill1Ending = false;
        if (_playerAnimCtrls.Count > 0)
        {
            foreach (var playerAnimCtrl in _playerAnimCtrls)
            {
                playerAnimCtrl.SetPlayerAniState(PlayerAniState.Idle);
            }
        }
    }

    public override void Release()
    {
        stableMovementData.MaxStableMoveSpeed = _normalMoveSpeed;
        airMovementData.MaxAirMoveSpeed = _normalMoveSpeed;
        airMovementData.AirAccelerationSpeed = _normalAccelSpeed;
        base.Release();
    }

    public override void UpdateVehicleVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        if (isSkill1LoopPlaying && !_isSkill1Ending && _moveInputVector.sqrMagnitude == 0f)
        {
            _moveInputVector = Motor.CharacterForward;
        }

        if (_isSkill1Ending && _moveInputVector.sqrMagnitude == 0f)
        {
            currentVelocity = Vector3.zero;
            return;
        }

        base.UpdateVehicleVelocity(ref currentVelocity, deltaTime);
    }

    protected override void UpdateAniState(Vector3 currentVelocity)
    {
        if (isSkill1LoopPlaying)
        {
            return;
        }
        base.UpdateAniState(currentVelocity);
    }

    public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        base.UpdateRotation(ref currentRotation, deltaTime);
    }

    public override void OnSkill(int skillId, bool isPress, string extraJson = null)
    {
        if (skillId != (int)SkillType.Skill1)
        {
            return;
        }

        if (isPress)
        {
            _pgcVehicleAnimCtrl.SetSkilling(true);
            _pgcVehicleAnimCtrl.SetAniState(PGCVehicleAniState.Skill1);
            _skill1PressTime = Time.time;
            _skill1PendingRelease = false;
            isSkill1LoopPlaying = true;
            if (_pgcVehicleConfig.skill1Value > 0f)
            {
                stableMovementData.MaxStableMoveSpeed = _pgcVehicleConfig.skill1Value * 2f;
                airMovementData.MaxAirMoveSpeed = _pgcVehicleConfig.skill1Value * 2f;
                airMovementData.AirAccelerationSpeed = _normalAccelSpeed * 3f;
            }
            MessageHelper.Broadcast(MessageName.GameSound, "Vehicle_Skill", "Xiaowei_Skill_Start",
                IsSelf ? "Play_Skill_1P" : "Play_Skill_3P", Motor.gameObject);
            
            if (_playerAnimCtrls.Count > 0)
            {
                foreach (var playerAnimCtrl in _playerAnimCtrls)
                {
                    playerAnimCtrl.LoadPlay(startAni,0);
                }
            }

            TimerManager.Inst.RunOnce("", 0.8f, () =>
            {
                //MessageHelper.Broadcast(MessageName.GameSound, "Vehicle_Skill", "Xiaowei_Skill_Loop", IsSelf ? "Play_Skill_1P" : "Play_Skill_3P", Motor.gameObject);
                MessageHelper.Broadcast(MessageName.GameSound, "Vehicle_Skill", "Vehicle_Skill_Loop", IsSelf ? "Play_Drive_1P" : "Play_Drive_3P", Motor.gameObject);
                if (_playerAnimCtrls.Count > 0)
                {
                    foreach (var playerAnimCtrl in _playerAnimCtrls)
                    {
                        playerAnimCtrl.LoadPlay(loopAni,0);
                    }
                }
            });
        }
        else
        {
            if (Time.time - _skill1PressTime < 1f)
            {
                _skill1PendingRelease = true;
                TimerManager.Inst.RunOnce("", 2f, () =>
                {
                    if (_skill1PendingRelease)
                    {
                        _skill1PendingRelease = false;
                        ReleaseSkill1();
                    }
                });
            }
            else
            {
                ReleaseSkill1();
            }
        }
    }

    private void ReleaseSkill1()
    {
        _isSkill1Ending = true;
        _pgcVehicleAnimCtrl.SetSkilling(false);
        _pgcVehicleAnimCtrl.SetAniState(PGCVehicleAniState.Skill1);
        stableMovementData.MaxStableMoveSpeed = _normalMoveSpeed;
        airMovementData.MaxAirMoveSpeed = _normalMoveSpeed;
        airMovementData.AirAccelerationSpeed = _normalAccelSpeed;
        MessageHelper.Broadcast(MessageName.GameSound, "Vehicle_Skill", "Xiaowei_Skill_Stop",
            IsSelf ? "Play_Skill_1P" : "Play_Skill_3P", Motor.gameObject);
        
        if (_playerAnimCtrls.Count > 0)
        {
            foreach (var playerAnimCtrl in _playerAnimCtrls)
            {
                playerAnimCtrl.LoadPlay(endAni,0);
            }
        }

        TimerManager.Inst.RunOnce("", 1.1f, () =>
        {
            ToIdle();
        });
    }

    public override void ChangeHonkingState(bool isHonking, string honkingSound)
    {
        base.ChangeHonkingState(isHonking, honkingSound);
        if (!isHonking && _pgcVehicleAnimCtrl != null && _pgcVehicleAnimCtrl.CurState == PGCVehicleAniState.Run)
        {
            MessageHelper.Broadcast(MessageName.GameSound, "Vehicle_Drive", "Xiaowei_Run",
                IsSelf ? "Play_Drive_1P" : "Play_Drive_3P", Motor.gameObject);
        }
    }

    public override void ChangeAirBanner(string strParam)
    {

    }
}