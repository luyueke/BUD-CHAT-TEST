using System;
using System.Collections.Generic;
using Pb.Theatre;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class TheatreEditorSearchSection : MonoBehaviour
{
    [SerializeField] private Button previousBtn;
    [SerializeField] private Button nextBtn;
    [SerializeField] private Text pageNumText;
    [SerializeField] private Text searchText;
    [SerializeField] private Button searchBtn;
    [SerializeField] private ScrollRect searchResultScrollRect;
    [SerializeField] private GameObject searchResultBoard;
    [SerializeField] private Transform searchResultRoot;
    [SerializeField] private Button closeBtn;
    [SerializeField] private TheatreEditorTriggerTimeOptionItem searchResultItemPrefab;

    private const int PageSize = 10;
    private const string Placeholder = "搜索对话/选项";

    private TheatreEditorDataCenter dataCenter;
    private TheatreEditorMainList mainList;
    private Action<POCTheatreSection> onSectionSelected;
    private int currentPage = 1;
    private string currentInput = "";
    private readonly List<TheatreEditorTriggerTimeOptionItem> resultItems = new();

    public void Init(TheatreEditorDataCenter dc, TheatreEditorMainList list, Action<POCTheatreSection> onSelected)
    {
        if (dataCenter != null)
        {
            dataCenter.OnSectionCreated -= OnSectionCountChanged;
            dataCenter.OnSectionRemoved -= OnSectionCountChanged;
        }
        if (mainList != null)
            mainList.OnScrollFirstIndexChanged -= OnScrollFirstIndexChanged;

        dataCenter = dc;
        mainList = list;
        onSectionSelected = onSelected;
        currentPage = 1;
        currentInput = "";

        UpdateSearchText(Placeholder);
        searchResultBoard?.SetActive(false);

        previousBtn?.onClick.RemoveAllListeners();
        previousBtn?.onClick.AddListener(OnPrevPage);

        nextBtn?.onClick.RemoveAllListeners();
        nextBtn?.onClick.AddListener(OnNextPage);

        searchBtn?.onClick.RemoveAllListeners();
        searchBtn?.onClick.AddListener(OpenKeyboard);

        closeBtn?.onClick.RemoveAllListeners();
        closeBtn?.onClick.AddListener(CloseResultBoard);

        if (dataCenter != null)
        {
            dataCenter.OnSectionCreated += OnSectionCountChanged;
            dataCenter.OnSectionRemoved += OnSectionCountChanged;
        }
        if (mainList != null)
            mainList.OnScrollFirstIndexChanged += OnScrollFirstIndexChanged;

        RefreshPageNum();
    }

    private void OnSectionCountChanged(POCTheatreSection _) => RefreshPageNum();

    private void OnScrollFirstIndexChanged(int firstIndex)
    {
        int newPage = firstIndex / PageSize + 1;
        if (newPage == currentPage) return;
        currentPage = newPage;
        RefreshPageNum();
    }

    private void RefreshPageNum()
    {
        if (dataCenter == null || pageNumText == null) return;
        int total = dataCenter.SectionList.Count;
        int totalPages = Mathf.Max(1, Mathf.CeilToInt(total / (float)PageSize));
        currentPage = Mathf.Clamp(currentPage, 1, totalPages);
        pageNumText.text = $"{currentPage}/{totalPages}";
    }

    private void OnPrevPage()
    {
        if (dataCenter == null) return;
        int totalPages = Mathf.Max(1, Mathf.CeilToInt(dataCenter.SectionList.Count / (float)PageSize));
        if (currentPage <= 1) return;
        currentPage--;
        RefreshPageNum();
        ScrollToCurrentPageStart();
    }

    private void OnNextPage()
    {
        if (dataCenter == null) return;
        int totalPages = Mathf.Max(1, Mathf.CeilToInt(dataCenter.SectionList.Count / (float)PageSize));
        if (currentPage >= totalPages) return;
        currentPage++;
        RefreshPageNum();
        ScrollToCurrentPageStart();
    }

    private void ScrollToCurrentPageStart()
    {
        if (mainList == null || dataCenter == null) return;
        int listIndex = (currentPage - 1) * PageSize;
        if (listIndex >= dataCenter.SectionList.Count) return;
        mainList.ScrollToSection(dataCenter.SectionList[listIndex]);
    }

    private void OpenKeyboard()
    {
        var info = new KeyBoardInfo
        {
            type = 0,
            inputMode = (int)KeyBoardInputMode.All,
            maxLength = 100,
            inputFlag = 0,
            defaultText = currentInput,
            returnKeyType = (int)ReturnType.Done,
            textSecurity = 1
        };
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, OnKeyboardInput);
        MobileInterface.Instance.ShowKeyboard(JsonUtility.ToJson(info));
    }

    private void OnKeyboardInput(string input)
    {
        currentInput = input ?? "";
        if (string.IsNullOrEmpty(currentInput))
        {
            UpdateSearchText(Placeholder);
            CloseResultBoard();
            return;
        }

        UpdateSearchText(currentInput);
        ShowSearchResults(currentInput);
    }

    private void UpdateSearchText(string text)
    {
        if (searchText != null) searchText.text = text;
    }

    private void ShowSearchResults(string keyword)
    {
        ClearResultItems();
        if (dataCenter == null || searchResultItemPrefab == null) return;

        var results = BuildSearchResults(keyword);
        if (results.Count == 0)
        {
            searchResultBoard?.SetActive(false);
            return;
        }

        searchResultBoard?.SetActive(true);

        for (int i = 0; i < results.Count; i++)
        {
            var (sectionIndex, preview, section) = results[i];
            bool isLast = i == results.Count - 1;
            var item = Instantiate(searchResultItemPrefab, searchResultRoot);
            var captured = section;
            item.Init(sectionIndex, preview, isLast, _ =>
            {
                mainList?.ScrollToSection(captured);
                onSectionSelected?.Invoke(captured);
                CloseResultBoard();
            });
            item.gameObject.SetActive(true);
            resultItems.Add(item);
        }
    }

    private List<(int sectionIndex, string preview, POCTheatreSection section)> BuildSearchResults(string keyword)
    {
        var results = new List<(int, string, POCTheatreSection)>();
        foreach (var section in dataCenter.SectionList)
        {
            string matched = FindMatchInSection(section, keyword);
            if (matched != null)
                results.Add((section.SectionIndex, matched, section));
        }
        return results;
    }

    private static string FindMatchInSection(POCTheatreSection section, string keyword)
    {
        foreach (var dialogue in section.Dialogues)
        {
            if (dialogue.Type == 1 && Contains(dialogue.Text, keyword))
                return dialogue.Text;

            if (dialogue.Type == 2)
            {
                if (Contains(dialogue.Text, keyword))
                    return dialogue.Text;
                foreach (var option in dialogue.Options)
                {
                    if (Contains(option.Text, keyword))
                        return option.Text;
                }
            }
        }
        return null;
    }

    private static bool Contains(string text, string keyword)
    {
        return !string.IsNullOrEmpty(text) && text.Contains(keyword);
    }

    public void CloseResultBoard()
    {
        searchResultBoard?.SetActive(false);
        ClearResultItems();
    }

    private void ClearResultItems()
    {
        foreach (var item in resultItems)
        {
            if (item != null) Destroy(item.gameObject);
        }
        resultItems.Clear();
    }

    private void OnDestroy()
    {
        if (dataCenter != null)
        {
            dataCenter.OnSectionCreated -= OnSectionCountChanged;
            dataCenter.OnSectionRemoved -= OnSectionCountChanged;
        }
        if (mainList != null)
            mainList.OnScrollFirstIndexChanged -= OnScrollFirstIndexChanged;
    }
}
