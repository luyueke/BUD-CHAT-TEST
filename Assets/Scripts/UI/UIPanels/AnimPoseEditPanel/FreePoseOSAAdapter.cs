using System;
using UnityEngine;
using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.IO;
using Com.TheFallenGames.OSA.Util.IO.Pools;
using frame8.Logic.Misc.Other.Extensions;
using GameData.MapData;
using UI.UIWidgets;

namespace BUD.AnimPose
{
	public class FreePoseOSAAdapter : GridAdapter<GridParams, FreePoseItemHolder>
	{
		public UnityEngine.Events.UnityEvent OnItemsUpdated;
		public LazyDataHelper<QuickPoseData> Data { get; set; }
		private Action<QuickPoseData> _onSelectAct;
		private int currentSelect = -1;

		public override void Refresh(bool contentPanelEndEdgeStationary = false /*ignored*/, bool keepVelocity = false)
		{
			_CellsCount = Data.Count;
			OnItemsUpdated?.Invoke();
			base.Refresh(false, keepVelocity);
		}
		

		protected override void UpdateCellViewsHolder(FreePoseItemHolder newOrRecycled)
		{
			var model = Data.GetOrCreate(newOrRecycled.ItemIndex);
			newOrRecycled.RefreshData(model, OnSelect);
		}

		public void SetOnSelectAct(Action<QuickPoseData> act)
		{
			this._onSelectAct = act;
		}

		public void OnSelect(QuickPoseData data)
		{
			this._onSelectAct?.Invoke(data);
		}
	}

	public class FreePoseItemHolder : CellViewsHolder
	{
		public AnimPoseLoadItem PoseItem;

		public override void CollectViews()
		{
			base.CollectViews();
			PoseItem = root.GetComponentInParent<AnimPoseLoadItem>();
		}

		public void RefreshData(QuickPoseData data, Action<QuickPoseData> onClick)
		{
			PoseItem.UpdateViews(data, onClick);
		}
	}
}
