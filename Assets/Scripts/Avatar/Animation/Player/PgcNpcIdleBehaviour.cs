using System;
using System.Collections.Generic;
using Es;
using GameData.BaseInfo;
using GameData.PgcData;
using UnityEngine;

public class PgcNpcIdleBehaviour : MonoBehaviour
{
    public const float MinLoopTime = 15;
    public const string IdleDefaultName = "default";

    public PlayerAnimationCtrl animationCtrl;
    protected List<AINpcIdleAnim> mData;
    protected List<EmoAniConfig> emoAniDataList;
    protected EmoteSubType emoteSubType;
    protected float time;
    protected bool isMainPlaying;
    protected bool isNeedSound;
    private bool isContainSubAnim = true;
    
    public void Init(PlayerAnimationCtrl ctrl, bool needSound = false)
    {
        isNeedSound = needSound;
        animationCtrl = ctrl;
    }

    public void SetData(List<AINpcIdleAnim> data)
    {
        mData = data;
        ResetEmoteForUICharacter(animationCtrl);
    }

    public void SetSubAnimState(bool containSubAnim)
    {
        isContainSubAnim = containSubAnim;
    }
    
    public void PlayMainAnim()
    {
        var pgcList = GetPgcListByNpcType((int) AINpcAnimType.Idle);
        if (pgcList == null)
        {
            pgcList = new List<string> {IdleDefaultName};
        }

        if (pgcList.Count == 0)
        {
            pgcList.Add(IdleDefaultName);
        }

        int index = UnityEngine.Random.Range(0, pgcList.Count);
        string mainId = pgcList[index];
        PlayMainAnimByUI(mainId);
    }

    public void PlayMainAnimByUI(string id)
    {
        var emoConfigData = Es.DataTables.GetEmoUIConfig(id);
        if (emoConfigData == null)
        {
            if (id == IdleDefaultName)
            {
                SetPlayerState(animationCtrl, PlayerState.Default);
            }

            ResetEmoteForUICharacter(animationCtrl);
        }
        else
        {
            emoAniDataList = Es.DataTables.GetEmoAniConfigList()
                .FindAll((emoAniData) => emoAniData.emoId == id);
            var uiEmoData = DataTables.GetEmoUIConfig(id);
            if (uiEmoData != null)
            {
                emoteSubType = (EmoteSubType) uiEmoData.emoType;
                PlayUIEmoAnim(id);
            }
        }

        isMainPlaying = true;
    }

    public void PlayDefaultSub()
    {
        if (isContainSubAnim)
        {
            PlaySubAnimByNpcType((int) AINpcAnimType.Assist);
        }
        else
        {
            time = 0;
            PlayMainAnim();
        }
    }

    public void PlaySubAnimByUI(string emoteId)
    {
        isMainPlaying = false;
        animationCtrl.ResetEmoteForUICharacter();
        PlaySubAnim(emoteId);
    }

    
    public void PlaySubAnimByNpcType(int npcType)
    {
        var pgcList = GetPgcListByNpcType(npcType);
        if (pgcList == null || pgcList.Count == 0)
        {
            return;
        }
        int index = UnityEngine.Random.Range(0, pgcList.Count);
        string emoteID = pgcList[index];
        PlaySubAnim(emoteID);
    }

   
    public void ResetEmoteAnim()
    {
        animationCtrl.ResetEmoteForUICharacter();
    }

    private void OnEnable()
    {
        if (mData != null)
        {
            ResetEmoteForUICharacter(animationCtrl);
            PlayMainAnim();
        }
    }

    private void ResetEmoteForUICharacter(PlayerAnimationCtrl ctrl)
    {
        if (ctrl != null)
        {
            ctrl.ResetEmoteForUICharacter();
        }
    }

    private void SetPlayerState(PlayerAnimationCtrl ctrl, PlayerState state)
    {
        if (ctrl != null)
        {
            ctrl.SetPlayerState(state);
        }
    }

    private List<string> GetPgcListByNpcType(int npcType)
    {
        var idleAnim = mData?.Find(x => x.npcAnimationType == npcType);
        if (idleAnim != null)
        {
            return idleAnim.pgcIdleList;
        }

        return null;
    }

    
    private void Update()
    {
        if (!isMainPlaying || mData == null) return;
        time += Time.deltaTime;

        if (time > MinLoopTime)
        {
            PlayDefaultSub();
        }
    }


    private void PlayUIEmoAnim(string id)
    {
        switch (emoteSubType)
        {
            case EmoteSubType.SingleLoop:
                animationCtrl.PlaySingleEmoteForUICharacter(id, null, isNeedSound, null, LoopNeedFinish);
                break;
        }
    }

    private void PlaySubAnim(string emoteID)
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
                        OnCompleteSingleEmote: PlayMainAnim);
                    break;
            }
        }
    }

    private bool LoopNeedFinish()
    {
        if (emoteSubType == EmoteSubType.SingleLoop)
        {
            if (time > MinLoopTime)
            {
                isMainPlaying = false;
                ResetEmoteForUICharacter(animationCtrl);
                var config = animationCtrl.PlayConfigAni(emoAniDataList.ConvertToAniConfig(), PlayAniType.SingleLoopEnd,
                    PlayDefaultSub, isNeedSound);
                animationCtrl.SetUIEmoteExpression(animationCtrl.CreateExpression(config));
                return true;
            }

            return false;
        }

        return false;
    }

    public void OnlyPlayChangeOcAni(Action callback)
    {
        isMainPlaying = false;
        time = 0;
        animationCtrl.PlayerChangeClothesForUICharacer(callback, true);
    }


    public void ChangeOcAni()
    {
        isMainPlaying = false;
        time = 0;
        animationCtrl.PlayerChangeClothesForUICharacer(PlayMainAnim, true);
    }
}