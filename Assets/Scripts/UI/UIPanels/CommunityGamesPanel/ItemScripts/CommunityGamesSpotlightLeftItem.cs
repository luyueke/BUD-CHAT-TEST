using System;
using Basic.Extensions;
using ChocDino.UIFX;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace Game.CommunityGame
{
    public class CommunityGamesSpotlightLeftItem : MonoBehaviour
    {
        public CButton button;
        public CText title;
        public Image activeBg;
        public OutlineFilter OutlineFilter;
        public DropShadowFilter DropShadowFilter;
        private Action<OfficalRecommendItem> clickAction;
        public OfficalRecommendItem spotlightSectionItem;
        
        private Color OriTextColor = Color.white;
        private Color OriOutLineColor = Color.black;

        private void Awake()
        {
            
            if(title != null)
                OriTextColor = title.color;
            
            if(OutlineFilter != null)
                OriOutLineColor = OutlineFilter.Color;
            
        }

        private void Start()
        {
            button.onClick.AddListener(RootClick);
        }
    
        private void RootClick()
        {
            this.clickAction.Invoke(spotlightSectionItem);
        }
    
        public void SetSelect(bool isSel)
        {
            activeBg.gameObject.SetActive(isSel);
            InvertColor(isSel);
        }
    
        public void OnInitCreate(OfficalRecommendItem spotlightSectionItem, Action<OfficalRecommendItem> clickAction)
        {
            this.spotlightSectionItem = spotlightSectionItem;
            this.title.text = this.spotlightSectionItem.sectionName;
            this.clickAction = clickAction;
        }
        
        private void SetTextConfig(Text text, string fontColor, string strokeColor)
        {
            if (string.IsNullOrEmpty(fontColor) || string.IsNullOrEmpty(strokeColor) || text == null)
            {
                return;
            }
            try
            {
                text.color = DataUtil.DeSerializeColor(fontColor);
                Shadow titleShadow = text.gameObject.GetComponent<Shadow>();
                if (titleShadow != null)
                {
                    titleShadow.effectColor = DataUtil.DeSerializeColor(strokeColor);
                }
            }
            catch (Exception e)
            {
                
            }
        }
        
        private void InvertColor(bool isOn)
        {
            if (isOn)
            {
                if (title)
                    title.color = OriOutLineColor;
                
                if (OutlineFilter)
                    OutlineFilter.Color = OriTextColor;
                
                if (DropShadowFilter)
                    DropShadowFilter.Color = OriTextColor;
            }
            else
            {
                
                if (title)
                    title.color = OriTextColor;
                
                if (OutlineFilter)
                    OutlineFilter.Color = OriOutLineColor;
                
                if (DropShadowFilter)
                    DropShadowFilter.Color = OriOutLineColor;
            }
        }
    }
}

