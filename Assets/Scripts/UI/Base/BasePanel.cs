using System;
using Es;
using UnityEngine;
using System.Collections;

namespace UI.Base
{
    public abstract class BasePanel : MonoBehaviour, IUILifetime
    {
        [NonSerialized]
        public UIPanel Config;

        [NonSerialized]
        public BaseWindow BelongWindow;

        protected Action _openPanelAction;
        protected Action _closePanelAction;

        // 缓动效果相关
        public bool isOpenTakeAni = false;
        private Coroutine _openEffectCoroutine;
        private Coroutine _closeEffectCoroutine;
        private Vector3 _originalScale;
        private Vector3 _originalMaskScale;
        private CanvasGroup _maskCanvasGroup;
        private float _originalMaskAlpha;
        protected Transform mask;
  
        public virtual void CloseSelf()
        {
            if(isOpenTakeAni)
            {
                //关闭特效
                CloseEffect();
            }
            else
            {
                UIManager.Inst.ClosePanel(this);
            }
     
        }

        public void OpenAnimation()
        {
            isOpenTakeAni = true;

            FindMask();
            // 打开效果
            OpenEffect();
        }

        //找遮罩,如果名字不是BgMask,就通过复写
        protected virtual void FindMask()
        {
            mask = transform.Find("BgMask");    
        }

        protected virtual void CloseEffect()
        {
            // 停止之前的缓动
            if (_openEffectCoroutine != null)
            {
                StopCoroutine(_openEffectCoroutine);
                _openEffectCoroutine = null;
            }
            
            // 开始关闭缓动
            _closeEffectCoroutine = StartCoroutine(CloseEffectCoroutine());
        }

        protected virtual void OpenEffect()
        {
            if (mask != null)
            {
                _originalMaskScale = mask.localScale;
                _maskCanvasGroup = mask.GetComponent<CanvasGroup>();
                if (_maskCanvasGroup == null)
                {
                    _maskCanvasGroup = mask.gameObject.AddComponent<CanvasGroup>();
                }
                _originalMaskAlpha = _maskCanvasGroup.alpha;
            }

            // 停止之前的缓动
            if (_closeEffectCoroutine != null)
            {
                StopCoroutine(_closeEffectCoroutine);
                _closeEffectCoroutine = null;
            }
            
            // 保存原始缩放
            _originalScale = transform.localScale;
            
            // 开始打开缓动
            _openEffectCoroutine = StartCoroutine(OpenEffectCoroutine());
        }
        
        /// <summary>
        /// 打开缓动效果：0.3秒从0.2到1.1，然后0.1秒从1.1到1
        /// </summary>
        private IEnumerator OpenEffectCoroutine()
        {
            // 初始状态：缩放为0
            transform.localScale = Vector3.zero;
            
            // 初始化mask状态
            if (mask != null)
            {
                mask.localScale = Vector3.zero;
                if (_maskCanvasGroup != null)
                {
                    _maskCanvasGroup.alpha = 0f;
                }
            }
            
            // 第一阶段：0.3秒内从0.2到1.1
            float duration1 = 0.3f;
            float elapsed1 = 0f;
            
            while (elapsed1 < duration1)
            {
                elapsed1 += Time.deltaTime;
                float progress = elapsed1 / duration1;
                float scale = Mathf.Lerp(0.2f, 1.1f, progress);
                transform.localScale = Vector3.one * scale;
                
                // mask同步缩放和透明度
                if (mask != null)
                {
                    mask.localScale = _originalMaskScale / scale; //需要铺满
                    if (_maskCanvasGroup != null)
                    {
                        _maskCanvasGroup.alpha = Mathf.Lerp(0f, _originalMaskAlpha, progress);
                    }
                }
                
                yield return null;
            }
            
            // 确保达到1.1
            transform.localScale = Vector3.one * 1.1f;
            if (mask != null)
            {
                mask.localScale = _originalMaskScale * 1.1f;
                if (_maskCanvasGroup != null)
                {
                    _maskCanvasGroup.alpha = _originalMaskAlpha;
                }
            }
            
            // 第二阶段：0.1秒内从1.1到1
            float duration2 = 0.1f;
            float elapsed2 = 0f;
            
            while (elapsed2 < duration2)
            {
                elapsed2 += Time.deltaTime;
                float progress = elapsed2 / duration2;
                float scale = Mathf.Lerp(1.1f, 1f, progress);
                transform.localScale = Vector3.one * scale;
                
                // mask同步缩放
                if (mask != null)
                {
                    mask.localScale = _originalMaskScale * scale;
                }
                
                yield return null;
            }
            
            // 确保达到最终状态
            transform.localScale = _originalScale;
            if (mask != null)
            {
                mask.localScale = _originalMaskScale;
            }
            
            // 缓动完成，调用打开回调
            _openPanelAction?.Invoke();
            
            _openEffectCoroutine = null;
        }
        
        /// <summary>
        /// 关闭缓动效果：0.3秒内从1到0
        /// </summary>
        private IEnumerator CloseEffectCoroutine()
        {
            // 初始状态：使用当前缩放
            Vector3 startScale = transform.localScale;
            Vector3 startMaskScale = mask != null ? mask.localScale : Vector3.one;
            float startMaskAlpha = _maskCanvasGroup != null ? _maskCanvasGroup.alpha : 1f;
            
            // 0.5秒内从当前缩放到0
            float duration = 0.2f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                float scale = Mathf.Lerp(1f, 0.5f, progress);
                transform.localScale = startScale * scale;
                
                // mask同步缩放和透明度
                if (mask != null)
                {
                    mask.localScale = startMaskScale / scale;
                    if (_maskCanvasGroup != null)
                    {
                        _maskCanvasGroup.alpha = Mathf.Lerp(startMaskAlpha, 0f, progress);
                    }
                }
                
                yield return null;
            }
            
            // 确保达到0
            transform.localScale = Vector3.zero;
            if (mask != null)
            {
                mask.localScale = Vector3.zero;
                if (_maskCanvasGroup != null)
                {
                    _maskCanvasGroup.alpha = 0f;
                }
            }
            
            // 缓动完成，关闭面板
            UIManager.Inst.ClosePanel(this);
            
            _closeEffectCoroutine = null;
        }
        
        /// <summary>
        /// 清理缓动协程
        /// </summary>
        protected virtual void OnDestroy()
        {
            if (_openEffectCoroutine != null)
            {
                StopCoroutine(_openEffectCoroutine);
                _openEffectCoroutine = null;
            }
            
            if (_closeEffectCoroutine != null)
            {
                StopCoroutine(_closeEffectCoroutine);
                _closeEffectCoroutine = null;
            }
        }
        
        public abstract void OnCreate();
        public abstract void OnShow(params object[] args);
        public abstract void OnHidden();

        /// <summary>
        /// 被其他window遮挡时回调
        /// </summary>
        /// <param name="isCover">是否有全屏UI覆盖</param>
        public abstract void OnWindowBeCovered(bool isCover);

        /// <summary>
        /// window重新显示到栈顶时回调
        /// </summary>
        public abstract void OnWindowBeFocused();
        
        
        /// <summary>
        /// window内容未被完成遮挡回调
        /// </summary>
        public abstract void OnWindowShow();

        public abstract void OnWindowPop();

        public void SetPanelActions(Action openAction, Action closeAction)
        {
            // 先清除之前的事件
            this._openPanelAction= null;
            this._closePanelAction = null;

            // 设置新事件
            this._openPanelAction = openAction;
            this._closePanelAction = closeAction;
        }
    }

    public abstract class BasePanel<T> : BasePanel where T : BasePanel<T>
    {

        #region Unity生命周期

        protected virtual void Awake()
        {
        }

        protected virtual void Start()
        {
        }

        protected virtual void OnEnable()
        {
        }

        protected virtual void OnDisable()
        {
        }

        protected virtual void OnBecameVisible()
        {
        }

        protected virtual void OnBecameInvisible()
        {
        }

        protected virtual void OnDestroy()
        {
            base.OnDestroy(); // 调用基类的OnDestroy来清理缓动协程
            _openPanelAction = null;
            _closePanelAction = null;
        }

        protected virtual void Update()
        {
        }

        protected virtual void FixedUpdate()
        {
        }

        #endregion

        protected Transform GetBaseLayout2D()
        {
            return transform.Find("BaseLayout2D");
        }

        protected Transform GetBaseLayout3D()
        {
            return transform.Find("BaseLayout3D");
        }

        //统一管理BaseLayout2D的显隐
        protected void DealBaseLayout2D(bool value)
        {
            var layout2D = GetBaseLayout2D();
            if (layout2D != null)
            {
                layout2D.gameObject.SetActive(value);
            }
        }


        //统一管理BaseLayout3D下的相机
        protected void DealBaseLayout3D(bool value)
        {
            var layout3D = GetBaseLayout3D();
            if (layout3D != null)
            {
                layout3D.gameObject.SetActive(value);
            }
        }


        public override void OnCreate()
        {
            LoggerUtils.Log($"UI {Config.Name} OnCreate : {Config.PanelId}");
        }

        public override void OnShow(params object[] args)
        {
            LoggerUtils.Log($"UI {Config?.Name} OnShow,,, argsCount:{args?.Length}");
            _openPanelAction?.Invoke();
        }

        public override void OnHidden()
        {
            LoggerUtils.Log($"UI {Config.Name} OnHidden");
            _closePanelAction?.Invoke();
        }

        public override void OnWindowBeCovered(bool isCover)
        {
            LoggerUtils.Log($"UI {Config.Name} OnCoveredByOtherWindow");
        }

        public override void OnWindowBeFocused()
        {
            LoggerUtils.Log($"UI {Config.Name} OnRefocusWindow");
        }

        public override void OnWindowShow()
        {
            LoggerUtils.Log($"UI {Config.Name} OnWindowShow");
        }

        
        public override void OnWindowPop()
        {
            LoggerUtils.Log($"UI {Config.Name} OnWindowPop");
        }
    }
}