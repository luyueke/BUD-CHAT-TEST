using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Game.Props.PropsComponents;
using Game.Avatar;
using DG.Tweening;
using Game.KinematicCharacter;
using Game.Props.PropsManagers.AIGames.AIPark.FSM;

namespace Game.Props.PropsBehaviours
{
    public class AIPark_SlideBehaviour : AIPark_BasePropBehaviour
    {
        protected string animName = "40200496";

        private Vector3[] slidePos = new Vector3[2];
        private Vector3[] slideRot = new Vector3[2];
        Vector3[] slideEndPos = new Vector3[2];
        Vector3[] slideTempPos = new Vector3[2];

        private bool isPlaying;

        private bool[] _isCanClickSlidePos;
        private readonly object _chairLock = new object();
        public bool[] isCanClickSlidePos
        {
            get
            {
                lock (_chairLock)
                {
                    return _isCanClickSlidePos;
                }
            }
            set
            {
                lock (_chairLock)
                {
                    _isCanClickSlidePos = value;
                }
            }
        }
        private float speed = 2.0f; // 移动速度

        public override bool IsCanClick
        {
            get
            {
                if (_currentUsedCount < _maxUsedCount)
                {
                    return true;
                }
                foreach (var usePropCharacterMsg in _usePropCharacterMsg)
                {
                    if (usePropCharacterMsg.Value.seatStatus != PropSeatStatus.Using)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        List<int> _finishList = new List<int>();


        public override void OnInitByCreate()
        {
            base.OnInitByCreate();
            gameObject.layer = LayerMask.NameToLayer("Model");
            _isCanClickSlidePos = new bool[2] { true, true };

            _maxUsedCount = 2;
            var collider = transform.gameObject.GetOrAddComponent<BoxCollider>();
            collider.size = new Vector3(0.0225f,0.01606f,0.01347f);

            slidePos[0] = transform.TransformPoint(new Vector3(-0.00165f, -0.00402f, 0.01f));
            slideRot[0] = Vector3.zero;
            slideTempPos[0] = slidePos[0];
            slideEndPos[0] = new Vector3(slidePos[0].x, 0, 0);


            slidePos[1] = transform.TransformPoint(new Vector3(-0.00158f, 0.00477f, 0.01f));
            slideRot[1] = Vector3.zero;
            slideTempPos[1] = slidePos[1];
            slideEndPos[1] = new Vector3(slidePos[1].x, 0, 0);
        }

        public void SetChairStatus(int index, bool status)
        {
            lock (_chairLock)
            {
                if (_isCanClickSlidePos != null && index >= 0 && index < _isCanClickSlidePos.Length)
                {
                    _isCanClickSlidePos[index] = status;
                }
            }
        }
        public bool GetChairStatus(int index)
        {
            lock (_chairLock)
            {
                if (_isCanClickSlidePos != null && index >= 0 && index < _isCanClickSlidePos.Length)
                {
                    return _isCanClickSlidePos[index];
                }
                return false;
            }
        }
        public int GetEmptyNode()
        {
            if (GetChairStatus(0))
            {
                return 0;
            }
            if (GetChairStatus(1))
            {
                return 1;
            }

            return 0;
        }

        public void Set2BeginState(UsePropCharacterMsg usePropCharacterMsg)
        {
            var index = GetEmptyNode();
            SetChairStatus(index, false);
            usePropCharacterMsg.usePosIndex = index;
            usePropCharacterMsg.usePos = slidePos[index];
            usePropCharacterMsg.useRot = slideRot[index];
            slideTempPos[index] = slidePos[index];
            New_PlayInteractiveAnim(usePropCharacterMsg, animName, usePropCharacterMsg.usePos, usePropCharacterMsg.useRot);
            MoveToNode(usePropCharacterMsg.kccMotor, null, false);
            usePropCharacterMsg.kcc.AfterCharacterMove();
        }

        protected override void Play()
        {
            // isCanClickSlidePos = new bool[2] { true, true };
            // foreach (var usePropCharacterMsg in _usePropCharacterMsg)
            // {
            //     if (usePropCharacterMsg.Value.originParentTrans == null)
            //     {
            //         usePropCharacterMsg.Value.originParentTrans = usePropCharacterMsg.Value.kccMotor.transform.parent;
            //     }
            //     if (usePropCharacterMsg.Value.useParentTrans == null)
            //     {
            //         var index = GetEmptyNode();
            //         usePropCharacterMsg.Value.usePosIndex = index;
            //         usePropCharacterMsg.Value.usePos = slidePos[index];
            //         usePropCharacterMsg.Value.useRot = slideRot[index];
            //     }
            //     isCanClickSlidePos[usePropCharacterMsg.Value.usePosIndex] = false;
            //     // New_PlayInteractiveAnim(usePropCharacterMsg.Value, animName, usePropCharacterMsg.Value.usePos, usePropCharacterMsg.Value.useRot);
            //     // usePropCharacterMsg.Value.kcc.AfterCharacterMove();
            // }


            // _kcc.SetCameraPos(new Vector3(0, 3.35f, 0));
            // 实现滑梯特有的单人游玩逻辑
            // New_PlayInteractiveAnim(animName, pos, rot);
            isPlaying = true;
            StartPlaySound(AIParkConfig.Para_Slide);
            // UnityEditor.EditorApplication.isPaused = true;
        }

        protected override void PlayWithBuddy()
        {
            // 实现滑梯特有的双人游玩逻辑
        }

        public override void OnPropReset()
        {
            if (isPlaying == false)
            {
                return;
            }
            isPlaying = false;
            EndPlaySound(AIParkConfig.Para_Slide);
        }

        public override void OnPropPosFree(KinematicCharacterController kcc)
        {
            var posIdx = GetPropPosIdxByKcc(kcc);
            if (posIdx != -1)
            {
                var usePropCharacterMsg = _usePropCharacterMsg[posIdx];
                MoveToNode(usePropCharacterMsg.kccMotor, null, true);
                                if (kcc != AvatarController.Inst.SelfController.Motor)
                {
                    Revert2Ori(usePropCharacterMsg.kccMotor);
                }
                _usePropCharacterMsg.Remove(posIdx);
                SetChairStatus(posIdx, true);
            }
            if (_currentUsedCount == 0)
            {
                isPlaying = false;
                OnReset();
                OnPropReset();
            }
        }

        public override int GetUsePosIndex(string playerID, KinematicCharacterController kcc)
        {
            var index = GetEmptyNode();
            if (index == -1)
            {
                return -1;
            }
            return index;
        }

        private void Finish(UsePropCharacterMsg usePropCharacterMsg)
        {
            if (!BLinkEmoteState)
            {
                var roleId = usePropCharacterMsg.kcc.transform.GetComponent<AIPark_CharacterBehaviour>().GetNpcID();
                AIParkPropsManager.Inst.ExitAction(roleId, ActionType.SlideSlides);
            }

        }

        private void Update()
        {
            if (isPlaying)
            {
                _finishList.Clear();
                foreach (var data in _usePropCharacterMsg)
                {
                    var usePosIndex = data.Value.usePosIndex;
                    if (_usePropCharacterMsg[data.Key].waitFrame > 0){
                        _usePropCharacterMsg[data.Key].waitFrame--;
                        slideTempPos[usePosIndex] = slideTempPos[usePosIndex];
                    }else{
                        slideTempPos[usePosIndex] = Vector3.Lerp(slideTempPos[usePosIndex], slideEndPos[usePosIndex], speed * Time.deltaTime);
                    }
                    data.Value.kccMotor.SetPosition(slideTempPos[usePosIndex]);
                    if (Vector3.Distance(slideTempPos[usePosIndex], slideEndPos[usePosIndex]) <= 0.12f)
                    {
                        _finishList.Add(usePosIndex);
                    }
                }
                foreach (var finishIndex in _finishList)
                {
                    Finish(_usePropCharacterMsg[finishIndex]);
                }
                if (_currentUsedCount == 0)
                {
                    isPlaying = false;
                }
            }
        }
    }
}
