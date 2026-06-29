using System;
using System.Collections.Generic;
using System.Text;
using Game.BLE;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

/// <summary>
/// 硬件侧（BLE 服务端）测试面板。
/// 挂到任意 GameObject 即可运行，使用 IMGUI 绘制，无需 UI Prefab。
/// 测试流程：
///   1. Request Permissions  →  2. Enable Bluetooth
///   3. Generate QR Code（生成二维码图片，软件侧手机扫码）
///   4. 等待客户端扫码绑定 → 查看事件日志
///   5. Stop Service
/// </summary>
public class BleHardwareSideTestMono : MonoBehaviour
{
    #region Inspector

    [Header("UI 缩放（根据屏幕分辨率调整，1080p 建议 2.0~2.5）")]
    [SerializeField] private float uiScale = 2.5f;

    [Header("二维码图片尺寸（像素）")]
    [SerializeField] private int qrSize = 512;

    #endregion

    #region 状态

    private enum Tab { Control, QrCode, Events }
    private Tab _currentTab = Tab.Control;

    private string _btStatus      = "未知";
    private string _permStatus    = "未知";
    private string _locStatus     = "未知";
    private string _serviceStatus = "未知";

    // QR Code
    private string    _qrJson    = "";
    private Texture2D _qrTexture;
    private object _qrWriter;

    // Binding status
    private string  _bindingDetail = "";
    private Vector2 _bindingScroll;

    // Log
    private Vector2        _logScroll;
    private readonly StringBuilder _log      = new StringBuilder();
    private readonly List<string>  _logLines = new List<string>();

    #endregion

    #region Unity 生命周期

    private static bool? _mqttVersionCache;
    private static bool CheckMqttVersion()
    {
        if (_mqttVersionCache.HasValue) return _mqttVersionCache.Value;

        _mqttVersionCache = DeviceInfoManager.Inst.CheckVersion_1_0_19();

        return _mqttVersionCache.Value;
    }

    private void Awake()
    {
        uiScale= 0.7f;
        if (CheckMqttVersion())
        {
            try
            {
                var writerType = Type.GetType("ZXing.BarcodeWriter, zxing.unity")
                              ?? Type.GetType("ZXing.BarcodeWriter, zxing")
                              ?? Type.GetType("ZXing.BarcodeWriter, ZXing");
                if (writerType != null)
                {
                    _qrWriter = Activator.CreateInstance(writerType);
                    var formatType = Type.GetType("ZXing.BarcodeFormat, zxing.unity")
                                  ?? Type.GetType("ZXing.BarcodeFormat, zxing")
                                  ?? Type.GetType("ZXing.BarcodeFormat, ZXing");
                    if (formatType != null)
                        writerType.GetProperty("Format")?.SetValue(_qrWriter, Enum.Parse(formatType, "QR_CODE"));
                    var optType = Type.GetType("ZXing.QrCode.QrCodeEncodingOptions, zxing.unity")
                               ?? Type.GetType("ZXing.QrCode.QrCodeEncodingOptions, zxing")
                               ?? Type.GetType("ZXing.QrCode.QrCodeEncodingOptions, ZXing");
                    if (optType != null)
                    {
                        var opt = Activator.CreateInstance(optType);
                        optType.GetProperty("Height")?.SetValue(opt, qrSize);
                        optType.GetProperty("Width")?.SetValue(opt, qrSize);
                        optType.GetProperty("Margin")?.SetValue(opt, 1);
                        var ecType = Type.GetType("ZXing.QrCode.Internal.ErrorCorrectionLevel, zxing.unity")
                                  ?? Type.GetType("ZXing.QrCode.Internal.ErrorCorrectionLevel, zxing")
                                  ?? Type.GetType("ZXing.QrCode.Internal.ErrorCorrectionLevel, ZXing");
                        if (ecType != null)
                            optType.GetProperty("ErrorCorrection")?.SetValue(opt, Enum.Parse(ecType, "L"));
                        writerType.GetProperty("Options")?.SetValue(_qrWriter, opt);
                    }
                }
            }
            catch { _qrWriter = null; }
            _qrTexture = new Texture2D(qrSize, qrSize);
        }

        Log("BleHardwareSideTestMono 初始化");
        RegisterBleEvents();
    }

    private void OnDestroy()
    {
        UnregisterBleEvents();
        if (_qrTexture != null) Destroy(_qrTexture);
    }

    #endregion

    #region BLE 事件

    private void RegisterBleEvents()
    {
        BleHardwareSideController.OnBindSuccess           += OnBindSuccess;
        BleHardwareSideController.OnBindFailed            += OnBindFailed;
        BleHardwareSideController.OnConnectionStateChanged += OnConnectionStateChanged;
        BleHardwareSideController.OnQrCodeExpired         += OnQrCodeExpired;
        BleHardwareSideController.OnWifiConnected         += OnWifiConnected;
        BleHardwareSideController.OnWifiListReceived      += OnWifiListReceived;
        BleHardwareSideController.OnDataReceived          += OnDataReceived;
        BleHardwareSideController.EnsureEventsRegistered();
    }

    private void UnregisterBleEvents()
    {
        BleHardwareSideController.OnBindSuccess           -= OnBindSuccess;
        BleHardwareSideController.OnBindFailed            -= OnBindFailed;
        BleHardwareSideController.OnConnectionStateChanged -= OnConnectionStateChanged;
        BleHardwareSideController.OnQrCodeExpired         -= OnQrCodeExpired;
        BleHardwareSideController.OnWifiConnected         -= OnWifiConnected;
        BleHardwareSideController.OnWifiListReceived      -= OnWifiListReceived;
        BleHardwareSideController.OnDataReceived          -= OnDataReceived;
    }

    private void OnBindSuccess(BleBindSuccessData d)
    {
        _serviceStatus = "已绑定客户端 ✓";
        Log($"[绑定成功BindSuccess] deviceId={d.deviceId}  clientId={d.clientDeviceId}");
    }
    private void OnBindFailed(BleBindFailedData d)
        => Log($"[绑定失败BindFailed] code={d.errorCode}  msg={d.errorMessage}");
    private void OnConnectionStateChanged(BleConnectionStateData d)
        => Log($"[连接状态改变ConnState] addr={d.deviceAddress}  connected={d.isConnected}");
    private void OnQrCodeExpired()
    {
        _serviceStatus = "二维码已过期，请重新生成";
        _qrJson = "";
        Log("[二维码过期QrCodeExpired] 二维码已过期");
    }
    private void OnWifiConnected(WifiConnectResult r)
        => Log($"[WiFi连接成功WifiConnected] code={r.resultCode}  ssid={r.ssid}  msg={r.message}");
    private void OnWifiListReceived(WifiListResponse r)
    {
        Log($"[WiFi列表接收WifiList] count={r.wifiList?.Count ?? 0}  code={r.resultCode}");
        if (r.wifiList != null)
            foreach (var w in r.wifiList)
                Log($"  {w.ssid}  {w.level}dBm  {w.securityType}  connected={w.isConnected}");
    }
    private void OnDataReceived(string data)
        => Log($"[数据接收DataReceived] {data}");

    #endregion

    #region 二维码生成

    private void RenderQrTexture(string json)
    {
        if (!CheckMqttVersion() || _qrWriter == null) return;
        try
        {
            var writeMethod = _qrWriter.GetType().GetMethod("Write", new[] { typeof(string) });
            var pixels = (Color32[])writeMethod?.Invoke(_qrWriter, new object[] { json });
            if (pixels == null) return;
            _qrTexture.SetPixels32(pixels);
            _qrTexture.Apply();
        }
        catch (Exception e)
        {
            Log($"QR 生成失败: {e.Message}");
        }
    }

    #endregion

    #region IMGUI

    private GUIStyle _btnStyle;
    private GUIStyle _labelStyle;
    private GUIStyle _labelBoldStyle;
    private GUIStyle _textAreaStyle;
    private bool     _stylesInit;

    private void InitStyles()
    {
        // if (_stylesInit) return;
        _stylesInit = true;
        int fs  = Mathf.RoundToInt(14 * uiScale);
        float h = 58 * uiScale;
        _btnStyle = new GUIStyle(GUI.skin.button)
            { fontSize = fs, fixedHeight = h };
        _labelStyle = new GUIStyle(GUI.skin.label)
            { fontSize = fs, wordWrap = true };
        _labelStyle.normal.textColor = Color.black;
        _labelBoldStyle = new GUIStyle(GUI.skin.label)
            { fontSize = fs, wordWrap = true, fontStyle = FontStyle.Bold };
        _labelBoldStyle.normal.textColor = Color.black;
        _textAreaStyle = new GUIStyle(GUI.skin.textArea)
            { fontSize = Mathf.RoundToInt(12 * uiScale), wordWrap = true };
    }

    private void OnGUI()
    {
        InitStyles();
        float m = 20 * uiScale;
        GUILayout.BeginArea(new Rect(m, m, Screen.width - m * 2, Screen.height - m * 2));

        GUILayout.Label("【硬件侧 BLE 服务端 测试面板】", _labelBoldStyle);
        GUILayout.Label($"蓝牙：{_btStatus}  权限：{_permStatus}  位置：{_locStatus}  服务：{_serviceStatus}", _labelStyle);
        GUILayout.Space(6);

        // Tab bar
        GUILayout.BeginHorizontal();
        DrawTab(Tab.Control, "控制");
        DrawTab(Tab.QrCode,  "二维码");
        DrawTab(Tab.Events,  "事件日志");
        GUILayout.EndHorizontal();
        GUILayout.Space(8);

        switch (_currentTab)
        {
            case Tab.Control: DrawControlTab(); break;
            case Tab.QrCode:  DrawQrCodeTab();  break;
            case Tab.Events:  DrawEventsTab();  break;
        }

        GUILayout.EndArea();
    }

    private void DrawTab(Tab tab, string label)
    {
        GUI.backgroundColor = _currentTab == tab ? Color.cyan : Color.white;
        if (GUILayout.Button(label, _btnStyle, GUILayout.Width((Screen.width - 40 * uiScale) / 3f)))
            _currentTab = tab;
        GUI.backgroundColor = Color.white;
    }

    // ── 控制 Tab ──────────────────────────────────────────
    private void DrawControlTab()
    {
        if (GUILayout.Button("① 检查位置权限状态", _btnStyle))
            BleHardwareSideController.CheckLocationStatus(s =>
            {
                _locStatus = $"权限:{(s.permissionGranted ? "✓" : "✗")}  GPS:{(s.locationEnabled ? "✓" : "✗")}  SDK:{s.sdkVersion}";
                Log($"CheckLocationStatus -> granted={s.permissionGranted}  locationEnabled={s.locationEnabled}  sdk={s.sdkVersion}");
            });

        if (GUILayout.Button("② 请求位置权限", _btnStyle))
            BleHardwareSideController.RequestLocationPermission(r =>
            {
                _locStatus = $"权限:{(r.granted ? "✓" : "✗")}  GPS:{(r.locationEnabled ? "✓" : "✗")}";
                Log($"RequestLocationPermission -> granted={r.granted}  locationEnabled={r.locationEnabled}  msg={r.message}");
            });

        GUILayout.Space(4);

        if (GUILayout.Button("③ 请求蓝牙权限", _btnStyle))
            BleHardwareSideController.RequestBlePermissions(ok =>
            {
                _permStatus = ok ? "已授权" : "已拒绝";
                Log($"RequestBlePermissions -> {ok}");
            });

        if (GUILayout.Button("④ 检查蓝牙状态", _btnStyle))
            BleHardwareSideController.IsBluetoothEnabled(ok =>
            {
                _btStatus = ok ? "已开启" : "未开启";
                Log($"IsBluetoothEnabled -> {ok}");
            });

        if (GUILayout.Button("⑤ 开启蓝牙", _btnStyle))
            BleHardwareSideController.RequestEnableBluetooth(ok =>
            {
                _btStatus = ok ? "已开启" : "开启失败";
                Log($"RequestEnableBluetooth -> {ok}");
            });

        GUILayout.Space(4);

        GUI.backgroundColor = new Color(0.3f, 0.8f, 1f);
        if (GUILayout.Button("⑥ 生成二维码 & 启动 BLE 服务", _btnStyle)){
            BleHardwareSideController.GenerateBleQrCode(
                onSuccess: qr =>
                {
                    _qrJson = JsonConvert.SerializeObject(qr);
                    RenderQrTexture(_qrJson);
                    _serviceStatus = "服务已启动，等待扫码";
                    _currentTab    = Tab.QrCode;
                    Log($"GenerateBleQrCode 成功  advHint={qr.advHint}  expire={qr.expireAt}");
                },
                onError: err =>
                {
                    _serviceStatus = $"生成失败: {err}";
                    Log($"GenerateBleQrCode 失败: {err}");
                });
    }
        GUI.backgroundColor = Color.white;

        GUILayout.Space(8);

        if (GUILayout.Button("查询绑定 & 服务状态", _btnStyle))
            BleHardwareSideController.GetBleBindingStatus(
                onSuccess: s =>
                {
                    _serviceStatus  = s.isRunning ? $"运行中 bound={s.boundCount} conn={s.connectedCount}" : "未运行";
                    _bindingDetail  = JsonConvert.SerializeObject(s, Formatting.Indented);
                    Log($"BindingStatus running={s.isRunning}  bound={s.boundCount}  conn={s.connectedCount}");
                },
                onError: err => Log($"GetBleBindingStatus 失败: {err}"));

        if (GUILayout.Button("检查服务是否运行中", _btnStyle))
            BleHardwareSideController.IsBleServiceRunning(ok =>
            {
                _serviceStatus = ok ? "运行中" : "未运行";
                Log($"IsBleServiceRunning -> {ok}");
            });

        GUILayout.Space(8);

        GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
        if (GUILayout.Button("⑦ 停止 BLE 服务", _btnStyle))
            BleHardwareSideController.StopBleService(() =>
            {
                _serviceStatus = "已停止";
                Log("StopBleService 成功");
            });

        if (GUILayout.Button("关闭蓝牙", _btnStyle))
            BleHardwareSideController.DisableBluetooth(ok =>
            {
                _btStatus = ok ? "已关闭" : "关闭失败";
                Log($"DisableBluetooth -> {ok}");
            });
        GUI.backgroundColor = Color.white;

        GUILayout.Space(8);
        GUILayout.Label("── BLE 广播 / WiFi 查询 ──", _labelStyle);

        if (GUILayout.Button("开始 BLE 广播", _btnStyle))
            BleHardwareSideController.StartBleAdvertising((ok, deviceId) =>
            {
                Log($"StartBleAdvertising -> ok={ok}  deviceId={deviceId}");
            });

        if (GUILayout.Button("停止 BLE 广播", _btnStyle))
            BleHardwareSideController.StopBleAdvertising(ok =>
            {
                Log($"StopBleAdvertising -> ok={ok}");
            });

        if (GUILayout.Button("获取已连接 WiFi 信息", _btnStyle))
            BleHardwareSideController.GetConnectedWifiInfo(
                onSuccess: info =>
                {
                    Log($"GetConnectedWifiInfo -> connected={info.connected}  ssid={info.ssid}  bssid={info.bssid}  rssi={info.rssi}dBm  linkSpeed={info.linkSpeed}  freq={info.frequency}");
                },
                onError: err => Log($"GetConnectedWifiInfo 失败: {err}"));

        if (!string.IsNullOrEmpty(_bindingDetail))
        {
            GUILayout.Space(6);
            GUILayout.Label("绑定详情：", _labelStyle);
            _bindingScroll = GUILayout.BeginScrollView(_bindingScroll, GUILayout.Height(150 * uiScale));
            GUILayout.TextArea(_bindingDetail, _textAreaStyle);
            GUILayout.EndScrollView();
        }
    }

    // ── 二维码 Tab ────────────────────────────────────────
    private void DrawQrCodeTab()
    {
        if (string.IsNullOrEmpty(_qrJson))
        {
            GUILayout.Label("尚未生成二维码，请在「控制」Tab 点击「生成二维码」。", _labelStyle);
            return;
        }

        GUILayout.Label("让软件侧手机用相机扫描下方二维码完成连接：", _labelStyle);
        GUILayout.Space(6);

        // 居中绘制二维码图片
        float qrDrawSize = Mathf.Min(Screen.width, Screen.height) * 0.65f;
        float xOffset    = (Screen.width - qrDrawSize) * 0.5f - 20 * uiScale;
        GUILayout.BeginHorizontal();
        GUILayout.Space(xOffset);
        GUILayout.Label(_qrTexture, GUILayout.Width(qrDrawSize), GUILayout.Height(qrDrawSize));
        GUILayout.EndHorizontal();

        GUILayout.Space(8);
        GUILayout.Label($"有效期 10 分钟，过期后请重新生成。\nadvHint（后 8 位）：{GetAdvHint()}", _labelStyle);

        GUILayout.Space(6);
        if (GUILayout.Button("复制 JSON 到剪贴板（备用）", _btnStyle))
        {
            GUIUtility.systemCopyBuffer = _qrJson;
            Log("QrCode JSON 已复制到剪贴板");
        }

        if (GUILayout.Button("重新生成二维码", _btnStyle))
            _currentTab = Tab.Control;
    }

    // ── 事件日志 Tab ──────────────────────────────────────
    private void DrawEventsTab()
    {
        GUILayout.Label("事件日志（最新在底部）：", _labelStyle);
        _logScroll = GUILayout.BeginScrollView(_logScroll, GUILayout.ExpandHeight(true));
        GUILayout.TextArea(_log.ToString(), _textAreaStyle, GUILayout.ExpandHeight(true));
        GUILayout.EndScrollView();
        GUILayout.Space(4);
        if (GUILayout.Button("清空", _btnStyle))
        {
            _log.Clear();
            _logLines.Clear();
        }
    }

    #endregion

    #region 工具

    private string GetAdvHint()
    {
        if (string.IsNullOrEmpty(_qrJson)) return "";
        try
        {
            var qr = JsonConvert.DeserializeObject<QrCodeData>(_qrJson);
            return qr?.advHint ?? "";
        }
        catch { return ""; }
    }

    private void Log(string msg)
    {
        string line = $"[{DateTime.Now:HH:mm:ss}] {msg}";
        _logLines.Add(line);
        if (_logLines.Count > 200) _logLines.RemoveAt(0);
        _log.Clear();
        foreach (var l in _logLines) _log.AppendLine(l);
        _logScroll.y = float.MaxValue;
        Debug.Log($"[BleHwTest] {msg}");
    }

    #endregion
}
