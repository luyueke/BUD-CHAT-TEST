using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Game.BLE;
using Newtonsoft.Json;
using UnityEngine;
// using UnityEngine.XR.ARFoundation;
// using UnityEngine.XR.ARSubsystems;

/// <summary>
/// 软件侧（BLE 客户端）测试面板。
/// 挂到任意 GameObject 即可运行，使用 IMGUI 绘制，无需 UI Prefab。
/// 测试流程：
///   1. 「连接」Tab → 点击「开启相机扫码」→ 对准硬件侧二维码 → 自动识别并连接
///      （或手动粘贴 JSON 后点击「连接」）
///   2. 「数据」Tab → 发送 WiFi 配置 / 自定义数据 / 请求 WiFi 列表
///   3. 「WiFi」Tab → 查看 WiFi 列表，点击条目可自动填入 SSID
///   4. 「日志」Tab → 查看所有回调
///   5. 「连接」Tab → 断开连接
/// </summary>
public class BleSoftwareSideTestMono : MonoBehaviour
{
    #region Inspector

    [Header("UI 缩放（1080p 建议 2.0~2.5）")]
    [SerializeField] private float uiScale = 2.5f;

    [Header("扫码间隔（秒，越小越灵敏但越耗性能）")]
    [SerializeField] private float scanInterval = 0.3f;

    #endregion

    #region 状态

    private enum Tab { Connect, Data, Wifi, Events }
    private Tab _currentTab = Tab.Connect;

    // 连接状态
    private string _connStatus  = "未连接";
    private bool   _isConnected = false;
    private string _locStatus   = "未知";
    private string _btStatus    = "未知";
    private string _permStatus  = "未知";

    // ── 相机扫码 ──
    private WebCamTexture _camTexture;
    private bool          _scanning       = false;
    private bool          _scanSucceeded  = false;
    private float         _scanTimer      = 0f;
    private Rect          _camPreviewRect;
    // 后台解码线程控制
    private volatile bool   _decoding      = false;
    private volatile string _pendingResult = null;

    private object _reader;

    private void EnsureBarcodeReader()
    {
        if (_reader != null) return;
        try
        {
            var readerType = Type.GetType("ZXing.BarcodeReader, zxing.unity")
                          ?? Type.GetType("ZXing.BarcodeReader, zxing")
                          ?? Type.GetType("ZXing.BarcodeReader, ZXing");
            if (readerType == null) { Debug.LogError("[BleSwTest] ZXing.BarcodeReader type not found"); return; }
            _reader = Activator.CreateInstance(readerType);
            readerType.GetProperty("AutoRotate")?.SetValue(_reader, true);
            var options = readerType.GetProperty("Options")?.GetValue(_reader);
            if (options != null)
            {
                var optType = options.GetType();
                optType.GetProperty("TryHarder")?.SetValue(options, true);
                var formatType = Type.GetType("ZXing.BarcodeFormat, zxing.unity")
                              ?? Type.GetType("ZXing.BarcodeFormat, zxing")
                              ?? Type.GetType("ZXing.BarcodeFormat, ZXing");
                if (formatType != null)
                {
                    var qrCode = Enum.Parse(formatType, "QR_CODE");
                    var listType = typeof(System.Collections.Generic.List<>).MakeGenericType(formatType);
                    var formats = Activator.CreateInstance(listType);
                    listType.GetMethod("Add")?.Invoke(formats, new[] { qrCode });
                    optType.GetProperty("PossibleFormats")?.SetValue(options, formats);
                }
            }
        }
        catch { _reader = null; }
    }

    private static bool? _mqttVersionCache;
    private static bool CheckMqttVersion()
    {
        if (_mqttVersionCache.HasValue) return _mqttVersionCache.Value;

        _mqttVersionCache = DeviceInfoManager.Inst.CheckVersion_1_0_19();

        return _mqttVersionCache.Value;
    }

    // ── 手动输入 ──
    private string  _qrJsonInput  = "";
    private Vector2 _qrInputScroll;

    // ── 数据 Tab ──
    private string   _customData    = "Hello BLE!";
    private string   _wifiSsid      = "";
    private string   _wifiPassword  = "";
    private int      _wifiSecIndex  = 3;
    private readonly string[] _secTypes = { "OPEN", "WEP", "WPA", "WPA2", "WPA3" };

    // ── WiFi 列表 Tab ──
    private List<WifiInfo> _wifiList     = new List<WifiInfo>();
    private Vector2        _wifiScroll;

    // ── 日志 ──
    private Vector2        _logScroll;
    private readonly StringBuilder _log      = new StringBuilder();
    private readonly List<string>  _logLines = new List<string>();

    #endregion

    #region Unity 生命周期

    private void Awake()
    {
        Log("BleSoftwareSideTestMono 初始化");
        uiScale= 1;
        BleSoftwareSideController.OnDataReceived += HandleDataReceived;
        BleSoftwareSideController.EnsureEventsRegistered();
    }

    private void Update()
    {
        TickScan();
    }

    private void OnDestroy()
    {
        BleSoftwareSideController.OnDataReceived -= HandleDataReceived;
        StopCamera();
    }

    #endregion

    #region 相机扫码

    private void StartCamera()
    {
        if (_scanning) return;

        StartCoroutine(RequestCameraAndStart());
    }

    private IEnumerator RequestCameraAndStart()
    {
        // Step 1: Android 相机权限（通过 MobileInterface）
        bool cameraGranted = false;
        bool cameraDone    = false;
        BleSoftwareSideController.RequestCameraPermission(granted =>
        {
            cameraGranted = granted;
            cameraDone    = true;
        });
        yield return new WaitUntil(() => cameraDone);

        if (!cameraGranted)
        {
            Log("相机权限被拒绝（Android），请手动粘贴 JSON");
            yield break;
        }



        WebCamDevice[] devices = WebCamTexture.devices;
        if (devices.Length == 0)
        {
            Log("未检测到摄像头");
            yield break;
        }

        // 优先选后置摄像头
        string camName = devices[0].name;
        for (int i = 0; i < devices.Length; i++)
        {
            if (!devices[i].isFrontFacing) { camName = devices[i].name; break; }
        }

        // 1920×1080 兼顾显示清晰度与 QR 识别距离
        _camTexture = new WebCamTexture(camName, 1920, 1080, 30);
        _camTexture.Play();

        // 等待摄像头初始化
        yield return new WaitUntil(() => _camTexture.width > 16);

        _camPreviewRect = new Rect(0, 0, Screen.width, Screen.height);
        _scanning       = true;
        _scanSucceeded  = false;
        _decoding       = false;
        _pendingResult  = null;
        Log($"相机已开启：{camName}  {_camTexture.width}x{_camTexture.height}");
    }

    private void StopCamera()
    {
        _scanning = false;
        if (_camTexture != null)
        {
            _camTexture.Stop();
            Destroy(_camTexture);
            _camTexture = null;
        }
    }

    private void TickScan()
    {
        // 后台线程有结果时，在主线程处理
        if (_pendingResult != null)
        {
            string text    = _pendingResult;
            _pendingResult = null;
            _scanSucceeded = true;
            _qrJsonInput   = text;
            Log($"扫码成功：{text.Substring(0, Mathf.Min(80, text.Length))}…");
            ConnectWithCurrentJson();
            StopCamera();
            return;
        }

        if (!_scanning || _camTexture == null || !_camTexture.isPlaying || _scanSucceeded) return;
        if (_decoding) return; // 上一帧 Decode 还未结束，跳过

        _scanTimer += Time.deltaTime;
        if (_scanTimer < scanInterval) return;
        _scanTimer = 0f;

        int w = _camTexture.width;
        int h = _camTexture.height;
        if (w == 0 || h == 0) return;

        // GetPixels32 必须在主线程调用；返回新数组，直接交给后台线程使用，无需再 Copy
        Color32[] pixels = _camTexture.GetPixels32();

        _decoding = true;
        System.Threading.ThreadPool.QueueUserWorkItem(_ =>
        {
            try
            {
                if (CheckMqttVersion())
                {
                    EnsureBarcodeReader();
                    if (_reader != null)
                    {
                        var decodeMethod = _reader.GetType().GetMethod("Decode", new[] { typeof(Color32[]), typeof(int), typeof(int) });
                        var result = decodeMethod?.Invoke(_reader, new object[] { pixels, w, h });
                        if (result != null)
                            _pendingResult = (string)result.GetType().GetProperty("Text")?.GetValue(result);
                    }
                }
            }
            catch { /* 忽略解码异常 */ }
            finally { _decoding = false; }
        });
    }

    #endregion

    #region BLE 操作

    private void ConnectWithCurrentJson()
    {
        if (string.IsNullOrWhiteSpace(_qrJsonInput))
        {
            Log("QrCode JSON 为空，请扫码或手动粘贴");
            return;
        }
        _connStatus  = "连接中…";
        _isConnected = false;
        Log("ScanAndConnectDevice 开始…");
        BleSoftwareSideController.ScanAndConnectDevice(_qrJsonInput, (ok, msg) =>
        {
            _isConnected = ok;
            _connStatus  = ok ? "已连接 ✓" : $"连接失败: {msg}";
            Log($"ScanAndConnectDevice -> {ok}  msg={msg}");
            if (ok) _currentTab = Tab.Data;
        });
    }

    private void HandleDataReceived(string data)
    {
        Log($"[DataReceived] {data}");
        // 尝试解析为 WiFi 列表
        try
        {
            var rsp = JsonConvert.DeserializeObject<WifiListResponse>(data);
            if (rsp?.wifiList != null && rsp.wifiList.Count > 0)
            {
                _wifiList   = rsp.wifiList;
                _currentTab = Tab.Wifi;
                Log($"WiFi 列表已更新，共 {_wifiList.Count} 个");
            }
        }
        catch { /* 不是 WifiListResponse */ }
    }

    #endregion

    #region IMGUI

    private GUIStyle _btnStyle;
    private GUIStyle _labelStyle;
    private GUIStyle _labelBoldStyle;
    private GUIStyle _textAreaStyle;
    private GUIStyle _textFieldStyle;
    private bool     _stylesInit;

    private void InitStyles()
    {
        if (_stylesInit) return;
        _stylesInit = true;
        int   fs = Mathf.RoundToInt(30 * uiScale);
        float bh = 58 * uiScale;
        _btnStyle = new GUIStyle(GUI.skin.button)
            { fontSize = fs, fixedHeight = bh };
        _btnStyle.normal.textColor  = Color.red;
        _btnStyle.hover.textColor   = Color.red;
        _btnStyle.active.textColor  = Color.red;
        _btnStyle.focused.textColor = Color.red;
        _labelStyle = new GUIStyle(GUI.skin.label)
            { fontSize = fs, wordWrap = true };
        _labelStyle.normal.textColor = Color.black;
        _labelBoldStyle = new GUIStyle(GUI.skin.label)
            { fontSize = fs, wordWrap = true, fontStyle = FontStyle.Bold };
        _labelBoldStyle.normal.textColor = Color.black;
        _textAreaStyle = new GUIStyle(GUI.skin.textArea)
            { fontSize = Mathf.RoundToInt(12 * uiScale), wordWrap = true };
        _textFieldStyle = new GUIStyle(GUI.skin.textField)
            { fontSize = fs, fixedHeight = bh * 0.9f };
    }

    private void OnGUI()
    {
        InitStyles();

        // 相机全屏预览（在 UI 之下）
        if (_scanning && _camTexture != null && _camTexture.isPlaying)
        {
            DrawCameraPreview();
        }

        float m = 20 * uiScale;
        GUILayout.BeginArea(new Rect(m, m, Screen.width - m * 2, Screen.height - m * 2));

        // 状态条
        GUI.backgroundColor = _isConnected ? new Color(0.2f, 0.9f, 0.2f) : new Color(0.9f, 0.3f, 0.3f);
        GUILayout.Label($"【软件侧 BLE 客户端】  {_connStatus}  蓝牙：{_btStatus}  权限：{_permStatus}  位置：{_locStatus}", _labelBoldStyle);
        GUI.backgroundColor = Color.white;
        GUILayout.Space(6);

        // Tab bar
        GUILayout.BeginHorizontal();
        DrawTab(Tab.Connect, "连接");
        DrawTab(Tab.Data,    "数据");
        DrawTab(Tab.Wifi,    $"WiFi({_wifiList.Count})");
        DrawTab(Tab.Events,  "日志");
        GUILayout.EndHorizontal();
        GUILayout.Space(8);

        switch (_currentTab)
        {
            case Tab.Connect: DrawConnectTab(); break;
            case Tab.Data:    DrawDataTab();    break;
            case Tab.Wifi:    DrawWifiTab();    break;
            case Tab.Events:  DrawEventsTab();  break;
        }

        GUILayout.EndArea();
    }

    private void DrawCameraPreview()
    {
        int  angle  = _camTexture.videoRotationAngle;
        bool mirror = _camTexture.videoVerticallyMirrored;
        // 旋转 90° 或 270° 时，贴图的宽高在屏幕上会互换
        bool rotated = (angle % 180 != 0);

        float tw = _camTexture.width;
        float th = _camTexture.height;
        float sw = Screen.width;
        float sh = Screen.height;

        // 旋转后的有效显示尺寸用于比例计算
        float effW = rotated ? th : tw;
        float effH = rotated ? tw : th;
        float scale = Mathf.Max(sw / effW, sh / effH);
        float drawW = tw * scale;
        float drawH = th * scale;
        var rect = new Rect((sw - drawW) * 0.5f, (sh - drawH) * 0.5f, drawW, drawH);

        // videoVerticallyMirrored=true 时翻转 UV Y 轴
        var uvRect = mirror ? new Rect(0, 1, 1, -1) : new Rect(0, 0, 1, 1);

        var pivot = new Vector2(sw * 0.5f, sh * 0.5f);
        GUIUtility.RotateAroundPivot(-angle, pivot);
        GUI.DrawTextureWithTexCoords(rect, _camTexture, uvRect);
        GUIUtility.RotateAroundPivot(angle, pivot);

        // 扫码框提示（旋转外绘制，始终正向显示）
        float boxSize = Mathf.Min(sw, sh) * 0.6f;
        GUI.color = new Color(1, 1, 0, 0.8f);
        GUI.Box(new Rect((sw - boxSize) * 0.5f, (sh - boxSize) * 0.5f, boxSize, boxSize), "");
        GUI.color = Color.white;
    }

    private void DrawTab(Tab tab, string label)
    {
        GUI.backgroundColor = _currentTab == tab ? Color.cyan : Color.white;
        if (GUILayout.Button(label, _btnStyle, GUILayout.Width((Screen.width - 40 * uiScale) / 4f)))
            _currentTab = tab;
        GUI.backgroundColor = Color.white;
    }

    // ── 连接 Tab ──────────────────────────────────────────
    private void DrawConnectTab()
    {
        if (_scanning)
        {
            GUILayout.Label("正在扫码中…将相机对准二维码", _labelBoldStyle);
            GUILayout.Space(4);
            GUI.backgroundColor = new Color(1f, 0.5f, 0.2f);
            if (GUILayout.Button("取消扫码", _btnStyle))
                StopCamera();
            GUI.backgroundColor = Color.white;
            return;
        }

        // ── 蓝牙权限 ──
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("请求蓝牙权限", _btnStyle))
            BleSoftwareSideController.RequestBlePermissions(ok =>
            {
                _permStatus = ok ? "已授权 ✓" : "已拒绝 ✗";
                Log($"RequestBlePermissions -> {ok}");
            });
        if (GUILayout.Button("检查蓝牙状态", _btnStyle))
            BleSoftwareSideController.IsBluetoothEnabled(ok =>
            {
                _btStatus = ok ? "已开启 ✓" : "未开启 ✗";
                Log($"IsBluetoothEnabled -> {ok}");
            });
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUI.backgroundColor = new Color(0.3f, 1f, 0.6f);
        if (GUILayout.Button("开启蓝牙", _btnStyle))
            BleSoftwareSideController.RequestEnableBluetooth(ok =>
            {
                _btStatus = ok ? "已开启 ✓" : "开启失败 ✗";
                Log($"RequestEnableBluetooth -> {ok}");
            });
        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();

        GUILayout.Space(4);

        // ── 位置权限 ──
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("检查位置状态", _btnStyle))
            BleSoftwareSideController.CheckLocationStatus(s =>
            {
                _locStatus = $"权限:{(s.permissionGranted ? "✓" : "✗")}  GPS:{(s.locationEnabled ? "✓" : "✗")}  SDK:{s.sdkVersion}";
                Log($"CheckLocationStatus -> granted={s.permissionGranted}  locationEnabled={s.locationEnabled}  sdk={s.sdkVersion}");
            });
        if (GUILayout.Button("请求位置权限", _btnStyle))
            BleSoftwareSideController.RequestLocationPermission(r =>
            {
                _locStatus = $"权限:{(r.granted ? "✓" : "✗")}  GPS:{(r.locationEnabled ? "✓" : "✗")}";
                Log($"RequestLocationPermission -> granted={r.granted}  locationEnabled={r.locationEnabled}  msg={r.message}");
            });
        GUILayout.EndHorizontal();

        GUILayout.Space(6);

        // ── 扫码 ──
        GUI.backgroundColor = new Color(0.3f, 0.8f, 1f);
        GUI.enabled = CheckMqttVersion();
        if (GUILayout.Button(CheckMqttVersion() ? "① 开启相机扫码" : "① 开启相机扫码（版本不支持）", _btnStyle) && CheckMqttVersion())
            StartCamera();
        GUI.enabled = true;
        GUI.backgroundColor = Color.white;

        GUILayout.Space(8);
        GUILayout.Label("── 或手动粘贴 JSON ──", _labelStyle);

        _qrInputScroll = GUILayout.BeginScrollView(_qrInputScroll, GUILayout.Height(Screen.height * 0.25f));
        _qrJsonInput   = GUILayout.TextArea(_qrJsonInput, _textAreaStyle, GUILayout.ExpandHeight(true));
        GUILayout.EndScrollView();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("从剪贴板粘贴", _btnStyle))
        {
            _qrJsonInput = GUIUtility.systemCopyBuffer;
            Log("从剪贴板粘贴 JSON");
        }
        if (GUILayout.Button("清空", _btnStyle, GUILayout.Width(120 * uiScale)))
            _qrJsonInput = "";
        GUILayout.EndHorizontal();

        GUILayout.Space(6);
        GUI.backgroundColor = new Color(0.3f, 0.8f, 1f);
        if (GUILayout.Button("连接设备（使用上方 JSON）", _btnStyle))
            ConnectWithCurrentJson();
        GUI.backgroundColor = Color.white;

        GUILayout.Space(4);
        if (GUILayout.Button("检查连接状态", _btnStyle))
            BleSoftwareSideController.IsDeviceConnected(ok =>
            {
                _isConnected = ok;
                _connStatus  = ok ? "已连接 ✓" : "未连接";
                Log($"IsDeviceConnected -> {ok}");
            });

        GUILayout.Space(8);
        GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
        if (GUILayout.Button("断开连接", _btnStyle))
            BleSoftwareSideController.DisconnectDevice((ok, msg) =>
            {
                _isConnected = false;
                _connStatus  = ok ? "已断开" : $"断开失败: {msg}";
                Log($"DisconnectDevice -> {ok}  msg={msg}");
            });
        GUI.backgroundColor = Color.white;
    }

    // ── 数据 Tab ──────────────────────────────────────────
    private void DrawDataTab()
    {
        if (!_isConnected)
        {
            GUILayout.Label("请先在「连接」Tab 完成设备连接。", _labelStyle);
            return;
        }

        // 自定义数据
        GUILayout.Label("发送自定义数据：", _labelBoldStyle);
        GUILayout.BeginHorizontal();
        GUILayout.Label("内容：", _labelStyle, GUILayout.Width(80 * uiScale));
        _customData = GUILayout.TextField(_customData, _textFieldStyle);
        GUILayout.EndHorizontal();
        if (GUILayout.Button("发送数据", _btnStyle))
            BleSoftwareSideController.SendDataToDevice(_customData,
                (ok, msg) => Log($"SendDataToDevice -> {ok}  msg={msg}"));

        GUILayout.Space(12);

        // WiFi 配置
        GUILayout.Label("发送 WiFi 配置：", _labelBoldStyle);

        GUILayout.BeginHorizontal();
        GUILayout.Label("SSID：", _labelStyle, GUILayout.Width(90 * uiScale));
        _wifiSsid = GUILayout.TextField(_wifiSsid, _textFieldStyle);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("密码：", _labelStyle, GUILayout.Width(90 * uiScale));
        _wifiPassword = GUILayout.TextField(_wifiPassword, _textFieldStyle);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("加密：", _labelStyle, GUILayout.Width(90 * uiScale));
        for (int i = 0; i < _secTypes.Length; i++)
        {
            GUI.backgroundColor = _wifiSecIndex == i ? Color.yellow : Color.white;
            if (GUILayout.Button(_secTypes[i], _btnStyle, GUILayout.Width(88 * uiScale)))
                _wifiSecIndex = i;
        }
        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();

        GUILayout.Space(4);
        if (GUILayout.Button("发送 WiFi 配置", _btnStyle))
        {
            if (string.IsNullOrEmpty(_wifiSsid)) { Log("请先输入 SSID"); return; }
            BleSoftwareSideController.SendWifiConfig(
                new WifiConfig { ssid = _wifiSsid, password = _wifiPassword, securityType = _secTypes[_wifiSecIndex] },
                (ok, msg) => Log($"SendWifiConfig -> {ok}  ssid={_wifiSsid}  msg={msg}"));
        }

        GUILayout.Space(12);

        // WiFi 列表
        GUILayout.Label("请求 WiFi 列表（结果在「WiFi」Tab）：", _labelBoldStyle);
        if (GUILayout.Button("RequestWifiList", _btnStyle))
            BleSoftwareSideController.RequestWifiList((ok, msg) =>
            {
                Log($"RequestWifiList -> {ok}  msg={msg}");
                if (ok) Log("等待 onDataReceived 异步返回 WiFi 列表…");
            });
    }

    // ── WiFi Tab ──────────────────────────────────────────
    private void DrawWifiTab()
    {
        if (_wifiList.Count == 0)
        {
            GUILayout.Label("暂无数据。请在「数据」Tab 点击 RequestWifiList 后等待回调。", _labelStyle);
            return;
        }

        GUILayout.Label($"共 {_wifiList.Count} 个 WiFi，点击可填入「数据」Tab：", _labelStyle);
        GUILayout.Space(4);

        _wifiScroll = GUILayout.BeginScrollView(_wifiScroll, GUILayout.ExpandHeight(true));
        foreach (var w in _wifiList)
        {
            GUI.backgroundColor = w.isConnected ? new Color(0.5f, 1f, 0.5f) : Color.white;
            string band    = FreqBand(w.frequency);
            string connMk  = w.isConnected ? " ★已连接" : "";
            if (GUILayout.Button(
                    $"{w.ssid}{connMk}  {LevelToPercent(w.level)}%  {band}  {w.securityType}",
                    _btnStyle))
            {
                _wifiSsid     = w.ssid;
                _wifiSecIndex = Array.IndexOf(_secTypes, w.securityType);
                if (_wifiSecIndex < 0) _wifiSecIndex = 3;
                _currentTab = Tab.Data;
                Log($"已选择 WiFi: {w.ssid}");
            }
        }
        GUI.backgroundColor = Color.white;
        GUILayout.EndScrollView();

        GUILayout.Space(4);
        if (GUILayout.Button("清空列表", _btnStyle)) _wifiList.Clear();
    }

    // ── 日志 Tab ──────────────────────────────────────────
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

    private static int LevelToPercent(int lvl)
    {
        if (lvl >= -50) return 100;
        if (lvl >= -60) return 80;
        if (lvl >= -70) return 60;
        if (lvl >= -80) return 40;
        if (lvl >= -90) return 20;
        return 0;
    }

    private static string FreqBand(int freq)
    {
        if (freq >= 2400 && freq <= 2500) return "2.4G";
        if (freq >= 5000 && freq <= 6000) return "5G";
        return $"{freq}MHz";
    }

    private void Log(string msg)
    {
        string line = $"[{DateTime.Now:HH:mm:ss}] {msg}";
        _logLines.Add(line);
        if (_logLines.Count > 200) _logLines.RemoveAt(0);
        _log.Clear();
        foreach (var l in _logLines) _log.AppendLine(l);
        _logScroll.y = float.MaxValue;
        Debug.Log($"[BleSWTest] {msg}");
    }

    #endregion
}
