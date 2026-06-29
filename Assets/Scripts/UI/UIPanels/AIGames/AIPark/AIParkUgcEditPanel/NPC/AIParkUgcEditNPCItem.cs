using System;
using System.Collections;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.Util.IO;
using Game.AINPCStudio;
using GameData.BaseInfo;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.BaseWidgets;
using UnityEngine;

namespace AIGame.Base
{
    public class AIParkUgcEditNPCItem : MonoBehaviour
    {
        [Header("SlectedContent")]
        public GameObject SlectedContent;
        public RemoteImageBehaviour RM_Cover;
        public CButton Btn_Delete;
        public CButton Btn_Edit;

        public GameObject Btn_Edit_On;

        [Header("UnSelectedContent")]
        public GameObject UnSelectedContent;
        public CButton Btn_AddNpc;

        private Func<string,bool> canSelect;

        private AICommonGameConfig_NPC _curData;
        private string _curMapId;

        private AIParkUgcEditNPC _root;
        private void Awake()
        {
            Btn_Delete.onClick.AddListener(OnBtnDeleteClick);
            Btn_Edit.onClick.AddListener(OnBtnEditClick);
            Btn_AddNpc.onClick.AddListener(OnBtnAddNpcClick);
        }

        public void InitData(AIParkUgcEditNPC root, string mapId, Func<string, bool> canSelect)
        {
            _root = root;
            this._curMapId = mapId;
            this.canSelect = canSelect;
        }

        public void SetData(AICommonGameConfig_NPC data)
        {
            this._curData = data;
            gameObject.SetActive(true);
            Btn_Edit_On.gameObject.SetActive(false);
            SetDisplay();
        }

        public AICommonGameConfig_NPC GetNpcData()
        {
            return _curData;
        }

        private void SetDisplay()
        {
            if (_curData == null)
            {
                SlectedContent.SetActive(false);
                UnSelectedContent.SetActive(true);
            }
            else
            {
                SlectedContent.SetActive(true);
                UnSelectedContent.SetActive(false);
                InitSelectedContent();
            }
        }

        private void InitSelectedContent()
        {
            if (this._curData == null)
                return;

            RM_Cover.Load(_curData.cover);
        }

        private void OnBtnDeleteClick()
        {
            this._curData = null;
            SetDisplay();
            _root.AddNpc(null);
            Btn_Edit_On.gameObject.SetActive(false);
            gameObject.SetActive(false);
        }

        public void OnBtnEditClick()
        {
            if (!Btn_Edit_On.gameObject.activeSelf)
            {
                Btn_Edit_On.gameObject.SetActive(true);
                _root.ClickNpc(this);
            }
            //var panel = UIManager.Inst.OpenPanel<AIHospitalNpcInfoCard>(PanelId.AIHospitalNpcInfoCard, _curData);
            //panel.SetFinishAct((data =>
            //{
            //    this._curData = data;
            //}));
        }

        private void OnBtnAddNpcClick()
        {
            NpcStoreEnterData enterData = new NpcStoreEnterData();
            enterData.EnterType = NpcStoreEnterType.SelectNPC;
            enterData.OnSelectNpcAct = OnSelectNpc;
            enterData.CanSelect = this.canSelect;

            UIManager.Inst.OpenPanel<AINpcStorePanel>(PanelId.AINpcStorePanel, enterData);
        }

        private void OnSelectNpc(AINpcInfo npcInfo)
        {
            LoggerUtils.Log("选择了 NPC : ", npcInfo.id);

            var data = new AICommonGameConfig_NPC();
            data.id = npcInfo.id;
            //data.role = (int)_curNpcType;
            data.name = npcInfo.name;
            data.cover = npcInfo.cover;
            data.desc = npcInfo.desc;
            data.npcAvatarJson = npcInfo.npcAvatarJson;

            _root.AddNpc(data);

            UIManager.Inst.ClosePanel(PanelId.AINpcStorePanel);
        }
    }
}