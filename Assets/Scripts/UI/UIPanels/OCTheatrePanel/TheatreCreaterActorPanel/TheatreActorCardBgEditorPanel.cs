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

public class TheatreActorCardBgEditorPanel : BasePanel<TheatreActorCardBgEditorPanel>
{
    private Camera photoCamera;

    public Button btn_bg;
    public Button btn_close;
    public Button btn_ok;
    public Button btn_scale;
    public Button btn_rot;
    public RemoteImageBehaviour img_icon;
    public Image img_icon_bg;

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
        img_camera = GameObject.Find("RawImageActorCardBgCamera")?.GetComponent<Camera>();
        if (img_camera == null)
        {
            var cam_prefab = Loader.Load<GameObject>("Assets/Loadable/UI/UIPanel/OCTheatrePanel/RawImageActorCardBgCamera.prefab")?.RetainAsset(gameObject);
            var cam_obj = Instantiate(cam_prefab);
            cam_obj.name = "RawImageActorCardBgCamera";
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
                CloseSelf();
            });
        }

        if (btn_ok != null)
        {
            btn_ok.onClick.AddListener(() =>
            {
                 try
                {
                    byte[] imgBytes = ScreenShotUtils.TakeRenderTexture(img_camera.targetTexture);
                    Save(imgBytes);
                    
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

     private void Save( byte[] imgBytes)
    {
        try
        {
            string userId = AccountDataManager.Inst.Uid;
            userId = string.IsNullOrEmpty(userId) ? "shotTemplate" : userId;
            string fileName = LocalDataUtils.Inst.SaveTempImgRes(userId, imgBytes);

            var uri = $"OCTheatreActor/bg/{AccountDataManager.Inst.Uid}/{System.IO.Path.GetFileName(fileName)}";
            CosXmlUploadManager.UploadFile(uri, fileName, (uploadedUrl, err) =>
            {
                File.Delete(fileName);
                if (string.IsNullOrEmpty(uploadedUrl))
                {
                    Debug.Log("ActorCardView - 上传失败!");
                     //TipPanel.ShowToast("上传失败!");
                     return;
                }
                //本地上传的图要审核
                
                var req = new Dictionary<string, string>()
                {
                    {"url", uploadedUrl}
                };
                
                NetworkManager.Inst.SendHttpRequest<AuditImageData>(HttpUrlDefine.AuditImage,
                HttpMethod.POST, req, rsp =>
                {
                    if (rsp != null && rsp.auditResult == (int)AuditResult.Passed)
                    {
                        OCTheatreActorEditorDataManager.Inst.SetActorCarBg(uploadedUrl);
                    }
                    else
                    {
                        //TipPanel.ShowToast("图片审核未通过，请重新上传!");
                    }
                }, 
                failRsp =>
                {
                     Debug.Log("ActorCardView 上传失败!");
                        //TipPanel.ShowToast("上传失败!");
                });
                
            });
        }
        catch (Exception e)
        {
             Debug.Log("error " + e.ToString());
            //TipPanel.ShowToast("上传失败!");
        }
    }
    public override void OnShow(params object[] args)
    {
        base.OnShow(args);

        var imgPath = args[0].ToString();
        img_icon.Load(imgPath, true, (ca, bo) =>
        {
            img_icon.GetComponent<RawImage>().SetNativeSize();
            cameraImage.texture = img_icon.GetTexture();
            cameraImage.SetNativeSize();
        });
    } 

   

    private void SetupDragHandlers()
    {
        if (btn_scale != null)
        {
            var scaleHandler = btn_scale.GetComponent<ImageScaleDragHandler>();
            if (scaleHandler == null) scaleHandler = btn_scale.gameObject.AddComponent<ImageScaleDragHandler>();
            scaleHandler.target = img_icon.RawImage.rectTransform;
            //scaleHandler.avatar = avatar != null ? avatar.RawImage.rectTransform : null;
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
            //rotateHandler.avatar = avatar != null ? avatar.RawImage.rectTransform : null;
            rotateHandler.img_camera = cameraImage.rectTransform;
            rotateHandler.rotateMultiplier = rotateMultiplier;
            rotateHandler.buttonRect = btn_rot.GetComponent<RectTransform>();
        }


        if (img_icon != null)
        {
            var moveHandler = img_icon.GetComponent<ImageDragMoveHandler>();
            if (moveHandler == null) moveHandler = img_icon.gameObject.AddComponent<ImageDragMoveHandler>();
            moveHandler.target = img_icon.RawImage.rectTransform;
            //moveHandler.avatar = avatar != null ? avatar.RawImage.rectTransform : null;
            moveHandler.img_camera = cameraImage.rectTransform;
            moveHandler.boundary = img_icon_bg != null ? img_icon_bg.rectTransform : null;
        }
    }
}
