using System;
using Game.Audio;
using Pb.Theatre;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Section BGM 配置组件（URL 上传模式）。
/// triggerTime:    0=段落开始，-1=段落结束，N>0=第N句对话开始播放
/// triggerTimeEnd: 0=不主动停止，N>0=第N句对话结束时停止
/// triggerType:    0=对话开始前，1=打字完成后（仅影响开始时机）
/// isLoop:         0=不循环，1=循环
/// </summary>
public class TheatreEditorMusicInput : MonoBehaviour
{
    [SerializeField] private Button pickBtn;
    [SerializeField] private Text text;
    [SerializeField] private GameObject settingOptionBoard;
    [SerializeField] private Button changeBtn;
    [SerializeField] private Button deleteBtn;

    // 开始触发时机
    [SerializeField] private USwitchToggle triggerTimeStartToggle;
    [SerializeField] private USwitchToggle triggerTimeEndToggle;
    [SerializeField] private Button triggerTimeBtn;
    [SerializeField] private Text triggerTimeText;
    [SerializeField] private GameObject triggerTimeBoard;
    [SerializeField] private TheatreEditorTriggerInput triggerInput;

    // 结束触发时机
    [SerializeField] private Button triggerTimeEndBtn;
    [SerializeField] private Text triggerTimeEndText;
    [SerializeField] private GameObject triggerTimeEndBoard;
    [SerializeField] private TheatreEditorTriggerInput triggerEndInput;

    // 循环
    [SerializeField] private USwitchToggle triggerNotLoopToggle;
    [SerializeField] private USwitchToggle triggerLoopToggle;

    // URL 上传
    [SerializeField] private TheatreEditorMusicUploader musicUploader;

    private POCTheatreSection currentSection;
    private bool initialized;
    private bool _refreshing;
    private bool _isPreviewing;

    public Action<POCTheatreAudio> onAudioUpdated;
    public Action onBoardOpen;
    public Action onBoardClose;

    public void Init()
    {
        if (initialized) return;
        initialized = true;

        triggerTimeStartToggle?.Init();
        triggerTimeEndToggle?.Init();
        triggerNotLoopToggle?.Init();
        triggerLoopToggle?.Init();

        pickBtn?.onClick.RemoveAllListeners();
        pickBtn?.onClick.AddListener(OnPickBtnClicked);

        changeBtn?.onClick.RemoveAllListeners();
        changeBtn?.onClick.AddListener(OnChangeBtnClicked);

        deleteBtn?.onClick.RemoveAllListeners();
        deleteBtn?.onClick.AddListener(OnDeleteBtnClicked);

        triggerTimeStartToggle?.onValueChanged.AddListener(isOn =>
        {
            if (!_refreshing && isOn) ApplyTriggerType(0);
        });
        triggerTimeEndToggle?.onValueChanged.AddListener(isOn =>
        {
            if (!_refreshing && isOn) ApplyTriggerType(1);
        });
        triggerNotLoopToggle?.onValueChanged.AddListener(isOn =>
        {
            if (!_refreshing && isOn) ApplyLoop(0);
        });
        triggerLoopToggle?.onValueChanged.AddListener(isOn =>
        {
            if (!_refreshing && isOn) ApplyLoop(1);
        });

        // 开始时机选择面板
        triggerTimeBtn?.onClick.RemoveAllListeners();
        triggerTimeBtn?.onClick.AddListener(() =>
        {
            bool show = !(triggerTimeBoard?.activeSelf ?? false);
            triggerTimeBoard?.SetActive(show);
            if (show)
            {
                onBoardOpen?.Invoke();
                if (currentSection != null)
                    triggerInput?.Refresh(currentSection, OnStartTriggerTimeSelected);
            }
        });

        // 结束时机选择面板
        triggerTimeEndBtn?.onClick.RemoveAllListeners();
        triggerTimeEndBtn?.onClick.AddListener(() =>
        {
            bool show = !(triggerTimeEndBoard?.activeSelf ?? false);
            triggerTimeEndBoard?.SetActive(show);
            if (show)
            {
                onBoardOpen?.Invoke();
                if (currentSection != null)
                    triggerEndInput?.Refresh(currentSection, OnEndTriggerTimeSelected);
            }
        });

        settingOptionBoard?.SetActive(false);
        triggerTimeBoard?.SetActive(false);
        triggerTimeEndBoard?.SetActive(false);

        musicUploader?.Init(url =>
        {
            var audio = BuildCurrentAudio();
            audio.AudioUrl = url ?? "";
            audio.Type = 2;
            onAudioUpdated?.Invoke(audio);
            RefreshDisplay(audio);
        });
    }

    public void Refresh(POCTheatreSection section)
    {
        StopPreview();
        currentSection = section;
        if (section == null) return;

        RefreshDisplay(section.Audio);
        triggerTimeBoard?.SetActive(false);
        triggerTimeEndBoard?.SetActive(false);
    }

    private void RefreshDisplay(POCTheatreAudio audio)
    {
        _refreshing = true;
        bool hasAudio = audio != null && !string.IsNullOrEmpty(audio.AudioUrl);

        if (text != null)
            text.text = hasAudio ? "已添加音频" : "上传音频";

        settingOptionBoard?.SetActive(false);

        musicUploader?.SetDisplayUrl(audio?.AudioUrl ?? "");

        int triggerType = audio?.TriggerType ?? 0;
        if (triggerTimeStartToggle != null) triggerTimeStartToggle.isOn = triggerType == 0;
        if (triggerTimeEndToggle != null) triggerTimeEndToggle.isOn = triggerType == 1;

        int triggerTime = audio?.TriggerTime ?? 0;
        if (triggerTimeText != null)
            triggerTimeText.text = GetDialogueText(triggerTime);

        int triggerTimeEnd = audio?.TriggerTimeEnd ?? 0;
        if (triggerTimeEndText != null)
            triggerTimeEndText.text = GetDialogueText(triggerTimeEnd);

        int isLoop = audio?.IsLoop ?? 0;
        if (triggerNotLoopToggle != null) triggerNotLoopToggle.isOn = isLoop == 0;
        if (triggerLoopToggle != null) triggerLoopToggle.isOn = isLoop == 1;
        _refreshing = false;
    }

    private void OnPickBtnClicked()
    {
        var audio = currentSection?.Audio;
        bool hasAudio = audio != null && !string.IsNullOrEmpty(audio.AudioUrl);
        if (hasAudio)
        {
            TogglePreview(audio.AudioUrl);
            settingOptionBoard?.SetActive(true);
            onBoardOpen?.Invoke();
        }
        else
        {
            musicUploader?.gameObject.SetActive(true);
        }
    }

    private void TogglePreview(string url)
    {
        if (_isPreviewing)
        {
            StopPreview();
        }
        else
        {
            AkSoundManager.Inst.StopUGCAudio(gameObject);
            AkSoundManager.Inst.PlayUGCAudioByUrl(url, false, gameObject);
            _isPreviewing = true;
        }
    }

    private void StopPreview()
    {
        if (!_isPreviewing) return;
        AkSoundManager.Inst.StopUGCAudio(gameObject);
        _isPreviewing = false;
    }

    private void OnChangeBtnClicked()
    {
        settingOptionBoard?.SetActive(false);
        musicUploader?.gameObject.SetActive(true);
    }

    private void OnDeleteBtnClicked()
    {
        StopPreview();
        settingOptionBoard?.SetActive(false);
        onAudioUpdated?.Invoke(null);
        RefreshDisplay(null);
        if (AllBoardsClosed()) onBoardClose?.Invoke();
    }

    private void OnStartTriggerTimeSelected(int dialogueIndex)
    {
        triggerTimeBoard?.SetActive(false);
        if (triggerTimeText != null) triggerTimeText.text = GetDialogueText(dialogueIndex);
        var existing = currentSection?.Audio;
        if (existing == null) return;
        var audio = existing.Clone();
        audio.TriggerTime = dialogueIndex;
        onAudioUpdated?.Invoke(audio);
        if (AllBoardsClosed()) onBoardClose?.Invoke();
    }

    private void OnEndTriggerTimeSelected(int dialogueIndex)
    {
        triggerTimeEndBoard?.SetActive(false);
        if (triggerTimeEndText != null) triggerTimeEndText.text = GetDialogueText(dialogueIndex);
        var existing = currentSection?.Audio;
        if (existing == null) return;
        var audio = existing.Clone();
        audio.TriggerTimeEnd = dialogueIndex;
        onAudioUpdated?.Invoke(audio);
        if (AllBoardsClosed()) onBoardClose?.Invoke();
    }

    private bool AllBoardsClosed() =>
        !(settingOptionBoard?.activeSelf ?? false) &&
        !(triggerTimeBoard?.activeSelf ?? false) &&
        !(triggerTimeEndBoard?.activeSelf ?? false);

    private void ApplyTriggerType(int triggerType)
    {
        var existing = currentSection?.Audio;
        if (existing == null) return;
        var audio = existing.Clone();
        audio.TriggerType = triggerType;
        onAudioUpdated?.Invoke(audio);
    }

    private void ApplyLoop(int isLoop)
    {
        var existing = currentSection?.Audio;
        if (existing == null) return;
        var audio = existing.Clone();
        audio.IsLoop = isLoop;
        onAudioUpdated?.Invoke(audio);
    }

    public void CloseAllBoards()
    {
        settingOptionBoard?.SetActive(false);
        triggerTimeBoard?.SetActive(false);
        triggerTimeEndBoard?.SetActive(false);
    }

    private string GetDialogueText(int triggerTime)
    {
        if (triggerTime <= 0) return "";
        if (currentSection == null) return $"[对话({triggerTime})]";
        int arrayIndex = triggerTime - 1;
        if (arrayIndex < 0 || arrayIndex >= currentSection.Dialogues.Count)
            return $"[对话({triggerTime})]";
        var d = currentSection.Dialogues[arrayIndex];
        return string.IsNullOrEmpty(d.Text) ? $"[未配置文本({triggerTime})]" : d.Text;
    }

    private POCTheatreAudio BuildCurrentAudio()
    {
        var existing = currentSection?.Audio;
        if (existing != null) return existing.Clone();
        return new POCTheatreAudio { Type = 2, TriggerTime = 0, TriggerTimeEnd = 0, IsLoop = 0, TriggerType = 0 };
    }
}
