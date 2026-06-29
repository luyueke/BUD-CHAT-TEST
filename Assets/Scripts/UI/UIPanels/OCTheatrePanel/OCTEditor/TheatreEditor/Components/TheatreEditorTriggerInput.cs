using System;
using System.Collections.Generic;
using Pb.Theatre;
using UnityEngine;

/// <summary>
/// 触发时机选择面板：列出当前 Section 所有 dialogue，让用户选择在哪一句触发。
/// triggerTime 语义：0=段落开始，-1=段落结束，N>0=第N句对话（1-based）
/// </summary>
public class TheatreEditorTriggerInput : MonoBehaviour
{
    [SerializeField] private Transform triggerTimeOptionRoot;
    [SerializeField] private TheatreEditorTriggerTimeOptionItem triggerTimeItem;

    private readonly List<TheatreEditorTriggerTimeOptionItem> items = new();

    /// <summary>
    /// 用当前 Section 的 dialogue 列表填充选项。
    /// onSelected 回调参数为 dialogueIndex（1-based，对应 triggerTime）。
    /// </summary>
    public void Refresh(POCTheatreSection section, Action<int> onSelected)
    {
        ClearItems();
        if (triggerTimeOptionRoot == null || triggerTimeItem == null || section == null) return;

        // 列出 Type==1（普通对话）和 Type==2（选项对话），triggerTime = 数组位置（1-based）
        // 先收集数量，用于判断 isLast
        int shownCount = 0;
        foreach (var d in section.Dialogues)
            if (d.Type == 1 || d.Type == 2) shownCount++;

        int arrayPos = 1;
        int shownSoFar = 0;
        foreach (var d in section.Dialogues)
        {
            if (d.Type == 1 || d.Type == 2)
            {
                int capturePos = arrayPos;
                string preview = string.IsNullOrEmpty(d.Text) ? "(空)" : (d.Text.Length > 12 ? d.Text.Substring(0, 12) + "..." : d.Text);
                shownSoFar++;
                bool isLast = shownSoFar == shownCount;
                var obj = Instantiate(triggerTimeItem.gameObject, triggerTimeOptionRoot);
                var item = obj.GetComponent<TheatreEditorTriggerTimeOptionItem>();
                item.Init(capturePos, preview, isLast, onSelected);
                items.Add(item);
                obj.SetActive(true);
            }
            arrayPos++;
        }
    }

    private void ClearItems()
    {
        foreach (var item in items)
        {
            if (item != null) Destroy(item.gameObject);
        }
        items.Clear();
    }
}
