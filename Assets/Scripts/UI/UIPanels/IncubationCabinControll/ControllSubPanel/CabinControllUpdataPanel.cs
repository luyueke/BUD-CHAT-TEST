using Game.BudBox;
using Message;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Author:
/// Desc: 结算面板 - 硬件升级子面板，包含导航按钮及底包升级入口
/// Date: 26-04-09
/// </summary>
public class CabinControllUpdataPanel : CabinControllSettleSubPanel
{
    [SerializeField] private Button UpgradeBtn;       // 升级按钮（仅当有新版本时显示）
    //[SerializeField] private Button UpgradeBtnTest;       // 固件升级按钮测试
    [SerializeField] private Text VersionsText;      // 当前版本
    [SerializeField] private Text NewVersionsText;   // 最新版本
    [SerializeField] private Text NewVersionsDest;   // 版本描述
    [SerializeField] private GameObject UpgradingTxtGo;   //更新描述

    private UpgradeBoxType _pendingUpgradeType = UpgradeBoxType.APK; // 当前检测到的待升级类型（面板刷新时更新）
    private UpgradeBoxType _inProgressUpgradeType = UpgradeBoxType.APK; // 正在进行的升级类型（用于升级结果回调中生成提示文案）

    private void Awake()
    {
        UpgradeBtn.onClick.AddListener(OnUpgradeBtnClick);
        //UpgradeBtnTest.onClick.AddListener(OnUpgradeBtnTestClick);

        MessageHelper.AddListener<string>(MessageName.OnBoxDeviceStateChanged, OnBoxDeviceStateChanged);
        MessageHelper.AddListener<UpgradeBoxState>(MessageName.OnBudBoxUpgradeResult, OnBudBoxUpgradeResult);
    }

    private void OnEnable() => RefreshUpgradeBtnVisible();

    private void OnDestroy()
    {
        MessageHelper.RemoveListener<string>(MessageName.OnBoxDeviceStateChanged, OnBoxDeviceStateChanged);
        MessageHelper.RemoveListener<UpgradeBoxState>(MessageName.OnBudBoxUpgradeResult, OnBudBoxUpgradeResult);
    }

    private void OnBoxDeviceStateChanged(string deviceId)
    {
        if (deviceId != CabinBoxManager.Inst.GetCurrentDeviceId()) return;
        if (CabinBoxManager.Inst.GetBoxState() != BoxState.Online)
        {
            //去掉弹窗
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
        RefreshUpgradeBtnVisible();
    }

    private void RefreshUpgradeBtnVisible()
    {
        string curFirmware = CabinBoxManager.Inst.GetCurrentFirmwareVersion();
        string latestFirmware = CabinBoxManager.Inst.GetLatestFirmwareVersion();

        string curBase = CabinBoxManager.Inst.GetCurrentBaseVersion();
        string latestBase = CabinBoxManager.Inst.GetLatestBaseVersion();
        string latestBaseDest = CabinBoxManager.Inst.GetLatestBaseDest();

        string curHot = CabinBoxManager.Inst.GetCurrentHotVersion();
        string latestHot = CabinBoxManager.Inst.GetLatestHotVersion();
        string hotDest = CabinBoxManager.Inst.GetHotUpdateDest();
        // curFirmware = "1.0.0"; //测试用
        if (HasNewerVersion(curFirmware, latestFirmware))
        {
            _pendingUpgradeType = UpgradeBoxType.Firmware;
            SetVersionDisplay(curFirmware, latestFirmware, CabinBoxManager.Inst.GetFirmwareProductDetail());
            UpgradeBtn.gameObject.SetActive(true);
        }
        else if (HasNewerVersion(curBase, latestBase))
        {
            _pendingUpgradeType = UpgradeBoxType.APK;
            SetVersionDisplay(curBase, latestBase, latestBaseDest);
            UpgradeBtn.gameObject.SetActive(true);
        }
        else if (HasNewerVersion(curHot, latestHot))
        {
            _pendingUpgradeType = UpgradeBoxType.HotUpdate;
            SetVersionDisplay(curHot, latestHot, hotDest);
            UpgradeBtn.gameObject.SetActive(true);
        }
        else
        {
            SetVersionDisplay(curBase, latestBase, latestBaseDest);
            UpgradeBtn.gameObject.SetActive(false);
        }

        if (CabinBoxManager.Inst.GetBoxState() != BoxState.Online)
        {
            var curDeviceId = CabinBoxManager.Inst.GetCurrentDeviceId();
            bool isUpgrading = PlayerPrefs.GetInt("Device_Update" + curDeviceId, 0) == 1;
            if (isUpgrading)
            {
                UpgradeBtn.gameObject.SetActive(false);
                UpgradingTxtGo.gameObject.SetActive(true);
            }
            else
            {
                UpgradingTxtGo.gameObject.SetActive(false);
            }
        }
        else
        {
            var curDeviceId = CabinBoxManager.Inst.GetCurrentDeviceId();
            UpgradingTxtGo.gameObject.SetActive(false);
            PlayerPrefs.SetInt("Device_Update" + curDeviceId, 0);
        }
    }

    private void SetVersionDisplay(string current, string latest, string desc)
    {
        if (VersionsText != null) VersionsText.text = current;
        if (NewVersionsText != null) NewVersionsText.text = $"最新版本:{latest}";
        if (NewVersionsDest != null) NewVersionsDest.text = desc;
    }

    private static bool HasNewerVersion(string current, string latest)
    {
        if (string.IsNullOrEmpty(current) || string.IsNullOrEmpty(latest))
            return false;
        if (!System.Version.TryParse(StripVersionSuffix(latest), out var latestVer) ||
            !System.Version.TryParse(StripVersionSuffix(current), out var currentVer))
            return false;
        return latestVer > currentVer;
    }

    // 去掉版本号中 "-" 及其后的所有字符，如 "1.0.2-T" → "1.0.2"
    private static string StripVersionSuffix(string version)
    {
        if (string.IsNullOrEmpty(version)) return version;
        int dash = version.IndexOf('-');
        return dash >= 0 ? version.Substring(0, dash) : version;
    }

    private void OnUpgradeBtnClick()
    {
        if (CabinBoxManager.Inst.GetBoxState() != BoxState.Online) return;
        _inProgressUpgradeType = _pendingUpgradeType;
        CabinBoxManager.Inst.SendMqttMessage(MqttMsgOperType.start_upgrade, _pendingUpgradeType);
    }

    void OnUpgradeBtnTestClick()
    {
        if (CabinBoxManager.Inst.GetBoxState() != BoxState.Online) return;
        _pendingUpgradeType = UpgradeBoxType.Firmware;
        _inProgressUpgradeType = _pendingUpgradeType;

        var data = CabinBoxManager.Inst.GetCabinBudBoxData();
        data.deviceState.firmwareDownloadUrl = "https://cdn-hotupdate.budapp.cn/budbox/prod/update_1_0_2_1780128763636.zip";
        data.deviceState.latestFirmwareVersion = "1.0.3";
        CabinBoxManager.Inst.SendMqttMessage(MqttMsgOperType.start_upgrade, _pendingUpgradeType);
    }

    private void OnBudBoxUpgradeResult(UpgradeBoxState state)
    {
        switch (state)
        {
            case UpgradeBoxState.StartUpgrade:
                var panel = UIManager.Inst.OpenPanel<CommonLoadingPanel>(PanelId.CommonBoxLoadingPanel);
                panel.SetLocalText(
                    "硬件升级中",
                    "请保持BUD BOX连接电源并且网络状态良好\n更新过程中请勿关闭该页面。"
                );
                break;
            case UpgradeBoxState.UpgradeSuccess:
                UIManager.Inst.ClosePanel(PanelId.CommonBoxLoadingPanel);
                //记录升级后box在升级状态
                var curDeviceId = CabinBoxManager.Inst.GetCurrentDeviceId();
                PlayerPrefs.SetInt("Device_Update" + curDeviceId, 1);
                //
                string successMsg = _inProgressUpgradeType switch
                {
                    UpgradeBoxType.Firmware => "固件已是最新版。",
                    UpgradeBoxType.HotUpdate => "当前热更版本已是最新版。",
                    _ => "当前底包版本已是最新版。",
                };
                var successPanel = UIManager.Inst.OpenPanel<CommonBoxConfirmWithTitlePanel>(PanelId.CommonBoxConfirmWithTitlePanel);
                successPanel.SetTextAndAction("提示", successMsg, "完成", null, confirmClick: null, cancelClick: null);
                RefreshUpgradeBtnVisible();
                break;
            case UpgradeBoxState.UpgradeFail:
                UIManager.Inst.ClosePanel(PanelId.CommonBoxLoadingPanel);
                TipPanel.ShowToast("升级失败，请重试");
                break;
        }
    }
}
