using System.Collections.Generic;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using Game.Base;
using Game.Props.PropsManagers;
using GameData;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;

namespace Game.AssetToolBox
{
    public class InventoryPublishBagPanel : BasePanel<InventoryPublishBagPanel>
    {
        public GameObject NoneTips;
        public CButton _btnClose;
        public Transform _transBG;
        public PullToRefreshBehaviour RefreshCtr;
        public PublishAssetOSAAdapter Adapter;
        public PublishAssetBaseDataLoader DataLoader;
        private List<PropResInfo> allDatas = new List<PropResInfo>();

        public override void OnCreate()
        {
            base.OnCreate();
            _btnClose.onClick.AddListener(() =>
            {
                CloseSelf();
            });
            Loader.Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/AvatarBg.prefab").Instantiate(_transBG);

            BindUI();
        }

        public override void OnShow(params object[] args)
        {
            base.OnShow(args);
            GetFirstPageDatas();
        }

        private void BindUI()
        {
            //需要动态拉取数据必须要做的初始化操作
            RefreshCtr.OnRefreshWithSign.AddListener(OnPullReleased);

            DataLoader.ResetCookie();
            Adapter.OnItemsUpdated.AddListener(RefreshCtr.HideGizmo);
            Adapter.Data = new LazyDataHelper<PropResInfo>(Adapter, CreateNewModel);
            Adapter.Init();
            Adapter.SetOnSelectAct(OnOwnedItemClick);
            

        }
        
        private void GetFirstPageDatas()
        {
            DataLoader.ResetCookie();
            DataLoader.GetDatas((datas) =>
            {
                ShowDatas(datas);
            });
        }

        private void ShowDatas(List<PropResInfo> datas)
        {
            if (datas == null || datas.Count == 0)
            {
                NoneTips.SetActive(true);
                return;
            }

            allDatas.AddRange(datas);
            Adapter.Data.ResetItems(datas.Count, false);
            Adapter.OnItemsUpdated?.Invoke();
        }

        /// <summary>
        /// 可以调整单元格数据内容,实现单元格大小分类致等特殊需求
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private PropResInfo CreateNewModel(int index)
        {
            if (index >= 0 && index < allDatas.Count)
            {
                return allDatas[index];
            }

            return new PropResInfo();
        }

        private void OnReceivedNewModelsForInsert(List<PropResInfo> newDatas)
        {
            if (newDatas == null || newDatas.Count == 0)
            {
                Adapter.OnItemsUpdated?.Invoke();
                return;
            }

            Adapter.Data.List.AddRange(newDatas);
            Adapter.Refresh(false);
        }

        private void OnReceivedPublishModelsForInsert(bool isSuccess, List<PropResInfo> newDatas)
        {
            if (newDatas == null || newDatas.Count == 0)
            {
                Adapter.OnItemsUpdated?.Invoke();
                return;
            }

            Adapter.Data.List.AddRange(newDatas);
            Adapter.Refresh(false);
        }

        private void OnPullReleased(float sign)
        {

            if (sign < 0)
            {
                DataLoader.GetPublishList(OnReceivedPublishModelsForInsert);
            }
        }
        
        private void OnOwnedItemClick(PropResInfo itemData)
        {
            CreateUgcAsset(itemData);
        }
        
        private void CreateUgcAsset(PropResInfo itemData)
        {
            if (itemData != null && itemData.propInfo != null)
            {
                GlobalNodeManager.Inst.Get<PropManager>().Create(itemData.propInfo, behaviour =>
                {
                    if (behaviour != null)
                    {
                        UI.Manager.InputHandlerManager.Inst.SelectEntity(behaviour.entity);
                    }
                    CloseSelf();
                });
            }
            else
            {
                LoggerUtils.LogError("itemData 类型不对或为null:");
            }
        }
    }
}
