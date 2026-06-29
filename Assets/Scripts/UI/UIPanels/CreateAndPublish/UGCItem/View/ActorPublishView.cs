using System;
using System.Collections.Generic;
using GameData.Base;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UI.BaseWidgets;
using UIAgent;
using UGCAsset;
using UnityEngine;
using UnityEngine.UI;
using PaymentInfo = GameData.Base.PaymentInfo;

namespace UI
{
    public class ActorPublishView : BaseDetailView
    {
        [SerializeField] private Toggle[] priceToggles;
        [SerializeField] private CButton priceEditBtn;
        [SerializeField] private SuperTextMesh selfPriceText;
         [SerializeField] private Button priceInfoBtn;
         [SerializeField] private Button priceInfo;
        private const int MaxPrice = 9999;
        private int _minPrice = 25;

        private readonly Dictionary<int, Toggle> priceToggleDic = new();
        private KeyBoardInfo priceKeyBoardInfo;
        private ActorEditData _actorEditData;
        
        protected override int NameLimitCount => 16;
        protected override int DescLimitCount => 100;

        public override void Awake()
        {
            base.Awake();
            editCoverBtn.gameObject.SetActive(false);
            priceInfo.gameObject.SetActive(false);
            priceInfoBtn.onClick.AddListener(()=>{priceInfo.gameObject.SetActive(true);});
            priceInfo.onClick.AddListener(()=>{priceInfo.gameObject.SetActive(false);});
            priceKeyBoardInfo = new KeyBoardInfo()
            {
                type = 0,
                placeHolder = LocalizationManager.Inst.GetLocalizedText("请输入整数"),
                inputMode = 1,
                maxLength = 4,
                inputFlag = 0,
                textSecurity = 1,
                lengthTips = LocalizationManager.Inst.GetLocalizedText("请输入一个介于{0}和{1}之间的整数。", _minPrice, MaxPrice),
                returnKeyType = (int)ReturnType.Return
            };

            if (priceEditBtn != null)
                priceEditBtn.onClick.AddListener(OnPriceEditBtnClick);
        }

        private void InitPriceToggles()
        {
            int clothesCount = Mathf.Max(1, _actorEditData?.actorInfo?.avatarClothes?.Count ?? 1);
            _minPrice = 20 + clothesCount * 5;
            priceKeyBoardInfo.lengthTips = LocalizationManager.Inst.GetLocalizedText("请输入一个介于{0}和{1}之间的整数。", _minPrice, MaxPrice);

            priceToggleDic.Clear();
            if (priceToggles == null) return;

            for (int i = 0; i < priceToggles.Length; i++)
            {
                int price = _minPrice + i * 10;
                var labelTf = priceToggles[i].transform.Find("Label");
                if (labelTf != null)
                {
                    var priceLabel = labelTf.GetComponent<Text>();
                    if (priceLabel != null) priceLabel.text = price.ToString();
                }

                priceToggleDic[price] = priceToggles[i];
                priceToggles[i].onValueChanged.RemoveAllListeners();
                int captured = price;
                priceToggles[i].onValueChanged.AddListener(isOn =>
                {
                    if (isOn) OnPriceToggleClick(captured);
                });
            }
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
                OnPriceToggleClick(value, true);
        }

        private void OnPriceToggleClick(int value, bool isCustom = false)
        {
            if (isCustom && (value < _minPrice || value > MaxPrice))
            {
                TipPanel.ShowToast(LocalizationManager.Inst.GetLocalizedText("请输入一个介于{0}和{1}之间的整数。", _minPrice, MaxPrice));
                return;
            }

            if (_actorEditData == null || _actorEditData.actorInfo == null) return;

            if (_actorEditData.actorInfo.paymentInfo == null)
            {
                _actorEditData.actorInfo.paymentInfo = new PaymentInfo()
                {
                    price = value,
                    currencyType = _actorEditData.currencyType
                };
            }
            else
            {
                _actorEditData.actorInfo.paymentInfo.price = value;
                _actorEditData.actorInfo.paymentInfo.currencyType = _actorEditData.currencyType;
            }

            bool isCustomPrice = !priceToggleDic.ContainsKey(value);
            SetPriceEditBtnState(isCustomPrice, isCustomPrice ? value : 0);
            UpdateEarningsText(value);
            CheckNextEnable();
        }

        private void InitPaymentInfo()
        {
            if (_actorEditData == null || _actorEditData.actorInfo == null) return;

            var paymentInfo = _actorEditData.actorInfo.paymentInfo;
            if (paymentInfo == null || paymentInfo.price == 0)
            {
                int defaultPrice = _minPrice;
                if (_actorEditData.actorInfo.paymentInfo == null)
                {
                    _actorEditData.actorInfo.paymentInfo = new PaymentInfo()
                    {
                        price = defaultPrice,
                        currencyType = _actorEditData.currencyType
                    };
                }
                else
                {
                    _actorEditData.actorInfo.paymentInfo.price = defaultPrice;
                    _actorEditData.actorInfo.paymentInfo.currencyType = _actorEditData.currencyType;
                }
                if (priceToggles != null && priceToggles.Length > 0 && priceToggles[0].group != null)
                    priceToggles[0].group.SetAllTogglesOff(false);
                if (priceToggleDic.TryGetValue(defaultPrice, out var defaultToggle) && defaultToggle != null)
                    defaultToggle.SetIsOnWithoutNotify(true);
                SetPriceEditBtnState(false, 0);
                UpdateEarningsText(defaultPrice);
            }
            else
            {
                bool isCustom = !priceToggleDic.TryGetValue(paymentInfo.price, out var toggle);
                if (isCustom)
                {
                    if (priceToggles != null && priceToggles.Length > 0 && priceToggles[0].group != null)
                        priceToggles[0].group.SetAllTogglesOff(false);
                    SetPriceEditBtnState(true, paymentInfo.price);
                }
                else
                {
                    if (priceToggles != null && priceToggles.Length > 0 && priceToggles[0].group != null)
                        priceToggles[0].group.SetAllTogglesOff(false);
                    if (toggle != null) toggle.SetIsOnWithoutNotify(true);
                    SetPriceEditBtnState(false, 0);
                }
                UpdateEarningsText(paymentInfo.price);
            }
        }

        private void SetPriceEditBtnState(bool isCustomSelected, int customPrice)
        {
            if (priceEditBtn == null) return;

            var checkmark = priceEditBtn.transform.Find("PriceCheckmark");
            var numEdit = priceEditBtn.transform.Find("PriceNumEdit");
            var hasPrize = priceEditBtn.transform.Find("HasPrizeGroup");

            if (checkmark != null) checkmark.gameObject.SetActive(isCustomSelected);
            if (numEdit != null) numEdit.gameObject.SetActive(!isCustomSelected);
            if (hasPrize != null)
            {
                hasPrize.gameObject.SetActive(isCustomSelected);
                if (isCustomSelected)
                {
                    var labelTf = hasPrize.Find("Label");
                    if (labelTf != null)
                    {
                        var label = labelTf.GetComponent<CText>();
                        if (label != null) label.text = customPrice.ToString();
                    }
                }
            }
        }

        private void UpdateEarningsText(int price)
        {
            if (selfPriceText != null)
                selfPriceText.SetLocalText("每个商品你会获得等值的{0}个     创作者币", price / 2.0);// "<q=icn_common_green_big>"
        }

        protected override void OnNextBtnClick()
        {
            if (_actorEditData == null || _actorEditData.actorInfo == null) return;

            if (string.IsNullOrEmpty(_actorEditData.actorInfo.name))
            {
                TipPanel.ShowToast("请填写演员名称");
                return;
            }

            if (_actorEditData.actorInfo.paymentInfo == null || _actorEditData.actorInfo.paymentInfo.price <= 0)
            {
                TipPanel.ShowToast("请设置价格");
                return;
            }

            if (_actorEditData.actorInfo.avatarClothes == null || _actorEditData.actorInfo.avatarClothes.Count == 0)
            {
                TipPanel.ShowToast("请先添加衣柜后再发布");
                return;
            }

            if (nextBtn != null)
                ((LoadingButton)nextBtn).ShowLoading();

            var actorInfo = _actorEditData.actorInfo;
            var req = new SetActorInfoReq
            {
                actorInfo = actorInfo,
                setType = (int)SetType.Publish
            };
            var auditReq = new UGCSetRequest(actorInfo, UGCOperationType.Publish);
            NetworkManager.Inst.SendHttpRequest(
                HttpUrlDefine.ActorSet,
                HttpMethod.POST,
                JsonConvert.SerializeObject(req),
                _ =>
                {
                    base.OnNextBtnClick();
                    MessageHelper.Broadcast(MessageName.OnActorStudioPublishedListChange);
                },
                failMsg =>
                {
                    var rsp = JsonConvert.DeserializeObject<HttpResponseRawData>(failMsg);
                    if (rsp == null)
                    {
                        if (nextBtn != null) ((LoadingButton)nextBtn).HideLoading();
                        return;
                    }
                    if (rsp.result == 501)
                    {
                        Action<bool> onAppealCallback = isAppeal =>
                        {
                            if (isAppeal)
                            {
                                if (actorInfo.auditInfo == null)
                                    actorInfo.auditInfo = new AuditStatus { auditResult = 4 };
                                else
                                    actorInfo.auditInfo.auditResult = 4;
                                base.OnNextBtnClick();
                                MessageHelper.Broadcast(MessageName.OnActorStudioPublishedListChange);
                            }
                            else
                            {
                                if (nextBtn != null) ((LoadingButton)nextBtn).HideLoading();
                                TipPanel.ShowToast(rsp.rmsg);
                            }
                        };
                        UIAgentManager.Inst.OpenPanel(PanelId.UGCAuditRejectedPanel, WindowId.None, auditReq, rsp.rmsg, onAppealCallback);
                    }
                    else
                    {
                        new HttpErrorCodeHandler().HandleErrorCodeResult(rsp);
                        LoggerUtils.LogError($"发布失败 [{actorInfo.id}]: {rsp.rmsg}");
                        if (nextBtn != null) ((LoadingButton)nextBtn).HideLoading();
                        TipPanel.ShowToast(rsp.rmsg);
                    }
                }
            );
        }

        public override void Show()
        {
            base.Show();
            InitPriceToggles();
            InitPaymentInfo();
        }

        protected override void SyncEditData()
        {
            _actorEditData = editData as ActorEditData;
            base.SyncEditData();
            if (_actorEditData == null) return;

            if (nextBtn != null)
                ((LoadingButton)nextBtn).HideLoading();
        }

        protected override void CheckNextEnable()
        {
            var info = editData != null ? editData.GetInfo() : null;
            bool isEnable = info != null && !string.IsNullOrEmpty(info.name);
            isEnable &= info != null && !string.IsNullOrEmpty(info.desc);
            isEnable &= _actorEditData != null
                        && _actorEditData.actorInfo != null
                        && _actorEditData.actorInfo.paymentInfo != null
                        && _actorEditData.actorInfo.paymentInfo.price > 0;
            SetNextEnabled(isEnable);
        }

        protected override void SyncCover()
        {
            if (_actorEditData == null || _actorEditData.actorInfo == null) return;
            var coverUrl = _actorEditData.actorInfo.cover;
            if (!string.IsNullOrEmpty(coverUrl))
                cover.Load(coverUrl);
        }
    }
}
