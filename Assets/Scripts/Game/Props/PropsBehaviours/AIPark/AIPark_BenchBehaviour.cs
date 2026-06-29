using AIGame.Base;
using Game.Avatar;
using Game.Base;
using Game.KinematicCharacter;
using Game.Props.PropsComponents;
using Game.Props.PropsManagers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Props.PropsBehaviours
{
    /// <summary>
    /// s11交互道具：长椅
    /// 这个道具需要注意的是有左右两个交互点位，可以两人一起玩
    /// </summary>
    public class AIPark_BenchBehaviour : AIPark_BasePropBehaviour
    {
        protected string animName = "40200507";

        private Vector3[] pos = new Vector3[2];
        private Vector3[] rot = new Vector3[2];

        private bool isPlaying;
        public IList<int> _neighbor;
        private bool[] _isCanClickChair;
        private readonly object _chairLock = new object();

        public bool[] isCanClickChair
        {
            get
            {
                lock (_chairLock)
                {
                    return _isCanClickChair;
                }
            }
            set
            {
                lock (_chairLock)
                {
                    _isCanClickChair = value;
                }
            }
        }

        bool _initChair = false;


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
                    if (usePropCharacterMsg.Value.isUsing == false)
                    {
                        return true;
                    }
                }
                return false;
            }
        }
        public override void OnInitByCreate()
        {
            base.OnInitByCreate();
            gameObject.layer = LayerMask.NameToLayer("Model");


            _maxUsedCount = 2; //暂不启用
            var collider = transform.gameObject.GetOrAddComponent<BoxCollider>();
            // collider.size = new Vector3(0.02f,0.005f,0.01f);
            collider.center = new(0, 0.3f, 0);
            collider.size = new Vector3(.3f, .3f,.3f);

            var data = entity.GetComp<AIGameCommonComponent>();
            _isCanClickChair = new bool[2] { true, true };
            _neighbor = data.neighbor;
        }
        public void InitChair()
        {
            if (_initChair) return;
            if (_neighbor.Count != _maxUsedCount)
            {
                Debug.LogError("长椅椅子数量不正确");
                return;
            }
            for (int i = 0; i < _neighbor.Count; i++)
            {
                var chair = GlobalNodeManager.Inst.Get<AIPark_BenchChairMgr>().GetBehaviourByUid(_neighbor[i]);
                // if (chair != null && chair.IsCanClick)
                if (chair != null)
                {
                    var data = chair.entity.GetComp<AIGameCommonComponent>();
                    if (i == 0)
                    {
                        pos[i] = data.Pos;
                        rot[i] = data.Rot;
                        SetChairStatus(i, true);
                    }
                    else
                    {
                        pos[i] = data.Pos;
                        rot[i] = data.Rot;
                        SetChairStatus(i, true);
                    }
                    _initChair = true;
                }
            }
        }
        public void SetChairStatus(int index, bool status)
        {
            lock (_chairLock)
            {
                if (_isCanClickChair != null && index >= 0 && index < _isCanClickChair.Length)
                {
                    _isCanClickChair[index] = status;
                }
            }
        }
        public bool GetChairStatus(int index)
        {
            lock (_chairLock)
            {
                if (_isCanClickChair != null && index >= 0 && index < _isCanClickChair.Length)
                {
                    return _isCanClickChair[index];
                }
                return false;
            }
        }
        public (Transform, int) GetEmptyNode()
        {

            if (GetChairStatus(0))
            {
                return (transform, 0);
            }
            if (GetChairStatus(1))
            {
                return (transform, 1);
            }

            return (null, 0);
        }

        protected override void Play()
        {
            InitChair();
            // isCanClickChair = new bool[2]{true,true};

            foreach (var usePropCharacterMsg in _usePropCharacterMsg)
            {
                if (usePropCharacterMsg.Value.originParentTrans == null)
                {
                    usePropCharacterMsg.Value.originParentTrans = usePropCharacterMsg.Value.kccMotor.transform.parent;
                }
                if (usePropCharacterMsg.Value.useParentTrans == null)
                {
                    var (emptyNode, index) = GetEmptyNode();
                    usePropCharacterMsg.Value.useParentTrans = emptyNode;
                    usePropCharacterMsg.Value.usePosIndex = index;
                    usePropCharacterMsg.Value.usePos = pos[index];
                    usePropCharacterMsg.Value.useRot = rot[index];
                }
                SetChairStatus(usePropCharacterMsg.Value.usePosIndex, false);
                LoggerUtils.Log("长椅Play posIdx:"+usePropCharacterMsg.Value.usePosIndex+ "kcc="+usePropCharacterMsg.Value.kcc.gameObject.name);
                if (usePropCharacterMsg.Value.kccMotor.transform.parent != usePropCharacterMsg.Value.useParentTrans)
                {
                    MoveToNode(usePropCharacterMsg.Value.kccMotor, usePropCharacterMsg.Value.useParentTrans, false);
                    New_PlayInteractiveAnim(usePropCharacterMsg.Value, animName, usePropCharacterMsg.Value.usePos, usePropCharacterMsg.Value.useRot);
                    usePropCharacterMsg.Value.kcc.AfterCharacterMove();
                }
            }
            // 实现跷跷板特有的单人游玩逻辑
            isPlaying = true;
        }


        protected override void PlayWithBuddy()
        {
            // 实现跷跷板特有的双人游玩逻辑
        }

        public override void OnPropReset()
        {
            if (isPlaying == false)
                return;
            isPlaying = false;
        }

        public override void OnPropPosFree(KinematicCharacterController kcc)
        {
            var posIdx = GetPropPosIdxByKcc(kcc);
            if (posIdx != -1)
            {
                var usePropCharacterMsg = _usePropCharacterMsg[posIdx];
                MoveToNode(usePropCharacterMsg.kccMotor, usePropCharacterMsg.originParentTrans, true);
                if (kcc != AvatarController.Inst.SelfController.Motor)
                {
                    Revert2Ori(usePropCharacterMsg.kccMotor);
                }
                _usePropCharacterMsg.Remove(posIdx);
                SetChairStatus(posIdx, true);
                LoggerUtils.Log("长椅OnPropPosFree posIdx:"+posIdx+ "kcc="+kcc.gameObject.name);
            }
            if (_currentUsedCount == 0)
            {
                OnReset();
                OnPropReset();
            }
        }

        public override int GetUsePosIndex(string playerID, KinematicCharacterController kcc)
        {
            var (emptyNode,index) = GetEmptyNode();
            if(emptyNode == null)
            {
                LoggerUtils.Log("长椅没有空位");
                return -1;
            }
            return index;
        }

        public ParkNpcTransData GetEmptySeesawChair(int idx)
        {
            InitChair();
            if (idx == 0)
            {
                return new ParkNpcTransData()
                {
                    pos = pos[idx],
                    rot = rot[idx]
                };
            }
            else
            {
                return new ParkNpcTransData()
                {
                    pos = pos[idx],
                    rot = rot[idx]
                };
            }
        }
    }
}