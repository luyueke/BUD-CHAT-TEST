using System.Collections.Generic;
using BUD.AnimPose;
using Com.TheFallenGames.OSA.DataHelpers;
using Game.Avatar;
using Game.Store;
using GameData.PgcData;
using UI.UIPanels.FittingRoom;
using UnityEngine;

namespace UI.UIPanels.LobbyCharacterIdlePanel {
    public class BaseUGCLobbyIdleView : BaseLobbyIdleView{
        [Header("列表")]
        [SerializeField] internal FittingRoomAdapter assetsList;
        [Header("已选表情")]
        [SerializeField] internal GameObject actionsRoot;
        [Header("已选表情")]
        [SerializeField] internal List<ActionItem> actions;



        protected GoodsDataClassifyList assetsDatas = new();

        protected IdleData curIdleData;
        protected IdleData avatarIdleData;

        protected UgcIdleBehaviour playerBehaviour;
        protected UgcIdleBehaviour curIdleBehaviour;
        protected CharacterWrap otherWrap;

        protected string previewId;
        protected SecondTabs.Tab curThirdTab;//Main:主画面 Sub:辅助画面

        protected AvatarBagSceneHandler currentSceneHandler;
        protected AvatarBagSceneHandler dataHandler;

        protected LobbyRoleType curRoleType = LobbyRoleType.Avatar;

        protected UgcAnimSubType curAnimSubType;
        protected HallAnimType curAnimType;


        public virtual void OnCreate() {
            dataHandler = AssetsDataManager.GetData<AvatarBagSceneHandler>();
            dataHandler.AddDataChange(gameObject, OnDataChange);
        }

        public void OnShow(CharacterWrap character,CharacterWrap other)
        {
            otherWrap = other;
            playerBehaviour = character.Avatar.AddComponent<UgcIdleBehaviour>();
            var playerIkController = character.Avatar.GetComponent<AnimIKController>();
            playerBehaviour.Init(playerIkController);

            var avatarIdleBehaviour = character.Avatar.GetComponent<PlayerIdleBehaviour>();
            avatarIdleData = avatarIdleBehaviour.GetData();

            assetsList.Data = new LazyDataHelper<GoodsData>(assetsList, CreateNewModel);
            assetsList.Init();
            assetsList.ResetColor();
            ColorUtility.TryParseHtmlString("#AEA6CC", out assetsList.BgColor);
            assetsList.OnItemSelected = OnItemSelected;
        }


        public void OnResetClick()
        {
            curIdleBehaviour.SetData(curAnimSubType,curIdleData);
            curIdleBehaviour.PlayMain();
            RefreshSelected();
            assetsList.Data.ResetItems(assetsDatas.Count());
        }

        public virtual void OnThirdTabs(HallAnimType hallAnimType, LobbyRoleType first, SecondTabs.Tab tab) {
            curAnimType = hallAnimType;
            curRoleType = first;
            curThirdTab = tab;
        }

        public virtual UgcIdleData GetDefaultAnimInfo(UgcAnimSubType subType)
        {
            return null;
        }



        protected void RefreshSelected()
        {
            for (int i = 0, C = actions.Count; i < C; i++)
            {
                if (curIdleData.subIdle.Count > i)
                {
                    var idleData = curIdleData.ugcIdleList.Find(x => x.id.Equals(curIdleData.subIdle[i]));
                    if (idleData != null)
                    {
                        actions[i].UpdateViews(idleData.id , idleData.cover, OnCancelAction);
                    }
                }
                else
                {
                    actions[i].UpdateViews( null,null, OnCancelAction);
                }
            }
        }


        private GoodsData CreateNewModel(int index)
        {
            var assetsData = assetsDatas.Get(index);
            assetsData.Selected = EmoteIsSelected(assetsData.Id);
            return assetsData;
        }

        internal void OnDataChange(AssetsData[] changes)
        {
            assetsDatas.SetData(currentSceneHandler.GetGoodsData(GetSelectedClassType()), null);
            assetsList.Data.ResetItems(assetsDatas.Count());
        }

        private bool EmoteIsSelected(string pgcId)
        {
            return curIdleData != null && (curIdleData.mainIdle == pgcId || previewId == pgcId);
        }


        private void OnItemSelected(GoodsData data)
        {
            switch (curThirdTab)
            {
                case SecondTabs.Tab.Main:
                    OnSelectedMainIdle(data);
                    break;
                case SecondTabs.Tab.Sub:
                    OnSelectedSubIdle(data);
                    break;
            }
            assetsList.Data.ResetItems(assetsDatas.Count());
        }

        private void OnSelectedMainIdle(GoodsData data)
        {
            if (curIdleData.mainIdle == data.Id) return;
            ReplaceIdleData(curIdleData.mainIdle,data);
            curIdleData.mainIdle = data.Id;
            curIdleBehaviour.SetData(curAnimSubType,curIdleData);
            curIdleBehaviour.PlayMainAnimByUI(data.Id);
        }

        private void ReplaceIdleData(string oldId,GoodsData data)
        {
            if (curIdleData.ugcIdleList == null)
            {
                curIdleData.ugcIdleList = new List<UgcIdleData>();
            }
            var idleData = curIdleData.ugcIdleList.Find(x => x.id.Equals(oldId));
            if (idleData == null)
            {
                AddNewIdleData(data);
            }
            else
            {
                UpdateIdleData(idleData,data);
            }
        }

        private void AddNewIdleData(GoodsData data)
        {
            var idleData = new UgcIdleData();
            UpdateIdleData(idleData,data);
            curIdleData.ugcIdleList.Add(idleData);
        }

        private void UpdateIdleData(UgcIdleData idleData,GoodsData data)
        {
            idleData.id = data.Id;
            idleData.metaDataUrl = data.Assets[0].UgcInfo.UgcInfo.metaDataUrl;
            idleData.cover = data.Assets[0].UgcInfo.UgcInfo.cover;
            idleData.propList =  data.Assets[0].UgcInfo.animInfo.propList;
        }

        private void OnSelectedSubIdle(GoodsData data)
        {
            if (curIdleData.subIdle.Contains(data.Id))
            {
                PreviewSubEmote(data.Id);
                return;
            }
            if (curIdleData.subIdle.Count < 5)
            {
                if (curIdleData.subIdle == null)
                {
                    curIdleData.subIdle = new List<string>();
                }
                if (curIdleData.ugcIdleList == null)
                {
                    curIdleData.ugcIdleList = new List<UgcIdleData>();
                }
                curIdleData.subIdle.Add(data.Id);
                curIdleBehaviour.SetData(curAnimSubType,curIdleData);
                string coverUrl = data.Assets[0].UgcInfo.UgcInfo.cover;
                AddNewIdleData(data);
                OnAddSubAnim(curIdleData.subIdle.Count - 1,data.Id,coverUrl);
            }
            // 预览动画
            PreviewSubEmote(data.Id);
        }

        private void OnAddSubAnim(int index,string id,string coverUrl)
        {
            actions[index].UpdateViews(id,coverUrl,OnCancelAction);
        }

        private void PreviewSubEmote(string emoteId)
        {
            previewId = emoteId;
            curIdleBehaviour.PlaySubAnimByUI(emoteId);
        }

        protected void OnCancelAction(string actionId)
        {
            if (string.IsNullOrEmpty(actionId)) return;

            if (curIdleData.subIdle.Contains(actionId))
            {
                curIdleData.subIdle.Remove(actionId);
                OnRemoveSubAnim(actionId);
                curIdleBehaviour.SetData(curAnimSubType,curIdleData);
            }
        }

        private void OnRemoveSubAnim(string removeId)
        {
            var index = actions.FindIndex(x => x.mData.Equals(removeId));
            if (index >= 0)
            {
                for (int i = index, C = actions.Count - 1; i < C; i++)
                {
                    var nextAction = actions[i + 1];
                    actions[i].UpdateViews(nextAction.mData,nextAction.CoverUrl,OnCancelAction);
                }
            }
        }



        protected virtual int GetSelectedClassType() {
            return 0;
        }




    }
}
