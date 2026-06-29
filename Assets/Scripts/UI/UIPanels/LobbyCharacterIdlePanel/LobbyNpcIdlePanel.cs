using System;
using System.Collections.Generic;
using BUD.AnimPose;
using Es;
using Game.Avatar;
using GameData.Account;
using GameData.BaseInfo;
using GameData.PgcData;
using Pb.Base;
using UnityEngine;

namespace UI.UIPanels.LobbyCharacterIdlePanel {
    public class LobbyNpcIdlePanel : BaseLobbyIdlePanel<LobbyNpcIdlePanel> {
        public Action<AccountUserInfo, AIBuddyInfo> UpdateHallAnim;



        private AnimIKController npcIkController;
        [Header("宠物形象")]
        private CharacterWrap npcWrap;


        private Dictionary<UgcAnimSubType, string> ugcDefaultIds = new Dictionary<UgcAnimSubType, string>();
        private IdleData npcIdleData;
        private string doubleDefaultId = "40400452";

        private string previewId;
        private bool isHasNpc;


        public override void OnShow(params object[] args) {
            base.OnShow(args);

            isHasNpc = !HallCharacterManager.IsHidden;
            Action showAction = isHasNpc ? ShowCharacterOnDouble : ShowCharacterByOnlySingle;
            showAction.Invoke();

            mainTabsUI.SetIsOnWithoutNotify(firstTab);
            secondTabsUI.SetIsOnWithoutNotify(secondTab);
            thirdTabsUI.SetIsOnWithoutNotify(thirdTab);
            UpdateIdleView();
            SetCurrentRole();

        }

        private void ShowCharacterByOnlySingle()
        {
            var data = AccountDataManager.Inst.UserInfo.idleData;

            firstTab = MainTabs.Tab.Main;
            secondTab = data.animResType == 0? SecondTabs.Tab.Main:SecondTabs.Tab.Sub;
            thirdTab = SecondTabs.Tab.Main;

            selectModeBtn.gameObject.SetActive(false);
            mainTabsUI.SetToggleLineVisible(false);
            mainTabsUI.SetToggleVisible(MainTabs.Tab.Sub, false);
            (pgcLobbyView as NPCPGCLobbyIdleView)?.OnShow(characterWrap,otherCharacterWrap,npcWrap);
            (ugcLobbyView as NpcUGCLobbyIdleView)?.OnShow(characterWrap,otherCharacterWrap,npcWrap);
        }



        internal override void CreateAvatar() {
            base.CreateAvatar();

            //TODO: 待修改为 AIBuddy 数据
            var data = AccountDataManager.Inst.AIBuddyInfo.idleData;
            npcIdleData = data != null
                ? data.Clone()
                : new IdleData() {mainIdle = BaseIdleBehaviour.IdleLeisureName, subIdle = new()};


            var saveCharacterData = AccountDataManager.Inst.UserInfo.avatarInfo;
            if (saveCharacterData == null)
                saveCharacterData = AvatarDataManager.Inst.GetDefaultDataByGender(1);
            if (saveCharacterData != null) {
                // 形象改用大厅伙伴角色当前皮肤（characterInfo）；无则回退玩家形象，保证 npc 被创建出来
                var npcSkinJson = HallCharacterManager.CurrentSkinAvatarJson();
                if (string.IsNullOrEmpty(npcSkinJson)) {
                    npcSkinJson = AccountDataManager.Inst.UserInfo.avatarJson;
                }
                npcWrap = AvatarController.Inst.CreateUIAvatarWithIKController(CharacterData.DeserializeObject(npcSkinJson),characterRoot);
                npcWrap.Avatar.name = "NpcCharacter";
                npcIkController = npcWrap.Avatar.GetComponent<AnimIKController>();
                var playerAnimationCtrl = npcWrap.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
                avatarCameraController.RotateTarget = characterRoot;
                var npcIdleBehaviour = npcWrap.Avatar.AddComponent<NpcPlayerIdleBehaviour>();
                npcIdleBehaviour.avatarAnimCtr = otherAnimationCtrl;
                npcIdleBehaviour.Init(playerAnimationCtrl, true);
                npcIdleBehaviour.SetData(npcIdleData);
            }
        }

        protected override bool CheckUpdateHallAnim() {
            bool isUpdateAnim = base.CheckUpdateHallAnim();
            // TODO: 待修改为 AIBuddy 数据
            if (AccountDataManager.Inst.AIBuddyInfo.idleData != npcIdleData)
            {
                AccountDataManager.Inst.AIBuddyInfo.idleData = npcIdleData;
                AccountDataManager.Inst.SyncAIBuddyIdleData(npcIdleData);
                isUpdateAnim = true;
            }

            if (isUpdateAnim)
            {
                UpdateHallAnim?.Invoke(AccountDataManager.Inst.UserInfo,AccountDataManager.Inst.AIBuddyInfo);
            }
            return isUpdateAnim;
        }



        protected override IdleData GetIdleData(MainTabs.Tab tab) {
            return tab == MainTabs.Tab.Main ? playerIdleData : npcIdleData;
        }

        protected override void ResetAllPlayerData() {
            base.ResetAllPlayerData();
            npcIdleData.animResType = 0;
            npcIdleData.personType = 0;
            npcIdleData.mainIdle = BaseIdleBehaviour.IdleDefaultName;
            npcIdleData.subIdle?.Clear();
            npcIdleData.ugcIdleList?.Clear();
        }

        protected override void ResetOnlyPlayerData()
        {
            base.ResetOnlyPlayerData();
            npcIdleData.mainIdle = BaseIdleBehaviour.IdleDefaultName;
            npcIdleData.subIdle?.Clear();
        }

        private void ResetNpcWithPlayerData() {
            switch (secondTab)
            {
                case SecondTabs.Tab.Main:
                    npcIdleData.animResType = 0;
                    npcIdleData.mainIdle = doubleDefaultId;
                    npcIdleData.ugcIdleList = new List<UgcIdleData>();
                    break;
                case SecondTabs.Tab.Sub:
                    npcIdleData.animResType = 1;
                    var animInfo = ugcLobbyView.GetDefaultAnimInfo(UgcAnimSubType.Double);
                    npcIdleData.mainIdle = animInfo.id;
                    npcIdleData.ugcIdleList = new List<UgcIdleData>() { animInfo};
                    break;
            }

            npcIdleData.personType = 1;
            npcIdleData.subIdle?.Clear();

            playerIdleData.animResType = 0;
            playerIdleData.personType = 0;
            playerIdleData.mainIdle = BaseIdleBehaviour.IdleLeisureName;
            playerIdleData.subIdle?.Clear();
            playerIdleData.ugcIdleList?.Clear();
        }

        protected override void ResetDefaultIdleData() {
            base.ResetDefaultIdleData();
            if (!isHasNpc)
            {
                ResetOnlyPlayerData();
                return;
            }
            if (animType == HallAnimType.Interactive)
            {
                ResetNpcWithPlayerData();
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
                    SetNpcPgcDefault();
                }
                else if(secondTab == SecondTabs.Tab.Sub)
                {
                    SetNpcUgcDefault();
                }
            }
        }

        internal override void ResetDefaultIdleDataOnClick() {
            base.ResetDefaultIdleDataOnClick();
            if (!isHasNpc)
            {
                ResetIdleDataByButton(playerIdleData,BaseIdleBehaviour.IdleLeisureName,UgcAnimSubType.Single);
                return;
            }
            if (animType == HallAnimType.Interactive)
            {
                ResetIdleDataByButton(npcIdleData,doubleDefaultId,UgcAnimSubType.Double);
                return;
            }

            if (firstTab == MainTabs.Tab.Main)
            {
                ResetIdleDataByButton(playerIdleData,BaseIdleBehaviour.IdleLeisureName,UgcAnimSubType.Single);
            }
            else
            {
                ResetIdleDataByButton(npcIdleData,BaseIdleBehaviour.IdleDefaultName,UgcAnimSubType.Single);
            }
        }

        protected override void ResetIdleDataByButton(IdleData idleData,string pgcDefaultID, UgcAnimSubType ugcAnimType)
        {
            base.ResetIdleDataByButton(idleData,pgcDefaultID,ugcAnimType);
            playerIdleData.personType = 0;
            npcIdleData.mainIdle = BaseIdleBehaviour.IdleDefaultName;
            npcIdleData.subIdle?.Clear();
        }

        private void SetNpcPgcDefault()
        {
            npcIdleData.animResType = 0;
            npcIdleData.personType = 0;
            npcIdleData.mainIdle = BaseIdleBehaviour.IdleDefaultName;
            npcIdleData.subIdle?.Clear();
            npcIdleData.ugcIdleList?.Clear();
        }

        private void SetNpcUgcDefault()
        {
            npcIdleData.animResType = 1;
            npcIdleData.personType = 0;
            var animInfo = ugcLobbyView.GetDefaultAnimInfo(UgcAnimSubType.Double);
            npcIdleData.mainIdle = animInfo.id;
            npcIdleData.subIdle?.Clear();
            npcIdleData.ugcIdleList = new List<UgcIdleData> {animInfo};
        }



        protected override LobbyRoleType GetRoleType() {
            return firstTab == MainTabs.Tab.Main ? LobbyRoleType.Avatar : LobbyRoleType.Npc;
        }


        protected override void UpdateIdleView()
        {
            base.UpdateIdleView();
            otherCharacterWrap.Avatar.GetComponent<Animator>().enabled = secondTab == SecondTabs.Tab.Main;
            avatarIkController.ChangeAnimResType(secondTab == SecondTabs.Tab.Sub ? AnimResType.UGC : AnimResType.PGC);
            npcIkController.ChangeAnimResType(secondTab == SecondTabs.Tab.Sub? AnimResType.UGC : AnimResType.PGC);
            avatarIkController.RemovePropIks();
            npcIkController.RemovePropIks();
            SetNpcWithPlayerPosition();

        }

        private void SetNpcWithPlayerPosition() {
            var poseModeData = DataTables.GetPoseModeConfig((int)UgcPoseSubType.Single);
            characterWrap.Avatar.transform.localPosition = poseModeData.RoleDefPos[0];

            var parent1 = characterWrap.Avatar.transform.parent;
            parent1.localPosition =  poseModeData.EditPos[0];
            parent1.localEulerAngles = Vector3.zero;

            otherCharacterWrap.Avatar.transform.localPosition = poseModeData.RoleDefPos[0];
            var parent2 = otherCharacterWrap.Avatar.transform.parent;
            parent2.localPosition = poseModeData.EditPos[0];
            parent2.localEulerAngles = Vector3.zero;

            var npcPoseModeData = DataTables.GetPoseModeConfig((int)UgcPoseSubType.Single);
            npcWrap.Avatar.transform.localPosition = npcPoseModeData.RoleDefPos[0];

            var parent3 = npcWrap.Avatar.transform.parent;
            parent3.localPosition =  npcPoseModeData.EditPos[0];
            parent3.localEulerAngles = Vector3.zero;


            if (animType == HallAnimType.NotInteractive)
            {
                characterWrap.CustomAvatar.transform.localPosition = Vector3.zero;
                npcWrap.CustomAvatar.transform.localPosition = Vector3.zero;
                otherCharacterWrap.CustomAvatar.transform.localPosition = Vector3.zero;
            }
            else
            {
                if (secondTab == SecondTabs.Tab.Main)
                {
                    otherCharacterWrap.CustomAvatar.transform.localPosition = Vector3.zero;
                    npcWrap.CustomAvatar.transform.localPosition = Vector3.zero;
                }
                else
                {
                    Vector3 position = new Vector3(0.3f, 0, 0);
                    otherCharacterWrap.CustomAvatar.transform.localPosition = position;
                    npcWrap.CustomAvatar.transform.localPosition = position;
                }
            }
        }

        protected override void SetCurrentRole() {
            base.SetCurrentRole();
            if (npcWrap != null)
            {
                var poseModeData = DataTables.GetPoseModeConfig((int)UgcPoseSubType.Single);
                npcWrap.Avatar.transform.localPosition = poseModeData.RoleDefPos[0];
                npcWrap.Avatar.transform.parent.localPosition =  poseModeData.EditPos[0];
                npcWrap.Avatar.gameObject.SetActive(firstTab ==  MainTabs.Tab.Sub);
            }
        }



        private void ShowCharacterOnDouble()
        {
            var buddyInfo = AccountDataManager.Inst.AIBuddyInfo;

            bool isInteractive = IsDoubleInteractionAnim();
            animType = isInteractive ? HallAnimType.Interactive : HallAnimType.NotInteractive;
            firstTab = isInteractive ? MainTabs.Tab.Sub : MainTabs.Tab.Main;
            var resType = isInteractive ? buddyInfo.idleData?.animResType ?? 0 : playerIdleData.animResType;
            secondTab = resType == 0 ? SecondTabs.Tab.Main:SecondTabs.Tab.Sub;
            thirdTab = SecondTabs.Tab.Main;

            SelectAnimModeClickOnUI(isInteractive ? HallAnimType.Interactive : HallAnimType.NotInteractive);
            (pgcLobbyView as NPCPGCLobbyIdleView)?.OnShow(characterWrap,otherCharacterWrap,npcWrap);
            (ugcLobbyView as NpcUGCLobbyIdleView)?.OnShow(characterWrap,otherCharacterWrap,npcWrap);
        }


        //兼容PGC资源
        private bool IsDoubleInteractionAnim()
        {
            var buddyInfo = AccountDataManager.Inst.AIBuddyInfo;
            if (buddyInfo != null && buddyInfo.idleData != null)
            {
                if ((AnimResType)buddyInfo.idleData.animResType == AnimResType.PGC)
                {
                    var emoData = Es.DataTables.GetEmoUIConfig(buddyInfo.idleData.mainIdle);
                    if (emoData != null)
                    {
                        var emoAniType = (EmoteType) emoData.emoAniType;
                        if (emoAniType == EmoteType.DoubleOnce || emoAniType == EmoteType.DoubleLoop)
                        {
                            return true;
                        }
                    }
                }
                else
                {
                    return buddyInfo.idleData.personType == (int)HallAnimType.Interactive;
                }
            }
            return false;
        }


        protected override void SelectAnimModeOnOtherIkController(bool isInteractive)
        {
            if (isInteractive)
            {
                npcIkController.Insert(0,otherIkController);
            }
            else
            {
                npcIkController.RemoveAnimIK(otherIkController);
            }
        }





        /// <summary>
        /// UI表现
        /// </summary>
        /// <param name="aType"></param>
        protected override void SelectAnimModeClickOnUI(HallAnimType aType)
        {
            base.SelectAnimModeClickOnUI(aType);
            bool isInteractive = aType == HallAnimType.Interactive;
            string tabName = isInteractive? "双人动画":"伙伴动画";
            mainTabsUI.SetToggleLineVisible(!isInteractive);
            mainTabsUI.SetToggleName(MainTabs.Tab.Sub, tabName);
        }

        protected override bool SinglePersonModeClick() {
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
            ResetNpcWithPlayerData();
            otherCharacterWrap.Avatar.SetActive(true);
            mainTabsUI.DefualtOn(MainTabs.Tab.Sub);
            return true;
        }














    }
}
