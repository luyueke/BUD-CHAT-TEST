
using UnityEngine;
using DG.Tweening;

public enum TheatreEditAnimationMode {
    Scale,          // 缩放弹入
    FadeIn,         // 淡入
    SlideFromLeft,  // 从左滑入
    SlideFromRight, // 从右滑入
    SlideFromTop,   // 从上滑入
    SlideFromBottom,// 从下滑入
    ScaleFade,      // 缩放 + 淡入
}

/// <summary>
/// 剧场编辑器UI基类,用于处理初始化，显示隐藏和卸载，可以管理入场动画
/// </summary>
public class TheatreEditorUIBase<T> : MonoBehaviour
{
    [SerializeField] private bool isAnima = false;
    [SerializeField] private bool isAnimationAllChildNode = false;
    [SerializeField] private Transform[] animationNodes;
    [SerializeField] private float animationDuration = 0.75f;
    [SerializeField] private TheatreEditAnimationMode animationMode = TheatreEditAnimationMode.Scale;
    [SerializeField] private Ease animationEase = Ease.OutBack;
    [SerializeField] private float slideOffset = 300f;

    private bool isInit = false;
    protected T DataRoot;
    public TheatreEditorPanel Panel { get; set; }

    private void OnEnable() {
    }

    private void OnDestroy() {
        Destroy();
    }

    public virtual void OnInit(T param)
    {
        DataRoot = param;
    }

    public virtual void OnShow(T param) {
        if(!isInit) {
            OnInit(param);
            isInit = true;
        }
        if(isAnima) {
            DataRoot = param;
            if(isAnimationAllChildNode) {
                animationNodes = new Transform[transform.childCount];
                for(int i = 0; i < transform.childCount; i++) {
                    animationNodes[i] = transform.GetChild(i);
                }
            }
            Animation();
        }
    }

    public virtual void OnHide() {
    }

    public virtual void Destroy() {
    }

    private void Animation() {
        if(animationNodes == null || animationNodes.Length == 0) return;
        foreach(var node in animationNodes) {
            if(node == null) continue;
            PlayNodeAnimation(node);
        }
    }

    private void PlayNodeAnimation(Transform node) {
        switch(animationMode) {
            case TheatreEditAnimationMode.Scale:
                node.localScale = Vector3.zero;
                node.DOScale(Vector3.one, animationDuration).SetEase(animationEase);
                break;

            case TheatreEditAnimationMode.FadeIn: {
                var cg = GetOrAddCanvasGroup(node);
                cg.alpha = 0f;
                cg.DOFade(1f, animationDuration).SetEase(animationEase);
                break;
            }

            case TheatreEditAnimationMode.SlideFromLeft:
                SlideIn(node, new Vector2(-slideOffset, 0f));
                break;

            case TheatreEditAnimationMode.SlideFromRight:
                SlideIn(node, new Vector2(slideOffset, 0f));
                break;

            case TheatreEditAnimationMode.SlideFromTop:
                SlideIn(node, new Vector2(0f, slideOffset));
                break;

            case TheatreEditAnimationMode.SlideFromBottom:
                SlideIn(node, new Vector2(0f, -slideOffset));
                break;

            case TheatreEditAnimationMode.ScaleFade: {
                node.localScale = Vector3.zero;
                node.DOScale(Vector3.one, animationDuration).SetEase(animationEase);
                var cg = GetOrAddCanvasGroup(node);
                cg.alpha = 0f;
                cg.DOFade(1f, animationDuration).SetEase(Ease.Linear);
                break;
            }
        }
    }

    private void SlideIn(Transform node, Vector2 offset) {
        var rt = node as RectTransform ?? node.GetComponent<RectTransform>();
        if(rt == null) return;
        var targetPos = rt.anchoredPosition;
        rt.anchoredPosition = targetPos + offset;
        rt.DOAnchorPos(targetPos, animationDuration).SetEase(animationEase);
    }

    private CanvasGroup GetOrAddCanvasGroup(Transform node) {
        var cg = node.GetComponent<CanvasGroup>();
        if(cg == null) cg = node.gameObject.AddComponent<CanvasGroup>();
        return cg;
    }
}
