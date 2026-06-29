using System;
using Game.KinematicCharacter;
using UnityEngine;

namespace UI.Utils
{
    /// <summary>
    /// Tip:需要在Game视图下使用
    /// </summary>
    public class DebugInputUtil : MonoBehaviour
    {
        private MobileJoystick _mobileJoystick;

        public MobileJoystick MobileJoystick
        {
            get
            {
                if (_mobileJoystick == null)
                {
                    var mjs = GameObject.Find("MobileJoyStick");
                    if (mjs)
                    {
                        _mobileJoystick = mjs.GetComponentInChildren<MobileJoystick>();
                    }
                }

                return _mobileJoystick;
            }
        }

        private bool _enable = false;
        private bool mIsPCPress = false;

        private void Start()
        {
#if UNITY_EDITOR
            this.gameObject.DontDestroy();
            this.gameObject.name = "UNITY_DEBUG_INPUT";
#endif
        }

        public void BeginDebugInput()
        {
#if UNITY_EDITOR
            _enable = true;
#endif
        }

        public void StopDebugInput()
        {
#if UNITY_EDITOR
            _enable = false;
            Destroy(this.gameObject);
#endif
        }

        private bool isEditorPress = false;
        private void Update()
        {
#if UNITY_EDITOR
            if (!enabled)
            {
                return;
            }

            if (MobileJoystick == null)
            {
                return;
            }
            
            float horizontalInput = Input.GetAxisRaw("Horizontal");
            float verticalInput = Input.GetAxisRaw("Vertical");

            //if (isEditorPress)
            //{
            //    if (Math.Abs(horizontalInput) > 0 || Math.Abs(verticalInput) > 0)
            //    {
            //        isEditorPress = true;
            //    }
            //}

            if (Math.Abs(horizontalInput) > 0 || Math.Abs(verticalInput) > 0)
            {
                isEditorPress = true;

                MobileJoystick.Pressed = true;
                MobileJoystick.axisValue = new Vector2(horizontalInput, verticalInput);
            }
            else if (isEditorPress)
            {
                MobileJoystick.Pressed = false;
                MobileJoystick.axisValue = Vector2.zero;

                isEditorPress = false;
            }

            //松开
            //if (isEditorPress && Math.Abs(horizontalInput) <= 0 && Math.Abs(verticalInput) <= 0)
            //{
            //    if (MobileJoystick.Pressed)
            //    {
            //        MobileJoystick.Pressed = false;
            //        MobileJoystick.axisValue = Vector2.zero;

            //        isEditorPress = false;
            //    }
            //}
            //else
            //{
            //    isEditorPress = true;

            //    MobileJoystick.Pressed = true;
            //    MobileJoystick.axisValue = new Vector2(horizontalInput,verticalInput);
            //}
            

            if (Input.GetKeyDown(KeyCode.Space))
            {
                Jump();
            }
#endif
        }

        private void OnDestroy()
        {
        }

        private void Jump()
        {
            MobileJoystick.JumpButton.onClick.Invoke();
        }
    }
}