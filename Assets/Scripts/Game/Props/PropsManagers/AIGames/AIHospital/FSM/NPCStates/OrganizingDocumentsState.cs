using System;
using System.Collections;
using System.Collections.Generic;
using Game.Base;
using Game.Props.PropsBehaviours;
using UnityEngine;

namespace Game.Props.PropsManagers.AIGames.AIHospital.FSM
{
    //ActionType.OrganizingDocuments
    public class OrganizingDocumentsState : AIChapterState
    {
        protected bool _hasReachedTarget = false;
        public OrganizingDocumentsState(AIHospital_CharacterBehaviour character, AIHospital_ChapterData data) : base(character, data) { }
        private AIHospital_FileCabinetBehaviour _curCabinetBev;

        public override void OnEnter()
        {
            base.OnEnter();
            LoggerUtils.Log($"[OrganizingDocumentsState] {_character.GetNpcName()} 进入整理文件状态");
            
            //1.获取当前行为的目标地点
            _hasReachedTarget = false;
            var transData = this._characterUtils.GetBehaviourLocation(_curLocationType, _curActionType);
            
            //2.开始移动
            _character.MoveToPosition(transData.pos, OnReachedTarget);
        }

        public override void OnExit()
        {
            base.OnExit();
            LoggerUtils.Log($"[OrganizingDocumentsState] {_character.GetNpcName()} 退出整理文件状态");
            
            if(_curCabinetBev != null)
            {
                _curCabinetBev.IsCanClick = true;
            }
        }

        public override void OnUpdate(float deltaTime)
        {
        }

        private void OnReachedTarget()
        {
            // LoggerUtils.LogError("OrganizingDocumentsState OnReachedTarget");
            _hasReachedTarget = true;
            
            switch (_curLocationType)
            {
                //院长室
                case LocationType.DirectorsOffice:
                    var wardCabinetMgr = GlobalNodeManager.Inst.Get<AIHospital_DeanCabinetManager>();
                    _curCabinetBev = wardCabinetMgr.GetCabinetBev();
                    _curCabinetBev.IsCanClick = false;
                    _curCabinetBev.HandOpenDoor();
                    _character.SetPositionAndRotation(_curCabinetBev.pos, Quaternion.Euler(_curCabinetBev.rot));
                    _character.PlayAnim("40200465");
                    break;
                
                //药房
                case LocationType.Ward:
                    var officeCabinetMgr = GlobalNodeManager.Inst.Get<AIHospital_FileCabinetManager>();
                    _curCabinetBev = officeCabinetMgr.GetCabinetBev();
                    _curCabinetBev.IsCanClick = false;
                    _curCabinetBev.HandOpenDoor();
                    _character.SetPositionAndRotation(_curCabinetBev.pos, Quaternion.Euler(_curCabinetBev.rot));
                    _character.PlayAnim("40200465");
                    break;
            }
        }
    }
} 