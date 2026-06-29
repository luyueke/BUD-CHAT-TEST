/**
 * @ Author: Jun Zhou
 * @ Create Time: 2023-08-30 11:32:52
 * @ Modified by: Jun Zhou
 * @ Modified time: 2023-09-06 17:36:51
 * @ Description: 素材编辑器一级界面
 */

using System;
using System.Collections.Generic;
using Basic.UndoRedo;
using System.Linq;
using Game.Base;
using Game.Config;
using Game.ECS;
using Game.Utils;
using GameData.Config;
using Message;
using UGCAsset;
using UI.Base;
using UI.BaseWidgets;
using UI.EditOperation;
using UI.Manager;
using UI.UIPanels.GameEdit;
using UndoSystem;
using UnityEngine;
using UnityEngine.U2D;
using UI.UIWidgets;
using Es;
using GameData;
using Game.Props.PropsBehaviours;
using Game.Props.PropsManagers;
using Game.Scene.ModeController;
using GameData.Manager;
using GameData.BaseInfo;
using GameData.UGCData;
using Google.Protobuf;
using Pb.Map;
using UI;
using UnityEngine.UI;

public class UGCItemEditPanel : BasePanel<UGCItemEditPanel> {
    [SerializeField] private ModelHandleView HandleView;
    [SerializeField] private Transform propListViewTF;
    [SerializeField] private GameIconSelectItem iconItemTmpl;

    const string TAG = "UGCItemEditPanel";
    List<GameIconSelectItem> items = new List<GameIconSelectItem>();
    int curPropSelectIndex = -1; // 默认选中的道具

    private LoadingButton saveMapBtn;
    private CButton previewBtn;
    private CButton exitMapBtn;
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
    private CButton floorBtn;
    private CButton envirLightBtn;
    private ProfilerView _profilerView;
    private Text versionText;

    List<NodeModelType> FilterNodeTypes = new List<NodeModelType>() {
        NodeModelType.DText,
        NodeModelType.SimpleShape
    };

    public override void OnCreate() {
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
        gridBtn = GameObjectEx.FindChildByName(transform, "GridBtn").GetComponent<CButton>();
        globalBtn = GameObjectEx.FindChildByName(transform, "GlobalBtn").GetComponent<CButton>();
        floorBtn = GameObjectEx.FindChildByName(transform, "FloorBtn").GetComponent<CButton>();
        previewBtn = GameObjectEx.FindComponentByName<CButton>(transform, "Panel/TopMenuPanel/PreviewButton");
        _profilerView = GameObjectEx.FindChildByName(transform, "ProfilerView").GetComponent<ProfilerView>();
        envirLightBtn = GameObjectEx.FindChildByName(transform, "EnvirLightBtn")?.GetComponent<CButton>();
        versionText = GameObjectEx.FindChildByName(transform, "Version").GetComponent<Text>();
        menuBtn.onClick.AddListener(OnMenuClick);
        menuCloseBtn.onClick.AddListener(OnMenuCloseClick);
        coverPhotoBtn.onClick.AddListener(OnCoverPhotoClick);
        undoBtn.onClick.AddListener(OnUndoClick);
        redoBtn.onClick.AddListener(OnRedoClick);
        saveMapBtn.onClick.AddListener(DoSave);
        exitMapBtn.onClick.AddListener(DoExit);
        showBtn.onClick.AddListener(OnClickShow);
        combineBtn.onClick.AddListener(OnCombineBtnClick);
        gridBtn.onClick.AddListener(OnGridBtnClick);
        globalBtn.onClick.AddListener(OnGlobalBtnClick);
        floorBtn.onClick.AddListener(OnFloorBtnClick);
        if (envirLightBtn)
        {
            envirLightBtn.onClick.AddListener(OnEnvirLightBtnClick);
        }
        previewBtn.onClick.AddListener(OnPropSkinPreviewBtnClick);
        InitUI();
        InitController();
        _profilerView.InitData(LimitType.Prop);
        versionText.SetLocalText("版本 {0}",DeviceInfoManager.Inst.DeviceBaseData.version);
    }


    private void InitController() {
        InitSceneController();
        InitEditOperationRules();
    }

    private void InitUI() {
        // 初始化道具选择列表
        var atlasPath = XAssetLoaderMgr.Inst.GetSpriteAltasPath(SpriteAtlasType.EditorPropSprite);
        var spriteAtlas = XAssetLoaderMgr.Inst.LoadResource<SpriteAtlas>(atlasPath, gameObject);

        iconItemTmpl.gameObject.SetActive(false);
        var propList = GamePropDataHelper.GetPropDataList();
        List<GamePropData> filterPropList = new List<GamePropData>();
        foreach (var nodeType in FilterNodeTypes) {
            var tmpList = propList.Where(x => (NodeModelType)x.ModelType == nodeType && x.Enable)
            .OrderBy(x => x.Order).ToList();
            filterPropList.AddRange(tmpList);
        }
        for (int i = 0; i < filterPropList.Count; i++) {
            var index = i;
            var propConfig = filterPropList[i];
            var iconItem = Instantiate(iconItemTmpl, propListViewTF);
            iconItem.gameObject.SetActive(true);
            iconItem.name = $"Item_{propConfig.Id}";
            iconItem.AddOnSelectListener(() => OnPropClick(propConfig, index));
            iconItem.SetIcon(spriteAtlas.GetSprite(propConfig.IconName));
            iconItem.SetSelectWithNoNotify(false);
            items.Add(iconItem);
        }

        HandleView.InitView();
        HandleView.OnUnSelectAll();
        menuView.SetActive(false);

        if (GameController.GetEnterGameModel() == EnterGameModel.UgcSkinEmpty || GameController.GetEnterGameModel() ==  EnterGameModel.UgcSkinContinueEdit) {
            previewBtn.gameObject.SetActive(true);
        } 
        else if (GameController.GetEnterGameModel() == EnterGameModel.UgcMusicalInstrumentEmpty || GameController.GetEnterGameModel() == EnterGameModel.UgcMusicalInstrumentContinueEdit)
        {
            previewBtn.gameObject.SetActive(true);
        }
        else 
        {
            previewBtn.gameObject.SetActive(false);
        }
        UpdateUndoBtnView();
    }

    private void InitEditOperationRules() {
        EditOperationManager.Inst.Init();
        HandleView.InitUgcItemOperateRule();
    }

    private void InitSceneController() {
        GizmoManager.Inst.CurGizmoCtrl.SetLockingEditAction(OnLockingEdit);
        GizmoManager.Inst.CurGizmoCtrl.LimitMoveSize = Vector3.one * GlobalNodeManager.Inst.Get<TerrainManager>().GetTerrainComponent().size * 100;
        GizmoManager.Inst.CurGizmoCtrl.IsLimitMoveSize = true;
        GizmoManager.Inst.CurGizmoCtrl.IsLimitGroundOn = true;

    }

    public SceneEntity GetCurSelectEntity() {
        var curObj = GizmoManager.Inst.CurGizmoCtrl.GetCurrentTarget();
        if (curObj == null) return null;
        NodeBaseBehaviour nodeBehav = curObj.GetComponent<NodeBaseBehaviour>();
        return nodeBehav.entity;
    }

    void OnPropClick(GamePropData gamePropData, int index) {
        if (curPropSelectIndex >= 0) items[curPropSelectIndex].SetSelectWithNoNotify(false);
        curPropSelectIndex = index;
        // 创建道具
        NodeBaseBehaviour nBehav;
        var opReason = GamePropNodeManager.Inst.TryCreateInEdit(gamePropData.Id, out nBehav);
        if (opReason == GameGlobalEnum.NodeOpReason.CreateSuccess) {
            InputHandlerManager.Inst.SelectEntity(nBehav.entity);
            UndoRecordUtils.AddCreateRecord(nBehav.gameObject);
        } else if (opReason == GameGlobalEnum.NodeOpReason.CreateFail_MaxNum) {
            var propConfig = GamePropDataHelper.GetPropDataByID(gamePropData.Id);
            TipPanel.ShowToast($"最多支持{propConfig.MaxNum}个，已超出数量上限。");
        }
    }

    private void OnClickShow() {
        UndoRecordUtils.AddShowAllRecrod();
        LockHideManager.Inst.ClearHideList();
        InputHandlerManager.Inst.UnSelectAll();
    }

    private void OnGridBtnClick() {
        //TODO:尚未添加的功能
        TipPanel.ShowToast("功能开发中，敬请期待");
    }

    private void OnGlobalBtnClick() {
        //TODO:尚未添加的功能
        TipPanel.ShowToast("功能开发中，敬请期待");
    }

    private void OnEnvirLightBtnClick()
    {
        OnUnSelectAll();
        if (!UIManager.Inst.TryFindPanel(PanelId.GamePropertyEditPanel, out var peopertyEditPanel))
        {
            peopertyEditPanel = UIManager.Inst.OpenPanel(PanelId.GamePropertyEditPanel);
        }
        if (peopertyEditPanel.gameObject.GetComponent<DirLightViewAdapter>() == null) {
            peopertyEditPanel.gameObject.AddComponent<DirLightViewAdapter>().SetSelectEntity(null);
        }
    }

    private void OnFloorBtnClick()
    {
        OnUnSelectAll();
        UIManager.Inst.ClosePanel(UIManager.Inst.FindPanel(PanelId.GameGlobalSettingPanel));
        if (!UIManager.Inst.TryFindPanel(PanelId.GamePropertyEditPanel, out var peopertyEditPanel))
        {
            peopertyEditPanel = UIManager.Inst.OpenPanel(PanelId.GamePropertyEditPanel);
        }
        if (peopertyEditPanel.gameObject.GetComponent<TerrainViewAdapter>() == null) {
            peopertyEditPanel.gameObject.AddComponent<TerrainViewAdapter>().SetSelectEntity(null);
        }
    }

    private void OnCombineBtnClick() {
        InputHandlerManager.Inst.UnSelectAll();
        Action closeCombineCallback = OnCombinePanelClose;
        UIManager.Inst.ClosePanel(this);
        UIManager.Inst.OpenPanel(PanelId.CombinePanel, closeCombineCallback);
    }

    public virtual void OnCoverPhotoClick() {

        var itemData = GamePropNodeManager.Inst.SaveUgcItemData();
        var ugcInfo = GameDataManager.Inst.mapGlobalData.curUgcBaseInfo;
        var publishStateMachine = new UGCPublishStateMachine();
        var propManager = GlobalNodeManager.Inst.Get<PropManager>();
        propManager.RemovePropData(ugcInfo.id);

        if (ugcInfo is SkinInfo skinInfo) {
            if (skinInfo.skinDetailInfo == null) {
                skinInfo.skinDetailInfo = SkinDetailInfo.FromDetailInfo(GamePropNodeManager.Inst.GetUGCItemDetailInfo());
            } else {
                skinInfo.skinDetailInfo.Assign(GamePropNodeManager.Inst.GetUGCItemDetailInfo());
            }
            var draftInfo = SkinAssetManager.Inst.GetOrCreateDraftInfo(skinInfo);
            publishStateMachine.SetStates(new List<UGCPublishStateBase>() {
                new UGCPublishStateBase(UGCPublishState.SetPropSkinCover)
            });
            publishStateMachine.SetEditData(new SkinEditData() {
                draftInfo = draftInfo,
                metaDataBytes = itemData.ToByteArray(),
            });
        } else if (ugcInfo is PropInfo propInfo) {
            propInfo.detailInfo = GamePropNodeManager.Inst.GetUGCItemDetailInfo();
            var draftInfo = PropAssetManager.Inst.GetOrCreateDraftInfo(propInfo);
            publishStateMachine.SetStates(new List<UGCPublishStateBase>() {
                new UGCPublishStateBase(UGCPublishState.SetPropCover)
            });
            publishStateMachine.SetEditData(new PropEditData() {
                draftInfo = draftInfo,
                metaDataBytes = itemData.ToByteArray(),
            });
        }


        publishStateMachine.Start();
    }
    public virtual void OnCombinePanelClose() {
        LoggerUtils.Log($"{TAG} OnCombinePanelClose");
        UIManager.Inst.OpenPanel(PanelId.UGCItemEditPanel);
    }

    private void OnCoverPanelClose() {
        LoggerUtils.Log($"{TAG} OnCoverPanelClose");
        SetScreenShotNodeLayer(false);
    }

    /// <summary>
    /// 设置截图时候的Layer
    /// </summary>
    void SetScreenShotNodeLayer(bool isScreenShot) {
        var layer = LayerMask.NameToLayer("Model");
        if (isScreenShot) {
            layer = LayerMask.NameToLayer("ShotInclude");
        }

        var ugcItemNodes = GamePropNodeManager.Inst.GetAllNodeInFirstLayer(SceneBuilder.Inst.StageParent,
            new List<Type>() { typeof(TerrainBehaviour) });

        ugcItemNodes.ForEach(ugcItemNode => {
            var nodeTfs = ugcItemNode.GetComponentsInChildren<Transform>();
            for (int i = 0; i < nodeTfs.Length; i++) {
                nodeTfs[i].gameObject.layer = layer;
            }
        });
    }

    private void DoSave() {
        // 保存
        LoggerUtils.Log($"{TAG} DoSave");
        saveMapBtn.SetLoadingVisible(true);
        GameController.GetModeController<EditModeController>(GameMode.Edit).SaveData(isSuccess => {
            TipPanel.ShowToast(isSuccess ? "保存成功:D" : "保存失败");
            saveMapBtn.SetLoadingVisible(false);
        });
    }


    public virtual void DoExit() {
        LoggerUtils.Log($"{TAG} DoExitMap");

        CommonConfirmPanel commonConfirmPanel = UIManager.Inst.OpenPanel<CommonConfirmPanel>(PanelId.CommonConfirmPanel);
        commonConfirmPanel.SetIsCloseSelf(false);
        commonConfirmPanel.SetLocalText("确认保存", "保存当前的创作进度吗？", "保存", "不保存");
        commonConfirmPanel.SetOnClickAction(() => {
            commonConfirmPanel.SetConfirmLoadingVisible(true);
            MessageHelper.Broadcast(MessageName.VehicleEditCloseRefresh);
            GameController.GetModeController<EditModeController>(GameMode.Edit).SaveData((isSuccess) =>
                {
                    if (commonConfirmPanel != null && commonConfirmPanel.gameObject != null)
                    {
                        commonConfirmPanel.Close();
                    }
                    GameController.ExitGame(() => {
                        UIManager.Inst.ForceSetOtherWindowTransInStack(WindowId.GameEditModeWindow, true);
                        UIManager.Inst.BackToLastWindow();
                        MessageHelper.Broadcast(DraftMessage.RefreshDraft);
                    });
                });

            }, () => {
                if (commonConfirmPanel != null && commonConfirmPanel.gameObject != null)
                {
                    commonConfirmPanel.Close();
                }
                GameController.ExitGame(() => {
                    UIManager.Inst.ForceSetOtherWindowTransInStack(WindowId.UGCItemEditWindow, true);
                    UIManager.Inst.BackToLastWindow();
                    MessageHelper.Broadcast(DraftMessage.RefreshDraft);
                });
            }
        );
    }

    private void OnMenuClick() {
        InputHandlerManager.Inst.UnSelectAll();
        menuView.SetActive(true);
    }

    private void OnMenuCloseClick() {
        menuView.SetActive(false);
    }

    protected virtual void OnPropSkinPreviewBtnClick() {
        var itemData = GamePropNodeManager.Inst.SaveUgcItemData();
        var ugcInfo = GameDataManager.Inst.mapGlobalData.GetCurInfo<SkinInfo>();
        var propManager = GlobalNodeManager.Inst.Get<PropManager>();
        propManager.RemovePropData(ugcInfo.id);
        if (ugcInfo.skinDetailInfo == null) {
            ugcInfo.skinDetailInfo = new SkinDetailInfo();
        }
        ugcInfo.skinDetailInfo.size = itemData.Size.ToVector3();
        var draftInfo = SkinAssetManager.Inst.GetOrCreateDraftInfo(ugcInfo);
        var publishStateMachine = new UGCPublishStateMachine();
        publishStateMachine.SetStates(new List<UGCPublishStateBase>() {
            new UGCPublishStateBase(UGCPublishState.SetPropSkinAnchor),
            new UGCPublishStateBase(UGCPublishState.SetPropSkinAdjust),
        });
        publishStateMachine.SetEditData(new SkinEditData() {
            draftInfo = draftInfo,
            metaDataBytes = itemData.ToByteArray(),
        });

        publishStateMachine.Start();
    }


    private void OnUndoClick() {
        UndoRedoManager.Inst.Undo();
        UpdateUndoBtnView();
    }


    private void OnRedoClick() {
        UndoRedoManager.Inst.Redo();
        UpdateUndoBtnView();
    }

    private void UpdateUndoBtnView() {
        LoggerUtils.Log($"{TAG} UpdateUndoBtnView");
        bool hasUndo = (UndoRecordPool.Inst.GetUndoCount() > 0);
        undoBtn.transform.GetChild(0).gameObject.SetActive(hasUndo);
        undoBtn.transform.GetChild(1).gameObject.SetActive(!hasUndo);

        bool hasRedo = (UndoRecordPool.Inst.GetRedoCount() > 0);
        redoBtn.transform.GetChild(0).gameObject.SetActive(hasRedo);
        redoBtn.transform.GetChild(1).gameObject.SetActive(!hasRedo);
    }

    protected override void OnDestroy() {
        MessageHelper.RemoveListener(MessageName.UpdateUndoView, UpdateUndoBtnView);

        if (InputHandlerManager.HasInstance) {
            InputHandlerManager.Inst.RemoveUnSelectAllListener(OnUnSelectAll);
            InputHandlerManager.Inst.RemoveSelectEntityListener(OnSelectEntity);
        }
    }

    #region Scene control

    private void OnUnSelectAll() {
        DisableAllPanel();
        GizmoManager.Inst.CurGizmoCtrl?.DisableGizmo();
    }

    private void OnSelectEntity(SceneEntity entity) {
        DisableAllPanel();
        GizmoManager.Inst.CurGizmoCtrl?.SetTarget(entity.GetViewGo());
        HandleView.OnSelectEntity(entity);

        // 反选UI
        var propId = entity.GetGameObjectComponent().PropId;
        for (int i = 0; i < items.Count; i++) {
            if (items[i].name == $"Item_{propId}") {
                if (curPropSelectIndex >= 0)
                    items[curPropSelectIndex].SetSelectWithNoNotify(false);
                items[i].SetSelectWithNoNotify(true);
                curPropSelectIndex = i;
                break;
            }
        }
    }

    private void OnLockingEdit(string msg) {
        TipPanel.ShowToast(msg);
    }

    #endregion

    protected void DisableAllPanel() {
        menuView.SetActive(false);
        HandleView.OnUnSelectAll();

        var panels = UIManager.Inst.FindPanels<IPropertyPanel>();
        if (panels != null && panels.Count > 0) {
            foreach (var panel in panels) {
                if (panel is BasePanel convertedPanel) {
                    UIManager.Inst.ClosePanel(convertedPanel);
                }
            }
        }
    }
}
