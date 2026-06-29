using System;
using System.Collections.Generic;
using BUD.AnimPose;
using GameData.BaseInfo;
using GameData.MapData;
using GameData.PgcData;
using Message;
using Newtonsoft.Json;
using UGCAsset;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;
using xasset;
using PaymentInfo = GameData.Base.PaymentInfo;

namespace UI
{
    public class UgcAnimPublishView : BaseDetailView
    {
        protected override int NameLimitCount => 6;
        [SerializeField] private Toggle priceToggle10;
        [SerializeField] private Toggle priceToggle15;
        [SerializeField] private Toggle priceToggle20;
        [SerializeField] private Toggle priceToggle25;
        [SerializeField] private Toggle priceToggle30;
        [SerializeField] private CButton priceEditBtn;
        [SerializeField] private Text selfPriceText;

        [SerializeField] private GameObject[] gemObjects;
        [SerializeField] private GameObject[] pinkCoinObjects;
        [SerializeField] private CButton btn_Tips;
        [SerializeField] private GameObject go_Tips;
        [SerializeField] private CButton btn_CloseTips;
        [SerializeField] private Toggle tog_SingleAnim;
        [SerializeField] private Toggle tog_LoopAnim;

        private Dictionary<int, Toggle> priceToggleDic;
        private Dictionary<int, Toggle> gemToggleDic;
        [SerializeField] private GameObject coinText;
        private KeyBoardInfo priceKeyBoardInfo;

        private UGCAnimEditData _animEditData;
        private AnimFrameData _animFrameData;

        private int Min_Price = 100;
        private int Price_Delta = 50;


        public override void Awake()
        {
            base.Awake();
            MessageHelper.AddListener(DraftMessage.RefreshDraft, SyncCover);
            _animEditData = editData as UGCAnimEditData;
            btn_Tips.onClick.AddListener(() =>
            {
                go_Tips.SetActive(true);
            });
            btn_CloseTips.onClick.AddListener(() =>
            {
                go_Tips.SetActive(false);
            });
            InitPriceToggleView();
            tog_SingleAnim.onValueChanged.AddListener(OnSingleTogValueChange);
            tog_LoopAnim.onValueChanged.AddListener(OnLoopTogValueChange);
        }
        
        public void InitPriceToggleView()
        {
            var animInfo = _animEditData.GetAnimInfo();
            switch ((EmoteSubType)animInfo.animType)
            {
                case EmoteSubType.PetSingle :
                case EmoteSubType.Single :
                    if (animInfo.animationTime <= 5)
                    {
                        Min_Price = 100;
                    }
                    else if (animInfo.animationTime <= 10)
                    {
                        Min_Price = 150;
                    }
                    else if (animInfo.animationTime <= 15)
                    {
                        Min_Price = 200;
                    }
                    else if (animInfo.animationTime <= 20)
                    {
                        Min_Price = 250;
                    }
                    break;
                
                case EmoteSubType.PetWithPlayer :
                case EmoteSubType.Double :
                    if (animInfo.animationTime <= 5)
                    {
                        Min_Price = 150;
                    }
                    else if (animInfo.animationTime <= 10)
                    {
                        Min_Price = 200;
                    }
                    else if (animInfo.animationTime <= 15)
                    {
                        Min_Price = 250;
                    }
                    else if (animInfo.animationTime <= 20)
                    {
                        Min_Price = 300;
                    }
                    break;
            }
            priceEditBtn.onClick.AddListener(OnPriceEditBtnClick);
            priceToggle10.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    OnPriceToggleClick(Min_Price + Price_Delta * 0);
                }
            });
            priceToggle10.GetComponentInChildren<Text>().SetText((Min_Price + Price_Delta * 0).ToString());
            
            priceToggle15.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    OnPriceToggleClick(Min_Price + Price_Delta * 1);
                }
            });
            priceToggle15.GetComponentInChildren<Text>().SetText((Min_Price + Price_Delta * 1).ToString());
            
            priceToggle20.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    OnPriceToggleClick(Min_Price + Price_Delta * 2);
                }
            });
            priceToggle20.GetComponentInChildren<Text>().SetText((Min_Price + Price_Delta * 2).ToString());
            
            priceToggle25.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    OnPriceToggleClick(Min_Price + Price_Delta * 3);
                }
            });
            priceToggle25.GetComponentInChildren<Text>().SetText((Min_Price + Price_Delta * 3).ToString());
            
            priceToggle30.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    OnPriceToggleClick(Min_Price + Price_Delta * 4);
                }
            });
            priceToggle30.GetComponentInChildren<Text>().SetText((Min_Price + Price_Delta * 4).ToString());
            
            priceToggleDic = new Dictionary<int, Toggle>()
            {
                { Min_Price + Price_Delta * 0, priceToggle10 },
                { Min_Price + Price_Delta * 1, priceToggle15 },
                { Min_Price + Price_Delta * 2, priceToggle20 },
                { Min_Price + Price_Delta * 3, priceToggle25 },
                { Min_Price + Price_Delta * 4, priceToggle30 }
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
                defaultText = "",
                returnKeyType = (int)ReturnType.Return
            };
        }

        private void OnDestroy()
        {
            MessageHelper.RemoveListener(DraftMessage.RefreshDraft, SyncCover);
        }

        private void OnSingleTogValueChange(bool isOn)
        {
            if (isOn)
            {
                _animEditData.GetAnimInfo().loop = 0;
            }
        }
        
        private void OnLoopTogValueChange(bool isOn)
        {
            if (!UgcAnimVipChecker.Inst.CanChooseLoopAnim())
            {
                tog_SingleAnim.isOn = true;
                return;
            }
            
            if (isOn)
            {
                _animEditData.GetAnimInfo().loop = 1;
            }
        }

        protected override void OnEditCoverBtnClick()
        {
            base.OnEditCoverBtnClick();
            var curAnimInfo = _animEditData.GetAnimInfo();
            var metaDataUrl = curAnimInfo.metaDataUrl;
            if (_animFrameData != null)
            {
                UIManager.Inst.OpenPanel<AnimationStudioEditCoverPanel>(PanelId.AnimationStudioEditCoverPanel, curAnimInfo, _animFrameData, true);
                return;
            }
            new AssetLoader().DownloadAsset(metaDataUrl.Replace('\\', '/'), (byte[] metaDataBytes) =>
            {
                if (metaDataBytes == null || metaDataBytes.Length == 0)
                {
                    return;
                }
            
                string jsonString = System.Text.Encoding.UTF8.GetString(metaDataBytes);
                if(string.IsNullOrEmpty(jsonString))
                    return;
                
                _animFrameData = JsonConvert.DeserializeObject<AnimFrameData>(jsonString);
                UIManager.Inst.OpenPanel<AnimationStudioEditCoverPanel>(PanelId.AnimationStudioEditCoverPanel, curAnimInfo, _animFrameData, true);
            }, null);
            
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


        protected override void OnNextBtnClick()
        {
            if (!UgcAnimVipChecker.Inst.CheckUgcAnimCanPublish(_animEditData.GetAnimInfo()))
            {
                return;
            }
            
            ((LoadingButton)nextBtn).ShowLoading();

            // 先上传本地草稿
            // 成功后，发布草稿
            _animEditData.draftInfo.Upload((info, isSuccess) =>
            {
                if (gameObject == null)
                {
                    return;
                }

                if (isSuccess)
                {
                    _animEditData.draftInfo.PublishDraftToServer((newInfo, newSuccess) =>
                    {
                        if (newSuccess)
                        {
                            base.OnNextBtnClick();
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
                    TipPanel.ShowToast($"{_animEditData.draftInfo.GetUploadMessage()}");
                }
            });
        }


        private bool CheckEmpty() {
            if (_animEditData.metaDataBytes == null && !string.IsNullOrEmpty(editData.GetInfo().metaDataUrl)) {
                _animEditData.metaDataBytes = Asset.LoadRemoteAssetSync(editData.GetInfo().metaDataUrl);
            }
            if (_animEditData.metaDataBytes != null) {
                var itemPb = MapPbDataTool.ParsePropPb(_animEditData.metaDataBytes);
                if (itemPb != null && itemPb.NodeData.Prims.Count > 0) {
                    return false;
                } else {
                    return true;
                }
            } else {
                return true;
            }
        }

        private void OnPriceToggleClick(int value,bool isCustom = false )
        {
            var lengthTips = LocalizationManager.Inst.GetLocalizedText("请输入一个介于{0}和{1}之间的整数。", Min_Price, 9999);
            if (isCustom && (value < Min_Price || value > 9999))
            {
                TipPanel.ShowToast(lengthTips);
                return;
            }

            if (_animEditData.GetAnimInfo().paymentInfo == null)
            {
                _animEditData.GetAnimInfo().paymentInfo = new PaymentInfo()
                {
                    price = value,
                    currencyType = _animEditData.currencyType
                };
            }
            else
            {
                _animEditData.GetAnimInfo().paymentInfo.currencyType = _animEditData.currencyType;
                _animEditData.GetAnimInfo().paymentInfo.price = value;
            }

            SyncEditData();
        }

        protected override void SyncEditData()
        {
            _animEditData = editData as UGCAnimEditData;

            base.SyncEditData();
            if (editData == null || editData.GetInfo() == null)
            {
                return;
            }

            ((LoadingButton)nextBtn).HideLoading();

            SetCurrencyView(_animEditData.currencyType);
            var paymentInfo = _animEditData.GetAnimInfo().paymentInfo;
            if (paymentInfo == null || paymentInfo.price == 0)
            {
                priceEditBtn.transform.Find("PriceCheckmark").gameObject.SetActive(false);
                priceEditBtn.transform.Find("PriceNumEdit").gameObject.SetActive(true);
                priceEditBtn.transform.Find("HasPrizeGroup").gameObject.SetActive(false);
                selfPriceText.text = "";
                coinText.SetActive(false);
                
                priceToggle10.SetIsOnWithoutNotify(true);
                OnPriceToggleClick(Min_Price);
            }
            else
            {
                bool isCustom = false;
                Toggle findToggle;
                if (_animEditData.GetAnimInfo().paymentInfo.currencyType == CurrencyType.Gem)
                {
                    isCustom = !gemToggleDic.TryGetValue(_animEditData.GetAnimInfo().paymentInfo.price, out var toggle);
                    findToggle = toggle;
                }
                else
                {
                    isCustom = !priceToggleDic.TryGetValue(_animEditData.GetAnimInfo().paymentInfo.price,
                        out var toggle);
                    findToggle = toggle;
                }

                if (isCustom)
                {
                    priceToggle10.group.SetAllTogglesOff(false);
                    priceEditBtn.transform.Find("PriceCheckmark").gameObject.SetActive(true);
                    priceEditBtn.transform.Find("PriceNumEdit").gameObject.SetActive(false);
                    priceEditBtn.transform.Find("HasPrizeGroup").gameObject.SetActive(true);
                    priceEditBtn.transform.Find("HasPrizeGroup/Label").GetComponent<CText>().text = paymentInfo.price.ToString();
                    priceEditBtn.transform.Find("HasPrizeGroup/Image").GetComponent<Image>().sprite = PgcUtils.LoadCurrencyIcon(_animEditData.currencyType, gameObject);
                }
                else
                {
                    findToggle.SetIsOnWithoutNotify(true);
                    priceEditBtn.transform.Find("PriceCheckmark").gameObject.SetActive(false);
                    priceEditBtn.transform.Find("PriceNumEdit").gameObject.SetActive(true);
                    priceEditBtn.transform.Find("HasPrizeGroup").gameObject.SetActive(false);
                }

                selfPriceText.text = _animEditData.currencyType == CurrencyType.Gem ? $"每个商品你会获得等值的{paymentInfo.price}个" : $"每个商品你会获得等值的{paymentInfo.price / 2.0}个";
                coinText.SetActive(true);
            }
            SetTipIconAndText(_animEditData.currencyType);
        }

        private void SetCurrencyView(CurrencyType currencyType)
        {
            priceToggle10.gameObject.SetActive(currencyType == CurrencyType.PinkCoin);
            priceToggle15.gameObject.SetActive(currencyType == CurrencyType.PinkCoin);
            priceToggle20.gameObject.SetActive(currencyType == CurrencyType.PinkCoin);
            priceToggle25.gameObject.SetActive(currencyType == CurrencyType.PinkCoin);
            priceToggle30.gameObject.SetActive(currencyType == CurrencyType.PinkCoin);
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
            base.SyncCover();
            var tmpCover = _animEditData.draftInfo.GetCoverUrl();
            if (!string.IsNullOrEmpty(tmpCover))
            {
                cover.Load(tmpCover);
            }
        }
    }
}
