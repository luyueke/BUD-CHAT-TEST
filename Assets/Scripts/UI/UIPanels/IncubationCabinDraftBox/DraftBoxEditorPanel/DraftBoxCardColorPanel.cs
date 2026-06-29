using Es;
using System.Collections.Generic;
using Message;
using UnityEngine;

/// <summary>
/// Author:
/// Desc: 卡片颜色面板，从配置表 DraftBoxCardColorConfig 读取颜色数据，
///       动态生成 DraftBoxCardColorItem 列表，支持单选高亮。
///       选中颜色后直接修改 characterInfo 并广播 CardColor 事件。
/// Date:26-04-02
/// </summary>
public class DraftBoxCardColorPanel : MonoBehaviour
{
    [SerializeField] private GameObject ColorItemPrefab;  // 颜色 Item 预制体
    [SerializeField] private Transform ColorContainer;    // 颜色列表容器

    private List<GameObject> _itemList = new List<GameObject>();
    private DraftBoxCardColorItem _selectedItem;
    private CabinCharacterBaseInfo _info;

    /// <summary>初始化面板：从配置表读取颜色数据并生成颜色 Item 列表</summary>
    public void InitUI()
    {
        RefreshList();
    }

    /// <summary>设置当前编辑的角色数据引用，并根据已保存的 colorId 恢复默认选中态</summary>
    public void SetData(CabinCharacterBaseInfo info)
    {
        _info = info;
        var savedColorId = info?.coverInfo?.GetDetail()?.colorId ?? 0;
        ApplyDefaultSelection(savedColorId);
    }

    /// <summary>销毁旧 Item，从配置表重新生成全部颜色 Item</summary>
    private void RefreshList()
    {
        foreach (var go in _itemList)
            Destroy(go);
        _itemList.Clear();
        _selectedItem = null;

        var dataList = DataTables.GetDraftBoxCardColorConfigList();
        foreach (var data in dataList)
            CreateColorItem(data);
    }

    /// <summary>实例化颜色 Item 预制体，绑定配置数据和点击回调</summary>
    private void CreateColorItem(DraftBoxCardColorConfig data)
    {
        var go = Instantiate(ColorItemPrefab, ColorContainer);
        go.SetActive(true);
        var item = go.GetComponent<DraftBoxCardColorItem>();
        if (item != null)
            item.SetData(data, () => OnColorItemClick(item, data.Id));
        _itemList.Add(go);
    }

    /// <summary>根据 colorId 在已生成的 Item 列表中找到对应项并设为选中态</summary>
    private void ApplyDefaultSelection(int colorId)
    {
        if (colorId == 0) return;
        var dataList = DataTables.GetDraftBoxCardColorConfigList();
        for (int i = 0; i < dataList.Count && i < _itemList.Count; i++)
        {
            if (dataList[i].Id != colorId) continue;
            var item = _itemList[i].GetComponent<DraftBoxCardColorItem>();
            if (item == null) break;
            _selectedItem?.SetSelected(false);
            _selectedItem = item;
            item.SetSelected(true);
            break;
        }
    }

    /// <summary>点击颜色 Item：切换选中高亮态，将 colorId 写入 coverInfo.detail，广播 CardColor 事件</summary>
    private void OnColorItemClick(DraftBoxCardColorItem item, int colorId)
    {
        _selectedItem?.SetSelected(false);
        _selectedItem = item;
        item.SetSelected(true);

        if (_info == null) return;
        if (_info.coverInfo == null) _info.coverInfo = new CabinCoverInfo();
        var detail = _info.coverInfo.GetDetail();
        detail.colorId = colorId;
        _info.coverInfo.SetDetail(detail);
        MessageHelper.Broadcast(MessageName.OnCabinCharacterUpdated, _info.id, CabinCharacterUpdateType.CardColor);
    }
}
