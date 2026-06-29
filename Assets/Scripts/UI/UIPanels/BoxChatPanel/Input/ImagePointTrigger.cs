using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game
{
    public class ImagePointTrigger : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,IPointerClickHandler
    {
        public Action OnPointDown;

        public Action OnPointUp;

        public Action OnPointClick;

        public Action<Vector2> OnPointDownPos;

        public Action<Vector2> OnPointUpPos;

        public Action OnLongPress;

        float time;
        public void OnPointerClick(PointerEventData eventData)
        {
            OnPointClick?.Invoke();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            OnPointDown?.Invoke();
            OnPointDownPos?.Invoke(eventData.position);
            time = 0.3f;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            OnPointUp?.Invoke();
            OnPointUpPos?.Invoke(eventData.position);
        }

        private void Update()
        {
            if (OnLongPress != null && time > 0)
            {
                time -= Time.deltaTime;
                if (time < 0) 
                {
                    OnLongPress?.Invoke();
                }
            }
        }
    }
}
