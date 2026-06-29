/**
 * @ Author: Jun Zhou
 * @ Create Time: 2023-07-24 15:07:03
 * @ Modified by: Jun Zhou
 * @ Modified time: 2023-09-10 15:50:38
 * @ Description: 道具编辑面板的子界面
 */

using Game.ECS;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.UIPanels.GameEdit
{
    public abstract class BasePropertyEditSubView : MonoBehaviour
    {
        [SerializeField]protected GameObject expandUI; // 展开的UI
        [SerializeField]protected GameObject normalUI; // 普通的UI

        [Header("调试")]
        [SerializeField]protected bool isExpand = true; // 是否展开

        RectTransform uiContent;
        const int ExpandHeight = 408; // 展开的高度
        const int UnExpandHeight = 240; // 缩小的高度
        protected int viewIndex = 0; // 视图所处的Tab栏Index
        protected GamePropertyEditPanel mainPanel;
        protected SceneEntity selectEntity;
        protected abstract void OnInit();
        protected virtual void OnStart(){}

        private void Awake() 
        {
            OnInit();
        }

        private void Start()
        {
            OnStart();
        }

        /// <summary>
        /// 设置是否展开
        /// </summary>
        public void SetIsExpand(bool bIsExpand)
        {
            // 如果没有特殊的展开界面，直接改变UI的大小
            if (HaveExpandUI())
            {
                expandUI.SetActive(bIsExpand);
                normalUI.SetActive(!bIsExpand);
            } else {
                if (uiContent == null)
                    uiContent = transform.GetComponent<RectTransform>();

                var originSize = uiContent.sizeDelta;
                originSize.y = bIsExpand ? ExpandHeight : UnExpandHeight;
                uiContent.sizeDelta = originSize;
            }
            isExpand = bIsExpand;
            OnExpand(bIsExpand);
        }

        /// <summary>
        /// 是否支持展开UI
        /// </summary>
        public bool HaveExpandUI()
        {
            return expandUI != null && normalUI != null;
        }

        public bool IsExpand()
        {
            return isExpand;
        }

        protected virtual void OnExpand(bool bIsExpand)
        {
            
        }

        protected BasePropertyAdapter GetAdapter()
        {
            return mainPanel.GetComponent<BasePropertyAdapter>();
        }

        public void Init(GamePropertyEditPanel panel, int vIndex)
        {
            mainPanel = panel;
            viewIndex = vIndex;
        }
        
        public void SetSelectEntity(SceneEntity entity)
        {
            selectEntity = entity;
            OnSelectEntity(entity);
        }

        public virtual void OnSelectEntity(SceneEntity entity)
        {
            
        }

        public int GetViewIndex()
        {
            return viewIndex;
        }
    }
}