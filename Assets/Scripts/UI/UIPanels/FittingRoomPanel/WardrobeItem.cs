using System;
using System.Collections.Generic;
using System.Linq;
using Com.TheFallenGames.OSA.DataHelpers;
using Es;
using Game.Avatar;
using Game.Store;
using GameData.BaseInfo;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class WardrobeItem : MonoBehaviour
{
    [SerializeField] private CButton ItemBtn;
    [SerializeField] private GameObject SelectImg;
    [SerializeField] private RawImage ActorImg;
    [SerializeField] private Text ClothName;
    [SerializeField] private CButton SelectAllBtn;
    [SerializeField] private GameObject Selected;
    [SerializeField] private WardrobeClothsAdapter _wardrobeClothsAdapter;

    private OTCAvatarClothes _clothData;
    private List<CharacterPartData> _clothsDataList = new List<CharacterPartData>();
    private readonly HashSet<string> _selectionState = new();
    private readonly Dictionary<string, long> _ugcPriceCache = new();

    public Action OnSelectionChanged;
    public Action OnItemClick;
    public Action<string> OnDirectBuyOfficialItem;

    void Awake()
    {
        SelectAllBtn.onClick.AddListener(SelectAllBtnOnClick);
        ItemBtn.onClick.AddListener(() => { SelectImg.SetActive(true); OnItemClick?.Invoke(); });
    }

    private void SelectAllBtnOnClick()
    {
        if (_clothsDataList == null || _clothsDataList.Count == 0) return;

        var eligible = _clothsDataList.Where(d =>
            DataTables.GetGameResData(d.Id)?.ResourceType != (int)GameData.PgcData.ResourceType.Avatar
            && !IsUgcOwned(d)).ToList();
        if (eligible.Count == 0) return;

        bool allSelected = eligible.All(d => _selectionState.Contains(d.Id));
        bool targetState = !allSelected;

        foreach (var data in eligible)
        {
            if (targetState) _selectionState.Add(data.Id);
            else _selectionState.Remove(data.Id);
        }
        Selected.SetActive(targetState);
        if(Selected.activeSelf)
        {
            SelectAll();
        }
        else
        {
            DeselectAll();
        }
        OnSelectionChanged?.Invoke();
    }

    public void SetData(OTCAvatarClothes clothData, bool filterByStore = true)
    {
        _clothData = clothData;
        ClothName.text = _clothData.clothesName;
        if (!string.IsNullOrEmpty(_clothData.clothesURL))
        {
            Game.Utils.GameSimpleImageDownloader.Instance.Enqueue(new Game.Utils.GameSimpleImageDownloader.Request
            {
                url = _clothData.clothesURL,
                onDone = result => {
                    ActorImg.texture = result.CreateTextureFromReceivedData();
                    ActorImg.enabled = true;
                }
            });
        }
        if (_wardrobeClothsAdapter == null || string.IsNullOrEmpty(_clothData.clothesJson)) return;
        var characterData = CharacterData.DeserializeObject(_clothData.clothesJson);
        if (characterData?.partDatas == null) return;

        if (filterByStore)
        {
            var validPgcIds = new HashSet<string>();
            if (AssetsDataManager.StoreData != null)
                foreach (var product in AssetsDataManager.StoreData.ProductList)
                    foreach (var asset in product.AssetDataList)
                        validPgcIds.Add(asset.PgcId);

            _clothsDataList = characterData.partDatas
                .Where(p => p != null && !p.IsNull() && p.Id != "12200000" && p.Id != "12300001"
                    && (!string.IsNullOrEmpty(p.UId) || validPgcIds.Contains(p.Id)))
                .ToList();
        }
        else
        {
            _clothsDataList = characterData.partDatas
                .Where(p => p != null && !p.IsNull() && p.Id != "12200000" && p.Id != "12300001")
                .ToList();
        }
        _selectionState.Clear();
        _ugcPriceCache.Clear();
        foreach (var part in _clothsDataList.Where(p => !string.IsNullOrEmpty(p.UId)))
        {
            AssetsDataManager.GetUgcInfo(part.UId, serverData =>
            {
                var payment = serverData?.skinInfo?.paymentInfo;
                if (payment != null && payment.price > 0)
                    _ugcPriceCache[part.UId] = payment.price;
            });
        }

        if (!_wardrobeClothsAdapter.IsInitialized)
            _wardrobeClothsAdapter.Init();
        _wardrobeClothsAdapter.OnItemSelected = OnClothsItemSelected;
        _wardrobeClothsAdapter.IsSelected = id => _selectionState.Contains(id);
        _wardrobeClothsAdapter.OnDirectBuy = id => OnDirectBuyOfficialItem?.Invoke(id);
        _wardrobeClothsAdapter.Data = new LazyDataHelper<CharacterPartData>(
            _wardrobeClothsAdapter,
            idx => idx < _clothsDataList.Count ? _clothsDataList[idx] : null
        );
        _wardrobeClothsAdapter.Data.ResetItems(_clothsDataList.Count);
    }

    public List<CharacterPartData> GetSelectedItems()
    {
        var result = new List<CharacterPartData>();
        foreach (var d in _clothsDataList)
        {
            if (!_selectionState.Contains(d.Id)) continue;
            var resData = DataTables.GetGameResData(d.Id);
            if (resData == null || resData.ResourceType == (int)GameData.PgcData.ResourceType.Avatar) continue;
            result.Add(d);
        }
        return result;
    }

    public long GetItemPrice(CharacterPartData part)
    {
        if (string.IsNullOrEmpty(part.UId)) return 0;
        return _ugcPriceCache.TryGetValue(part.UId, out var price) ? price : 0;
    }

    public void SetSelected(bool selected)
    {
        SelectImg.SetActive(selected);
    }

    public void SelectAll()
    {
        Selected.SetActive(true);
        if (_clothsDataList == null || _clothsDataList.Count == 0) return;

        var eligible = _clothsDataList.Where(d =>
            DataTables.GetGameResData(d.Id)?.ResourceType != (int)GameData.PgcData.ResourceType.Avatar
            && !IsUgcOwned(d)).ToList();
        if (eligible.Count == 0) return;

        foreach (var data in eligible)
            _selectionState.Add(data.Id);

        _wardrobeClothsAdapter.RefreshVisibleSelectionState();
        OnSelectionChanged?.Invoke();
    }

    public void DeselectAll()
    {
        Selected.SetActive(false);
        if (_clothsDataList == null || _clothsDataList.Count == 0) return;
        _selectionState.Clear();
        _wardrobeClothsAdapter.RefreshVisibleSelectionState();
        OnSelectionChanged?.Invoke();
    }

    private void OnClothsItemSelected(CharacterPartData data)
    {
        if (IsUgcOwned(data)) return;
        if (!_selectionState.Add(data.Id))
            _selectionState.Remove(data.Id);
        OnSelectionChanged?.Invoke();
    }

    public void RefreshOwnedState()
    {
        _selectionState.Clear();
        Selected.SetActive(false);
        _wardrobeClothsAdapter.RefreshVisibleOwnedState();
        OnSelectionChanged?.Invoke();
    }

    public bool HasPurchasableItems()
    {
        return _clothsDataList.Any(d =>
            DataTables.GetGameResData(d.Id)?.ResourceType != (int)GameData.PgcData.ResourceType.Avatar
            && !IsUgcOwned(d));
    }

    private static bool IsUgcOwned(CharacterPartData data) =>
        !string.IsNullOrEmpty(data.UId) && AssetsDataManager.IsOwned(data.UId);
}
