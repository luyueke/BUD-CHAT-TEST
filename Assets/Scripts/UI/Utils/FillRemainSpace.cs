using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 用来填充父节点剩余纵向空间，一般用于父节点有VerticalLayoutGroup，某一个子节点想要填充空余空间时使用
/// </summary>
public class FillRemainSpace : MonoBehaviour
{
    private RectTransform rectTransform;
    private RectTransform parentRectTransform;

    public bool isCalculateUnActive = false;//用来控制，是否考虑隐藏节点的高度

    private void OnEnable()
    {
        UpdateHeight();
    }

    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        parentRectTransform = transform.parent.GetComponent<RectTransform>();

        if (rectTransform == null || parentRectTransform == null)
        {
            LoggerUtils.LogError("Missing RectTransform or parent RectTransform.");
            return;
        }

        UpdateHeight();
    }

    public void UpdateHeight(bool calculateUnActive)
    {
        isCalculateUnActive = calculateUnActive;
        UpdateHeight();
    }

    public void UpdateHeight()
    {
        if (rectTransform == null || parentRectTransform == null)
            return;

        float totalPreferredHeight = 0f;
        
        for (int i = 0; i < parentRectTransform.childCount; i++)
        {
            RectTransform childTransform = parentRectTransform.GetChild(i) as RectTransform;
            if (childTransform != null && childTransform != rectTransform)
            {
                bool isActive = isCalculateUnActive ? true : childTransform.gameObject.activeSelf;
                if (isActive)
                {
                    totalPreferredHeight += childTransform.rect.height;
                }
            }
        }

        float remainingHeight = parentRectTransform.rect.height - totalPreferredHeight;
        rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, remainingHeight);
    }
}