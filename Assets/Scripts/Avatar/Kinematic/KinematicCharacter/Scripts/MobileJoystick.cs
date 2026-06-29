using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using Message;
namespace  Game.KinematicCharacter
{
    public class MobileJoystick : MonoBehaviour, IDragHandler, IPointerUpHandler, IPointerDownHandler
    {
        public KinematicCharacterController Character;
        public KinematicCharacterController Pet;
        public Camera CharacterCamera;

        public Button JumpButton;
        
        [Tooltip("Inverts the Horizontal value of the joystick")]
        public bool invertX;
        [Tooltip("Inverts the Vertical value of the joystick")]
        public bool invertY;                     // Bollean to define whether or not the Y axis is inverted.

        [Tooltip("If the Axis Magnitude is lower than this value then the Axis will zero out")]
        public float deathpoint = 0.1f;
        /// <summary>sensitivity for the X Axis</summary>
        public float sensitivityX = 0.05f;
        /// <summary>sensitivity for the Y Axis</summary>
        public float sensitivityY = 0.05f;


        [Tooltip("The Joystick Start position will be First click on the Area")]
        public bool Dynamic = false;

        [Tooltip("If the Joystick is not Moving it will stop moving the Axis ")]
        public bool StopJoyStick = false;

        //    [Header("References")]
        /// <summary> Is the Joystick is being pressed.</summary>
        public bool pressed;
        /// <summary>Variable to Store the XAxis and Y Axis of the JoyStick</summary>
        public Vector2 axisValue;
        private Vector2 DeltaDrag;

        //   [Header("Events")]
        public UnityEvent OnJoystickDown = new UnityEvent();
        public UnityEvent OnJoystickUp = new UnityEvent();
        public UnityEvent<Vector2> OnAxisChange = new UnityEvent<Vector2>();
        public UnityEvent<float> OnXAxisChange = new UnityEvent<float>();
        public UnityEvent<float> OnYAxisChange = new UnityEvent<float>();
        public UnityEvent<bool> OnJoystickPressed = new UnityEvent<bool>();

        private float BgXSize;
        private float BgYSize;


        public bool AxisEditor = true;
        public bool EventsEditor = true;
        public bool ReferencesEditor = true;
        [Tooltip("If true, then the joystick will not use the starting position as guide for calculating the movement axis")]
        public bool m_Drag = false;


        /// <summary>Lets use it to see if the mouse has not moved.Zero means that it moves</summary>
        private int DragRegistered;

        /// <summary>JoyStick Background</summary>
        public Graphic bg;

        /// <summary>Drag Area Background</summary>
        public Graphic DragRect;

        /// <summary>JoyStick Button</summary>
        public Graphic Jbutton;

        /// <summary>Mutliplier to </summary>
        private const float mult = 3;

        private bool isInit = false;
        public bool Pressed
        {
            get => pressed; 
            set { OnJoystickPressed.Invoke(pressed = value); }
        }

        public Vector2 AxisValue
        {
            get => axisValue;  
            set
            {
                if (invertX) value.x *= -1;
                if (invertY) value.y *= -1;

                axisValue = value;
            }
        }

        public float XAxis => AxisValue.x;
        public float YAxis => AxisValue.y;

        private float pressJoystickTime;

        public void SetSelfAvatar(KinematicCharacterController ctrl, KinematicCharacterController pet, Camera cam)
        {
            isInit = true;
            Character = ctrl;
            Pet = pet;
            CharacterCamera = cam;
        }

        private void OnPressChaned(bool isPressed)
        {
            if (!isPressed)
            {
                pressJoystickTime = 0;
            }
        }

        public static MobileJoystick Inst;
        void Awake()
        {
            Inst = this;
        }

        void Start()
        {
          
            if (bg == null)   bg = GetComponent<Graphic>();
            if (Jbutton == null) Jbutton = transform.GetChild(0).GetComponent<Graphic>();
            if (DragRect == null) DragRect = GetComponent<Graphic>(); 
           
            BgXSize = bg.rectTransform.sizeDelta.x;
            BgYSize = bg.rectTransform.sizeDelta.y;
            
            JumpButton?.onClick.AddListener(() =>
            {
                characterInputs.JumpDown =  true;
            });

            OnJoystickPressed.AddListener(OnPressChaned);
        }
        PlayerCharacterInputs characterInputs = new PlayerCharacterInputs();

        /// <summary>
        /// 当需要把左摇杆临时改成“控制相机/镜头”等用途时，开启此开关可阻止摇杆轴值输出给角色移动，
        /// 但仍然保留 axisValue 与 OnAxisChange 事件（用于外部模块持续读取/驱动）。
        /// </summary>
        public bool BlockMoveAxisOutput { get; set; }

        
        public void SetJumpVisible(bool visible)
        {
            JumpButton.gameObject.SetActive(visible);
        }
        
        public void SimulatorJump()
        {
            characterInputs.JumpDown =  true;
        }
        
        void Update()
        {
            if (!isInit)
                return;

            if (StopJoyStick && DragRegistered > 1)
            {
                AxisValue = Vector3.zero;
            }

            if (Pressed)
            {
                OnAxisChange.Invoke(axisValue);
                OnXAxisChange.Invoke(axisValue.x);
                OnYAxisChange.Invoke(axisValue.y);
                DragRegistered++;
                pressJoystickTime += Time.deltaTime;
            }

            // Build the CharacterInputs struct
            characterInputs.MoveAxisForward = axisValue.y;
            characterInputs.MoveAxisRight = axisValue.x;
            characterInputs.CameraRotation = CharacterCamera.transform.rotation;
            characterInputs.PressJoystickTime = pressJoystickTime;
        }

        public void SetInputs(PlayerCharacterInputs inputData)
        {
            if (inputData.MoveAxisForward != 0 || inputData.MoveAxisRight != 0 || inputData.JumpDown)
            {
                MessageHelper.Broadcast(MessageName.OnJoystickChange_new); //这个不受emote影响
            }
            Character.SetInputs(ref inputData);
            if ((inputData.MoveAxisForward != 0 && inputData.MoveAxisRight != 0) || inputData.JumpDown)
            {
                MessageHelper.Broadcast(MessageName.OnJoystickChange);
            }

            
        }

        public PlayerCharacterInputs GetCharacterIputs()
        {
            PlayerCharacterInputs curFrameInputs = characterInputs;
            if (BlockMoveAxisOutput)
            {
                curFrameInputs.MoveAxisForward = 0;
                curFrameInputs.MoveAxisRight = 0;
            }
            if (characterInputs.JumpDown)
            {
                characterInputs.JumpDown = false;
            }

            return curFrameInputs;
        }

        // When draging is occuring this will be called every time the cursor is moved.
        public virtual void OnDrag(PointerEventData Point)
        {
            Vector2 TargetAxis = Vector2.zero;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(bg.rectTransform, Point.position, Point.pressEventCamera, out Vector2 pos))
            {
                if (!m_Drag || Dynamic)
                {
                    pos.x /= BgXSize;              // Get the Joystick position on the 2 axes based on the Bg position.
                    pos.y /= BgYSize;              // Get the Joystick position on the 2 axes based on the Bg position.

                    TargetAxis = new Vector3(pos.x * mult * sensitivityX, pos.y * mult * sensitivityY);        // Position is relative to the  Bg.

                    TargetAxis = (TargetAxis.magnitude > 1.0f ? TargetAxis.normalized : TargetAxis);

                    Vector2 JButtonPos = new Vector2(TargetAxis.x * (BgXSize / mult), TargetAxis.y * (BgYSize / mult));

                    Jbutton.rectTransform.anchoredPosition = JButtonPos;
                }
                else
                {
                    Jbutton.rectTransform.anchoredPosition = pos;
                    var relative = pos - DeltaDrag;

                    TargetAxis =
                        new Vector3(relative.x * sensitivityX * Screen.width * 0.001f, relative.y * sensitivityY * 0.001f * Screen.height);      // Position is relative to the  Bg.
                    DeltaDrag = pos;
                }
            }
            DragRegistered = 0;

            if (TargetAxis.magnitude <= deathpoint)
            {
                AxisValue = Vector2.zero;
            }
            else
            {
                AxisValue = TargetAxis;
            }
        }

        // When the virtual analog's press occured this will be called.
        public virtual void OnPointerDown(PointerEventData Point)
        {
            OnJoystickDown.Invoke();
            Pressed = true;

            DeltaDrag = Vector2.zero;
            if (Dynamic && !m_Drag)
            {
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(DragRect.rectTransform, Point.position, Point.pressEventCamera, out Vector2 DeltaDrag))
                {
                    DeltaDrag.x -= DragRect.rectTransform.sizeDelta.x;              // Get the Joystick Correct X Position
                    DeltaDrag.y -= DragRect.rectTransform.sizeDelta.y;              // Get the Joystick Correct X Position
                    bg.rectTransform.anchoredPosition = DeltaDrag;
                }
            }
            else
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(bg.rectTransform, Point.position, Point.pressEventCamera, out DeltaDrag);
            }
            OnDrag(Point);
        }

        // When the virtual analog's release occured this will be called.
        public virtual void OnPointerUp(PointerEventData _)
        {
            OnJoystickUp.Invoke();
            OnResetJoystick();
        }


        public void OnResetJoystick()
        {
            Pressed = false;
            AxisValue = Vector2.zero;
            pressJoystickTime = 0;
            Jbutton.rectTransform.anchoredPosition = Vector3.zero;
            DeltaDrag = Vector2.zero;
            OnAxisChange.Invoke(axisValue);
            OnXAxisChange.Invoke(axisValue.x);
            OnYAxisChange.Invoke(axisValue.y);
        }

        public void UpdateCharacterInputs()
        {
            characterInputs.MoveAxisForward = axisValue.y;
            characterInputs.MoveAxisRight = axisValue.x;
            characterInputs.CameraRotation = CharacterCamera.transform.rotation;
            characterInputs.PressJoystickTime = pressJoystickTime;
        }
        
        public void SetJoyStickVisible(bool visible)
        {
            if(visible){
                bg.color = new Color(bg.color.r, bg.color.g, bg.color.b, 1);
                Jbutton.color = new Color(Jbutton.color.r, Jbutton.color.g, Jbutton.color.b, 1);
                JumpButton.colors = new ColorBlock{
                    normalColor = new Color(JumpButton.colors.normalColor.r, JumpButton.colors.normalColor.g, JumpButton.colors.normalColor.b, 1),
                    highlightedColor = new Color(JumpButton.colors.highlightedColor.r, JumpButton.colors.highlightedColor.g, JumpButton.colors.highlightedColor.b, 1),
                    pressedColor = new Color(JumpButton.colors.pressedColor.r, JumpButton.colors.pressedColor.g, JumpButton.colors.pressedColor.b, 1),
                    selectedColor = new Color(JumpButton.colors.selectedColor.r, JumpButton.colors.selectedColor.g, JumpButton.colors.selectedColor.b, 1),
                    disabledColor = JumpButton.colors.disabledColor,
                    colorMultiplier = JumpButton.colors.colorMultiplier,
                    fadeDuration = JumpButton.colors.fadeDuration
                };
            }else{
                bg.color = new Color(bg.color.r, bg.color.g, bg.color.b, 0);
                Jbutton.color = new Color(Jbutton.color.r, Jbutton.color.g, Jbutton.color.b, 0);
                JumpButton.colors = new ColorBlock{
                    normalColor = new Color(JumpButton.colors.normalColor.r, JumpButton.colors.normalColor.g, JumpButton.colors.normalColor.b, 0),
                    highlightedColor = new Color(JumpButton.colors.highlightedColor.r, JumpButton.colors.highlightedColor.g, JumpButton.colors.highlightedColor.b, 0),
                    pressedColor = new Color(JumpButton.colors.pressedColor.r, JumpButton.colors.pressedColor.g, JumpButton.colors.pressedColor.b, 0),
                    selectedColor = new Color(JumpButton.colors.selectedColor.r, JumpButton.colors.selectedColor.g, JumpButton.colors.selectedColor.b, 0),
                    disabledColor = JumpButton.colors.disabledColor,
                    colorMultiplier = JumpButton.colors.colorMultiplier,
                    fadeDuration = JumpButton.colors.fadeDuration
                };
            }
        }
    }
}