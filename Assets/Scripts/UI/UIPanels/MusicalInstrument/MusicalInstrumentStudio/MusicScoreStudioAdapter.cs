using System;
using UnityEngine;
using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.IO;
using Com.TheFallenGames.OSA.Util.IO.Pools;
using frame8.Logic.Misc.Other.Extensions;
using GameData.BaseInfo;

public class MusicScoreStudioAdapter : GridAdapter<GridParams, MusicScoreStudioListViewsHolder>
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
		
		protected override void OnCellViewsHolderCreated(MusicScoreStudioListViewsHolder cellVH, CellGroupViewsHolder<MusicScoreStudioListViewsHolder> cellGroup)
		{
			base.OnCellViewsHolderCreated(cellVH, cellGroup);
			cellVH.IconRemoteImageBehaviour.InitializeWithPool(texturePool);
		}

		protected override void UpdateCellViewsHolder(MusicScoreStudioListViewsHolder newOrRecycled)
		{
			var model = Data[newOrRecycled.ItemIndex];
			newOrRecycled.UpdateViews(model, OnSelectItemAct, curStudioType);
			if (string.IsNullOrEmpty(model?.musicScoreInfo?.cover)||model?.musicScoreInfo?.cover == "https://")
			{
				newOrRecycled.IconRemoteImageBehaviour.gameObject.SetActive(false);
				return;
			}
			newOrRecycled.IconRemoteImageBehaviour.Load(model?.musicScoreInfo?.cover,onCompleted: (fromCache,isSuccess) =>
			{
				newOrRecycled.IconRemoteImageBehaviour.gameObject.SetActive(isSuccess);
			});
		}
	}
	
	public class MusicScoreStudioListViewsHolder : CellViewsHolder
	{
		public RemoteImageBehaviour IconRemoteImageBehaviour;
		public MusicScoreStudioItem StudioItem;
	
		public override void CollectViews()
		{
			base.CollectViews();
			IconRemoteImageBehaviour = GameObjectEx.FindChildByName(views, "MusicScoreIcon").GetComponent<RemoteImageBehaviour>();
			root.TryGetComponent(out StudioItem);
		}

		public virtual void UpdateViews(DraftListItem data, Action<DraftListItem> onSelect, StudioSubType studioType)
		{
			if(StudioItem == null)
				return;

			if (studioType == StudioSubType.Drafts)
			{
				//说明是CreateBtn
				bool isCreate = (data == null) || GameStudioUtils.GetBaseInfo(data) == null;
				if (isCreate)
				{
					StudioItem.InitCreateMode();
				}
				else
				{
					StudioItem.InitSelectMode(data, onSelect, studioType);
				}
			}
			else
			{
				StudioItem.InitSelectMode(data, onSelect, studioType);
			}
			
		}
	}
