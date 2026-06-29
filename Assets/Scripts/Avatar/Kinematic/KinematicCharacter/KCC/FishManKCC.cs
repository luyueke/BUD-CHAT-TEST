using Game.Avatar.Kinematic.KinematicCharacter.KCC;
using GameData;
using Message;
using UnityEngine;

public class FishManKCC : BaseSpecialKCC {
    public FishManKCC(string uid, bool isSelf) : base(uid, isSelf) {
    }


    // 人鱼的快跑和跑 不依赖于动画，直接播放
    protected override void OnRunStateChange() {
        base.OnRunStateChange();
        if (IsFastRun) {
            MessageHelper.Broadcast<CharacterFootType, GameObject, GameObject, int>(MessageName.FootSound,
                CharacterFootType.FastRun, null, Motor.gameObject, IsSelf ? 0 : 1);
        } else if (PlayerAnimCtrl.CurAniState == PlayerAniState.Run) {
            MessageHelper.Broadcast<CharacterFootType, GameObject, GameObject, int>(MessageName.FootSound,
                CharacterFootType.Run, null, Motor.gameObject, IsSelf ? 0 : 1);
        }
    }


}
