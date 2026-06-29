using System;
using System.Collections.Generic;
using UI;
using UI.Base;
using UIAgent;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.U2D;


public class UIManager : GlobalInstance<UIManager>
{
    private UIWindowModule _windowModule;

    private Transform _uiRoot;

    public Transform UIRoot
    {
        get
        {
            if (_uiRoot == null)
            {
                InitUIRoot();
            }

            return _uiRoot;
        }
    }

    private Canvas _canvas;

    public Canvas Canvas
    {
        get
        {
            if (_canvas == null)
            {
                _canvas = UIRoot.Find("Canvas").GetComponent<Canvas>();
            }

            return _canvas;
        }
    }

    private Action<BasePanel> _openPanelAction;
    private Action<BasePanel> _closePanelAction;

    private void InitUIRoot()
    {
        var newUIRootGo = GameObject.Find("UIRoot");
        if(newUIRootGo == null)
        {
            var uiRootGo = Loader.Load<GameObject>("Assets/Arts/Prefabs/UIRoot.prefab").RetainAsset();
            newUIRootGo = GameObject.Instantiate(uiRootGo);
            newUIRootGo.DontDestroy();
        }

        _uiRoot = newUIRootGo.transform;
        _uiRoot.gameObject.name = "UIRoot";
        XAssetLoaderMgr.Inst.LoadResource<SpriteAtlas>(XAssetLoaderMgr.Inst.GetSpriteAltasPath(SpriteAtlasType.Common), _uiRoot.gameObject);
    }

    public void Init()
    {
        Profiler.BeginSample($"{nameof(UIManager)} Init");
        InitUIRoot();
        _windowModule = new UIWindowModule();
        RegisUIAgent();
        MobileInterface.Instance.OpenDebugKeyboardUIAction = OpenDebugKeyboardPanel;
        MobileInterface.Instance.OpenDebugKeyboardWithEmoteUIAction = OpenDebugKeyboardPanelWithEmote;
        
        // 初始化KCC调试手势管理器
        //KCCDebugGestureManager.Inst.Init();
        
        Profiler.EndSample();
    }

    public override void Release()
    {
        base.Release();
        _windowModule?.Release();

        if (GameObject.Find(nameof(MobileInterface)))
        {
            MobileInterface.Instance.OpenDebugKeyboardUIAction = null;
            MobileInterface.Instance.OpenDebugKeyboardWithEmoteUIAction= null;
        }
    }

    #region UI Agent

    private void RegisUIAgent()
    {
        UIAgentManager.Inst.OpenPanelEventHandler += OpenPanelByAgent;
        UIAgentManager.Inst.ClosePanelEventHandler += ClosePanelByAgent;
        UIAgentManager.Inst.FindPanelEventHandler += FindPanelOnAgent;
        UIAgentManager.Inst.CallPanelMethodEventHandler += CallPanelMethod;
        UIAgentManager.Inst.SwapPanelEventHandler += SwapPanelByAgent;
        UIAgentManager.Inst.AssetDetailGetInfoHandler += AssetDetailUtil.GetInfo;
        UIAgentManager.Inst.BatchGetInfoHandle += AssetDetailUtil.GetBatchInfo;
    }

    private void OpenPanelByAgent(PanelId pId, WindowId wId, params object[] args)
    {
        OpenPanel(pId, wId, args);
    }

    private void SwapPanelByAgent(PanelId pId, params object[] args)
    {
        SwapPanel(pId, args);
    }

    private void ClosePanelByAgent(WindowId wId, PanelId pId)
    {
        ClosePanel(wId, pId);
    }

    private void CallPanelMethod(PanelId pId, string methodName, params object[] args)
    {
        if (TryFindPanel(pId, out var panel))
        {
            var method = panel.GetType().GetMethod(methodName);
            if (method != null)
            {
                method.Invoke(panel, args);
            }
            else
            {
                LoggerUtils.LogError($"CallPanelMethod: Not found method, Panel:{pId} Method:{methodName}");
            }
        }
        else
        {
            LoggerUtils.LogError($"CallPanelMethod: Not found panel, Panel:{pId} Method:{methodName}");
        }
    }

    private void OpenDebugKeyboardPanel(string info)
    {
        OpenPanel(PanelId.DebugInputPanel, info);
    }

    private void OpenDebugKeyboardPanelWithEmote(string npcId)
    {
        OpenPanel(PanelId.DebugInputPanel, true, npcId);
    }

    #endregion

    #region Open

    /// <summary>
    /// 在指定window打开panel,如果当前WindowId传None，则默认在栈顶window打开，若栈顶无window，则会抛出error
    /// </summary>
    public T OpenPanel<T>(PanelId pId, WindowId wId, params object[] args) where T : BasePanel
    {
        return _windowModule.OpenPanel<T>(pId, wId, args);
    }

    public T OpenPanel<T>(PanelId pId, params object[] args) where T : BasePanel
    {
        return _windowModule.OpenPanel<T>(pId, WindowId.None, args);
    }

    public T OpenPanelTakeAni<T>(PanelId pId, params object[] args) where T : BasePanel
    {
        var panel = _windowModule.OpenPanel<T>(pId, WindowId.None, args);
        panel?.OpenAnimation();
        return panel;
    }

    public BasePanel OpenPanel(PanelId pId, WindowId wId, params object[] args)
    {
        return _windowModule.OpenPanel<BasePanel>(pId, wId, args);
    }

    public BasePanel OpenPanel(PanelId pId, params object[] args)
    {
        return _windowModule.OpenPanel<BasePanel>(pId, WindowId.None, args);
    }

    public BasePanel OpenPanelTakeAni(PanelId pId, params object[] args)
    {
        var panel = _windowModule.OpenPanel<BasePanel>(pId, WindowId.None, args);
        panel?.OpenAnimation();
        return panel;
    }

    public BasePanel OpenPanel(PanelId pId, Action openAction, Action closeAction, params object[] args)
    {
        var panel = _windowModule.OpenPanel<BasePanel>(pId, WindowId.None, args);
        if (panel!=null&&(openAction!=null||closeAction!=null))
        {
            panel.SetPanelActions(openAction,closeAction);
        }
        return panel;
    }

    public BasePanel OpenPanelTakeAni(PanelId pId, Action openAction, Action closeAction, params object[] args)
    {
        var panel = _windowModule.OpenPanel<BasePanel>(pId, WindowId.None, args);
        panel?.OpenAnimation();
        if (panel != null && (openAction != null || closeAction != null))
        {
            panel.SetPanelActions(openAction, closeAction);
    
        }
        return panel;
    }

    public BasePanel OpenPanel(PanelId pId,WindowId wId, Action openAction, Action closeAction, params object[] args)
    {
        var panel = _windowModule.OpenPanel<BasePanel>(pId, wId, args);
        if (panel != null && (openAction != null || closeAction != null))
        {
            panel.SetPanelActions(openAction, closeAction);
        }
        return panel;
    }

    //如果同Window里已经相同的弹窗，则关闭上一个
    public BasePanel SwapPanel(PanelId pId,params object[] args)
    {
        var lastPanel = FindPanel(pId);
        if (lastPanel != null)
        {
            lastPanel.CloseSelf();
        }
        
        return _windowModule.OpenPanel<BasePanel>(pId, WindowId.None, args);
    }

    #endregion

    #region Close

    public void ClosePanel(BasePanel panel)
    {
        _windowModule?.ClosePanel(panel);
   
    }

    public void CloseCommonPanel(PanelId panelId)
    {
        ClosePanel(WindowId.CommonWindow, panelId);
    }

    public void ClosePanel(PanelId panelId)
    {
        if (TryFindPanel(panelId, out var panel))
        {
            ClosePanel(panel);
        }
        else
        {
            LoggerUtils.Log($"Not found panel:{panelId} when closing panel.");
        }
    }

    public void ClosePanel(WindowId windowId, PanelId panelId)
    {
        if (TryFindPanel(windowId, panelId, out var panel))
        {
            ClosePanel(panel);
        }
        else
        {
            LoggerUtils.Log($"Not found panel:{panelId} when closing panel.");
        }
    }

    #endregion

    #region Hide Panel

    /// <summary>
    /// 隐藏同window下的，除了panel参数以外的所有其他panel, 不影响window栈，会回调到Panel的OnHide
    /// </summary>
    public void HideAllOtherPanelInWindow(BasePanel panel)
    {
        _windowModule?.HideAllOtherPanelInWindow(panel);
    }

    /// <summary>
    /// 显示同window下的，除了panel参数以外的所有其他panel, 不影响window栈，会回调到Panel的OnShow
    /// </summary>
    public void ShowAllOtherPanelInWindow(BasePanel panel)
    {
        _windowModule?.ShowAllOtherPanelInWindow(panel);
    }

    #endregion

    #region Get Panel

    public T FindPanelInCommonWindow<T>(PanelId panelId) where T : BasePanel
    {
        return FindPanel<T>(WindowId.CommonWindow, panelId);
    }

    public T FindPanel<T>(WindowId windowId, PanelId panelId) where T : BasePanel
    {
        return _windowModule.FindPanel<T>(windowId, panelId);
    }

    public bool FindPanelOnAgent(WindowId windowId, PanelId panelId)
    {
        return TryFindPanel(windowId, panelId,out var panel);
    }
    
    public BasePanel FindPanel(WindowId windowId, PanelId panelId)
    {
        return FindPanel<BasePanel>(windowId, panelId);
    }

    public List<T> FindPanels<T>(WindowId windowId)
    {
        return _windowModule.FindPanels<T>(windowId);
    }

    public List<T> FindPanels<T>()
    {
        if (_windowModule?.CurShowingWindow == null || _windowModule?.CurShowingWindow.config == null)
        {
            return null;
        }

        return _windowModule.FindPanels<T>((WindowId)_windowModule.CurShowingWindow.config.WindowId);
    }


    /// <summary>
    /// 从当前栈顶Window找对应panel
    /// </summary>
    public T FindPanel<T>(PanelId panelId) where T : BasePanel
    {
        if (_windowModule?.CurShowingWindow == null || _windowModule?.CurShowingWindow.config == null)
        {
            return null;
        }

        return FindPanel<T>((WindowId)_windowModule.CurShowingWindow.config.WindowId, panelId);
    }

    public BasePanel FindPanel(PanelId panelId)
    {
        return FindPanel<BasePanel>(panelId);
    }

    public bool TryFindPanel<T>(WindowId windowId, PanelId panelId, out T panel) where T : BasePanel
    {
        panel = null;
        var p = FindPanel<T>(windowId, panelId);
        if (!p) return false;
        panel = p;
        return true;
    }

    public bool TryFindPanel(WindowId windowId, PanelId panelId, out BasePanel panel)
    {
        return TryFindPanel<BasePanel>(windowId, panelId, out panel);
    }

    public bool TryFindPanel<T>(PanelId panelId, out T panel) where T : BasePanel
    {
        panel = null;
        if (_windowModule?.CurShowingWindow == null || _windowModule?.CurShowingWindow.config == null)
        {
            return false;
        }

        return TryFindPanel<T>((WindowId)_windowModule.CurShowingWindow.config.WindowId, panelId, out panel);
    }

    public bool TryFindPanel(PanelId panelId, out BasePanel panel)
    {
        return TryFindPanel<BasePanel>(panelId, out panel);
    }

    #endregion

    #region Window Control

    /// <summary>
    /// 显示栈中的window, 非栈顶window会被移出栈
    /// </summary>
    public void SwitchWindow(WindowId windowId)
    {
        _windowModule?.SwitchStackWindowShow(windowId);
    }

    /// <summary>
    /// 从栈中回退当前window，显示上一个
    /// </summary>
    public void BackToLastWindow()
    {
        _windowModule?.PopCurWindow();
    }

    /// <summary>
    /// 强制控制除wId以外的window节点显隐，不会影响ui栈和panel生命周期。
    /// 危险方法，调用时不会判断此前window状态，仅允许在切换场景/游戏模式时使用
    /// </summary>
    public void ForceSetOtherWindowTransInStack(WindowId wId, bool isShow)
    {
        _windowModule?.ForceSetOtherWindowTransInStack(wId, isShow);
    }

    /// <summary>
    /// <para>临时设置除TargetWindow以外的window的节点显示/隐藏</para>
    /// <para>1.调用时会判断之前window节点状态，例如已经隐藏的则不会再隐藏</para>
    /// <para><i>2.使用后会锁定, 其他人再调用则无效, 要记得及时调用 UnLockControlledOtherWindow 解锁！！</i></para>
    /// <para>3.不影响UI栈和生命周期</para>
    /// </summary>
    public void ControlOtherWindowShowWithLock(WindowId targetWinId, bool isShow)
    {
        _windowModule?.ControlOtherWindowShowWithLock(targetWinId, isShow);
    }

    /// <summary>
    /// 恢复之前控制过的window，并解除此前由于调用ControlOtherWindowShowWithLock导致的锁定
    /// </summary>
    public void UnLockControlledOtherWindow()
    {
        _windowModule?.UnLockControlledOtherWindow();
    }

    public BaseWindow GetCurWindow()
    {
        return _windowModule?.CurShowingWindow;
    }

    public int GetCurWindowId()
    {
        return _windowModule.CurShowingWindow.config.WindowId;
    }

    #endregion

    #region Action Listener

    public void AddOpenPanelAction(Action<BasePanel> action)
    {
        if (action != null)
        {
            _openPanelAction += action;
        }
    }

    public void RemoveOpenPanelAction(Action<BasePanel> action)
    {
        if (action != null)
        {
            _openPanelAction -= action;
        }
    }

    public void CallOpenPanelAct(BasePanel panel)
    {
        _openPanelAction?.Invoke(panel);
    }

    public void AddClosePanelAction(Action<BasePanel> action)
    {
        if (action != null)
        {
            _closePanelAction += action;
        }
    }

    public void RemoveClosePanelAction(Action<BasePanel> action)
    {
        if (action != null)
        {
            _closePanelAction -= action;
        }
    }

    public void CallClosePanelAct(BasePanel panel)
    {
        _closePanelAction?.Invoke(panel);
    }

    #endregion

    #region Utils

    public static PanelId GetPanelId(BasePanel panel)
    {
        return (PanelId)panel.Config.PanelId;
    }

    public Camera CreateBgCanvas(GameObject userObj)
    {
        var bgCanvasPrefab = Loader.Load<GameObject>("Assets/Arts/Prefabs/BgCamera.prefab").RetainAsset(userObj);
        var bgCanvas = GameObject.Instantiate(bgCanvasPrefab);
        bgCanvas.transform.localScale = Vector3.one;
        return bgCanvas.GetComponentInChildren<Camera>();
    }

    #endregion
}