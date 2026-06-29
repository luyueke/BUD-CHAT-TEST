using System.Collections.Generic;
using Game.Audio;
using Game.Avatar;
using Game.MapSetting;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Author:Shaocheng
/// Description: 通关结算panel
/// Date: 2023-8-18 14:27:07
/// </summary>
public class PassLevelPanel : BasePanel<PassLevelPanel>
{
    [Header("获胜/失败")]
    public Transform winTitle;

    public Transform lostTitle;
    public Transform timeOutTitle;
    public List<Image> bgs;
    public List<Image> bgs2;
    public Color failColor;
    public Color failColor2;
    public GameObject timeRoot;

    [Header("操作按钮")]
    public Button negativeBtn;

    public Button positiveBtn;

    [Header("预览")]
    public Transform characterRoot;

    [Header("动画&特效")]
    public Animator animator;

    public Transform winEffectTrans;

    [Header("奖励")]
    public Text expText;

    public Text coinText;
    public Text badgeText;
    public Text timeText;

    [Header("光照")]
    public Color ambientSkyColor;

    public Color ambientEquatorColor;
    public Color ambientGroundColor;
    public Color dirLight;
    public float reflectionIntensity;
    public float lightIntensity;

    private Color _ambientSkyColor;
    private Color _ambientEquatorColor;
    private Color _ambientGroundColor;
    private Color _dirLightColor;
    private float _reflectionIntensity;
    private float _lightIntensity;
    private LightShadows _shadow;

    private bool _isWin = false;
    private CharacterWrap characterWrap;
    private BudTimer _buttonLockTimer;
    private bool isButtonCanClick = true;
    private PlayerAnimationCtrl _playerAnimationCtrl;

    private bool isPlayAniOver = false;
    private bool isFirstWin = false;

    protected override void Awake()
    {
        base.Awake();
        SetEnvColor();
    }


    public override void OnCreate()
    {
        base.OnCreate();
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        UIManager.Inst.ClosePanel(PanelId.CameraModePanel);
        _isWin = (bool)args[0];

        ChangeBgColor();
        ShowCharacter();

        SetWinResultShow();
        ShowPanelAnimation();
        PlaySound();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }

    #region 人物展示

    private void ShowCharacter()
    {
        CharacterData avatarInfo = AccountDataManager.Inst.UserInfo.avatarInfo;
        characterWrap = AvatarController.Inst.CreateUIAvatar(avatarInfo);
        characterWrap.SetParent(characterRoot, true);

        _playerAnimationCtrl = characterWrap.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
    }

    private void ShowPanelAnimation()
    {
        animator.Play("Clearthespecial_RoleNode");
        // animator.enabled = true;
        if (_isWin)
        {
            // animator.Play("youwon_ani2");
            //放礼花
            _playerAnimationCtrl.PlaySingleEmoteForUICharacter("40100061", isPlaySound: false);
        }
        else
        {
            // animator.Play("time_ani2");
            //郁闷
            _playerAnimationCtrl.PlaySingleEmoteForUICharacter("40100013", () =>
            {
                _playerAnimationCtrl.PlaySingleEmoteForUICharacter("40100016", () =>
                {

                }, isPlaySound: false); //急跺脚
            }, isPlaySound: false);
        }

        /*TimerManager.Inst.Stop(_buttonLockTimer);
        _buttonLockTimer = TimerManager.Inst.RunOnce(nameof(_buttonLockTimer), 3.0f, () =>
        {
            isButtonCanClick = true;
        });*/
    }

    #endregion

    #region UI控制

    private void SetWinResultShow()
    {
        winTitle.gameObject.SetActive(_isWin);
        lostTitle.gameObject.SetActive(!_isWin && PassLevelManager.Inst.PassLevelData.FailedType == (int)PassLevelFailedType.HP);
        timeOutTitle.gameObject.SetActive(!_isWin && PassLevelManager.Inst.PassLevelData.FailedType == (int)PassLevelFailedType.Timout);

        // timeText.text = CostTimeToStr(PassLevelManager.Inst.GetCurPassLevelTime());
        winEffectTrans.gameObject.SetActive(_isWin);
    }

    private void ChangeBgColor()
    {
        if (_isWin) return;
        for (int i = 0, C = bgs.Count; i < C; i++)
        {
            bgs[i].color = failColor;
        }

        for (int i = 0, C = bgs2.Count; i < C; i++)
        {
            bgs2[i].color = failColor2;
        }

        // timeRoot.SetActive(false);
    }

    private string CostTimeToStr(int seconds)
    {
        var curMin = seconds / 60;
        var curSecs = seconds % 60;
        return $"{curMin} : {curSecs}";
    }

    #endregion

    #region 按钮设置

    public class ButtonSetting
    {
        public string BtnText;
        public UnityAction ClickAction;
        public UButtonBaseConfig CButtonConfig;
    }

    public class UButtonBaseConfig
    {
        public UISoundType UISoundType;
    }

    public void SetButtons(ButtonSetting leftBtnSet, ButtonSetting rightBtnSet)
    {
        if (leftBtnSet != null)
        {
            negativeBtn.onClick.AddListener(() =>
            {
                if (isButtonCanClick)
                {
                    ResetEnvColor();
                    leftBtnSet.ClickAction.Invoke();
                }
            });
            var text = negativeBtn.GetComponentInChildren<Text>();
            text.SetLocalText(leftBtnSet.BtnText);
            negativeBtn.gameObject.SetActive(true);

            if (leftBtnSet.CButtonConfig != null)
            {
                var uBtn = negativeBtn.GetComponent<CButton>();
                if (uBtn) uBtn.SoundType = leftBtnSet.CButtonConfig.UISoundType;
            }
        }
        else
        {
            negativeBtn.gameObject.SetActive(false);
        }

        if (rightBtnSet != null)
        {
            positiveBtn.onClick.AddListener(() =>
            {
                if (isButtonCanClick)
                {
                    ResetEnvColor();
                    rightBtnSet.ClickAction.Invoke();
                }
            });
            var text = positiveBtn.GetComponentInChildren<Text>();
            text.SetLocalText(rightBtnSet.BtnText);
            positiveBtn.gameObject.SetActive(true);

            if (rightBtnSet.CButtonConfig != null)
            {
                var uBtn = positiveBtn.GetComponent<CButton>();
                if (uBtn) uBtn.SoundType = rightBtnSet.CButtonConfig.UISoundType;
            }
        }
        else
        {
            positiveBtn.gameObject.SetActive(false);
        }
    }

    #endregion

    #region 音效

    private void PlaySound()
    {
        var switchName = string.Empty;
        if (_isWin)
        {
            switchName = "GameResult_YouWin";
        }
        else
        {
            switchName = (PassLevelFailedType)PassLevelManager.Inst.PassLevelData.FailedType switch
            {
                PassLevelFailedType.Timout => "GameResult_TimesUp",
                PassLevelFailedType.HP => "GameResult_GameOver",
                _ => "GameResult_GameOver"
            };
        }

        if (!string.IsNullOrEmpty(switchName))
        {
            AkSoundManager.Inst.PlayInteractable3DSound(switchName, AvatarController.Inst.SelfController.gameObject);
        }
    }

    #endregion

    #region 光照控制

    public void SetEnvColor()
    {
        _ambientSkyColor = RenderSettings.ambientSkyColor;
        _ambientEquatorColor = RenderSettings.ambientEquatorColor;
        _ambientGroundColor = RenderSettings.ambientGroundColor;
        _reflectionIntensity = RenderSettings.reflectionIntensity;

        RenderSettings.ambientSkyColor = ambientSkyColor;
        RenderSettings.ambientEquatorColor = ambientEquatorColor;
        RenderSettings.ambientGroundColor = ambientGroundColor;
        RenderSettings.reflectionIntensity = reflectionIntensity;


        var curLight = DirLightManager.Inst.GetGlobalDirLight();
        _dirLightColor = curLight.color;
        _lightIntensity = curLight.intensity;
        _shadow = curLight.shadows;

        curLight.color = dirLight;
        curLight.intensity = lightIntensity;
        curLight.shadows = LightShadows.None;
    }

    public void ResetEnvColor()
    {
        RenderSettings.ambientSkyColor = _ambientSkyColor;
        RenderSettings.ambientEquatorColor = _ambientEquatorColor;
        RenderSettings.ambientGroundColor = _ambientGroundColor;
        RenderSettings.reflectionIntensity = _reflectionIntensity;

        var curLight = DirLightManager.Inst.GetGlobalDirLight();
        curLight.color = _dirLightColor;
        curLight.intensity = _lightIntensity;
        curLight.shadows = _shadow;
    }

    #endregion
}
