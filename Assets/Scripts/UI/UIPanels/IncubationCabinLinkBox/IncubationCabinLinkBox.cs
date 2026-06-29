using Game.BLE;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class IncubationCabinLinkBox : BasePanel<IncubationCabinLinkBox>
{
    [SerializeField] Button BackBtn;

    [SerializeField] GameObject ProcessLinkSupply;
    [SerializeField] Button ProcessNextBtn;

    [SerializeField] GameObject ProcessPowerOn;
    [SerializeField] Button ProcessPowerNextBtn;
    [SerializeField] Button ProcessPowerLastBtn;

    [SerializeField] GameObject ScanQRCodes;
    [SerializeField] RawImage CameraPreviewImage;
    [SerializeField] Button ScanExitBtn;

    [SerializeField] GameObject LinkPanle; //包含deviceNameText waitConnectGo connectingGo connectedSucGo connectedFailGo


    [SerializeField] GameObject waitSureGo;
    [SerializeField] GameObject waitConnectGo;
    [SerializeField] Text[] DeviceIdName;
    [SerializeField] GameObject connectingGo;
    [SerializeField] GameObject connectedSucGo;
    [SerializeField] GameObject connectedFailGo; //包含reScanBtn
    [SerializeField] Button reScanBtn;


    // 3 个裁剪变体：短边的 40% / 60% / 80%，stride=2 下采样
    // [0] 近距离（QR 几乎铺满视野）  [1] 中距离  [2] 远距离
    private static readonly float[] CropFactors = { 0.4f, 0.6f, 0.8f };
    private const int CropStride = 2;
    private RectInt[] _cropRects;
    private int[] _cropW, _cropH;
    private Color32[][] _cropBufs;

    // ── 相机扫码 ──
    private WebCamTexture _camTexture;
    private bool _scanning = false;
    private bool _scanSucceeded = false;
    private float _scanTimer = 0f;
    private const float ScanInterval = 0.03f;

    // 后台解码线程控制
    private volatile bool _decoding = false;
    private volatile string _pendingResult = null;

    // 扫码得到的 JSON 原文，供后续流程使用
    public string ScannedQrJson { get; private set; } = "";

    private const string Tag = "[LinkBox]";

    // ZXing.BarcodeReader 通过反射延迟初始化，避免编译期依赖 ZXing 程序集
    private object _reader;

    private static bool? _mqttVersionCache;
    private static bool CheckMqttVersion()
    {
        if (_mqttVersionCache.HasValue) return _mqttVersionCache.Value;

        _mqttVersionCache = DeviceInfoManager.Inst.CheckVersion_1_0_19();

        return _mqttVersionCache.Value;
    }

    private void EnsureBarcodeReader()
    {
        if (_reader != null) return;
        Debug.Log("[LinkBox] EnsureBarcodeReader 开始初始化");
        try
        {
            var readerType = Type.GetType("ZXing.BarcodeReader, zxing.unity")
                          ?? Type.GetType("ZXing.BarcodeReader, zxing")
                          ?? Type.GetType("ZXing.BarcodeReader, ZXing");
            if (readerType == null)
            {
                Debug.LogError("[LinkBox] EnsureBarcodeReader 失败：ZXing.BarcodeReader 类型未找到，请确认 ZXing 程序集已正确加载");
                return;
            }
            Debug.Log($"[LinkBox] EnsureBarcodeReader 找到类型: {readerType.AssemblyQualifiedName}");
            _reader = Activator.CreateInstance(readerType);
            Debug.Log("[LinkBox] EnsureBarcodeReader BarcodeReader 实例创建成功");

            var autoRotateProp = readerType.GetProperty("AutoRotate");
            if (autoRotateProp != null)
            {
                autoRotateProp.SetValue(_reader, true);
                Debug.Log("[LinkBox] EnsureBarcodeReader AutoRotate = true");
            }
            else
            {
                Debug.LogWarning("[LinkBox] EnsureBarcodeReader 未找到 AutoRotate 属性");
            }

            var options = readerType.GetProperty("Options")?.GetValue(_reader);
            if (options == null)
            {
                Debug.LogWarning("[LinkBox] EnsureBarcodeReader Options 为 null，将跳过 TryHarder 和 PossibleFormats 配置");
            }
            else
            {
                var optType = options.GetType();
                Debug.Log($"[LinkBox] EnsureBarcodeReader Options 类型: {optType.Name}");

                var tryHarderProp = optType.GetProperty("TryHarder");
                if (tryHarderProp != null)
                {
                    tryHarderProp.SetValue(options, true);
                    Debug.Log("[LinkBox] EnsureBarcodeReader TryHarder = true");
                }
                else
                {
                    Debug.LogWarning("[LinkBox] EnsureBarcodeReader 未找到 TryHarder 属性");
                }

                var formatType = Type.GetType("ZXing.BarcodeFormat, zxing.unity")
                              ?? Type.GetType("ZXing.BarcodeFormat, zxing")
                              ?? Type.GetType("ZXing.BarcodeFormat, ZXing");
                if (formatType == null)
                {
                    Debug.LogWarning("[LinkBox] EnsureBarcodeReader 未找到 ZXing.BarcodeFormat 类型，PossibleFormats 将不限制格式");
                }
                else
                {
                    Debug.Log($"[LinkBox] EnsureBarcodeReader 找到 BarcodeFormat 类型: {formatType.AssemblyQualifiedName}");
                    var qrCode = Enum.Parse(formatType, "QR_CODE");
                    var listType = typeof(System.Collections.Generic.List<>).MakeGenericType(formatType);
                    var formats = Activator.CreateInstance(listType);
                    listType.GetMethod("Add")?.Invoke(formats, new[] { qrCode });
                    var possibleFormatsProp = optType.GetProperty("PossibleFormats");
                    if (possibleFormatsProp != null)
                    {
                        possibleFormatsProp.SetValue(options, formats);
                        Debug.Log("[LinkBox] EnsureBarcodeReader PossibleFormats = [QR_CODE]");
                    }
                    else
                    {
                        Debug.LogWarning("[LinkBox] EnsureBarcodeReader 未找到 PossibleFormats 属性");
                    }
                }
            }
            Debug.Log("[LinkBox] EnsureBarcodeReader 初始化完成");
        }
        catch (Exception e)
        {
            Debug.LogError($"[LinkBox] EnsureBarcodeReader 异常: {e.GetType().Name} — {e.Message}\n{e.StackTrace}");
            _reader = null;
        }
    }

    public override void OnCreate()
    {
        base.OnCreate();
        Debug.Log($"{Tag} OnCreate — 面板创建");
        ProcessLinkSupply.SetActive(true);
        BackBtn.onClick.AddListener(BackBtnOn);

        ProcessNextBtn.onClick.AddListener(ProcessNextBtnOn);

        ProcessPowerNextBtn.onClick.AddListener(ProcessPowerNextBtnOn);
        ProcessPowerLastBtn.onClick.AddListener(ProcessPowerLastBtnOn);

        reScanBtn.onClick.AddListener(ReScanBtnOn);
        ScanExitBtn.onClick.AddListener(ScanExitBtnOn);
    }

    protected override void Update()
    {
        base.Update();
        TickScan();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Debug.Log($"{Tag} OnDestroy — 面板销毁，清理 BLE 监听");
        StopCamera();
        BleSoftwareSideController.OnDataReceived -= OnBleDataReceived;
    }

    private void BackBtnOn()
    {
        Debug.Log($"{Tag} BackBtnOn — 点击返回，弹出确认框");
        var panel = UIManager.Inst.OpenPanel<CommonBoxConfirmWithTitlePanel>(PanelId.CommonBoxConfirmWithTitlePanel2);
        panel.SetTextAndAction(
            "确认退出？",
            "BUD BOX仍在连接设备中。\n现在退出会断开本次连接，下次需要重新开始",
            "确定",
            "取消",
            confirmClick: () =>
            {
                Debug.Log($"{Tag} 确认退出，关闭面板");
                StopCamera();
                UIManager.Inst.ClosePanel(this);
                //通知硬件回到二维码页
                BleSoftwareSideController.SendDataToDevice("{\"data\":\"back2Code\"}");
            },
            cancelClick: null
        );
    }

    private void ProcessNextBtnOn()
    {
        Debug.Log($"{Tag} ProcessNextBtnOn — 步骤1→步骤2（开机引导）");
        ProcessLinkSupply.SetActive(false);
        ProcessPowerOn.SetActive(true);
    }

    void CheckLocation(Action action)
    {
        CheckLocation((result) =>
        {
#if UNITY_ANDROID
            if (!result.locationEnabled)
            {
                if (DeviceInfoManager.Inst.CheckVersion_1_0_20())
                {
                    MobileInterface.Instance.SendMessage(MobileInterfaceDefine.openLocationSettings, "");
                }
                else
                {
                    TipPanel.ShowToast($"下拉菜单开启\"定位服务\"");
                }
                return;
            }
#endif
            action?.Invoke();
        });
    }

    private void ProcessPowerNextBtnOn()
    {
        CheckLocation(() =>
        {
            Debug.Log($"{Tag} ProcessPowerNextBtnOn — 步骤2→扫码，启动相机");
            ProcessPowerOn.SetActive(false);
            ScanQRCodes.SetActive(true);
            StartCameraScan();
        });
    }

    private void ProcessPowerLastBtnOn()
    {
        Debug.Log($"{Tag} ProcessPowerLastBtnOn — 步骤2→返回步骤1");
        ProcessPowerOn.SetActive(false);
        ProcessLinkSupply.SetActive(true);
    }

    // ── 扫码结果 PlayerPrefs Key（与 IncubationCabinWifiSetting 共用）──
    public const string QrJsonPrefsKey = "IncubationCabin_ScannedQrJson";

    // ── 相机扫码 ───────────────────────────────────────────────
    void CheckLocation(Action<LocationPermissionResult> cb)
    {
        StartCoroutine(CheckLocationPermission(cb));
    }

    private IEnumerator CheckLocationPermission(Action<LocationPermissionResult> cb)
    {
        // ② 位置权限（BLE 扫描 Android 侧需要）
        Debug.Log($"{Tag} ② 请求位置权限...");
        bool locDone = false;
        LocationPermissionResult locationPermissionResult = null;
        BleSoftwareSideController.RequestLocationPermission(result =>
        {
            locationPermissionResult = result;
            Debug.Log($"{Tag} ② 位置权限结果: granted={result.granted} locationEnabled={result.locationEnabled}");
            locDone = true;
        });
        while (!locDone) yield return null;
        cb?.Invoke(locationPermissionResult);
    }


    void StartCameraScan()
    {
        Debug.Log($"{Tag} StartCameraScan — 启动扫码");
#if UNITY_EDITOR
        Debug.Log($"{Tag} StartCameraScan — Editor 模式，跳过相机，直接触发 ScanSucc");
        ScanSucc();
        return;
#endif
        if (_scanning)
        {
            Debug.LogWarning($"{Tag} StartCameraScan — 相机已在运行，忽略重复调用");
            return;
        }
        StartCoroutine(RequestCameraAndStart());
    }

    private IEnumerator RequestCameraAndStart()
    {
        // ① 蓝牙开启检查
        Debug.Log($"{Tag} ① 检查蓝牙状态...");
        bool btEnabled = false, btCheckDone = false;
        BleSoftwareSideController.IsBluetoothEnabled(enabled => { btEnabled = enabled; btCheckDone = true; });
        while (!btCheckDone) yield return null;
        Debug.Log($"{Tag} ① 蓝牙状态: {(btEnabled ? "已开启" : "未开启")}");

        if (!btEnabled)
        {
            Debug.Log($"{Tag} ① 请求开启蓝牙...");
            bool btEnableDone = false;
            BleSoftwareSideController.RequestEnableBluetooth(enabled => { btEnabled = enabled; btEnableDone = true; });
            while (!btEnableDone) yield return null;
            Debug.Log($"{Tag} ① 请求开启蓝牙结果: {(btEnabled ? "成功" : "失败")}");

            if (!btEnabled)
                LoggerUtils.LogError($"{Tag} 蓝牙开启失败，WiFi 列表请求可能无法送达硬件");
        }

        // ② 位置权限（BLE 扫描 Android 侧需要）
        Debug.Log($"{Tag} ② 请求位置权限...");
        bool locDone = false;
        BleSoftwareSideController.RequestLocationPermission(result =>
        {
            Debug.Log($"{Tag} ② 位置权限结果: granted={result.granted} locationEnabled={result.locationEnabled}");
            locDone = true;
        });
        while (!locDone) yield return null;

        // ③ BLE 权限（Android 12+ BLUETOOTH_SCAN / BLUETOOTH_CONNECT）
        Debug.Log($"{Tag} ③ 请求 BLE 权限...");
        bool bleDone = false;
        BleSoftwareSideController.RequestBlePermissions(granted =>
        {
            Debug.Log($"{Tag} ③ BLE 权限结果: granted={granted}");
            bleDone = true;
        });
        while (!bleDone) yield return null;
#if UNITY_IOS
        // iOS: 原生 AVFoundation QR 扫码器，完全绕开 Unity WebCamTexture
        Debug.Log($"{Tag} ④ iOS: 原生请求相机权限...");
        bool iosCamGranted = false, iosCamDone = false;
        BleSoftwareSideController.RequestCameraPermission(g => { iosCamGranted = g; iosCamDone = true; });
        while (!iosCamDone) yield return null;
        Debug.Log($"{Tag} ④ iOS: 相机权限结果: {iosCamGranted}");
        if (!iosCamGranted)
        {
            TipPanel.ShowToast("相机权限未授权，请前往系统设置开启");
            Application.OpenURL("app-settings:");
            yield break;
        }

        // 权限已授予，启动原生扫码器，回调里直接触发 ScanSucc
        Debug.Log($"{Tag} ④ iOS: 启动原生 QR 扫码器");
        _scanning = true;
        ScanExitBtn?.gameObject.SetActive(true);
        BleSoftwareSideController.StartNativeCameraQR((success, qrText) =>
        {
            if (this == null) return;
            Debug.Log($"{Tag} iOS 原生扫码结果: success={success} len={qrText?.Length ?? 0}");
            _scanning = false;
            ScanExitBtn?.gameObject.SetActive(false);
            if (success && !string.IsNullOrEmpty(qrText))
            {
                ScannedQrJson = qrText;
                PlayerPrefs.SetString(QrJsonPrefsKey, qrText);
                ScanSucc();
            }
            else
            {
                // 原生扫码界面点击了返回按钮，回到步骤2
                ScanExitBtnOn();
            }
        });
        yield break; // 协程结束，原生扫码器运行中，结果由回调处理
#endif

        // Step 1: Android 相机权限（iOS 由系统在 Step 2 统一处理，跳过此步）
        Debug.Log($"{Tag} RequestCameraAndStart — 请求相机权限...");
        bool cameraGranted = false;
        bool cameraDone = false;
        BleSoftwareSideController.RequestCameraPermission(granted =>
        {
            cameraGranted = granted;
            cameraDone = true;
        });
        while (!cameraDone) yield return null;
        Debug.Log($"{Tag} RequestCameraAndStart — 相机权限: {(cameraGranted ? "已授权" : "被拒绝")}");

        if (!cameraGranted)
        {
            Debug.LogWarning($"{Tag} RequestCameraAndStart — 相机权限被拒绝，中止");
            yield break;
        }

        // Step 2: Unity WebCam 授权（iOS 在此触发系统权限弹窗）
        Debug.Log($"{Tag} RequestCameraAndStart — 请求 WebCam 授权...");
        yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);

        if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
        {
            Debug.LogWarning($"{Tag} RequestCameraAndStart — WebCam 授权被拒绝，中止");
            yield break;
        }

        WebCamDevice[] devices = WebCamTexture.devices;
        Debug.Log($"{Tag} RequestCameraAndStart — 检测到摄像头数量: {devices.Length}");
        if (devices.Length == 0)
        {
            Debug.LogWarning($"{Tag} RequestCameraAndStart — 未检测到摄像头，中止");
            yield break;
        }

        // 优先选后置摄像头
        string camName = devices[0].name;
        for (int i = 0; i < devices.Length; i++)
        {
            if (!devices[i].isFrontFacing) { camName = devices[i].name; break; }
        }
        Debug.Log($"{Tag} RequestCameraAndStart — 选用摄像头: {camName}");

        _camTexture = new WebCamTexture(camName, 1920, 1080, 30);
        _camTexture.Play();

        while (_camTexture.width <= 16) yield return null;

        int camW = _camTexture.width;
        int camH = _camTexture.height;
        int shorter = Mathf.Min(camW, camH);

        // 初始化 3 个裁剪变体
        _cropRects = new RectInt[3];
        _cropW = new int[3];
        _cropH = new int[3];
        _cropBufs = new Color32[3][];
        for (int i = 0; i < 3; i++)
        {
            int side = Mathf.RoundToInt(shorter * CropFactors[i]);
            _cropRects[i] = new RectInt((camW - side) / 2, (camH - side) / 2, side, side);
            _cropW[i] = side / CropStride;
            _cropH[i] = side / CropStride;
            _cropBufs[i] = new Color32[_cropW[i] * _cropH[i]];
            Debug.Log($"{Tag} crop[{i}] factor={CropFactors[i]} side={side} decode={_cropW[i]}x{_cropH[i]}");
        }


        if (CameraPreviewImage != null)
        {
            CameraPreviewImage.texture = _camTexture;
            CameraPreviewImage.uvRect = _camTexture.videoVerticallyMirrored
                ? new Rect(0, 1, 1, -1)
                : new Rect(0, 0, 1, 1);
            CameraPreviewImage.rectTransform.localEulerAngles =
                new Vector3(0, 0, -_camTexture.videoRotationAngle);

            var fitter = CameraPreviewImage.GetComponent<AspectRatioFitter>();
            if (fitter == null)
                fitter = CameraPreviewImage.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            bool isRotated = _camTexture.videoRotationAngle % 180 != 0;
            float ratio = (float)camW / camH;
            fitter.aspectRatio = isRotated ? 1f / ratio : ratio;

            CameraPreviewImage.gameObject.SetActive(true);
        }
        ScanExitBtn?.gameObject.SetActive(true);
        _scanning = true;
        _scanSucceeded = false;
        _decoding = false;
        _pendingResult = null;
        Debug.Log($"{Tag} RequestCameraAndStart — 相机已开启: {camName} {_camTexture.width}x{_camTexture.height} rotation={_camTexture.videoRotationAngle}");
    }

    private void StopCamera()
    {
        if (!_scanning && _camTexture == null) return;
        Debug.Log($"{Tag} StopCamera — 停止并释放相机");
        _scanning = false;
        ScanExitBtn?.gameObject.SetActive(false);
        if (CameraPreviewImage != null)
        {
            CameraPreviewImage.gameObject.SetActive(false);
            CameraPreviewImage.texture = null;
        }

        if (_camTexture != null)
        {
            _camTexture.Stop();
            Destroy(_camTexture);
            _camTexture = null;
        }
    }

    private int _tickScanLogCounter = 0; // 节流：每N次 tick 打一次普通日志

    private void TickScan()
    {
        // 后台线程有结果时，在主线程处理
        if (_pendingResult != null)
        {
            string text = _pendingResult;
            _pendingResult = null;
            _scanSucceeded = true;
            ScannedQrJson = text;
            PlayerPrefs.SetString(QrJsonPrefsKey, text);
            Debug.Log($"{Tag} TickScan — 扫码识别成功: {text.Substring(0, Mathf.Min(80, text.Length))}");
            ScanSucc();
            StopCamera();
            return;
        }

        if (!_scanning || _camTexture == null || !_camTexture.isPlaying || _scanSucceeded) return;
        if (_decoding) return; // 上一帧 Decode 还未结束，跳过

        _scanTimer += Time.deltaTime;
        if (_scanTimer < ScanInterval) return;
        _scanTimer = 0f;

        int camW = _camTexture.width;
        int camH = _camTexture.height;
        if (camW == 0 || camH == 0 || _cropBufs == null)
        {
            Debug.LogWarning($"{Tag} TickScan — 相机尺寸异常或裁剪缓冲未就绪: camW={camW} camH={camH} cropBufs={((_cropBufs == null) ? "null" : "ok")}");
            return;
        }

        // 每 10 次 tick 输出一次心跳日志，避免每帧刷屏
        _tickScanLogCounter++;
        if (_tickScanLogCounter % 10 == 1)
        {
            Debug.Log($"{Tag} TickScan — 正在扫码 cam={camW}x{camH} tick={_tickScanLogCounter}");
        }

        // 一次 GetPixels32，填充 3 个裁剪变体
        Color32[] allPixels = _camTexture.GetPixels32();
        BuildCrops(allPixels, camW);

        // 线程池：依次对 3 张图 Decode，任一成功即记录并退出
        _decoding = true;
        Color32[][] snapBufs = _cropBufs;
        int[] snapW = _cropW, snapH = _cropH;
        int tickIndex = _tickScanLogCounter;
        System.Threading.ThreadPool.QueueUserWorkItem(_ =>
        {
            try
            {
                if (!CheckMqttVersion())
                {
                    Debug.LogWarning($"{Tag} TickScan[{tickIndex}] — CheckMqttVersion 返回 false，跳过 Decode（设备版本不满足 1.0.19）");
                    _decoding = false;
                    return;
                }
                EnsureBarcodeReader();
                if (_reader == null)
                {
                    Debug.LogError($"{Tag} TickScan[{tickIndex}] — EnsureBarcodeReader 后 _reader 仍为 null，无法 Decode");
                    _decoding = false;
                    return;
                }
                var decodeMethod = _reader.GetType().GetMethod("Decode", new[] { typeof(Color32[]), typeof(int), typeof(int) });
                if (decodeMethod == null)
                {
                    Debug.LogError($"{Tag} TickScan[{tickIndex}] — 反射未找到 BarcodeReader.Decode(Color32[], int, int) 方法");
                    _decoding = false;
                    return;
                }
                bool anyDecode = false;
                for (int i = 0; i < 3; i++)
                {
                    var r = decodeMethod.Invoke(_reader, new object[] { snapBufs[i], snapW[i], snapH[i] });
                    if (r != null)
                    {
                        var text = (string)r.GetType().GetProperty("Text")?.GetValue(r);
                        _pendingResult = text;
                        Debug.Log($"{Tag} TickScan[{tickIndex}] — Decode 成功 crop[{i}] factor={CropFactors[i]} size={snapW[i]}x{snapH[i]} text={text?.Substring(0, Mathf.Min(80, text?.Length ?? 0))}");
                        anyDecode = true;
                        break;
                    }
                }
                if (!anyDecode && tickIndex % 10 == 1)
                {
                    Debug.Log($"{Tag} TickScan[{tickIndex}] — 3 个 crop 均未识别到二维码 sizes={snapW[0]}x{snapH[0]}/{snapW[1]}x{snapH[1]}/{snapW[2]}x{snapH[2]}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"{Tag} TickScan[{tickIndex}] — Decode 线程异常: {e.GetType().Name} — {e.Message}\n{e.StackTrace}");
            }
            finally { _decoding = false; }
        });
    }

    // 从 src 中提取 3 个裁剪区，每个以 CropStride 步长下采样写入对应 buffer
    private void BuildCrops(Color32[] src, int srcW)
    {
        for (int c = 0; c < 3; c++)
        {
            RectInt crop = _cropRects[c];
            int dw = _cropW[c], dh = _cropH[c];
            Color32[] dst = _cropBufs[c];
            for (int y = 0; y < dh; y++)
            {
                int srcBase = (crop.y + y * CropStride) * srcW + crop.x;
                int dstBase = y * dw;
                for (int x = 0; x < dw; x++)
                    dst[dstBase + x] = src[srcBase + x * CropStride];
            }
        }
    }

    // ──────────────────────────────────────────────────────────
    void ScanSucc()
    {
#if UNITY_EDITOR
        ScannedQrJson = "AQAAAAAAaiJf-AxUQUItRDIxNzQxQjk0XVuYnopN0IVi0GGskHc-AAD_8AAAEACAAACAX5s0-5mKocdzcbVAI6p4cICIKe8GDctCWLGZOFCmYH6-zbRMDFRBQi1EMjE3NDFCOQ";
#endif
        ScanQRCodes.SetActive(false);
        LinkPanle.SetActive(true);

        var qrData = QRPayloadEncoder.Decode(ScannedQrJson);
        // 解析 pipe-delimited 格式，再转为 JSON 供下游使用
        // var qrData = ParsePipeQrCode(ScannedQrJson);
        string deviceId = qrData?.deviceId ?? "";
        foreach (var tex in DeviceIdName)
        {
            tex.text = deviceId;
        }

        // 在 ScannedQrJson 被转成 JSON 之前，缓存原始编码字符串并发布 MQTT retain
        // 若 MQTT 尚未连接，CacheQrCodeForRetain 内部会暂存，待 SubscribeBox 时补发
        if (!string.IsNullOrEmpty(deviceId))
        {
            CabinBoxManager.Inst.CacheQrCodeForRetain(deviceId, ScannedQrJson);
        }

        string qrJson = "";
        try { qrJson = JsonConvert.SerializeObject(qrData); } catch { }

        if (!string.IsNullOrEmpty(qrJson))
        {
            ScannedQrJson = qrJson;
            PlayerPrefs.SetString(QrJsonPrefsKey, qrJson);
        }

        Debug.Log($"{Tag} ScanSucc — 扫码完成: deviceId={deviceId}，直接发起 BLE 连接");

        // 直接进入连接中状态
        waitConnectGo.SetActive(false);
        connectingGo.SetActive(false);
        connectedSucGo.SetActive(false);
        connectedFailGo.SetActive(false);
        waitSureGo.SetActive(true);

#if UNITY_EDITOR
        Debug.Log($"{Tag} ScanSucc — Editor 模式，模拟 BLE 连接成功");
        TimerManager.Inst.RunOnce("waitSure", 2, () =>
        {
            if (this != null)
            {
                OnBleConnectResult(true, "");
            }
        });
        return;
#endif

        Debug.Log($"{Tag} ScanSucc — 调用 ScanAndConnectDevice，qrJson={ScannedQrJson.Substring(0, Mathf.Min(60, ScannedQrJson.Length))}");
        BleSoftwareSideController.ScanAndConnectDevice(ScannedQrJson, OnBleConnectResult);
    }

    private static QrCodeData ParsePipeQrCode(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return null;
        var parts = raw.Split('|');
        if (parts.Length < 8) return null;
        return new QrCodeData
        {
            version = int.TryParse(parts[0], out int v) ? v : 0,
            transport = parts[1],
            deviceId = parts[2],
            serviceUuid = parts[3],
            advHint = parts[4],
            bindToken = parts[5],
            expireAt = long.TryParse(parts[6], out long t) ? t : 0,
            signature = parts[7]
        };
    }

    /// <summary>BLE 连接结果回调</summary>
    private void OnBleConnectResult(bool ok, string msg)
    {
        Debug.Log($"{Tag} OnBleConnectResult — ok={ok} msg={msg}");
        if (msg.Contains("成功"))
        {
            waitConnectGo.SetActive(true);
            waitSureGo.SetActive(false);
            Debug.Log($"{Tag} OnBleConnectResult — BLE 连接成功，订阅 OnDataReceived，等待 doubleclick");
            BleSoftwareSideController.OnDataReceived += OnBleDataReceived;
        }
        else
        {
            if (msg == "连接断开")
            {
                BleSoftwareSideController.DisableBluetooth();
            }
            LoggerUtils.LogError($"{Tag} BLE 连接失败: {msg}");
            waitConnectGo.SetActive(false);
            waitSureGo.SetActive(false);
            connectedFailGo.SetActive(true);
        }
    }

    /// <summary>接收到蓝牙设备推送的数据，等待 "doubleclick" 消息</summary>
    private void OnBleDataReceived(string raw)
    {
        Debug.Log($"{Tag} OnBleDataReceived — 收到数据: {raw.Substring(0, Mathf.Min(120, raw.Length))}");
        try
        {
            var bleMsg = JsonConvert.DeserializeObject<BleDeviceMessage>(raw);
            if (bleMsg?.type == BleDeviceMessageType.DoubleClick)
            {
                Debug.Log($"{Tag} OnBleDataReceived — 识别为 doubleclick，取消订阅并执行跳转");
                BleSoftwareSideController.OnDataReceived -= OnBleDataReceived;
                OnDoubleClickToConnect();
            }
            else
            {
                Debug.Log($"{Tag} OnBleDataReceived — 消息类型={bleMsg?.type}，忽略");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"{Tag} OnBleDataReceived — 解析失败: {e.Message}");
        }
    }

    /// <summary>收到蓝牙 doubleclick 确认后，导航到 WiFi 配置页</summary>
    private void OnDoubleClickToConnect()
    {
        waitConnectGo.SetActive(false);
        connectingGo.SetActive(true);
        TimerManager.Inst.RunOnce("OnDoubleClickToConnect", 2, () =>
        {
            if (this == null) return;
            Debug.Log($"{Tag} OnDoubleClickToConnect — 显示连接成功，2s 后跳转 WifiSetting");
            BackBtn.interactable = false;
            TipPanel.ShowToast("连接成功");
            connectingGo.SetActive(false);
            connectedSucGo.SetActive(true);
            TimerManager.Inst.RunOnce("scanSucc", 2, () =>
            {
                Debug.Log($"{Tag} OnDoubleClickToConnect — 跳转到 IncubationCabinWifiSetting");
                CloseSelf();
                UIManager.Inst.OpenPanel(PanelId.IncubationCabinWifiSetting);
            });
        });
    }

    private void ScanExitBtnOn()
    {
        Debug.Log($"{Tag} ScanExitBtnOn — 退出扫码，返回步骤2");
        StopCamera();
        ScanQRCodes.SetActive(false);
        ProcessPowerOn.SetActive(true);
    }

    /// <summary>连接失败后，点击重扫按钮：隐藏 LinkPanle，直接回到扫码流程</summary>
    private void ReScanBtnOn()
    {
        CheckLocation(() =>
        {
            Debug.Log($"{Tag} ReScanBtnOn — 重新扫码，重置所有状态");
            BleSoftwareSideController.OnDataReceived -= OnBleDataReceived;

            // 重置 LinkPanle 内所有子状态并隐藏
            LinkPanle.SetActive(false);
            waitConnectGo.SetActive(false);
            connectingGo.SetActive(false);
            connectedSucGo.SetActive(false);
            connectedFailGo.SetActive(false);

            BackBtn.interactable = true;

            // 重置扫码状态
            _scanSucceeded = false;
            ScannedQrJson = "";

            // 直接展示扫码界面并启动相机
            ScanQRCodes.SetActive(true);
            StartCameraScan();
        });
    }


    [Button("模拟蓝牙 doubleclick")]
    void TestBleDoubleClick()
    {
        Debug.Log($"{Tag} [Test] 模拟蓝牙 doubleclick");
        OnDoubleClickToConnect();
    }

    [Button("测试扫码成功（直连）")]
    void TestScanSucc()
    {
        ScanSucc(); return;
#if UNITY_EDITOR
        Debug.Log($"{Tag} [Test] 测试扫码成功 — 模拟 BLE 已连接，等待 doubleclick");
        var mock = ParsePipeQrCode("1|BLE|TestDevice_001|00001800-0000-1000-8000-00805f9b34fb|advHint|bindToken|9999999999|signature");
        ScannedQrJson = JsonConvert.SerializeObject(mock);
        PlayerPrefs.SetString(QrJsonPrefsKey, ScannedQrJson);
        StopCamera();
        ScanQRCodes.SetActive(false);
        LinkPanle.SetActive(true);

        waitConnectGo.SetActive(false);
        connectingGo.SetActive(false);
        connectedSucGo.SetActive(true);
        connectedFailGo.SetActive(false);
        Debug.Log($"{Tag} [Test] BLE 已连接，可点击「模拟蓝牙 doubleclick」继续流程");
#endif
    }

    [Button("测试连接失败")]
    void TestScanFail()
    {
#if UNITY_EDITOR
        Debug.Log($"{Tag} [Test] 测试连接失败");
        var mock = ParsePipeQrCode("1|BLE|TestDevice_001|00001800-0000-1000-8000-00805f9b34fb|advHint|bindToken|9999999999|signature");
        ScannedQrJson = JsonConvert.SerializeObject(mock);
        PlayerPrefs.SetString(QrJsonPrefsKey, ScannedQrJson);
        StopCamera();
        ScanQRCodes.SetActive(false);
        LinkPanle.SetActive(true);

        waitConnectGo.SetActive(false);
        connectingGo.SetActive(false);
        connectedSucGo.SetActive(false);
        connectedFailGo.SetActive(true);
#endif
    }

    [Button("测试蓝牙连接成功")]
    void TestBleConnectResult()
    {
#if UNITY_EDITOR
        Debug.Log($"{Tag} [Test] 模拟 BLE 连接成功回调");
        OnBleConnectResult(true, "");
        UIManager.Inst.OpenPanel(PanelId.IncubationCabinControll);
#endif
    }

    [Button("绑定测试")]
    void BindTest()
    {
        CabinBoxManager.Inst.BindCabinBox("BUD-E02EDBF8"); //009
        CabinBoxManager.Inst.BindCabinBox("BUD-12345678"); //009
        // CabinBoxManager.Inst.BindCabinBox("BUD-269BF2A9"); //009
        // CabinBoxManager.Inst.BindCabinBox("BUD-E02EDBF8"); //009
        // CabinBoxManager.Inst.BindCabinBox("BUD-13EDD13E"); //012
    }
    [Button("解绑测试")]
    void UnBindTest()
    {
        CabinBoxManager.Inst.UnbindBudBox("BUD-2D6429E1");
    }

    [Button("测试解码")]
    void testtt()
    {
        ScannedQrJson = "AQAAAAAAaiJumNIXQbk0XVuYnopN0IVi0GGskHc-AAD_8AAAEACAAACAX5s0-5mKocdzcbVAI6p4cICIKe8GDctCWLGZOFCmYH6-zbRMDFRBQi1EMjE3NDFCOQ";

        var qrData = QRPayloadEncoder.Decode(ScannedQrJson);
        Debug.LogError(JsonConvert.SerializeObject(qrData));
    }

    [Button("测试")]
    void test()
    {
        string qrCode = "AQAAAAHoEojSLxPt0T4EHsHXbsVFw4OlmZRErB1TAAD_8AAAEACAAACAX5s0-8o39H4-kF25cteCcv5su06F98g7dMqt-KlqdFktDQZxDEJVRC0xM0VERDEzRQ";
        var qrCodeData = QRPayloadEncoder.Decode(qrCode);
        Debug.LogError(JsonConvert.SerializeObject(qrCodeData));
    }

}
