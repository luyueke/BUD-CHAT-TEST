using UnityEngine;
using UnityEngine.UI;

public class DynamicAlignment : MonoBehaviour
{
    public HorizontalLayoutGroup layoutGroup; // 拖入Horizontal Layout Group组件
    public RectTransform contentPanel; // 拖入要调整对齐的内容面板
    public int threshold = 4; // 设置一个阈值，超过这个值左对齐

    void Update()
    {
        // 获取当前子元素的数量
        int childCount = contentPanel.childCount;

        if (childCount <= threshold)
        {
            // 子元素少于等于阈值时居中对齐
            layoutGroup.childAlignment = TextAnchor.MiddleCenter;
            layoutGroup.padding.left = 0;
        }
        else
        {
            // 子元素多于阈值时左对齐
            layoutGroup.childAlignment = TextAnchor.MiddleLeft;
            layoutGroup.padding.left = 10; // 根据需求调整padding值
        }

        // 强制更新布局
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentPanel);
    }
}