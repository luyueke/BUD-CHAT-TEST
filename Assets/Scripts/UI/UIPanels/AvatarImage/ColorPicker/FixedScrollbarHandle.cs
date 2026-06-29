using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 
/// </summary>
public class FixedScrollbarHandle : MonoBehaviour
{
    public float fixedHandleSize = 105f; // 设置您希望手柄的固定大小

    private Scrollbar scrollbar;
    private RectTransform handleRect;

    private void Start()
    {
        scrollbar = GetComponent<Scrollbar>();
        handleRect = scrollbar.handleRect;
        FixHandleSize();
    }

    private void FixHandleSize()
    {
        if (scrollbar.direction == Scrollbar.Direction.LeftToRight || scrollbar.direction == Scrollbar.Direction.RightToLeft)
        {
            handleRect.sizeDelta = new Vector2(fixedHandleSize, handleRect.sizeDelta.y);
        }
        else if (scrollbar.direction == Scrollbar.Direction.BottomToTop || scrollbar.direction == Scrollbar.Direction.TopToBottom)
        {
            handleRect.sizeDelta = new Vector2(handleRect.sizeDelta.x, fixedHandleSize);
        }
    }
}