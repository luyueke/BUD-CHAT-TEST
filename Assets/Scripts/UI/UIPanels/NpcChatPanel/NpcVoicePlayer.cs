using UnityEngine;
using System;
using Basic.Utils;
using Game.Audio;
using NetCoreServer;
using Newtonsoft.Json;

public class NpcVoicePlayer : MonoBehaviour
{
    public enum PlayState
    {
        Idle,
        Loading,
        Playing,
        Paused
    }

    public float Volume
    {
        get => audioSource.volume;
        set => audioSource.volume = value;
    }

    public float ClipLength => Mathf.Max(0.5f, curVoiceType != SpeechRspV2.VoiceType.HumanSpeech || audioSource.clip == null ? curText.Length / 20f : audioSource.clip.length);

    [SerializeField]
    private PlayState state;
    public PlayState State { get => state; private set => state = value; }
    public Action<PlayState> OnStateChanged { get; set; }
    public Action OnVoiceLoaded { get; set; }

    private AudioSource audioSource;

    private string curVoiceId;
    private string curText;

    private SpeechRspV2.VoiceType curVoiceType;
    private float StopTime;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (!audioSource)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.Stop();
        State = PlayState.Idle;
    }
    #region 外部接口

    public float GetVoiceLength()
    {
        return ClipLength;
    }

    public void PlayText(string text, int textType = 0, Action onSucc = null, Action<string> onFail = null)
    {
    }
    

    public void Play()
    {
        ToState(PlayState.Playing);
        OnPlay(true);
        if (curVoiceType == SpeechRspV2.VoiceType.HumanSpeech)
        {
            audioSource.Play();
        }
        else
        {
            PlaySound();
        }
    }

    public void Pause()
    {
        ToState(PlayState.Paused);
        OnPlay(false);
        if (curVoiceType == SpeechRspV2.VoiceType.HumanSpeech)
        {
            audioSource.Pause();
        }
        else
        {
            StopSound();
        }
    }

    public void Resume()
    {
        ToState(PlayState.Playing);
        OnPlay(true);
        if (curVoiceType == SpeechRspV2.VoiceType.HumanSpeech)
        {
            audioSource.UnPause();
        }
        else
        {
            PlaySound();
        }
    }

    public void Stop()
    {
        ToState(PlayState.Idle);
        OnPlay(false);
        audioSource.Stop();
        StopSound();
    }

    public void Destroy()
    {
        OnStateChanged = null;
        OnVoiceLoaded = null;
        StopSound();
        if (gameObject != null)
        {
            Destroy(gameObject);
        }
    }

    public bool IsCached(string voiceId, string text)
    {
        return curVoiceId == voiceId && curText == text;
    }

    public void Release()
    {
        if (this == null || gameObject == null)
        {
            return;
        }
        if (audioSource.clip)
        {
            Destroy(audioSource.clip);
            audioSource.clip = null;
        }
    }

    #endregion

    #region 播放控制
    private void Update()
    {
        if (State == PlayState.Playing) //监控播放完毕
        {
            if (curVoiceType == SpeechRspV2.VoiceType.HumanSpeech)
            {
                if (!audioSource.isPlaying)
                {
                    ToState(PlayState.Idle);
                    OnPlay(false);
                }
            }
            else
            {
                if (Time.time >= StopTime)
                {
                    StopSound();
                    ToState(PlayState.Idle);
                    OnPlay(false);
                }
            }
        }
    }

    private void OnDestroy()
    {
        Stop();
        Release();
    }
    #endregion

    #region 播放流程
    private void PlayWithChache()
    {
        if (State == PlayState.Paused)
        {
            Resume();
        }
        else
        {
            Play();
        }
    }

    private void PlaySound()
    {
        StopSound();
        StopTime = Time.time + ClipLength;
        if (curVoiceType == SpeechRspV2.VoiceType.Walawala)
        {
            // AkSoundManager.Inst.PlayAttackSound("", "Play_StoryMode_NPC_Voice_Loop", "", CameraNodeUtils.Instance.MainCamera.gameObject);
        }
        if (curVoiceType == SpeechRspV2.VoiceType.Typing)
        {
            // AkSoundManager.Inst.PlayAttackSound("", "Play_Props_AIQuest_UI_Loading_Loop", "", CameraNodeUtils.Instance.MainCamera.gameObject);
        }
    }

    private void StopSound()
    {
        // AKSoundManager.Inst.PlayAttackSound("", "Stop_StoryMode_NPC_Voice_Loop", "", CameraNodeUtils.Instance.MainCamera.gameObject);
        // AKSoundManager.Inst.PlayAttackSound("", "Stop_Props_AIQuest_UI_Loading_Loop", "", CameraNodeUtils.Instance.MainCamera.gameObject);
    }

    #endregion

    #region Helpers
    private void ToState(PlayState state)
    {
        State = state;
        OnStateChanged?.Invoke(state);
    }

    private void OnPlay(bool isOn)
    {
        if (isOn)
        {
            // BgmVolumnShifter.SetVolumnWithRatio(0.2f);
            // BgmVolumnShifter.SetSfxVolumnWithRatio(0.2f);
        }
        else
        {
            // BgmVolumnShifter.Restore();
            // BgmVolumnShifter.RestoreSfx();
        }
    }
    #endregion

    public static NpcVoicePlayer Create(string name)
    {
        GameObject go = new GameObject(name);
        NpcVoicePlayer player = go.AddComponent<NpcVoicePlayer>();
        return player;
    }
}

public class TextToSpeechV2
{
    public string voiceId;  // 语音Id
    public string text;   // 文案
    public int textType;  // 0 默认  1 静态文案（intro/旁白）
    public int gameType;  // ai游戏类型
    public string mapId;     // 游玩模式地图Id
}

public class SpeechRspV2
{
    public enum VoiceType
    {
        Walawala,     // 卡通音
        HumanSpeech,  // 人声
        Mute,         // 静音
        Typing,       // 打字机音
    }
}
