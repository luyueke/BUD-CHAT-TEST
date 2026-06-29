using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

namespace Fsbm.Runtime
{
    public class BtnLongPress : MonoBehaviour, IEventSystemHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler 
    {
        public enum Mode
        {
            Point,Touch
        }

        public UEvent onClick= new UEvent();
        public UEvent onClickDown= new UEvent();
        public UEvent onClickUp= new UEvent();
        public UEvent onLongPress= new UEvent();
        public UEvent onLongPressUp = new UEvent();

        public float longPressTime = 0.5f;
        public bool isRepeatLongPress = false;
        public float repeatLongPressTime = 0.1f;

        public Mode mode = Mode.Point;

        private bool _isPointerDown = false;
        private bool _longPressTriggered = false;
        private float _timePressStarted;
        private PointerEventData _pointData;
        private List<RaycastResult> _raycastResults = new List<RaycastResult>();
        protected virtual void Awake()
        {

        }

        public bool longPressTriggered
        {
            get
            {
                return _longPressTriggered;
            }
        }
        protected  void Update()
        {
            if (_isPointerDown && !_longPressTriggered)
            {
                if (mode == Mode.Touch)
                {
                    if ((Input.GetMouseButton(0) || Input.touchCount > 0) && IsUnderTouch(gameObject))
                    {
                        //not do
                    }
                    else
                    {
                        CancelInvoke("InvokeonLongPressEvent");
                        _isPointerDown = false;
                    }
                        
                }
                
                if (Time.time - _timePressStarted > longPressTime)
                {
                    if (_isPointerDown)
                    {
                        _longPressTriggered = true;
                        onLongPress.Invoke();
                        if (isRepeatLongPress && repeatLongPressTime > 0)
                            InvokeRepeating("InvokeonLongPressEvent", repeatLongPressTime, repeatLongPressTime);
                    }
                }
            }
        }
        private void InvokeonLongPressEvent()
        {
            if ((Input.GetMouseButton(0) || Input.touchCount > 0) && IsUnderTouch(gameObject))
            {
                onLongPress.Invoke();
            }
            else
            {
                CancelInvoke("InvokeonLongPressEvent");
            }
        }

        public bool IsUnderTouch(GameObject go, bool canThrough = false)
        {
            GameObject currentGameObject = null;


            var isTouch = false;
            if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
                isTouch = EventSystem.current != null && Input.touchCount > 0 && EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
            else
                isTouch = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (isTouch)
            {
                if (_pointData == null)
                    _pointData = new PointerEventData(EventSystem.current);

                _pointData.position = Input.mousePosition;
                _raycastResults.Clear();
                EventSystem.current.RaycastAll(_pointData, _raycastResults);

                if (_raycastResults.Count > 0)
                {
                    if (canThrough == false)
                    {
                        currentGameObject = _raycastResults[0].gameObject;
                        if (go == currentGameObject || currentGameObject.transform.IsChildOf(go.transform))
                            return true;
                    }
                    else
                    {
                        for (int i = 0, imax = _raycastResults.Count; i < imax; i++)
                        {
                            currentGameObject = _raycastResults[i].gameObject;
                            if (go == currentGameObject || currentGameObject.transform.IsChildOf(go.transform))
                                return true;
                        }
                    }

                }
                _raycastResults.Clear();
            }
            else
            {


            }
            return false;
        }

        protected void OnDisable()
        {
            _isPointerDown = false;
            _longPressTriggered = false;
        }
        public  void OnPointerDown(PointerEventData eventData)
        {
            
            _timePressStarted = Time.time;
            _isPointerDown = true;
            _longPressTriggered = false;
            onClickDown.Invoke();
        }
        public  void OnPointerExit(PointerEventData eventData)
        {
           if(mode==Mode.Point)
            {
                _isPointerDown = false;
                CancelInvoke("InvokeonLongPressEvent");
            }
        }


        public  void OnPointerUp(PointerEventData eventData)
        {
            if (_isPointerDown && _longPressTriggered == false )
            {
                onClick.Invoke();
            }
            if (mode == Mode.Point)
            {
                _isPointerDown = false;
                CancelInvoke("InvokeonLongPressEvent");
            }
            if (_longPressTriggered)
            {
                onLongPressUp.Invoke();
            }
            onClickUp.Invoke();
        }  
    }
}
