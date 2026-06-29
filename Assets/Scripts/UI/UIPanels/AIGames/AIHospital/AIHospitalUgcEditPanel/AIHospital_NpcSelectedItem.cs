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
    public class AIHospital_NpcSelectedItem : MonoBehaviour
    {
        [Header("SlectedContent")] 
        public GameObject SlectedContent;
        public RemoteImageBehaviour RM_Cover;
        public CButton Btn_Delete;
        public CButton Btn_Edit;

        [Header("UnSelectedContent")] 
        public GameObject UnSelectedContent;
        public CButton Btn_AddNpc;

        [Header("LockContent")] 
        public GameObject LockContent;
        public CButton Btn_UnLockSolt;

        private HospitalNPCData _curData;
        private bool _isLockSolt = false;
        private HospitalNPCType _curNpcType;
        private DisplayType _curDisplayType = DisplayType.EmptySolt;
        private string _curMapId;
        private Func<string, bool> _canSelectNpc;
        
        private enum DisplayType
        {
            EmptySolt = 1,
            SelectedNPC = 2,
            LockSolt = 3,
        }

        private void Awake()
        {
            Btn_Delete.onClick.AddListener(OnBtnDeleteClick);
            Btn_Edit.onClick.AddListener(OnBtnEditClick);
            Btn_AddNpc.onClick.AddListener(OnBtnAddNpcClick);
            Btn_UnLockSolt.onClick.AddListener(OnBtnUnLockSoltClick);
        }

        public void InitData(Func<string, bool> canSelect, string mapId)
        {
            this._curMapId = mapId;
            this._canSelectNpc = canSelect;
        }

        public void SetData(HospitalNPCType npcType, HospitalNPCData data, bool isLockSolt = false)
        {
            this._curNpcType = npcType;
            this._curData = data;
            this._isLockSolt = isLockSolt;

            if (this._isLockSolt)
            {
                SetDisplay(DisplayType.LockSolt);
            }
            else
            {
                if (data != null)
                {
                    SetDisplay(DisplayType.SelectedNPC);
                }
                else
                {
                    SetDisplay(DisplayType.EmptySolt);
                }
            }
        }

        public HospitalNPCData GetNpcData()
        {
            return _curData;
        }

        private void SetDisplay(DisplayType displayType)
        {
            _curDisplayType = displayType;
            switch (displayType)
            {
                case DisplayType.EmptySolt:
                    //未解锁卡槽
                    SlectedContent.SetActive(false);
                    UnSelectedContent.SetActive(true);
                    LockContent.SetActive(false);
                    break;
                case DisplayType.SelectedNPC:
                    //解锁卡槽 且 有数据
                    SlectedContent.SetActive(true);
                    UnSelectedContent.SetActive(false);
                    LockContent.SetActive(false);

                    InitSelectedContent();
                    break;
                case DisplayType.LockSolt:
                    //解锁卡槽 且 无数据
                    SlectedContent.SetActive(false);
                    UnSelectedContent.SetActive(false);
                    LockContent.SetActive(true);
                    break;
            }
        }

        private void InitSelectedContent()
        {
            if(this._curData == null)
                return;
            
            RM_Cover.Load(_curData.cover);
        }

        private void OnBtnDeleteClick()
        {
            this._curData = null;
            SetDisplay(DisplayType.EmptySolt);
        }

        private void OnBtnEditClick()
        {
            if (this._curData == null)
            {
                LoggerUtils.LogError("OnBtnEditClick this._curData == null");
                return;
            }

            var panel = UIManager.Inst.OpenPanel<AIHospitalNpcInfoCard>(PanelId.AIHospitalNpcInfoCard, _curData);
            panel.SetFinishAct((data =>
            {
                this._curData = data;
            }));
        }

        private void OnBtnAddNpcClick()
        {
            NpcStoreEnterData enterData = new NpcStoreEnterData();
            enterData.EnterType = NpcStoreEnterType.SelectNPC;
            enterData.OnSelectNpcAct = OnSelectNpc;
            enterData.CanSelect = this._canSelectNpc;

            UIManager.Inst.OpenPanel<AINpcStorePanel>(PanelId.AINpcStorePanel, enterData);
        }
        
        private void OnSelectNpc(AINpcInfo npcInfo)
        {
            LoggerUtils.Log("选择了 NPC : ", npcInfo.id);
            
            _curData = new HospitalNPCData();
            _curData.id = npcInfo.id;
            _curData.role = (int)_curNpcType;
            _curData.name = npcInfo.name;
            _curData.cover = npcInfo.cover;

            _curData.npcConfig = new NPCConfig();
            if (_curNpcType == HospitalNPCType.Provost)
            {
                _curData.npcConfig.winCondition = (int)Hospital_Persuade_Type.Persuade;
            }
            
            SetDisplay(DisplayType.SelectedNPC);
            
            UIManager.Inst.ClosePanel(PanelId.AINpcStorePanel);
            OnBtnEditClick();
        }

        private void OnBtnUnLockSoltClick()
        {
            CommonConfirmPanel commonConfirmPanel = UIManager.Inst.OpenPanel<CommonConfirmPanel>(PanelId.CommonConfirmPanel);
            commonConfirmPanel.SetThemeColor("#68CCBE", "#905CFF", "#68D896");
            commonConfirmPanel.SetLocalText("", "是否花费10钻石解锁监管者/逃亡者卡位？", "确认", "取消");
            commonConfirmPanel.SetOnClickAction(() =>
            {
                commonConfirmPanel.SetConfirmLoadingVisible(true);
                var needNum = 10;
                CurrencyType bType = CurrencyType.Gem;
                int selfCount = AccountDataManager.Inst.BalanceInfo.GetAccountCount(bType);
                if (needNum > selfCount)
                {
                    UIManager.Inst.OpenPanel(PanelId.GetMoreGemsPanel, needNum);
                    commonConfirmPanel.CloseSelf();
                }
                else
                {
                    AIHospitalUtils.Inst.BuyAIHospitalSlot(_curMapId, _curNpcType, (int)BUDProductType.AIHospitalSlot,() =>
                    {
                        SetDisplay(DisplayType.EmptySolt);
                        commonConfirmPanel.CloseSelf();
                    }, () =>
                    {
                        commonConfirmPanel.CloseSelf();
                    });
                }
            }, () =>
            {
                commonConfirmPanel.CloseSelf();
            });
        }
    }
}