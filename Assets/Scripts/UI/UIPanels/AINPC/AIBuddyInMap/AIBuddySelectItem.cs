using System;
using System.Collections.Generic;
using Game.AssetToolBox;
using GameData.Base;
using GameData.BaseInfo;
using Network.Http;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace Game.AINPCStudio
{
    public class AIBuddySelectItem : MonoBehaviour
    {
        public CButton Btn_Select;
        // public CButton Btn_Create;
        // public GameObject Go_InfoView;
        // public GameObject Go_CreateView;
        public GameObject Go_Selected;
        public Text Text_Name;

        //Data
        private AIBuddySelectItemData _curData;
        private Action<AIBuddySelectItemData> _onClickAct;

        public void InitSelectMode(AIBuddySelectItemData data, Action<AIBuddySelectItemData> act)
        {
            this._curData = data;
            this._onClickAct = act;

            Text_Name.text = data.ugcInfo.name;
            
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
            //Go_Selected.SetActive(isSelect);
        }
    }
}
