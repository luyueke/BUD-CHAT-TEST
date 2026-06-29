using Message;
using System.Collections.Generic;
using System.Linq;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.IncubationCabin
{
    /// <summary>
    /// Author:
    /// Desc: 导入BOX角色弹窗。
    ///       三个 Tab（官方角色 / 我创作的 / 已购买）共享同一角色列表，
    ///       切换 Tab 时刷新列表并更新底部按钮可见性；
    ///       选中角色后 OnBtn 变为可交互状态。
    /// Date: 26-04-09
    /// </summary>
    public class IncubationImportPanel : BasePanel<IncubationImportPanel>
    {
        [Header("关闭")]
        [SerializeField] private Button CloseBtn;

        [Header("Tab Toggles")]
        [SerializeField] private Toggle OfficialToggle;    // 官方角色
        [SerializeField] private Toggle MyCreatedToggle;   // 我创作的
        [SerializeField] private Toggle PurchasedToggle;   // 已购买

        [Header("列表")]
        [SerializeField] private GameObject ItemPrefab;
        [SerializeField] private Transform ItemContainer;

        [Header("底部按钮")]
        [SerializeField] private Button OnBtn;             // 确定（所有 Tab 都显示，无选中时置灰）
        [SerializeField] private Image OnBtnImage;         // 确定按钮背景图组件
        [SerializeField] private Sprite OnBtnNormalSprite; // 正常态图片
        [SerializeField] private Sprite OnBtnGraySprite;   // 置灰态图片
        [SerializeField] private Button GoBtn;             // 前往创作（仅"我创作的"）
        [SerializeField] private Button GetBtn;            // 获取更多（仅"已购买"）

        private enum TabType { Official, MyCreated, Buy }
        private TabType _currentTab;

        private CabinCharacterUgcInfo _selectedData;                          // 当前选中的角色数据
        private CabinCharacterCardItem _currentSelectedItem;             // 当前选中的列表 Item

        private readonly List<CabinCharacterCardItem> _itemList = new List<CabinCharacterCardItem>(); // 当前展示的 Item
        private readonly List<CabinCharacterCardItem> _itemPool = new List<CabinCharacterCardItem>(); // 复用对象池

        #region 生命周期

        public override void OnCreate()
        {
            CloseBtn.onClick.AddListener(CloseSelf);

            // Tab 切换：只响应选中事件，避免取消选中时重复触发
            OfficialToggle.onValueChanged.AddListener(on => { if (on) OnTabSwitch(TabType.Official); });
            MyCreatedToggle.onValueChanged.AddListener(on => { if (on) OnTabSwitch(TabType.MyCreated); });
            PurchasedToggle.onValueChanged.AddListener(on => { if (on) OnTabSwitch(TabType.Buy); });

            OnBtn.onClick.AddListener(OnConfirmClick);
            GoBtn.onClick.AddListener(OnGoCreateClick);
            GetBtn.onClick.AddListener(OnGetMoreClick);
            MessageHelper.AddListener(MessageName.OnCabinPublishListChange, OnCabinPublishListChange);
        }

        public override void OnShow(params object[] args)
        {
            _selectedData = null;
            _currentSelectedItem = null;

            // 若 Toggle 已处于 on 状态，onValueChanged 不会再触发，手动调用一次
            if (OfficialToggle.isOn)
                OnTabSwitch(TabType.Official);
            else
                OfficialToggle.isOn = true;
        }

        private void OnCabinPublishListChange()
        {
            FetchAndRefreshList();
        }

        public override void OnHidden()
        {
            ClearAllItems();
            _selectedData = null;
            _currentSelectedItem = null;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            MessageHelper.RemoveListener(MessageName.OnCabinPublishListChange, OnCabinPublishListChange);
        }

        #endregion

        #region Tab 切换

        /// <summary>
        /// 切换 Tab：清空选中状态，刷新按钮和列表
        /// </summary>
        private void OnTabSwitch(TabType tab)
        {
            _currentTab = tab;
            _selectedData = null;
            _currentSelectedItem = null;
            RefreshButtons();
            FetchAndRefreshList();
        }

        #endregion

        #region 数据拉取

        /// <summary>
        /// 根据当前 Tab 拉取对应角色列表数据
        /// </summary>
        private void FetchAndRefreshList()
        {
            switch (_currentTab)
            {
                case TabType.Official:
                    // TODO: 拉取官方角色列表接口，回调中传入数据
                    RefreshList(null);
                    break;
                case TabType.MyCreated:
                    CabinNetManager.Inst.GetNetCabinCharacterPublishList(CabinPurchasedType.Published, (isSuccess, list) =>
                    {
                        if (!isSuccess || this == null) return;
                        RefreshList(list);
                    });
                    break;
                case TabType.Buy:
                    // TODO: 拉取已购买角色列表接口，回调中传入数据
                    CabinNetManager.Inst.GetNetCabinCharacterPublishList(CabinPurchasedType.UGCShop, (isSuccess, list) =>
                    {
                        if (!isSuccess || this == null) return;
                        RefreshList(list);
                    });
                    break;
            }
        }

        /// <summary>
        /// 用数据列表刷新 Item 展示，先归还旧 Item 到对象池
        /// </summary>
        private void RefreshList(List<CabinPublishData> dataList)
        {
            foreach (var item in _itemList)
                ReturnToPool(item);
            _itemList.Clear();
            _currentSelectedItem = null;

            if (dataList == null) return;

            foreach (var data in dataList)
            {
                var item = GetFromPool();
                item.SetData(data.characterInfo, (d) => OnItemSelected(item, d));
                _itemList.Add(item);
            }
        }

        #endregion

        #region 选中

        /// <summary>
        /// 点击列表 Item 时：取消上一个选中，选中当前 Item，并刷新底部按钮
        /// </summary>
        private void OnItemSelected(CabinCharacterCardItem clickedItem, CabinCharacterBaseInfo data)
        {
            if (_currentSelectedItem != null)
                _currentSelectedItem.SetSelected(false);

            _currentSelectedItem = clickedItem;
            _currentSelectedItem.SetSelected(true);
            _selectedData = (CabinCharacterUgcInfo)data;

            RefreshButtons();
        }

        #endregion

        #region 按钮刷新

        /// <summary>
        /// 刷新底部按钮状态：特殊按钮按 Tab 显示，确定按钮根据是否有选中切换可交互状态
        /// </summary>
        private void RefreshButtons()
        {
            GoBtn.gameObject.SetActive(_currentTab == TabType.MyCreated);
            GetBtn.gameObject.SetActive(_currentTab == TabType.Buy);

            bool hasSelection = _selectedData != null;
            OnBtn.interactable = hasSelection;
            OnBtnImage.sprite = hasSelection ? OnBtnNormalSprite : OnBtnGraySprite;
        }

        #endregion

        #region 按钮点击

        /// <summary>
        /// 确定按钮：将选中角色写入BOX并关闭弹窗
        /// </summary>
        private void OnConfirmClick()
        {
            if (_selectedData == null)
                return;
            // TODO: 后续改为调用网络接口提交，临时直接写入 IncubationCabinControll._data
            CabinBoxManager.Inst.ImportBoxCharacterData(_selectedData);
            CloseSelf();
        }

        /// <summary>
        /// 前往创作按钮：打开草稿箱面板
        /// </summary>
        private void OnGoCreateClick()
        {
            UIManager.Inst.OpenPanel(PanelId.IncubationCabinDraftBox);
        }

        /// <summary>
        /// 获取更多按钮：跳转到角色购买页面
        /// </summary>
        private void OnGetMoreClick()
        {
            // TODO: 跳转已购买角色的获取页面（商城等）
            UIManager.Inst.OpenPanel(PanelId.AIPartnerShopPanel);
        }

        #endregion

        #region 对象池

        /// <summary>
        /// 从对象池取出一个 Item，池为空时实例化新对象
        /// </summary>
        private CabinCharacterCardItem GetFromPool()
        {
            if (_itemPool.Count > 0)
            {
                var pooled = _itemPool[_itemPool.Count - 1];
                _itemPool.RemoveAt(_itemPool.Count - 1);
                pooled.gameObject.SetActive(true);
                return pooled;
            }
            var go = Instantiate(ItemPrefab, ItemContainer);
            go.SetActive(true);
            return go.GetComponent<CabinCharacterCardItem>();
        }

        /// <summary>
        /// 将 Item 归还对象池：清理数据并隐藏
        /// </summary>
        private void ReturnToPool(CabinCharacterCardItem item)
        {
            item.ClearData();
            item.gameObject.SetActive(false);
            _itemPool.Add(item);
        }

        /// <summary>
        /// 彻底销毁所有 Item（列表和对象池），用于面板关闭时完全释放
        /// </summary>
        private void ClearAllItems()
        {
            foreach (var item in _itemList)
            {
                item.ClearData();
                Destroy(item.gameObject);
            }
            _itemList.Clear();

            foreach (var item in _itemPool)
                Destroy(item.gameObject);
            _itemPool.Clear();

            _currentSelectedItem = null;
        }

        public override void OnWindowBeFocused() { }
        public override void OnWindowPop() { }

        #endregion
    }
}
