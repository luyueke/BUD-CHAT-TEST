using System;
using Basic.Extensions;
using Com.TheFallenGames.OSA.Core;
using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.CustomParams;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.IO;
using Com.TheFallenGames.OSA.Util.IO.Pools;
using frame8.Logic.Misc.Other.Extensions;
using GameData.Base;
using UGCAsset;
using UGCAsset.Draft;
using UI.TopList;

namespace BUD.GameStudio
{

    public class AIMapRankAdapter : GridAdapter<GridParams, MapHeatItemHolder>
    {
        public UnityEngine.Events.UnityEvent OnItemsUpdated;
        public SimpleDataHelper<RankItem> Data { get; set; }
        public IPool texturePool;

        protected override void Start()
        {
            texturePool = new FIFOCachingPool(5);
            base.Start();
        }

        /// <summary>
        /// 销毁必须清空池对象
        /// </summary>
        protected override void OnDestroy()
        {
            base.OnDestroy();
            ClearPool();
        }

        public void ClearPool()
        {
            texturePool.Clear();
        }


        public override void Refresh(bool contentPanelEndEdgeStationary = false /*ignored*/, bool keepVelocity = false)
        {
            _CellsCount = Data.Count;
            OnItemsUpdated?.Invoke();
            base.Refresh(false, keepVelocity);
        }


        protected override void OnCellViewsHolderCreated(MapHeatItemHolder cellVH, CellGroupViewsHolder<MapHeatItemHolder> cellGroup)
        {
            base.OnCellViewsHolderCreated(cellVH, cellGroup);
        }

        // This is called anytime a previously invisible item become visible, or after it's created, 
        // or when anything that requires a refresh happens
        // Here you bind the data from the model to the item's views
        // *For the method's full description check the base implementation
        protected override void UpdateCellViewsHolder(MapHeatItemHolder newOrRecycled)
        {
            var curData = Data[newOrRecycled.ItemIndex]; //RecommendItemData

            //更新方法，要什么就传
            newOrRecycled.UpdateViews(curData);
        }

    }

    public class MapHeatItemHolder : CellViewsHolder
    {
        //TopListItem
        public MapHeatContributionItem item;

        public override void CollectViews()
        {
            base.CollectViews();
            root.TryGetComponent(out item);
        }

        public virtual void UpdateViews(RankItem data)
        {
            if (item == null)
                return;

            item.InitData(data);
        }
    }
}



