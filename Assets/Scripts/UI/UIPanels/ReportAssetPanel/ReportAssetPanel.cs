using System.Collections;
using System.Collections.Generic;
using System.IO;
using Game.COSXML;
using GameData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class ReportAssetPanel : BasePanel<ReportAssetPanel>
{
    public CButton Btn_Close;

    [Header("Sptep1")]
    public GameObject Go_Step_1;
    public ReportPanelItem _itemPrefab;
    public Transform _itemContent;
    public CButton Btn_NextStep1;

    [Header("Sptep2")]
    public GameObject Go_Step_2;
    public LoadingButton Btn_NextStep2;
    public CButton reasonEditBtn;
    public LoadingButton uploadImgBtn;
    public RawImage upLoadRImg;
    public SuperTextMesh reasonText;
    public GameObject emptyNameObj;
    public Text reasonLimitText;
    private KeyBoardInfo reasonKeyBoardInfo;
    private int reasonLimitCount => 250;

    private string UnableColor = "#D9D9D9";
    private string EnableColor = "#FFD336";
    private ErrReportType _curReportType;
    private List<ReportPanelItem> _items = new List<ReportPanelItem>();
    private Dictionary<ErrReportType, string> _Configs = new Dictionary<ErrReportType, string>
    {
        {ErrReportType.Porn, "涉黄、赌、毒"},
        {ErrReportType.Violent, "枪支、暴力、恐怖等信息"},
        {ErrReportType.Political, "涉政"},
       {ErrReportType.Terrorism, "言语辱骂"},
         {ErrReportType.Misinformation, "广告、虚假信息、诈骗"},
         {ErrReportType.Suicide, "炸图、炸麦"},
        {ErrReportType.Harassment, "刷屏、骚扰"},
        {ErrReportType.Plagiarize, "抄袭他人作品"},     
        //{ErrReportType.DrugsAndGuns, "毒品枪支"},
        //{ErrReportType.IPViolation, "侵犯IP"},
        {ErrReportType.DontWantToSee, "单纯不想看见"},  
        {ErrReportType.Others, "其他"},
    };
    private ErrReportReq _reportReq = new ErrReportReq();

    public override void OnCreate()
    {
        base.OnCreate();

        Go_Step_1.SetActive(true);
        Go_Step_2.SetActive(false);

        Btn_Close.onClick.AddListener(CloseSelf);
        Btn_NextStep1.onClick.AddListener(OnBtn_NextStep1Click);
        Btn_NextStep2.onClick.AddListener(OnBtn_NextStep2Click);
        reasonEditBtn.onClick.AddListener(OnReasonEditBtnClick);
        uploadImgBtn.onClick.AddListener(OnUploadImgBtnClick);

        reasonKeyBoardInfo = new KeyBoardInfo
        {
            type = 0,
            placeHolder = "",
            inputMode = 0,
            maxLength = 250,
            inputFlag = 0,
            textSecurity = 1,
            lengthTips = LocalizationManager.Inst.GetLocalizedText("字数超出限制"),
            returnKeyType = (int)ReturnType.Return
        };

        InitPanel();
        SetStep1BtnEnable(false);
        SetStep2BtnEnable(false);
    }

    /// <summary>
    /// 所需要的参数
    /// args[0] ErrReportReq
    /// </summary>
    /// <param name="args"></param>
    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        if (args[0] != null)
        {
            _reportReq = (ErrReportReq)args[0];
        }
    }

    private void InitPanel()
    {
        foreach (var config in _Configs)
        {
            var item = GameObject.Instantiate(_itemPrefab, _itemContent).GetComponent<ReportPanelItem>();
            item.InitData(config.Key, config.Value, OnItemSelect);
            _items.Add(item);
        }
    }

    private void OnItemSelect(ErrReportType type)
    {
        _items.ForEach(x=>x.Go_IsSelected.SetActive(false));
        this._curReportType = type;

        SetStep1BtnEnable(true);
    }

    private void SetStep1BtnEnable(bool isEnable)
    {
        Btn_NextStep1.GetComponent<Image>().color = DataUtil.DeSerializeColorCheckHash(isEnable ? EnableColor : UnableColor);
        Btn_NextStep1.SetClickAble(isEnable);
    }

    private void SetStep2BtnEnable(bool isEnable)
    {
        Btn_NextStep2.GetComponent<Image>().color = DataUtil.DeSerializeColorCheckHash(isEnable ? EnableColor : UnableColor);
        Btn_NextStep2.SetClickAble(isEnable);
    }

    private void OnBtn_NextStep1Click()
    {
        _reportReq.reportType = (int)this._curReportType;

        Go_Step_1.SetActive(false);
        Go_Step_2.SetActive(true);
    }

    private void OnBtn_NextStep2Click()
    {
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.Report, HttpMethod.POST, JsonConvert.SerializeObject(_reportReq),
            (content) =>
            {
                CloseSelf();
                TipPanel.ShowToast("举报成功");
            }, (error) =>
            {
                LoggerUtils.LogError(error);
            });
    }

    private void OnReasonEditBtnClick()
    {
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, OnGetReasonFormNative);
        MobileInterface.Instance.ShowKeyboard(JsonConvert.SerializeObject(reasonKeyBoardInfo));
    }

    private void OnGetReasonFormNative(string value) {
        MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);
        _reportReq.reason = value;
        SyncEditData();
    }

    private void SyncEditData() {
        if (string.IsNullOrEmpty(_reportReq.reason)) {
            emptyNameObj.SetActive(true);
            reasonText.gameObject.SetActive(false);
            reasonLimitText.text = $"0/{reasonLimitCount}";
        }
        else
        {
            reasonText.text = _reportReq.reason;
            emptyNameObj.SetActive(false);
            reasonText.gameObject.SetActive(true);
            reasonLimitText.text = $"{_reportReq.reason.Length}/{reasonLimitCount}";
        }

        CheckNextEnable();
    }

    private void CheckNextEnable() {
        bool isEnable = !string.IsNullOrEmpty(_reportReq.reason);
        SetStep2BtnEnable(isEnable);
    }

    private void OnUploadImgBtnClick()
    {
        OpenSystemAlbumParams albumParams = new OpenSystemAlbumParams()
        {
            albumType = 1, //0竖屏 1横屏
            isCrop = 0, //裁剪
            cropAspectRatio = 1, //宽高比
        };
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.openSystemAlbum, OnNativeUri);
        MobileInterface.Instance.OpenSystemAlbum(JsonConvert.SerializeObject(albumParams));
    }

    private void OnNativeUri(string msg)
    {
        uploadImgBtn.ShowLoading();
        MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.openSystemAlbum);
        AlbumResData authData = JsonConvert.DeserializeObject<AlbumResData>(msg);
        if (string.IsNullOrEmpty(authData.localUrl))
        {
            return;
        }

        UploadImg(authData.localUrl);
    }

    private void UploadImg(string filePath)
    {
        var uri = $"UserErrorReport/{AccountDataManager.Inst.Uid}/{Path.GetFileName(filePath)}";
        CosXmlUploadManager.UploadFile(uri, filePath, (url, err) =>
        {
            UploadImgCallback(url, err, filePath);
        });
    }

    private void UploadImgCallback(string url, string err, string filePath)
    {
        if (!string.IsNullOrEmpty(err))
        {
            LoggerUtils.LogError($"Upload Image Fail!!! Err : {err}");
        }
        else
        {
            LoggerUtils.Log("Upload Image Success url: " + url);
            _reportReq.imageUrl = url;

            if (File.Exists(filePath))
            {
                // 读取文件的字节数据
                byte[] fileData = File.ReadAllBytes(filePath);

                // 创建一个新的 Texture2D
                Texture2D texture = new Texture2D(2, 2);

                // 加载图片数据到 Texture2D
                if (texture.LoadImage(fileData))
                {
                    // 将 Texture2D 应用于 RawImage 组件
                    upLoadRImg.texture = texture;
                    uploadImgBtn.HideLoading();
                    uploadImgBtn.gameObject.SetActive(false);
                }
                else
                {
                    LoggerUtils.LogError("Failed to load image data.");
                }
            }
        }
    }
}
