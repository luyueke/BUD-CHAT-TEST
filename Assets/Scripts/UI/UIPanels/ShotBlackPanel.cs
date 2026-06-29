using Basic;
using DG.Tweening;
using Game.Avatar;
using Game.Base;
using Game.Event;
using Game.Props.PropsManagers;
using Game.Utils;
using GameData;
using GameData.Manager;
using System.Collections;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Author:
/// Desc:
/// Date:24-07-11 16:24:48
/// </summary>
public class ShotBlackPanel : BasePanel<ShotBlackPanel>
{
    public Image BlackImage;
    public override void OnCreate()
    {
        // 拍照后仅保存加密字节流到本地索引，不再直接保存到系统相册。
    }

    public override void OnShow(params object[] args)
    {
        ScreenShotBtnClick();
    }

    public override void OnHidden()
    {
    }

    protected void ScreenShotBtnClick()
    {
        StartCoroutine(ShotAnimation());
        var selfAvatar = AvatarController.Inst.SelfController;
        if (selfAvatar != null)
        {
            Game.Audio.AkSoundManager.Inst.PostEvent("Play_UI_Screenshot", selfAvatar.gameObject);
        }

        var mainCamera = GlobalCameraManager.Inst.GlobalMainCamera;
        mainCamera.RemoveLayer(LayerMask.NameToLayer("ShotExclude"));
        var bytes = ScreenShotUtils.ScreenShot(mainCamera, new Rect(0, 0, Screen.width, Screen.height), true);
        if (bytes.Length == 0)
        {
            TipPanel.ShowToast("保存失败，请再试一次!");
            mainCamera.RemoveLayer(LayerMask.NameToLayer("ShotExclude"));
            return;
        }

        // 仅把 CameraMode 的 Frame 边框合成到截图，不引入其它 UI 元素。
        bytes = TryComposeSelectedFrame(bytes);

        string userId = AccountDataManager.Inst.Uid;
        userId = string.IsNullOrEmpty(userId) ? "shotTemplate" : userId;
        var locationName = default(string);
        var mapId = default(string);
        var landMarkManager = GlobalNodeManager.Inst.Get<CameraLandMarkManager>();
        var isLandMark = false;
        if (landMarkManager != null && landMarkManager.TryGetCurrentLocation(out var hitLocationName, out var hitMapId, out isLandMark))
        {
            locationName = hitLocationName;
            mapId = hitMapId;
            if(string.IsNullOrEmpty(locationName)){
                locationName = GameDataManager.Inst.mapGlobalData?.curUgcBaseInfo?.name;
            }
            if(string.IsNullOrEmpty(mapId)){
                mapId = GameDataManager.Inst.mapGlobalData?.curUgcBaseInfo?.id;
            }
        }

        // 通过相机图片中间件保存：加密入库，同步导出到玩家系统相册
        // 先用内存中的 bytes 导出系统相册（避免 SaveCaptured 加密写盘后再解密读盘的冗余 IO）
        var pack = CameraImgDataUtils.Inst.SaveCaptured(bytes, Screen.width, Screen.height, userId, locationName, null, mapId, isLandMark);
        CameraImgDataUtils.Inst.ExportRawImageBytesToPlayerAlbum(bytes, pack.name, out _);
        TipPanel.ShowToast("照片已保存");
        EventTracking.LoadEvent.ReportTask(176, 0);
        if (isLandMark)
        {
            EventCenterDataManager.Inst.ReportTask(PostEventId.TakePhotoCheckIn);  // 打卡包含拍照
            EventTracking.LoadEvent.ReportTask(177, 0,mapId);
        }
    }

    private byte[] TryComposeSelectedFrame(byte[] baseBytes)
    {
        var framePath = CameraModeFrameMenu.CurrentSelectedFramePath;
        if (string.IsNullOrEmpty(framePath) || baseBytes == null || baseBytes.Length == 0)
        {
            return baseBytes;
        }

        Texture2D baseTex = null;
        Texture2D frameTex = null;
        Texture2D readableFrameTex = null;
        try
        {
            baseTex = new Texture2D(2, 2, TextureFormat.RGB24, false);
            if (!baseTex.LoadImage(baseBytes, false))
            {
                return baseBytes;
            }

            Texture frameRaw = null;
            try
            {
                frameRaw = Loader.Load<Texture>(framePath, gameObject);
                if (frameRaw == null)
                {
                    var sp = Loader.Load<Sprite>(framePath, gameObject);
                    if (sp != null) frameRaw = sp.texture;
                }
            }
            catch
            {
                frameRaw = null;
            }

            if (frameRaw == null)
            {
                return baseBytes;
            }

            frameTex = frameRaw as Texture2D;
            readableFrameTex = EnsureReadableTexture(frameRaw);
            if (readableFrameTex == null)
            {
                return baseBytes;
            }

            var basePixels = baseTex.GetPixels32();
            var framePixels = readableFrameTex.GetPixels32();
            int baseW = baseTex.width;
            int baseH = baseTex.height;
            int frameW = readableFrameTex.width;
            int frameH = readableFrameTex.height;

            for (int y = 0; y < baseH; y++)
            {
                int fy = y * frameH / baseH;
                int baseRow = y * baseW;
                int frameRow = fy * frameW;
                for (int x = 0; x < baseW; x++)
                {
                    int fx = x * frameW / baseW;
                    var over = framePixels[frameRow + fx];
                    if (over.a == 0) continue;

                    int idx = baseRow + x;
                    var src = basePixels[idx];
                    int a = over.a;
                    int invA = 255 - a;
                    src.r = (byte)((over.r * a + src.r * invA) / 255);
                    src.g = (byte)((over.g * a + src.g * invA) / 255);
                    src.b = (byte)((over.b * a + src.b * invA) / 255);
                    basePixels[idx] = src;
                }
            }

            baseTex.SetPixels32(basePixels);
            baseTex.Apply(false, false);
            return baseTex.EncodeToJPG();
        }
        catch (System.Exception e)
        {
            LoggerUtils.LogError($"Compose frame into screenshot failed: {e}");
            return baseBytes;
        }
        finally
        {
            if (readableFrameTex != null && readableFrameTex != frameTex)
            {
                Destroy(readableFrameTex);
            }
            if (baseTex != null) Destroy(baseTex);
        }
    }

    private static Texture2D EnsureReadableTexture(Texture tex)
    {
        if (tex == null) return null;
        if (tex is Texture2D t2d && t2d.isReadable) return t2d;

        var rt = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.ARGB32);
        var prev = RenderTexture.active;
        try
        {
            Graphics.Blit(tex, rt);
            RenderTexture.active = rt;
            var readable = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);
            readable.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
            readable.Apply(false, false);
            return readable;
        }
        finally
        {
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
        }
    }

    private IEnumerator ShotAnimation()
    {
        RawImage tempRawImage = this.GetComponent<RawImage>();
        tempRawImage.enabled = true;
        tempRawImage.CrossFadeAlpha(0, 0, false);
        tempRawImage.color = new Color(1, 1, 1, 1);
        Image blackImage = this.BlackImage;
        blackImage.color = new Color(1, 1, 1, 0);
        blackImage.DOFade(1, 0.4f).SetEase(Ease.InExpo).onComplete = () =>
        {
            blackImage.DOFade(0, 0.4f).SetEase(Ease.OutExpo);
        };
        //临时存储截面图片信息
        yield return new WaitForEndOfFrame();
        Texture2D screenShot = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        screenShot.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        screenShot.Apply();
        tempRawImage.texture = screenShot;
        tempRawImage.CrossFadeAlpha(1, 0, false);
        
        yield return new WaitForSeconds(1.3f);
        var lastTexture = tempRawImage.texture;
        if (lastTexture)
        {
            UnityEngine.Object.Destroy(lastTexture);
        }
        tempRawImage.color = new Color(1, 1, 1, 0);
        var mainCamera = GlobalCameraManager.Inst.GlobalMainCamera;
        mainCamera.AddLayer(LayerMask.NameToLayer("ShotExclude"));
        UIManager.Inst.ClosePanel(WindowId.CommonWindow, PanelId.ShotBlackPanel);
    }
    
    protected override void OnDestroy()
    {
    }

    public override void OnWindowBeFocused()
    {
    }

    public override void OnWindowPop()
    {
    }
}