
using System.Collections.Generic;
using Game.Avatar;
using GameData.BaseInfo;
using Message;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.UI;

public class WardrobeView : MonoBehaviour
{
    public Button wardrobe_add;
    public WardrobeViewWardrobelistItem wardrobeViewWardrobelistItem;

    private Transform _wardrobeListParent;
    public WardrobeViewWardrobelistItem SelectedItem { get; private set; }

    private void Awake()
    {
        _wardrobeListParent = GameObjectEx.FindComponentByName<Transform>(transform, "Scroll View/Viewport/Content");
        wardrobeViewWardrobelistItem.gameObject.SetActive(false);

        wardrobe_add.onClick.AddListener(OpenFittingRoomForAdd);

        MessageHelper.AddListener<OCTheatreAvatarInfo>(MessageName.ActorCardInfoUpdate, RefreshData);
        MessageHelper.AddListener<WardrobeViewWardrobelistItem>(MessageName.ActorWardrobeItemSelected, OnItemSelected);
    }

    public void OpenFittingRoomForAdd()
    {
        var clothes = OCTheatreActorEditorDataManager.Inst.GetClothesList();
        if(clothes.Count>=6)
        {
            TipPanel.ShowToast("最多只能添加6套衣服哦");
            return;
        }
        UIManager.Inst.OpenPanel<FittingRoomPanel>(PanelId.FittingRoomPanel);
        MessageHelper.Broadcast(MessageName.ActorWardrobeViewOpenFittingRoomPanel, wardrobeViewWardrobelistItem);
    }

    private void OnItemSelected(WardrobeViewWardrobelistItem item)
    {
        SelectedItem = item;
    }

    private void OnEnable()
    {
        // 激活时主动刷新，防止 Awake 未执行时错过广播
        if (_wardrobeListParent == null) return;
        RefreshData(null);
    }

    private void RefreshData(OCTheatreAvatarInfo _)
    {
        var clothesList = OCTheatreActorEditorDataManager.Inst.GetClothesList();
        if (clothesList == null) clothesList = new List<OTCAvatarClothes>();

        // 建立现有 item 的 clothesIndex → item 映射（跳过模板）
        var existingMap = new Dictionary<int, WardrobeViewWardrobelistItem>();
        foreach (var item in _wardrobeListParent.GetComponentsInChildren<WardrobeViewWardrobelistItem>(true))
        {
            if (item == wardrobeViewWardrobelistItem || item.ClothesData == null) continue;
            existingMap[item.ClothesData.clothesIndex] = item;
        }

        // 销毁数据中已不存在的 item
        foreach (var kv in existingMap)
        {
            bool found = false;
            foreach (var c in clothesList) { if (c.clothesIndex == kv.Key) { found = true; break; } }
            if (!found) DestroyImmediate(kv.Value.gameObject);
        }

        // 按数据顺序更新或创建 item，修正排列（sibling 0 为模板，实际 item 从 1 开始）
        for (int i = 0; i < clothesList.Count; i++)
        {
            var cloth = clothesList[i];
            if (existingMap.TryGetValue(cloth.clothesIndex, out var item))
                item.UpdateData(cloth);
            else
                item = CreateItem(cloth);
            item.transform.SetSiblingIndex(i + 1);
        }
    }

    private WardrobeViewWardrobelistItem CreateItem(OTCAvatarClothes clothes)
    {
        var go = Instantiate(wardrobeViewWardrobelistItem.gameObject, _wardrobeListParent);
        go.SetActive(true);
        go.transform.localPosition = Vector3.zero;
        var item = go.GetComponent<WardrobeViewWardrobelistItem>();
        item.InitData(clothes);
        return item;
    }

    private void OnDestroy()
    {
        MessageHelper.RemoveListener<OCTheatreAvatarInfo>(MessageName.ActorCardInfoUpdate, RefreshData);
        MessageHelper.RemoveListener<WardrobeViewWardrobelistItem>(MessageName.ActorWardrobeItemSelected, OnItemSelected);
    }

    
}
