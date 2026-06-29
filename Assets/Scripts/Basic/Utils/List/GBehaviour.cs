using UnityEngine;
using System.Collections;
using System;
using UnityEngine.Events;

namespace Fsbm.Runtime
{

    /// <summary>
    /// 所有组件基类
    /// </summary>
    public abstract class GBehaviour : MonoBehaviour, IReference
    {
        /// <summary>
        /// 事件：组件刷新显示
        /// </summary>

        public Action onUpdateView;
        /// <summary>
        /// 事件：组件刷新布局尺寸
        /// </summary>

        public Action onUpdateSize ;
        //*************  属性定义  **************
        private bool _waitUpdateing = false;

        private bool _isStarted = false;
        private bool _isAwake = false;

        //************ 公共方法 *********

        //******** 子类可重写方法 ********


        protected UnityAction initAfterShow;

        protected bool _bDestroyed = false;

        /// <summary>
        /// 组件是否等待刷新，调用InvalidView后这里为true
        /// </summary>
        public bool isWaitForUpdate
        {
            get;
            protected set;
        }


        /// <summary>
        /// 一些影响界面的属性变更时可调用该方法，使界面无效，等下次Update时就会调用UpdateView刷新，提升性能
        /// </summary>
        public void InvalidView()
        {
            //if (this.IsAliveNoInPool() == false)
            //{
            //    Debug.LogError("GameObject 已经被收回(放回对象池或直接销毁了)，你依然对它有引用操作");
            //    return;
            //}
            isWaitForUpdate = true;
            if (_isStarted == false)
                return;
            if (Application.isPlaying == false)
                _UpdateView();
            else if (gameObject.activeInHierarchy == false)
            {

            }
            else if(_waitUpdateing==false)
            {
                _waitUpdateing = true;
                DelayCall.CallLate(_UpdateView);
            }
               
        }

        protected virtual void OnEnable()
        {
            if(isWaitForUpdate)
            {
                InvalidView();
            }
        }

        /// <summary>
        /// 立即刷新组件
        /// </summary>
        public virtual void Refresh()
        {

            if (_isAwake == false && Application.isPlaying)
            {
                isWaitForUpdate = false;
                _isAwake = true;
                Init();
            }
            _UpdateView();
        }


        //******** 子类可重写方法 ********
        protected virtual void Awake()
        {
            isWaitForUpdate = true;
            if (_isAwake) return;
            _isAwake = true;
            Init();
        }
        protected virtual void Start()
        {
            if(initAfterShow!=null)
            {
                //var showHide = GetComponent<ShowHideComponent>();
                //if(showHide!=null)
                //{
                //    if(showHide.isShowing||showHide.isShow==false)
                //    {
                //        showHide.onShowComplete.AddListenerOnce(InitAfterShow);
                //        return;
                //    }
                //}
                initAfterShow();
            }
            _isStarted = true;
            if (isWaitForUpdate)
            {
                _UpdateView(); 
            }
        }
        private void InitAfterShow()
        {
            _isStarted = true;
            if (initAfterShow != null)
                initAfterShow();
            if (isWaitForUpdate)
            {
                _UpdateView();
            }
        }



        protected virtual void OnDisable()
        { }

        protected virtual void OnDestroy()
        {
            _bDestroyed = true;

            if (_waitUpdateing)
                DelayCall.CancelCallLate(_UpdateView);
            _waitUpdateing = false;
        }
        /// <summary>
        /// 初始化
        /// </summary>
        protected virtual void Init()
        {

        }


        /// <summary>
        /// 界面更新,是延时更新，提升性能
        /// </summary>
        internal void _UpdateView()
        {
            if (_bDestroyed)
            {
                Debug.LogWarning("[GBehaviour] _UpdateView, already destroy");
                return;
            }

            _waitUpdateing = false;
            if (isWaitForUpdate)
                 DelayCall.CancelCallLate(_UpdateView);
            isWaitForUpdate = false;
            UpdateView();
            if(onUpdateView!=null)
                onUpdateView();
            UpdateSize();
            if(onUpdateSize!=null)
                onUpdateSize();
            isWaitForUpdate = false;
        }
        /// <summary>
        /// 把组件内所有的显示逻辑都写在这方法内。
        /// </summary>
        protected virtual void UpdateView()
        {

        }
        /// <summary>
        /// UpdateView后立调用，把调整组组布局或大小尺寸的逻 写在这个方法内
        /// </summary>
        protected virtual void UpdateSize()
        {
         
        }

        /// <summary>
        /// 清除组件的数据和状态， 被放入对象池时会被调用
        /// </summary>
        public virtual  void Clear()
        {
            
        }

    }

}
