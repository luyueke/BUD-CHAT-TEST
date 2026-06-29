using System;
using Com.TheFallenGames.OSA.Util.IO;
using Game.Store;
using GameData.BaseInfo;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class TheatreEditorBusinessView : MonoBehaviour
{
    [SerializeField] private Button buyBtn;
    [SerializeField] private Button closeBtn;
    [SerializeField] private Text itemName;
    [SerializeField] private Text priceText;
    [SerializeField] private Image itemImage;   // 音效场景下隐藏
    [SerializeField] private RemoteImageBehaviour itemRemoteImage;  
    [SerializeField] private Image iconImage;
    [SerializeField] private Text itemText;

    /// <summary>
    /// 显示购买确认弹窗。
    /// onBuy：用户点击购买后触发（实际支付流程由外部处理）。
    /// onClose：用户关闭弹窗（不购买）。
    /// </summary>
    public void Show(AnimMusicInfo info, Action onBuy, Action onClose = null)
    {
        gameObject.SetActive(true);

        string displayName = info?.name ?? "";
        if (itemName != null) itemName.text = displayName;
        if (itemText != null) itemText.text = displayName;
        itemImage?.gameObject.SetActive(false);

        int price = info?.paymentInfo?.price ?? 0;
        var currencyType = info?.paymentInfo?.currencyType ?? CurrencyType.Free;
        if (priceText != null)
            priceText.text = price > 0 ? $"花费{price}" : "免费";
        LoadIcon(price, currencyType);

        buyBtn?.onClick.RemoveAllListeners();
        buyBtn?.onClick.AddListener(() =>
        {
            gameObject.SetActive(false);
            TipPanel.ShowToast("购买成功");
            onBuy?.Invoke();
        });

        closeBtn?.onClick.RemoveAllListeners();
        closeBtn?.onClick.AddListener(() =>
        {
            gameObject.SetActive(false);
            onClose?.Invoke();
        });
    }

    public void Show(string name, int price, Action onBuy, Action onClose = null,
        CurrencyType currencyType = CurrencyType.PinkCoin)
    {
        gameObject.SetActive(true);

        if (itemName != null) itemName.text = name;
        if (itemText != null) itemText.text = name;
        itemImage?.gameObject.SetActive(false);

        if (priceText != null)
            priceText.text = price > 0 ? $"花费{price}" : "免费";
        LoadIcon(price, currencyType);

        buyBtn?.onClick.RemoveAllListeners();
        buyBtn?.onClick.AddListener(() =>
        {
            gameObject.SetActive(false);
            TipPanel.ShowToast("购买成功");
            onBuy?.Invoke();
        });

        closeBtn?.onClick.RemoveAllListeners();
        closeBtn?.onClick.AddListener(() =>
        {
            gameObject.SetActive(false);
            onClose?.Invoke();
        });
    }

    /// <summary>
    /// Emote 购买弹窗：显示 itemImage（PGC）或 itemRemoteImage（UGC），隐藏 itemText，价格格式"花费X"。
    /// coverUrl: UGC 封面 URL；itemId: PGC/UGC 的 id（用于图片加载和实际购买请求）。
    /// </summary>
    public void ShowForEmote(string name, int price, Action onBuy, Action onClose = null,
        CurrencyType currencyType = CurrencyType.PinkCoin,
        string coverUrl = null, string itemId = null)
    {
        gameObject.SetActive(true);

        if (itemName != null) itemName.text = name;
        itemText?.gameObject.SetActive(false);

        bool isUgc = !string.IsNullOrEmpty(coverUrl);
        bool isPgc = !isUgc && !string.IsNullOrEmpty(itemId);

        // UGC: remote image
        if (itemRemoteImage != null)
        {
            itemRemoteImage.gameObject.SetActive(isUgc);
            if (isUgc) itemRemoteImage.Load(coverUrl);
        }

        // PGC: sprite image
        if (itemImage != null)
        {
            itemImage.gameObject.SetActive(isPgc);
            if (isPgc)
            {
                var sprite = PgcUtils.LoadEmoteIcon(itemId, gameObject);
                if (sprite != null) itemImage.sprite = sprite;
            }
        }

        if (priceText != null)
            priceText.text = price > 0 ? $"花费{price}" : name;
        LoadIcon(price, currencyType);

        buyBtn?.onClick.RemoveAllListeners();
        buyBtn?.onClick.AddListener(() =>
        {
            if (price <= 0)
            {
                gameObject.SetActive(false);
                onBuy?.Invoke();
                return;
            }

            Action<bool, string, int> buyCallback = (success, reason, needNum) =>
            {
                if (success)
                {
                    gameObject.SetActive(false);
                    TipPanel.ShowToast("购买成功");
                    onBuy?.Invoke();
                }
                else if (reason == "余额不足")
                {
                    HandleInsufficientBalance(currencyType, needNum);
                }
            };

            if (isUgc)
                AssetsDataManager.BuyUgc(itemId, currencyType, price, buyCallback);
            else
                AssetsDataManager.BuyPgc(itemId, currencyType, price, buyCallback);
        });

        closeBtn?.onClick.RemoveAllListeners();
        closeBtn?.onClick.AddListener(() =>
        {
            gameObject.SetActive(false);
            onClose?.Invoke();
        });
    }

    private void HandleInsufficientBalance(CurrencyType currencyType, int needNum)
    {
        switch (currencyType)
        {
            case CurrencyType.Coin:
                var coinPanel = UIManager.Inst.OpenPanel<ExchangeCoinPanel>(PanelId.ExchangeCoinPanel);
                coinPanel?.SetData(CurrencyType.Coin, CurrencyType.Gem, needNum);
                break;
            case CurrencyType.Badge:
                var badgePanel = UIManager.Inst.OpenPanel<ExchangeCoinPanel>(PanelId.ExchangeCoinPanel);
                badgePanel?.SetData(CurrencyType.Badge, CurrencyType.Gem, needNum);
                break;
            case CurrencyType.PinkCoin:
                if (ExchangeCoinPanel.JudgePinkCoin(needNum))
                {
                    var pinkPanel = UIManager.Inst.OpenPanel<ExchangeCoinPanel>(PanelId.ExchangeCoinPanel);
                    pinkPanel?.SetData(CurrencyType.PinkCoin, CurrencyType.Gem, needNum);
                }
                break;
            case CurrencyType.Gem:
                UIManager.Inst.OpenPanel(PanelId.GetMoreGemsPanel, needNum);
                break;
        }
    }

    private void LoadIcon(int price, CurrencyType currencyType)
    {
        if (iconImage == null) return;
        if (price <= 0)
        {
            iconImage.gameObject.SetActive(false);
            return;
        }
        var sprite = PgcUtils.LoadCurrencyIcon(currencyType, gameObject);
        iconImage.sprite = sprite;
        iconImage.gameObject.SetActive(sprite != null);
    }
}
