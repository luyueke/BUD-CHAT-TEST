using System;
using System.Collections;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.Util.IO;
using GameData.BaseInfo;
using UI.BaseWidgets;
using UnityEngine;

namespace Game.AINPCStudio
{
    public class PgcNpcSelectItem : MonoBehaviour
    {
        public CButton Btn_Select;
        public RemoteImageBehaviour Rmote_Cover;
        public GameObject Go_Selected;
        
        private AINpcInfo _npcInfo;
        private Action<AINpcInfo> _onSelectedAct;

        private void Awake()
        {
            Btn_Select.onClick.AddListener(OnBtnSelected);
        }

        public void InitData(AINpcInfo npcInfo, Action<AINpcInfo> act)
        {
            this._npcInfo = npcInfo;
            this.Rmote_Cover.Load(npcInfo?.cover);
            this._onSelectedAct = act;
        }

        private void OnBtnSelected()
        {
            this._onSelectedAct?.Invoke(this._npcInfo);
            SetSelectState(true);
        }
        
        public void SetSelectState(bool isSelect)
        {
            Go_Selected.SetActive(isSelect);
        }
    }
}