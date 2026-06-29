using Game.KinematicCharacter;
using System.Collections.Generic;
using UnityEngine;

public class SkateKCC : BaseKCC
{
    public override KCCType KCCType => KCCType.Skate;

    public SkateKCC(string uid, bool isSelf) : base(uid, isSelf)
    {
        m_StableMovementData.MaxStableMoveSpeed = 8f;
        m_StableMovementData.StableMovementSharpness = 1f;
        m_StableMovementData.OrientationSharpness = 5f;

        m_AirMovementData.AirAccelerationSpeed = 1f;
        m_AirMovementData.MaxAirMoveSpeed = 8f;

        m_JumpingData.JumpUpSpeed = 12f;

        m_FastRunData.FastRunSpeed = 8f;

        m_MiscData.BonusOrientationMethod = BonusOrientationMethod.TowardsGroundSlopeAndGravity;
    }

    protected override void GroundMovement(ref Vector3 currentVelocity, float deltaTime)
    {
        float currentVelocityMagnitude = currentVelocity.magnitude;

        Vector3 effectiveGroundNormal = Motor.GroundingStatus.GroundNormal;

        // Reorient velocity on slope
        currentVelocity = Motor.GetDirectionTangentToSurface(currentVelocity, effectiveGroundNormal) * currentVelocityMagnitude;

        // Calculate target velocity
        Vector3 inputRight = Vector3.Cross(_moveInputVector, Motor.CharacterUp);
        Vector3 reorientedInput = Vector3.Cross(effectiveGroundNormal, inputRight).normalized * _moveInputVector.magnitude;

        float value = Mathf.Pow(reorientedInput.y, 2);
        if (reorientedInput.y > 0)
        {
            _stableMoveSpeed = (1 - value) * _stableMoveSpeed;
        }
        else
        {
            _stableMoveSpeed = (1 + value) * _stableMoveSpeed;
        }

        Vector3 targetMovementVelocity = reorientedInput * _stableMoveSpeed;

        // Smooth movement Velocity
        currentVelocity = Vector3.Lerp(currentVelocity, targetMovementVelocity, 1f - Mathf.Exp(-m_StableMovementData.StableMovementSharpness * deltaTime));
    }

    protected override void HandleJump(ref Vector3 currentVelocity, float deltaTime)
    {
        _jumpedThisFrame = false;
        _timeSinceJumpRequested += deltaTime;
        if (_jumpRequested)
        {
            if (!_jumpConsumed && ((m_JumpingData.AllowJumpingWhenSliding ? Motor.GroundingStatus.FoundAnyGround : Motor.GroundingStatus.IsStableOnGround) || _timeSinceLastAbleToJump <= m_JumpingData.JumpPostGroundingGraceTime))
            {
                Vector3 jumpDirection = Vector3.up;

                Motor.ForceUnground();

                currentVelocity += (jumpDirection * m_JumpingData.JumpUpSpeed) - Vector3.Project(currentVelocity, Motor.CharacterUp);
                currentVelocity += (_moveInputVector * m_JumpingData.JumpScalableForwardSpeed);
                _jumpRequested = false;
                _jumpConsumed = true;
                _jumpedThisFrame = true;
            }
        }
    }
}
