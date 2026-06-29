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
//     private Texture2D _scanTex;        // 送给 ZXing + 显示在 scanRawImg 的裁剪预览
//     private RectInt _scanCropRect;     // 摄像头原始帧上的中央裁剪区（像素坐标）
//     private Color32[] _cropBuffer;     // 可复用：裁剪 + 2x 下采样后的像素
//     private int _decodeW, _decodeH;   // _cropBuffer 对应的宽高
//     private bool _scanning = false;
//     private bool _scanSucceeded = false;
//     private float _scanTimer = 0f;
//     private const float ScanInterval = 0.03f;

//     private volatile bool _decoding = false;
//     private volatile string _pendingResult = null;

//     public string ScannedQrJson { get; private set; } = "";
//     public const string QrJsonPrefsKey = "IncubationCabin_ScannedQrJson";

//     private const string Tag = "[LinkBox]";

//     // AutoRotate=true 保留（摄像头原始像素方向可能与显示方向不同）
//     // TryHarder=false：二维码显示在干净的屏幕上，不需要最慢的扫描算法
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
//             var srt = scanRawImg.rectTransform;
//             srt.anchorMin = Vector2.zero;
//             srt.anchorMax = Vector2.zero;
//             srt.pivot = Vector2.zero;
//             srt.anchoredPosition = new Vector2(10f, 10f);
//             srt.sizeDelta = new Vector2(270f, 270f);
//             scanRawImg.uvRect = new Rect(0, 0, 1, 1);
//             scanRawImg.gameObject.SetActive(false);
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

//         // 取短边的 60% 作为正方形裁剪区，居中
//         int side = Mathf.RoundToInt(Mathf.Min(camW, camH) * 0.6f);
//         _scanCropRect = new RectInt((camW - side) / 2, (camH - side) / 2, side, side);

//         // 2x 下采样后的解码尺寸（约为 324×324 @ 1080p）
//         _decodeW = side / 2;
//         _decodeH = side / 2;
//         _cropBuffer = new Color32[_decodeW * _decodeH];

//         if (_scanTex != null) Destroy(_scanTex);
//         _scanTex = new Texture2D(_decodeW, _decodeH, TextureFormat.RGBA32, false);

//         if (CameraPreviewImage != null)
//         {
//             CameraPreviewImage.texture = _camTexture;
//             CameraPreviewImage.uvRect = _camTexture.videoVerticallyMirrored
//                 ? new Rect(0, 1, 1, -1)
//                 : new Rect(0, 0, 1, 1);
//             CameraPreviewImage.rectTransform.localEulerAngles =
//                 new Vector3(0, 0, -_camTexture.videoRotationAngle);

//             var fitter = CameraPreviewImage.GetComponent<AspectRatioFitter>();
//             if (fitter != null)
//             {
//                 bool isRotated = _camTexture.videoRotationAngle % 180 != 0;
//                 float ratio = (float)camW / camH;
//                 fitter.aspectRatio = isRotated ? 1f / ratio : ratio;
//             }

//             CameraPreviewImage.gameObject.SetActive(true);
//         }

//         if (scanRawImg != null)
//         {
//             scanRawImg.texture = _scanTex;
//             scanRawImg.gameObject.SetActive(true);
//         }

//         ScanExitBtn?.gameObject.SetActive(true);
//         _scanning = true;
//         _scanSucceeded = false;
//         _decoding = false;
//         _pendingResult = null;
//         Debug.Log($"{Tag} RequestCameraAndStart — 相机已开启: {camName} {camW}x{camH} " +
//                   $"crop={_scanCropRect} decode={_decodeW}x{_decodeH} rotation={_camTexture.videoRotationAngle}");
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

//         if (scanRawImg != null)
//         {
//             scanRawImg.texture = null;
//             scanRawImg.gameObject.SetActive(false);
//         }

//         if (_scanTex != null)
//         {
//             Destroy(_scanTex);
//             _scanTex = null;
//         }

//         _cropBuffer = null;
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
//         if (camW == 0 || camH == 0 || _cropBuffer == null) return;

//         // 从整帧中裁剪中央区域并 2x 下采样
//         Color32[] allPixels = _camTexture.GetPixels32();
//         CropAndDownsample2x(allPixels, camW);

//         // 更新 scanRawImg 调试预览（主线程，Apply 之后立即可见）
//         if (scanRawImg != null && _scanTex != null && scanRawImg.gameObject.activeSelf)
//         {
//             _scanTex.SetPixels32(_cropBuffer);
//             _scanTex.Apply(false);
//         }

//         // 在线程池解码（_decoding=true 期间主线程不会再写 _cropBuffer）
//         _decoding = true;
//         int snapW = _decodeW, snapH = _decodeH;
//         Color32[] snapBuf = _cropBuffer;
//         System.Threading.ThreadPool.QueueUserWorkItem(_ =>
//         {
//             try
//             {
//                 Result result = _reader.Decode(snapBuf, snapW, snapH);
//                 if (result != null)
//                     _pendingResult = result.Text;
//             }
//             catch { }
//             finally { _decoding = false; }
//         });
//     }

//     // 将 src 中的 _scanCropRect 区域以 2x 步长采样写入 _cropBuffer
//     private void CropAndDownsample2x(Color32[] src, int srcW)
//     {
//         RectInt crop = _scanCropRect;
//         int dw = _decodeW, dh = _decodeH;
//         Color32[] dst = _cropBuffer;
//         const int step = 2;
//         for (int y = 0; y < dh; y++)
//         {
//             int srcBase = (crop.y + y * step) * srcW + crop.x;
//             int dstBase = y * dw;
//             for (int x = 0; x < dw; x++)
//                 dst[dstBase + x] = src[srcBase + x * step];
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
