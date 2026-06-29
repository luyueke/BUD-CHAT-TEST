using Game.BudBox;
using Message;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.IncubationCabin
{
    /// <summary>
    /// BOX BudBox 设备列表主面板，仅在绑定设备数量 ≥ 2 时由
    /// <see cref="CabinBoxManager.OpenBoxEntrance"/> 路由打开。
    /// 职责：展示已配对的 Box 列表，用户选中后跳转控制台。
    /// </summary>
    public class IncubationCabinBoxMain : BasePanel<IncubationCabinBoxMain>
    {
        [SerializeField] private Button backBtn;     // 返回按钮
        [SerializeField] private Button AddBoxBtn;   // 添加新 Box 按钮

        [SerializeField] private GameObject itemPrefab;  // BudBox 列表 Item 预制体
        [SerializeField] private Transform ContentTrans; // 列表容器

        /// <summary>Scroll View 上的 ScrollRect 组件，设备数 &gt;3 时启用，否则禁用</summary>
        [SerializeField] private ScrollRect _scrollRect;

        private readonly List<CabinBudBoxItem> _activeItems = new List<CabinBudBoxItem>(); // 当前激活的 Item
        private readonly List<CabinBudBoxItem> _itemPool = new List<CabinBudBoxItem>();    // Item 对象池

        #region 生命周期

        public override void OnCreate()
        {
            base.OnCreate();
            // 监听设备列表刷新事件（如添加新 Box 后重建列表）
            MessageHelper.AddListener(MessageName.OnBudBoxListInit, OnBudBoxListInit);

            backBtn.onClick.AddListener(CloseSelf);
            AddBoxBtn.onClick.AddListener(() =>
            {
                UIManager.Inst.OpenPanel(PanelId.IncubationCabinLinkBox);
            });

            // 面板打开时设备数据已由 OpenBoxEntrance 加载完毕，直接刷新列表
            UpdateBudBoxList();
        }

        #endregion

        #region 消息回调

        /// <summary>
        /// 设备列表刷新回调（添加新 Box 后触发），重建列表展示
        /// </summary>
        private void OnBudBoxListInit()
        {
            UpdateBudBoxList();
        }

        #endregion

        #region 列表更新

        /// <summary>
        /// 回收旧 Item 并根据最新设备列表重建展示内容，
        /// 同时补全空 deviceName 的默认名称，并动态控制 ScrollRect 激活状态
        /// </summary>
        private void UpdateBudBoxList()
        {
            RecycleAllItems();

            var budBoxList = CabinBoxManager.Inst.GetBudBoxList();

            for (int i = 0; i < budBoxList.Count; i++)
            {
                // 服务端未返回设备名时，按顺序应用默认名
                if (string.IsNullOrEmpty(budBoxList[i].deviceName))
                {
                    budBoxList[i].deviceName = GetDefaultBoxName(i);
                }

                CabinBudBoxItem item = GetItemFromPool();
                item.onSelectClick = OnBudBoxSelected;
                item.Init(budBoxList[i]);
                item.gameObject.SetActive(true);
                _activeItems.Add(item);
            }

            // 超过 3 台时才允许滑动，否则禁用 ScrollRect（固定布局）
            if (_scrollRect != null)
            {
                _scrollRect.enabled = budBoxList.Count > 3;
            }
        }

        /// <summary>
        /// 根据设备序号生成默认名称。
        /// 第 1 台：{昵称}的BUD BOX；第 2 台起：{昵称}的BUD BOX_{序号}
        /// </summary>
        /// <param name="index">设备在列表中的 0-based 序号</param>
        /// <returns>默认设备显示名称</returns>
        private static string GetDefaultBoxName(int index)
        {
            var nickname = AccountDataManager.Inst.UserInfo.nickname;

            if (index == 0)
                return $"{nickname}的BUD BOX";

            return $"{nickname}的BUD BOX_{index + 1}";
        }

        /// <summary>
        /// 从对象池取出一个 Item，池为空时实例化新对象
        /// </summary>
        private CabinBudBoxItem GetItemFromPool()
        {
            while (_itemPool.Count > 0)
            {
                var item = _itemPool[_itemPool.Count - 1];
                _itemPool.RemoveAt(_itemPool.Count - 1);

                if (item != null)
                    return item;
            }

            return Instantiate(itemPrefab, ContentTrans).GetComponent<CabinBudBoxItem>();
        }

        /// <summary>
        /// 选中 Box 后发起连接
        /// </summary>
        private void OnBudBoxSelected(CabinBudBoxData data)
        {
            CabinBoxManager.Inst.JumpToBox(data);
        }

        /// <summary>
        /// 将所有激活 Item 回收入对象池
        /// </summary>
        private void RecycleAllItems()
        {
            for (int i = 0; i < _activeItems.Count; i++)
            {
                _activeItems[i].onSelectClick = null;
                _activeItems[i].gameObject.SetActive(false);
                _itemPool.Add(_activeItems[i]);
            }

            _activeItems.Clear();
        }

        protected override void OnDestroy()
        {
            MessageHelper.RemoveListener(MessageName.OnBudBoxListInit, OnBudBoxListInit);
            RecycleAllItems();
            CabinBoxManager.Inst.DisconnectMqtt();
            base.OnDestroy();
        }

        #endregion

        [Button("绑定测试")]
        void BindTest()
        {
            // CabinBoxManager.Inst.BindCabinBox("BUD-330076C9");  //013
            CabinBoxManager.Inst.BindCabinBox("BUD-13EDD13E");  //013
            
            // CabinBoxManager.Inst.BindCabinBox("BUD-269BF2A9");  //risa
            
        }
        [Button("解绑测试")]
        void UnBindTest()
        {
            CabinBoxManager.Inst.UnbindBudBox("BUD-11C68153");

                // MessageHelper.Broadcast(MessageName.OnBoxDeviceStateChanged, "BUD-70029D72");
        }
    }
}
