using System.Collections.Generic;
using Es;
using Game.Avatar;
using Game.Base;
using Game.ECS;
using Game.KinematicCharacter;
using Message;
using UnityEngine;

namespace Game.Props.PropsBehaviours
{
    public enum PropSeatStatus{
        Free,//空闲
        Prebook,//预定 刚进入行为时，先预定座位 避免npc都往一个位置去 导致抢座位
        Using,//使用中
    }
    public class UsePropCharacterMsg{
        public KinematicCharacterController kcc;
        public KinematicCharacterMotor kccMotor;
        public PlayerStateController playerStateController;
        public int usePosIndex = -1; //使用位置索引
        public Vector3 usePos; //使用位置
        public Vector3 useRot; //使用旋转
        public int uid; //子位置索引
        public PropSeatStatus seatStatus; //座位状态
        public bool isUsing = false; //是否在使用
        public Transform originParentTrans; //使用者的源父点
        public Transform useParentTrans; //使用者的使用父点
        public int waitFrame = 0; //等待帧数
    }
    public class AIPropBaseBehaviour : NodeBaseBehaviour
    {
        public int PropId { get; set; }
        public Vector3 OriginalPosition;
        public Vector3 OriginalEulerAngles;
        public Vector3 OriginalScale;

        // 定义一个属性来标记该道具是否允许在双人链接模式下点击
        public virtual bool AllowClickInEmoteLink => false;

        public bool BLinkEmoteState { get; private set; }

        private bool LastClickState = false;

        private bool _baseCanClick = true;


        public Transform _oriParentTrans; //使用道具者的源父点
        public KinematicCharacterController _kcc; //被使用的kcc
        public KinematicCharacterMotor _kccMotor; //被使用的kccMotor
        public PlayerStateController _playerStateController; //被使用的playerStateController

        public Dictionary<int,UsePropCharacterMsg> _usePropCharacterMsg = new();
        public int _currentUsedCount{
            get{
                return _usePropCharacterMsg.Count;
            }
        }
        public int _maxUsedCount = 1; //最大使用者数量

        // public virtual UsePropCharacterMsg GetEmptyUsePropCharacterMsg()
        // {
        //     if(_currentUsedCount < _maxUsedCount)
        //     {
        //         return _currentUsedCount;
        //     }
        //     foreach(var usePropCharacterMsg in _usePropCharacterMsg)
        //     {
        //         if(usePropCharacterMsg.Value.isCanUse == false)
        // }

        public virtual int GetUsePosIndex(string playerID, KinematicCharacterController kcc)
        {
            return 0;
        }

        public override bool IsCanClick
        {
            get
            {
                if (BLinkEmoteState)
                {
                    return _baseCanClick && AllowClickInEmoteLink;
                }
                return _baseCanClick;
            }
            set => _baseCanClick = value;
        }

        public virtual Vector3 GetTouchData()
        {
            return Vector3.zero;
        }
        public virtual void OnColliderHit()
        {

        }

        public virtual void OnNpcColliderHit()
        {

        }
        public virtual void OnColliderExit()
        {

        }

        protected virtual void SelfPlayer_PlayInteractiveAnim(string emoId, Vector3 pos, Vector3 rot)
        {
            var emoAniDataList = DataTables.GetEmoAniConfigList().FindAll((emoAniData) => emoAniData.emoId == emoId);
            if (emoAniDataList.Count > 0)
            {
                if (AvatarController.Inst.SelfStateController.CanEnterState(PlayerState.SingleEmote))
                {
                    int random = emoAniDataList[0].randomCount;
                    int randomResult = random == 0 ? 0 : UnityEngine.Random.Range(1, random + 1);
                    AvatarController.Inst.SelfController.Motor.SetPositionAndRotation(pos, Quaternion.Euler(rot));
                    AvatarController.Inst.SelfStateController.EnterState(PlayerState.SingleEmote, emoId, randomResult);
                    IsCanClick = false;
                }
            }
        }

        protected virtual void New_PlayInteractiveAnim(string emoId, Vector3 pos, Vector3 rot)
        {
            var emoAniDataList = DataTables.GetEmoAniConfigList().FindAll((emoAniData) => emoAniData.emoId == emoId);
            if (emoAniDataList.Count > 0)
            {
                if (_playerStateController.CanEnterState(PlayerState.SingleEmote))
                {
                    int random = emoAniDataList[0].randomCount;
                    int randomResult = random == 0 ? 0 : UnityEngine.Random.Range(1, random + 1);
                    _kccMotor.SetPositionAndRotation(pos, Quaternion.Euler(rot));
                    _playerStateController.EnterState(PlayerState.SingleEmote, emoId, randomResult);
                    // IsCanClick = false;
                }
            }
        }

        protected virtual void New_PlayInteractiveAnim(UsePropCharacterMsg usePropCharacterMsg, string emoId, Vector3 pos, Vector3 rot)
        {
            var emoAniDataList = DataTables.GetEmoAniConfigList().FindAll((emoAniData) => emoAniData.emoId == emoId);
            if (emoAniDataList.Count > 0)
            {
                if (usePropCharacterMsg.playerStateController.CanEnterState(PlayerState.SingleEmote))
                {
                    int random = emoAniDataList[0].randomCount;
                    int randomResult = random == 0 ? 0 : UnityEngine.Random.Range(1, random + 1);
                    usePropCharacterMsg.kccMotor.SetPositionAndRotation(pos, Quaternion.Euler(rot));
                    usePropCharacterMsg.playerStateController.EnterState(PlayerState.SingleEmote, emoId, randomResult);
                }
            }
        }


        protected virtual void AIBuddy_PlayerInteractiveAnim(string emoId, Vector3 pos, Vector3 rot)
        {
            var emoAniDataList = DataTables.GetEmoAniConfigList().FindAll((emoAniData) => emoAniData.emoId == emoId);
            if (emoAniDataList.Count > 0)
            {
                var aibuddy = AIBuddyAvatarController.Inst.GetPlayerStateCtrl(AccountDataManager.Inst.Uid);
                if (aibuddy.CanEnterState(PlayerState.SingleEmote))
                {
                    int random = emoAniDataList[0].randomCount;
                    int randomResult = random == 0 ? 0 : UnityEngine.Random.Range(1, random + 1);
                    aibuddy.PlayerKCCtrl.Motor.SetPositionAndRotation(pos, Quaternion.Euler(rot));
                    aibuddy.EnterState(PlayerState.SingleEmote, emoId, randomResult);
                }
            }
        }

        protected virtual void AIBuddy_PlayerInteractiveAnim(string id, string emoId, Vector3 pos, Vector3 rot)
        {
            var emoAniDataList = DataTables.GetEmoAniConfigList().FindAll((emoAniData) => emoAniData.emoId == emoId);
            if (emoAniDataList.Count > 0)
            {
                var aibuddy = AIBuddyAvatarController.Inst.GetPlayerStateCtrl(id);
                if (aibuddy.CanEnterState(PlayerState.SingleEmote))
                {
                    int random = emoAniDataList[0].randomCount;
                    int randomResult = random == 0 ? 0 : UnityEngine.Random.Range(1, random + 1);
                    aibuddy.PlayerKCCtrl.Motor.SetPositionAndRotation(pos, Quaternion.Euler(rot));
                    aibuddy.EnterState(PlayerState.SingleEmote, emoId, randomResult);
                }
            }
        }

        #region 双人状态管理

        public override void OnInitByCreate()
        {
            base.OnInitByCreate();

            // 添加双人链接状态变化的监听
            MessageHelper.AddListener<bool>(MessageName.BuddyLinkEmoteStateChange, OnPlayerLinkEmoteStateChanged);
        }

        private void OnDestroy()
        {
            // 移除监听
            MessageHelper.RemoveListener<bool>(MessageName.BuddyLinkEmoteStateChange, OnPlayerLinkEmoteStateChanged);
        }

        // 处理双人链接状态变化
        protected virtual void OnPlayerLinkEmoteStateChanged(bool isInEmoteLink)
        {
            // 根据道具类型和当前状态决定是否可点击
            UpdateClickableState(isInEmoteLink);
        }

        // 更新可点击状态
        protected virtual void UpdateClickableState(bool isInEmoteLink)
        {
            BLinkEmoteState = isInEmoteLink;
            // 不需要额外的状态管理，IsCanClick 的 get 会自动处理
        }

        // 原有的OnPlayerLinkEmote方法可以重构为
        public virtual void OnPlayerLinkEmote(bool value)
        {
            UpdateClickableState(value);
        }
        #endregion
    }
}
