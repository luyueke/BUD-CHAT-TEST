using System;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.IO;
using Com.TheFallenGames.OSA.Util.IO.Pools;

namespace Game.AINPCStudio
{
	public class AIBuddySelectAdapter : GridAdapter<GridParams, AIBuddySelectViewsHolder>
	{
		public UnityEngine.Events.UnityEvent OnItemsUpdated;
		public SimpleDataHelper<AIBuddySelectItemData> Data { get; set; }
		public IPool texturePool;

		public Action<AIBuddySelectItemData> OnSelectItemAct;
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


		protected override void OnCellViewsHolderCreated(AIBuddySelectViewsHolder cellVH,
			CellGroupViewsHolder<AIBuddySelectViewsHolder> cellGroup)
		{
			base.OnCellViewsHolderCreated(cellVH, cellGroup);
			cellVH.IconRemoteImageBehaviour.InitializeWithPool(texturePool);
		}
		
		protected override void UpdateCellViewsHolder(AIBuddySelectViewsHolder newOrRecycled)
		{
			var curData = Data[newOrRecycled.ItemIndex]; //RecommendItemData
			newOrRecycled.UpdateViews(curData, (data) =>
			{
				OnSelectItemAct?.Invoke(data);
				Refresh();
			});
			newOrRecycled.IconRemoteImageBehaviour.Load(curData?.ugcInfo?.cover);
			
			// var item = newOrRecycled.infoItem;
			// if (item != null)
			// {
			// 	item.SetSelectState(curData?.ugcInfo?.id == GetCurSelectedIdFunc());
			// }
		}

		public void SetOnClickAction(Action<AIBuddySelectItemData> act)
		{
			this.OnSelectItemAct = act;
		}
		
		public void SetGetSelectedIdFunc(Func<string> func)
		{
			this.GetCurSelectedIdFunc = func;
		}
	}

	public class AIBuddySelectViewsHolder : CellViewsHolder
	{
		public RemoteImageBehaviour IconRemoteImageBehaviour;
		public AIBuddySelectItem infoItem;

		public override void CollectViews()
		{
			base.CollectViews();
			IconRemoteImageBehaviour = GameObjectEx.FindComponentByName<RemoteImageBehaviour>(views, "MapCover");
			root.TryGetComponent(out infoItem);
		}

		public virtual void UpdateViews(AIBuddySelectItemData data,  Action<AIBuddySelectItemData> act)
		{
			if (infoItem == null)
				return;

			infoItem.InitSelectMode(data, act);
		}
	}
}