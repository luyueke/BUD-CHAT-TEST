using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#pragma warning disable 649
#pragma warning disable 414

namespace Fsbm.Runtime
{
    [RequireComponent(typeof(ScrollRect))]
    public class ScrollList : BaseList
    {
        //********   需要在编辑器里配置的项 **********

        [SerializeField]
        [Tooltip("列数")]
        private int _column = 1;

        [SerializeField]
        [Tooltip("行数")]
        private int _row = 1;

        [SerializeField]
        [Tooltip("列表项固定尺寸,若要动态尺寸。列表项上需要挂上组件,若设为false，girdCount自动为1")]
        private bool _isItemFixedSize = true;

        [SerializeField]
        [Tooltip("列表项的宽")]
        private float _itemWidth = 0;

        [SerializeField]
        private float _itemHeight = 0;

        [SerializeField]
        [Tooltip("列表渲方式")]
        private ItemRenderMode _itemRenderMode = 0;

        [SerializeField]
        private int _maxDataCount = -1;

        [SerializeField]
        private int _maxDeleteCount = 1;

        [SerializeField]
        private Direction _direction;

        [SerializeField]
        private float _horizontalGap = 0;

        [SerializeField]
        private float _verticalGap = 0;

        private string __paddingHead = "Padding";

        [SerializeField]
        private float _paddingLeft;

        [SerializeField]
        private float _paddingTop;

        [SerializeField]
        private float _paddingRight;

        [SerializeField]
        private float _paddingBottom;

        [SerializeField]
        [Tooltip("首项")]
        private RectTransform _headRenderer = null;

        [SerializeField]
        [Tooltip("尾项")]
        private RectTransform _endRenderer = null;

        [SerializeField]
        [Tooltip("加载追加")]
        private RectTransform _loading = null;

        [SerializeField]
        private bool _isShowHead = false;

        [SerializeField]
        private bool _isShowEnd = false;

        [SerializeField]
        [Tooltip("列表项的宽")]
        private bool _isItemWidthFill = false;

        [SerializeField]
        private bool _isItemHeightFill = false;

        [SerializeField]
        [Tooltip("更新自动拉到最后")]
        private bool _isAutoScrollLast = false;

        [SerializeField]
        private bool _isCacheItemInPool = false;

        [SerializeField]
        private bool _autoSize = false;

        [SerializeField]
        private Vector2 _pivot = new Vector2(0, 1);

        public UEvent onScroll = new UEvent();

        /// <summary>
        /// 列表滑动到最底部时回调函数
        /// </summary>
        public Action OnMoveToEndCallback;

        public Action<ItemRenderer> OnItemUpdate;

        //**********  私有变量  *************
        private List<ItemRenderer> _tempRenderers = new List<ItemRenderer>();

        private List<float> _lineSizes = new List<float>();
        private List<Rect> _itemSizes = new List<Rect>();
        private float _startX;
        private float _startY;

        private ItemRenderer _tempListItemRenderer;

        private RectTransform _scrollContent;
        private ScrollRect _scrollRect;

        // private Vector2 _oldScrollVector = new Vector2();
        private bool _isShowLoading = false;

        private float _scrollWidth;
        private float _scrollHeight;

        protected Action<int> ResetPosAction;
        protected int resetIndex;

        //**********  对外接口  *************
        public List<ItemRenderer> Items
        {
            get { return itemRenderers; }
        }

        /// <summary>
        /// 是否显示首项
        /// </summary>
        public bool isShowHead
        {
            get
            {
                return _isShowHead;
            }
            set
            {
                if (_isShowHead == value)
                    return;
                _isShowHead = value;
                appendIndex = 0;
                if (_headRenderer != null)
                    InvalidView();
            }
        }

        /// <summary>
        /// 是否显示尾项
        /// </summary>
        public bool isShowEnd
        {
            get
            {
                return _isShowEnd;
            }
            set
            {
                if (_isShowEnd == value)
                    return;
                _isShowEnd = value;
                if (_endRenderer != null)
                    InvalidView();
            }
        }

        public bool isShowLoading
        {
            get
            {
                return _isShowLoading;
            }
            set
            {
                if (_isShowLoading == value)
                    return;
                _isShowLoading = value;
                if (_loading != null)
                    InvalidView();
            }
        }

        /// <summary>
        /// 方向
        /// </summary>
        public Direction direction
        {
            get
            {
                return _direction;
            }
            set
            {
                if (_direction == value)
                    return;
                appendIndex = 0;
                _direction = value;
                InvalidView();
            }
        }

        public float horizontalGap
        {
            get
            {
                return _horizontalGap;
            }
            set
            {
                if (_horizontalGap == value)
                    return;
                _horizontalGap = value;
                appendIndex = 0;
                InvalidView();
            }
        }

        public float verticalGap
        {
            get
            {
                return _verticalGap;
            }
            set
            {
                if (_verticalGap == value)
                    return;
                _verticalGap = value;
                appendIndex = 0;
                InvalidView();
            }
        }

        public float paddingTop
        {
            get
            {
                return _paddingTop;
            }
            set
            {
                if (_paddingTop == value)
                    return;
                _paddingTop = value;
                appendIndex = 0;
                InvalidView();
            }
        }

        public float paddingLeft
        {
            get
            {
                return _paddingLeft;
            }
            set
            {
                if (_paddingLeft == value)
                    return;
                _paddingLeft = value;
                appendIndex = 0;
                InvalidView();
            }
        }

        public float paddingRight
        {
            get
            {
                return _paddingRight;
            }
            set
            {
                if (_paddingRight == value)
                    return;
                _paddingRight = value;
                InvalidView();
            }
        }

        public float paddingBottom
        {
            get
            {
                return _paddingBottom;
            }
            set
            {
                if (_paddingBottom == value)
                    return;
                _paddingBottom = value;
                InvalidView();
            }
        }

        /// <summary>
        ///
        /// </summary>
        public int column
        {
            get
            {
                return _column;
            }
            set
            {
                if (_column == value)
                    return;
                _column = value;
                if (_direction == Direction.vertical)
                {
                    appendIndex = 0;
                    InvalidView();
                }
            }
        }

        public int row
        {
            get
            {
                return _row;
            }
            set
            {
                if (_row == value)
                    return;
                _row = value;
                if (_direction == Direction.horizontal)
                {
                    appendIndex = 0;
                    InvalidView();
                }
            }
        }

        /// <summary>
        /// 列表项固定尺寸,若要动态尺寸。列表项上需要挂上组件,若设为false，girdCount自动为1
        /// </summary>
        public bool isItemFixedSize
        {
            get
            {
                return _isItemFixedSize;
            }
            set
            {
                if (_isItemFixedSize == value)
                    return;
                _isItemFixedSize = value;
                appendIndex = 0;
                InvalidView();
            }
        }

        public bool isItemHeightFill
        {
            get
            {
                return _isItemHeightFill;
            }
            set
            {
                if (_isItemHeightFill == value)
                    return;
                _isItemHeightFill = value;
                appendIndex = 0;
                InvalidView();
            }
        }

        public bool isItemWidthFill
        {
            get
            {
                return _isItemWidthFill;
            }
            set
            {
                if (_isItemWidthFill == value)
                    return;
                _isItemWidthFill = value;
                appendIndex = 0;
                InvalidView();
            }
        }

        public float itemWidth
        {
            get
            {
                return _itemWidth;
            }
            set
            {
                if (_itemWidth == value)
                    return;
                _itemWidth = value;
                if (_isItemFixedSize)
                {
                    appendIndex = 0;
                    InvalidView();
                }
            }
        }

        public float itemHeight
        {
            get
            {
                return _itemHeight;
            }
            set
            {
                if (_itemHeight == value)
                    return;
                _itemHeight = value;
                if (_isItemFixedSize)
                {
                    appendIndex = 0;
                    InvalidView();
                }
            }
        }

        public ItemRenderMode itemRenderMode
        {
            get
            {
                return _itemRenderMode;
            }
            set
            {
                if (_itemRenderMode == value)
                    return;
                _itemRenderMode = value;
                appendIndex = 0;
                InvalidView();
            }
        }

        public bool isAutoScrollLast
        {
            get
            {
                return _isAutoScrollLast;
            }
            set
            {
                if (_isAutoScrollLast == value)
                    return;
                _isAutoScrollLast = value;
                if (_isAutoScrollLast)
                {
                    int maxValue = maxScrollValue;
                    int currentVlaue = scrollValue;
                    if (maxValue > currentVlaue)
                        ScrollTo(maxValue);
                }
            }
        }

        public bool isCacheItemInPool
        {
            get
            {
                return _isCacheItemInPool;
            }
            set
            {
                _isCacheItemInPool = value;
            }
        }

        public bool autoSize
        {
            get
            {
                return _autoSize;
            }
            set
            {
                if (_autoSize == value)
                    return;
                _autoSize = value;
                RectTransform rect = transform as RectTransform;
                if (_autoSize)
                    rect.sizeDelta = _scrollContent.sizeDelta;
            }
        }

        public override IList datas
        {
            get
            {
                return base.datas;
            }
            set
            {
                if (CheckAnim(value)) return;
                _lineSizes.Clear();
                _itemSizes.Clear();
                base.datas = value;
            }
        }

        public int maxScrollValue
        {
            get
            {
                float value = 0;
                if (_scrollContent != null)
                {
                    if (_direction == Direction.horizontal)
                        value = _scrollContent.rect.width - _scrollRect.viewport.rect.width;
                    else
                        value = _scrollContent.rect.height - _scrollRect.viewport.rect.height;
                }
                if (value < 0)
                    value = 0;
                return (int)value;
            }
        }

        public int scrollValue
        {
            get
            {
                float value = 0;
                if (_scrollContent != null)
                {
                    if (_direction == Direction.horizontal)
                        value = -_scrollContent.localPosition.x;
                    else
                        value = _scrollContent.localPosition.y;
                }
                if (value < 0)
                    value = 0;
                return (int)value;
            }
            set
            {
                if (_scrollContent != null)
                {
                    if (_direction == Direction.horizontal)
                        _scrollContent.localPosition = new Vector2(-value, _scrollContent.localPosition.y);
                    else
                        _scrollContent.localPosition = new Vector2(_scrollContent.localPosition.x, value);
                }
            }
        }

        //***************  公开方法 ****************

        public void UpdateItem(int index)
        {
            if (dataList != null && dataList.Count > index)
            {
                object data = dataList[index];
                UpdateItem(index, data);
            }
        }

        public void UpdateItem(int index, object data)
        {
            if (dataList != null && dataList.Count > index)
            {
                dataList[index] = data;
                if (_isItemFixedSize)
                {
                    if (_itemRenderMode == ItemRenderMode.cycle)
                    {
                        for (int i = 0; i < itemRenderers.Count; i++)
                        {
                            ItemRenderer renderer = itemRenderers[i];
                            if (renderer.index == index)
                            {
                                renderer.data = data;
                                if (data != null)
                                    renderer.InvalidView();
                                break;
                            }
                        }
                    }
                    else
                    {
                        if (itemRenderers.Count > index)
                        {
                            ItemRenderer renderer = itemRenderers[index];
                            renderer.data = data;
                            if (data != null)
                                renderer.InvalidView();
                        }
                    }
                }
                else
                {
                    if (appendIndex > index)
                        appendIndex = index;
                    InvalidView();
                }
            }
        }

        public void ScrollToIndex(int value, bool isTop = true, bool noEffect = false, bool smooth = true)
        {
            if (scrollItems != null && datas != scrollItems)
            {
                Debug.Log("ScrollListTag ScrollToIndex PlayEnd");
                PlayEnd();
            }

            float maxValue = maxScrollValue;
            if (value < 0)
                ScrollTo(0, noEffect);
            else if (value > dataList.Count)
                ScrollTo(maxValue, noEffect);
            else if (maxValue > 0)
            {
                float d = 0;
                if (_itemRenderMode == ItemRenderMode.cycle)
                {
                    // Rect rect = _itemSizes[value];
                    if (_direction == Direction.horizontal)
                    {
                        d = _startX + (itemWidth + _horizontalGap) * value;
                        if (isTop == false)
                            d += itemWidth;
                    }
                    else
                    {
                        d = _startY + (itemHeight + _verticalGap) * value;
                        if (isTop == false)
                            d += itemHeight;
                    }
                }
                else
                {
                    ItemRenderer item = itemRenderers[value];
                    RectTransform rf = item.gameObject.transform as RectTransform;
                    if (_direction == Direction.horizontal)
                    {
                        d = GetPoint(rf).x;
                        if (isTop == false)
                            d += ((RectTransform)item.gameObject.transform).rect.width;
                    }
                    else
                    {
                        d = -GetPoint(rf).y;
                        if (isTop == false)
                            d += ((RectTransform)item.gameObject.transform).rect.height;
                    }
                }
                if (isTop == false)
                {
                    if (_direction == Direction.horizontal)
                        d = d - _scrollRect.viewport.rect.width;
                    else
                        d = d - _scrollRect.viewport.rect.height;
                }
                if (d < 0)
                    d = 0;
                else if (d > maxValue)
                    d = maxValue;
                ScrollTo(d, noEffect, 0.3f, smooth);
            }
            else
            {
                ScrollTo(0, noEffect);
            }
        }

        public void ScrollToIndex2(int value, bool isTop, bool noEffect, bool smooth, float time)
        {
            if (scrollItems != null && datas != scrollItems)
            {
                Debug.Log("ScrollListTag ScrollToIndex PlayEnd");
                PlayEnd();
            }

            float maxValue = maxScrollValue;
            if (value < 0)
                ScrollTo(0, noEffect);
            else if (value > dataList.Count)
                ScrollTo(maxValue, noEffect);
            else if (maxValue > 0)
            {
                float d = 0;
                if (_itemRenderMode == ItemRenderMode.cycle)
                {
                    // Rect rect = _itemSizes[value];
                    if (_direction == Direction.horizontal)
                    {
                        d = _startX + (itemWidth + _horizontalGap) * value;
                        if (isTop == false)
                            d += itemWidth;
                    }
                    else
                    {
                        d = _startY + (itemHeight + _verticalGap) * value;
                        if (isTop == false)
                            d += itemHeight;
                    }
                }
                else
                {
                    ItemRenderer item = itemRenderers[value];
                    RectTransform rf = item.gameObject.transform as RectTransform;
                    if (_direction == Direction.horizontal)
                    {
                        d = GetPoint(rf).x;
                        if (isTop == false)
                            d += ((RectTransform)item.gameObject.transform).rect.width;
                    }
                    else
                    {
                        d = -GetPoint(rf).y;
                        if (isTop == false)
                            d += ((RectTransform)item.gameObject.transform).rect.height;
                    }
                }
                if (isTop == false)
                {
                    if (_direction == Direction.horizontal)
                        d = d - _scrollRect.viewport.rect.width;
                    else
                        d = d - _scrollRect.viewport.rect.height;
                }
                if (d < 0)
                    d = 0;
                else if (d > maxValue)
                    d = maxValue;
                ScrollTo(d, noEffect, time, smooth);
            }
            else
            {
                ScrollTo(0, noEffect);
            }
        }

        public void ScrollTo(int value, bool noEffect = false)
        {
            ScrollTo((float)value, noEffect);
        }

        public void ScrollTo(float value, bool noEffect = false, float time = 0.3f, bool smooth = true)
        {
            time = smooth ? time : 0f;
            if (_direction == Direction.horizontal)
            {
                if (_scrollContent != null && _scrollContent.localPosition.x != value)
                {
                    _scrollContent.transform.DOKill();
                    _scrollRect.StopMovement();
                    if (noEffect)
                        _scrollContent.transform.localPosition = new Vector2(-value, _scrollContent.transform.localPosition.y);
                    else
                    {
                        var v = _scrollContent.transform.DOLocalMoveX(-value, time);
                        v.SetEase(Ease.Linear);
                    }

                }
            }
            else
            {
                //_content.localPosition = new Vector2(_content.localPosition.x, value);

                if (_scrollContent != null && _scrollContent.localPosition.y != value)
                {
                    _scrollContent.transform.DOKill();
                    _scrollRect.StopMovement();
                    if (noEffect)
                        _scrollContent.transform.localPosition = new Vector2(_scrollContent.transform.localPosition.x, value);
                    else
                    {
                         var v = _scrollContent.transform.DOLocalMoveY(value, time);
                        v.SetEase(Ease.Linear);
                    }
       
                }
            }
        }

        /// <summary>
        /// 手动刷新列表
        /// </summary>
        public void RefreshRenders()
        {
            for (int i = 0; i < itemRenderers.Count; i++)
            {
                ItemRenderer renderer = itemRenderers[i];
                renderer.Refresh();

                if (OnItemUpdate != null)
                {
                    OnItemUpdate.Invoke(renderer);
                }
            }
        }

        public ItemRenderer GetItemRender(int idx)
        {
            if (itemRenderers.Count > idx && idx >= 0)
            {
                ItemRenderer renderer = itemRenderers[idx];
                return renderer;
            }
            return null;
        }

        /// <summary>
        /// 获取数据数量
        /// </summary>
        /// <returns></returns>
        public int GetItemCount()
        {
            return itemRenderers.Count;
        }

        public object GetCurFirstItemData()
        {
            //获取目前视野里第一个Item
            float x = 0;
            ItemRenderer obj = null;
            foreach (var item in itemRenderers)
            {
                var tem = item.rectTransform.anchoredPosition.x;
                if (tem >= 0 && x <= tem)
                {
                    x = tem;
                    obj = item;
                }
            }
            if (obj != null)
            {
                return obj.data;
            }
            return null;
        }

        //********** 重载内容 ************

        protected override void Init()
        {
            base.Init();
            _scrollRect = GetComponent<ScrollRect>();
            _scrollContent = _scrollRect.content;

            _scrollRect.onValueChanged.AddListener(ScrollRectOnValueChange);
            if (_scrollContent != null)
            {
                _scrollContent.pivot = _pivot;
                _scrollContent.anchorMin = new Vector2(0, 1);
                _scrollContent.anchorMax = new Vector2(0, 1);
                _scrollContent.offsetMin = new Vector2(0, 0);

                ((RectTransform)(_scrollContent.transform)).anchoredPosition = new Vector3(0, 0, 0);
            }

            _scrollWidth = ((RectTransform)_scrollRect.transform).rect.size.x;
            _scrollHeight = ((RectTransform)_scrollRect.transform).rect.size.y;

            InitItemRenderer();
            InitHeadRenderer();
            InitEndRenderer();
        }

        protected override void UpdateView()
        {
            if (_bDestroyed)
            {
                Debug.LogWarning("[ScrollList] UpdateView, view already destroy");
                return;
            }

            base.UpdateView();
            if (_scrollContent == null)
            {
                Debug.LogError("scrollContent未设置");
                return;
            }
            if (itemRenderer == null)
            {
                Debug.LogError("itemRenderer未设置");
                return;
            }

            // float contentWidth = _paddingLeft + _paddingRight;
            //float contentHeight = _paddingTop + paddingBottom;
            if (dataList == null || dataList.Count == 0)
            {
                _lineSizes.Clear();
                _itemSizes.Clear();
                int n = itemRenderers.Count;
                for (int i = 0; i < n; i++)
                {
                    ItemRenderer item = itemRenderers[i];
                    item.container = null;
                    item.data = null;
                    item.selected = false;
                    if (_itemRenderMode != ItemRenderMode.cycle)
                    {
                        if (_isCacheItemInPool)
                            PoolManager.Instance.PushGameObject(item.gameObject, itemRenderer);
                        else
                            Destroy(item.gameObject);
                    }
                    else
                    {
                        _tempRenderers.Add(item);
                    }
                }

                itemRenderers.Clear();
                for (int i = 0; i < _tempRenderers.Count; i++)
                {
                    ItemRenderer item = _tempRenderers[i];
                    item.transform.localPosition = new Vector2(-10000, -10000);
                }
                return;
            }

            if (_itemWidth == 0)
                _itemWidth = itemRenderer.GetComponent<RectTransform>().rect.width;
            if (_itemHeight == 0)
                _itemHeight = itemRenderer.GetComponent<RectTransform>().rect.height;

            //最大数检查
            if (_maxDataCount > 0 && dataList.Count > _maxDataCount)
            {
                int deleCount = dataList.Count - _maxDataCount;
                deleCount = Mathf.Max(_maxDeleteCount, deleCount);
                int n = 0;
                //凑够一行才能删
                if (_direction == Direction.horizontal)
                {
                    n = deleCount / _row;
                    deleCount = n * _row;
                }
                else
                {
                    n = deleCount / _column;
                    deleCount = n * _column;
                }

                if (deleCount > 0)
                {
                    dataList.RemoveRange(0, deleCount);
                    appendIndex = 0;
                    float size = 0;
                    if (_isItemFixedSize)
                    {
                        if (_direction == Direction.horizontal)
                            size = n * (_itemWidth + _horizontalGap);
                        else
                            size = n * (_itemHeight + _verticalGap);
                    }
                    else
                    {
                        for (int i = 0; i < n; i++)
                        {
                            if (_lineSizes.Count > i)
                                size += _lineSizes[i] + (_direction == Direction.horizontal ? _horizontalGap : _verticalGap);
                        }
                    }

                    if (_direction == Direction.horizontal)
                    {
                        float x = _scrollContent.localPosition.x + size;
                        if (x > 0)
                            x = 0;
                        _scrollContent.localPosition = new Vector2(x, _scrollContent.localPosition.y);
                    }
                    else
                    {
                        float y = _scrollContent.localPosition.y - size;
                        if (y < 0)
                            y = 0;
                        _scrollContent.localPosition = new Vector2(_scrollContent.localPosition.x, y);
                    }
                }
            }

            UpdateList();
        }

        //****************** 私有 *****************

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (_isCacheItemInPool)
            {
                if (PoolManager.HasInstance() && itemRenderer != null && itemRenderer.IsPrefabs() == false)
                {
                    PoolManager.Instance.Clear(itemRenderer);
                }
            }
        }

        private void InitItemRenderer()
        {
            if (itemRenderer != null)
            {
                //   tf.offsetMin = new Vector2(0, 0);
                if (itemRenderer.transform.parent != null)
                    itemRenderer.SetActive(false);
            }
        }

        private void InitHeadRenderer()
        {
            if (_headRenderer != null)
            {
                _headRenderer.pivot = new Vector2(0, 1);
                _headRenderer.anchorMin = new Vector2(0, 1);
                _headRenderer.anchorMax = new Vector2(0, 1);
                // _headRenderer.offsetMin = new Vector2(0, 0);

                ItemRenderer item = BootTool.GetComponent<ItemRenderer>(_headRenderer.gameObject);
                item.container = this;
                item.index = -1;

                _headRenderer.gameObject.SetActive(false);
            }
        }

        private void InitEndRenderer()
        {
            if (_endRenderer != null)
            {
                _endRenderer.pivot = new Vector2(0, 1);
                _endRenderer.anchorMin = new Vector2(0, 1);
                _endRenderer.anchorMax = new Vector2(0, 1);
                // _endRenderer.offsetMin = new Vector2(0, 0);
                ItemRenderer item = BootTool.GetComponent<ItemRenderer>(_endRenderer.gameObject);
                item.container = this;
                item.index = -2;
                _endRenderer.gameObject.SetActive(false);
            }
        }

        private void UpdateList()
        {
            float x = paddingLeft;
            float y = -paddingTop;
            float maxSize = 0;

            if (_headRenderer && _headRenderer.gameObject.activeSelf != _isShowHead)
                _headRenderer.gameObject.SetActive(_isShowHead);

            if (appendIndex > 0)
            {
                if (_itemRenderMode != ItemRenderMode.cycle)
                {
                    RectTransform tf = itemRenderers[appendIndex - 1].rectTransform;
                    Vector2 tfpoint = GetPoint(tf);
                    x = tfpoint.x;
                    y = tfpoint.y;
                    ComputeItemXY(ref x, ref y, appendIndex - 1, tf.rect);
                }
                else
                {
                    if (_isItemFixedSize == false)
                    {
                        Rect rect = _itemSizes[appendIndex - 1];
                        x = rect.x;
                        y = rect.y;
                        ComputeItemXY(ref x, ref y, appendIndex - 1, rect);
                    }
                }
                if (_direction == Direction.horizontal)
                    maxSize = _scrollContent.rect.height;
                else
                    maxSize = _scrollContent.rect.width;
            }
            else if (_isShowHead && _headRenderer != null)
            {
                _headRenderer.offsetMin = new Vector2(x, y);
                if (_direction == Direction.horizontal)
                {
                    x = _paddingLeft + _headRenderer.rect.width + _horizontalGap;
                    y = -_paddingTop;
                    maxSize = _paddingTop + _paddingBottom + _headRenderer.rect.height;
                }
                else
                {
                    x = _paddingLeft;
                    y = -_paddingTop - _headRenderer.rect.height - _verticalGap;
                    maxSize = _paddingLeft + _paddingRight + _headRenderer.rect.width;
                }
                _startX = x;
                _startY = y;
            }
            else
            {
                _startX = x;
                _startY = y;
            }
            if (appendIndex <= 0)
            {
                _lineSizes.Clear();
                _itemSizes.Clear();
            }
            int n = dataList.Count;
            RectTransform scrollListRect = transform as RectTransform;
            if (_isItemFixedSize)
            {
                _lineSizes.Clear();
                _itemSizes.Clear();
                float itemW = _isItemWidthFill ? scrollListRect.rect.width : _itemWidth;
                float itemH = _isItemHeightFill ? scrollListRect.rect.height : _itemHeight;
                if (_itemRenderMode != ItemRenderMode.cycle)
                {
                    for (int i = appendIndex; i < n; i++)
                    {
                        ItemRenderer item = CreateItemRenderer(i, dataList[i]);
                        RectTransform tf = item.rectTransform;
                        if (tf == null)
                            tf = item.gameObject.GetComponent<RectTransform>();
                        tf.sizeDelta = new Vector2(itemW, itemH);
                        if (_direction == Direction.horizontal)
                        {
                            SetPoint(tf,
                                 _startX + (i / _row) * (itemW + _horizontalGap),
                                 _startY - (i % _row) * (itemH + _verticalGap)
                                 );
                        }
                        else
                        {
                            SetPoint(tf,
                             _startX + (i % _column) * (itemW + _horizontalGap),
                             _startY - (i / _column) * (itemH + _verticalGap)
                             );
                        }
                    }
                }
                if (_direction == Direction.horizontal)
                {
                    int m = Mathf.Min(_row, n);
                    maxSize = _paddingTop + m * (itemH) + (m - 1) * _verticalGap + _paddingBottom;

                    m = Mathf.CeilToInt((float)n / (float)_row);
                    y = -_paddingTop;
                    x = _startX + m * (itemW + _horizontalGap);
                }
                else
                {
                    int m = Mathf.Min(_column, n);
                    maxSize = _paddingLeft + m * (itemW) + (m - 1) * _horizontalGap + _paddingRight;

                    m = Mathf.CeilToInt((float)n / (float)_column);
                    x = _paddingLeft;
                    y = _startY - m * (itemH + _verticalGap);
                }
            }
            else
            {
                RectTransform tf = null;
                if (_itemRenderMode == ItemRenderMode.cycle)
                {
                    if (_tempListItemRenderer == null)
                    {
                        GameObject renderer = null;
                        //   if (itemRenderer.transform.parent != null)
                        //    renderer = itemRenderer;
                        //else
                        renderer = PoolManager.Instance.GetGameObject(itemRenderer, _scrollContent.transform);
                        RectTransform tf2 = renderer.GetComponent<RectTransform>();
                        tf2.pivot = new Vector2(0, 1);
                        tf2.anchorMin = new Vector2(0, 1);
                        tf2.anchorMax = new Vector2(0, 1);
                        _tempListItemRenderer = BootTool.GetComponent<ItemRenderer>(renderer);
                        _tempListItemRenderer.transform.localPosition = new Vector3(-10000, -10000);
                        _tempListItemRenderer.gameObject.SetActive(true);
                    }
                    tf = _tempListItemRenderer.rectTransform;
                }
                else
                {
                    _itemSizes.Clear();
                }

                for (int i = appendIndex; i < n; i++)
                {
                    ItemRenderer item = null;
                    float itemW = 0;
                    float itemH = 0;
                    if (_itemRenderMode != ItemRenderMode.cycle)
                    {
                        item = CreateItemRenderer(i, dataList[i]);
                        itemW = _isItemWidthFill ? scrollListRect.rect.width : item.rectTransform.rect.width;
                        itemH = _isItemHeightFill ? scrollListRect.rect.height : item.rectTransform.rect.height;
                    }
                    else
                    {
                        item = _tempListItemRenderer;
                        UpdateItemRenderer(item, i, dataList[i]);
                        itemW = _isItemWidthFill ? scrollListRect.rect.width : item.rectTransform.rect.width;
                        itemH = _isItemHeightFill ? scrollListRect.rect.height : item.rectTransform.rect.height;
                        Rect rect = new Rect(x, y, itemW, itemH);
                        if (_itemSizes.Count > i)
                            _itemSizes[i] = rect;
                        else
                            _itemSizes.Add(rect);
                    }
                    tf = item.rectTransform;

                    tf.sizeDelta = new Vector2(itemW, itemH);
                    SetPoint(tf, x, y);

                    ///
                    if (_direction == Direction.horizontal)
                    {
                        int m = i / _row;
                        if (_lineSizes.Count > m)
                            _lineSizes[m] = Mathf.Max(_lineSizes[m], tf.rect.width);
                        else
                            _lineSizes.Add(tf.rect.width);
                        if (i % _row == 0 || i == (n - 1))
                            maxSize = Mathf.Max(maxSize, -y + tf.rect.height + _paddingBottom);
                    }
                    else
                    {
                        int m = i / _column;
                        if (_lineSizes.Count > m)
                            _lineSizes[m] = Mathf.Max(_lineSizes[m], tf.rect.height);
                        else
                            _lineSizes.Add(tf.rect.height);

                        if (i % _column == 0 || i == (n - 1))
                            maxSize = Mathf.Max(maxSize, x + tf.rect.width + _paddingRight);
                    }

                    ComputeItemXY(ref x, ref y, i, tf.rect);
                }
                if (_tempListItemRenderer != null)
                    _tempListItemRenderer.transform.localPosition = new Vector3(-10000, -10000);
            }

            if (_endRenderer && _endRenderer.gameObject.activeSelf != _isShowEnd)
                _endRenderer.gameObject.SetActive(_isShowEnd);
            if (_isShowEnd && _endRenderer != null)
            {
                _endRenderer.offsetMin = new Vector2(x, y);
                if (_direction == Direction.horizontal)
                {
                    x += _endRenderer.rect.width + _horizontalGap;
                    maxSize = Mathf.Max(_paddingTop + _paddingBottom + _endRenderer.rect.height, maxSize);
                }
                else
                {
                    y -= _endRenderer.rect.height + _verticalGap;
                    maxSize = Mathf.Max(_paddingLeft + _paddingRight + _endRenderer.rect.width, maxSize);
                }
            }

            if (_loading && _loading.gameObject.activeSelf != _isShowLoading)
                _loading.gameObject.SetActive(_isShowLoading);
            if (_isShowLoading && _loading != null)
            {
                _loading.offsetMin = new Vector2(x, y);
                if (_direction == Direction.horizontal)
                {
                    x += _loading.rect.width + _paddingRight;
                    maxSize = Mathf.Max(_paddingTop + _paddingBottom + _loading.rect.height, maxSize);
                }
                else
                {
                    y -= _loading.rect.height + _paddingBottom;
                    maxSize = Mathf.Max(_paddingLeft + _paddingRight + _loading.rect.width, maxSize);
                }
            }
            else
            {
                if (_direction == Direction.horizontal)
                    x = x - _horizontalGap + _paddingRight;
                else
                    y = y + _verticalGap - _paddingBottom;
            }

            float contentW = 0;
            float contentH = 0;
            if (_direction == Direction.horizontal)
            {
                contentW = x;
                contentH = maxSize;
            }
            else
            {
                contentW = maxSize;
                contentH = -y;
            }
            /*
            if(_autoSize==false)
            {
                if (contentW < _scrollRect.viewport.rect.width)
                    contentW = _scrollRect.viewport.rect.width;
                if (contentH < _scrollRect.viewport.rect.height)
                    contentH = _scrollRect.viewport.rect.height;
            }
            */
            _scrollContent.sizeDelta = new Vector2(contentW, contentH);
            if (_autoSize)
                ((RectTransform)transform).sizeDelta = _scrollContent.sizeDelta;
            if (_itemRenderMode != ItemRenderMode.cycle)
            {
                while (itemRenderers.Count > n)
                {
                    ItemRenderer item = itemRenderers[itemRenderers.Count - 1];
                    itemRenderers.Remove(item);
                    item.container = null;
                    item.data = null;
                    item.selected = false;
                    if (_isCacheItemInPool)
                        PoolManager.Instance.PushGameObject(item.gameObject, itemRenderer);
                    else
                        Destroy(item.gameObject);
                }
            }
            UpdateRenderMode();
            appendIndex = n;

            if (_isAutoScrollLast)
            {
                int maxValue = maxScrollValue;
                int currentVlaue = scrollValue;
                if (maxValue > currentVlaue)
                    ScrollTo(maxValue);
            }
        }

        private ItemRenderer CreateItemRenderer(int index, object data)
        {
            ItemRenderer item = null;
            if (itemRenderers.Count > index)
                item = itemRenderers[index];
            else
            {
                GameObject go = null;
                if (_tempRenderers.Count > 0)
                {
                    item = _tempRenderers[_tempRenderers.Count - 1];
                    _tempRenderers.Remove(item);
                    go = item.gameObject;
                }
                else
                {
                    if (_isCacheItemInPool)
                        go = PoolManager.Instance.GetGameObject(itemRenderer, _scrollContent);
                    else
                    {
                        go = BootTool.CreateChild(itemRenderer, _scrollContent);
                        BootTool.SetLayer(go, _scrollContent.gameObject.layer);
                        BootTool.SetSortingLayer(go, _scrollContent);
                    }

                    RectTransform tf2 = go.GetComponent<RectTransform>();
                    tf2.pivot = new Vector2(0, 1);
                    tf2.anchorMin = new Vector2(0, 1);
                    tf2.anchorMax = new Vector2(0, 1);
                    item = go.GetComponent<ItemRenderer>();
                    if (item == null)
                        item = BootTool.GetComponent<ItemRenderer>(go);
                }
                if (go != null && go.activeSelf == false)
                    go.SetActive(true);
                itemRenderers.Add(item);
            }

            UpdateItemRenderer(item, index, data);
            return item;
        }

        private void UpdateItemRenderer(ItemRenderer item, int index, object data)
        {
            item.index = index;
            item.container = this;
            item.data = data;
            item.selected = _selectIndexs.Contains(index);
            if (data != null)
            {
                item.Refresh();
                if (OnItemUpdate != null)
                {
                    OnItemUpdate.Invoke(item);
                }
            }
        }

        private void ComputeItemXY(ref float x, ref float y, int index, Rect size)
        {
            if (_direction == Direction.horizontal)
            {
                float lineSize = _itemWidth;
                if (_isItemFixedSize == false)
                {
                    int m = index / _row;
                    lineSize = _lineSizes[m];
                }

                if (index % _row == 0)
                {
                    x = x + lineSize + _horizontalGap;
                    y = -_paddingTop;
                }
                else
                {
                    y = y - size.height - _verticalGap;
                }
            }
            else
            {
                float lineSize = _itemHeight;
                if (_isItemFixedSize == false)
                {
                    int m = index / _column;
                    lineSize = _lineSizes[m];
                }

                if (index % _column == 0)
                {
                    x = _paddingLeft;
                    y = y - lineSize - _verticalGap;
                }
                else
                {
                    x = x + size.width + _horizontalGap;
                }
            }
        }

        private void UpdateRenderMode()
        {
            if (dataList == null || dataList.Count == 0)
                return;
            if (_itemRenderMode != ItemRenderMode.all)
            {
                RectTransform viewRect = _scrollRect.viewport;
                if (viewRect.rect.width == 0 && viewRect.rect.height == 0)
                    viewRect = transform as RectTransform;
                int startShowIndex = 0;
                int endShowIndex = 0;
                int startShowLine = 0;
                int endShowLine = 0;
                float x = _scrollContent.localPosition.x;
                float y = _scrollContent.localPosition.y;
                if (x > 0)
                    x = 0;
                if (y < 0)
                    y = 0;
                x = Mathf.Abs(x);
                y = Mathf.Abs(y);
                x -= _startX;
                y -= _startY;
                if (_isItemFixedSize)
                {
                    if (_direction == Direction.horizontal)
                    {
                        startShowLine = (int)(x / (_itemWidth + _horizontalGap));
                        endShowLine = (int)((x + viewRect.rect.width) / (_itemWidth + _horizontalGap));
                    }
                    else
                    {
                        startShowLine = (int)(y / (_itemHeight + _verticalGap));
                        endShowLine = (int)((y + viewRect.rect.height) / (_itemHeight + _verticalGap));
                    }
                }
                else
                {
                    float currentSize = 0;
                    for (int i = 0; i < _lineSizes.Count; i++)
                    {
                        currentSize += _lineSizes[i];
                        startShowLine = i;
                        if (_direction == Direction.horizontal)
                        {
                            if (currentSize > x)
                                break;
                            currentSize += _horizontalGap;
                        }
                        else
                        {
                            if (currentSize > y)
                                break;
                            currentSize += _verticalGap;
                        }
                    }
                    endShowLine = startShowLine;
                    currentSize += _verticalGap;
                    for (int i = startShowLine + 1; i < _lineSizes.Count; i++)
                    {
                        currentSize += _lineSizes[i];
                        endShowLine = i;
                        if (_direction == Direction.horizontal)
                        {
                            if (currentSize > (x + viewRect.rect.width))
                                break;
                            currentSize += _horizontalGap;
                        }
                        else
                        {
                            if (currentSize > (y + viewRect.rect.height))
                                break;
                            currentSize += _verticalGap;
                        }
                    }
                }
                if (_direction == Direction.horizontal)
                {
                    startShowIndex = startShowLine * _row;
                    endShowIndex = endShowLine * _row + _row - 1;
                    if (endShowIndex >= dataList.Count)
                    {
                        endShowIndex = dataList.Count - 1;
                        endShowLine = endShowIndex / _row;
                    }
                }
                else
                {
                    startShowIndex = startShowLine * _column;
                    endShowIndex = endShowLine * _column + _column - 1;
                    if (endShowIndex >= dataList.Count)
                    {
                        endShowIndex = dataList.Count - 1;
                        endShowLine = endShowIndex / _column;
                    }
                }

                if (_itemRenderMode == ItemRenderMode.hideUnSee)
                {
                    int n = itemRenderers.Count;
                    for (int i = 0; i < n; i++)
                    {
                        ItemRenderer item = itemRenderers[i];
                        if (i < startShowIndex)
                        {
                            if (item.gameObject.activeSelf)
                                item.gameObject.SetActive(false);
                        }
                        else if (i > endShowIndex)
                        {
                            if (item.gameObject.activeSelf)
                                item.gameObject.SetActive(false);
                        }
                        else if (item.gameObject.activeSelf == false)
                            item.gameObject.SetActive(true);
                    }
                }
                else if (_itemRenderMode == ItemRenderMode.cycle)
                {
                    int n = itemRenderers.Count;

                    for (int i = n - 1; i >= 0; i--)
                    {
                        ItemRenderer item = itemRenderers[i];
                        if (item.index < startShowIndex || item.index > endShowIndex || item.index >= appendIndex)
                        {
                            _tempRenderers.Add(item);
                            itemRenderers.Remove(item);
                        }
                    }
                    int index = 0;
                    for (int i = startShowIndex; i <= endShowIndex; i++)
                    {
                        ItemRenderer item = null;
                        if (itemRenderers.Count > index)
                            item = itemRenderers[index];

                        if (item == null || item.index != i)
                        {
                            if (_tempRenderers.Count > 0)
                            {
                                item = _tempRenderers[_tempRenderers.Count - 1];
                                _tempRenderers.Remove(item);
                            }
                            else
                            {
                                GameObject go = null;
                                if (_isCacheItemInPool)
                                    go = PoolManager.Instance.GetGameObject(itemRenderer, _scrollContent);
                                else
                                {
                                    go = BootTool.CreateChild(itemRenderer, _scrollContent);
                                    BootTool.SetLayer(go, _scrollContent.gameObject.layer);
                                    BootTool.SetSortingLayer(go, _scrollContent);
                                }
                                item = BootTool.GetComponent<ItemRenderer>(go);
                            }
                            if (item.gameObject.activeSelf == false)
                                item.gameObject.SetActive(true);
                            itemRenderers.Insert(index, item);
                            RectTransform tf = item.transform as RectTransform;
                            if (_isItemFixedSize == false)
                            {
                                if (_itemSizes.Count > i)
                                {
                                    Rect rect = _itemSizes[i];
                                    tf.sizeDelta = new Vector2(rect.width, rect.height);
                                    SetPoint(tf, rect.x, rect.y);
                                }
                            }
                            else
                            {
                                //  tf.sizeDelta = new Vector2(_itemWidth, _itemHeight);
                                if (_direction == Direction.horizontal)
                                {
                                    SetPoint(tf,
                                        _startX + (i / _row) * (_itemWidth + _horizontalGap),
                                        _startY - (i % _row) * (_itemHeight + _verticalGap)
                                        );
                                }
                                else
                                {
                                    SetPoint(tf,
                                     _startX + (i % _column) * (_itemWidth + _horizontalGap),
                                     _startY - (i / _column) * (_itemHeight + _verticalGap)
                                     );
                                }
                            }
                            UpdateItemRenderer(item, i, dataList[i]);
                        }
                        index++;
                    }
                }
                if (_itemRenderMode != ItemRenderMode.cycle)
                {
                    int tempCount = _direction == Direction.horizontal ? _row : _column;
                    tempCount = tempCount * 2;
                    for (int i = 0; i < _tempRenderers.Count; i++)
                    {
                        ItemRenderer item = _tempRenderers[i];
                        if (i > tempCount - 1)
                        {
                            item.data = null;
                            item.container = null;
                            item.selected = false;
                            if (_isCacheItemInPool)
                                PoolManager.Instance.PushGameObject(item.gameObject, itemRenderer);
                            else
                                Destroy(item.gameObject);
                        }
                        else
                        {
                            // item.transform.localPosition = new Vector2(-1000,-1000);
                            item.gameObject.SetActive(false); ///留一个作缓冲之前
                        }
                    }
                    if (_tempRenderers.Count > tempCount)
                        _tempRenderers.RemoveRange(tempCount, _tempRenderers.Count - tempCount);
                }
                else
                {
                    for (int i = 0; i < _tempRenderers.Count; i++)
                    {
                        ItemRenderer item = _tempRenderers[i];
                        item.transform.localPosition = new Vector2(-10000, -10000);
                    }
                }

                if (_isShowHead && _headRenderer != null)
                {
                    bool isShow;
                    if (_direction == Direction.horizontal)
                        isShow = _scrollContent.offsetMin.x + _startX > 0;
                    else
                        isShow = _scrollContent.offsetMin.y - _startY < 0;
                    _headRenderer.gameObject.SetActive(isShow);
                }
                if (_isShowEnd && _endRenderer != null)
                {
                    bool isShow = endShowIndex == (dataList.Count - 1);
                    _endRenderer.gameObject.SetActive(isShow);
                }
                if (_isShowLoading && _loading != null)
                {
                    bool isShow = endShowIndex == (dataList.Count - 1);
                    _loading.gameObject.SetActive(isShow);
                }
            }
        }

        public override void Clear()
        {
            base.Clear();
            onScroll.RemoveAllListeners();
        }

        //****************** 事件 *****************

        private void ScrollRectOnValueChange(Vector2 vec)
        {
            if (isWaitForUpdate == false)
            {
                //乘100减少抖动
                // if ((int)(_oldScrollVector.x * 100) != (int)(vec.x * 100) || (int)(_oldScrollVector.y * 100) != (int)(vec.y * 100))
                //  {
                if (itemRenderer != null)
                    UpdateRenderMode();
                //  }

                // _oldScrollVector = vec;
            }

            //拉到列表最底部时调用一下回调函数
            if (scrollValue == maxScrollValue)
            {
                OnMoveToEndCallback?.Invoke();
            }

            onScroll.Invoke();
        }

        private void SetPoint(RectTransform tf, float x, float y)
        {
            tf.localPosition = new Vector3(x + tf.rect.width * tf.pivot.x, y - tf.rect.height * (1 - tf.pivot.y));
        }

        private Vector2 GetPoint(RectTransform tf)
        {
            return new Vector2(tf.localPosition.x - tf.rect.width * tf.pivot.x, tf.localPosition.y + tf.rect.height * (1 - tf.pivot.y));
        }

        /// /////////////////////////////

        public enum Direction
        {
            vertical, horizontal
        }

        public enum ItemRenderMode
        {
            all, cycle, hideUnSee
        }

        #region 动画

        [SerializeField]
        private float animTimeLen = 0.33f;

        [SerializeField]
        private float animItemPlayCD = 0.17f;

        [SerializeField]
        private Vector2 animAlpha = Vector2.up;

        [SerializeField]
        private Vector3 animPosOffset;

        [SerializeField]
        private AnimationCurve animCurve;

        private bool isPlayAnim = false;
        private float time = 0;
        private int index;
        private int itemLen;
        private List<ItemRenderer> animItemList = new List<ItemRenderer>();
        private List<ItemRenderer> tempAnimItemList = new List<ItemRenderer>();
        private IList scrollItems;

        public bool CheckAnim(IList datas)
        {
            if (isPlayAnim)
            {
                scrollItems = datas;

                return true;
            }
            return false;
        }

        /// <summary>
        /// 播放入场动画
        /// </summary>
        public void Play(Action<int> resetPosAction = null, int _resetIndex = 0)
        {
            isPlayAnim = false;
            if (scrollItems != null)
            {
                datas = scrollItems;
            }
            isPlayAnim = false;

            RefreshDatas();
            ScrollTo(0, true);
            scrollItems = null;
            if (Items.Count == 0)
            {
                return;
            }
            ResetPosAction = resetPosAction;
            resetIndex = _resetIndex;
            resetPosAction?.Invoke(_resetIndex);

            foreach (var item in Items)
            {
                item.canvasGroup.enabled = true;
                item.canvasGroup.alpha = 0;
                item.canvasGroup.blocksRaycasts = true;
            }
            isPlayAnim = true;
            time = 0;
            index = 0;
            itemLen = direction == Direction.vertical ? column : row;
            animItemList.Clear();
            tempAnimItemList.Clear();
            PlayNextItem();
        }

        private void PlayNextItem()
        {
            if (index * itemLen >= Items.Count)
            {
                return;
            }
            int count = Math.Min(itemLen * index + itemLen, Items.Count);

            for (int i = index * itemLen; i < count; i++)
            {
                ItemRenderer item = Items[i];
                item.animTagerPos_ = item.trans.localPosition;
                item.animPos_ = item.animTagerPos_ + animPosOffset;
                item.trans.localPosition = item.animPos_;
                item.animTime_ = 0;
                animItemList.Add(item);
            }
        }

        private void PlayEnd()
        {
            isPlayAnim = false;
            animItemList.Clear();
            foreach (var item in Items)
            {
                item.canvasGroup.alpha = 1;
                item.canvasGroup.blocksRaycasts = true;
                item.canvasGroup.enabled = false;
                item.trans.localPosition = item.animTagerPos_;
            }
            foreach (var item in _tempRenderers)
            {
                item.canvasGroup.alpha = 1;
                item.canvasGroup.blocksRaycasts = true;
                item.canvasGroup.enabled = false;
            }
            if (scrollItems != null)
            {
                datas = scrollItems;
                scrollItems = null;
            }
            RefreshDatas();

            ResetPosAction?.Invoke(resetIndex);
        }

        private void FixedUpdate()
        {
            if (!isPlayAnim)
            {
                time = 0;
                return;
            }
            time += Time.fixedDeltaTime / 1.5f;
            if (time > animItemPlayCD)
            {
                time = 0;
                index++;
                PlayNextItem();
            }

            foreach (var item in animItemList)
            {
                item.animTime_ += Time.deltaTime;
                if (item.animTime_ > animTimeLen)
                {
                    item.animTime_ = animTimeLen;
                    tempAnimItemList.Add(item);
                }
                float lineDt = item.animTime_ / animTimeLen;
                float dt = animCurve != null ? animCurve.Evaluate(lineDt) : lineDt;
                item.canvasGroup.alpha = Mathf.Lerp(animAlpha.x, animAlpha.y, lineDt);
                item.trans.localPosition = Vector3.Lerp(item.animPos_, item.animTagerPos_, dt);
            }
            if (tempAnimItemList.Count > 0)
            {
                foreach (var item in tempAnimItemList)
                {
                    animItemList.Remove(item);
                }
                tempAnimItemList.Clear();
                if (animItemList.Count == 0)
                {
                    PlayEnd();
                }
            }
        }

        public override void Refresh()
        {
            if (isPlayAnim) return;
            base.Refresh();
        }

        public override void RefreshDatas()
        {
            if (isPlayAnim) return;
            base.RefreshDatas();
        }

        #endregion 动画
    }

    ///////////////////////////////////////////////////////////////////
}
