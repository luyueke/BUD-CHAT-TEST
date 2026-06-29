using Game.Avatar;
using Game.Base;
using Game.KinematicCharacter;
using Game.Props.PropsComponents;
using Game.Props.PropsManagers;
using Game.Props.PropsManagers.AIGames.AIPark;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Props.PropsBehaviours
{
    /// <summary>
    /// s11道具:秋千
    /// </summary>
    public class AIPark_SwingBehaviour : AIPark_BasePropBehaviour
    {
        protected string propAnim = "AI_Park_Swing_01";

        protected string animName = "40200495";

        private string node2Name = "Swing_bone_02";

        private string node1Name = "Swing_bone_01";

        private Transform node2, node1;
        private Vector3[] pos = new Vector3[2];
        private Vector3[] rot = new Vector3[2];


        private bool isPlaying;
        public IList<int> _neighbor;

        private bool[] _isCanClickSwingChair;
        private readonly object _chairLock = new object();
        public bool[] isCanClickSwingChair
        {
            get
            {
                lock (_chairLock)
                {
                    return _isCanClickSwingChair;
                }
            }
            set
            {
                lock (_chairLock)
                {
                    _isCanClickSwingChair = value;
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
            var boxCollider = transform.gameObject.GetOrAddComponent<BoxCollider>();
            boxCollider.center = new(0, 0.5f, 0);
            boxCollider.size = new Vector3(2,1,0.5f);

            var data = entity.GetComp<AIGameCommonComponent>();
            _isCanClickSwingChair = new bool[2] { true, true };
            _neighbor = data.neighbor;
        }

        public void InitSwingChair()
        {
            if (_initChair) return;
            if (_neighbor.Count != _maxUsedCount)
            {
                Debug.LogError("秋千椅子数量不正确");
                return;
            }
            for (int i = 0; i < _neighbor.Count; i++)
            {
                var chair = GlobalNodeManager.Inst.Get<AIPark_SwingChairMgr>().GetBehaviourByUid(_neighbor[i]);
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
                if (_isCanClickSwingChair != null && index >= 0 && index < _isCanClickSwingChair.Length)
                {
                    _isCanClickSwingChair[index] = status;
                }
            }
        }
        public bool GetChairStatus(int index)
        {
            lock (_chairLock)
            {
                if (_isCanClickSwingChair != null && index >= 0 && index < _isCanClickSwingChair.Length)
                {
                    return _isCanClickSwingChair[index];
                }
                return false;
            }
        }
        public (Transform,int) GetEmptyNode()
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
            InitSwingChair();
            anim.enabled = true;

            // isCanClickSwingChair = new bool[2] { true, true };
            foreach (var usePropCharacterMsg in _usePropCharacterMsg)
            {
                if(usePropCharacterMsg.Value.originParentTrans == null){
                    usePropCharacterMsg.Value.originParentTrans = usePropCharacterMsg.Value.kccMotor.transform.parent;
                }
                if(usePropCharacterMsg.Value.useParentTrans == null){
                    var (emptyNode,index) = GetEmptyNode();
                    usePropCharacterMsg.Value.useParentTrans = emptyNode;
                    usePropCharacterMsg.Value.usePosIndex = index;
                    usePropCharacterMsg.Value.usePos = pos[index];
                    usePropCharacterMsg.Value.useRot = rot[index];
                }
                usePropCharacterMsg.Value.usePos = usePropCharacterMsg.Value.useParentTrans.TransformPoint(pos[usePropCharacterMsg.Value.usePosIndex]);
                // 将本地欧拉角度转换为世界欧拉角度
                usePropCharacterMsg.Value.useRot = LocalEulerAnglesToWorldEulerAngles(rot[usePropCharacterMsg.Value.usePosIndex], usePropCharacterMsg.Value.useParentTrans);
                SetChairStatus(usePropCharacterMsg.Value.usePosIndex, false);

                if(usePropCharacterMsg.Value.kccMotor.transform.parent != usePropCharacterMsg.Value.useParentTrans){
                    MoveToNode(usePropCharacterMsg.Value.kccMotor, usePropCharacterMsg.Value.useParentTrans, false);
                    New_PlayInteractiveAnim(usePropCharacterMsg.Value, animName, usePropCharacterMsg.Value.usePos, usePropCharacterMsg.Value.useRot);
                    usePropCharacterMsg.Value.kcc.AfterCharacterMove();
                }

            }

            StartPlaySound(AIParkConfig.Para_Swing_Loop);
            isPlaying = true;
        }

        public ParkNpcTransData GetFreePosition()
        {
            InitSwingChair();
            return new ParkNpcTransData()
            {
                pos = node1.position,
                rot = node1.rotation.eulerAngles
            };
        }

        protected override void PlayWithBuddy()
        {
            // 实现秋千特有的双人游玩逻辑
            anim.enabled = true;
        }

        public override void OnReset()
        {
            base.OnReset();

        }

        public override void OnPropReset()
        {
            if (isPlaying == false)
                return;
            isPlaying = false;
          
            node2.transform.localRotation = Quaternion.Euler(0, 90, 90);

            anim.enabled = false;
            EndPlaySound(AIParkConfig.Para_Swing_Loop);
        }

        public override void OnPropPosFree(KinematicCharacterController kcc)
        {
            var posIdx = GetPropPosIdxByKcc(kcc);
            if(posIdx != -1)
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
            if(_currentUsedCount == 0)
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
                Debug.LogError("秋千没有空位");
                return -1;
            }
            return index;
        }

        public ParkNpcTransData GetEmptySwingChair(int idx)
        {
            InitSwingChair();
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

        /// <summary>
        /// 将本地欧拉角度转换为世界欧拉角度
        /// </summary>
        /// <param name="localEulerAngles">本地欧拉角度</param>
        /// <param name="parentTransform">父变换</param>
        /// <returns>世界欧拉角度</returns>
        public static Vector3 LocalEulerAnglesToWorldEulerAngles(Vector3 localEulerAngles, Transform parentTransform)
        {
            // 将本地欧拉角度转换为四元数
            Quaternion localRotation = Quaternion.Euler(localEulerAngles);
            
            // 将本地四元数转换为世界四元数
            Quaternion worldRotation = parentTransform.rotation * localRotation;
            
            // 将世界四元数转换回欧拉角度
            return worldRotation.eulerAngles;
        }

        /// <summary>
        /// 将世界欧拉角度转换为本地欧拉角度
        /// </summary>
        /// <param name="worldEulerAngles">世界欧拉角度</param>
        /// <param name="parentTransform">父变换</param>
        /// <returns>本地欧拉角度</returns>
        public static Vector3 WorldEulerAnglesToLocalEulerAngles(Vector3 worldEulerAngles, Transform parentTransform)
        {
            // 将世界欧拉角度转换为四元数
            Quaternion worldRotation = Quaternion.Euler(worldEulerAngles);
            
            // 将世界四元数转换为本地四元数
            Quaternion localRotation = Quaternion.Inverse(parentTransform.rotation) * worldRotation;
            
            // 将本地四元数转换回欧拉角度
            return localRotation.eulerAngles;
        }
    }
}