using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Props.PropsManagers.AIGames.AIPark.FSM
{
    public class FountainState : AIChapterState
    {
        protected bool _hasReachedTarget = false;
        private ParkNpcTransData _targetPointData;
        private bool _isMoving = false;

        public FountainState(AIPark_CharacterBehaviour character, AIPark_ChapterData data) : base(character, data) { }

        public override void OnEnter()
        {
            base.OnEnter();
            LoggerUtils.Log($"[FountainState] {_character.GetNpcName()} 进入滑梯状态");

            _targetPointData = new ParkNpcTransData()
            {
                pos = new Vector3(10.1f, 0, 2.35f),
                rot = new Vector3(0, -75, 0),
            };
            MoveToTargetPoint();
        }

        public override void OnExit()
        {
            base.OnExit();
            LoggerUtils.Log($"[FountainState] {_character.GetNpcName()} 退出滑梯状态");
            CleanData();
        }

        public override void OnInterrupt()
        {
            base.OnInterrupt();
            CleanData();
        }

        public override void OnUpdate(float deltaTime)
        {
        }

        private void OnReachedTarget()
        {
            _isMoving = false;
            _hasReachedTarget = true;

            _character.SetPositionAndRotation(_targetPointData.pos, Quaternion.Euler(_targetPointData.rot));

            //todo 具体化动作
            _character.SetHandNodeActive(false); //手部动画是否隐藏
            _character.PlayAnim(""); // 播放对应动画ID
        }

        private void CleanData()
        {
            _isMoving = false;
            _hasReachedTarget = false;

            _character.SetHandNodeActive(true);
        }

        // private IEnumerator WaitAndMoveToNextPoint()
        // {
        //     // 等待5秒
        //     // yield return new WaitForSeconds(5.0f);
        //     // // 退出状态
        //     // _character._npcStateController.ExitState(PlayerState.SingleEmote);
        //     // yield return new WaitForSeconds(3.0f);

        // }

        private void MoveToTargetPoint()
        {
            _isMoving = true;
            _character.SetHandNodeActive(true);

            LoggerUtils.Log($"[FountainState] {_character.GetNpcName()} 移动到滑梯位置: {_targetPointData.pos}");

            _character.MoveToPosition(_targetPointData.pos, OnReachedTarget);
        }
    }
}