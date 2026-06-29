// @Author: YangJie
// @Description:
// @Date:  2023/09/12
// @Modify:

using System;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using UnityEngine;

namespace UI.BaseOSA
{
    public class BaseGridEntry<T, TD, TF, TG, TH, TI> : MonoBehaviour 
        where T: BaseGridAdapter<TD, TF, TG> 
        where TD : BaseItemHolder<TG, TF>, new() 
        where TF : BaseItem<TG>, new() 
        where TG : BaseData
        where TH: BaseDataLoader<TG>
    {
        public T adapter;
        [SerializeField] private TH dataLoader;
        [SerializeField] private PullToRefreshBehaviour refreshController;
        private List<TG> allModels = new List<TG>();
        private Action<TF, TG> selectedCallBack;

        public void AddSelectedCallBack(Action<TF, TG> callBack)
        {
            selectedCallBack += callBack;
        }
        
        public void RemoveSelectedCallBack(Action<TF, TG> callBack)
        {
            selectedCallBack -= callBack;
        }

        public void OnItemSelected(TF item, TG data)
        {
            selectedCallBack?.Invoke(item, data);
        }

        public void ResetAdapter()
        {
            // Resetting to 0 count clears everything, including visible items, so nothing will be recycled
            if (adapter.Data != null)
            {
                adapter.ResetItems(0);
                adapter.ClearPool();
            }
        }
        
        public void InitDataList(List<TG> dataList)
        {
            if (!adapter.IsInitialized)
            {
                return;
            }
            if (dataList == null || dataList.Count == 0)
            {
                adapter.OnItemsUpdated?.Invoke();
                adapter.ResetItems(0);
                adapter.ClearPool();
                return;
            }
            allModels = dataList;
            adapter.Data.ResetItems(dataList.Count, false);
        }
        
        protected void Start()
        {
            //需要动态拉取数据必须要做的初始化操作
            refreshController.OnRefreshWithSlideUp.AddListener(OnPullReleased);
            adapter.OnItemsUpdated.AddListener(refreshController.HideGizmo);
            adapter.Data = new LazyDataHelper<TG>(adapter, CreateNewModel);
            adapter.Init();
            adapter.AddSelectedCallBack(OnItemSelected);
            ResetAdapter();
        }
        
        public void OnPullReleased()
        {
            if (dataLoader != null)
            {
                dataLoader.GetDataList(OnReceivedNewModelsForInsert);
            }
        }
        private void OnReceivedNewModelsForInsert(List<TG> newModels)
        {
            if (newModels == null || newModels.Count == 0)
            {
                adapter.OnItemsUpdated?.Invoke();
                return;
            }
            adapter.Data.List.AddRange(newModels);
            adapter.Refresh(false);
        }

        /// <summary>
        /// 可以调整单元格数据内容,实现单元格大小分类致等特殊需求
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        protected virtual TG CreateNewModel(int index)
        {
            return allModels[index];
        }

        protected virtual void OnDestroy()
        {
            selectedCallBack = null;
            adapter.RemoveSelectedCallBack(OnItemSelected);
        }


    }
}