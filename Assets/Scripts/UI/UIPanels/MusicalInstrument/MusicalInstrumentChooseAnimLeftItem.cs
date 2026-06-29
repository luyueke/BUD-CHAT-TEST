using System;
using System.Collections;
using System.Collections.Generic;
using Es;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace Game.MusicalInstrument
{
    public class MusicalInstrumentChooseAnimLeftItem : MonoBehaviour
    {
        public GameObject Go_Selected;
        public GameObject Go_VipTag;
        public GameObject Go_TimeFreeTag;
        public Image Img_Icon;
        public CButton Btn_Click;
        public Text Txt_Name;

        private string _atlasPath = "Assets/Loadable/UI/UIPanel/MusicalInstrument/MusicalInstrument.spriteatlas";
        private string _curEmoteId = "";

        private Action<string> _onItemSelected;
        private void Awake()
        {
            Btn_Click.onClick.AddListener(OnBtnClick);
        }

        public void Init(string emoteId, Action<string> act)
        {
            this._curEmoteId = emoteId;
            this._onItemSelected = act;

            var sp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(_atlasPath, "Pgc_MusicAnim_" + emoteId, gameObject);
            Img_Icon.sprite = sp;

            var instrumentAniList = DataTables.GetInstrumentAniConfigList().FindAll((emoAniData) => emoAniData.emoId == emoteId);
            var config = instrumentAniList[0];
            string animName = config.name;
            string keyName = animName.Replace("DIY", "").Replace("BUD", "");
            Txt_Name.SetLocalText(keyName);
            bool isFree = MusicalInstrumentUtils.FreeMoveIdList.Contains(emoteId);
            // 取消限时免费标识
            // if (isFree && emoteId != MusicalInstrumentUtils.InstrumentDefaultMoveId)
            // {
            //     Go_TimeFreeTag.SetActive(true);
            // }
            Go_VipTag.SetActive(!isFree);
        }

        public void OnBtnClick()
        {
            _onItemSelected?.Invoke(_curEmoteId);

            SetSelectedState(true);
        }

        public void SetSelectedState(bool isSelected)
        {
            Go_Selected.SetActive(isSelected);
        }

        public string GetCurEmoId()
        {
            return _curEmoteId;
        }
    }
}
