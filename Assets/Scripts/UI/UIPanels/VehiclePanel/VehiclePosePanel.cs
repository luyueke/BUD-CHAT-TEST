using System;
using System.Collections;
using System.Collections.Generic;
using BUD.AnimPose;
using GameData.BaseInfo;
using GameData.Manager;
using GameData.PgcData;
using Newtonsoft.Json;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class VehiclePosePanel : BasePanel<VehiclePosePanel>
{
    [SerializeField] private VehiclePoseSource miSourceUI;
    public Button CloseBtn;
    public CButton DoneBtn;
    public FreePoseOSAView FreePoseView;
    public CreatePoseOSAView CreatePoseView;
    public BuyPoseOSAView BuyPoseView;
    private Dictionary<VehiclePoseSource.VehicleSource, BasePoseOSAView> allViews;
    private string curPoseData = string.Empty;
    private VehiclePoseSource.VehicleSource curSource = VehiclePoseSource.VehicleSource.Free;
    private UgcPoseSubType poseSubType = UgcPoseSubType.Single;
    public Action enterCallBack;
    private VehicleInfo vehicleInfo;
    public override void OnCreate()
    {
        base.OnCreate();
        CloseBtn.onClick.AddListener(OnClose);
        allViews = new Dictionary<VehiclePoseSource.VehicleSource, BasePoseOSAView>();
        CreatePoseView.isVehicle = true;
        BuyPoseView.isVehicle = true;
        allViews.Add(VehiclePoseSource.VehicleSource.Free, FreePoseView);
        allViews.Add(VehiclePoseSource.VehicleSource.Create, CreatePoseView);
        allViews.Add(VehiclePoseSource.VehicleSource.Buy, BuyPoseView);
        miSourceUI.SetCallback(OnValueChange);
        DoneBtn.interactable = false;
        DoneBtn.onClick.AddListener(OnDoneClick);
        vehicleInfo = GameDataManager.Inst.mapGlobalData?.GetCurInfo<VehicleInfo>();
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        OnStartView();
        miSourceUI.DefualtOn(curSource);
    }

    private void OnSelectPose(string poseData)
    {
        curPoseData = poseData;
        DoneBtn.interactable = !string.IsNullOrEmpty(curPoseData);
    }

    private void OnDoneClick()
    {
        if (vehicleInfo != null && vehicleInfo.vehicleType == (int)VehicleType.Double)
        {
            var posSelectPanel = UIManager.Inst.OpenPanel<VehiclePosSelectPanel>(PanelId.VehiclePosSelectPanel, curPoseData);
            posSelectPanel.OnSelectPoseEnter = DoubleSelectEvent;
        }
        else
        {
            DoubleSelectEvent((int)VehicleType.Single);
        }
    }

    private void DoubleSelectEvent(int type)
    {
        var frameData = JsonConvert.DeserializeObject<KeyFrameData>(curPoseData);
        switch ((VehicleType)type)
        {
            case VehicleType.Single:
                vehicleInfo.curPoseData = curPoseData;
                VehicleDataManager.Inst.SingleVehicleData.keyFrameData = frameData;
                break;
            case VehicleType.Double:
                vehicleInfo.doublePoseData = curPoseData;
                VehicleDataManager.Inst.DoubleVehicleData.keyFrameData = frameData;
                break;
        }
        enterCallBack?.Invoke();
        CloseSelf();
    }

    private void OnStartView()
    {
        foreach (var keyValue in allViews)
        {
            keyValue.Value.OnStart(poseSubType);
            keyValue.Value.OnSelectPoseClick = OnSelectPose;
        }
    }

    private void OnValueChange(VehiclePoseSource.VehicleSource source)
    {
        foreach (var keyValue in allViews)
        {
            keyValue.Value.gameObject.SetActive(false);
        }

        if (curSource != source)
        {
            OnSelectPose(string.Empty);
        }

        allViews[source].gameObject.SetActive(true);
        allViews[source].OnUpdate();
    }

    private void OnClose()
    {
        CloseSelf();
    }
}
