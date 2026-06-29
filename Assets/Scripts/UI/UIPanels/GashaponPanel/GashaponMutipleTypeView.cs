using System;
using System.Collections.Generic;
using Game.Store;
using UI.Manager;
using UI.UIPanels.GashaponPanel;
using UnityEngine;
using UnityEngine.UI;

public class GashaponMutipleTypeView : MonoBehaviour
{
    [SerializeField] private GashaponMutipleTypeItem prefabItem;
    [SerializeField] private Transform content;
    [SerializeField] private ToggleGroup toggleGroup;

    private List<List<GashaponExchangeData>> _groups;     // 每个大头一组(含其变体)
    private Action<GashaponExchangeData> _onSelect;
    private int _curGroup;

    public void Init(List<List<GashaponExchangeData>> groups, Action<GashaponExchangeData> onSelect)
    {
        _groups = groups;
        _onSelect = onSelect;
        _curGroup = 0;

        // 初始只展示不触发选中：此时面板预览角色尚未创建(面板靠延迟的 DefClickFirst 初始化)，
        // 同步触发 StartPreview 会空引用。初始选中交给面板 DefClickFirst，再由 ShowGroupByHead 同步本视图。
        ShowGroup(0, selectHead: false);
    }

    // 切到包含指定 pgcId(大头或变体)的组，不重复触发选中(用于和外部选中同步)
    public void ShowGroupContaining(string pgcId)
    {
        if (_groups == null) return;
        for (int i = 0; i < _groups.Count; i++)
        {
            for (int j = 0; j < _groups[i].Count; j++)
            {
                if (_groups[i][j].Id == pgcId)
                {
                    if (i != _curGroup) ShowGroup(i, selectHead: false);
                    return;
                }
            }
        }
    }

    // 兑换成功后刷新当前组的锁状态(已拥有->隐藏锁)，不改变所在组
    public void RefreshOwned()
    {
        ShowGroup(_curGroup, selectHead: false);
    }

    private void ShowGroup(int index, bool selectHead)
    {
        if (_groups == null || index < 0 || index >= _groups.Count) return;
        _curGroup = index;

        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);

        var list = _groups[index];
        for (int j = 0; j < list.Count; j++)
        {
            var data = list[j];
            var item = Instantiate(prefabItem, content);
            item.gameObject.SetActive(true);
            bool owned = GashaponUtils.IsOwnedReward(data);
            var icon = PgcUtils.LoadVehicleIcon(data.Id, item.gameObject);
            item.Init(data, icon, owned, toggleGroup, _onSelect, j == 0); // 默认选中大头
        }

        if (selectHead && list.Count > 0) _onSelect?.Invoke(list[0]);
    }
}
