using System.Collections.Generic;
using GameData.BaseInfo;
using GameData.MapData;
using GameData.PgcData;
using Newtonsoft.Json;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;
using xasset;
using PaymentInfo = GameData.Base.PaymentInfo;

namespace UI
{
    public class UgcPosePublishView : BaseDetailView
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

        private Dictionary<int, Toggle> priceToggleDic;
        private Dictionary<int, Toggle> gemToggleDic;
        [SerializeField] private GameObject coinText;
        private KeyBoardInfo priceKeyBoardInfo;

        private UGCPoseEditData _poseEditData;
        
        private int Min_Price = 10;
        private int Price_Delta = 10;

        public override void Awake()
        {
            base.Awake();
            _poseEditData = editData as UGCPoseEditData;
            btn_Tips.onClick.AddListener(() =>
            {
                go_Tips.SetActive(true);
            });
            btn_CloseTips.onClick.AddListener(() =>
            {
                go_Tips.SetActive(false);
            });
            InitPriceToggleView();
        }
        
        public void InitPriceToggleView()
        {
            var animInfo = _poseEditData.GetPoseInfo();
            switch ((EmoteSubType)animInfo.poseType)
            {
                case EmoteSubType.PetSingle :
                case EmoteSubType.Single :
                    Min_Price = 10;
                    break;
                
                case EmoteSubType.PetWithPlayer :
                case EmoteSubType.Double :
                    Min_Price = 20;
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

        protected override void OnEditCoverBtnClick()
        {
            base.OnEditCoverBtnClick();
            //Test
            return;
            
            gameObject.SetActive(false);
            UGCPublishStateBase coverState = null;
            if (editData.GetInfo() is PropInfo)
            {
                coverState = new PropCoverState();
            }

            if (coverState == null)
            {
                return;
            }

            coverState.InitData(editData);
            coverState.OnEnter();
            coverState.SetNextCallback(() =>
            {
                coverState.OnExit();
                gameObject.SetActive(true);
                SyncEditData();
            });
            coverState.SetPreCallback(() =>
            {
                coverState.OnExit();
                gameObject.SetActive(true);
                SyncEditData();
            });
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
            ((LoadingButton)nextBtn).ShowLoading();

            // 先上传本地草稿
            // 成功后，发布草稿
            _poseEditData.draftInfo.Upload((info, isSuccess) =>
            {
                if (gameObject == null)
                {
                    return;
                }

                if (isSuccess)
                {
                    _poseEditData.draftInfo.PublishDraftToServer((newInfo, newSuccess) =>
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
                    TipPanel.ShowToast($"{_poseEditData.draftInfo.GetUploadMessage()}");
                }
            });
        }

        private void OnPriceToggleClick(int value,bool isCustom = false )
        {
            if (isCustom && (value < Min_Price || value > 9999))
            {
                var lengthTips = LocalizationManager.Inst.GetLocalizedText("请输入一个介于{0}和{1}之间的整数。", Min_Price, 9999);
                TipPanel.ShowToast(lengthTips);
                return;
            }

            if (_poseEditData.GetPoseInfo().paymentInfo == null)
            {
                _poseEditData.GetPoseInfo().paymentInfo = new PaymentInfo()
                {
                    price = value,
                    currencyType = _poseEditData.currencyType
                };
            }
            else
            {
                _poseEditData.GetPoseInfo().paymentInfo.currencyType = _poseEditData.currencyType;
                _poseEditData.GetPoseInfo().paymentInfo.price = value;
            }

            SyncEditData();
        }

        protected override void SyncEditData()
        {
            _poseEditData = editData as UGCPoseEditData;

            base.SyncEditData();
            if (editData == null || editData.GetInfo() == null)
            {
                return;
            }

            ((LoadingButton)nextBtn).HideLoading();

            SetCurrencyView(_poseEditData.currencyType);
            var paymentInfo = _poseEditData.GetPoseInfo().paymentInfo;
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
                if (_poseEditData.GetPoseInfo().paymentInfo.currencyType == CurrencyType.Gem)
                {
                    isCustom = !gemToggleDic.TryGetValue(_poseEditData.GetPoseInfo().paymentInfo.price, out var toggle);
                    findToggle = toggle;
                }
                else
                {
                    isCustom = !priceToggleDic.TryGetValue(_poseEditData.GetPoseInfo().paymentInfo.price,
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
                    priceEditBtn.transform.Find("HasPrizeGroup/Image").GetComponent<Image>().sprite = PgcUtils.LoadCurrencyIcon(_poseEditData.currencyType, gameObject);
                }
                else
                {
                    findToggle.SetIsOnWithoutNotify(true);
                    priceEditBtn.transform.Find("PriceCheckmark").gameObject.SetActive(false);
                    priceEditBtn.transform.Find("PriceNumEdit").gameObject.SetActive(true);
                    priceEditBtn.transform.Find("HasPrizeGroup").gameObject.SetActive(false);
                }

                selfPriceText.text = _poseEditData.currencyType == CurrencyType.Gem ? $"每个商品你会获得等值的{paymentInfo.price}个" : $"每个商品你会获得等值的{paymentInfo.price / 2.0}个";
                coinText.SetActive(true);
            }
            SetTipIconAndText(_poseEditData.currencyType);
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
            var tmpCover = _poseEditData.draftInfo.GetCoverUrl();
            if (!string.IsNullOrEmpty(tmpCover))
            {
                cover.Load(tmpCover);
            }
        }
    }
}
