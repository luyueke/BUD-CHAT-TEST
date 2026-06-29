using System;
using System.Collections.Generic;
using System.Linq;
using BUD.AnimPose;
using Es;
using Game.Avatar;
using Game.KinematicCharacter;
using GameData.BaseInfo;
using GameData.PgcData;
using Pb.Base;
using RootMotion.FinalIK;
using UnityEngine;
namespace Game.Pet {
    public class PetAvatarController : MonoBehaviour {
        private static PetAvatarController _inst;
        public static PetAvatarController Inst {
            get {
                if (_inst == null) {
                    LoggerUtils.Log("AvatarController Create");
                    _inst = new GameObject("PetAvatarController").AddComponent<PetAvatarController>();
                    DontDestroyOnLoad(_inst.gameObject);
                }
                return _inst;
            }
        }

        private Dictionary<string, PetWrap> petDic = new Dictionary<string, PetWrap>();
        Action<string, KinematicCharacterController> OnPetCreate;
        Action<string> OnPetRemove;

        public PetWrap CreateUIAvatar(PetData data = null) {
            var uiAvatarWrap = CreateAvatar(data,true);

            return uiAvatarWrap;
        }

        public PetWrap CreateUIAvatarWithIK(PetData data,Transform parent,bool isEdit = false)
        {
            var avatarWrap = CreateAvatar(data,true);
            var poseModeData = DataTables.GetPoseModeConfig((int)UgcPoseSubType.PetSingle);
            
            //UI界面位置偏移节点
            var nodePar = new GameObject(poseModeData.JointNames[0]).transform;
            var petLocalScale = avatarWrap.Avatar.transform.localScale;
            nodePar.SetParent(parent);
            nodePar.localEulerAngles = Vector3.zero;
            nodePar.localPosition = Vector3.zero;
            nodePar.localScale = Vector3.one;
            var node = new GameObject(poseModeData.JointNames[0]).transform;
            node.SetParent(nodePar);
            node.localEulerAngles = Vector3.zero;
            node.localScale = Vector3.one;
            node.localPosition = poseModeData.EditPos[0];
            avatarWrap.SetParent(node,true);
            var animIK = avatarWrap.Avatar.GetComponent<BaseAnimIK>();
            animIK.transform.localPosition = poseModeData.RoleDefPos[0];
            animIK.SetOptEntity(avatarWrap.Avatar);
            animIK.InitAnimIK(isEdit);
            avatarWrap.CustomAvatar = nodePar.gameObject;
            avatarWrap.Avatar.transform.localScale = petLocalScale;
            return avatarWrap;
        }
        
        public PetWrap CreateUIAvatarWithIKController(PetData data,Transform parent,bool isEdit = false)
        {
            var avatarWrap = CreateUIAvatarWithIK(data,parent,isEdit);
            var animIK = avatarWrap.Avatar.GetComponent<BaseAnimIK>();
            var ikController = animIK.gameObject.AddComponent<AnimIKController>();
            ikController.AddAnimIK(animIK);
            return avatarWrap;
        }
        
        //仅仅编辑器使用，禁止其他业务调用
        public PetWrap CreateAnimAvatar(PetData data,bool isEdit = false)
        {
            var avatarWrap = CreateAvatar(data,false);
            var animator = avatarWrap.Avatar.GetComponent<Animator>();
            var animIK = avatarWrap.Avatar.GetComponent<BaseAnimIK>();
            animIK.SetOptEntity(avatarWrap.Avatar);
            animIK.InitAnimIK(isEdit);
            var iks = avatarWrap.Avatar.GetComponents<CCDIK>();
            animator.enabled = false;
            animIK.enabled = true;
            for (var i = 0; i < iks.Length; i++)
            {
                iks[i].enabled = true;
            }
            return avatarWrap;
        }

        private PetWrap CreateAvatar(PetData data,bool isUI, Action callback = null) {
            var avatar = Loader.Load<GameObject>("Assets/Loadable/Pet/Body/Pet.prefab", this.gameObject);
            var avatarInstance = GameObject.Instantiate(avatar);
            var petAnimationCtrl = avatarInstance.AddComponent<PetAnimationCtrl>();
            avatarInstance.AddComponent<PetCustomBodyTypeController>();
            var wrap = new PetWrap(avatarInstance, isUI);
            petAnimationCtrl.Init(wrap);
            if (data != null) wrap.SetData(data.Clone(),callback);
            return wrap;
        }

        public PetWrap CreatePetGameAvatar(PetData data, PlayerInfo playerInfo, Action complete = null)
        {
            var playerId = playerInfo.Uid;
            if (data == null) data = new PetData() { partDatas = new() };
            var kinematicCharacterPrefab = Loader
                .Load<GameObject>("Assets/Loadable/Avatar/CharacterBody/KinematicCharacter.prefab").RetainAsset(gameObject);
            var kinematicCharacter = GameObject.Instantiate(kinematicCharacterPrefab, this.transform);
            kinematicCharacter.layer = LayerMask.NameToLayer("Model");
            kinematicCharacter.transform.localScale = Vector3.one;
            var petWrap = CreateAvatar(data, true, complete);
            petWrap.SetParent(kinematicCharacter.transform.GetChild(0), true);
            var petController = kinematicCharacter.GetComponent<KinematicCharacterController>();
            var animController = petWrap.Avatar.GetComponent<PetAnimationCtrl>();
            petWrap.kinematicCharacterController = petController;
            AddGamePet(petWrap, playerId);
            kinematicCharacter.SetActive(playerInfo.HiddenPet == 0);
            var petId = "pet-" + playerId;
            animController.SetPlayerID(petId);
            petController.Init(animController, petId, false);
            UserInfoHeadView.Load(petController.gameObject, new PlayerInfo() { Name = playerInfo.PetName}, SkinType.Pet);
            OnPetCreate?.Invoke(petId, petController);
            return petWrap;
        }

        public KinematicCharacterController GetOrCreatePetGameAvatar(PetData petData, PlayerInfo playerInfo)
        {
            var ownerId = playerInfo.Uid;
            var petWrap = GetPetKCCtrl(ownerId);
            if (petWrap != null)
            {
                LoggerUtils.Log("##GetOrCreateOtherGameAvatar 已存在该玩家，更新角色穿着");
                petWrap.RefreshAvatar(petData);
                return petWrap.kinematicCharacterController;
            }
            petWrap = CreateGamePetWithIKController(petData, playerInfo);
            return petWrap.kinematicCharacterController;
        }

        private PetWrap CreateGamePetWithIK(PetData data, PlayerInfo playerInfo, Action complete = null)
        {
            var petWrap = CreatePetGameAvatar(data,playerInfo,complete);
            var poseModeData = DataTables.GetPoseModeConfig((int)UgcPoseSubType.PetSingle);
            
            //UI界面位置偏移节点
            var nodePar = new GameObject(poseModeData.JointNames[0]).transform;
            nodePar.SetParent(petWrap.Avatar.transform.parent);
            nodePar.localEulerAngles = Vector3.zero;
            nodePar.localPosition = Vector3.zero;
            nodePar.localScale = Vector3.one;
            
            var node = new GameObject(poseModeData.JointNames[0]).transform;
            node.SetParent(nodePar);
            node.localEulerAngles = Vector3.zero;
            node.localScale = Vector3.one;
            node.localPosition = poseModeData.EditPos[0];
            petWrap.SetParent(node,true);
            
            var animIK = petWrap.Avatar.GetComponent<BaseAnimIK>();
            animIK.transform.localPosition = poseModeData.RoleDefPos[0];
            animIK.SetOptEntity(petWrap.Avatar);
            animIK.InitAnimIK(false);
            petWrap.CustomAvatar = nodePar.gameObject;
            return petWrap;
        }
        
        public PetWrap CreateGamePetWithIKController(PetData data, PlayerInfo playerInfo, Action complete = null)
        {
            var avatarWrap = CreateGamePetWithIK(data,playerInfo,complete);
            var animIK = avatarWrap.Avatar.GetComponent<BaseAnimIK>();
            var ikController = animIK.gameObject.AddComponent<AnimIKController>();
            ikController.AddAnimIK(animIK);
            return avatarWrap;
        }
        
        
        public void AddGamePet(PetWrap petWrap, string ownerId)
        {
            if (petDic.ContainsKey(ownerId))
            {
                petDic[ownerId] = petWrap;
            }
            else
            {
                petDic.Add(ownerId, petWrap);
            }
        }

        public PetWrap GetPetKCCtrl(string ownerId)
        {
            PetWrap petWrap;

            if (petDic.TryGetValue(ownerId, out petWrap))
            {
                return petWrap;
            }

            LoggerUtils.LogError("GetPetWrap - The ownerId could not be found in the GetPetWrap");
            return null;
        }

        public void DestorySelfGameAvatar(KinematicCharacterController controller)
        {
            if (controller != null)
            {
                GameObject.Destroy(controller.gameObject);
            }
        }

        public void DestoryOtherPet()
        {
            var playerIds = petDic.Keys.ToList();
            for (int i = 0; i < playerIds.Count; i++)
            {
                RemoveGamePet(playerIds[i]);
            }
        }

        public void RemoveGamePet(string playerID)
        {
            if (petDic.ContainsKey(playerID))
            {
                OnPetRemove?.Invoke(playerID);
                petDic[playerID]?.DestorySelf();
                Destroy(petDic[playerID].kinematicCharacterController.gameObject);
                petDic.Remove(playerID);
            }
        }
    }


}
