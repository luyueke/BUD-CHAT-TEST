using System.Collections.Generic;
using Com.TheFallenGames.OSA.DataHelpers;
using Es;
using Game.Avatar;
using Game.Pet;
using Game.Store;
using GameData.PgcData;
using UI.UIPanels.FittingRoom;
using UnityEngine;

namespace UI.UIPanels.LobbyCharacterIdlePanel
{
    public class PetPGCLobbyIdleView : BasePGCLobbyIdleView
    {


        private IdleData petIdleData;

        private PetPlayerIdleBehaviour petIdleBehaviour;
        private PetWrap petWrap;

        private AvatarBagSceneHandler petDataHandler;

        public void OnShow(CharacterWrap selfWrap,CharacterWrap otherWrap,PetWrap pWrap)
        {

            base.OnShow(selfWrap, otherWrap);
            petWrap = pWrap;
            petIdleBehaviour = petWrap.Avatar.GetComponent<PetPlayerIdleBehaviour>();
            petIdleData = petIdleBehaviour.GetData();
            if (petIdleBehaviour != null)
            {
                petDataHandler = AssetsDataManager.GetData<AvatarBagSceneHandler>();
                petDataHandler.AddDataChange(gameObject, OnDataChange);
                SetEmoteView(petIdleData.mainIdle);
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
                var petData = DataTables.GetPoseModeConfig((int)UgcPoseSubType.PetSingle);
                petWrap.Avatar.transform.parent.localPosition = petData.EditPos[0];

                var chaData = DataTables.GetPoseModeConfig((int)UgcPoseSubType.Single);
                characterWrap.Avatar.transform.parent.localPosition = chaData.EditPos[0];
                SetEmoteView(petIdleData.mainIdle);
                curIdleBehaviour.SetData(curIdleData);
            }
        }




        public override void OnResetClick()
        {
            petIdleBehaviour.SetData(petIdleData);
            base.OnResetClick();
        }


        public override void OnThirdTabs(HallAnimType animType, LobbyRoleType first, SecondTabs.Tab tab)
        {
            base.OnThirdTabs(animType, first, tab);
            curIdleData = first == LobbyRoleType.Avatar ? avatarIdleData : petIdleData;
            curIdleBehaviour = first == LobbyRoleType.Avatar ? avatarIdleBehaviour : petIdleBehaviour;
            currentSceneHandler = first == LobbyRoleType.Avatar ? dataHandler : petDataHandler;
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
                    if (curAnimType == HallAnimType.NotInteractive)
                    {
                        subType = curRoleType == LobbyRoleType.Avatar
                            ? EmoteSubType.SingleLoop
                            : EmoteSubType.PetSingleLoop;
                    }
                    else
                    {
                        subType = EmoteSubType.PetWithPlayerLoop;
                    }
                    return UniqueType.Get(ResourceType.Emote, (int) subType);
                case SecondTabs.Tab.Sub:
                    if (curAnimType == HallAnimType.NotInteractive)
                    {
                        subType = curRoleType == LobbyRoleType.Avatar ? EmoteSubType.Single : EmoteSubType.PetSingle;
                    }
                    else
                    {
                        subType = EmoteSubType.PetWithPlayer;
                    }
                    return UniqueType.Get(ResourceType.Emote, (int) subType);
            }
            return 0;
        }
    }
}
