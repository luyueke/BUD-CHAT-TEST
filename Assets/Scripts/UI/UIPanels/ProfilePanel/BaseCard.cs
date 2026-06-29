using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.ProfilePanel
{
    public enum ProfileCardBgType
    {
        None,
        Bg1,
        Bg2,
        Bg3,
        Bg4,
        Bg5,
    }
    public class BaseCard : MonoBehaviour
    {
        public bool flexible = false;
        public float shrinkWidth;
        public float expandWidth;
        // protected ProfileDataBundle DataBundle { get; set; }
        public bool IsShow => gameObject.activeSelf;
        private string _userId;
        internal ProfilePanel _profilePanel;

        protected Image CardBg;
        protected Image TitleBg;

        private ProfileThemeInfo themeInfo;

        public virtual ProfileCardBgType cardBgType { get; set; } = ProfileCardBgType.None;

        protected GameObject addBg = null;

        public virtual void OnCreate(ProfilePanel profilePanel)
        {
            _profilePanel = profilePanel;
            var cardBgNode = GameObjectEx.FindChildByName(transform, "Bg");
            if (cardBgNode != null)
            {
                CardBg = cardBgNode.GetComponent<Image>();
                TitleBg = GameObjectEx.FindChildByName(cardBgNode, "Top")?.GetComponent<Image>();
            }
        }

        public virtual void OnUpdateTheme(ProfileThemeInfo themeInfo)
        {
            this.themeInfo = themeInfo;
            if (themeInfo.colorInfo != null)
            {
                if (CardBg != null && !string.IsNullOrEmpty(themeInfo.colorInfo.cardColor))
                {
                    CardBg.color = DataUtil.DeSerializeColorCheckHash(themeInfo.colorInfo.cardColor);
                }
                
                if (TitleBg != null && !string.IsNullOrEmpty(themeInfo.colorInfo.titleBgColor))
                {
                    TitleBg.color = DataUtil.DeSerializeColorCheckHash(themeInfo.colorInfo.titleBgColor);
                }
            }
            ShowBg();

        }

        private void ShowBg()
        {

            if (addBg != null && addBg.transform.parent != null)
            {
                Destroy(addBg);
                addBg = null;
            }
            if (cardBgType != ProfileCardBgType.None)
            {
                string url = string.Empty;
                switch (cardBgType)
                {
                    case ProfileCardBgType.Bg1:
                        url = themeInfo.colorInfo.info_bg1; break;
                    case ProfileCardBgType.Bg2:
                        url = themeInfo.colorInfo.info_bg2; break;
                    case ProfileCardBgType.Bg3:
                        url = themeInfo.colorInfo.info_bg3; break;
                    case ProfileCardBgType.Bg4:
                        url = themeInfo.colorInfo.info_bg4; break;
                    case ProfileCardBgType.Bg5:
                        url = themeInfo.colorInfo.info_bg5; break;
                }
                if (!string.IsNullOrEmpty(url))
                {
                    addBg = Loader.Load<GameObject>(url).Instantiate(transform);
                    addBg.transform.SetSiblingIndex(1);
                }
            }
        }

        public virtual void OnShow(string uid)
        {
            _userId = uid;
        }

        
        public virtual void Show(bool isOn)
        {
            gameObject.SetActive(isOn);
        }

        public virtual void Shrink()
        {
            if (flexible)
            {
                SetTransformWidth(shrinkWidth);
            }
            cardBgType = ProfileCardBgType.Bg2;
            if(themeInfo != null)
            {
                ShowBg();
            }
        }

        public virtual void Expand()
        {
            if (flexible)
            {
                SetTransformWidth(expandWidth);
            }
        }

        public virtual void ChangeRawImageState(bool isRelease)
        {
        }

        protected void SetTransformWidth(float width)
        {
            RectTransform rectTransform = transform as RectTransform;
            Vector2 curSizeDelta = rectTransform.sizeDelta;
            curSizeDelta.x = width;
            rectTransform.sizeDelta = curSizeDelta;

            LayoutElement element = GetComponent<LayoutElement>();
            if (element != null)
            {
                element.preferredHeight = width;
            }
        }

        public void ShowLoading(bool isShow)
        {
            Transform tr = transform.Find("Empty");
            if (tr)
            {
                tr.gameObject.SetActive(isShow);
            }
        }
    }
}
