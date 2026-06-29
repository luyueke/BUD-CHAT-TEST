using Es;
using Game.Pet;
using GameData.PgcData;

public class NpcPlayerIdleBehaviour : BaseIdleBehaviour {
    public PlayerAnimationCtrl avatarAnimCtr {
        set;
        get;
    }

    protected override void PlayUIEmoAnim() {
        switch (emoteSubType) {
            case EmoteSubType.SingleLoop:
                animationCtrl.PlaySingleEmoteForUICharacter(mData.mainIdle, null, isNeedSound, null, LoopNeedFinish);
                break;
            case EmoteSubType.DoubleLoop:
                avatarAnimCtr.gameObject.SetActive(false);
                animationCtrl.PlayDoubleEmoteForUICharacter(mData.mainIdle, avatarAnimCtr, null,
                    isNeedSound, null,LoopNeedFinish);

                break;
        }
    }

    protected override void PlayerSubAnim(string emoteID) {
        time = 0;
        var uiEmoData = DataTables.GetEmoUIConfig(emoteID);
        if (uiEmoData != null) {
            emoteSubType = (EmoteSubType)uiEmoData.emoType;
            switch (emoteSubType) {
                case EmoteSubType.Single:
                case EmoteSubType.SingleLoop:
                    animationCtrl.PlaySingleEmoteForUICharacter(emoteID, isPlaySound: isNeedSound,
                        OnCompleteSingleEmote: OnSingleOnceEmoteFinish);
                    break;
                case EmoteSubType.Double:
                case EmoteSubType.DoubleLoop:
                    animationCtrl.PlayDoubleEmoteForUICharacter(emoteID, avatarAnimCtr,
                        OnSingleOnceEmoteFinish, isNeedSound);

                    break;
            }
        }
    }

    protected override bool LoopNeedFinish() {
        if (emoteSubType == EmoteSubType.SingleLoop) {
            if (time > MinLoopTime) {
                isMainPlaying = false;
                ResetEmoteForUICharacter(animationCtrl);
                var config = animationCtrl.PlayConfigAni(emoAniDataList.ConvertToAniConfig(),
                    PlayAniType.SingleLoopEnd, PlaySub, isNeedSound);
                animationCtrl.SetUIEmoteExpression(animationCtrl.CreateExpression(config));
                return true;
            }

            return false;
        }

        if (emoteSubType == EmoteSubType.DoubleLoop) {
            if (time > MinLoopTime) {
                isMainPlaying = false;
                ResetEmoteForUICharacter(animationCtrl);
                var config = animationCtrl.PlayConfigAni(emoAniDataList.ConvertToAniConfig(),
                    PlayAniType.DoubleLoopPlayerAEnd, PlaySub, isNeedSound);
                animationCtrl.SetUIEmoteExpression(animationCtrl.CreateExpression(config));
                return true;
            }

            return false;
        }

        return false;
    }

    public override void ChangeOcAni() {
        base.ChangeOcAni();
        animationCtrl.PlayerChangeClothesForUICharacer(OnSingleOnceEmoteFinish, true);
    }
}
