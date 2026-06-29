using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Basic.Utils;
using BUD.AnimPose;
using Es;
using Game.Audio;
using Game.AvatarTool;
using Game.Config;
using Game.KinematicCharacter;
using Game.Pet;
using GameData.Account;
using GameData.BaseInfo;
using GameData.MapData;
using GameData.PgcData;
using GameData.UGCData;
using Pb.Base;
using RootMotion.FinalIK;
using UnityEngine;
using xasset;
using Debug = UnityEngine.Debug;


namespace Game.Avatar
{
    public class AIBuddyAvatarController : MonoBehaviour
    {
        #region 自己的
        public KinematicCharacterController SelfController { get; private set; }
        public PlayerStateController SelfStateController { get; private set; }
        public CharacterWrap SelfWrap { get; private set; }
        public AIBuddyInfo SelfAIBuddyInfo{ get; private set; }
        #endregion

        public string CurrentInteractNpcID { get; set; }

        // 换肤时换衣动作的「亮相」延迟：到此刻才切换外观(与玩家退出换装间一致)；待机也应延后此值，避免覆盖换衣动作。
        public const float ChangeSkinRevealDelay = 1f;
        private BudTimer _changeSkinTimer;

        // 地图放置共享 buddy 的运行时 key 前缀（确定性 key = MapBuddyKeyPrefix + entityUid，跨端一致）。
        public const string MapBuddyKeyPrefix = "AINpcInMap_";
        // 判别一个 buddy key 是否为地图共享 buddy（玩家 uid / 空串都不匹配）。
        // 双人交互同步据此区分"本人 buddy"与"地图共享 buddy"，保证对现有流程零影响。
        public static bool IsMapBuddyKey(string key) => !string.IsNullOrEmpty(key) && key.StartsWith(MapBuddyKeyPrefix);

        private Dictionary<string, PlayerStateController> _buddyPlayerSateDict = new Dictionary<string, PlayerStateController>();
        private Dictionary<string, CharacterWrap> _buddyCharacterWrapDict = new Dictionary<string, CharacterWrap>();
        private static AIBuddyAvatarController _inst;
        private Transform _rootParent;
        
        Action<string, KinematicCharacterController> OnAvatarCreate;
        Action<string> OnAvatarRemove;

        private GameObject EffectNode;



        public static AIBuddyAvatarController Inst
        {
            get
            {
                if (_inst == null)
                {
                    LoggerUtils.Log("AIBuddyAvatarController Create");
                    _inst = new GameObject("AIBuddyCharacterController").AddComponent<AIBuddyAvatarController>();
                    DontDestroyOnLoad(_inst.gameObject);
                }
                return _inst;
            }
        }
        
        public Vector3 GetSelfAvatarPosition()
        {
            if (SelfController != null)
            {
                return SelfController.transform.position;
            }
            return Vector3.zero;
        }
        
        public Quaternion GetSelfAvatarRotation()
        {
            if (SelfController != null)
            {
                return SelfController.transform.rotation;
            }
            return Quaternion.identity;
        }
        
        #region 创建流程
        public KinematicCharacterController CreateSelfAIBuddy(string playerId, AIBuddyInfo buddyInfo)
        {
            if (buddyInfo == null)
            {
                LoggerUtils.LogError("AIBuddyAvatarController ---- npcInfo Is Null");
                return null;
            }
            
            DestroyAIBuddy(playerId);
            
            var creatorPlayerStateCtr = AvatarController.Inst.GetPlayerStateCtrl(playerId);
            if (creatorPlayerStateCtr == null)
            {
                LoggerUtils.LogError("AIBuddyAvatarController ---- Buddy 的主人 没有找到");
                return null;
            }

            #region 创建AIBuddy
            var creatorKccCtr = creatorPlayerStateCtr.PlayerKCCtrl;
            var createPos = creatorKccCtr.transform.position + new Vector3(0, 0, 2);
            var createRot = creatorKccCtr.transform.rotation;
            
            var createPlayerId = playerId;
            var buddyNpcInfo = buddyInfo.npc;
            var buddyAvatarJson = buddyNpcInfo.npcAvatarJson;
            var buddyAvatarData = CharacterData.DeserializeObject(buddyAvatarJson);
            
            var kinematicCharacterPrefab = Loader.Load<GameObject>("Assets/Loadable/Avatar/CharacterBody/KinematicCharacter.prefab").RetainAsset(gameObject);
            var kinematicCharacter = GameObject.Instantiate(kinematicCharacterPrefab, this.transform);
            kinematicCharacter.layer = LayerMask.NameToLayer("Model");
            kinematicCharacter.transform.localScale = Vector3.one;

            var avatarRoot = kinematicCharacter.transform.GetChild(0);
            var createBuddyWrap = CreateGameAIBuddyWithIKController(buddyAvatarData, avatarRoot);
            var createBuddyKcc = kinematicCharacter.GetComponent<KinematicCharacterController>();
            var createBuddyAnimController = createBuddyWrap.Avatar.GetComponent<PlayerAnimationCtrl>();
            
            PlayerStateController createBuddyStateController;
            createBuddyStateController = createBuddyWrap.Avatar.GetOrAddComponent<SelfBuddyStateController>();
            createBuddyStateController.IsAIBuddy = true;
            createBuddyStateController.InitAIBuddy(createBuddyAnimController, createBuddyWrap, createBuddyKcc, createPlayerId);
            SelfController = createBuddyKcc;
            SelfStateController = createBuddyStateController;
            SelfWrap = createBuddyWrap;
            SelfAIBuddyInfo = buddyInfo;
            AddGameAIBuddyState(createBuddyStateController);
            #endregion
            
            createBuddyKcc.Motor.SetPositionAndRotation(createPos, createRot);
            var callEffectRoot = new GameObject("CallEffectRoot");
            callEffectRoot.transform.SetParent(this.transform);
            callEffectRoot.transform.SetPositionAndRotation(createPos + new Vector3(0, 0.02f, 0), createRot);
            
            PlayerInfo info = new PlayerInfo();
            if (!string.IsNullOrEmpty(buddyInfo?.npc?.npcName))
            {
                info.Name = buddyInfo?.npc?.npcName;
            }
            UserInfoHeadView.Load(createBuddyKcc.gameObject, info);

            string effectId = "0";
            if (buddyInfo.summoningEffects != null)
            { 
                effectId = buddyInfo.summoningEffects[0];
            }
            ShowCallEffect(kinematicCharacter.transform, effectId, createPos, createRot);

            OnAvatarCreate?.Invoke(GameConsts.AIBuddyTag + AccountDataManager.Inst.Uid, SelfController);
            return createBuddyKcc;
        }
        
        /// <summary>
        /// 用 Cabin AI 伙伴数据创建自己的 Buddy Avatar（替换旧版 AIBuddyInfo 入口）。
        /// 调用方负责拆解 CabinCharacterUgcInfo，只传入基础类型，避免 Avatar 层依赖 UI 层。
        /// 位置/旋转由调用方通过 Motor.SetPositionAndRotation 设置。
        /// </summary>
        /// <param name="playerId">玩家 UID</param>
        /// <param name="avatarData">从 skinPack 默认皮肤反序列化得到的形象数据</param>
        /// <param name="buddyName">AI 伙伴名称，用于头顶显示</param>
        public KinematicCharacterController CreateSelfAIBuddyByCabin(string playerId, CharacterData avatarData, string buddyName)
        {
            if (avatarData == null)
            {
                LoggerUtils.LogError("AIBuddyAvatarController ---- avatarData is Null");
                return null;
            }

            DestroyAIBuddy(playerId);

            var creatorPlayerStateCtr = AvatarController.Inst.GetPlayerStateCtrl(playerId);
            if (creatorPlayerStateCtr == null)
            {
                LoggerUtils.LogError("AIBuddyAvatarController ---- Buddy 的主人 没有找到");
                return null;
            }

            var kinematicCharacterPrefab = Loader.Load<GameObject>("Assets/Loadable/Avatar/CharacterBody/KinematicCharacter.prefab").RetainAsset(gameObject);
            var kinematicCharacter = GameObject.Instantiate(kinematicCharacterPrefab, this.transform);
            kinematicCharacter.layer = LayerMask.NameToLayer("Model");
            kinematicCharacter.transform.localScale = Vector3.one;

            var avatarRoot = kinematicCharacter.transform.GetChild(0);
            var createBuddyWrap = CreateGameAIBuddyWithIKController(avatarData, avatarRoot);
            var createBuddyKcc = kinematicCharacter.GetComponent<KinematicCharacterController>();
            var createBuddyAnimController = createBuddyWrap.Avatar.GetComponent<PlayerAnimationCtrl>();

            var createBuddyStateController = createBuddyWrap.Avatar.GetOrAddComponent<SelfBuddyStateController>();
            createBuddyStateController.IsAIBuddy = true;
            createBuddyStateController.InitAIBuddy(createBuddyAnimController, createBuddyWrap, createBuddyKcc, playerId);
            SelfController = createBuddyKcc;
            SelfStateController = createBuddyStateController;
            SelfWrap = createBuddyWrap;
            AddGameAIBuddyState(createBuddyStateController);

            var info = new PlayerInfo { Name = buddyName ?? string.Empty };
            UserInfoHeadView.Load(createBuddyKcc.gameObject, info);

            // 立即设置位置，使外部 GetSelfAvatarPosition 能拿到正确坐标（否则同步给其他端的初始位置会是默认值）
            var createPos = creatorPlayerStateCtr.PlayerKCCtrl.transform.position + new Vector3(0, 0, 2);
            var createRot = creatorPlayerStateCtr.PlayerKCCtrl.transform.rotation;
            createBuddyKcc.Motor.SetPositionAndRotation(createPos, createRot);
            // 立即开启模拟使其重力接地：buddy 的 IsSelf=false，DefaultKCC.OnEnter 不会关它的模拟，
            // 但 Motor 初始 IsOnSimulate=false、DefaultKCC.SetInputs 仅在有移动输入时才开启，
            // 否则 buddy 会悬空直到玩家移动带动跟随才掉落。
            createBuddyKcc.Motor.SetIsOnSimulate(true);

            // Cabin 暂无召唤特效配置，使用默认特效 0
            ShowCallEffect(kinematicCharacter.transform, "0", createPos, createRot);

            OnAvatarCreate?.Invoke(GameConsts.AIBuddyTag + AccountDataManager.Inst.Uid, SelfController);
            return createBuddyKcc;
        }

        public KinematicCharacterController CreateAIGameAINpc(string npcName, string npcId, string npcAvatarJson, Action onComplete = null)
        {
            var kinematicCharacterPrefab = Loader.Load<GameObject>("Assets/Loadable/Avatar/CharacterBody/KinematicCharacter.prefab").RetainAsset(gameObject);
            var kinematicCharacter = GameObject.Instantiate(kinematicCharacterPrefab, this.transform);
            kinematicCharacter.layer = LayerMask.NameToLayer("Model");
            kinematicCharacter.transform.localScale = Vector3.one;
            
            var buddyAvatarData = CharacterData.DeserializeObject(npcAvatarJson);
            var createBuddyWrap = CreateGameAIBuddyWithIKController(buddyAvatarData, kinematicCharacter.transform.GetChild(0), onComplete);
            var createBuddyKcc = kinematicCharacter.GetComponent<KinematicCharacterController>();
            var createBuddyAnimController = createBuddyWrap.Avatar.GetComponent<PlayerAnimationCtrl>();

            //test
            kinematicCharacter.name = kinematicCharacter.name + npcId;
            //

            PlayerStateController createBuddyStateController;
            createBuddyStateController = createBuddyWrap.Avatar.GetOrAddComponent<OtherStateController>();
            createBuddyStateController.IsAIBuddy = true;
            createBuddyStateController.InitAIBuddy(createBuddyAnimController, createBuddyWrap, createBuddyKcc, npcId);
 
            AddGameAIBuddyState(createBuddyStateController);

            OnAvatarCreate?.Invoke(npcId, createBuddyKcc);
            
            PlayerInfo info = new PlayerInfo();
            info.Name = npcName;
            // UserInfoHeadView.Load(createBuddyKcc.gameObject, info);
            
            return createBuddyKcc;
        }
        
        
        public KinematicCharacterController CreateOtherGameAIBuddy(string buddyName, CharacterData buddyAvatarData, string createPlayerId, Action onComplete = null)
        {
            var kinematicCharacterPrefab = Loader.Load<GameObject>("Assets/Loadable/Avatar/CharacterBody/KinematicCharacter.prefab").RetainAsset(gameObject);
            var kinematicCharacter = GameObject.Instantiate(kinematicCharacterPrefab, this.transform);
            kinematicCharacter.layer = LayerMask.NameToLayer("Model");
            kinematicCharacter.transform.localScale = Vector3.one;
            
            var createBuddyWrap = CreateGameAIBuddyWithIKController(buddyAvatarData, kinematicCharacter.transform.GetChild(0), onComplete);
            var createBuddyKcc = kinematicCharacter.GetComponent<KinematicCharacterController>();
            var createBuddyAnimController = createBuddyWrap.Avatar.GetComponent<PlayerAnimationCtrl>();

            PlayerStateController createBuddyStateController;
            createBuddyStateController = createBuddyWrap.Avatar.GetOrAddComponent<OtherStateController>();
            createBuddyStateController.IsAIBuddy = true;
            createBuddyStateController.InitAIBuddy(createBuddyAnimController, createBuddyWrap, createBuddyKcc, createPlayerId);
 
            AddGameAIBuddyState(createBuddyStateController);

            OnAvatarCreate?.Invoke(GameConsts.AIBuddyTag + createPlayerId, createBuddyKcc);
            
            PlayerInfo info = new PlayerInfo();
            info.Name = buddyName;
            UserInfoHeadView.Load(createBuddyKcc.gameObject, info);
            
            return createBuddyKcc;
        }
        
        // deterministicEntityUid：地图放置伙伴传入稳定的 ECS 实体 uid，使运行时 key 跨客户端一致
        // （双人交互/载具的房间同步据此定位同一 buddy）；其余调用方（如剧场）传 0，沿用本地计数 key。
        public string CreateLocalGameAIBuddy(string buddyId, string buddyName, CharacterData buddyAvatarData, Transform root, Action onComplete = null, uint deterministicEntityUid = 0)
        {
            var kinematicCharacterPrefab = Loader.Load<GameObject>("Assets/Loadable/Avatar/CharacterBody/KinematicCharacter.prefab").RetainAsset(gameObject);
            var kinematicCharacter = GameObject.Instantiate(kinematicCharacterPrefab, this.transform);
            kinematicCharacter.layer = LayerMask.NameToLayer("Model");
            kinematicCharacter.transform.localScale = Vector3.one;
            
            // 实体机上头像资源已缓存→部件加载回调会“同步”触发（PC 上异步延后），届时下方状态机尚未注册。
            // 用标志位把 onComplete 推迟到注册完成之后，避免回调里 GetPlayerStateCtrl 取不到该 buddy。
            bool avatarReady = false;
            bool buddyRegistered = false;
            Action onAvatarReady = () =>
            {
                avatarReady = true;
                if (buddyRegistered) onComplete?.Invoke();
            };
            var createBuddyWrap = CreateGameAIBuddyWithIKController(buddyAvatarData, kinematicCharacter.transform.GetChild(0), onAvatarReady);
            createBuddyWrap.TakeOffSpecialSkin();
            var createBuddyKcc = kinematicCharacter.GetComponent<KinematicCharacterController>();
            var createBuddyAnimController = createBuddyWrap.Avatar.GetComponent<PlayerAnimationCtrl>();


            string localAIBuddyID = deterministicEntityUid != 0
                ? $"{MapBuddyKeyPrefix}{deterministicEntityUid}"
                : $"{MapBuddyKeyPrefix}{buddyId}_{_buddyPlayerSateDict.Count}";

            PlayerStateController createBuddyStateController;
            createBuddyStateController = createBuddyWrap.Avatar.GetOrAddComponent<OtherStateController>();
            createBuddyStateController.IsAIBuddy = true;
            createBuddyStateController.InitAIBuddy(createBuddyAnimController, createBuddyWrap, createBuddyKcc, localAIBuddyID);
 
            AddGameAIBuddyState(createBuddyStateController);
            createBuddyKcc.transform.SetParent(root);
            createBuddyKcc.Motor.SetIsOnSimulate(false);
            createBuddyKcc.Motor.SetPositionAndRotation(root.position, root.rotation);
            createBuddyKcc.Motor.enabled = false;

            OnAvatarCreate?.Invoke(localAIBuddyID, createBuddyKcc);
            
            PlayerInfo info = new PlayerInfo();
            info.Name = buddyName;
            UserInfoHeadView.Load(createBuddyKcc.gameObject, info);
            
            // 注册完成；同步情形（部件已就绪）在此补发 onComplete，异步情形由 onAvatarReady 触发
            buddyRegistered = true;
            if (avatarReady) onComplete?.Invoke();

            return localAIBuddyID;
        }

        /// <summary>
        /// 原地更换自己 buddy 的皮肤形象：只替换外观与碰撞胶囊高度，不重新召唤、保留当前位置。
        /// 用于「换装」场景（待机/口令由调用方另行刷新）。
        /// </summary>
        public void RefreshSelfAIBuddyAvatar(CharacterData avatarData)
        {
            if (avatarData == null || SelfWrap == null)
                return;

            SelfWrap.TakeOffSpecialSkin();
            SelfWrap.RefreshAvatar(avatarData);
            if (SelfController != null)
                SelfController.Motor.SetCapsuleHeightData((CustomBodyTypeController.BodyType)avatarData.bodyType);
        }

        /// <summary>换肤专用：先对伙伴播一次换衣动作(ChangeClothesAni)，到「亮相」时刻(ChangeSkinRevealDelay)再切换外观，
        /// 复刻玩家退出换装间的表现。无法进入该状态时退化为立即刷新，保证外观一定切换。</summary>
        public void RefreshSelfAIBuddyAvatarWithChangeClothes(CharacterData avatarData)
        {
            if (avatarData == null || SelfWrap == null)
                return;

            if (_changeSkinTimer != null)
                TimerManager.Inst.Stop(_changeSkinTimer);

            bool playAni = SelfStateController != null
                && SelfStateController.CanEnterState(PlayerState.ChangeClothesAni);
            if (!playAni)
            {
                // 无法播放换衣动作时直接刷新，保证皮肤一定切换
                RefreshSelfAIBuddyAvatar(avatarData);
                return;
            }

            SelfStateController.EnterState(PlayerState.ChangeClothesAni);
            // 延迟到换衣动作「亮相」点再切外观（仿 RoomEditAvatarManager 退出换装的延迟刷新）
            _changeSkinTimer = TimerManager.Inst.RunOnce("BuddyChangeSkinReveal", ChangeSkinRevealDelay,
                () => RefreshSelfAIBuddyAvatar(avatarData));
        }

        public void ChangeLocalGameAIBuddyAvatar(string buddyId, CharacterData buddyAvatarData)
        {
            // 防御：buddyId 为空直接返回（实体机同步回调可能在 id 就绪前调入，避免 Dictionary 抛 key null）
            if (string.IsNullOrEmpty(buddyId))
                return;
            if (_buddyPlayerSateDict.ContainsKey(buddyId))
            {
                _buddyPlayerSateDict[buddyId].Wrap.TakeOffSpecialSkin();
                _buddyPlayerSateDict[buddyId].Wrap.SetCharacterData(buddyAvatarData);
            }
            else
            {
                LoggerUtils.LogError($"AIBuddyAvatarController :: 没有ID为{buddyId}的本地AINPC!!");
            }
        }
        
        private void ShowCallEffect(Transform parent, string effectId, Vector3 createPos, Quaternion createRot)
        {
            SelfController.gameObject.SetActive(false);

            if (string.IsNullOrEmpty(effectId))
                return;

            // 动态生成特效路径
            string effectPath = $"Assets/Loadable/Avatar/CharacterBody/CallNpcEffect/Effect_{effectId}/Effect_{effectId}.prefab";

            // 加载并实例化特效
            var effect = Loader.Load<GameObject>(effectPath);
            if (effect != null)
            {
                effect.Instantiate(parent);
            }

            float delayTime = 0;
            switch (effectId)
            {
                default:
                case "0":
                    delayTime = 0;
                    break;

                case "1":
                    delayTime = 3;
                    break;
            }
            AkSoundManager.Inst.PlaySound("Emote_Group_S7", "npc_summon", "Play_Emote_S7_1P", gameObject);
            TimerManager.Inst.RunOnce("ShowCallEffect", delayTime, () =>
            {
                SelfController.Motor.SetPositionAndRotation(createPos + new Vector3(0, 0.5f, 0), createRot);
                SelfController.gameObject.SetActive(true);
            });
        }

        private CharacterWrap CreateGameAIBuddyWithIKController(CharacterData buddyAvatarData, Transform parent, Action onComplete = null)
        {
            var avatarWrap = CreateGameBuddyAvatarWithIK(buddyAvatarData, parent, onComplete);
            var animIK = avatarWrap.Avatar.GetComponent<BaseAnimIK>();
            var ikController = animIK.gameObject.AddComponent<AnimIKController>();
            ikController.AddAnimIK(animIK);
            return avatarWrap;
        }
        
        private CharacterWrap CreateGameBuddyAvatarWithIK(CharacterData data, Transform parent, Action callback = null)
        {
            var buddyWrap = CreateBuddyAvatar(data, callback);
            ChangePgcEye(buddyWrap);
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
            buddyWrap.SetParent(node, true);

            var animIK = buddyWrap.Avatar.GetComponent<BaseAnimIK>();
            animIK.transform.localPosition = poseModeData.RoleDefPos[0];
            animIK.SetOptEntity(buddyWrap.Avatar);
            animIK.InitAnimIK(false);
            buddyWrap.CustomAvatar = nodePar.gameObject;

            if(buddyWrap.Avatar.TryGetComponent<CustomBodyTypeController>(out var shapCtrl))
            {
                CustomBodyTypeController.BodyType type = data.bodyType == 0 ? CustomBodyTypeController.BodyType.None : (CustomBodyTypeController.BodyType)data.bodyType;
                shapCtrl.ApplyBodyType(type);
            }
            
            return buddyWrap;
        }
        
        private CharacterWrap CreateBuddyAvatar(CharacterData data, Action callback = null)
        {
            var wrap = CreateBuddyAvatar(data);
            if (data != null)
            {
                wrap.SetCharacterData(data.Clone(), callback);
            }
            return wrap;
        }
        
        private CharacterWrap CreateBuddyAvatar(CharacterData data)
        {
            var avatar = Loader.Load<GameObject>("Assets/Loadable/Avatar/CharacterBody/Character.prefab", this.gameObject);
            var avatarInstance = GameObject.Instantiate(avatar);
            var bodyTypeCtrl = avatarInstance.AddComponent<CustomBodyTypeController>();
            CustomBodyTypeController.BodyType type = data.bodyType == 0 ? CustomBodyTypeController.BodyType.None : (CustomBodyTypeController.BodyType)data.bodyType;
            bodyTypeCtrl.ApplyBodyType(type);
            var playerAnimationCtrl = avatarInstance.AddComponent<PlayerAnimationCtrl>();
            CharacterWrap wrap = new CharacterWrap(avatarInstance);
            playerAnimationCtrl.Init(wrap);
            var playerHoldBehaviour = avatarInstance.AddComponent<PlayerHoldBehaviour>();
            playerHoldBehaviour.Init(wrap);
            return wrap;
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

        public void AddGameAIBuddyForHospital(PlayerStateController playerStateCtrl)
        {
            var playerID = AccountDataManager.Inst.Uid;
            if (_buddyPlayerSateDict.ContainsKey(playerID))
            {
                _buddyPlayerSateDict[playerID] = playerStateCtrl;
            }
            else
                _buddyPlayerSateDict.Add(playerID, playerStateCtrl);
            playerStateCtrl.IsSelfAIBuddy = true;
            playerStateCtrl.PlayerID = playerID;
        }

        public void AddGameAIBuddyForPark(PlayerStateController playerStateCtrl)
        {
            var playerID = AccountDataManager.Inst.Uid;
            if (_buddyPlayerSateDict.ContainsKey(playerID))
            {
                _buddyPlayerSateDict[playerID] = playerStateCtrl;
            }
            else
                _buddyPlayerSateDict.Add(playerID, playerStateCtrl);
            playerStateCtrl.IsSelfAIBuddy = true;
            playerStateCtrl.PlayerID = playerID;
        }

        public void RemoveGameAIBuddyForHospital(PlayerStateController playerStateCtrl)
        {
            var playerID = AccountDataManager.Inst.Uid;
            _buddyPlayerSateDict.Remove(playerID);
            playerStateCtrl.IsSelfAIBuddy = false;
        }

        public void RemoveGameAIBuddyForPark(PlayerStateController playerStateCtrl)
        {
            var playerID = AccountDataManager.Inst.Uid;
            _buddyPlayerSateDict.Remove(playerID);
            playerStateCtrl.IsSelfAIBuddy = false;
        }

        public void AddGameAIBuddyState(PlayerStateController playerStateCtrl)
        {
            if (_buddyPlayerSateDict.ContainsKey(playerStateCtrl.PlayerID))
            {
                _buddyPlayerSateDict[playerStateCtrl.PlayerID] = playerStateCtrl;
            }
            else
            {
                _buddyPlayerSateDict.Add(playerStateCtrl.PlayerID, playerStateCtrl);
            }
        }
        #endregion

        #region 外部获取接口
        public Dictionary<string, PlayerStateController> GetDic()
        {
            return _buddyPlayerSateDict;
        }

        public PlayerStateController GetPlayerStateCtrl(string playerID)
        {
            // 防御：playerID 为空时直接返回 null（实体机头像同步回调可能在 id 就绪前调入，避免 Dictionary 抛 key null）
            if (string.IsNullOrEmpty(playerID))
                return null;

            PlayerStateController playerStateController;

            if (_buddyPlayerSateDict.TryGetValue(playerID, out playerStateController))
            {
                return playerStateController;
            }

            LoggerUtils.Log("GetPlayerStateCtrl - The playerID could not be found in the playerStateDic--", playerID);
            return null;
        }
        #endregion

        public void DestroyAIBuddy(string playerId)
        {
            // 防御：playerId 为空直接返回（避免 ContainsKey / StartsWith 对 null 抛异常）
            if (string.IsNullOrEmpty(playerId))
                return;
            if (_buddyPlayerSateDict.ContainsKey(playerId))
            {
                if (playerId.StartsWith(MapBuddyKeyPrefix)) { _buddyPlayerSateDict.Remove(playerId); return; }

                OnAvatarRemove?.Invoke(GameConsts.AIBuddyTag + playerId);
                _buddyPlayerSateDict[playerId].Wrap?.DestorySelf();
                if (_buddyPlayerSateDict[playerId].PlayerKCCtrl != null)
                {
                    Destroy(_buddyPlayerSateDict[playerId].PlayerKCCtrl.gameObject);
                }
                _buddyPlayerSateDict.Remove(playerId);

                if (AccountDataManager.Inst.IsSelf(playerId))
                {
                    SelfController = null;
                    SelfStateController = null;
                    SelfWrap = null;
                    SelfAIBuddyInfo = null;
                    
                    if (EffectNode != null)
                        GameObject.Destroy(EffectNode);
                }
            }
        }

        public void DestoryAllLocalAIBuddy()
        {
            List<string> localAIBuddyKeys = _buddyPlayerSateDict.Keys
                .Where(k => k.StartsWith(GameConsts.AIBuddyTag + "Local"))
                .Select(k => k.Clone().ToString())
                .ToList();

            foreach (var key in localAIBuddyKeys)
            {
                if (_buddyPlayerSateDict.ContainsKey(key))
                {
                    OnAvatarRemove?.Invoke(key);
                    _buddyPlayerSateDict[key].Wrap?.DestorySelf();
                    Destroy(_buddyPlayerSateDict[key].PlayerKCCtrl.gameObject);
                    _buddyPlayerSateDict.Remove(key);
                }
            }
        }

        public void DestoryAllPlayer()
        {
            var playerIds = _buddyPlayerSateDict.Keys.ToList();
            for (int i = 0; i < playerIds.Count; i++)
            {
                DestroyAIBuddy(playerIds[i]);
            }

            if (EffectNode != null)
                GameObject.Destroy(EffectNode);
        }

        public ConversationListItem GetConversationListItem()
        {
            ConversationListItem buddyItem = new ConversationListItem();
            buddyItem.uid = GameConsts.AIBuddyTag;
            buddyItem.nickname = SelfAIBuddyInfo.npc.npcName;
            buddyItem.isOnline = 1;
            buddyItem.portraitUrl = SelfAIBuddyInfo.npc.npcPortraitUrl;

            return buddyItem; 
        }

        #region Listener
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
        
        public void OnDestroy()
        {
            OnAvatarCreate = null;
            OnAvatarRemove = null;
        }
        #endregion
    }
}
