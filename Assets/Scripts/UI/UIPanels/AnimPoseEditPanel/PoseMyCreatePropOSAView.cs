using System;
using System.Collections;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using Game.AssetToolBox;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace BUD.AnimPose
{
    public class PoseMyCreatePropOSAView : BasePosePropOSAView
    {
        public GameObject EmptyText;
        public PullToRefreshBehaviour RefreshCtr;
        public PoseMyCreatePropOSAAdapter Adapter;
        public PoseMyCreatePropDataLoader DataLoader;
        private List<PoseCreateItemData> allDatas = new List<PoseCreateItemData>();
        private string CurSelectId;

        public void OnCreate()
        {
            RefreshCtr.OnRefreshWithSign.AddListener(OnPullReleased);
          
            Adapter.Data = new LazyDataHelper<PoseCreateItemData>(Adapter, CreateNewModel);
            Adapter.Init();
            Adapter.SetOnSelectAct(OnItemSelected);
            Adapter.OnItemsUpdated.AddListener(RefreshCtr.HideGizmo);
            GetFirstPageDatas();
        }

        /// <summary>
        /// 可以调整单元格数据内容,实现单元格大小分类致等特殊需求
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private PoseCreateItemData CreateNewModel(int index)
        {
            if (index >= 0 && index < allDatas.Count)
            {
                var poseData = allDatas[index];
                poseData.Selected = CurSelectId == poseData.propInfo.id;
                return allDatas[index];
            }

            return new PoseCreateItemData();
        }

        private void OnItemSelected(PoseCreateItemData data)
        {
            CurSelectId = data.propInfo.id;
            Adapter.Data.ResetItems(allDatas.Count);
            OnSelectProp?.Invoke(data.propInfo);
        }

        public void GetFirstPageDatas()
        {
            allDatas.Clear();
            DataLoader.GetDatas((datas) =>
            {
                EmptyText.SetActive(datas.Count == 0);
                allDatas.AddRange(datas);
                Adapter.OnItemsUpdated?.Invoke();
                Adapter.Data.ResetItems(datas.Count, false);
            });
        }

        private void OnPullReleased(float sign)
        {
            if (sign < 0)
            {
                DataLoader.GetDatas(OnReceivedNewModelsForInsert);
            }
        }

        private void OnReceivedNewModelsForInsert(List<PoseCreateItemData> newDatas)
        {
            if (newDatas == null || newDatas.Count == 0)
            {
                Adapter.OnItemsUpdated?.Invoke();
                return;
            }
            allDatas.AddRange(newDatas);
            Adapter.OnItemsUpdated?.Invoke();
            Adapter.Data.ResetItems(allDatas.Count);
        }
    }
}
