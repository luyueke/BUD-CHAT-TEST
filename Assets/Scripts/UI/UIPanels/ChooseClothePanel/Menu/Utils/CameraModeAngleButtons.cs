using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CameraModeAngleButtons : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private List<CameraModeToggle> buttons;
    [SerializeField] private float space = 22f; // 按钮间隔角度 (参考React: 22度)
    [SerializeField] private float radius = 160f; // 半径 (参考React: 160)
    [SerializeField] public float angleOffset = 0f; // 整体角度偏移
    
    [Header("Visibility")]
    [SerializeField] private float centerAngle = 180f; // 圆心角度 (180度即9点钟方向/左侧)
    [SerializeField] private float visibleRange = 60f; // 可视范围的一半 (7点到11点大约是120度范围，即左右各60度)
    [SerializeField] private float fadeRange = 20f; // 边缘淡出范围

    private List<RectTransform> buttonRects;
    private List<CanvasGroup> buttonGroups;

    private void Awake()
    {
        InitializeButtons();
    }

    private void InitializeButtons()
    {
        buttonRects = new List<RectTransform>();
        buttonGroups = new List<CanvasGroup>();

        foreach (var btn in buttons)
        {
            if (btn == null) continue;

            buttonRects.Add(btn.GetComponent<RectTransform>());
            btn.Init();

            // 使用 CanvasGroup 控制整体透明度 (包括文字和图片)
            var group = btn.GetComponent<CanvasGroup>();
            if (group == null) group = btn.gameObject.AddComponent<CanvasGroup>();
            buttonGroups.Add(group);
        }

        if(buttons.Count > 0){
            buttons[0].isOn = true;
        }
    }

    private void Update()
    {
        UpdateLayout();
    }

    private void UpdateLayout()
    {
        if (buttons == null || buttons.Count == 0) return;

        // 获取当前父物体的旋转角度 (处理 0-360 到 -180~180 的转换)
        float currentRotation = transform.localEulerAngles.z;
        if (currentRotation > 180) currentRotation -= 360;

        // 计算起始偏移，使按钮组整体居中
        // 如果 currentRotation 变大 (逆时针)，按钮视觉上应该顺时针移动以保持相对位置，或者跟随移动。
        // 需求是：按它的旋转来处理按钮位置。通常意味着父物体转，按钮跟着转。
        // 但为了保持在 7-11点区间显示，我们需要计算它们在这个固定窗口内的“绝对”角度。
        
        int count = buttons.Count;
        float startOffset = -(count - 1) * space / 2f;

        for (int i = 0; i < count; i++)
        {
            if (i >= buttonRects.Count) break;

            // 1. 计算基础角度分布
            // baseAngle: 按钮在序列中的相对角度
            // currentRotation: 父物体的旋转偏移
            float offsetAngle = startOffset + (i * space);
            
            // 最终角度 = 中心角度 + 序列偏移 + 父物体旋转 + 额外偏移
            // 注意：Unity UI 坐标系中，0度是右(3点)，90度是上(12点)，180度是左(9点)
            // 加上 currentRotation 意味着如果父物体旋转了，按钮的角度也会增加
            float finalAngle = centerAngle + offsetAngle + currentRotation + angleOffset;

            // 规范化角度到 0-360
            finalAngle = (finalAngle % 360 + 360) % 360;

            // 2. 计算位置 (极坐标转笛卡尔坐标)
            float rad = finalAngle * Mathf.Deg2Rad;
            float x = Mathf.Cos(rad) * radius;
            float y = Mathf.Sin(rad) * radius;

            buttonRects[i].anchoredPosition = new Vector2(x, y);

            // 3. 保持按钮水平 (抵消父物体的旋转)
            // 实际上按钮只是位移了，如果它有父级旋转，它的 localRotation 需要设为反向父级旋转
            // 但如果脚本挂在旋转物体上，buttons是子物体，那么:
            buttonRects[i].localRotation = Quaternion.Euler(0, 0, -currentRotation); 
            // 或者如果只需要绝对水平，不管父级：
            buttonRects[i].rotation = Quaternion.identity;

            // 4. 计算透明度 (限制在 7点(210度) 到 11点(150度) 之间)
            // 11点~150度, 9点=180度, 7点~210度. 
            // 也就是距离 180度 +/- visibleRange 范围内
            
            // 计算与中心角度(180)的距离
            float distToCenter = Mathf.Abs(Mathf.DeltaAngle(finalAngle, centerAngle));

            float alpha = 0;
            if (distToCenter < visibleRange)
            {
                // 在可视范围内
                // 计算边缘淡出
                // distToCenter 越小 alpha 越接近 1
                // 模拟 React: opacity = 1 - Math.pow(distToCenter / 55, 3)
                
                // 简单的线性淡出或者平滑淡出
                float fadeStart = visibleRange - fadeRange;
                if (distToCenter < fadeStart)
                {
                    alpha = 1f;
                }
                else
                {
                    // 渐变区间
                    float t = (distToCenter - fadeStart) / fadeRange;
                    alpha = 1 - t;
                }
            }

            buttonGroups[i].alpha = alpha;
            
            // 如果完全透明，可以禁用交互以防误触
            buttonGroups[i].interactable = alpha > 0.1f;
            buttonGroups[i].blocksRaycasts = alpha > 0.1f;
        }
    }
}