using System.Collections;
using System.Collections.Generic;
using Es;
using Game.Audio;
using UI.Base;
using UI.BaseWidgets;
using Game.Avatar;
using Game.Pet;
using GameData.BaseInfo;
using GameData.PgcData;
using UnityEngine;
using UnityEngine.UI;


namespace Game.Event
{
    public class EventCenterRewardPanel : BasePanel<EventCenterRewardPanel>
    {
        public Transform _transBG;
        public CButton _btnBack;
        public EventCenterRewardItem itemPrefab;
        public Transform itemParent;
        private EventRewardPanelData _data;

        public Transform characterRoot;
        public AvatarCameraController avatarCameraController;
        
        internal BaseAvatarWrapper avatarWrapper;

        internal CharacterWrap characterWrap;
        internal CharacterWrap otherCharacterWrap;
        internal PetWrap petWrap;
        internal PlayerAnimationCtrl animationCtrl;
        internal PlayerAnimationCtrl otherAnimationCtrl;
        internal PetAnimationCtrl petAnimationCtrl;

        private Image _bgImage;

        public override void OnCreate()
        {
            base.OnCreate();
            _transBG = GameObjectEx.FindChildByName(this.transform, "Trans_BG");
            _bgImage = _transBG.GetComponent<Image>();
            _btnBack = GameObjectEx.FindChildByName(this.transform, "BackButton").GetComponent<CButton>();
            _btnBack.onClick.AddListener(CloseSelf);
        }

        public override void OnShow(params object[] args)
        {
            base.OnShow(args);
            _data = (EventRewardPanelData)args[0];
            InitUI();
            InitRewardIcon();
            StartPreview();

            Invoke("Delay", 0.1f);
        }

        public override void OnHidden()
        {
            base.OnHidden();
            animationCtrl?.ResetEmoteForUICharacter();
            otherAnimationCtrl?.gameObject.SetActive(false);
            otherAnimationCtrl?.ResetEmoteForUICharacter();
            petAnimationCtrl?.ResetEmoteForUICharacter();
        }

        protected override void OnDestroy()
        {
            StopAllEmoteSound();
        }

        public void StartPreview()
        {
            this.gameObject.SetActive(true);
            
            var saveCharacterData = AccountDataManager.Inst.UserInfo.avatarInfo;
            if (saveCharacterData == null)
                saveCharacterData = AvatarDataManager.Inst.GetDefaultDataByGender(1);
            if (saveCharacterData != null)
            {
                characterWrap = AvatarController.Inst.CreateUIAvatar(saveCharacterData);
                characterWrap.SetParent(characterRoot, true);
                animationCtrl = characterWrap.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
                avatarCameraController.RotateTarget = characterRoot;

                var otherCharacterWrap = AvatarController.Inst.CreateUIAvatar(saveCharacterData);
                otherCharacterWrap.SetParent(characterRoot, true);
                otherAnimationCtrl = otherCharacterWrap.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
                otherCharacterWrap.Avatar.gameObject.SetActive(false);
            }
            
            bool hasPet = _data.IsPet;
            if (hasPet)
            {
                animationCtrl.gameObject.SetActive(false);
                var avatarJson = AccountDataManager.Inst.PetInfo.avatarJson;
                var petWrapper = PetAvatarController.Inst.CreateUIAvatar(PetData.DeserializeObject(avatarJson));
                petWrapper.SetParent(characterRoot, true);
                petWrap = petWrapper;
                petAnimationCtrl = petWrapper.Avatar.GetComponentInChildren<PetAnimationCtrl>();
                characterRoot.localScale = Vector3.one * 1.32f;
                characterRoot.localPosition = new Vector3(0, -0.5f, 0);

                avatarCameraController.RotateTarget = characterRoot;
                
                SetCharacterAvatarCamera();
            }
        }

        private void StopAllEmoteSound()
        {
            //关闭的时候清除所有音效
            if (animationCtrl != null && animationCtrl.gameObject != null)
            {
                AkSoundManager.Inst.StopAll(animationCtrl.gameObject);
            }

            if (otherAnimationCtrl != null && otherAnimationCtrl.gameObject != null)
            {
                AkSoundManager.Inst.StopAll(otherAnimationCtrl.gameObject);
            }

            if (petAnimationCtrl != null && petAnimationCtrl.gameObject != null)
            {
                AkSoundManager.Inst.StopAll(petAnimationCtrl.gameObject);
            }
        }

        private void Delay()
        {
            WearPgcClothes(_data.rewardList);
        }

        private void WearPgcClothes(List<string> pgcIds)
        {
            if (pgcIds == null)
            {
                return;
            }

            for (int i = 0; i < pgcIds.Count; i++)
            {
                var pgcId = pgcIds[i];
                var config = DataTables.GetGameResData(pgcId);
                if (config == null) continue;
                switch ((ResourceType)config.ResourceType)
                {
                    case ResourceType.Avatar:
                        TryOn(pgcId);
                        break;
                    case ResourceType.Emote:
                        PreviewEmote(pgcId, (EmoteSubType)config.SubType);
                        break;
                    case ResourceType.PGCPetAvatar:
                        TryOnForPet(pgcId);
                        break;
                }
            }
        }

        internal void TryOn(string pgcId)
        {
            if (characterWrap == null)
            {
                return;
            }

            var config = DataTables.GetAvatarCommonData(pgcId);
            var classType = UniqueType.GetAvatar(pgcId);
            characterWrap.ChangePart(classType, pgcId);
            characterWrap.ChangeColor(classType, config.defaultColor);
            characterWrap.Move(classType, config.pDef);
            characterWrap.Rotate(classType, config.rDef);
            characterWrap.Scale(classType, config.sDef);
            characterWrap.HVScale(classType, config.vhSDef);
            characterWrap.SetLeftOrRight(classType, config.leftRightType);
        }

        private void TryOnForPet(string pgcId)
        {
            if (petWrap == null)
            {
                return;
            }

            var petConfig = DataTables.GetPetAvatarCommonData(pgcId);
            var petSubType = UniqueType.GetPGCPetAvatar((AvatarSubType)petConfig.SubType);

            petWrap.ChangePart(petSubType, pgcId);
            petWrap.ChangeColor(petSubType, petConfig.defaultColor);
            petWrap.Move(petSubType, petConfig.pDef);
            petWrap.Rotate(petSubType, petConfig.rDef);
            petWrap.Scale(petSubType, petConfig.sDef);
            petWrap.HVScale(petSubType, petConfig.vhSDef);
            petWrap.SetLeftOrRight(petSubType, petConfig.leftRightType);
        }

        private void SetPetAvatarCamera()
        {
            avatarCameraController.ZoomUpperPosY = -0.1f;
            avatarCameraController.ZoomUppereCameraSize = 0.4f;
            avatarCameraController.ZoomWholePosY = -0.2f;
            avatarCameraController.ZoomWholeCameraSize = 0.5f;
            avatarCameraController.ZoomFootPosY = -0.46f;
            avatarCameraController.ZoomFootCameraSize = 0.3f;
            avatarCameraController.customEmoteCameraScale = 0.533f;
        }

        private void SetCharacterAvatarCamera()
        {
            avatarCameraController.ZoomUpperPosY = 0.3f;
            avatarCameraController.ZoomUppereCameraSize = 0.6f;
            avatarCameraController.ZoomWholePosY = 0;
            avatarCameraController.ZoomWholeCameraSize = 1f;
            avatarCameraController.ZoomFootPosY = -0.375f;
            avatarCameraController.ZoomFootCameraSize = 0.75f;
            avatarCameraController.customEmoteCameraScale = 1.0f;
        }
        
        internal void PreviewEmote(string pgcId, EmoteSubType emoteSubType)
        {
            avatarCameraController?.SetEmoteView(pgcId);
            animationCtrl?.ResetEmoteForUICharacter();
            otherAnimationCtrl?.gameObject.SetActive(false);
            otherAnimationCtrl?.ResetEmoteForUICharacter();
            petAnimationCtrl?.ResetEmoteForUICharacter();
            switch (emoteSubType)
            {
                case EmoteSubType.Single:
                case EmoteSubType.SingleLoop:
                    animationCtrl?.PlaySingleEmoteForUICharacter(pgcId, null);
                    break;
                case EmoteSubType.Double:
                case EmoteSubType.DoubleLoop:
                    animationCtrl?.PlayDoubleEmoteForUICharacter(pgcId, otherAnimationCtrl, null);
                    break;
                case EmoteSubType.PetSingle:
                case EmoteSubType.PetSingleLoop:
                    petAnimationCtrl?.PlaySingleEmoteForUICharacter(pgcId, null);
                    break;
                case EmoteSubType.PetWithPlayer:
                case EmoteSubType.PetWithPlayerLoop:
                    SetPetAvatarCamera();
                    animationCtrl.gameObject.SetActive(true);
                    petAnimationCtrl?.PlayPetWithPlayerEmoteForUICharacter(pgcId, animationCtrl,
                        null, true);
                    break;
                    
            }
        }

        private void InitRewardIcon()
        {
            for (int i = 0; i < _data.rewardList.Count; i++)
            {
                var item = GameObject.Instantiate(itemPrefab, itemParent);
                item.InitData(_data.rewardItemBgColor, _data.rewardList[i]);
            }
        }

        private void InitUI()
        {
            if (_transBG == null)
            {
                return;
            }

            if ((_data.iconList == null || _data.iconList.Count <= 0) && !string.IsNullOrEmpty(_data.bgSpriteName))
            {
                var sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(_data.atlasPath, _data.bgSpriteName, gameObject);
                if (_bgImage != null)
                {
                    _bgImage.sprite = sprite;
                }
            }
            else
            {
                var itemObj = Loader
                    .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
                    .Instantiate(_transBG);
                var item = itemObj.GetComponent<ActivityCenterBgItem>();
                item.InitCustomBgItem(_data.bgColor, _data.atlasPath, _data.iconList);
                item.gameObject.SetActive(true);
            }

          
        }
    }

    public class EventRewardPanelData
    {
        public string bgColor;
        public string atlasPath;
        public string rewardItemBgColor;
        public string bgSpriteName;
        public List<string> iconList;
        public List<string> rewardList;
        public bool IsPet = false;
    }
}