using System;
using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.IO;
using Game.AssetToolBox;

namespace BUD.AnimPose
{
    public class PoseMyCreatePropOSAAdapter: GridAdapter<GridParams, PoseAssetItemHolder>
	{
		public UnityEngine.Events.UnityEvent OnItemsUpdated;
		public LazyDataHelper<PoseCreateItemData> Data { get; set; }
		private Action<PoseCreateItemData> _onSelectAct;
		private int currentSelect = -1;

		public override void Refresh(bool contentPanelEndEdgeStationary = false /*ignored*/, bool keepVelocity = false)
		{
			_CellsCount = Data.Count;
			OnItemsUpdated?.Invoke();
			base.Refresh(false, keepVelocity);
		}

		protected override void UpdateCellViewsHolder(PoseAssetItemHolder newOrRecycled)
		{
			var model = Data.GetOrCreate(newOrRecycled.ItemIndex);
			newOrRecycled.RefreshData(model, OnSelect);
		}

		public void SetOnSelectAct(Action<PoseCreateItemData> act)
		{
			this._onSelectAct = act;
		}

		public void OnSelect(PoseCreateItemData data)
		{

			this._onSelectAct?.Invoke(data);
		}
	}

	public class PoseAssetItemHolder : CellViewsHolder
	{
		public PoseMyCreatePropLoadItem propItem;

		public override void CollectViews()
		{
			base.CollectViews();
			propItem = views.GetComponentInParent<PoseMyCreatePropLoadItem>();
		}

		public void RefreshData(PoseCreateItemData data, Action<PoseCreateItemData> onClickAct)
		{
			if(propItem != null)
				propItem.UpdateViews(data, onClickAct);
		}
	}
}