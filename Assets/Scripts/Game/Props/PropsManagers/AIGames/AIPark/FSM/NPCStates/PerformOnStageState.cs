using Es;
using Game.Avatar;
using Game.Base;
using Game.Props.PropsBehaviours;
using GameData.PgcData;
using Message;
using NetEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UIAgent;
using UnityEngine;
using UnityEngine.TextCore.Text;

namespace Game.Props.PropsManagers.AIGames.AIPark.FSM
{
    public class PerformOnStageState : AIChapterState
    {
        protected bool _hasReachedTarget = false;
        private ParkNpcTransData _targetPointData;
        private bool _isMoving = false;
        private AIPark_StageBehaviour _behaviour;

        [HideInInspector] public AIPark_InstrumentBehaviour InstrumentBehaviour;

        public PerformOnStageState(AIPark_CharacterBehaviour character, AIPark_ChapterData data) : base(character, data) { }

        public override void OnEnter()
        {
            base.OnEnter();
            LoggerUtils.Log($"[PerformOnStageState] {_character.GetNpcName()} 进入舞台状态");

            //_targetPointData = new ParkNpcTransData()
            //{
            //    pos = new Vector3(8.4f, 0, -4.6f),
            //    rot = new Vector3(0, -161, 0),
            //};
            //MoveToTargetPoint();
            OnReachedTarget();
        }

        public override void OnExit()
        {
            base.OnExit();
            LoggerUtils.Log($"[PerformOnStageState] {_character.GetNpcName()} 退出舞台状态");
            var behaviour = _chapterData.nodeBaseBehaviour as AIPark_StageBehaviour;
            if (behaviour != null)
            {
                behaviour.OnPropPosFree(_character._npcKccCtr);
            }

            var playerHoldBehaviour = _character.playerStateControllerState.GetComponentInChildren<PlayerHoldBehaviour>(true);

            playerHoldBehaviour.ExitPlayInstrument(true, () =>
            {
                playerHoldBehaviour.SetActiveMusic(false);
            });

            var pos = _character.gameObject.transform.position;
            // pos.y = 0;
            var _npcTransData = AIPark_CharacterUtils.Inst.GetNearTransData(pos, 2f, 4f);
            _character.SetPositionAndRotation(_npcTransData.pos, Quaternion.Euler(_npcTransData.rot));

            //_character.playerStateControllerState.DirectIntoState(PlayerState.Default);

            CleanData();
        }

        public override void OnUpdate(float deltaTime)
        {



        }

        private void OnReachedTarget()
        {
            _isMoving = false;
            _hasReachedTarget = true;

            if (_chapterData.nodeBaseBehaviour == null)
            {
                _behaviour = GlobalNodeManager.Inst.Get<AIPark_StageManager>().GetBehaviour();
            }
            else
            {
                _behaviour = _chapterData.nodeBaseBehaviour as AIPark_StageBehaviour;
            }

            if (_behaviour == null)
            {
                LoggerUtils.LogError($"[PerformOnStageState] {_character.GetNpcName()} 舞台行为为空");
                AIParkPropsManager.Inst.ExitAction(_character.GetNpcID(), ActionType.PerformOnStage);
                return;
            }

            //暂时注释
            var playerHoldBehaviour = _character.playerStateControllerState.GetComponentInChildren<PlayerHoldBehaviour>(true);
            var idx = _behaviour.AddUsePropCharacterMsg(_character._npcKccCtr, _character._npcKccCtr.Motor, _character.playerStateControllerState);
            _behaviour.SetNpc2(idx, _character.playerStateControllerState.PlayerID);
            _behaviour.OnTouchClick();

            //behaviour.GetMusicalSync(behaviour._currentUsedCount - 1,(_musicalSyncParam) => 
            //{
            //    //乐器动作
            //    var config = DataTables.GetInstrumentConfig(_musicalSyncParam.resId);
            //    if (config != null)
            //    {
            //        _character.playerStateControllerState.Wrap.ChangePart(UniqueType.GetAvatar(AvatarSubType.MusicalInstrument), _musicalSyncParam.resId, () =>
            //        {
            //            playerHoldBehaviour.StartPlayInstrument(_musicalSyncParam);
            //        });
            //    }
            //    else
            //    {
            //        // 4  AssetDetailType.Instrument
            //        UIAgentManager.Inst.AssetGetInfo(4, _musicalSyncParam.resId, (t) =>
            //        {
            //            _character.playerStateControllerState.Wrap.ChangeUGCPart(t.skinInfo, () =>
            //            {
            //                playerHoldBehaviour.PreviewUGCInstrument(t.skinActionInfo.instrumentInfo);
            //            });
            //        });
            //    }
            //});

            //todo 具体化动作
            // _character.SetHandNodeActive(false); //手部动画是否隐藏
            //_character.PlayAnim(""); // 播放对应动画ID
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
        //private void MoveToTargetPoint()
        //{
        //    _isMoving = true;
        //    _character.SetHandNodeActive(true);
        //
        //    LoggerUtils.Log($"[PerformOnStageState] {_character.GetNpcName()} 移动到舞台位置: {_targetPointData.pos}");
        //
        //    _character.MoveToPosition(_targetPointData.pos, OnReachedTarget);
        //}
    }
}