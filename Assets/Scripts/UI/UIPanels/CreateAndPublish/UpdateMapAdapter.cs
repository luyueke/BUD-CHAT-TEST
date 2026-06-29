using System;
using UnityEngine;
using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.IO;
using Com.TheFallenGames.OSA.Util.IO.Pools;
using frame8.Logic.Misc.Other.Extensions;

namespace BUD.GameStudio
{
    public class UpdateMapAdapter: GridAdapter<GridParams, UpdateMapHolder>
    {
		public UnityEngine.Events.UnityEvent OnItemsUpdated;
		public Action<DraftListItem> dataAction;
		public LazyDataHelper<DraftListItem> Data { get; set; }
		private IPool texturePool;
		private int currentSelect = -1;
		protected override void Start()
		{
			texturePool = new FIFOCachingPool(12, TextureDestoryer);

		}

		/// <summary>
		/// 销毁必须清空池对象
		/// </summary>
		protected override void OnDestroy()
		{
			base.OnDestroy();
			texturePool.Clear();
			Resources.UnloadUnusedAssets();
		}

		private void TextureDestoryer(object urlKey, object texture)
		{
			var asUnityObject = texture as UnityEngine.Object;
			if (asUnityObject != null)
				Destroy(asUnityObject);
		}
		
		public override void Refresh(bool contentPanelEndEdgeStationary = false /*ignored*/, bool keepVelocity = false)
		{
			_CellsCount = Data.Count;
			OnItemsUpdated?.Invoke();
			base.Refresh(false, keepVelocity);
		}
		
		protected override void OnCellViewsHolderCreated(UpdateMapHolder cellVH, CellGroupViewsHolder<UpdateMapHolder> cellGroup)
		{
			base.OnCellViewsHolderCreated(cellVH, cellGroup);
			cellVH.iconRemoteImageBehaviour.InitializeWithPool(texturePool);
		}

		protected override void UpdateCellViewsHolder(UpdateMapHolder newOrRecycled)
		{
			var model = Data.GetOrCreate(newOrRecycled.ItemIndex);
			newOrRecycled.UpdateViews(model,OnSelect);
			newOrRecycled.UpdateSelect(newOrRecycled.ItemIndex == currentSelect);
			if (model.mapInfo != null)
			{
				string cover = model.mapInfo.cover;
				if (!string.IsNullOrEmpty(cover))
				{
					newOrRecycled.iconRemoteImageBehaviour.gameObject.SetActive(false);
					newOrRecycled.iconRemoteImageBehaviour.Load(cover, true, (fromCache,  success) =>
					{
						if (success)
						{
							newOrRecycled.iconRemoteImageBehaviour.gameObject.SetActive(true);
						}
					});
				}
			}
		}
		
		public void OnSelect(DraftListItem info)
		{
			var ovh = GetCellViewsHolderIfVisible(currentSelect);
			if (ovh != null)
			{
				ovh.UpdateSelect(false);
			}
			int index = 0;
			for (var i = 0; i < Data.Count; i++)
			{
				if (Data.GetOrCreate(i) == info)
				{
					index = i;
					break;
				}
			}
			var nvh = GetCellViewsHolderIfVisible(index);
			{
				if (nvh != null)
				{
					currentSelect = index;
					nvh.UpdateSelect(true);
					dataAction?.Invoke(info);
				}
			}
		}
		
		

    }
    public class UpdateMapHolder : CellViewsHolder
    {
        public RemoteImageBehaviour iconRemoteImageBehaviour;
        public UpdateListItem updateDraftItem;
        public override void CollectViews()
        {
            base.CollectViews();
            views.GetComponentAtPath("MapCover", out iconRemoteImageBehaviour);
            updateDraftItem = root.GetComponentInParent<UpdateListItem>();
            updateDraftItem.InitUI();
        }


        public void UpdateViews(DraftListItem info,Action<DraftListItem> onSelect)
        {
	        updateDraftItem.SetData(info, onSelect);
        }

        public void UpdateSelect(bool isSelect)
        {
            if (updateDraftItem != null)
            {
	            updateDraftItem.UpdateSelect(isSelect);
            }
        }
        
    }
}