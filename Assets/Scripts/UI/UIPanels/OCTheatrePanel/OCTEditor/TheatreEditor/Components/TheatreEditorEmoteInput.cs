using System;
using Com.TheFallenGames.OSA.Util.IO;
using Pb.Theatre;
using UGCAsset;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 管理 Section 的 Emote 配置。
/// triggerTime: 0=段落开始，-1=段落结束，N>0=第N句对话
/// triggerType: 0=对话开始时，1=打字完成时
/// </summary>
public class TheatreEditorEmoteInput : MonoBehaviour
{
    [SerializeField] private Button addBtn;
    [SerializeField] private GameObject hintGObject;
    [SerializeField] private RemoteImageBehaviour emoteImage; // UGC 远程图片
    [SerializeField] private Image pgcEmoteIcon; // PGC 本地 atlas，正确保留比例
    [SerializeField] private USwitchToggle triggerOnStartToggle;
    [SerializeField] private USwitchToggle triggerOnEndToggle;
    [SerializeField] private Button triggerTimeBtn;
    [SerializeField] private Text triggerTimeText;
    [SerializeField] private GameObject triggerTimeBoard;
    [SerializeField] private TheatreEditorTriggerInput triggerInput;
    [SerializeField] private GameObject selectionPanel;
    [SerializeField] private Button switchBtn;
    [SerializeField] private Button deleteBtn;

    private POCTheatreSection currentSection;
    private Action onAddClicked;
    private bool initialized;
    private bool _refreshing;

    public Action onBoardOpen;

    public void Init(Action onAdd)
    {
        onAddClicked = onAdd;
        if (initialized) return;
        initialized = true;

        addBtn?.onClick.RemoveAllListeners();
        addBtn?.onClick.AddListener(() =>
        {
            bool hasEmote = currentSection?.Emote != null && !string.IsNullOrEmpty(currentSection.Emote.EmoteId);
            if (hasEmote)
            {
                triggerTimeBoard?.SetActive(false);
                selectionPanel?.SetActive(true);
                onBoardOpen?.Invoke();
            }
            else
            {
                onAddClicked?.Invoke();
            }
        });

        switchBtn?.onClick.RemoveAllListeners();
        switchBtn?.onClick.AddListener(() =>
        {
            selectionPanel?.SetActive(false);
            onAddClicked?.Invoke();
        });

        deleteBtn?.onClick.RemoveAllListeners();
        deleteBtn?.onClick.AddListener(OnDeleteClicked);

        triggerOnStartToggle?.Init();
        triggerOnEndToggle?.Init();

        triggerOnStartToggle?.onValueChanged.AddListener(isOn =>
        {
            if (!_refreshing && isOn) ApplyTriggerType(0);
        });
        triggerOnEndToggle?.onValueChanged.AddListener(isOn =>
        {
            if (!_refreshing && isOn) ApplyTriggerType(1);
        });

        triggerTimeBtn?.onClick.RemoveAllListeners();
        triggerTimeBtn?.onClick.AddListener(() =>
        {
            bool show = !(triggerTimeBoard?.activeSelf ?? false);
            triggerTimeBoard?.SetActive(show);
            if (show)
            {
                onBoardOpen?.Invoke();
                if (currentSection != null)
                    triggerInput?.Refresh(currentSection, OnDialogueTriggerTimeSelected);
            }
        });
    }

    public void Refresh(POCTheatreSection section, TheatreEditorDataCenter dataCenter)
    {
        currentSection = section;
        if (section == null) return;

        var emote = section.Emote;
        bool hasEmote = emote != null && !string.IsNullOrEmpty(emote.EmoteId);

        hintGObject?.SetActive(!hasEmote);

        if (hasEmote)
        {
            LoadEmoteCover(emote.EmoteId);
            RefreshTriggerDisplay(emote.TriggerTime, emote.TriggerType);
        }
        else
        {
            emoteImage?.gameObject.SetActive(false);
            pgcEmoteIcon?.gameObject.SetActive(false);
            RefreshTriggerDisplay(0, 0);
        }

        triggerTimeBoard?.SetActive(false);
        selectionPanel?.SetActive(false);
    }

    private void LoadEmoteCover(string emoteId)
    {
        if (string.IsNullOrEmpty(emoteId)) return;

        bool isPgc = int.TryParse(emoteId, out _);
        emoteImage?.gameObject.SetActive(!isPgc);
        pgcEmoteIcon?.gameObject.SetActive(isPgc);

        if (isPgc)
        {
            if (pgcEmoteIcon == null) return;
            var sprite = PgcUtils.LoadEmoteIcon(emoteId, gameObject);
            pgcEmoteIcon.sprite = sprite;
            pgcEmoteIcon.preserveAspect = true;
        }
        else
        {
            if (emoteImage == null) return;
            UGCAnimAssetManager.Inst.GetAssetInfo<UGCAnimGetResponse>(emoteId, animInfo =>
            {
                if (animInfo == null) return;
                string cover = animInfo.cover;
                if (!string.IsNullOrEmpty(cover)) emoteImage.Load(cover);
            });
        }
    }

    private void RefreshTriggerDisplay(int triggerTime, int triggerType = 0)
    {
        _refreshing = true;
        if (triggerOnStartToggle != null) triggerOnStartToggle.isOn = triggerType == 0;
        if (triggerOnEndToggle != null) triggerOnEndToggle.isOn = triggerType == 1;

        if (triggerTimeText != null)
            triggerTimeText.text = GetDialogueText(triggerTime);
        _refreshing = false;
    }

    private string GetDialogueText(int triggerTime)
    {
        if (triggerTime <= 0) return "";
        if (currentSection == null) return $"[未配置文本的对话({triggerTime})]";
        int arrayIndex = triggerTime - 1;
        if (arrayIndex < 0 || arrayIndex >= currentSection.Dialogues.Count)
            return $"[未配置文本的对话({triggerTime})]";
        var d = currentSection.Dialogues[arrayIndex];
        return string.IsNullOrEmpty(d.Text) ? $"[未配置文本的对话({triggerTime})]" : d.Text;
    }

    private void ApplyTriggerType(int triggerType)
    {
        if (currentSection?.Emote == null) return;
        var emote = currentSection.Emote.Clone();
        emote.TriggerType = triggerType;
        onEmoteUpdated?.Invoke(emote);
    }

    private void OnDialogueTriggerTimeSelected(int dialogueIndex)
    {
        triggerTimeBoard?.SetActive(false);
        if (triggerTimeText != null)
            triggerTimeText.text = GetDialogueText(dialogueIndex);
        if (currentSection?.Emote == null) return;
        var emote = currentSection.Emote.Clone();
        emote.TriggerTime = dialogueIndex;
        onEmoteUpdated?.Invoke(emote);
    }

    public void CloseTriggerBoard() => triggerTimeBoard?.SetActive(false);
    public void CloseSelectionPanel() => selectionPanel?.SetActive(false);

    private void OnDeleteClicked()
    {
        selectionPanel?.SetActive(false);
        hintGObject?.SetActive(true);
        emoteImage?.gameObject.SetActive(false);
        pgcEmoteIcon?.gameObject.SetActive(false);
        RefreshTriggerDisplay(0, 0);
        onEmoteUpdated?.Invoke(null);
    }

    /// <summary>当 emote 数据有变化时回调，外部（SectionEdit）持有并写回 DataCenter。</summary>
    public Action<POCTheatreEmote> onEmoteUpdated;
}
