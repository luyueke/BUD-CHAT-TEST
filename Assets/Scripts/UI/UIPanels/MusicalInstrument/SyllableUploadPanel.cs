using System;
using System.Collections;
using System.Collections.Generic;
using GameData.BaseInfo;
using UnityEngine;

namespace Game.MusicalInstrument
{
    public class SyllableUploadPanel : MonoBehaviour
    {
        [Header("高音")] public GameObject HighGroup;
        [Header("中音")] public GameObject MiddleGroup;
        [Header("低音")] public GameObject LowGroup;
        public SyllableUploadItem uploadItem;
        
        private ToneType _curToneType;
        private Action<SyllableType, string> _onUploadSyllableAct;

        private List<SyllableUploadItem> _uploadItems = new List<SyllableUploadItem>();

        public void Init()
        {
            InitUploadItems();
        }

        public void SwitchToneType(ToneType toneType, Action<SyllableType, string> act)
        {
            this._curToneType = toneType;
            this._onUploadSyllableAct = act;
            this._uploadItems.ForEach(x => x.ClearData());
            this._uploadItems.ForEach(x => x.SetUploadSuccessAction(this._onUploadSyllableAct));

            switch (toneType)
            {
                case ToneType.Fifteen:
                    HighGroup.SetActive(true);
                    MiddleGroup.SetActive(true);
                    LowGroup.SetActive(false);
                    break;

                case ToneType.TwentyTwo:
                    HighGroup.SetActive(true);
                    MiddleGroup.SetActive(true);
                    LowGroup.SetActive(true);
                    break;
            }
        }
        
        public void RestoreFromCache(Dictionary<int, SyllableData> tempDict)
        {
            foreach (var config in tempDict)
            {
                var targetItem = this._uploadItems.Find(x => x._syllableType == (SyllableType)config.Key);
                if (targetItem != null && config.Value != null)
                {
                    targetItem.RestoreFromCache(config.Value.url);
                }
            }
        }
        
        #region 音节配置
        private void InitUploadItems()
        {
            _uploadItems.Clear();

            if (MusicalInstrumentUtils.High_Config == null)
            {
                Debug.LogError("MusicalInstrumentUtils.High_Config = null");
            }
            
            foreach (var type in MusicalInstrumentUtils.High_Config)
            {
                var item = GameObject.Instantiate(uploadItem, HighGroup.transform);
                item.SetType(type);
                _uploadItems.Add(item);
            }

            foreach (var type in MusicalInstrumentUtils.Middle_Config)
            {
                var item = GameObject.Instantiate(uploadItem, MiddleGroup.transform);
                item.SetType(type);
                _uploadItems.Add(item);
            }

            foreach (var type in MusicalInstrumentUtils.Low_Config)
            {
                var item = GameObject.Instantiate(uploadItem, LowGroup.transform);
                item.SetType(type);
                _uploadItems.Add(item);
            }
        }

        #endregion
    }
}
