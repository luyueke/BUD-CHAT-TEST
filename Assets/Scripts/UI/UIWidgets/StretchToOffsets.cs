using Sirenix.OdinInspector;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class StretchToOffsets : MonoBehaviour
{
    private RectTransform rectTransform;
    void Awake()
    {
        Refresh();
    }

    void OnEnable()
    {
        Refresh();
    }
    void OnValidate()
    {

    }

    void Refresh()
    {
        rectTransform = GetComponent<RectTransform>();
        ConvertStretchToOffsets();
    }
    public void ConvertStretchToOffsets()
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();

        // 当前的stretch信息
        Vector2 oldAnchorMin = rectTransform.anchorMin;
        Vector2 oldAnchorMax = rectTransform.anchorMax;

        // Canvas RectTransform
        RectTransform parentRect = rectTransform.parent as RectTransform;
        if (parentRect == null) return;

        Vector2 parentSize = parentRect.rect.size;
        Debug.Log($"parentRect: {parentRect.name}");
        Debug.Log($"parentSize: {parentSize}");

        // 计算 left, right, top, bottom
        float left = rectTransform.offsetMin.x;
        float bottom = rectTransform.offsetMin.y;
        float right = -rectTransform.offsetMax.x;
        float top = -rectTransform.offsetMax.y;

        // 如果当前是stretch模式，offsetMin/Max是0，所以我们用anchoredPosition和sizeDelta计算
        Vector2 anchoredPosition = rectTransform.anchoredPosition;
        Vector2 sizeDelta = rectTransform.sizeDelta;

        // 转换为非stretch模式
        rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);

        // sizeDelta = 覆盖canvas的大小
        rectTransform.sizeDelta = new(parentSize.y,parentSize.x);
        rectTransform.anchoredPosition = Vector2.zero;

        Debug.Log($"Converted {rectTransform.name} to non-stretch. SizeDelta: {rectTransform.sizeDelta}, AnchoredPos: {rectTransform.anchoredPosition}");
    }
    [Button("ConvertStretchToOffsets")]
    public void ConvertStretchToOffsetsButton()
    {
        ConvertStretchToOffsets();
    }
}
