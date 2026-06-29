using System;
using System.Collections.Generic;
using System.IO;
using Es;
using Game.COSXML;
using GameData;
using GameData.Base;
using GameData.BaseInfo;
using GameData.UGCData;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UGCAsset;
using UI.BaseWidgets;
using UI.Manager;
using UIAgent;
using UnityEngine;
using UnityEngine.UI;
using PaymentInfo = GameData.Base.PaymentInfo;
using Text = UnityEngine.UI.Text;

namespace UI
{
    public class TonePublishView : BaseDetailView
    {
        [SerializeField] private Toggle[] priceToggles;
        // [SerializeField] private Toggle priceToggle10;
        // [SerializeField] private Toggle priceToggle15;
        // [SerializeField] private Toggle priceToggle30;
        // [SerializeField] private Toggle priceToggle55;
        // [SerializeField] private Toggle priceToggle75;
        [SerializeField] private CButton priceEditBtn;
        [SerializeField] private SuperTextMesh selfPriceText;


        // private Dictionary<int, Toggle> priceToggleDic;

        private KeyBoardInfo priceKeyBoardInfo;

        private ToneEditData toneEditData;
        
        private int MinPrice = 10;
        private const int MaxPrice = 9999;
        private Dictionary<int, Toggle> priceToggleDic = new Dictionary<int, Toggle>();
        
        public override void Awake()
        {
            base.Awake();
            
#if PACKAGE_TYPE_US
            MinPrice = 30;
#endif

            priceEditBtn.onClick.AddListener(OnPriceEditBtnClick);
            // priceToggle10.onValueChanged.AddListener((isOn) =>
            // {
            //     if (isOn)
            //     {
            //         OnPriceToggleClick(30);
            //     }
            // });
            // priceToggle15.onValueChanged.AddListener((isOn) =>
            // {
            //     if (isOn)
            //     {
            //         OnPriceToggleClick(40);
            //     }
            // });
            // priceToggle30.onValueChanged.AddListener((isOn) =>
            // {
            //     if (isOn)
            //     {
            //         OnPriceToggleClick(50);
            //     }
            // });
            // priceToggle55.onValueChanged.AddListener((isOn) =>
            // {
            //     if (isOn)
            //     {
            //         OnPriceToggleClick(60);
            //     }
            // });
            // priceToggle75.onValueChanged.AddListener((isOn) =>
            // {
            //     if (isOn)
            //     {
            //         OnPriceToggleClick(70);
            //     }
            // });
            //
            // priceToggleDic = new Dictionary<int, Toggle>()
            // {
            //     { 30, priceToggle10 },
            //     { 40, priceToggle15 },
            //     { 50, priceToggle30 },
            //     { 60, priceToggle55 },
            //     { 70, priceToggle75 }
            // };

            string holderStr = LocalizationManager.Inst.GetLocalizedText("请输入整数");
            string lengthStr = LocalizationManager.Inst.GetLocalizedText("请输入一个介于{0}和{1}之间的整数。", MinPrice,MaxPrice);
            priceKeyBoardInfo = new KeyBoardInfo()
            {
                type = 0,
                placeHolder = holderStr,
                inputMode = 1,
                maxLength = 4,
                inputFlag = 0,
                textSecurity = 1,
                lengthTips = lengthStr,
                returnKeyType = (int)ReturnType.Return
            };

            // foreach (var key in priceToggleDic.Keys)
            // {
            //     Toggle toggle = priceToggleDic[key];
            //     CText priceText = GameObjectEx.FindChildByName(toggle.transform, "Label").GetComponent<CText>();
            //     priceText.text = key + "";
            // }
        }

        private void OnPriceEditBtnClick()
        {
            priceKeyBoardInfo.defaultText = "";
            priceKeyBoardInfo.placeHolder = LocalizationManager.Inst.GetLocalizedText("请输入自定义价格");
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
                            toneEditData.GetToneInfo().cover = url;
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
            if (string.IsNullOrEmpty(toneEditData.GetToneInfo().cover))
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
            if (editData is not ToneEditData toneEditData)
            {
                return;
            }

            if (string.IsNullOrEmpty(toneEditData.GetToneInfo().cover))
            {
                CheckNextEnable();
                return;
            }

            ((LoadingButton)nextBtn).ShowLoading();
            var curToneInfo = toneEditData.GetToneInfo();
            // EditToneInfoReq req = new EditToneInfoReq()
            // {
            //     musicToneInfo = curToneInfo,
            //     setType = (int)SetType.Publish
            // };
            var req = new UGCSetRequest(curToneInfo, UGCOperationType.Publish);


            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.SetUGCTone, HttpMethod.POST,
                JsonConvert.SerializeObject(req),
                success =>
                {
                    base.OnNextBtnClick();
                    MessageHelper.Broadcast<UgcBaseInfo>(MessageName.UgcToneDidPublishedNew, curToneInfo);
                }, (failResp =>
                {
                    HttpResponseRawData rsp= JsonConvert.DeserializeObject<HttpResponseRawData>(failResp);
                    if (rsp == null)
                    {
                        return;
                    }
                    if (rsp.result == 501) {
                        // 审核失败
                        Action<bool> onAppealCallBack = (isAppeal) => {
                            // 提交申诉，视作发布成功
                            if (isAppeal) {
                                if (curToneInfo.auditInfo == null)
                                {
                                    curToneInfo.auditInfo = new AuditStatus()
                                    {
                                        auditResult = 4
                                    };
                                }
                                else
                                {
                                    curToneInfo.auditInfo.auditResult = 4;
                                }
                                base.OnNextBtnClick();
                                MessageHelper.Broadcast<UgcBaseInfo>(MessageName.UgcToneDidPublishedNew, curToneInfo);
                            } else {
                                // 放弃申诉，视作发布失败
                                ((LoadingButton)nextBtn).HideLoading();
                                TipPanel.ShowToast($"{rsp.rmsg}");
                            }
                        };
                        UIAgentManager.Inst.OpenPanel(PanelId.UGCAuditRejectedPanel, WindowId.None, req, rsp.rmsg, onAppealCallBack);
                    } else {
                        HttpErrorCodeHandler errorCodeHandler = new Network.Http.HttpErrorCodeHandler();
                        errorCodeHandler.HandleErrorCodeResult(rsp);
                        LoggerUtils.LogError($"发布失败 [{curToneInfo.id}]:" + rsp.rmsg);
                        ((LoadingButton)nextBtn).HideLoading();
                        TipPanel.ShowToast($"{rsp.rmsg}");
                    }
                }));
        }

        private void OnPriceToggleClick(int value, bool isCustom = false)
        {
            if (isCustom && (value < MinPrice || value > MaxPrice))
            {
                string lengthStr = LocalizationManager.Inst.GetLocalizedText("请输入一个介于{0}和{1}之间的整数。", MinPrice,MaxPrice);
                TipPanel.ShowToast(lengthStr);
                return;
            }

            if (toneEditData.GetToneInfo().paymentInfo == null)
            {
                toneEditData.GetToneInfo().paymentInfo = new PaymentInfo()
                {
                    price = value,
                    currencyType = toneEditData.currencyType
                };
            }
            else
            {
                toneEditData.GetToneInfo().paymentInfo.price = value;
                toneEditData.GetToneInfo().paymentInfo.currencyType = toneEditData.currencyType;
            }

            SyncEditData();
        }
        
        protected override void SetUgcShaderStyle(UgcShaderStyle ugcStyle)
        {
            base.SetUgcShaderStyle(ugcStyle);
            string freeText = LocalizationManager.Inst.GetLocalizedText("免费");
            var priceData = DataTables.GetOtherPublishPrice((int)PublishResType.Tone);
            priceToggleDic.Clear();
            MinPrice = priceData.normalLowPrice;
#if PACKAGE_TYPE_US
            MinPrice = priceData.globalLowPrice;
#endif
            for (var i = 0; i < priceToggles.Length; i++)
            {
                var priceText = priceToggles[i].transform.Find("Label").GetComponent<Text>();
                int price = priceData.prices[i];
#if PACKAGE_TYPE_US
                price = priceData.globalPrices[i];
#endif
                priceText.text = price == 0 ? freeText : price.ToString();
                priceToggleDic.Add(price, priceToggles[i]);
                priceToggles[i].onValueChanged.AddListener(isOn =>
                {
                    if (isOn)
                    {
                        OnPriceToggleClick(price);
                    }
                });
            }
        }

        protected override void SyncEditData()
        {
            toneEditData = editData as ToneEditData;
            base.SyncEditData();
            if (editData == null || editData.GetInfo() == null)
            {
                return;
            }

            ((LoadingButton)nextBtn).HideLoading();

            var paymentInfo = toneEditData.GetToneInfo().paymentInfo;
            if (paymentInfo == null || paymentInfo.price == 0)
            {
                priceToggles[0].SetIsOnWithoutNotify(true);
                OnPriceToggleClick(10);
                priceEditBtn.transform.Find("PriceCheckmark").gameObject.SetActive(false);
                priceEditBtn.transform.Find("PriceNumEdit").gameObject.SetActive(true);
                priceEditBtn.transform.Find("HasPrizeGroup").gameObject.SetActive(false);
                selfPriceText.SetLocalText("每个商品你会获得等值的{0}个{1}创作者币",5,"<q=icn_common_green_big>");
            }
            else
            {
                bool isCustom = false;
                Toggle findToggle;

                isCustom = !priceToggleDic.TryGetValue(toneEditData.GetToneInfo().paymentInfo.price,
                    out var toggle);
                findToggle = toggle;
                if (isCustom)
                {
                    priceToggles[0].group.SetAllTogglesOff(false);
                    priceEditBtn.transform.Find("PriceCheckmark").gameObject.SetActive(true);
                    priceEditBtn.transform.Find("PriceNumEdit").gameObject.SetActive(false);
                    priceEditBtn.transform.Find("HasPrizeGroup").gameObject.SetActive(true);
                    priceEditBtn.transform.Find("HasPrizeGroup/Label").GetComponent<CText>().text =
                        paymentInfo.price.ToString();
                    priceEditBtn.transform.Find("HasPrizeGroup/Image").GetComponent<Image>().sprite =
                        PgcUtils.LoadCurrencyIcon(toneEditData.currencyType, gameObject);
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

            CheckNextEnable();
        }


        protected override void SyncCover()
        {
            var coverUrl = toneEditData.GetToneInfo().cover;
            cover.Load(coverUrl);
        }

        protected override void CheckNextEnable()
        {
            bool isEnable = !string.IsNullOrEmpty(toneEditData.GetToneInfo().name);
            isEnable &= !string.IsNullOrEmpty(toneEditData.GetToneInfo().cover);
            SetNextEnabled(isEnable);
        }
    }
}