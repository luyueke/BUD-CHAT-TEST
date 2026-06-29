using System;
using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace AIGame.Base
{
    public class AIParkBgmSelectBtn : MonoBehaviour
    {
        public CButton Btn_Select;
        public CButton Btn_DeleteUgcBgm;
        public Text Txt_UgcBgmName;
        public GameObject Go_Selected;
        public AIHospitalBgmType _curType;

        public string OfficalName;

        public string MusciName;

        private int _audioLimit = 120;
        private Action<string, string, AIParkBgmSelectBtn> _onSelectBgm;

        [HideInInspector]public string bgmUrl;
        private void Awake()
        {
            Btn_Select.onClick.AddListener(OnBtnSelectClick);
            if (Btn_DeleteUgcBgm != null)
            {
                Btn_DeleteUgcBgm.onClick.AddListener(OnDeleteUgcBgmClick);
            }
        }

        public void SetData(string bgmName, string bgmUrl, Action<string, string, AIParkBgmSelectBtn> act)
        {
            this.bgmUrl = bgmUrl;
            this._onSelectBgm = act;
            switch (_curType)
            {
                case AIHospitalBgmType.OfficalBgm:
                    //Txt_UgcBgmName.text = "BGM" + OfficalName;
                    break;
                case AIHospitalBgmType.UploadUgcBgm:
                    Txt_UgcBgmName.text = "从本地资源提取";
                    break;
                case AIHospitalBgmType.UploadedUgcBgm:
                    Txt_UgcBgmName.text = bgmName;
                    break;
            }
        }

        public void SetSelectState(bool isSelected)
        {
            Go_Selected.SetActive(isSelected);
        }

        public bool GetSelectState()
        {
            return Go_Selected.activeSelf;
        }

        private void OnBtnSelectClick()
        {
            switch (_curType)
            {
                case AIHospitalBgmType.OfficalBgm:
                    this._onSelectBgm?.Invoke(OfficalName, "",this);
                    break;
                case AIHospitalBgmType.UploadUgcBgm:
                    OnBtnUploadClick();
                    break;
            }
        }

        private void OnBtnUploadClick()
        {
            bool isCancel = false;
            Action onCancel = () =>
            {
                isCancel = true;
            };
            var panel = UIManager.Inst.OpenPanel<BgMusicUploadingPanel>(PanelId.BgMusicUploadingPanel, onCancel);
            panel.SetTopColor("#A883FF");
            AlbumUtils.Inst.UploadMusic(_audioLimit, (remoteUrl) =>
            {
                if (isCancel)
                {
                    return;
                }
                if (!string.IsNullOrEmpty(remoteUrl))
                {
                    OnUploadSyllableSuccess(remoteUrl);
                }
                UIManager.Inst.ClosePanel(PanelId.BgMusicUploadingPanel);
            }, err =>
            {
                UIManager.Inst.ClosePanel(PanelId.BgMusicUploadingPanel);
            });
        }

        private void OnUploadSyllableSuccess(string bgMusicUrl)
        {
            var bgName = "提取音乐 " + DataUtil.GetUtcTimeStampAsSpan();
            this._onSelectBgm?.Invoke(bgName, bgMusicUrl,this);
        }

        private void OnDeleteUgcBgmClick()
        {
            this._onSelectBgm?.Invoke("", "",this);
        }
    }
}
