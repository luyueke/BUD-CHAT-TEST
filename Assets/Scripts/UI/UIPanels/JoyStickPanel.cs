using System.Collections;
using System.Collections.Generic;
using Game.KinematicCharacter;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public partial class JoyStickPanel : BasePanel<JoyStickPanel>
{
   public GameObject AdjustPanel;
   public Button AdjuestBtn;
   public InputField speedInput;
   public InputField rotateInput;
   public InputField upSpeedInput;
   public InputField airSpeedInput;
   public InputField gravitySpeedInput;
   public InputField FPSInput;
   public Text FPSText;
   private KinematicCharacterController controller;
   public override void OnCreate()
   {
      base.OnCreate();
      AdjuestBtn.onClick.AddListener(() =>
      {
         AdjustPanel.SetActive(!AdjustPanel.activeInHierarchy);
      });
      //speedInput.text = "3.2";
      //speedInput.onValueChanged.AddListener(x =>
      //{
      //   if (float.TryParse(x, out var val))
      //   {
      //      GetController();
      //      controller.DefautStableMoveSpeed = val;
      //   }
      //});
      //rotateInput.text = "15";
      //rotateInput.onValueChanged.AddListener(x =>
      //{
      //   if (float.TryParse(x, out var val))
      //   {
      //      GetController();
      //      controller.OrientationSharpness = val;
      //   }
      //});
      //upSpeedInput.text = "11";
      //upSpeedInput.onValueChanged.AddListener(x =>
      //{
      //   if (float.TryParse(x, out var val))
      //   {
      //      GetController();
      //      controller.JumpUpSpeed = val;
      //   }
      //});
      
      //airSpeedInput.text = "3.2";
      //airSpeedInput.onValueChanged.AddListener(x =>
      //{
      //   if (float.TryParse(x, out var val))
      //   {
      //      GetController();
      //      controller.MaxAirMoveSpeed = val;
      //   }
      //});
      
      //gravitySpeedInput.text = "-30";
      //gravitySpeedInput.onValueChanged.AddListener(x =>
      //{
      //   if (float.TryParse(x, out var val))
      //   {
      //      GetController();
      //      controller.Gravity = new Vector3(0,val,0);
      //   }
      //});
      
      //FPSInput.onValueChanged.AddListener(x =>
      //{
      //   if (int.TryParse(x, out var val))
      //   {
      //      Application.targetFrameRate = val;
      //   }
      //});
      GetController();
   }

   private int frames2 = 0;
   private float timer = 0;

   protected override void Update()
   {
      ++frames2;
      timer += Time.deltaTime;
      if (timer>=1)
      {
         timer = 0;
         FPSText.text = $"FPS:{frames2.ToString()}";
         frames2 = 0;
      }

        if (Input.GetKeyDown(KeyCode.Space))
        {

        }
   }
   private void GetController()
   {
      if (controller == null)
      {
         MobileJoystick joystick = transform.GetComponentInChildren<MobileJoystick>();
         controller = joystick.Character;
      }
   }
}
