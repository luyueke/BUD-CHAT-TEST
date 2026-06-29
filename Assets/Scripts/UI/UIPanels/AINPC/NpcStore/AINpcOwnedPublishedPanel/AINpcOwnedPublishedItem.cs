using System;
using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UnityEngine;

namespace Game.AINPCStudio
{
    public class AINpcOwnedPublishedItem : MonoBehaviour
    {
        public CButton Btn_Select;
        public CButton Btn_Create;
        public GameObject Go_CreateView;
        public GameObject Go_SelectedView;
        public GameObject Go_Selected;

        //Data
        private DraftListItem _curData;
        private Action<DraftListItem> _onClickAct;

        public void InitSelectMode(DraftListItem data, Action<DraftListItem> act)
        {
            this._curData = data;
            this._onClickAct = act;
            
            Btn_Select.onClick.RemoveAllListeners();
            Btn_Select.onClick.AddListener(OnSelectBtnClick);
            
            Go_Selected.SetActive(false);
            Go_SelectedView.SetActive(true);
            Go_CreateView.SetActive(false);
        }
        
        private void OnSelectBtnClick() {
            if (_curData == null) {
                return;
            }
            _onClickAct?.Invoke(_curData);
           // SetSelectState(true);
        }
        
        public void SetSelectState(bool isSelect)
        {
            Go_Selected.SetActive(isSelect);
        }

        public void InitCreateMode()
        {
            Btn_Create.onClick.RemoveAllListeners();
            Btn_Create.onClick.AddListener(OnBtnCreateClick);
            
            Go_Selected.SetActive(false);
            Go_SelectedView.SetActive(false);
            Go_CreateView.SetActive(true);
        }

        private void OnBtnCreateClick()
        {
            UIManager.Inst.OpenPanel<AINpcStudioMainPanel>(PanelId.AINpcStudioMainPanel);
        }
    }
}