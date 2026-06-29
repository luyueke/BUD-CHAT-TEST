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
    public class BaseProfileGrid : MonoBehaviour
    {
        protected Image gridImg;
        protected Image topImg;

        private Color oldColor;
        private Color oldPriceColor;
       
        void Start()
        {
            if(gridImg != null)
                oldColor = gridImg.color;
            if (topImg != null)
                oldPriceColor = topImg.color;
            if (ProfilePanel.accountUserInfo != null)
            {
                UpdateGridColor(ProfilePanel.accountUserInfo.homepageSkin);
            }else
            {
                UpdateGridColor(AccountDataManager.Inst.UserInfo.homepageSkin);
            }
  
            AccountDataManager.Inst.AddUserInfoChangeListener(OnUserInfoChange);
        }

        private void OnDestroy()
        {
            AccountDataManager.Inst.RemoveUserInfoChangeListener(OnUserInfoChange);
        }

        private void OnUserInfoChange(AccountUserInfo userInfo)
        {
            if (userInfo != null)
            {
                UpdateGridColor(userInfo.homepageSkin);
            }
        }



        private void UpdateGridColor(ProfileThemeInfo themeInfo)
        {
            if (gridImg == null)
                return;
            if(!string.IsNullOrEmpty( themeInfo?.colorInfo?.gridColor))
            {
                gridImg.color = DataUtil.DeSerializeColorCheckHash(themeInfo.colorInfo.gridColor);
            }else if(oldColor != null)
            {
                gridImg.color = oldColor;
            }
            if (topImg == null)
                return;
            if (!string.IsNullOrEmpty(themeInfo?.colorInfo?.priceColor))
            {
                topImg.color = DataUtil.DeSerializeColorCheckHash(themeInfo.colorInfo.priceColor);
            }
            else if (oldPriceColor != null)
            {
                topImg.color = oldPriceColor;
            }
        }

        private void UpdateGridColor(int themeId)
        {
          //  var themeId = AccountDataManager.Inst.UserInfo.homepageSkin;
            ProfileThemeInfo themeInfo = ProfileThemeManager.Inst.GetThemeInfo(themeId);
            UpdateGridColor(themeInfo);
        }
    }


    public class ProfileCommonItem : BaseProfileGrid
    {
        [SerializeField] protected RemoteImageBehaviour cover;
        [SerializeField] protected CButton clickBtn;
        [SerializeField] protected Text priceTxt;
        [SerializeField] protected Image priceIcon;
        [SerializeField] protected GameObject auditView;
        protected DraftListItem _resInfo;
        protected Action<DraftListItem, Texture> _onSelect;

        private void Awake()
        {
            gridImg = transform.Find("Views/ItemBg").GetComponent<Image>();
            topImg = transform.Find("Views/ItemBg/Top").GetComponent<Image>();
        }
        public void InitUI()
        {

        }

        public virtual void SetData(DraftListItem item, Action<DraftListItem, Texture> onSelect)
        {
            if (item == null)
            {
                return;
            }

            _onSelect = onSelect;
            _resInfo = item;
            clickBtn.onClick.RemoveAllListeners();
            clickBtn.onClick.AddListener(() => { _onSelect?.Invoke(_resInfo, cover.RawImage.texture); });

            //UpdateGridColor();
        }

        
        protected virtual void SetPrice(PaymentInfo paymentInfo)
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
                priceTxt.SetText(paymentInfo.price + "");
            }
        }
        
    }
}