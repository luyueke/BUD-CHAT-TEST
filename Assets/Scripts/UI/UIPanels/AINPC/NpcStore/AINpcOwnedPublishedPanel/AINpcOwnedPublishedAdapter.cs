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
	public class AINpcOwnedPublishedAdapter : GridAdapter<GridParams, AINpcOwnedListViewsHolder>
	{
		public Action<DraftListItem> OnSelectItemAct;
		public UnityEngine.Events.UnityEvent OnItemsUpdated;
		public SimpleDataHelper<DraftListItem> Data { get; set; }
		public IPool texturePool;
		private bool IsInDeleteMode;
		private Func<string> GetCurSelectedIdFunc;

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
			if(Data == null)
				return;
			
			_CellsCount = Data.Count;
			OnItemsUpdated?.Invoke();
			base.Refresh(false, keepVelocity);
		}
		
		protected override void OnCellViewsHolderCreated(AINpcOwnedListViewsHolder cellVH, CellGroupViewsHolder<AINpcOwnedListViewsHolder> cellGroup)
		{
			base.OnCellViewsHolderCreated(cellVH, cellGroup);
			cellVH.IconRemoteImageBehaviour.InitializeWithPool(texturePool);
		}

		protected override void UpdateCellViewsHolder(AINpcOwnedListViewsHolder newOrRecycled)
		{
			var model = Data[newOrRecycled.ItemIndex];
			newOrRecycled.UpdateViews(model, (data) =>
			{
				OnSelectItemAct?.Invoke(data);
				Refresh();
			});

			string coverUrl = "";
			coverUrl = model?.npc?.cover;
			
			//不保存使用默认封面时，url为null
			if (!string.IsNullOrEmpty(coverUrl))
			{
				newOrRecycled.IconRemoteImageBehaviour.Load(coverUrl);
			}
            
			var item = newOrRecycled.OwnedPublishedItem;
			if (item != null && model?.npc != null)
			{
				item.SetSelectState(model.npc.id == GetCurSelectedIdFunc());
			}
		}
		
		public void SetOnClickAction(Action<DraftListItem> act)
		{
			this.OnSelectItemAct = act;
		}
		
		public void SetGetSelectedIdFunc(Func<string> func)
		{
			this.GetCurSelectedIdFunc = func;
		}
	}
	
	public class AINpcOwnedListViewsHolder : CellViewsHolder
	{
		public RemoteImageBehaviour IconRemoteImageBehaviour;
		public AINpcOwnedPublishedItem OwnedPublishedItem;
	
		public override void CollectViews()
		{
			base.CollectViews();
			IconRemoteImageBehaviour = GameObjectEx.FindChildByName(views, "RemoteIcon").GetComponent<RemoteImageBehaviour>();
			root.TryGetComponent(out OwnedPublishedItem);
		}

		public virtual void UpdateViews(DraftListItem data, Action<DraftListItem> onSelect)
		{
			if(OwnedPublishedItem == null)
				return;
			
			if(data.npc != null)
			{
				OwnedPublishedItem.InitSelectMode(data, onSelect);
			}
			else
			{
				OwnedPublishedItem.InitCreateMode();
			}
		}
	}
}
