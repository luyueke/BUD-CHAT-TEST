using System;
using UnityEngine;
using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.IO;
using Com.TheFallenGames.OSA.Util.IO.Pools;
using frame8.Logic.Misc.Other.Extensions;
using GameData;
using GameData.Base;
using GameData.UGCData;
using UnityEngine.UI;

namespace UI.UIPanels.ProfilePanel
{
    public class ProfileMapAdapter: GridAdapter<GridParams, ProfileMapHolder>
    {
		public UnityEngine.Events.UnityEvent OnItemsUpdated;
		public Action<MapResInfo, Texture> dataAction;
		public LazyDataHelper<MapResInfo> Data { get; set; }
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
			texturePool?.Clear();
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
		
		protected override void OnCellViewsHolderCreated(ProfileMapHolder cellVH, CellGroupViewsHolder<ProfileMapHolder> cellGroup)
		{
			base.OnCellViewsHolderCreated(cellVH, cellGroup);
			cellVH.iconRemoteImageBehaviour.InitializeWithPool(texturePool);
		}

		protected override void UpdateCellViewsHolder(ProfileMapHolder newOrRecycled)
		{
			var model = Data.GetOrCreate(newOrRecycled.ItemIndex);
			newOrRecycled.UpdateViews(model,OnSelect);
			if (model.mapInfo.cover != null)
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
		
		public void OnSelect(MapResInfo info, Texture texture)
		{
			var ovh = GetCellViewsHolderIfVisible(currentSelect);
			if (ovh != null)
			{
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
					dataAction?.Invoke(info, texture);
				}
			}
		}
    }
    public class ProfileMapHolder : CellViewsHolder
    {
        public RemoteImageBehaviour iconRemoteImageBehaviour;
        public ProfileGameItem profileGameItem;
        public override void CollectViews()
        {
            base.CollectViews();
            views.GetComponentAtPath("Cover", out iconRemoteImageBehaviour);
            profileGameItem = root.GetComponentInParent<ProfileGameItem>();
            profileGameItem.InitUI();
        }


        public void UpdateViews(MapResInfo info,Action<MapResInfo, Texture> onSelect)
        {
	        profileGameItem.SetData(info, onSelect);
        }
        
        
    }
}