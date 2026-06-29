using System;
using Com.TheFallenGames.OSA.Util.IO;
using GameData;
using GameData.Base;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.ProfilePanel
{
    public class ProfileMaterialItem : BaseProfileGrid
    {
        [SerializeField] private RemoteImageBehaviour cover;
        [SerializeField] private CButton clickBtn;
        [SerializeField] private Text priceTxt;
        [SerializeField] private Image priceIcon;
        [SerializeField] private GameObject auditView;
        [SerializeField] private Image gemOutfitTag;
        private MaterialResInfo _resInfo;
        private Action<MaterialResInfo, Texture> _onSelect;
        private void Awake()
        {
            gridImg = transform.Find("Views/ItemBg").GetComponent<Image>();
            topImg = transform.Find("Views/ItemBg/Top").GetComponent<Image>();
        }

        public void InitUI()
        {

        }

        public void SetData(MaterialResInfo item, Action<MaterialResInfo, Texture> onSelect)
        {
            if (item == null)
            {
                return;
            }

            _onSelect = onSelect;
            _resInfo = item;
            cover.Load(item.materialInfo.cover);
            
            SetPrice(item.materialInfo.paymentInfo);
            
            auditView.SetActive(FormatUtils.IsAuditing(item.materialInfo));

            clickBtn.onClick.RemoveAllListeners();
            clickBtn.onClick.AddListener(() => { _onSelect?.Invoke(_resInfo, cover.RawImage.texture); });

            
        }

        private void SetPrice(PaymentInfo paymentInfo)
        {
            if (paymentInfo == null || paymentInfo.price <= 0)
            {
                priceIcon.gameObject.SetActive(false);
                priceTxt.SetLocalText("免费");
            }
            else
            {
                priceIcon.gameObject.SetActive(true);
                priceIcon.sprite = PgcUtils.LoadCurrencyIcon(paymentInfo.currencyType, this.gameObject);
                gemOutfitTag.gameObject.SetActive(paymentInfo.currencyType == CurrencyType.Gem);
                priceTxt.SetText(paymentInfo.price + "");
            }
        }
        
    }
}