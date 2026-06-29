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
    /// s11交互道具：跷跷板
    /// 这个道具需要注意的是有左右两个交互点位，可以两人一起玩
    /// </summary>
    public class AIPark_SeesawBehaviour : AIPark_BasePropBehaviour
    {
        protected string animName = "40200497";

        protected string propAnim = "AI_Park_Seesaw_01";

        private string node2Name = "seesaw_bone_02";

        private string node1Name = "seesaw_bone_01";

        private Transform node2, node1;

        private Vector3[] pos = new Vector3[2];
        private Vector3[] rot = new Vector3[2];

        private bool isPlaying;
        public IList<int> _neighbor;
        private bool[] _isCanClickSeeSawChair;
        private readonly object _chairLock = new object();

        public bool[] isCanClickSeeSawChair
        {
            get
            {
                lock (_chairLock)
                {
                    return _isCanClickSeeSawChair;
                }
            }
            set
            {
                lock (_chairLock)
                {
                    _isCanClickSeeSawChair = value;
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
                    // if (usePropCharacterMsg.Value.seatStatus != PropSeatStatus.Using)
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
            anim.enabled = false;

            node1 = transform.Find(node1Name);
            node2 = transform.Find(node2Name);

            _maxUsedCount = 2;
            var collider = transform.gameObject.GetOrAddComponent<BoxCollider>();
            collider.size = new Vector3(4, 0.5f, 0.25f);

            var data = entity.GetComp<AIGameCommonComponent>();
            _isCanClickSeeSawChair = new bool[2] { true, true };
            _neighbor = data.neighbor;
        }
        public void InitSeesawChair()
        {
            if (_initChair) return;
            if (_neighbor.Count != _maxUsedCount)
            {
                Debug.LogError("跷跷板椅子数量不正确");
                return;
            }
            for (int i = 0; i < _neighbor.Count; i++)
            {
                var chair = GlobalNodeManager.Inst.Get<AIPark_SeesawChairMgr>().GetBehaviourByUid(_neighbor[i]);
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
                if (_isCanClickSeeSawChair != null && index >= 0 && index < _isCanClickSeeSawChair.Length)
                {
                    _isCanClickSeeSawChair[index] = status;
                }
            }
        }
        public bool GetChairStatus(int index)
        {
            lock (_chairLock)
            {
                if (_isCanClickSeeSawChair != null && index >= 0 && index < _isCanClickSeeSawChair.Length)
                {
                    return _isCanClickSeeSawChair[index];
                }
                return false;
            }
        }

        public (Transform, int) GetEmptyNode()
        {

            if (GetChairStatus(0))
            {
                return (node2, 0);
            }
            if (GetChairStatus(1))
            {
                return (node2, 1);
            }

            return (null, 0);
        }

        protected override void Play()
        {
            InitSeesawChair();
            anim.enabled = true;
            // isCanClickSeeSawChair = new bool[2] { true, true };

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

                if(usePropCharacterMsg.Value.kccMotor.transform.parent != usePropCharacterMsg.Value.useParentTrans){
                    MoveToNode(usePropCharacterMsg.Value.kccMotor, usePropCharacterMsg.Value.useParentTrans, false);
                    New_PlayInteractiveAnim(usePropCharacterMsg.Value, animName, usePropCharacterMsg.Value.usePos, usePropCharacterMsg.Value.useRot);
                    usePropCharacterMsg.Value.kcc.AfterCharacterMove();
                }

            }
            StartPlaySound(AIParkConfig.Para_Seesaw_Loop);
            // 实现跷跷板特有的单人游玩逻辑
            isPlaying = true;
        }

        public ParkNpcTransData GetFreePosition()
        {
            InitSeesawChair();
            return new ParkNpcTransData()
            {
                pos = node1.position,
                rot = node1.rotation.eulerAngles
            };
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

            anim.enabled = false;

            // 重置跷跷板状态
            node1.transform.localRotation = Quaternion.Euler(Vector3.zero);
            node2.transform.localRotation = Quaternion.Euler(Vector3.zero);
            EndPlaySound(AIParkConfig.Para_Seesaw_Loop);
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
                usePropCharacterMsg.kccMotor.transform.localScale = Vector3.one;
                _usePropCharacterMsg.Remove(posIdx);
                SetChairStatus(posIdx, true);
            }
            if (_currentUsedCount == 0)
            {
                OnReset();
                OnPropReset();
            }
        }

        public override int GetUsePosIndex(string playerID, KinematicCharacterController kcc)
        {
            var (emptyNode, index) = GetEmptyNode();
            if (emptyNode == null)
            {
                Debug.LogError("跷跷板没有空位");
                return -1;
            }
            return index;
        }

        public ParkNpcTransData GetEmptySeesawChair(int idx)
        {
            InitSeesawChair();
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