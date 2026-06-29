using System;
using System.Collections.Generic;
using System.IO;
using Com.TheFallenGames.OSA.Util.IO;
using Game.COSXML;
using Game.Utils;
using GameData;
using GameData.Base;
using Network;
using Network.Http;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class TheatreEditorImageEditPanel : BasePanel<TheatreEditorImageEditPanel>
{
    public Button btn_close;
    public Button btn_ok;
    public Button btn_scale;
    public Button btn_rot;
    public RemoteImageBehaviour img_icon;
    public Image img_icon_bg;

    [Header("拖拽缩放(等比)")]
    public float scaleSensitivity = 0.003f;
    public float minIconScale = 0.1f;
    public float maxIconScale = 10f;

    [Header("拖拽旋转")]
    public float rotateMultiplier = 1f;

    private RawImage cameraImage;
    private Camera img_camera;
    private Action<string> onUploadSuccess;
    private Action onUploadStarted;
    private string uploadFolder;
    private TheatreEditorDataCenter _coverDataCenter;
    // Ratio: BgCanvas canvas-units-width / img_icon_bg.sizeDelta.x
    // Converts preview-space localScale/anchoredPosition to BgCanvas space
    private float _cameraFactor = 1f;

    [Header("给封面显示的特殊显示")]
    public GameObject theatreInfoPanel; //仅编辑剧本封面时显示
    public Text theatreTitle; //取用剧本名称
    public Text theatreDes; // 取用剧本描述
    public Text theatreDialogueNum; // 取用剧本对话数量
    public RawImage theatreImage; // 取用剧本封面图
    public Transform avatarListRoot; // 取用剧本角色列表显示
    public TheatreAvatarItem itemPrefab; // 取用剧本角色列表显示预制体





    public override void OnCreate()
    {
        base.OnCreate();
        img_camera = GameObject.Find("RawImageActorCardBgCamera")?.GetComponent<Camera>();
        if (img_camera == null)
        {
            var camPrefab = Loader.Load<GameObject>("Assets/Loadable/UI/UIPanel/OCTheatrePanel/RawImageActorCardBgCamera.prefab")?.RetainAsset(gameObject);
            var camObj = Instantiate(camPrefab);
            camObj.name = "RawImageActorCardBgCamera";
            img_camera = camObj.GetComponent<Camera>();
            img_camera.transform.position = new Vector3(25000, 0, 0);
        }

        if (img_camera == null)
        {
            CloseSelf();
            return;
        }

        cameraImage = img_camera.transform.Find("BgCanvas/Image").GetComponent<RawImage>();

        // BgCanvas: Screen Space Camera, refH=520 matchHeight=1, RT=img_camera.targetTexture
        // bgCanvasW = RT.width * 520 / RT.height; factor = bgCanvasW / img_icon_bg.sizeDelta.x
        var camTex = img_camera.targetTexture;
        float previewW = img_icon_bg != null ? img_icon_bg.rectTransform.sizeDelta.x : 0f;
        if (camTex != null && previewW > 0f)
            _cameraFactor = camTex.width * 520f / (camTex.height * previewW);

        btn_close?.onClick.AddListener(CloseSelf);

        btn_ok?.onClick.AddListener(() =>
        {
            try
            {
                byte[] imgBytes = ScreenShotUtils.TakeRenderTextureJpg(img_camera.targetTexture);
                UploadAndCallback(imgBytes);
                onUploadStarted?.Invoke();
                CleanupCamera();
            }
            catch
            {
                TipPanel.ShowToast("保存失败，请重试！");
            }
            CloseSelf();
        });

        SetupDragHandlers();
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        var imgPath = args[0].ToString();
        onUploadSuccess = args.Length > 1 ? args[1] as Action<string> : null;
        uploadFolder = args.Length > 2 && args[2] is string f && !string.IsNullOrEmpty(f)
            ? f
            : $"UgcOCTheatreBG/{AccountDataManager.Inst.Uid}";
        onUploadStarted = args.Length > 3 ? args[3] as Action : null;
        _coverDataCenter = args.Length > 4 ? args[4] as TheatreEditorDataCenter : null;

        theatreInfoPanel?.SetActive(false);

        img_icon.Load(imgPath, true, (ca, bo) =>
        {
            var rt = img_icon.GetComponent<RawImage>().rectTransform;
            rt.anchoredPosition = Vector2.zero;
            rt.localScale       = Vector3.one;
            rt.localRotation    = Quaternion.identity;
            img_icon.GetComponent<RawImage>().SetNativeSize();

            cameraImage.texture = img_icon.GetTexture();
            cameraImage.SetNativeSize();
            cameraImage.rectTransform.anchoredPosition = Vector2.zero;
            cameraImage.rectTransform.localScale       = Vector3.one;
            cameraImage.rectTransform.localRotation    = Quaternion.identity;

            // Default cover: scale to fill img_icon_bg with no empty edges.
            // cameraImage lives in a different canvas (BgCanvas, scale ~2.077) — apply _cameraFactor.
            float texW = rt.sizeDelta.x;
            float texH = rt.sizeDelta.y;
            if (texW > 0 && texH > 0 && img_icon_bg != null)
            {
                var bgSize = img_icon_bg.rectTransform.sizeDelta;
                float fillScale = Mathf.Max(bgSize.x / texW, bgSize.y / texH);
                rt.localScale = new Vector3(fillScale, fillScale, fillScale);
                float camScale = fillScale * _cameraFactor;
                cameraImage.rectTransform.localScale = new Vector3(camScale, camScale, camScale);
            }

            if (_coverDataCenter != null)
                ShowCoverInfo(_coverDataCenter);
        });
    }

    private void ShowCoverInfo(TheatreEditorDataCenter dc)
    {
        if (theatreInfoPanel == null) return;
        theatreInfoPanel.SetActive(true);

        var info = dc.TheatreInfo;
        if (theatreTitle != null) theatreTitle.text = info?.name ?? "";
        if (theatreDes != null) theatreDes.text = info?.desc ?? "";

        if (theatreDialogueNum != null)
        {
            int total = 0;
            foreach (var s in dc.SectionList)
                total += dc.GetNormalDialogueCount(s);
            theatreDialogueNum.text = total.ToString();
        }

        if (theatreImage != null && img_camera != null)
            theatreImage.texture = img_camera.targetTexture;

        if (avatarListRoot != null && itemPrefab != null)
        {
            foreach (Transform child in avatarListRoot)
                Destroy(child.gameObject);
            foreach (var av in dc.AllAvatars)
            {
                var item = Instantiate(itemPrefab, avatarListRoot);
                item.SetData(av.AvatarName, av.AvatarUrl);
            }
        }
    }

    public override void OnHidden()
    {
        base.OnHidden();
        _coverDataCenter = null;
        if (avatarListRoot != null)
        {
            foreach (Transform child in avatarListRoot)
                Destroy(child.gameObject);
        }
        theatreInfoPanel?.SetActive(false);
    }

    private void UploadAndCallback(byte[] imgBytes)
    {
        try
        {
            string uid = AccountDataManager.Inst.Uid;
            uid = string.IsNullOrEmpty(uid) ? "unknown" : uid;
            string fileName = LocalDataUtils.Inst.SaveTempImgRes(uid, imgBytes);
            string uri = $"{uploadFolder}/{Path.GetFileName(fileName)}";

            CosXmlUploadManager.UploadFile(uri, fileName, (uploadedUrl, err) =>
            {
                File.Delete(fileName);
                if (string.IsNullOrEmpty(uploadedUrl))
                {
                    TipPanel.ShowToast("上传失败！");
                    return;
                }

                var req = new Dictionary<string, string> { { "url", uploadedUrl } };
                NetworkManager.Inst.SendHttpRequest<AuditImageData>(
                    HttpUrlDefine.AuditImage,
                    HttpMethod.POST,
                    req,
                    rsp =>
                    {
                        if (rsp != null && rsp.auditResult == (int)AuditResult.Passed)
                            onUploadSuccess?.Invoke(uploadedUrl);
                        else
                            TipPanel.ShowToast("图片审核未通过，请重新上传！");
                    },
                    _ => TipPanel.ShowToast("审核请求失败，请重试"));
            });
        }
        catch (Exception e)
        {
            Debug.LogError("TheatreEditorImageEditPanel upload error: " + e);
            TipPanel.ShowToast("上传失败！");
        }
    }

    private void CleanupCamera()
    {
        if (img_camera == null) return;
        if (img_camera.targetTexture != null)
        {
            img_camera.targetTexture.Release();
            img_camera.targetTexture = null;
        }
        RenderTexture.active = null;
        Destroy(img_camera.gameObject);
        img_camera = null;
    }

    private void SetupDragHandlers()
    {
        if (btn_scale != null)
        {
            var scaleHandler = btn_scale.GetComponent<ImageScaleDragHandler>();
            if (scaleHandler == null) scaleHandler = btn_scale.gameObject.AddComponent<ImageScaleDragHandler>();
            scaleHandler.target = img_icon.RawImage.rectTransform;
            scaleHandler.img_camera = cameraImage.rectTransform;
            scaleHandler.scaleSensitivity = scaleSensitivity;
            scaleHandler.minScale = minIconScale;
            scaleHandler.maxScale = maxIconScale;
            scaleHandler.buttonRect = btn_scale.GetComponent<RectTransform>();
            scaleHandler.cameraFactor = _cameraFactor;
        }

        if (btn_rot != null)
        {
            var rotateHandler = btn_rot.GetComponent<ImageRotateDragHandler>();
            if (rotateHandler == null) rotateHandler = btn_rot.gameObject.AddComponent<ImageRotateDragHandler>();
            rotateHandler.target = img_icon.RawImage.rectTransform;
            rotateHandler.img_camera = cameraImage.rectTransform;
            rotateHandler.rotateMultiplier = rotateMultiplier;
            rotateHandler.buttonRect = btn_rot.GetComponent<RectTransform>();
        }

        if (img_icon != null)
        {
            var moveHandler = img_icon.GetComponent<ImageDragMoveHandler>();
            if (moveHandler == null) moveHandler = img_icon.gameObject.AddComponent<ImageDragMoveHandler>();
            moveHandler.target = img_icon.RawImage.rectTransform;
            moveHandler.img_camera = cameraImage.rectTransform;
            moveHandler.boundary = img_icon_bg != null ? img_icon_bg.rectTransform : null;
            moveHandler.cameraFactor = _cameraFactor;
        }
    }
}
