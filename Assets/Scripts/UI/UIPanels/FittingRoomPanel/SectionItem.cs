using System;
using System.Collections;
using System.Collections.Generic;
using ChocDino.UIFX;
using Game.Audio;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace UI.UIPanels.FittingRoom
{
    public class SectionItem : MonoBehaviour, IPointerDownHandler
    {
        [SerializeField] Text nameText;
        [SerializeField] OutlineFilter outlineFilter;
        [SerializeField] Image bgImage;
        [SerializeField] Image icon;
        [SerializeField] Toggle toggle;
        [SerializeField] Image selectImage;
        [SerializeField] SpriteAtlas iconAtlas;

        private Action<SectionUIData> onValueChanged;
        private SectionUIData mClass;

        public void SetSelectedCallBack(Action<SectionUIData> action)
        {
            onValueChanged = action;
            toggle.onValueChanged.RemoveAllListeners();
            toggle.onValueChanged.AddListener(OnValueChanged);
        }

        public void SetSection(SectionUIData data)
        {
            mClass = data;
            nameText.SetLocalText(data.Name);
            bgImage.color = data.BgColor;
            selectImage.color = SectionUIData.SelectedColor;
            icon.sprite = iconAtlas.GetSprite($"section_{data.Id}");
            icon.gameObject.SetActive(icon.sprite != null);
            nameText.color = Color.white;
            outlineFilter.Color = new Color(0, 0, 0, 0);

            if (data.sectionConfig != null)
            {
                ColorUtility.TryParseHtmlString(data.sectionConfig.backgroundColor, out Color bgColor);
                bgImage.color = bgColor;
                ColorUtility.TryParseHtmlString(data.sectionConfig.selectColor, out Color selectColor);
                selectImage.color = selectColor;
                ColorUtility.TryParseHtmlString(data.sectionConfig.textColor, out Color textColor);
                nameText.color = textColor;
                ColorUtility.TryParseHtmlString(data.sectionConfig.lineColor, out Color lineColor);
                outlineFilter.Color = lineColor;
            }
            else if(data.seriesConfig != null)
            {
                ColorUtility.TryParseHtmlString(data.seriesConfig.ServerData.BackgroundColor, out Color bgColor);
                bgImage.color = bgColor;
                ColorUtility.TryParseHtmlString(data.seriesConfig.ServerData.TextColor, out Color selectColor);
                selectImage.color = selectColor;
                ColorUtility.TryParseHtmlString(data.seriesConfig.ServerData.TextColor, out Color textColor);
                nameText.color = textColor;
                ColorUtility.TryParseHtmlString(data.seriesConfig.ServerData.StrokeColor, out Color lineColor);
                outlineFilter.Color = lineColor;
            }
        }

        private void OnValueChanged(bool isOn)
        {
            if (isOn) onValueChanged?.Invoke(mClass);
        }

        public void SetIsOn(bool isOn)
        {
            toggle.SetIsOnWithoutNotify(isOn);
        }

        public void SetToggleGroup(ToggleGroup group)
        {
            toggle.group = group;
        }
        
        public void OnPointerDown(PointerEventData eventData)
        {
            AkSoundManager.Inst.PlayUIEffectSound(UISoundType.UI_ShiftTab_B1);
        }
    }
}