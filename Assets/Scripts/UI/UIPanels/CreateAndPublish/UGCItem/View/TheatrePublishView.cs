using System;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.Util.IO;
using GameData.Base;
using GameData.BaseInfo;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UI.BaseWidgets;
using UGCAsset;
using UIAgent;
using UnityEngine;
using UnityEngine.UI;
using PaymentInfo = GameData.Base.PaymentInfo;

namespace UI
{
    public class TheatrePublishView : UGCBaseStateView
    {
        [SerializeField] private RemoteImageBehaviour cover;
        [SerializeField] private SuperTextMesh nameText;
        [SerializeField] private SuperTextMesh descText;
        [SerializeField] private Toggle[] priceToggles;
        [SerializeField] private Button priceEditBtn;
        [SerializeField] private Text selfPriceText;
        [SerializeField] private Button infoBtn;
        [SerializeField] private Button closeInfoBtn;
        [SerializeField] private GameObject priceRulesObj;
        [SerializeField] private Text totalDialogueText;
        [SerializeField] private Text totalActorNumberText;
        [SerializeField] private SuperTextMesh earningsText;


        private OCTheatreEditData _theatreEditData;
        private int _minPrice = 100;
        private const int MaxPrice = 9999;
        private readonly Dictionary<int, Toggle> _priceToggleDic = new();
        private KeyBoardInfo _priceKeyBoardInfo;

        public override void Awake()
        {
            base.Awake();

            if (priceEditBtn != null)
                priceEditBtn.onClick.AddListener(OnPriceEditBtnClick);

            if (infoBtn != null)
                infoBtn.onClick.AddListener(() => priceRulesObj?.SetActive(true));

            if (closeInfoBtn != null)
                closeInfoBtn.onClick.AddListener(() => priceRulesObj?.SetActive(false));

            if (priceRulesObj != null)
                priceRulesObj.SetActive(false);
        }

        protected override void SyncEditData()
        {
            _theatreEditData = editData as OCTheatreEditData;
            var info = _theatreEditData?.GetOCTheatreInfo();
            if (info == null) return;

            if (nameText != null) nameText.text = info.name;
            if (descText != null) descText.text = info.desc;
            if (cover != null && !string.IsNullOrEmpty(info.cover))
                cover.Load(info.cover);

            int dialogueCount = info.textCount;
            int actorCount = info.avatarList?.Count ?? 0;
            if (totalDialogueText != null)
                totalDialogueText.text = dialogueCount.ToString();
            if (totalActorNumberText != null)
                totalActorNumberText.text = actorCount.ToString();

            _minPrice = GetDialogueBasePrice(dialogueCount) + GetActorSurcharge(actorCount);
            InitKeyBoardInfo();
            InitPriceToggles();
            InitPaymentInfo(info);

            SetNextEnabled(true);
        }

        private void UpdateEarningsText(int price)
        {
            if (earningsText != null)
                earningsText.SetLocalText("每个商品你会获得等值的{0}个{1}创作者币", price / 2.0, "<q=icn_common_green_big>");
        }

        private static int GetDialogueBasePrice(int dialogueCount)
        {
            if (dialogueCount <= 10) return 250;
            if (dialogueCount <= 30) return 300;
            if (dialogueCount <= 80) return 350;
            return 400;
        }

        private static int GetActorSurcharge(int actorCount)
        {
            return Math.Max(0, actorCount - 1) * 20;
        }

        private void InitKeyBoardInfo()
        {
            _priceKeyBoardInfo = new KeyBoardInfo
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
        }

        private void InitPriceToggles()
        {
            _priceToggleDic.Clear();
            if (priceToggles == null) return;

            for (int i = 0; i < priceToggles.Length; i++)
            {
                int price = _minPrice + i * 50;
                var labelTf = priceToggles[i].transform.Find("Label");
                if (labelTf != null)
                {
                    var label = labelTf.GetComponent<Text>();
                    if (label != null) label.text = price.ToString();
                }

                _priceToggleDic[price] = priceToggles[i];
                priceToggles[i].onValueChanged.RemoveAllListeners();
                int captured = price;
                priceToggles[i].onValueChanged.AddListener(isOn =>
                {
                    if (isOn) OnPriceToggleClick(captured);
                });
            }
        }

        private void InitPaymentInfo(OCTheatreInfo info)
        {
            var paymentInfo = info.paymentInfo;
            if (paymentInfo == null || paymentInfo.price < _minPrice)
            {
                int defaultPrice = _minPrice;
                if (info.paymentInfo == null)
                {
                    info.paymentInfo = new PaymentInfo
                    {
                        price = defaultPrice,
                        currencyType = _theatreEditData.currencyType
                    };
                }
                else
                {
                    info.paymentInfo.price = defaultPrice;
                    info.paymentInfo.currencyType = _theatreEditData.currencyType;
                }

                if (_priceToggleDic.TryGetValue(defaultPrice, out var defaultToggle) && defaultToggle != null)
                    defaultToggle.SetIsOnWithoutNotify(true);
                SetPriceEditBtnState(false, 0);
                UpdateEarningsText(defaultPrice);
            }
            else
            {
                bool isCustom = !_priceToggleDic.TryGetValue(paymentInfo.price, out var toggle);
                if (isCustom)
                {
                    if (priceToggles != null && priceToggles.Length > 0 && priceToggles[0].group != null)
                        priceToggles[0].group.SetAllTogglesOff(false);
                    SetPriceEditBtnState(true, paymentInfo.price);
                }
                else
                {
                    if (toggle != null) toggle.SetIsOnWithoutNotify(true);
                    SetPriceEditBtnState(false, 0);
                }
                UpdateEarningsText(paymentInfo.price);
            }
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
                OnPriceToggleClick(value, true);
        }

        private void OnPriceToggleClick(int value, bool isCustom = false)
        {
            if (isCustom && (value < _minPrice || value > MaxPrice))
            {
                TipPanel.ShowToast(LocalizationManager.Inst.GetLocalizedText("请输入一个介于{0}和{1}之间的整数。", _minPrice, MaxPrice));
                return;
            }

            var info = _theatreEditData?.GetOCTheatreInfo();
            if (info == null) return;

            if (info.paymentInfo == null)
            {
                info.paymentInfo = new PaymentInfo
                {
                    price = value,
                    currencyType = _theatreEditData.currencyType
                };
            }
            else
            {
                info.paymentInfo.price = value;
                info.paymentInfo.currencyType = _theatreEditData.currencyType;
            }

            bool isCustomPrice = !_priceToggleDic.ContainsKey(value);
            SetPriceEditBtnState(isCustomPrice, isCustomPrice ? value : 0);
            UpdateEarningsText(value);
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

            if (selfPriceText != null)
                selfPriceText.text = isCustomSelected ? customPrice.ToString() : "输入自定义金额";
        }

        private bool _isPublishing;

        private LoadingButton NextLoadingBtn => nextBtn as LoadingButton;

        protected override void OnNextBtnClick()
        {
            if (_isPublishing) return;

            var info = _theatreEditData?.GetOCTheatreInfo();
            if (info == null) return;

            if (info.paymentInfo == null || info.paymentInfo.price < _minPrice)
            {
                TipPanel.ShowToast("请设置价格");
                return;
            }

            _isPublishing = true;
            NextLoadingBtn?.ShowLoading();
            var req = new SetTheatreInfoReq
            {
                theaterInfo = info,
                setType = (int)SetType.Publish
            };
            var auditReq = new UGCSetRequest(info, UGCOperationType.Publish);
            NetworkManager.Inst.SendHttpRequest(
                HttpUrlDefine.TheatreSet,
                HttpMethod.POST,
                JsonConvert.SerializeObject(req),
                _ =>
                {
                    _isPublishing = false;
                    base.OnNextBtnClick();
                    MessageHelper.Broadcast(MessageName.OnTheatreStudioPublishedListChange);
                },
                failMsg =>
                {
                    _isPublishing = false;
                    NextLoadingBtn?.HideLoading();
                    var rsp = JsonConvert.DeserializeObject<HttpResponseRawData>(failMsg);
                    if (rsp == null) return;
                    if (rsp.result == 501)
                    {
                        Action<bool> onAppealCallback = isAppeal =>
                        {
                            if (isAppeal)
                            {
                                if (info.auditInfo == null)
                                    info.auditInfo = new AuditStatus { auditResult = 4 };
                                else
                                    info.auditInfo.auditResult = 4;
                                base.OnNextBtnClick();
                                MessageHelper.Broadcast(MessageName.OnTheatreStudioPublishedListChange);
                            }
                            else
                            {
                                TipPanel.ShowToast(rsp.rmsg);
                            }
                        };
                        UIAgentManager.Inst.OpenPanel(PanelId.UGCAuditRejectedPanel, WindowId.None, auditReq, rsp.rmsg, onAppealCallback);
                    }
                    else
                    {
                        new HttpErrorCodeHandler().HandleErrorCodeResult(rsp);
                        LoggerUtils.LogError($"发布失败 [{info.id}]: {rsp.rmsg}");
                        TipPanel.ShowToast(rsp.rmsg);
                    }
                });
        }
    }
}
