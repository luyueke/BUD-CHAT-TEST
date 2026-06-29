using Game.Avatar;
using Game.KinematicCharacter;
using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 载具速度档位
/// </summary>
public enum VehicleGear
{
    Gear1 = 0,  // 1档
    Gear2 = 1,  // 2档
    Gear3 = 2   // 3档
}

/// <summary>
/// 载具档位选择视图
/// 提供下拉列表选择载具速度档位
/// </summary>
public class ChargeGearView : MonoBehaviour
{
    [SerializeField] private Button gearBtn;
    [SerializeField] private Text gearLabel;

    [SerializeField] List<Toggle> toggles;
    [SerializeField] private Transform content;
    [SerializeField] private Transform arraw;


    private BaseKCC baseKCC;
    
    // 档位选项文本
    private readonly string[] gearOptions = { "1档 ", "2档 ", "3档 " };
    private Dictionary<int, CText> toggleLabels = new Dictionary<int, CText>(3);

    private bool isInit = false;
    private Coroutine coroutine;

    /// <summary>
    /// 载具当前档位（仅在载具模式下使用）
    /// </summary>
    protected VehicleGear _vehicleGear = VehicleGear.Gear2;


    private void Start()
    {
        InitializeDropdown();
        coroutine = StartCoroutine( RefreshKCCController());
    }
    /// <summary>
    /// 初始化下拉列表
    /// </summary>
    private void InitializeDropdown()
    {
        if (toggles == null || toggles.Count == 0)
        {
            LoggerUtils.LogError("ChargeGearView: toggles is null!");
            return;
        }

        gearBtn.onClick.AddListener(ShowToggleClick);
        for (int i = 0, C = toggles.Count; i < C; i++)
        {
            var sourceToggle = toggles[i];
            VehicleGear source = (VehicleGear)i;
            toggleLabels[i] = toggles[i].transform.Find("Label").GetComponent<CText>();
            toggleLabels[i].text = gearOptions[i];
            sourceToggle.onValueChanged.AddListener((isOn) =>
            {
                OnGearChanged(source);
            });
        }

        toggles[(int)_vehicleGear].isOn = true;
        OnGearChanged(_vehicleGear);
    }

    private void ShowToggleClick()
    {
        bool show = content.gameObject.activeSelf;
        content.gameObject.SetActive(!show);
        arraw.transform.localRotation = !show ? Quaternion.Euler(Vector3.zero) : Quaternion.Euler(new Vector3(180, 0, 0));
    }

    public void SetVehicleGear(VehicleGear gear)
    {
        _vehicleGear = gear;
        toggles[(int)_vehicleGear].isOn = true;
        OnGearChanged(_vehicleGear);
    }

    /// <summary>
    /// 刷新KCC控制器引用
    /// </summary>
    private IEnumerator RefreshKCCController()
    {
      //  gameObject.SetActive(false);
        while(isInit == false)
        {
            if (AvatarController.Inst != null && AvatarController.Inst.SelfController != null)
            {
                var kccController = AvatarController.Inst.SelfController.CurIKCController;
                baseKCC = kccController as BaseKCC;

                SetVehicleGear();
                // baseKCC已获取，无需额外操作
                isInit = true;

             //   gameObject.SetActive(true);



                if(coroutine != null)
                {
                    StopCoroutine(coroutine);
                }

                coroutine = null;
            }
            yield return 2;
        }

    }
    

    /// <summary>
    /// 档位改变时的回调
    /// </summary>
    private void OnGearChanged(VehicleGear gear)
    {

        content.gameObject.SetActive(false);

        arraw.transform.localRotation = Quaternion.Euler(new Vector3(180, 0, 0));

        // 使用公共方法设置档位（方法内部会检查是否在载具模式）
        _vehicleGear = gear;
        SetVehicleGear();
        
        gearLabel.text = gearOptions[(int)gear];

        if(toggleLabels == null || toggleLabels.Count == 0)
        {
            return;
        }

        for (int i = 0, C = toggles.Count; i < C; i++)
        {
            toggleLabels[i].color = Color.white;
        }

        toggleLabels[(int)gear].color = Color.black;

        LoggerUtils.Log($"ChargeGearView: Gear changed to {gear} ");
    }
    

    
    /// <summary>
    /// 获取当前档位
    /// </summary>
    public void SetVehicleGear()
    {
        if (baseKCC == null)
        {
            LoggerUtils.Log("ChargeGearView: Cannot change gear, BaseKCC not available");
            return;
        }

        // 根据档位设置速度和加速度
        switch (_vehicleGear)
        {
            case VehicleGear.Gear1:
                baseKCC.ApplyVehicleGearSpeed(8f, 8f);
                //m_AirMovementData.MaxAirMoveSpeed = 8f;
                //m_AirMovementData.AirAccelerationSpeed = 4f;
                break;
            case VehicleGear.Gear2:
                baseKCC.ApplyVehicleGearSpeed(30f, 15f);
                //m_AirMovementData.MaxAirMoveSpeed = 12f;
                //m_AirMovementData.AirAccelerationSpeed = 8f;
                break;
            case VehicleGear.Gear3:
                baseKCC.ApplyVehicleGearSpeed(60f, 25f);
                //m_AirMovementData.MaxAirMoveSpeed = 20f;
                //m_AirMovementData.AirAccelerationSpeed = 20f;
                break;
        }
    }

}

