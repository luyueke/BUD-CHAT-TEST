
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Com.TheFallenGames.OSA.Util.IO;
using Game.Avatar;
using Game.COSXML;
using Game.Utils;
using GameData;
using GameData.Base;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UI.Base;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TheatreActorCharacterEditorPanel : BasePanel<TheatreActorCharacterEditorPanel>
{
    private Camera photoCamera;
    private BaseAvatarWrapper avatarWrapper;

    public Button btn_bg;
    public Button btn_close;
    public Button btn_ok;
    public Button btn_scale;
    public Button btn_rot;
    public RemoteImageBehaviour img_icon;
    public Image img_icon_bg;
    public RemoteImageBehaviour avatar;

    CharacterViewExpressionItem currentExpressionItem;//当前表情数据（如果是从相册选图打开的话）

    [Header("拖拽缩放(等比)")]
    public float scaleSensitivity = 0.003f; // 像素 -> 缩放系数
    public float minIconScale = 0.1f;
    public float maxIconScale = 10f;

    [Header("拖拽旋转")]
    public float rotateMultiplier = 1f;
    private RawImage cameraImage;
    private Camera img_camera;
   
    public override void OnCreate()
    {
        base.OnCreate();
        img_camera = GameObject.Find("RawImageCamera")?.GetComponent<Camera>();
        if (img_camera == null)
        {
            var cam_prefab = Loader.Load<GameObject>("Assets/Loadable/UI/UIPanel/OCTheatrePanel/RawImageCamera.prefab")?.RetainAsset(gameObject);
            var cam_obj = Instantiate(cam_prefab);
            cam_obj.name = "RawImageCamera";
            img_camera = cam_obj.GetComponent<Camera>();
            img_camera.transform.position = new Vector3(25000, 0, 0); 
        }
        
        if(img_camera == null)
        {
            CloseSelf();
            return;
        }
        cameraImage = img_camera.transform.Find("BgCanvas/Image").GetComponent<RawImage>();    
         
        if (btn_close != null)
        {
            btn_close.onClick.AddListener(() =>
            {
                if (currentExpressionItem != null) currentExpressionItem.HideLoading();
                CloseSelf();
            });
        }

        if (btn_ok != null)
        {
            btn_ok.onClick.AddListener(() =>
            {
                 try
                {
                    if (currentExpressionItem != null)
                    {
                        byte[] imgBytes = ScreenShotUtils.TakeRenderTexture(img_camera.targetTexture);
                        Save(imgBytes);
                    }

                    if (img_camera != null)
                    {
                        if (img_camera.targetTexture != null)
                        {
                            img_camera.targetTexture.Release();
                            img_camera.targetTexture = null;
                        }
                        RenderTexture.active = null;
                        Destroy(img_camera.gameObject);
                        img_camera = null;
                    }
                    if (photoCamera != null)
                    {
                        Destroy(photoCamera.gameObject);
                        photoCamera = null;
                    }

                    UIManager.Inst.ClosePanel(PanelId.FittingRoomPanel);

                    if (currentExpressionItem != null && currentExpressionItem.expressionData.expressionType == (int)ExpressionType.custom)//自定义要弹出命名界面
                    {
                        UIManager.Inst.OpenPanel(PanelId.CommonSetNamePanel, new CommonSetNamePanelData()
                        {
                            title = "命名表情",
                            inputTxt = currentExpressionItem.expressionData.expressionName,
                            maxLength = 7,
                            btn_close_action = _ => { UIManager.Inst.ClosePanel(PanelId.CommonSetNamePanel); },
                            btn_ok_action = str =>
                            {
                                if (string.IsNullOrEmpty(str))
                                {
                                    TipPanel.ShowToast("名称不能为空");
                                    return;
                                }
                                var expressionData = currentExpressionItem.expressionData;
                                OCTheatreActorEditorDataManager.Inst.SetExpressionName(expressionData, str);
                                currentExpressionItem.ChangeName(str);
                                UIManager.Inst.ClosePanel(PanelId.CommonSetNamePanel);
                            }
                        });
                    }
                }
                catch
                {
                    TipPanel.ShowToast("保存失败,请重试！");
                }
                CloseSelf();
            });
           
        }
        // 通过拖拽按钮来驱动 img_icon
        SetupDragHandlers();
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        currentExpressionItem = args[0] as CharacterViewExpressionItem;;
    
        switch (currentExpressionItem.expressionData.mType)
        {
            case  (int)OCTAvatarExpressionType.materials://截屏
                avatarWrapper = args[1] as BaseAvatarWrapper;
                TakeScreenshot();
            break;
            case  (int)OCTAvatarExpressionType.image://相册
                var imgPath = args[1].ToString();
                img_icon.Load(imgPath, true, (ca, bo) =>
                {
                    var tex = img_icon.GetTexture();
                    cameraImage.texture = tex;
                    FitRectToTexture(img_icon.RawImage.rectTransform, tex);
                    FitRectToTexture(cameraImage.rectTransform, tex);
                    //img_icon.RawImage.rectTransform.anchoredPosition = new Vector2(0, -150);
                    //img_icon.RawImage.rectTransform.localScale = new Vector3(2, 2, 2);
                    //cameraImage.rectTransform.anchoredPosition = new Vector2(0, -150);
                    //cameraImage.rectTransform.localScale = new Vector3(2, 2, 2);
                });
                avatar.Load(imgPath, true, (ca, bo) =>
                {
                    FitRectToTexture(avatar.RawImage.rectTransform, avatar.GetTexture());
                    //avatar.RawImage.rectTransform.anchoredPosition = ToAvatarPos(new Vector2(0, -150));
                    //avatar.RawImage.rectTransform.localScale = new Vector3(2, 2, 2);
                });
            break;
        }
    } 

    #region 截屏相关

    private void TakeScreenshot()
    {
       
        if (currentExpressionItem.expressionData.mType == (int)OCTAvatarExpressionType.materials && avatarWrapper == null)
        {
            LoggerUtils.LogError("TheatreActorCharacterEditorPanel: avatarWrapper is null, cannot take screenshot");
            return;
        }

        if (photoCamera == null)
        {
            photoCamera = Loader.Load<GameObject>("Assets/Arts/Prefabs/CharacterUICamera.prefab")
                .Instantiate(avatarWrapper.Avatar.transform).GetComponent<Camera>();
            photoCamera.transform.SetParent(null, true); // 脱离父节点，避免随角色旋转

            Vector3 avatarPos = avatarWrapper.Avatar.transform.position;

            // 用蒙皮网格包围盒求角色实际垂直中心，分辨率变化时始终垂直居中
            float centerY = avatarPos.y;
            var skins = avatarWrapper.Avatar.GetComponentsInChildren<SkinnedMeshRenderer>(false);
            if (skins.Length > 0)
            {
                float minY = float.MaxValue, maxY = float.MinValue;
                foreach (var s in skins)
                {
                    if (s.bounds.min.y < minY) minY = s.bounds.min.y;
                    if (s.bounds.max.y > maxY) maxY = s.bounds.max.y;
                }
                if (minY < maxY) centerY = (minY + maxY) * 0.5f;
            }

            photoCamera.transform.position = new Vector3(avatarPos.x, centerY, avatarPos.z - 1);
            photoCamera.transform.eulerAngles = new Vector3(0, 0, 0);
            photoCamera.orthographicSize = photoCamera.orthographicSize
                * ResolutionAutoFit.CameraScale
                * avatarWrapper.Avatar.transform.localScale.x * 1.2f;
        }

        // Panel 可能已 inactive，借用临时 GameObject 运行协程
        var runner = new GameObject("TakeActorPhotoRunner").AddComponent<CoroutineRunner>();
        runner.Run(TakeActorPhoto(runner));
    }

    private IEnumerator TakeActorPhoto(CoroutineRunner runner)
    {
        yield return new WaitForEndOfFrame();
        try
        {
            int fullW = photoCamera.pixelWidth;
            int fullH = photoCamera.pixelHeight;

            // 渲染全屏（保留原有 orthographicSize 校准）
            var rt = new RenderTexture(fullW, fullH, 24);
            photoCamera.targetTexture = rt;
            photoCamera.Render();
            RenderTexture.active = rt;

            // 从渲染中心裁出正方形，消除竖屏比例导致的横向拉伸
            int size = Mathf.Min(fullW, fullH);
            int srcX = (fullW - size) / 2;
            int srcY = (fullH - size) / 2;
            Texture2D tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
            tex.ReadPixels(new Rect(srcX, srcY, size, size), 0, 0);
            tex.Apply();
            photoCamera.targetTexture = null;
            RenderTexture.active = null;
            Destroy(rt);

            if (avatar != null && avatar.RawImage != null)
            {
                avatar.RawImage.texture = tex;
                var avatarRt = avatar.RawImage.rectTransform;
                avatarRt.anchoredPosition = ToAvatarPos(new Vector2(0, -150));
                avatarRt.localScale       = new Vector3(2, 2, 2);
                avatarRt.localRotation    = Quaternion.identity;
            }
            if (img_icon != null && img_icon.RawImage != null)
            {
                img_icon.RawImage.texture = tex;
                var rawRt = img_icon.RawImage.rectTransform;
                rawRt.anchoredPosition = new Vector2(0, -150);
                rawRt.localScale       = new Vector3(2, 2, 2);
                rawRt.localRotation    = Quaternion.identity;
            }
            if (cameraImage != null)
            {
                cameraImage.texture = tex;
                cameraImage.rectTransform.anchoredPosition = new Vector2(0, -150);
                cameraImage.rectTransform.localScale       = new Vector3(2, 2, 2);
                cameraImage.rectTransform.localRotation    = Quaternion.identity;
            }
        }
        catch (Exception e)
        {
            TipPanel.ShowToast("截图失败");
            LoggerUtils.LogError(e.Message);
        }
        finally
        {
            Destroy(runner.gameObject);
        }
    }

    private class CoroutineRunner : MonoBehaviour
    {
        public void Run(IEnumerator coroutine) => StartCoroutine(coroutine);
    }

    private void Save( byte[] imgBytes)
    {
        try
        {
            string userId = AccountDataManager.Inst.Uid;
            userId = string.IsNullOrEmpty(userId) ? "shotTemplate" : userId;
            string fileName = LocalDataUtils.Inst.SaveTempImgRes(userId, imgBytes);

            var uri = $"OCTheatreActor/imgs/{AccountDataManager.Inst.Uid}/{System.IO.Path.GetFileName(fileName)}";
            CosXmlUploadManager.UploadFile(uri, fileName, (uploadedUrl, err) =>
            {
                File.Delete(fileName);
                if (string.IsNullOrEmpty(uploadedUrl))
                {
                     TipPanel.ShowToast("上传失败!");
                     return;
                }
                //本地上传的图要审核
                if(currentExpressionItem.expressionData.mType == (int)OCTAvatarExpressionType.image)
                {
                    var req = new Dictionary<string, string>()
                    {
                        {"url", uploadedUrl}
                    };
                    
                    NetworkManager.Inst.SendHttpRequest<AuditImageData>(HttpUrlDefine.AuditImage,
                    HttpMethod.POST, req, rsp =>
                    {
                        if (rsp != null && rsp.auditResult == (int)AuditResult.Passed)
                        {
                            currentExpressionItem.ChangeIcon(uploadedUrl);
                            if(currentExpressionItem.expressionData.expressionType == (int)ExpressionType.normal)
                            {
                                OCTheatreActorEditorDataManager.Inst.SetCover(uploadedUrl);
                            }
                        }
                        else
                        {
                            TipPanel.ShowToast("图片审核未通过，请重新上传!");
                        }
                    }, failRsp =>
                    {
                         TipPanel.ShowToast("上传失败!");
                    });
                    CloseSelf();
                    UIManager.Inst.ClosePanel(PanelId.FittingRoomPanel);
                    return;
                }
                
                currentExpressionItem.ChangeIcon(uploadedUrl);
                if(currentExpressionItem.expressionData.expressionType == (int)ExpressionType.normal)
                {
                    OCTheatreActorEditorDataManager.Inst.SetCover(uploadedUrl);
                }
                   
                CloseSelf();
                UIManager.Inst.ClosePanel(PanelId.FittingRoomPanel);
            });
        }
        catch (Exception e)
        {
            Debug.LogError(e.ToString());
            TipPanel.ShowToast("上传失败!");
        }
    }

    #endregion

    // 将 img_icon 坐标按 boundary→avatarParent 比例映射，与拖拽移动逻辑一致
    private Vector2 ToAvatarPos(Vector2 imgIconPos)
    {
        var boundary = img_icon_bg != null ? img_icon_bg.rectTransform : null;
        var avatarParent = (avatar != null && avatar.RawImage != null)
            ? avatar.RawImage.rectTransform.parent as RectTransform : null;
        if (boundary == null || avatarParent == null) return Vector2.zero;
        float halfW = boundary.rect.width  * 0.5f;
        float halfH = boundary.rect.height * 0.5f;
        return new Vector2(
            halfW > 0f ? imgIconPos.x / halfW * avatarParent.rect.width  * 0.5f : 0f,
            halfH > 0f ? imgIconPos.y / halfH * avatarParent.rect.height * 0.5f : 0f
        );
    }

    private static void FitRectToTexture(RectTransform rt, Texture tex)
    {
        if (rt == null || tex == null || tex.width == 0 || tex.height == 0) return;
        float aspect = (float)tex.width / tex.height;
        float baseSize = Mathf.Max(rt.sizeDelta.x, rt.sizeDelta.y);
        rt.sizeDelta = aspect >= 1f
            ? new Vector2(baseSize, baseSize / aspect)
            : new Vector2(baseSize * aspect, baseSize);
    }

    private void SetupDragHandlers()
    {
        if (btn_scale != null)
        {
            var scaleHandler = btn_scale.GetComponent<ImageScaleDragHandler>();
            if (scaleHandler == null) scaleHandler = btn_scale.gameObject.AddComponent<ImageScaleDragHandler>();
            scaleHandler.target = img_icon.RawImage.rectTransform;
            scaleHandler.avatar = avatar != null ? avatar.RawImage.rectTransform : null;
            scaleHandler.img_camera = cameraImage.rectTransform;
            scaleHandler.scaleSensitivity = scaleSensitivity;
            scaleHandler.minScale = minIconScale;
            scaleHandler.maxScale = maxIconScale;
            scaleHandler.buttonRect = btn_scale.GetComponent<RectTransform>();
        }

        if (btn_rot != null)
        {
            var rotateHandler = btn_rot.GetComponent<ImageRotateDragHandler>();
            if (rotateHandler == null) rotateHandler = btn_rot.gameObject.AddComponent<ImageRotateDragHandler>();
            rotateHandler.target = img_icon.RawImage.rectTransform;
            rotateHandler.avatar = avatar != null ? avatar.RawImage.rectTransform : null;
            rotateHandler.img_camera = cameraImage.rectTransform;
            rotateHandler.rotateMultiplier = rotateMultiplier;
            rotateHandler.buttonRect = btn_rot.GetComponent<RectTransform>();
        }


        if (img_icon != null)
        {
            var moveHandler = img_icon.GetComponent<ImageDragMoveHandler>();
            if (moveHandler == null) moveHandler = img_icon.gameObject.AddComponent<ImageDragMoveHandler>();
            moveHandler.target = img_icon.RawImage.rectTransform;
            moveHandler.avatar = avatar != null ? avatar.RawImage.rectTransform : null;
            moveHandler.img_camera = cameraImage.rectTransform;
            moveHandler.boundary = img_icon_bg != null ? img_icon_bg.rectTransform : null;
        }
    }

}

/// <summary>
/// 拖拽 `btn_scale` 等比缩放 `img_icon`
/// </summary>
public class ImageScaleDragHandler : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [HideInInspector] public RectTransform target;
    [HideInInspector] public RectTransform buttonRect;
    [HideInInspector] public RectTransform avatar;
    [HideInInspector] public RectTransform img_camera;
    public float scaleSensitivity = 0.003f;
    public float minScale = 0.1f;
    public float maxScale = 10f;
    [HideInInspector] public float cameraFactor = 1f;

    private Vector2 _startPointerPos;
    private Vector3 _startScale;
    private bool _dragging;
    private bool _axisLocked;
    private bool _useXAxis;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (target == null) return;

        _dragging = true;
        _axisLocked = false;
        _startPointerPos = eventData.position;
        _startScale = target.localScale;

        // 避免拖拽时触发 Button 的 click
        eventData.eligibleForClick = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_dragging || target == null) return;

        var dx = eventData.position.x - _startPointerPos.x;
        var dy = eventData.position.y - _startPointerPos.y;

        // 第一帧超过阈值时锁定轴，避免轴切换导致 scale 跳变
        if (!_axisLocked && (Mathf.Abs(dx) > 2f || Mathf.Abs(dy) > 2f))
        {
            _useXAxis = Mathf.Abs(dx) >= Mathf.Abs(dy);
            _axisLocked = true;
        }

        float d = _useXAxis ? dx : dy;
        float factor = 1f + d * scaleSensitivity;

        float newScale = Mathf.Clamp(_startScale.x * factor, minScale, maxScale);
        target.localScale = new Vector3(newScale, newScale, newScale);
        
        // avatar 跟随 img_icon 同比例缩放
        if (avatar != null)
        {
            avatar.localScale = new Vector3(newScale, newScale, newScale);
        }
        if(img_camera != null)
        {
            float cs = newScale * cameraFactor;
            img_camera.localScale = new Vector3(cs, cs, cs);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _dragging = false;
    }
}

/// <summary>
/// 拖拽 img_icon 在正方形区域内移动，边界由 boundary（img_icon_bg）限定
/// </summary>
public class ImageDragMoveHandler : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [HideInInspector] public RectTransform target;
    [HideInInspector] public RectTransform avatar;
    [HideInInspector] public RectTransform img_camera;
    /// <summary>正方形边界，通常赋值为 img_icon_bg.rectTransform</summary>
    [HideInInspector] public RectTransform boundary;
    [HideInInspector] public float cameraFactor = 1f;

    private bool _dragging;
    private Canvas _canvas;
    private Vector2 _startTargetPos;
    private Vector2 _startAvatarPos;

    private void Awake()
    {
        _canvas = GetComponentInParent<Canvas>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (target == null) return;
        _dragging = true;
        _startTargetPos = target.anchoredPosition;
        if (avatar != null) _startAvatarPos = avatar.anchoredPosition;
        eventData.eligibleForClick = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_dragging || target == null) return;

        float scaleFactor = (_canvas != null) ? _canvas.scaleFactor : 1f;
        Vector2 delta = eventData.delta / scaleFactor;

        Vector2 oldPos = target.anchoredPosition;
        Vector2 newPos = oldPos + delta;
        ClampToBoundary(ref newPos);
        target.anchoredPosition = newPos;

        if (avatar != null)
        {
            // img_icon 相对拖拽起点移动了多少比例的 boundary 半径，
            // avatar 就从其起始位置移动同等比例的父容器半径。
            // 从起始点算 delta 而非绝对映射，消除第一帧的位置闪跳。
            var avatarParent = avatar.parent as RectTransform;
            if (boundary != null && avatarParent != null)
            {
                float halfW = boundary.rect.width  * 0.5f;
                float halfH = boundary.rect.height * 0.5f;
                Vector2 normDelta = new(
                    halfW > 0f ? (newPos.x - _startTargetPos.x) / halfW : 0f,
                    halfH > 0f ? (newPos.y - _startTargetPos.y) / halfH : 0f
                );
                avatar.anchoredPosition = _startAvatarPos + new Vector2(
                    normDelta.x * avatarParent.rect.width  * 0.5f,
                    normDelta.y * avatarParent.rect.height * 0.5f
                );
            }
            else
            {
                avatar.anchoredPosition += newPos - oldPos;
            }
        }

        if (img_camera != null)
            img_camera.anchoredPosition = target.anchoredPosition * cameraFactor;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _dragging = false;
    }

    private void ClampToBoundary(ref Vector2 pos)
    {
        if (boundary == null) return;

        // target 与 boundary 共用同一父节点时，直接用 anchoredPosition 做边界中心
        Vector2 center = boundary.anchoredPosition;
        float halfW = boundary.rect.width * 0.5f;
        float halfH = boundary.rect.height * 0.5f;

        pos.x = Mathf.Clamp(pos.x, center.x - halfW, center.x + halfW);
        pos.y = Mathf.Clamp(pos.y, center.y - halfH, center.y + halfH);
    }
}

/// <summary>
/// 拖拽 `btn_rot` 旋转 `img_icon`（绕 Z 轴）
/// </summary>
public class ImageRotateDragHandler : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [HideInInspector] public RectTransform target;
    [HideInInspector] public RectTransform buttonRect;
    [HideInInspector] public RectTransform avatar;
    [HideInInspector] public RectTransform img_camera;

    public float rotateMultiplier = 1f;

    private Vector2 _centerScreenPoint;
    private float _startAngleDeg;
    private float _startLocalZ;
    private bool _dragging;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (target == null) return;

        _dragging = true;
        eventData.eligibleForClick = false;

        _centerScreenPoint = RectTransformUtility.WorldToScreenPoint(eventData.pressEventCamera, target.position);
        _startAngleDeg = Mathf.Atan2(eventData.position.y - _centerScreenPoint.y, eventData.position.x - _centerScreenPoint.x) * Mathf.Rad2Deg;
        _startLocalZ = target.localEulerAngles.z;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_dragging || target == null) return;

        // 旋转时中心点随 target.position 更新（防止父节点或摄像机变化）
        _centerScreenPoint = RectTransformUtility.WorldToScreenPoint(eventData.pressEventCamera, target.position);

        float currentAngleDeg = Mathf.Atan2(eventData.position.y - _centerScreenPoint.y, eventData.position.x - _centerScreenPoint.x) * Mathf.Rad2Deg;
        float delta = currentAngleDeg - _startAngleDeg;

        float z = _startLocalZ + delta * rotateMultiplier;
        target.localRotation = Quaternion.Euler(0f, 0f, z);
        
        // avatar 跟随 img_icon 同比例旋转（基于当前角度累加）
        if (avatar != null)
        {
            avatar.localRotation = Quaternion.Euler(0f, 0f, z);
            
        }
        if(img_camera != null)
        {
            img_camera.localRotation = Quaternion.Euler(0f, 0f, z);
            
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _dragging = false;
    }
}
