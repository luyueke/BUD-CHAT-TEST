using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace UI.BaseWidgets
{
    public class ButtonLongClickEvent : UnityEvent { }

    [AddComponentMenu("BudUI/CLongButton", 31)]
    public class CLongButton : CButton
    {
        public float m_LongPressTime = 0.5f;

        private float m_PressTime = 0;
        private bool m_IsPressBtn = false;

        private ButtonLongClickEvent m_OnLongClick = new ButtonLongClickEvent();
        public ButtonLongClickEvent onLongClick
        {
            get { return m_OnLongClick; }
            set { m_OnLongClick = value; }
        }

        private void Update()
        {
            CheckLongPress();
        }

        private void CheckLongPress()
        {
            if (m_IsPressBtn)
            {
                m_PressTime += Time.deltaTime;

                if (m_PressTime >= m_LongPressTime)
                {
                    m_IsPressBtn = false;

                    onLongClick?.Invoke();
                }
            }
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            base.OnPointerUp(eventData);

            m_IsPressBtn = false;
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);

            m_IsPressBtn = true;
            m_PressTime = 0;
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            if (m_PressTime >= m_LongPressTime) return;

            base.OnPointerClick(eventData);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            m_IsPressBtn = false;
        }
    }
}