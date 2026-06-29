using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using Game.Store;
using Game.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BUD.AnimPose
{
    public class QuickPoseAdapter : GridAdapter<GridParams, PoseOcItemHolder>
    {
        public PullToRefreshBehaviour PullToRefreshBehaviour;
        public LazyDataHelper<PoseOcServerData> Data { get; set; }

        public Action<PoseOcServerData, bool> OnItemSelected;
        public Action<PoseOcServerData> OnItemDelete;

        protected override void OnInitialized()
        {
            base.OnInitialized();
        }

        protected override void UpdateCellViewsHolder(PoseOcItemHolder viewsHolder)
        {
            var model = Data.GetOrCreate(viewsHolder.ItemIndex);
            viewsHolder.UpdateViews(model, OnItemSelected, OnItemDelete);
        }

        protected override void OnCellViewsHolderCreated(PoseOcItemHolder cellVH, CellGroupViewsHolder<PoseOcItemHolder> cellGroup)
        {
            base.OnCellViewsHolderCreated(cellVH, cellGroup);
        }
    }

    public class PoseOcItemHolder : CellViewsHolder
    {
        public QuickPoseItem item;

        public override void CollectViews()
        {
            base.CollectViews();
            item = root.GetComponent<QuickPoseItem>();
        }


        public void UpdateViews(PoseOcServerData data, Action<PoseOcServerData, bool> action, Action<PoseOcServerData> deleteAct)
        {
            item.UpdateViews(data, action, deleteAct);
        }
    }
}