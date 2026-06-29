using System;
using System.Collections.Generic;
using Basic.UndoRedo;
using Game.Base;
using Game.ECS;
using Game.Props.PropsBehaviours;
using Game.Utils;
using GameData;
using GameData.BaseInfo;
using GameData.Manager;
using UI.Base;
using UI.BaseWidgets;
using UI.EditOperation;
using UI.Manager;
using UndoSystem;
using UnityEngine;
using UnityEngine.EventSystems;


/// <summary>
/// Author:
/// Desc:
/// Date:23-07-27 10:51:41
/// </summary>
///
public class EntitySelectState
{
    public SceneEntity entity;
    public bool isSelect;
}
public class CombinePanel : BasePanel<CombinePanel>
{
    private CButton backBtn;
    private CButton confirmBtn;
    private Action onReturnCallback;
    private Camera mainCamera;
    private float maxCamDist = 350;
    private float firstTouchTime = 0;
#if UNITY_EDITOR 
    private readonly float shortTouchThreshold = 1f;
#else
    private readonly float shortTouchThreshold = 0.2f;
#endif

    private bool _isVehicleEditMode;
    private Dictionary<SceneEntity, EntitySelectState> selects;
    public override void OnCreate()
    {
        selects = new Dictionary<SceneEntity, EntitySelectState>();
        mainCamera = GameCameraUtils.Inst.GetMainCamera();
        backBtn = GameObjectEx.FindChildByName(transform, "BackBtn").GetComponent<CButton>();
        confirmBtn = GameObjectEx.FindChildByName(transform, "ConfirmBtn").GetComponent<CButton>();
        backBtn.onClick.AddListener(OnBackBtnClick);
        confirmBtn.onClick.AddListener(OnConfirmBtnClick);
        confirmBtn.gameObject.SetActive(false);
    }

    public override void OnShow(params object[] args)
    {
        InputHandlerManager.Inst.SetIsCanSelect(false);
        _isVehicleEditMode = GameDataManager.Inst.mapGlobalData.curUgcBaseInfo is VehicleInfo;
        if (args.Length == 0)
        {
            return;
        }

        Action callback = args[0] as Action;
        onReturnCallback += callback;

        EditOperationManager.Inst.TriggerPropRuleStart(OperationType.CanCombine);
        if (_isVehicleEditMode)
        {
            SceneCharge(true);
        }
    }

    protected override void Update()
    {
        if (Input.touchCount == 1)
        {
            HandleSingleTouch();
            
        }
    }

    private void HandleSingleTouch()
    {
        var curTouch = Input.GetTouch(0);
        if (EventSystem.current.IsPointerOverGameObject(curTouch.fingerId))
        {
            return;
        }

        if (curTouch.phase == TouchPhase.Began)
        {
            firstTouchTime = Time.timeSinceLevelLoad;
        }

        if (curTouch.phase == TouchPhase.Ended)
        {
            if (Time.timeSinceLevelLoad - firstTouchTime < shortTouchThreshold)
            {
                OnSelectTarget(curTouch);
            }
        }
    }

    private void OnSelectTarget(Touch touch)
    {
        LoggerUtils.Log("####OnSelectTarget");
        Ray ray = mainCamera.ScreenPointToRay(touch.position);
        bool isHit = Physics.Raycast(ray, out RaycastHit hit, 2 * maxCamDist, 
            1 << LayerMask.NameToLayer("Model"));
            // | 1 << LayerMask.NameToLayer("SpecialModel")
            // | 1 << LayerMask.NameToLayer("Touch"));
        if (isHit)
        {
            var go = hit.collider.gameObject;
            var nodeBehav = go.GetComponentInParent<NodeBaseBehaviour>();
            var entity = GamePropUtils.GetCanControllerNode(nodeBehav.gameObject);
            OnSelectEntity(entity);
            UpdateConfirmBtnState();
        }
    }

    private void OnSelectEntity(SceneEntity entity)
    {
        if (selects.ContainsKey(entity))
        {
            EntitySelectState data = selects[entity];
            data.isSelect = !data.isSelect;
            EntityHighLight(data.entity, data.isSelect);
            selects.Remove(entity);
        }
        else
        {
            EntitySelectState data = new EntitySelectState();
            data.entity = entity;
            data.isSelect = true;
            EntityHighLight(data.entity, data.isSelect);
            selects.Add(entity, data);
        }
    }

    private void UpdateConfirmBtnState()
    {
        int acount = 0;
        foreach (var val in selects.Values)
        {
            if (val.isSelect)
            {
                acount++;
            }
        }

        confirmBtn.gameObject.SetActive(acount > 1);
    }
    
    private void EntityHighLight(SceneEntity entity, bool isHigh)
    {
        var entityGo = entity.GetViewGo();
        var nodeBehav = entityGo.GetComponentsInChildren<NodeBaseBehaviour>();
        for (int i = 0; i < nodeBehav.Length; i++)
        {
            HighLight(nodeBehav[i], isHigh);
        }
    }

    private void HighLight(NodeBaseBehaviour baseBehav,bool isHigh)
    {
        baseBehav.HighLight(isHigh);
    }

    private void OnBackBtnClick()
    {
        if (_isVehicleEditMode)
        {
            SceneCharge(false);
        }
        foreach (var data in selects.Values)
        {
            EntityHighLight(data.entity, false);
        }
        CloseSelf();
    }

    private void OnConfirmBtnClick()
    {
        if (_isVehicleEditMode)
        {
            SceneCharge(false);
        }
        //操作前
        var entitys = new List<SceneEntity>(selects.Keys);
        CombineUndoData beginData = new CombineUndoData();
        beginData.combineUndoMode = (int)UndoRedoConfig.CombineUndoMode.Combine;
        beginData.InitMultiData(entitys);
        
        var entity = GamePropNodeManager.Inst.CombineNode(new List<SceneEntity>(selects.Keys));
        CombineBehaviour combineBev = entity.GetViewGo().GetComponent<CombineBehaviour>();
        combineBev.CombineSuccess();
        EntityHighLight(entity, false);
        
        //操作后
        CombineUndoData endData = new CombineUndoData();
        endData.combineUndoMode = (int)UndoRedoConfig.CombineUndoMode.Combine;
        endData.InitCombinedData(entity);
        UndoRecord record = new UndoRecord(UndoHelperName.CombineUndoHelper);
        record.BeginData = beginData;
        record.EndData = endData;
        AddRecord(record);
        CloseSelf();
    }
    
    public void AddRecord(UndoRecord record)
    {
        UndoRecordPool.Inst.PushRecord(record);
    }

    public override void OnHidden()
    {
        onReturnCallback?.Invoke();
        InputHandlerManager.Inst.SetIsCanSelect(true);
        
        EditOperationManager.Inst.TriggerPropRuleEnd(OperationType.CanCombine);
    }

    protected override void OnDestroy()
    {
        onReturnCallback = null;
    }
    
    public override void OnWindowBeFocused()
    {
    }

    public override void OnWindowPop()
    {
    }

    private void SceneCharge(bool enable)
    {
        float num = enable ? 5f : 1 / 5f;
        GameObject gdgt_terrain_PREFAB = GameObject.Find("Scene/Stage/gdgt_terrain_PREFAB");
        if (gdgt_terrain_PREFAB != null && gdgt_terrain_PREFAB.transform.childCount > 0)
        {
            for (int i = 0; i < gdgt_terrain_PREFAB.transform.childCount; i++)
            {
                gdgt_terrain_PREFAB.transform.GetChild(i).localScale *= num;
            }
            if (enable)
            {
                TempSetMaterial(115);
            }
            else
            {
                TempSetMaterial(0);
            }
        }

    }

    private void TempSetMaterial(int matId)
    {
        var root = GameObject.Find("Scene/Stage/gdgt_terrain_PREFAB");
        if (root == null)
        {
            return;
        }

        var mpb = new MaterialPropertyBlock();
        var unionId = new MaterialUnionID(matId);

        for (int i = 0; i < root.transform.childCount; i++)
        {
            var child = root.transform.GetChild(i);
            if (child == null) continue;

            var renderers = child.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0) continue;

            // 复用游戏内材质加载逻辑（支持普通材质/UGC材质）
            var loader = new GameMaterialLoader(child.gameObject, renderers, mpb);
            loader.Load(unionId);
        }
    }
}