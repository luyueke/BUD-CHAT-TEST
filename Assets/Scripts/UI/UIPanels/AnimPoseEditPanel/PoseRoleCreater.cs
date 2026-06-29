using System;
using System.Collections.Generic;
using Es;
using Game.Avatar;
using Game.Pet;
using GameData.BaseInfo;
using GameData.PgcData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace BUD.AnimPose
{
    public class PoseRoleCreater
    {
        public PoseImageType curImageMode = PoseImageType.Current;
        public List<GameObject> OptNodes { get; private set; } = new();
        
        private string avatarPath = "Assets/Loadable/Avatar/CharacterBody/WhiteCharacter.prefab";
        private string petPath = "Assets/Loadable/Pet/Body/WhitePet.prefab";
        public List<BaseAnimIK> whiteRoles = new List<BaseAnimIK>();
        public List<BaseAnimIK> roles = new List<BaseAnimIK>();
        private Dictionary<PoseImageType,AnimIKController> ikControllers = new Dictionary<PoseImageType, AnimIKController>();

        private const float Radius = 0.3f;
        public UgcPoseSubType curAnimType = UgcPoseSubType.Single;
        public PoseRoleCreater Create(UgcPoseSubType animType,Transform parent = null)
        {
            curAnimType = animType;
            var poseModeData = DataTables.GetPoseModeConfig((int) animType);
            for (var i = 0; i < poseModeData.JointNames.Count; i++)
            {
                var node = new GameObject(poseModeData.JointNames[i]);
                node.transform.SetParent(parent);
                node.layer = LayerMask.NameToLayer("Model");
                OptNodes.Add(node);
            }
            return this;
        }

        public void Release()
        {
            for (var i = 0; i < OptNodes.Count; i++)
            {
                GameObject.Destroy(OptNodes[i]);
            }
        }

        public void CreateWhiteRole(GameObject refNode)
        {
            BaseAnimIK animIK = null;
            BaseAnimIK secondAnimIK = null;
            switch (curAnimType)
            {
                case UgcPoseSubType.Single:
                    animIK = CreateWhiteRole(avatarPath, refNode,true);
                    break;
                case UgcPoseSubType.PetSingle:
                    animIK = CreateWhiteRole(petPath, refNode,true);
                    break;
                case UgcPoseSubType.Double:
                    animIK = CreateWhiteRole(avatarPath, refNode,true);
                    secondAnimIK = CreateWhiteRole(avatarPath, refNode,true);
                    break;
                case UgcPoseSubType.PetWithPlayer:
                    animIK = CreateWhiteRole(avatarPath, refNode,true);
                    secondAnimIK = CreateWhiteRole(petPath, refNode,true);
                    break;
            }
            whiteRoles.Add(animIK);
            if (secondAnimIK != null)
            {
                whiteRoles.Add(secondAnimIK);
            }
            var ikController = BindIKController(animIK, whiteRoles);
            ikControllers[PoseImageType.WhiteBody] = ikController;
            SetParentDefaultPosition(PoseImageType.WhiteBody);
        }

        public AnimIKController CreateRole(CharacterData avatarData, PetData petData,bool isEdit = false)
        {
            BaseAnimIK animIK = null;
            BaseAnimIK secondAnimIK = null;
            switch (curAnimType)
            {
                case UgcPoseSubType.Single:
                    animIK = CreateRole(avatarData,isEdit);
                    break;
                case UgcPoseSubType.PetSingle:
                    animIK = CreateRole(petData,isEdit);
                    break;
                case UgcPoseSubType.Double:
                    animIK = CreateRole(avatarData,isEdit);
                    var otherAvatarData = AccountDataManager.Inst.UserInfo.otherAvatarInfo;
                    secondAnimIK = CreateRole(otherAvatarData,isEdit);
                    break;
                case UgcPoseSubType.PetWithPlayer:
                    animIK = CreateRole(avatarData,isEdit);
                    secondAnimIK = CreateRole(petData,isEdit);
                    break;
            }
            roles.Add(animIK);
            if (secondAnimIK != null)
            {
                roles.Add(secondAnimIK);
            }
            var ikController = BindIKController(animIK, roles);
            ikControllers[PoseImageType.Current] = ikController;
            SetParentDefaultPosition(PoseImageType.Current);
            return ikController;
        }

        public void SetCurImageParentDefaultPosition()
        {
            SetParentDefaultPosition(curImageMode);
        }

        private void SetParentDefaultPosition(PoseImageType imageType)
        {
            var animIKs = ikControllers[imageType].GetAnimIKs();
            var poseModeData = DataTables.GetPoseModeConfig((int) curAnimType);
            for (var i = 0; i < OptNodes.Count; i++)
            {
                animIKs[i].transform.localPosition = poseModeData.RoleDefPos[i];
                OptNodes[i].transform.localPosition = poseModeData.EditPos[i];
                OptNodes[i].transform.localEulerAngles = Vector3.zero;
            }
        }


        public void RemovePropIK(PropAnimIK propIk)
        {
            foreach (var keyValue in ikControllers)
            {
                keyValue.Value.RemovePropIK(propIk);
            }
        }

        public void AddFullBodyJointNodeCollider()
        {
            var poseModeData = DataTables.GetPoseModeConfig((int) curAnimType);
            for (var i = 0; i < OptNodes.Count; i++)
            {
                var bodyCollider = OptNodes[i].AddComponent<CapsuleCollider>();
                bodyCollider.radius = Radius;
                bodyCollider.height = poseModeData.roleHeight[i];
            }
        }


        // private void SetParentDefaultPosition(List<BaseAnimIK> animIKs)
        // {
        //     var poseModeData = DataTables.GetPoseModeConfig((int) curAnimType);
        //     for (var i = 0; i < OptNodes.Count; i++)
        //     {
        //         animIKs[i].transform.localPosition = poseModeData.RoleDefPos[i];
        //         OptNodes[i].transform.localPosition = poseModeData.EditPos[i];
        //     }
        // }

        private void ResetMoveNodePosition()
        {
            var poseModeData = DataTables.GetPoseModeConfig((int) curAnimType);
            for (var i = 0; i < OptNodes.Count; i++)
            {
                OptNodes[i].transform.localPosition = poseModeData.EditPos[i];
            }
        }

        private AnimIKController BindIKController(BaseAnimIK animIK, List<BaseAnimIK> animIKs)
        {
            var ikController = animIK.gameObject.AddComponent<AnimIKController>();
            for (var i = 0; i < animIKs.Count; i++)
            {
                BindIKAnim(animIKs[i],OptNodes[i].transform,ikController);
            }
            return ikController;
        }

        private void BindIKAnim(BaseAnimIK animIK,Transform parent,AnimIKController ikController)
        {
            if (animIK != null)
            {
                animIK.transform.SetParent(parent);
                ikController.AddAnimIK(animIK);
            }
        }


        private BaseAnimIK CreateWhiteRole(string prefabPath, GameObject refNode,bool isEdit = false)
        {
            var avatar = Loader.Load<GameObject>(prefabPath, refNode);
            var avatarInstance = GameObject.Instantiate(avatar);
            var animIK = avatarInstance.GetComponent<BaseAnimIK>();
            animIK.SetOptEntity(animIK.gameObject);
            animIK.InitAnimIK(isEdit);
            return animIK;
        }

        private BaseAnimIK CreateRole(CharacterData data,bool isEdit = false)
        {
            var characterWrap = AvatarController.Inst.CreateAnimAvatar(data,isEdit);
            var animIK = characterWrap.Avatar.GetComponent<BaseAnimIK>();
            return animIK;
        }

        private BaseAnimIK CreateRole(PetData data,bool isEdit = false)
        {
            var characterWrap = PetAvatarController.Inst.CreateAnimAvatar(data,isEdit);
            var animIK = characterWrap.Avatar.GetComponent<BaseAnimIK>();
            return animIK;
        }

        public void ResetJointNodes()
        {
            for (var i = 0; i < whiteRoles.Count; i++)
            {
                whiteRoles[i].ResetJointNodes();
            }

            for (var i = 0; i < roles.Count; i++)
            {
                roles[i].ResetJointNodes();;
            }
            ResetMoveNodePosition();
        }


        public void ChangeImageModeAction(PoseImageType imageType)
        {
            var ikController = ikControllers[PoseImageType.Current];
            var whiteIKController = ikControllers[PoseImageType.WhiteBody];
            List<BaseAnimIK> orgAnimIks = null;
            List<BaseAnimIK> copyAnimIks = null;
            AnimIKController orgIKController = null;
            AnimIKController copyIkController = null;
            switch (imageType)
            {
                case PoseImageType.Current:
                    orgAnimIks = whiteRoles;
                    copyAnimIks = roles;
                    orgIKController = whiteIKController;
                    copyIkController = ikController;
                    break;
                case PoseImageType.WhiteBody:
                    orgAnimIks = roles;
                    copyAnimIks = whiteRoles;
                    orgIKController = ikController;
                    copyIkController = whiteIKController;
                    break;
            }
            for (var i = 0; i < orgAnimIks.Count; i++)
            {
                CopyJointNode(orgAnimIks[i],copyAnimIks[i]);
            }
            var propIks = orgIKController.GetPropIKs();
            foreach (var keyValue in propIks)
            {
                var propIk = keyValue.Value;
                var par = copyIkController.GetBindNode(propIk.poseType, propIk.bindIndex);
                propIk.transform.SetParent(par);
                propIk.UpdateBindNodes(copyIkController.GetBindNodes(propIk.poseType));
                copyIkController.SetPropIks(propIks);
            }
            
            for (var i = 0; i < whiteRoles.Count; i++)
            {
                whiteRoles[i].gameObject.SetActive(imageType == PoseImageType.WhiteBody);
                whiteRoles[i].FixTransform();
            }

            for (var i = 0; i < roles.Count; i++)
            {
                roles[i].gameObject.SetActive(imageType == PoseImageType.Current);
                roles[i].FixTransform();
            }
        }
        

        private void CopyJointNode(BaseAnimIK orgAnim, BaseAnimIK copyAnim)
        {
            SyncTransform(orgAnim.transform, copyAnim.transform);
            for (int i = 0; i < copyAnim.jointNodeParent.childCount; i++)
            {
                var ordChild = orgAnim.jointNodeParent.GetChild(i).transform;
                var child = copyAnim.jointNodeParent.GetChild(i).transform;
                SyncTransform(ordChild, child);
            }
        }

        public void SetKeyFrameData(KeyFrameData data)
        {
            foreach (var keyValue in ikControllers)
            {
                keyValue.Value.SetKeyFrameData(curAnimType,data);
            }
        }

        public void SetJointNodeInVisible()
        {
            foreach (var keyValue in ikControllers)
            {
                keyValue.Value.SetJointNodeVisible(false);
            }
        }

        
        public void SaveKeyFrameData(KeyFrameData data)
        {
            if (ikControllers.TryGetValue(curImageMode,out var ikController))
            {
                ikController.SaveKeyFrameData(data);
            }
        }
        
        
        private void SyncTransform(Transform org, Transform copy)
        {
            copy.localPosition = org.localPosition;
            copy.localRotation = org.localRotation;
            copy.localScale = org.localScale;
        }

        public AnimIKController GetIkController(PoseImageType imageMode)
        {
            if (ikControllers.ContainsKey(imageMode))
            {
                return ikControllers[imageMode];
            }
            return null;
        }

        public AnimIKController GetCurrentIkController()
        {
            return GetIkController(curImageMode);
        }
    }
}