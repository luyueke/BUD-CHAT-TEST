using System.Collections.Generic;
using Com.TheFallenGames.OSA.DataHelpers;
using Game.Avatar;
using Game.Store;
using UI.UIPanels.FittingRoom;
using UnityEngine;

namespace UI.UIPanels.LobbyCharacterIdlePanel {
    public class BasePGCLobbyIdleView : BaseLobbyIdleView {
        [Header("列表")] [SerializeField] internal FittingRoomAdapter assetsList;
        [Header("已选表情")] [SerializeField] internal GameObject actionsRoot;
        [Header("已选表情")] [SerializeField] internal List<ActionItem> actions;
        [SerializeField] internal AvatarCameraController avatarCameraController;
        protected AvatarBagSceneHandler dataHandler;
        protected GoodsDataClassifyList assetsDatas = new();
        protected AvatarBagSceneHandler currentSceneHandler;

        protected IdleData curIdleData;
        protected IdleData avatarIdleData;


        protected BaseIdleBehaviour curIdleBehaviour;
        protected PlayerIdleBehaviour avatarIdleBehaviour;
        protected PlayerAnimationCtrl otherAnimationCtrl;
        protected CharacterWrap otherCharacterWrap;
        protected CharacterWrap characterWrap;
        protected string previewId;
        protected SecondTabs.Tab curThirdTab;//Main:主画面 Sub:辅助画面
        protected LobbyRoleType curRoleType = LobbyRoleType.Avatar;

        protected virtual string doubleDefaultId => "40800361";
        protected HallAnimType curAnimType = HallAnimType.NotInteractive;


        public virtual void OnCreate() {
            dataHandler = AssetsDataManager.GetData<AvatarBagSceneHandler>();
            dataHandler.AddDataChange(gameObject, OnDataChange);
        }

        public void OnShow(CharacterWrap selfWrap,CharacterWrap otherWrap) {
            characterWrap = selfWrap;
            otherCharacterWrap = otherWrap;
            avatarIdleBehaviour = characterWrap.Avatar.GetComponent<PlayerIdleBehaviour>();
            avatarIdleData = avatarIdleBehaviour.GetData();

            assetsList.Data = new LazyDataHelper<GoodsData>(assetsList, CreateNewModel);
            assetsList.Init();
            assetsList.ResetColor();
            ColorUtility.TryParseHtmlString("#AEA6CC", out assetsList.BgColor);
            assetsList.OnItemSelected = OnItemSelected;
            otherAnimationCtrl = otherCharacterWrap.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
        }

        private GoodsData CreateNewModel(int index)
        {
            var assetsData = assetsDatas.Get(index);
            assetsData.Selected = EmoteIsSelected(assetsData.Id);
            return assetsData;
        }

        protected virtual bool EmoteIsSelected(string pgcId)
        {
            return curIdleData != null && (curIdleData.mainIdle == pgcId || previewId == pgcId);
        }


        internal void OnItemSelected(GoodsData data)
        {
            switch (curThirdTab)
            {
                case SecondTabs.Tab.Main:
                    OnSelectedMainIdle(data.Id);
                    break;
                case SecondTabs.Tab.Sub:
                    OnSelectedSubIdle(data.Id);
                    break;
            }
            assetsList.Data.ResetItems(assetsDatas.Count());
        }

        public void ForceChangeDefaultAnim(LobbyRoleType curRoleType)
        {
            avatarCameraController.ResetEmoteView();
            if (curIdleBehaviour != null)
            {
                curIdleBehaviour.SetData(curIdleData);
            }
        }

        private void OnSelectedMainIdle(string id)
        {
            if (curIdleData.mainIdle == id) return;
            curIdleData.mainIdle = id;
            SetEmoteView(id);
            curIdleBehaviour.SetData(curIdleData);
        }

        public virtual void OnResetClick()
        {
            avatarIdleBehaviour.SetData(avatarIdleData);
            otherAnimationCtrl.ResetEmoteForUICharacter();
            RefreshSelected();
            SetEmoteView(curIdleData.mainIdle);
            assetsList.Data.ResetItems(assetsDatas.Count());
        }

        public virtual void OnThirdTabs(HallAnimType animType, LobbyRoleType first, SecondTabs.Tab tab)
        {
            curAnimType = animType;
            curRoleType = first;
            curThirdTab = tab;

        }


        private void OnSelectedSubIdle(string id)
        {
            if (curIdleData.subIdle.Contains(id))
            {
                PreviewEmote(id);
                return;
            }
            if (curIdleData.subIdle.Count < 5)
            {
                curIdleData.subIdle.Add(id);
                curIdleBehaviour.SetData(curIdleData);
            }
            RefreshSelected();
            // 预览动画
            PreviewEmote(id);
        }

        protected void SetEmoteView(string id)
        {
            var uiConfig = Es.DataTables.GetEmoUIConfig(id);
            if (uiConfig != null)
            {
                avatarCameraController.SetEmoteView(id);
            }
        }

        private void PreviewEmote(string emoteId)
        {
            previewId = emoteId;
            SetEmoteView(emoteId);
            curIdleBehaviour.PreviewSub(emoteId);
        }

        protected void RefreshSelected()
        {
            for (int i = 0, C = actions.Count; i < C; i++)
            {
                actions[i].UpdateViews(curIdleData.subIdle.Count > i ? curIdleData.subIdle[i] : null, OnCancelAction);
            }
        }

        private void OnCancelAction(string actionId)
        {
            if (string.IsNullOrEmpty(actionId)) return;

            if (curIdleData.subIdle.Contains(actionId))
            {
                curIdleData.subIdle.Remove(actionId);
                RefreshSelected();
                curIdleBehaviour.SetData(curIdleData);
            }
        }

        protected void UpdateAssetList()
        {
            actionsRoot.SetActive(curThirdTab == SecondTabs.Tab.Sub);
            assetsDatas.SetData(currentSceneHandler.GetGoodsData(GetSelectedClassType()), null);
            assetsList.Data.ResetItems(assetsDatas.Count());
            RefreshSelected();
        }


        internal virtual void OnDataChange(AssetsData[] changes) {
            assetsDatas.SetData(currentSceneHandler.GetGoodsData(GetSelectedClassType()), null);
            assetsList.Data.ResetItems(assetsDatas.Count());
        }

        protected virtual int GetSelectedClassType() {
            return 0;
        }
    }
}
