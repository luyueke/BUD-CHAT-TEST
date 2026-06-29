using System;
using Com.TheFallenGames.OSA.Util.IO;
using Game.Audio;
using Game.Store;
using GameData.Base;
using GameData.BaseInfo;
using GameData.Manager;
using Message;
using Network.Http;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    public class ChatBuyVoiceCom : MonoBehaviour
    {
        public Button hideBtn;                          // 返回 ChatVoiceCom
        public RemoteImageBehaviour remoteImageBehaviour; // 音色封面
        public Button buyBtn;
        public Text txt_buy;                            // 花费数额
        public ChatChoiceAvatarCom chatChoiceAvatarCom;

        private CabinCharacterToneSearchSubData _data;
        private ChatVoiceCom _parent;

        private bool _buying = false;

        private void Awake()
        {
            hideBtn?.onClick.AddListener(OnHideClick);
            buyBtn?.onClick.AddListener(OnBuyClick);
        }

        private void OnDestroy()
        {
            AkSoundManager.Inst?.StopUGCAudio(gameObject);
        }

        // ── 公共入口 ──────────────────────────────────────────

        public void Show(CabinCharacterToneSearchSubData data, ChatVoiceCom parent)
        {
            _data = data;
            _parent = parent;
            _buying = false;

            gameObject.SetActive(true);
            RefreshUI();
        }

        // ── UI 刷新 ───────────────────────────────────────────

        private void RefreshUI()
        {
            var tone = _data?.ugcInfo;
            if (tone == null) return;

            // 封面
            if (!string.IsNullOrEmpty(tone.cover))
            {
                remoteImageBehaviour?.gameObject.SetActive(false);
                remoteImageBehaviour?.Load(tone.cover, true,
                    (_, ok) => { if (remoteImageBehaviour != null) remoteImageBehaviour.gameObject.SetActive(true); });
            }

            // 价格
            int price = tone.paymentInfo?.price ?? 0;
            if (txt_buy != null)
                txt_buy.text = price > 0 ? price.ToString() : "免费";

            buyBtn?.gameObject.SetActive(price > 0);
        }

        // ── 返回 ─────────────────────────────────────────────

        private void OnHideClick()
        {
            gameObject.SetActive(false);
            _parent?.ShowSelf();
        }

        // ── 购买 ─────────────────────────────────────────────

        private void OnBuyClick()
        {
            if (_buying || _data?.ugcInfo == null) return;

            var tone = _data.ugcInfo;
            int price = tone.paymentInfo?.price ?? 0;
            if (price <= 0) return;

            _buying = true;
            buyBtn?.gameObject.SetActive(false);

            var currencyType = (CurrencyType)(tone.paymentInfo?.currencyType ?? 0);

            AssetsDataManager.BuyUgc(tone.id, currencyType, price, (success, reason, needNum) =>
            {
                _buying = false;

                if (!success)
                {
                    buyBtn?.gameObject.SetActive(true);
                    HandleBuyFail(reason, currencyType, needNum);
                }
                else
                {
                    OnBuySuccess(tone);
                }
            });
        }

        private void HandleBuyFail(string reason, CurrencyType currencyType, int needNum)
        {
            if (!reason.Equals("余额不足")) return;
            if(Screen.orientation == ScreenOrientation.Portrait || Screen.autorotateToPortrait)
            {
                //竖屏不能打开
                TipPanel.ShowToast("余额不足");
                return;
            }

            switch (currencyType)
            {
                case CurrencyType.Coin:
                case CurrencyType.Badge:
                    var panel = UIManager.Inst.OpenPanel<ExchangeCoinPanel>(PanelId.ExchangeCoinPanel);
                    panel?.SetData(currencyType, CurrencyType.Gem, needNum);
                    break;
                case CurrencyType.PinkCoin:
                    if (ExchangeCoinPanel.JudgePinkCoin(needNum))
                    {
                        var p = UIManager.Inst.OpenPanel<ExchangeCoinPanel>(PanelId.ExchangeCoinPanel);
                        p?.SetData(CurrencyType.PinkCoin, CurrencyType.Gem, needNum);
                    }
                    break;
                case CurrencyType.Gem:
                    UIManager.Inst.OpenPanel(PanelId.GetMoreGemsPanel, needNum);
                    break;
            }
        }

        private void OnBuySuccess(CabinToneInfo tone)
        {
            var successPanel = UIManager.Inst.OpenPanel<BuySuccessTipPanel>(PanelId.BuySuccessTipPanel);
            successPanel?.InitData(tone, "购买成功！");
            MessageHelper.Broadcast(MessageName.OnBuyUgcItemSuccess, tone.id);

            gameObject.SetActive(false);

            // 购买成功后直接进入穿搭选择（toneId = 刚购买的音色）
            if (chatChoiceAvatarCom != null && _parent != null)
            {
                chatChoiceAvatarCom.Show(
                    _parent.BotProfile,
                    tone.id,
                    onBack: () => _parent.ShowSelf());
            }
            else
            {
                _parent?.ShowSelf();
            }
        }
    }
}
