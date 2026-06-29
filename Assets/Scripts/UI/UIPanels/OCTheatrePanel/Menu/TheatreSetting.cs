using System;
using Game.GameSetting;
using UnityEngine;
using UnityEngine.UI;
public class TheatreSetting : MonoBehaviour{
    [SerializeField] private Slider bgSoundSlider;
    [SerializeField] private Slider otherSoundSlider;
    [SerializeField] private Button bgSoundSwitchBtn;
    [SerializeField] private Button otherSoundSwitchBtn;
    [SerializeField] private Button closeBtn;
    [SerializeField] private Toggle typingToggle;
    [SerializeField] private Sprite openSound;
    [SerializeField] private Sprite closeSound;
    [SerializeField] private Text bgSoundText;
    [SerializeField] private Text otherSoundText;

    // TTS disabled
    // [Header("TTS 语音朗读")]
    // [SerializeField] private Toggle ttsToggle;
    // [SerializeField] private Slider ttsVolumeSlider;
    // [SerializeField] private Text ttsVolumeText;
    // [SerializeField] private Button ttsSoundSwitchBtn;


    private Action<bool> TypingToggleChanged;
    private Action<bool> BgSoundToggleChanged;

    private bool isOpenBGSound = true;
    private bool isOpenOtherSound = true;
    private bool isOpenTyping = true;
    // private bool _isOpenTtsSound = true;     // TTS disabled
    // private float _storedTtsVolume = 1f;     // TTS disabled

    private float systemBgVolume;
    private float systemSFXVolume;
    private float currentBgVolume;
    private float currentSFXVolume;
    private float storedBgVolume;
    private float storedSFXVolume;

    private AudioSource bgAudioSource;
    private AudioSource dumbingAudioSource;
    private AudioSource audioAudioSource;
    // private TheatreTtsPlayer _ttsPlayer;     // TTS disabled


    public void Init(Action<bool> onTypingToggleChanged, Action<bool> onBgSoundToggleChanged){
        bgSoundSlider.value = GlobalSettingManager.Inst.GetBgmVolume() / 100f;
        otherSoundSlider.value = GlobalSettingManager.Inst.GetSoundEffectVolume() / 100f;
        typingToggle.isOn = isOpenTyping;
        bgSoundSwitchBtn.onClick.AddListener(OnBgSoundSwitchBtnClick);
        otherSoundSwitchBtn.onClick.AddListener(OnOtherSoundSwitchBtnClick);
        typingToggle.onValueChanged.AddListener(OnTypingToggleValueChanged);
        closeBtn.onClick.AddListener(OnCloseBtnClick);
        bgSoundSlider.onValueChanged.AddListener(OnBgSliderChanged);
        otherSoundSlider.onValueChanged.AddListener(OnOtherSoundSliderChanged);
        typingToggle.isOn = isOpenTyping;

        systemBgVolume = GlobalSettingManager.Inst.GetBgmVolume();
        systemSFXVolume = GlobalSettingManager.Inst.GetSoundEffectVolume();
        currentBgVolume = systemBgVolume / 100f;
        currentSFXVolume = systemSFXVolume / 100f;
        storedBgVolume = currentBgVolume;
        storedSFXVolume = currentSFXVolume;
        TypingToggleChanged = onTypingToggleChanged;
        BgSoundToggleChanged = onBgSoundToggleChanged;

        bgSoundText.text = $"{(int)(currentBgVolume * 100)}";
        otherSoundText.text = $"{(int)(currentSFXVolume * 100)}";
        isOpenBGSound = currentBgVolume > 0;
        isOpenOtherSound = currentSFXVolume > 0;
        bgSoundSwitchBtn.image.sprite = isOpenBGSound ? openSound : closeSound;
        otherSoundSwitchBtn.image.sprite = isOpenOtherSound ? openSound : closeSound;

    }

    public void SetMusicRoot(GameObject musicRoot)
    {
        if(musicRoot != null)
        {   
            var bgNode = GameObjectEx.FindChildByName(musicRoot.transform, "TheatreBgMusicNode");
            var dubbingNode = GameObjectEx.FindChildByName(musicRoot.transform, "TheatreDubbingNode");
            var audioNode = GameObjectEx.FindChildByName(musicRoot.transform, "TheatreAudioNode");
            if(bgNode != null)
                bgAudioSource = bgNode.GetComponentInChildren<AudioSource>(true);
            if(dubbingNode != null)
                dumbingAudioSource = dubbingNode.GetComponentInChildren<AudioSource>(true);
            if(audioNode != null)
                audioAudioSource = audioNode.GetComponentInChildren<AudioSource>(true);   
        }
        
    }

    // TTS disabled
    // public void SetTtsPlayer(TheatreTtsPlayer player)
    // {
    //     _ttsPlayer = player;
    //     if (_ttsPlayer == null) return;
    //     if (ttsToggle != null)
    //     {
    //         ttsToggle.isOn = _ttsPlayer.IsEnabled;
    //         ttsToggle.onValueChanged.RemoveListener(OnTtsToggleChanged);
    //         ttsToggle.onValueChanged.AddListener(OnTtsToggleChanged);
    //     }
    //     if (ttsVolumeSlider != null)
    //     {
    //         ttsVolumeSlider.value = _ttsPlayer.Volume;
    //         ttsVolumeSlider.onValueChanged.RemoveListener(OnTtsVolumeChanged);
    //         ttsVolumeSlider.onValueChanged.AddListener(OnTtsVolumeChanged);
    //         if (ttsVolumeText != null)
    //             ttsVolumeText.text = $"{(int)(_ttsPlayer.Volume * 100)}";
    //     }
    //     if (ttsSoundSwitchBtn != null)
    //     {
    //         _storedTtsVolume = _ttsPlayer.Volume > 0 ? _ttsPlayer.Volume : 1f;
    //         _isOpenTtsSound = _ttsPlayer.Volume > 0;
    //         ttsSoundSwitchBtn.onClick.RemoveAllListeners();
    //         ttsSoundSwitchBtn.onClick.AddListener(OnTtsSoundSwitchBtnClick);
    //         ttsSoundSwitchBtn.image.sprite = _isOpenTtsSound ? openSound : closeSound;
    //     }
    // }

    public void OnRelease()
    {
        GlobalSettingManager.Inst.BgmChange(systemBgVolume);
        GlobalSettingManager.Inst.SoundEffectChange(systemSFXVolume);
    }

    public void SetBGVolume(bool isMute)
    {
        if (isMute)
        {
            storedBgVolume = currentBgVolume;
            if(bgAudioSource != null) bgAudioSource.volume = 0;
        }
        else
        {
            currentBgVolume = storedBgVolume;
            if(bgAudioSource != null) bgAudioSource.volume = storedBgVolume;
        }
        bgSoundSwitchBtn.image.sprite = isMute ? closeSound : openSound;
        bgSoundText.text = $"{(int)(currentBgVolume * 100)}";
    }

    private void OnBgSliderChanged(float value){
        currentBgVolume = value;
        if(currentBgVolume > 0 && !isOpenBGSound){
            isOpenBGSound = true;
            bgSoundSwitchBtn.image.sprite = openSound;
            BgSoundToggleChanged?.Invoke(true);

        }else if(currentBgVolume == 0 && isOpenBGSound){
            isOpenBGSound = false;
            bgSoundSwitchBtn.image.sprite = closeSound;
            BgSoundToggleChanged?.Invoke(false);

        }
        if(bgAudioSource != null) bgAudioSource.volume = currentBgVolume;
        bgSoundText.text = $"{(int)(currentBgVolume * 100)}";
    }

    private void OnOtherSoundSliderChanged(float value){
        currentSFXVolume = value;
        if(currentSFXVolume > 0 && !isOpenOtherSound){
            isOpenOtherSound = true;
            otherSoundSwitchBtn.image.sprite = openSound;
        }else if(currentSFXVolume == 0 && isOpenOtherSound){
            isOpenOtherSound = false;
            otherSoundSwitchBtn.image.sprite = closeSound;
        }
        if(audioAudioSource != null) audioAudioSource.volume = currentSFXVolume;
        if(dumbingAudioSource != null) dumbingAudioSource.volume = currentSFXVolume;
        otherSoundText.text = $"{(int)(currentSFXVolume * 100)}";
    }

    private void OnBgSoundSwitchBtnClick(){
        isOpenBGSound = !isOpenBGSound;
        if (!isOpenBGSound)
        {
            storedBgVolume = currentBgVolume;
            bgSoundSlider.value = 0;
            if(bgAudioSource != null) bgAudioSource.volume = 0;
            BgSoundToggleChanged?.Invoke(false);
        }
        else
        {
            currentBgVolume = storedBgVolume;
            if(bgAudioSource != null) bgAudioSource.volume = storedBgVolume;
            bgSoundSlider.value = currentBgVolume;
            BgSoundToggleChanged?.Invoke(true);
        }
        bgSoundSwitchBtn.image.sprite = isOpenBGSound ? openSound : closeSound;
        bgSoundText.text = $"{(int)(currentBgVolume * 100)}";
    }

    private void OnOtherSoundSwitchBtnClick(){
        isOpenOtherSound = !isOpenOtherSound;
        if (!isOpenOtherSound)
        {
            storedSFXVolume = currentSFXVolume;
            otherSoundSlider.value = 0;
            if(audioAudioSource != null) audioAudioSource.volume = 0;
            if(dumbingAudioSource != null) dumbingAudioSource.volume = 0;
        }
        else
        {
            currentSFXVolume = storedSFXVolume;
            if(audioAudioSource != null) audioAudioSource.volume = storedSFXVolume;
            if(dumbingAudioSource != null) dumbingAudioSource.volume = storedSFXVolume;
            otherSoundSlider.value = currentSFXVolume;
        }
        otherSoundSwitchBtn.image.sprite = isOpenOtherSound ? openSound : closeSound;
        otherSoundText.text = $"{(int)(currentSFXVolume * 100)}";
    }
    private void OnTypingToggleValueChanged(bool isOn){
        isOpenTyping = isOn;
        typingToggle.isOn = isOpenTyping;
        TypingToggleChanged?.Invoke(isOpenTyping);

    }

    private void OnCloseBtnClick(){
        gameObject.SetActive(false);
    }

    // TTS disabled
    // private void OnTtsToggleChanged(bool isOn)
    // {
    //     if (_ttsPlayer != null) _ttsPlayer.IsEnabled = isOn;
    // }

    // private void OnTtsVolumeChanged(float value)
    // {
    //     if (_ttsPlayer != null) _ttsPlayer.Volume = value;
    //     if (ttsVolumeText != null) ttsVolumeText.text = $"{(int)(value * 100)}";
    //     if (value > 0 && !_isOpenTtsSound)
    //     {
    //         _isOpenTtsSound = true;
    //         if (ttsSoundSwitchBtn != null) ttsSoundSwitchBtn.image.sprite = openSound;
    //     }
    //     else if (value == 0 && _isOpenTtsSound)
    //     {
    //         _isOpenTtsSound = false;
    //         if (ttsSoundSwitchBtn != null) ttsSoundSwitchBtn.image.sprite = closeSound;
    //     }
    // }

    // private void OnTtsSoundSwitchBtnClick()
    // {
    //     _isOpenTtsSound = !_isOpenTtsSound;
    //     if (!_isOpenTtsSound)
    //     {
    //         _storedTtsVolume = _ttsPlayer?.Volume ?? 1f;
    //         if (ttsVolumeSlider != null) ttsVolumeSlider.value = 0;
    //     }
    //     else
    //     {
    //         if (ttsVolumeSlider != null) ttsVolumeSlider.value = _storedTtsVolume > 0 ? _storedTtsVolume : 1f;
    //     }
    //     if (ttsSoundSwitchBtn != null) ttsSoundSwitchBtn.image.sprite = _isOpenTtsSound ? openSound : closeSound;
    // }
}