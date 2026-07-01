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
    [SerializeField] private Toggle tog_my;
    [SerializeField] private Toggle tog_pgc;

    private TheatreEditorDataCenter dataCenter;
    private readonly List<TheatreEditorBGItem> bgItems = new();
    private bool isEditMode;
    private int _lastBgCount = 0;

    private const string RemoteFolderBase = "UgcOCTheatreBG";
    private const string DefImagePath = "Assets/Loadable/UI/UIPanel/OCTheatrePanel/TheatreDefaultBG/";

    private static readonly string[] DefBgNames =
    {
        "车站", "废弃医院", "审讯室", "路口", "废弃小屋", "便利店", "海边",
        "小屋", "公园", "天台", "餐馆", "停车场", "客厅", "小院"
    };

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

        if (tog_my != null)
        {
            tog_my.onValueChanged.RemoveListener(OnMyToggleChanged);
            tog_my.onValueChanged.AddListener(OnMyToggleChanged);
        }
        if (tog_pgc != null)
        {
            tog_pgc.onValueChanged.RemoveListener(OnPgcToggleChanged);
            tog_pgc.onValueChanged.AddListener(OnPgcToggleChanged);
        }

        // 默认开启 tog_my
        if (tog_my != null) tog_my.isOn = true;

        RefreshList();
    }

    private void OnMyToggleChanged(bool isOn)
    {
        if (isOn) RefreshList();
    }

    private void OnPgcToggleChanged(bool isOn)
    {
        if (isOn) RefreshList();
    }

    private bool IsPgcMode => tog_pgc != null && tog_pgc.isOn;

    private void RefreshList()
    {
        bool newItemAdded = dataCenter != null && dataCenter.AllBackgrounds.Count > _lastBgCount;
        _lastBgCount = dataCenter?.AllBackgrounds.Count ?? 0;
        ClearItems();
        isEditMode = false;
        deleteBtn?.gameObject.SetActive(false);
        if (dataCenter == null || bgListRoot == null || bgItemPrefab == null) return;

        if (IsPgcMode)
        {
            // tog_pgc：加载内置默认背景
            editBtn?.gameObject.SetActive(false);
            for (int i = 0; i < DefBgNames.Length; i++)
            {
                var obj = Instantiate(bgItemPrefab, bgListRoot);
                var item = obj.GetComponent<TheatreEditorBGItem>();
                item?.InitAsDef($"{DefImagePath}TheatreDefaultBG{i}.png", DefBgNames[i]);
                bgItems.Add(item);
            }
            return;
        }

        // tog_my：加载用户自己的背景
        editBtn?.gameObject.SetActive(true);
        string folder = $"{RemoteFolderBase}/{AccountDataManager.Inst.Uid}";

        if (dataCenter.AllBackgrounds.Count < TheatreEditorDataCenter.MaxBackgroundCount)
        {
            var obj = Instantiate(bgItemPrefab, bgListRoot);
            var item = obj.GetComponent<TheatreEditorBGItem>();
            item?.InitAsEmpty(folder, url => dataCenter.AddBackground(url));
            bgItems.Add(item);
        }
        for (int i = 0; i < dataCenter.AllBackgrounds.Count; i++)
        {
            var obj = Instantiate(bgItemPrefab, bgListRoot);
            var item = obj.GetComponent<TheatreEditorBGItem>();
            item?.InitAsFilled(dataCenter.AllBackgrounds[i]);
            bgItems.Add(item);
        }

        if (newItemAdded && scrollRect != null && gameObject.activeInHierarchy)
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
