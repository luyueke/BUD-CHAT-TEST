using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Es;
using Game.Avatar;
using Game.Base;
using GameData.PgcData;
using Message;
using Pb.Base;
using UnityEngine;

namespace Game.Props.PropsManagers.AIGames.AIHospital.FSM
{
    //ActionType.TalkWithPlayer
    public class TalkWithPlayerState : AIChapterState
    {
        public TalkWithPlayerState(AIHospital_CharacterBehaviour character, AIHospital_ChapterData data = null) : base(character, data) { }
        private string _curPlayEmoteId;
        
        public override void OnEnter()
        {
            base.OnEnter();
            LoggerUtils.Log($"[TalkWithPlayerState] {_character.GetNpcName()} 进入与玩家交谈状态");
            
            MessageHelper.AddListener<EmoteNetData>(MessageName.SelfCancelEmote, OnSelfCancelEmote);
            //1.面朝玩家
            _character.StopMoving();
            _character.LookAtPlayer();
        }

        public override void OnExit()
        {
            base.OnExit();
            LoggerUtils.Log($"[TalkWithPlayerState] {_character.GetNpcName()} 退出与玩家交谈状态");
            MessageHelper.RemoveListener<EmoteNetData>(MessageName.SelfCancelEmote, OnSelfCancelEmote);
            _character.RevertLookAt();
        }


        public override void OnUpdate(float deltaTime)
        {
            
        }

        private void OnSelfCancelEmote(EmoteNetData netData)
        {
            var emoteId = netData.EmoteId;
            if (_curPlayEmoteId == emoteId)
            {
                var npcStateCtr = _character._npcStateController;
                npcStateCtr.ExitState(PlayerState.DoubleEmote);
                npcStateCtr.ExitState(PlayerState.SingleEmote);
            }
        }

        /// <summary>
        /// 播放动画
        /// </summary>
        /// <param name="emoteId"></param>
        /// <param name="bSelfActive">是否玩家主动进入状态</param>
        public void StartPlayNpcEmote(string emoteId,bool bSelfActive = false)
        {
            _curPlayEmoteId = emoteId;
            var emoteData = DataTables.GetEmoUIConfigList().Find((emoData) => emoData.pgcId == emoteId);
            if (emoteData.emoType == (int)EmoteSubType.Double || emoteData.emoType == (int)EmoteSubType.DoubleLoop)
            {
                var selfStateCtr = AvatarController.Inst.SelfStateController;
                var npcStateCtr = _character._npcStateController;
                npcStateCtr.EnterState(PlayerState.DoubleEmote, emoteId, !bSelfActive, AccountDataManager.Inst.Uid);
                selfStateCtr.EnterState(PlayerState.DoubleEmote, emoteId, bSelfActive, npcStateCtr.PlayerID);
            }
            else
            {
                if (bSelfActive)
                {
                    AvatarController.Inst.SelfStateController.EnterState(PlayerState.SingleEmote, emoteId, 0);
                }
                else
                    _character.PlayAnim(emoteId);
            }
        }
    }
} 