using Com.TheFallenGames.OSA.Util.IO;
using Game.COSXML.Model.Tag;
using GameData.BaseInfo;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks.Sources;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AIGame.Base
{
    public class AIParkUgcInputAnim : MonoBehaviour, IPointerDownHandler,IPointerUpHandler
    {
        [HideInInspector] public bool trigger = false;
        [HideInInspector] public Transform target;

        private bool down = false;
        private void Awake()
        {
            trigger = false;
            down = false;
            target = null;
        }
 
        private void OnDestroy()
        {
            trigger = false;
            down = false;
            target = null;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            down = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            down = false;
        }
        private void Update()
        {
            if (trigger && down && target !=null)
            {
#if UNITY_EDITOR
                if (Input.GetKey(KeyCode.Mouse0))
                {
                    target.Translate(1 * Time.deltaTime, 1 * Time.deltaTime, 0);
                }

                if (Input.GetKey(KeyCode.Mouse2))
                {
                    var difference = Input.GetAxis("Mouse ScrollWheel");
                    target.localScale += new Vector3(difference * 1, difference * 1, 0);
                }

#else  
                if (Input.touchCount == 1)
                {
                    Touch touch = Input.GetTouch(0);
                    if (touch.phase == TouchPhase.Moved)
                    {
                        Vector2 touchDeltaPosition = touch.deltaPosition;
                        var speed = 0.01f;
                        target.Translate(touchDeltaPosition.x * speed, touchDeltaPosition.y * speed, 0);
                    }
                }

                if (Input.touchCount == 2)
                {
                    Touch touchZero = Input.GetTouch(0);
                    Touch touchOne = Input.GetTouch(1);

                    Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
                    Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

                    float prevMagnitude = (touchZeroPrevPos - touchOnePrevPos).magnitude;
                    float currentMagnitude = (touchZero.position - touchOne.position).magnitude;

                    float difference = currentMagnitude - prevMagnitude;

                    var zoomSpeed = 0.01f;
                    target.localScale += new Vector3(difference * zoomSpeed, difference * zoomSpeed, 1);
                }
#endif
            }
        }
    }
}