using System;
using System.Collections;
using System.Collections.Generic;
using BUD.AnimPose;
using Es;
using Game.Avatar;
using Game.Pet;
using GameData.BaseInfo;
using GameData.PgcData;
using UnityEngine;

public class AnimIkPreview : MonoBehaviour
{
    public Camera Cam_PreCam;
    public Transform characterRoot;
    public AvatarCameraController dragUtil;
    
    private AnimIKController animationCtrlIK;
    private AnimIKController otherAnimationCtrlIK;
    private AnimIKController petAnimationCtrlIK;
    protected PoseModeConfig _poseModeConfig;
    protected GameObject rotCenter;

    private void OnDisable()
    {
        UgcAnimToneManager.Inst.StopPreviewTone();
        if (rotCenter != null)
        {
            GameObject.Destroy(rotCenter);
        }
    }

    public void StartPreviewAnim(AnimInfo animInfo)
    {
        this.gameObject.SetActive(true);
        _poseModeConfig = DataTables.GetPoseModeConfig((int)animInfo.animType);
        var emoSubType = (UgcPoseSubType)animInfo.animType;
        switch (emoSubType)
        {
            case UgcPoseSubType.Single:
                InitCharacterWrapper();
                animationCtrlIK.Play(animInfo, otherAnimationCtrlIK);
                break;
            case UgcPoseSubType.Double:
                InitCharacterWrapper();
                animationCtrlIK.Play(animInfo, otherAnimationCtrlIK);
                break;
            
            case UgcPoseSubType.PetSingle:
                InitPetWrapper();
                petAnimationCtrlIK.Play(animInfo, otherAnimationCtrlIK);
                break;
            case UgcPoseSubType.PetWithPlayer:
                InitPetWrapper();
                petAnimationCtrlIK.Play(animInfo, otherAnimationCtrlIK);
                break;
        }
    }
    
    public void StartPreviewPose(PoseInfo poseInfo)
    {
        this.gameObject.SetActive(true);
        _poseModeConfig = DataTables.GetPoseModeConfig((int)poseInfo.poseType);
        var emoSubType = (EmoteSubType)poseInfo.poseType;
        switch (emoSubType)
        {
            case EmoteSubType.Single:
                InitCharacterWrapper();
                animationCtrlIK.Pose(poseInfo, otherAnimationCtrlIK);
                break;
            
            case EmoteSubType.Double:
                InitCharacterWrapper();
                animationCtrlIK.Pose(poseInfo, otherAnimationCtrlIK);
                break;
            
            case EmoteSubType.PetSingle:
                InitPetWrapper();
                petAnimationCtrlIK.Pose(poseInfo, otherAnimationCtrlIK);
                break;
            
            case EmoteSubType.PetWithPlayer:
                InitPetWrapper();
                petAnimationCtrlIK.Pose(poseInfo, otherAnimationCtrlIK);
                break;
        }
    }
    
    private void InitCharacterWrapper() {
        var saveCharacterData = AccountDataManager.Inst.UserInfo.avatarInfo;
        var characterWrapper = AvatarController.Inst.CreateUIAvatarWithIKController(saveCharacterData, characterRoot);
        animationCtrlIK = characterWrapper.Avatar.GetComponent<AnimIKController>();
        var otherCharacterWrap = AvatarController.Inst.CreateUIAvatarWithIKController(AccountDataManager.Inst.UserInfo.otherAvatarInfo, characterRoot);
        otherAnimationCtrlIK = otherCharacterWrap.Avatar.GetComponent<AnimIKController>();
        otherCharacterWrap.Avatar.gameObject.SetActive(false);
        SetAnimNodeRotateCenter();
    }
    
    private void InitPetWrapper() {
        var savePetData = AccountDataManager.Inst.PetInfo.avatarInfo;
        var petWrapper = PetAvatarController.Inst.CreateUIAvatarWithIKController(savePetData, characterRoot);
        petAnimationCtrlIK = petWrapper.Avatar.GetComponent<AnimIKController>();
        var otherCharacterWrap = AvatarController.Inst.CreateUIAvatarWithIKController(AccountDataManager.Inst.UserInfo.avatarInfo, characterRoot);
        otherAnimationCtrlIK = otherCharacterWrap.Avatar.GetComponent<AnimIKController>();
        otherCharacterWrap.Avatar.gameObject.SetActive(false);
        SetAnimNodeRotateCenter();
    }
    
    protected void SetAnimNodeRotateCenter()
    {
        rotCenter = new GameObject("AnimNodeRotateCenter");
        rotCenter.transform.SetParent(characterRoot);
        rotCenter.transform.localPosition = new Vector3(_poseModeConfig.ShotCamPos.x, 0, 0);
        rotCenter.transform.SetParent(characterRoot.transform.parent);
        characterRoot.transform.SetParent(rotCenter.transform);
        
        dragUtil.MoveRoot = rotCenter.transform;
        dragUtil.RotateTarget = rotCenter.transform;
        
        rotCenter.transform.localPosition = new Vector3(_poseModeConfig.ShotCamPos.x, 0, _poseModeConfig.ShotCamPos.z);

        Cam_PreCam.transform.localPosition = new Vector3(_poseModeConfig.ShotCamPos.x,
            4, Cam_PreCam.transform.localPosition.z);
    }
}
