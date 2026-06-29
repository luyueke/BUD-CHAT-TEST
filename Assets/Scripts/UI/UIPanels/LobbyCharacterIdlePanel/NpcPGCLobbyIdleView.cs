using System.Collections.Generic;
using Com.TheFallenGames.OSA.DataHelpers;
using Es;
using Game.Avatar;
using Game.Pet;
using Game.Store;
using GameData.PgcData;
using UI.UIPanels.FittingRoom;
using UnityEngine;

namespace UI.UIPanels.LobbyCharacterIdlePanel {
    public class NPCPGCLobbyIdleView: BasePGCLobbyIdleView {
        private IdleData npcIdleData;

        private NpcPlayerIdleBehaviour npcIdleBehaviour;
        private CharacterWrap npcWrap;

        private AvatarBagSceneHandler npcDataHandler;

        protected override string doubleDefaultId {
            get => "40400452";
        }

        public void OnShow(CharacterWrap selfWrap,CharacterWrap otherWrap,CharacterWrap nWrap)
        {

            base.OnShow(selfWrap, otherWrap);
            npcWrap = nWrap;
            npcIdleBehaviour = npcWrap.Avatar.GetComponent<NpcPlayerIdleBehaviour>();
            npcIdleData = npcIdleBehaviour.GetData();
            if (npcIdleBehaviour != null)
            {
                npcDataHandler = AssetsDataManager.GetData<AvatarBagSceneHandler>();
                npcDataHandler.AddDataChange(gameObject, OnDataChange);
                SetEmoteView(npcIdleData.mainIdle);
            }
        }

        public void SetAnimMode(HallAnimType animMode)
        {
            if (animMode == HallAnimType.NotInteractive)
            {
                otherCharacterWrap.Avatar.SetActive(false);
                avatarCameraController.ResetEmoteView();
                avatarCameraController.SetCameraZoom(ViewType.ZoomEmote);
            }
            else
            {
                var chaData = DataTables.GetPoseModeConfig((int)UgcPoseSubType.Single);
                characterWrap.Avatar.transform.parent.localPosition = new Vector3(-0.3f, chaData.EditPos[0].y, chaData.EditPos[0].z);
                npcWrap.Avatar.transform.parent.localPosition = new Vector3(0.3f, chaData.EditPos[0].y, chaData.EditPos[0].z);
                SetEmoteView(npcIdleData.mainIdle);
                curIdleBehaviour.SetData(curIdleData);
            }
        }


        protected override bool EmoteIsSelected(string pgcId) {
            if (curIdleData != null && curIdleData.mainIdle == "default") {
                if (pgcId == doubleDefaultId || curIdleData.mainIdle == pgcId) {
                    return true;
                }
            }

            return curIdleData != null && (curIdleData.mainIdle == pgcId || previewId == pgcId);
        }

        public override void OnResetClick()
        {
            npcIdleBehaviour.SetData(npcIdleData);
            base.OnResetClick();
        }


        public override void OnThirdTabs(HallAnimType animType, LobbyRoleType first, SecondTabs.Tab tab)
        {
            base.OnThirdTabs(animType, first, tab);
            curIdleData = first == LobbyRoleType.Avatar ? avatarIdleData : npcIdleData;
            curIdleBehaviour = first == LobbyRoleType.Avatar ? avatarIdleBehaviour : npcIdleBehaviour;
            currentSceneHandler = first == LobbyRoleType.Avatar ? dataHandler : npcDataHandler;
            UpdateAssetList();
            SetEmoteView(curIdleData.mainIdle);
            SetAnimMode(animType);
        }





        protected override int GetSelectedClassType()
        {
            EmoteSubType subType;
            switch (curThirdTab)
            {
                case SecondTabs.Tab.Main:
                    if (curAnimType == HallAnimType.NotInteractive) {
                        subType = EmoteSubType.SingleLoop;
                    }
                    else
                    {
                        subType = EmoteSubType.DoubleLoop;
                    }
                    return UniqueType.Get(ResourceType.Emote, (int) subType);
                case SecondTabs.Tab.Sub:
                    if (curAnimType == HallAnimType.NotInteractive)
                    {
                        subType = EmoteSubType.Single;
                    }
                    else
                    {
                        subType = EmoteSubType.Double;
                    }
                    return UniqueType.Get(ResourceType.Emote, (int) subType);
            }
            return 0;
        }
    }
}
