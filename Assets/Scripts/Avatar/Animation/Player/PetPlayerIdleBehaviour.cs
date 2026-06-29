using Es;
using System.Collections;
using System.Collections.Generic;
using Game.Pet;
using GameData.PgcData;
using UnityEngine;

public class PetPlayerIdleBehaviour : BaseIdleBehaviour
{
    public PlayerAnimationCtrl avatarAnimCtr { set; get; }
    protected override void PlayUIEmoAnim()
    {
        switch (emoteSubType)
        {
            case EmoteSubType.PetSingleLoop:
                animationCtrl.PlaySingleEmoteForUICharacter(mData.mainIdle,null,isNeedSound,null, LoopNeedFinish);
                break;
            case EmoteSubType.PetWithPlayerLoop:
                var petAnimationCtrl = animationCtrl as PetAnimationCtrl;
                if (petAnimationCtrl != null)
                {
                    petAnimationCtrl.PlayPetWithPlayerEmoteForUICharacter(mData.mainIdle, avatarAnimCtr, null,
                        isNeedSound, null, LoopNeedFinish);
                }
                break;
        }
    }

    protected override  void PlayerSubAnim(string emoteID)
    {
        time = 0;
        var uiEmoData = DataTables.GetEmoUIConfig(emoteID);
        if (uiEmoData != null)
        {
            emoteSubType = (EmoteSubType) uiEmoData.emoType;
            switch (emoteSubType)
            {
                case EmoteSubType.PetSingle:
                case EmoteSubType.PetSingleLoop:
                    animationCtrl.PlaySingleEmoteForUICharacter(emoteID, isPlaySound: isNeedSound,
                        OnCompleteSingleEmote: OnSingleOnceEmoteFinish);
                    break;
                case EmoteSubType.PetWithPlayer:
                case EmoteSubType.PetWithPlayerLoop:
                    var petAnimationCtrl = animationCtrl as PetAnimationCtrl;
                    if (petAnimationCtrl != null)
                    {
                        petAnimationCtrl.PlayPetWithPlayerEmoteForUICharacter(emoteID, avatarAnimCtr,
                            OnSingleOnceEmoteFinish, isNeedSound);
                    }
                    break;
            }
        }
    }
    
    protected override  bool LoopNeedFinish()
    {
        if (emoteSubType == EmoteSubType.PetSingleLoop)
        {
            if (time > MinLoopTime)
            {
                isMainPlaying = false;
                ResetEmoteForUICharacter(animationCtrl);
                var config = animationCtrl.PlayConfigAni(emoAniDataList.ConvertToAniConfig(), PlayAniType.PetSingleLoopEnd, PlaySub, isNeedSound);
                animationCtrl.SetUIEmoteExpression(animationCtrl.CreateExpression(config));
                return true;
            }
            return false;
        }
        if (emoteSubType == EmoteSubType.PetWithPlayerLoop)
        {
            if (time > MinLoopTime)
            {
                isMainPlaying = false;
                ResetEmoteForUICharacter(animationCtrl);
                var config = animationCtrl.PlayConfigAni(emoAniDataList.ConvertToAniConfig(), PlayAniType.PetWithPlayerLoopPetAEnd, PlaySub, isNeedSound);
                animationCtrl.SetUIEmoteExpression(animationCtrl.CreateExpression(config));
                return true;
            }
            return false;
        }
        return false;
    }
    
    public override void ChangeOcAni()
    {
        base.ChangeOcAni();
        var petCtrl = animationCtrl as PetAnimationCtrl;
        petCtrl.PlayChangeClothAni(OnSingleOnceEmoteFinish, true);
    }
}
