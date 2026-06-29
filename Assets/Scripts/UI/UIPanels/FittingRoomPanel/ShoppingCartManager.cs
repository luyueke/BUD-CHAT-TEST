using System.Collections.Generic;
using Game.Store;
using Message;
using Newtonsoft.Json;
using UnityEngine;

// 购物车数据管理器：角色商品和宠物商品完全分开存储，互不干扰。
// 调用方通过 forPet 参数明确指定操作哪条列表，不再依赖 isPet 字段过滤。
public class ShoppingCartManager : GlobalInstance<ShoppingCartManager>
{
    private List<ShoppingCartItemData> _items    = new();  // 角色商品
    private List<ShoppingCartItemData> _petItems = new();  // 宠物商品
    private List<ShoppingCartItemData> _selectedItems = new();
    private readonly Dictionary<string, GoodsData> _goodsCache = new();
    private string _cachedKeyUid;

    // 角色 key
    private string Key    => "ShoppingCart_"    + AccountDataManager.Inst.UserInfo.uid;
    // 宠物 key（独立存储）
    private string PetKey => "ShoppingCartPet_" + AccountDataManager.Inst.UserInfo.uid;

    // 兼容旧调用（默认人物列表），建议传 forPet 明确区分
    public List<ShoppingCartItemData> Items => GetList(false);

    public List<ShoppingCartItemData> GetList(bool forPet)
    {
        EnsureLoadedForCurrentUser();
        return forPet ? _petItems : _items;
    }

    public override void Initialize()
    {
        base.Initialize();
        EnsureLoadedForCurrentUser();
        // 监听购买成功：无论从哪条路径购买，都自动把对应商品从购物车移除
        MessageHelper.AddListener<string>(MessageName.OnBuyUgcItemSuccess, OnBuySuccess);
    }

    public override void Release()
    {
        MessageHelper.RemoveListener<string>(MessageName.OnBuyUgcItemSuccess, OnBuySuccess);
        base.Release();
    }

    private void OnBuySuccess(string ugcId)
    {
        if (string.IsNullOrEmpty(ugcId)) return;
        // 两个列表都尝试移除（同一 id 只会在其中一个列表里）
        bool removedChar = _items.RemoveAll(it => it != null && it.id == ugcId) > 0;
        bool removedPet  = _petItems.RemoveAll(it => it != null && it.id == ugcId) > 0;
        if (removedChar || removedPet)
        {
            _goodsCache.Remove(ugcId);
            RemoveSelected(ugcId);
            if (removedChar) Save(false);
            if (removedPet)  Save(true);
            // 购物车视图若当前打开，通知刷新（监听 OnBuyUgcItemSuccess，见 ShoppingCartRootView）
        }
    }

    // 返回 true=实际新增；false=id 为空或已存在（去重）
    public bool AddItem(ShoppingCartItemData data, GoodsData goods = null)
    {
        if (data == null || string.IsNullOrEmpty(data.id)) return false;
        EnsureLoadedForCurrentUser();
        if (goods != null) _goodsCache[data.id] = goods;
        var list = data.isPet ? _petItems : _items;
        if (list.Exists(it => it != null && it.id == data.id)) return false;
        list.Add(data);
        Save(data.isPet);
        return true;
    }

    public void RemoveItem(string id, bool forPet = false)
    {
        if (string.IsNullOrEmpty(id)) return;
        EnsureLoadedForCurrentUser();
        var list = forPet ? _petItems : _items;
        if (list.RemoveAll(it => it != null && it.id == id) > 0)
        {
            _goodsCache.Remove(id);
            RemoveSelected(id);
            Save(forPet);
        }
    }

    public void Clear(bool forPet = false)
    {
        EnsureLoadedForCurrentUser();
        var list = forPet ? _petItems : _items;
        if (list.Count == 0) return;
        list.Clear();
        Save(forPet);
    }

    public GoodsData GetGoodsData(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        return _goodsCache.TryGetValue(id, out var goods) ? goods : null;
    }

    public void CacheGoods(GoodsData goods)
    {
        if (goods == null || string.IsNullOrEmpty(goods.Id)) return;
        _goodsCache[goods.Id] = goods;
    }

    public bool Contains(string id, bool forPet = false)
    {
        if (string.IsNullOrEmpty(id)) return false;
        EnsureLoadedForCurrentUser();
        var list = forPet ? _petItems : _items;
        return list.Exists(it => it != null && it.id == id);
    }

    // ---- 选中数据(批量购买用,不持久化) ----

    public IReadOnlyList<ShoppingCartItemData> SelectedItems => _selectedItems;

    public void AddSelected(ShoppingCartItemData data)
    {
        if (data == null || string.IsNullOrEmpty(data.id)) return;
        if (_selectedItems.Exists(it => it != null && it.id == data.id)) return;
        _selectedItems.Add(data);
    }

    public void RemoveSelected(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        _selectedItems.RemoveAll(it => it != null && it.id == id);
    }

    public void ClearSelected()
    {
        _selectedItems.Clear();
    }

    public bool IsSelected(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;
        return _selectedItems.Exists(it => it != null && it.id == id);
    }

    private void EnsureLoadedForCurrentUser()
    {
        var uid = AccountDataManager.Inst.UserInfo.uid;
        if (_cachedKeyUid == uid) return;
        _cachedKeyUid = uid;
        Load();
    }

    private void Save(bool forPet)
    {
        if (forPet)
        {
            PlayerPrefs.SetString(PetKey, JsonConvert.SerializeObject(_petItems));
        }
        else
        {
            PlayerPrefs.SetString(Key, JsonConvert.SerializeObject(_items));
        }
        PlayerPrefs.Save();
    }

    private void Load()
    {
        _items.Clear();
        _petItems.Clear();
        _goodsCache.Clear();
        LoadList(Key, _items);
        LoadList(PetKey, _petItems);
    }

    private void LoadList(string key, List<ShoppingCartItemData> target)
    {
        if (!PlayerPrefs.HasKey(key)) return;
        var json = PlayerPrefs.GetString(key);
        if (string.IsNullOrEmpty(json)) return;
        try
        {
            var list = JsonConvert.DeserializeObject<List<ShoppingCartItemData>>(json);
            if (list != null) target.AddRange(list);
        }
        catch { }
    }
}
