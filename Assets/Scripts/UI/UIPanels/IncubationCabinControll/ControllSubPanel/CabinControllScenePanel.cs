using System;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using GameData.BaseInfo;
using Message;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.IncubationCabin
{
    /// <summary>
    /// 控制台场景子界面，通过 BoxScene_DataLoader 从服务器拉取 BoxScene 列表，
    /// 由 BoxScene_Adapter（OSA Grid）驱动列表展示，并提供切换到角色控制面板的入口。
    /// </summary>
    public class CabinControllScenePanel : MonoBehaviour
    {
        [SerializeField] private Button PlayerBtn;    // 切换到角色控制面板的按钮

        /// <summary>OSA Grid 适配器，管理场景列表的数据与视图</summary>
        public BoxScene_Adapter Adapter;

        /// <summary>OSA 是否已完成初始化（仅首次激活时执行一次）</summary>
        private bool _isOSAInitialized = false;

        /// <summary>服务器数据加载器，负责分页拉取 BoxScene 列表</summary>
        public BoxScene_DataLoader DataLoader;

        /// <summary>上拉刷新控制器（可选），未绑定时跳过上拉逻辑</summary>
        public PullToRefreshBehaviour refreshController;

        /// <summary>点击"角色"按钮时触发，由父级面板绑定</summary>
        public Action onPlayerClick;

        /// <summary>场景 Item 被选中时触发，携带 BoxSceneInfo 数据，由父级面板绑定</summary>
        public Action<object> onSceneSelect;

        #region 初始化

        /// <summary>
        /// 初始化面板：绑定按钮事件及回调。
        /// 由父级 IncubationCabinControll 的 InitUI 调用一次（OnCreate 阶段，此时 GO 尚未激活）。
        /// OSA 初始化及首次数据加载推迟到 OnEnable，确保 activeInHierarchy == true。
        /// </summary>
        public void InitUI()
        {
            PlayerBtn.onClick.AddListener(() => onPlayerClick?.Invoke());

            // 绑定上拉加载更多（refreshController 为可选组件）
            if (refreshController != null)
            {
                refreshController.OnRefreshWithSlideUp.AddListener(OnPullReleased);
                Adapter.OnItemsUpdated.AddListener(refreshController.HideGizmo);
            }

            // 将 onSceneSelect 桥接到 Adapter 的强类型回调
            Adapter.SetOnSceneItemSelectAct((info) => onSceneSelect?.Invoke(info));

            // 绑定「去购买」Item 的跳转逻辑：打开 AI 伙伴商店并定位到伙伴盒子页签
            Adapter.SetOnGoPurchaseClick(() =>
            {
                UIManager.Inst.OpenPanel(PanelId.AIPartnerShopPanel, AIPartnerTabSecond.PartnerBox);
            });

            // 订阅 Box 场景同步结果广播，同步成功后刷新列表选中态
            MessageHelper.AddListener<bool>(MessageName.OnBoxSceneSyncResult, OnBoxSceneSyncResult);

            // 订阅 UGC 购买成功广播，购买后重新拉取场景列表以展示新购入的场景
            MessageHelper.AddListener<string>(MessageName.OnBuyUgcItemSuccess, OnBuyUgcItemSuccess);
        }

        /// <summary>
        /// GameObject 激活时触发：首次激活时初始化 OSA Adapter，每次激活时刷新场景列表。
        /// OnEnable 保证 gameObject.activeInHierarchy == true，满足 OSA.Init() 的前提条件。
        /// </summary>
        private void OnEnable()
        {
            if (!_isOSAInitialized)
            {
                Adapter.Data = new SimpleDataHelper<BoxSceneInfo>(Adapter);
                Adapter.Init();
                _isOSAInitialized = true;
            }

            GetSceneListData();
        }

        #endregion

        #region 数据加载

        /// <summary>
        /// 重置分页并重新拉取第一页场景列表
        /// </summary>
        public void GetSceneListData()
        {
            DataLoader.ResetCookie();
            DataLoader.GetBoxSceneList(OnGetFirstPageDatas);
        }

        /// <summary>
        /// 首页数据到达回调：清空 Adapter 旧数据，在列表前插入两个固定特殊 Item，再填入服务器数据
        /// </summary>
        private void OnGetFirstPageDatas(List<BoxSceneInfo> sceneList)
        {
            // 重置 OSA 列表（count=0 清空所有已渲染 Cell）
            if (Adapter.IsInitialized)
            {
                Adapter.ResetItems(0);
            }

            if (sceneList == null)
            {
                sceneList = new List<BoxSceneInfo>();
            }

            // 第2位：默认场景（内部 sentinel id 用于选中态追踪，回调向外传递 id="" 表示清除场景）
            var defaultItem = new BoxSceneInfo
            {
                id = "__defaultScene__",
                specialType = BoxSceneSpecialType.DefaultScene
            };
            sceneList.Insert(0, defaultItem);

            // 第1位：去购买入口（点击后跳转 AIPartnerShopPanel 伙伴盒子页签，不改变选中状态）
            var goPurchaseItem = new BoxSceneInfo
            {
                id = "__goPurchase__",
                specialType = BoxSceneSpecialType.GoPurchase
            };
            sceneList.Insert(0, goPurchaseItem);

            Adapter.Data.ResetItems(sceneList);
            Adapter.OnItemsUpdated?.Invoke();
            // 刷新列表，由 IsItemActive() 根据 GetBoxScene() 驱动每个 Item 的选中态
            Adapter.Refresh();
        }

        /// <summary>
        /// 上拉触发的分页加载
        /// </summary>
        private void OnPullReleased()
        {
            DataLoader.GetBoxSceneList(OnReceivedMoreItems);
        }

        /// <summary>
        /// 分页数据到达回调：将新数据追加到列表末尾
        /// </summary>
        private void OnReceivedMoreItems(List<BoxSceneInfo> sceneList)
        {
            if (sceneList == null || sceneList.Count == 0)
            {
                Adapter.OnItemsUpdated?.Invoke();
                return;
            }

            Adapter.Data.List.AddRange(sceneList);
            Adapter.Refresh(false);
        }

        #endregion

        #region 状态刷新

        /// <summary>
        /// 刷新所有场景 Item 的选中状态，触发时机：MQTT 同步成功、列表初始化后
        /// </summary>
        public void RefreshSceneDefaultTags()
        {
            if (Adapter.IsInitialized)
            {
                Adapter.Refresh();
            }
        }

        /// <summary>
        /// Box 场景同步结果回调：同步成功后刷新列表选中态，使 SelectObj 切换到已同步的场景 Item
        /// </summary>
        private void OnBoxSceneSyncResult(bool isSuccess)
        {
            if (!isSuccess)
                return;

            RefreshSceneDefaultTags();
        }

        /// <summary>
        /// UGC 购买成功回调：购买后重新拉取场景列表以展示新购入的场景卡片
        /// </summary>
        private void OnBuyUgcItemSuccess(string ugcId)
        {
            // 面板尚未激活时跳过，等 OnEnable 时会主动拉取
            if (!_isOSAInitialized)
                return;

            GetSceneListData();
        }

        #endregion

        private void OnDestroy()
        {
            MessageHelper.RemoveListener<bool>(MessageName.OnBoxSceneSyncResult, OnBoxSceneSyncResult);
            MessageHelper.RemoveListener<string>(MessageName.OnBuyUgcItemSuccess, OnBuyUgcItemSuccess);
        }
    }
}
