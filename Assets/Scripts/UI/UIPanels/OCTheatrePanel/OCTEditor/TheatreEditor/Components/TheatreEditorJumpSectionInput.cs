using System;
using System.Collections;
using System.Collections.Generic;
using Pb.Theatre;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 跳转目标 Section 选择面板，一次性加载全量 Section，初始滚动到已选位置。
/// </summary>
public class TheatreEditorJumpSectionInput : MonoBehaviour
{
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Transform triggerTimeOptionRoot;
    [SerializeField] private TheatreEditorTriggerTimeOptionItem triggerTimeItem;
    [SerializeField] private Button closeOptionBtn;

    private List<POCTheatreSection> allSections;
    private readonly List<TheatreEditorTriggerTimeOptionItem> items = new();

    private Action<int> onSectionSelected;

    /// <param name="sections">全量 Section 列表</param>
    /// <param name="currentSectionIndex">当前 option 所属 Section 的 SectionIndex（排除自身）</param>
    /// <param name="preselectedJumpIndex">已配置的跳转 SectionIndex（0=未设置）</param>
    /// <param name="onSelected">用户选中某 Section 时回调，参数为 SectionIndex</param>
    public void Init(List<POCTheatreSection> sections, int currentSectionIndex, int preselectedJumpIndex, Action<int> onSelected)
    {
        allSections = sections.FindAll(s => s.SectionIndex != currentSectionIndex);
        onSectionSelected = onSelected;

        closeOptionBtn?.onClick.RemoveAllListeners();
        closeOptionBtn?.onClick.AddListener(() => gameObject.SetActive(false));

        scrollRect?.onValueChanged.RemoveAllListeners();

        RebuildAllItems();

        if (preselectedJumpIndex > 0)
            StartCoroutine(ScrollToItem(preselectedJumpIndex));
    }

    private void RebuildAllItems()
    {
        ClearItems();
        if (triggerTimeOptionRoot == null || triggerTimeItem == null || allSections == null) return;

        for (int i = 0; i < allSections.Count; i++)
        {
            var section = allSections[i];
            bool isLast = (i == allSections.Count - 1);
            string contentText = GetSectionFirstText(section);

            var obj = Instantiate(triggerTimeItem.gameObject, triggerTimeOptionRoot);
            var item = obj.GetComponent<TheatreEditorTriggerTimeOptionItem>();
            int capturedSectionIndex = section.SectionIndex;
            item.Init(capturedSectionIndex, contentText, isLast, idx => onSectionSelected?.Invoke(idx));
            items.Add(item);
            obj.SetActive(true);
        }

        if (triggerTimeOptionRoot is RectTransform rt)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
    }

    private IEnumerator ScrollToItem(int targetSectionIndex)
    {
        yield return null;
        Canvas.ForceUpdateCanvases();

        int idx = allSections != null ? allSections.FindIndex(s => s.SectionIndex == targetSectionIndex) : -1;
        if (idx < 0 || idx >= items.Count || scrollRect == null) yield break;

        var itemRT = items[idx].GetComponent<RectTransform>();
        var contentRT = scrollRect.content;
        if (itemRT == null || contentRT == null) yield break;

        float contentHeight = contentRT.rect.height;
        float viewportHeight = scrollRect.viewport != null
            ? scrollRect.viewport.rect.height
            : scrollRect.GetComponent<RectTransform>().rect.height;
        if (contentHeight <= viewportHeight) yield break;

        float itemTop = Mathf.Abs(itemRT.anchoredPosition.y);
        float scrollPos = Mathf.Clamp01((itemTop - viewportHeight * 0.5f) / (contentHeight - viewportHeight));
        scrollRect.verticalNormalizedPosition = 1f - scrollPos;
    }

    private void ClearItems()
    {
        foreach (var item in items)
        {
            if (item != null)
            {
                item.gameObject.SetActive(false);
                Destroy(item.gameObject);
            }
        }
        items.Clear();
    }

    /// <summary>
    /// 取 Section 中用于预览的第一句文本。
    /// </summary>
    public static string GetSectionFirstText(POCTheatreSection section)
    {
        if (section == null) return "[段落无文本]";
        foreach (var d in section.Dialogues)
        {
            if (d.Type == 1 && !string.IsNullOrEmpty(d.Text)) return d.Text;
            if (d.Type == 2 && !string.IsNullOrEmpty(d.Text)) return d.Text;
        }
        return $"[段落{section.SectionIndex}无文本]";
    }
}
