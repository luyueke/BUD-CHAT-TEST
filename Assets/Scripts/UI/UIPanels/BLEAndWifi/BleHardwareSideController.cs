using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Game.BLE
{
    #region Data Models

    public class QrCodeData
    {
        public int version;
        public string transport;
        public string deviceId;
        public string serviceUuid;
        public string advHint;
        public string bindToken;
        public long expireAt;
        public string signature;
    }

    /// <summary>
    /// 映射QrCodeData
    /// </summary>
    public class QrEasyCodeData
    {
        public int a;
        public string b;
        public string c;
        public string d;
        public string e;
        public string f;
        public long g;
        public string h;
    }


    public class BleBindingStatus
    {
        public bool isRunning;
        public bool isAdvertising;
        public string deviceId;
        public int connectedCount;
        public int boundCount;
        public List<BoundDevice> boundDevices;
    }

    public class BoundDevice
    {
        public string macAddress;
        public string clientDeviceId;
    }

    public class BleBindSuccessData
    {
        public string deviceId;
        public string clientDeviceId;
        public long timestamp;
    }

    public class BleBindFailedData
    {
        public int errorCode;
        public string errorMessage;
        public long timestamp;
    }

    public class BleConnectionStateData
    {
        public string deviceAddress;
        public bool isConnected;
        public long timestamp;
    }

    public class WifiConnectResult
    {
        public int resultCode;
        public string message;
        public string ssid;
        public long timestamp;
    }

    public class WifiListResponse
    {
        public int resultCode;
        public string message;
        public long timestamp;
        public List<WifiInfo> wifiList;
    }

    /// <summary>
    /// 硬件单条 Wi-Fi 网络信息，通过 <seecref="MqttMsgOperType.get_wifi"/> 指令从设备获取。
    /// </summary>
    public class WifiInfo
    {
        /// <summary>Wi-Fi 网络名称（SSID）</summary>
        public string ssid;
        /// <summary>接入点 MAC 地址（BSSID），格式如 "AA:BB:CC:DD:EE:FF"</summary>
        public string bssid;
        /// <summary>信号强度（dBm），值越大信号越强，通常为负数，如 -50</summary>
        public int level;
        /// <summary>信道频率（MHz），2.4GHz 约为 2412~2484，5GHz 约为 5180~5825</summary>
        public int frequency;
        /// <summary>安全能力字符串，如 "[WPA2-PSK-CCMP]"，由 Android 系统原始返回</summary>
        public string capabilities;
        /// <summary>安全类型，如 "WPA2"、"OPEN" 等</summary>
        public string securityType;
        /// <summary>是否为当前已连接的 Wi-Fi</summary>
        public bool isConnected;
        public string password;
    }

    /// <summary>requestLocationPermission 回调数据</summary>
    public class LocationPermissionResult
    {
        public bool granted;
        public bool locationEnabled;
        public string message;
    }

    /// <summary>checkLocationStatus 回调数据</summary>
    public class LocationStatus
    {
        public bool permissionGranted;
        public bool locationEnabled;
        public int sdkVersion;
    }

    /// <summary>getConnectedWifiInfo 回调数据</summary>
    public class ConnectedWifiInfo
    {
        public bool connected;
        public string ssid;
        public string bssid;
        public int rssi;
        public int linkSpeed;
        public int frequency;
    }

    #endregion

    /// <summary>
    /// 硬件侧（BLE 服务端）控制器。
    /// 通过 MobileInterface 与 Android 层交互：
    ///   发送：MobileInterface.Instance.SendMessage(funcName, data)
    ///   接收：MobileInterface.Instance.AddClientRespose / AddClientFail（funcName 即方法名）
    /// 持久事件（OnBleBindSuccess 等）在首次调用时注册，不会自动移除。
    /// </summary>
    public static class BleHardwareSideController
    {
        #region Android 方法名常量

        // 2.1 蓝牙基础
        private const string FnIsBluetoothEnabled = "isBluetoothEnabled";
        private const string FnRequestEnableBluetooth = "requestEnableBluetooth";
        private const string FnDisableBluetooth = "disableBluetooth";
        private const string FnRequestBlePermissions = "requestBlePermissions";

        // 2.1.5 位置权限
        private const string FnRequestLocationPermission = "requestLocationPermission";
        private const string FnCheckLocationStatus = "checkLocationStatus";

        // 2.2 服务端核心
        private const string FnGenerateBleQrCode = "generateBleQrCode";
        private const string FnStopBleService = "stopBleService";
        private const string FnGetBleBindingStatus = "getBleBindingStatus";
        private const string FnIsBleServiceRunning = "isBleServiceRunning";
        private const string FnStartBleAdvertising = "startBleAdvertising";
        private const string FnStopBleAdvertising = "stopBleAdvertising";
        private const string FnGetConnectedWifiInfo = "getConnectedWifiInfo";

        // 2.3 服务端事件回调（Android 主动推送，funcName 即回调名）
        private const string CbOnBleBindSuccess = "OnBleBindSuccess";
        private const string CbOnBleBindFailed = "OnBleBindFailed";
        private const string CbOnBleConnectionStateChanged = "OnBleConnectionStateChanged";
        private const string CbOnQrCodeExpired = "OnQrCodeExpired";

        // 2.4 服务端 WiFi 回调
        private const string CbOnWifiConnected = "onWifiConnected";
        private const string CbOnWifiListReceived = "onWifiListReceived";
        private const string CbOnDataReceived = "onDataReceived";

        #endregion

        #region C# Events（供业务层订阅）

        /// <summary>客户端绑定成功</summary>
        public static event Action<BleBindSuccessData> OnBindSuccess;
        /// <summary>客户端绑定失败</summary>
        public static event Action<BleBindFailedData> OnBindFailed;
        /// <summary>连接状态变化</summary>
        public static event Action<BleConnectionStateData> OnConnectionStateChanged;
        /// <summary>二维码过期（生成后 10 分钟）</summary>
        public static event Action OnQrCodeExpired;
        /// <summary>硬件设备返回 WiFi 连接结果</summary>
        public static event Action<WifiConnectResult> OnWifiConnected;
        /// <summary>硬件设备返回 WiFi 列表扫描结果</summary>
        public static event Action<WifiListResponse> OnWifiListReceived;
        /// <summary>收到客户端发送的其他业务数据</summary>
        public static event Action<string> OnDataReceived;

        #endregion

        #region 持久事件注册

        private static bool _eventsRegistered;

        /// <summary>
        /// 注册所有持久事件回调（每个 funcName 只注册一次）。
        /// 所有 API 方法内部会自动调用，业务层无需手动调用。
        /// </summary>
        public static void EnsureEventsRegistered()
        {
            if (_eventsRegistered) return;
            _eventsRegistered = true;

            MobileInterface.Instance.AddClientRespose(CbOnBleBindSuccess, data =>
            {
                try { OnBindSuccess?.Invoke(JsonConvert.DeserializeObject<BleBindSuccessData>(data)); }
                catch (Exception e) { LoggerUtils.LogError($"[BleHardwareSideController] {CbOnBleBindSuccess} parse err: {e.Message}"); }
            });

            MobileInterface.Instance.AddClientRespose(CbOnBleBindFailed, data =>
            {
                try { OnBindFailed?.Invoke(JsonConvert.DeserializeObject<BleBindFailedData>(data)); }
                catch (Exception e) { LoggerUtils.LogError($"[BleHardwareSideController] {CbOnBleBindFailed} parse err: {e.Message}"); }
            });

            MobileInterface.Instance.AddClientRespose(CbOnBleConnectionStateChanged, data =>
            {
                try { OnConnectionStateChanged?.Invoke(JsonConvert.DeserializeObject<BleConnectionStateData>(data)); }
                catch (Exception e) { LoggerUtils.LogError($"[BleHardwareSideController] {CbOnBleConnectionStateChanged} parse err: {e.Message}"); }
            });

            MobileInterface.Instance.AddClientRespose(CbOnQrCodeExpired, _ =>
            {
                OnQrCodeExpired?.Invoke();
            });

            MobileInterface.Instance.AddClientRespose(CbOnWifiConnected, data =>
            {
                try { OnWifiConnected?.Invoke(JsonConvert.DeserializeObject<WifiConnectResult>(data)); }
                catch (Exception e) { LoggerUtils.LogError($"[BleHardwareSideController] {CbOnWifiConnected} parse err: {e.Message}"); }
            });

            MobileInterface.Instance.AddClientRespose(CbOnWifiListReceived, data =>
            {
                try { OnWifiListReceived?.Invoke(JsonConvert.DeserializeObject<WifiListResponse>(data)); }
                catch (Exception e) { LoggerUtils.LogError($"[BleHardwareSideController] {CbOnWifiListReceived} parse err: {e.Message}"); }
            });

            MobileInterface.Instance.AddClientRespose(CbOnDataReceived, data =>
            {
                OnDataReceived?.Invoke(data);
            });
        }

        #endregion

        #region 2.1 蓝牙基础操作

        /// <summary>检查蓝牙是否已开启。cb(true) = 已开启</summary>
        public static void IsBluetoothEnabled(Action<bool> cb)
        {
            EnsureEventsRegistered();
            MobileInterface.Instance.AddClientRespose(FnIsBluetoothEnabled, data =>
            {
                MobileInterface.Instance.DelClientResponse(FnIsBluetoothEnabled);
                MobileInterface.Instance.DelClientFail(FnIsBluetoothEnabled);
                try
                {
                    var d = JsonConvert.DeserializeAnonymousType(data, new { enabled = false });
                    cb?.Invoke(d.enabled);
                }
                catch { cb?.Invoke(false); }
            });
            MobileInterface.Instance.AddClientFail(FnIsBluetoothEnabled, data =>
            {
                MobileInterface.Instance.DelClientResponse(FnIsBluetoothEnabled);
                MobileInterface.Instance.DelClientFail(FnIsBluetoothEnabled);
                cb?.Invoke(false);
            });
            MobileInterface.Instance.SendMessage(FnIsBluetoothEnabled, "");
        }

        /// <summary>请求开启蓝牙。cb(true) = 蓝牙已成功开启</summary>
        public static void RequestEnableBluetooth(Action<bool> cb)
        {
            EnsureEventsRegistered();
            MobileInterface.Instance.AddClientRespose(FnRequestEnableBluetooth, data =>
            {
                MobileInterface.Instance.DelClientResponse(FnRequestEnableBluetooth);
                MobileInterface.Instance.DelClientFail(FnRequestEnableBluetooth);
                try
                {
                    var d = JsonConvert.DeserializeAnonymousType(data, new { enabled = false });
                    cb?.Invoke(d.enabled);
                }
                catch { cb?.Invoke(false); }
            });
            MobileInterface.Instance.AddClientFail(FnRequestEnableBluetooth, data =>
            {
                MobileInterface.Instance.DelClientResponse(FnRequestEnableBluetooth);
                MobileInterface.Instance.DelClientFail(FnRequestEnableBluetooth);
                cb?.Invoke(false);
            });
            MobileInterface.Instance.SendMessage(FnRequestEnableBluetooth, "");
        }

        /// <summary>关闭蓝牙。cb(true) = 蓝牙已成功关闭</summary>
        public static void DisableBluetooth(Action<bool> cb)
        {
            EnsureEventsRegistered();
            MobileInterface.Instance.AddClientRespose(FnDisableBluetooth, data =>
            {
                MobileInterface.Instance.DelClientResponse(FnDisableBluetooth);
                MobileInterface.Instance.DelClientFail(FnDisableBluetooth);
                cb?.Invoke(true);
            });
            MobileInterface.Instance.AddClientFail(FnDisableBluetooth, data =>
            {
                MobileInterface.Instance.DelClientResponse(FnDisableBluetooth);
                MobileInterface.Instance.DelClientFail(FnDisableBluetooth);
                cb?.Invoke(false);
            });
            MobileInterface.Instance.SendMessage(FnDisableBluetooth, "");
        }

        /// <summary>请求蓝牙权限（Android 12+ 必需）。cb(true) = 已授权</summary>
        public static void RequestBlePermissions(Action<bool> cb)
        {
            EnsureEventsRegistered();
            MobileInterface.Instance.AddClientRespose(FnRequestBlePermissions, data =>
            {
                MobileInterface.Instance.DelClientResponse(FnRequestBlePermissions);
                MobileInterface.Instance.DelClientFail(FnRequestBlePermissions);
                try
                {
                    var d = JsonConvert.DeserializeAnonymousType(data, new { granted = false });
                    cb?.Invoke(d.granted);
                }
                catch { cb?.Invoke(false); }
            });
            MobileInterface.Instance.AddClientFail(FnRequestBlePermissions, data =>
            {
                MobileInterface.Instance.DelClientResponse(FnRequestBlePermissions);
                MobileInterface.Instance.DelClientFail(FnRequestBlePermissions);
                cb?.Invoke(false);
            });
            MobileInterface.Instance.SendMessage(FnRequestBlePermissions, "");
        }

        /// <summary>请求位置权限并返回授权结果与位置服务状态</summary>
        public static void RequestLocationPermission(Action<LocationPermissionResult> cb)
        {
            EnsureEventsRegistered();
            MobileInterface.Instance.AddClientRespose(FnRequestLocationPermission, data =>
            {
                MobileInterface.Instance.DelClientResponse(FnRequestLocationPermission);
                MobileInterface.Instance.DelClientFail(FnRequestLocationPermission);
                try { cb?.Invoke(JsonConvert.DeserializeObject<LocationPermissionResult>(data)); }
                catch { cb?.Invoke(new LocationPermissionResult { granted = false, message = "解析失败" }); }
            });
            MobileInterface.Instance.AddClientFail(FnRequestLocationPermission, data =>
            {
                MobileInterface.Instance.DelClientResponse(FnRequestLocationPermission);
                MobileInterface.Instance.DelClientFail(FnRequestLocationPermission);
                cb?.Invoke(new LocationPermissionResult { granted = false, message = data });
            });
            MobileInterface.Instance.SendMessage(FnRequestLocationPermission, "");
        }

        /// <summary>仅查询位置权限和位置服务状态，不弹窗</summary>
        public static void CheckLocationStatus(Action<LocationStatus> cb)
        {
            EnsureEventsRegistered();
            MobileInterface.Instance.AddClientRespose(FnCheckLocationStatus, data =>
            {
                MobileInterface.Instance.DelClientResponse(FnCheckLocationStatus);
                MobileInterface.Instance.DelClientFail(FnCheckLocationStatus);
                try { cb?.Invoke(JsonConvert.DeserializeObject<LocationStatus>(data)); }
                catch { cb?.Invoke(new LocationStatus()); }
            });
            MobileInterface.Instance.AddClientFail(FnCheckLocationStatus, data =>
            {
                MobileInterface.Instance.DelClientResponse(FnCheckLocationStatus);
                MobileInterface.Instance.DelClientFail(FnCheckLocationStatus);
                cb?.Invoke(new LocationStatus());
            });
            MobileInterface.Instance.SendMessage(FnCheckLocationStatus, "");
        }

        #endregion

        #region 2.2 BLE 服务端核心功能

        /// <summary>
        /// 生成二维码并启动 BLE 服务端。
        /// 成功后 Unity 需将 QrCodeData 自行渲染为二维码图片展示。
        /// </summary>
        public static void GenerateBleQrCode(Action<QrCodeData> onSuccess, Action<string> onError = null)
        {
            EnsureEventsRegistered();
            MobileInterface.Instance.AddClientRespose(FnGenerateBleQrCode, data =>
            {
                MobileInterface.Instance.DelClientResponse(FnGenerateBleQrCode);
                MobileInterface.Instance.DelClientFail(FnGenerateBleQrCode);
                try
                {
                    var qr = JsonConvert.DeserializeObject<QrCodeData>(data);
                    onSuccess?.Invoke(qr);
                }
                catch (Exception e)
                {
                    LoggerUtils.LogError($"[BleHardwareSideController] {FnGenerateBleQrCode} parse err: {e.Message}");
                    onError?.Invoke(e.Message);
                }
            });
            MobileInterface.Instance.AddClientFail(FnGenerateBleQrCode, data =>
            {
                MobileInterface.Instance.DelClientResponse(FnGenerateBleQrCode);
                MobileInterface.Instance.DelClientFail(FnGenerateBleQrCode);
                onError?.Invoke(data);
            });
            MobileInterface.Instance.SendMessage(FnGenerateBleQrCode, "");
        }

        /// <summary>停止 BLE 服务</summary>
        public static void StopBleService(Action onSuccess = null)
        {
            EnsureEventsRegistered();
            MobileInterface.Instance.AddClientRespose(FnStopBleService, data =>
            {
                MobileInterface.Instance.DelClientResponse(FnStopBleService);
                MobileInterface.Instance.DelClientFail(FnStopBleService);
                onSuccess?.Invoke();
            });
            MobileInterface.Instance.AddClientFail(FnStopBleService, data =>
            {
                MobileInterface.Instance.DelClientResponse(FnStopBleService);
                MobileInterface.Instance.DelClientFail(FnStopBleService);
                LoggerUtils.LogError($"[BleHardwareSideController] {FnStopBleService} failed: {data}");
            });
            MobileInterface.Instance.SendMessage(FnStopBleService, "");
        }

        /// <summary>获取 BLE 服务端当前运行状态及客户端绑定状态</summary>
        public static void GetBleBindingStatus(Action<BleBindingStatus> onSuccess, Action<string> onError = null)
        {
            EnsureEventsRegistered();
            MobileInterface.Instance.AddClientRespose(FnGetBleBindingStatus, data =>
            {
                MobileInterface.Instance.DelClientResponse(FnGetBleBindingStatus);
                MobileInterface.Instance.DelClientFail(FnGetBleBindingStatus);
                try
                {
                    var status = JsonConvert.DeserializeObject<BleBindingStatus>(data);
                    onSuccess?.Invoke(status);
                }
                catch (Exception e)
                {
                    LoggerUtils.LogError($"[BleHardwareSideController] {FnGetBleBindingStatus} parse err: {e.Message}");
                    onError?.Invoke(e.Message);
                }
            });
            MobileInterface.Instance.AddClientFail(FnGetBleBindingStatus, data =>
            {
                MobileInterface.Instance.DelClientResponse(FnGetBleBindingStatus);
                MobileInterface.Instance.DelClientFail(FnGetBleBindingStatus);
                onError?.Invoke(data);
            });
            MobileInterface.Instance.SendMessage(FnGetBleBindingStatus, "");
        }

        /// <summary>检查 BLE 服务是否正在运行。cb(true) = 运行中</summary>
        public static void IsBleServiceRunning(Action<bool> cb)
        {
            EnsureEventsRegistered();
            MobileInterface.Instance.AddClientRespose(FnIsBleServiceRunning, data =>
            {
                MobileInterface.Instance.DelClientResponse(FnIsBleServiceRunning);
                MobileInterface.Instance.DelClientFail(FnIsBleServiceRunning);
                try
                {
                    var d = JsonConvert.DeserializeAnonymousType(data, new { isRunning = false });
                    cb?.Invoke(d.isRunning);
                }
                catch { cb?.Invoke(false); }
            });
            MobileInterface.Instance.AddClientFail(FnIsBleServiceRunning, data =>
            {
                MobileInterface.Instance.DelClientResponse(FnIsBleServiceRunning);
                MobileInterface.Instance.DelClientFail(FnIsBleServiceRunning);
                cb?.Invoke(false);
            });
            MobileInterface.Instance.SendMessage(FnIsBleServiceRunning, "");
        }

        /// <summary>开始 BLE 广播。cb(success, deviceId)</summary>
        public static void StartBleAdvertising(Action<bool, string> cb)
        {
            EnsureEventsRegistered();
            MobileInterface.Instance.AddClientRespose(FnStartBleAdvertising, data =>
            {
                MobileInterface.Instance.DelClientResponse(FnStartBleAdvertising);
                MobileInterface.Instance.DelClientFail(FnStartBleAdvertising);
                try
                {
                    var d = JsonConvert.DeserializeAnonymousType(data, new { success = false, deviceId = "" });
                    cb?.Invoke(d.success, d.deviceId);
                }
                catch { cb?.Invoke(false, ""); }
            });
            MobileInterface.Instance.AddClientFail(FnStartBleAdvertising, data =>
            {
                MobileInterface.Instance.DelClientResponse(FnStartBleAdvertising);
                MobileInterface.Instance.DelClientFail(FnStartBleAdvertising);
                cb?.Invoke(false, data);
            });
            MobileInterface.Instance.SendMessage(FnStartBleAdvertising, "");
        }

        /// <summary>停止 BLE 广播。cb(true) = 成功</summary>
        public static void StopBleAdvertising(Action<bool> cb)
        {
            EnsureEventsRegistered();
            MobileInterface.Instance.AddClientRespose(FnStopBleAdvertising, data =>
            {
                MobileInterface.Instance.DelClientResponse(FnStopBleAdvertising);
                MobileInterface.Instance.DelClientFail(FnStopBleAdvertising);
                try
                {
                    var d = JsonConvert.DeserializeAnonymousType(data, new { success = false });
                    cb?.Invoke(d.success);
                }
                catch { cb?.Invoke(false); }
            });
            MobileInterface.Instance.AddClientFail(FnStopBleAdvertising, data =>
            {
                MobileInterface.Instance.DelClientResponse(FnStopBleAdvertising);
                MobileInterface.Instance.DelClientFail(FnStopBleAdvertising);
                cb?.Invoke(false);
            });
            MobileInterface.Instance.SendMessage(FnStopBleAdvertising, "");
        }

        /// <summary>获取当前已连接的 WiFi 信息</summary>
        public static void GetConnectedWifiInfo(Action<ConnectedWifiInfo> onSuccess, Action<string> onError = null)
        {
            EnsureEventsRegistered();
            MobileInterface.Instance.AddClientRespose(FnGetConnectedWifiInfo, data =>
            {
                MobileInterface.Instance.DelClientResponse(FnGetConnectedWifiInfo);
                MobileInterface.Instance.DelClientFail(FnGetConnectedWifiInfo);
                try
                {
                    var info = JsonConvert.DeserializeObject<ConnectedWifiInfo>(data);
                    onSuccess?.Invoke(info);
                }
                catch (Exception e)
                {
                    LoggerUtils.LogError($"[BleHardwareSideController] {FnGetConnectedWifiInfo} parse err: {e.Message}");
                    onError?.Invoke(e.Message);
                }
            });
            MobileInterface.Instance.AddClientFail(FnGetConnectedWifiInfo, data =>
            {
                MobileInterface.Instance.DelClientResponse(FnGetConnectedWifiInfo);
                MobileInterface.Instance.DelClientFail(FnGetConnectedWifiInfo);
                onError?.Invoke(data);
            });
            MobileInterface.Instance.SendMessage(FnGetConnectedWifiInfo, "");
        }

        #endregion
    }
}
