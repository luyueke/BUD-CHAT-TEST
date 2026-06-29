using System.Collections;
using System.Collections.Generic;
using GameData.BaseInfo;
using UnityEngine;


namespace AIGame.Base
{
    public class NpcSettingContent : SettingContentBase
    {
        public Transform Trans_ProvostContent;
        public Transform Trans_RunagateContent;
        public AIHospital_NpcSelectedItem Item_Prefab;

        private const int _freeProvostSoltCount = 2;
        private const int _provostCount = 7;
        private const int _freeRunagateSoltCount = 1;
        private const int _runagateCount = 5;
        private List<AIHospital_NpcSelectedItem> _provostNPCList = new List<AIHospital_NpcSelectedItem>();
        private List<AIHospital_NpcSelectedItem> _runagateNPCList = new List<AIHospital_NpcSelectedItem>();

        
        public override void InitData(MapInfo mapInfo, EditType editType)
        {
            base.InitData(mapInfo, editType);
            InitItems();
            SetNpcItemData();
        }

        public override void SaveData()
        {
            base.SaveData();
            var hospitalNPCs = new List<HospitalNPCData>();
            _provostNPCList.ForEach((provost) =>
            {
                if (provost.GetNpcData() != null)
                {
                    hospitalNPCs.Add(provost.GetNpcData());
                }
            });
            _runagateNPCList.ForEach((runagate) =>
            {
                if (runagate.GetNpcData() != null)
                {
                    hospitalNPCs.Add(runagate.GetNpcData());
                }
            });

            this.curMapInfo.gameSetting.aIGameConfig.hospitalNPCs = hospitalNPCs;
        }

        private void InitItems()
        {
            for (int i = 0; i < _provostCount; i++)
            {
                var item = GameObject.Instantiate(Item_Prefab, Trans_ProvostContent);
                item.InitData(CheckCanSelectNpc, this.curMapInfo.id);
                item.SetData(HospitalNPCType.Provost, null);
                _provostNPCList.Add(item);
                item.gameObject.SetActive(true);
            }
            
            for (int i = 0; i < _runagateCount; i++)
            {
                var item = GameObject.Instantiate(Item_Prefab, Trans_RunagateContent);
                item.InitData(CheckCanSelectNpc, this.curMapInfo.id);
                item.SetData(HospitalNPCType.Runagate, null);
                _runagateNPCList.Add(item);
                item.gameObject.SetActive(true);
            }
        }

        private void SetNpcItemData()
        {
            if(this.curMapInfo.gameSetting.aIGameConfig.hospitalNPCs == null)
                return;

            //购买的监管者NPC卡位数
            var purchasedRegulatorAmount = this.curMapInfo.gameSetting.aIGameConfig.purchasedRegulatorAmount;
            //购买的逃亡者NPC卡位数
            var purchasedFugitiveAmount = this.curMapInfo.gameSetting.aIGameConfig.purchasedFugitiveAmount;
            
            var hospitalNPCs = this.curMapInfo.gameSetting.aIGameConfig.hospitalNPCs;

            var remoteProvostNPCList = new List<HospitalNPCData>();
            var remoteRunagateNPCList = new List<HospitalNPCData>();
            hospitalNPCs.ForEach(x =>
            {
                if (x.role == (int)HospitalNPCType.Provost)
                {
                    remoteProvostNPCList.Add(x);
                }
                if (x.role == (int)HospitalNPCType.Runagate)
                {
                    remoteRunagateNPCList.Add(x);
                }
            });
            
            //1.先设置NPC数据
            for (int i = 0; i < remoteProvostNPCList.Count; i++)
            {
                _provostNPCList[i].SetData(HospitalNPCType.Provost, remoteProvostNPCList[i]);
            }

            for (int i = 0; i < remoteRunagateNPCList.Count; i++)
            {
                _runagateNPCList[i].SetData(HospitalNPCType.Runagate, remoteRunagateNPCList[i]);
            }

            //2.设置锁定卡位
            var provostUnlockCount = _freeProvostSoltCount + purchasedRegulatorAmount;
            for (int i = 0; i < _provostNPCList.Count; i++)
            {
                if (i < provostUnlockCount)
                    continue;
                else
                    _provostNPCList[i].SetData(HospitalNPCType.Provost, null, true);
            }
            
            var runagateUnlockCount = _freeRunagateSoltCount + purchasedFugitiveAmount;
            for (int i = 0; i < _runagateNPCList.Count; i++)
            {
                if (i < runagateUnlockCount)
                    continue;
                else
                    _runagateNPCList[i].SetData(HospitalNPCType.Runagate, null, true);
            }
        }
        
        private bool CheckCanSelectNpc(string npcId){
            var hospitalNPCs = new List<HospitalNPCData>();
            _provostNPCList.ForEach((provost) =>
            {
                if (provost.GetNpcData() != null)
                {
                    hospitalNPCs.Add(provost.GetNpcData());
                }
            });
            _runagateNPCList.ForEach((runagate) =>
            {
                if (runagate.GetNpcData() != null)
                {
                    hospitalNPCs.Add(runagate.GetNpcData());
                }
            });

            if (hospitalNPCs.Find(x => x.id == npcId) != null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}