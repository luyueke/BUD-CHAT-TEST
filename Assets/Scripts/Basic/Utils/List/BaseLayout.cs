using UnityEngine;

using System;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Fsbm.Runtime
{
    [DisallowMultipleComponent]
    /// <summary>
    /// Oscar
    /// </summary>
    public class BaseLayout : GBehaviour
    {
        public UEvent onLayoutComplete = new UEvent();

        [SerializeField]
        private bool _isUpdateParentAndChildren = true;
        [SerializeField]
        private bool _isAutoLayout = true;

        protected static bool isUpdateViewing = false;
        private RectTransform _rectTransform;
        protected RectTransform rectTransform
        {
            get
            {
                if(_rectTransform==null)
                    _rectTransform  = transform as RectTransform;
                return _rectTransform;
            }
        }

        private Action<Transform, Vector3> _SetPosition;
        public Action<Transform, Vector3> SetPosition
        {
            set
            {
                _SetPosition = value;
            }
            get
            {
                return _SetPosition ?? DefautSetPositionFun;
            }
        }
        public bool isUpdateParentAndChildren { get { return _isUpdateParentAndChildren; } set { _isUpdateParentAndChildren = value; } }
        public bool isAutoLayout { get { return _isAutoLayout; } set { _isAutoLayout = value; } }
        protected override void Init()
        {
            base.Init();
     
           
            RectTransform.reapplyDrivenProperties += ReapplyDrivenProperties;

            Graphic g = GetComponent<Graphic>();
            if (g != null)
                g.RegisterDirtyLayoutCallback(RegisterDirtyLayoutCallback);
        }

        public override void Refresh()
        {
            if (gameObject != null)
            {
                if (Application.isPlaying==false || gameObject.activeInHierarchy)
                    base.Refresh();
                else
                    isWaitForUpdate = true;
            }
        }

        public   void DefautSetPositionFun( Transform child, Vector3 pos)
        {
            child.localPosition = pos;
        }

        /// <summary>
        /// CanvasRender重绘时调用
        /// </summary>
        private void RegisterDirtyLayoutCallback()
        {
            if (isUpdateViewing == false&& _isAutoLayout)
            {
                if (gameObject != null && gameObject.activeInHierarchy)
                {
                    InvalidView();
                }
            }
        }
        /// <summary>
        /// 场景上有RectTransform对象显示或隐藏时调用
        /// </summary>
        /// <param name="driven"></param>
        private void ReapplyDrivenProperties(RectTransform driven)
        {
            if (isUpdateViewing == false&& _isAutoLayout)
            {
                if (gameObject == null)
                {
                    RectTransform.reapplyDrivenProperties -= ReapplyDrivenProperties;
                }
                else
                {
                    if (driven == transform || driven.parent == transform)
                    {
                        InvalidView();
                    }
                }
            }
        }
        /// <summary>
        /// 自身大小改变时调用
        /// </summary>
        protected virtual void OnRectTransformDimensionsChange()
        {
            if (isUpdateViewing == false && gameObject.activeInHierarchy&& _isAutoLayout)
            {
                InvalidView();
            }

        }

        /// <summary>
        /// 子对象增减时调用
        /// </summary>
        protected virtual void OnTransformChildrenChanged()
        {
            if (isUpdateViewing == false && gameObject.activeInHierarchy&& _isAutoLayout)
                InvalidView();
        }
        protected override void OnEnable()
        {
            base.OnEnable();
            if (isUpdateViewing == false&& _isAutoLayout)
                InvalidView();
        }
        protected override void OnDisable()
        {
            base.OnDisable();
            if (isUpdateViewing == false&& _isAutoLayout )
                InvalidView();
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            RectTransform.reapplyDrivenProperties -= ReapplyDrivenProperties;
        }
        protected override void UpdateView()
        {
            isUpdateViewing = true;
            base.UpdateView();
            BaseLayout layout = this;
            if (isUpdateParentAndChildren)
            {
                while (layout.transform.parent != null)
                {
                    BaseLayout parentLayout = layout.transform.parent.GetComponent<BaseLayout>();
                    if (parentLayout != null)
                        layout = parentLayout;
                    else
                        break;
                }
            }
            layout._UpdateLayout();
            isUpdateViewing = false;
        }
        private  void _UpdateLayout()
        {
            isWaitForUpdate = false;
         
            if (isActiveAndEnabled == false || (gameObject != null && gameObject.activeInHierarchy == false))
                return;
            
            List<RectTransform> tfs = GetLayoutChildren();
            //检查子项大小
            UpdateLayoutChildrenSize(tfs);
            UpdateLayout();
            onLayoutComplete.Invoke();
        }
        protected virtual void UpdateLayout()
        {
        }

        protected List<RectTransform> GetLayoutChildren()
        {
            List<RectTransform> tfs = new List<RectTransform>();
            for (var i = 0; i < transform.childCount; i++)
            {
                RectTransform child = transform.GetChild(i) as RectTransform;
                if (child != null && child.gameObject.activeInHierarchy)
                {
                    IgnoreLayout ignoreLayout = child.GetComponent<IgnoreLayout>();
                    if (ignoreLayout == null|| ignoreLayout.enabled==false)
                        tfs.Add(child);
                }
            }
            return tfs;
        }

        protected void UpdateLayoutChildrenSize(List<RectTransform> tfs)
        {
            for (int i = 0; i < tfs.Count; i++)
            {
                BaseLayout layout = tfs[i].GetComponent<BaseLayout>();
                if (layout != null && layout.isActiveAndEnabled && layout.isUpdateParentAndChildren && layout.gameObject.activeInHierarchy)
                    layout._UpdateLayout();
            }
        }
        private void Layout()
        {
            UpdateView();
        }
    }
}
