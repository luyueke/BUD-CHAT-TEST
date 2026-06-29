using System;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.IO;
using Com.TheFallenGames.OSA.Util.IO.Pools;
using GameData.Account;

namespace Game.AINPCStudio
{
	public class AIBuddyOwnedAdapter : GridAdapter<GridParams, AIBuddyOwnedViewsHolder>
	{
		public UnityEngine.Events.UnityEvent OnItemsUpdated;
		public SimpleDataHelper<AIBuddyInfo> Data { get; set; }
		public IPool texturePool;

		public Action<AIBuddyInfo> OnSelectItemAct;
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


		protected override void OnCellViewsHolderCreated(AIBuddyOwnedViewsHolder cellVH, CellGroupViewsHolder<AIBuddyOwnedViewsHolder> cellGroup)
		{
			base.OnCellViewsHolderCreated(cellVH, cellGroup);
		}
		
		protected override void UpdateCellViewsHolder(AIBuddyOwnedViewsHolder newOrRecycled)
		{
			var curData = Data[newOrRecycled.ItemIndex]; //RecommendItemData
			newOrRecycled.UpdateViews(curData, (data) =>
			{
				OnSelectItemAct?.Invoke(data);
				Refresh();
			});
			
			var item = newOrRecycled.infoItem;
			if (item != null)
			{
				item.SetSelectState(curData?.id == GetCurSelectedIdFunc());
			}
		}

		public void SetOnClickAction(Action<AIBuddyInfo> act)
		{
			this.OnSelectItemAct = act;
		}
		
		public void SetGetSelectedIdFunc(Func<string> func)
		{
			this.GetCurSelectedIdFunc = func;
		}
	}

	public class AIBuddyOwnedViewsHolder : CellViewsHolder
	{
		public AIBuddyOwnedItem infoItem;

		public override void CollectViews()
		{
			base.CollectViews();
			root.TryGetComponent(out infoItem);
		}

		public virtual void UpdateViews(AIBuddyInfo data,  Action<AIBuddyInfo> act)
		{
			if (infoItem == null)
				return;

			if (data.npc == null)
			{
				infoItem.InitUnSelectedMode(data, act);
			}
			else
			{
				infoItem.InitSelectMode(data, act);
			}
		}
	}
}