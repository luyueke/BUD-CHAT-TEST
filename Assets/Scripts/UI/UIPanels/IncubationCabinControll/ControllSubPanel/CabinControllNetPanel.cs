using Game.BLE;
using Game.BudBox;
using Message;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Author:
/// Desc: 结算面板 - 网络设置子面板，显示当前 Wi-Fi 及可选 Wi-Fi 列表
/// Date: 26-04-09
/// </summary>
public class CabinControllNetPanel : CabinControllSettleSubPanel
{
    [SerializeField] private Button AddWifiBtn;    // 添加 Wi-Fi 按钮
    [SerializeField] private List<GameObject> UnLinkObj; // Box 离线时显示的提示对象列表

    [Header("网络显示")]
    [SerializeField] private Text CurWifi;                      // 当前已连接的 Wi-Fi 名称
    [SerializeField] private Transform WifiListContent;         // Wi-Fi 列表容器
    [SerializeField] private CabinControllWifiItem WifiItemPrefab; // Wi-Fi 列表 Item 预制体

    private readonly List<CabinControllWifiItem> _wifiItems = new List<CabinControllWifiItem>();

    private void Awake()
    {
        AddWifiBtn.onClick.AddListener(OnAddWifiBtnClick);

        MessageHelper.AddListener<string>(MessageName.OnBudBoxBaseDataRest, OnBudBoxBaseDataRest);
        MessageHelper.AddListener<string>(MessageName.OnBoxDeviceStateChanged, OnBoxDeviceStateChanged);

        CabinBoxManager.Inst.SendMqttMessage(MqttMsgOperType.get_wifidata);
    }

    private void OnEnable()
    {
        RefreshWifiDisplay(CabinBoxManager.Inst.GetCurrentDeviceId());
        RefreshConnectState();
    }

    /// <summary>设备状态变化回调：只处理当前设备，刷新离线提示显隐</summary>
    private void OnBoxDeviceStateChanged(string deviceId)
    {
        if (deviceId != CabinBoxManager.Inst.GetCurrentDeviceId())
            return;

        RefreshConnectState();
    }

    /// <summary>刷新 UnLinkObj 显隐：Box 离线时显示，在线时隐藏</summary>
    private void RefreshConnectState()
    {
        foreach (var item in UnLinkObj)
        {
            item.SetActive(CabinBoxManager.Inst.GetBoxState() != BoxState.Online);
        }
    }

    /// <summary>设备基础数据重置回调：刷新 Wi-Fi 显示</summary>
    private void OnBudBoxBaseDataRest(string deviceId)
    {
        if (deviceId != CabinBoxManager.Inst.GetCurrentDeviceId())
            return;
        RefreshWifiDisplay(deviceId);
    }

    /// <summary>刷新当前 Wi-Fi 名称与可选 Wi-Fi 列表</summary>
    private void RefreshWifiDisplay(string deviceId)
    {
        // 当前已连接 Wi-Fi
        var connected = CabinBoxManager.Inst.GetConnectedWifi(deviceId);
        if (CurWifi != null)
            CurWifi.text = connected != null ? connected.ssid : "未连接";

        // 其他 Wi-Fi 列表
        var otherList = CabinBoxManager.Inst.GetOtherWifiList(deviceId);
        RefreshWifiList(otherList);
    }

    /// <summary>根据数据刷新 Wi-Fi 列表 Item</summary>
    private void RefreshWifiList(List<WifiInfo> wifiList)
    {
        if (WifiListContent == null || WifiItemPrefab == null)
            return;

        // 回收多余 Item
        for (int i = _wifiItems.Count - 1; i >= wifiList.Count; i--)
        {
            Destroy(_wifiItems[i].gameObject);
            _wifiItems.RemoveAt(i);
        }

        // 补充或更新 Item
        for (int i = 0; i < wifiList.Count; i++)
        {
            if (i >= _wifiItems.Count)
            {
                var item = Instantiate(WifiItemPrefab, WifiListContent);
                item.gameObject.SetActive(true);
                item.onSelectClick = OnWifiItemClick;
                _wifiItems.Add(item);
            }
            _wifiItems[i].SetData(wifiList[i]);
        }
    }

    /// <summary>
    /// 点击"添加 Wi-Fi"按钮：Box 离线时弹出提示，在线时打开 Wi-Fi 配置面板。
    /// </summary>
    private void OnAddWifiBtnClick()
    {
        //if (CabinBoxManager.Inst.GetBoxState() != BoxState.Online)
        //{
        //    TipPanel.ShowToast("BUD BOX 离线，无法添加网络");
        //    return;
        //}

        UIManager.Inst.OpenPanel(PanelId.IncubationCabinWifiSetting, "CabinControll");
    }

    private void OnWifiItemClick(WifiInfo wifiInfo)
    {
        BleSoftwareSideController.SendWifiConfig(new()
        {
            ssid = wifiInfo.ssid,
            password = wifiInfo.password,
            securityType = wifiInfo.securityType
        }, (success, msg) =>
        {
            if (!success)
            {

            }
            else
            {
                Debug.Log($"SendWifiConfig — 配置已发送，等待连接结果...");
                //收到成功后,再刷新当前wifi
            }
        });
    }

    private void OnDestroy()
    {
        MessageHelper.RemoveListener<string>(MessageName.OnBudBoxBaseDataRest, OnBudBoxBaseDataRest);
        MessageHelper.RemoveListener<string>(MessageName.OnBoxDeviceStateChanged, OnBoxDeviceStateChanged);
    }
}
