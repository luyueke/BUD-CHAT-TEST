using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class FlowLayoutGroup : LayoutGroup
{
    public float horizontalSpacing = 10;
    public float verticalSpacing = 10;

    // 是否尊重原始大小
    public bool respectOriginalSize = true;

    // 对齐方式
    public enum ChildAlignment { Left, Center, Right }
    public ChildAlignment rowAlignment = ChildAlignment.Left;

    public override void CalculateLayoutInputHorizontal()
    {
        base.CalculateLayoutInputHorizontal();

        float width = rectTransform.rect.width;
        float xOffset = padding.left;
        float yOffset = padding.top;
        float rowWidth = 0;
        float rowHeight = 0;
        int rowItemCount = 0;

        List<RectTransform> rowItems = new List<RectTransform>();
        List<float> itemWidths = new List<float>();

        // 第一次遍历：测量和计算位置
        for (int i = 0; i < rectChildren.Count; i++)
        {
            var child = rectChildren[i];

            // 获取子项的尺寸
            float childWidth = LayoutUtility.GetPreferredWidth(child);
            float childHeight = LayoutUtility.GetPreferredHeight(child);

            // 检查是否需要换行
            if (xOffset + childWidth > width - padding.right && rowWidth > 0)
            {
                // 处理行对齐
                ArrangeRow(rowItems, itemWidths, xOffset, yOffset, rowWidth, rowHeight);

                // 重置行数据
                xOffset = padding.left;
                yOffset += rowHeight + verticalSpacing;
                rowWidth = 0;
                rowHeight = 0;
                rowItemCount = 0;
                rowItems.Clear();
                itemWidths.Clear();
            }

            // 更新行数据
            rowWidth += childWidth;
            if (rowItemCount > 0) rowWidth += horizontalSpacing;
            rowHeight = Mathf.Max(rowHeight, childHeight);
            rowItemCount++;

            // 存储项目及其宽度
            rowItems.Add(child);
            itemWidths.Add(childWidth);

            // 更新偏移量
            xOffset += childWidth + horizontalSpacing;
        }

        // 处理最后一行
        if (rowItems.Count > 0)
        {
            ArrangeRow(rowItems, itemWidths, xOffset, yOffset, rowWidth, rowHeight);
            yOffset += rowHeight;
        }

        // 设置容器高度
        float totalHeight = yOffset + padding.bottom;
        SetLayoutInputForAxis(totalHeight, totalHeight, -1, 1);
    }

    private void ArrangeRow(List<RectTransform> items, List<float> widths, float rowEndX, float yPos, float rowWidth, float rowHeight)
    {
        float containerWidth = rectTransform.rect.width;
        float startX = padding.left;

        // 根据对齐方式调整起始X位置
        if (rowAlignment == ChildAlignment.Center)
        {
            startX = padding.left + (containerWidth - padding.left - padding.right - rowWidth) / 2;
        }
        else if (rowAlignment == ChildAlignment.Right)
        {
            startX = containerWidth - padding.right - rowWidth;
        }

        // 设置每个项目的位置
        float xPos = startX;
        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            float itemWidth = widths[i];
            float itemHeight = LayoutUtility.GetPreferredHeight(item);

            // 设置位置和大小
            SetChildAlongAxis(item, 0, xPos, respectOriginalSize ? itemWidth : rowHeight);
            SetChildAlongAxis(item, 1, yPos, respectOriginalSize ? itemHeight : rowHeight);

            xPos += itemWidth + horizontalSpacing;
        }
    }

    public override void SetLayoutHorizontal() { }

    public override void SetLayoutVertical() { }

    public override void CalculateLayoutInputVertical() { }
}