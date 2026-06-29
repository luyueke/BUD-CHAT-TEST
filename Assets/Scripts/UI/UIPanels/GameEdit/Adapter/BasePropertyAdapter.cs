/**
 * @ Author: Jun Zhou
 * @ Create Time: 2023-07-27 13:23:25
 * @ Modified by: Jun Zhou
 * @ Modified time: 2023-09-10 18:07:28
 * @ Description: 道具属性面板的适配器基类
 */

using Game.ECS;
using UnityEngine;
namespace UI.UIPanels.GameEdit
{
    public abstract class BasePropertyAdapter : MonoBehaviour 
    {
        protected GamePropertyEditPanel mainPanel;
        protected SceneEntity selectEntity;

        protected abstract void OnCreate();
        protected virtual void OnStart(){}
        protected abstract void OnSelectEntity();

        private void Awake() 
        {
            mainPanel = GetComponent<GamePropertyEditPanel>();
            OnCreate(); 
        }

        private void Start() 
		{
			OnStart();
            OnViewInitComplete();
		}

        public void SetSelectEntity(SceneEntity entity)
        {
            selectEntity = entity;
            mainPanel.SetSelectEntity(entity);
            OnSelectEntity();
        }

        void OnViewInitComplete()
        {
            mainPanel.OnViewInitComplete();
        }

        protected void SelectTabItem(int index)
        {
            mainPanel.SelectTabItem(index);
        }

        /// <summary>
        /// 提供颜色的基础组件
        /// </summary>
        protected GameColorEditSubView AddColorSubView()
        {
            return AddTabView<GameColorEditSubView>("颜色");
        }

        /// <summary>
        /// 提供材质的基础组件
        /// </summary>
        protected GameMatEditSubView AddMatSubView()
        {
            return AddTabView<GameMatEditSubView>("设置");
        }

        protected T AddTabView<T>(string tabName) where T : BasePropertyEditSubView
        {
            return mainPanel.AddTabView<T>(typeof(T).Name, tabName);
        }
    }
}