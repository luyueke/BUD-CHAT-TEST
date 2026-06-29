using System;
using Com.TheFallenGames.OSA.Util.IO;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.ProfilePanel
{
    public class ProfileCharacterBoxItem : MonoBehaviour
    {
        [SerializeField] private RemoteImageBehaviour cover;
        [SerializeField] private CButton clickBtn;
        [SerializeField] private Text priceTxt;
        [SerializeField] private Image priceIcon;

        private CharacterBoxInfo _info;
        private Action<CharacterBoxInfo> _onClick;

        private void Awake()
        {
            clickBtn.onClick.AddListener(() => _onClick?.Invoke(_info));
        }

        public void SetData(CharacterBoxInfo info, Action<CharacterBoxInfo> onClick)
        {
            _info = info;
            _onClick = onClick;

            cover.gameObject.SetActive(false);
            if (!string.IsNullOrEmpty(info.cover))
            {
                cover.Load(info.cover, true, (_, success) =>
                {
                    if (success) cover.gameObject.SetActive(true);
                });
            }

            var payment = info.paymentInfo;
            if (payment == null || payment.price <= 0)
            {
                priceIcon.gameObject.SetActive(false);
                priceTxt.SetLocalText("免费");
            }
            else
            {
                priceIcon.gameObject.SetActive(true);
                priceIcon.sprite = PgcUtils.LoadCurrencyIcon(payment.currencyType, gameObject);
                priceTxt.SetText(payment.price.ToString());
            }
        }
    }
}
