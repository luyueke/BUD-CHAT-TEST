using System;
using GameData.BaseInfo;
using Pb.Game;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.MusicalInstrument
{
    public class CommonSyllableItem : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public Image Img_Icon;
        public Image Img_Selected;

        private string _atlasPath = "Assets/Loadable/UI/UIPanel/MusicalInstrument/MusicalInstrument.spriteatlas";
        public int _syllableId;
        private ToneInfo _toneInfo;

        private Action<ToneInfo, int> _onItemSelected;
        private bool isClick;
        public void OnPointerDown(PointerEventData eventData)
        {
            OnBtnClick();
            isClick = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Img_Selected.gameObject.SetActive(false);
            isClick = false;
        }

        public void InitData(ToneInfo toneInfo, int syllableId, Action<ToneInfo, int> onItemSelected)
        {
            this._toneInfo = toneInfo;
            this._syllableId = syllableId;
            this._onItemSelected = onItemSelected;

            InitSyllableIcon(this._syllableId);
        }

        private void InitSyllableIcon(int syllableId)
        {
            var sp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(SpriteAtlasType.Common, "Syllable_Icon_" + syllableId, gameObject);
            Img_Icon.sprite = sp;
        }

        private void OnBtnClick()
        {
            this._onItemSelected?.Invoke(_toneInfo, _syllableId);
            SetSelectState(true);
        }

        public void SetSelectState(bool isSelect)
        {
            if (isSelect)
            {
                if(!Img_Selected.gameObject.activeInHierarchy)
                    Img_Selected.color = MusicalInstrumentUtils.GetRandomSyllableItemSelectedColor();

                Img_Selected.gameObject.SetActive(true);
            }
            else
            {
                Img_Selected.gameObject.SetActive(false);
                // MusicalInstrumentManager.Inst.StopSingleSyllable(_toneInfo, _syllableId);
            }
        }

        private BudTimer _budTimer;
        public void SetSelect(SyllablePlayData data)
        {
            if (_budTimer!=null)
            {
                TimerManager.Inst.Stop(_budTimer);
                _budTimer = null;
            }
            _budTimer = TimerManager.Inst.RunOnce("CommonSyllableItem"+_syllableId, data.Length>0.5f?data.Length:0.5f,
                () =>
                {
                    if (this&&!isClick)
                    {
                        SetSelectState(false);
                    }
                });
            SetSelectState(true);
        }
    }
}
