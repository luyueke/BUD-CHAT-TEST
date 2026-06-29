using System.Collections.Generic;
using GameData;
using UnityEngine;
using UnityEngine.UI;

public class TheatreEditorBGLibrary : MonoBehaviour
{
    [SerializeField] private Transform bgListRoot;
    [SerializeField] private GameObject bgItemPrefab;
    [SerializeField] private Button editBtn;
    [SerializeField] private Button deleteBtn;
    [SerializeField] private ScrollRect scrollRect;

    private TheatreEditorDataCenter dataCenter;
    private readonly List<TheatreEditorBGItem> bgItems = new();
    private bool isEditMode;
    private int _lastBgCount = 0;

    private const string RemoteFolderBase = "UgcOCTheatreBG";

    public void Init(TheatreEditorDataCenter dc)
    {
        if (dataCenter != null) dataCenter.OnBackgroundsChanged -= RefreshList;
        dataCenter = dc;
        dataCenter.OnBackgroundsChanged += RefreshList;
        _lastBgCount = dataCenter.AllBackgrounds.Count;

        editBtn?.onClick.RemoveAllListeners();
        editBtn?.onClick.AddListener(OnEditClicked);

        deleteBtn?.onClick.RemoveAllListeners();
        deleteBtn?.onClick.AddListener(OnDeleteClicked);

        RefreshList();
    }

    private void RefreshList()
    {
        bool newItemAdded = dataCenter != null && dataCenter.AllBackgrounds.Count > _lastBgCount;
        _lastBgCount = dataCenter?.AllBackgrounds.Count ?? 0;
        ClearItems();
        isEditMode = false;
        deleteBtn?.gameObject.SetActive(false);
        if (dataCenter == null || bgListRoot == null || bgItemPrefab == null) return;

        string folder = $"{RemoteFolderBase}/{AccountDataManager.Inst.Uid}";

        for (int i = 0; i < dataCenter.AllBackgrounds.Count; i++)
        {
            var obj = Instantiate(bgItemPrefab, bgListRoot);
            var item = obj.GetComponent<TheatreEditorBGItem>();
            item?.InitAsFilled(dataCenter.AllBackgrounds[i]);
            bgItems.Add(item);
        }

        if (dataCenter.AllBackgrounds.Count < TheatreEditorDataCenter.MaxBackgroundCount)
        {
            var obj = Instantiate(bgItemPrefab, bgListRoot);
            var item = obj.GetComponent<TheatreEditorBGItem>();
            item?.InitAsEmpty(folder, url => dataCenter.AddBackground(url));
            bgItems.Add(item);
        }

        if (newItemAdded && scrollRect != null)
            StartCoroutine(ScrollToBottomNextFrame());
    }

    private System.Collections.IEnumerator ScrollToBottomNextFrame()
    {
        yield return null;
        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 0f;
    }

    private void OnEditClicked()
    {
        isEditMode = !isEditMode;
        foreach (var item in bgItems)
            item?.SetEditMode(isEditMode);
        deleteBtn?.gameObject.SetActive(isEditMode);
    }

    private void OnDeleteClicked()
    {
        var toDelete = new List<int>();
        int filledCount = dataCenter.AllBackgrounds.Count;

        for (int i = 0; i < filledCount && i < bgItems.Count; i++)
        {
            if (bgItems[i] != null && bgItems[i].IsSelected)
                toDelete.Add(i);
        }

        if (toDelete.Count == 0) return;

        // 从高索引到低索引删除，避免移位影响
        // 临时取消订阅，批量删除后统一刷新一次，避免每删一张就重建一次 UI
        toDelete.Sort((a, b) => b.CompareTo(a));
        dataCenter.OnBackgroundsChanged -= RefreshList;
        foreach (int idx in toDelete)
            dataCenter.RemoveBackground(idx);
        dataCenter.OnBackgroundsChanged += RefreshList;
        RefreshList();
    }

    private void ClearItems()
    {
        foreach (var item in bgItems)
        {
            if (item != null) Destroy(item.gameObject);
        }
        bgItems.Clear();
    }

    private void OnDestroy()
    {
        if (dataCenter != null) dataCenter.OnBackgroundsChanged -= RefreshList;
    }
}
