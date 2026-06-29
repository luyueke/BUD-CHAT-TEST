
using System.Collections.Generic;
using BUD.AnimPose;
using DG.Tweening;
using Es;
using Game.Avatar;
using Game.Pet;
using GameData.Account;
using GameData.BaseInfo;
using GameData.PgcData;
using Pb.Base;
using UnityEngine;


public class AvatarAndPetAnimView
{
    public enum HallRoleAnim
    {
        Avatar,
        AvatarPet,
        PetInteractive,
        AvatarAIBuddy,
        AIBuddyInteractive,
    }

    private CharacterWrap characterWrap;
    private PlayerIdleBehaviour idleBehaviour;
    private PetWrap petWrap;
    private PetPlayerIdleBehaviour petIdleBehaviour;
    private UgcIdleBehaviour characterUgcIdleBehaviour;
    private UgcIdleBehaviour petUgcIdleBehaviour;
    private CharacterWrap npcWrap;
    private NpcPlayerIdleBehaviour npcIdleBehaviour;
    private UgcIdleBehaviour npcUgcIdleBehaviour;
    private Camera avatarCamera;
    private Vector3 defaultCameraLocalPos;
    private Tweener cameraTw;

    //动作在大厅显示太大，在这里设置
    private Dictionary<string,int> pose_camera_z = new Dictionary<string, int>
    {
        {"40200568",-512}
    };

    public AvatarAndPetAnimView( CharacterWrap cWrap, PetWrap pWrap, CharacterWrap nWrap, PlayerIdleBehaviour avatar,PetPlayerIdleBehaviour pet, NpcPlayerIdleBehaviour npc, Camera camera = null, Vector3? cameraDefaultLocalPos = null)
    {
        characterWrap = cWrap;
        petWrap = pWrap;
        npcWrap = nWrap;

        idleBehaviour = avatar;
        petIdleBehaviour = pet;
        npcIdleBehaviour = npc;

        avatarCamera = camera;
        if (avatarCamera != null)
            // 优先使用外部传入的默认位置；否则回退读取当前相机位置。
            // 大厅每次切换动作都会 new 一个新的 view，若直接读"当前"相机位置，会把上一次 SetEmoteView 移动后的位置当成默认值，导致无法还原。
            defaultCameraLocalPos = cameraDefaultLocalPos ?? avatarCamera.transform.localPosition;

        characterUgcIdleBehaviour = avatar.GetComponent<UgcIdleBehaviour>();
        petUgcIdleBehaviour = pet.GetComponent<UgcIdleBehaviour>();
        npcUgcIdleBehaviour = npc.GetComponent<UgcIdleBehaviour>();
    }


    public void OnAnimChange(bool isHall,HallRoleAnim animType, AccountUserInfo userInfo,AccountPetInfo petInfo, AIBuddyInfo buddyInfo)
    {
        switch (animType)
        {
            case HallRoleAnim.Avatar:
                characterWrap.CustomAvatar.transform.localPosition = Vector3.zero;
                break;
            case HallRoleAnim.AvatarPet:
                characterWrap.CustomAvatar.transform.localPosition = new Vector3(-0.3f, 0, 0);
                petWrap.CustomAvatar.transform.localPosition = new Vector3(0.3f, 0, 0);
                break;
            case HallRoleAnim.PetInteractive:
                if (petInfo.idleData.animResType == (int) AnimResType.PGC)
                {
                    characterWrap.CustomAvatar.transform.localPosition = isHall?new Vector3(0.3f, 0, 0): Vector3.zero;
                    petWrap.CustomAvatar.transform.localPosition = isHall?new Vector3(0.3f, 0, 0): Vector3.zero;
                }
                else
                {
                    characterWrap.CustomAvatar.transform.localPosition = new Vector3(-0.3f, 0, 0);
                    petWrap.CustomAvatar.transform.localPosition = new Vector3(-0.3f, 0, 0);
                }
                break;
            case HallRoleAnim.AvatarAIBuddy:
                characterWrap.CustomAvatar.transform.localPosition = new Vector3(-0.3f, 0, 0);
                npcWrap.CustomAvatar.transform.localPosition = new Vector3(0.3f, 0, 0);
                break;
            case HallRoleAnim.AIBuddyInteractive:
                if (buddyInfo.idleData.animResType == (int) AnimResType.PGC)
                {
                    characterWrap.CustomAvatar.transform.localPosition = isHall?new Vector3(0.3f, 0, 0): Vector3.zero;
                    npcWrap.CustomAvatar.transform.localPosition = isHall?new Vector3(0.3f, 0, 0): Vector3.zero;
                }
                else
                {
                    characterWrap.CustomAvatar.transform.localPosition = new Vector3(0.3f, 0, 0);
                    npcWrap.CustomAvatar.transform.localPosition = new Vector3(0.3f, 0, 0);
                }
                break;
        }

        var chaPoseModeData = DataTables.GetPoseModeConfig((int)UgcPoseSubType.Single);
        characterWrap.Avatar.transform.parent.localPosition = chaPoseModeData.EditPos[0];
        characterWrap.Avatar.transform.localPosition = chaPoseModeData.RoleDefPos[0];
        characterWrap.Avatar.SetActive(true);

        var poseModeData = DataTables.GetPoseModeConfig((int)UgcPoseSubType.PetSingle);
        petWrap.Avatar.transform.parent.localPosition = poseModeData.EditPos[0];
        petWrap.Avatar.transform.localPosition = poseModeData.RoleDefPos[0];
        petWrap.Avatar.SetActive(animType == HallRoleAnim.AvatarPet || animType == HallRoleAnim.PetInteractive);

        npcWrap.Avatar.transform.parent.localPosition = chaPoseModeData.EditPos[0];
        npcWrap.Avatar.transform.localPosition = chaPoseModeData.RoleDefPos[0];
        npcWrap.Avatar.SetActive(animType == HallRoleAnim.AvatarAIBuddy || animType == HallRoleAnim.AIBuddyInteractive);

        switch (animType)
        {
          case HallRoleAnim.Avatar:
              PlayCharacterIdleAnim(UgcAnimSubType.Single, userInfo);
              break;
          case HallRoleAnim.AvatarPet:
              PlayCharacterIdleAnim(UgcAnimSubType.Single,userInfo);
              PlayPetIdleAnim(HallRoleAnim.AvatarPet,petInfo);
              break;
          case HallRoleAnim.PetInteractive:
              PlayPetIdleAnim(HallRoleAnim.PetInteractive,petInfo);
              break;
          case HallRoleAnim.AvatarAIBuddy:
              PlayCharacterIdleAnim(UgcAnimSubType.Single,userInfo);
              PlayAIBuddyIdleAnim(HallRoleAnim.AvatarAIBuddy, buddyInfo);
              break;
          case HallRoleAnim.AIBuddyInteractive:
              PlayAIBuddyIdleAnim(HallRoleAnim.AIBuddyInteractive, buddyInfo);
              break;
        }
    }

    private void PlayCharacterIdleAnim(UgcAnimSubType animType,AccountUserInfo userInfo)
    {
        if (userInfo != null && userInfo.idleData != null)
        {
            bool isPgcAnim = userInfo.idleData.animResType == (int)AnimResType.PGC;
            idleBehaviour.ResetEmoteAnim();
            idleBehaviour.enabled = isPgcAnim;
            characterUgcIdleBehaviour.enabled = !isPgcAnim;
            var ikController = characterWrap.Avatar.GetComponent<AnimIKController>();
            ikController.ResetDefaultPosition();
            ikController.ChangeAnimResType(isPgcAnim ? AnimResType.PGC : AnimResType.UGC);
            if (userInfo.idleData.animResType == (int)AnimResType.PGC)
            {
                characterUgcIdleBehaviour.RemovePropIKs();
                idleBehaviour.SetData(userInfo.idleData);
            }
            else
            {
                characterUgcIdleBehaviour.SetData(animType,userInfo.idleData);
                characterUgcIdleBehaviour.PlayMain();
            }
            SetEmoteView(userInfo.idleData.mainIdle);
        }
    }


    private void PlayAIBuddyIdleAnim(HallRoleAnim hallAnimType,AIBuddyInfo buddyInfo) {

        var npcIkController = npcWrap.Avatar.GetComponent<AnimIKController>();
        var characterIkController = characterWrap.Avatar.GetComponent<AnimIKController>();
        npcIkController.ResetDefaultPosition();
        characterIkController.ResetDefaultPosition();
        if (buddyInfo != null && buddyInfo.idleData != null)
        {
            var chaAnimIk = characterWrap.Avatar.GetComponent<BaseAnimIK>();
            bool isPgcAnim = buddyInfo.idleData.animResType == (int)AnimResType.PGC;
            if (hallAnimType == HallRoleAnim.AIBuddyInteractive)
            {
                characterIkController.ChangeAnimResType(isPgcAnim ? AnimResType.PGC : AnimResType.UGC);
                characterIkController.enabled = false;
                npcIkController.Insert(0,chaAnimIk);
                SetEmoteView(buddyInfo.idleData.mainIdle);
            }
            else
            {
                SetEmoteView(buddyInfo.idleData.mainIdle);
                npcIkController.RemoveAnimIK(chaAnimIk);
            }
            npcIdleBehaviour.ResetEmoteAnim();
            npcIdleBehaviour.enabled = isPgcAnim;
            npcUgcIdleBehaviour.enabled = !isPgcAnim;
            npcIkController.ChangeAnimResType(isPgcAnim ? AnimResType.PGC : AnimResType.UGC);
            if (buddyInfo.idleData.animResType == (int)AnimResType.PGC)
            {
                npcUgcIdleBehaviour.RemovePropIKs();
                npcIdleBehaviour.SetData(buddyInfo.idleData);
            }
            else
            {
                var ugcAnimType = hallAnimType == HallRoleAnim.AvatarAIBuddy
                    ? UgcAnimSubType.Single
                    : UgcAnimSubType.Double;
                npcUgcIdleBehaviour.SetData(ugcAnimType,buddyInfo.idleData);
                npcUgcIdleBehaviour.PlayMain();

            }
        }
    }


    private void SetEmoteView(string emoteId = null) {
        if (avatarCamera == null) return;
        var targetPos = defaultCameraLocalPos;
        if (!string.IsNullOrEmpty(emoteId) && pose_camera_z.TryGetValue(emoteId, out var cameraZ))
        {
            targetPos.z = cameraZ;
        }
        cameraTw?.Kill();
        cameraTw = avatarCamera.transform.DOLocalMove(targetPos, 0.5f);
    }


    private void PlayPetIdleAnim(HallRoleAnim hallAnimType,AccountPetInfo petInfo)
    {
        if (petInfo != null && petInfo.idleData != null)
        {
            var petIkController = petWrap.Avatar.GetComponent<AnimIKController>();
            var characterIkController = characterWrap.Avatar.GetComponent<AnimIKController>();
            petIkController.ResetDefaultPosition();
            characterIkController.ResetDefaultPosition();
            var chaAnimIk = characterWrap.Avatar.GetComponent<BaseAnimIK>();
            bool isPgcAnim = petInfo.idleData.animResType == (int)AnimResType.PGC;
            if (hallAnimType == HallRoleAnim.PetInteractive)
            {
                characterIkController.ChangeAnimResType(isPgcAnim ? AnimResType.PGC : AnimResType.UGC);
                characterIkController.enabled = false;
                petIkController.Insert(0,chaAnimIk);
            }
            else
            {
                petIkController.RemoveAnimIK(chaAnimIk);
            }
            petIdleBehaviour.ResetEmoteAnim();
            petIdleBehaviour.enabled = isPgcAnim;
            petUgcIdleBehaviour.enabled = !isPgcAnim;
            petIkController.ChangeAnimResType(isPgcAnim ? AnimResType.PGC : AnimResType.UGC);

            if (petInfo.idleData.animResType == (int)AnimResType.PGC)
            {
                petUgcIdleBehaviour.RemovePropIKs();
                petIdleBehaviour.SetData(petInfo.idleData);
            }
            else
            {
                var ugcAnimType = hallAnimType == HallRoleAnim.AvatarPet
                    ? UgcAnimSubType.PetSingle
                    : UgcAnimSubType.PetWithPlayer;
                petUgcIdleBehaviour.SetData(ugcAnimType,petInfo.idleData);
                petUgcIdleBehaviour.PlayMain();
            }
        }
    }

    public HallRoleAnim GetHallAnimatoionType(AccountPetInfo petInfo, AIBuddyInfo buddyInfo)
    {
        HallRoleAnim animType = HallRoleAnim.Avatar;


        if (petInfo == null && buddyInfo == null)
        {
            return animType;
        }


        if (petInfo != null && petInfo.isHidden == 0) {
            // 显示宠物
            animType = HallRoleAnim.AvatarPet;
            //兼容自动添加宠物逻辑
            if (petInfo.idleData == null)
            {
                return animType;
            }
            if (petInfo.idleData.animResType == 0)
            {
                var emoData = Es.DataTables.GetEmoUIConfig(petInfo.idleData.mainIdle);
                if (emoData != null)
                {
                    var emoAniType = (EmoteType) emoData.emoAniType;
                    if (emoAniType == EmoteType.PetWithPlayer || emoAniType == EmoteType.PetWithPlayerLoop)
                    {
                        animType = HallRoleAnim.PetInteractive;
                    }
                }
            }
            else
            {
                animType = petInfo.idleData.personType == 0 ? HallRoleAnim.AvatarPet : HallRoleAnim.PetInteractive;
            }
        } else if (buddyInfo != null && buddyInfo.isHidden == 0) {
            // 显示AIBuddy
            animType = HallRoleAnim.AvatarAIBuddy;
            if (buddyInfo.idleData == null)
            {
                return animType;
            }
            if (buddyInfo.idleData.animResType == 0)
            {
                var emoData = Es.DataTables.GetEmoUIConfig(buddyInfo.idleData.mainIdle);
                if (emoData != null)
                {
                    var emoAniType = (EmoteType) emoData.emoAniType;
                    if (emoAniType == EmoteType.DoubleOnce || emoAniType == EmoteType.DoubleLoop)
                    {
                        animType = HallRoleAnim.AIBuddyInteractive;
                    }
                }
            }
            else
            {
                animType = buddyInfo.idleData.personType == 0 ? HallRoleAnim.AvatarAIBuddy : HallRoleAnim.AIBuddyInteractive;
            }
        }
        return animType;
    }
}
