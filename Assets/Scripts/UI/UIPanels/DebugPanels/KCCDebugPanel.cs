using Game.Avatar;
using Game.KinematicCharacter;
using UnityEngine;

/// <summary>
/// KCC调试面板
/// 使用IMGUI实现，无需Prefab
/// 用于在运行时调节角色移动参数
/// </summary>
public class KCCDebugPanel : MonoBehaviour
{
    private static KCCDebugPanel _instance;
    public static KCCDebugPanel Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("KCCDebugPanel");
                _instance = go.AddComponent<KCCDebugPanel>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }
    private IKCController kccController;
    private float originalMaxAirMoveSpeed;
    private float originalAirAccelerationSpeed;
    
    // GUI参数
    private float currentMaxSpeed;
    private float currentAcceleration;
    private float currentSpeed;
    
    // 参数范围
    private const float MIN_MAX_SPEED = 0;
    private const float MAX_MAX_SPEED = 50f;
    private const float MIN_ACCELERATION = 0;
    private const float MAX_ACCELERATION = 50f;
    
    // GUI窗口设置
    private Rect windowRect = new Rect(50, 50, 500, 400);
    private bool isWindowVisible = false;
    private int windowId = 12345; // 唯一窗口ID

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    /// <summary>
    /// 显示调试面板
    /// </summary>
    public void Show()
    {
        RefreshKCCController();
        
        if (kccController != null)
        {
            // 保存原始值
            originalMaxAirMoveSpeed = kccController.AirMovementData.MaxAirMoveSpeed;
            originalAirAccelerationSpeed = kccController.AirMovementData.AirAccelerationSpeed;
            
            // 设置当前值
            currentMaxSpeed = originalMaxAirMoveSpeed;
            currentAcceleration = originalAirAccelerationSpeed;
            
            isWindowVisible = true;
        }
        else
        {
            LoggerUtils.LogError("KCCDebugPanel: KCC Controller not found!");
        }
    }

    /// <summary>
    /// 隐藏调试面板
    /// </summary>
    public void Hide()
    {
        isWindowVisible = false;
    }

    private void Update()
    {
        if (kccController != null)
        {
            // 实时更新当前速度
            if (AvatarController.Inst != null && AvatarController.Inst.SelfController != null)
            {
                var motor = AvatarController.Inst.SelfController.Motor;
                if (motor != null)
                {
                    currentSpeed = motor.Velocity.magnitude;
                }
            }
            
            // 同步KCC的值到GUI（防止外部修改）
            if (kccController.AirMovementData.MaxAirMoveSpeed != currentMaxSpeed)
            {
                currentMaxSpeed = kccController.AirMovementData.MaxAirMoveSpeed;
            }
            if (kccController.AirMovementData.AirAccelerationSpeed != currentAcceleration)
            {
                currentAcceleration = kccController.AirMovementData.AirAccelerationSpeed;
            }
        }
    }

    private void OnGUI()
    {
        if (!isWindowVisible || kccController == null)
            return;

        // 设置GUI样式 - 调大字体
        GUI.skin.window.fontSize = 20;
        GUI.skin.label.fontSize = 18;
        GUI.skin.button.fontSize = 18;
        
        // 绘制窗口
        windowRect = GUILayout.Window(windowId, windowRect, DrawWindow, "KCC调试面板", GUILayout.Width(500), GUILayout.Height(400));
        
        // 限制窗口在屏幕内
        windowRect.x = Mathf.Clamp(windowRect.x, 0, Screen.width - windowRect.width);
        windowRect.y = Mathf.Clamp(windowRect.y, 0, Screen.height - windowRect.height);
    }

    private void DrawWindow(int windowID)
    {
        GUILayout.BeginVertical(GUILayout.ExpandHeight(true));
        
        // 标题
        GUILayout.Space(10);
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 25;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        GUILayout.Label("角色移动参数调试", titleStyle);
        GUILayout.Space(10);
        
        // 分隔线
        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
        GUILayout.Space(10);
        
        // 当前速度显示（只读）
        GUILayout.BeginHorizontal();
        GUILayout.Label("当前速度:", GUILayout.Width(120));
        GUIStyle speedStyle = new GUIStyle(GUI.skin.label);
        speedStyle.fontSize = 18;
        speedStyle.normal.textColor = Color.cyan;
        speedStyle.fontStyle = FontStyle.Bold;
        GUILayout.Label($"{currentSpeed:F2} m/s", speedStyle);
        GUILayout.EndHorizontal();
        
        GUILayout.Space(15);
        
        // 最大速度调节
        GUILayout.BeginVertical("box");
        GUILayout.Label($"最大速度: {currentMaxSpeed:F2} m/s", GUILayout.Width(450));
        float newMaxSpeed = GUILayout.HorizontalSlider(currentMaxSpeed, MIN_MAX_SPEED, MAX_MAX_SPEED, GUILayout.Width(450), GUILayout.Height(20));
        if (Mathf.Abs(newMaxSpeed - currentMaxSpeed) > 0.01f)
        {
            currentMaxSpeed = newMaxSpeed;
            OnMaxSpeedChanged(currentMaxSpeed);
        }
        
        GUILayout.BeginHorizontal();
        GUILayout.Label($"{MIN_MAX_SPEED:F1}", GUILayout.Width(60));
        GUILayout.FlexibleSpace();
        GUILayout.Label($"{MAX_MAX_SPEED:F1}", GUILayout.Width(60));
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();
        
        GUILayout.Space(10);
        
        // 加速度调节
        GUILayout.BeginVertical("box");
        GUILayout.Label($"加速度: {currentAcceleration:F2} m/s²", GUILayout.Width(450));
        float newAcceleration = GUILayout.HorizontalSlider(currentAcceleration, MIN_ACCELERATION, MAX_ACCELERATION, GUILayout.Width(450), GUILayout.Height(20));
        if (Mathf.Abs(newAcceleration - currentAcceleration) > 0.01f)
        {
            currentAcceleration = newAcceleration;
            OnAccelerationChanged(currentAcceleration);
        }
        
        GUILayout.BeginHorizontal();
        GUILayout.Label($"{MIN_ACCELERATION:F1}", GUILayout.Width(60));
        GUILayout.FlexibleSpace();
        GUILayout.Label($"{MAX_ACCELERATION:F1}", GUILayout.Width(60));
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();
        
        GUILayout.Space(15);
        
        // 按钮区域
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("重置", GUILayout.Height(40), GUILayout.Width(150)))
        {
            OnResetClick();
        }
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("关闭", GUILayout.Height(40), GUILayout.Width(150)))
        {
            OnCloseClick();
        }
        GUILayout.EndHorizontal();
        
        GUILayout.EndVertical();
        
        // 使窗口可拖动
        GUI.DragWindow();
    }

    private void RefreshKCCController()
    {
        if (AvatarController.Inst != null && AvatarController.Inst.SelfController != null)
        {
            kccController = AvatarController.Inst.SelfController.CurIKCController;
        }
        else
        {
            kccController = null;
        }
    }

    private void OnMaxSpeedChanged(float value)
    {
        if (kccController != null)
        {
            kccController.AirMovementData.MaxAirMoveSpeed = value;
        }
    }

    private void OnAccelerationChanged(float value)
    {
        if (kccController != null)
        {
            kccController.AirMovementData.AirAccelerationSpeed = value;
        }
    }

    private void OnResetClick()
    {
        if (kccController != null)
        {
            kccController.AirMovementData.MaxAirMoveSpeed = originalMaxAirMoveSpeed;
            kccController.AirMovementData.AirAccelerationSpeed = originalAirAccelerationSpeed;
            
            currentMaxSpeed = originalMaxAirMoveSpeed;
            currentAcceleration = originalAirAccelerationSpeed;
        }
    }

    private void OnCloseClick()
    {
        Hide();
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}

