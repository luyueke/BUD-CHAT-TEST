using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Game.BLE
{
    #region Data Models

    public class WifiConfig
    {
        public string ssid;
        public string password;
        /// <summary>OPEN / WEP / WPA / WPA2 / WPA3</summary>
        public string securityType;
    }

    /// <summary>
    /// 硬件设备通过 BLE 主动推送的消息结构（对应 onDataReceived 回调）。
    /// 示例 JSON：{"type":"doubleclick","data":{}}
    /// </summary>
    public class BleDeviceMessage
    {
        /// <summary>消息类型，见 <see cref="BleDeviceMessageType"/></summary>
        public string type;
        /// <summary>附加数据（可选，字符串/对象/数组均可）</summary>
        public JToken data;
    }

    /// <summary>BLE 设备消息类型常量</summary>
    public static class BleDeviceMessageType
    {
        /// <summary>硬件旋钮双击确认</summary>
        public const string DoubleClick = "doubleclick";
    }

    #endregion

    /// <summary>
    /// 软件侧（BLE 客户端）控制器。
    ///
    /// 发送：通过 MobileInterface.Instance.SendMessage 调用 Android。
    /// 接收：通过 MobileInterface.Instance.AddClientRespose 常驻监听，
    ///       与硬件侧保持一致的回调机制。
    /// 持久监听（EnsureEventsRegistered）在首次调用时注册，不会自动移除。
    /// </summary>
    public static class BleSoftwareSideController
    {
        #region Android 方法名常量

        private const string FnScanAndConnectDevice      = "scanAndConnectDevice";
        private const string FnSendDataToDevice          = "sendDataToDevice";
        private const string FnSendWifiConfig            = "sendWifiConfig";
        private const string FnRequestWifiList           = "requestWifiList";
        private const string FnDisconnectDevice          = "disconnectDevice";
        private const string FnIsDeviceConnected         = "isDeviceConnected";
        private const string FnRequestLocationPermission = "requestLocationPermission";
        private const string FnCheckLocationStatus       = "checkLocationStatus";
        private const string FnIsBluetoothEnabled        = "isBluetoothEnabled";
        private const string FnRequestEnableBluetooth    = "requestEnableBluetooth";
        private const string FnDisableBluetooth = "disableBluetooth";
        private const string FnRequestBlePermissions     = "requestBlePermissions";
        private const string FnRequestCameraPermission     = "requestCameraPermission";
        private const string FnRequestMicrophonePermission = "requestMicrophonePermission";
        private const string FnStartNativeCameraQR       = "startNativeCameraQR";
        private const string FnStopNativeCameraQR        = "stopNativeCameraQR";
        private const string FnCheckWifiUsable           = "checkWifiUsable";
        private const string FnRequestEnableWifi         = "requestEnableWifi";
        private const string CbOnDataReceived            = "onDataReceived";

        #endregion

        #region C# Events（供业务层订阅）

        /// <summary>收到硬件设备发送的业务数据（包括 WiFi 列表等异步响应）</summary>
        public static event Action<string> OnDataReceived;

        #endregion

        #region 内部待处理回调（每次调用前写入，回调触发后清空）

        private static Action<bool, string>             _pendingScanConnect;
        private static Action<bool, string>             _pendingSendData;
        private static Action<bool, string>             _pendingSendWifiConfig;
        private static Action<bool, string>             _pendingRequestWifiList;
        private static Action<bool, string>             _pendingDisconnect;
        private static Action<bool>                     _pendingIsConnected;
        private static Action<LocationPermissionResult> _pendingRequestLocation;
        private static Action<LocationStatus>           _pendingCheckLocation;
        private static Action<bool>                     _pendingIsBluetoothEnabled;
        private static Action<bool>                     _pendingRequestEnableBluetooth;
        private static Action<bool>                     _pendingRequestBlePermissions;
        private static Action<bool>                     _pendingRequestCameraPermission;
        private static Action<bool>                     _pendingRequestMicrophonePermission;
        private static Action<bool, string>             _pendingNativeCameraQR;
        private static Action<bool>                     _pendingCheckWifiUsable;
        private static Action<bool>                     _pendingRequestEnableWifi;

        #endregion

        #region 持久事件注册

        private static bool _eventsRegistered;

        /// <summary>
        /// 注册所有持久回调监听（每个 funcName 只注册一次）。
        /// 所有 API 方法内部会自动调用，业务层无需手动调用。
        /// </summary>
        public static void EnsureEventsRegistered()
        {
            if (_eventsRegistered) return;
            _eventsRegistered = true;

            // ── 一次性请求回调（常驻监听，用 _pending* 字段传递具体 Action）──

            MobileInterface.Instance.AddClientRespose(FnScanAndConnectDevice, data =>
            {
                var cb = _pendingScanConnect; _pendingScanConnect = null;
                cb?.Invoke(true, data);
            });
            MobileInterface.Instance.AddClientFail(FnScanAndConnectDevice, data =>
            {
                var cb = _pendingScanConnect; _pendingScanConnect = null;
                cb?.Invoke(false, data);
            });

            MobileInterface.Instance.AddClientRespose(FnSendDataToDevice, data =>
            {
                var cb = _pendingSendData; _pendingSendData = null;
                cb?.Invoke(true, data);
            });
            MobileInterface.Instance.AddClientFail(FnSendDataToDevice, data =>
            {
                var cb = _pendingSendData; _pendingSendData = null;
                cb?.Invoke(false, data);
            });

            MobileInterface.Instance.AddClientRespose(FnSendWifiConfig, data =>
            {
                var cb = _pendingSendWifiConfig; _pendingSendWifiConfig = null;
                cb?.Invoke(true, data);
            });
            MobileInterface.Instance.AddClientFail(FnSendWifiConfig, data =>
            {
                var cb = _pendingSendWifiConfig; _pendingSendWifiConfig = null;
                cb?.Invoke(false, data);
            });

            MobileInterface.Instance.AddClientRespose(FnRequestWifiList, data =>
            {
                var cb = _pendingRequestWifiList; _pendingRequestWifiList = null;
                cb?.Invoke(true, data);
            });
            MobileInterface.Instance.AddClientFail(FnRequestWifiList, data =>
            {
                var cb = _pendingRequestWifiList; _pendingRequestWifiList = null;
                cb?.Invoke(false, data);
            });

            MobileInterface.Instance.AddClientRespose(FnDisconnectDevice, data =>
            {
                var cb = _pendingDisconnect; _pendingDisconnect = null;
                cb?.Invoke(true, data);
            });
            MobileInterface.Instance.AddClientFail(FnDisconnectDevice, data =>
            {
                var cb = _pendingDisconnect; _pendingDisconnect = null;
                cb?.Invoke(false, data);
            });

            MobileInterface.Instance.AddClientRespose(FnIsDeviceConnected, data =>
            {
                var cb = _pendingIsConnected; _pendingIsConnected = null;
                if (cb == null) return;
                try
                {
                    var d = JsonConvert.DeserializeAnonymousType(data, new { isConnected = false });
                    cb.Invoke(d.isConnected);
                }
                catch { cb.Invoke(false); }
            });
            MobileInterface.Instance.AddClientFail(FnIsDeviceConnected, data =>
            {
                var cb = _pendingIsConnected; _pendingIsConnected = null;
                cb?.Invoke(false);
            });

            MobileInterface.Instance.AddClientRespose(FnRequestLocationPermission, data =>
            {
                var cb = _pendingRequestLocation; _pendingRequestLocation = null;
                if (cb == null) return;
                try { cb.Invoke(JsonConvert.DeserializeObject<LocationPermissionResult>(data)); }
                catch { cb.Invoke(new LocationPermissionResult { granted = false, message = "解析失败" }); }
            });
            MobileInterface.Instance.AddClientFail(FnRequestLocationPermission, data =>
            {
                var cb = _pendingRequestLocation; _pendingRequestLocation = null;
                cb?.Invoke(new LocationPermissionResult { granted = false, message = data });
            });

            MobileInterface.Instance.AddClientRespose(FnCheckLocationStatus, data =>
            {
                var cb = _pendingCheckLocation; _pendingCheckLocation = null;
                if (cb == null) return;
                try { cb.Invoke(JsonConvert.DeserializeObject<LocationStatus>(data)); }
                catch { cb.Invoke(new LocationStatus()); }
            });
            MobileInterface.Instance.AddClientFail(FnCheckLocationStatus, data =>
            {
                var cb = _pendingCheckLocation; _pendingCheckLocation = null;
                cb?.Invoke(new LocationStatus());
            });

            MobileInterface.Instance.AddClientRespose(FnIsBluetoothEnabled, data =>
            {
                var cb = _pendingIsBluetoothEnabled; _pendingIsBluetoothEnabled = null;
                if (cb == null) return;
                try
                {
                    var d = JsonConvert.DeserializeAnonymousType(data, new { enabled = false });
                    cb.Invoke(d.enabled);
                }
                catch { cb.Invoke(false); }
            });
            MobileInterface.Instance.AddClientFail(FnIsBluetoothEnabled, data =>
            {
                var cb = _pendingIsBluetoothEnabled; _pendingIsBluetoothEnabled = null;
                cb?.Invoke(false);
            });

            MobileInterface.Instance.AddClientRespose(FnRequestEnableBluetooth, data =>
            {
                var cb = _pendingRequestEnableBluetooth; _pendingRequestEnableBluetooth = null;
                if (cb == null) return;
                try
                {
                    var d = JsonConvert.DeserializeAnonymousType(data, new { enabled = false });
                    cb.Invoke(d.enabled);
                }
                catch { cb.Invoke(false); }
            });
            MobileInterface.Instance.AddClientFail(FnRequestEnableBluetooth, data =>
            {
                var cb = _pendingRequestEnableBluetooth; _pendingRequestEnableBluetooth = null;
                cb?.Invoke(false);
            });

            MobileInterface.Instance.AddClientRespose(FnRequestBlePermissions, data =>
            {
                var cb = _pendingRequestBlePermissions; _pendingRequestBlePermissions = null;
                if (cb == null) return;
                try
                {
                    var d = JsonConvert.DeserializeAnonymousType(data, new { granted = false });
                    cb.Invoke(d.granted);
                }
                catch { cb.Invoke(false); }
            });
            MobileInterface.Instance.AddClientFail(FnRequestBlePermissions, data =>
            {
                var cb = _pendingRequestBlePermissions; _pendingRequestBlePermissions = null;
                cb?.Invoke(false);
            });

            MobileInterface.Instance.AddClientRespose(FnRequestCameraPermission, data =>
            {
                var cb = _pendingRequestCameraPermission; 
                if (cb == null) return;
                try
                {
                    var d = JsonConvert.DeserializeAnonymousType(data, new { granted = false });
                    cb.Invoke(d.granted);
                    _pendingRequestCameraPermission = null;
                }
                catch { 
                    cb.Invoke(false);
                    _pendingRequestCameraPermission = null;
                }
            });
            MobileInterface.Instance.AddClientFail(FnRequestCameraPermission, data =>
            {
                var cb = _pendingRequestCameraPermission;
                cb?.Invoke(false);
                _pendingRequestCameraPermission = null;
            });

            MobileInterface.Instance.AddClientRespose(FnRequestMicrophonePermission, data =>
            {
                var cb = _pendingRequestMicrophonePermission;
                if (cb == null) return;
                try
                {
                    var d = JsonConvert.DeserializeAnonymousType(data, new { granted = false });
                    cb.Invoke(d.granted);
                    _pendingRequestMicrophonePermission = null;
                }
                catch
                {
                    cb.Invoke(false);
                    _pendingRequestMicrophonePermission = null;
                }
            });
            MobileInterface.Instance.AddClientFail(FnRequestMicrophonePermission, data =>
            {
                var cb = _pendingRequestMicrophonePermission;
                cb?.Invoke(false);
                _pendingRequestMicrophonePermission = null;
            });

            MobileInterface.Instance.AddClientRespose(FnCheckWifiUsable, data =>
            {
                var cb = _pendingCheckWifiUsable; _pendingCheckWifiUsable = null;
                if (cb == null) return;
                try
                {
                    var d = JsonConvert.DeserializeAnonymousType(data, new { isWifiUsable = false });
                    cb.Invoke(d.isWifiUsable);
                }
                catch { cb.Invoke(false); }
            });
            MobileInterface.Instance.AddClientFail(FnCheckWifiUsable, data =>
            {
                var cb = _pendingCheckWifiUsable; _pendingCheckWifiUsable = null;
                cb?.Invoke(false);
            });

            MobileInterface.Instance.AddClientRespose(FnRequestEnableWifi, data =>
            {
                var cb = _pendingRequestEnableWifi; _pendingRequestEnableWifi = null;
                if (cb == null) return;
                try
                {
                    var d = JsonConvert.DeserializeAnonymousType(data, new { requested = false });
                    cb.Invoke(d.requested);
                }
                catch { cb.Invoke(false); }
            });
            MobileInterface.Instance.AddClientFail(FnRequestEnableWifi, data =>
            {
                var cb = _pendingRequestEnableWifi; _pendingRequestEnableWifi = null;
                cb?.Invoke(false);
            });

            MobileInterface.Instance.AddClientRespose(FnStartNativeCameraQR, data =>
            {
                var cb = _pendingNativeCameraQR; _pendingNativeCameraQR = null;
                if (cb == null) return;
                try
                {
                    var d = JsonConvert.DeserializeAnonymousType(data, new { qrData = "" });
                    cb.Invoke(true, d.qrData ?? "");
                }
                catch { cb.Invoke(false, ""); }
            });
            MobileInterface.Instance.AddClientFail(FnStartNativeCameraQR, data =>
            {
                var cb = _pendingNativeCameraQR; _pendingNativeCameraQR = null;
                cb?.Invoke(false, "");
            });

            // ── 持久事件：硬件侧主动推送 ──

            MobileInterface.Instance.AddClientRespose(CbOnDataReceived, data =>
            {
                OnDataReceived?.Invoke(data);
            });
        }

        #endregion

        #region 3.1 BLE 客户端核心功能

        /// <summary>
        /// 扫描并连接硬件设备。
        /// 内部先检查位置权限（BLE 扫描在 Android 上需要位置权限）。
        /// </summary>
        public static void ScanAndConnectDevice(string qrCodeJson, Action<bool, string> cb = null, bool isReconnect = false)
        {
            EnsureEventsRegistered();
            CheckLocationStatus(status =>
            {
                Debug.Log("test检查locationStatus:" + JsonConvert.SerializeObject(status));
                if (!status.permissionGranted)
                {
                    RequestLocationPermission(result =>
                    {
                        if (!result.granted)
                        {
                            cb?.Invoke(false, result.message ?? "位置权限被拒绝，BLE扫描功能不可用");
                            return;
                        }
                        if (!result.locationEnabled)
                            LoggerUtils.Log("[BleSoftwareSideController] 位置权限已授予，但位置服务未开启，扫描可能受限");
                        DoScanAndConnect(qrCodeJson, cb, isReconnect);
                    });
                }
                else
                {
                    if (!status.locationEnabled)
                        LoggerUtils.Log("[BleSoftwareSideController] 位置服务未开启，扫描可能受限");
                    DoScanAndConnect(qrCodeJson, cb, isReconnect);
                }
            });
        }

        public const string PlayerPrefsKeyQrCodeJson = "BLE_LastQrCodeJson";

        private static void DoScanAndConnect(string qrCodeJson, Action<bool, string> cb, bool isReconnect = false)
        {
            _pendingScanConnect = (ok, data) =>
            {
                if (ok)
                {
                    PlayerPrefs.SetString(PlayerPrefsKeyQrCodeJson, qrCodeJson);
                    PlayerPrefs.Save();
                }
                cb?.Invoke(ok, data);
            };
            // 按协议包一层 qrCodeData + isReconnect
            string wrappedJson = $"{{\"qrCodeData\":{qrCodeJson},\"isReconnect\":{isReconnect.ToString().ToLower()}}}";
            Debug.Log("test DoScanAndConnect");
            MobileInterface.Instance.SendMessage(FnScanAndConnectDevice, wrappedJson);
        }

        /// <summary>请求位置权限并返回授权结果与位置服务状态</summary>
        public static void RequestLocationPermission(Action<LocationPermissionResult> cb)
        {
            EnsureEventsRegistered();
            _pendingRequestLocation = cb;
            MobileInterface.Instance.SendMessage(FnRequestLocationPermission, "");
        }

        /// <summary>仅查询位置权限和位置服务状态，不弹窗</summary>
        public static void CheckLocationStatus(Action<LocationStatus> cb)
        {
            EnsureEventsRegistered();
            _pendingCheckLocation = cb;
            MobileInterface.Instance.SendMessage(FnCheckLocationStatus, "");
        }

        /// <summary>发送数据到硬件设备（需已连接）</summary>
        public static void SendDataToDevice(string data, Action<bool, string> cb = null)
        {
            EnsureEventsRegistered();
            _pendingSendData = cb;
            MobileInterface.Instance.SendMessage(FnSendDataToDevice, data);
        }

        static WifiConfig _curSendWifiConfig;//记录当前发送给硬件连接的wifi设备

        /// <summary>发送 WiFi 配置到硬件设备（需已连接）</summary>
        public static void SendWifiConfig(WifiConfig config, Action<bool, string> cb = null)
        {
            EnsureEventsRegistered();
            _pendingSendWifiConfig = cb;
            MobileInterface.Instance.SendMessage(FnSendWifiConfig, JsonConvert.SerializeObject(config));
        }

        /// <summary>请求硬件设备扫描 WiFi 列表（需已连接）。结果通过 OnDataReceived 异步返回</summary>
        public static void RequestWifiList(Action<bool, string> cb = null)
        {
            EnsureEventsRegistered();
            _pendingRequestWifiList = cb;
            MobileInterface.Instance.SendMessage(FnRequestWifiList, "");
        }

        /// <summary>断开与硬件设备的连接</summary>
        public static void DisconnectDevice(Action<bool, string> cb = null)
        {
            EnsureEventsRegistered();
            _pendingDisconnect = cb;
            MobileInterface.Instance.SendMessage(FnDisconnectDevice, "");
        }

        /// <summary>检查当前是否已连接到目标设备。cb(true) = 已连接</summary>
        public static void IsDeviceConnected(Action<bool> cb)
        {
            EnsureEventsRegistered();
            _pendingIsConnected = cb;
            MobileInterface.Instance.SendMessage(FnIsDeviceConnected, "");
        }

        /// <summary>检查蓝牙是否已开启。cb(true) = 已开启</summary>
        public static void IsBluetoothEnabled(Action<bool> cb)
        {
            EnsureEventsRegistered();
            _pendingIsBluetoothEnabled = cb;
            MobileInterface.Instance.SendMessage(FnIsBluetoothEnabled, "");
        }

        /// <summary>请求开启蓝牙。cb(true) = 开启成功</summary>
        public static void RequestEnableBluetooth(Action<bool> cb)
        {
            EnsureEventsRegistered();
            _pendingRequestEnableBluetooth = cb;
            MobileInterface.Instance.SendMessage(FnRequestEnableBluetooth, "");
        }

        /// <summary>关闭蓝牙。cb(true) = 蓝牙已成功关闭</summary>
        public static void DisableBluetooth()
        {
            EnsureEventsRegistered();
            MobileInterface.Instance.SendMessage(FnDisableBluetooth, "");
        }

        /// <summary>请求蓝牙权限（Android 12+ 必需）。cb(true) = 已授权</summary>
        public static void RequestBlePermissions(Action<bool> cb)
        {
            EnsureEventsRegistered();
            _pendingRequestBlePermissions = cb;
            MobileInterface.Instance.SendMessage(FnRequestBlePermissions, "");
        }

        /// <summary>启动原生 QR 扫码器（iOS 专用）。cb(true, qrText) = 扫码成功；cb(false, "") = 取消/失败</summary>
        public static void StartNativeCameraQR(Action<bool, string> cb)
        {
            EnsureEventsRegistered();
            _pendingNativeCameraQR = cb;
            MobileInterface.Instance.SendMessage(FnStartNativeCameraQR, "");
        }

        /// <summary>关闭原生 QR 扫码器（iOS 专用）</summary>
        public static void StopNativeCameraQR()
        {
            _pendingNativeCameraQR = null;
            MobileInterface.Instance.SendMessage(FnStopNativeCameraQR, "");
        }

        /// <summary>请求相机权限。cb(true) = 已授权</summary>
        public static void RequestCameraPermission(Action<bool> cb)
        {
            EnsureEventsRegistered();
            _pendingRequestCameraPermission = cb;
            MobileInterface.Instance.SendMessage(FnRequestCameraPermission, "");
        }

        /// <summary>请求麦克风权限。cb(true) = 已授权</summary>
        public static void RequestMicrophonePermission(Action<bool> cb)
        {
            EnsureEventsRegistered();
            _pendingRequestMicrophonePermission = cb;
            MobileInterface.Instance.SendMessage(FnRequestMicrophonePermission, "");
        }

        /// <summary>检查 WiFi 是否可用。cb(true) = 可用</summary>
        public static void CheckWifiUsable(Action<bool> cb)
        {
            EnsureEventsRegistered();
            _pendingCheckWifiUsable = cb;
            MobileInterface.Instance.SendMessage(FnCheckWifiUsable, "");
        }

        /// <summary>
        /// 请求开启 WiFi。
        /// Android 10+ 弹出系统 WiFi 设置面板；Android 9 及以下直接启用。
        /// cb(true) = 已发起请求（不代表用户一定开启）
        /// </summary>
        public static void RequestEnableWifi(Action<bool> cb)
        {
            EnsureEventsRegistered();
            _pendingRequestEnableWifi = cb;
            MobileInterface.Instance.SendMessage(FnRequestEnableWifi, "");
        }

        #endregion
    }
}
