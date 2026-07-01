using System;
using System.Collections.Generic;
using GameData.BaseInfo;
using Google.Protobuf;
using Pb.Theatre;
using UnityEngine;

public class TheatreEditorDataCenter
{
    public const int MaxSectionCount = 150;
    public const int MaxDialogueCount = 30;
    public const int MaxOptionCount = 4;
    public const int MaxBackgroundCount = 10;
    public const int MaxAvatarCount = 6;
    public const int MaxTotalDialogueCount = 150;
    public const string NarratorAvatarId = "1";

    public List<POCTheatreSection> SectionList = new();
    public List<POCTheatreAvatarOc> AllAvatars = new();
    public List<string> AllBackgrounds = new();
    public string BgMusic = "";

    public POCTheatreSection currentSelected;
    public OCTheatreInfo TheatreInfo { get; private set; }

    public List<string> SelectedAvatarIds = new();
    public Dictionary<string, OCTheatreAvatarInfo> AvatarInfoCache = new();

    public Action<POCTheatreSection> OnSectionCreated;
    public Action<POCTheatreSection> OnSectionRemoved;
    public Action<POCTheatreSection> OnSectionSelected;
    public Action OnDataChanged;
    public Action OnDialogueAdded;
    public Action OnBackgroundsChanged;
    public Action OnSectionListReordered;

    private bool isInit;

    public void Init()
    {
        isInit = true;
    }

    public void InitLoad(OCTheatreInfo info)
    {
        if (!isInit)
        {
            Debug.LogError("TheatreEditorDataCenter: Init() must be called first");
            return;
        }

        TheatreInfo = info;

        if (info.detailInfoPb != null)
        {
            LoadFromPb(info.detailInfoPb);
        }

        // 新建剧本时确保至少有一个默认 Section（静默插入，不发事件，订阅者此时尚未挂载）
        if (SectionList.Count == 0)
        {
            var defaultSection = new POCTheatreSection
            {
                SectionIndex = 1,
                NextIndex = 0,
            };
            defaultSection.Dialogues.Add(new POCTheatreDialogue { Type = 1, Text = "" });
            SectionList.Add(defaultSection);
        }

        // 自动选中第一个 section，供 MainList 初始化时高亮显示
        if (SectionList.Count > 0 && currentSelected == null)
            currentSelected = SectionList[0];
    }

    private void LoadFromPb(POCTheatreDetailInfo pb)
    {
        SectionList.Clear();
        AllAvatars.Clear();
        AllBackgrounds.Clear();

        foreach (var section in pb.Sections)
            SectionList.Add(section.Clone());

        foreach (var avatar in pb.AllAvatars)
        {
            AllAvatars.Add(avatar.Clone());
            if (!SelectedAvatarIds.Contains(avatar.PlayerId))
                SelectedAvatarIds.Add(avatar.PlayerId);
            // Pre-populate cache so SectionEdit/EmoteEdit work before AvatarEdit is ever visited
            if (!string.IsNullOrEmpty(avatar.PlayerId) && !AvatarInfoCache.ContainsKey(avatar.PlayerId))
            {
                AvatarInfoCache[avatar.PlayerId] = new OCTheatreAvatarInfo
                {
                    id = avatar.PlayerId,
                    name = avatar.AvatarName ?? "",
                    cover = avatar.AvatarUrl ?? "",
                };
            }
        }

        AllBackgrounds.AddRange(pb.AllBackgrounds);
        BgMusic = pb.BgMusic;
    }

    public POCTheatreDetailInfo BuildPb()
    {
        var pb = new POCTheatreDetailInfo();
        pb.BgMusic = BgMusic ?? "";

        foreach (var section in SectionList)
            pb.Sections.Add(section);

        foreach (var avatarId in SelectedAvatarIds)
        {
            // Prefer data already loaded from PB (preserves clothesIndex for existing theatres)
            var existingPb = AllAvatars.Find(a => a.PlayerId == avatarId);
            if (existingPb != null)
            {
                pb.AllAvatars.Add(existingPb);
                continue;
            }
            // Fall back to AvatarInfoCache for new theatres where AllAvatars is empty
            if (AvatarInfoCache.TryGetValue(avatarId, out var info))
            {
                pb.AllAvatars.Add(new POCTheatreAvatarOc
                {
                    PlayerId = info.id ?? "",
                    AvatarName = info.name ?? "",
                    AvatarUrl = info.cover ?? "",
                    ClothesIndex = 0
                });
            }
        }

        pb.AllBackgrounds.AddRange(AllBackgrounds);
        return pb;
    }

    public byte[] SerializeToBytes()
    {
        var pb = BuildPb();
        return pb.ToByteArray();
    }

    // ============= Section 管理 =============

    public POCTheatreSection AddNewSection(int insertIndex = -1)
    {
        if (SectionList.Count >= MaxSectionCount)
        {
            TipPanel.ShowToast("已达最大段落数量限制");
            return null;
        }

        // 新段落自带1条对话，需提前检查总对话上限
        int totalForNewSection = 0;
        foreach (var s in SectionList) totalForNewSection += GetNormalDialogueCount(s);
        if (totalForNewSection >= MaxTotalDialogueCount)
        {
            TipPanel.ShowToast("已达剧场最大对话数量限制");
            return null;
        }

        int newSectionIndex;

        if (insertIndex > 0)
        {
            newSectionIndex = insertIndex;

            // Collect indices >= insertIndex sorted to find contiguous block
            var aboveSorted = new List<int>();
            for (int i = 0; i < SectionList.Count; i++)
                if (SectionList[i].SectionIndex >= insertIndex)
                    aboveSorted.Add(SectionList[i].SectionIndex);
            aboveSorted.Sort();

            // Only bump the contiguous block from insertIndex; stop at first gap
            var toBump = new HashSet<int>();
            int expected = insertIndex;
            foreach (int idx in aboveSorted)
            {
                if (idx == expected) { toBump.Add(idx); expected++; }
                else break;
            }

            for (int i = 0; i < SectionList.Count; i++)
            {
                if (toBump.Contains(SectionList[i].SectionIndex))
                    SectionList[i].SectionIndex++;
                if (SectionList[i].NextIndex != 0 && toBump.Contains(SectionList[i].NextIndex))
                    SectionList[i].NextIndex++;
                foreach (var dialogue in SectionList[i].Dialogues)
                    foreach (var option in dialogue.Options)
                        if (option.JumpIndex != 0 && toBump.Contains(option.JumpIndex))
                            option.JumpIndex++;
            }
        }
        else
        {
            // Find the first unused index in the contiguous sequence from 1
            var sortedAll = new List<int>();
            foreach (var s in SectionList) sortedAll.Add(s.SectionIndex);
            sortedAll.Sort();
            int next = 1;
            foreach (int idx in sortedAll)
            {
                if (idx == next) next++;
                else break;
            }
            newSectionIndex = next;
        }

        var newSection = new POCTheatreSection
        {
            SectionIndex = newSectionIndex,
            NextIndex = 0,
        };

        newSection.Dialogues.Add(new POCTheatreDialogue { Type = 1, Text = "" });

        // Insert in sorted position by SectionIndex
        int listInsertPos = SectionList.Count;
        for (int i = 0; i < SectionList.Count; i++)
        {
            if (SectionList[i].SectionIndex > newSectionIndex)
            {
                listInsertPos = i;
                break;
            }
        }
        SectionList.Insert(listInsertPos, newSection);

        currentSelected = newSection;
        OnSectionCreated?.Invoke(newSection);
        OnDataChanged?.Invoke();
        return newSection;
    }

    public void RemoveSection(POCTheatreSection section)
    {
        if (section == null) return;

        int removedIndex = section.SectionIndex;
        SectionList.Remove(section);

        for (int i = 0; i < SectionList.Count; i++)
        {
            if (SectionList[i].SectionIndex > removedIndex)
                SectionList[i].SectionIndex--;

            if (SectionList[i].NextIndex == removedIndex)
                SectionList[i].NextIndex = 0;
            else if (SectionList[i].NextIndex > removedIndex)
                SectionList[i].NextIndex--;

            foreach (var dialogue in SectionList[i].Dialogues)
            {
                foreach (var option in dialogue.Options)
                {
                    if (option.JumpIndex == removedIndex)
                        option.JumpIndex = 0;
                    else if (option.JumpIndex > removedIndex)
                        option.JumpIndex--;
                }
            }
        }

        if (currentSelected == section)
            currentSelected = SectionList.Count > 0 ? SectionList[0] : null;

        OnSectionRemoved?.Invoke(section);
        OnDataChanged?.Invoke();
    }

    /// <summary>
    /// 将 section 移动到新的 sectionIndex 位置，自动重映射所有 NextIndex / JumpIndex 引用，
    /// 重新排序 SectionList，触发 OnSectionListReordered。
    /// </summary>
    public void MoveSectionToIndex(POCTheatreSection section, int newIndex)
    {
        if (section == null) return;
        if (newIndex < 1 || newIndex > SectionList.Count) return;
        int oldIndex = section.SectionIndex;
        if (oldIndex == newIndex) return;

        int Remap(int refIdx)
        {
            if (refIdx == 0) return 0;
            if (refIdx == oldIndex) return newIndex;
            if (newIndex < oldIndex)
            {
                if (refIdx >= newIndex && refIdx < oldIndex) return refIdx + 1;
            }
            else
            {
                if (refIdx > oldIndex && refIdx <= newIndex) return refIdx - 1;
            }
            return refIdx;
        }

        foreach (var s in SectionList)
            s.SectionIndex = Remap(s.SectionIndex);

        foreach (var s in SectionList)
        {
            s.NextIndex = Remap(s.NextIndex);
            foreach (var d in s.Dialogues)
                foreach (var o in d.Options)
                    o.JumpIndex = Remap(o.JumpIndex);
        }

        SectionList.Sort((a, b) => a.SectionIndex.CompareTo(b.SectionIndex));

        OnSectionListReordered?.Invoke();
        OnDataChanged?.Invoke();
    }

    public POCTheatreSection DuplicateSection(POCTheatreSection source)
    {
        if (source == null) return null;
        if (SectionList.Count >= MaxSectionCount)
        {
            TipPanel.ShowToast("已达最大段落数量限制");
            return null;
        }

        // 复制段落会携带源段落的所有对话，需提前检查总对话上限
        int totalForDup = 0;
        foreach (var s in SectionList) totalForDup += GetNormalDialogueCount(s);
        if (totalForDup + GetNormalDialogueCount(source) > MaxTotalDialogueCount)
        {
            TipPanel.ShowToast("已达剧场最大对话数量限制");
            return null;
        }

        int insertIndex = source.SectionIndex + 1;

        // Only bump the contiguous block starting at insertIndex (stop at first gap)
        var aboveSorted = new List<int>();
        for (int i = 0; i < SectionList.Count; i++)
            if (SectionList[i].SectionIndex >= insertIndex)
                aboveSorted.Add(SectionList[i].SectionIndex);
        aboveSorted.Sort();

        var toBump = new HashSet<int>();
        int expected = insertIndex;
        foreach (int idx in aboveSorted)
        {
            if (idx == expected) { toBump.Add(idx); expected++; }
            else break;
        }

        for (int i = 0; i < SectionList.Count; i++)
        {
            if (toBump.Contains(SectionList[i].SectionIndex))
                SectionList[i].SectionIndex++;
            if (SectionList[i].NextIndex != 0 && toBump.Contains(SectionList[i].NextIndex))
                SectionList[i].NextIndex++;
            foreach (var dialogue in SectionList[i].Dialogues)
                foreach (var option in dialogue.Options)
                    if (option.JumpIndex != 0 && toBump.Contains(option.JumpIndex))
                        option.JumpIndex++;
        }

        // Deep-clone source (after shift so clone inherits updated NextIndex/JumpIndexes)
        var newSection = source.Clone();
        newSection.SectionIndex = insertIndex;

        int listInsertPos = SectionList.IndexOf(source) + 1;
        SectionList.Insert(listInsertPos, newSection);

        currentSelected = newSection;
        OnSectionCreated?.Invoke(newSection);
        OnDataChanged?.Invoke();
        return newSection;
    }

    public void SelectSection(POCTheatreSection section)
    {
        currentSelected = section;
        OnSectionSelected?.Invoke(section);
    }

    // ============= Dialogue 管理 =============

    public POCTheatreDialogue AddDialogue(POCTheatreSection section)
    {
        if (section == null) return null;
        if (GetNormalDialogueCount(section) >= MaxDialogueCount)
        {
            TipPanel.ShowToast("已达最大对话数量限制");
            return null;
        }

        int totalDialogues = 0;
        foreach (var s in SectionList)
            totalDialogues += GetNormalDialogueCount(s);
        if (totalDialogues >= MaxTotalDialogueCount)
        {
            TipPanel.ShowToast("已达剧场最大对话数量限制");
            return null;
        }

        var dialogue = new POCTheatreDialogue { Type = 1, Text = "" };
        int insertPos = GetNormalDialogueCount(section);
        section.Dialogues.Insert(insertPos, dialogue);
        OnDataChanged?.Invoke();
        OnDialogueAdded?.Invoke();
        return dialogue;
    }

    public bool HasOptionsConfigured(POCTheatreSection section)
    {
        var od = GetOptionDialogue(section);
        return od != null && (od.Options.Count > 0 || !string.IsNullOrEmpty(od.Text));
    }

    public void RemoveDialogue(POCTheatreSection section, int dialogueIndex)
    {
        if (section == null || dialogueIndex < 0 || dialogueIndex >= section.Dialogues.Count) return;

        if (!HasOptionsConfigured(section) && GetNormalDialogueCount(section) <= 1 && section.Dialogues[dialogueIndex].Type == 1)
        {
            TipPanel.ShowToast("至少保留一句对话");
            return;
        }

        section.Dialogues.RemoveAt(dialogueIndex);
        OnDataChanged?.Invoke();
    }

    public void MoveDialogueUp(POCTheatreSection section, int dialogueIndex)
    {
        if (section == null || dialogueIndex <= 0) return;
        var prev = section.Dialogues[dialogueIndex - 1];
        if (prev.Type != 1) return;

        var temp = section.Dialogues[dialogueIndex];
        section.Dialogues[dialogueIndex] = section.Dialogues[dialogueIndex - 1];
        section.Dialogues[dialogueIndex - 1] = temp;
        OnDataChanged?.Invoke();
    }

    public void SetDialogueText(POCTheatreSection section, int dialogueIndex, string text)
    {
        if (section == null || dialogueIndex < 0 || dialogueIndex >= section.Dialogues.Count) return;
        section.Dialogues[dialogueIndex].Text = text;
        OnDataChanged?.Invoke();
    }

    // ============= Option 管理 =============

    public POCTheatreDialogue GetOrCreateOptionDialogue(POCTheatreSection section)
    {
        if (section == null) return null;
        for (int i = section.Dialogues.Count - 1; i >= 0; i--)
        {
            if (section.Dialogues[i].Type == 2)
                return section.Dialogues[i];
        }

        var optionDialogue = new POCTheatreDialogue { Type = 2, Text = "" };
        section.Dialogues.Add(optionDialogue);
        OnDataChanged?.Invoke();
        return optionDialogue;
    }

    public POCTheatreDialogue GetOptionDialogue(POCTheatreSection section)
    {
        if (section == null) return null;
        for (int i = section.Dialogues.Count - 1; i >= 0; i--)
        {
            if (section.Dialogues[i].Type == 2)
                return section.Dialogues[i];
        }
        return null;
    }

    public POCTheatreOption AddOption(POCTheatreSection section)
    {
        var optionDialogue = GetOrCreateOptionDialogue(section);
        if (optionDialogue == null) return null;

        if (optionDialogue.Options.Count >= MaxOptionCount)
        {
            TipPanel.ShowToast("已达最大选项数量限制");
            return null;
        }

        var option = new POCTheatreOption { Text = "", JumpIndex = 0 };
        optionDialogue.Options.Add(option);
        OnDataChanged?.Invoke();
        return option;
    }

    public void RemoveOption(POCTheatreSection section, int optionIndex)
    {
        var optionDialogue = GetOptionDialogue(section);
        if (optionDialogue == null || optionIndex < 0 || optionIndex >= optionDialogue.Options.Count) return;

        optionDialogue.Options.RemoveAt(optionIndex);

        if (optionDialogue.Options.Count == 0)
            section.Dialogues.Remove(optionDialogue);

        OnDataChanged?.Invoke();
    }

    public void MoveOptionUp(POCTheatreSection section, int optionIndex)
    {
        var optionDialogue = GetOptionDialogue(section);
        if (optionDialogue == null || optionIndex <= 0) return;

        var temp = optionDialogue.Options[optionIndex];
        optionDialogue.Options[optionIndex] = optionDialogue.Options[optionIndex - 1];
        optionDialogue.Options[optionIndex - 1] = temp;
        OnDataChanged?.Invoke();
    }

    public void SetOptionText(POCTheatreSection section, int optionIndex, string text)
    {
        var optionDialogue = GetOptionDialogue(section);
        if (optionDialogue == null || optionIndex < 0 || optionIndex >= optionDialogue.Options.Count) return;
        optionDialogue.Options[optionIndex].Text = text;
        OnDataChanged?.Invoke();
    }

    public void SetOptionJumpIndex(POCTheatreSection section, int optionIndex, int jumpIndex)
    {
        var optionDialogue = GetOptionDialogue(section);
        if (optionDialogue == null || optionIndex < 0 || optionIndex >= optionDialogue.Options.Count) return;
        optionDialogue.Options[optionIndex].JumpIndex = jumpIndex;
        OnDataChanged?.Invoke();
    }

    /// <summary>
    /// 写入 option dialogue 的 Text 字段（对应 OptionSectionInput）。
    /// 若 optionDialogue 不存在则创建；若有内容且无选项则自动添加一个空选项。
    /// </summary>
    public void SetOptionDialogueText(POCTheatreSection section, string text)
    {
        var optionDialogue = GetOrCreateOptionDialogue(section);
        if (optionDialogue == null) return;
        optionDialogue.Text = text;
        if (!string.IsNullOrEmpty(text) && optionDialogue.Options.Count == 0)
            optionDialogue.Options.Add(new POCTheatreOption { Text = "", JumpIndex = 0 });
        OnDataChanged?.Invoke();
    }

    // ============= Section 属性设置 =============

    public void SetSectionNextIndex(POCTheatreSection section, int nextIndex)
    {
        if (section == null) return;
        section.NextIndex = nextIndex;
        OnDataChanged?.Invoke();
    }

    public void SetSectionAvatar(POCTheatreSection section, string avatarId, string avatarType)
    {
        if (section == null) return;
        section.AvatarId = avatarId;
        section.AvatarType = avatarType;
        OnDataChanged?.Invoke();
    }

    public void SetSectionHideAvatar(POCTheatreSection section, bool hideAvatar)
    {
        if (section == null) return;
        section.HideAvatar = hideAvatar;
        OnDataChanged?.Invoke();
    }

    public void SetSectionBackground(POCTheatreSection section, int backgroundIndex)
    {
        if (section == null) return;
        section.BackgroundUrl = backgroundIndex;
        OnDataChanged?.Invoke();
    }

    public void SetSectionAudio(POCTheatreSection section, POCTheatreAudio audio)
    {
        if (section == null) return;
        section.Audio = audio;
        OnDataChanged?.Invoke();
    }

    public void SetSectionDubbing(POCTheatreSection section, POCTheatreAudio dubbing)
    {
        if (section == null) return;
        section.Dubbing = dubbing;
        OnDataChanged?.Invoke();
    }

    public void SetSectionEmote(POCTheatreSection section, POCTheatreEmote emote)
    {
        if (section == null) return;
        section.Emote = emote;
        OnDataChanged?.Invoke();
    }

    // ============= 背景图库管理 =============

    public bool AddBackground(string url)
    {
        if (AllBackgrounds.Count >= MaxBackgroundCount)
        {
            TipPanel.ShowToast("已达最大背景图片数量限制");
            return false;
        }
        AllBackgrounds.Add(url);
        OnBackgroundsChanged?.Invoke();
        OnDataChanged?.Invoke();
        return true;
    }

    public void RemoveBackground(int index)
    {
        if (index < 0 || index >= AllBackgrounds.Count) return;
        AllBackgrounds.RemoveAt(index);

        // BackgroundUrl is 1-based (0 = none); AllBackgrounds uses 0-based list index
        foreach (var section in SectionList)
        {
            if (section.BackgroundUrl == index + 1)
                section.BackgroundUrl = 0;
            else if (section.BackgroundUrl > index + 1)
                section.BackgroundUrl--;
        }

        OnBackgroundsChanged?.Invoke();
        OnDataChanged?.Invoke();
    }

    // ============= 剧本基本信息 =============

    public void SetTheatreName(string name)
    {
        if (TheatreInfo != null) TheatreInfo.name = name;
        OnDataChanged?.Invoke();
    }

    public void SetTheatreDescription(string desc)
    {
        if (TheatreInfo != null) TheatreInfo.desc = desc;
        OnDataChanged?.Invoke();
    }

    public void SetTheatreCover(string coverUrl)
    {
        if (TheatreInfo != null) TheatreInfo.cover = coverUrl;
        OnDataChanged?.Invoke();
    }

    public void SetBgMusic(string musicUrl)
    {
        BgMusic = musicUrl;
        OnDataChanged?.Invoke();
    }

    // ============= 演员管理 =============

    public void ToggleAvatarSelected(string avatarId)
    {
        if (SelectedAvatarIds.Contains(avatarId))
            SelectedAvatarIds.Remove(avatarId);
        else
            SelectedAvatarIds.Add(avatarId);
        OnDataChanged?.Invoke();
    }

    public bool IsAvatarSelected(string avatarId)
    {
        return SelectedAvatarIds.Contains(avatarId);
    }

    // ============= 辅助方法 =============

    public int GetNormalDialogueCount(POCTheatreSection section)
    {
        if (section == null) return 0;
        int count = 0;
        foreach (var d in section.Dialogues)
        {
            if (d.Type == 1) count++;
        }
        return count;
    }

    public void UpdateTheatreInfoCounts()
    {
        if (TheatreInfo == null) return;
        TheatreInfo.sectionCount = SectionList.Count;

        int optionCount = 0;
        int textCount = 0;
        var emoteSet = new HashSet<string>();
        var soundSet = new HashSet<string>();

        foreach (var section in SectionList)
        {
            foreach (var dialogue in section.Dialogues)
            {
                if (dialogue.Type == 1) textCount++;
                optionCount += dialogue.Options.Count;
            }

            if (section.Emote != null && !string.IsNullOrEmpty(section.Emote.EmoteId))
                emoteSet.Add(section.Emote.EmoteId);

            if (section.Audio != null && !string.IsNullOrEmpty(section.Audio.AudioId))
                soundSet.Add(section.Audio.AudioId);

            if (section.Dubbing != null && !string.IsNullOrEmpty(section.Dubbing.AudioId))
                soundSet.Add(section.Dubbing.AudioId);
        }

        TheatreInfo.optionCount = optionCount;
        TheatreInfo.textCount = textCount;
        TheatreInfo.allUseEmote = new List<string>(emoteSet);
        TheatreInfo.allSoundEffects = new List<string>(soundSet);

        var avatarList = new List<GameData.BaseInfo.OCTheatreAvatarOc>();
        foreach (var id in SelectedAvatarIds)
        {
            string name = "";
            string url = "";
            int clothesIndex = 0;

            if (AvatarInfoCache.TryGetValue(id, out var info))
            {
                name = info.name ?? "";
                if (info.expressions != null && info.expressions.Count > 0)
                {
                    var neutral = info.expressions.Find(e => e.expressionName == "常态");
                    url = (neutral ?? info.expressions[0]).expressionURL ?? "";
                }
                if (string.IsNullOrEmpty(url)) url = info.cover ?? "";
            }

            var pbAvatar = AllAvatars.Find(a => a.PlayerId == id);
            if (pbAvatar != null)
            {
                if (string.IsNullOrEmpty(name)) name = pbAvatar.AvatarName ?? "";
                if (string.IsNullOrEmpty(url)) url = pbAvatar.AvatarUrl ?? "";
                clothesIndex = pbAvatar.ClothesIndex;
            }

            avatarList.Add(new GameData.BaseInfo.OCTheatreAvatarOc
            {
                playerId = id,
                avatarName = name,
                avatarURL = url,
                clothesIndex = clothesIndex,
            });
        }
        TheatreInfo.avatarList = avatarList;
    }

    public void Dispose()
    {
        SectionList.Clear();
        AllAvatars.Clear();
        AllBackgrounds.Clear();
        SelectedAvatarIds.Clear();
        AvatarInfoCache.Clear();
        OnSectionCreated = null;
        OnSectionRemoved = null;
        OnSectionSelected = null;
        OnDataChanged = null;
        OnBackgroundsChanged = null;
    }
}
