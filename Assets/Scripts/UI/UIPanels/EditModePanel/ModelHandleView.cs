using System;
using System.Collections.Generic;
using System.Linq;
using Game;
using Game.Base;
using Game.ECS;
using RTG;
using UI.Manager;
using Pb.Map;
using UndoSystem;
using UnityEngine;
using UnityEngine.UI;
using Game.Config;
using Game.Utils;
using Game.Props.PropsBehaviours;
using GameData.BaseInfo;
using GameData.MapData;
using Google.Protobuf;
using Newtonsoft.Json;
using UGCAsset.Draft;
using UI;
using UI.EditOperation;
using UI.EditOperation.Rules;
using UnityEngine.EventSystems;
using UGCAsset;

public class ModelHandleView : MonoBehaviour
{
    #region UI Elements
    private Button moveBtn;
    private Button rotateBtn;
    private Button scaleBtn;
    private Button deleteBtn;
    private Button unCombineBtn;
    private Toggle lockToggle;
    private Button uploadBtn;
    private Button hideBtn;
    private Button duplicateBtn;

    private Button addXBtn;
    private Button subXBtn;
    private Button addYBtn;
    private Button subYBtn;
    private Button addZBtn;
    private Button subZBtn;

    private CoToggle posCoToggle;
    private CoToggle rotCoToggle;
    private CoToggle scaCoToggle;
    private CoToggleGroup coGroup;

    private Text rotInputText;
    private GameObject rotExtraGo;
    private Toggle fixAngleToggle;

    private Button norAddBtn;
    private Button norSubBtn;
    private GameObject axisGo;
    private GameObject extraGo;
    private GameObject stepPanel;
    private GameObject xAxisGo;
    private GameObject yAxisGo;
    private GameObject zAxisGo;
    private GameObject scaleLabel;
    private GameObject scaleExtraGo;
    private Toggle scaleToggle;

    #endregion

    #region Params

    private float moveDefSnap = 0.1f;
    private float scaleDefSnap = 0.1f;
    private float fixRotDefSnap = 1;
    private float fixScaleDefSnap = 0.1f;
    private float minScaleVal = 0.01f;

    private Vector3 targetLastScale;
    private HandleMode handleMode = HandleMode.Move;
    private bool fixScaEnable = false;
    SceneEntity curSelectEntity;

    public Action OnDestroyNode;

    #endregion

    #region UI Control

    private const int DefFixAngle = 15;

    public void InitView()
    {
        moveBtn = GameObjectEx.FindChildByName(transform, "MoveBtn").GetComponent<Button>();
        rotateBtn = GameObjectEx.FindChildByName(transform, "RotateBtn").GetComponent<Button>();
        scaleBtn = GameObjectEx.FindChildByName(transform, "ScaleBtn").GetComponent<Button>();
        unCombineBtn = GameObjectEx.FindChildByName(transform, "UnCombineBtn").GetComponent<Button>();
        uploadBtn = GameObjectEx.FindChildByName(transform, "UploadBtn").GetComponent<Button>();
        hideBtn = GameObjectEx.FindChildByName(transform, "HideBtn").GetComponent<Button>();
        duplicateBtn = GameObjectEx.FindChildByName(transform, "DuplicateBtn").GetComponent<Button>();
        lockToggle = GameObjectEx.FindChildByName(transform, "LockToggle").GetComponent<Toggle>();
        deleteBtn = GameObjectEx.FindChildByName(transform, "DeleteBtn").GetComponent<Button>();


        addXBtn = GameObjectEx.FindChildByName(transform, "XAxis/X+").GetComponent<Button>();
        subXBtn = GameObjectEx.FindChildByName(transform, "XAxis/X-").GetComponent<Button>();
        addYBtn = GameObjectEx.FindChildByName(transform, "YAxis/Y+").GetComponent<Button>();
        subYBtn = GameObjectEx.FindChildByName(transform, "YAxis/Y-").GetComponent<Button>();
        addZBtn = GameObjectEx.FindChildByName(transform, "ZAxis/Z+").GetComponent<Button>();
        subZBtn = GameObjectEx.FindChildByName(transform, "ZAxis/Z-").GetComponent<Button>();


        norAddBtn = GameObjectEx.FindChildByName(transform, "ExtraAxis/Z+").GetComponent<Button>();
        norSubBtn = GameObjectEx.FindChildByName(transform, "ExtraAxis/Z-").GetComponent<Button>();
        coGroup = GameObjectEx.FindChildByName(transform, "HandlePanel").GetComponent<CoToggleGroup>();
        scaleExtraGo = GameObjectEx.FindChildByName(transform, "CheckMarks").gameObject;
        scaleToggle = GameObjectEx.FindChildByName(scaleExtraGo, "ScaleCheckMark").GetComponent<Toggle>();
        stepPanel = GameObjectEx.FindChildByName(transform, "StepPanel").gameObject;
        extraGo = GameObjectEx.FindChildByName(transform, "ExtraAxis").gameObject;
        extraGo = GameObjectEx.FindChildByName(transform, "ExtraAxis").gameObject;

        axisGo = GameObjectEx.FindChildByName(transform, "StepPanel/Axis").gameObject;
        xAxisGo = GameObjectEx.FindChildByName(axisGo, "XAxis").gameObject;
        yAxisGo = GameObjectEx.FindChildByName(axisGo, "YAxis").gameObject;
        zAxisGo = GameObjectEx.FindChildByName(axisGo, "ZAxis").gameObject;

        scaleLabel = GameObjectEx.FindChildByName(scaleExtraGo, "Label").gameObject;
        rotExtraGo = GameObjectEx.FindChildByName(stepPanel, "RotExtra").gameObject;
        rotInputText = GameObjectEx.FindChildByName(rotExtraGo, "RotLabel").GetComponent<Text>();
        fixAngleToggle = GameObjectEx.FindChildByName(rotExtraGo, "FixRotToggle").GetComponent<Toggle>();


        moveBtn.onClick.AddListener(OnMoveClick);
        rotateBtn.onClick.AddListener(OnRotateClick);
        scaleBtn.onClick.AddListener(OnScaleClick);
        deleteBtn.onClick.AddListener(OnDeleteClick);
        unCombineBtn.onClick.AddListener(OnUnCombineClick);
        uploadBtn.onClick.AddListener(OnUploadClick);
        hideBtn.onClick.AddListener(OnHideClick);
        duplicateBtn.onClick.AddListener(OnDuplicateClick);
        lockToggle.onValueChanged.AddListener(OnLockValueChange);
        fixAngleToggle.onValueChanged.AddListener(OnFixAngleChange);


        posCoToggle = moveBtn.GetComponent<CoToggle>();
        rotCoToggle = rotateBtn.GetComponent<CoToggle>();
        scaCoToggle = scaleBtn.GetComponent<CoToggle>();
        addXBtn.onClick.AddListener(() => OnChangeXYZClick(1, 0));
        subXBtn.onClick.AddListener(() => OnChangeXYZClick(-1, 0));
        addYBtn.onClick.AddListener(() => OnChangeXYZClick(1, 1));
        subYBtn.onClick.AddListener(() => OnChangeXYZClick(-1, 1));
        addZBtn.onClick.AddListener(() => OnChangeXYZClick(1, 2));
        subZBtn.onClick.AddListener(() => OnChangeXYZClick(-1, 2));

        var trigger = rotInputText.GetComponent<EventTrigger>();
        EventTrigger.Entry onSelect = new EventTrigger.Entry();
        onSelect.eventID = EventTriggerType.PointerClick;
        onSelect.callback.AddListener(Select);
        trigger.triggers.Add(onSelect);

        scaleToggle.onValueChanged.AddListener(OnScaToggleClick);
        norAddBtn.onClick.AddListener(OnNorAddClick);
        norSubBtn.onClick.AddListener(OnNorSubClick);

        SetRotStepAngle(DefFixAngle);
        GizmoManager.Inst.CurGizmoCtrl.SetRotSnapEnabled(fixAngleToggle.isOn);

    }

    private void Select(BaseEventData data)
    {
        string str = rotInputText.text;
        KeyBoardInfo keyBoardInfo = new KeyBoardInfo
        {
            type = 0,
            placeHolder = "",
            inputMode = 1,
            maxLength = 3,
            inputFlag = 0,
            lengthTips = LocalizationManager.Inst.GetLocalizedText("字数超出限制"),
            defaultText = str,
            textSecurity =  1,
            returnKeyType = (int)ReturnType.Return
        };
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, ShowKeyBoard);
        MobileInterface.Instance.ShowKeyboard(JsonConvert.SerializeObject(keyBoardInfo));
    }

    private void ShowKeyBoard(string str)
    {
        if (string.IsNullOrEmpty(str)) return;
        int v;
        if (int.TryParse(str, out v))
        {
            if (v >= 1 && v <= 360)
            {
                SetRotStepAngle(v);

            }
            else
            {
                TipPanel.ShowToast("请输入正确的值");
            }
        }
        else
        {
            TipPanel.ShowToast("请输入正确的值");
        }

        MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);
    }

    private void SetRotStepAngle(int v)
    {
        rotInputText.text = v + "";
        fixRotDefSnap = v;
        RTGApp.Get.GetComponentInChildren<RTGizmosEngine>().RotationGizmoSettings3D.SetAxisSnapStep(0, fixRotDefSnap);
        RTGApp.Get.GetComponentInChildren<RTGizmosEngine>().RotationGizmoSettings3D.SetAxisSnapStep(1, fixRotDefSnap);
        RTGApp.Get.GetComponentInChildren<RTGizmosEngine>().RotationGizmoSettings3D.SetAxisSnapStep(2, fixRotDefSnap);
    }

    private void OnScaToggleClick(bool enable)
    {
        fixScaEnable = enable;
        scaleToggle.targetGraphic.enabled = !enable;
        axisGo.SetActive(!fixScaEnable);
        extraGo.SetActive(fixScaEnable);
        scaleLabel.SetActive(fixScaEnable);
    }

    private void OnNorAddClick()
    {
        if (CheckLockState()) return;
        var curTarget = GizmoManager.Inst.CurGizmoCtrl.GetCurrentTarget();
        if (curTarget == null)
            return;
        Vector3 newScaled = GetUniformStepScale(curTarget.transform.localScale, 1);
        ChangeTargetScale(curTarget, newScaled);
    }

    private void OnNorSubClick()
    {
        if (CheckLockState()) return;
        var curTarget = GizmoManager.Inst.CurGizmoCtrl.GetCurrentTarget();
        if (curTarget == null)
            return;
        Vector3 newScaled = GetUniformStepScale(curTarget.transform.localScale, -1);
        for (int i = 0; i < 3; i++)
        {
            if (newScaled[i] < minScaleVal)
            {
                newScaled[i] = minScaleVal;
            }
        }

        ChangeTargetScale(curTarget, newScaled);
    }

    private Vector3 GetUniformStepScale(Vector3 current, float dir)
    {
        List<float> curVals = new List<float>();
        for (int i = 0; i < 3; i++)
        {
            if (current[i] > fixScaleDefSnap)
            {
                curVals.Add(current[i]);
            }
        }

        int index = 2; //default z axis
        float min = current[index];
        if (curVals.Count != 0)
        {
            min = curVals.Min();
            for (int i = 0; i < 3; i++)
            {
                if (current[i].Equals(min))
                {
                    index = i;
                    break;
                }
            }
        }

        Vector3 newVec = Vector3.zero;
        newVec[index] = min + dir * fixScaleDefSnap;
        for (int i = 0; i < 3; i++)
        {
            if (i != index)
            {
                newVec[i] = newVec[index] * current[i] / current[index];
            }
        }

        return newVec;
    }


    private void ResetView()
    {
        moveBtn.gameObject.SetActive(true);
        rotateBtn.gameObject.SetActive(true);
        scaleBtn.gameObject.SetActive(true);
        deleteBtn.gameObject.SetActive(true);
        unCombineBtn.gameObject.SetActive(false);

        var curTarget = GizmoManager.Inst.CurGizmoCtrl.GetCurrentTarget();
        var combineBehaviour = curTarget.GetComponent<CombineBehaviour>();
        unCombineBtn.gameObject.SetActive(combineBehaviour != null);
    }

    #endregion

    #region Rules

    public void InitOperationRule()
    {
        EditOperationManager.Inst.BindRuleUI(OperationType.Move, moveBtn.gameObject, xAxisGo, yAxisGo, zAxisGo);
        EditOperationManager.Inst.BindRuleUI(OperationType.Rotate, rotateBtn.gameObject, xAxisGo, yAxisGo, zAxisGo);
        EditOperationManager.Inst.BindRuleUI(OperationType.Scale, scaleBtn.gameObject, xAxisGo, yAxisGo, zAxisGo, scaleToggle.gameObject);
        EditOperationManager.Inst.BindRuleUI(OperationType.Copy, duplicateBtn.gameObject);
        EditOperationManager.Inst.BindRuleUI(OperationType.Lock, lockToggle.gameObject);
        EditOperationManager.Inst.BindRuleUI(OperationType.Hide, hideBtn.gameObject);
        EditOperationManager.Inst.BindRuleUI(OperationType.PublishProp, uploadBtn.gameObject);
        EditOperationManager.Inst.BindRuleUI(OperationType.CanDelete, deleteBtn.gameObject);
    }

    /// <summary>
    /// 素材编辑器没有 上传/
    /// </summary>
    public void InitUgcItemOperateRule()
    {
        EditOperationManager.Inst.BindRuleUI(OperationType.Move, moveBtn.gameObject, xAxisGo, yAxisGo, zAxisGo);
        EditOperationManager.Inst.BindRuleUI(OperationType.Rotate, rotateBtn.gameObject, xAxisGo, yAxisGo, zAxisGo);
        EditOperationManager.Inst.BindRuleUI(OperationType.Scale, scaleBtn.gameObject, xAxisGo, yAxisGo, zAxisGo, scaleExtraGo);
        EditOperationManager.Inst.BindRuleUI(OperationType.Copy, duplicateBtn.gameObject);
        EditOperationManager.Inst.BindRuleUI(OperationType.Lock, lockToggle.gameObject);
        EditOperationManager.Inst.BindRuleUI(OperationType.Hide, hideBtn.gameObject);
        EditOperationManager.Inst.BindRuleUI(OperationType.CanDelete, deleteBtn.gameObject);

        uploadBtn.gameObject.SetActive(false);
    }

    #endregion

    #region Move

    public void OnMoveClick()
    {
        GizmoManager.Inst.CurGizmoCtrl.SetMoveCtr();
        handleMode = HandleMode.Move;
        posCoToggle.IsOn = true;
        SwitchStepPanelShow(HandleMode.Move);
        EditOperationManager.Inst.TriggerRule(OperationType.Move, BasicAxisRule.ControlType.Axis);
    }

    public void ResetBtnStatus()
    {
        moveBtn.gameObject.SetActive(true);
        rotateBtn.gameObject.SetActive(true);
        scaleBtn.gameObject.SetActive(false);
        unCombineBtn.gameObject.SetActive(false);
        uploadBtn.gameObject.SetActive(false);
        hideBtn.gameObject.SetActive(false);
        duplicateBtn.gameObject.SetActive(false);
        lockToggle.gameObject.SetActive(false);
        deleteBtn.gameObject.SetActive(false);
    }

    private void StepMove(int dir, int axis)
    {
        Vector3 moveVec = Vector3.zero;
        moveVec[axis] = moveDefSnap * dir;
        GizmoManager.Inst.CurGizmoCtrl.MoveTarget(moveVec);
    }

    #endregion

    #region Rotate

    private void OnRotateClick()
    {
        GizmoManager.Inst.CurGizmoCtrl.SetRotateCtr();
        handleMode = HandleMode.Rotate;
        rotCoToggle.IsOn = true;
        SwitchStepPanelShow(HandleMode.Rotate);

        EditOperationManager.Inst.TriggerRule(OperationType.Rotate, BasicAxisRule.ControlType.Axis);
    }

    private void StepRotate(int dir, int axis)
    {
        float rotAngle = fixAngleToggle.isOn ? fixRotDefSnap : 1;
        rotAngle *= dir;
        GizmoManager.Inst.CurGizmoCtrl.RotateTarget(axis, rotAngle);
    }

    #endregion

    #region Scale

    private void OnScaleClick()
    {
        GizmoManager.Inst.CurGizmoCtrl.SetScaleCtr(() =>
        {
            GizmoManager.Inst.CurGizmoCtrl.scaleGizmo.Gizmo.PostDragBegin += OnDragBegin;
            GizmoManager.Inst.CurGizmoCtrl.scaleGizmo.Gizmo.PostDragUpdate += OnDragUpdate;
        });
        handleMode = HandleMode.Scale;
        scaCoToggle.IsOn = true;

        SwitchStepPanelShow(HandleMode.Scale);

        EditOperationManager.Inst.TriggerRule(OperationType.Scale, BasicAxisRule.ControlType.Axis);
    }

    private void OnDragBegin(Gizmo giz, int handle)
    {
        var curTarget = GizmoManager.Inst.CurGizmoCtrl.GetCurrentTarget();
        if (curTarget != null)
        {
            targetLastScale = curTarget.transform.lossyScale;
        }
    }

    private void OnDragUpdate(Gizmo giz, int handle)
    {
        var curTarget = GizmoManager.Inst.CurGizmoCtrl.GetCurrentTarget();
        if (curTarget != null)
        {
            // var constrain = curTarget.GetComponent<NodeBe>();
            // if (constrain)
            // {
            //     constrain.SetScale(curTarget.transform.lossyScale, targetLastScale);
            //     SaveTiling(constrain);
            // }

            targetLastScale = curTarget.transform.lossyScale;
        }
    }

    private void StepScale(int dir, int axis)
    {
        Vector3 scaleVec = Vector3.zero;
        scaleVec[axis] = scaleDefSnap * dir;
        var curTarget = GizmoManager.Inst.CurGizmoCtrl.GetCurrentTarget();
        if (curTarget != null)
        {
            Vector3 localScale = curTarget.transform.localScale;
            Vector3 newScaled = localScale + scaleVec;
            if (newScaled[axis] < minScaleVal)
            {
                newScaled[axis] = minScaleVal;
            }

            ChangeTargetScale(curTarget, newScaled);
        }
    }

    private void ChangeTargetScale(GameObject curTarget, Vector3 newScaled)
    {
        var lastScale = curTarget.transform.lossyScale;
        GizmoManager.Inst.CurGizmoCtrl.ScaleTarget(newScaled);
        var newScale = curTarget.transform.lossyScale;
        ChangeBaseTile(lastScale, newScale);
    }

    private void ChangeBaseTile(Vector3 lastScale, Vector3 newScale)
    {
        // if(modelMode != NodeHandleType.Base)
        //     return;
        // var curTarget = GizmoManager.Inst.CurGizmoCtrl.GetCurrentTarget();
        // var nodeBehav = curTarget.GetComponent<NodeBaseBehaviour>();
        // if (nodeBehav != null)
        // {
        //     nodeBehav.SetScale(newScale,lastScale);
        //     SaveTiling(nodeBehav);
        // }
    }

    #endregion


    private void OnDeleteClick()
    {
        if (CheckLockState()) return;
        var gController = GizmoManager.Inst.CurGizmoCtrl;
        var curObj = gController.GetCurrentTarget();
        if (curObj == null) return;
        if (WinConditionManager.Inst.OnDestroyBtnClicked(curObj)) return;

        curObj = GamePropUtils.GetSelectRealNode(curObj);
        var reason = GamePropNodeManager.Inst.TryDeleteInEdit(curObj);

        if (reason == GameGlobalEnum.NodeOpReason.RemoveSuccess)
        {
            coGroup.TurnOn(null);
            OnDestroyNode?.Invoke();
            UndoRecordUtils.AddDestroyRecord(curObj);
            GizmoManager.Inst.CurGizmoCtrl.DisableGizmo();
            InputHandlerManager.Inst.UnSelectAll();
        }
        else if (reason == GameGlobalEnum.NodeOpReason.RemoveFail_MinNum)
        {
            TipPanel.ShowToast("道具不能再删除。");
        }
    }

    private void OnUnCombineClick()
    {
        if (CheckLockState()) return;
        var gController = GizmoManager.Inst.CurGizmoCtrl;
        var curTarget = gController.GetCurrentTarget();
        if (curTarget != null)
        {
            //解组合前
            CombineBehaviour combineBehaviour = curTarget.GetComponent<CombineBehaviour>();
            if (combineBehaviour == null)
            {
                LoggerUtils.LogError("this node is not CombineNode");
                return;
            }

            SceneEntity entity = combineBehaviour.entity;
            CombineUndoData beginData = new CombineUndoData();
            beginData.combineUndoMode = (int)UndoRedoConfig.CombineUndoMode.UnCombine;
            beginData.InitCombinedData(entity);
            List<SceneEntity> entitys = new List<SceneEntity>();

            int childCount = curTarget.transform.childCount;
            for (int i = 0; i < childCount; i++)
            {
                var childNode = curTarget.transform.GetChild(0);
                childNode.SetParent(curTarget.transform.parent);
                childNode.transform.localScale = childNode.transform.localScale.LimitVector3();

                SceneEntity childEntity = childNode.GetComponent<NodeBaseBehaviour>()?.entity;
                if (childEntity != null)
                {
                    entitys.Add(childEntity);
                }
            }

            //解组合后
            CombineUndoData endData = new CombineUndoData();
            endData.combineUndoMode = (int)UndoRedoConfig.CombineUndoMode.UnCombine;
            endData.InitMultiData(entitys);

            UndoRecordUtils.AddUnCombineRecord(beginData, endData);
        }

        GamePropNodeManager.Inst.DestroyNodeToSecondCache(curTarget);
        InputHandlerManager.Inst.UnSelectAll();
    }

    private void OnHideClick()
    {
        if (CheckLockState()) return;
        SceneEntity entity = this.curSelectEntity;
        LockHideManager.Inst.HideNode(entity);
        UndoRecordUtils.AddHideRecord(entity);
        InputHandlerManager.Inst.UnSelectAll();
    }

#region 素材上传
    private void OnUploadClick()
    {
        var itemData = GamePropNodeManager.Inst.SaveUGCItemData(curSelectEntity);
        var mapMetaData = PropAssetManager.Inst.GetTemplateMetaData();
        var mapData = MapPbDataTool.ParseMapPb(mapMetaData);
        mapData.PropData.Pref.AddRange(itemData.NodeData.Prims);
        mapData.UgcmatData = itemData.UgcmatData;
        var publish = new UGCPublishStateMachine();
        publish.SetStates(new List<UGCPublishStateBase>() {
            new PropCoverState(),
            new UGCAnchorState(),
            new UGCPublishSizeState(),
            new UGCPropDetailState()
        });
        var itemInfo = new PropInfo() {
            id = GameConsts.ScenePropDraftId,
        };
        var draftInfo = PropAssetManager.Inst.GetDraftInfo(itemInfo);
        if (draftInfo == null)
        {
            draftInfo = new PropDraftInfo(itemInfo);
        }
        draftInfo.SetMetaData(itemData.ToByteArray());
        draftInfo.SetMapMetaData(mapData.ToByteArray());
        publish.SetEditData(new PropEditData() {
            draftInfo = draftInfo,
            metaDataBytes = itemData.ToByteArray(),
        });
        publish.Start();


    }
#endregion

    /// <summary>
    /// 复制逻辑
    /// </summary>
    private void OnDuplicateClick()
    {
        if (CheckLockState()) return;
        var gController = GizmoManager.Inst.CurGizmoCtrl;
        var curObj = gController.GetCurrentTarget();
        if (curObj == null) return;
        NodeBaseBehaviour newBehv;
        curObj = GamePropUtils.GetSelectRealNode(curObj);
        var reason = GamePropNodeManager.Inst.TryCloneInEidt(curObj, out newBehv);
        if (reason == GameGlobalEnum.NodeOpReason.CreateSuccess)
        {
            InputHandlerManager.Inst.SelectEntity(newBehv.entity);
            if (!FilterDuplicateRecord(newBehv))
            {
                UndoRecordUtils.AddDuplicateRecord(newBehv.gameObject);
            }
        }
        else if (reason == GameGlobalEnum.NodeOpReason.CreateFail_MaxNum)
        {
            var nodeBaseBehv = curObj.GetComponent<NodeBaseBehaviour>();
            var propConfig = GamePropDataHelper.GetPropDataByID(nodeBaseBehv.entity.GetPropConfig().Id);
            TipPanel.ShowToast($"最多支持{propConfig.MaxNum}个，已超出数量上限。");
        }
    }

    //TODO:暂时写在这里，后续可以改成配置化
    /// <summary>
    /// 过滤复制记录
    /// </summary>
    /// <param name="nodeBaseBehaviour"></param>
    /// <returns></returns>
    private bool FilterDuplicateRecord(NodeBaseBehaviour nodeBaseBehaviour)
    {
        if (nodeBaseBehaviour == null) return true;
        if (nodeBaseBehaviour is MovePointBehaviour)
        {
            return true;
        }
        return false;
    }

    #region Lock

    private void OnLockValueChange(bool isLock)
    {
        SceneEntity entity = curSelectEntity;
        //有时序性，AddLockRecord需要在SetCurLockState前面
        UndoRecordUtils.AddLockRecord(isLock, entity);
        SetCurLockState(isLock);
    }


    private bool GetCurLockState()
    {
        SceneEntity curEntity = curSelectEntity;
        if (curEntity == null) return false;
        bool lockState = LockHideManager.Inst.GetLockState(curEntity);
        return lockState;
    }

    private bool CheckLockState()
    {
        if (GetCurLockState())
        {
            TipPanel.ShowToast("需要先解锁");
        }

        return GetCurLockState();
    }

    private void SetCurLockState(bool isLock)
    {
        SceneEntity curEntity = curSelectEntity;
        LockHideManager.Inst.SetLockState(curEntity, isLock);
        SetLockUI(isLock);
        GizmoManager.Inst.CurGizmoCtrl.SetLockState(isLock);
    }

    private void SetLockUI(bool isLock)
    {
        var spUnLock = lockToggle.transform.Find("IconUnLock").gameObject;
        var spLock = lockToggle.transform.Find("IconLock").gameObject;
        if ((spUnLock != null) && (spLock != null))
        {
            spUnLock.SetActive(!isLock);
            spLock.SetActive(isLock);
        }

        lockToggle.SetIsOnWithoutNotify(isLock);
    }

    #endregion

    #region Select/Unselect Entity

    public void OnSelectEntity(SceneEntity entity)
    {
        if (entity == null)
        {
            return;
        }
        curSelectEntity = entity;
        var gameComp = entity.GetComp<GameObjectComponent>();
        var propConfig = GamePropDataHelper.GetPropDataByID(gameComp.PropId);
        UIOpenUntils.ReOpenGamePropertyEditPanel(propConfig.UIAdapter, entity);

        this.gameObject.SetActive(true);
        fixScaEnable = false;
        bool lockState = LockHideManager.Inst.GetLockState(entity);
        SetCurLockState(lockState);

        RefreshView(entity);
    }

    public void OnUnSelectAll()
    {
        curSelectEntity = null;
        this.gameObject.SetActive(false);
    }

    #endregion

    #region Top Bar

    private void SwitchStepPanelShow(HandleMode type)
    {
        switch (type)
        {
            case HandleMode.Move:
                axisGo.gameObject.SetActive(true);
                rotExtraGo.gameObject.SetActive(false);
                extraGo.gameObject.SetActive(false);
                scaleExtraGo.gameObject.SetActive(false);
                break;
            case HandleMode.Rotate:
                axisGo.SetActive(true);
                rotExtraGo.gameObject.SetActive(true);
                extraGo.gameObject.SetActive(false);
                scaleExtraGo.gameObject.SetActive(false);
                break;
            case HandleMode.Scale:
                var isSpecialScale = EditOperationManager.Inst.IsEqualProportionScale();

                axisGo.gameObject.SetActive(!isSpecialScale);
                scaleExtraGo.gameObject.SetActive(true);
                rotExtraGo.gameObject.SetActive(false);
                scaleToggle.isOn = isSpecialScale || scaleToggle.isOn;
                scaleToggle.interactable = !isSpecialScale;
                axisGo.SetActive(!scaleToggle.isOn);
                extraGo.gameObject.SetActive(scaleToggle.isOn);
                scaleLabel.SetActive(scaleToggle.isOn);

                //三向轴等比缩放
                GizmoManager.Inst.CurGizmoCtrl.scaleGizmo.Gizmo.ObjectTransformGizmo.isSpecialScale = isSpecialScale;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    private void OnChangeXYZClick(int dir, int axis)
    {
        if (CheckLockState()) return;
        switch (handleMode)
        {
            case HandleMode.Move:
                StepMove(dir, axis);
                break;
            case HandleMode.Rotate:
                StepRotate(dir, axis);
                break;
            case HandleMode.Scale:
                StepScale(dir, axis);
                break;
        }
    }

    private void OnFixAngleChange(bool isOn)
    {
        GizmoManager.Inst.CurGizmoCtrl.SetRotSnapEnabled(isOn);
    }

    #endregion

    #region HandleType处理

    private void RefreshView(SceneEntity entity)
    {
        ResetView();
        TriggerRules();
        OnMoveClick();
        CheckIsCombine(entity);
    }

    private void CheckIsCombine(SceneEntity entity)
    {
        if (entity.GetViewGo().GetComponent<NodeBaseBehaviour>() is CombineBehaviour)
        {
            unCombineBtn.gameObject.SetActive(true);
        }
    }

    private void TriggerRules()
    {
        EditOperationManager.Inst.TriggerRule(OperationType.Move);
        EditOperationManager.Inst.TriggerRule(OperationType.Rotate);
        EditOperationManager.Inst.TriggerRule(OperationType.Scale);
        EditOperationManager.Inst.TriggerRule(OperationType.Copy);
        EditOperationManager.Inst.TriggerRule(OperationType.Lock);
        EditOperationManager.Inst.TriggerRule(OperationType.Hide);
        EditOperationManager.Inst.TriggerRule(OperationType.PublishProp);

        EditOperationManager.Inst.TriggerRule(OperationType.CanDelete);
    }

    #endregion
}
