using Game.BudBox;
using Message;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Author:
/// Desc: 结算面板 - 恢复出厂设置子面板，包含跳转其他设置页的导航按钮
/// Date: 26-04-09
/// </summary>
public class CabinControllRestPanel : CabinControllSettleSubPanel
{
    [SerializeField] private Button ClearConfigBtn; // 触发恢复出厂设置按钮
    [SerializeField] private List<GameObject> DeviceNotConnectedObj; // 设备离线时显示的提示对象列表
    [SerializeField] private Text VersionsText;   // 当前固件版本文本

    private void Awake()
    {
        ClearConfigBtn.onClick.AddListener(OnClearConfigClick);
        MessageHelper.AddListener<string>(MessageName.OnBoxDeviceStateChanged, OnBoxDeviceStateChanged);
        MessageHelper.AddListener<RestBoxState>(MessageName.OnBudBoxRestInit, OnBudBoxRestInit);
    }

    private void OnEnable() => RefreshConnectState();

    private void OnDestroy()
    {
        MessageHelper.RemoveListener<string>(MessageName.OnBoxDeviceStateChanged, OnBoxDeviceStateChanged);
        MessageHelper.RemoveListener<RestBoxState>(MessageName.OnBudBoxRestInit, OnBudBoxRestInit);

    }

    // 第一级确认：展示恢复出厂的后果说明，引导用户进入第二级确认
    private void OnClearConfigClick()
    {
        if (CabinBoxManager.Inst.GetBoxState() != BoxState.Online)
            return;

        var panel = UIManager.Inst.OpenPanel<CommonBoxConfirmWithTitlePanel>(PanelId.CommonBoxConfirmWithTitlePanel);
        panel.SetTextAndAction(
            "确认恢复出厂设置",
            "恢复后，BUD BOX将清除当前设置、网络连接及已同步内容，并回到初始状态。",
            "确定",
            "取消",
            confirmClick: OnTowConfirm,
            cancelClick: null
        );
    }

    // 第二级确认：再次提醒需重新连接设备，确认后进入第三级
    private void OnTowConfirm()
    {
        if (CabinBoxManager.Inst.GetBoxState() != BoxState.Online)
            return;

        var panel = UIManager.Inst.OpenPanel<CommonBoxConfirmWithTitlePanel>(PanelId.CommonBoxConfirmWithTitlePanel);
        panel.SetTextAndAction(
            "确认恢复出厂设置",
            "恢复出厂设置后，设备需要重新连接并重新导入内容。确定继续吗？",
            "确定",
            "取消",
            confirmClick: OnThreeConfirm,
            cancelClick: null
        );
    }

    // 第三级确认：用户最终确认，发送 MQTT 指令触发恢复出厂
    private void OnThreeConfirm()
    {
        if (CabinBoxManager.Inst.GetBoxState() != BoxState.Online)
            return;
        string deviceId = CabinBoxManager.Inst.GetCurrentDeviceId();
        CabinBoxManager.Inst.UnbindBudBox(deviceId, isSuccess =>
        {
            if (isSuccess)
            {
                CabinBoxManager.Inst.SendMqttMessage(MqttMsgOperType.start_rest);
            }
            else
                TipPanel.ShowToast("解绑失败，请稍后重试。");
        });
    }

    // 监听恢复出厂状态变化，根据阶段分别处理 Loading、完成和失败
    private void OnBudBoxRestInit(RestBoxState restBoxState)
    {
        switch (restBoxState)
        {
            case RestBoxState.None:
                // 恢复中途取消或异常中断，关闭所有弹窗
                UIManager.Inst.ClosePanel(PanelId.CommonLoadingPanel);
                UIManager.Inst.ClosePanel(PanelId.CommonBoxConfirmWithTitlePanel);
                break;
            case RestBoxState.StartRest:
                // 设备开始恢复，关闭确认弹窗并显示 Loading
                UIManager.Inst.ClosePanel(PanelId.CommonBoxConfirmWithTitlePanel);
                OnStartRestBox();
                break;
            case RestBoxState.RestFinish:
                // 恢复完成，关闭 Loading 并引导用户重新连接
                UIManager.Inst.ClosePanel(PanelId.CommonLoadingPanel);
                OnEndRestBox();
                break;
            case RestBoxState.RestFail:
                break;
        }
    }

    // 显示恢复进行中 Loading 面板，防止用户断电或关 App
    private void OnStartRestBox()
    {
        var panel = UIManager.Inst.OpenPanel<CommonLoadingPanel>(PanelId.CommonLoadingPanel);
        panel.SetLocalText(
            "正在恢复出厂设置",
            "请保持设备通电，并不要关闭 App"
        );
    }

    private void OnEndRestBox()
    {
        var panel = UIManager.Inst.OpenPanel<CommonBoxConfirmWithTitlePanel>(PanelId.CommonBoxConfirmWithTitlePanel);
        panel.SetTextAndAction(
            "已恢复出厂设置",
            "BUD BOX已恢复为初始状态，你可以重新开始设备连接与设置。",
            "重新连接",
            "取消",
            //这里需要跳转到重新连接界面
            confirmClick: () =>
            {
                UIManager.Inst.CloseCommonPanel(PanelId.IncubationCabinControll);
                UIManager.Inst.OpenPanel(PanelId.IncubationCabinLinkBox);
            },
            cancelClick: () =>
            {
                UIManager.Inst.CloseCommonPanel(PanelId.IncubationCabinControll);
                CabinBoxManager.Inst.OpenBoxEntrance();
            }
        );
    }


    private void OnBoxDeviceStateChanged(string deviceId)
    {
        if (deviceId != CabinBoxManager.Inst.GetCurrentDeviceId()) return;
        if (CabinBoxManager.Inst.GetBoxState() != BoxState.Online)
        {
            // UIManager.Inst.ClosePanel(PanelId.CommonLoadingPanel);
            // UIManager.Inst.OpenPanel(PanelId.CommonSingleConfirmPanel_Style2,
            //     new CommonSingleConfirmPanel_Style2Data
            //     {
            //         TopTitleString = "BUD BOX",
            //         ContextString = "设备断开连接。",
            //         ConfirmString = "确定",
            //         CanClose = true,
            //         ConfirmClickAction = null
            //     });
        }
        RefreshConnectState();
    }

    // 刷新离线提示对象的显隐状态，并更新当前固件版本号文本
    private void RefreshConnectState()
    {
        bool onLine = CabinBoxManager.Inst.GetBoxState() == BoxState.Online;
        foreach (var item in DeviceNotConnectedObj)
            item.SetActive(!onLine);

        ClearConfigBtn.gameObject.SetActive(onLine);
        //if (VersionsText != null)
        //    VersionsText.text = $"最新版本:{CabinBoxManager.Inst.GetCurrentBaseVersion()}";
    }
}
