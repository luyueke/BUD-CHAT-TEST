using System;
using Basic.UndoRedo;
using Game.AssetToolBox;
using Game.Audio;
using Game.Base;
using Game.Config;
using Game.ECS;
using Game.Props.PropsComponents;
using Game.Props.PropsManagers;
using Game.Scene.ModeController;
using GameData;
using Message;
using UGCAsset;
using UI.Base;
using UI.BaseWidgets;
using UI.EditOperation;
using UI.Manager;
using UI.UIPanels.GameEdit;
using UndoSystem;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Author:fsc
/// Desc:地图编辑器主UI
/// Date:23-07-19 14:19:13
/// </summary>
public class GameEditModePanel : BasePanel<GameEditModePanel>
{

    [Header("ModeHandler")] public ModelHandleView HandleView;
    private const string TAG = "GameEditModePanel";
    private LoadingButton saveMapBtn;
    private CButton exitMapBtn;
    private Text versionText;
    public CButton playBtn;
    private CButton undoBtn;
    private CButton redoBtn;
    private CButton showBtn;
    private CButton combineBtn;
    private GameObject menuView;
    private CButton menuBtn;
    private CButton menuCloseBtn;
    private CButton coverPhotoBtn;
    private CButton gridBtn;
    private CButton globalBtn;
    private SearchInputView input_search;

    private AssetToolBoxPanel _toolBoxPanel;
    private ProfilerView _profilerView;
    public override void OnCreate()
    {
        // 编辑态进场景时确保地图 AI 伙伴编排器已初始化（注册装配监听 + 首次兜底扫描已放置伙伴）；
        // 其为跨场景持久化的 GlobalInstance，配合 AIBuddyInMapManager.OnEdit 的重新广播，保证重进场景原有伙伴也加载。
        _ = UI.UIPanels.AINPC.AIBuddyInMap.AIBuddyInMapUIManager.Inst;
        MessageHelper.AddListener(MessageName.UpdateUndoView, UpdateUndoBtnView);
        InputHandlerManager.Inst.AddUnSelectAllListener(OnUnSelectAll);
        InputHandlerManager.Inst.AddSelectEntityListener(OnSelectEntity);

        menuView = GameObjectEx.FindChildByName(transform, "MenuView").gameObject;
        menuBtn = GameObjectEx.FindChildByName(transform, "MenuButton").GetComponent<CButton>();
        menuCloseBtn = GameObjectEx.FindChildByName(transform, "MenuCloseButton").GetComponent<CButton>();
        undoBtn = GameObjectEx.FindChildByName(transform, "UndoBtn").GetComponent<CButton>();
        redoBtn = GameObjectEx.FindChildByName(transform, "RedoBtn").GetComponent<CButton>();
        showBtn = GameObjectEx.FindChildByName(transform, "ShowBtn").GetComponent<CButton>();
        combineBtn = GameObjectEx.FindChildByName(transform, "PackBtn").GetComponent<CButton>();
        exitMapBtn = GameObjectEx.FindChildByName(transform, "ExitMapBtn").GetComponent<CButton>();
        saveMapBtn = GameObjectEx.FindComponentByName<LoadingButton>(transform, "Panel/TopMenuPanel/SaveBtn");
        coverPhotoBtn = GameObjectEx.FindChildByName(transform, "PhotoButton").GetComponent<CButton>();
        versionText = GameObjectEx.FindChildByName(transform, "Version").GetComponent<Text>();
        gridBtn = GameObjectEx.FindChildByName(transform, "GridBtn").GetComponent<CButton>();
        globalBtn = GameObjectEx.FindChildByName(transform, "GlobalBtn").GetComponent<CButton>();
        _toolBoxPanel = GameObjectEx.FindChildByName(transform, "AssetToolBoxPanel").GetComponent<AssetToolBoxPanel>();
        _profilerView = GameObjectEx.FindChildByName(transform, "ProfilerView").GetComponent<ProfilerView>();
        menuBtn.onClick.AddListener(OnMenuClick);
        menuCloseBtn.onClick.AddListener(OnMenuCloseClick);
        coverPhotoBtn.onClick.AddListener(OnCoverPhotoClick);
        undoBtn.onClick.AddListener(OnUndoClick);
        redoBtn.onClick.AddListener(OnRedoClick);
        saveMapBtn.onClick.AddListener(DoSaveMap);
        exitMapBtn.onClick.AddListener(DoExitMap);
        playBtn.onClick.AddListener(OnClickedPlayMode);
        showBtn.onClick.AddListener(OnClickShow);
        combineBtn.onClick.AddListener(OnCombineBtnClick);
        gridBtn.onClick.AddListener(OnGridBtnClick);
        globalBtn.onClick.AddListener(OnGlobalBtnClick);
        // versionText.text = $"版本  {DeviceInfoManager.Inst.DeviceBaseData.version}";
        versionText.SetLocalText("版本 {0}",DeviceInfoManager.Inst.DeviceBaseData.version);

        InitUI();
        InitController();
        _profilerView.InitData(LimitType.Map);
    }

    private void InitController()
    {
        InitSceneController();
        InitEditOperationRules();
    }

    private void InitUI()
    {
        _toolBoxPanel.Init();
        HandleView.InitView();
        HandleView.OnUnSelectAll();
        menuView.SetActive(false);
        UpdateUndoBtnView();

        var inputSearchGo = GameObjectEx.FindChildByName(transform, "input_search");
        if (inputSearchGo != null)
            input_search = inputSearchGo.GetComponent<SearchInputView>();
        if (input_search != null)
        {
            input_search.SetOnConfirm(_toolBoxPanel.SearchByKeyword);
            input_search.SetOnClear(() => _toolBoxPanel.SearchByKeyword(""));
        }
    }

    private void InitEditOperationRules()
    {
        EditOperationManager.Inst.Init();
        HandleView.InitOperationRule();
    }

    private void InitSceneController()
    {
        GizmoManager.Inst.CurGizmoCtrl.SetLockingEditAction(OnLockingEdit);
        GizmoManager.Inst.CurGizmoCtrl.LimitMoveSize = Vector3.one * GlobalNodeManager.Inst.Get<TerrainManager>().GetTerrainComponent().size * 100;
        GizmoManager.Inst.CurGizmoCtrl.IsLimitMoveSize = false;
        GizmoManager.Inst.CurGizmoCtrl.IsLimitGroundOn = false;
    }

    public SceneEntity GetCurSelectEntity()
    {
        var curObj = GizmoManager.Inst.CurGizmoCtrl.GetCurrentTarget();
        if (curObj == null) return null;
        NodeBaseBehaviour nodeBehav = curObj.GetComponent<NodeBaseBehaviour>();
        return nodeBehav.entity;
    }

    private void OnClickedPlayMode()
    {
        GameController.ChangeMode(GameMode.Play, () =>
        {
            InputHandlerManager.Inst.UnSelectAll();
            UIManager.Inst.OpenPanel(PanelId.GamePlayPanel, GameMode.Play);
            UIManager.Inst.OpenPanel(PanelId.UIOperationOnWorldPanel);
            CloseSelf();
        });
    }

    private void OnClickShow()
    {
        UndoRecordUtils.AddShowAllRecrod();
        LockHideManager.Inst.ClearHideList();
        InputHandlerManager.Inst.UnSelectAll();
    }

    private void OnGridBtnClick()
    {
        //TODO:尚未添加的功能
        TipPanel.ShowToast("功能开发中，敬请期待");
    }

    private void OnGlobalBtnClick()
    {
        //TODO:尚未添加的功能
        TipPanel.ShowToast("功能开发中，敬请期待");
    }

    private void OnCombineBtnClick()
    {
        InputHandlerManager.Inst.UnSelectAll();
        Action closeCombineCallback = OnCombinePanelClose;
        UIManager.Inst.OpenPanel(PanelId.CombinePanel,closeCombineCallback);
        UIManager.Inst.ClosePanel(this);
    }

    private void OnCoverPhotoClick()
    {
        InputHandlerManager.Inst.UnSelectAll();
        Action closeCoverCallback = OnCoverPanelClose;
        UIManager.Inst.OpenPanel(PanelId.CoverPanel,closeCoverCallback);
        UIManager.Inst.ClosePanel(this);
    }

    private void OnCombinePanelClose()
    {
        LoggerUtils.Log($"{TAG} OnCombinePanelClose");
        UIManager.Inst.OpenPanel(PanelId.GameEditModePanel);
    }

    private void OnCoverPanelClose()
    {
        LoggerUtils.Log($"{TAG} OnCoverPanelClose");
        UIManager.Inst.OpenPanel(PanelId.GameEditModePanel);
    }


    private void DoSaveMap()
    {
        LoggerUtils.Log($"{TAG} DoSaveMap");
        saveMapBtn.SetLoadingVisible(true);
        GameController.GetModeController<EditModeController>(GameMode.Edit).SaveData( isSuccess =>
        {
            if (gameObject == null)
            {
                return;
            }
            if (isSuccess)
            {
                TipPanel.ShowToast("保存成功:D");
            }
            else
            {
                TipPanel.ShowToast("保存失败");
            }
            saveMapBtn.SetLoadingVisible(false);
        });
    }


    private void DoExitMap()
    {
        LoggerUtils.Log($"{TAG} DoExitMap");


        CommonConfirmPanel commonConfirmPanel = UIManager.Inst.OpenPanel<CommonConfirmPanel>(PanelId.CommonConfirmPanel);
        commonConfirmPanel.SetLocalText("确认保存","保存当前的创作进度吗？", "保存", "不保存");
        commonConfirmPanel.SetIsCloseSelf(false);
        commonConfirmPanel.SetOnClickAction(() =>
        {
            commonConfirmPanel.SetConfirmLoadingVisible(true);
            GameController.GetModeController<EditModeController>(GameMode.Edit).SaveData((isSuccess) =>
            {
                if (commonConfirmPanel != null && commonConfirmPanel.gameObject != null)
                {
                    commonConfirmPanel.Close();
                }
                GameController.ExitGame( () =>
                {
                    UIManager.Inst.ForceSetOtherWindowTransInStack(WindowId.GameEditModeWindow, true);
                    UIManager.Inst.BackToLastWindow();
                    MessageHelper.Broadcast(DraftMessage.RefreshDraft);
                });
            });

        }, () =>
        {
            if (commonConfirmPanel != null && commonConfirmPanel.gameObject != null)
            {
                commonConfirmPanel.Close();
            }

            GameController.ExitGame(() =>
            {
                UIManager.Inst.ForceSetOtherWindowTransInStack(WindowId.GameEditModeWindow, true);
                UIManager.Inst.BackToLastWindow();
                MessageHelper.Broadcast(DraftMessage.RefreshDraft);
            });

        });
    }

    private void OnMenuClick()
    {
        InputHandlerManager.Inst.UnSelectAll();
        menuView.SetActive(true);
    }

    private void OnMenuCloseClick()
    {
        menuView.SetActive(false);
    }


    private void OnUndoClick()
    {
        UndoRedoManager.Inst.Undo();
        UpdateUndoBtnView();
    }


    private void OnRedoClick()
    {
        UndoRedoManager.Inst.Redo();
        UpdateUndoBtnView();
    }

    public void OnSkyboxClick()
    {
        GamePropNodeManager.Inst.TryCreateInEdit("20100025",out var behaviour);
    }

    private void UpdateUndoBtnView()
    {
        LoggerUtils.Log($"{TAG} UpdateUndoBtnView");
        bool hasUndo = (UndoRecordPool.Inst.GetUndoCount() > 0);
        undoBtn.transform.GetChild(0).gameObject.SetActive(hasUndo);
        undoBtn.transform.GetChild(1).gameObject.SetActive(!hasUndo);

        bool hasRedo = (UndoRecordPool.Inst.GetRedoCount() > 0);
        redoBtn.transform.GetChild(0).gameObject.SetActive(hasRedo);
        redoBtn.transform.GetChild(1).gameObject.SetActive(!hasRedo);
    }

    public override void OnShow(params object[] args)
    {
    }

    public override void OnHidden()
    {
    }

    protected override void OnDestroy()
    {
        MessageHelper.RemoveListener(MessageName.UpdateUndoView, UpdateUndoBtnView);

        InputHandlerManager.Inst.RemoveUnSelectAllListener(OnUnSelectAll);
        InputHandlerManager.Inst.RemoveSelectEntityListener(OnSelectEntity);

    }
    
    public override void OnWindowBeFocused()
    {
    }

    public override void OnWindowPop()
    {
    }

    #region Scene control

    private void OnUnSelectAll()
    {
        DisableAllPanel();
        _toolBoxPanel.gameObject.SetActive(true);
        GizmoManager.Inst.CurGizmoCtrl?.DisableGizmo();
    }

    private void OnSelectEntity(SceneEntity entity)
    {
        DisableAllPanel();
        _toolBoxPanel.gameObject.SetActive(false);
        GizmoManager.Inst.CurGizmoCtrl?.SetTarget(entity.GetViewGo());
        HandleView.OnSelectEntity(entity);
        AkSoundManager.Inst.PlayUIEffectSound(UISoundType.UI_Asset_D1);
    }

    private void OnLockingEdit(string msg)
    {
        TipPanel.ShowToast(msg);
    }

    #endregion

    private void DisableAllPanel()
    {
        menuView.SetActive(false);
        _toolBoxPanel.gameObject.SetActive(false);
        HandleView.OnUnSelectAll();
        if (UIManager.Inst.TryFindPanel(PanelId.GameGlobalSettingPanel, out var settingPanel))
        {
            UIManager.Inst.ClosePanel(settingPanel);
        }

        if (UIManager.Inst.TryFindPanel(PanelId.GamePropertyEditPanel, out var oldPanel))
        {
            UIManager.Inst.ClosePanel(oldPanel);
        }

        var panels = UIManager.Inst.FindPanels<IPropertyPanel>();
        if (panels != null && panels.Count > 0)
        {
            foreach (var panel in panels)
            {
                if (panel is BasePanel convertedPanel)
                {
                    UIManager.Inst.ClosePanel(convertedPanel);
                }
            }
        }
    }
}
