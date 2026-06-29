using System;
using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.IO;
using Com.TheFallenGames.OSA.Util.IO.Pools;
using frame8.Logic.Misc.Other.Extensions;
using UnityEngine;

namespace UI.UIPanels.ProfilePanel
{
    public class ProfileCharacterBoxAdapter : GridAdapter<GridParams, ProfileCharacterBoxHolder>
    {
        public UnityEngine.Events.UnityEvent OnItemsUpdated;
        public Action<CharacterBoxInfo> dataAction;
        public LazyDataHelper<CharacterBoxInfo> Data { get; set; }

        private IPool texturePool;

        protected override void Start()
        {
            texturePool = new FIFOCachingPool(12, TextureDestroyer);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            texturePool?.Clear();
            Resources.UnloadUnusedAssets();
        }

        private void TextureDestroyer(object urlKey, object texture)
        {
            var obj = texture as UnityEngine.Object;
            if (obj != null) Destroy(obj);
        }

        public override void Refresh(bool contentPanelEndEdgeStationary = false, bool keepVelocity = false)
        {
            _CellsCount = Data.Count;
            OnItemsUpdated?.Invoke();
            base.Refresh(false, keepVelocity);
        }

        protected override void OnCellViewsHolderCreated(ProfileCharacterBoxHolder cellVH, CellGroupViewsHolder<ProfileCharacterBoxHolder> cellGroup)
        {
            base.OnCellViewsHolderCreated(cellVH, cellGroup);
            cellVH.iconRemoteImageBehaviour.InitializeWithPool(texturePool);
        }

        protected override void UpdateCellViewsHolder(ProfileCharacterBoxHolder newOrRecycled)
        {
            var model = Data.GetOrCreate(newOrRecycled.ItemIndex);
            newOrRecycled.UpdateViews(model, OnSelect);
        }

        private void OnSelect(CharacterBoxInfo info)
        {
            dataAction?.Invoke(info);
        }
    }

    public class ProfileCharacterBoxHolder : CellViewsHolder
    {
        public RemoteImageBehaviour iconRemoteImageBehaviour;
        public ProfileCharacterBoxItem ItemNode;

        public override void CollectViews()
        {
            base.CollectViews();
            iconRemoteImageBehaviour = GameObjectEx.FindChildByName(views, "Cover").GetComponent<RemoteImageBehaviour>();
            ItemNode = root.GetComponentInParent<ProfileCharacterBoxItem>();
        }

        public void UpdateViews(CharacterBoxInfo info, Action<CharacterBoxInfo> onSelect)
        {
            ItemNode.SetData(info, onSelect);
        }
    }
}
