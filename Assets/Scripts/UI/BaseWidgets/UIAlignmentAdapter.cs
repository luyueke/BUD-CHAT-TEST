using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIAlignmentAdapter : MonoBehaviour
{
    //[SerializeField] private RectTransform backgroundImage;
    [Header("目标对象")]
    [SerializeField] private RectTransform titleText;
    [Header("距离top间距")]
    [SerializeField] private float topPadding = 100f; // 背景顶部padding，单位像素
    [Header("标题居中向上偏移百分比"),Range(0,1)]
    [SerializeField] private float titleOffsetPercent = 0.27f; // 标题向上偏移比例

    private void Start()
    {
        SetupLayout();
    }

    private void SetupLayout()
    {
        if (titleText != null)
        {
            // 计算标题的位置
            float screenHeight = Screen.height - topPadding;
            float offsetY = screenHeight * titleOffsetPercent;
            
            // 设置标题位置
            //titleText.anchorMin = new Vector2(0.5f, 0.5f);
            //titleText.anchorMax = new Vector2(0.5f, 0.5f);
            //titleText.pivot = new Vector2(0.5f, 0.5f);
            titleText.anchoredPosition = new Vector2(0, offsetY);
            LoggerUtils.Log($"当前屏幕分辨率是 screenHeight = {screenHeight} ,topPadding = {topPadding}, 计算出来的 offsetY ={offsetY}");
        }
    }

    // 屏幕分辨率改变时重新计算
    //private void OnRectTransformDimensionsChange()
    //{
    //    SetupLayout();
    //}
}
