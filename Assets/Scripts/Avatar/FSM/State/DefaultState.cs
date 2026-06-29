using FSM;
using Game.KinematicCharacter;
using GameData;
using Message;
using UnityEngine;

public class DefaultState : PlayerStateTemplate<PlayerStateController> {
    public DefaultState(PlayerState id, PlayerStateController owner) : base(id, owner) {
    }

    public override void EnterMainState() {
        base.EnterMainState();
        // StateEventManager.Inst.RegisterStateEvent<Vector3>(owner.PlayerID, StateEvent.MoveJoystick, owner.MoveJoystick);
        StateEventManager.Inst.RegisterStateEvent(owner.PlayerID, StateEvent.JumpBtn, owner.OnClickJumpBtn);

        // 兜底：回到默认(idle)主状态时，人物必须可移动。
        // 自拍/载具等状态用的冻结开关(SetFreezeCharacter / SetIsOnSimulate)非引用计数，
        // 在自拍↔载具的中断/缓存交织退出顺序下可能漏掉复位，导致回到 idle 后仍无法移动。
        // 这里在恢复到默认状态时强制复位，保证该不变量。
        if (owner.PlayerKCCtrl != null) {
            owner.PlayerKCCtrl.SetFreezeCharacter(false);
            if (owner.PlayerKCCtrl.Motor != null) {
                owner.PlayerKCCtrl.Motor.SetIsOnSimulate(true);
            }
        }
    }

    public override void OnUpdate() {
        base.OnUpdate();
    }

    public override void ExitMainState() {
        base.ExitMainState();
        // StateEventManager.Inst.UnRegisterStateEvent<Vector3>(owner.PlayerID, StateEvent.MoveJoystick,
        //     owner.MoveJoystick);
        StateEventManager.Inst.UnRegisterStateEvent(owner.PlayerID, StateEvent.JumpBtn, owner.OnClickJumpBtn);
    }


}
