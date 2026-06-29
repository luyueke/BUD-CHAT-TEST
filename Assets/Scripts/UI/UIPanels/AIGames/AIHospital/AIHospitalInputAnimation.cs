using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

/// <summary>
/// UI位移动画组件
/// </summary>
public class AIHospitalInputAnimation : MonoBehaviour
{
    [Header("动画设置")]
    [SerializeField] private Vector2 startPosition = new Vector2(0, -100);  // 起始位置
    [SerializeField] private Vector2 targetPosition = Vector2.zero;         // 目标位置
    [SerializeField] private float duration = 0.5f;                         // 动画时长
    [SerializeField] private Ease easeType = Ease.OutBack;                 // 缓动类型

    private RectTransform rectTransform;
    private Tweener moveTween;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    /// <summary>
    /// 播放进入动画
    /// </summary>
    /// <param name="skipAnimation">是否跳过动画直接显示</param>
    public void PlayEnterAnimation(bool skipAnimation = false)
    {
        // 停止当前动画
        StopAnimation();

        // 设置起始位置
        rectTransform.anchoredPosition = startPosition;

        if (skipAnimation)
        {
            SkipToTarget(targetPosition);
            return;
        }

        // 创建新动画
        moveTween = rectTransform.DOAnchorPos(targetPosition, duration)
            .SetEase(easeType)
            .OnComplete(() => {
                moveTween = null;
            });
    }

    /// <summary>
    /// 播放退出动画
    /// </summary>
    /// <param name="skipAnimation">是否跳过动画直接显示</param>
    public void PlayExitAnimation(bool skipAnimation = false)
    {
        StopAnimation();

        if (skipAnimation)
        {
            SkipToTarget(startPosition);
            return;
        }

        moveTween = rectTransform.DOAnchorPos(startPosition, duration)
            .SetEase(easeType)
            .OnComplete(() => {
                moveTween = null;
            });
    }

    /// <summary>
    /// 跳过动画直接设置到目标位置
    /// </summary>
    /// <param name="targetPos">目标位置</param>
    public void SkipToTarget(Vector2 targetPos)
    {
        StopAnimation();
        if (duration > 0)
        {
            DOVirtual.DelayedCall(duration, () => {
                rectTransform.anchoredPosition = targetPos;
            });
        }
        else
        {
            rectTransform.anchoredPosition = targetPos;
        }
    }

    /// <summary>
    /// 停止当前动画
    /// </summary>
    public void StopAnimation()
    {
        if (moveTween != null && moveTween.IsPlaying())
        {
            moveTween.Kill();
            moveTween = null;
        }
    }

    private void OnDestroy()
    {
        StopAnimation();
    }

    /// <summary>
    /// 设置自定义动画参数
    /// </summary>
    public void SetAnimationParams(Vector2 from, Vector2 to, float time, Ease ease)
    {
        startPosition = from;
        targetPosition = to;
        duration = time;
        easeType = ease;
    }
}