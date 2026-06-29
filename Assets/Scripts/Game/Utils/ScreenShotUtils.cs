using System;
using RTG;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Game.Utils
{

    public static class ScreenShotUtils
    {


        public static byte[] ScreenShot(Camera camera, Vector2 size, bool isHighQuality = false) {
            float oriMaskW = 2436;
            float oriMaskH = 1125;
            float curRefWidth = 0;
            float curRefHeight = 0;
            var ratioH = Screen.height / oriMaskH;
            var ratioW = Screen.width / oriMaskW;
            var ratio = Mathf.Min(ratioH, ratioW);
            curRefHeight = ratio * size.y;
            curRefWidth = ratio * size.x;
            var screenShotRec = new Rect(Screen.width / 2.0f - curRefWidth / 2, Screen.height / 2.0f - curRefHeight / 2, curRefWidth, curRefHeight);
            // screenShotRec = new Rect(Screen.width / 2 + refImg.rectTransform.rect.x, Screen.height / 2 + refImg.rectTransform.rect.y, refImg.rectTransform.rect.width, refImg.rectTransform.rect.height);

            return ScreenShot(camera, screenShotRec,isHighQuality);
        }

        public static byte[] ScreenShot(Camera camera, Vector2 size, int[] cullingLayers, bool isHighQuality = false)
        {
            foreach (var layer in cullingLayers)
            {
                camera.RemoveLayer(layer);
            }
            var lastClearFlags = camera.clearFlags;
            var lastBgColor = camera.backgroundColor;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0, 0, 0, 0);
            var comBuffers = camera.GetCommandBuffers(CameraEvent.BeforeImageEffects);
            camera.RemoveCommandBuffers(CameraEvent.BeforeImageEffects);
            var cameraData = camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            bool isPostProcessing = true;
            if (cameraData != null)
            {
                isPostProcessing = cameraData.renderPostProcessing;
                if (isPostProcessing)
                {
                    cameraData.renderPostProcessing = false;
                }
            }

            var rtg = RTGApp.Get;
            var lastRtgEnabled = false;
            if (rtg != null)
            {
                lastRtgEnabled = rtg.enabled;
                rtg.enabled = false;
            }


            var shotBytes = ScreenShot(camera, size, isHighQuality);


            foreach (var layer in cullingLayers)
            {
                camera.AddLayer(layer);
            }
            camera.clearFlags = lastClearFlags;
            camera.backgroundColor = lastBgColor;
            foreach (var buffer in comBuffers)
            {
                camera.AddCommandBuffer(CameraEvent.BeforeImageEffects, buffer);
            }
            if (cameraData != null)
            {
                cameraData.renderPostProcessing = isPostProcessing;
            }
            if (rtg != null)
            {
                rtg.enabled = lastRtgEnabled;
            }

            return shotBytes;
        }


        public static byte[] ScreenShot(Camera camera, Rect rect, bool isHighQuality = false)
        {
            // GlobalFieldController.isScreenShoting = true;
            camera.RemoveLayer(LayerMask.NameToLayer("ShotExclude"));
            // camera.RemoveLayer(LayerMask.NameToLayer("Ignore Raycast"));
            // camera.RemoveLayer(LayerMask.NameToLayer("Airwall"));
            byte[] shot = new byte[0];
            try
            {
                shot = TakeShot(camera, rect, isHighQuality);
            }
            catch(Exception e)
            {
                shot = null;
                LoggerUtils.LogError("ScreenShotUtils ScreenShot Exception:"+e.ToString());
            }
            camera.AddLayer(LayerMask.NameToLayer("ShotExclude"));
            // camera.AddLayer(LayerMask.NameToLayer("Ignore Raycast"));
            // camera.AddLayer(LayerMask.NameToLayer("Airwall"));
            // GlobalFieldController.isScreenShoting = false;
            return shot;
        }

        public static byte[] TakeShot(Camera camera, Rect rect, bool isHighQuality)
        {
            RenderTexture rt = new RenderTexture(Screen.width, Screen.height, 24);
            return TakeShot(camera, rt, rect, isHighQuality);
        }

        public static byte[] TakeShotUIRectTF(Camera camera, RectTransform uiTransform, bool isHighQuality = false)
        {
            var uiCamera = GameCameraUtils.Inst.GetUICamera();
            Vector3[] corners = new Vector3[4];
            uiTransform.GetWorldCorners(corners);
            Rect adapterRect = new Rect(uiCamera.WorldToScreenPoint(corners[0]), uiTransform.rect.size);
            RenderTexture rt = new RenderTexture(Screen.width, Screen.height, 24);
            return TakeShot(camera, rt, adapterRect, isHighQuality);
        }

        /// <summary>
        /// 截带Alpha通道的图
        /// </summary>
        public static byte[] TakeShotWithAlphaUIRectTF(Camera camera, RectTransform uiTransform)
        {
            // 关闭后处理效果
            var cameraData = camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            bool originPostProcessing = true;
            if (cameraData != null)
            {
                originPostProcessing = cameraData.renderPostProcessing;
                cameraData.renderPostProcessing = false;
            }

            var screenBytes = TakeShotUIRectTF(camera, uiTransform, true);

            if (cameraData != null)
            {
                cameraData.renderPostProcessing = originPostProcessing;
            }

            return screenBytes;
        }

        public static byte[] TakeShot(Camera camera, Rect renderTexRect, Rect rect, bool isHighQuality = false)
        {
            RenderTexture rt = new RenderTexture((int)renderTexRect.width, (int)renderTexRect.height, 24);
            return TakeShot(camera, rt, rect, isHighQuality);
        }
        
        public static byte[] TakeShot(Camera camera, Vector2 size, bool isHighQuality = false)
        {
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0, 0, 0, 0);
            camera.RemoveCommandBuffers(CameraEvent.BeforeImageEffects);
            var cameraData = camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            if (cameraData != null)
            {
                cameraData.renderPostProcessing = false;
            }

            RenderTexture rt = new RenderTexture((int)size.x, (int)size.y, 24);
            Rect rect = new Rect(0, 0, size.x, size.y);
            return TakeShot(camera, rt, rect, isHighQuality);
        }

        public static byte[] TakeShot(Camera camera, RenderTexture rt, Rect rect, bool isHighQuality = false)
        {
            camera.targetTexture = rt;
            camera.Render();
            RenderTexture.active = rt;
            Texture2D screenShot;
            if (isHighQuality)
            {
                screenShot = new Texture2D((int)rect.width, (int)rect.height, TextureFormat.ARGB32, false);
            } else {
                screenShot = new Texture2D((int)rect.width, (int)rect.height, TextureFormat.RGB24, false);
            }

            screenShot.ReadPixels(rect, 0, 0);
            screenShot.Apply();
            camera.targetTexture = null;
            RenderTexture.active = null;
            Object.Destroy(rt);
            var screenBytes = isHighQuality ? screenShot.EncodeToPNG() : screenShot.EncodeToJPG();
            Object.Destroy(screenShot);
            return screenBytes;
        }

        public static void RemoveLayer(this Camera cam, int target)
        {
            cam.cullingMask = cam.cullingMask & ~(1 << target);
        }

        public static void AddLayer(this Camera cam, int target)
        {
            cam.cullingMask = cam.cullingMask | (1 << target);
        }

        public static byte[] TakeShot(Camera camera, Rect rect)
        {
            RenderTexture.active = camera.targetTexture;
            Texture2D screenShot = new Texture2D((int)rect.width, (int)rect.height, TextureFormat.ARGB32, false);
            screenShot.ReadPixels(rect, 0, 0);
            Color[] pixels = screenShot.GetPixels();
            for (int i = 0; i < pixels.Length; i++) {
                pixels[i] = pixels[i];
            }
            screenShot.SetPixels(pixels);
            screenShot.Apply();
            RenderTexture.active = null;
            var screenBytes = screenShot.EncodeToPNG();
            Object.Destroy(screenShot);
            return screenBytes;
        }

        public static byte[] TakeShotGamma(Camera camera, Rect rect)
        {
            RenderTexture.active = camera.targetTexture;
            Texture2D screenShot = new Texture2D((int)rect.width, (int)rect.height, TextureFormat.ARGB32, false);
            screenShot.ReadPixels(rect, 0, 0);
            Color[] pixels = screenShot.GetPixels();
            for (int i = 0; i < pixels.Length; i++) {
                pixels[i] = pixels[i].gamma;
            }
            screenShot.SetPixels(pixels);
            screenShot.Apply();
            RenderTexture.active = null;
            var screenBytes = screenShot.EncodeToPNG();
            Object.Destroy(screenShot);
            return screenBytes;
        }

        public static byte[] TakeRenderTexture(RenderTexture rt)
        {
            var tex2D = new Texture2D(rt.width, rt.height, TextureFormat.ARGB32, false);
            RenderTexture.active = rt;
            tex2D.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            var pixels = tex2D.GetPixels();
            for (var i = 0; i < pixels.Length; i++) {
                pixels[i] = pixels[i].gamma;
            }
            tex2D.SetPixels(pixels);
            tex2D.Apply();
            var bytes = tex2D.EncodeToPNG();
            Object.Destroy(tex2D);
            return bytes;
        }

        public static byte[] TakeRenderTextureJpg(RenderTexture rt, int quality = 85)
        {
            var tex2D = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            RenderTexture.active = rt;
            tex2D.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            var pixels = tex2D.GetPixels();
            for (var i = 0; i < pixels.Length; i++) { pixels[i] = pixels[i].gamma; }
            tex2D.SetPixels(pixels);
            tex2D.Apply();
            var bytes = tex2D.EncodeToJPG(quality);
            Object.Destroy(tex2D);
            return bytes;
        }

    }
}
