using System;
using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace AIGame.Base
{
    public enum AIHospitalBgmType
    {
        OfficalBgm = 0,
        UploadUgcBgm = 1,
        UploadedUgcBgm = 2,
    }
    public class AIHospitalBgmSelectBtn : MonoBehaviour
    {
        public CButton Btn_Select;
        public CButton Btn_DeleteUgcBgm;
        public Text Txt_UgcBgmName;
        public GameObject Go_Selected;
        public AIHospitalBgmType _curType;
        
        private int _audioLimit = 120;
        private Action<string, string> _onSelectBgm;

        private void Awake()
        {
            Btn_Select.onClick.AddListener(OnBtnSelectClick);
            if (Btn_DeleteUgcBgm != null)
            {
                Btn_DeleteUgcBgm.onClick.AddListener(OnDeleteUgcBgmClick);
            }
        }
        
        public void SetData(string bgmName, string bgmUrl, Action<string, string> act)
        {
            this._onSelectBgm = act;
            switch (_curType)
            {
                case AIHospitalBgmType.OfficalBgm:
                    Txt_UgcBgmName.text = "官方背景音";
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

        private void OnBtnSelectClick()
        {
            switch (_curType)
            {
                case AIHospitalBgmType.OfficalBgm:
                    this._onSelectBgm?.Invoke("", AIHospitalUtils.AIHospital_Offical_Bgm);
                    break;
                case AIHospitalBgmType.UploadUgcBgm:
                    OnBtnUploadClick();
                    break;
            }
        }
        
        private void OnBtnUploadClick()
        {
            bool isCancel = false;
            Action onCancel = ()=>{
                isCancel = true;
            };
            var panel = UIManager.Inst.OpenPanel<BgMusicUploadingPanel>(PanelId.BgMusicUploadingPanel, onCancel);
            panel.SetTopColor("#68CCBE");
            AlbumUtils.Inst.UploadMusic(_audioLimit, (remoteUrl) => {
                if (isCancel) {
                    return;
                }
                if (!string.IsNullOrEmpty(remoteUrl)) {
                    OnUploadSyllableSuccess(remoteUrl);
                }
                UIManager.Inst.ClosePanel(PanelId.BgMusicUploadingPanel);
            }, err => {
                UIManager.Inst.ClosePanel(PanelId.BgMusicUploadingPanel);
            });
        }
        
        private void OnUploadSyllableSuccess(string bgMusicUrl)
        {
            var bgName = "提取音乐 " + DataUtil.GetUtcTimeStampAsSpan();
            this._onSelectBgm?.Invoke(bgName, bgMusicUrl);
        }

        private void OnDeleteUgcBgmClick()
        {
            this._onSelectBgm?.Invoke("","");
        }
    }
}
