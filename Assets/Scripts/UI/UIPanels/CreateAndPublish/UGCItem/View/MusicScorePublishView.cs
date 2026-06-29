using System.Collections.Generic;
using System.IO;
using Basic.Extensions;
using Com.TheFallenGames.OSA.Util.IO;
using Es;
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
    public class MusicScorePublishView :  BaseDetailView
    {
        [SerializeField] private Toggle[] priceToggles;
        [SerializeField] private CButton priceEditBtn;
        [SerializeField] private SuperTextMesh selfPriceText;
        
        [SerializeField] private GameObject[] gemObjects;
        [SerializeField] private GameObject[] pinkCoinObjects;

        private KeyBoardInfo priceKeyBoardInfo;

        private MusicScoreEditData musicScoreEditData;
        
        private int MinPrice = 10;
        private const int MaxPrice = 9999;
        private Dictionary<int, Toggle> priceToggleDic = new Dictionary<int, Toggle>();

        public override void Awake()
        {
            base.Awake();
            
#if PACKAGE_TYPE_US
            MinPrice = 20;
#endif
           
            priceEditBtn.onClick.AddListener(OnPriceEditBtnClick);

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
            if (this==null)
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
                            musicScoreEditData.GetMusicScoreInfo().cover = url;
                            musicScoreEditData.draftInfo.SetCover(url);
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
            if (string.IsNullOrEmpty(musicScoreEditData.GetMusicScoreInfo().cover))
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
            if (editData is not MusicScoreEditData musicScoreEditData) {
                return;
            }
            if (string.IsNullOrEmpty(musicScoreEditData.GetMusicScoreInfo().cover))
            {
                CheckNextEnable();
                return;
            }
            ((LoadingButton)nextBtn).ShowLoading();

            musicScoreEditData.draftInfo.Upload((info, isSuccess) => {
                if (gameObject == null) {
                    return;
                }
                if (isSuccess) {
                    musicScoreEditData.draftInfo.PublishDraftToServer((info, newSuccess) => {
                        if (newSuccess) 
                        {
                            base.OnNextBtnClick();
                            MessageHelper.Broadcast<UgcBaseInfo>(MessageName.UgcMusicScoreDidPublishedNew, info);
                        } else {
                            ((LoadingButton)nextBtn).HideLoading();
                        }
                    });
                } else {
                    ((LoadingButton)nextBtn).HideLoading();
                    TipPanel.ShowToast($"{musicScoreEditData.draftInfo.GetUploadMessage()}");
                }
            });


        }

        private void OnPriceToggleClick(int value, bool isCustom = false)
        {
            if (isCustom && (value < MinPrice || value > MaxPrice))
            {
                string tipsStr = LocalizationManager.Inst.GetLocalizedText("请输入一个介于{0}和{1}之间的整数。", MinPrice,MaxPrice);
                TipPanel.ShowToast(tipsStr);
                return;
            }

            if (musicScoreEditData.GetMusicScoreInfo().paymentInfo == null)
            {
                musicScoreEditData.GetMusicScoreInfo().paymentInfo = new PaymentInfo()
                {
                    price = value,
                    currencyType = musicScoreEditData.currencyType
                };
            }
            else
            {
                musicScoreEditData.GetMusicScoreInfo().paymentInfo.price = value;
                musicScoreEditData.GetMusicScoreInfo().paymentInfo.currencyType = musicScoreEditData.currencyType;
            }

            SyncEditData();
        }

        protected override void SetUgcShaderStyle(UgcShaderStyle ugcStyle)
        {
            base.SetUgcShaderStyle(ugcStyle);
            string freeText = LocalizationManager.Inst.GetLocalizedText("免费");
            var priceData = DataTables.GetOtherPublishPrice((int)PublishResType.MusicScore);
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
            musicScoreEditData = editData as MusicScoreEditData;
            base.SyncEditData();
            if (editData == null || editData.GetInfo() == null)
            {
                return;
            }

            ((LoadingButton)nextBtn).HideLoading();

            // SetCurrencyView(musicScoreEditData.currencyType);
            var paymentInfo = musicScoreEditData.GetMusicScoreInfo().paymentInfo;
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
                Toggle findToggle = null;
                if (musicScoreEditData.GetMusicScoreInfo().paymentInfo.currencyType == CurrencyType.PinkCoin)
                {
                    isCustom = !priceToggleDic.TryGetValue(musicScoreEditData.GetMusicScoreInfo().paymentInfo.price,
                        out var toggle);
                    findToggle = toggle;
                }
                if (isCustom)
                {
                    priceToggles[0].group.SetAllTogglesOff(false);
                    priceEditBtn.transform.Find("PriceCheckmark").gameObject.SetActive(true);
                    priceEditBtn.transform.Find("PriceNumEdit").gameObject.SetActive(false);
                    priceEditBtn.transform.Find("HasPrizeGroup").gameObject.SetActive(true);
                    priceEditBtn.transform.Find("HasPrizeGroup/Label").GetComponent<CText>().text =
                        paymentInfo.price.ToString();
                    priceEditBtn.transform.Find("HasPrizeGroup/Image").GetComponent<Image>().sprite = PgcUtils.LoadCurrencyIcon(musicScoreEditData.currencyType, gameObject);

                }
                else
                {
                    findToggle?.SetIsOnWithoutNotify(true);
                    priceEditBtn.transform.Find("PriceCheckmark").gameObject.SetActive(false);
                    priceEditBtn.transform.Find("PriceNumEdit").gameObject.SetActive(true);
                    priceEditBtn.transform.Find("HasPrizeGroup").gameObject.SetActive(false);
                }
                double price = paymentInfo.price / 2.0;
                selfPriceText.SetLocalText("每个商品你会获得等值的{0}个{1}创作者币",price,"<q=icn_common_green_big>");
            }
            // SetTipIconAndText(musicScoreEditData.currencyType);
            CheckNextEnable();
        }
        
       

        protected override void SyncCover() {
            var coverUrl = musicScoreEditData.draftInfo.GetCoverUrl();
            cover.Load(coverUrl);
        }
        protected override void CheckNextEnable() {
            bool isEnable = !string.IsNullOrEmpty(musicScoreEditData.GetMusicScoreInfo().name);
            isEnable &= !string.IsNullOrEmpty(musicScoreEditData.GetMusicScoreInfo().cover);
            SetNextEnabled(isEnable);
        }
    }
}
