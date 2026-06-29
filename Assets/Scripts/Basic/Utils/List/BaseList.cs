using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace Fsbm.Runtime
{
    /// <summary>
    /// 列表
    /// </summary>

    public class BaseList : GBehaviour
    {
        [SerializeField]
        private GameObject _itemRenderer;

        [SerializeField]
        private bool _doubleClickEnabled = false;
        [SerializeField]
        private bool _longPressEnabled = false;
        [SerializeField]
        private bool _selectable = false;
        [SerializeField]
        private bool _isMultipleSelection = false;

        [HideInInspector]
        public UEvent<string, int, object, object> onItemEvent = new UEvent<string, int, object, object>();
        /** 列表项被点击 */
        [HideInInspector]
        public UEvent<int, object> onItemClick = new UEvent<int, object>();
        [HideInInspector]
        /** 列表项被双点击 */
        public UEvent<int, object> onItemDoubleClick = new UEvent<int, object>();
        [HideInInspector]
        public UEvent<int, object> onItemLongPress = new UEvent<int, object>();
        [HideInInspector]
        public UEvent onSelectChanged = new UEvent();
        [HideInInspector]
        public Action<int, object> ClickAction;
        [HideInInspector]
        public Func<int,object, bool> IsClickAction;

        protected List<object> dataList = new List<object>();
        protected int appendIndex = 0;
        protected List<ItemRenderer> itemRenderers = new List<ItemRenderer>();

        protected List<int> _selectIndexs = new List<int>();

        public object content;

        protected override void Init()
        {
            base.Init();
            if (_itemRenderer != null && BootTool.IsPrefabs(_itemRenderer) == false)
                _itemRenderer.transform.localPosition = new Vector3(-10000, -10000);
        }

        public bool selectable
        {
            get { return _selectable; }
            set
            {
                if (_selectable == value)
                    return;
                _selectable = value;
                if (_selectable == false && _selectIndexs.Count > 0)
                {
                    _selectIndexs.Clear();
                    UpdateSelectItem();
                    onSelectChanged.Invoke();
                }
            }
        }

        public void SetableSelect(bool isAble) {
            if (_selectable == isAble)
                return;
            _selectable = isAble;
        }

        public bool isMultipleSelection
        {
            get { return _isMultipleSelection; }
            set
            {
                if (_isMultipleSelection == value)
                    return;
                _isMultipleSelection = value;
                if (_isMultipleSelection == false && _selectIndexs.Count > 1)
                {
                    _selectIndexs.RemoveRange(1, _selectIndexs.Count - 1);
                    UpdateSelectItem();
                    onSelectChanged.Invoke();
                }
            }
        }

        public bool doubleClickEnabled
        {
            get { return _doubleClickEnabled; }
            set { _doubleClickEnabled = value; }
        }
        public bool longPressEnabled
        {
            get { return _longPressEnabled; }
            set
            {
                if (_longPressEnabled == value)
                    return;
                _longPressEnabled = value;

                for (int i = 0; i < itemRenderers.Count; i++)
                {
                    itemRenderers[i].CheckOnLongPressEnabled();
                }

            }
        }
        public virtual GameObject itemRenderer
        {
            get
            {
                return _itemRenderer;
            }
            set
            {
                if (_itemRenderer == value)
                    return;
                appendIndex = 0;
                GameObject tempItemRenderer = value;
                _itemRenderer = value;
                if (_itemRenderer != null && BootTool.IsPrefabs(_itemRenderer) == false)
                {
                    _itemRenderer.transform.localPosition = new Vector3(-1000, -1000);
                    if (tempItemRenderer != null && tempItemRenderer.activeSelf == false)
                        _itemRenderer.SetActive(false);
                }
                InvalidView();
            }
        }

        public int selectIndex
        {
            get { return _selectIndexs.Count > 0 ? _selectIndexs[0] : -1; }
            set
            {
                if (_selectIndexs.Count == 1 && _selectIndexs[0] == value)
                    return;
                _selectIndexs.Clear();
                if (value >= 0)
                    _selectIndexs.Add(value);
                UpdateSelectItem();
                onSelectChanged.Invoke();
            }
        }

        /// <summary>
        /// 增加设置index方法 支持是否派发事件
        /// </summary>
        /// <param name="value">index值</param>
        /// <param name="isDispatchEvent">是否派发事件</param>
        public void SelectIndex(int value, bool isDispatchEvent = true)
        {

            if (_selectIndexs.Count == 1 && _selectIndexs[0] == value)
                return;
            _selectIndexs.Clear();
            if (value >= 0)
                _selectIndexs.Add(value);
            UpdateSelectItem();
            if (isDispatchEvent) onSelectChanged.Invoke();
        }
        public int[] selectIndexs
        {
            get { return _selectIndexs.ToArray(); }
            set
            {
                _selectIndexs.Clear();
                if (value != null && value.Length > 0)
                {
                    for (int i = 0; i < value.Length; i++)
                    {
                        if (value[i] < dataList.Count && value[i] >= 0)
                            _selectIndexs.Add(value[i]);
                    }
                }

                UpdateSelectItem();
                onSelectChanged.Invoke();
            }
        }

        internal void ToggleSeleced(int index)
        {
            if (_selectIndexs.Contains(index))
            {
                _selectIndexs.Remove(index);
                UpdateSelectItem();
                onSelectChanged.Invoke();
            }
            else
            {
                if (_isMultipleSelection == false)
                    _selectIndexs.Clear();

                _selectIndexs.Add(index);
                UpdateSelectItem();
                onSelectChanged.Invoke();
            }
        }

        public virtual IList datas
        {
            get
            {
                return dataList;
            }
            set
            {
                dataList.Clear();
                if (value != null && value.Count > 0)
                {

                    for (int i = 0, imax = value.Count; i < imax; i++)
                    {
                        dataList.Add(value[i]);
                    }
                }
                appendIndex = 0;
                InvalidView();
            }
        }

        public virtual void AddData(object value)
        {
            dataList.Add(value);
            InvalidView();
        }
        public virtual void AddDataAt(object value, int index)
        {
            appendIndex = Mathf.Min(appendIndex, index);
            dataList.Insert(index, value);
            InvalidView();
        }
        public virtual void RemoveData(int index)
        {
            appendIndex = Mathf.Min(appendIndex, index);
            dataList.RemoveAt(index);
            if (_selectIndexs.Contains(index))
                _selectIndexs.Remove(index);
            InvalidView();
        }
        public override void Refresh()
        {
            // appendIndex = 0;
            base.Refresh();

        }

        public virtual void RefreshDatas()
        {
            appendIndex = 0;
            base.Refresh();
        }
        public override void Clear()
        {
            base.Clear();
            onItemEvent.RemoveAllListeners();
            onItemClick.RemoveAllListeners();
            onItemDoubleClick.RemoveAllListeners();
            onItemLongPress.RemoveAllListeners();
            onSelectChanged.RemoveAllListeners();
        }

        private void UpdateSelectItem()
        {
            for (int i = _selectIndexs.Count - 1; i >= 0; i--)
            {
                if (_selectIndexs[i] > dataList.Count && _selectIndexs[i] < 0)
                    _selectIndexs.RemoveAt(i);
            }
            for (int i = 0; i < itemRenderers.Count; i++)
            {
                ItemRenderer renderer = itemRenderers[i];
                renderer.selected = _selectIndexs.Contains(renderer.index);
            }

        }

    }
}
