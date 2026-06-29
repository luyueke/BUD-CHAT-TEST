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
	public class JoinFriendAdapter : GridAdapter<GridParams, JoinFriendItemHolder>
	{
		public UnityEngine.Events.UnityEvent OnItemsUpdated;
		public LazyDataHelper<JoinFriendData> Data { get; set; }
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

		protected override void OnCellViewsHolderCreated(JoinFriendItemHolder cellVH, CellGroupViewsHolder<JoinFriendItemHolder> cellGroup)
		{
			base.OnCellViewsHolderCreated(cellVH, cellGroup);
			cellVH.Remote_Cover.InitializeWithPool(texturePool);
		}

		protected override void UpdateCellViewsHolder(JoinFriendItemHolder newOrRecycled)
		{
			var model = Data.GetOrCreate(newOrRecycled.ItemIndex);
			newOrRecycled.RefreshData(model);
		}
	}

	public class JoinFriendItemHolder : CellViewsHolder
	{
		public RemoteImageBehaviour Remote_Cover;
		public JoinFriendItem ServerItem;

		public override void CollectViews()
		{
			base.CollectViews();
			views.GetComponentAtPath("Cover", out Remote_Cover);
			ServerItem = root.GetComponentInParent<JoinFriendItem>();
		}

		public void RefreshData(JoinFriendData data)
		{
			ServerItem.InitData(data);
		}
	}
}
