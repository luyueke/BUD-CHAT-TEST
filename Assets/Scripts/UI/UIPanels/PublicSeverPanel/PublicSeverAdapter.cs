using System;
using UnityEngine;
using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.IO;
using Com.TheFallenGames.OSA.Util.IO.Pools;
using frame8.Logic.Misc.Other.Extensions;
using GameData.MapData;

namespace Game.PublicSever
{
	public class PublicSeverAdapter : GridAdapter<GridParams, PublicSeverBoxItemHolder>
	{
		public UnityEngine.Events.UnityEvent OnItemsUpdated;
		public LazyDataHelper<PublicMapData> Data { get; set; }
		private Action<PublicMapData> _onSelectAct;
		private IPool texturePool;
		private int currentSelect = -1;

		protected override void Start()
		{
			base.Start();
			texturePool = new FIFOCachingPool(12, TextureDestoryer);
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

		protected override void OnCellViewsHolderCreated(PublicSeverBoxItemHolder cellVH, CellGroupViewsHolder<PublicSeverBoxItemHolder> cellGroup)
		{
			base.OnCellViewsHolderCreated(cellVH, cellGroup);
			cellVH.Remote_Cover.InitializeWithPool(texturePool);
		}

		protected override void UpdateCellViewsHolder(PublicSeverBoxItemHolder newOrRecycled)
		{
			var model = Data.GetOrCreate(newOrRecycled.ItemIndex);
			newOrRecycled.RefreshData(model);
			newOrRecycled.Remote_Cover.Load(model.mapCover);
		}
	}

	public class PublicSeverBoxItemHolder : CellViewsHolder
	{
		public RemoteImageBehaviour Remote_Cover;
		public PublicServerItem ServerItem;

		public override void CollectViews()
		{
			base.CollectViews();
			views.GetComponentAtPath("MapCover", out Remote_Cover);
			ServerItem = root.GetComponentInParent<PublicServerItem>();
		}

		public void RefreshData(PublicMapData data)
		{
			ServerItem.InitData(data);
		}
	}
}
