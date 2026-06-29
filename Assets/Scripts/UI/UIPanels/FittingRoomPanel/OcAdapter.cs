using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using Game.Store;
using Game.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI.UIPanels.FittingRoom
{
    public class OcAdapter : GridAdapter<GridParams, OcItemHolder>
    {
        public PullToRefreshBehaviour PullToRefreshBehaviour;
        public LazyDataHelper<OcServerData> Data { get; set; }

        public Action<OcServerData, bool> OnItemSelected;
        public Action<OcServerData> OnItemDelete;

        protected override void OnInitialized()
        {
            base.OnInitialized();
        }

        protected override void UpdateCellViewsHolder(OcItemHolder viewsHolder)
        {
            var model = Data.GetOrCreate(viewsHolder.ItemIndex);
            viewsHolder.UpdateViews(model, OnItemSelected, OnItemDelete);
        }

        protected override void OnCellViewsHolderCreated(OcItemHolder cellVH, CellGroupViewsHolder<OcItemHolder> cellGroup)
        {
            base.OnCellViewsHolderCreated(cellVH, cellGroup);
        }
    }

    public class OcItemHolder : CellViewsHolder
    {
        public OcItem item;

        public override void CollectViews()
        {
            base.CollectViews();
            item = root.GetComponent<OcItem>();
        }


        public void UpdateViews(OcServerData data, Action<OcServerData, bool> action, Action<OcServerData> deleteAct)
        {
            item.UpdateViews(data, action, deleteAct);
        }
    }
}