using Basic.Utils;
using Com.TheFallenGames.OSA.Util.IO;
using Game.COSXML;
using GameData;
using GameData.Base;
using GameData.BaseInfo;
using Network;
using Network.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.IncubationCabin
{
    public class CabinPublishToneView : MonoBehaviour
    {
        public LoadingButton nextBtn;
        public CButton nameEditBtn;
        public SuperTextMesh nameText;
        public GameObject emptyNameObj;
        public Text nameLimitText;
        public CButton descriptionEditBtn;
        public SuperTextMesh descriptionText;
        public GameObject emptyDescriptionObj;
        public Text descLimitText;
        public Toggle priceToggle10;
        public Toggle priceToggle15;
        public Toggle priceToggle20;
        public Toggle priceToggle25;
        public Toggle priceToggle30;
        public CButton priceEditBtn;
        public Text priceSubText;
        public Text selfPriceText;
        public Button CreatBtn;
        [SerializeField] private Button AddCardBtn;
        [SerializeField] private RemoteImageBehaviour Icon;
        [SerializeField] private GameObject IconBg;
        [SerializeField] private GameObject Chinese;
        [SerializeField] private GameObject English;
        [SerializeField] private GameObject Japan;

        protected virtual int NameLimitCount => 5;
        protected virtual int DescLimitCount => 250;

        private KeyBoardInfo _nameKeyBoardInfo;
        private KeyBoardInfo _descKeyBoardInfo;
        private KeyBoardInfo _priceKeyBoardInfo;
        private Dictionary<int, Toggle> _priceToggleDic;

        /// <summary>是否当前为自定义价格模式（由 priceEditBtn 键盘输入触发）</summary>
        private bool _isCustomPrice = false;
        // 缓存 priceEditBtn 子节点引用，仅在 InitPriceToggle 中 Find 一次
        private GameObject _go_PriceCheckmark;
        private GameObject _go_PriceNumEdit;
        private GameObject _go_HasPrizeGroup;
        private CText _txt_HasPrizeGroupLabel;
        private Image _img_HasPrizeGroupCurrency;

        private CabinToneInfo _curCabinToneInfo = new CabinToneInfo();
        private const int MinPrice = 50;

        public Action onPublishSuccess;
        public Action onGoToCreatTone;

        public void Init()
        {
            nextBtn.onClick.AddListener(PublishTone);
            CreatBtn.onClick.AddListener(() => onGoToCreatTone?.Invoke());
            AddCardBtn.onClick.AddListener(OnAddCardBtnClick);
            IconBg.SetActive(false);
            InitNameEdit();
            InitPriceToggle();
            // 初始状态：名称和封面均为空，发布按钮默认禁用
            CheckNextEnable();
        }

        public void SetData(string metaDataUrl, List<ToneLanguageData> languageList)
        {
            _curCabinToneInfo = new CabinToneInfo();
            _curCabinToneInfo.metaDataUrl = metaDataUrl;
            _curCabinToneInfo.languageList = languageList;

            // 重置为默认价格档位，不进入自定义模式
            _isCustomPrice = false;
            priceToggle10.SetIsOnWithoutNotify(true);
            OnPriceToggleClick(50);

            InitLanguageItems();
            SyncEditData();
        }

        private void InitLanguageItems()
        {
            var _languageList = _curCabinToneInfo.languageList;
            bool hasChinese = _languageList != null && _languageList.Exists(l => l.type == 0);
            bool hasEnglish = _languageList != null && _languageList.Exists(l => l.type == 1);
            bool hasJapanese = _languageList != null && _languageList.Exists(l => l.type == 2);
            Chinese.SetActive(hasChinese);
            English.SetActive(hasEnglish);
            Japan.SetActive(hasJapanese);
        }

        private void OnAddCardBtnClick()
        {
            var albumParams = new OpenSystemAlbumParams
            {
                albumType = 1,
                isCrop = 1,
                cropAspectRatio = 1,
            };
            var paramsJson = JsonConvert.SerializeObject(albumParams);
            LoggerUtils.Log($"[CabinPublishToneView] OnAddCardBtnClick 点击，准备打开系统相册，params={paramsJson}");

            MobileInterface.Instance.AddClientFail(MobileInterfaceDefine.openSystemAlbum, OnCoverAlbumFail);
            MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.openSystemAlbum, OnCoverAlbumResult);
            MobileInterface.Instance.OpenSystemAlbum(paramsJson);
            LoggerUtils.Log("[CabinPublishToneView] 已调用原生 OpenSystemAlbum，等待原生裁剪回调...");
#if UNITY_EDITOR
            var filePath = Path.Combine(Application.streamingAssetsPath, "signIn_bg.png");
            var resData = new AlbumResData { localUrl = filePath };
            OnCoverAlbumResult(JsonConvert.SerializeObject(resData));
#endif
        }

        /// <summary>
        /// 原生相册/裁剪返回失败时的回调（isSuccess=0）。
        /// 对齐 AlbumProcess.OnNativeFail，清理两个回调并弹提示。
        /// </summary>
        private void OnCoverAlbumFail(string msg)
        {
            LoggerUtils.LogError($"[CabinPublishToneView] 原生相册/裁剪返回失败 msg={msg}");
            MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.openSystemAlbum);
            MobileInterface.Instance.DelClientFail(MobileInterfaceDefine.openSystemAlbum);
            TipPanel.ShowToast("导入图片失败，请再试一遍!");
        }

        private void OnCoverAlbumResult(string msg)
        {
            LoggerUtils.Log($"[CabinPublishToneView] OnCoverAlbumResult 收到原生回调 msg={msg}");

            MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.openSystemAlbum);
            MobileInterface.Instance.DelClientFail(MobileInterfaceDefine.openSystemAlbum);

            var albumRes = JsonConvert.DeserializeObject<AlbumResData>(msg);

            if (albumRes == null)
            {
                LoggerUtils.LogError("[CabinPublishToneView] OnCoverAlbumResult 反序列化失败，albumRes 为 null");
                return;
            }

            if (string.IsNullOrEmpty(albumRes.localUrl))
            {
                LoggerUtils.LogError($"[CabinPublishToneView] OnCoverAlbumResult localUrl 为空，mediaType={albumRes.mediaType}");
                return;
            }

            string filePath = albumRes.localUrl;
            bool fileExists = File.Exists(filePath);
            LoggerUtils.Log($"[CabinPublishToneView] OnCoverAlbumResult localUrl={filePath} fileExists={fileExists}");

            string uri = $"UgcToneCover/{AccountDataManager.Inst.Uid}/{Path.GetFileName(filePath)}";
            CosXmlUploadManager.UploadFile(uri, filePath, (url, err) =>
            {
                LoggerUtils.Log($"[CabinPublishToneView] UploadFile 回调 url={url} err={err}");

                if (!string.IsNullOrEmpty(err))
                {
                    TipPanel.ShowToast("导入图片失败，请再试一遍!");
                    return;
                }

                var req = new Dictionary<string, string> { { "url", url } };
                NetworkManager.Inst.SendHttpRequest<AuditImageData>(HttpUrlDefine.AuditImage,
                    HttpMethod.POST, req,
                    rsp =>
                    {
                        LoggerUtils.Log($"[CabinPublishToneView] AuditImage 回调 auditResult={rsp?.auditResult}");

                        if (rsp != null && rsp.auditResult == (int)AuditResult.Passed)
                        {
                            _curCabinToneInfo.cover = url;
                            IconBg.SetActive(true);
                            Icon.Load(url);
                            // 封面上传审核通过后刷新发布按钮可用状态
                            CheckNextEnable();
                        }
                        else
                        {
                            TipPanel.ShowToast("图片审核未通过，请重新上传!");
                        }
                    }, null);
            });
        }

        private void InitNameEdit()
        {
            nameEditBtn.onClick.AddListener(OnNameEditBtnClick);
            descriptionEditBtn.onClick.AddListener(OnDescriptionEditBtnClick);

            _nameKeyBoardInfo = new KeyBoardInfo
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

            _descKeyBoardInfo = new KeyBoardInfo
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
            _nameKeyBoardInfo.defaultText = _curCabinToneInfo.name;
            MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, OnGetNameFromNative);
            MobileInterface.Instance.ShowKeyboard(JsonConvert.SerializeObject(_nameKeyBoardInfo));
        }

        private void OnGetNameFromNative(string value)
        {
            MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);
            _curCabinToneInfo.name = value;
            SyncEditData();
        }

        private void OnDescriptionEditBtnClick()
        {
            _descKeyBoardInfo.defaultText = _curCabinToneInfo.desc;
            MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, OnGetDescFromNative);
            MobileInterface.Instance.ShowKeyboard(JsonConvert.SerializeObject(_descKeyBoardInfo));
        }

        private void OnGetDescFromNative(string desc)
        {
            MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);
            _curCabinToneInfo.desc = desc;
            SyncEditData();
        }

        /// <summary>
        /// 同步名称和描述的 UI 显示，并刷新发布按钮可用状态。
        /// 不处理价格相关 UI（由 RefreshCustomPriceUI 负责）。
        /// </summary>
        private void SyncEditData()
        {
            if (string.IsNullOrEmpty(_curCabinToneInfo.name))
            {
                emptyNameObj.SetActive(true);
                nameText.gameObject.SetActive(false);
                nameLimitText.text = $"0/{NameLimitCount}";
            }
            else
            {
                nameText.text = _curCabinToneInfo.name;
                emptyNameObj.SetActive(false);
                nameText.gameObject.SetActive(true);
                nameLimitText.text = $"{_curCabinToneInfo.name.Length}/{NameLimitCount}";
            }

            if (string.IsNullOrEmpty(_curCabinToneInfo.desc))
            {
                emptyDescriptionObj.SetActive(true);
                descriptionText.gameObject.SetActive(false);
                descLimitText.text = $"0/{DescLimitCount}";
            }
            else
            {
                descriptionText.text = _curCabinToneInfo.desc;
                emptyDescriptionObj.SetActive(false);
                descriptionText.gameObject.SetActive(true);
                descLimitText.text = $"{_curCabinToneInfo.desc.Length}/{DescLimitCount}";
            }

            nextBtn.HideLoading();
            CheckNextEnable();
        }

        private void CheckNextEnable()
        {
            // 音色名称和封面均为必填项，两者均填写后发布按钮才可点击
            bool isEnable = !string.IsNullOrEmpty(_curCabinToneInfo.name)
                            && !string.IsNullOrEmpty(_curCabinToneInfo.cover);
            nextBtn.SetClickAble(isEnable);
        }

        /// <summary>
        /// 初始化价格相关 Toggle 和自定义价格按钮。
        /// 在此处缓存 priceEditBtn 子节点，避免后续频繁 Find。
        /// </summary>
        private void InitPriceToggle()
        {
            // 缓存 priceEditBtn 子节点，仅 Find 一次
            _go_PriceCheckmark = priceEditBtn.transform.Find("PriceCheckmark").gameObject;
            _go_PriceNumEdit = priceEditBtn.transform.Find("PriceNumEdit").gameObject;
            _go_HasPrizeGroup = priceEditBtn.transform.Find("HasPrizeGroup").gameObject;
            _txt_HasPrizeGroupLabel = priceEditBtn.transform.Find("HasPrizeGroup/Label").GetComponent<CText>();
            _img_HasPrizeGroupCurrency = priceEditBtn.transform.Find("HasPrizeGroup/Image").GetComponent<Image>();

            priceEditBtn.onClick.AddListener(OnPriceEditBtnClick);
            priceToggle10.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    OnPriceToggleClick(50);
                }
            });
            priceToggle15.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    OnPriceToggleClick(60);
                }
            });
            priceToggle20.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    OnPriceToggleClick(70);
                }
            });
            priceToggle25.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    OnPriceToggleClick(80);
                }
            });
            priceToggle30.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    OnPriceToggleClick(90);
                }
            });

            _priceKeyBoardInfo = new KeyBoardInfo()
            {
                type = 0,
                placeHolder = LocalizationManager.Inst.GetLocalizedText("请输入自定义价格"),
                inputMode = 1,
                maxLength = 4,
                inputFlag = 0,
                textSecurity = 1,
                lengthTips = LocalizationManager.Inst.GetLocalizedText("请输入一个介于{0}和{1}之间的整数。", MinPrice, 9999),
                defaultText = "50",
                returnKeyType = (int)ReturnType.Return
            };
        }

        private void OnPriceEditBtnClick()
        {
            _priceKeyBoardInfo.defaultText = "";
            _priceKeyBoardInfo.placeHolder = LocalizationManager.Inst.GetLocalizedText("请输入自定义价格");
            MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, OnGetPriceFromNative);
            MobileInterface.Instance.ShowKeyboard(JsonConvert.SerializeObject(_priceKeyBoardInfo));
        }

        private void OnGetPriceFromNative(string price)
        {
            MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);

            if (int.TryParse(price, out var value))
            {
                // isCustom = true 表示来自自定义输入，需要关闭所有 Toggle 并显示 HasPrizeGroup
                OnPriceToggleClick(value, true);
            }
        }

        /// <summary>
        /// 更新价格数据和 priceSubText。
        /// Toggle 点击时 isCustom = false，仅更新数据；
        /// 自定义价格输入时 isCustom = true，额外关闭所有 Toggle 并显示 HasPrizeGroup。
        /// </summary>
        /// <param name="value">选中的价格值</param>
        /// <param name="isCustom">是否来自 priceEditBtn 的自定义输入</param>
        private void OnPriceToggleClick(int value, bool isCustom = false)
        {
            if (isCustom && (value < MinPrice || value > 9999))
            {
                TipPanel.ShowToast(LocalizationManager.Inst.GetLocalizedText("请输入一个介于{0}和{1}之间的整数。", MinPrice, 9999));
                return;
            }

            if (_curCabinToneInfo.paymentInfo == null)
            {
                _curCabinToneInfo.paymentInfo = new PaymentInfo()
                {
                    price = value,
                    currencyType = CurrencyType.PinkCoin
                };
            }
            else
            {
                _curCabinToneInfo.paymentInfo.currencyType = CurrencyType.PinkCoin;
                _curCabinToneInfo.paymentInfo.price = value;
            }

            _isCustomPrice = isCustom;
            priceSubText.text = $"每个商品你会获得等值的{(int)(value * 0.5f)}个";

            if (isCustom)
            {
                // 仅自定义价格输入时才关闭所有 Toggle 的选中状态
                priceToggle10.group.SetAllTogglesOff(false);
            }

            RefreshCustomPriceUI();
        }

        /// <summary>
        /// 刷新 priceEditBtn 的自定义价格显示区域。
        /// _isCustomPrice 为 true 时显示 HasPrizeGroup（含价格数值和图标），否则显示默认态。
        /// </summary>
        private void RefreshCustomPriceUI()
        {
            _go_PriceCheckmark.SetActive(_isCustomPrice);
            _go_PriceNumEdit.SetActive(!_isCustomPrice);
            _go_HasPrizeGroup.SetActive(_isCustomPrice);

            if (_isCustomPrice && _curCabinToneInfo.paymentInfo != null)
            {
                _txt_HasPrizeGroupLabel.text = _curCabinToneInfo.paymentInfo.price.ToString();
                _img_HasPrizeGroupCurrency.sprite = PgcUtils.LoadCurrencyIcon(CurrencyType.PinkCoin, gameObject);
            }
        }

        private void PublishTone()
        {
            if (string.IsNullOrEmpty(_curCabinToneInfo.cover))
            {
                TipPanel.ShowToast("请上传封面图");
                return;
            }

            if (string.IsNullOrEmpty(_curCabinToneInfo.name))
            {
                TipPanel.ShowToast("请输入音色名称");
                return;
            }

            CabinToneNetManager.Inst.SetCabinToneInfo((int)SetType.Publish, _curCabinToneInfo, isSuccess =>
            {
                if (isSuccess)
                {
                    onPublishSuccess?.Invoke();
                }
                else
                {
                    nextBtn.SetClickAble(true);
                }
            });
            nextBtn.SetClickAble(false);
        }
    }
}
