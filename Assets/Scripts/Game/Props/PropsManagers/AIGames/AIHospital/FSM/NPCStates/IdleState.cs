using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Props.PropsManagers.AIGames.AIHospital.FSM
{
    /// <summary>
    /// 默认的空闲状态，用于未知的ActionType类型
    /// </summary>
    public class IdleState : AIChapterState
    {
        public IdleState(AIHospital_CharacterBehaviour character, AIHospital_ChapterData data) : base(character, data) { }

        public override void OnEnter()
        {
            base.OnEnter();
            LoggerUtils.Log($"[IdleState] {_character.GetNpcName()} 进入空闲状态");
        }

        public override void OnExit()
        {
            base.OnExit();
            LoggerUtils.Log($"[IdleState] {_character.GetNpcName()} 退出空闲状态");
        }

        public override void OnUpdate(float deltaTime)
        {
        }
    }
} 