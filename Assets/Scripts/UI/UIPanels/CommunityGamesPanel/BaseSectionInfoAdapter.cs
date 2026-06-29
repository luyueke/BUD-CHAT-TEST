using System;
using UnityEngine;
using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.IO;
using Com.TheFallenGames.OSA.Util.IO.Pools;
using frame8.Logic.Misc.Other.Extensions;
using Game.CommunityGame;

namespace Game.CommunityGame
{
	public class BaseSectionInfoAdapter : GridAdapter<GridParams, BaseSectionInfoViewsHolder>
	{
		public UnityEngine.Events.UnityEvent OnItemsUpdated;
		public SimpleDataHelper<RecommendItemData> Data { get; set; }
		public IPool texturePool;

		protected override void Start()
		{
			texturePool = new FIFOCachingPool(12, TextureDestoryer);
			base.Start();
		}

		/// <summary>
		/// 销毁必须清空池对象
		/// </summary>
		protected override void OnDestroy()
		{
			base.OnDestroy();
			ClearPool();
		}

		public void ClearPool()
		{
			texturePool?.Clear();
		}

		private void TextureDestoryer(object urlKey, object texture)
		{
			var asUnityObject = texture as UnityEngine.Object;
			if (asUnityObject != null)
				Destroy(asUnityObject);
		}
		
		public override void Refresh(bool contentPanelEndEdgeStationary = false /*ignored*/, bool keepVelocity = false)
		{
			if(Data == null)
				return;
			
			_CellsCount = Data.Count;
			OnItemsUpdated?.Invoke();
			base.Refresh(false, keepVelocity);
		}
		
		
		protected override void OnCellViewsHolderCreated(BaseSectionInfoViewsHolder cellVH, CellGroupViewsHolder<BaseSectionInfoViewsHolder> cellGroup)
		{
			base.OnCellViewsHolderCreated(cellVH, cellGroup);
		}

		// This is called anytime a previously invisible item become visible, or after it's created, 
		// or when anything that requires a refresh happens
		// Here you bind the data from the model to the item's views
		// *For the method's full description check the base implementation
		protected override void UpdateCellViewsHolder(BaseSectionInfoViewsHolder newOrRecycled)
		{
			
		}

		public void ChangeRawImageState(bool isRelease)
		{
			
		}
	}
}

public class BaseSectionInfoViewsHolder : CellViewsHolder
{
	public RemoteImageBehaviour IconRemoteImageBehaviour;
	public BaseSectionInfoItem SectionInfoItem;
	
	public override void CollectViews()
	{
		base.CollectViews();
		IconRemoteImageBehaviour = GameObjectEx.FindComponentByName<RemoteImageBehaviour>(views, "MapCover");
		root.TryGetComponent(out SectionInfoItem);
	}

	public virtual void UpdateViews(RecommendItemData data)
	{
		if(SectionInfoItem == null)
			return;
			
		SectionInfoItem.InitData(data);
	}
	
	public void SetInteractiveCallback(Action act)
	{
		if(SectionInfoItem == null)
			return;
			
		SectionInfoItem.SetInteractiveCallback(act);
	}
}