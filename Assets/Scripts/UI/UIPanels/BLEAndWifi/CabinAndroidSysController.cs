using System;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// BOX Android 系统层控制器。
/// 封装系统音量、屏幕亮度、静音、休眠时间的 SDK 调用，
/// 以及物理旋钮按钮事件的接收。
/// 与 Android 交互通过 MobileInterface，遵循 BleHardwareSideController 模式：
///   发送：MobileInterface.Instance.SendMessage(funcName, json)
///   接收：MobileInterface.Instance.AddClientRespose / AddClientFail
/// </summary>
public static class CabinAndroidSysController
{
    #region Android 方法名常量

    // 调用 SDK
    private const string FnSetSysVolume = "setSysVolumn";
    private const string FnSetScreenBrightness = "setScreenBrightness";
    private const string FnSetVolumeMute = "setVolumnMute";
    private const string FnSetScreenSleepTime = "setScreenSleepTime";
    private const string FnGetSysBaseMsg = "getSysBaseMsg";

    // 持久事件（Android 主动推送到 Unity）
    private const string CbOnClickBtnDown = "onClickBtnDown";
    private const string CbOnRotateBtn = "onRotateBtn";

    static CabinHardWareBtnController cabinHardWareBtnController = new();

    /// <summary>
    /// 隐式驱动器：保证 CabinHardWareBtnController.Tick 每帧被调用，
    /// 用于超时确认单击。EnsureEventsRegistered 时自动创建。
    /// </summary>
    private class TickDriver : MonoBehaviour
    {
        private void Update() => cabinHardWareBtnController.Tick();
    }

    #endregion

    #region C# Events

    /// <summary>收到一次物理按钮按下事件（原始单次）</summary>
    public static event Action OnClickBtnDown;

    /// <summary>双击事件（两次按下间隔 ≤ 0.3s）</summary>
    public static event Action OnDoubleClickBtnDown;

    /// <summary>收到物理按钮旋转事件（上一次的值, 当前的值）</summary>
    public static event Action<int, int> OnRotateBtn;

    #endregion

    #region 持久事件注册

    private static bool _eventsRegistered;

    /// <summary>注册持久推送回调，所有 API 内部自动调用，业务层无需手动调用。</summary>
    public static void EnsureEventsRegistered()
    {
        if (_eventsRegistered) return;
        _eventsRegistered = true;

        // 接通 CabinHardWareBtnController 的双击事件
        cabinHardWareBtnController.OnDoubleClick += () => OnDoubleClickBtnDown?.Invoke();

        // 创建驱动器 GameObject，保证 Tick 每帧执行（用于超时单击确认）
        var go = new GameObject("[CabinAndroidSysController.TickDriver]");
        UnityEngine.Object.DontDestroyOnLoad(go);
        go.AddComponent<TickDriver>();

        MobileInterface.Instance.AddClientRespose(CbOnClickBtnDown, _ =>
        {
            OnClickBtnDown?.Invoke();
            cabinHardWareBtnController.OnClickBtnDown();
        });

        MobileInterface.Instance.AddClientRespose(CbOnRotateBtn, data =>
        {
            try
            {
                var d = JsonConvert.DeserializeAnonymousType(data, new { prev = 0, curr = 0 });
                OnRotateBtn?.Invoke(d.prev, d.curr);
                cabinHardWareBtnController.OnRotateBtn(d.prev,d.curr);
            }
            catch (Exception e)
            {
                Debug.LogError($"[CabinAndroidSysController] {CbOnRotateBtn} parse err: {e.Message}");
            }
        });
    }

    #endregion

    #region 数据模型

    public class SysBaseMsg
    {
        public int brightness;
        public int volume;
        public bool isMute;
        public int sleepTime;
    }

    #endregion

    #region API

    /// <summary>设置系统音量（0~100）</summary>
    public static void SetSysVolume(int volume, Action onDone = null)
    {
        EnsureEventsRegistered();
        MobileInterface.Instance.AddClientRespose(FnSetSysVolume, _ =>
        {
            MobileInterface.Instance.DelClientResponse(FnSetSysVolume);
            MobileInterface.Instance.DelClientFail(FnSetSysVolume);
            onDone?.Invoke();
        });
        MobileInterface.Instance.AddClientFail(FnSetSysVolume, _ =>
        {
            MobileInterface.Instance.DelClientResponse(FnSetSysVolume);
            MobileInterface.Instance.DelClientFail(FnSetSysVolume);
            onDone?.Invoke();
        });
        MobileInterface.Instance.SendMessage(FnSetSysVolume, JsonConvert.SerializeObject(new { volume }));
        Debug.Log($"[CabinAndroidSysController] SetSysVolume volume={volume}");
    }

    /// <summary>设置屏幕亮度（0~100）</summary>
    public static void SetScreenBrightness(int brightness, Action onDone = null)
    {
        EnsureEventsRegistered();
        MobileInterface.Instance.AddClientRespose(FnSetScreenBrightness, _ =>
        {
            MobileInterface.Instance.DelClientResponse(FnSetScreenBrightness);
            MobileInterface.Instance.DelClientFail(FnSetScreenBrightness);
            onDone?.Invoke();
        });
        MobileInterface.Instance.AddClientFail(FnSetScreenBrightness, _ =>
        {
            MobileInterface.Instance.DelClientResponse(FnSetScreenBrightness);
            MobileInterface.Instance.DelClientFail(FnSetScreenBrightness);
            onDone?.Invoke();
        });
        MobileInterface.Instance.SendMessage(FnSetScreenBrightness, JsonConvert.SerializeObject(new { brightness }));
        Debug.Log($"[CabinAndroidSysController] SetScreenBrightness brightness={brightness}");
    }

    /// <summary>设置是否静音</summary>
    public static void SetVolumeMute(bool isMute, Action onDone = null)
    {
        EnsureEventsRegistered();
        MobileInterface.Instance.AddClientRespose(FnSetVolumeMute, _ =>
        {
            MobileInterface.Instance.DelClientResponse(FnSetVolumeMute);
            MobileInterface.Instance.DelClientFail(FnSetVolumeMute);
            onDone?.Invoke();
        });
        MobileInterface.Instance.AddClientFail(FnSetVolumeMute, _ =>
        {
            MobileInterface.Instance.DelClientResponse(FnSetVolumeMute);
            MobileInterface.Instance.DelClientFail(FnSetVolumeMute);
            onDone?.Invoke();
        });
        MobileInterface.Instance.SendMessage(FnSetVolumeMute, JsonConvert.SerializeObject(new { isMute }));
        Debug.Log($"[CabinAndroidSysController] SetVolumeMute isMute={isMute}");
    }

    /// <summary>设置休眠时间（分钟，-1 为不熄屏）</summary>
    public static void SetScreenSleepTime(int sleepTime, Action onDone = null)
    {
        EnsureEventsRegistered();
        MobileInterface.Instance.AddClientRespose(FnSetScreenSleepTime, _ =>
        {
            MobileInterface.Instance.DelClientResponse(FnSetScreenSleepTime);
            MobileInterface.Instance.DelClientFail(FnSetScreenSleepTime);
            onDone?.Invoke();
        });
        MobileInterface.Instance.AddClientFail(FnSetScreenSleepTime, _ =>
        {
            MobileInterface.Instance.DelClientResponse(FnSetScreenSleepTime);
            MobileInterface.Instance.DelClientFail(FnSetScreenSleepTime);
            onDone?.Invoke();
        });
        MobileInterface.Instance.SendMessage(FnSetScreenSleepTime, JsonConvert.SerializeObject(new { sleepTime }));
        Debug.Log($"[CabinAndroidSysController] SetScreenSleepTime sleepTime={sleepTime}");
    }

    /// <summary>获取系统基础信息（brightness / volume / isMute / sleepTime）</summary>
    public static void GetSysBaseMsg(Action<SysBaseMsg> onSuccess, Action<string> onError = null)
    {
        EnsureEventsRegistered();
        MobileInterface.Instance.AddClientRespose(FnGetSysBaseMsg, data =>
        {
            MobileInterface.Instance.DelClientResponse(FnGetSysBaseMsg);
            MobileInterface.Instance.DelClientFail(FnGetSysBaseMsg);
            try
            {
                var msg = JsonConvert.DeserializeObject<SysBaseMsg>(data);
                onSuccess?.Invoke(msg);
            }
            catch (Exception e)
            {
                Debug.LogError($"[CabinAndroidSysController] {FnGetSysBaseMsg} parse err: {e.Message}");
                onError?.Invoke(e.Message);
            }
        });
        MobileInterface.Instance.AddClientFail(FnGetSysBaseMsg, data =>
        {
            MobileInterface.Instance.DelClientResponse(FnGetSysBaseMsg);
            MobileInterface.Instance.DelClientFail(FnGetSysBaseMsg);
            onError?.Invoke(data);
        });
        MobileInterface.Instance.SendMessage(FnGetSysBaseMsg, "");
        Debug.Log("[CabinAndroidSysController] GetSysBaseMsg");
    }

    #endregion
}
