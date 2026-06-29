using System;
using System.Collections;
using System.Collections.Generic;
using Es;
using GameData.BaseInfo;
using UnityEngine;
using UnityEngine.UI;

namespace Game.MusicalInstrument
{
    public class PgcToneContent : MonoBehaviour
    {
        public Text Txt_Title;
        public Transform PgcToneItemContent;
        public PgcToneInfoPublishedItem Prefab_PgcToneItem;
        private string _itemColor;
        private Action<ToneInfo> _onItemSelect;
        public void SetTitle(string title)
        {
            Txt_Title.SetLocalText(title);
        }

        public void SetItemColor(string color)
        {
            _itemColor = color;
        }
        
        public void SetOnToneItemSelectAct(Action<ToneInfo> act)
        {
            this._onItemSelect = act;
        }
        
        public List<PgcToneInfoPublishedItem> InitPgcToneItem(List<InstrumentToneConfig> instrumentToneConfigs)
        {
            List<PgcToneInfoPublishedItem> items = new List<PgcToneInfoPublishedItem>();
            for (int i = 0; i < instrumentToneConfigs.Count; i++)
            {
                PgcToneInfoPublishedItem item = GameObject.Instantiate(Prefab_PgcToneItem, PgcToneItemContent);
                items.Add(item);
                item.InitSelectMode(instrumentToneConfigs[i], _onItemSelect, _itemColor);
            }

            return items;
        }
    }
}
