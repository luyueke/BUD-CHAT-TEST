using System;
using Game.Base;
using Game.Store;
using GameData.BaseInfo;
using GameData.PgcData;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

// 演员卡商城列表 item：结构参照 MusicStoreItemView。
// 封面由列表 Adapter 加载（RemoteIcon），这里展示实际购买消耗：
// priceText = 折后价数量（与详情区 BuyButton 同一套 GoodsData.Update() 折算，含粉币月卡折扣），
// Icon = 消耗货币图标；已拥有显示「已拥有」，价格为 0 显示「免费」。
public class ActorCardStoreItemView : MonoBehaviour
{
    // 列表首位「去创作」占位项的 ugcId 标记（由 ListView 插入），命中时本 item 显示 Design 入口
    public const string DesignEntryUgcId = "__actor_card_design_entry__";

    public static RecommendItemData CreateDesignEntryData()
    {
        return new RecommendItemData { ugcId = DesignEntryUgcId };
    }

    public static bool IsDesignEntry(RecommendItemData data)
    {
        return data != null && data.ugcId == DesignEntryUgcId;
    }

    [SerializeField] CButton Btn_View;
    [SerializeField] Image SelectBg;
    [SerializeField] Image Icon;
    [SerializeField] Text priceText;
    [SerializeField] private Color SelectColor;
    [SerializeField] private GameObject Design; //去创造
    [SerializeField] private GameObject InfoRoot;


    private RecommendItemData _curData;
    private Action<RecommendItemData> _onSelectAct;

    private void Awake()
    {
        Btn_View.onClick.AddListener(OnBtnViewClick);
    }

    public void SetData(RecommendItemData data, Action<RecommendItemData> clickAction = null)
    {
        if (data == null)
            return;

        this._curData = data;
        this._onSelectAct = clickAction;
        RefreshUI();
    }

    public void SetOwned()
    {
        if (_curData == null)
        {
            return;
        }
        if (_curData.interactInfo == null)
        {
            _curData.interactInfo = new GameData.Base.BaseInteractInfo();
        }
        _curData.interactInfo.consumed = 1;
        RefreshUI();
    }

    private void RefreshUI()
    {
        // 「去创作」占位项：只显示 Design 入口，隐藏商品信息(InfoRoot)
        bool isDesignEntry = IsDesignEntry(_curData);
        if (Design != null)
        {
            Design.SetActive(isDesignEntry);
        }
        if (InfoRoot != null)
        {
            InfoRoot.SetActive(!isDesignEntry);
        }
        if (isDesignEntry)
        {
            if (Icon != null) Icon.gameObject.SetActive(false);
            if (priceText != null) priceText.text = string.Empty;
            return;
        }

        var actorInfo = _curData?.UgcInfo as OCTheatreAvatarInfo;
        if (actorInfo == null)
        {
            return;
        }

        if (priceText == null)
        {
            return;
        }

        var isOwned = _curData?.interactInfo?.consumed == 1 ||
                      (!string.IsNullOrEmpty(actorInfo.id) && AssetsDataManager.IsOwned(actorInfo.id)) ||
                      _curData?.creatorInfo?.uid == AccountDataManager.Inst?.Uid;
        if (isOwned)
        {
            if (Icon != null) Icon.gameObject.SetActive(false);
            priceText.SetLocalText("已拥有");
            return;
        }

        var price = actorInfo.paymentInfo?.price ?? 0;
        var currencyType = actorInfo.paymentInfo?.currencyType ?? CurrencyType.PinkCoin;

        if (price > 0)
        {
            if (Icon != null)
            {
                Icon.gameObject.SetActive(true);
                Icon.sprite = PgcUtils.LoadCurrencyIcon(currencyType, gameObject);
            }
            priceText.text = Mathf.CeilToInt(price).ToString();
        }
        else
        {
            if (Icon != null) Icon.gameObject.SetActive(false);
            priceText.SetLocalText("免费");
        }
    }

    private void OnBtnViewClick()
    {
        if (_curData == null)
        {
            return;
        }
        // 「去创作」：跳演员卡创作入口（与原商城 BuyButton 的 AvatarCard 创作分支一致）
        if (IsDesignEntry(_curData))
        {
            if (!GameController.IsInHallScene())
            {
                TipPanel.ShowToast("游玩过程中无法进行创作哦");
                return;
            }
            UIManager.Inst.OpenPanel(PanelId.TheatreActorEditorMainPanel);
            return;
        }
        this._onSelectAct?.Invoke(_curData);
    }

    public void SetSelectState(bool isSelected)
    {
        SelectBg.color = isSelected ? SelectColor : Color.white;
    }
}
