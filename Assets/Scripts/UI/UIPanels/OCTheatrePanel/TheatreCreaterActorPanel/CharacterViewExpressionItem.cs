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
using UI.BaseWidgets;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CharacterViewExpressionItem : MonoBehaviour
{
    public OCTAvatarExpression expressionData;
    public System.Action onIconChanged; // 图标保存完成后的一次性回调
    private RemoteImageBehaviour Rm_Cover;
    //public RawImage rawImage;
    private Text txt_name;
    private CButton btn_icon;
    private Transform btns;
    private CButton cbtn_replace;
    private CButton cbtn_rename;
    private CButton cbtn_del;
    private CButton btnsMask;
    private Transform img_add;
    private bool _btnsJustShown;
    public GameObject _loadingObj;

    private void Awake()
    {
        Init();
    }

    private void Update()
    {
        if (!btns.gameObject.activeSelf) return;
        if (_btnsJustShown) { _btnsJustShown = false; return; }
        if (!Input.GetMouseButtonDown(0)) return;
        var pointerData = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);
        if (results.Exists(r => r.gameObject.transform.IsChildOf(btns)))
            return;
        btns.gameObject.SetActive(false);
    }
    private void Init()
    {
        btns = GameObjectEx.FindComponentByName<Transform>(transform, "btns");
        Rm_Cover = GameObjectEx.FindComponentByName<RemoteImageBehaviour>(transform,"icon");
        //rawImage = GameObjectEx.FindComponentByName<RawImage>(transform,"icon");
        btn_icon = GameObjectEx.FindComponentByName<CButton>(transform,"icon");
        txt_name = GameObjectEx.FindComponentByName<Text>(transform, "txt_name");
        cbtn_replace = GameObjectEx.FindComponentByName<CButton>(transform, "cbtn_replace");
        cbtn_rename = GameObjectEx.FindComponentByName<CButton>(transform, "cbtn_rename");
        cbtn_del = GameObjectEx.FindComponentByName<CButton>(transform, "cbtn_del");
        btns.gameObject.SetActive(false);
        btnsMask = GameObjectEx.FindComponentByName<CButton>(transform, "btnsMask");
        btnsMask.gameObject.SetActive(false);
        img_add = GameObjectEx.FindComponentByName<Transform>(transform, "img_add");
        if (_loadingObj != null) _loadingObj.SetActive(false);

    }
    private void ShowLoading()
    {
        if (_loadingObj != null) _loadingObj.SetActive(true);
    }

    public void HideLoading()
    {
        if (_loadingObj != null) _loadingObj.SetActive(false);
    }

    public void ChangeName(string str)
    {
        txt_name.text = str;
    }
    public void ChangeIcon(string url)
    {
      
        OCTheatreActorEditorDataManager.Inst.SetExpressionURL(expressionData, url);

        Rm_Cover.ResetRawImage();
        img_add.gameObject.SetActive(false);
        Rm_Cover.gameObject.SetActive(true);
        HideLoading();
        ShowLoading();
        Rm_Cover.Load(url, true, (_, _) => HideLoading(), HideLoading);

        var callback = onIconChanged;
        onIconChanged = null;
        callback?.Invoke();
    }
    public void InitData(OCTAvatarExpression expression)
    {
        expressionData = expression;
        Init();
        txt_name.text = expression.expressionName;     
        transform.GetComponent<Button>().onClick.RemoveAllListeners();   
        transform.GetComponent<Button>().onClick.AddListener((() =>
        { 
           ItemOnClick();
           
        }));
        if(string.IsNullOrEmpty(expression.expressionURL))
        {
            Rm_Cover.gameObject.SetActive(false);
            img_add.gameObject.SetActive(true);
        }
        else
        {
            Rm_Cover.gameObject.SetActive(true);
            img_add.gameObject.SetActive(false);
            Rm_Cover.Load(expression.expressionURL);
           
        }
        btnsMask.onClick.RemoveAllListeners();
        btn_icon.onClick.AddListener((() =>
        {
             btns.gameObject.SetActive(true);
             //btnsMask.gameObject.SetActive(true);
             _btnsJustShown = true;
             bool isCustom = expressionData.expressionType == (int)ExpressionType.custom;
             bool isNormal = expressionData.expressionType == (int)ExpressionType.normal;
             bool isExp = expressionData.expressionType == (int)ExpressionType.expression;
             
             cbtn_replace.gameObject.SetActive(isNormal || isCustom || isExp);
             cbtn_rename.gameObject.SetActive(isCustom);
             cbtn_del.gameObject.SetActive(isCustom || isExp);
        }
        ));
    btnsMask.onClick.RemoveAllListeners();
        btnsMask.onClick.AddListener((() =>
        {
            btns.gameObject.SetActive(false);
            btnsMask.gameObject.SetActive(false);
        }));
         cbtn_replace.onClick.RemoveAllListeners();
        cbtn_replace.onClick.AddListener((() =>
        {
            btns.gameObject.SetActive(false);
            ItemOnClick();
        }));
        cbtn_rename.onClick.AddListener((() =>
        {
             btns.gameObject.SetActive(false);
            UIManager.Inst.OpenPanel(PanelId.CommonSetNamePanel, new CommonSetNamePanelData()
            {
                title = "修改名字",
                inputTxt = expressionData.expressionName,
                btn_close_action = ((_) =>
                {
                    btnsMask.onClick.Invoke();
                    UIManager.Inst.ClosePanel(PanelId.CommonSetNamePanel);
                }),
                btn_ok_action = ((str) =>
                {
                    btnsMask.onClick.Invoke();
                    OCTheatreActorEditorDataManager.Inst.SetExpressionName(expressionData, str);
                    ChangeName(str);
                    UIManager.Inst.ClosePanel(PanelId.CommonSetNamePanel);
                })
            });
        }));
        cbtn_del.onClick.AddListener(() =>
        {
            CommonConfirmPanel commonConfirmPanel = UIManager.Inst.OpenPanel<CommonConfirmPanel>(PanelId.CommonConfirmPanel);
            commonConfirmPanel.SetLocalText("确认删除", "你确定要删除该草稿吗？\n 一旦删除就无法找回", "删除", "取消");
            commonConfirmPanel.SetOnClickAction(() =>
            {
                //自定义的要把整个item删除
                if(expressionData.expressionType == (int)ExpressionType.custom)
                {
                    MessageHelper.Broadcast<OCTAvatarExpression>(MessageName.ActorExpressionItemDeleted, expressionData);
                    OCTheatreActorEditorDataManager.Inst.RemoveOCTAvatarExpression(expressionData);
                    Destroy(gameObject);
                }
                else//非自定义的只清空图片和URL
                {
                    btnsMask.onClick.Invoke();
                    OCTheatreActorEditorDataManager.Inst.SetExpressionURL(expressionData, "");
                    Rm_Cover.ResetRawImage();
                    //rawImage.texture = null;
                    Rm_Cover.gameObject.SetActive(false);
                    img_add.gameObject.SetActive(true);
                    if (expressionData.expressionType == (int)ExpressionType.normal)
                        MessageHelper.Broadcast<Texture2D>(MessageName.ActorNormalIconUpdate, null);
                    
                }

            }, null);
        });

    }

    public void InitDisplayOnly(OCTAvatarExpression expression)
    {
        expressionData = expression;
        Init();
        UpdateDisplay(expression);
    }

    public void UpdateDisplay(OCTAvatarExpression expression)
    {
        txt_name.text = expression.expressionName;
        if (!string.IsNullOrEmpty(expression.expressionURL))
        {
            Rm_Cover.gameObject.SetActive(true);
            img_add.gameObject.SetActive(false);
            Rm_Cover.Load(expression.expressionURL);
        }
        else
        {
            Rm_Cover.gameObject.SetActive(false);
            img_add.gameObject.SetActive(true);
        }
    }
    

    public void ItemOnClick()
    {
        if(expressionData.mType == (int)OCTAvatarExpressionType.materials)
        {
            UIManager.Inst.OpenPanel<FittingRoomPanel>(PanelId.FittingRoomPanel);
            MessageHelper.Broadcast<CharacterViewExpressionItem>(MessageName.ActorCharacterOpenFittingRoomPanel, this);
        }
        else if(expressionData.mType == (int)OCTAvatarExpressionType.image)
        {
            ShowLoading();
            OnBtnUploadCoverClick();
        }
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
            MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.openSystemAlbum, OnNativeUrl);
            MobileInterface.Instance.OpenSystemAlbum(JsonConvert.SerializeObject(albumParams));
        }

        private void OnNativeUrl(string msg)
        {
            MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.openSystemAlbum);
            AlbumResData authData = JsonConvert.DeserializeObject<AlbumResData>(msg);
            if (authData == null || string.IsNullOrEmpty(authData.localUrl))
            {
                HideLoading();
                LoggerUtils.LogError("authData.localUrl is null ", authData?.localUrl);
                return;
            }
            LoadImgCallback(authData.localUrl);
        }

        private void LoadImgCallback(string filePath)
        {
            UIManager.Inst.OpenPanel(PanelId.TheatreActorCharacterEditorPanel,transform.GetComponent<CharacterViewExpressionItem>() , filePath);
        }

}
