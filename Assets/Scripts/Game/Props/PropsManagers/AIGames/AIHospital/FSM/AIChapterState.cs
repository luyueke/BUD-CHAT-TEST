using System;
using Game.Avatar;
using Game.Base;
using UnityEngine;

namespace Game.Props.PropsManagers.AIGames.AIHospital.FSM
{
    /// <summary>
    /// AI章节状态基类，定义所有章节状态的基本接口
    /// </summary>
    public abstract class AIChapterState
    {
        protected AIHospital_CharacterBehaviour _character;
        protected AIHospital_CharacterManager _characterManager;
        protected AIHospital_CharacterUtils _characterUtils;
        protected AIHospital_ChapterData _chapterData;
        protected string _npcId;

        protected LocationType _curLocationType;
        protected ActionType _curActionType;

        public AIChapterState(AIHospital_CharacterBehaviour character, AIHospital_ChapterData data)
        {
            this._character = character;
            this._characterManager = AIHospital_CharacterManager.Inst;
            this._characterUtils = AIHospital_CharacterUtils.Inst;
            this._npcId = this._character.GetNpcID();

            this._chapterData = data;
            if (this._chapterData != null)
            {
                this._curLocationType = (LocationType)data.location;
                this._curActionType = (ActionType)data.action;
            }
            else
            {
                this._curLocationType = LocationType.None;
                this._curActionType = ActionType.None;
            }
        }

        /// <summary>
        /// 进入状态时调用
        /// </summary>
        public virtual void OnEnter()
        {
            _character._npcStateController.ExitState(PlayerState.SingleEmote, false);
            _character._npcStateController.ExitState(PlayerState.DoubleEmote, false);
        }

        /// <summary>
        /// 退出状态时调用
        /// </summary>
        public virtual void OnExit()
        {
            _character._npcStateController.ExitState(PlayerState.SingleEmote, false);
            _character._npcStateController.ExitState(PlayerState.DoubleEmote, false);
            _character.StopMoving();
        }

        //被打断
        public virtual void OnInterrupt()
        {
            _character._npcStateController.ExitState(PlayerState.SingleEmote, false);
            _character._npcStateController.ExitState(PlayerState.DoubleEmote, false);
            _character.StopMoving();
        }

        /// <summary>
        /// 状态更新时调用
        /// </summary>
        /// <param name="deltaTime">帧间隔时间</param>
        public abstract void OnUpdate(float deltaTime);
    }
} 