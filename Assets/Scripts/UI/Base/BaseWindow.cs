using System;
using System.Collections.Generic;
using Es;
using UnityEngine;
using UnityEngine.Serialization;

namespace UI.Base
{
    [Serializable]
    public class BaseWindow : MonoBehaviour
    {
        public UIWindow config;
        public List<BasePanel> panels;

        //记录panel transform缓存
        public List<BasePanel> panelTransformCache;

        public BaseWindow()
        {
            panels = new List<BasePanel>();
            panelTransformCache = new List<BasePanel>();
        }

        public bool AddPanel(BasePanel panel)
        {
            if (panels.Contains(panel))
            {
                return false;
            }

            panels.Add(panel);
            UIOrderHelper.SetPanelOrder(panel);

            if (!panelTransformCache.Contains(panel))
            {
                panelTransformCache.Add(panel);
            }

            return true;
        }

        public bool RemovePanel(BasePanel panel)
        {
            if (!panels.Contains(panel))
            {
                return false;
            }

            panels.Remove(panel);
            return true;
        }

        /// <summary>
        /// 找最后一个对应panelId的实例
        /// </summary>
        public BasePanel FindPanelByPanelId(PanelId panelId)
        {
            for (int i = panels.Count - 1; i >= 0; i--)
            {
                var p = panels[i];
                if (p.Config.PanelId == (int)panelId)
                {
                    return p;
                }
            }

            return null;
        }

        public BasePanel GetPanelNodeCache(PanelId panelId)
        {
            foreach (var p in panelTransformCache)
            {
                if (p != null && p.Config != null && p.Config.PanelId == (int)panelId)
                {
                    return p;
                }
            }

            return null;
        }

        public void RemovePanelTransCache(BasePanel panel)
        {
            if (panelTransformCache.Contains(panel))
            {
                panelTransformCache.Remove(panel);
            }
        }

        #region window 控制

        public void BeCovered(bool isCover)
        {
            if (config.IsControlByStack == false) return;
            foreach (var subPanel in panels)
            {
                subPanel.OnWindowBeCovered(isCover);
            }
        }

        public void BeFocus()
        {
            if (config.IsControlByStack == false) return;
            foreach (var subPanel in panels)
            {
                subPanel.OnWindowBeFocused();
            }
        }

        public void BeShow()
        {
            if (config.IsControlByStack == false) return;
            foreach (var subPanel in panels)
            {
                subPanel.OnWindowShow();
            }
        }

        public void PopFromStack(Action onWinDestroy, bool isFromStackPop = true)
        {
            if (config.IsControlByStack == false) return;
            foreach (var subPanel in panels)
            {
                subPanel.OnHidden();
                UIManager.Inst.CallClosePanelAct(subPanel);
                subPanel.gameObject.SetActive(false);

                subPanel.OnWindowPop();
            }

            this.gameObject.SetActive(false);
            
            if (isFromStackPop && config.IsDestroyWhenPop)
            {
                //外部业务直接控制pop window时，才根据window配置决定是否销毁
                onWinDestroy?.Invoke();
                GameObject.Destroy(this.gameObject);
            }
            else if (!isFromStackPop && panelTransformCache is { Count: <= 0 })
            {
                //如果是因为panel close而触发的window出栈(比如 window里最后一个panel 调用了closeSelf).是否销毁window节点取决于window里面还有没有panel缓存
                onWinDestroy?.Invoke();
                GameObject.Destroy(this.gameObject);
            }
        }

        #endregion

        public void Clear()
        {
            panels?.Clear();
            panelTransformCache?.Clear();
        }
    }
}