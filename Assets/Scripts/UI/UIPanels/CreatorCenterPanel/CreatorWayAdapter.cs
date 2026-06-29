using System;
using System.Collections;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.DataHelpers;
using UnityEngine;

public class CreatorWayAdapter : GridAdapter<GridParams, CreatorWayItemHolder>
{

        public Action<CreatorCenterTaskLocalInfo> OnSelectItemAct;
        public SimpleDataHelper<CreatorCenterTaskLocalInfo> Data { get; private set; }

        public void SetData(List<CreatorCenterTaskLocalInfo> newModels)
        {
            Data = new SimpleDataHelper<CreatorCenterTaskLocalInfo>(this);
            Data.ResetItems(newModels);
        }

        /// <summary>
        /// 销毁必须清空池对象
        /// </summary>
        protected override void OnDestroy()
        {
            base.OnDestroy();
        }
        public void OnSelectItem(CreatorCenterTaskLocalInfo info)
        {
            OnSelectItemAct?.Invoke(info);
        }
        public override void Refresh(bool contentPanelEndEdgeStationary = false /*ignored*/, bool keepVelocity = false)
        {
            base.Refresh(false, keepVelocity);
        }

        protected override void UpdateCellViewsHolder(CreatorWayItemHolder viewsHolder)
        {
            viewsHolder.UpdateViews(OnSelectItem, Data[viewsHolder.ItemIndex]);
        }

       
}
public class CreatorWayItemHolder : CellViewsHolder
{
    public CreatorWayItem creatorWayItem;
    public override void CollectViews()
    {
        base.CollectViews();
        creatorWayItem = root.GetComponentInParent<CreatorWayItem>();
    }
    public void UpdateViews(Action<CreatorCenterTaskLocalInfo> onSelect, CreatorCenterTaskLocalInfo model)
    {
        creatorWayItem.Init(model,onSelect);
    }
}


