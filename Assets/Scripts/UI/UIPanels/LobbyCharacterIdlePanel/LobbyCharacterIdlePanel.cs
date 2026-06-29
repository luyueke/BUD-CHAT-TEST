using System;
using Game.Avatar;
using Game.Store;
using System.Collections.Generic;
using BUD.AnimPose;
using Es;
using Game.Pet;
using GameData.Account;
using GameData.BaseInfo;
using GameData.PgcData;
using Pb.Base;
using UI.Base;
using UI.UIPanels.AvatarImage.ColorPicker;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace UI.UIPanels.LobbyCharacterIdlePanel
{


    public class LobbyCharacterIdlePanel : BaseLobbyIdlePanel<LobbyCharacterIdlePanel>
    {
        public Action<AccountUserInfo, AccountPetInfo> UpdateHallAnim;

        private AnimIKController petIkController;
        [Header("宠物形象")]
        private PetWrap petWrap;

        private IdleData petIdleData;
        // private UIModeType curUIType = UIModeType.SingleTab;

        private string doubleDefaultId = "40800361";

        private Dictionary<UgcAnimSubType, string> ugcDefaultIds = new Dictionary<UgcAnimSubType, string>();
        private string previewId;
        public bool isHasPet = true;
        public override void OnCreate()
        {
            base.OnCreate();
            InitUgcDefaultIds();
        }

        private void InitUgcDefaultIds()
        {
            ugcDefaultIds.Clear();
            ugcDefaultIds.Add(UgcAnimSubType.Single,"default_"+ (int)UgcAnimSubType.Single);
            ugcDefaultIds.Add(UgcAnimSubType.PetSingle,"default_"+ (int)UgcAnimSubType.PetSingle);
            ugcDefaultIds.Add(UgcAnimSubType.PetWithPlayer,"default_"+ (int)UgcAnimSubType.PetWithPlayer);
        }

        public override void OnShow(params object[] args)
        {
            base.OnShow(args);
            CreatePet();
            isHasPet = AccountDataManager.Inst.PetInfo?.isHidden == 0;
            Action showAction = isHasPet ? ShowCharacterOnDouble : ShowCharacterByOnlySingle;
            showAction.Invoke();

            mainTabsUI.SetIsOnWithoutNotify(firstTab);
            secondTabsUI.SetIsOnWithoutNotify(secondTab);
            thirdTabsUI.SetIsOnWithoutNotify(thirdTab);
            UpdateIdleView();
            SetCurrentRole();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            var panel = UIManager.Inst.FindPanel<FittingRoomPanel>(PanelId.FittingRoomPanel);
            if (panel != null)
            {
                panel.characterRoot.gameObject.SetActive(false);
            }
        }

        protected override void OnDisable()
        {
            var panel = UIManager.Inst.FindPanel<FittingRoomPanel>(PanelId.FittingRoomPanel);
            if (panel != null)
            {
                panel.characterRoot.gameObject.SetActive(true);
            }
            base.OnDisable();
        }


        private void ShowCharacterByOnlySingle()
        {
            firstTab = MainTabs.Tab.Main;
            secondTab = playerIdleData.animResType == 0? SecondTabs.Tab.Main:SecondTabs.Tab.Sub;
            thirdTab = SecondTabs.Tab.Main;

            selectModeBtn.gameObject.SetActive(false);
            mainTabsUI.SetToggleLineVisible(false);
            mainTabsUI.SetToggleVisible(MainTabs.Tab.Sub, false);
            (pgcLobbyView as PetPGCLobbyIdleView)?.OnShow(characterWrap,otherCharacterWrap,petWrap);
            (ugcLobbyView as PetUGCLobbyIdleView)?.OnShow(characterWrap,otherCharacterWrap,petWrap);


        }

        private void ShowCharacterOnDouble()
        {
            var chaData = AccountDataManager.Inst.UserInfo.idleData;
            var petInfo = AccountDataManager.Inst.PetInfo;
            bool isInteractive = IsDoubleInteractionAnim();
            animType = isInteractive ? HallAnimType.Interactive : HallAnimType.NotInteractive;
            firstTab = isInteractive ? MainTabs.Tab.Sub : MainTabs.Tab.Main;
            var resType = isInteractive ? petInfo.idleData?.animResType ?? 0 : chaData.animResType;
            secondTab = resType == 0 ? SecondTabs.Tab.Main:SecondTabs.Tab.Sub;
            thirdTab = SecondTabs.Tab.Main;

            SelectAnimModeClickOnUI(isInteractive ? HallAnimType.Interactive : HallAnimType.NotInteractive);
            (pgcLobbyView as PetPGCLobbyIdleView)?.OnShow(characterWrap,otherCharacterWrap,petWrap);
            (ugcLobbyView as PetUGCLobbyIdleView)?.OnShow(characterWrap,otherCharacterWrap,petWrap);
        }


        //兼容PGC资源
        private bool IsDoubleInteractionAnim()
        {
            var petInfo = AccountDataManager.Inst.PetInfo;
            if (petInfo != null && petInfo.idleData != null)
            {
                if ((AnimResType)petInfo.idleData.animResType == AnimResType.PGC)
                {
                    var emoData = Es.DataTables.GetEmoUIConfig(petInfo.idleData.mainIdle);
                    if (emoData != null)
                    {
                        var emoAniType = (EmoteType) emoData.emoAniType;
                        if (emoAniType == EmoteType.PetWithPlayer || emoAniType == EmoteType.PetWithPlayerLoop)
                        {
                            return true;
                        }
                    }
                }
                else
                {
                    return petInfo.idleData.personType == (int)HallAnimType.Interactive;
                }
            }
            return false;
        }
        protected override bool SinglePersonModeClick()
        {
            if (!base.SinglePersonModeClick()) {
                return false;
            }
            otherCharacterWrap.Avatar.SetActive(false);
            return true;
        }

        protected override bool DoublePersonModeClick()
        {
            if (!base.DoublePersonModeClick()) {
                return false;
            }
            ResetPetWithPlayerData();
            otherCharacterWrap.Avatar.SetActive(true);
            mainTabsUI.DefualtOn(MainTabs.Tab.Sub);
            return true;
        }

        /// <summary>
        /// UI表现
        /// </summary>
        /// <param name="aType"></param>
        protected override void SelectAnimModeClickOnUI(HallAnimType aType)
        {
            base.SelectAnimModeClickOnUI(aType);
            bool isInteractive = aType == HallAnimType.Interactive;
            string tabName = isInteractive? "人和宠物动画":"宠物动画";
            mainTabsUI.SetToggleLineVisible(!isInteractive);
            mainTabsUI.SetToggleName(MainTabs.Tab.Sub, tabName);
        }

        protected override void SelectAnimModeOnOtherIkController(bool isInteractive)
        {
            if (isInteractive)
            {
                petIkController.Insert(0,otherIkController);
            }
            else
            {
                petIkController.RemoveAnimIK(otherIkController);
            }
        }

        private void CreatePet()
        {
            var data = AccountDataManager.Inst.PetInfo.idleData;
            petIdleData = data != null
                ? data.Clone()
                : new IdleData() {mainIdle = BaseIdleBehaviour.IdleLeisureName, subIdle = new()};

            var petData = AccountDataManager.Inst.PetInfo.avatarInfo;
            if (petData != null && petWrap == null)
            {
                petWrap = PetAvatarController.Inst.CreateUIAvatarWithIKController(petData,characterRoot);
                petIkController = petWrap.Avatar.GetComponent<AnimIKController>();
                var petAnimationCtrl = petWrap.Avatar.GetComponentInChildren<PetAnimationCtrl>();
                avatarCameraController.RotateTarget = characterRoot;
                var petIdleBehaviour = petWrap.Avatar.AddComponent<PetPlayerIdleBehaviour>();
                petIdleBehaviour.avatarAnimCtr = otherAnimationCtrl;
                petIdleBehaviour.Init(petAnimationCtrl, true);
                petIdleBehaviour.SetData(petIdleData);
            }
        }


        protected override bool CheckUpdateHallAnim() {
            bool isUpdateAnim = base.CheckUpdateHallAnim();
            if (AccountDataManager.Inst.PetInfo.idleData != petIdleData)
            {
                AccountDataManager.Inst.PetInfo.idleData = petIdleData;
                AccountDataManager.Inst.SyncPetIdleData(petIdleData);
                isUpdateAnim = true;
            }

            if (isUpdateAnim)
            {
                UpdateHallAnim?.Invoke(AccountDataManager.Inst.UserInfo,AccountDataManager.Inst.PetInfo);
            }
            return isUpdateAnim;
        }

        protected override IdleData GetIdleData(MainTabs.Tab tab) {
            return tab == MainTabs.Tab.Main ? playerIdleData : petIdleData;
        }


        protected override void ResetAllPlayerData()
        {
            base.ResetAllPlayerData();

            petIdleData.animResType = 0;
            petIdleData.personType = 0;
            petIdleData.mainIdle = BaseIdleBehaviour.IdleDefaultName;
            petIdleData.subIdle?.Clear();
            petIdleData.ugcIdleList?.Clear();
        }

        protected override void ResetOnlyPlayerData()
        {
            base.ResetOnlyPlayerData();
            petIdleData.mainIdle = BaseIdleBehaviour.IdleDefaultName;
            petIdleData.subIdle?.Clear();
        }

        private void ResetPetWithPlayerData()
        {
            switch (secondTab)
            {
                case SecondTabs.Tab.Main:
                    petIdleData.animResType = 0;
                    petIdleData.mainIdle = doubleDefaultId;
                    petIdleData.ugcIdleList = new List<UgcIdleData>();
                    break;
                case SecondTabs.Tab.Sub:
                    petIdleData.animResType = 1;
                    var animInfo = ugcLobbyView.GetDefaultAnimInfo(UgcAnimSubType.PetWithPlayer);
                    petIdleData.mainIdle = animInfo.id;
                    petIdleData.ugcIdleList = new List<UgcIdleData>() { animInfo};
                    break;
            }

            petIdleData.personType = 1;
            petIdleData.subIdle?.Clear();

            playerIdleData.animResType = 0;
            playerIdleData.personType = 0;
            playerIdleData.mainIdle = BaseIdleBehaviour.IdleLeisureName;
            playerIdleData.subIdle?.Clear();
            playerIdleData.ugcIdleList?.Clear();
        }


        protected override void ResetDefaultIdleData()
        {
            if (!isHasPet)
            {
                ResetOnlyPlayerData();
                return;
            }
            if (animType == HallAnimType.Interactive)
            {
                ResetPetWithPlayerData();
                return;
            }

            if (firstTab == MainTabs.Tab.Main)
            {
                if (secondTab == SecondTabs.Tab.Main)
                {
                    SetPlayerPgcDefault();
                }
                else if(secondTab == SecondTabs.Tab.Sub)
                {
                    SetPlayerUgcDefault();
                }
            }
            else
            {
                if (secondTab == SecondTabs.Tab.Main)
                {
                    SetPetPgcDefault();
                }
                else if(secondTab == SecondTabs.Tab.Sub)
                {
                    SetPetUgcDefault();
                }
            }
        }


        internal override void ResetDefaultIdleDataOnClick() {
            base.ResetDefaultIdleDataOnClick();
            if (!isHasPet)
            {
                ResetIdleDataByButton(playerIdleData,BaseIdleBehaviour.IdleLeisureName,UgcAnimSubType.Single);
                return;
            }
            if (animType == HallAnimType.Interactive)
            {
                ResetIdleDataByButton(petIdleData,doubleDefaultId,UgcAnimSubType.PetWithPlayer);
                return;
            }
            if (firstTab == MainTabs.Tab.Main)
            {
                ResetIdleDataByButton(playerIdleData,BaseIdleBehaviour.IdleLeisureName,UgcAnimSubType.Single);
            }
            else
            {
                ResetIdleDataByButton(petIdleData,BaseIdleBehaviour.IdleDefaultName,UgcAnimSubType.PetSingle);
            }
        }

        protected override void ResetIdleDataByButton(IdleData idleData,string pgcDefaultID, UgcAnimSubType ugcAnimType)
        {
            base.ResetIdleDataByButton(idleData,pgcDefaultID,ugcAnimType);
            playerIdleData.personType = 0;
            petIdleData.mainIdle = BaseIdleBehaviour.IdleDefaultName;
            petIdleData.subIdle?.Clear();
        }


        private void SetPetPgcDefault()
        {
            petIdleData.animResType = 0;
            petIdleData.personType = 0;
            petIdleData.mainIdle = BaseIdleBehaviour.IdleDefaultName;
            petIdleData.subIdle?.Clear();
            petIdleData.ugcIdleList?.Clear();
        }

        private void SetPetUgcDefault()
        {
            petIdleData.animResType = 1;
            petIdleData.personType = 0;
            var animInfo = ugcLobbyView.GetDefaultAnimInfo(UgcAnimSubType.PetSingle);
            petIdleData.mainIdle = animInfo.id;
            petIdleData.subIdle?.Clear();
            petIdleData.ugcIdleList = new List<UgcIdleData> {animInfo};
        }

        protected override LobbyRoleType GetRoleType() {
            return firstTab == MainTabs.Tab.Main ? LobbyRoleType.Avatar : LobbyRoleType.Pet;
        }

        protected override void UpdateIdleView()
        {
            base.UpdateIdleView();
            otherCharacterWrap.Avatar.GetComponent<Animator>().enabled = secondTab == SecondTabs.Tab.Main;
            avatarIkController.ChangeAnimResType(secondTab == SecondTabs.Tab.Sub ? AnimResType.UGC : AnimResType.PGC);
            petIkController.ChangeAnimResType(secondTab == SecondTabs.Tab.Sub ? AnimResType.UGC : AnimResType.PGC);
            avatarIkController.RemovePropIks();
            petIkController.RemovePropIks();
            SetPetWithPlayerPosition();
        }

        private void SetPetWithPlayerPosition()
        {
            var poseModeData = DataTables.GetPoseModeConfig((int)UgcPoseSubType.Single);
            characterWrap.Avatar.transform.localPosition = poseModeData.RoleDefPos[0];
            var parent1 = characterWrap.Avatar.transform.parent;
            parent1.localPosition =  poseModeData.EditPos[0];
            parent1.localEulerAngles = Vector3.zero;

            otherCharacterWrap.Avatar.transform.localPosition = poseModeData.RoleDefPos[0];
            var parent2 = otherCharacterWrap.Avatar.transform.parent;
            parent2.localPosition = poseModeData.EditPos[0];
            parent2.localEulerAngles = Vector3.zero;

            var petPoseModeData = DataTables.GetPoseModeConfig((int)UgcPoseSubType.PetSingle);
            petWrap.Avatar.transform.localPosition = petPoseModeData.RoleDefPos[0];
            var parent3 = petWrap.Avatar.transform.parent;
            parent3.localPosition =  petPoseModeData.EditPos[0];
            parent3.localEulerAngles = Vector3.zero;


            if (animType == HallAnimType.NotInteractive)
            {
                characterWrap.CustomAvatar.transform.localPosition = Vector3.zero;
                petWrap.CustomAvatar.transform.localPosition = Vector3.zero;
                otherCharacterWrap.CustomAvatar.transform.localPosition = Vector3.zero;
            }
            else
            {
                if (secondTab == SecondTabs.Tab.Main)
                {
                    petWrap.CustomAvatar.transform.localPosition = Vector3.zero;
                }
                else
                {
                    Vector3 position = new Vector3(-0.3f, 0, 0);
                    otherCharacterWrap.CustomAvatar.transform.localPosition = position;
                    petWrap.CustomAvatar.transform.localPosition = position;
                }
            }
        }

        protected override void SetCurrentRole()
        {
            base.SetCurrentRole();
            if (petWrap != null)
            {
                var poseModeData = DataTables.GetPoseModeConfig((int)UgcPoseSubType.PetSingle);
                petWrap.Avatar.transform.localPosition = poseModeData.RoleDefPos[0];
                petWrap.Avatar.transform.parent.localPosition =  poseModeData.EditPos[0];
                petWrap.Avatar.gameObject.SetActive(firstTab ==  MainTabs.Tab.Sub);
            }
        }
    }
}
