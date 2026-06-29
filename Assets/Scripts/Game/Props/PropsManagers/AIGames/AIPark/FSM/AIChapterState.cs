using System;
using Game.Avatar;
using Game.Base;
using UnityEngine;

namespace Game.Props.PropsManagers.AIGames.AIPark.FSM
{
    /// <summary>
    /// AI章节状态基类，定义所有章节状态的基本接口
    /// </summary>
    public abstract class AIChapterState
    {
        protected AIPark_CharacterBehaviour _character;
        protected AIPark_CharacterManager _characterManager;
        protected AIPark_CharacterUtils _characterUtils;
        protected AIPark_ChapterData _chapterData;
        protected string _npcId;

        protected LocationType _curLocationType;
        protected ActionType _curActionType;

        protected bool _reachTarget = false; //是否到达目标点 

        public AIChapterState(AIPark_CharacterBehaviour character, AIPark_ChapterData data)
        {
            this._character = character;
            this._characterManager = AIPark_CharacterManager.Inst;
            this._characterUtils = AIPark_CharacterUtils.Inst;
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
                this._curActionType = ActionType.Idle;
            }
        }

        /// <summary>
        /// 进入状态时调用
        /// </summary>
        public virtual void OnEnter()
        {
            if (_character._npcStateController != null)
            {
                            _character._npcStateController.ExitState(PlayerState.SingleEmote, false);
            _character._npcStateController.ExitState(PlayerState.DoubleEmote, false);
            }

            _reachTarget = false; //可能需要一个保底机制 确保reachTarget一定时间后为true
            _reachTarget = true; //
        }

        /// <summary>
        /// 退出状态时调用
        /// </summary>
        public virtual void OnExit()
        {
            if (_character._npcStateController != null)
            { 
            _character._npcStateController.ExitState(PlayerState.SingleEmote, false);
            _character._npcStateController.ExitState(PlayerState.DoubleEmote, false);
            }

            _character.StopMoving();
        }

        //被打断
        public virtual void OnInterrupt()
        {
            if (_character._npcStateController != null)
            { 
            _character._npcStateController.ExitState(PlayerState.SingleEmote, false);
            _character._npcStateController.ExitState(PlayerState.DoubleEmote, false);
            }

            _character.StopMoving();
        }

        /// <summary>
        /// 状态更新时调用
        /// </summary>
        /// <param name="deltaTime">帧间隔时间</param>
        public abstract void OnUpdate(float deltaTime);
    }
} 