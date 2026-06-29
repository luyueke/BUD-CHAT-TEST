using FSM;
using UnityEngine;

public class PassengerState : PlayerStateTemplate<PlayerStateController>
{
    public PassengerState(PlayerState id, PlayerStateController owner) : base(id, owner)
    {
    }

    public override void EnterMainState()   
    {
        base.EnterMainState();
        owner.PlayerKCCtrl.SetFreezeCharacter(true);
        owner.PlayerKCCtrl.enabled = false;
        owner.PlayerKCCtrl.Motor.SetIsOnSimulate(false);
        owner.PlayerKCCtrl.IsDriveVehicle = true;

        if (owner.IsSelf)
        {

        }
    }

    public override void ExitMainState()
    {
        base.ExitMainState();
        owner.PlayerKCCtrl.SetFreezeCharacter(false);
        owner.PlayerKCCtrl.enabled = true;
        owner.PlayerKCCtrl.Motor.SetIsOnSimulate(true);
        owner.PlayerKCCtrl.IsDriveVehicle = false;
        if(owner.IsSelf)
        {

            

        }
    }

    // 如果需要播放特定的坐下动画，可以在 InitData 或 EnterMainState 中调用
    // public override void InitData(params object[] args)
    // {
    //     base.InitData(args);
    //     // owner.PlayerAnimCtrl.PlayAnim("Sit");
    // }
}

