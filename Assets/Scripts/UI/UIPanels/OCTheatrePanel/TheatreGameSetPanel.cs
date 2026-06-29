using System.Collections.Generic;
using GameData.BaseInfo;
using GameData.Config;
using Message;
using GameData.Manager;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class TheatreGameSetPanelSaveData
{
    public List<TheatreActorSaveItem> currentPanelActors;
    public List<TheatreActorSaveItem> myPanelActors;
    public List<TheatreActorSaveItem> defPanelActors;
    public List<TheatreActorSaveItem> wardrobePanelActors;
}

public class TheatreGameSetPanel : BasePanel<TheatreGameSetPanel>
{
    private CButton cbtn_close;
    private CButton cbtn_restore_default;
    private CButton cbtn_save;
    private Toggle tog_actor;
    private Toggle tog_wardrobe;
    private TheatreGameSetPanelItem theatreGameSetPanelItem;
    private Transform acotr_curent_parent;
    private Transform actor_def_parent;
    private Transform actor_map_parent;
    private Transform img_map;
    private Transform sv_wardrobe;
    private Transform sv_replaceable_actor;
    private Transform actor_my_parent;
    private Transform actor_wardrobe_parent;
    private Text txt_wardrobe;
    private Transform tog_actor_label;
    private Transform tog_actor_label1;
    private Transform tog_wardrobe_label;
    private Transform tog_wardrobe_label1;
    private Transform resetActorTitle;

    private const string ActorLineupKey = "OCTheatreActorLineup_";
    private enum ActorOrigin { Default, My }

    private readonly List<TheatreGameSetPanelItem> _currentActorItems = new();
    private readonly List<TheatreGameSetPanelItem> _myActorItems = new();
    private readonly List<TheatreGameSetPanelItem> _defActorItems = new();
    private readonly List<TheatreGameSetPanelItem> _wardrobeItems = new();
    private readonly List<TheatreGameSetPanelItem> _defPlaceholderItems = new();
    private readonly List<TheatreGameSetPanelItem> _mapItems = new();
    private readonly List<TheatreActorSaveItem> _mapActors = new();
    private readonly Dictionary<TheatreGameSetPanelItem, TheatreActorSaveItem> _itemToSaveItem = new();
    private OCTheatreInfo _roomTheatreInfo;
    private TheatreGameSetPanelItem _globalSelectedItem;
    private TheatreGameSetPanelItem _selectedWardrobeItem;
    private TheatreGameSetPanelSaveData _saveData;

    public override void OnCreate()
    {
        base.OnCreate();
        // string theatreID = OCTheatreGameController.Current?.TheatreID;
        // PlayerPrefs.DeleteKey(ActorLineupKey + theatreID);
        // PlayerPrefs.Save();
        cbtn_close = GameObjectEx.FindComponentByName<CButton>(transform, "cbtn_close");
        cbtn_restore_default = GameObjectEx.FindComponentByName<CButton>(transform, "cbtn_restore_default");
        cbtn_save = GameObjectEx.FindComponentByName<CButton>(transform, "cbtn_save");
        tog_actor = GameObjectEx.FindComponentByName<Toggle>(transform, "tog_actor");
        tog_wardrobe = GameObjectEx.FindComponentByName<Toggle>(transform, "tog_wardrobe");
        theatreGameSetPanelItem = GameObjectEx.FindComponentByName<TheatreGameSetPanelItem>(transform, "TheatreGameSetPanelItem");
        acotr_curent_parent = GameObjectEx.FindChildByName(transform, "acotr_curent_parent");
        actor_def_parent = GameObjectEx.FindChildByName(transform, "actor_def_parent");
        actor_map_parent = GameObjectEx.FindChildByName(transform, "actor_map_parent");
        img_map = GameObjectEx.FindChildByName(transform, "img_map");
        sv_wardrobe = GameObjectEx.FindChildByName(transform, "sv_wardrobe");
        sv_replaceable_actor = GameObjectEx.FindChildByName(transform, "sv_replaceable_actor");
        
        actor_my_parent = GameObjectEx.FindChildByName(transform, "actor_my_parent");
        actor_wardrobe_parent = GameObjectEx.FindChildByName(transform, "actor_wardrobe_parent");
        txt_wardrobe = GameObjectEx.FindComponentByName<Text>(transform, "txt_wardrobe");
        tog_actor_label = GameObjectEx.FindChildByName(tog_actor.transform, "Label");
        tog_actor_label1 = GameObjectEx.FindChildByName(tog_actor.transform, "Label1");
        tog_wardrobe_label = GameObjectEx.FindChildByName(tog_wardrobe.transform, "Label");
        tog_wardrobe_label1 = GameObjectEx.FindChildByName(tog_wardrobe.transform, "Label1");
        resetActorTitle = GameObjectEx.FindChildByName(transform, "resetActorTitle");

        cbtn_close.onClick.AddListener(CloseSelf);
        cbtn_restore_default.onClick.AddListener(OnRestoreDefaultBtnClick);
        cbtn_save.onClick.AddListener(OnSaveBtnClick);
        tog_actor.onValueChanged.AddListener(OnActorToggleValueChanged);
        tog_wardrobe.onValueChanged.AddListener(OnWardrobeToggleValueChanged);
        theatreGameSetPanelItem.gameObject.SetActive(false);

        tog_actor.SetIsOnWithoutNotify(true);
        tog_wardrobe.SetIsOnWithoutNotify(false);
        OnActorToggleValueChanged(true);
        OnWardrobeToggleValueChanged(false);

        _saveData = LoadSavedData();
        _saveData ??= BuildDefaultSaveData();

        InitActorCurrentList();
        InitActorDefNullList();
        InitMyActorList();

        if (_saveData.myPanelActors == null || _saveData.myPanelActors.Count == 0)
            FetchMyPublishedActors();

        RefreshReplaceableActorScroll();

         sv_replaceable_actor.GetComponent<ScrollRect>().content.gameObject.SetActive(true);
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        _roomTheatreInfo = null;
        if (args.Length > 0 && args[0] is List<TheatreActorSaveItem> mapActors)
            InitMapActorList(mapActors);
        if (args.Length > 1 && args[1] is OCTheatreInfo theatreInfo)
        {
            _roomTheatreInfo = theatreInfo;
            InitRoomCurrentActorList(theatreInfo);
        }
    }
    // ──────────── 初始化 ────────────

    private TheatreGameSetPanelSaveData BuildDefaultSaveData()
    {
        var data = new TheatreGameSetPanelSaveData
        {
            currentPanelActors = new List<TheatreActorSaveItem>(),
            myPanelActors = new List<TheatreActorSaveItem>(),
            defPanelActors = new List<TheatreActorSaveItem>(),
            wardrobePanelActors = new List<TheatreActorSaveItem>()
        };
        var actorList = OCTheatreGameController.Current?.GetDefaultActorList();
        if (actorList != null)
            for (int i = 0; i < actorList.Count; i++)
                data.currentPanelActors.Add(MakeSaveItem(actorList[i], (int)ActorOrigin.Default, i));
        for (int i = 0; i < 3; i++)
            data.defPanelActors.Add(MakePlaceholderSaveItem(i));
        return data;
    }

    private void InitActorCurrentList()
    {
        if (_saveData.currentPanelActors == null) return;
        _saveData.currentPanelActors.Sort((a, b) => a.siblingIndex.CompareTo(b.siblingIndex));
        foreach (var saveItem in _saveData.currentPanelActors)
        {
            var item = Instantiate(theatreGameSetPanelItem, acotr_curent_parent);
            item.gameObject.SetActive(true);
            item.SetDataByOrigin(ToAvatarOc(saveItem), saveItem.origin);
            _currentActorItems.Add(item);
            _itemToSaveItem[item] = saveItem;
            var captured = item;
            item.SetClickCallback(() => OnActorItemClicked(captured));
        }
    }

    private void InitActorDefNullList()
    {
        if (_saveData.defPanelActors == null) return;

        // 只取有数据的演员，按 siblingIndex 排序后依次创建；占位格统一追加到末尾
        var realActors = _saveData.defPanelActors.FindAll(s => !string.IsNullOrEmpty(s.playerId));
        realActors.Sort((a, b) => a.siblingIndex.CompareTo(b.siblingIndex));
        foreach (var saveItem in realActors)
        {
            var item = Instantiate(theatreGameSetPanelItem, actor_def_parent);
            item.gameObject.SetActive(true);
            item.SetDataByOrigin(ToAvatarOc(saveItem), saveItem.origin);
            _defActorItems.Add(item);
            _itemToSaveItem[item] = saveItem;
            var captured = item;
            item.SetClickCallback(() => OnActorItemClicked(captured));
        }

        // 清除旧占位格 save 记录，按实际需要重新生成
        _saveData.defPanelActors.RemoveAll(s => string.IsNullOrEmpty(s.playerId));
        int desiredPlaceholders = System.Math.Max(0, 3 - _defActorItems.Count);
        for (int i = 0; i < desiredPlaceholders; i++)
            AddDefPlaceholder();
        RefreshReplaceableActorScroll();
    }

    private void InitMyActorList()
    {
        if (_saveData.myPanelActors == null) return;
        _saveData.myPanelActors.Sort((a, b) => a.siblingIndex.CompareTo(b.siblingIndex));
        foreach (var saveItem in _saveData.myPanelActors)
        {
            var item = Instantiate(theatreGameSetPanelItem, actor_my_parent);
            item.gameObject.SetActive(true);
            item.SetDataByOrigin(ToAvatarOc(saveItem), saveItem.origin);
            _myActorItems.Add(item);
            _itemToSaveItem[item] = saveItem;
            var captured = item;
            item.SetClickCallback(() => OnActorItemClicked(captured));
        }
        RefreshReplaceableActorScroll();
    }


private void CreateDefPlaceholderUI()
    {
        var placeholder = Instantiate(theatreGameSetPanelItem, actor_def_parent);
        placeholder.gameObject.SetActive(true);
        placeholder.SetNullData();
        _defPlaceholderItems.Add(placeholder);
    }

    private void AddDefPlaceholder()
    {
        CreateDefPlaceholderUI();
        _saveData.defPanelActors.Add(MakePlaceholderSaveItem(_defPlaceholderItems[^1].transform.GetSiblingIndex()));
    }


// ──────────── 点击逻辑 ────────────

    private void OnActorItemClicked(TheatreGameSetPanelItem item)
    {
        if (tog_wardrobe.isOn)
        {
            if (_currentActorItems.Contains(item))
            {
                SelectGlobalItem(item);
                RefreshWardrobeList();
            }
            return;
        }

        if (_globalSelectedItem == null)
        {
            SelectGlobalItem(item);
            return;
        }

        if (_globalSelectedItem == item)
        {
            ClearGlobalSelection();
            return;
        }

        if (CanSwap(_globalSelectedItem, item))
        {
            SwapActors(_globalSelectedItem, item);
            ClearGlobalSelection();
            item.SetSelected(false);
        }
        else
        {
            ClearGlobalSelection();
            SelectGlobalItem(item);
        }
    }

    private void OnWardrobeItemClicked(TheatreGameSetPanelItem wardrobeItem, OTCAvatarClothes clothes)
    {
        if (_globalSelectedItem == null) return;
        if (!_currentActorItems.Contains(_globalSelectedItem) && !_myActorItems.Contains(_globalSelectedItem)) return;
        if (_selectedWardrobeItem != null)
            _selectedWardrobeItem.SetSelected(false);
        _selectedWardrobeItem = wardrobeItem;
        wardrobeItem.SetSelected(true);
        if (_itemToSaveItem.TryGetValue(_globalSelectedItem, out var saveItem))
            saveItem.clothesIndex = clothes.clothesIndex;
    }

    // ──────────── 替换逻辑 ────────────

    // current↔current（位置互换）、current↔my、current↔def 均可；my↔def 不可；占位格不参与交换
    private bool CanSwap(TheatreGameSetPanelItem a, TheatreGameSetPanelItem b)
    {
        if (_defPlaceholderItems.Contains(a) || _defPlaceholderItems.Contains(b)) return false;
        return _currentActorItems.Contains(a) || _currentActorItems.Contains(b);
    }

    private void SwapActors(TheatreGameSetPanelItem a, TheatreGameSetPanelItem b)
    {
        bool aIsCurrent = _currentActorItems.Contains(a);
        bool bIsCurrent = _currentActorItems.Contains(b);

        if (aIsCurrent && bIsCurrent)
        {
            int idxA = a.transform.GetSiblingIndex();
            int idxB = b.transform.GetSiblingIndex();
            a.transform.SetSiblingIndex(idxB);
            b.transform.SetSiblingIndex(idxA);
            if (_itemToSaveItem.TryGetValue(a, out var sa)) sa.siblingIndex = a.transform.GetSiblingIndex();
            if (_itemToSaveItem.TryGetValue(b, out var sb)) sb.siblingIndex = b.transform.GetSiblingIndex();
            return;
        }

        var currentItem = aIsCurrent ? a : b;
        var replacementItem = currentItem == a ? b : a;
        int currentSiblingIdx = currentItem.transform.GetSiblingIndex();
        int replacementSiblingIdx = replacementItem.transform.GetSiblingIndex();

        _itemToSaveItem.TryGetValue(currentItem, out var saveCurrentItem);
        _itemToSaveItem.TryGetValue(replacementItem, out var saveReplacementItem);
        bool currentWasOriginallyMy = saveCurrentItem?.origin == (int)ActorOrigin.My;
        bool currentWasOriginallyMap = saveCurrentItem?.origin == 2;

        replacementItem.transform.SetParent(acotr_curent_parent, false);
        replacementItem.transform.SetSiblingIndex(currentSiblingIdx);

        Transform currentTargetParent = currentWasOriginallyMy ? actor_my_parent
            : currentWasOriginallyMap ? actor_map_parent
            : actor_def_parent;
        currentItem.transform.SetParent(currentTargetParent, false);

        // 在修改列表前先判断 replacementItem 来源
        bool replacementWasDef = _defActorItems.Contains(replacementItem);
        bool replacementWasMap = _mapItems.Contains(replacementItem);

        if (currentTargetParent == actor_def_parent)
        {
            if (replacementWasDef)
            {
                // current ↔ def：1:1 互换，currentItem 占据被换出 def_actor 的原位，不增减占位格
                currentItem.transform.SetSiblingIndex(replacementSiblingIdx);
            }
            else
            {
                // current(Default) ↔ my：currentItem 新进 def，插到现有真实演员末尾（占位格在其后）
                currentItem.transform.SetSiblingIndex(_defActorItems.Count);
            }
        }
        else
        {
            currentItem.transform.SetSiblingIndex(replacementSiblingIdx);
        }

        _currentActorItems.Remove(currentItem);
        _currentActorItems.Add(replacementItem);

        bool replacementWasMy = _myActorItems.Contains(replacementItem);
        if (replacementWasMy)
            _myActorItems.Remove(replacementItem);
        else if (replacementWasDef)
            _defActorItems.Remove(replacementItem);
        else if (replacementWasMap)
            _mapItems.Remove(replacementItem);

        if (currentWasOriginallyMy)
            _myActorItems.Add(currentItem);
        else if (currentWasOriginallyMap)
            _mapItems.Add(currentItem);
        else
            _defActorItems.Add(currentItem);

        if (saveCurrentItem != null)
        {
            _saveData.currentPanelActors.Remove(saveCurrentItem);
            saveCurrentItem.siblingIndex = currentItem.transform.GetSiblingIndex();
            if (currentWasOriginallyMy)
                _saveData.myPanelActors.Add(saveCurrentItem);
            else if (!currentWasOriginallyMap)
                _saveData.defPanelActors.Add(saveCurrentItem);
        }
        if (saveReplacementItem != null)
        {
            if (replacementWasMy)
                _saveData.myPanelActors.Remove(saveReplacementItem);
            else if (replacementWasDef)
                _saveData.defPanelActors.Remove(saveReplacementItem);
            saveReplacementItem.siblingIndex = replacementItem.transform.GetSiblingIndex();
            _saveData.currentPanelActors.Add(saveReplacementItem);
        }
        int desired = System.Math.Max(0, 3 - _defActorItems.Count);
        while (_defPlaceholderItems.Count > desired)
        {
            var excess = _defPlaceholderItems[^1];
            Destroy(excess.gameObject);
            _defPlaceholderItems.RemoveAt(_defPlaceholderItems.Count - 1);
            for (int i = _saveData.defPanelActors.Count - 1; i >= 0; i--)
            {
                if (string.IsNullOrEmpty(_saveData.defPanelActors[i].playerId))
                { _saveData.defPanelActors.RemoveAt(i); break; }
            }
        }
        while (_defPlaceholderItems.Count < desired)
            AddDefPlaceholder();

        RefreshReplaceableActorScroll();
    }

    private void RefreshReplaceableActorScroll()
    {
        sv_replaceable_actor.gameObject.SetActive(false);
        LayoutRebuilder.ForceRebuildLayoutImmediate(sv_replaceable_actor.GetComponent<ScrollRect>().content);
        sv_replaceable_actor.gameObject.SetActive(true);
    }

    // ──────────── 选中状态 ────────────

    private void SelectGlobalItem(TheatreGameSetPanelItem item)
    {
        if (_globalSelectedItem != null && _globalSelectedItem != item)
            _globalSelectedItem.SetSelected(false);
        _globalSelectedItem = item;
        _globalSelectedItem.SetSelected(true);
    }

    private void ClearGlobalSelection()
    {
        if (_globalSelectedItem != null)
            _globalSelectedItem.SetSelected(false);
        _globalSelectedItem = null;
    }

    // ──────────── 页签切换 ────────────

    private void OnActorToggleValueChanged(bool isOn)
    {
        tog_actor_label.gameObject.SetActive(isOn);
        tog_actor_label1.gameObject.SetActive(!isOn);
        if (isOn)
        {
            sv_replaceable_actor.gameObject.SetActive(true);
            sv_wardrobe.gameObject.SetActive(false);
            if (resetActorTitle != null) resetActorTitle.gameObject.SetActive(true);
        }
    }

    private void OnWardrobeToggleValueChanged(bool isOn)
    {
        tog_wardrobe_label.gameObject.SetActive(isOn);
        tog_wardrobe_label1.gameObject.SetActive(!isOn);
        if (isOn)
        {
            sv_replaceable_actor.gameObject.SetActive(false);
            sv_wardrobe.gameObject.SetActive(true);
            if (resetActorTitle != null) resetActorTitle.gameObject.SetActive(false);

            // 替换后新进入 current 区的 item 需重新绑定点击事件
            foreach (var item in _currentActorItems)
            {
                var captured = item;
                item.SetClickCallback(() => OnActorItemClicked(captured));
            }

            // My actors in the My panel can also pre-select wardrobe before being swapped into Current
            bool isMyPanelActor = _globalSelectedItem != null && _myActorItems.Contains(_globalSelectedItem);
            if (_globalSelectedItem == null || (!_currentActorItems.Contains(_globalSelectedItem) && !isMyPanelActor))
            {
                ClearGlobalSelection();
                if (_currentActorItems.Count > 0)
                    SelectGlobalItem(_currentActorItems[0]);
            }
            RefreshWardrobeList();
        }
    }


    // ──────────── 衣柜列表 ────────────

    private void RefreshWardrobeList()
    {
        foreach (var old in _wardrobeItems)
        {
            old.gameObject.SetActive(false);
            Destroy(old.gameObject);
        }
        _wardrobeItems.Clear();
        _selectedWardrobeItem = null;

        if (_globalSelectedItem == null) return;
        var actorData = _globalSelectedItem.ActorData;
        txt_wardrobe.text = actorData != null ? actorData.avatarName + "的衣柜" : string.Empty;
        if (actorData == null) return;

        // 地图玩家（origin=2）没有 OCT 演员档案，用其头像 URL 作为唯一衣柜项
        _itemToSaveItem.TryGetValue(_globalSelectedItem, out var mapSaveItem);
        if (mapSaveItem?.origin == 2)
        {
            if (!string.IsNullOrEmpty(actorData.avatarURL))
            {
                var portraitClothes = new OTCAvatarClothes
                {
                    clothesIndex = 0,
                    clothesName = actorData.avatarName,
                    clothesURL = actorData.avatarURL
                };
                var mapItem = Instantiate(theatreGameSetPanelItem, actor_wardrobe_parent);
                mapItem.gameObject.SetActive(true);
                mapItem.SetWardrobeData(portraitClothes);
                _wardrobeItems.Add(mapItem);
                mapItem.SetSelected(true);
                _selectedWardrobeItem = mapItem;
                var captured = mapItem;
                mapItem.SetClickCallback(() => OnWardrobeItemClicked(captured, portraitClothes));
            }
            return;
        }

        OCTheatreActorEditorDataManager.Inst.GetActorForId(actorData.playerId, avatarInfo =>
        {
            if (avatarInfo == null) return;
            if (avatarInfo.avatarClothes == null) return;

            var usedClothesIndices = new HashSet<int>();
            foreach (var currentItem in _currentActorItems)
            {
                if (currentItem == _globalSelectedItem) continue;
                if (currentItem.ActorData?.playerId == actorData.playerId)
                    usedClothesIndices.Add(currentItem.CurrentClothesIndex);
            }

            _itemToSaveItem.TryGetValue(_globalSelectedItem, out var actorSaveItem);
            int selectedClothesIndex = actorSaveItem?.clothesIndex ?? -1;
            bool isMyActor = actorSaveItem?.origin == (int)ActorOrigin.My;
            // My演员无存档时选第一个；Default演员无存档时不选
            bool autoSelectFirst = isMyActor && selectedClothesIndex < 0;

            foreach (var clothes in avatarInfo.avatarClothes)
            {
                if (usedClothesIndices.Contains(clothes.clothesIndex)) continue;
                var item = Instantiate(theatreGameSetPanelItem, actor_wardrobe_parent);
                item.gameObject.SetActive(true);
                item.SetWardrobeData(clothes);
                _wardrobeItems.Add(item);
                if (clothes.clothesIndex == selectedClothesIndex)
                {
                    item.SetSelected(true);
                    _selectedWardrobeItem = item;
                }
                var capturedItem = item;
                var capturedClothes = clothes;
                item.SetClickCallback(() => OnWardrobeItemClicked(capturedItem, capturedClothes));
            }
            if (autoSelectFirst && _wardrobeItems.Count > 0)
            {
                _wardrobeItems[0].SetSelected(true);
                _selectedWardrobeItem = _wardrobeItems[0];
                // Sync auto-selection into saveItem so the choice is persisted on save
                if (_itemToSaveItem.TryGetValue(_globalSelectedItem, out var autoSaveItem))
                    autoSaveItem.clothesIndex = _wardrobeItems[0].CurrentClothesIndex;
            }
        });
    }   

    // ──────────── 按钮回调 ────────────

    private void OnRestoreDefaultBtnClick()
    {
        var confirmPanel = UIManager.Inst.OpenPanel<CommonConfirmWithTitlePanel>(PanelId.CommonConfirmWithTitlePanel);
        confirmPanel.SetLocalText(string.Empty, "此操作将重置所有演员阵容和衣柜装扮到初始状态，无法撤销。", "确认重置", "取消");
        confirmPanel.SetOnClickAction(confirmClick: DoRestoreDefault);
    }

    private void DoRestoreDefault()
    {
        if (_roomTheatreInfo != null)
        {
            foreach (var item in _defActorItems) { _itemToSaveItem.Remove(item); Destroy(item.gameObject); }
            _defActorItems.Clear();
            foreach (var item in _defPlaceholderItems) Destroy(item.gameObject);
            _defPlaceholderItems.Clear();
            _saveData.defPanelActors.Clear();
            for (int i = 0; i < 3; i++) _saveData.defPanelActors.Add(MakePlaceholderSaveItem(i));
            InitActorDefNullList();

            foreach (var item in _mapItems) { _itemToSaveItem.Remove(item); Destroy(item.gameObject); }
            _mapItems.Clear();
            InitRoomCurrentActorList(_roomTheatreInfo);
            InitMapActorList(_mapActors);
            ClearGlobalSelection();
            if (tog_wardrobe.isOn && _currentActorItems.Count > 0)
                SelectGlobalItem(_currentActorItems[0]);
            RefreshWardrobeList();
            TipPanel.ShowToast("已恢复默认设置");
            return;
        }

        string theatreID = OCTheatreGameController.Current?.TheatreID;
        if (string.IsNullOrEmpty(theatreID)) return;

        PlayerPrefs.DeleteKey(ActorLineupKey + theatreID);
        PlayerPrefs.Save();

        foreach (var item in _currentActorItems) Destroy(item.gameObject);
        _currentActorItems.Clear();
        foreach (var item in _myActorItems) Destroy(item.gameObject);
        _myActorItems.Clear();
        foreach (var item in _defActorItems) Destroy(item.gameObject);
        _defActorItems.Clear();
        foreach (var item in _defPlaceholderItems) Destroy(item.gameObject);
        _defPlaceholderItems.Clear();
        foreach (var item in _mapItems) Destroy(item.gameObject);
        _mapItems.Clear();
        _itemToSaveItem.Clear();

        _saveData = BuildDefaultSaveData();

        InitActorCurrentList();
        InitActorDefNullList();
        InitMapActorList(_mapActors);
        FetchMyPublishedActors();

        ClearGlobalSelection();
        if (tog_wardrobe.isOn && _currentActorItems.Count > 0)
            SelectGlobalItem(_currentActorItems[0]);
        RefreshWardrobeList();

        TipPanel.ShowToast("已恢复默认设置");
    }

    private void OnSaveBtnClick()
    {
        var confirmPanel = UIManager.Inst.OpenPanel<CommonConfirmWithTitlePanel>(PanelId.CommonConfirmWithTitlePanel);
        confirmPanel.SetLocalText(string.Empty, "保存后，调整的演员及装扮会接管对应的所有对话与演出", "保存", "取消");
        confirmPanel.SetOnClickAction(confirmClick: DoSaveActorLineup,cancelClick: CloseSelf);
    }

    private void DoSaveActorLineup()
    {
        if (_roomTheatreInfo != null)
        {
            var result = new List<TheatreActorSaveItem>();
            foreach (var panelItem in _currentActorItems)
            {
                if (_itemToSaveItem.TryGetValue(panelItem, out var si))
                {
                    si.siblingIndex = panelItem.transform.GetSiblingIndex();
                    result.Add(si);
                }
            }
            TheatreGameManager.Inst.RoomActorAssignment = result;
            MessageHelper.Broadcast(MessageName.TheatreRoomActorAssignmentChanged); 
            TipPanel.ShowToast("保存成功");
            CloseSelf();
            return;
        }

        string theatreID = OCTheatreGameController.Current?.TheatreID;
        if (string.IsNullOrEmpty(theatreID)) return;

        foreach (var item in _currentActorItems)
            if (_itemToSaveItem.TryGetValue(item, out var si)) si.siblingIndex = item.transform.GetSiblingIndex();
        foreach (var item in _myActorItems)
            if (_itemToSaveItem.TryGetValue(item, out var si)) si.siblingIndex = item.transform.GetSiblingIndex();

        _saveData.defPanelActors.Clear();
        foreach (Transform child in actor_def_parent)
        {
            var item = child.GetComponent<TheatreGameSetPanelItem>();
            if (item == null || !item.gameObject.activeSelf) continue;
            if (item.ActorData != null && _itemToSaveItem.TryGetValue(item, out var existing))
            {
                existing.siblingIndex = child.GetSiblingIndex();
                _saveData.defPanelActors.Add(existing);
            }
            else
            {
                _saveData.defPanelActors.Add(MakePlaceholderSaveItem(child.GetSiblingIndex()));
            }
        }

        PlayerPrefs.SetString(ActorLineupKey + theatreID, JsonConvert.SerializeObject(_saveData));
        PlayerPrefs.Save();
        MessageHelper.Broadcast(MessageName.OnTheatreActorLineupChanged);
        TipPanel.ShowToast("保存成功");
    }

    private TheatreGameSetPanelSaveData LoadSavedData()
    {
        string theatreID = OCTheatreGameController.Current?.TheatreID;
        if (string.IsNullOrEmpty(theatreID)) return null;
        string json = PlayerPrefs.GetString(ActorLineupKey + theatreID, string.Empty);
        if (string.IsNullOrEmpty(json)) return null;

        try
        {
            var result = JsonConvert.DeserializeObject<TheatreGameSetPanelSaveData>(json);
            if (result?.currentPanelActors != null) return result;
        }
        catch { }

        try
        {
            var oldData = JsonConvert.DeserializeObject<List<OCTheatreAvatarOc>>(json);
            if (oldData == null) return null;
            var migrated = new TheatreGameSetPanelSaveData
            {
                currentPanelActors = new List<TheatreActorSaveItem>(),
                myPanelActors = new List<TheatreActorSaveItem>(),
                defPanelActors = new List<TheatreActorSaveItem>(),
                wardrobePanelActors = new List<TheatreActorSaveItem>()
            };
            for (int i = 0; i < oldData.Count; i++)
                migrated.currentPanelActors.Add(MakeSaveItem(oldData[i], (int)ActorOrigin.Default, i));
            for (int i = 0; i < 3; i++)
                migrated.defPanelActors.Add(MakePlaceholderSaveItem(i));
            return migrated;
        }
        catch { return null; }
    }

    // ──────────── 网络请求 ────────────

    private void FetchMyPublishedActors()
    {
        OCTheatreActorEditorDataManager.Inst.FetchMyBagActors(OnFetchMyActorsSuccess);
    }

    private void OnFetchMyActorsSuccess(List<OCTheatreAvatarInfo> actorInfoList)
    {
        foreach (var actorInfo in actorInfoList)
        {
            bool alreadyInCurrent = _currentActorItems.Exists(c => c.ActorData?.playerId == actorInfo.id);
            if (alreadyInCurrent) continue;
            bool alreadyInMy = _myActorItems.Exists(m => m.ActorData?.playerId == actorInfo.id);
            if (alreadyInMy) continue;

            var saveItem = new TheatreActorSaveItem
            {
                playerId = actorInfo.id,
                avatarName = actorInfo.name,
                avatarURL = actorInfo.cover,
                clothesIndex = -1,
                origin = (int)ActorOrigin.My,
                siblingIndex = _saveData.myPanelActors.Count
            };
            _saveData.myPanelActors.Add(saveItem);

            var item = Instantiate(theatreGameSetPanelItem, actor_my_parent);
            item.gameObject.SetActive(true);
            item.SetMyData(actorInfo);
            _myActorItems.Add(item);
            _itemToSaveItem[item] = saveItem;
            var captured = item;
            item.SetClickCallback(() => OnActorItemClicked(captured));
        }
        RefreshReplaceableActorScroll();
    }

    private void InitRoomCurrentActorList(OCTheatreInfo theatreInfo)
    {
        foreach (var item in _currentActorItems)
        {
            _itemToSaveItem.Remove(item);
            Destroy(item.gameObject);
        }
        _currentActorItems.Clear();
        _saveData.currentPanelActors.Clear();

        if (theatreInfo?.avatarList == null) return;
        for (int i = 0; i < theatreInfo.avatarList.Count; i++)
        {
            var avatar = theatreInfo.avatarList[i];
            var saveItem = MakeSaveItem(avatar, (int)ActorOrigin.Default, i);
            _saveData.currentPanelActors.Add(saveItem);
            var item = Instantiate(theatreGameSetPanelItem, acotr_curent_parent);
            item.gameObject.SetActive(true);
            item.SetDataByOrigin(avatar, (int)ActorOrigin.Default);
            _currentActorItems.Add(item);
            _itemToSaveItem[item] = saveItem;
            var captured = item;
            item.SetClickCallback(() => OnActorItemClicked(captured));
        }
        RefreshReplaceableActorScroll();
    }

    private void InitMapActorList(List<TheatreActorSaveItem> mapActors)
    {
        if (!ReferenceEquals(mapActors, _mapActors))
        {
            _mapActors.Clear();
            if (mapActors != null) _mapActors.AddRange(mapActors);
        }

        foreach (Transform child in actor_map_parent) Destroy(child.gameObject);
        _mapItems.Clear();

        bool hasMapActors = mapActors != null && mapActors.Count > 0;
        actor_map_parent.gameObject.SetActive(hasMapActors);
        img_map?.gameObject.SetActive(hasMapActors);
        if (!hasMapActors) return;

        foreach (var saveItem in mapActors)
        {
            var item = Instantiate(theatreGameSetPanelItem, actor_map_parent);
            item.gameObject.SetActive(true);
            item.SetDataByOrigin(ToAvatarOc(saveItem), 2);
            _mapItems.Add(item);
            _itemToSaveItem[item] = saveItem;
            var captured = item;
            item.SetClickCallback(() => OnActorItemClicked(captured));
        }
        RefreshReplaceableActorScroll();
    }

    // ──────────── 工具方法 ────────────

    private static OCTheatreAvatarOc ToAvatarOc(TheatreActorSaveItem s) => new()
    {
        playerId = s.playerId,
        avatarName = s.avatarName,
        avatarURL = s.avatarURL,
        clothesIndex = s.clothesIndex
    };

    private static TheatreActorSaveItem MakeSaveItem(OCTheatreAvatarOc actor, int origin, int siblingIndex) => new()
    {
        playerId = actor.playerId,
        avatarName = actor.avatarName,
        avatarURL = actor.avatarURL,
        clothesIndex = actor.clothesIndex,
        origin = origin,
        siblingIndex = siblingIndex
    };

    private static TheatreActorSaveItem MakePlaceholderSaveItem(int siblingIndex) => new()
    {
        playerId = string.Empty,
        avatarName = string.Empty,
        avatarURL = string.Empty,
        clothesIndex = -1,
        siblingIndex = siblingIndex
    };
}
