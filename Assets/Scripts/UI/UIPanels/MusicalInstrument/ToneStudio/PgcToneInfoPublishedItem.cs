using System;
using Es;
using GameData.BaseInfo;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace Game.MusicalInstrument
{
    public class PgcToneInfoPublishedItem : MonoBehaviour
    {
        public Text Txt_Title;
        public CButton Btn_Select;
        public Image Img_BtnBg;
        public GameObject Go_Selected;
        public GameObject Go_Normal;
        public GameObject Go_TryPlay;
        public GameObject Go_VipTag;
        public GameObject Go_TimeFreeTag;
        
        private InstrumentToneConfig _curConfig;
        private Action<ToneInfo> _onItemSelect;
        
        private void Awake()
        {
            Btn_Select.onClick.AddListener(OnToneItemSelect);
            Go_Normal.SetActive(true);
            Go_TryPlay.SetActive(false);
        }

        public string GetPgcId()
        {
            return _curConfig?.pgcId;
        }
        
        private void OnToneItemSelect()
        {
            var toneInfo = MusicalInstrumentUtils.ConvertPgcToneConfigToToneInfo(_curConfig);
            this._onItemSelect?.Invoke(toneInfo);
            SetSelectState(true);

            Preview();
        }
        
        public void InitSelectMode(InstrumentToneConfig config, Action<ToneInfo> act, string bgColor)
        {
            this._curConfig = config;
            this._onItemSelect = act;
            Img_BtnBg.color = DataUtil.DeSerializeColorCheckHash(bgColor);
            Btn_Select.gameObject.SetActive(true);
            Txt_Title.SetLocalText(config.toneName);
            Go_VipTag.gameObject.SetActive(config.isVip == 1);

            var defaultToneInfo = MusicalInstrumentUtils.GetDefaultToneInfo();
            if (config.isVip == 0 && (config.pgcId != defaultToneInfo.id))
            {
                Go_TimeFreeTag.SetActive(true);
            }
        }
        
        public void SetSelectState(bool isSelect, bool playAnim = true)
        {
            Go_Selected.SetActive(isSelect);

            if (playAnim)
            {
                Go_Normal.SetActive(!isSelect);
                Go_TryPlay.SetActive(isSelect);
            }
            else
            {
                Go_Normal.SetActive(true);
                Go_TryPlay.SetActive(false);
            }
        }

        private void Preview()
        {
            MusicalInstrumentManager.Inst.PreviewPgcTone(this._curConfig);
        }
    }
}