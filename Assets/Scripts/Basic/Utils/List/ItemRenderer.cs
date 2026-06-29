using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#pragma warning disable 649
namespace Fsbm.Runtime
{
    /// <summary>
    /// 列表项 
    /// </summary>
    public class ItemRenderer : GBehaviour, IPointerClickHandler
    {
        public Action<object> onDataChanged;

        [SerializeField]
        private Transform _selectClip;


        private int _index;
        private object _data;
        private BaseList _container;
        private float _clickTime;
        private bool _selected = false;
        private Transform _transform;
        private RectTransform _rectTransform;
        private bool _isStart = false;

        public bool selected
        {
            get { return _selected; }
            set
            {
                if (_selected == value)
                    return;
                _selected = value;
                if (_selectClip != null)
                    _selectClip.gameObject.SetActive(_selected);
                if (data != null)
                    InvalidView();
            }
        }
        public int index
        {
            get
            {
                return _index;
            }
            internal set
            {
                if (_index == value)
                    return;
                _index = value;
                if(_index!=-1)
                    InvalidView();
            }
        }
        #region   入场动画
        private CanvasGroup _canvasGroup;
        public CanvasGroup canvasGroup
        {
            get
            {
                if(_canvasGroup== null)
                {
                    _canvasGroup = gameObject.GetComponent<CanvasGroup>();
                    if(_canvasGroup == null)
                    {
                        _canvasGroup= gameObject.AddComponent<CanvasGroup>();
                    }
                }
                return _canvasGroup;
            }
        }
        [HideInInspector]
        public Vector3 animTagerPos_;
        [HideInInspector]
        public Vector3 animPos_;
        [HideInInspector]
        public float animTime_;

        #endregion
        public virtual object data
        {
            get
            {
                return _data;
            }
            set
            {
                if (_data == value)
                    return;
                _data = value;
                if(_data!=null)
                    InvalidView();
                if(_isStart&& onDataChanged!=null)
                    onDataChanged.Invoke(_data);
            }
        }
        public BaseList container
        {
            get
            {
                return _container;
            }
            set
            {
                if (_container == value)
                    return;
                _container = value;
                CheckOnLongPressEnabled();
                if(_container!=null)
                    InvalidView();
            }
        }

        public RectTransform rectTransform
        {
            get
            {
                if(_rectTransform==null)
                    _rectTransform = GetComponent<RectTransform>();
                return _rectTransform;
            } 
        }
        public Transform trans
        {
            get
            {
                if (_transform == null)
                    _transform = transform;
                return _transform;
            }
        }

        protected override void Start()
        {
            base.Start();
            if(_data!=null&& onDataChanged!=null)
                onDataChanged.Invoke(_data);
            _isStart = true;
        }
        protected override void Init()
        {
            base.Init();
           
        }

        public override void Refresh()
        {
            base.Refresh();
        }

        internal void CheckOnLongPressEnabled()
        {
            if (_container != null)
            {
                BtnLongPress longPress = GetComponent<BtnLongPress>();
                if (_container.longPressEnabled)
                {
                    if (longPress == null)
                    {
                        longPress = gameObject.AddComponent<BtnLongPress>();
                        longPress.mode = BtnLongPress.Mode.Touch;
                    }
                    if (!longPress.onLongPress.HasListener(onLongPressEvent))
                    {
                        longPress.onLongPress.AddListener(onLongPressEvent);
                    }
                }
                else
                {
                    if (longPress != null)
                    {
                        longPress.onLongPress.RemoveListener(onLongPressEvent);
                        Destroy(longPress);
                    }
                }
            }
        }



        protected override void UpdateView()
        {
            base.UpdateView();
            if (_selectClip != null)
                _selectClip.gameObject.SetActive(_selected);
        }
        protected override void UpdateSize()
        {
            BaseLayout layout = GetComponent<BaseLayout>();
            if (layout != null)
                layout.Refresh();
        }

        public virtual void OnPointerClick(PointerEventData eventData)
        {
            BtnLongPress longPress = GetComponent<BtnLongPress>();
            if (longPress && longPress.longPressTriggered)
                return;
            bool isDoubleClick = false;
            if (_container != null&&_container.doubleClickEnabled)
            {
                float t = Time.realtimeSinceStartup;
                if (t - _clickTime < 0.2)
                    isDoubleClick = true;
                _clickTime = t;
            }
            if (isDoubleClick)
            {
                CancelInvoke("OnClickLate");
                if (_container != null)
                    _container.onItemDoubleClick.Invoke(index, data );
            }
            else
            {
                if (_container != null && _container.doubleClickEnabled)
                {
                    if (IsInvoking("OnClickLate") == false)
                        Invoke("OnClickLate", 0.2f);
                }
                else
                    OnClickLate();
            }
 
        }

        private void OnClickLate()
        {
            CancelInvoke("OnClickLate");
            if (_container != null)
            {
                _container.onItemClick.Invoke(index, data);
                if (_container.selectable)
                {
                    if(_container.isMultipleSelection==false)
                    {
                        _container.selectIndex = index;
                    }
                    else
                    {
                        _container.ToggleSeleced(index);
                    }
                }
      
            }
               
        }

        protected void TriggerItemEvent(string eventName, object param=null)
        {
            if (_container != null&& _container.onItemEvent!=null)
                _container.onItemEvent.Invoke(eventName, index, data,param);
        }

        private void onLongPressEvent()
        {
            if (_container != null&& _container.longPressEnabled)
                _container.onItemLongPress.Invoke( index, data );
        }
    }
}

