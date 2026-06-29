using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using BUD.AnimPose;
using Es;
using Game.Avatar;
using Game.Base;
using Game.KinematicCharacter;
using Game.MusicalInstrument;
using Game.Props.PropsBehaviours;
using Game.Props.PropsManagers;
using Game.Utils;
using GameData;
using GameData.Base;
using GameData.BaseInfo;
using GameData.Config;
using GameData.Manager;
using GameData.MapData;
using GameData.PgcData;
using GameData.UGCData;
using Google.Protobuf;
using Message;
using Network.Message;
using Newtonsoft.Json;
using Pb.Map;
using UGCAsset;
using UI;
using UI.BaseOSA;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Game.AssetToolBox;
using Game.ECS;
using Game.MapSetting;
using Game.Audio;
//using static UnityEditor.UIElements.CurveField;


public class UGCVehicleEditPanel : UGCItemEditPanel
{
    public CButton Btn_Drive;
    public CButton Btn_DriveAction;
    public CButton Btn_DrivePos;
    public CButton Btn_DriveSound;
    public CButton Btn_DriveAni;
    public GameObject DrivePosSelect;
    public ModelHandleView handleTool;
    public Transform SoundListTsf;
    public CButton soundMaskBtn;
    public List<CButton> SoundListBtns;

    private VehicleInfo vehicleInfo;
    private PoseRoleCreater DriverCreater;
    private PoseRoleCreater takeCreater;
    private PublishAssetBoxPanel _publishAssetPanel;
    private Coroutine _onShowInitCoroutine;

    private GameObject curTarget;
    private float _lastScaleLimitToastTime = -999f;
    private const float ScaleLimitToastCooldown = 1.5f;
    private const float MaxTargetScale = 5f;

    private GameObject _lastCachedTarget;  // 缓存上一次的目标对象
    private CombineBehaviour _cachedCombineBehaviour;  // 缓存的组件引用

    public override void OnCreate()
    {
        base.OnCreate();

        _publishAssetPanel = GameObjectEx.FindChildByName(transform, "PublishAssetBoxPanel").GetComponent<PublishAssetBoxPanel>();
        _publishAssetPanel.Init();

        Btn_Drive.onClick.AddListener(OnBtnDriveClick);
        Btn_DriveAction.onClick.AddListener(OnBtnDriveActionClick);
        Btn_DrivePos.onClick.AddListener(OnBtnDrivePosClick);
        Btn_DriveSound.onClick.AddListener(OnBtnDriveSoundClick);
        Btn_DriveAni.onClick.AddListener(OnBtnDriveAniClick);
        soundMaskBtn.onClick.AddListener(OnBtnSoundMaskClick);

        for(int i = 0; i < SoundListBtns.Count; i++)
        {
            int index = i;
            SoundListBtns[i].onClick.AddListener(() => {
                SoundListTsf.gameObject.SetActive(false);
                UIManager.Inst.OpenPanel(PanelId.VehicleAudioPanel,vehicleInfo, (VehicleAudioType)index);
            });
        }

        vehicleInfo = GameDataManager.Inst.mapGlobalData?.GetCurInfo<VehicleInfo>();

        if(vehicleInfo == null)
        {
            return;
        }

        DriverCreater = new PoseRoleCreater();
        if(vehicleInfo.vehicleType == 2)
        {
            takeCreater = new PoseRoleCreater();
        }

        if (vehicleInfo.detailInfo == null)
        {
            vehicleInfo.detailInfo = VehicleUtils.GetDefaultVehicleDetailInfo();
        }

        //ChangeDrivePosStatus();
        InputHandlerManager.Inst.AddSelectObjListener(SelectAvatarNode);
        InputHandlerManager.Inst.AddUnSelectAllListener(ChangeDrivePosStatus);
        InputHandlerManager.Inst.AddSelectEntityListener(OnSelectEntity);

        MessageHelper.AddListener(MessageName.OnTouchForVehiclePerson, ChangeDrivePosStatus);
        MessageHelper.AddListener(MessageName.VehicleEditCloseRefresh, CloseRefreshInfo);

    }

    private void OnSelectEntity(SceneEntity entity)
    {
        _publishAssetPanel.Reset();
        _publishAssetPanel.gameObject.SetActive(false);
    }
    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        if (vehicleInfo.doubleUserDetail == null)
        {
            vehicleInfo.doubleUserDetail = new VehicleDetailInfo();
        }
        if (_onShowInitCoroutine != null)
        {
            StopCoroutine(_onShowInitCoroutine);
        }
        _onShowInitCoroutine = StartCoroutine(CoOnShowInit());
    }

    private IEnumerator CoOnShowInit()
    {
        if (vehicleInfo == null)
        {
            yield break;
        }

        CreateMode(DriverCreater);
        if (vehicleInfo.vehicleType == 2)
        {
            CreateMode(takeCreater);
        }
        yield return null;

        ResetModelPos();
        PoseSelectCallBack();
        DriverCreater.OptNodes[0].transform.rotation = Quaternion.Euler(vehicleInfo.detailInfo.rDef);
        if (vehicleInfo.vehicleType == 2 && takeCreater?.OptNodes != null && takeCreater.OptNodes.Count > 0 && takeCreater.OptNodes[0] != null)
        {
            takeCreater.OptNodes[0].transform.rotation = Quaternion.Euler(vehicleInfo.doubleUserDetail.rDef);
        }
        if(vehicleInfo.vehicleType == 2 && DriverCreater.OptNodes[0] != null && takeCreater.OptNodes[0] != null)
        {
            if(DriverCreater.OptNodes[0].transform.position == takeCreater.OptNodes[0].transform.position
            && DriverCreater.OptNodes[0].transform.position == new Vector3(0, 0.5f, 0))
            {
                DriverCreater.OptNodes[0].transform.position = new Vector3(1, 0.5f, 0);
                takeCreater.OptNodes[0].transform.position = new Vector3(0, 0.5f, 0);
            }
        }
    }

    protected override void Update()
    {
        base.Update();
        curTarget = GizmoManager.Inst.CurGizmoCtrl.GetCurrentTarget();
        if(curTarget == null)
        {
            _lastCachedTarget = null;
            _cachedCombineBehaviour = null;
            return;
        }
        // 只在目标改变时重新获取组件
        if (_lastCachedTarget != curTarget)
        {
            _lastCachedTarget = curTarget;
            curTarget.TryGetComponent(out _cachedCombineBehaviour);
        }
        
        // 如果持有 CombineBehaviour，固定缩放为1
        if (_cachedCombineBehaviour != null)
        {
            if (curTarget.transform.localScale != Vector3.one)
            {
                curTarget.transform.localScale = Vector3.one;
                TryShowScaleLimitToast(1);
            }
            return;  // 直接返回，不执行后续的缩放限制逻辑
        }

        if(curTarget.transform.localScale.x > MaxTargetScale)
        {
            curTarget.transform.localScale = new Vector3(MaxTargetScale, curTarget.transform.localScale.y, curTarget.transform.localScale.z);
            TryShowScaleLimitToast(0);
        }
        else if(curTarget.transform.localScale.y > MaxTargetScale)
        {
            curTarget.transform.localScale = new Vector3(curTarget.transform.localScale.x, MaxTargetScale, curTarget.transform.localScale.z);
            TryShowScaleLimitToast(0);
        }
        else if(curTarget.transform.localScale.z > MaxTargetScale)
        {
            curTarget.transform.localScale = new Vector3(curTarget.transform.localScale.x, curTarget.transform.localScale.y, MaxTargetScale);
            TryShowScaleLimitToast(0);
        }
    }

    private void TryShowScaleLimitToast(int type)
    {
        if (Time.unscaledTime - _lastScaleLimitToastTime < ScaleLimitToastCooldown)
        {
            return;
        }
        _lastScaleLimitToastTime = Time.unscaledTime;
        
        if(type == 0)
        {
            TipPanel.ShowToast("缩放超过最大限制，已自动调整为最大限制");
        }
        else if(type == 1)
        {
            TipPanel.ShowToast("载具编辑下，组合后的物体不能调整缩放大小");
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (_onShowInitCoroutine != null)
        {
            StopCoroutine(_onShowInitCoroutine);
            _onShowInitCoroutine = null;
        }
        InputHandlerManager.Inst.RemoveSelectObjListener(SelectAvatarNode);
        InputHandlerManager.Inst.RemoveUnSelectAllListener(ChangeDrivePosStatus);
        InputHandlerManager.Inst.RemoveSelectEntityListener(OnSelectEntity);

        MessageHelper.RemoveListener(MessageName.OnTouchForVehiclePerson, ChangeDrivePosStatus);
        MessageHelper.RemoveListener(MessageName.VehicleEditCloseRefresh, CloseRefreshInfo);
        if (DriverCreater != null)
        {
            DriverCreater.Release();
        }
        if (takeCreater != null)
        {
            takeCreater.Release();
        }
        BgMusicManager.Inst.IsPlaying = true;
    }

    protected override void OnEnable()
    {
      //  Debug.LogError("edit onenable");
        base.OnEnable();
        SceneCharge(true);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
       // Debug.LogError("edit OnDisable");
        SceneCharge(false);
    }
    private void SelectAvatarNode(GameObject obj)
    {
        GizmoManager.Inst.CurGizmoCtrl.SetTarget(obj);
        _publishAssetPanel.Reset();
        _publishAssetPanel.gameObject.SetActive(false);
        handleTool.gameObject.SetActive(true);
        handleTool.OnMoveClick();
        handleTool.ResetBtnStatus();
    }

    private void CreateMode(PoseRoleCreater creater)
    {
        creater.Create(UgcPoseSubType.Single);
        CreateAnimIKs(creater);
        creater.ChangeImageModeAction(PoseImageType.WhiteBody);
    }

    public void CreateAnimIKs(PoseRoleCreater creater)
    {
        var selfData = AccountDataManager.Inst.UserInfo.avatarInfo;
        var petData = AccountDataManager.Inst.PetInfo.avatarInfo;
        creater.CreateWhiteRole(this.gameObject);
        creater.CreateRole(selfData, petData, true);
        creater.AddFullBodyJointNodeCollider();
        creater.ChangeImageModeAction(PoseImageType.Current);
    }

    private void OnBtnDriveClick()
    {
        var itemData = GamePropNodeManager.Inst.SaveUgcItemData();
        var propManager = GlobalNodeManager.Inst.Get<PropManager>();
        propManager.RemovePropData(vehicleInfo.id);
        if (vehicleInfo.detailInfo == null)
        {
            vehicleInfo.detailInfo = VehicleDetailInfo.FromDetailInfo(GamePropNodeManager.Inst.GetUGCItemDetailInfo());
        }
        vehicleInfo.detailInfo.size = itemData.Size.ToVector3();
        vehicleInfo.detailInfo.materials = GamePropNodeManager.Inst.GetUGCItemDetailInfo().materials;
        vehicleInfo.detailInfo.dTexts = GamePropNodeManager.Inst.GetUGCItemDetailInfo().dTexts;
        if(vehicleInfo.vehicleType == (int)VehicleSubType.DoubleVehicle)
        {
            vehicleInfo.doubleUserDetail.pDef = DriverCreater.OptNodes[0].transform.localPosition - takeCreater.OptNodes[0].transform.localPosition;
            vehicleInfo.doubleUserDetail.rDef = takeCreater.OptNodes[0].transform.rotation.eulerAngles;
        }
        SetVehicleDetailOffset();
        VehicleUtils.ConvertVehicleDetailInfo(vehicleInfo.detailInfo);

        var metaDataBytes = itemData.ToByteArray();
#if UNITY_EDITOR
        // 保存临时数据供编辑器或其他工具使用
        vehicleInfo.metaData = metaDataBytes;
        TempVehicleDataSave.SaveTempVehicleInfo(vehicleInfo);
#endif
        var itemPb = MapPbDataTool.ParsePropPb(metaDataBytes);
        if (itemPb != null)
        {
            if (itemPb.UgcmatData != null)
            {
                GameUgcMatManager.Inst.AddUGCMatData(itemPb.UgcmatData);
                var pNodeData = AssetPropNodeManager.Inst.GetEmptyNodeData(vehicleInfo.id);
                pNodeData.Prims.AddRange(itemPb.NodeData.Prims);
                propManager?.AddPropData(vehicleInfo.id, pNodeData);
            }
        }
        BgMusicManager.Inst.IsPlaying = false;
        GameController.ChangeMode(GameMode.Play, () =>
        {
            InputHandlerManager.Inst.UnSelectAll();
            UIManager.Inst.ClosePanel(PanelId.UGCVehicleEditPanel);
            VehicleTryPlayInfo vehicleTryPlayInfo = new VehicleTryPlayInfo()
            {
                vehicleInfo = vehicleInfo,
                isPlay = true
            };
            var vehiclePlayPanel = UIManager.Inst.OpenPanel<VehiclePlayPanel>(PanelId.VehiclePlayPanel, vehicleTryPlayInfo);
            vehiclePlayPanel.heightSetCallBack = SetVehicleHeight;
        });
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
    private void SetVehicleHeight(float height)
    {
        vehicleInfo.vehicleHeight = height;
    }

    private void SetVehicleDetailOffset()
    {
        if(DriverCreater.OptNodes == null || DriverCreater.OptNodes.Count == 0)
        {
            return;
        }
        //生成坐标系不同，这里值取反
        var combinePos = GamePropNodeManager.Inst.CombineNodePosLogic();

        var endPos = (DriverCreater.OptNodes[0].transform.position - combinePos) * -1;
        vehicleInfo.detailInfo.pDef = new Vector3(endPos.x, endPos.y + 0.5f, endPos.z);
        vehicleInfo.detailInfo.rDef = DriverCreater.OptNodes[0].transform.rotation.eulerAngles;
        if(vehicleInfo.vehicleType == (int)VehicleSubType.DoubleVehicle)
        {
            vehicleInfo.doubleUserDetail.pDef = DriverCreater.OptNodes[0].transform.localPosition - takeCreater.OptNodes[0].transform.localPosition;
            vehicleInfo.doubleUserDetail.rDef = takeCreater.OptNodes[0].transform.rotation.eulerAngles;
        }
    }

    private void ResetModelPos()
    {
        if (vehicleInfo?.detailInfo == null)
        {
            return;
        }

        if (DriverCreater?.OptNodes == null || DriverCreater.OptNodes.Count == 0 || DriverCreater.OptNodes[0] == null)
        {
            return;
        }
        if (vehicleInfo.detailInfo.pDef == Vector3.zero)
        {
            return;
        }
        var pos = vehicleInfo.detailInfo.pDef;
        pos = new Vector3(pos.x, pos.y - 0.5f, pos.z) * -1;
        var combinePos = GamePropNodeManager.Inst.CombineNodePosLogic();
        pos = pos + combinePos;
        DriverCreater.OptNodes[0].transform.position = pos;

        if (vehicleInfo.vehicleType != 2 || takeCreater == null)
        {
            return;
        }
        if(vehicleInfo.doubleUserDetail.pDef != Vector3.zero)
        {
            takeCreater.OptNodes[0].transform.position = pos - vehicleInfo.doubleUserDetail.pDef;
        }
    }


    private void OnBtnDriveActionClick()
    {
        var posePanel = UIManager.Inst.OpenPanel<VehiclePosePanel>(PanelId.VehiclePosePanel);
        posePanel.enterCallBack = PoseSelectCallBack;
    }

    private void PoseSelectCallBack()
    {
        Vector3 driverWorldPos = Vector3.zero;
        Vector3 driverLocalScale = Vector3.one;
        if (DriverCreater?.OptNodes != null && DriverCreater.OptNodes.Count > 0 && DriverCreater.OptNodes[0] != null)
        {
            var t = DriverCreater.OptNodes[0].transform;
            driverWorldPos = t.position;
            driverLocalScale = t.localScale;
        }

        Vector3 takeWorldPos = Vector3.zero;
        Vector3 takeLocalScale = Vector3.one;
        bool hasTake = takeCreater?.OptNodes != null && takeCreater.OptNodes.Count > 0 && takeCreater.OptNodes[0] != null;
        if (hasTake)
        {
            var t = takeCreater.OptNodes[0].transform;
            takeWorldPos = t.position;
            takeLocalScale = t.localScale;
        }
        //每次设置姿势时，重置旋转角度，避免被之前的旋转角度影响
        DriverCreater.OptNodes[0].transform.rotation = Quaternion.identity;
        var curKeyFrameData = JsonConvert.DeserializeObject<KeyFrameData>(vehicleInfo.curPoseData);
        DriverCreater.SetKeyFrameData(curKeyFrameData);
        if(!string.IsNullOrEmpty(vehicleInfo.doublePoseData) && takeCreater != null)
        {
            var keyFrameData = JsonConvert.DeserializeObject<KeyFrameData>(vehicleInfo.doublePoseData);
            takeCreater.SetKeyFrameData(keyFrameData);
        }

        // 恢复姿势设置前的座位坐标（避免被 SetKeyFrameData 覆盖）
        if (DriverCreater?.OptNodes != null && DriverCreater.OptNodes.Count > 0 && DriverCreater.OptNodes[0] != null)
        {
            var t = DriverCreater.OptNodes[0].transform;
            t.position = driverWorldPos;
            t.localScale = driverLocalScale;
        }
        if (hasTake)
        {
            var t = takeCreater.OptNodes[0].transform;
            t.position = takeWorldPos;
            t.localScale = takeLocalScale;
        }
    }

 
    private void OnBtnDrivePosClick()
    {
        VehicleDataManager.Inst.IsSelectDrivePos = true;
        SelectAvatarNode(DriverCreater.OptNodes[0]);
        DrivePosSelect.SetActive(VehicleDataManager.Inst.IsSelectDrivePos);
    }


    private void OnBtnDriveSoundClick()
    {
        SoundListTsf.gameObject.SetActive(!SoundListTsf.gameObject.activeSelf);
    }

    private void OnBtnDriveAniClick()
    {
        UIManager.Inst.OpenPanel(PanelId.VehicleAniPanel, vehicleInfo);
    }

    private void OnBtnSoundMaskClick()
    {
        SoundListTsf.gameObject.SetActive(false);
    }

    private void ChangeDrivePosStatus()
    {
        VehicleDataManager.Inst.IsSelectDrivePos = false;
        _publishAssetPanel.Reset();
        _publishAssetPanel.gameObject.SetActive(true);
        SetVehicleDetailOffset();
        DrivePosSelect?.SetActive(VehicleDataManager.Inst.IsSelectDrivePos);
    }

    private void CloseRefreshInfo()
    {
        DriverCreater.OptNodes[0].gameObject.SetActive(false);
        if (takeCreater != null)
        {
            takeCreater.OptNodes[0].gameObject.SetActive(false);
        }
    }

    protected override void OnPropSkinPreviewBtnClick()
    {
        var itemData = GamePropNodeManager.Inst.SaveUgcItemData();
        var propManager = GlobalNodeManager.Inst.Get<PropManager>();
        propManager.RemovePropData(vehicleInfo.id);
        if (vehicleInfo.detailInfo == null)
        {
            vehicleInfo.detailInfo = VehicleDetailInfo.FromDetailInfo(GamePropNodeManager.Inst.GetUGCItemDetailInfo());
        }
        vehicleInfo.detailInfo.size = itemData.Size.ToVector3();
        vehicleInfo.detailInfo.materials = GamePropNodeManager.Inst.GetUGCItemDetailInfo().materials;
        vehicleInfo.detailInfo.dTexts = GamePropNodeManager.Inst.GetUGCItemDetailInfo().dTexts;

        var instrumentDraftInfo = VehicleAssetManager.Inst.GetOrCreateDraftInfo(vehicleInfo);
        if (instrumentDraftInfo == null)
        {
            return;
        }

        var publishStateMachine = new UGCPublishStateMachine();
        publishStateMachine.SetStates(new List<UGCPublishStateBase>() {
                new UGCPublishStateBase(UGCPublishState.SetPropSkinAnchor),
                new UGCPublishStateBase(UGCPublishState.SetInstrumentAdjust),
                new UGCPublishStateBase(UGCPublishState.SetInstrumentAdjustWithAnim),
            });
        publishStateMachine.SetEditData(new VehicleEditData()
        {
            skinActionDraftInfo = instrumentDraftInfo,
            metaDataBytes = itemData.ToByteArray(),
        });

        publishStateMachine.Start();
    }

    public override void OnCoverPhotoClick()
    {
        var itemData = GamePropNodeManager.Inst.SaveUgcItemData();
        var publishStateMachine = new UGCPublishStateMachine();
        var propManager = GlobalNodeManager.Inst.Get<PropManager>();
        propManager.RemovePropData(vehicleInfo.id);

        if (vehicleInfo.detailInfo == null)
        {
            vehicleInfo.detailInfo = VehicleDetailInfo.FromDetailInfo(GamePropNodeManager.Inst.GetUGCItemDetailInfo());
        }
        else
        {
            vehicleInfo.detailInfo.Assign(GamePropNodeManager.Inst.GetUGCItemDetailInfo());
        }

        vehicleInfo.detailInfo.materials = GamePropNodeManager.Inst.GetUGCItemDetailInfo().materials;
        vehicleInfo.detailInfo.dTexts = GamePropNodeManager.Inst.GetUGCItemDetailInfo().dTexts;         
        var instrumentDraftInfo = VehicleAssetManager.Inst.GetOrCreateDraftInfo(vehicleInfo);

        publishStateMachine.SetStates(new List<UGCPublishStateBase>() {
                new UGCPublishStateBase(UGCPublishState.VehicleCoverView)
            });
        publishStateMachine.SetEditData(new VehicleEditData()
        {
            skinActionDraftInfo = instrumentDraftInfo,
            metaDataBytes = itemData.ToByteArray(),
        });

        publishStateMachine.Start();
    }

    public override void OnCombinePanelClose()
    {
        UIManager.Inst.OpenPanel(PanelId.UGCVehicleEditPanel);
    }
}

public enum VehicleAudioType
{
    Star, //启动
    Drive,//运行
    Horn,//喇叭
}