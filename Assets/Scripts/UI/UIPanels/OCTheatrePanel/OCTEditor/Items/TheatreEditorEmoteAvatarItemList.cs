using System;
using System.Collections.Generic;
using GameData.BaseInfo;
using UnityEngine;
using UnityEngine.UI;

public class TheatreEditorEmoteAvatarItemList : MonoBehaviour
{
    [SerializeField] private Text avatarNameText;
    [SerializeField] private ScrollRect expressListScroll;
    [SerializeField] private Transform expressRoot;
    [SerializeField] private TheatreEditorEmoteAvatarItem itemPrefab;

    private readonly List<TheatreEditorEmoteAvatarItem> _items = new();

    public string AvatarId { get; private set; }

    private void Awake()
    {
        if (expressListScroll != null && expressListScroll.GetComponent<NestedScrollRect>() == null)
            expressListScroll.gameObject.AddComponent<NestedScrollRect>();
    }

    public void Deselect()
    {
        foreach (var item in _items)
            item?.SetSelected(false);
    }

    public void Init(string avatarId, OCTheatreAvatarInfo info, int selectedClothesIndex,
        Action<string, int> onClothesSelected)
    {
        AvatarId = avatarId;
        if (avatarNameText != null) avatarNameText.text = info?.name ?? "";

        ClearItems();
        if (info?.avatarClothes == null || itemPrefab == null || expressRoot == null) return;

        for (int i = 0; i < info.avatarClothes.Count; i++)
        {
            var clothes = info.avatarClothes[i];
            if (clothes == null) continue;

            var obj = Instantiate(itemPrefab.gameObject, expressRoot);
            var item = obj.GetComponent<TheatreEditorEmoteAvatarItem>();
            if (item == null) { Destroy(obj); continue; }

            int clothesIndex = i;
            item.Init(clothes, clothesIndex == selectedClothesIndex, _ =>
            {
                UpdateSelection(clothesIndex);
                onClothesSelected?.Invoke(avatarId, clothesIndex);
            });

            _items.Add(item);
            obj.SetActive(true);
        }
    }

    private void UpdateSelection(int selectedIndex)
    {
        for (int i = 0; i < _items.Count; i++)
            _items[i].SetSelected(i == selectedIndex);
    }

    private void ClearItems()
    {
        foreach (var item in _items)
            if (item != null) Destroy(item.gameObject);
        _items.Clear();
    }
}
