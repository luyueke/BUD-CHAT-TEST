using Game.BLE;
using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Author:
/// Desc: 网络设置面板 - Wi-Fi 列表 Item，展示单条可选 Wi-Fi 网络
/// Date: 26-04-17
/// </summary>
public class CabinControllWifiItem : MonoBehaviour
{
    [SerializeField] private Text SsidText;   // Wi-Fi 名称
    [SerializeField] private Button SelectBtn; // 点击选中按钮

    /// <summary>Wi-Fi 选中回调（SelectBtn 暂未实现，当前不会被触发）</summary>
    public Action<WifiInfo> onSelectClick;

    private WifiInfo _data;

    void Awake()
    {
       SelectBtn.onClick.AddListener(() => onSelectClick?.Invoke(_data));
    }

    /// <summary>绑定 Wi-Fi 数据并刷新显示</summary>
    public void SetData(WifiInfo data)
    {
        _data = data;
        if (SsidText != null)
            SsidText.text = data.ssid;
    }
}
