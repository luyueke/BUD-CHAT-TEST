using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

/// <summary>
/// 全局函数扩展
/// </summary>
public static class GlobalFuncExtensions
{
    static float lastClickTime = 0;

    /// <summary>
    /// 检查是否可以点击 防止连点
    /// </summary>
    /// <param name="time">时间间隔</param>
    /// <returns></returns>
    public static bool CheckCanClick(float time = 1f)
    {
        if (Time.time - lastClickTime < time)
        {
            return false;
        }
        lastClickTime = Time.time;
        return true;
    }

    public static void RefreshLayout(Transform targetTransform)
    {
        if (targetTransform == null) return;

        RectTransform rectTransform = targetTransform.GetComponent<RectTransform>();
        if (rectTransform == null) return;

        // 方法1: 使用 LayoutRebuilder 强制重建布局（推荐）
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        // 方法2: 如果有关联的 ContentSizeFitter，也需要刷新
        ContentSizeFitter contentSizeFitter = targetTransform.GetComponent<ContentSizeFitter>();
        if (contentSizeFitter != null && contentSizeFitter.enabled)
        {
            contentSizeFitter.SetLayoutHorizontal();
            contentSizeFitter.SetLayoutVertical();
            // 再次强制重建
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }

        // // 方法3: 如果有关联的 VerticalLayoutGroup，也需要刷新
        VerticalLayoutGroup verticalLayoutGroup = targetTransform.GetComponent<VerticalLayoutGroup>();
        if (verticalLayoutGroup != null && verticalLayoutGroup.enabled)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }

        // // 方法4: 标记父级布局也需要重建（向上遍历）
        // Transform parent = targetTransform.parent;
        // while (parent != null)
        // {
        //     RectTransform parentRect = parent.GetComponent<RectTransform>();
        //     if (parentRect != null)
        //     {
        //         LayoutRebuilder.MarkLayoutForRebuild(parentRect);
        //     }
        //     parent = parent.parent;
        // }
    }
}
