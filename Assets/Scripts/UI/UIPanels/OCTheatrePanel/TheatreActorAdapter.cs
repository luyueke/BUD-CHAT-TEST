using System;
using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.IO;
using Com.TheFallenGames.OSA.Util.IO.Pools;
using frame8.Logic.Misc.Other.Extensions;
using GameData;
using GameData.BaseInfo;
using UnityEngine;

namespace Game.MusicalInstrument
{
	public class TheatreActorAdapter : GridAdapter<GridParams, TheatreActorListViewsHolder>
	{
		public ActorSubType curActorType;
		public Action<DraftListItem> OnSelectItemAct;
		public UnityEngine.Events.UnityEvent OnItemsUpdated;
		public SimpleDataHelper<DraftListItem> Data { get; set; }
		public IPool texturePool;
		private bool IsInDeleteMode;

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
			if(texturePool != null)
				texturePool.Clear();
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
		
		protected override void OnCellViewsHolderCreated(TheatreActorListViewsHolder cellVH, CellGroupViewsHolder<TheatreActorListViewsHolder> cellGroup)
		{
			base.OnCellViewsHolderCreated(cellVH, cellGroup);
			cellVH.IconRemoteImageBehaviour.InitializeWithPool(texturePool);
		}

		protected override void UpdateCellViewsHolder(TheatreActorListViewsHolder newOrRecycled)
		{
			var model = Data[newOrRecycled.ItemIndex];
			newOrRecycled.UpdateViews(model, OnSelectItemAct, curActorType);
			string url = model?.actorInfo?.cover;
			//不保存使用默认封面时，url为null
			if (!string.IsNullOrEmpty(url))
			{
				newOrRecycled.IconRemoteImageBehaviour.Load(url);
			}
		}
	}
	
	public class TheatreActorListViewsHolder : CellViewsHolder
	{
		public RemoteImageBehaviour IconRemoteImageBehaviour;
		public TheatreActorDraftsItem ActorItem;
	
		public override void CollectViews()
		{
			base.CollectViews();
			IconRemoteImageBehaviour = GameObjectEx.FindChildByName(views, "InstrumentIcon").GetComponent<RemoteImageBehaviour>();
			root.TryGetComponent(out ActorItem);
		}

		public virtual void UpdateViews(DraftListItem data, Action<DraftListItem> onSelect, ActorSubType actorType)
		{
			if(ActorItem == null)
				return;

			if (actorType == ActorSubType.Drafts)
			{
				//说明是CreateBtn
				bool isCreate = (data == null) || GameStudioUtils.GetBaseInfo(data) == null;
				if (isCreate)
				{
					ActorItem.InitCreateMode(EnterGameModel.OCActorCreaterEmpty);
				}
				else
				{
					ActorItem.InitSelectMode(data, onSelect, actorType);
				}
			}
			else
			{
				ActorItem.InitSelectMode(data, onSelect, actorType);
			}
		}
	}
}
