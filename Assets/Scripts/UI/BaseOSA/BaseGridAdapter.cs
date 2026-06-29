// @Author: YangJie
// @Description:
// @Date:  2023/09/12
// @Modify:

using System;
using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.IO.Pools;

namespace UI.BaseOSA
{
    public class BaseGridAdapter<T, TD, TF>: GridAdapter<GridParams, T> 
        where T: BaseItemHolder<TF, TD>, new() 
        where TD: BaseItem<TF>, new() 
        where TF: BaseData
    {
        
        public UnityEngine.Events.UnityEvent OnItemsUpdated;
    
        public LazyDataHelper<TF> Data { get; set; }
        
        protected virtual int PoolCapacity => 36;
        
        protected IPool texturePool;
        protected Action<TD, TF> itemSelected;
        
        
        
        protected override void Start()
        {
            // Prevent initialization. It'll be done from the outside
            base.Start();
            texturePool = new FIFOCachingPool(PoolCapacity, TextureDestroyer);
        }
        
        public override void Refresh(bool contentPanelEndEdgeStationary = false /*ignored*/, bool keepVelocity = false)
        {
            _CellsCount = Data.Count;
            OnItemsUpdated?.Invoke();
            base.Refresh(false, keepVelocity);
        }
        
        public void AddSelectedCallBack(Action<TD, TF> callBack)
        {
            itemSelected += callBack;
        }
        
        public void RemoveSelectedCallBack(Action<TD, TF> callBack)
        {
            itemSelected -= callBack;
        }

        protected override void OnCellViewsHolderCreated(T cellVH, CellGroupViewsHolder<T> cellGroup)
        {
            base.OnCellViewsHolderCreated(cellVH, cellGroup);
            cellVH.item.remoteImageBehaviour.InitializeWithPool(texturePool);
            cellVH.item.SetSelectedCallBack((data) =>
            {
                itemSelected?.Invoke(cellVH.item, data);
            });
        }

        protected override void UpdateCellViewsHolder(T viewsHolder)
        {
            var model = Data.GetOrCreate(viewsHolder.ItemIndex);
            viewsHolder.UpdateViews(model);
        }
        
        private void TextureDestroyer(object urlKey, object texture)
        {
            var asUnityObject = texture as UnityEngine.Object;
            if (asUnityObject != null)
                Destroy(asUnityObject);
        }
        
        /// <summary>
        /// 销毁必须清空池对象
        /// </summary>
        protected override void OnDestroy()
        {
            base.OnDestroy();
            ClearPool();
            itemSelected = null;
        }


        public void ClearPool()
        {
            texturePool?.Clear();
        }
    }
}