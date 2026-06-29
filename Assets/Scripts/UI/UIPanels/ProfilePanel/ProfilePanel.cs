using Basic.Utils;
using BUD.AnimPose;
using Es;
using Game.Avatar;
using Game.Pet;
using GameData;
using GameData.Account;
using GameData.BaseInfo;
using GameData.PgcData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RTG;
using System;
using System.Collections.Generic;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace UI.UIPanels.ProfilePanel
{
    /// <summary>
    ///  个人主页
    /// </summary>
    public class ProfilePanel : BasePanel<ProfilePanel>
    {
        //UI
        [SerializeField] private SpriteAtlas bgAtlas;
        private Button _backBtn;
        private AvatarCard _avatarCard;
        private ProfileCard _profileCard;
        private CreatorSeasonCard _creatorSeasonCard;
        private AccountCard _accountCard;
        private WearingCard _wearingCard;
        private PetWearingCard _petWearingCard;
        private GamesCard _gamesCard;
        private OutfitsCard _outfitsCard;
        private AnimCard _animCard;
        private NpcCard _npcCard;
        private AICharacterCard _aiCharacterCard;
        private PhotoCard _photoCard;
        private PoseCard _poseCard;
        private VehicleCard _vehicleCard;
        private PetOutfitsCard _petOutfitsCard;
        private AnimCard _petAnimCard;
        private PoseCard _petPoseCard;
        private PropsCard _propsCard;
        private MaterialsCard _materialsCard;
        private MusicScoreCard _musicScoreCard;
        private ToneCard _toneCard;
        private StatisticsCard _statisticsCard;
        private TheatreCard _theatreCard;
        private TheatreActorCard _theatreActorCard;
        private CharacterBoxCard _characterBoxCard;
        private ProfileEditBoardView _profileEditBoardView;
        private ProfileBottomView _profileBottomView;
        private TabScrollGroup tabScrollGroup;
        private GameObject createTab;
        private GameObject gameTab;
        private GameObject petTab;
        private GameObject photoTab;
        private GameObject theaterTab;
        private GameObject actorTab;
        private GameObject otherTab;
        private GameObject aiCharacterTab;
        private GameObject characterBoxTab;
        //主题化
        private Image tabBg;
        private Transform themeBgContent;

        //3D形象
        private Transform characterRoot;
        private AvatarCameraController cameraContrller;
        private CharacterWrap characterWrap;
        private PlayerAnimationCtrl avatarAnimCtrl;
        private PlayerIdleBehaviour idleBehaviour;
        private UgcIdleBehaviour characterUgcIdleBehaviour;

        private PetWrap petWrap;
        private PetPlayerIdleBehaviour petIdleBehaviour;
        private UgcIdleBehaviour petUgcIdleBehaviour;

        private CharacterWrap npcWrap;
        private NpcPlayerIdleBehaviour npcIdleBehaviour;
        private UgcIdleBehaviour npcUGCIdleBehaviour;

        private List<BaseCard> _cardList;
        private string _userId;
        private AccountUserInfo _accountUserInfo;
        private AccountAmount _accountAmount;
        private RelationShipData _relationShipData;
        private AccountPetInfo _targetPetInfo;
        private AIBuddyInfo _targetAIBuddyInfo;
        private HallCharacterInfo _targetCharacterInfo; // 他人主页大厅伙伴角色（来自 publicProfile.characterInfo）
        private VehicleInfo _targetVehicleInfo;

        private AmbientLightSetting _srcLightSetting;
        private bool _srcHallLightVisible;
        private bool _srcGameSceneLightVisible;
        private bool _srcPreviewSceneLightVisible;

        private int curThemeId = -1;

        public static AccountUserInfo accountUserInfo;

        //动作在主页皮肤显示太大，在这里设置
        public static readonly Dictionary<string,float> camera_scale = new Dictionary<string, float>
        {
            {"40200568",2.5f}
        };

        //List<GameObject> bgObjs = new List<GameObject>();
        public override void OnCreate()
        {
            base.OnCreate();
            InitUI();
        }

        public override void OnShow(params object[] args)
        {
            _userId = args[0] as string;

            foreach (var card in _cardList)
            {
                card.OnShow(_userId);
            }

            if (AccountDataManager.Inst.IsMySelf(_userId))
            {
                AccountDataManager.Inst.AddUserInfoChangeListener(OnUserInfoChange);
                ShowCharacter(AccountDataManager.Inst.UserInfo);
                OnUpdateTheme(AccountDataManager.Inst.UserInfo.homepageSkin);
#if PACKAGE_TYPE_US
                _accountCard.Show(AccountDataManager.Inst.accountPlatform == AccountPlatform.Tourists);
#endif
            }

            RequestUserInfo(_userId);
            RequestPublishList(_userId);
            RequestPhotoList(_userId);

            _srcPreviewSceneLightVisible = AmbientLightManager.Inst.ShowPreviewDirLight();
            _srcLightSetting = AmbientLightManager.Inst.OpenUILight();
            _srcHallLightVisible = AmbientLightManager.Inst.HideHallLight();
            _srcGameSceneLightVisible = AmbientLightManager.Inst.HideGameSceneLight();

        }

        public void SetCameraColor(string colorStr = "")
        {
            if (cameraContrller != null && cameraContrller.roleCamera != null)
            {
                if (string.IsNullOrEmpty(colorStr))
                {
                    colorStr = "#FFFFFF";
                }
                var cameraColor = DataUtil.DeSerializeColorByHex(colorStr+"00");
                cameraContrller.roleCamera.backgroundColor = cameraColor;
            }
        }

        public void OnUpdateTheme(int themeId)
        {
            if (curThemeId == themeId)return;
            curThemeId = themeId;
            ProfileThemeInfo themeInfo = ProfileThemeManager.Inst.GetThemeInfo(themeId);

            foreach (var card in _cardList)
            {
                card.OnUpdateTheme(themeInfo);
            }

            tabBg.color = DataUtil.DeSerializeColorCheckHash(themeInfo.colorInfo.titleBgColor);
            if (!string.IsNullOrEmpty(themeInfo.colorInfo.mainBgColor))
            {
                SetCameraColor(themeInfo.colorInfo.mainBgColor);
            }
            
            if (themeId == 0)
            {
                themeBgContent.gameObject.SetActive(false);
            }
            else
            {
                themeBgContent.gameObject.SetActive(true);
                for (int i = 0; i < themeBgContent.childCount; i++) {
                    Destroy(themeBgContent.GetChild(i).gameObject);
                }
                string bgPath =  ProfileThemeManager.Inst.GetThemeBgPath(themeId);
                Loader.Load<GameObject>(bgPath).Instantiate(themeBgContent);

                //是否需要卡片特效
                if (ProfileThemeManager.Inst.IsNeedCardEffect(themeId))
                {
                    string effPath = ProfileThemeManager.Inst.GetThemeEffPath(themeId);
                    Loader.Load<GameObject>(effPath).Instantiate(_profileCard.transform);
                }

            }

            //foreach (var o in bgObjs) {
            //    Destroy(o.gameObject);
            //}
            //bgObjs.Clear();
            // 替换资源类
            if (!string.IsNullOrEmpty(themeInfo.colorInfo.bg_bottom))
            {
                tabBg.color = DataUtil.DeSerializeColorCheckHash("FFFFFF");
                tabBg.sprite = Loader.Load<Sprite>(themeInfo.colorInfo.bg_bottom,gameObject);
            }else
            {
                tabBg.sprite = null;
            }
            //if (!string.IsNullOrEmpty(themeInfo.colorInfo.info_bg1))
            //{
            //    var obj = Loader.Load<GameObject>(themeInfo.colorInfo.info_bg1).Instantiate(_profileCard.transform);
            //    obj.transform.SetSiblingIndex(1);
            //    bgObjs.Add(obj);
            //}

            //foreach (var card in _cardList)
            //{
            //    if(card != _statisticsCard && card != _profileCard)
            //    {
            //        if (!string.IsNullOrEmpty(themeInfo.colorInfo.info_bg2))
            //        {
            //            var obj = Loader.Load<GameObject>(themeInfo.colorInfo.info_bg2).Instantiate(card.transform);
            //            obj.transform.SetSiblingIndex(1);
            //            bgObjs.Add(obj);
            //        }
            //    }  
            //}

            //if (!string.IsNullOrEmpty(themeInfo.colorInfo.info_bg2))
            //{
            //    var obj = Loader.Load<GameObject>(themeInfo.colorInfo.info_bg2).Instantiate(_wearingCard.transform);
            //    obj.transform.SetSiblingIndex(1);
            //    bgObjs.Add(obj);
            //}
        }

        public override void OnHidden()
        {
            base.OnHidden();

            AmbientLightManager.Inst.CloseUILight(_srcLightSetting);
            AmbientLightManager.Inst.RevertHallLight(_srcHallLightVisible);
            AmbientLightManager.Inst.RevertGameSceneLight(_srcGameSceneLightVisible);
            AmbientLightManager.Inst.RevertPreviewLight(_srcPreviewSceneLightVisible);
        }

        private void InitUI()
        {
            _backBtn = GameObjectEx.FindChildByName(transform, "BackBtn").GetComponent<CButton>();
            _avatarCard = GameObjectEx.FindChildByName(transform, "AvatarCard").GetComponent<AvatarCard>();
            _profileCard = GameObjectEx.FindChildByName(transform, "ProfileCard").GetComponent<ProfileCard>();
            _accountCard = GameObjectEx.FindChildByName(transform, "AccountCard").GetComponent<AccountCard>();
            _wearingCard = GameObjectEx.FindChildByName(transform, "WearingCard").GetComponent<WearingCard>();
            _petWearingCard = GameObjectEx.FindChildByName(transform, "PetWearingCard").GetComponent<PetWearingCard>();
            _gamesCard = GameObjectEx.FindChildByName(transform,"GamesCard").GetComponent<GamesCard>();
            _outfitsCard = GameObjectEx.FindChildByName(transform, "OutfitsCard").GetComponent<OutfitsCard>();
            _animCard = GameObjectEx.FindChildByName(transform, "AnimCard").GetComponent<AnimCard>();
            _npcCard = GameObjectEx.FindChildByName(transform, "NpcCard").GetComponent<NpcCard>();
            _aiCharacterCard = GameObjectEx.FindChildByName(transform, "AICharacterCard")?.GetComponent<AICharacterCard>();
            _photoCard = GameObjectEx.FindChildByName(transform, "PhotoCard").GetComponent<PhotoCard>();
            _poseCard = GameObjectEx.FindChildByName(transform, "PoseCard").GetComponent<PoseCard>();
            _vehicleCard = GameObjectEx.FindChildByName(transform, "VehicleCard").GetComponent<VehicleCard>();
            _petOutfitsCard = GameObjectEx.FindChildByName(transform, "PetOutfitsCard").GetComponent<PetOutfitsCard>();
            _petAnimCard = GameObjectEx.FindChildByName(transform, "PetAnimCard").GetComponent<AnimCard>();
            _petPoseCard = GameObjectEx.FindChildByName(transform, "PetPoseCard").GetComponent<PoseCard>();
            _propsCard = GameObjectEx.FindChildByName(transform,"PropsCard").GetComponent<PropsCard>();
            _materialsCard = GameObjectEx.FindChildByName(transform,"MaterialsCard").GetComponent<MaterialsCard>();
            _musicScoreCard = GameObjectEx.FindChildByName(transform, "MusicScoreCard").GetComponent<MusicScoreCard>();
            _toneCard = GameObjectEx.FindChildByName(transform, "ToneCard").GetComponent<ToneCard>();
            _statisticsCard = GameObjectEx.FindChildByName(transform,"StatisticsCard").GetComponent<StatisticsCard>();
            _theatreCard = GameObjectEx.FindChildByName(transform, "TheatreCard").GetComponent<TheatreCard>();
            _theatreActorCard = GameObjectEx.FindChildByName(transform, "TheatreActorCard").GetComponent<TheatreActorCard>();
            var characterBoxCardTf = GameObjectEx.FindChildByName(transform, "CharacterBoxCard");
            if (characterBoxCardTf != null) _characterBoxCard = characterBoxCardTf.GetComponent<CharacterBoxCard>();
            _creatorSeasonCard = GameObjectEx.FindChildByName(transform, "CreatorSeasonCard").GetComponent<CreatorSeasonCard>();
            characterRoot = GameObjectEx.FindChildByName(transform, "AvatarRoot");
            cameraContrller = GameObjectEx.FindChildByName(transform, "ClickArea").GetComponent<AvatarCameraController>();
            _profileEditBoardView = GameObjectEx.FindChildByName(transform, "EditProfileView").GetComponent<ProfileEditBoardView>();
            _profileBottomView = GameObjectEx.FindChildByName(transform, "ProfileBottomView").GetComponent<ProfileBottomView>();
            tabScrollGroup = GameObjectEx.FindChildByName(transform, "MainSection").GetComponent<TabScrollGroup>();
            tabBg = GameObjectEx.FindChildByName(tabScrollGroup.transform, "Tabs").GetComponent<Image>();
            themeBgContent = GameObjectEx.FindChildByName(transform, "ThemeBgContent");
            createTab = GameObjectEx.FindChildByName(transform, "Creation").gameObject;
            gameTab = GameObjectEx.FindChildByName(tabScrollGroup.transform, "GameTab").gameObject;
            petTab = GameObjectEx.FindChildByName(tabScrollGroup.transform, "PetTab").gameObject;
            otherTab = GameObjectEx.FindChildByName(tabScrollGroup.transform, "OtherTab").gameObject;
            photoTab = GameObjectEx.FindChildByName(tabScrollGroup.transform, "PhotoTab").gameObject;
            theaterTab = GameObjectEx.FindChildByName(tabScrollGroup.transform, "TheaterTab").gameObject;
            actorTab = GameObjectEx.FindChildByName(tabScrollGroup.transform, "ActorTab").gameObject;
            var aiCharacterTabTf = GameObjectEx.FindChildByName(tabScrollGroup.transform, "AICharacterTab");
            if (aiCharacterTabTf != null) aiCharacterTab = aiCharacterTabTf.gameObject;
            var characterBoxTabTf = GameObjectEx.FindChildByName(tabScrollGroup.transform, "CharacterBoxTab");
            if (characterBoxTabTf != null) characterBoxTab = characterBoxTabTf.gameObject;

            SetCreateTabVisible(false);
            gameTab.SetActive(false);
            petTab.SetActive(false);
            otherTab.SetActive(false);
            photoTab.SetActive(false);
            theaterTab.SetActive(false);
            actorTab.SetActive(false);
            if (aiCharacterTab != null) aiCharacterTab.SetActive(false);
            if (characterBoxTab != null) characterBoxTab.SetActive(false);

            _wearingCard.Show(false);
            _petWearingCard.Show(false);
            _accountCard.Show(false);
            _outfitsCard.Show(false);
            _petOutfitsCard.Show(false);
            _propsCard.Show(false);
            _gamesCard.Show(false);
            _materialsCard.Show(false);
            _musicScoreCard.Show(false);
            _animCard.Show(false);
            _npcCard.Show(false);
            _aiCharacterCard?.Show(false);
            _photoCard.Show(false);
            _poseCard.Show(false);
            _vehicleCard.Show(false);
            _petAnimCard.Show(false);
            _petPoseCard.Show(false);
            _toneCard.Show(false);
            _theatreCard.Show(false);
            _theatreActorCard.Show(false);
            if (_characterBoxCard != null) _characterBoxCard.Show(false);
            _creatorSeasonCard.Show(false);

            _backBtn.onClick.AddListener(OnBackBtnClick);

            _cardList = new List<BaseCard>()
            {
                _avatarCard,
                _profileCard,
                _accountCard,
                _wearingCard,
                _petWearingCard,
                _gamesCard,
                _outfitsCard,
                _animCard,
                _npcCard,
                _aiCharacterCard,
                _photoCard,
                _poseCard,
                _vehicleCard,
                _petOutfitsCard,
                _petAnimCard,
                _petPoseCard,
                _propsCard,
                _materialsCard,
                _musicScoreCard,
                _toneCard,
                _theatreCard,
                _statisticsCard,
                _creatorSeasonCard,
                _theatreActorCard,
                _characterBoxCard,
            };

            foreach (var card in _cardList)
            {
                card.OnCreate(this);
            }
        }

        public void SetCreateTabVisible(bool isVisible)
        {
            createTab?.SetActive(isVisible);
        }


        public void SetEditViewVisible(bool isVisible)
        {
            _profileEditBoardView.SetVisible(isVisible);
        }


        public void SetBottomViewVisible(bool isVisible)
        {
            _profileBottomView.gameObject.SetActive(isVisible);
            if (isVisible)
            {
                _profileBottomView.OnInitCreated(_userId,_relationShipData);
            }
        }
        private void ShowCharacter(AccountUserInfo userInfo)
        {
            if (userInfo == null)
            {
                return;
            }

            string avatarJson = userInfo.avatarJson;
            if (string.IsNullOrEmpty(avatarJson))
            {
                return;
            }

            CharacterData avatarInfo = CharacterData.DeserializeObject(avatarJson);

            if (characterWrap == null)
            {
                characterWrap = AvatarController.Inst.CreateUIAvatarWithIKController(avatarInfo,characterRoot);
                avatarAnimCtrl = characterWrap.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
                avatarAnimCtrl.CheckAndOverrideSpecialAnim();
                avatarAnimCtrl.ApplyUIPreviewIdleOverride(); // 特殊皮肤 idle 用 preview（仅 specialAnimPgcId 非空时生效）
                idleBehaviour = characterWrap.Avatar.AddComponent<PlayerIdleBehaviour>();
                idleBehaviour.Init(avatarAnimCtrl);
                characterUgcIdleBehaviour = characterWrap.Avatar.AddComponent<UgcIdleBehaviour>();
                var characterIkController = characterWrap.Avatar.GetComponent<AnimIKController>();
                characterUgcIdleBehaviour.Init(characterIkController);
            }
            else
            {
                characterWrap.RefreshAvatar(avatarInfo);
                avatarAnimCtrl.CheckAndOverrideSpecialAnim();
                avatarAnimCtrl.ApplyUIPreviewIdleOverride(); // 特殊皮肤 idle 用 preview
            }

            characterWrap.PutOnDefaultClothes(avatarInfo);
            // 在全部加载（含 PutOnDefaultClothes 可能重建的特效）之后再驱动一次，确保最终的特效也切到 preview
            avatarAnimCtrl?.ApplyUIPreviewIdleOverride();
            _avatarCard.SetRoleTarget(characterRoot);
        }
        private void ShowPetAndNpc(AccountUserInfo avatarInfo,AccountPetInfo petInfo, AIBuddyInfo aiBuddyInfo)
        {
            if (petInfo == null)
            {
                return;
            }

            string petJson = petInfo.avatarJson;
            if (string.IsNullOrEmpty(petJson))
            {
                return;
            }

            PetData petData = PetData.DeserializeObject(petJson);
            if (petWrap == null)
            {
                petWrap = PetAvatarController.Inst.CreateUIAvatarWithIKController(petData,characterRoot);
                var animationCtrl = petWrap.Avatar.GetComponentInChildren<PetAnimationCtrl>();
                petIdleBehaviour = petWrap.Avatar.AddComponent<PetPlayerIdleBehaviour>();
                petIdleBehaviour.Init(animationCtrl);

                petUgcIdleBehaviour = petWrap.Avatar.AddComponent<UgcIdleBehaviour>();
                var petIkController = petWrap.Avatar.GetComponent<AnimIKController>();
                petUgcIdleBehaviour.Init(petIkController);
            }
            else
            {
                petWrap.RefreshAvatar(petData);
            }
            petIdleBehaviour.avatarAnimCtr = avatarAnimCtrl;



            CharacterData aiBuddyData;
            var npcAvatarJson = GetHallNpcAvatarJson();
            if (!string.IsNullOrEmpty(npcAvatarJson)) {
                aiBuddyData = CharacterData.DeserializeObject(npcAvatarJson);
            } else {
                aiBuddyData = CharacterData.DeserializeObject(avatarInfo.avatarJson);
            }

            if (npcWrap == null) {
                npcWrap = AvatarController.Inst.CreateUIAvatarWithIKController(aiBuddyData, characterRoot);
                var animationCtrl = npcWrap.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
                npcIdleBehaviour = npcWrap.Avatar.AddComponent<NpcPlayerIdleBehaviour>();
                npcIdleBehaviour.Init(animationCtrl);
                npcIdleBehaviour.avatarAnimCtr = avatarAnimCtrl;
                npcUGCIdleBehaviour = npcWrap.Avatar.AddComponent<UgcIdleBehaviour>();
                var npcIkController = npcWrap.Avatar.GetComponent<AnimIKController>();
                npcUGCIdleBehaviour.Init(npcIkController);
            }
            AvatarAndPetAnimView animView = new AvatarAndPetAnimView(characterWrap,petWrap, npcWrap, idleBehaviour,petIdleBehaviour, npcIdleBehaviour);
            var animType = animView.GetHallAnimatoionType(petInfo, aiBuddyInfo);
            if (animType == AvatarAndPetAnimView.HallRoleAnim.PetInteractive && petInfo.idleData.animResType == (int)AnimResType.PGC)
            {
                cameraContrller.SetEmoteView(petInfo.idleData.mainIdle);
            }
            animView.OnAnimChange(false,animType,avatarInfo,petInfo, aiBuddyInfo);
        }

        private void OnBackBtnClick()
        {
            UIManager.Inst.ClosePanel(this);
        }



        private void SetUserInfo(AccountUserInfo userInfo,AccountAmount amount, RelationShipData relationShipData)
        {
            _profileCard.SetUserInfo(userInfo,amount, relationShipData);
            _statisticsCard.SetTipsText(userInfo.registerTime);
            _gamesCard.SetUserInfo(userInfo);
            _creatorSeasonCard.SetUserInfo(userInfo);
        }

        private void OnUserInfoChange(AccountUserInfo accountUserInfo)
        {
            if (AccountDataManager.Inst.IsMySelf(_userId) && accountUserInfo.uid == _userId)
            {
                //更换主题
                if (accountUserInfo.homepageSkin != curThemeId)
                {
                    OnUpdateTheme(accountUserInfo.homepageSkin);
                }

                _creatorSeasonCard.SetUserInfo(accountUserInfo);
            }
        }

        public void RequestUserInfo(string uid)
        {
            var jb = new JObject
            {
                ["targetUid"] = uid
            };
            var requestId = Guid.NewGuid().ToString("N");
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.publicProfile,
                HttpMethod.GET,
                JsonConvert.SerializeObject(jb),
                OnUserInfoSuccess,
                OnUserInfoFail,
                retryCount:3);
        }


        public void RequestPublishList(string uid)
        {
            var jb = new JObject
            {
                ["targetUid"] = uid
            };
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.publicList,
                HttpMethod.GET,
                JsonConvert.SerializeObject(jb),
                OnGetPublishListSuccess,
                OnGetPublishListFail,
                retryCount:3);
        }

        private void OnUserInfoSuccess(string content)
        {
            if (!this) return;
            GetUserInfoRsp resData = JsonConvert.DeserializeObject<GetUserInfoRsp>(content);
            AccountUserInfo userInfo = resData.userInfo;
            AccountAmount accountAmount = resData.amount;
            accountUserInfo = userInfo;
            _accountUserInfo = userInfo;
            _accountAmount = accountAmount;
            _targetPetInfo = resData.petInfo;
            _targetAIBuddyInfo = resData.aibuddyInfo;
            _targetCharacterInfo = resData.characterInfo;
            _targetVehicleInfo = resData.vehicleInfo;
            ShowCharacter(userInfo);
            ShowPetAndNpc(userInfo, resData.petInfo, BuildLayoutBuddyInfo());
            // 单人展示时：camera_scale 白名单里的动作(目前为 40200568)通过缩放相机(orthographicSize)特殊处理大小，
            // 避免其在主页显示过大；不在白名单里的动作还原成默认相机大小（避免界面复用时残留上一次的特殊尺寸）。
            // 注：宠物/AIBuddy 展示时不在此处理，相机由 ShowPetAndNpc 负责。
            bool petShown = resData.petInfo != null && resData.petInfo.isHidden == 0;
            bool buddyShown = resData.aibuddyInfo != null && resData.aibuddyInfo.isHidden == 0;
            if (!petShown && !buddyShown && userInfo.idleData != null
                && !string.IsNullOrEmpty(userInfo.idleData.mainIdle))
            {
                if (camera_scale.TryGetValue(userInfo.idleData.mainIdle, out var orthoSize))
                {
                    cameraContrller.SetCameraScale(orthoSize);
                }
                else
                {
                    cameraContrller.ResetCameraScale();
                }
            }
            _relationShipData = resData.relationShip;
            SetUserInfo(resData.userInfo,accountAmount, resData.relationShip);
            _statisticsCard.SetData(resData.statistics);
            _wearingCard.RefreshWithData(_userId,resData.wearings);
            _petWearingCard.RefreshWithData(_userId, resData.petWearings);
            _avatarCard.SetWhiteListData(resData.debugCfg);
            _profileCard.SetCreatorScoreInfo(resData.creatorScoreInfo);
            _creatorSeasonCard.SetCreatorScoreInfo(resData.creatorScoreInfo);
            OnUpdateTheme(userInfo.homepageSkin);

            //如果是自己刷新一下头像框和聊天气泡的拥有状态
            if (AccountDataManager.Inst.IsMySelf(_userId))
            {
                AccountDataManager.Inst.UpdateHeadCycleWithoutNotify(userInfo.ownedAvatarFrameList);
                AccountDataManager.Inst.UpdateChatBubbleWithoutNotify(userInfo.ownedChatBubblesList);
            }

            StartSetPosByBodyType(CharacterData.DeserializeObject(userInfo.avatarJson));
            StartCheckVehicle(); //检查载具
        }

        private void OnUserInfoFail(string message)
        {

        }


        private void OnGetPublishListSuccess(string content)
        {
            if (!this) return;
            GetPublishListRsp resData = JsonConvert.DeserializeObject<GetPublishListRsp>(content);

            bool hasMap =_gamesCard.RefreshWithData(_userId,resData.map);
            bool hasSkin = _outfitsCard.RefreshWithData(_userId,resData.skin);
            bool hasPetSkin = _petOutfitsCard.RefreshWithData(_userId, resData.petSkin);
            bool hasProp = _propsCard.RefreshWithData(_userId,resData.prop);
            bool hasMaterial = _materialsCard.RefreshWithData(_userId, resData.material);
            bool hasMusicScore = _musicScoreCard.RefreshWithData(_userId, resData.musicScore);
            bool hasTone = _toneCard.RefreshWithData(_userId, resData.musicTone);
            bool hasAnim = _animCard.RefreshWithData(_userId, resData.anim);
            bool hasNpc = false;// _npcCard.RefreshWithData(_userId, resData.npc);
            if (_aiCharacterCard != null)
            {
                _aiCharacterCard.RefreshWithData(_userId, hasData =>
                {
                    if (!this) return;
                    if (aiCharacterTab != null) aiCharacterTab.SetActive(hasData);
                    if (hasData) tabScrollGroup.Rebuild();
                });
            }
            bool hasPose = _poseCard.RefreshWithData(_userId, resData.pose);
            bool hasPetAnim = _petAnimCard.RefreshWithData(_userId, resData.petAnim);
            bool hasPetPose = _petPoseCard.RefreshWithData(_userId, resData.petPose);
            bool hasVehicle = _vehicleCard.RefreshWithData(_userId, resData.vehicle);
            bool hasTheater = _theatreCard.RefreshWithData(_userId, resData.theater);
            bool hasActor = _theatreActorCard.RefreshWithData(_userId, resData.actor);
            if (_characterBoxCard != null)
            {
                var jObj = JObject.Parse(content);
                var characterBoxData = jObj["characterBox"]?.ToObject<CharacterBoxPublishListData>();
                bool hasCharacterBox = _characterBoxCard.RefreshWithData(_userId, characterBoxData);
                if (characterBoxTab != null) characterBoxTab.SetActive(hasCharacterBox);
            }
            bool isShowCreate = hasSkin || hasAnim ||  hasPose;
            SetCreateTabVisible(isShowCreate);

            gameTab.SetActive(hasMap);

            bool isShowPet = hasPetSkin || hasPetAnim || hasPetPose;
            petTab.SetActive(isShowPet);

            bool isShowOther = hasProp || hasMaterial || hasMusicScore || hasTone || hasNpc || hasVehicle;
            otherTab.SetActive(isShowOther);

            theaterTab.SetActive(hasTheater);
            actorTab.SetActive(hasActor);

        }


        private void OnGetPublishListFail(string message)
        {

        }

        private void RequestPhotoList(string uid)
        {
            _photoCard.Show(true);
            photoTab.SetActive(true);
            AlbumRequestCtrl.Inst.RequestRemoteAlbumAllPages(uid, 0, 1, pageRes =>
            {
                List<AlbumPhotoInfo> photoList = new List<AlbumPhotoInfo>();
                foreach(var item in pageRes.list){
                    if(item.albumItem.isPublic == 1){
                        photoList.Add(item);
                    }
                }
                _photoCard.RefreshWithData(uid, photoList);
            }, error =>
            {
                photoTab.SetActive(false);
                LoggerUtils.LogError(error);
            });
        }

        private void StartSetPosByBodyType(CharacterData userAvatarData){
            SetPosByBodyType(characterWrap,(CustomBodyTypeController.BodyType)userAvatarData.bodyType);
            var npcAvatarJson = GetHallNpcAvatarJson();
            if (!string.IsNullOrEmpty(npcAvatarJson) && npcWrap != null)
            {
                var npcData = CharacterData.DeserializeObject(npcAvatarJson);
                SetPosByBodyType(npcWrap,(CustomBodyTypeController.BodyType)npcData.bodyType);
            }
        }

        // 摆位/激活用的 buddyInfo：可见性来自 characterInfo（自己 HallCharacterManager / 他人 _targetCharacterInfo），
        // 因为服务端已不返回 aibuddyInfo。GetHallAnimatoionType/OnAnimChange 据 isHidden 决定是否激活并摆位 npc。
        private AIBuddyInfo BuildLayoutBuddyInfo()
        {
            bool isSelf = AccountDataManager.Inst.IsMySelf(_userId);
            if (isSelf)
            {
                // 自己：复用大厅同源 AIBuddyInfo（含 idleData，驱动玩家+伙伴双人动作 emote）；可见性对齐 characterInfo
                var buddy = AccountDataManager.Inst.AIBuddyInfo;
                if (buddy != null) buddy.isHidden = HallCharacterManager.IsHidden ? 1 : 0;
                return buddy;
            }
            // 他人：publicProfile 不返回对方 idle 数据，仅按 characterInfo 可见性静态并排展示（无双人动作）
            bool hidden = _targetCharacterInfo == null || _targetCharacterInfo.isHidden == 1;
            return new AIBuddyInfo { isHidden = hidden ? 1 : 0 };
        }

        // 大厅伙伴形象 avatarJson：自己取 HallCharacterManager 当前皮肤；他人取其 publicProfile 的 characterInfo
        private string GetHallNpcAvatarJson()
        {
            bool isSelf = AccountDataManager.Inst.IsMySelf(_userId);
            if (isSelf)
            {
                return HallCharacterManager.CurrentSkinAvatarJson();
            }
            return _targetCharacterInfo?.GetEquippedAvatarJson();
        }

        private void StartCheckVehicle(){
            bool isSelf = AccountDataManager.Inst.IsMySelf(_userId);
            var userInfo = isSelf ? AccountDataManager.Inst.UserInfo : _accountUserInfo;
            var petInfo = isSelf ? AccountDataManager.Inst.PetInfo : _targetPetInfo;
            var aiBuddyInfo = BuildLayoutBuddyInfo();
            var vehicleInfo = isSelf ? AccountDataManager.Inst.VehicleInfo : _targetVehicleInfo;

            if (vehicleInfo != null && vehicleInfo.isHidden == 0)
            {
                if(npcWrap != null) npcWrap.Avatar.gameObject.SetActive(false);
                if(petWrap != null) petWrap.Avatar.gameObject.SetActive(false);
                AvatarAndPetAnimView animView = new AvatarAndPetAnimView(characterWrap,petWrap, npcWrap, idleBehaviour,petIdleBehaviour, npcIdleBehaviour);
                animView.OnAnimChange(false, AvatarAndPetAnimView.HallRoleAnim.Avatar, userInfo, petInfo, aiBuddyInfo);
                AvatarAndVehicleAnimView vehicleAnimView = new AvatarAndVehicleAnimView(characterWrap, vehicleInfo);
                cameraContrller.ResetEmoteView();
                vehicleAnimView.StartVehicleAnim();
                // 载具骑乘动作覆盖在 idle(Default) 状态上：强制角色进入 idle 而不是 leisure_idle，
                // 并停掉会把状态切回 leisure 的 idle 驱动器，避免骑乘姿势被 leisure_idle 顶掉。
                idleBehaviour.ResetEmoteAnim();
                idleBehaviour.enabled = false;
                if (characterUgcIdleBehaviour != null) characterUgcIdleBehaviour.enabled = false;
                avatarAnimCtrl?.SetPlayerState(PlayerState.Default);
                if(int.TryParse(vehicleInfo.id, out int pgcId))
                {
                    var config = DataTables.GetPgcVehicleConfig(pgcId);
                    cameraContrller.SetVehicleViewByConfig(config);
                }else{
                    cameraContrller.SetCameraZoom((int)ResourceType.UgcVehicle);
                }
            }
            #if UNITY_EDITOR
            if (isSelf && DebugSetting.Inst != null && DebugSetting.Inst.isShowTempVehicle && TempVehicleDataSave.GetTempVehicleInfo() != null) {
                if(npcWrap != null) npcWrap.Avatar.SetActive(false);
                if(petWrap != null) petWrap.Avatar.SetActive(false);
                AvatarAndPetAnimView animView = new AvatarAndPetAnimView(characterWrap,petWrap, npcWrap, idleBehaviour,petIdleBehaviour, npcIdleBehaviour);
                animView.OnAnimChange(false, AvatarAndPetAnimView.HallRoleAnim.Avatar, AccountDataManager.Inst.UserInfo, AccountDataManager.Inst.PetInfo, AccountDataManager.Inst.AIBuddyInfo);
                AvatarAndVehicleAnimView vehicleAnimView = new AvatarAndVehicleAnimView(characterWrap, TempVehicleDataSave.GetTempVehicleInfo());
                vehicleAnimView.StartVehicleAnim();
                cameraContrller.ResetEmoteView();
                // 载具骑乘动作覆盖在 idle(Default) 状态上：强制角色进入 idle 而不是 leisure_idle，
                // 并停掉会把状态切回 leisure 的 idle 驱动器，避免骑乘姿势被 leisure_idle 顶掉。
                idleBehaviour.ResetEmoteAnim();
                idleBehaviour.enabled = false;
                if (characterUgcIdleBehaviour != null) characterUgcIdleBehaviour.enabled = false;
                avatarAnimCtrl?.SetPlayerState(PlayerState.Default);
            }
            #endif
        }

        private void SetPosByBodyType(CharacterWrap wrap, CustomBodyTypeController.BodyType bodyType)
        {
            if(wrap == null) return;
            if(wrap.Avatar == null) return;
            switch(bodyType)
            {
                case CustomBodyTypeController.BodyType.Type1:
                case CustomBodyTypeController.BodyType.Type2:
                    wrap.Avatar.transform.localPosition = new Vector3(0, -0.6f, 0);
                    break;
                case CustomBodyTypeController.BodyType.Type3:
                    wrap.Avatar.transform.localPosition = new Vector3(0, -0.43f, 0);
                    break;
                case CustomBodyTypeController.BodyType.Type4:
                    wrap.Avatar.transform.localPosition = new Vector3(0, -0.26f, 0);
                    break;
                case CustomBodyTypeController.BodyType.Type5:
                    wrap.Avatar.transform.localPosition = new Vector3(0, -0.49f, 0);
                    break;
                case CustomBodyTypeController.BodyType.Type6:
                    wrap.Avatar.transform.localPosition = new Vector3(0, -0.57f, 0);
                    break;
                default:
                    wrap.Avatar.transform.localPosition = new Vector3(0, -0.5f, 0);
                    break;
            }
        }




        protected override void OnDestroy()
        {
            base.OnDestroy();
            AccountDataManager.Inst.RemoveUserInfoChangeListener(OnUserInfoChange);
        }

    }
}
