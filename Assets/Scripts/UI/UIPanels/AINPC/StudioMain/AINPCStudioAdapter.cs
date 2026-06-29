using System;
using UnityEngine;
using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.IO;
using Com.TheFallenGames.OSA.Util.IO.Pools;
using frame8.Logic.Misc.Other.Extensions;
using GameData.BaseInfo;

namespace Game.AINPCStudio
{
	public class AINPCStudioAdapter : GridAdapter<GridParams, AINPCStudioListViewsHolder>
	{
		public StudioSubType curStudioType;
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
		
		protected override void OnCellViewsHolderCreated(AINPCStudioListViewsHolder cellVH, CellGroupViewsHolder<AINPCStudioListViewsHolder> cellGroup)
		{
			base.OnCellViewsHolderCreated(cellVH, cellGroup);
			cellVH.IconRemoteImageBehaviour.InitializeWithPool(texturePool);
		}

		protected override void UpdateCellViewsHolder(AINPCStudioListViewsHolder newOrRecycled)
		{
			var model = Data[newOrRecycled.ItemIndex];
			newOrRecycled.UpdateViews(model, OnSelectItemAct, curStudioType);

			string coverUrl = "";
			coverUrl = model?.npc?.cover;
			
			//不保存使用默认封面时，url为null
			if (!string.IsNullOrEmpty(coverUrl))
			{
				newOrRecycled.IconRemoteImageBehaviour.Load(coverUrl);
			}
		}
	}
	
	public class AINPCStudioListViewsHolder : CellViewsHolder
	{
		public RemoteImageBehaviour IconRemoteImageBehaviour;
		public AINPCStudioItem StudioItem;
	
		public override void CollectViews()
		{
			base.CollectViews();
			IconRemoteImageBehaviour = GameObjectEx.FindChildByName(views, "RemoteIcon").GetComponent<RemoteImageBehaviour>();
			root.TryGetComponent(out StudioItem);
		}

		public virtual void UpdateViews(DraftListItem data, Action<DraftListItem> onSelect, StudioSubType studioType)
		{
			if(StudioItem == null)
				return;
			
			if(data.npc == null)
			{
				if (studioType == StudioSubType.Drafts)
				{
					StudioItem.InitCreateMode();
				}

				if (studioType == StudioSubType.Published)
				{
					StudioItem.InitGoToStore();
				}
			}
			else
			{
				StudioItem.InitSelectMode(data, onSelect, studioType);
			}
		}
	}
}
