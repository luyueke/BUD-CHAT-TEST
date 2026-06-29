using System;
using System.Collections;
using System.Collections.Generic;
using Es;
using Game.Avatar;
using Game.Base;
using GameData.PgcData;
using Message;
using Pb.Base;
using UnityEngine;

namespace Game.Props.PropsManagers.AIGames.AIHospital.FSM
{
    //ActionType.LinkEmote
    public class LinkEmoteState : AIChapterState
    {
        private string _curLinkEmoteId;
        private bool _isLinked;
        
        public LinkEmoteState(AIHospital_CharacterBehaviour character, AIHospital_ChapterData data = null) : base(character, data) 
        { 
            _isLinked = false;
        }
        
        public override void OnEnter()
        {
            base.OnEnter();
            LoggerUtils.Log($"[LinkEmoteState] {_character.GetNpcName()} 进入连接状态");
            
            // 添加消息监听
            MessageHelper.AddListener<EmoteNetData>(MessageName.SelfCancelEmote, OnSelfCancelEmote);
            
            //1.停止移动并面向玩家
            //_character.StopMoving();
            // _character.LookAtPlayer();
        }

        public override void OnExit()
        {
            base.OnExit();
            LoggerUtils.Log($"[LinkEmoteState] {_character.GetNpcName()} 退出连接状态");
            
            // 移除消息监听
            MessageHelper.RemoveListener<EmoteNetData>(MessageName.SelfCancelEmote, OnSelfCancelEmote);
            // 如果还在连接状态,需要解除连接
            if (_isLinked)
            {
                StopLinkEmote();
                _character.RecalculateChapter(3f);
            }
        }

        public override void OnUpdate(float deltaTime)
        {
            // 在连接状态下可能需要的更新逻辑
            if(_isLinked)
            {
                // 例如:更新位置跟随
                //UpdateFollowPosition();
            }
        }

        private void OnSelfCancelEmote(EmoteNetData netData)
        {
            if (_curLinkEmoteId == netData.EmoteId)
            {
                // 如果还在连接状态,需要解除连接
                if (_isLinked)
                {
                    StopLinkEmote();
                    _character.RecalculateChapter(3f);
                }
            }
        }

        /// <summary>
        /// 开始连接表情动作
        /// </summary>
        /// <param name="emoteId">表情动作ID</param>
        /// <param name="bSelfActive">是否玩家主动发起</param>
        public void StartLinkEmote(string emoteId, bool bSelfActive = false)
        {
            _curLinkEmoteId = emoteId;
            _isLinked = true;

            var selfStateCtr = AvatarController.Inst.SelfStateController;
            var npcStateCtr = _character._npcStateController;

            // 设置NPC和玩家的状态
            if(bSelfActive)
            {
                // 玩家发起的连接
                AIBuddyAvatarController.Inst.AddGameAIBuddyForHospital(_character._npcStateController);
                //这里相当于只调用了里面的follower脚本挂载,由于没有处理linkemotemanager，所以没有网络同步 就在UgcEmoView和EmoContentItem中的EnterEmote 强制调用了伙伴emote
                BuddyLinkEmoteManager.Inst.StartBind(AccountDataManager.Inst.Uid, emoteId);

                MessageHelper.Broadcast(MessageName.OnS9EmoteWithMsg, emoteId,_character.GetNpcName(),true);
                MessageHelper.Broadcast(MessageName.BuddyLinkEmoteStateChange, true);
            }
            else
            {
                // NPC发起的连接
                //BuddyLinkEmoteManager.Inst.StartBind(npcStateCtr.PlayerID, emoteId);
            }
        }

        /// <summary>
        /// 停止连接表情动作
        /// </summary>
        private void StopLinkEmote()
        {
            if(!_isLinked) return;

            _isLinked = false;
            var npcStateCtr = _character._npcStateController;

            if (npcStateCtr.emoteOPId == AccountDataManager.Inst.Uid)
            {
                MessageHelper.Broadcast(MessageName.BuddyLinkEmoteStateChange, false);
                MessageHelper.Broadcast(MessageName.OnS9EmoteWithMsg, _curLinkEmoteId, _character.GetNpcName(), false);
                //这里通过本地消息来退出，不用主动退出
                //BuddyLinkEmoteManager.Inst.StopBind(AccountDataManager.Inst.Uid);
                AIBuddyAvatarController.Inst.RemoveGameAIBuddyForHospital(_character._npcStateController);
            }

            // 清理状态
            _curLinkEmoteId = null;
        }

        /// <summary>
        /// 更新跟随位置
        /// </summary>
        private void UpdateFollowPosition()
        {
            if(!_isLinked) return;

            var selfPlayer = AvatarController.Inst.SelfStateController;
            if(selfPlayer != null)
            {
                // 获取动画配置中的偏移位置
                var aniConfig = BuddyLinkEmoteManager.Inst.GetAniConfig(_curLinkEmoteId, PlayAniType.LinkIdleB);
                if(aniConfig != null)
                {
                    // 更新NPC位置
                    BuddyLinkEmoteManager.Inst.SetPlayerOffsetPos(
                        _character._npcStateController.PlayerID,
                        aniConfig.interactPos,
                        aniConfig.interactRot
                    );
                }
            }
        }

        /// <summary>
        /// 检查是否可以进入连接状态
        /// </summary>
        public bool CanEnterLinkState()
        {
            // 检查距离
            float distanceToPlayer = Vector3.Distance(
                _character.transform.position,
                AvatarController.Inst.SelfStateController.transform.position
            );
            
            // 检查当前是否已经在其他连接状态
            bool isPlayerLinked = BuddyLinkEmoteManager.Inst.IsInBuddyLinkState(AccountDataManager.Inst.Uid);
            
            return distanceToPlayer <= 2f && !isPlayerLinked;
        }
    }
}
