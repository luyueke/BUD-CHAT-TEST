using System;
using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.IO;
using Game.AssetToolBox;

namespace BUD.AnimPose
{
    public class PoseBuyPropOSAAdapter: GridAdapter<GridParams, PoseBuyItemHolder>
	{
		public UnityEngine.Events.UnityEvent OnItemsUpdated;
		public LazyDataHelper<PoseBuyItemData> Data { get; set; }
		private Action<PoseBuyItemData> _onSelectAct;
		private int currentSelect = -1;

		public override void Refresh(bool contentPanelEndEdgeStationary = false /*ignored*/, bool keepVelocity = false)
		{
			_CellsCount = Data.Count;
			OnItemsUpdated?.Invoke();
			base.Refresh(false, keepVelocity);
		}

		protected override void UpdateCellViewsHolder(PoseBuyItemHolder newOrRecycled)
		{
			var model = Data.GetOrCreate(newOrRecycled.ItemIndex);
			newOrRecycled.RefreshData(model, OnSelect);
		}

		public void SetOnSelectAct(Action<PoseBuyItemData> act)
		{
			this._onSelectAct = act;
		}

		public void OnSelect(PoseBuyItemData data)
		{

			this._onSelectAct?.Invoke(data);
		}
	}

	public class PoseBuyItemHolder : CellViewsHolder
	{
		public PoseBuyPropLoadItem propItem;

		public override void CollectViews()
		{
			base.CollectViews();
			propItem = views.GetComponentInParent<PoseBuyPropLoadItem>(true);
		}

		public void RefreshData(PoseBuyItemData data, Action<PoseBuyItemData> onClickAct)
		{
			if(propItem != null)
				propItem.UpdateViews(data, onClickAct);
		}
	}
}