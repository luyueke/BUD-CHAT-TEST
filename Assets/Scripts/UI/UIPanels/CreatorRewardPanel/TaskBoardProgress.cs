
using UnityEngine;
using UnityEngine.UI;

public class TaskBoardProgress : MonoBehaviour
{
    private Scrollbar ProgressScrollBar;

    public Sprite activeBg;
    public Sprite inactiveBg;

    public Image[] icons;
    
    private void Awake()
    {
        ProgressScrollBar = GetComponentInChildren<Scrollbar>();
    }

    public void InitProgress(int fanNum)
    {
        // 粉丝数对应的阈值
        int[] fanThresholds = { 50, 200, 500, 1000 };
        // 每个阈值对应的进度值
        float[] progressValues = { 0.25f, 0.441f, 0.599f, 0.81f };

        // 初始化进度
        float progress = 0f;

        // 根据粉丝数计算对应的进度
        for (int i = 0; i < fanThresholds.Length - 1; i++)
        {
            if (fanNum >= fanThresholds[i] && fanNum < fanThresholds[i + 1])
            {
                // 插值计算进度
                float t = (float)(fanNum - fanThresholds[i]) / (fanThresholds[i + 1] - fanThresholds[i]);
                progress = Mathf.Lerp(progressValues[i], progressValues[i + 1], t);
                break;
            }
            else if (fanNum >= fanThresholds[fanThresholds.Length - 1])
            {
                // 如果粉丝数超过最大阈值
                progress = progressValues[progressValues.Length - 1];
            }
        }

        // 更新进度图标
        for (int i = 0; i < icons.Length; i++)
        {
            icons[i].sprite = (fanNum >= fanThresholds[i]) ? activeBg : inactiveBg;
        }

        // 设置进度条的大小
        ProgressScrollBar.size = progress;
    }


    
}
