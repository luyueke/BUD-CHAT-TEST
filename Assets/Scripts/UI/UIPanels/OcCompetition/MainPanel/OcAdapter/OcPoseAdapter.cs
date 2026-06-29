using BUD.AnimPose;
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
    public class OcPoseAdapter : GridAdapter<GridParams, OcPoseItemHolder>
    {
        public LazyDataHelper<OcPoseItemData> Data { get; set; }

        public Action<OcPoseItemData> OnItemSelected;

        protected override void OnInitialized()
        {
            base.OnInitialized();
        }

        protected override void UpdateCellViewsHolder(OcPoseItemHolder viewsHolder)
        {
            var model = Data.GetOrCreate(viewsHolder.ItemIndex);
            if (model == null)
            {
                return;
            }
            viewsHolder.UpdateViews(model, OnItemSelected, viewsHolder.ItemIndex);
        }
    }

    public class OcPoseItemHolder : CellViewsHolder
    {
        public OcPoseItem item;

        public override void CollectViews()
        {
            base.CollectViews();
            item = root.GetComponent<OcPoseItem>();
        }

        public void UpdateViews(OcPoseItemData data, Action<OcPoseItemData> action, int idx)
        {
            if (data == null)
            {
                return;
            }

            item.SetData(data, action, idx);
        }
    }
}