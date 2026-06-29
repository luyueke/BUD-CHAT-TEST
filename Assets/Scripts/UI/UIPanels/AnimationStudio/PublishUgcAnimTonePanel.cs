using System;
using System.Collections;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.Util.IO;
using GameData.Base;
using GameData.BaseInfo;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UI;
using UI.Base;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class PublishUgcAnimTonePanel : BasePanel<PublishUgcAnimTonePanel>
{
    public Transform BG;
    public CButton backBtn;
    public CButton nextBtn;
    public CButton nameEditBtn;
    public SuperTextMesh nameText;
    public GameObject emptyNameObj;
    public Text nameLimitText;
    private KeyBoardInfo nameKeyBoardInfo;
    protected virtual int NameLimitCount => 5;
    public CButton descriptionEditBtn;
    public SuperTextMesh descriptionText;
    public GameObject emptyDescriptionObj;
    public Text descLimitText;
    private KeyBoardInfo descriptionKeyBoardInfo;
    protected virtual int DescLimitCount => 250;
    public Toggle priceToggle10;
    public Toggle priceToggle15;
    public Toggle priceToggle20;
    public Toggle priceToggle25;
    public Toggle priceToggle30;
    public CButton priceEditBtn;
    public Text selfPriceText;
    private Dictionary<int, Toggle> priceToggleDic;
    private KeyBoardInfo priceKeyBoardInfo;

    public CButton Btn_AddTone;
    public CButton Btn_PreviewTone;
    public CButton Btn_StopPreview;
    public CButton Btn_ReUpload;

    private bool _isPlaying;
    private AnimMusicInfo _curAnimMusicInfo = new AnimMusicInfo();
    private int Min_Price = 10;
    private int Price_Delta = 10;
    public override void OnCreate()
    {
        base.OnCreate();
        InitBG();
        AddListener();
        InitNameEdit();
        InitPriceToggle();
        SyncEditData();
    }

    private void InitBG()
    {
        if (BG == null)
        {
            return;
        }

        string atlasPath = "Assets/Loadable/UI/UIPanel/CommonBgPanel/CommonBgIcon.spriteatlas";
        var itemObj = Loader
            .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
            .Instantiate(BG);
        var item = itemObj.GetComponent<ActivityCenterBgItem>();
        item.InitCustomBgItem("#FFFFFF", atlasPath, new List<string>()
        {
            "animStudio_icon1", "animStudio_icon2", "animStudio_icon3"
        });
        item.gameObject.SetActive(true);
    }

    private void SetNextEnabled(bool value)
    {
        nextBtn.SetClickAble(value);
    }

    private void AddListener()
    {
        nextBtn.onClick.AddListener(PublishTone);
        backBtn.onClick.AddListener(DoClose);
        Btn_AddTone.onClick.AddListener(OnBtnUploadClick);
        Btn_PreviewTone.onClick.AddListener(PreviewTone);
        Btn_StopPreview.onClick.AddListener(StopPreviewTone);
        Btn_ReUpload.onClick.AddListener(OnBtnReuploadClick);
    }

    private void InitNameEdit()
    {
        nameEditBtn.onClick.AddListener(OnNameEditBtnClick);
        descriptionEditBtn.onClick.AddListener(OnDescriptionEditBtnClick);

        nameKeyBoardInfo = new KeyBoardInfo
        {
            type = 0,
            placeHolder = "",
            inputMode = 0,
            maxLength = 5,
            inputFlag = 0,
            textSecurity = 1,
            lengthTips = "字数超出限制",
            returnKeyType = (int)ReturnType.Return
        };

        descriptionKeyBoardInfo = new KeyBoardInfo
        {
            type = 0,
            placeHolder = "",
            inputMode = 0,
            maxLength = 250,
            inputFlag = 0,
            textSecurity = 1,
            lengthTips = "字数超出限制",
            returnKeyType = (int)ReturnType.Return
        };
    }

    private void OnNameEditBtnClick()
    {
        nameKeyBoardInfo.defaultText = _curAnimMusicInfo.name;
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, OnGetNameFormNative);
        MobileInterface.Instance.ShowKeyboard(JsonConvert.SerializeObject(nameKeyBoardInfo));
    }

    private void OnGetNameFormNative(string value)
    {
        MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);
        _curAnimMusicInfo.name = value;
        SyncEditData();
    }

    private void OnDescriptionEditBtnClick()
    {
        descriptionKeyBoardInfo.defaultText = _curAnimMusicInfo.desc;
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, OnGetDescFromNative);
        MobileInterface.Instance.ShowKeyboard(JsonConvert.SerializeObject(descriptionKeyBoardInfo));
    }

    private void OnGetDescFromNative(string desc)
    {
        MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);
        _curAnimMusicInfo.desc = desc;
        SyncEditData();
    }

    private void SyncEditData()
    {
        if (string.IsNullOrEmpty(_curAnimMusicInfo.name))
        {
            emptyNameObj.SetActive(true);
            nameText.gameObject.SetActive(false);
            nameLimitText.text = $"0/{NameLimitCount}";
        }
        else
        {
            nameText.text = _curAnimMusicInfo.name;
            emptyNameObj.SetActive(false);
            nameText.gameObject.SetActive(true);
            nameLimitText.text = $"{_curAnimMusicInfo.name.Length}/{NameLimitCount}";
        }

        if (string.IsNullOrEmpty(_curAnimMusicInfo.desc))
        {
            emptyDescriptionObj.SetActive(true);
            descriptionText.gameObject.SetActive(false);
            descLimitText.text = $"0/{DescLimitCount}";
        }
        else
        {
            descriptionText.text = _curAnimMusicInfo.desc;
            emptyDescriptionObj.SetActive(false);
            descriptionText.gameObject.SetActive(true);
            descLimitText.text = $"{_curAnimMusicInfo.desc.Length}/{DescLimitCount}";
        }

        ((LoadingButton)nextBtn).HideLoading();

        var paymentInfo = _curAnimMusicInfo.paymentInfo;
        if (paymentInfo == null || paymentInfo.price == 0)
        {
            priceToggle10.SetIsOnWithoutNotify(true);
            OnPriceToggleClick(10);
            priceEditBtn.transform.Find("PriceCheckmark").gameObject.SetActive(false);
            priceEditBtn.transform.Find("PriceNumEdit").gameObject.SetActive(true);
            priceEditBtn.transform.Find("HasPrizeGroup").gameObject.SetActive(false);
            selfPriceText.text = "";
        }
        else
        {
            bool isCustom = false;
            Toggle findToggle;
            isCustom = !priceToggleDic.TryGetValue(_curAnimMusicInfo.paymentInfo.price, out var toggle);
            findToggle = toggle;

            if (isCustom)
            {
                priceToggle10.group.SetAllTogglesOff(false);
                priceEditBtn.transform.Find("PriceCheckmark").gameObject.SetActive(true);
                priceEditBtn.transform.Find("PriceNumEdit").gameObject.SetActive(false);
                priceEditBtn.transform.Find("HasPrizeGroup").gameObject.SetActive(true);
                priceEditBtn.transform.Find("HasPrizeGroup/Label").GetComponent<CText>().text =
                    paymentInfo.price.ToString();
                priceEditBtn.transform.Find("HasPrizeGroup/Image").GetComponent<Image>().sprite =
                    PgcUtils.LoadCurrencyIcon(CurrencyType.PinkCoin, gameObject);
            }
            else
            {
                findToggle.SetIsOnWithoutNotify(true);
                priceEditBtn.transform.Find("PriceCheckmark").gameObject.SetActive(false);
                priceEditBtn.transform.Find("PriceNumEdit").gameObject.SetActive(true);
                priceEditBtn.transform.Find("HasPrizeGroup").gameObject.SetActive(false);
            }

            selfPriceText.text = $"每个商品你会获得等值的{paymentInfo.price / 2.0}个";
        }

        CheckNextEnable();
    }

    protected virtual void CheckNextEnable()
    {
        bool isEnable = !string.IsNullOrEmpty(_curAnimMusicInfo.name);
        isEnable &= !string.IsNullOrEmpty(_curAnimMusicInfo.desc);
        isEnable &= !string.IsNullOrEmpty(_curAnimMusicInfo.metaDataUrl);

        SetNextEnabled(isEnable);
    }

    private void InitPriceToggle()
    {
        priceEditBtn.onClick.AddListener(OnPriceEditBtnClick);
        priceToggle10.onValueChanged.AddListener((isOn) =>
        {
            if (isOn)
            {
                OnPriceToggleClick(10);
            }
        });
        priceToggle15.onValueChanged.AddListener((isOn) =>
        {
            if (isOn)
            {
                OnPriceToggleClick(15);
            }
        });
        priceToggle20.onValueChanged.AddListener((isOn) =>
        {
            if (isOn)
            {
                OnPriceToggleClick(20);
            }
        });
        priceToggle25.onValueChanged.AddListener((isOn) =>
        {
            if (isOn)
            {
                OnPriceToggleClick(25);
            }
        });
        priceToggle30.onValueChanged.AddListener((isOn) =>
        {
            if (isOn)
            {
                OnPriceToggleClick(30);
            }
        });
        priceToggleDic = new Dictionary<int, Toggle>()
        {
            { 10, priceToggle10 },
            { 15, priceToggle15 },
            { 20, priceToggle20 },
            { 25, priceToggle25 },
            { 30, priceToggle30 }
        };

        priceKeyBoardInfo = new KeyBoardInfo()
        {
            type = 0,
            placeHolder = LocalizationManager.Inst.GetLocalizedText("请输入自定义价格"),
            inputMode = 1,
            maxLength = 4,
            inputFlag = 0,
            textSecurity = 1,
            lengthTips = LocalizationManager.Inst.GetLocalizedText("请输入一个介于{0}和{1}之间的整数。", Min_Price, 9999),
            defaultText = "10",
            returnKeyType = (int)ReturnType.Return
        };
    }

    private void OnPriceEditBtnClick()
    {
        priceKeyBoardInfo.defaultText = "";
        priceKeyBoardInfo.placeHolder = LocalizationManager.Inst.GetLocalizedText("请输入自定义价格");
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, OnGetPriceFromNative);
        MobileInterface.Instance.ShowKeyboard(JsonConvert.SerializeObject(priceKeyBoardInfo));
    }

    private void OnGetPriceFromNative(string price)
    {
        MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);
        if (int.TryParse(price, out var value))
        {
            OnPriceToggleClick(value, true);
        }
    }


    private void OnPriceToggleClick(int value, bool isCustom = false)
    {
        if (isCustom && (value < Min_Price || value > 9999))
        {
            var lengthTips = LocalizationManager.Inst.GetLocalizedText("请输入一个介于{0}和{1}之间的整数。", Min_Price, 9999);
            TipPanel.ShowToast(lengthTips);
            return;
        }

        if (_curAnimMusicInfo.paymentInfo == null)
        {
            _curAnimMusicInfo.paymentInfo = new PaymentInfo()
            {
                price = value,
                currencyType = CurrencyType.PinkCoin
            };
        }
        else
        {
            _curAnimMusicInfo.paymentInfo.currencyType = CurrencyType.PinkCoin;
            _curAnimMusicInfo.paymentInfo.price = value;
        }

        SyncEditData();
    }

    private void PublishTone()
    {
        var req = new SetUgcAnimMusicReq
        {
            animMusicInfo = this._curAnimMusicInfo,
            setType = (int)SetType.Publish
        };

        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.setUgcAnimMusic, HttpMethod.POST,
            JsonConvert.SerializeObject(req), OnPublishSuccess, OnPublishFail);

        nextBtn.SetClickAble(false);
    }

    private void OnPublishSuccess(string msg)
    {
        CloseSelf();
        var UgcAnimChooseTonePanel = UIManager.Inst.FindPanel<UgcAnimChooseTonePanel>(PanelId.UgcAnimChooseTonePanel);
        if (UgcAnimChooseTonePanel != null)
        {
            UgcAnimChooseTonePanel.RefreshOwnedList();
        }
    }

    private void OnPublishFail(string error)
    {
        nextBtn.SetClickAble(true);
    }

    #region PreviewFuns
    private void RefreshPreviewUI()
    {
        if (string.IsNullOrEmpty(this._curAnimMusicInfo.metaDataUrl))
        {
            Btn_AddTone.gameObject.SetActive(true);
            Btn_PreviewTone.gameObject.SetActive(false);
            Btn_StopPreview.gameObject.SetActive(false);
            Btn_ReUpload.gameObject.SetActive(false);
        }
        else
        {
            if (_isPlaying)
            {
                Btn_AddTone.gameObject.SetActive(false);
                Btn_PreviewTone.gameObject.SetActive(false);
                Btn_StopPreview.gameObject.SetActive(true);
                Btn_ReUpload.gameObject.SetActive(true);
            }
            else
            {
                Btn_AddTone.gameObject.SetActive(false);
                Btn_PreviewTone.gameObject.SetActive(true);
                Btn_StopPreview.gameObject.SetActive(false);
                Btn_ReUpload.gameObject.SetActive(true);
            }
        }
    }

    private void DoClose()
    {
        StopPreviewTone();
        CloseSelf();
    }

    private void StopPreviewTone()
    {
        _isPlaying = false;
        UgcAnimToneManager.Inst.StopPreviewTone();
        RefreshPreviewUI();
    }

    private void OnBtnReuploadClick()
    {
        this._curAnimMusicInfo.metaDataUrl = "";
        this._curAnimMusicInfo.frameLen = 0;
        OnBtnUploadClick();
        StopPreviewTone();
    }

    private int _audioLimit = 20;

    private void OnBtnUploadClick()
    {
        bool isCancel = false;
        Action onCancel = () => { isCancel = true; };
        UIManager.Inst.OpenPanel<BgMusicUploadingPanel>(PanelId.BgMusicUploadingPanel, onCancel);
        AlbumUtils.Inst.UploadMusic(_audioLimit, (remoteUrl) =>
        {
            if (isCancel)
            {
                return;
            }

            if (!string.IsNullOrEmpty(remoteUrl))
            {
                OnUploadToneSuccess(remoteUrl);
            }

            UIManager.Inst.ClosePanel(PanelId.BgMusicUploadingPanel);
        }, err => { UIManager.Inst.ClosePanel(PanelId.BgMusicUploadingPanel); }, OnGetMusicLen);
    }

    private void OnGetMusicLen(float len)
    {
        this._curAnimMusicInfo.frameLen = (int)(len * 10);
    }

    private void OnUploadToneSuccess(string url)
    {
        this._curAnimMusicInfo.metaDataUrl = url;
        SyncEditData();
        RefreshPreviewUI();
    }

    private void PreviewTone()
    {
        if (string.IsNullOrEmpty(this._curAnimMusicInfo.metaDataUrl))
            return;
        
        UgcAnimToneManager.Inst.PreviewTone(_curAnimMusicInfo, () =>
        {
            StopPreviewTone();
        });
        _isPlaying = true;
        RefreshPreviewUI();
    }
    #endregion
}
