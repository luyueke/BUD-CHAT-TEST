using System.Collections.Generic;
using System.Linq;
using EasySpreadsheet;
using Es;
using UI.Base;
using UnityEngine;
using Object = UnityEngine.Object;
using xasset;
#if UNITY_EDITOR
using UI.Performance;
#endif

namespace UI
{
    public class UIWindowModule : IUIModule
    {
        private UIWindowStack _windowStack;

        //所有window的Cache，一旦window节点销毁，才从_allWindows移除
        private Dictionary<WindowId, BaseWindow> _allWindowsCache;

        //当前正在显示的window
        public BaseWindow CurShowingWindow => _windowStack?.Peek();

        public UIWindowModule()
        {
            Init();
        }

        public void Init()
        {
            _windowStack ??= new UIWindowStack();
            _allWindowsCache ??= new Dictionary<WindowId, BaseWindow>();

            Debug.Log(nameof(UIWindowModule) + " Init success");
        }

        public void Release()
        {
            _windowStack?.ClearAll();
            _allWindowsCache?.Clear();
        }

#region Window管理

        private bool IsWindowValued(BaseWindow window)
        {
            return window != null && window.config != null;
        }

        private void AddWindowCache(BaseWindow window)
        {
            if (!IsWindowValued(window)) return;
            _allWindowsCache.TryAdd((WindowId)window.config.WindowId, window);
        }

        private void RemoveWindowCache(BaseWindow window)
        {
            if (!IsWindowValued(window)) return;
            var windowId = (WindowId)window.config.WindowId;
            if (_allWindowsCache.ContainsKey(windowId))
            {
                _allWindowsCache.Remove(windowId);
            }
        }

        private void AddWindowToStack(BaseWindow window)
        {
            if (!IsWindowValued(window)) return;
            if (window.config.IsControlByStack)
            {
                _windowStack.Push(window);
            }
        }

        private void RemoveWindowFromStack(BaseWindow window)
        {
            if (!IsWindowValued(window)) return;
            if (window.config.IsControlByStack)
            {
                _windowStack.RemoveWindow(window);
            }
        }

        private BaseWindow CreateWindow(WindowId wId)
        {
            return CreateWindow((int)wId);
        }

        private BaseWindow CreateWindow(int winId)
        {
            var winConfig = Es.DataTables.GetUIWindow(winId);
            if (winConfig == null) return null;
            var newNode = new GameObject(winConfig.Name).transform;
            newNode.parent = UIManager.Inst.Canvas.transform;
            newNode.localPosition = Vector3.zero;
            newNode.localScale = Vector3.one;
            newNode.gameObject.layer = LayerMask.NameToLayer("UI");
            newNode.gameObject.SetActive(true);
            var win = newNode.gameObject.AddComponent<BaseWindow>();
            win.config = winConfig;
            win.gameObject.SetActive(false);
            AddWindowCache(win);
            return win;
        }

        private List<BasePanel> FindPanels(BaseWindow window, PanelId panelId)
        {
            List<BasePanel> retPanelList = null;
            if (window != null && window.panels.Count > 0)
            {
                foreach (var panel in window.panels)
                {
                    if (panel.Config.PanelId == (int)panelId)
                    {
                        retPanelList ??= new List<BasePanel>();
                        retPanelList.Add(panel);
                    }
                }
            }

            return retPanelList;
        }

        public List<T> FindPanels<T>(WindowId windowId)
        {
            var window = FindWindow(windowId);
            if (window == null || window.panels.Count <= 0) return null;
            List<T> result = new List<T>();
            foreach (var p in window.panels)
            {
                if (p is T convertedPanel)
                {
                    result.Add(convertedPanel);
                }
            }

            return result;
        }

        private BaseWindow FindWindow(int windowId)
        {
            if (_allWindowsCache.TryGetValue((WindowId)windowId, out var window))
            {
                return window;
            }

            return null;
        }

        private BaseWindow FindWindow(WindowId windowId)
        {
            return FindWindow((int)windowId);
        }

        private BaseWindow FindOrCreateWindow(WindowId winId)
        {
            var win = FindWindow(winId);
            return win != null ? win : CreateWindow(winId);
        }

        private bool AddPanelToWindow(BaseWindow window, BasePanel panel)
        {
            return window != null && window.AddPanel(panel);
        }

        private bool RemovePanelFromWindow(BaseWindow window, BasePanel panel)
        {
            return window != null && window.RemovePanel(panel);
        }

        // window里所有panel都被移出后，window也自动出栈
        private void OnWindowPanelRemoved(BaseWindow window)
        {
            // var childPanels = window.gameObject.GetAllChildren();
            // var childPanelCaches = window.panelTransformCache;
            // var isChildCacheEmpty = childPanelCaches == null || childPanelCaches.Count <= 0;
            if (window.panels.Count <= 0)
            {
                RemoveWindowFromStack(window);
                window.PopFromStack(() => { RemoveWindowCache(window); }, false);

                if (CurShowingWindow)
                {
                    CurShowingWindow.BeShow();
                    CurShowingWindow.BeFocus();
                }
            }
            else
            {
                var isCover = window.panels.Any(x => x.Config.FullScreen == 1);
                if (!isCover)
                {
                    var lastWin = _windowStack.GetLastWindow(CurShowingWindow);
                    if (lastWin)
                    {
                        lastWin.BeShow();
                    }
                }
            }
        }

        private bool IsWindowControlByStack(WindowId windowId)
        {
            var window = FindWindow(windowId);
            return window != null && window.config.IsControlByStack;
        }

        public void SwitchStackWindowShow(WindowId windowId)
        {
            var window = FindWindow(windowId);
            if (window == null) return;
            if (IsWindowControlByStack(windowId) == false)
            {
                SwitchNoStackControlWindowShow(window);
            }
            else
            {
                SwitchStackControlWindowShow(window);
            }
        }

        // 不受栈控制的window
        private void SwitchNoStackControlWindowShow(BaseWindow window)
        {
            window.gameObject.SetActive(true);
            window.transform.SetAsLastSibling();
            window.BeFocus();
        }

        // 受Stack控制的window
        private void SwitchStackControlWindowShow(BaseWindow window)
        {
            if (CurShowingWindow != null && CurShowingWindow == window)
            {
                //已经在栈顶，直接显示(只有window被创建时activeSelf才是false)
                if (CurShowingWindow.gameObject.activeSelf == false)
                {
                    CurShowingWindow.gameObject.SetActive(true);
                    CurShowingWindow.transform.SetAsLastSibling();
                    CurShowingWindow.BeFocus();
                    var isCover = CurShowingWindow.panels.Any(x => x.Config.FullScreen == 1);
                    var lastWin = _windowStack.GetLastWindow(CurShowingWindow);
                    if (lastWin)
                    {
                        lastWin.BeCovered(isCover);
                    }
                }
            }
            else
            {
                //非栈顶window，进行切换
                var ret = _windowStack.TryPopToTarget(window, (popWindow) =>
                {
                    if (!IsWindowValued(popWindow)) return;
                    popWindow.PopFromStack(() => { RemoveWindowCache(popWindow); });
                });

                if (ret)
                {
                    window.gameObject.SetActive(true);
                    window.transform.SetAsLastSibling();
                    window.BeFocus();
                }
            }
        }

        public void PopCurWindow()
        {
            if (_windowStack.TryPop(out var popWindow))
            {
                popWindow.PopFromStack(() => { RemoveWindowCache(popWindow); });

                if (CurShowingWindow)
                {
                    CurShowingWindow.BeShow();
                    CurShowingWindow.BeFocus();
                }
            }
        }

#endregion

#region Panel管理

        public T OpenPanel<T>(PanelId pId, WindowId wId, params object[] args) where T : BasePanel
        {
#if UNITY_EDITOR
    var monitor = new UIPanelPerformanceMonitor();
    monitor.StartMonitor(pId.ToString());
#endif
            var cfg = Es.DataTables.GetUIPanel((int)pId);
            if (cfg == null)
            {
                Debug.LogError($"OpenPanelFailed : can not find config {pId}");
                return null;
            }

            if (wId == WindowId.None)
            {
                //None->找配置数据->配置表中不配winId,则在当前栈顶window打开
                var cfgWid = (WindowId)cfg.WindowId;
                wId = cfgWid == WindowId.None && CurShowingWindow ? (WindowId)CurShowingWindow.config.WindowId : cfgWid;
            }

            var window = FindOrCreateWindow(wId);
            if (window == null)
            {
                Debug.LogError($"Can not find window of panel:{pId}");
                return null;
            }

            BasePanel panel = null;
            if (cfg.IsAllowMultiInstance)
            {
                panel = CreatePanel(cfg, window);
            }
            else
            {
                var panelNode = window.GetPanelNodeCache((PanelId)cfg.PanelId);
                panel = panelNode == null ? CreatePanel(cfg, window) : panelNode;
            }

            if (!AddPanelToWindow(window, panel))
            {
                Debug.Log($"Can not add BasePanel:{pId} to window:{window.name}");
                panel.gameObject.SetActive(true);
                return panel as T;
            }

            AddWindowToStack(window);
            SwitchStackWindowShow(wId);
            panel.gameObject.SetActive(true);
            panel.OnShow(args);
            UIManager.Inst.CallOpenPanelAct(panel);
#if UNITY_EDITOR
            monitor.CollectPanelInfo(pId.ToString(),panel.gameObject);
            monitor.CompleteMonitor(pId.ToString());
#endif
            return panel as T;
        }

        public void ClosePanel(BasePanel panel)
        {
            if (panel == null) return;
            var cfg = panel.Config;
            var window = panel.BelongWindow;
            if (window == null)
            {
                Debug.LogError($"ClosePanel Failed, not found belong window in panel:{panel.name}");
                return;
            }

            if (RemovePanelFromWindow(window, panel))
            {
                panel.OnHidden();
                UIManager.Inst.CallClosePanelAct(panel);
                panel.gameObject.SetActive(false);

                if (cfg.IsDestroyOnClose)
                {
                    window.RemovePanelTransCache(panel);
                    Object.Destroy(panel.gameObject);
                }

                OnWindowPanelRemoved(window);
            }
        }

        public T FindPanel<T>(WindowId windowId, PanelId panelId) where T : BasePanel
        {
            var window = FindWindow(windowId);
            if (window == null) return null;
            return window.FindPanelByPanelId(panelId) as T;
        }

        private BasePanel CreatePanel(UIPanel panelConfig, BaseWindow window = null)
        {
            var obj = LoadUIGameObject(panelConfig);
            var panel = obj.GetComponent<BasePanel>();
            if (panel == null)
            {
                Debug.LogError($"Can not find BasePanel script in prefab {panelConfig.ResPath}");
                return null;
            }

            panel.Config = panelConfig;
            panel.transform.SetParent(window.transform, true);
            panel.BelongWindow = window;
            panel.OnCreate();
            return panel;
        }

        private GameObject LoadUIGameObject(UIPanel config)
        {
            //Debug.Log("LoadUIGameObject config.ResPath=" + config.ResPath);
            if (!string.IsNullOrEmpty(config.ResPath))
            {
                // var dialogObjWrapper = Loader.Load<GameObject>(config.ResPath);
                // return dialogObjWrapper.Instantiate(UIManager.Inst.UICanvasRootRoot);

                var aotReq = Asset.Load(config.ResPath, typeof(GameObject));
                return GameObject.Instantiate(aotReq.asset as GameObject, UIManager.Inst.Canvas.transform);
            }

            // var dialogPrefab = ResManager.Inst.LoadRes<GameObject>("Prefabs/UI/Panel/" + config.Name);
            var dialogPrefab = Resources.Load<GameObject>($"UIPanel/{config.Name}/{config.Name}");
            return GameObject.Instantiate(dialogPrefab, UIManager.Inst.Canvas.transform);
        }

        public void HideAllOtherPanelInWindow(BasePanel panel)
        {
            if (panel == null || panel.BelongWindow == null) return;
            var win = panel.BelongWindow;
            foreach (var p in win.panels)
            {
                if (p != panel && p.gameObject.activeSelf)
                {
                    p.OnHidden();
                    UIManager.Inst.CallClosePanelAct(p);
                    p.gameObject.SetActive(false);
                }
            }
        }

        public void ShowAllOtherPanelInWindow(BasePanel panel)
        {
            if (panel == null || panel.BelongWindow == null) return;
            var win = panel.BelongWindow;
            foreach (var p in win.panels)
            {
                if (p != panel && !p.gameObject.activeSelf)
                {
                    p.gameObject.SetActive(true);
                    p.OnShow();
                    UIManager.Inst.CallOpenPanelAct(p);
                }
            }
        }

#endregion

#region window显隐控制

        //危险方法，尽量避免使用
        public void ForceSetOtherWindowTransInStack(WindowId wId, bool isShow)
        {
            foreach (var w in _windowStack._windows)
            {
                if (w.config.WindowId == (int)wId)
                {
                    continue;
                }

                w.gameObject.SetActive(isShow);
            }
        }

        private WindowControlLock winControlLock;
        
        private class WindowControlLock
        {
            public bool lockOperation; // visible or invisible
            public List<BaseWindow> lockedWindows = new List<BaseWindow>();
        }

        public void ControlOtherWindowShowWithLock(WindowId targetWinId, bool isShow)
        {
            if (winControlLock != null)
            {
                LoggerUtils.LogError("SetOtherWindowShowWithLock failed, already been locked !!!");
                return;
            }

            var tempList = new List<BaseWindow>();
            foreach (var w in _windowStack._windows)
            {
                if (w.config.WindowId == (int)targetWinId)
                {
                    continue;
                }

                if (w.gameObject.activeSelf != isShow)
                {
                    w.gameObject.SetActive(isShow);
                    tempList.Add(w);
                }
            }

            winControlLock = new WindowControlLock()
            {
                lockOperation = isShow,
            };
            winControlLock.lockedWindows.Clear();
            winControlLock.lockedWindows.AddRange(tempList);
        }

        public void UnLockControlledOtherWindow()
        {
            if (winControlLock is not { lockedWindows: { Count: > 0 } })
            {
                return;
            }

            foreach (var w in winControlLock.lockedWindows)
            {
                if (w.gameObject)
                {
                    w.gameObject.SetActive(!winControlLock.lockOperation);
                }
            }
            
            winControlLock.lockedWindows.Clear();
            winControlLock = null;
        }

#endregion
    }
}