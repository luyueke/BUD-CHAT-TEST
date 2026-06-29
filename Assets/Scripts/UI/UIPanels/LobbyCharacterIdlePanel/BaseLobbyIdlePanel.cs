using System.Collections.Generic;
using BUD.AnimPose;
using Game.Avatar;
using Game.Pet;
using GameData.PgcData;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.LobbyCharacterIdlePanel {


    public enum LobbyRoleType
    {
        Avatar,
        Pet,
        Npc,
    }

    //不包含宠物隐藏，右边分签方式
    // public enum UIModeType
    // {
    //     SingleTab = 0,
    //     DoubleTab
    // }

    public enum HallAnimType
    {
        NotInteractive = 0,
        Interactive
    }

    public abstract class BaseLobbyIdlePanel<T> : BasePanel<T> where T : BasePanel<T> {
        [SerializeField] internal Transform transBg;
        [Header("人物形象")] [SerializeField] internal Transform characterRoot;
        [SerializeField] internal AvatarCameraController avatarCameraController;
        [SerializeField] internal Button backButton;

        [Header("主标签UI")] [SerializeField] internal MainTabs mainTabsUI;
        [Header("二级标签UI")] [SerializeField] internal SecondTabs secondTabsUI;
        [Header("三级标签UI")] [SerializeField] internal SecondTabs thirdTabsUI;

        [Header("重置")] [SerializeField] internal Button resetButton;

        [Header("设置模式")] [SerializeField] internal GameObject modePanel;
        [SerializeField] internal Button singleBtn;
        [SerializeField] internal Button doubleBtn;
        [SerializeField] internal Button selectModeBtn;
        [SerializeField] internal Transform arrowNode;
        [SerializeField] internal BasePGCLobbyIdleView pgcLobbyView;
        [SerializeField] internal BaseUGCLobbyIdleView ugcLobbyView;


        protected CharacterWrap characterWrap;
        protected AnimIKController avatarIkController;
        protected AvatarAnimIK avatarAnimIK;


        protected AvatarAnimIK otherIkController;
        protected CharacterWrap otherCharacterWrap;
        protected PlayerAnimationCtrl otherAnimationCtrl;


        // otherIkController = otherCharacterWrap.Avatar.GetComponent<AvatarAnimIK>();
        protected IdleData playerIdleData;


        protected MainTabs.Tab firstTab;
        protected SecondTabs.Tab secondTab;
        protected SecondTabs.Tab thirdTab;

        protected HallAnimType animType = HallAnimType.NotInteractive;


        public override void OnCreate() {
            base.OnCreate();
            InitUI();
        }


        internal virtual void InitUI() {
            if (transBg == null) {
                return;
            }

            string atlasPath = "Assets/Loadable/UI/UIPanel/CommonBgPanel/CommonBgIcon.spriteatlas";
            string prefabPath = "Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab";
            var itemObj = Loader.Load<GameObject>(prefabPath).Instantiate(transBg);
            var item = itemObj.GetComponent<ActivityCenterBgItem>();
            item.InitCustomBgItem("#FFFFFF", atlasPath, new List<string>() {
                "AvatarBg_icon1", "AvatarBg_icon5", "AvatarBg_icon3", "AvatarBg_icon4", "AvatarBg_icon2"
            });
            item.gameObject.SetActive(true);

            backButton.onClick.AddListener(OnBackClick);
            resetButton.onClick.AddListener(OnResetClick);
            CreateAvatar();

            mainTabsUI.SetCallback(OnMainTabs);
            secondTabsUI.SetCallback(OnSecondTabs);
            thirdTabsUI.SetCallback(OnThirdTabs);

            singleBtn.onClick.AddListener(() => {
                SinglePersonModeClick();
            });
            doubleBtn.onClick.AddListener(() => {
                DoublePersonModeClick();
            });
            selectModeBtn.onClick.AddListener(ChangeModeClick);
            pgcLobbyView.OnCreate();
            ugcLobbyView.OnCreate();
        }


        internal virtual void CreateAvatar() {
            var data = AccountDataManager.Inst.UserInfo.idleData;
            playerIdleData = data != null
                ? data.Clone()
                : new IdleData() { mainIdle = BaseIdleBehaviour.IdleLeisureName, subIdle = new() };

            var saveCharacterData = AccountDataManager.Inst.UserInfo.avatarInfo;
            if (saveCharacterData == null)
                saveCharacterData = AvatarDataManager.Inst.GetDefaultDataByGender(1);
            if (saveCharacterData != null) {
                characterWrap = AvatarController.Inst.CreateUIAvatarWithIKController(saveCharacterData, characterRoot);
                avatarIkController = characterWrap.Avatar.GetComponent<AnimIKController>();
                avatarAnimIK = characterWrap.Avatar.GetComponent<AvatarAnimIK>();
                var animationCtrl = characterWrap.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
                avatarCameraController.RotateTarget = characterRoot;
                var avatarIdleBehaviour = characterWrap.Avatar.AddComponent<PlayerIdleBehaviour>();
                avatarIdleBehaviour.Init(animationCtrl, true);
                avatarIdleBehaviour.SetData(playerIdleData);


                otherCharacterWrap = AvatarController.Inst.CreateUIAvatarWithIK(saveCharacterData,characterRoot);
                otherAnimationCtrl = otherCharacterWrap.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
                otherIkController = otherCharacterWrap.Avatar.GetComponent<AvatarAnimIK>();
                otherCharacterWrap.Avatar.SetActive(false);
                otherCharacterWrap.Avatar.name = "OtherCharacter";


            }
        }


        private void OnMainTabs(MainTabs.Tab tab) {
            firstTab = tab;
            SetCurrentRole();
            var curIdleData = GetIdleData(firstTab);
            secondTab = curIdleData.animResType == 0 ? SecondTabs.Tab.Main : SecondTabs.Tab.Sub;
            secondTabsUI.SetIsOnWithoutNotify(secondTab);
            thirdTab = SecondTabs.Tab.Main;
            thirdTabsUI.DefualtOn(thirdTab);
        }

        private void OnSecondTabs(SecondTabs.Tab tab) {
            secondTab = tab;
            thirdTab = SecondTabs.Tab.Main;
            var curIdleData = GetIdleData(firstTab);
            curIdleData.animResType = (int)secondTab;
            ResetDefaultIdleData();
            thirdTabsUI.DefualtOn(thirdTab);
        }

        protected abstract IdleData GetIdleData(MainTabs.Tab tab);

        private void OnThirdTabs(SecondTabs.Tab tab) {
            thirdTab = tab;
            UpdateIdleView();
        }

        private void ChangeModeClick()
        {
            arrowNode.transform.localEulerAngles = Vector3.zero;
            modePanel.SetActive(true);
        }

        protected virtual void ResetDefaultIdleData() {
        }

        protected virtual void ResetOnlyPlayerData()
        {
            switch (secondTab)
            {
                case SecondTabs.Tab.Main:
                    playerIdleData.animResType = 0;
                    playerIdleData.mainIdle = BaseIdleBehaviour.IdleLeisureName;
                    playerIdleData.subIdle?.Clear();
                    playerIdleData.ugcIdleList?.Clear();
                    break;
                case SecondTabs.Tab.Sub:
                    playerIdleData.animResType = 1;
                    var animInfo = ugcLobbyView.GetDefaultAnimInfo(UgcAnimSubType.Single);
                    playerIdleData.mainIdle = animInfo.id;
                    playerIdleData.subIdle?.Clear();
                    playerIdleData.ugcIdleList = new List<UgcIdleData> {animInfo};
                    break;
            }
            playerIdleData.personType = 0;
        }

        protected void SetPlayerPgcDefault()
        {
            playerIdleData.animResType = 0;
            playerIdleData.personType = 0;
            playerIdleData.mainIdle = BaseIdleBehaviour.IdleLeisureName;
            playerIdleData.subIdle?.Clear();
            playerIdleData.ugcIdleList?.Clear();
        }

        protected void SetPlayerUgcDefault()
        {
            var animInfo = ugcLobbyView.GetDefaultAnimInfo(UgcAnimSubType.Single);
            playerIdleData.animResType = 1;
            playerIdleData.personType = 0;
            playerIdleData.mainIdle = animInfo.id;
            playerIdleData.subIdle?.Clear();
            playerIdleData.ugcIdleList = new List<UgcIdleData>(){animInfo};
        }

        protected virtual void UpdateIdleView() {
            pgcLobbyView.gameObject.SetActive(secondTab == SecondTabs.Tab.Main);
            ugcLobbyView.gameObject.SetActive(secondTab == SecondTabs.Tab.Sub);
            var curRoleType = GetRoleType();
            switch (secondTab)
            {
                case SecondTabs.Tab.Main:
                    pgcLobbyView.OnThirdTabs(animType,curRoleType , thirdTab);
                    break;
                case SecondTabs.Tab.Sub:
                    pgcLobbyView.ForceChangeDefaultAnim(curRoleType);
                    ugcLobbyView.OnThirdTabs(animType,curRoleType,thirdTab);
                    break;
            }
        }

        protected abstract LobbyRoleType GetRoleType();


        protected virtual bool CheckUpdateHallAnim() {

            if (AccountDataManager.Inst.UserInfo.idleData != playerIdleData)
            {
                AccountDataManager.Inst.UserInfo.idleData = playerIdleData;
                AccountDataManager.Inst.SyncIdleData(playerIdleData);
                return true;
            }

            return false;
        }

        internal virtual void OnBackClick() {
            CheckUpdateHallAnim();
            UIManager.Inst.ClosePanel(this);
        }


        protected virtual void OnResetClick() {
            ResetDefaultIdleDataOnClick();
            switch (secondTab) {
                case SecondTabs.Tab.Main:
                    pgcLobbyView.OnResetClick();
                    break;
                case SecondTabs.Tab.Sub:
                    ugcLobbyView.OnResetClick();
                    break;
            }
        }

        internal virtual void ResetDefaultIdleDataOnClick() {
        }

        protected virtual void ResetIdleDataByButton(IdleData idleData,string pgcDefaultID, UgcAnimSubType ugcAnimType)
        {
            switch (secondTab)
            {
                case SecondTabs.Tab.Main:
                    idleData.animResType = 0;
                    idleData.ugcIdleList?.Clear();
                    if (thirdTab == SecondTabs.Tab.Main)
                    {
                        idleData.mainIdle = pgcDefaultID;
                    }
                    else
                    {
                        idleData.subIdle?.Clear();
                    }

                    break;
                case SecondTabs.Tab.Sub:
                    UgcIdleData mainUgcIdle = null;
                    if (thirdTab == SecondTabs.Tab.Main)
                    {
                        mainUgcIdle = ugcLobbyView.GetDefaultAnimInfo(ugcAnimType);
                        idleData.mainIdle = mainUgcIdle.id;
                    }
                    else
                    {
                        mainUgcIdle = idleData.ugcIdleList?.Find(x=>x.id.Equals(idleData.mainIdle));
                        idleData.subIdle?.Clear();
                    }

                    if (mainUgcIdle != null)
                    {
                        idleData.ugcIdleList.Clear();
                        idleData.ugcIdleList.Add(mainUgcIdle);
                    }
                    break;
            }

            playerIdleData.personType = 0;
        }

        protected virtual void SetCurrentRole() {
            characterWrap.Avatar.gameObject.SetActive(firstTab == MainTabs.Tab.Main);
        }

        protected virtual bool SinglePersonModeClick()
        {
            if (animType == HallAnimType.NotInteractive)
            {
                CloseModePanel();
                return false;
            }
            animType = HallAnimType.NotInteractive;
            secondTab = SecondTabs.Tab.Main;

            SelectAnimModeClickOnUI(animType);
            ResetAllPlayerData();
            mainTabsUI.DefualtOn(MainTabs.Tab.Main);
            return true;
        }

        protected virtual bool DoublePersonModeClick()
        {
            if (animType == HallAnimType.Interactive)
            {
                CloseModePanel();
                return false;
            }
            animType = HallAnimType.Interactive;
            secondTab = SecondTabs.Tab.Main;
            SelectAnimModeClickOnUI(animType);
            return true;
        }


        private void CloseModePanel()
        {
            arrowNode.transform.localEulerAngles = new Vector3(0, 0, -90);
            modePanel.SetActive(false);
        }

        /// <summary>
        /// UI表现
        /// </summary>
        /// <param name="aType"></param>
        protected virtual void SelectAnimModeClickOnUI(HallAnimType aType)
        {
            bool isInteractive = aType == HallAnimType.Interactive;
            SelectAnimModeOnOtherIkController(isInteractive);
            var selectBtn = isInteractive ? doubleBtn : singleBtn;
            selectModeBtn.GetComponent<Image>().color = selectBtn.GetComponent<Image>().color;
            selectModeBtn.GetComponentInChildren<Text>().SetLocalText(selectBtn.GetComponentInChildren<LocalizationComponent>().localizationKey);
            mainTabsUI.SetToggleVisible(MainTabs.Tab.Main, !isInteractive);
            CloseModePanel();
        }


        protected virtual void SelectAnimModeOnOtherIkController(bool isInteractive)
        {
        }

        protected virtual void ResetAllPlayerData()
        {
            playerIdleData.animResType = 0;
            playerIdleData.personType = 0;
            playerIdleData.mainIdle = BaseIdleBehaviour.IdleLeisureName;
            playerIdleData.subIdle?.Clear();
            playerIdleData.ugcIdleList?.Clear();
        }

    }
}
