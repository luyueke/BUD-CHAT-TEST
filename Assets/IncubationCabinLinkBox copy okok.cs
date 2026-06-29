// using System.Collections;
// using UnityEngine;
// using UnityEngine.UI;
// using ZXing;

// public class IncubationCabinLinkBox : MonoBehaviour
// {
//     [SerializeField] RawImage CameraPreviewImage;
//     [SerializeField] RawImage scanRawImg;
//     [SerializeField] Button ScanbeginBtn;
//     [SerializeField] Button ScanExitBtn;

//     private WebCamTexture _camTexture;

//     // 3 个裁剪变体：短边的 40% / 60% / 80%，stride=2 下采样
//     // [0] 近距离（QR 几乎铺满视野）  [1] 中距离  [2] 远距离
//     private static readonly float[] CropFactors = { 0.4f, 0.6f, 0.8f };
//     private const int CropStride = 2;
//     private RectInt[] _cropRects;
//     private int[]     _cropW, _cropH;
//     private Color32[][] _cropBufs;

//     private bool _scanning = false;
//     private bool _scanSucceeded = false;
//     private float _scanTimer = 0f;
//     private const float ScanInterval = 0.03f;

//     private volatile bool _decoding = false;
//     private volatile string _pendingResult = null;

//     public string ScannedQrJson { get; private set; } = "";
//     public const string QrJsonPrefsKey = "IncubationCabin_ScannedQrJson";

//     private const string Tag = "[LinkBox]";

//     private readonly IBarcodeReader _reader = new BarcodeReader
//     {
//         AutoRotate = true,
//         Options = new ZXing.Common.DecodingOptions
//         {
//             TryHarder = false,
//             PossibleFormats = new System.Collections.Generic.List<BarcodeFormat> { BarcodeFormat.QR_CODE }
//         }
//     };

//     public void Awake()
//     {
//         Screen.orientation = ScreenOrientation.LandscapeLeft;

//         var rt = CameraPreviewImage.rectTransform;
//         rt.anchorMin = Vector2.zero;
//         rt.anchorMax = Vector2.one;
//         rt.offsetMin = Vector2.zero;
//         rt.offsetMax = Vector2.zero;

//         if (scanRawImg != null)
//         {
//             // var srt = scanRawImg.rectTransform;
//             // srt.anchorMin = Vector2.zero;
//             // srt.anchorMax = Vector2.zero;
//             // srt.pivot = Vector2.zero;
//             // srt.anchoredPosition = new Vector2(10f, 10f);
//             // srt.sizeDelta = new Vector2(270f, 270f);
//             // scanRawImg.uvRect = new Rect(0, 0, 1, 1);
//             // scanRawImg.gameObject.SetActive(false);
//         }

//         ScanbeginBtn.onClick.AddListener(StartCameraScan);
//     }

//     void Update()
//     {
//         TickScan();
//     }

//     void OnDestroy()
//     {
//         StopCamera();
//     }

//     void StartCameraScan()
//     {
//         Debug.Log($"{Tag} StartCameraScan — 启动扫码");
// #if UNITY_EDITOR
//         Debug.Log($"{Tag} StartCameraScan — Editor 模式，跳过相机，直接触发 ScanSucc");
//         ScanSucc();
//         return;
// #endif
//         if (_scanning)
//         {
//             Debug.LogWarning($"{Tag} StartCameraScan — 相机已在运行，忽略重复调用");
//             return;
//         }
//         StartCoroutine(RequestCameraAndStart());
//     }

//     private IEnumerator RequestCameraAndStart()
//     {
//         Debug.Log($"{Tag} RequestCameraAndStart — 请求 WebCam 授权...");
//         yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);

//         if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
//         {
//             Debug.LogWarning($"{Tag} RequestCameraAndStart — WebCam 授权被拒绝，中止");
//             yield break;
//         }

//         WebCamDevice[] devices = WebCamTexture.devices;
//         Debug.Log($"{Tag} RequestCameraAndStart — 检测到摄像头数量: {devices.Length}");
//         if (devices.Length == 0)
//         {
//             Debug.LogWarning($"{Tag} RequestCameraAndStart — 未检测到摄像头，中止");
//             yield break;
//         }

//         string camName = devices[0].name;
//         for (int i = 0; i < devices.Length; i++)
//         {
//             if (!devices[i].isFrontFacing) { camName = devices[i].name; break; }
//         }
//         Debug.Log($"{Tag} RequestCameraAndStart — 选用摄像头: {camName}");

//         _camTexture = new WebCamTexture(camName, 1920, 1080, 30);
//         _camTexture.Play();

//         while (_camTexture.width <= 16) yield return null;

//         int camW = _camTexture.width;
//         int camH = _camTexture.height;
//         int shorter = Mathf.Min(camW, camH);

//         // 初始化 3 个裁剪变体
//         _cropRects = new RectInt[3];
//         _cropW     = new int[3];
//         _cropH     = new int[3];
//         _cropBufs  = new Color32[3][];
//         for (int i = 0; i < 3; i++)
//         {
//             int side = Mathf.RoundToInt(shorter * CropFactors[i]);
//             _cropRects[i] = new RectInt((camW - side) / 2, (camH - side) / 2, side, side);
//             _cropW[i] = side / CropStride;
//             _cropH[i] = side / CropStride;
//             _cropBufs[i] = new Color32[_cropW[i] * _cropH[i]];
//             Debug.Log($"{Tag} crop[{i}] factor={CropFactors[i]} side={side} decode={_cropW[i]}x{_cropH[i]}");
//         }



//         if (CameraPreviewImage != null)
//         {
//             CameraPreviewImage.texture = _camTexture;
//             CameraPreviewImage.uvRect = _camTexture.videoVerticallyMirrored
//                 ? new Rect(0, 1, 1, -1)
//                 : new Rect(0, 0, 1, 1);
//             CameraPreviewImage.rectTransform.localEulerAngles =
//                 new Vector3(0, 0, -_camTexture.videoRotationAngle);

//             var fitter = CameraPreviewImage.GetComponent<AspectRatioFitter>();
//             if (fitter == null)
//                 fitter = CameraPreviewImage.gameObject.AddComponent<AspectRatioFitter>();
//             fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
//             bool isRotated = _camTexture.videoRotationAngle % 180 != 0;
//             float ratio = (float)camW / camH;
//             fitter.aspectRatio = isRotated ? 1f / ratio : ratio;

//             CameraPreviewImage.gameObject.SetActive(true);
//         }

    

//         ScanExitBtn?.gameObject.SetActive(true);
//         _scanning = true;
//         _scanSucceeded = false;
//         _decoding = false;
//         _pendingResult = null;
//         Debug.Log($"{Tag} RequestCameraAndStart — 相机已开启: {camName} {camW}x{camH} rotation={_camTexture.videoRotationAngle}");
//     }

//     private void StopCamera()
//     {
//         if (!_scanning && _camTexture == null) return;
//         Debug.Log($"{Tag} StopCamera — 停止并释放相机");
//         _scanning = false;
//         ScanExitBtn?.gameObject.SetActive(false);

//         if (CameraPreviewImage != null)
//         {
//             CameraPreviewImage.gameObject.SetActive(false);
//             CameraPreviewImage.texture = null;
//         }

//         if (_camTexture != null)
//         {
//             _camTexture.Stop();
//             Destroy(_camTexture);
//             _camTexture = null;
//         }

    
//         _cropBufs = null;
//     }

//     private void TickScan()
//     {
//         if (_pendingResult != null)
//         {
//             string text = _pendingResult;
//             _pendingResult = null;
//             ScannedQrJson = text;
//             PlayerPrefs.SetString(QrJsonPrefsKey, text);
//             Debug.Log($"{Tag} TickScan — 扫码识别成功: {text.Substring(0, Mathf.Min(80, text.Length))}");
//             ScanSucc();
//         }

//         if (!_scanning || _camTexture == null || !_camTexture.isPlaying || _scanSucceeded) return;
//         if (_decoding) return;

//         _scanTimer += Time.deltaTime;
//         if (_scanTimer < ScanInterval) return;
//         _scanTimer = 0f;

//         int camW = _camTexture.width;
//         int camH = _camTexture.height;
//         if (camW == 0 || camH == 0 || _cropBufs == null) return;

//         // 一次 GetPixels32，填充 3 个裁剪变体
//         Color32[] allPixels = _camTexture.GetPixels32();
//         BuildCrops(allPixels, camW);



//         // 线程池：依次对 3 张图 Decode，任一成功即记录并退出
//         _decoding = true;
//         Color32[][] snapBufs = _cropBufs;
//         int[] snapW = _cropW, snapH = _cropH;
//         System.Threading.ThreadPool.QueueUserWorkItem(_ =>
//         {
//             try
//             {
//                 for (int i = 0; i < 3; i++)
//                 {
//                     Result r = _reader.Decode(snapBufs[i], snapW[i], snapH[i]);
//                     if (r != null)
//                     {
//                         _pendingResult = r.Text;
//                         Debug.Log($"{Tag} Decode 成功 crop[{i}] factor={CropFactors[i]} size={snapW[i]}x{snapH[i]}");
//                         break;
//                     }
//                 }
//             }
//             catch { }
//             finally { _decoding = false; }
//         });
//     }

//     // 从 src 中提取 3 个裁剪区，每个以 CropStride 步长下采样写入对应 buffer
//     private void BuildCrops(Color32[] src, int srcW)
//     {
//         for (int c = 0; c < 3; c++)
//         {
//             RectInt crop = _cropRects[c];
//             int dw = _cropW[c], dh = _cropH[c];
//             Color32[] dst = _cropBufs[c];
//             for (int y = 0; y < dh; y++)
//             {
//                 int srcBase = (crop.y + y * CropStride) * srcW + crop.x;
//                 int dstBase = y * dw;
//                 for (int x = 0; x < dw; x++)
//                     dst[dstBase + x] = src[srcBase + x * CropStride];
//             }
//         }
//     }

//     void ScanSucc()
//     {
// #if UNITY_EDITOR
//         ScannedQrJson = "{\"deviceId\":\"TestDevice_001\",\"macAddress\":\"AA:BB:CC:DD:EE:FF\"}";
//         PlayerPrefs.SetString(QrJsonPrefsKey, ScannedQrJson);
// #endif
//         Debug.Log($"{Tag} ScanSucc — 扫码完成: ScannedQrJson");
//     }

//     private void ScanExitBtnOn()
//     {
//         Debug.Log($"{Tag} ScanExitBtnOn — 退出扫码");
//         StopCamera();
//     }
// }
