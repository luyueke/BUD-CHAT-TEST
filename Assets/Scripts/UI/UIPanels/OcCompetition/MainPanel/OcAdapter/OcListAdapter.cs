using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.DataHelpers;
using GameData.Base;
using GameUI;
using System;
using System.Collections;
using UI.UIPanels.FittingRoom;
using UnityEngine;

namespace UI.UIPanels.FittingRoom
{
    public class OcListAdapter : GridAdapter<GridParams, OcListItemHolder>
    {
        public LazyDataHelper<OcServerData> Data { get; set; }

        public Action<OcServerData> OnItemSelected;

        protected override void OnInitialized()
        {
            base.OnInitialized();
        }

        protected override void UpdateCellViewsHolder(OcListItemHolder viewsHolder)
        {
            var model = Data.GetOrCreate(viewsHolder.ItemIndex);
            if (model == null)
            {
                return;
            }
            viewsHolder.UpdateViews(model, OnItemSelected, viewsHolder.ItemIndex);
        }
    }

    public class OcListItemHolder : CellViewsHolder
    {
        public OcCompetitionItem item;

        public override void CollectViews()
        {
            base.CollectViews();
            item = root.GetComponent<OcCompetitionItem>();
        }

        public void UpdateViews(OcServerData data, Action<OcServerData> action, int idx)
        {
            if (data == null)
            {
                return;
            }

            item.SetData(data, action, idx);
        }
    }
}