using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Es;
using Game;
using GameData.PgcData;
using Newtonsoft.Json;
using RootMotion;
using RootMotion.FinalIK;
using UnityEngine;
using UnityEngine.UI;

namespace BUD.AnimPose
{
    public class AvatarAnimIK : BaseAnimIK
    {
        private Dictionary<IKPart, PoseJointTransform> defaultTranforms = new Dictionary<IKPart, PoseJointTransform>();
        private float headRadius = 0.3f;
        private float headHeight = 0.545f;
        private Transform headNode;
        private FullBodyBipedIK bodyIK;
        private LookAtIK lookIK;
        private string[] skinRendererPaths = { "body","body_face" };
        private List<Material> selfMaterials;
        private const string pelvis = "Bip001/Bip001 Pelvis";
        private const string leftThigh = "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 L Thigh";
        private const string leftCalf = "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 L Thigh/Bip001 L Calf";
        private const string leftFoot = "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 L Thigh/Bip001 L Calf/Bip001 L Foot";
        private const string rightThigh = "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 R Thigh";
        private const string rightCalf = "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 R Thigh/Bip001 R Calf";
        private const string rightFoot = "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 R Thigh/Bip001 R Calf/Bip001 R Foot";

        private const string leftUpperArm =
            "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/Bip001 Neck/Bip001 L Clavicle/Bip001 L UpperArm";

        private const string leftForearm =
            "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/Bip001 Neck/Bip001 L Clavicle/Bip001 L UpperArm/Bip001 L Forearm";

        private const string leftHand =
            "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/Bip001 Neck/Bip001 L Clavicle/Bip001 L UpperArm/Bip001 L Forearm/Bip001 L Hand";

        private const string rightUpperArm =
            "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/Bip001 Neck/Bip001 R Clavicle/Bip001 R UpperArm";

        private const string rightForearm =
            "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/Bip001 Neck/Bip001 R Clavicle/Bip001 R UpperArm/Bip001 R Forearm";

        private const string rightHand =
            "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/Bip001 Neck/Bip001 R Clavicle/Bip001 R UpperArm/Bip001 R Forearm/Bip001 R Hand";

        private const string head = "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/Bip001 Neck/Bip001 Head";
        private const string spine0 = "Bip001/Bip001 Pelvis/Bip001 Spine";
        private const string spine1 = "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1";
        private const string spine2 = "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/Bip001 Neck";
        private const string rootNode = "Bip001/Bip001 Pelvis/Bip001 Spine";
        
        
        private List<BoneTransform> initialBones = new List<BoneTransform>();
        private List<string> bonePath = new()
        { 
            "Bip001",
            "Bip001/Bip001 Pelvis",
            "Bip001/Bip001 Pelvis/Bip001 Spine",
            "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1",
            "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/Bip001 Neck",
            "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/Bip001 Neck/Bip001 Head"
        };

        public override void InitAnimIK(bool isEdit)
        {
            if (!this.gameObject.TryGetComponent<FullBodyBipedIK>(out bodyIK))
            {
                bodyIK = AddFullBodyBipedIK();
            }
            bodyIK.enabled = isEdit;
            var bodyNode = LoadJointNode(IKPart.Body);
            bodyNode.position = bodyIK.solver.bodyEffector.bone.position;
            bodyNode.rotation = bodyIK.solver.bodyEffector.bone.rotation;
            bodyIK.solver.bodyEffector.target = bodyNode;

            var leftHandNode = LoadJointNode(IKPart.LeftHand);
            leftHandNode.position = bodyIK.solver.leftHandEffector.bone.position;
            leftHandNode.rotation = bodyIK.solver.leftHandEffector.bone.rotation;
            bodyIK.solver.leftHandEffector.target = leftHandNode;

            var leftShoulderNode = LoadJointNode(IKPart.LeftShoulder);
            leftShoulderNode.position = bodyIK.solver.leftShoulderEffector.bone.position;
            leftShoulderNode.rotation = bodyIK.solver.leftShoulderEffector.bone.rotation;
            bodyIK.solver.leftShoulderEffector.target = leftShoulderNode;

            var rightHandNode = LoadJointNode(IKPart.RightHand);
            rightHandNode.position = bodyIK.solver.rightHandEffector.bone.position;
            rightHandNode.rotation = bodyIK.solver.rightHandEffector.bone.rotation;
            bodyIK.solver.rightHandEffector.target = rightHandNode;

            var rightShoulderNode = LoadJointNode(IKPart.RightShoulder);
            rightShoulderNode.position = bodyIK.solver.rightShoulderEffector.bone.position;
            rightShoulderNode.rotation = bodyIK.solver.rightShoulderEffector.bone.rotation;
            bodyIK.solver.rightShoulderEffector.target = rightShoulderNode;

            var leftFootNode = LoadJointNode(IKPart.LeftFoot);
            leftFootNode.position = bodyIK.solver.leftFootEffector.bone.position;
            leftFootNode.rotation = bodyIK.solver.leftFootEffector.bone.rotation;
            bodyIK.solver.leftFootEffector.target = leftFootNode;

            var leftThighNode = LoadJointNode(IKPart.LeftThigh);
            leftThighNode.position = bodyIK.solver.leftThighEffector.bone.position;
            leftThighNode.rotation = bodyIK.solver.leftThighEffector.bone.rotation;
            bodyIK.solver.leftThighEffector.target = leftThighNode;

            var rightFootNode = LoadJointNode(IKPart.RightFoot);
            rightFootNode.position = bodyIK.solver.rightFootEffector.bone.position;
            rightFootNode.rotation = bodyIK.solver.rightFootEffector.bone.rotation;
            bodyIK.solver.rightFootEffector.target = rightFootNode;

            var rightThighNode = LoadJointNode(IKPart.RightThigh);
            rightThighNode.position = bodyIK.solver.rightThighEffector.bone.position;
            rightThighNode.rotation = bodyIK.solver.rightThighEffector.bone.rotation;
            bodyIK.solver.rightThighEffector.target = rightThighNode;
            
            bodyIK.solver.bodyEffector.positionWeight = 1;
            bodyIK.solver.bodyEffector.effectChildNodes = true;
            bodyIK.solver.spineStiffness = 0.5f;
            bodyIK.solver.pullBodyVertical = 0.5f;
            bodyIK.solver.pullBodyHorizontal = 0f;
            bodyIK.solver.spineMapping.iterations = 3;
            bodyIK.solver.spineMapping.twistWeight = 1;
            bodyIK.solver.headMapping.maintainRotationWeight = 0;
            bodyIK.solver.leftHandEffector.positionWeight = 1;
            bodyIK.solver.leftHandEffector.rotationWeight = 1;
            bodyIK.solver.leftShoulderEffector.positionWeight = 1;
            bodyIK.solver.rightHandEffector.positionWeight = 1;
            bodyIK.solver.rightHandEffector.rotationWeight = 1;
            bodyIK.solver.rightShoulderEffector.positionWeight = 1;
            bodyIK.solver.leftFootEffector.positionWeight = 1;
            bodyIK.solver.leftFootEffector.rotationWeight = 1;
            bodyIK.solver.leftThighEffector.positionWeight = 1;
            bodyIK.solver.rightFootEffector.positionWeight = 1;
            bodyIK.solver.rightFootEffector.rotationWeight = 1;
            bodyIK.solver.rightThighEffector.positionWeight = 1;
            
            if (isEdit)
            {
                bodyIK.solver.OnPostUpdate += () =>
                {
                    for (var i = 0; i < bodyIK.solver.effectors.Length; i++)
                    {
                        ForceUpdateBonePosition(bodyIK.solver.effectors[i]);
                    }
                };
            }

            if (!this.gameObject.TryGetComponent<LookAtIK>(out lookIK))
            {
                lookIK = AddLookAtIK();
            }
            lookIK.enabled = isEdit;
            headNode = LoadJointNode(IKPart.Head);
            lookIK.solver.IKPosition.z = headRadius;
            lookIK.solver.IKPosition.y = headHeight;
            headNode.localPosition = lookIK.solver.IKPosition;
            lookIK.solver.target = headNode;
            ReadTPoseBoneTransform();
            RecordDefaultTransform();
            SetJointNodeVisible(false);
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
        

        public override void SetWhiteMaterials()
        {
            selfMaterials = new List<Material>();
            for (var i = 0; i < skinRendererPaths.Length; i++)
            {
                var renderer = this.transform.Find(skinRendererPaths[i]).GetComponent<SkinnedMeshRenderer>();
                selfMaterials.Add(renderer.material);
                renderer.material = ShotMaterial;
            }
        }

        public override void SetDefaultMaterials()
        {
            for (var i = 0; i < skinRendererPaths.Length; i++)
            {
                var renderer = this.transform.Find(skinRendererPaths[i]).GetComponent<SkinnedMeshRenderer>();
                renderer.material = selfMaterials[i];
            }
        }




        private FullBodyBipedIK AddFullBodyBipedIK()
        {
            var bodyIk = this.gameObject.AddComponent<FullBodyBipedIK>();
            
            BipedReferences bipedRef = new BipedReferences();
            bipedRef.root = this.transform;
            bipedRef.pelvis = this.transform.Find(pelvis);
            bipedRef.leftThigh = this.transform.Find(leftThigh);
            bipedRef.leftCalf = this.transform.Find(leftCalf);
            bipedRef.leftFoot = this.transform.Find(leftFoot); 
            bipedRef.rightThigh = this.transform.Find(rightThigh); 
            bipedRef.rightCalf = this.transform.Find(rightCalf); 
            bipedRef.rightFoot = this.transform.Find(rightFoot);
            bipedRef.leftUpperArm = this.transform.Find(leftUpperArm);
            bipedRef.leftForearm = this.transform.Find(leftForearm); 
            bipedRef.leftHand = this.transform.Find(leftHand);
            bipedRef.rightUpperArm = this.transform.Find(rightUpperArm);
            bipedRef.rightForearm = this.transform.Find(rightForearm);
            bipedRef.rightHand = this.transform.Find(rightHand);
            bipedRef.head = this.transform.Find(head);
            var spineNode0 = this.transform.Find(spine0);
            var spineNode1 = this.transform.Find(spine1);
            var spineNode2 = this.transform.Find(spine2);
            bipedRef.spine = new[] {spineNode0, spineNode1, spineNode2};
            var curRootNode =  this.transform.Find(rootNode);
            bodyIk.SetReferences(bipedRef,curRootNode);
            
            bodyIk.solver.IKPositionWeight = 1;
            bodyIk.solver.iterations = 4;
            bodyIk.enabled = false;
            return bodyIk;
        }

        private LookAtIK AddLookAtIK()
        {
            var lookAt = this.gameObject.AddComponent<LookAtIK>();
            var lookAtNode = this.transform.Find(head);
            lookAt.solver.head = new IKSolverLookAt.LookAtBone(lookAtNode);
            lookAt.enabled = false;
            return lookAt;
        }


        public override void FixTransform()
        {
            if (bodyIK != null)
            {
                bodyIK.GetIKSolver().Update();
            }

            if (lookIK != null)
            {
                lookIK.GetIKSolver().Update();
            }
        }

        public override void ResetDefaultPosition()
        {
            var poseModeData = DataTables.GetPoseModeConfig((int)UgcPoseSubType.Single);
            this.transform.parent.localPosition = poseModeData.EditPos[0];
            this.transform.localPosition = poseModeData.RoleDefPos[0];
            this.transform.parent.localEulerAngles = Vector3.zero;
            this.transform.localEulerAngles = Vector3.zero;
        }

        private void ForceUpdateBonePosition(IKEffector effector)
        {
            if (effector.target != headNode && effector.target.position != effector.bone.position)
            {
                effector.target.position = effector.bone.position;
            }
        }

        public Vector3 GetHeadNodePosition()
        {
            if (headNode != null)
            {
                return headNode.localPosition;
            }

            Debug.LogError("headNode is null");
            return Vector3.zero;
        }

        private Transform LoadJointNode(IKPart part)
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
            var avatarData = poseEditDatas.Find(x => x.RoleType == (int) RoleType.Avatar && x.PartType == (int) part);
            jointNode.name = avatarData.JointName;
            jointNodes.Add((int) part, jointNode.transform);
            return jointNode.transform;
        }

        public override void SetJointNodeVisible(bool visible)
        {
            jointNodeParent.gameObject.SetActive(visible);
        }


        public override void ResetJointNodes()
        {
            WriteTPoseBoneTransform();
            foreach (var keyValue in jointNodes)
            {
                var transformData = defaultTranforms[(IKPart) keyValue.Key];
                if (keyValue.Value != null)
                {
                    keyValue.Value.localPosition = transformData.pos;
                    keyValue.Value.localRotation = transformData.rot;
                }
             
            }
        }

        private void RecordDefaultTransform()
        {
            foreach (var keyValue in jointNodes)
            {
                defaultTranforms[(IKPart) keyValue.Key] = new PoseJointTransform()
                {
                    pos = keyValue.Value.localPosition,
                    rot = keyValue.Value.localRotation
                };
            }
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

            keyFrame.roleType = RoleType.Avatar;
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