using System;
using System.Collections;
using System.Collections.Generic;
using GameData.BaseInfo;
using GameData.UGCData;
using UnityEngine;

namespace Game.MusicalInstrument
{
    public class SyllablePreviewPanel : MonoBehaviour
    {
        public Transform SyllableItemContent;
        public CommonSyllableItem _syllableItem;
        private List<CommonSyllableItem> _adjustSyllableItems = new List<CommonSyllableItem>();
        private Action<ToneInfo, int> _onItemSelected;
        public void InitData(ToneInfo toneInfo, Action<ToneInfo, int> act)
        {
            for (int i = 0; i < _adjustSyllableItems.Count; i++)
            {
                GameObject.Destroy(_adjustSyllableItems[i].gameObject);
            }

            _adjustSyllableItems.Clear();

            this._onItemSelected = act;
            var curToneInfo = toneInfo;
            if (curToneInfo == null)
            {
                LoggerUtils.LogError("乐器发布页面 curToneInfo 为空");
                return;
            }

            InitSyllableItem(curToneInfo);
        }

        private void InitSyllableItem(ToneInfo toneInfo)
        {
            var previewRange = MusicalInstrumentUtils.GetPreviewRange();
            for (int i = (int)previewRange.x; i <= (int)previewRange.y; i++)
            {
                var item = GameObject.Instantiate(_syllableItem, SyllableItemContent);
                item.InitData(toneInfo, i, OnItemSelected);
                _adjustSyllableItems.Add(item);
            }
        }

        private void OnItemSelected(ToneInfo toneInfo, int syllableId)
        {
            MusicalInstrumentManager.Inst.CheckInstrumentIsUgcToneAndShowToast(toneInfo);
            _onItemSelected?.Invoke(toneInfo, syllableId);
            
            DisableAllSelected();
        }

        public void DisableAllSelected()
        {
            if(_adjustSyllableItems == null)
                return;
            
            _adjustSyllableItems.ForEach(x => x.SetSelectState(false));
        }
    }
}
