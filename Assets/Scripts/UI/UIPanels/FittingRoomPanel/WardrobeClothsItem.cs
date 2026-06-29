using System;
using Es;
using Game.Avatar;
using Game.Database;
using Game.Store;
using Newtonsoft.Json;
using UI.BaseWidgets;
using UI.Manager;
using UI.UIPanels.GashaponPanel;
using UIAgent;
using UnityEngine;
using UnityEngine.UI;

public class WardrobeClothsData
{
    public GameResData ResData;
    public long Price;
    public bool IsSelected;
}

public class WardrobeClothsItem : MonoBehaviour
{
    [SerializeField] private CButton ItemBtn;
    [SerializeField] private Image ClothImg;
    [SerializeField] private CButton ShowBtn;
    [SerializeField] private GameObject Selected;
    [SerializeField] private GameObject PriceTsf;
    [SerializeField] private Text Price;
    [SerializeField] private CButton OfficialPrice;
    [SerializeField] private Image OfficialPriceImg;
    [SerializeField] private Text OfficialPriceText;
    [SerializeField] private GameObject OfficialGashapon;
    [SerializeField] private GameObject GetOver;
    [SerializeField] private GameObject Official;

    private CharacterPartData _itemData;
    private Action<CharacterPartData> _action;
    private bool _isOfficial;
    private bool _isOwned;
    private bool _isSelected;
    private bool _isLottery;
    private string _skipData;

    public Action<string> OnDirectBuy;

    void Awake()
    {
        ItemBtn.onClick.AddListener(ItemBtnOnClick);
        ShowBtn.onClick.AddListener(ShowBtnOnClick);
        OfficialPrice.onClick.AddListener(OfficialPriceOnClick);
    }

    private void ItemBtnOnClick()
    {
        if (_itemData == null || _isOfficial || _isOwned) return;
        _isSelected = !_isSelected;
        Selected.SetActive(_isSelected);
        _action?.Invoke(_itemData);
    }

    private void ShowBtnOnClick()
    {
        if (_itemData == null || string.IsNullOrEmpty(_itemData.UId)) return;
        UIManager.Inst.OpenPanel(PanelId.AssetDetailPanel, AssetDetailType.Skin, _itemData.UId, _itemData.UgcStyle);
    }

    private void OfficialPriceOnClick()
    {
        if (_isLottery)
        {
            if (string.IsNullOrEmpty(_skipData)) return;
            if (GashaponDataManager.Inst.GetGashaponView(_skipData) == null) return;
            GashaponDataManager.Inst.JumpToGashapon(_skipData);
        }
        else
        {
            OnDirectBuy?.Invoke(_itemData.Id);
        }
    }

    public void SetSelected(bool isSelected)
    {
        //if (_isOfficial || _isOwned) return;
        _isSelected = isSelected;
        Selected.SetActive(isSelected);
    }

    public void RefreshOwnedState()
    {
        if (_itemData == null) return;
        bool newOwned;
        if (_isOfficial)
        {
            var inventoryData = BagDatabase.Inst.Select(_itemData.Id);
            newOwned = (inventoryData != null && inventoryData.OwnedNum > 0) || AssetsDataManager.IsFreeAssets(_itemData.Id);
        }
        else
        {
            newOwned = !string.IsNullOrEmpty(_itemData.UId) && AssetsDataManager.IsOwned(_itemData.UId);
        }
        if (_isOwned == newOwned) return;
        _isOwned = newOwned;
        GetOver.SetActive(_isOwned);
        if (_isOfficial)
        {
            Official.SetActive(!_isOwned);
            OfficialPrice.gameObject.SetActive(!_isOwned);
        }
        else
        {
            PriceTsf.SetActive(!_isOwned);
        }
        if (_isOwned) { _isSelected = false; Selected.SetActive(false); }
    }

    public void SetData(CharacterPartData data, Action<CharacterPartData> action, bool isSelected)
    {
        _itemData = data;
        _action = action;
        _isSelected = isSelected;

        // 重置图片，防止 OSA 复用时显示上一个 item 的残影
        ClothImg.sprite = null;
        ClothImg.enabled = false;
        
        // 先确定是否官方，其余操作按类型分支（仅一次 O(1) 表查询）
        var resData = DataTables.GetGameResData(data.Id);
        if(resData != null)
        {
            _isOfficial = resData.ResourceType == (int)GameData.PgcData.ResourceType.Avatar;
        }
        else
        {
            _isOfficial = false;
        }
        Official.SetActive(_isOfficial);
        if (_isOfficial)
        {
            PriceTsf.SetActive(false);
            // 官方：通过 data.Id（PGC ID）走本地 Atlas 接口
            PgcUtils.LoadAvatarIconAsync(data.Id, gameObject, sprite =>
            {
                if (sprite != null)
                {
                    ClothImg.sprite = sprite;
                    ClothImg.enabled = true;
                }
            });

            var inventoryData = BagDatabase.Inst.Select(data.Id);
            _isOwned = (inventoryData != null && inventoryData.OwnedNum > 0)
                       || AssetsDataManager.IsFreeAssets(data.Id);
            GetOver.SetActive(_isOwned);
            if (_isOwned)
            {
                Official.SetActive(false);
                PriceTsf.SetActive(false);
            }

            Price.text = "";
            Selected.SetActive(false);
            ShowBtn.gameObject.SetActive(false);
            bool showOfficialPrice = !_isOwned && !isSelected;
            OfficialPrice.gameObject.SetActive(showOfficialPrice);
            if (showOfficialPrice) RefreshOfficialPriceIcon(data.Id);
        }
        else
        {
            OfficialPrice.gameObject.SetActive(false);
            // 非官方：通过 data.UId（UGC ID）获取 UGC 信息（内置缓存+批量请求）
            AssetsDataManager.GetUgcInfo(data.UId, serverData =>
            {
                if (this == null || _itemData?.UId != data.UId) return;
                // 封面图
                var cover = serverData?.UgcInfo?.cover;
                if (!string.IsNullOrEmpty(cover))
                {
                    Game.Utils.GameSimpleImageDownloader.Instance.Enqueue(
                        new Game.Utils.GameSimpleImageDownloader.Request
                        {
                            url = cover,
                            onDone = result =>
                            {
                                if (this == null || _itemData?.UId != data.UId) return;
                                var texture = result.CreateTextureFromReceivedData();
                                if (texture == null) return;
                                ClothImg.sprite = Sprite.Create(texture,
                                    new Rect(0, 0, texture.width, texture.height),
                                    new Vector2(0.5f, 0.5f));
                                ClothImg.enabled = true;
                            }
                        });
                }

                // 价格（UGC 价格来自 paymentInfo，不在本地 StoreData 里）
                var payment = serverData?.skinInfo?.paymentInfo;
                Price.text = (payment != null && payment.price > 0) ? payment.price.ToString() : "";
            });

            _isOwned = !string.IsNullOrEmpty(data.UId) && AssetsDataManager.IsOwned(data.UId);
            GetOver.SetActive(_isOwned);
            PriceTsf.SetActive(!_isOwned);

            Selected.SetActive(isSelected);
            ShowBtn.gameObject.SetActive(true);
            OfficialPrice.gameObject.SetActive(false);
        }
    }

    private void RefreshOfficialPriceIcon(string pgcId)
    {
        _isLottery = false;
        bool found = false;
        if (AssetsDataManager.StoreData != null)
        {
            foreach (var product in AssetsDataManager.StoreData.ProductList)
            {
                foreach (var assetData in product.AssetDataList)
                {
                    if (assetData.PgcId != pgcId) continue;
                    found = true;
                    _isLottery = product.SkipType == Product.StoreSkipType.Lottery;
                    _skipData = product.SkipData;
                    if (!_isLottery)
                    {
                        if (assetData.Price <= 0)
                        {
                            OfficialPrice.gameObject.SetActive(false);
                            return;
                        }
                        var sprite = PgcUtils.LoadCurrencyIcon(PurchaseTypeToCurrencyType(assetData.PurchaseType), gameObject);
                        if (sprite != null) OfficialPriceImg.sprite = sprite;
                        OfficialPriceText.text = assetData.Price.ToString();
                    }
                    goto Done;
                }
            }
        }

        if (!found)
        {
            OfficialPrice.gameObject.SetActive(false);
            return;
        }

        Done:
        OfficialGashapon.SetActive(_isLottery);
        OfficialPriceImg.gameObject.SetActive(!_isLottery);
        OfficialPriceText.gameObject.SetActive(!_isLottery);
    }

    private static CurrencyType PurchaseTypeToCurrencyType(Product.PurchaseType purchaseType)
    {
        switch (purchaseType)
        {
            case Product.PurchaseType.PurchaseByCoin:        return CurrencyType.Coin;
            case Product.PurchaseType.PurchaseByBadge:       return CurrencyType.Badge;
            case Product.PurchaseType.PurchaseByGem:         return CurrencyType.Gem;
            case Product.PurchaseType.PurchaseByCreatorCoin: return CurrencyType.GreenCoin;
            case Product.PurchaseType.PurchaseByLuckyTicket: return CurrencyType.LuckyTicket;
            default:                                          return CurrencyType.None;
        }
    }
}
