using System;
using UnityEngine;
using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.IO;
using Com.TheFallenGames.OSA.Util.IO.Pools;
using frame8.Logic.Misc.Other.Extensions;
using GameData.BaseInfo;

namespace BUD.GameStudio
{
    public class UpdateDraftGameAdapter: GridAdapter<GridParams, UpdateDraftItemHolder>
    {
		public UnityEngine.Events.UnityEvent OnItemsUpdated;
		public Action<MapInfo> dataAction;
		public LazyDataHelper<MapInfo> Data { get; set; }
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
		
		protected override void OnCellViewsHolderCreated(UpdateDraftItemHolder cellVH, CellGroupViewsHolder<UpdateDraftItemHolder> cellGroup)
		{
			base.OnCellViewsHolderCreated(cellVH, cellGroup);
			cellVH.iconRemoteImageBehaviour.InitializeWithPool(texturePool);
		}

		protected override void UpdateCellViewsHolder(UpdateDraftItemHolder newOrRecycled)
		{
			var model = Data.GetOrCreate(newOrRecycled.ItemIndex);
			newOrRecycled.UpdateViews(model,OnSelect);
			newOrRecycled.DefaultSelect(newOrRecycled.ItemIndex == currentSelect);
			if (!string.IsNullOrEmpty(model.cover))
			{
				newOrRecycled.iconRemoteImageBehaviour.gameObject.SetActive(false);
				newOrRecycled.iconRemoteImageBehaviour.Load(model.cover, true, (fromCache,  success) =>
				{
					if (success)
					{
						newOrRecycled.iconRemoteImageBehaviour.gameObject.SetActive(true);
					}
				});
			}
		}
		
		public void OnSelect(MapInfo info)
		{
			var ovh = GetCellViewsHolderIfVisible(currentSelect);
			if (ovh != null)
			{
				ovh.DefaultSelect(false);
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
					nvh.DefaultSelect(true);
					dataAction?.Invoke(info);
				}
			}
		}
		
		

    }
    public class UpdateDraftItemHolder : CellViewsHolder
    {
        public RemoteImageBehaviour iconRemoteImageBehaviour;
        public UpdateDraftGameItem updateDraftItem;
        public override void CollectViews()
        {
            base.CollectViews();
            views.GetComponentAtPath("Mask/MapCover", out iconRemoteImageBehaviour);
            updateDraftItem = root.GetComponentInParent<UpdateDraftGameItem>();
        }


        public void UpdateViews(MapInfo info,Action<MapInfo> onSelect)
        {
	        updateDraftItem.Init(onSelect,info);
        }

        public void DefaultSelect(bool isSelect)
        {
            if (updateDraftItem != null)
            {
	            updateDraftItem.UpdateSelected(isSelect);
            }
        }

    }
}