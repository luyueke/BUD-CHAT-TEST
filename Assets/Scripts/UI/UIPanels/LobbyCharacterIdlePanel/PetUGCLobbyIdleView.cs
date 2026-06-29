using System;
using System.Collections.Generic;
using System.Linq;
using BUD.AnimPose;
using Com.TheFallenGames.OSA.DataHelpers;
using Es;
using Game.Avatar;
using Game.Pet;
using Game.Store;
using GameData.BaseInfo;
using GameData.PgcData;
using Newtonsoft.Json;
using UI.UIPanels.FittingRoom;
using UnityEngine;

namespace UI.UIPanels.LobbyCharacterIdlePanel
{
    public class PetUGCLobbyIdleView : BaseUGCLobbyIdleView
    {


        private IdleData petIdleData;
        private UgcIdleBehaviour petBehaviour;
        private AvatarBagSceneHandler petDataHandler;

        private Dictionary<UgcAnimSubType,AnimInfo> animInfoDic = new Dictionary<UgcAnimSubType, AnimInfo>();
        private Dictionary<UgcAnimSubType, UgcIdleData> defaultIdleDatas = new Dictionary<UgcAnimSubType, UgcIdleData>();
        private string petContent;
        private string playerWithpetContent;




        public override void OnCreate()
        {
            base.OnCreate();
            LoadDefaultAnimInfos();
        }

        private void LoadDefaultAnimInfos()
        {
            animInfoDic.Clear();
            defaultIdleDatas.Clear();
            UgcAnimSubType[] tempList = {UgcAnimSubType.Single, UgcAnimSubType.PetSingle, UgcAnimSubType.PetWithPlayer};
            for (var i = 0; i < tempList.Length; i++)
            {
                var wrapper = Loader.Load<TextAsset>($"Assets/Arts/Config/CustomAnimConfig/AnimInfo_{(int)tempList[i]}.json");
                if (wrapper == null)
                {
                    LoggerUtils.LogError("localOfficialConfig  read Error");
                    return;
                }
                var content = wrapper.RetainAsset(this.gameObject).text;
                var animInfo = JsonConvert.DeserializeObject<AnimInfo>(content);
                var defaultIdleData = new UgcIdleData()
                {
                    id = animInfo.id,
                    metaDataUrl = animInfo.metaDataUrl,
                    cover = animInfo.cover,
                    propList = animInfo.propList
                };
                defaultIdleDatas.Add(tempList[i], defaultIdleData);
                animInfoDic.Add(tempList[i], animInfo);
            }
        }


        // return defaultIdleDatas[subType].Clone();

        public override UgcIdleData GetDefaultAnimInfo(UgcAnimSubType subType)
        {
            return defaultIdleDatas[subType].Clone();
        }


        public void OnShow(CharacterWrap character,CharacterWrap other,PetWrap pet)
        {
            base.OnShow(character, other);

            petBehaviour =pet.Avatar.AddComponent<UgcIdleBehaviour>();
            var petIkController = pet.Avatar.GetComponent<AnimIKController>();
            petBehaviour.Init(petIkController);

            var petIdleBehaviour = pet.Avatar.GetComponent<PetPlayerIdleBehaviour>();
            petIdleData = petIdleBehaviour.GetData();

            if (petIdleBehaviour != null)
            {
                petDataHandler = AssetsDataManager.GetData<AvatarBagSceneHandler>();
                petDataHandler.AddDataChange(gameObject, OnDataChange);
            }
        }



        public override void OnThirdTabs(HallAnimType hallAnimType, LobbyRoleType first, SecondTabs.Tab tab)
        {
            base.OnThirdTabs(hallAnimType, first, tab);
            curIdleData = first == LobbyRoleType.Avatar ? avatarIdleData : petIdleData;
            currentSceneHandler = first == LobbyRoleType.Avatar ? dataHandler : petDataHandler;
            curIdleBehaviour = first == LobbyRoleType.Avatar ? playerBehaviour : petBehaviour;
            var ugcAnimType = UgcAnimSubType.ErrAnimSubType;

            UpdateAssetList();
            ResetUgcPlayerPosition();
            curIdleBehaviour.SetData(curAnimSubType,curIdleData);
            curIdleBehaviour.PlayMain();
        }

        private void ResetUgcPlayerPosition()
        {
            var poseModeData = DataTables.GetPoseModeConfig((int)UgcPoseSubType.Single);
            playerBehaviour.transform.localPosition = poseModeData.RoleDefPos[0];
            otherWrap.Avatar.transform.localPosition = poseModeData.RoleDefPos[0];
            otherWrap.Avatar.gameObject.SetActive(curAnimType == HallAnimType.Interactive);
            var petData = DataTables.GetPoseModeConfig((int)UgcPoseSubType.PetSingle);
            petBehaviour.transform.localPosition = petData.RoleDefPos[0];

        }



        private void UpdateAssetList()
        {
            actionsRoot.SetActive(curThirdTab == SecondTabs.Tab.Sub);
            var list = currentSceneHandler.GetGoodsData(GetSelectedClassType());

            if (curAnimType == HallAnimType.Interactive)
            {
                curAnimSubType = UgcAnimSubType.PetWithPlayer;
            }
            else
            {
                curAnimSubType = curRoleType == LobbyRoleType.Avatar
                    ? UgcAnimSubType.Single
                    : UgcAnimSubType.PetSingle;
            }

            var defaultItem = list.Find(x => x.ButtonType == ButtonType.UgcEmoteIdle);
            if (defaultItem != null)
            {
                list.Remove(defaultItem);
                list.Add(defaultItem);
            }
            else
            {

                var animInfo = animInfoDic[curAnimSubType];
                RecommendItemData itemData = new RecommendItemData()
                {
                    UgcInfo =animInfo
                };
                UgcAnimAssetsData assetData = new UgcAnimAssetsData()
                {
                    Id = animInfo.id,
                    ResourceType = ResourceType.UgcEmote,
                    UgcInfo = itemData
                };
                var assetsDatas = new List<AssetsData>(){assetData};
                var goodData = new GoodsData()
                {
                    Id = animInfo.id,
                    ButtonType = ButtonType.UgcEmoteIdle,
                    GoodsType = GoodsType.SingleUgc,
                    IsBagScene = true,
                    IsOwned = true,
                    Assets = assetsDatas
                };
                list.Add(goodData);
            }

            assetsDatas.SetData(list, LoopPredicate);
            assetsList.Data.ResetItems(assetsDatas.Count());
            RefreshSelected();
        }



        private bool LoopPredicate(GoodsData goodsData)
        {
            if (goodsData.ButtonType == ButtonType.UgcEmoteIdle)
            {
                return curThirdTab == SecondTabs.Tab.Main;
            }
            if (goodsData.ButtonType == ButtonType.Design) return false;
            if (goodsData.GoodsType != GoodsType.SinglePgc && goodsData.GoodsType != GoodsType.SingleUgc) return false;
            if (goodsData.Assets == null || goodsData.Assets.Count != 1) return false;
            if (!goodsData.IsOwned) return false;
            var asset = goodsData.Assets[0];
            if (!(asset is UgcAnimAssetsData)) return false;
            if (curThirdTab == SecondTabs.Tab.Main && asset.InventoryData.Loop == 1) return true;
            if (curThirdTab == SecondTabs.Tab.Sub && asset.InventoryData.Loop == 0) return true;
            return false;
        }

        protected override int GetSelectedClassType()
        {
            UgcAnimSubType ugcSubType;
            if (curAnimType == HallAnimType.NotInteractive)
            {
                ugcSubType = curRoleType == LobbyRoleType.Avatar ? UgcAnimSubType.Single : UgcAnimSubType.PetSingle;
            }
            else
            {
                ugcSubType = UgcAnimSubType.PetWithPlayer;
            }
            return UniqueType.Get(ResourceType.UgcEmote, (int) ugcSubType);
        }

    }
}
