using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Es;
using GameData.PgcData;
using Newtonsoft.Json;
using RootMotion.FinalIK;
using UnityEngine;
using UnityEngine.UI;

namespace BUD.AnimPose
{
    
    public struct BoneTransform
    {
        public Vector3 pos;
        public Quaternion rot;
        public Vector3 sca;

        public BoneTransform(Transform transform)
        {
            pos = transform.localPosition;
            rot = transform.localRotation;
            sca = transform.localScale;
        }

        public void Write(Transform transform)
        {
            transform.localPosition = pos;
            transform.localRotation = rot;
            transform.localScale = sca;
        }
    }
    
    public class PetAnimIK : BaseAnimIK
    {
       

        private Dictionary<IKPetPart, PoseJointTransform> defaultTranforms = new Dictionary<IKPetPart, PoseJointTransform>();
        private List<BoneTransform> initialBones = new List<BoneTransform>();
        private List<string> bonePath = new()
        {
            "Pet001 Root",
            "Pet001 Root/Pet001 L Thign",
            "Pet001 Root/Pet001 L Thign/Pet001 L Calf",
            "Pet001 Root/Pet001 R Thign",
            "Pet001 Root/Pet001 R Thign/Pet001 R Calf",
            "Pet001 Root/Pet001 Spine",
            "Pet001 Root/Pet001 Spine/Pet001 Spine1",
            "Pet001 Root/Pet001 Spine/Pet001 Spine1/pet001 Head",
            "Pet001 Root/Pet001 Spine/Pet001 Spine1/Pet001 Spine2",
            "Pet001 Root/Pet001 Spine/Pet001 Spine1/Pet001 Spine2/Pet001 L UppArm",
            "Pet001 Root/Pet001 Spine/Pet001 Spine1/Pet001 Spine2/Pet001 L UppArm/Pet001 L ForeArm",
            "Pet001 Root/Pet001 Spine/Pet001 Spine1/Pet001 Spine2/Pet001 R UppArm",
            "Pet001 Root/Pet001 Spine/Pet001 Spine1/Pet001 Spine2/Pet001 R UppArm/Pet001 R ForeArm"
        };
        
        private List<Material> selfMaterials;
        private const string ugcNodeParent = "UgcBody";

        private int[] boneIndexs = {6,7,1,2,3,4,9,10,11,12};
        
        public override void FixTransform()
        {
            var bodyIKs = GetComponents<CCDIK>();
            for (var i = 0; i < bodyIKs.Length; i++)
            {
                bodyIKs[i].GetIKSolver().Update();
            }
        }
        
        public override void InitAnimIK(bool isEdit)
        {
            var bodyIKs = GetComponents<CCDIK>();
            if (bodyIKs.Length == 0)
            {
                bodyIKs = AddCCDIKs();
            }
            for (var i = 0; i < bodyIKs.Length; i++)
            {
                bodyIKs[i].enabled = isEdit;
                var ikNode = LoadJointNode((IKPetPart) i);
                ikNode.position = bodyIKs[i].solver.bones[1].transform.position;
                var bodyIK = bodyIKs[i];
                bodyIK.solver.target = ikNode;
                if (isEdit)
                {
                    bodyIK.solver.OnPostUpdate += () =>
                    {
                        if (bodyIK.solver.target.position != bodyIK.solver.bones[1].transform.position)
                        {
                            bodyIK.solver.target.position = bodyIK.solver.bones[1].transform.position;
                        }
                    };
                }
            }
            ReadTPoseBoneTransform();
            RecordDefaultTransform();
            SetJointNodeVisible(false);
        }

        private CCDIK[] AddCCDIKs()
        {
            var bodyIKs = new CCDIK[5];
            for (int i = 0; i < 5; i++)
            {
                bodyIKs[i] = this.gameObject.AddComponent<CCDIK>();
                bodyIKs[i].enabled = false;
                int index1 = boneIndexs[2 * i];
                var node1=  this.transform.Find(bonePath[index1]);
                var bone1 = new IKSolver.Bone(node1); 
                
                int index2 = boneIndexs[2 * i + 1];
                var node2=  this.transform.Find(bonePath[index2]);
                var bone2 = new IKSolver.Bone(node2); 
                
                bodyIKs[i].solver.bones = new []{bone1,bone2};
            }
            return bodyIKs;
        }

        public override void ResetDefaultPosition()
        {
            var poseModeData = DataTables.GetPoseModeConfig((int)UgcPoseSubType.PetSingle);
            var optParent = this.transform.parent;
            optParent.localPosition = poseModeData.EditPos[0];
            optParent.localScale = Vector3.one;
            this.transform.localPosition = poseModeData.RoleDefPos[0];
            optParent.localEulerAngles = Vector3.zero;
            this.transform.localEulerAngles = Vector3.zero;
        }

        public override void SetWhiteMaterials()
        {
            var ugcParent = this.transform.Find(ugcNodeParent);
            var ugcRenderers = ugcParent.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            selfMaterials = new List<Material>();
            for (var i = 0; i < ugcRenderers.Length; i++)
            {
                selfMaterials.Add(ugcRenderers[i].material);
                ugcRenderers[i].material = ShotMaterial;
            }
        }

        public override void SetDefaultMaterials()
        {
            var ugcParent = this.transform.Find(ugcNodeParent);
            var ugcRenderers = ugcParent.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (var i = 0; i < ugcRenderers.Length; i++)
            {
                ugcRenderers[i].material = selfMaterials[i];
            }
        }
        private void ReadTPoseBoneTransform()
        {
            initialBones.Clear();
            for (var i = 0; i < bonePath.Count; i++)
            {
                var trans = transform.Find(bonePath[i]);
                BoneTransform pet = new BoneTransform(trans);
                initialBones.Add(pet);
            }
        }


        private void WriteTPoseBoneTransform()
        {
            for (var i = 0; i < bonePath.Count; i++)
            {
                var trans = transform.Find(bonePath[i]);
                var pet = initialBones[i];
                pet.Write(trans);
            }
        }

        public override void ResetJointNodes()
        {
            WriteTPoseBoneTransform();
            foreach (var keyValue in jointNodes)
            {
                var transformData = defaultTranforms[(IKPetPart)keyValue.Key];
                keyValue.Value.localPosition = transformData.pos;
                keyValue.Value.localRotation = transformData.rot;
            }
        }


        private void RecordDefaultTransform()
        {
            foreach (var keyValue in jointNodes)
            {
                defaultTranforms[(IKPetPart)keyValue.Key] = new PoseJointTransform()
                {
                    pos = keyValue.Value.localPosition,
                    rot = keyValue.Value.localRotation
                };
            }
        }

        private Transform LoadJointNode(IKPetPart part)
        {
            if (jointNodeParent == null)
            {
                jointNodeParent = new GameObject("JointNodeParent").transform;
                jointNodeParent.SetParent(roleNode.transform);
                jointNodeParent.localPosition = Vector3.zero;
                jointNodeParent.localEulerAngles = Vector3.zero;
                jointNodeParent.localScale = Vector3.one;
            }

            var jointNode = GameObject.Instantiate(JointPrefab, jointNodeParent);
            var poseEditDatas = DataTables.GetPoseEditOperationList();
            var avatarData = poseEditDatas.Find(x => x.RoleType == (int)RoleType.Pet && x.PartType == (int)part); 
            jointNode.name = avatarData.JointName;
            jointNodes.Add((int)part, jointNode.transform);
            return jointNode.transform;
        }
        
        public override void SetJointNodeVisible(bool visible)
        {
            jointNodeParent.gameObject.SetActive(visible);
        }

        public override void SaveKeyFrame(RoleKeyframeData keyFrame)
        {
            List<PartKeyframeData> partKeyFrames = new List<PartKeyframeData>();
            foreach (var keyValue in jointNodes)
            {
                partKeyFrames.Add(new PartKeyframeData()
                {
                    partType = (int) keyValue.Key,
                    pos = keyValue.Value.localPosition,
                    rot = keyValue.Value.localRotation,
                });
            }

            keyFrame.roleType = RoleType.Pet;
            keyFrame.parts = partKeyFrames;
            var transformNode = roleNode.transform.parent;
            keyFrame.pos = transformNode.localPosition;
            keyFrame.rot = transformNode.localRotation;
            keyFrame.sca = transformNode.localScale;
        }

        public override void SetKeyFrameData(RoleKeyframeData keyFrameData)
        {
            for (int i = 0; i < keyFrameData.parts.Count; i++)
            {
                var curPart = keyFrameData.parts[i];
                var partNode = jointNodes[curPart.partType];
                partNode.localPosition = curPart.pos;
                partNode.localRotation = curPart.rot;
            }

            var transformNode = roleNode.transform.parent;
            transformNode.localPosition = keyFrameData.pos;
            transformNode.localRotation = keyFrameData.rot;
            transformNode.localScale = keyFrameData.sca;
        }

        public override void SetKeyFrameData(RoleKeyframeData current, RoleKeyframeData next, float offset)
        {
            for (int i = 0; i < current.parts.Count; i++)
            {
                var curPart = current.parts[i];
                var nextPart = next.parts[i];
                var partNode = jointNodes[curPart.partType];
                partNode.localPosition = Vector3.Lerp(curPart.pos, nextPart.pos, offset);
                partNode.localRotation = Quaternion.Lerp(curPart.rot, nextPart.rot, offset);
            }

            var transformNode = roleNode.transform.parent;
            transformNode.localPosition = Vector3.Lerp(current.pos, next.pos, offset);
            transformNode.localRotation = Quaternion.Lerp(current.rot, next.rot, offset);
            transformNode.localScale = Vector3.Lerp(current.sca, next.sca, offset);
        }
    }
}