using System;
using System.Collections.Generic;
using GameData.BaseInfo;
using UnityEngine;
using UnityEngine.UI;

public class TheatreEditorAvatarSelector : MonoBehaviour
{
    [SerializeField] private Transform avatarEmoteListRoot;
    [SerializeField] private TheatreEditorEmoteAvatarItemList emoteListItemPrefab;
    [SerializeField] private ScrollRect emoteListScrollRect;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Text countText;
    [SerializeField] private Button backBtn;
    [SerializeField] private GameObject noAvatarTip;

    private readonly List<TheatreEditorEmoteAvatarItemList> _lists = new();
    // ordered: position 0 = actor A, position 1 = actor B
    private readonly List<(string avatarId, int clothesIndex)> _selections = new();
    private int _maxSelections;
    private Action<List<(string avatarId, int clothesIndex)>> _onConfirm;
    private Action _onBack;

    private void Awake()
    {
        confirmButton?.onClick.AddListener(OnConfirmClick);
        backBtn?.onClick.AddListener(OnBackClick);
    }

    public void Show(
        List<string> avatarIds,
        Dictionary<string, OCTheatreAvatarInfo> avatarInfoCache,
        bool isSingle,
        Action<List<(string avatarId, int clothesIndex)>> onConfirm,
        Action onBack)
    {
        _maxSelections = isSingle ? 1 : 2;
        _onConfirm = onConfirm;
        _onBack = onBack;
        _selections.Clear();

        ClearLists();
        if (avatarEmoteListRoot != null)
            for (int i = avatarEmoteListRoot.childCount - 1; i >= 0; i--)
                Destroy(avatarEmoteListRoot.GetChild(i).gameObject);

        foreach (var avatarId in avatarIds)
        {
            if (!avatarInfoCache.TryGetValue(avatarId, out var info)) continue;
            if (emoteListItemPrefab == null || avatarEmoteListRoot == null) break;

            var obj = Instantiate(emoteListItemPrefab.gameObject, avatarEmoteListRoot);
            var list = obj.GetComponent<TheatreEditorEmoteAvatarItemList>();
            if (list == null) { Destroy(obj); continue; }

            list.Init(avatarId, info, -1, OnClothesSelected);
            _lists.Add(list);
            obj.SetActive(true);
        }

        noAvatarTip?.SetActive(_lists.Count == 0);
        RefreshUI();
    }

    private void OnClothesSelected(string avatarId, int clothesIndex)
    {
        int existing = _selections.FindIndex(s => s.avatarId == avatarId);
        if (existing >= 0)
        {
            _selections[existing] = (avatarId, clothesIndex);
        }
        else if (_selections.Count < _maxSelections)
        {
            _selections.Add((avatarId, clothesIndex));
        }
        else
        {
            // At capacity: replace the last slot and deselect its list
            int replaceIndex = _maxSelections - 1;
            var bumped = _selections[replaceIndex];
            var bumpedList = _lists.Find(l => l.AvatarId == bumped.avatarId);
            bumpedList?.Deselect();
            _selections[replaceIndex] = (avatarId, clothesIndex);
        }
        RefreshUI();
    }

    private void RefreshUI()
    {
        int count = _selections.Count;
        if (countText != null)
            countText.text = $"({count}/{_maxSelections})";
        if (confirmButton != null)
            confirmButton.interactable = count >= _maxSelections;
    }

    private void OnConfirmClick()
    {
        if (_selections.Count < _maxSelections) return;
        _onConfirm?.Invoke(new List<(string, int)>(_selections));
    }

    private void OnBackClick()
    {
        _onBack?.Invoke();
    }

    private void ClearLists()
    {
        foreach (var list in _lists)
            if (list != null) Destroy(list.gameObject);
        _lists.Clear();
    }
}
