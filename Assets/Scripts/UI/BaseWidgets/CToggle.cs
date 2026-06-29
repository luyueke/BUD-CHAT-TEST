using DG.Tweening;
using Game.Audio;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BudUI
{
    /// <summary>
    /// 扩展的Toggle组件，添加了缩放动画、音效和文本颜色控制等功能
    /// </summary>
    [AddComponentMenu("BudUI/CToggle", 30)]
    public class CToggle : Toggle, IPointerDownHandler, IPointerUpHandler
    {
        #region 缩放动画参数
        /// <summary>
        /// 是否启用缩放动画
        /// </summary>
        public bool HasScaleAnim = true;

        /// <summary>
        /// 按下时的缩放比例
        /// </summary>
        [Range(0.5f, 1f)]
        public float ScaleOnPressed = 0.8f;

        /// <summary>
        /// 按下时缩放动画的持续时间
        /// </summary>
        [Range(0.01f, 1f)]
        public float ScaleDurationOnPress = 0.3f;

        /// <summary>
        /// 释放时缩放动画的持续时间
        /// </summary>
        [Range(0.01f, 1f)]
        public float ScaleDurationOnReleased = 0.3f;

        /// <summary>
        /// 选中时的缩放比例
        /// </summary>
        [Range(0.5f, 1f)]
        public float ScaleOnSelected = 0.8f;

        /// <summary>
        /// 选中时缩放动画的持续时间
        /// </summary>
        [Range(0.01f, 1f)]
        public float ScaleDurationOnSelected = 0.1f;

        /// <summary>
        /// 取消选中时缩放动画的持续时间
        /// </summary>
        [Range(0.01f, 1f)]
        public float ScaleDurationOnDeselected = 0.1f;

        [HideInInspector]
        protected Vector3 OriginScale;
        #endregion

        #region 音效参数
        /// <summary>
        /// 是否启用音效
        /// </summary>
        public bool HasSound = true;

        /// <summary>
        /// 音效类型
        /// </summary>
        public UISoundType SoundType;

        /// <summary>
        /// 音效播放时机
        /// </summary>
        public SoundTiming Timing = SoundTiming.Click;
        #endregion

        #region 文本参数
        private Tweener _tweener;
        private Transform TextNode;
        protected Text ToggleText;

        /// <summary>
        /// 正常状态下的文本颜色
        /// </summary>
        public Color NormalTextColor = Color.white;

        /// <summary>
        /// 禁用状态下的文本颜色
        /// </summary>
        public Color DisableTextColor = new Color(0.24f, 0.24f, 0.24f);

        /// <summary>
        /// 选中状态下的文本颜色
        /// </summary>
        public Color OnTextColor = Color.white;

        /// <summary>
        /// 未选中状态下的文本颜色
        /// </summary>
        public Color OffTextColor = new Color(0.7f, 0.7f, 0.7f);

        private string localTextValue;
        private string textValue;
        #endregion

        protected override void Awake()
        {
            base.Awake();
            OriginScale = transform.localScale;

            // 查找文本组件
            TextNode = transform.Find("Label");
            if (TextNode != null)
            {
                ToggleText = TextNode.GetComponent<Text>();
            }
            else
            {
                // 尝试在子对象中查找Text组件
                ToggleText = GetComponentInChildren<Text>();
            }

            // 注册值变化事件
            onValueChanged.AddListener(OnToggleValueChanged);
        }

        protected override void Start()
        {
            base.Start();
            // 初始化状态
            UpdateVisualState(isOn);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            onValueChanged.RemoveListener(OnToggleValueChanged);
            if (_tweener != null)
            {
                _tweener.Kill();
                _tweener = null;
            }
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            if (!interactable) return;

            base.OnPointerClick(eventData);

            if (Timing == SoundTiming.Click && HasSound)
            {
                PlaySound();
            }
        }

        public virtual void OnPointerDown(PointerEventData eventData)
        {
            if (!interactable) return;

            if (HasScaleAnim)
            {
                if (_tweener != null)
                {
                    _tweener.Kill();
                }

                _tweener = transform.DOScale(OriginScale * ScaleOnPressed, ScaleDurationOnPress);
            }

            if (Timing == SoundTiming.Press && HasSound)
            {
                PlaySound();
            }
        }

        public virtual void OnPointerUp(PointerEventData eventData)
        {
            if (!interactable) return;

            if (HasScaleAnim)
            {
                if (_tweener != null)
                {
                    _tweener.Kill();
                }

                _tweener = transform.DOScale(OriginScale, ScaleDurationOnReleased);
            }

            if (Timing == SoundTiming.Release && HasSound)
            {
                PlaySound();
            }
        }

        private void PlaySound()
        {
            // 调用声音管理器播放UI音效
            if (AkSoundManager.Inst != null)
            {
                AkSoundManager.Inst.PlayUIEffectSound(SoundType);
            }
        }

        private void OnToggleValueChanged(bool value)
        {
            UpdateVisualState(value);

            if (Timing == SoundTiming.ValueChange && HasSound)
            {
                PlaySound();
            }
        }

        private void UpdateVisualState(bool isOn)
        {
            // 更新文本颜色
            if (ToggleText != null)
            {
                ToggleText.color = interactable ?
                    (isOn ? OnTextColor : OffTextColor) :
                    DisableTextColor;
            }
        }

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);

            // 处理选中状态的缩放动画
            if (!HasScaleAnim) return;

            if (state == SelectionState.Selected)
            {
                if (_tweener != null)
                {
                    _tweener.Kill();
                }

                _tweener = transform.DOScale(OriginScale * ScaleOnSelected, ScaleDurationOnSelected);
            }
            else if (state == SelectionState.Normal)
            {
                if (_tweener != null)
                {
                    _tweener.Kill();
                }

                _tweener = transform.DOScale(OriginScale, ScaleDurationOnDeselected);
            }
        }

        /// <summary>
        /// 设置Toggle是否可交互
        /// </summary>
        public void SetInteractable(bool value)
        {
            interactable = value;

            if (ToggleText != null)
            {
                ToggleText.color = interactable ?
                    (isOn ? OnTextColor : OffTextColor) :
                    DisableTextColor;
            }
        }

        /// <summary>
        /// 设置Toggle文本
        /// </summary>
        public void SetText(string textStr)
        {
            textValue = textStr;
            if (ToggleText != null)
            {
                ToggleText.text = textStr;
            }
        }

        /// <summary>
        /// 设置Toggle本地化文本
        /// </summary>
        public void SetLocalText(string textStr)
        {
            localTextValue = textStr;
            if (ToggleText != null)
            {
                // 如果有本地化管理器，则使用本地化文本
                if (LocalizationManager.Inst != null)
                {
                    ToggleText.text = LocalizationManager.Inst.GetLocalizedText(textStr);
                }
                else
                {
                    ToggleText.text = textStr;
                }
            }
        }

        /// <summary>
        /// 设置Toggle状态（不触发事件）
        /// </summary>
        public void SetIsOnWithoutNotify(bool value)
        {
            base.SetIsOnWithoutNotify(value);
            UpdateVisualState(value);
        }

        /// <summary>
        /// 获取Toggle文本
        /// </summary>
        public string GetText()
        {
            return ToggleText != null ? ToggleText.text : string.Empty;
        }
    }

    /// <summary>
    /// 音效播放时机
    /// </summary>
    public enum SoundTiming
    {
        Click,      // 点击时
        Press,      // 按下时
        Release,    // 释放时
        ValueChange // 值变化时
    }
}