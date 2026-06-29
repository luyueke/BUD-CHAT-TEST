using System;
using Game.Store;
using UnityEngine;
using UnityEngine.UI;

public class GashaponMutipleTypeItem : MonoBehaviour
{
    [SerializeField] private GameObject lockObj;
    [SerializeField] private Image iconImage;
    [SerializeField] private Toggle toggle;

    private GashaponExchangeData _data;
    private Action<GashaponExchangeData> _onSelect;

    public void Init(GashaponExchangeData data, Sprite icon, bool owned, ToggleGroup group,
        Action<GashaponExchangeData> onSelect, bool isOn)
    {
        _data = data;
        _onSelect = onSelect;

        if (iconImage != null) iconImage.sprite = icon;
        if (lockObj != null) lockObj.SetActive(!owned); // 已拥有 -> 隐藏锁

        if (toggle != null)
        {
            toggle.group = group;
            toggle.onValueChanged.RemoveListener(OnToggleChanged);
            toggle.SetIsOnWithoutNotify(isOn);               // 默认选中不触发回调
            toggle.onValueChanged.AddListener(OnToggleChanged);
        }
    }

    private void OnToggleChanged(bool on)
    {
        if (on) _onSelect?.Invoke(_data); // 选中 = 等同 ExchangePanel 选中该兑换项
    }
}
