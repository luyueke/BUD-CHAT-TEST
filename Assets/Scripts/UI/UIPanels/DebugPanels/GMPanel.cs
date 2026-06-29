using System;
using BUD.AnimPose;
using Game.Base;
using GameData;
using GameData.BaseInfo;
using GameData.PgcData;
using Newtonsoft.Json;
using UI.Base;
using UI.BaseWidgets;
using UI.UIWidgets;
using UIAgent;
using UnityEngine;
using UnityEngine.UI;

public class GMPanel : BasePanel<GMPanel>
{
    [SerializeField] private TextInputView IDInputText;
    [SerializeField] private CButton doneBtn;
    [SerializeField] private CButton closeBtn;
    [SerializeField] private Toggle mapToggle;
    [SerializeField] private Toggle propToggle;
    [SerializeField] private Toggle poseToggle;
    [SerializeField] private Toggle matToggle;
    [SerializeField] private Toggle skinToggle;
    [SerializeField] private Toggle editToggle;
    [SerializeField] private Toggle playToggle;
    [SerializeField] private Toggle fpsToggle;
    [SerializeField] private CButton runCodeBtn;
    [SerializeField] private CButton CreatePoseBtn;
    public override void OnCreate()
    {
        doneBtn.onClick.AddListener(OnDoneBtnClick);
        closeBtn.onClick.AddListener(OnCloseClick);
        fpsToggle?.onValueChanged.AddListener(OpenFPS);
        runCodeBtn.onClick.AddListener(OnRunCodeClick);
        CreatePoseBtn.onClick.AddListener(OnCreateClick);
    }

    private void OpenFPS(bool isFps)
    {
        var debugNode = GameObject.Find("DebugSetting");
        if (debugNode != null)
        {
            if (isFps)
            {
                debugNode.AddComponent<MemoryAnalysis>();
            }
            else
            {
                var analysis = debugNode.GetComponent<MemoryAnalysis>();
                if (analysis != null)
                {
                    UnityEngine.Object.Destroy(analysis);
                }
            }
        }
    }

    public override void OnShow(params object[] args)
    {
        IDInputText.SetInput("");
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }

    private void OnDoneBtnClick()
    {
        string inputStr = "";
        if (!String.IsNullOrEmpty(IDInputText.Input))
        {
            inputStr = IDInputText.Input;
        }

        if (string.IsNullOrEmpty(inputStr))
        {
            TipPanel.ShowToast("请输入ID");
            return;
        }

        if (mapToggle.isOn)
        {
            if (editToggle.isOn)
            {
                TestEnterGameUtils.Inst.EnterEditMap(inputStr);
            }
            else
            {
                TestEnterGameUtils.Inst.EnterGuestMap(inputStr);
            }
        }
        else
        {
            if (skinToggle.isOn)
            {
                if (editToggle.isOn)
                {
                    TestEnterGameUtils.Inst.EnterEditSkin(inputStr);
                }
                else
                {
                    UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel, AssetDetailType.Skin, inputStr);
                    CloseSelf();
                }
            }
            else if(propToggle.isOn)
            {
                if (editToggle.isOn)
                {
                    TestEnterGameUtils.Inst.EnterEditProp(inputStr);
                }
                else
                {
                    UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel, AssetDetailType.Prop, inputStr);
                    CloseSelf();
                }
            }
            else if (poseToggle.isOn)
            {
                if (editToggle.isOn)
                {
                    TestEnterGameUtils.Inst.EnterEditPose(inputStr);
                }
            }
            else if (mapToggle.isOn)
            {
                UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel, AssetDetailType.Mat, inputStr);
                CloseSelf();
            }
        }
        
    }

    private void OnCloseClick()
    {
        LoggerUtils.Log("DebugInputPanel OnBgClick");
        CloseSelf();
    }


    private void OnRunCodeClick()
    {
        UIAgentManager.Inst.OpenPanel(PanelId.AIBuddyListPanel);
        CloseSelf();
        // LocalizationManager.Inst.SetLang(LangCode.en);
    }
    
    
    private void OnCreateClick()
    {
        UIAgentManager.Inst.OpenPanel(PanelId.UgcLoadingPanel);
        var poseInfo = new PoseInfo();
        poseInfo.poseType = (int)UgcPoseSubType.Double;
        AnimDataManager.Inst.enterMode = EnterPanelMode.Standard;
        AnimDataManager.Inst.animPose = poseInfo;
        KeyFrameData frameData = new KeyFrameData();
        poseInfo.poseData = JsonConvert.SerializeObject(frameData);
        GameController.StartGame(EnterGameModel.AnimPoseEmpty, poseInfo, true, "AnimatedScene");
        CloseSelf();
    }
}