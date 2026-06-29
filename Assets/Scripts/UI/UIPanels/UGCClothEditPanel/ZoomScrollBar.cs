using UnityEngine;
using UnityEngine.UI;

public class ZoomScrollBar : MonoBehaviour
{
    public Scrollbar horizontalScrollbar;
    public Scrollbar verticalScrollbar;
    public RectTransform contentContainer;

    private float contentWidth;
    private float containerWidth;
    private float contentHeight;
    private float containerHeight;

    private void Start()
    {
        horizontalScrollbar.onValueChanged.AddListener(OnHorizontalScrollbarValueChanged);
        verticalScrollbar.onValueChanged.AddListener(OnVerticalScrollbarValueChanged);
        CalculateSizes();
        UpdateScrollbars();
    }

    private void CalculateSizes()
    {
        contentWidth = contentContainer.rect.width * contentContainer.localScale.x;
        containerWidth = contentContainer.parent.GetComponent<RectTransform>().rect.width;
        contentHeight = contentContainer.rect.height * contentContainer.localScale.y;
        containerHeight = contentContainer.parent.GetComponent<RectTransform>().rect.height;
    }

    private void UpdateScrollbars()
    {
        float scrollableWidth = Mathf.Max(contentWidth - containerWidth, 0);
        float scrollableHeight = Mathf.Max(contentHeight - containerHeight, 0);

        horizontalScrollbar.gameObject.SetActive(scrollableWidth > 0);
        verticalScrollbar.gameObject.SetActive(scrollableHeight > 0);

        horizontalScrollbar.size = containerWidth / (contentWidth + containerWidth);
        verticalScrollbar.size = containerHeight / (contentHeight + containerHeight);

        Vector2 position = contentContainer.anchoredPosition;
        position.x = Mathf.Clamp(position.x, -scrollableWidth * 0.5f, scrollableWidth * 0.5f);
        position.y = Mathf.Clamp(position.y, -scrollableHeight * 0.5f, scrollableHeight * 0.5f);
        contentContainer.anchoredPosition = position;

        float normalizedX = Mathf.InverseLerp(-scrollableWidth * 0.5f, scrollableWidth * 0.5f, position.x);
        float normalizedY = Mathf.InverseLerp(-scrollableHeight * 0.5f, scrollableHeight * 0.5f, position.y);

        horizontalScrollbar.SetValueWithoutNotify(normalizedX);
        verticalScrollbar.SetValueWithoutNotify(normalizedY);
    }

    public void OnContentResize()
    {
        CalculateSizes();
        UpdateScrollbars();
    }

    public void OnContentMove()
    {
        UpdateScrollbars();
    }

    private void OnHorizontalScrollbarValueChanged(float value)
    {
        Vector2 position = contentContainer.anchoredPosition;
        float scrollableWidth = contentWidth - containerWidth;
        position.x = Mathf.Lerp(-scrollableWidth * 0.5f, scrollableWidth * 0.5f, value);
        contentContainer.anchoredPosition = position;
    }

    private void OnVerticalScrollbarValueChanged(float value)
    {
        Vector2 position = contentContainer.anchoredPosition;
        float scrollableHeight = contentHeight - containerHeight;
        position.y = Mathf.Lerp(-scrollableHeight * 0.5f, scrollableHeight * 0.5f, value);
        contentContainer.anchoredPosition = position;
    }
}
