using System;
using System.Collections;
using System.Collections.Generic;
using Game.Audio;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.CommunityGame
{
    public class SectionItem : MonoBehaviour, IPointerDownHandler
    {
        public CText Txt_Title;
        public Toggle Tog;
        public Image Img_Bg;
        public List<Sprite> _iconSprites;
        
        private SectionItemData _curData;
        private Action<string> _onSelect;
        
        public void InitItem(SectionItemData data, int index, Action<string> act)
        {
            this._curData = data;
            this.Txt_Title.SetText(data.sectionName);
            this._onSelect = act;
            
            var curSprite = _iconSprites[index % _iconSprites.Count];
            Img_Bg.sprite = curSprite;
            Tog.onValueChanged.RemoveAllListeners();
            Tog.onValueChanged.AddListener(SetSelectState);
        }
        
        public void SetSelectState(bool isSelect)
        {
            if (isSelect)
            {
                _onSelect?.Invoke(this._curData.sectionId);
            }
        }
        
        public void OnPointerDown(PointerEventData eventData)
        {
            AkSoundManager.Inst.PlayUIEffectSound(UISoundType.UI_ShiftTab_B1);
        }
    }
}
