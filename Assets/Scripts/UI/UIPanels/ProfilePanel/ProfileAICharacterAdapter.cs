using System;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.IO;
using Com.TheFallenGames.OSA.Util.IO.Pools;
using frame8.Logic.Misc.Other.Extensions;
using UnityEngine;

namespace UI.UIPanels.ProfilePanel
{
    public class ProfileAICharacterAdapter : GridAdapter<GridParams, ProfileAICharacterHolder>
    {
        public UnityEngine.Events.UnityEvent OnItemsUpdated;
        public Action<CabinCharacterUgcInfo> dataAction;
        public LazyDataHelper<CabinCharacterUgcInfo> Data { get; set; }

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

        protected override void OnCellViewsHolderCreated(ProfileAICharacterHolder cellVH, CellGroupViewsHolder<ProfileAICharacterHolder> cellGroup)
        {
            base.OnCellViewsHolderCreated(cellVH, cellGroup);
            cellVH.iconRemoteImageBehaviour.InitializeWithPool(texturePool);
        }

        protected override void UpdateCellViewsHolder(ProfileAICharacterHolder newOrRecycled)
        {
            var model = Data.GetOrCreate(newOrRecycled.ItemIndex);
            newOrRecycled.UpdateViews(model, OnSelect);
        }

        private void OnSelect(CabinCharacterUgcInfo info)
        {
            dataAction?.Invoke(info);
        }
    }

    public class ProfileAICharacterHolder : CellViewsHolder
    {
        public RemoteImageBehaviour iconRemoteImageBehaviour;
        public ProfileAICharacterItem ItemNode;

        public override void CollectViews()
        {
            base.CollectViews();
            iconRemoteImageBehaviour = GameObjectEx.FindChildByName(views, "Cover").GetComponent<RemoteImageBehaviour>();
            ItemNode = root.GetComponentInParent<ProfileAICharacterItem>();
        }

        public void UpdateViews(CabinCharacterUgcInfo info, Action<CabinCharacterUgcInfo> onSelect)
        {
            ItemNode.SetData(info, onSelect);
        }
    }
}
