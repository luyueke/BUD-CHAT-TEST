using System;
using UnityEngine;
using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.IO;
using Com.TheFallenGames.OSA.Util.IO.Pools;
using frame8.Logic.Misc.Other.Extensions;
using GameData.MapData;
using GameData;

namespace Game.AssetToolBox
{
	public class PublishAssetOSAAdapter : GridAdapter<GridParams, PublishAssetBoxItemHolder>
	{
		public UnityEngine.Events.UnityEvent OnItemsUpdated;
		public LazyDataHelper<PropResInfo> Data { get; set; }
		private Action<PropResInfo> _onSelectAct;
		private IPool texturePool;
		private int currentSelect = -1;

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
			if (texturePool != null)
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

		protected override void OnCellViewsHolderCreated(PublishAssetBoxItemHolder cellVH, CellGroupViewsHolder<PublishAssetBoxItemHolder> cellGroup)
		{
			base.OnCellViewsHolderCreated(cellVH, cellGroup);
			cellVH.Remote_Cover.InitializeWithPool(texturePool);
		}

		protected override void UpdateCellViewsHolder(PublishAssetBoxItemHolder newOrRecycled)
		{
			var model = Data.GetOrCreate(newOrRecycled.ItemIndex);
			newOrRecycled.RefreshData(model, OnSelect);
		}

		public void SetOnSelectAct(Action<PropResInfo> act)
		{
			this._onSelectAct = act;
		}

		public void OnSelect(PropResInfo data)
		{

			this._onSelectAct?.Invoke(data);
		}
	}

	public class PublishAssetBoxItemHolder : CellViewsHolder
	{
		public RemoteImageBehaviour Remote_Cover;
		public PublishAssetBaseItem ToolBoxItem;

		public override void CollectViews()
		{
			base.CollectViews();
			Remote_Cover = GameObjectEx.FindChildByName(views, "Cover").GetComponent<RemoteImageBehaviour>();
			ToolBoxItem = views.GetComponentInParent<PublishAssetBaseItem>();
		}

		public void RefreshData(PropResInfo data, Action<PropResInfo> onClickAct)
		{
			if(ToolBoxItem != null)
				ToolBoxItem.RefreshData(data, onClickAct);
		}
	}
}
