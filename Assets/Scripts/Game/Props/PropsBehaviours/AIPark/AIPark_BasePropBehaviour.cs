using AIGame.Base;
using Game.Avatar;
using Game.KinematicCharacter;
using Message;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Props.PropsBehaviours
{
    public abstract class AIPark_BasePropBehaviour : AIPropBaseBehaviour
    {
        protected string AnimName = "";
        protected Animator anim;
        protected int aniState;

        public override bool AllowClickInEmoteLink => true;


        public override void OnInitByCreate()
        {
            base.OnInitByCreate();
            IsCanClick = true;
            aniState = Animator.StringToHash("state");
            anim = this.GetComponentInChildren<Animator>(true);

        }
        /// <summary>
        /// 预定座位 返回是否成功
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public bool PrebookSeats(int count)
        {
            if (_currentUsedCount + count > _maxUsedCount)
            {
                return false;
            }
            return true;
        }


        public int AddUsePropCharacterMsg(KinematicCharacterController kcc, KinematicCharacterMotor kccMotor, PlayerStateController playerStateController)
        {
            if (_currentUsedCount >= _maxUsedCount)
            {
                return -1;
            }
            int usePosIndex = GetUsePosIndex(playerStateController.PlayerID, kcc);
            _usePropCharacterMsg.Remove(usePosIndex);
            _usePropCharacterMsg.Add(usePosIndex, new UsePropCharacterMsg()
            {
                kcc = kcc,
                kccMotor = kccMotor,
                playerStateController = playerStateController,
                usePosIndex = usePosIndex,
                isUsing = true,
                seatStatus = PropSeatStatus.Prebook,
            });
            return usePosIndex;
        }

        public void ChangeSeatStatus(int posIdx, PropSeatStatus seatStatus)
        {
            if (_usePropCharacterMsg.ContainsKey(posIdx))
            {
                _usePropCharacterMsg[posIdx].seatStatus = seatStatus;
            }
        }

        public int GetPropPosIdxByKcc(KinematicCharacterController kcc)
        {
            foreach (var usePropCharacterMsg in _usePropCharacterMsg)
            {
                if (usePropCharacterMsg.Value.kcc == kcc)
                {
                    return usePropCharacterMsg.Key;
                }
            }
            return -1;
        }
        public void RemoveUsePropCharacterMsg(int posIdx)
        {
            if (_usePropCharacterMsg.ContainsKey(posIdx))
            {
                _usePropCharacterMsg.Remove(posIdx);
            }
        }

        public void ChangePropCharaterMsgPosIdx(int posIdx, int newPosIdx){
            if(posIdx == newPosIdx)
            {
                return;
            }
            if (_usePropCharacterMsg.ContainsKey(posIdx))
            {
                var usePropCharacterMsg = _usePropCharacterMsg[posIdx];
                _usePropCharacterMsg.Remove(posIdx);
                _usePropCharacterMsg.Add(newPosIdx, usePropCharacterMsg);
            }
        }
        public void InitKCC(KinematicCharacterController kcc)
        {
            _kcc = kcc;
        }

        public void InitKCCMotor(KinematicCharacterMotor kccMotor)
        {
            _kccMotor = kccMotor;
        }

        public void InitPlayerStateController(PlayerStateController playerStateController)
        {
            _playerStateController = playerStateController;
        }

        public override void OnTouchClick()
        {
            base.OnTouchClick();
            if (!BLinkEmoteState)
            {
                Play();
            }
            else
                PlayWithBuddy();
        }


        // 抽象方法，由子类实现具体的游玩逻辑
        protected abstract void Play();
        protected abstract void PlayWithBuddy();

        public abstract void OnPropReset();
        public virtual void OnPropPosFree(KinematicCharacterController kcc)
        {
            if (_maxUsedCount <= 1)
            {
                OnPropReset();
            }
        }

        protected virtual void OnDestroy()
        {
        }

        public override void OnReset()
        {
            base.OnReset();
        }

        // 事件处理器的包装方法
        // public void OnJoystickChangeHandler()
        // {
        //     // 基类的一些通用重置逻辑
        //     base.OnReset();

        //     // 调用子类的具体重置实现
        //     OnPropReset();
        // }

        protected virtual void StartPlaySound(string soundName)
        {
            AIGameSoundUtils.Inst.PlaySound(soundName, gameObject);
        }

        protected virtual void EndPlaySound(string soundName)
        {
            AIGameSoundUtils.Inst.StopSound(soundName, gameObject);
        }

        protected void MoveToNode(KinematicCharacterMotor motor, Transform node, bool enable)
        {
            if (node != null)
            {
                motor.transform.SetParent(node);
            }
            motor.enabled = enable;
            motor.GetComponent<Rigidbody>().isKinematic = !enable;
        }

        protected void Revert2Ori(KinematicCharacterMotor motor)
        { 
            motor.GetComponent<Rigidbody>().isKinematic = true;
        }
    }
}
