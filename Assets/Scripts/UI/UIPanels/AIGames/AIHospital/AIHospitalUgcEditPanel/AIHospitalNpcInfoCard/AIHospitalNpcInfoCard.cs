using System;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.Util.IO;
using GameData.BaseInfo;
using Newtonsoft.Json;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace AIGame.Base
{
    public class AIHospitalNpcInfoCard : BasePanel<AIHospitalNpcInfoCard>
    {
        [Header("基础UI")]
        public Transform BG;
        public CButton closeBtn;            // 关闭按钮
        public CButton saveBtn;             // 保存按钮
        [Header("具体的View")]
        public ProvostNpcInfoCard provostNpcInfoCard;
        public RunagateNpcInfoCard runagateNpcInfoCard;
        private HospitalNPCData _npcData;
        private Action<HospitalNPCData> _onFinishSetDataAct;
    
        public override void OnCreate()
        {
            base.OnCreate();
            AddListeners();
            InitBG();
        }

        public override void OnShow(params object[] args)
        {
            base.OnShow(args);
            _npcData = args[0] as HospitalNPCData;
            if (_npcData.role == (int)HospitalNPCType.Provost)
            {
                provostNpcInfoCard.InitData(_npcData);
            }
            else if (_npcData.role == (int)HospitalNPCType.Runagate)
            {
                runagateNpcInfoCard.InitData(_npcData);
            }   
        }

        private void InitBG(){
            if(BG == null){
                return;
            }
            string atlasPath = "Assets/Loadable/UI/UIPanel/CommonBgPanel/CommonBgIcon.spriteatlas";
            var itemObj = Loader
                .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
                .Instantiate(BG);
            var item = itemObj.GetComponent<ActivityCenterBgItem>();
            item.InitCustomBgItem("#FFFFFF", atlasPath, new List<string>()
            {
                "S9BgElement1_green", "S9BgElement2_green", "S9BgElement3_green"
            });
            item.gameObject.SetActive(true);
            item.transform.SetAsFirstSibling();
        }

        public void SetFinishAct(Action<HospitalNPCData> act)
        {
            this._onFinishSetDataAct = act;
        }

        private void AddListeners()
        {
            // 关闭按钮
            closeBtn.onClick.AddListener(CloseSelf);
            // 保存按钮
            saveBtn.onClick.AddListener(OnSaveBtnClick);
        }

        private void OnSaveBtnClick()
        {
            // 检查必填字段
            if (_npcData.role == (int)HospitalNPCType.Provost)
            {
                var persuadeType = (Hospital_Persuade_Type)_npcData.npcConfig.winCondition;
                if (persuadeType == Hospital_Persuade_Type.Answer)
                {
                    if (string.IsNullOrEmpty(_npcData.npcConfig.question) || string.IsNullOrEmpty(_npcData.npcConfig.answer))
                    {
                        TipPanel.ShowToast("请填写通关问题和参考答案");
                        return;
                    }
                }
            }
            
            
            // 保存方法将由用户补充
            SaveNpcInfo();
        }
        
        private void SaveNpcInfo()
        {
            // 此方法由用户补充实现
            CloseSelf();
            this._onFinishSetDataAct?.Invoke(_npcData);
        }
    }
}