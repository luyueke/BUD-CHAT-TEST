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
    public class CommunitySectionItem : MonoBehaviour, IPointerDownHandler
    {
        public CText Txt_Title;
        public Toggle Tog;

        private SectionItemData _curData;
        private Action<string> _onSelect;
        private void Awake()
        {
            Tog.onValueChanged.AddListener(SetSelectState);
        }

        public void InitItem(SectionItemData data, int index, Action<string> act)
        {
            this._curData = data;
            this.Txt_Title.text = data.sectionName;
            this.Txt_Title.GetComponent<ContentSizeFitter>().SetLayoutHorizontal();

            this._onSelect = act;

            var rect = transform as RectTransform;
            var textRect = Txt_Title.transform as RectTransform;
            rect.sizeDelta = new Vector2(textRect.sizeDelta.x + 70, 60);
        }

        public void RefreshLayout()
        {
            Txt_Title.GetComponent<ContentSizeFitter>().SetLayoutHorizontal();
            var rect = transform as RectTransform;
            var textRect = Txt_Title.transform as RectTransform;
            rect.sizeDelta = new Vector2(textRect.sizeDelta.x + 70, 60);
        }

        public void SetSelectState(bool isSelect)
        {
            if (isSelect)
            {
                Txt_Title.color = Color.black;
                _onSelect?.Invoke(this._curData.sectionId);
            }
            else
            {
                Txt_Title.color = Color.white;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            AkSoundManager.Inst.PlayUIEffectSound(UISoundType.UI_ShiftTab_B1);
        }
    }
}
