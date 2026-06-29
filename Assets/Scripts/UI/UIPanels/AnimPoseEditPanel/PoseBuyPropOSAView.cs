using System;
using System.Collections;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using Game.AssetToolBox;
using Game.PropStore;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace BUD.AnimPose
{
    public class PoseBuyPropOSAView : BasePosePropOSAView
    {
        public GameObject EmptyText;
        public PullToRefreshBehaviour RefreshCtr;
        public PoseBuyPropOSAAdapter Adapter;
        public PoseBuyPropDataLoader DataLoader;
        private List<PoseBuyItemData> allDatas = new List<PoseBuyItemData>();
        public Action SelectPropClick { private get; set; }
        private string CurSelectId;

        public void OnCreate()
        {
            RefreshCtr.OnRefreshWithSign.AddListener(OnPullReleased);
            Adapter.Data = new LazyDataHelper<PoseBuyItemData>(Adapter, CreateNewModel);
            Adapter.Init();
            Adapter.SetOnSelectAct(OnItemSelected);
            Adapter.OnItemsUpdated.AddListener(RefreshCtr.HideGizmo);
            GetFirstPageDatas();
            UIManager.Inst.AddClosePanelAction(OnClosePanelAction);
        }

        private void OnDestroy()
        {
            UIManager.Inst.RemoveClosePanelAction(OnClosePanelAction);
        }

        /// <summary>
        /// 可以调整单元格数据内容,实现单元格大小分类致等特殊需求
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private PoseBuyItemData CreateNewModel(int index)
        {
            if (index == 0)
            {
                return allDatas[0];
            }
            else if (index < allDatas.Count)
            {
                var poseData = allDatas[index];
                poseData.Selected = CurSelectId == poseData.ugcInfo.id;
                return allDatas[index];
            }

            return new PoseBuyItemData();
        }

        private void OnItemSelected(PoseBuyItemData data)
        {
            if (data.ugcInfo == null)
            {
                // 跳转商城
                var storePanel = UIManager.Inst.OpenPanel<PropStorePanel>(PanelId.PropStorePanel);
                storePanel.SetBuySuccessUpdate(OnRefreshList);
                return;
            }
            CurSelectId = data.ugcInfo.id;
            Adapter.Data.ResetItems(allDatas.Count);
            OnSelectProp?.Invoke(data.ugcInfo);
        }

        private void OnRefreshList()
        {
            GetFirstPageDatas();
        }

        private void OnClosePanelAction(BasePanel panel)
        {
            if (panel is PropStorePanel)
            {
                
            }
        }

        public void GetFirstPageDatas()
        {
            allDatas.Clear();
            allDatas.Add(new PoseBuyItemData(){isStore = true});
            DataLoader.ResetCookie();
            DataLoader.GetDatas((datas) =>
            {
                EmptyText.SetActive(datas.Count == 0);
                allDatas.AddRange(datas);
                Adapter.OnItemsUpdated?.Invoke();
                Adapter.Data.ResetItems(allDatas.Count, false);
            });
        }

        private void OnPullReleased(float sign)
        {
            if (sign < 0)
            {
                DataLoader.GetDatas(OnReceivedNewModelsForInsert);
            }
        }

        private void OnReceivedNewModelsForInsert(List<PoseBuyItemData> newDatas)
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
