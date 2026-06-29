using System;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.Util.IO;
using Game.AssetToolBox;
using Game.Avatar;
using GameData.Account;
using GameData.Base;
using GameData.BaseInfo;
using Network.Http;
using UI.BaseWidgets;
using UnityEngine;

namespace Game.AINPCStudio
{
    public class AIBuddyOwnedItem : MonoBehaviour
    {
        public CButton Btn_Select;
        public CButton Btn_UnSelect;
        public GameObject GO_SelectView;
        public GameObject Go_UnSelectView;
        public GameObject Go_Selected;
        public RemoteImageBehaviour Remote_Cover;

        //Data
        private AIBuddyInfo _curData;
        private Action<AIBuddyInfo> _onClickAct;

        public void InitSelectMode(AIBuddyInfo data, Action<AIBuddyInfo> act)
        {
            this._curData = data;
            this._onClickAct = act;
            
            Btn_Select.onClick.RemoveAllListeners();
            Btn_Select.onClick.AddListener(OnSelectBtnClick);
            
            Remote_Cover.Load(this._curData?.npc?.cover);
            
            GO_SelectView.SetActive(true);
            Go_UnSelectView.SetActive(false);
        }

        public void InitUnSelectedMode(AIBuddyInfo data, Action<AIBuddyInfo> act)
        {
            this._curData = data;
            this._onClickAct = act;
            
            Btn_UnSelect.onClick.RemoveAllListeners();
            Btn_UnSelect.onClick.AddListener(UnSelectedNpc);
            Go_UnSelectView.SetActive(true);
            GO_SelectView.SetActive(false);
        }
        
        private void OnSelectBtnClick() {
            if (_curData == null) {
                return;
            }
            _onClickAct?.Invoke(_curData);
        }

        public void SetSelectState(bool isSelect)
        {
            Go_Selected.SetActive(isSelect);
        }

        private void UnSelectedNpc()
        {
            if (AIBuddyAvatarController.Inst.SelfStateController != null && AIBuddyAvatarController.Inst.SelfStateController.IsInDoubleEmote())
            {
                TipPanel.ShowToast("请先解除双人动作");
                return;
            }
            
            if (AvatarController.Inst.SelfStateController.IsInLinkAIBuddy())
            {
                TipPanel.ShowToast("请先解除牵手状态");
                return;
            }
            
            GameAIBuddyManager.Inst.ExitSelfAIBuddy();
            _onClickAct?.Invoke(null);
        }
    }
}
