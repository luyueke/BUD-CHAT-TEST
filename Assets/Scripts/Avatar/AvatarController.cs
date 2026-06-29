using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BUD.AnimPose;
using Es;
using Game.Config;
using Game.KinematicCharacter;
using Game.Pet;
using GameData.BaseInfo;
using GameData.PgcData;
using GameData.UGCData;
using Message;
using Pb.Base;
using RootMotion.FinalIK;
using UnityEngine;
using xasset;
using Debug = UnityEngine.Debug;

namespace Game.Avatar
{

    public class AvatarController : MonoBehaviour
    {
        public KinematicCharacterController SelfController { get; private set; }
        public PlayerStateController SelfStateController { get; private set; }
        public AvatarRaycast SelfAvatarRaycast { get; private set; }
        public AvatarTrigger SelfAvatarTrigger { get; private set; }

        //游玩、试玩中self Avatar
        public CharacterWrap SelfWrap { get; private set; }
        private Dictionary<string, PlayerStateController> playerDic = new Dictionary<string, PlayerStateController>();

        private static AvatarController _inst;
        public static AvatarController Inst
        {
            get
            {
                if (_inst == null)
                {
                    LoggerUtils.Log("AvatarController Create");
                    _inst = new GameObject("CharacterController").AddComponent<AvatarController>();
                    DontDestroyOnLoad(_inst.gameObject);
                }
                return _inst;
            }
        }

        Action<string, KinematicCharacterController> OnAvatarCreate;
        Action<string> OnAvatarRemove;

        public Vector3 GetSelfAvatarPosition()
        {
            if (SelfController != null)
            {
                return SelfController.transform.position;
            }
            return Vector3.zero;
        }

        public Dictionary<string, PlayerStateController> GetDic()
        {
            return playerDic;
        }

        public void DestorySelfGameAvatar(KinematicCharacterController controller)
        {
            if (controller != null)
            {
                GameObject.Destroy(controller.gameObject);
            }
        }


        public void AddPetMoveController(KinematicCharacterController petController, KinematicCharacterController playerController, string playerId)
        {
            PetMoveControllerKcc petMoveCtr;
            if (!petController.gameObject.TryGetComponent<PetMoveControllerKcc>(out petMoveCtr))
            {
                petMoveCtr = petController.gameObject.AddComponent<PetMoveControllerKcc>();
            }
            petMoveCtr.InitPlayer(playerController.transform, playerId);
        }

        public KinematicCharacterController CreateSelfGameAvatar(CharacterData data, PetData petData = null)
        {
            var cameraTarget = GameObject.Find("CameraTarget");
            var kinematicCharacterPrefab = Loader
                .Load<GameObject>("Assets/Loadable/Avatar/CharacterBody/KinematicCharacter.prefab").RetainAsset(gameObject);
            var kinematicCharacter = GameObject.Instantiate(kinematicCharacterPrefab, this.transform);
            SelfAvatarRaycast = kinematicCharacter.AddComponent<AvatarRaycast>();
            SelfAvatarTrigger = kinematicCharacter.AddComponent<AvatarTrigger>();
            kinematicCharacter.transform.localScale = Vector3.one;
            SelfWrap = CreateGameAvatarWithIKController(data, kinematicCharacter.transform.GetChild(0),false);
            SelfController =
                kinematicCharacter.GetComponent<KinematicCharacterController>();
            var animController = SelfWrap.Avatar.GetComponent<PlayerAnimationCtrl>();
            SelfStateController = SelfWrap.Avatar.AddComponent<SelfStateController>();
            var selfPlayerInfo = new PlayerInfo()
            {
                Name = AccountDataManager.Inst.UserInfo.nickname,
                Uid = AccountDataManager.Inst.Uid,
                HiddenPet = AccountDataManager.Inst.PetInfo.isGameHidden,
                PetName = AccountDataManager.Inst.PetInfo.nickname,
            };
            var petController = PetAvatarController.Inst.GetOrCreatePetGameAvatar(petData ?? new PetData() { partDatas = new() }, selfPlayerInfo);
            SelfStateController.Init(animController, SelfWrap, SelfController, petController, AccountDataManager.Inst.Uid);
            AddGamePlayer(SelfStateController);
            if(cameraTarget != null)
            {
                SelfController.CameraTarget = cameraTarget.transform;
            }

            AddPetMoveController(petController, SelfController, AccountDataManager.Inst.Uid);


            OnAvatarCreate?.Invoke(AccountDataManager.Inst.Uid, SelfController);
            return SelfController;
        }

        public KinematicCharacterController CreateOtherGameAvatar(CharacterData data, PetData petData, PlayerInfo playerInfo, Action complete = null)
        {
            var otherPlayerId = playerInfo.Uid;
            var kinematicCharacterPrefab = Loader
                .Load<GameObject>("Assets/Loadable/Avatar/CharacterBody/KinematicCharacter.prefab").RetainAsset(gameObject);
            var kinematicCharacter = GameObject.Instantiate(kinematicCharacterPrefab, this.transform);
            kinematicCharacter.layer = LayerMask.NameToLayer("Model");
            kinematicCharacter.transform.localScale = Vector3.one;
            var otherWrap = CreateGameAvatarWithIKController(data, kinematicCharacter.transform.GetChild(0), true, complete);
            var otherController =
                kinematicCharacter.GetComponent<KinematicCharacterController>();
            var animController = otherWrap.Avatar.GetComponent<PlayerAnimationCtrl>();
            var otherStateController = otherWrap.Avatar.AddComponent<OtherStateController>();
            var petController = PetAvatarController.Inst.GetOrCreatePetGameAvatar(petData ?? new PetData() { partDatas = new() }, playerInfo);
            otherStateController.Init(animController, otherWrap, otherController, petController, otherPlayerId);
            AddGamePlayer(otherStateController);

            AddPetMoveController(petController, otherController, otherPlayerId);
            otherController.Motor.SetCapsuleHeightData((CustomBodyTypeController.BodyType)data.bodyType);

            OnAvatarCreate?.Invoke(otherPlayerId, otherController);
            return otherController;
        }

        public KinematicCharacterController GetOrCreateOtherGameAvatar(CharacterData avatarData, PetData petData, PlayerInfo otherPlayerInfo)
        {
            var otherPlayerId = otherPlayerInfo.Uid;
            var otherStateCtrl = GetPlayerStateCtrl(otherPlayerId);
            if (otherStateCtrl != null && otherStateCtrl.PlayerKCCtrl != null)
            {
                LoggerUtils.Log("##GetOrCreateOtherGameAvatar 已存在该玩家，更新角色穿着");
                otherStateCtrl.Wrap.RefreshAvatar(avatarData);
                //otherStateCtrl.PlayerKCCtrl.Motor.SetCapsuleHeightData((CustomBodyTypeController.BodyType)avatarData.bodyType);
                return otherStateCtrl.PlayerKCCtrl;
            }

            var otherPlayerKCCtrl = CreateOtherGameAvatar(avatarData, petData, otherPlayerInfo);
            return otherPlayerKCCtrl;
        }


        /// <summary>
        /// 批量创建其他玩家，和本地数据对比差异
        /// </summary>
        public void CreateDiffBatchOtherGameAvatar(List<PlayerStatus> players, Action complete = null)
        {
            var existPlayerIds = playerDic.Keys.ToList();
            int createCount = 0;
            Action callback = () =>
            {
                createCount++;
                if (createCount == players.Count)
                {
                    complete?.Invoke();
                }
            };
            for (int i = 0; i < players.Count; i++)
            {
                var playerInfo = players[i].PlayerInfo;
                if (playerInfo.Uid == AccountDataManager.Inst.Uid)
                {
                    callback?.Invoke();
                    continue;
                }
                var avatarData = CharacterData.DeserializeObject(playerInfo.AvatarJson);
                var petAvatarData = PetData.DeserializeObject(playerInfo.PetAvatarJson);
                if (existPlayerIds.Contains(playerInfo.Uid))
                {
                    // 已经存在, 刷新着装
                    RefreshAvatarByData(playerInfo.Uid, avatarData);
                    //playerDic[playerInfo.Uid].Wrap.RefreshAvatar(avatarData, callback);
                    existPlayerIds.Remove(playerInfo.Uid);
                }
                else
                {
                    // 不存在，创建
                    var otherPlayer = AvatarController.Inst.CreateOtherGameAvatar(avatarData, petAvatarData, playerInfo, callback);
                    UserInfoHeadView.Load(otherPlayer.gameObject, playerInfo);
                    //otherPlayer.Motor.SetCapsuleHeightData((CustomBodyTypeController.BodyType)avatarData.bodyType);
                }
                if (playerDic.ContainsKey(playerInfo.Uid) && playerDic[playerInfo.Uid] != null && playerDic[playerInfo.Uid].PetKCCtrl != null)
                {
                    playerDic[playerInfo.Uid].PetKCCtrl.gameObject.SetActive(playerInfo.HiddenPet == 0);
                }
            }

            // 剩余的说明已经不存在了
            for (int i = 0; i < existPlayerIds.Count; i++)
            {
                if (existPlayerIds[i] == AccountDataManager.Inst.Uid)
                {
                    continue;
                }
                RemoveGamePlayer(existPlayerIds[i]);
            }
        }

        public void RefreshAvatarByData(string playerId, CharacterData data,Action complete = null)
        {
            var existPlayerIds = playerDic.Keys.ToList();
            if (existPlayerIds.Contains(playerId))
            {
                playerDic[playerId].Wrap.RefreshAvatar(data, complete);
                playerDic[playerId].PlayerKCCtrl.Motor.SetCapsuleHeightData((CustomBodyTypeController.BodyType)data.bodyType);  
            }
        }


        public CharacterWrap CreateUIAvatar(CharacterData data = null, Action callback = null)
        {
            var uiAvatarWrap = CreateAvatar(data, callback: callback, isUIPreview: true);
            return uiAvatarWrap;
        }

        //仅仅编辑器使用，禁止其他业务调用
        public CharacterWrap CreateAnimAvatar(CharacterData data, bool isEdit = false)
        {
            var avatarWrap = CreateAvatarInfo(data);
            var animator = avatarWrap.Avatar.GetComponent<Animator>();
            animator.enabled = false;
            if (data != null) avatarWrap.SetCharacterData(data.Clone());
            ChangePgcEye(avatarWrap);
            var animIK = avatarWrap.Avatar.GetComponent<BaseAnimIK>();
            animIK.SetOptEntity(avatarWrap.Avatar);
            animIK.InitAnimIK(isEdit);
            var fullBodyIK = avatarWrap.Avatar.GetComponent<FullBodyBipedIK>();
            var lookAtIK = avatarWrap.Avatar.GetComponent<LookAtIK>();
            animIK.enabled = true;
            fullBodyIK.enabled = true;
            lookAtIK.enabled = true;
            return avatarWrap;
        }


        public CharacterWrap CreateUIAvatarWithIK(CharacterData data, Transform parent, bool isEdit = false, Action callback = null)
        {
            var avatarWrap = CreateUIAvatar(data, callback);
            ChangePgcEye(avatarWrap);
            var poseModeData = DataTables.GetPoseModeConfig((int)UgcPoseSubType.Single);
            //UI界面位置偏移节点
            var nodePar = new GameObject(poseModeData.JointNames[0]).transform;
            nodePar.SetParent(parent);
            nodePar.localEulerAngles = Vector3.zero;
            nodePar.localPosition = Vector3.zero;
            nodePar.localScale = Vector3.one;

            var node = new GameObject(poseModeData.JointNames[0]).transform;
            node.SetParent(nodePar);
            node.localEulerAngles = Vector3.zero;
            node.localPosition = poseModeData.EditPos[0];
            node.localScale = Vector3.one;
            avatarWrap.SetParent(node, true);

            var animIK = avatarWrap.Avatar.GetComponent<BaseAnimIK>();
            animIK.transform.localPosition = poseModeData.RoleDefPos[0];
            animIK.SetOptEntity(avatarWrap.Avatar);
            animIK.InitAnimIK(isEdit);
            avatarWrap.CustomAvatar = nodePar.gameObject;
            var bodyType = avatarWrap.Avatar.GetComponent<CustomBodyTypeController>();
            switch(bodyType.GetCurrentBodyType())
            {
                case CustomBodyTypeController.BodyType.Type6:
                    //avatarWrap.Avatar.transform.localPosition.y 
                    break;
                case CustomBodyTypeController.BodyType.Type4:
                    avatarWrap.Avatar.transform.localPosition = new Vector3(avatarWrap.Avatar.transform.localPosition.x, avatarWrap.Avatar.transform.localPosition.y + 0.2f, avatarWrap.Avatar.transform.localPosition.z);
                    break;
            }
            return avatarWrap;
        }

        private void ChangePgcEye(CharacterWrap avatarWrap)
        {
            var eyePartData = avatarWrap.GetPartData(UniqueType.GetAvatar(AvatarSubType.Eyes));
            if (eyePartData != null)
            {
                ResourceType resourceType = UniqueType.ResourceType(eyePartData.Type);
                if (resourceType == ResourceType.Avatar)
                {
                    avatarWrap.ChangePart(eyePartData.Type, eyePartData.Id, null);
                }
            }
        }

        public void ChangeSpecialPatternTex(Texture tex, Texture mask, out Texture curTex, out Texture maskTex)
        {
            var faceAdapter = SelfWrap.GetPartAdapter(UniqueType.GetAvatar(AvatarSubType.FacePaint)) as FacePaintPartAdapter;
            var faceMat = faceAdapter.skinRenderer.material;
            curTex = faceMat.GetTexture("_patterns_tex");
            maskTex = faceMat.GetTexture("_patterns_mask_tex");
            faceMat.SetTexture("_patterns_tex", tex);
            faceMat.SetTexture("_patterns_mask_tex", mask);
        }

        public CharacterWrap CreateUIAvatarWithIKController(CharacterData data, Transform parent, bool isEdit = false, Action callback = null)
        {
            var avatarWrap = CreateUIAvatarWithIK(data, parent, isEdit, callback);
            var animIK = avatarWrap.Avatar.GetComponent<BaseAnimIK>();
            var ikController = animIK.gameObject.AddComponent<AnimIKController>();
            ikController.AddAnimIK(animIK);

            if(avatarWrap.Avatar.TryGetComponent<CustomBodyTypeController>(out var shapCtrl))
            {
                CustomBodyTypeController.BodyType type = data.bodyType == 0 ? CustomBodyTypeController.BodyType.None : (CustomBodyTypeController.BodyType)data.bodyType;
                shapCtrl.ApplyBodyType(type);
            }
            return avatarWrap;
        }

        public CharacterWrap CreateGameAvatarWithIK(CharacterData data, Transform parent, bool isOtherPlayer = false, Action callback = null)
        {
            var avatarWrap = CreateAvatar(data, isOtherPlayer, callback);
            ChangePgcEye(avatarWrap);
            var poseModeData = DataTables.GetPoseModeConfig((int)UgcPoseSubType.Single);
            //UI界面位置偏移节点
            var nodePar = new GameObject(poseModeData.JointNames[0]).transform;
            nodePar.SetParent(parent);
            nodePar.localEulerAngles = Vector3.zero;
            nodePar.localPosition = Vector3.zero;
            nodePar.localScale = Vector3.one;

            var node = new GameObject(poseModeData.JointNames[0]).transform;
            node.SetParent(nodePar);
            node.localEulerAngles = Vector3.zero;
            node.localPosition = poseModeData.EditPos[0];
            node.localScale = Vector3.one;
            avatarWrap.SetParent(node, true);

            var vehicleAnimator = node.parent.gameObject.AddComponent<Animator>();
            var avatarAnimator = avatarWrap.Avatar.transform.Find("vehiclepos").GetComponent<Animator>();
            vehicleAnimator.runtimeAnimatorController = avatarAnimator.runtimeAnimatorController;

            var animIK = avatarWrap.Avatar.GetComponent<BaseAnimIK>();
            animIK.transform.localPosition = poseModeData.RoleDefPos[0];
            animIK.SetOptEntity(avatarWrap.Avatar);
            animIK.InitAnimIK(false);
            avatarWrap.CustomAvatar = nodePar.gameObject;

            if(avatarWrap.Avatar.TryGetComponent<CustomBodyTypeController>(out var shapCtrl))
            {
                CustomBodyTypeController.BodyType type = data.bodyType == 0 ? CustomBodyTypeController.BodyType.None : (CustomBodyTypeController.BodyType)data.bodyType;
                shapCtrl.ApplyBodyType(type);
            }
            return avatarWrap;
        }


        public CharacterWrap CreateGameAvatarWithIKController(CharacterData data, Transform parent, bool isOtherPlayer = false, Action callback = null)
        {
            var avatarWrap = CreateGameAvatarWithIK(data, parent, isOtherPlayer, callback);
            var animIK = avatarWrap.Avatar.GetComponent<BaseAnimIK>();
            var ikController = animIK.gameObject.AddComponent<AnimIKController>();
            ikController.AddAnimIK(animIK);
            return avatarWrap;
        }

        private CharacterWrap CreateAvatarInfo(CharacterData data, bool isOtherPlayer = false)
        {
            var avatar = Loader.Load<GameObject>("Assets/Loadable/Avatar/CharacterBody/Character.prefab", this.gameObject);
            var avatarInstance = GameObject.Instantiate(avatar);
            var playerAnimationCtrl = avatarInstance.AddComponent<PlayerAnimationCtrl>();
            var bodyTypeCtrl = avatarInstance.AddComponent<CustomBodyTypeController>();
            CustomBodyTypeController.BodyType type = data.bodyType == 0 ? CustomBodyTypeController.BodyType.None : (CustomBodyTypeController.BodyType)data.bodyType;
            bodyTypeCtrl.ApplyBodyType(type);
            //  bodyTypeCtrl.ApplyBodyType(CustomBodyTypeController.BodyType.Type1);

            CharacterWrap wrap = new CharacterWrap(avatarInstance, isOtherPlayer);
            playerAnimationCtrl.Init(wrap);
            var playerHoldBehaviour = avatarInstance.AddComponent<PlayerHoldBehaviour>();
            playerHoldBehaviour.Init(wrap);
            return wrap;
        }

        public CharacterWrap AddAIAvatar(CharacterData data, GameObject avatarInstance, bool isOtherPlayer = false, Action callback = null)
        {
            var playerAnimationCtrl = avatarInstance.AddComponent<PlayerAnimationCtrl>();
            CharacterWrap wrap = new CharacterWrap(avatarInstance, isOtherPlayer);
            playerAnimationCtrl.Init(wrap);
            var otherStateController = avatarInstance.AddComponent<OtherStateController>();
            var playerHoldBehaviour = avatarInstance.AddComponent<PlayerHoldBehaviour>();
            playerHoldBehaviour.Init(wrap);
            if (data != null) wrap.SetCharacterData(data.Clone(), callback);
            return wrap;
        }


        private CharacterWrap CreateAvatar(CharacterData data, bool isOtherPlayer = false, Action callback = null, bool isUIPreview = false)
        {
            var wrap = CreateAvatarInfo(data, isOtherPlayer);
            // UI 预览角色：在应用部件(SetCharacterData→RefreshSkinInfo)之前打标记，使特殊皮肤本体/特效走 preview；
            // 场景/局内不传 isUIPreview → 默认 false → 维持 base，零影响。
            if (isUIPreview)
            {
                var previewCtrl = wrap.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
                if (previewCtrl != null) previewCtrl.UIPreviewIdleMode = true;
            }
            if (data != null) wrap.SetCharacterData(data.Clone(), callback);
            return wrap;
        }

        public PlayerStateController GetPlayerStateCtrl(string playerID)
        {
            PlayerStateController playerStateController;

            if (playerDic.TryGetValue(playerID, out playerStateController))
            {
                return playerStateController;
            }

            LoggerUtils.Log("GetPlayerStateCtrl - The playerID could not be found in the playerStateDic--", playerID);
            return null;
        }

        public void AddGamePlayer(PlayerStateController playerStateCtrl)
        {
            if (playerDic.ContainsKey(playerStateCtrl.PlayerID))
            {
                playerDic[playerStateCtrl.PlayerID] = playerStateCtrl;
            }
            else
            {
                playerDic.Add(playerStateCtrl.PlayerID, playerStateCtrl);
            }
        }

        public void RemoveGamePlayer(string playerID)
        {
            if (playerDic.ContainsKey(playerID))
            {
                OnAvatarRemove?.Invoke(playerID);
                playerDic[playerID].Wrap?.DestorySelf();
                try
                {
                    Destroy(playerDic[playerID].PlayerKCCtrl.gameObject);
                }
                catch (System.Exception e)
                {
                    Debug.LogError("RemoveGamePlayer:" + e.Message);
                }
                playerDic.Remove(playerID);

                PetAvatarController.Inst.RemoveGamePet(playerID);
            }
        }

        public void DestoryOtherPlayer()
        {
            var playerIds = playerDic.Keys.ToList();
            for (int i = 0; i < playerIds.Count; i++)
            {
                RemoveGamePlayer(playerIds[i]);
            }
        }

        public void OnDestroy()
        {
            OnAvatarCreate = null;
            OnAvatarRemove = null;
        }

        public void AddAvatarCreateListener(Action<string, KinematicCharacterController> action)
        {
            OnAvatarCreate += action;
        }

        public void RemoveAvatarCreateListener(Action<string, KinematicCharacterController> action)
        {
            OnAvatarCreate -= action;
        }

        public void AddAvatarRemoveListener(Action<string> action)
        {
            OnAvatarRemove += action;
        }

        public void RemoveAvatarRemoveListener(Action<string> action)
        {
            OnAvatarRemove -= action;
        }

        public void TurnToFaceCamera()
        {
            var motor = SelfController.Motor;
            if (motor != null && SelfController.CameraTarget)
            {
                SelfController.CameraTarget.transform.forward = -motor.transform.forward;
            }
        }

    }





}
