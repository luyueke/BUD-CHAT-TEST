using System;
using System.Collections;
using System.Collections.Generic;
using Es;
using Game.Audio;
using Game.Avatar;
using GameData;
using GameData.PgcData;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;


[Serializable]
public class SpecialAnimToggle {
    public SpecialAnim specialAnim;
    public Toggle toggle;
}


public class GashaponPreviewAnimView : MonoBehaviour
{
    [SerializeField] private Image outlineImage;


    [SerializeField] private List<SpecialAnimToggle> specialAnimToggles;


    private Color selectedColor;
    private Color defaultColor;

    private PlayerAnimationCtrl animationCtrl;
    private AvatarCameraController cameraController;
    private SpecialAnim lastAnimation = SpecialAnim.Idle;


    private void Awake() {
        foreach (var specialAnimToggle in specialAnimToggles) {
            specialAnimToggle.toggle.onValueChanged.AddListener((isOn) => {
                if (isOn) {
                    AkSoundManager.Inst.PlayUIEffectSound(UISoundType.UI_ShiftItems_B2);
                    specialAnimToggle.toggle.GetComponent<Image>().color = selectedColor;
                } else {
                    specialAnimToggle.toggle.GetComponent<Image>().color = defaultColor;
                }
                OnToggleSelect(isOn, specialAnimToggle.specialAnim);
            });
        }
    }


    private void OnEnable() {
        OnToggleSelect(true, lastAnimation);
    }


    public void SetWrap(CharacterWrap wrap) {
        animationCtrl = wrap.Avatar.GetComponent<PlayerAnimationCtrl>();
    }

    public void SetCameraController(AvatarCameraController controller) {
        cameraController = controller;
    }


    public void SetColor(Color outlineColor, Color selectColor, Color normalColor) {
        outlineImage.color = outlineColor;
        defaultColor = normalColor;
        selectedColor = selectColor;
        foreach (var specialAnimToggle in specialAnimToggles) {
            if (specialAnimToggle.toggle.isOn) {
                specialAnimToggle.toggle.GetComponent<Image>().color = selectedColor;
            } else {
                specialAnimToggle.toggle.GetComponent<Image>().color = defaultColor;
            }
        }
    }

    private void OnToggleSelect(bool isOn, SpecialAnim anim) {
        if (!isOn || string.IsNullOrEmpty(animationCtrl.specialAnimPgcId)) {
            return;
        }

        animationCtrl.CheckAndOverrideSpecialAnim();

        var specialSkinConfig = DataTables.GetSpecialSkinConfig(animationCtrl.specialAnimPgcId);
        //var resConfig = DataTables.GetGameResData(animationCtrl.specialAnimPgcId);
        //AvatarSubType subType = (AvatarSubType)resConfig.SubType;
        //BodyNode bodyNode = BodyNode.BackDeckNode;
        //if (subType == AvatarSubType.Hats) {
        //    bodyNode = BodyNode.HatNode;
        //}
        Animator pgcAnimator = animationCtrl.specialAnimRoot?.GetComponentInChildren<Animator>(true);
        var switchEventName = "";
        var pgcAnimName = "";
        switch (anim) {
            case SpecialAnim.Idle:
                animationCtrl.SetPlayerState(PlayerState.Default);
                animationCtrl.SetPlayerAniState(PlayerAniState.Idle);
                switchEventName = specialSkinConfig.previewIdleAnimInfo.audio;
                // 本体 idle 用 preview，特效也跟随 preview（含异步/重建）；pgcAnimName 置空避免下方 Play 把特效打回 base idle
                animationCtrl.ApplyUIPreviewIdleOverride();
                pgcAnimName = "";
                break;
            case SpecialAnim.Run:
                animationCtrl.SetPlayerState(PlayerState.Default);
                animationCtrl.SetPressJoystickTime(0.0f);
                animationCtrl.SetPlayerAniState(PlayerAniState.Run);
                switchEventName = specialSkinConfig.previewRunAnimInfo.audio;
                pgcAnimName = "run";
                break;
            case SpecialAnim.FastRun:
                animationCtrl.SetPlayerState(PlayerState.Default);
                animationCtrl.SetPressJoystickTime(2.5f);
                animationCtrl.SetPlayerAniState(PlayerAniState.Run);
                switchEventName = specialSkinConfig.previewFastRunAnimInfo.audio;
                pgcAnimName = "fast_run";
                break;
            case SpecialAnim.Jump:
                animationCtrl.SetPlayerState(PlayerState.Leisure);
                animationCtrl.SetPlayerAniState(PlayerAniState.Jump);
                switchEventName = specialSkinConfig.previewJumpAnimInfo.audio;
                pgcAnimName = "preview_jump";
                break;
        }
        if (pgcAnimator != null && !string.IsNullOrEmpty(pgcAnimName)) {
            pgcAnimator.Play(pgcAnimName);
        }
        AkSoundManager.Inst.StopAll(animationCtrl.gameObject);
        if (!string.IsNullOrEmpty(switchEventName)) {
            AkSoundManager.Inst.StopAll(animationCtrl.gameObject);
            AkSoundManager.Inst.PlaySound($"Emote_Group_{specialSkinConfig.SoundVersion}", switchEventName, $"Play_Emote_{specialSkinConfig.SoundVersion}_1P", animationCtrl.gameObject);
        }

        lastAnimation = anim;
    }

}
