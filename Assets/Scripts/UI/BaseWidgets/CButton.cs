using DG.Tweening;
using Game.Audio;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


namespace UI.BaseWidgets
{
    /// <summary>
    /// 音效触发时机
    /// </summary>
    [AddComponentMenu("BudUI/CButton", 30)]
    public class CButton : Button,IPointerDownHandler, IPointerUpHandler
    {
        //动画相关参数---------------------------
        public bool HasScaleAnim = true;
        /// <summary>
        /// 按钮点击时缩放比例
        /// </summary>
        public float ScaleOnPressed = 0.8f;

        /// <summary>
        /// 点击动画时长
        /// </summary>
        public float ScaleDurationOnPress = 0.3f;

        /// <summary>
        /// 抬手动画时长
        /// </summary>
        public float ScaleDurationOnReleased = 0.3f;

        /// <summary>
        /// 按钮Selected缩放比例
        /// </summary>
        public float ScaleOnSelected = 0.8f;

        /// <summary>
        ///钮Selected动画时长
        /// </summary>
        public float ScaleDurationOnSelected = 0.1f;

        /// <summary>
        ///钮Selected 释放动画时长
        /// </summary>
        public float ScaleDurationOnDeselected = 0.1f;
        [HideInInspector]
        public RectTransform RTransform;
        protected Vector3 OriginScale;

        //音效相关参数
        public bool HasSound = true;
        public UISoundType SoundType;
        public SoundTiming Timing = SoundTiming.Click;


        private Tweener _tweener;
        private Transform TextNode;
        protected Text ButtonText;
        public Color NormalTextColor = Color.white;
        public Color DisableTextColor = new Color(0.24f,0.24f,0.24f);
        private string localTextValue;
        private string textValue;
        protected override void Awake()
        {
            base.Awake();
            if(RTransform == null)
            {
                RTransform = transform as RectTransform;
            }

            TextNode = this.transform.Find("ButtonText");
            if (TextNode != null)
            {
                ButtonText = TextNode.GetComponent<Text>();
                ButtonText.color = NormalTextColor;
                if (!string.IsNullOrEmpty(textValue)) {
                    SetText(textValue);
                } else if (!string.IsNullOrEmpty(localTextValue)) {
                    SetLocalText(localTextValue);
                }
            }
        }

        protected override void Start()
        {
            base.Start();
            OriginScale = RTransform.localScale;
        }

        protected override void OnDestroy()
        {
            _tweener?.Kill();
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            base.OnPointerClick(eventData);
            if (Timing == SoundTiming.Click)
            {
                PlaySound();
            }

        }

        public virtual void OnPointerDown(PointerEventData eventData)
        {
            if (Timing == SoundTiming.Down)
            {
                PlaySound();
            }

            if (HasScaleAnim && interactable)
            {
                _tweener?.Kill();
                _tweener = transform.DOScale(Vector3.one * ScaleOnPressed, ScaleDurationOnPress);
            }


        }

        public virtual void OnPointerUp(PointerEventData eventData)
        {
            if (Timing == SoundTiming.Up)
            {
                PlaySound();
            }

            if (HasScaleAnim)
            {
                _tweener?.Kill();
                _tweener = transform.DOScale(OriginScale, ScaleDurationOnReleased);
            }
        }

        private void PlaySound()
        {
            if(HasSound)
            {
                AkSoundManager.Inst.PlayUIEffectSound(SoundType);
            }
        }

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state,instant);
            if (!gameObject.activeInHierarchy)
                return;

            if (ButtonText != null)
            {
                ButtonText.color = interactable ? NormalTextColor : DisableTextColor;
            }
        }

        /// <summary>
        ///包装interactable，设置是否可以点击
        /// </summary>
        /// <param name="value"></param>
        public void SetClickAble(bool value)
        {
            interactable = value;
        }

        /// <summary>
        /// 设置按钮显示文案
        /// </summary>
        /// <param name="textStr"></param>
        public void SetText(string textStr)
        {
            if (ButtonText != null)
            {
                ButtonText.text = textStr;
            } else {
                textValue = textStr;
            }
        }

        public void SetLocalText(string textStr)
        {
            if (ButtonText != null)
            {
                ButtonText.SetLocalText(textStr);
            } else {
                localTextValue = textStr;
            }
        }
    }
}


