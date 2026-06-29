using System;
using System.Collections.Generic;
using Game.AssetToolBox;
using GameData.Base;
using GameData.BaseInfo;
using Network.Http;
using UI.BaseWidgets;
using UnityEngine;

namespace Game.AINPCStudio
{
    public class AINpcPurchasedItem : MonoBehaviour
    {
        public CButton Btn_Select;
        public CButton Btn_Create;
        public GameObject Go_InfoView;
        public GameObject Go_CreateView;
        public GameObject Go_Selected;

        //Data
        private AINpcPurchasedItemData _curData;
        private Action<AINpcPurchasedItemData> _onClickAct;

        public void InitSelectMode(AINpcPurchasedItemData data, Action<AINpcPurchasedItemData> act)
        {
            this._curData = data;
            this._onClickAct = act;
            
            Btn_Select.onClick.RemoveAllListeners();
            Btn_Select.onClick.AddListener(OnSelectBtnClick);
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
    }
}
