using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScreenOrientationHelper : MonoBehaviour
{
    private static ScreenOrientationHelper _inst;
    public static ScreenOrientationHelper Inst
    {
        get
        {
            if (_inst == null)
            {
                var go = new GameObject("[ScreenOrientationHelper]");
                DontDestroyOnLoad(go);
                _inst = go.AddComponent<ScreenOrientationHelper>();
            }
            return _inst;
        }
    }

    private void Awake()
    {
        if (_inst != null && _inst != this) { Destroy(gameObject); return; }
        _inst = this;
        DontDestroyOnLoad(gameObject);
    }


    private Coroutine _currentCoroutine;
    private GameObject _overlayCanvasGO;
    private GameObject _hiddenCanvas;
    private GameObject _hiddenBgCamera;

    public void Switch(ScreenOrientation target, Action onComplete = null, Action onPreRotate = null)
    {
        if (_currentCoroutine is not null)
        {
            StopCoroutine(_currentCoroutine);
            Cleanup();
        }

#if UNITY_IOS
         // 新底包（>= 1.0.20）使用 SwitchCoroutineNew：原生截图覆盖层隐藏旋转动画
         // 旧底包使用 SwitchCoroutine：无原生覆盖，旋转动画可见但功能正常
         if (DeviceInfoManager.Inst.CheckVersion_1_0_20())
         {
             _currentCoroutine = StartCoroutine(SwitchCoroutineNew(target, onComplete, onPreRotate));
             return;
         }
#endif
        _currentCoroutine = StartCoroutine(SwitchCoroutine(target, onComplete, onPreRotate));
    }

    public void Cancel()
    {
        if (_currentCoroutine is not null)
        {
            StopCoroutine(_currentCoroutine);
            _currentCoroutine = null;
        }
        Cleanup();
    }

    private void Cleanup()
    {
        if (_hiddenCanvas    is not null) { _hiddenCanvas.SetActive(true);    _hiddenCanvas    = null; }
        if (_hiddenBgCamera  is not null) { _hiddenBgCamera.SetActive(true);  _hiddenBgCamera  = null; }
        if (_overlayCanvasGO is not null) { Destroy(_overlayCanvasGO);        _overlayCanvasGO = null; }
    }


    // 底包 >= 1.0.20 专用。SOH_DisableUIKitAnimation 添加黑色 UIView 覆盖层，
    // 旋转动画在全黑背后进行视觉上不可见。SOH_EnableUIKitAnimation 淡出移除覆盖层。
    private IEnumerator SwitchCoroutineNew(ScreenOrientation target, Action onComplete, Action onPreRotate)
    {
        bool isPortrait = target == ScreenOrientation.Portrait || target == ScreenOrientation.PortraitUpsideDown;
        Debug.Log($"[SOH] ▶ NEW START target={target} isPortrait={isPortrait} Screen={Screen.width}x{Screen.height}");

        string sourceImgPath = isPortrait
            ? "Assets/Loadable/UI/UIPanel/AICompanionChatPanel/Landscape.png"
            : "Assets/Loadable/UI/UIPanel/AICompanionChatPanel/Portrait.png";
        string targetImgPath = isPortrait
            ? "Assets/Loadable/UI/UIPanel/AICompanionChatPanel/Portrait.png"
            : "Assets/Loadable/UI/UIPanel/AICompanionChatPanel/Landscape.png";

        // ── 1. 创建全屏截图覆盖层 ─────────────────────────────────────────────────
        _overlayCanvasGO = new GameObject("ScreenOrientationOverlay");
        var overlayCanvas = _overlayCanvasGO.AddComponent<Canvas>();
        overlayCanvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = 9999;

        var coverGO = new GameObject("CoverImage");
        coverGO.transform.SetParent(_overlayCanvasGO.transform, false);
        var coverRT = coverGO.AddComponent<RectTransform>();
        coverRT.anchorMin = Vector2.zero;
        coverRT.anchorMax = Vector2.one;
        coverRT.offsetMin = coverRT.offsetMax = Vector2.zero;
        var cg  = coverGO.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        var img = coverGO.AddComponent<Image>();
        img.sprite = Loader.Load<Sprite>(sourceImgPath, gameObject);

        // ── 2. 淡入 0→1（0.2 s）──────────────────────────────────────────────────
        const float fadeDur = 0.2f;
        float fadeInv = 1f / fadeDur;
        float elapsed = 0f;
        while (elapsed < fadeDur)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Clamp01(elapsed * fadeInv);
            yield return null;
        }
        cg.alpha = 1f;
        Debug.Log($"[SOH] NEW fade-in done Screen={Screen.width}x{Screen.height}");

        // ── 3. 等帧末确保 Metal 已提交 sourceImg 帧 ────────────────────────────────
        yield return new WaitForEndOfFrame();

        // ── 4. 隐藏场景内容 ───────────────────────────────────────────────────────
        var mainCanvasGO = GameObject.Find("Canvas");
        if (mainCanvasGO is not null) { _hiddenCanvas = mainCanvasGO; mainCanvasGO.SetActive(false); }
        var bgCameraGO = GameObject.Find("BgCamera(Clone)");
        if (bgCameraGO is not null) { _hiddenBgCamera = bgCameraGO; bgCameraGO.SetActive(false); }

        SetAutorotate(isPortrait);
        try { onPreRotate?.Invoke(); } catch (Exception e) { Debug.LogError($"[SOH] onPreRotate: {e}"); }

        // ── 5. 再等一帧确保场景隐藏后的 Metal 帧已提交 ─────────────────────────────
        yield return new WaitForEndOfFrame();

        // ── 6. 通知原生添加黑色覆盖层，隐藏 iOS 旋转动画 ─────────────────────────
        MobileInterface.Instance.SendMessage(MobileInterfaceDefine.SOH_DisableUIKitAnimation, "");
        Debug.Log($"[SOH] NEW 原生覆盖层已添加");

        // ── 7. 等帧末让 UIKit 合成 UIView ────────────────────────────────────────
        yield return new WaitForEndOfFrame();

        // ── 8. 请求旋转 ───────────────────────────────────────────────────────────
        Debug.Log($"[SOH] NEW Screen.orientation = {target} Screen={Screen.width}x{Screen.height}");
        Screen.orientation = target;

        yield return null;
        yield return null;

        // ── 9. 等待 Unity 报告新屏幕尺寸（最多 2 s）─────────────────────────────
        int loopCount = 0;
        float timeout = 2f;
        while (timeout > 0f)
        {
            bool correct = isPortrait ? Screen.height > Screen.width : Screen.width > Screen.height;
            if (correct) break;
            timeout -= Time.deltaTime;
            loopCount++;
            yield return null;
        }
        Debug.Log($"[SOH] NEW orient-wait done loopCount={loopCount} timeout={timeout:F3} Screen={Screen.width}x{Screen.height}");

        yield return new WaitForEndOfFrame();
        //Canvas.ForceUpdateCanvases();

        // ── 10. 更新 CanvasScaler 参考分辨率，换目标方向截图 ─────────────────────
        if (mainCanvasGO)
        {
            var cs = mainCanvasGO.GetComponent<CanvasScaler>();
            if (cs) cs.referenceResolution = isPortrait ? new Vector2(1125, 2436) : new Vector2(2436, 1125);
            if (cs) cs.matchWidthOrHeight  = isPortrait ? 1f : 0f;
        }
        img.sprite = Loader.Load<Sprite>(targetImgPath, gameObject);
        Debug.Log($"[SOH] NEW target image set Screen={Screen.width}x{Screen.height}");

        // ── 11. 恢复场景，回调 ────────────────────────────────────────────────────
        if (_hiddenCanvas)   { _hiddenCanvas.SetActive(true);   _hiddenCanvas   = null; }
        if (_hiddenBgCamera) { _hiddenBgCamera.SetActive(true); _hiddenBgCamera = null; }
        try { onComplete?.Invoke(); } catch (Exception e) { Debug.LogError($"[SOH] onComplete: {e}"); }

        // ── 12. 通知原生移除黑色覆盖层（淡出 0.15 s），露出 targetImg ────────────
        MobileInterface.Instance.SendMessage(MobileInterfaceDefine.SOH_EnableUIKitAnimation, "");
        Debug.Log($"[SOH] NEW 原生覆盖层移除");

        yield return null;

        // ── 13. 淡出 C# 覆盖层 1→0 ──────────────────────────────────────────────
        elapsed = 0f;
        while (elapsed < fadeDur)
        {
            elapsed += Time.deltaTime;
            cg.alpha = 1f - Mathf.Clamp01(elapsed * fadeInv);
            yield return null;
        }
        cg.alpha = 0f;
        Debug.Log($"[SOH] NEW ■ DONE Screen={Screen.width}x{Screen.height}");

        if (_overlayCanvasGO is not null) { Destroy(_overlayCanvasGO); _overlayCanvasGO = null; }
        _currentCoroutine = null;
    }


    // ─── SwitchCoroutine：保持原样不动 ──────────────────────────────────────────
    private IEnumerator SwitchCoroutine(ScreenOrientation target, Action onComplete, Action onPreRotate)
    {
        bool isPortrait = target == ScreenOrientation.Portrait || target == ScreenOrientation.PortraitUpsideDown;
#if UNITY_IOS
        if (DeviceInfoManager.Inst.CheckVersion_1_0_20())
        {
            //  通知原生添加黑色覆盖层，隐藏 iOS 旋转动画 ─────────────────────────
         //   MobileInterface.Instance.SendMessage(MobileInterfaceDefine.SOH_DisableUIKitAnimation, "");
        }
#endif
        // Capture source resolution before any orientation change
        int srcW = Screen.width;
        int srcH = Screen.height;

        string sourceImgPath = isPortrait
            ? "Assets/Loadable/UI/UIPanel/AICompanionChatPanel/Landscape.png"
            : "Assets/Loadable/UI/UIPanel/AICompanionChatPanel/Portrait.png";
        string targetImgPath = isPortrait
            ? "Assets/Loadable/UI/UIPanel/AICompanionChatPanel/Portrait.png"
            : "Assets/Loadable/UI/UIPanel/AICompanionChatPanel/Landscape.png";

        // Step 1: Create overlay
        _overlayCanvasGO = new GameObject("ScreenOrientationOverlay");
        var overlayCanvas = _overlayCanvasGO.AddComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = 9999;

        var coverGO = new GameObject("CoverImage");
        coverGO.transform.SetParent(_overlayCanvasGO.transform, false);
        var coverRT = coverGO.AddComponent<RectTransform>();
        coverRT.anchorMin = Vector2.zero;
        coverRT.anchorMax = Vector2.one;
        coverRT.offsetMin = Vector2.zero;
        coverRT.offsetMax = Vector2.zero;
        var cg = coverGO.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        var img = coverGO.AddComponent<Image>();
        img.sprite = Loader.Load<Sprite>(sourceImgPath, gameObject);

        // Fade-in: alpha 0 → 1
        const float fadeDur = 0.2f;
        float fadeInv = 1f / fadeDur;
        float elapsed = 0f;
        while (elapsed < fadeDur)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Clamp01(elapsed * fadeInv);
            yield return null;
        }
        cg.alpha = 1f;

        // Step 2: Ensure the opaque overlay frame is fully submitted to Metal/GPU,
        // then hide scene content and call Screen.orientation.
        // Two WaitForEndOfFrame calls:
        //   First:  submits the opaque overlay frame
        //   Second: iOS main thread processes the submitted frame; Screen.orientation
        //           is then called while iOS still has the overlay frame as the "current"
        //           display — this is the screenshot iOS uses for its rotation animation.
        yield return new WaitForEndOfFrame();

        var mainCanvasGO = GameObject.Find("Canvas");
        if (mainCanvasGO is not null) { _hiddenCanvas = mainCanvasGO; mainCanvasGO.SetActive(false); }
        var bgCameraGO = GameObject.Find("BgCamera(Clone)");
        if (bgCameraGO is not null) { _hiddenBgCamera = bgCameraGO; bgCameraGO.SetActive(false); }

        SetAutorotate(isPortrait);
        try { onPreRotate?.Invoke(); } catch (Exception e) { Debug.LogError($"[SOH] onPreRotate: {e}"); }

        yield return new WaitForEndOfFrame(); // portrait frame submitted before orientation change

        try { Screen.orientation = target; }
        catch (Exception e) { Debug.LogWarning($"[SOH] iOS: {e.Message}"); }

        // Wait for native animation to complete
        yield return new WaitForSeconds(0.1f);
        // Step 4: Update CanvasScaler, swap to target-orientation image
        if (mainCanvasGO)
        {
            var cs = mainCanvasGO.GetComponent<CanvasScaler>();
            if (cs) cs.referenceResolution = isPortrait ? new Vector2(1125, 2436) : new Vector2(2436, 1125);
            if (cs) cs.matchWidthOrHeight = isPortrait ? 1 : 0;
        }
        img.sprite = Loader.Load<Sprite>(targetImgPath, gameObject);

        // Step 5: Restore and fire onComplete, then fade-out overlay
        if (_hiddenCanvas) { _hiddenCanvas.SetActive(true); _hiddenCanvas = null; }
        if (_hiddenBgCamera) { _hiddenBgCamera.SetActive(true); _hiddenBgCamera = null; }
        try { onComplete?.Invoke(); } catch (Exception e) { Debug.LogError($"[SOH] onComplete: {e}"); }
        yield return null; // let new layout settle

        // Fade-out: alpha 1 → 0
        elapsed = 0f;
        while (elapsed < fadeDur)
        {
            elapsed += Time.deltaTime;
            cg.alpha = 1f - Mathf.Clamp01(elapsed * fadeInv);
            yield return null;
        }
        cg.alpha = 0f;

        if (_overlayCanvasGO is not null) { Destroy(_overlayCanvasGO); _overlayCanvasGO = null; }
        _currentCoroutine = null;
  #if UNITY_IOS
        if (DeviceInfoManager.Inst.CheckVersion_1_0_20())
        {
        // ── 通知原生移除黑色覆盖层（淡出 0.15 s），露出 targetImg ────────────
   //     MobileInterface.Instance.SendMessage(MobileInterfaceDefine.SOH_EnableUIKitAnimation, "");
        }
#endif
    }

    private static void SetAutorotate(bool portrait)
    {
        Screen.autorotateToPortrait           = portrait;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft      = !portrait;
        Screen.autorotateToLandscapeRight     = !portrait;
    }
}
