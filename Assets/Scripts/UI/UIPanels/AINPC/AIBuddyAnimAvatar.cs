using System;
using System.Collections;
using System.Collections.Generic;
using BUD.AnimPose;
using Game.Avatar;
using GameData.Account;
using GameData.BaseInfo;
using UnityEngine;

public class AIBuddyAnimAvatar : MonoBehaviour
{
    [SerializeField] private Transform _avatarRoot;
    [SerializeField] private Camera _avatarCamera;

    protected CharacterWrap _characterWrap;
    private RenderTexture _renderTexture;
    private PgcNpcIdleBehaviour pgcIdleBehav;
    private UgcNpcIdleBehaviour ugcIdleBehav;
    void Start()
    {
        
    }

    private CharacterWrap CreateAvatar(CharacterData characterData)
    {
        var characterWrap = AvatarController.Inst.CreateUIAvatarWithIKController(characterData,_avatarRoot);
        var animationCtrl = characterWrap.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
        pgcIdleBehav = characterWrap.Avatar.AddComponent<PgcNpcIdleBehaviour>();
        pgcIdleBehav.Init(animationCtrl);
            
        ugcIdleBehav = characterWrap.Avatar.AddComponent<UgcNpcIdleBehaviour>();
        var playerIkController = characterWrap.Avatar.GetComponent<AnimIKController>();
        ugcIdleBehav.Init(playerIkController);
        
        return characterWrap;
    }

    public void RefreshAvatar(CharacterData characterData)
    {
        if (_characterWrap == null)
        {
            _characterWrap = CreateAvatar(characterData);
        }
        else
        {
            _characterWrap.RefreshAvatar(characterData);
        }
    }

    public void ResetAnim()
    {
        pgcIdleBehav?.SetData(null);
        ugcIdleBehav?.ResetData();
    }

    public void PlayIdleAnim(AIBuddyInfo buddyInfo)
    {
        if (buddyInfo.npc != null && buddyInfo.npc.npcAnimations != null && buddyInfo.npc.npcAnimations.Count > 0)
        {
            var npcInfo = buddyInfo.npc;
            bool isPgcRes = npcInfo.animResType == (int)AnimResType.PGC;
            var ikController = _characterWrap?.Avatar.GetComponent<AnimIKController>();
            ikController?.ChangeAnimResType(isPgcRes ? AnimResType.PGC : AnimResType.UGC);
            ikController?.RemovePropIks();
            if (isPgcRes)
            {
                pgcIdleBehav?.SetData(npcInfo.npcAnimations);
                pgcIdleBehav?.SetSubAnimState(false);
                pgcIdleBehav?.PlayMainAnim();
            }
            else
            {
                ugcIdleBehav?.SetData(npcInfo.npcAnimations);
                ugcIdleBehav?.SetSubAnimState(false);
                ugcIdleBehav?.PlayMainAnim();
            }
        }
    }

    private RenderTexture CreateRenderTextureFromPrefab()
    {
        var srcRt = _avatarCamera.targetTexture;
        RenderTexture targetTexture = RenderTexture.GetTemporary(
            srcRt.width,
            srcRt.height,
            0,
            RenderTextureFormat.ARGB32,
            RenderTextureReadWrite.Linear);
        targetTexture.depthStencilFormat = srcRt.depthStencilFormat;
        _avatarCamera.targetTexture = targetTexture;
        return targetTexture;
    }

    public RenderTexture GetRenderTexture()
    {
        if (_renderTexture == null)
        {
            _renderTexture = CreateRenderTextureFromPrefab();
        }

        return _renderTexture;
    }

    private void OnDestroy()
    {
        if (_renderTexture != null)
        {
            // Destroy(_renderTexture);
            _renderTexture = null;
        }
    }
}
