using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 动态设置当前节点填充父节点的参数，一般用于父节点有LayoutGroup，子节点无法使用拉伸模式（Stretch）时使用
/// </summary>
public class FixSizeToParent : MonoBehaviour
{
    public float Left = 0;
    public float Right = 0;
    public float Top = 0;
    public float Bottom = 0;
    void Start()
    {
        FixSize();
    }

    private void OnEnable()
    {
        FixSize();
    }

    public void FixSize()
    {
        if(this.transform.parent == null)
        {
            return;
        }

        RectTransform rtf = this.gameObject.GetComponent<RectTransform>();
        RectTransform parentRtf = this.transform.parent.GetComponent<RectTransform>();
        
        if(rtf == null || parentRtf == null)
        {
            return;
        }

        float parentWidth = parentRtf.rect.width;
        float parentHeight = parentRtf.rect.height;

        float newWidth = parentWidth - Left - Right;
        float newHeight = parentHeight - Top - Bottom;

        rtf.sizeDelta = new Vector2(newWidth, newHeight);

        float newX = Left + (parentWidth - Left - Right) * rtf.pivot.x;
        float newY = -Bottom - (parentHeight - Top - Bottom) * rtf.pivot.y;
        
        rtf.anchoredPosition = new Vector2(newX, newY);
    }
}
