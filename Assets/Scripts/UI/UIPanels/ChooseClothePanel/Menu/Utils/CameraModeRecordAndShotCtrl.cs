using DG.Tweening;
using Basic;
using Basic.Utils;
using Game.Event;
using GameData;
using GameData.Manager;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using Game.Audio;

public enum CameraModeOptType
{
    Recording,
    Photo
}

public class CameraModeRecordAndShotCtrl : MonoBehaviour
{
    private const string CameraAlbumDirName = "CameraAlbum";
    private const string CameraVideoDirName = "Video";
    private static readonly bool RECORD_WITH_AUDIO = true;
    private const float MAX_RECORD_SECONDS = 120f;
    private const float RECORD_FRAME_RATE = 24f;
    private const int RECORD_VIDEO_BIT_RATE = 3_000_000;
    private const float LOW_MOBILE_RECORD_RESOLUTION_SCALE = 0.75f;
    private const int LOW_MOBILE_RECORD_VIDEO_BIT_RATE = 2_000_000;
    private const int RECORD_AUDIO_BIT_RATE = 32_000;
    private const int RECORD_KEYFRAME_INTERVAL = 2;

    private RectTransform recording_rect;
    private RectTransform photo_rect;

    private Button recording_btn;
    private Button photo_btn;

    private Text recordingTime_text;

    private GameObject recording_obj;
    private GameObject record_obj;

    // 改用世界坐标 (World Position) 来记录位置，避免锚点不同导致的位置错乱
    private Vector3 activeWorldPos;
    private Vector3 inactiveWorldPos;
    private Vector3 inactiveScale = new Vector3(0.85f, 0.85f, 0.85f);
    private bool is_recording = false;
    private bool is_stopping = false;
    private float recordingStartRealtime;
    private string latestThumbnailPath;

    private object recordingClock;
    private object mediaRecorder;
    private object cameraInput;
    private readonly List<object> audioInputs = new List<object>();
    private static Type natRealtimeClockType;
    private static Type natMp4RecorderType;
    private static Type natCameraInputType;
    private static Type natAudioInputType;
    private static bool natTypesInitialized;

    private CameraModeOptType cur_active = CameraModeOptType.Photo;

    public void Init()
    {
        recording_btn = GameObjectEx.FindComponentByName<Button>(transform, "RecordingBtn");
        photo_btn = GameObjectEx.FindComponentByName<Button>(transform, "PhotoBtn");

        if (recording_btn != null)
        {
            recording_rect = recording_btn.GetComponent<RectTransform>();
            recording_btn.onClick.AddListener(OnRecordingClick);
            recording_obj = GameObjectEx.FindChildByName(transform, "Recording").gameObject;
            record_obj = GameObjectEx.FindChildByName(transform, "Record").gameObject;
            recording_obj.SetActive(false);
            recording_rect.localScale = inactiveScale;
        }

        recordingTime_text = GameObjectEx.FindComponentByName<Text>(transform, "RecordingTime");
        if (recordingTime_text == null)
        {
            recordingTime_text = GameObjectEx.FindComponentByName<Text>(transform, "RecordingTimeText");
        }
        UpdateRecordingTimeText(0f);
        if (recordingTime_text != null)
        {
            recordingTime_text.gameObject.SetActive(false);
        }

        if (photo_btn != null)
        {
            photo_rect = photo_btn.GetComponent<RectTransform>();
            photo_btn.onClick.AddListener(OnPhotoClick);
            photo_rect.localScale = Vector3.one;
        }

        // 记录初始的世界坐标
        if (recording_rect != null && photo_rect != null)
        {
            activeWorldPos = photo_rect.position;
            inactiveWorldPos = recording_rect.position;
        }
    }

    private void Update()
    {
        if (!is_recording)
        {
            return;
        }

        var elapsed = Time.realtimeSinceStartup - recordingStartRealtime;
        UpdateRecordingTimeText(elapsed);
        if (!is_stopping && elapsed >= MAX_RECORD_SECONDS)
        {
            is_recording = false;
            SwitchRecordingBtnToIdleVisual();
            StartCoroutine(StopRecordingFlow(reachMaxDuration: true));
        }
    }

    private void OnRecordingClick()
    {
        if (!is_recording && !IsCameraModeFeatureVersionSupported())
        {
            return;
        }

        if (cur_active == CameraModeOptType.Recording)
        {
            HandleRecordingAction();
        }
        else
        {
            SwitchToMode(CameraModeOptType.Recording);
        }
    }

    private void OnPhotoClick()
    {
        if(is_recording){
            TipPanel.ShowToast("请先停止录像");
            return;
        }

        if (cur_active == CameraModeOptType.Photo)
        {
            HandlePhotoAction();
        }
        else
        {
            SwitchToMode(CameraModeOptType.Photo);
        }
    }

    private void SwitchToMode(CameraModeOptType targetMode)
    {
        if (cur_active == targetMode) return;
        if (recording_rect == null || photo_rect == null) return;

        recording_rect.DOKill();
        photo_rect.DOKill();

        float duration = 0.3f;
        Ease easeType = Ease.OutQuad;

        if (targetMode == CameraModeOptType.Recording)
        {
            // 切换到 Recording 激活
            // Recording 移动到 activeWorldPos
            recording_rect.DOMove(activeWorldPos, duration).SetEase(easeType);
            // Photo 移动到 inactiveWorldPos
            photo_rect.DOMove(inactiveWorldPos, duration).SetEase(easeType);
            recording_rect.DOScale(Vector3.one, duration).SetEase(easeType);
            photo_rect.DOScale(inactiveScale, duration).SetEase(easeType);
        }
        else
        {
            // 切换到 Photo 激活
            // Photo 移动到 activeWorldPos
            photo_rect.DOMove(activeWorldPos, duration).SetEase(easeType);
            // Recording 移动到 inactiveWorldPos
            recording_rect.DOMove(inactiveWorldPos, duration).SetEase(easeType);
            recording_rect.DOScale(inactiveScale, duration).SetEase(easeType);
            photo_rect.DOScale(Vector3.one, duration).SetEase(easeType);
        }

        cur_active = targetMode;
        Debug.Log($"Camera Mode Switched to: {cur_active}");
    }

    private bool IsCameraModeFeatureVersionSupported()
    {
        if (DeviceInfoManager.Inst != null
            && DeviceInfoManager.Inst.DeviceBaseData != null
            && DeviceInfoManager.Inst.CheckVersion_1_0_18())
        {
            return true;
        }
        UIManager.Inst.OpenPanel(PanelId.UpdateTipsPanel, GameData.Base.ForceUpdate.NeedUpdateFeature);
        //TipPanel.ShowToast("该功能仅在1.0.18及以上版本可用");
        return false;
    }

    private void HandleRecordingAction()
    {
        if (is_stopping)
        {
            return;
        }

        Debug.Log("Execute Recording Action");
        if(is_recording){
            is_recording = false;
            StartCoroutine(StopRecordingFlow(reachMaxDuration: false));
            SwitchRecordingBtnToIdleVisual();
        }else{
            if (!TryStartRecording())
            {
                return;
            }

            is_recording = true;
            recordingStartRealtime = Time.realtimeSinceStartup;
            if (recordingTime_text != null)
            {
                recordingTime_text.gameObject.SetActive(true);
            }
            recording_rect.DOScale(inactiveScale, 0.15f).onComplete += (
                () => {
                    recording_obj.SetActive(true);
                    record_obj.SetActive(false);
                    recording_rect.DOScale(Vector3.one, 0.15f).SetEase(Ease.OutQuad);
                }
            );
        }
    }

    private void SwitchRecordingBtnToIdleVisual()
    {
        if (recording_rect == null || recording_obj == null || record_obj == null)
        {
            return;
        }

        // 统一收口到“未录制”视觉状态，供手动停止与自动超时停止共用。
        recording_rect.DOKill();
        recording_rect.DOScale(inactiveScale, 0.15f).onComplete += (
            () => {
                recording_obj.SetActive(false);
                record_obj.SetActive(true);
                recording_rect.DOScale(Vector3.one, 0.15f).SetEase(Ease.OutQuad);
            }
        );
    }

    private void HandlePhotoAction()
    {
        UIManager.Inst.OpenPanel<ShotBlackPanel>(PanelId.ShotBlackPanel);
        EventCenterDataManager.Inst.ReportTask(PostEventId.TakePhotoCheckIn);
        AkSoundManager.Inst.PlayUIEffectSound("Play_UI_Takephoto");
    }

    private bool TryStartRecording()
    {
        if (!TryEnsureNatSuiteTypesAvailable(out string reflectionError))
        {
            Debug.LogWarning($"NatSuite reflection unavailable: {reflectionError}");
            //TipPanel.ShowToast("当前版本过低，请更新到最新版本");
            UIManager.Inst.OpenPanel(PanelId.UpdateTipsPanel, GameData.Base.ForceUpdate.NeedUpdateFeature);
            return false;
        }

        if (!IsNatCorderNativeAvailable())
        {
            UIManager.Inst.OpenPanel(PanelId.UpdateTipsPanel, GameData.Base.ForceUpdate.NeedUpdateFeature);
            //TipPanel.ShowToast("当前版本过低，请更新到最新版本");
            return false;
        }

        bool tryWithAudio = RECORD_WITH_AUDIO && IsAudioRecordingStableOnCurrentPlatform();
        if (TryStartRecordingInternal(tryWithAudio))
        {
            return true;
        }

        if (tryWithAudio)
        {
            if (RequireAudioRecordingOnCurrentPlatform())
            {
                TipPanel.ShowToast("当前设备音频录制初始化失败，请检查后重试");
                DisposeRecorderInputs();
                return false;
            }

            Debug.LogWarning("Audio recording init failed, fallback to silent recording.");
            DisposeRecorderInputs();
            if (TryStartRecordingInternal(false))
            {
                TipPanel.ShowToast("当前设备暂不支持声音录制，已切换为无声录像");
                return true;
            }
        }

        TipPanel.ShowToast("录像启动失败，请重试");
        DisposeRecorderInputs();
        return false;
    }

    private bool IsAudioRecordingStableOnCurrentPlatform()
    {
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        // macOS 下 NatCorder 音频轨道稳定性较差，优先保证 MP4 可播放。
        return false;
#else
        return true;
#endif
    }

    private bool TryStartRecordingInternal(bool withAudio)
    {
        try
        {
            var width = GetRecordingWidth();
            var height = GetRecordingHeight();
            if (width % 2 != 0) width -= 1;
            if (height % 2 != 0) height -= 1;

            if (width <= 0 || height <= 0)
            {
                Debug.LogError("Invalid recording resolution.");
                return false;
            }

            var recordingCameras = GetRecordingCameras();
            if (recordingCameras == null || recordingCameras.Length == 0)
            {
                TipPanel.ShowToast("录像启动失败，未找到相机");
                DisposeRecorderInputs();
                return false;
            }

            int sampleRate = 0;
            int channelCount = 0;
            AudioListener recordingAudioListener = null;
            if (withAudio)
            {
                if (!TryResolveRecordingAudioListener(out recordingAudioListener))
                {
                    Debug.LogWarning("Start recording with audio failed: GlobalCameraManager audio listener is null.");
                    return false;
                }

                sampleRate = Mathf.Max(1, AudioSettings.outputSampleRate);
                channelCount = GetRecordingChannelCount();
            }

            recordingClock = Activator.CreateInstance(natRealtimeClockType);

            mediaRecorder = Activator.CreateInstance(
                natMp4RecorderType,
                width,
                height,
                RECORD_FRAME_RATE,
                sampleRate,
                channelCount,
                GetRecordingVideoBitRate(),
                RECORD_KEYFRAME_INTERVAL,
                RECORD_AUDIO_BIT_RATE
            );

            cameraInput = Activator.CreateInstance(natCameraInputType, mediaRecorder, recordingClock, recordingCameras);

            if (withAudio)
            {
                try
                {
                    audioInputs.Add(Activator.CreateInstance(natAudioInputType, mediaRecorder, recordingClock, recordingAudioListener));
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Create audio input failed on listener {recordingAudioListener?.name}: {e.Message}");
                    return false;
                }
            }

            UpdateRecordingTimeText(0f);
            return true;
        }
        catch (DllNotFoundException dnfe)
        {
            Debug.LogError($"Start recording failed: {dnfe}");
            return false;
        }
        catch (Exception e)
        {
            Debug.LogError($"Start recording failed: {e}");
            return false;
        }
    }

    private static int GetRecordingChannelCount()
    {
        switch (AudioSettings.speakerMode)
        {
            case AudioSpeakerMode.Mono:
                return 1;
            default:
                return 2;
        }
    }

    private static int GetRecordingWidth()
    {
        return GetScaledRecordingSize(Screen.currentResolution.width);
    }

    private static int GetRecordingHeight()
    {
        return GetScaledRecordingSize(Screen.currentResolution.height);
    }

    private static int GetScaledRecordingSize(int sourceSize)
    {
        if (!IsLowMobileRecordingMode())
        {
            return sourceSize;
        }

        return Mathf.Max(1, Mathf.RoundToInt(sourceSize * LOW_MOBILE_RECORD_RESOLUTION_SCALE));
    }

    private static int GetRecordingVideoBitRate()
    {
        return IsLowMobileRecordingMode() ? LOW_MOBILE_RECORD_VIDEO_BIT_RATE : RECORD_VIDEO_BIT_RATE;
    }

    private static bool IsLowMobileRecordingMode()
    {
        return GameDataManager.Inst != null && GameDataManager.Inst.IsLowMobile();
    }

    private bool TryResolveRecordingAudioListener(out AudioListener listener)
    {
        listener = null;
        var mgr = GlobalCameraManager.Inst;
        if (mgr == null)
        {
            return false;
        }

        listener = mgr.GetCameraAudioListener();
        return listener != null;
    }

    private static bool RequireAudioRecordingOnCurrentPlatform()
    {
#if UNITY_IOS || UNITY_ANDROID
        return true;
#else
        return false;
#endif
    }

    private Camera[] GetRecordingCameras()
    {
        var cameras = new List<Camera>();
        var globalCameraMgr = GlobalCameraManager.Inst;

        var mainCamera = globalCameraMgr != null ? globalCameraMgr.GlobalMainCamera : Camera.main;
        if (mainCamera != null)
        {
            cameras.Add(mainCamera);
        }

        // 把 UI 相机录进去前先检查是否已在 MainCamera 的 URP Camera Stack 中，
        // 避免 UI 被 Main + UI 两次采集，导致半透明控件看起来“叠了一层”。
        var uiCamera = globalCameraMgr != null ? globalCameraMgr.UICamera : null;
        if (ShouldRecordUICameraSeparately(mainCamera, uiCamera))
        {
            cameras.Add(uiCamera);
        }

        return cameras.ToArray();
    }

    private static bool ShouldRecordUICameraSeparately(Camera mainCamera, Camera uiCamera)
    {
        if (uiCamera == null) return false;
        if (mainCamera == null) return true;
        if (uiCamera == mainCamera) return false;

        var mainData = mainCamera.GetComponent<UniversalAdditionalCameraData>();
        if (mainData == null) return true;

        var stack = mainData.cameraStack;
        if (stack == null) return true;

        return !stack.Contains(uiCamera);
    }

    private bool IsNatCorderNativeAvailable()
    {
#if UNITY_EDITOR_OSX
        string bundleBinary = Path.Combine(
            Application.dataPath,
            "OtherLibrary/NatCorder/Plugins/macOS/NatCorder.bundle/Contents/MacOS/NatCorder"
        );
        return File.Exists(bundleBinary);
#else
        return true;
#endif
    }

    private IEnumerator StopRecordingFlow(bool reachMaxDuration)
    {
        is_stopping = true;
        if (recordingTime_text != null)
        {
            recordingTime_text.gameObject.SetActive(false);
        }

        yield return new WaitForEndOfFrame();
        SaveRecordingThumbnail();

        var recorder = mediaRecorder;
        DisposeRecorderInputs();

        if (recorder == null)
        {
            is_stopping = false;
            yield break;
        }

        var finishTask = GetFinishWritingTask(recorder);
        if (finishTask == null)
        {
            TipPanel.ShowToast("录像保存失败，请重试");
            is_stopping = false;
            yield break;
        }

        while (!finishTask.IsCompleted)
        {
            yield return null;
        }

        if (finishTask.IsFaulted || finishTask.IsCanceled)
        {
            Debug.LogError($"Stop recording failed: {finishTask.Exception}");
            TipPanel.ShowToast("录像保存失败，请重试");
            is_stopping = false;
            yield break;
        }

        var videoPath = finishTask.Result;
        Debug.Log($"CameraMode record saved path: {videoPath}");
        SaveVideoToAlbum(videoPath);
        TipPanel.ShowToast(reachMaxDuration ? "到达最大录像时间，已为您保存" : "录像已保存");
        is_stopping = false;
    }

    private Task<string> GetFinishWritingTask(object recorder)
    {
        try
        {
            if (recorder == null)
            {
                return null;
            }

            var finishWritingMethod = recorder.GetType().GetMethod("FinishWriting", BindingFlags.Public | BindingFlags.Instance);
            if (finishWritingMethod == null)
            {
                Debug.LogError("Get finish writing task failed: FinishWriting method not found.");
                return null;
            }

            var result = finishWritingMethod.Invoke(recorder, null);
            return result as Task<string>;
        }
        catch (Exception e)
        {
            Debug.LogError($"Get finish writing task failed: {e}");
            return null;
        }
    }

    private void SaveVideoToAlbum(string videoPath)
    {
        if (string.IsNullOrEmpty(videoPath) || !File.Exists(videoPath))
        {
            TipPanel.ShowToast("录像保存失败，请重试");
            return;
        }

        var uid = GetCurrentUid();
        // 统一将视频归档到 CameraAlbum/{uid}/Video，保持与图片同一业务根目录。
        var targetPath = MoveVideoToUserFolder(videoPath, uid);
        if (string.IsNullOrEmpty(targetPath) || !File.Exists(targetPath))
        {
            Debug.LogError($"Video save path invalid. source={videoPath}, target={targetPath}");
            TipPanel.ShowToast("录像保存失败，请重试");
            return;
        }

        RenameThumbToMatchVideo(targetPath);

        // 通知 CameraImgDataUtils 失效视频缓存：视频文件已在目录内，SaveVideoToPrivate 检测到
        // source == dest 会跳过复制，只将 _videoPacksCache 置 null，确保相册下次打开时重新扫描目录。
        CameraImgDataUtils.Inst.SaveVideoToPrivate(targetPath, uid);
        // 同步导出到玩家系统相册
        CameraImgDataUtils.Inst.ExportPrivateVideoToAlbum(targetPath, out var albumExportError);
        if (!string.IsNullOrEmpty(albumExportError))
        {
            Debug.LogWarning($"[CameraModeRecordAndShotCtrl] Export video to system album failed: {albumExportError}");
        }
        Debug.Log($"[CameraModeRecordAndShotCtrl] Video saved to private sandbox: {targetPath}");
    }

    private void SaveRecordingThumbnail()
    {
        try
        {
            latestThumbnailPath = null;
            Texture2D thumb = null;
            var activeRt = RenderTexture.active;
            int captureWidth = Screen.width;
            int captureHeight = Screen.height;
            if (activeRt != null)
            {
                captureWidth = Mathf.Min(captureWidth, activeRt.width);
                captureHeight = Mathf.Min(captureHeight, activeRt.height);
            }

            if (captureWidth <= 0 || captureHeight <= 0)
            {
                Debug.LogWarning("Skip thumbnail capture: invalid render size.");
                return;
            }

            try
            {
                thumb = new Texture2D(captureWidth, captureHeight, TextureFormat.RGB24, false);
                thumb.ReadPixels(new Rect(0, 0, captureWidth, captureHeight), 0, 0);
                thumb.Apply();
            }
            catch (Exception e)
            {
                if (thumb != null)
                {
                    UnityEngine.Object.Destroy(thumb);
                }
                Debug.LogError($"Capture thumbnail pixels failed: {e.Message}");
                return;
            }

            var bytes = thumb.EncodeToPNG();
            UnityEngine.Object.Destroy(thumb);

            var uid = GetCurrentUid();
            var targetDir = GetUserVideoDir(uid);
            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            var thumbName = $"{uid}_{GameUtils.GetTimeStamp()}_video_thumb.png";
            latestThumbnailPath = Path.Combine(targetDir, thumbName);
            File.WriteAllBytes(latestThumbnailPath, bytes);
            Debug.Log($"Save video thumbnail success: {latestThumbnailPath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Save video thumbnail failed: {e}");
        }
    }

    private static string GetCurrentUid()
    {
        if (AccountDataManager.Inst != null && !string.IsNullOrEmpty(AccountDataManager.Inst.Uid))
        {
            return AccountDataManager.Inst.Uid;
        }

        return "shotTemplate";
    }

    private static string GetUserVideoDir(string uid)
    {
        uid = string.IsNullOrEmpty(uid) ? "shotTemplate" : uid;
        return Path.Combine(Application.persistentDataPath, "U3D", CameraAlbumDirName, uid, CameraVideoDirName);
    }

    private string MoveVideoToUserFolder(string sourcePath, string uid)
    {
        try
        {
            var targetDir = GetUserVideoDir(uid);
            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            var normalizedSourcePath = Path.GetFullPath(sourcePath);
            if (IsPathUnderUserVideoDir(normalizedSourcePath, uid))
            {
                return normalizedSourcePath;
            }
            var ext = Path.GetExtension(sourcePath);
            if (string.IsNullOrEmpty(ext)) ext = ".mp4";
            var targetFileName = $"{uid}_{GameUtils.GetTimeStamp()}_{Guid.NewGuid():N}{ext}";
            var targetPath = Path.Combine(targetDir, targetFileName);
            targetPath = Path.GetFullPath(targetPath);

            // 优先移动，避免编辑器下原始文件残留在项目根目录。
            try
            {
                if (File.Exists(targetPath))
                {
                    File.Delete(targetPath);
                }
                File.Move(normalizedSourcePath, targetPath);
            }
            catch
            {
                // 跨磁盘/权限等情况下回退为 copy + delete。
                File.Copy(normalizedSourcePath, targetPath, true);
                if (!string.Equals(normalizedSourcePath, targetPath, StringComparison.OrdinalIgnoreCase)
                    && File.Exists(normalizedSourcePath))
                {
                    File.Delete(normalizedSourcePath);
                }
            }
            return targetPath;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Move video to user folder failed: {e.Message}");
            return null;
        }
    }

    private void RenameThumbToMatchVideo(string videoLocalPath)
    {
        if (string.IsNullOrEmpty(latestThumbnailPath) || !File.Exists(latestThumbnailPath))
            return;
        if (string.IsNullOrEmpty(videoLocalPath))
            return;

        try
        {
            // 缩略图始终保留在 CameraAlbum 的 Video 目录，便于业务侧统一读取；
            // 视频本体在移动端由系统相册托管（如 DCIM/BUD），两者不要求同目录。
            var dir = Path.GetDirectoryName(latestThumbnailPath);
            if (string.IsNullOrEmpty(dir))
            {
                dir = Path.GetDirectoryName(videoLocalPath);
            }
            if (string.IsNullOrEmpty(dir)) return;
            var videoNameNoExt = Path.GetFileNameWithoutExtension(videoLocalPath);
            var newThumbPath = Path.Combine(dir, videoNameNoExt + "_thumb.png");

            if (string.Equals(
                    Path.GetFullPath(latestThumbnailPath),
                    Path.GetFullPath(newThumbPath),
                    StringComparison.OrdinalIgnoreCase))
                return;

            if (File.Exists(newThumbPath)) File.Delete(newThumbPath);
            File.Move(latestThumbnailPath, newThumbPath);
            latestThumbnailPath = newThumbPath;
            Debug.Log($"Renamed video thumbnail to: {newThumbPath}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Rename thumbnail failed: {e.Message}");
        }
    }

    private static bool IsPathUnderUserVideoDir(string filePath, string uid)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            return false;
        }

        try
        {
            var normalizedFilePath = Path.GetFullPath(filePath);
            var expectedRoot = Path.GetFullPath(GetUserVideoDir(uid) + Path.DirectorySeparatorChar);
            return normalizedFilePath.StartsWith(expectedRoot, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static void FlipTextureVerticallyInPlace(Texture2D texture)
    {
        if (texture == null)
        {
            return;
        }

        int width = texture.width;
        int height = texture.height;
        if (width <= 1 || height <= 1)
        {
            return;
        }

        var pixels = texture.GetPixels32();
        int halfHeight = height / 2;
        for (int y = 0; y < halfHeight; y++)
        {
            int topRow = y * width;
            int bottomRow = (height - 1 - y) * width;
            for (int x = 0; x < width; x++)
            {
                int topIndex = topRow + x;
                int bottomIndex = bottomRow + x;
                var temp = pixels[topIndex];
                pixels[topIndex] = pixels[bottomIndex];
                pixels[bottomIndex] = temp;
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
    }

    private void DisposeRecorderInputs()
    {
        try
        {
            DisposeNatObject(cameraInput);
            for (int i = 0; i < audioInputs.Count; i++)
            {
                DisposeNatObject(audioInputs[i]);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Dispose recorder inputs failed: {e}");
        }
        finally
        {
            cameraInput = null;
            audioInputs.Clear();
            recordingClock = null;
            mediaRecorder = null;
        }
    }

    private static void DisposeNatObject(object target)
    {
        if (target == null)
        {
            return;
        }

        try
        {
            if (target is IDisposable disposable)
            {
                disposable.Dispose();
                return;
            }

            var disposeMethod = target.GetType().GetMethod("Dispose", BindingFlags.Public | BindingFlags.Instance);
            disposeMethod?.Invoke(target, null);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Dispose nat object failed: {e.Message}");
        }
    }

    private static bool TryEnsureNatSuiteTypesAvailable(out string error)
    {
        if (!natTypesInitialized
            || natRealtimeClockType == null
            || natMp4RecorderType == null
            || natCameraInputType == null
            || natAudioInputType == null)
        {
            natRealtimeClockType = FindTypeInLoadedAssemblies("NatSuite.Recorders.Clocks.RealtimeClock");
            natMp4RecorderType = FindTypeInLoadedAssemblies("NatSuite.Recorders.MP4Recorder");
            natCameraInputType = FindTypeInLoadedAssemblies("NatSuite.Recorders.Inputs.CameraInput");
            natAudioInputType = FindTypeInLoadedAssemblies("NatSuite.Recorders.Inputs.AudioInput");

            var missingTypes = new List<string>();
            if (natRealtimeClockType == null) missingTypes.Add("NatSuite.Recorders.Clocks.RealtimeClock");
            if (natMp4RecorderType == null) missingTypes.Add("NatSuite.Recorders.MP4Recorder");
            if (natCameraInputType == null) missingTypes.Add("NatSuite.Recorders.Inputs.CameraInput");
            if (natAudioInputType == null) missingTypes.Add("NatSuite.Recorders.Inputs.AudioInput");

            error = missingTypes.Count > 0
                ? $"Missing NatSuite types: {string.Join(", ", missingTypes)}"
                : null;
            natTypesInitialized = true;
            return string.IsNullOrEmpty(error);
        }

        error = null;
        return true;
    }

    private static Type FindTypeInLoadedAssemblies(string fullTypeName)
    {
        if (string.IsNullOrEmpty(fullTypeName))
        {
            return null;
        }

        var type = Type.GetType(fullTypeName, false);
        if (type != null)
        {
            return type;
        }

        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        for (int i = 0; i < assemblies.Length; i++)
        {
            type = assemblies[i].GetType(fullTypeName, false);
            if (type != null)
            {
                return type;
            }
        }

        return null;
    }

    private void UpdateRecordingTimeText(float elapsedSeconds)
    {
        if (recordingTime_text == null)
        {
            return;
        }

        int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(elapsedSeconds));
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        recordingTime_text.SetLocalText($"{minutes:00}:{seconds:00}");
    }

    private void OnDisable()
    {
        if (is_recording && !is_stopping)
        {
            is_recording = false;
            SwitchRecordingBtnToIdleVisual();
            StartCoroutine(StopRecordingFlow(reachMaxDuration: false));
        }
    }
}
