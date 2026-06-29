using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.DataHelpers;
using GameUI;
using System;

namespace UI.UIPanels.FittingRoom
{
    public class GEParkWorkAdpter : GridAdapter<GridParams, GEParkWorkItemHolder>
    {
        public LazyDataHelper<DraftListItem> Data { get; set; }

        public Action<DraftListItem> OnItemSelected;

        protected override void OnInitialized()
        {
            base.OnInitialized();
        }

        protected override void UpdateCellViewsHolder(GEParkWorkItemHolder viewsHolder)
        {
            var model = Data.GetOrCreate(viewsHolder.ItemIndex);
            if (model == null)
            {
                return;
            }
            viewsHolder.UpdateViews(model, OnItemSelected, viewsHolder.ItemIndex);
        }
    }

    public class GEParkWorkItemHolder : CellViewsHolder
    {
        public GEParkWorkItem item;

        public override void CollectViews()
        {
            base.CollectViews();
            item = root.GetComponent<GEParkWorkItem>();
        }

        public void UpdateViews(DraftListItem data, Action<DraftListItem> action, int idx)
        {
            if (data == null)
            {
                return;
            }
            item.SetData(data, action, idx);
        }
    }
}
