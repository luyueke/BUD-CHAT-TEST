using System;
using GameData.BaseInfo;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 更新列表Item
/// </summary>
public class UpdateListItem : MonoBehaviour
{
    private RawImage mapCover;
    private Text mapName;
    private Image selectImage;
    private DraftListItem _draftListItem;
    private Action<DraftListItem> _onSelect;
    private Button clickBtn;

    public void InitUI()
    {
        mapCover = GameObjectEx.FindChildByName(transform, "MapCover").GetComponent<RawImage>();
        mapName = GameObjectEx.FindChildByName(transform, "MapName").GetComponent<Text>();
        selectImage = GameObjectEx.FindChildByName(transform, "SelectImage").GetComponent<Image>();
        
        clickBtn = GameObjectEx.FindChildByName(transform, "Views").GetComponent<Button>();
    }

    public void SetData(DraftListItem item, Action<DraftListItem> onSelect)
    {
        _onSelect = onSelect;
        if (item != null)
        {
            _draftListItem = item;
            MapInfo mapInfo = item.mapInfo;
            if (mapInfo != null)
            {
                mapName.text = mapInfo.name;
            }
        }
        
        clickBtn.onClick.AddListener(() =>
        {
            _onSelect?.Invoke(_draftListItem);
        });
    }

    public void UpdateSelect(bool isSelect)
    {
        selectImage.gameObject.SetActive(isSelect);
    }
}