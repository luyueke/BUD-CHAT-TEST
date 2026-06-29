using System;
using Es;
using Game.Pet;
using GameData.PgcData;

public class PlayerIdleBehaviour : BaseIdleBehaviour
{
    protected override void PlayUIEmoAnim()
    {
        switch (emoteSubType)
        {
            case EmoteSubType.SingleLoop:
                animationCtrl.PlaySingleEmoteForUICharacter(mData.mainIdle,null,isNeedSound,null, LoopNeedFinish);
                break;
        }
    }

    protected override  void PlayerSubAnim(string emoteID)
    {
        var uiEmoData = DataTables.GetEmoUIConfig(emoteID);
        if (uiEmoData != null)
        {
            emoteSubType = (EmoteSubType) uiEmoData.emoType;
            time = 0;
            switch (emoteSubType)
            {
                case EmoteSubType.Single:
                case EmoteSubType.SingleLoop:
                    animationCtrl.PlaySingleEmoteForUICharacter(emoteID, isPlaySound: isNeedSound,
                        OnCompleteSingleEmote: OnSingleOnceEmoteFinish);
                    break;
            }
        }
    }
    
    protected override bool LoopNeedFinish()
    {
        if (emoteSubType == EmoteSubType.SingleLoop)
        {
            if (time > MinLoopTime)
            {
                isMainPlaying = false;
                ResetEmoteForUICharacter(animationCtrl);
                var config = animationCtrl.PlayConfigAni(emoAniDataList.ConvertToAniConfig(), PlayAniType.SingleLoopEnd, PlaySub, isNeedSound);
                animationCtrl.SetUIEmoteExpression(animationCtrl.CreateExpression(config));
                return true;
            }
            return false;
        }
        return false;
    }

    public void OnlyPlayChangeOcAni(Action callback)
    {
        base.ChangeOcAni();
        animationCtrl.PlayerChangeClothesForUICharacer(callback, true);
    }
    

    public override void ChangeOcAni()
    {
        base.ChangeOcAni();
        animationCtrl.PlayerChangeClothesForUICharacer(OnSingleOnceEmoteFinish, true);
    }
}
