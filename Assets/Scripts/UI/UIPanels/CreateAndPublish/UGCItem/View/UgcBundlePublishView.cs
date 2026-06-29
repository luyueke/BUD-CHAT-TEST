using System.Collections.Generic;
using System.IO;
using Basic.Extensions;
using Com.TheFallenGames.OSA.Util.IO;
using Game.COSXML;
using GameData;
using GameData.Base;
using GameData.BaseInfo;
using GameData.Gashapon;
using GameData.MapData;
using GameData.UGCData;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UGCAsset.Draft;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;
using PaymentInfo = GameData.Base.PaymentInfo;

namespace UI
{
    public class UgcBundlePublishView : BaseDetailView
    {
        [SerializeField] private Toggle priceToggle100;
        [SerializeField] private Toggle priceToggle110;
        [SerializeField] private Toggle priceToggle120;
        [SerializeField] private Toggle priceToggle150;
        [SerializeField] private Toggle gem20;
        [SerializeField] private Toggle gem30;
        [SerializeField] private Toggle gem50;
        [SerializeField] private Toggle gem55;
        [SerializeField] private Toggle gem75;
        [SerializeField] private CButton priceEditBtn;
        [SerializeField] private SuperTextMesh selfPriceText;
        
        [SerializeField] private GameObject[] gemObjects;
        [SerializeField] private GameObject[] pinkCoinObjects;


        private Dictionary<int, Toggle> priceToggleDic;
        private Dictionary<int, Toggle> gemToggleDic;

        private KeyBoardInfo priceKeyBoardInfo;

        private UgcBundleEditData ugcBundleEditData;

        public override void Awake()
        {
            base.Awake();

            priceEditBtn.onClick.AddListener(OnPriceEditBtnClick);
            priceToggle100.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    OnPriceToggleClick(ugcBundleEditData.OriginPrice);
                }
            });
            priceToggle110.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    OnPriceToggleClick(Mathf.CeilToInt(ugcBundleEditData.OriginPrice * 1.1f));
                }
            });
            priceToggle120.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    OnPriceToggleClick(Mathf.CeilToInt(ugcBundleEditData.OriginPrice * 1.2f));
                }
            });
            priceToggle150.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    OnPriceToggleClick(Mathf.CeilToInt(ugcBundleEditData.OriginPrice * 1.5f));
                }
            });
            
            gem20.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    OnPriceToggleClick(20);
                }
            });
            gem30.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    OnPriceToggleClick(30);
                }
            });
            gem50.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    OnPriceToggleClick(50);
                }
            });
            gem55.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    OnPriceToggleClick(55);
                }
            });
            gem75.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    OnPriceToggleClick(75);
                }
            });
                       
            gemToggleDic = new Dictionary<int, Toggle>()
            {
                { 20, gem20 },
                { 30, gem30 },
                { 50, gem50 },
                { 55, gem55 },
                { 75, gem75 }
            };


            priceKeyBoardInfo = new KeyBoardInfo()
            {
                type = 0,
                placeHolder = LocalizationManager.Inst.GetLocalizedText("请输入自定义价格"),
                inputMode = 1,
                maxLength = 4,
                inputFlag = 0,
                textSecurity = 1,
                lengthTips = LocalizationManager.Inst.GetLocalizedText("请输入一个介于{0}和{1}之间的整数。", 10, 9999),
                defaultText = "",
                returnKeyType = (int)ReturnType.Return
            };
        }

        private void OnPriceEditBtnClick()
        {
            MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, OnGetPriceFromNative);
            MobileInterface.Instance.ShowKeyboard(JsonConvert.SerializeObject(priceKeyBoardInfo));
        }

        protected override void OnEditCoverBtnClick()
        {
            OpenSystemAlbumParams albumParams = new OpenSystemAlbumParams()
            {
                albumType = 1, //0竖屏 1横屏
                isCrop = 1, //裁剪
                cropAspectRatio = 1, //宽高比
            };
            MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.openSystemAlbum, OnNativeUrl);
            MobileInterface.Instance.OpenSystemAlbum(JsonConvert.SerializeObject(albumParams));
#if UNITY_EDITOR
            var filePath = Path.Combine(Application.streamingAssetsPath, "signIn_bg.png");
            AlbumResData resData = new AlbumResData();
            resData.localUrl = filePath;
            OnNativeUrl(JsonConvert.SerializeObject(resData));
#endif
        }
        private void OnNativeUrl(string msg)
        {
            MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.openSystemAlbum);
            AlbumResData authData = JsonConvert.DeserializeObject<AlbumResData>(msg);
            if (string.IsNullOrEmpty(authData.localUrl))
            {
                LoggerUtils.LogError("authData.localUrl is null ", authData.localUrl);
                return;
            }

            UploadImg(authData.localUrl);
        }
        private void UploadImg(string filePath)
        {
            var uri = $"AvatarPartTemplate/{AccountDataManager.Inst.Uid}/{Path.GetFileName(filePath)}";
            CosXmlUploadManager.UploadFile(uri, filePath, (url, err) => { UploadImgCallback(url, err, filePath); });
        }
        private void UploadImgCallback(string url, string err, string filePath)
        {
            if (this == null)
            {
                return;
            }
            if (!string.IsNullOrEmpty(err))
            {
                LoggerUtils.LogError($"Upload Image Fail!!! Err : {err}");
                TipPanel.ShowToast("导入图片失败， 请再试一遍!");
            }
            else
            {

                LoggerUtils.Log("Upload Image Success url: " + url);
                var req = new Dictionary<string, string>()
                {
                    {"url", url}
                };
                NetworkManager.Inst.SendHttpRequest<AuditImageData>(HttpUrlDefine.AuditImage,
                    HttpMethod.POST, req, rsp =>
                    {
                        if (rsp != null && rsp.auditResult == (int) AuditResult.Passed)
                        {
                            ugcBundleEditData.GetUgcBundleInfo().cover = url;
                            ugcBundleEditData.draftInfo.SetCover(url);
                            LoadPhoto();
                            CheckNextEnable();
                        }
                        else
                        {
                            TipPanel.ShowToast("图片审核未通过，请重新上传!");
                        }
                    }, null);
            }
        }
        public void LoadPhoto()
        {
            if (string.IsNullOrEmpty(ugcBundleEditData.GetUgcBundleInfo().cover))
            {
                return;
            }
            cover.Load(editData.GetInfo().cover);
        }
        private void OnGetPriceFromNative(string price)
        {
            MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);
            if (int.TryParse(price, out var value))
            {
                OnPriceToggleClick(value, true);
            }
        }


        protected override void OnNextBtnClick()
        {
            if (editData is not UgcBundleEditData ugcBundleEditData)
            {
                return;
            }
            if (string.IsNullOrEmpty(ugcBundleEditData.GetUgcBundleInfo().cover))
            {
                CheckNextEnable();
                return;
            }
            ((LoadingButton)nextBtn).ShowLoading();

            ugcBundleEditData.draftInfo.Upload((info, isSuccess) =>
            {
                if (gameObject == null)
                {
                    return;
                }
                if (isSuccess)
                {
                    ugcBundleEditData.draftInfo.PublishDraftToServer((info, newSuccess) =>
                    {
                        if (newSuccess)
                        {
                            base.OnNextBtnClick();
                            MessageHelper.Broadcast(MessageName.ClothPublishInEditSuccess);
                            
                            /// 申诉中不参加活动
                            bool isAppealing = info?.auditInfo?.auditResult == (int)AuditResult.Appealing;
                            if (!isAppealing)
                            {
                                ContestDataManager.Inst.JoinBundleContest(info.id, info.skinType == 1);
                            }
                        }
                        else
                        {
                            ((LoadingButton)nextBtn).HideLoading();
                        }
                    });
                }
                else
                {
                    ((LoadingButton)nextBtn).HideLoading();
                    TipPanel.ShowToast($"{ugcBundleEditData.draftInfo.GetUploadMessage()}");
                }
            });


        }

        private void OnPriceToggleClick(int value, bool isCustom = false)
        {
            if (isCustom && (value < ugcBundleEditData.OriginPrice || value > 9999))
            {
                if (value < ugcBundleEditData.OriginPrice)
                {
                    TipPanel.ShowToast("套装的价格不可低于单品价格的总和哦");
                }
                else if (value > 9999)
                {
                    var lengthTips = LocalizationManager.Inst.GetLocalizedText("请输入一个介于{0}和{1}之间的整数。", 100, 9999);
                    TipPanel.ShowToast(lengthTips);
                }
                return;
            }

            if (ugcBundleEditData.GetUgcBundleInfo().paymentInfo == null)
            {
                ugcBundleEditData.GetUgcBundleInfo().paymentInfo = new PaymentInfo()
                {
                    price = value,
                    currencyType = ugcBundleEditData.CurrencyType
                };
            }
            else
            {
                ugcBundleEditData.GetUgcBundleInfo().paymentInfo.price = value;
                ugcBundleEditData.GetUgcBundleInfo().paymentInfo.currencyType = ugcBundleEditData.CurrencyType;
            }

            SyncEditData();
        }

        protected override void SyncEditData()
        {
            ugcBundleEditData = editData as UgcBundleEditData;
            base.SyncEditData();
            if (editData == null || editData.GetInfo() == null)
            {
                return;
            }

            if (priceToggleDic == null)
            {
                priceToggle100.GetComponentInChildren<Text>().text = $"{ugcBundleEditData.OriginPrice}";
                priceToggle110.GetComponentInChildren<Text>().text = $"{Mathf.CeilToInt(ugcBundleEditData.OriginPrice * 1.1f)}";
                priceToggle120.GetComponentInChildren<Text>().text = $"{Mathf.CeilToInt(ugcBundleEditData.OriginPrice * 1.2f)}";
                priceToggle150.GetComponentInChildren<Text>().text = $"{Mathf.CeilToInt(ugcBundleEditData.OriginPrice * 1.5f)}";
                priceToggleDic = new Dictionary<int, Toggle>()
                {
                    { ugcBundleEditData.OriginPrice, priceToggle100 },
                    { Mathf.CeilToInt(ugcBundleEditData.OriginPrice * 1.1f), priceToggle110 },
                    { Mathf.CeilToInt(ugcBundleEditData.OriginPrice * 1.2f), priceToggle120 },
                    { Mathf.CeilToInt(ugcBundleEditData.OriginPrice * 1.5f), priceToggle150 },
                };

                priceKeyBoardInfo.defaultText = ugcBundleEditData.OriginPrice.ToString();
                priceKeyBoardInfo.placeHolder = $"最低价是 {ugcBundleEditData.OriginPrice}";
                priceKeyBoardInfo.lengthTips = $"请输入一个介于{ugcBundleEditData.OriginPrice}和9999之间的整数。";
            }

            ((LoadingButton)nextBtn).HideLoading();
            
            // SetCurrencyView(ugcBundleEditData.currencyType);
            var paymentInfo = ugcBundleEditData.GetUgcBundleInfo().paymentInfo;
            if (paymentInfo == null || paymentInfo.price == 0)
            {
                priceToggle100.SetIsOnWithoutNotify(true);
                OnPriceToggleClick(ugcBundleEditData.OriginPrice);
                priceEditBtn.transform.Find("PriceCheckmark").gameObject.SetActive(false);
                priceEditBtn.transform.Find("PriceNumEdit").gameObject.SetActive(true);
                priceEditBtn.transform.Find("HasPrizeGroup").gameObject.SetActive(false);
                selfPriceText.SetLocalText("每个商品你会获得等值的{0}个{1}创作者币",ugcBundleEditData.OriginPrice / 2,"<q=icn_common_green_big>");
            }
            else
            {
                // bool isCustom = !priceToggleDic.TryGetValue(ugcBundleEditData.GetUgcBundleInfo().paymentInfo.price, out var toggle);
                bool isCustom = false;
                Toggle findToggle;
                if (ugcBundleEditData.GetUgcBundleInfo().paymentInfo.currencyType == CurrencyType.Gem)
                {
                    isCustom = !gemToggleDic.TryGetValue(ugcBundleEditData.GetUgcBundleInfo().paymentInfo.price, out var toggle);
                    findToggle = toggle;
                }
                else
                {
                    isCustom = !priceToggleDic.TryGetValue(ugcBundleEditData.GetUgcBundleInfo().paymentInfo.price,
                        out var toggle);
                    findToggle = toggle;
                }
                if (isCustom)
                {
                    priceToggle100.group.SetAllTogglesOff(false);
                    priceEditBtn.transform.Find("PriceCheckmark").gameObject.SetActive(true);
                    priceEditBtn.transform.Find("PriceNumEdit").gameObject.SetActive(false);
                    priceEditBtn.transform.Find("HasPrizeGroup").gameObject.SetActive(true);
                    priceEditBtn.transform.Find("HasPrizeGroup/Label").GetComponent<CText>().text = paymentInfo.price.ToString();
                    priceEditBtn.transform.Find("HasPrizeGroup/Image").GetComponent<Image>().sprite = PgcUtils.LoadCurrencyIcon(ugcBundleEditData.currencyType, gameObject);

                }
                else
                {
                    findToggle.SetIsOnWithoutNotify(true);
                    priceEditBtn.transform.Find("PriceCheckmark").gameObject.SetActive(false);
                    priceEditBtn.transform.Find("PriceNumEdit").gameObject.SetActive(true);
                    priceEditBtn.transform.Find("HasPrizeGroup").gameObject.SetActive(false);
                }
                double price = paymentInfo.price / 2.0;
                selfPriceText.SetLocalText("每个商品你会获得等值的{0}个{1}创作者币",price,"<q=icn_common_green_big>");

            }
            // SetTipIconAndText(ugcBundleEditData.currencyType);
            CheckNextEnable();
        }
        
        private void SetCurrencyView(CurrencyType currencyType)
        {
            gem20.gameObject.SetActive(currencyType == CurrencyType.Gem);
            gem30.gameObject.SetActive(currencyType == CurrencyType.Gem);
            gem50.gameObject.SetActive(currencyType == CurrencyType.Gem);
            gem55.gameObject.SetActive(currencyType == CurrencyType.Gem);
            gem75.gameObject.SetActive(currencyType == CurrencyType.Gem);
            priceToggle100.gameObject.SetActive(currencyType == CurrencyType.PinkCoin);
            priceToggle110.gameObject.SetActive(currencyType == CurrencyType.PinkCoin);
            priceToggle120.gameObject.SetActive(currencyType == CurrencyType.PinkCoin);
            priceToggle150.gameObject.SetActive(currencyType == CurrencyType.PinkCoin);
        }
        
        private void SetTipIconAndText(CurrencyType currencyType)
        {
            if (currencyType != CurrencyType.Gem) return;
            for (int i = 0; i < gemObjects.Length; i++)
            {
                gemObjects[i].gameObject.SetActive(true);
            }

            for (int i = 0; i < pinkCoinObjects.Length; i++)
            {
                pinkCoinObjects[i].gameObject.SetActive(false);
            }
        }

        protected override void SyncCover()
        {
            var coverUrl = ugcBundleEditData.draftInfo.GetCoverUrl();
            cover.Load(coverUrl);
        }
        protected override void CheckNextEnable()
        {
            bool isEnable = !string.IsNullOrEmpty(ugcBundleEditData.GetUgcBundleInfo().name);
            isEnable &= !string.IsNullOrEmpty(ugcBundleEditData.GetUgcBundleInfo().cover);
            SetNextEnabled(isEnable);
        }
    }
}
