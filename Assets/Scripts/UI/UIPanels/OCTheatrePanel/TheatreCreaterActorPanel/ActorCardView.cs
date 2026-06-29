
using System;
using System.Collections.Generic;
using System.IO;
using Com.TheFallenGames.OSA.Util.IO;
using Game.COSXML;
using Game.Utils;
using GameData;
using GameData.Base;
using GameData.BaseInfo;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using RTG;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class ActorCardView : MonoBehaviour
{
    private Button btn_change_toggle_bg;
    private Button btn_change_toggle_c;
    private Button btn_change_txt;
    private Button btn_change_bg;

    private Button btn_def;
    private CButton cbtn_up_bg;
    private RemoteImageBehaviour cbtn_up_bg1;
    private Transform img_add;
    private Transform btns;
    private CButton btn_close_btns;
    private CButton cbtn_replace;
    private CButton cbtn_del;

    private Transform ChangeColorPanel;

   
    //横条背景默认颜色
    private string defColor1 = "3D4760";
    //强调默认颜色
  private string defColor2 = "7FF6BF";
  //文本默认颜色
    private string defColor3 = "1D253C";
    //背景默认颜色
      private string defColor4 = "9A62FF";
      bool isInit = false;
    private void Init()
    {
        if(isInit) return;
        isInit = true;
        MessageHelper.AddListener<OCTheatreAvatarInfo>(MessageName.ActorCardInfoUpdate,ActorCardInfoUpdate);
        btn_change_toggle_bg = GameObjectEx.FindComponentByName<Button>(transform,"btn_change_toggle_bg");
        btn_change_toggle_c = GameObjectEx.FindComponentByName<Button>(transform, "btn_change_toggle_c");
        btn_change_txt = GameObjectEx.FindComponentByName<Button>(transform, "btn_change_txt");
        btn_change_bg = GameObjectEx.FindComponentByName<Button>(transform, "btn_change_bg");
        btn_def = GameObjectEx.FindComponentByName<Button>(transform, "btn_def");
        cbtn_up_bg = GameObjectEx.FindComponentByName<CButton>(transform, "cbtn_up_bg");
        ChangeColorPanel = GameObjectEx.FindComponentByName<Transform>(transform, "ChangeColorPanel");
        cbtn_up_bg1 = GameObjectEx.FindComponentByName<RemoteImageBehaviour>(cbtn_up_bg.transform, "cbtn_up_bg1");
        btns = GameObjectEx.FindComponentByName<Transform>(cbtn_up_bg.transform, "btns");
        btn_close_btns = GameObjectEx.FindComponentByName<CButton>(cbtn_up_bg.transform, "btn_close_btns");
        cbtn_replace = GameObjectEx.FindComponentByName<CButton>(cbtn_up_bg.transform, "cbtn_replace");
        cbtn_del = GameObjectEx.FindComponentByName<CButton>(cbtn_up_bg.transform, "cbtn_del");
    
        cbtn_up_bg1.GetComponent<CButton>().onClick.AddListener(() =>
        {
            btns.gameObject.SetActive(true);
        });
        btn_close_btns.onClick.AddListener(() =>
        {
            btns.gameObject.SetActive(false);
        });

        cbtn_replace.onClick.AddListener(() =>
        {
            OnBtnUploadCoverClick();
        });

        cbtn_del.onClick.AddListener(() =>
        {
            OCTheatreActorEditorDataManager.Inst.SetActorCarBg(string.Empty);
            btns.gameObject.SetActive(false);
            var actor = OCTheatreActorEditorDataManager.Inst.GetCurActor();
            ChangeBg(actor);
        });

        img_add =  GameObjectEx.FindComponentByName<Transform>(cbtn_up_bg.transform, "img_add");
        
        MessageHelper.AddListener<object[]>(MessageName.OnSliderValueChanged,OnSliderValueChanged);
        
        btn_change_toggle_bg.onClick.AddListener((() =>
        {
            Image colorImg = btn_change_toggle_bg.transform.Find("btn_color/img_color").gameObject.GetComponent<Image>();
            var panel = UIManager.Inst.SwapPanel(PanelId.ActorColorChangeColorPanel, 1, colorImg.color);
           
            panel.transform.SetParent(ChangeColorPanel);
        }));
        btn_change_toggle_c.onClick.AddListener((() =>
        {
            Image colorImg = btn_change_toggle_c.transform.Find("btn_color/img_color").gameObject.GetComponent<Image>();
            var panel = UIManager.Inst.SwapPanel(PanelId.ActorColorChangeColorPanel, 2, colorImg.color);
       
            panel.transform.SetParent(ChangeColorPanel);
        }));
        btn_change_txt.onClick.AddListener((() =>
        {
            Image colorImg = btn_change_txt.transform.Find("btn_color/img_color").gameObject.GetComponent<Image>();
            var panel = UIManager.Inst.SwapPanel(PanelId.ActorColorChangeColorPanel, 3, colorImg.color);
            
            panel.transform.SetParent(ChangeColorPanel);
        }));
        btn_change_bg.onClick.AddListener((() =>
        {
            Image colorImg = btn_change_bg.transform.Find("btn_color/img_color").gameObject.GetComponent<Image>();
           
            var panel = UIManager.Inst.SwapPanel(PanelId.ActorColorChangeColorPanel, 4, colorImg.color);
            panel.transform.SetParent(ChangeColorPanel);
        }));
        btn_def.onClick.AddListener((() =>
        {
            Color m_color1 = ColorUtility.TryParseHtmlString($"#{defColor1}", out var color1) ? color1 : Color.white;
            btn_change_toggle_bg.transform.Find("btn_color/img_color").gameObject.GetComponent<Image>().color = m_color1;
            Color m_color2 = ColorUtility.TryParseHtmlString($"#{defColor2}", out var color2) ? color2 : Color.white;
            btn_change_toggle_c.transform.Find("btn_color/img_color").gameObject.GetComponent<Image>().color = m_color2;
            Color m_color3 = ColorUtility.TryParseHtmlString($"#{defColor3}", out var color3) ? color3 : Color.white;
            btn_change_txt.transform.Find("btn_color/img_color").gameObject.GetComponent<Image>().color = m_color3;
            Color m_color4 = ColorUtility.TryParseHtmlString($"#{defColor4}", out var color4) ? color4 : Color.white;
            btn_change_bg.transform.Find("btn_color/img_color").gameObject.GetComponent<Image>().color = m_color4;
            object[] m_args = new object[2];
            m_args[0] = 1;
            m_args[1] = m_color1;
            MessageHelper.Broadcast(MessageName.OnSliderValueChanged,m_args);
            m_args[0] = 2;
            m_args[1] = m_color2;
            MessageHelper.Broadcast(MessageName.OnSliderValueChanged,m_args);
            m_args[0] = 3;
            m_args[1] = m_color3;
            MessageHelper.Broadcast(MessageName.OnSliderValueChanged,m_args);
            m_args[0] = 4;
            m_args[1] = m_color4;
            MessageHelper.Broadcast(MessageName.OnSliderValueChanged,m_args);
        }));
        cbtn_up_bg.onClick.AddListener((() =>
        {
            OnBtnUploadCoverClick();
        }));
    }
    private void Awake()
    {
       Init();

    }

    private void ActorCardInfoUpdate(OCTheatreAvatarInfo actor)
    {
        Init();
        OCTheatreAvatarInfo m_info;
        if(actor != null)
            m_info = actor;
        else
            m_info = OCTheatreActorEditorDataManager.Inst.GetCurActor();
        ChangeBg(m_info);
    }
    private void ChangeBg(OCTheatreAvatarInfo _actor)
    {
        if(string.IsNullOrEmpty(_actor.bgUrl))
        {
            cbtn_up_bg1.ResetRawImage();
            img_add.gameObject.SetActive(true);
            cbtn_up_bg1.gameObject.SetActive(false);
            return;
        }
        cbtn_up_bg1.Load(_actor.bgUrl);
        cbtn_up_bg1.gameObject.SetActive(true);
        img_add.gameObject.SetActive(false);
    }
     
    private void OnEnable()
    {
        InitColor();
    }
    private void InitColor()
    {
        var actor = OCTheatreActorEditorDataManager.Inst.GetCurActor();
        if(string.IsNullOrEmpty(actor.barColor))
            actor.barColor = defColor1;
        Color m_color1 = ColorUtility.TryParseHtmlString($"#{actor.barColor}", out var color1) ? color1 : Color.white;
        btn_change_toggle_bg.transform.Find("btn_color/img_color").gameObject.GetComponent<Image>().color = m_color1;
        if(string.IsNullOrEmpty(actor.mainColor))
            actor.mainColor = defColor2;
        Color m_color2 = ColorUtility.TryParseHtmlString($"#{actor.mainColor}", out var color2) ? color2 : Color.white;
        btn_change_toggle_c.transform.Find("btn_color/img_color").gameObject.GetComponent<Image>().color = m_color2;
        if(string.IsNullOrEmpty(actor.textColor))
            actor.textColor = defColor3;
        Color m_color3 = ColorUtility.TryParseHtmlString($"#{actor.textColor}", out var color3) ? color3 : Color.white;
        btn_change_txt.transform.Find("btn_color/img_color").gameObject.GetComponent<Image>().color = m_color3;
          if(string.IsNullOrEmpty(actor.bgColor))
            actor.bgColor = defColor4;
        Color m_color4 = ColorUtility.TryParseHtmlString($"#{actor.bgColor}", out var color4) ? color4 : Color.white;
        btn_change_bg.transform.Find("btn_color/img_color").gameObject.GetComponent<Image>().color = m_color4;
        object[] m_args = new object[2];
        m_args[0] = 1;
        m_args[1] = m_color1;
        MessageHelper.Broadcast(MessageName.OnSliderValueChanged,m_args);
        m_args[0] = 2;
        m_args[1] = m_color2;
        MessageHelper.Broadcast(MessageName.OnSliderValueChanged,m_args);
        m_args[0] = 3;
        m_args[1] = m_color3;
        MessageHelper.Broadcast(MessageName.OnSliderValueChanged,m_args);
        m_args[0] = 4;
        m_args[1] = m_color4;
        MessageHelper.Broadcast(MessageName.OnSliderValueChanged,m_args);
    }
      private void OnBtnUploadCoverClick()
        {
           
#if UNITY_EDITOR
            //todo 在unity编辑器下，直接使用本地图片上传测试
            var path = $"Assets/Arts/UITexture/Cover/Map/{"10000"}.png";
            //UploadCustomCover(path);
            LoadImgCallback(path);
            //return;
#endif

            OpenSystemAlbumParams albumParams = new OpenSystemAlbumParams()
            {
                albumType = 1, //0竖屏 1横屏
                // isCrop = 1, //裁剪
                // cropAspectRatio = 1, //宽高比
            };
            MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.openSystemAlbum);
            MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.openSystemAlbum, OnNativeUrl);
            MobileInterface.Instance.OpenSystemAlbum(JsonConvert.SerializeObject(albumParams));
        }

        private void OnNativeUrl(string msg)
        {
            MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.openSystemAlbum);
            AlbumResData authData = JsonConvert.DeserializeObject<AlbumResData>(msg);
            if (authData == null || string.IsNullOrEmpty(authData.localUrl))
            {
                LoggerUtils.LogError("authData.localUrl is null ", authData?.localUrl);
                return;
            }
            LoadImgCallback(authData.localUrl);
        }

        private void LoadImgCallback(string filePath)
        {
            UIManager.Inst.OpenPanel(PanelId.TheatreActorCardBgEditorPanel,filePath);
            // byte[] imgBytes = File.ReadAllBytes(filePath);
            // Save(imgBytes);
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
    private void OnSliderValueChanged(object[] args)
    {
        int type = args[0] as int? ?? 1;
        Color m_color = args[1] as Color? ?? default;
        switch (type)
        {
            case 1:
                btn_change_toggle_bg.transform.Find("btn_color/img_color").gameObject.GetComponent<Image>().color = m_color;
                OCTheatreActorEditorDataManager.Inst.SetBarColor(ColorUtility.ToHtmlStringRGB(m_color));
                break;
            case 2:
                btn_change_toggle_c.transform.Find("btn_color/img_color").gameObject.GetComponent<Image>().color = m_color;
                 OCTheatreActorEditorDataManager.Inst.SetMainColor(ColorUtility.ToHtmlStringRGB(m_color));
                break;
            case 3:
                btn_change_txt.transform.Find("btn_color/img_color").gameObject.GetComponent<Image>().color = m_color;
                 OCTheatreActorEditorDataManager.Inst.SetTextColor(ColorUtility.ToHtmlStringRGB(m_color));
                break;
            case 4:
                btn_change_bg.transform.Find("btn_color/img_color").gameObject.GetComponent<Image>().color = m_color;
                OCTheatreActorEditorDataManager.Inst.SetBgColor(ColorUtility.ToHtmlStringRGB(m_color));
                break;
            default:
                
                break;
        }
    }

    private void OnDestroy()
    {
        MessageHelper.RemoveListener<object[]>(MessageName.OnSliderValueChanged,OnSliderValueChanged);
        MessageHelper.RemoveListener<OCTheatreAvatarInfo>(MessageName.ActorCardInfoUpdate,ActorCardInfoUpdate);
    }
}
