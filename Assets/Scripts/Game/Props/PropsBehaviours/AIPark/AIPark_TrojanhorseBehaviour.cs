using System.Collections;
using System.Collections.Generic;
using System.Threading;
using AIGame.Base;
using Game.Audio;
using Game.Avatar;
using Game.Base;
using Game.KinematicCharacter;
using Game.Props.PropsComponents;
using Game.Props.PropsManagers;
using Message;
using UnityEngine;

namespace Game.Props.PropsBehaviours
{
    public class AIPark_TrojanhorseBehaviour : AIPark_BasePropBehaviour
    {
        protected string animName = "40200497";

        List<string> nodeNames = new List<string>(){
            "Trojanhors_bone_01",
            "Trojanhors_bone_02",
            "Trojanhors_bone_03",
            "Trojanhors_bone_04",
            "Trojanhors_bone_05",
            "Trojanhors_bone_06",
            "Trojanhors_bone_07",
            "Trojanhors_bone_08",
            // "Trojanhors_bone_09",
            // "Trojanhors_bone_10",
            // "Trojanhors_bone_11",
            // "Trojanhors_bone_12",
        };
        List<Transform> nodes = new List<Transform>();

        Vector3[] pos = new Vector3[8];
        Vector3[] rot = new Vector3[8];

        bool isPlaying = false;
        public IList<int> _neighbor;
        private bool[] _isCanClickTrojanHorseChair;
        private readonly object _chairLock = new object();

        public bool[] isCanClickTrojanHorseChair
        {
            get
            {
                lock (_chairLock)
                {
                    return _isCanClickTrojanHorseChair;
                }
            }
            set
            {
                lock (_chairLock)
                {
                    _isCanClickTrojanHorseChair = value;
                }
            }
        }

        /// <summary>
        /// 线程安全地获取指定索引的椅子状态
        /// </summary>
        public bool GetChairStatus(int index)
        {
            lock (_chairLock)
            {
                if (_isCanClickTrojanHorseChair != null && index >= 0 && index < _isCanClickTrojanHorseChair.Length)
                {
                    return _isCanClickTrojanHorseChair[index];
                }
                return false;
            }
        }

        /// <summary>
        /// 线程安全地设置指定索引的椅子状态
        /// </summary>
        public void SetChairStatus(int index, bool status)
        {
            lock (_chairLock)
            {
                if (_isCanClickTrojanHorseChair != null && index >= 0 && index < _isCanClickTrojanHorseChair.Length)
                {
                    _isCanClickTrojanHorseChair[index] = status;
                }
            }
        }

        /// <summary>
        /// 线程安全地检查是否有可用的椅子
        /// </summary>
        public bool HasAvailableChair()
        {
            lock (_chairLock)
            {
                if (_isCanClickTrojanHorseChair == null) return false;

                for (int i = 0; i < _isCanClickTrojanHorseChair.Length; i++)
                {
                    if (_isCanClickTrojanHorseChair[i])
                    {
                        return true;
                    }
                }
                return false;
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
            anim.enabled = true; //产品要求一直转
            foreach (var nodeName in nodeNames)
            {
                nodes.Add(transform.Find(nodeName));
            }
            _maxUsedCount = 8;
            isCanClickTrojanHorseChair = new bool[8];
            for (int i = 0; i < 8; i++)
            {
                isCanClickTrojanHorseChair[i] = true;
            }
            _neighbor = entity.GetComp<AIGameCommonComponent>().neighbor;

            var collider = transform.gameObject.GetOrAddComponent<CapsuleCollider>();
            collider.radius = 4.6f;
            collider.center = new Vector3(0, 1f, 0);
        }

        public void InitTrojanHorseChair()
        {
            if (_initChair) return;
            if (_neighbor.Count != _maxUsedCount)
            {
                Debug.LogError("旋转木马椅子数量不正确");
                return;
            }
            for (int i = 0; i < _neighbor.Count; i++)
            {
                var chair = GlobalNodeManager.Inst.Get<AIPark_TrojanhorseChairMgr>().GetBehaviourByUid(_neighbor[i]);
                if (chair != null)
                {
                    var data = chair.entity.GetComp<AIGameCommonComponent>();
                    pos[i] = data.Pos;
                    rot[i] = data.Rot;
                    SetChairStatus(i, true);
                }
                _initChair = true;
            }
        }

        public (Transform, int) GetEmptyNode(Vector3 pos = default)
        {
            lock (_chairLock)
            {
                if (pos == default)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        if (_isCanClickTrojanHorseChair != null && i < _isCanClickTrojanHorseChair.Length && _isCanClickTrojanHorseChair[i])
                        {
                            return (nodes[i], i);
                        }
                    }
                }
                else
                {
                    //寻找离pos最近的点
                    var minDistance = float.MaxValue;
                    var minIndex = -1;
                    for (int i = 0; i < 8; i++)
                    {
                        if (_isCanClickTrojanHorseChair != null && i < _isCanClickTrojanHorseChair.Length && _isCanClickTrojanHorseChair[i])
                        {
                            var distance = Vector3.Distance(pos, nodes[i].position);
                            if (distance < minDistance)
                            {
                                minDistance = distance;
                                minIndex = i;
                            }
                        }
                    }
                    if (minIndex > -1)
                    {
                        return (nodes[minIndex], minIndex);
                    }
                }

            }
            return (null, 0);
        }

        protected override void Play()
        {
            InitTrojanHorseChair();
            anim.enabled = true;
            // isCanClickTrojanHorseChair = new bool[8] { true, true, true, true, true, true, true, true };

            foreach (var usePropCharacterMsg in _usePropCharacterMsg)
            {
                if (usePropCharacterMsg.Value.originParentTrans == null)
                {
                    usePropCharacterMsg.Value.originParentTrans = usePropCharacterMsg.Value.kccMotor.transform.parent;
                }
                var oldUsePosIdx = usePropCharacterMsg.Value.usePosIndex;
                var newUsePosIdx = oldUsePosIdx;
                if (usePropCharacterMsg.Value.useParentTrans == null)
                {
                    var (emptyNode, index) = GetEmptyNode(usePropCharacterMsg.Value.kccMotor.transform.position);
                    Debug.Log("111旋转木马emptyNode:" + emptyNode.name + " index:" + index+" kcc="+usePropCharacterMsg.Value.kcc.gameObject.name);
                    usePropCharacterMsg.Value.useParentTrans = emptyNode;
                    usePropCharacterMsg.Value.usePosIndex = index;
                    newUsePosIdx = index;
                }
                usePropCharacterMsg.Value.usePos = nodes[usePropCharacterMsg.Value.usePosIndex].TransformPoint(pos[usePropCharacterMsg.Value.usePosIndex]);
                // 将本地欧拉角度转换为世界欧拉角度
                usePropCharacterMsg.Value.useRot = LocalEulerAnglesToWorldEulerAngles(rot[usePropCharacterMsg.Value.usePosIndex], nodes[usePropCharacterMsg.Value.usePosIndex]);
                ChangePropCharaterMsgPosIdx(oldUsePosIdx,newUsePosIdx);
                SetChairStatus(oldUsePosIdx, true);
                SetChairStatus(newUsePosIdx, false);
                if (usePropCharacterMsg.Value.kccMotor.transform.parent != usePropCharacterMsg.Value.useParentTrans)
                {
                    MoveToNode(usePropCharacterMsg.Value.kccMotor, usePropCharacterMsg.Value.useParentTrans, false);
                    New_PlayInteractiveAnim(usePropCharacterMsg.Value, animName, usePropCharacterMsg.Value.usePos, usePropCharacterMsg.Value.useRot);
                    usePropCharacterMsg.Value.kcc.AfterCharacterMove();
                }
            }
            isPlaying = true;
            // UnityEditor.EditorApplication.isPaused = true;
        }

        public ParkNpcTransData GetFreePosition()
        {
            InitTrojanHorseChair();
            return new ParkNpcTransData()
            {
                pos = nodes[0].position,
                rot = nodes[0].rotation.eulerAngles
            };
        }

        protected override void PlayWithBuddy()
        {
            // 实现滑梯特有的双人游玩逻辑
        }
        public override void OnPropReset()
        {
            // 重置滑梯状态
            //  LoggerUtils.Log("Slide OnReset");
            // 例如：重置动画状态、停止音效等
            if (isPlaying == false)
                return;
            isPlaying = false;
            anim.enabled = true; //产品要求一直转
            for (int i = 0; i < nodes.Count; i++)
            {
                // nodes[i].transform.localRotation = Quaternion.Euler(Vector3.zero);
            }
        }

        public override void OnPropPosFree(KinematicCharacterController kcc)
        {
            var posIdx = GetPropPosIdxByKcc(kcc);
            Debug.Log("333旋转木马posIdx:" + posIdx+" kcc="+kcc.gameObject.name);
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
                isCanClickTrojanHorseChair[posIdx] = true;
            }
            if (_currentUsedCount == 0)
            {
                OnReset();
                OnPropReset();
            }
        }

        public void UsePosIdx(int idx)
        { 
            if (_usePropCharacterMsg.ContainsKey(idx))
            {
                SetChairStatus(idx, false);
            }
        }

        public override int GetUsePosIndex(string playerID, KinematicCharacterController kcc)
        {
            var (emptyNode, index) = GetEmptyNode(kcc.transform.position);
            Debug.Log("222旋转木马emptyNode:" + emptyNode?.name + " index:" + index + " playerID=" + playerID);

            if (emptyNode == null)
            {
                Debug.Log("旋转木马没有空位");
                return -1;
            }
            return index;
        }

        public ParkNpcTransData GetEmptySeesawChair(int idx)
        {
            InitTrojanHorseChair();
            return new ParkNpcTransData()
            {
                pos = pos[idx],
                rot = rot[idx]
            };
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
