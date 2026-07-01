using System;
using System.Collections.Generic;
using Game.Store;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using GameData.PgcData;
using UI.BaseWidgets;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.UI;

public class ShoppingCartRootView : MonoBehaviour
{
    ShoppingCartRootItem itemTemplate;
    Transform Content;
    CButton btn_close;
    Toggle tog_selectAll;//全选状态
    GameObject card;//开了月卡就显示
    GameObject card1;//开了月卡就显示
    GameObject goldCard;//开了金卡就显示
    GameObject silverCard;//开了银卡就显示
    Text discount;//选中项打折后消耗货币总数
    Text txt_price;//选中项打折前消耗货币总数
    CButton cbtn_del;//删除选中按钮
    CButton cbtn_buy;//批量购买按钮
    GameObject cbtn_buy_disable;
    GameObject txt_dis;

    readonly List<ShoppingCartRootItem> _spawned = new();
    bool _suppressSelectAll;//为 true 时不响应 tog_selectAll 的程序化变更
    int _filterClassId;//按 ClassList 选中类型筛选购物车;0 = 显示全部
    bool _forPet;//true = 只显示宠物相关商品，false = 只显示人物相关商品

    public Action onClose;

    public void Open(bool forPet)
    {
        _forPet = forPet;
        gameObject.SetActive(true);
    }

    void Awake()
    {
        itemTemplate = GameObjectEx.FindComponentByName<ShoppingCartRootItem>(transform, "ShoppingCartRootItem");
        Content = GameObjectEx.FindChildByName(transform, "Content");
        btn_close = GameObjectEx.FindComponentByName<CButton>(transform, "btn_close");
        tog_selectAll = GameObjectEx.FindComponentByName<Toggle>(transform, "tog_selectAll");
        card = GameObjectEx.FindChildByName(transform, "card").gameObject;
        card1 = GameObjectEx.FindChildByName(transform, "card1").gameObject;

        goldCard = GameObjectEx.FindChildByName(transform, "goldCard").gameObject;
        silverCard = GameObjectEx.FindChildByName(transform, "silverCard").gameObject;
        discount = GameObjectEx.FindComponentByName<Text>(transform, "discount");
        txt_price = GameObjectEx.FindComponentByName<Text>(transform, "txt_price");
        cbtn_del = GameObjectEx.FindComponentByName<CButton>(transform, "cbtn_del");
        cbtn_buy = GameObjectEx.FindComponentByName<CButton>(transform, "cbtn_buy");
        var disableNode = GameObjectEx.FindChildByName(transform, "cbtn_buy_disable");
        if (disableNode != null) cbtn_buy_disable = disableNode.gameObject;
        var disNode = GameObjectEx.FindChildByName(transform, "txt_dis");
        if (disNode != null) { txt_dis = disNode.gameObject; txt_dis.SetActive(false); }
        btn_close.onClick.AddListener(() =>
        {
            onClose?.Invoke();
            gameObject.SetActive(false);
            // 关闭购物车后重新触发 Ugc tab，刷新列表（购买/删除后状态可能变化）
            var panel = UIManager.Inst.FindPanel<FittingRoomPanel>(PanelId.FittingRoomPanel);
            if (panel != null && FittingRoomPanel.curTab == MainTabs.Tab.Ugc)
            {
                panel.mainTabsUI.DefualtOn(MainTabs.Tab.Ugc);
            }
        });
        if (tog_selectAll != null) tog_selectAll.onValueChanged.AddListener(OnSelectAllChanged);
        if (cbtn_del != null) cbtn_del.onClick.AddListener(OnClearClick);
        if (cbtn_buy != null) cbtn_buy.onClick.AddListener(OnBuyClick);
        if (itemTemplate != null) itemTemplate.gameObject.SetActive(false);
    }

    void OnEnable()
    {
        _filterClassId = 0;
        // 购买成功后自动刷新列表 + 红点（ShoppingCartManager 已从数据里移除）
        MessageHelper.AddListener<string>(MessageName.OnBuyUgcItemSuccess, OnBuySuccessRefresh);
        Refresh();
        OnSelectAllChanged(true);
    }

    void OnDisable()
    {
        MessageHelper.RemoveListener<string>(MessageName.OnBuyUgcItemSuccess, OnBuySuccessRefresh);
    }

    private void OnBuySuccessRefresh(string ugcId)
    {
        Refresh();
        RefreshPanelRedPoint();
    }

    public void Refresh()
    {
        if (itemTemplate == null || Content == null) return;

        // 先清理已拥有的商品（购买成功、或外部获得后未及时移除的）
        var items = ShoppingCartManager.Inst.GetList(_forPet);
        for (int i = items.Count - 1; i >= 0; i--)
        {
            var it = items[i];
            if (it == null || string.IsNullOrEmpty(it.id)) continue;
            if (AssetsDataManager.IsOwned(it.id))
            {
                ShoppingCartManager.Inst.RemoveItem(it.id, _forPet);
            }
        }

        for (int i = _spawned.Count - 1; i >= 0; i--)
        {
            if (_spawned[i] != null) Destroy(_spawned[i].gameObject);
        }
        _spawned.Clear();
        //保留上次选中:selectedItems 不在此清空,item 会在 SetData 里按 IsSelected 恢复勾选

        items = ShoppingCartManager.Inst.GetList(_forPet);
        for (int i = 0; i < items.Count; i++)
        {
            if (!MatchFilter(items[i])) continue;//按 ClassList 选中类型筛选
            var clone = Instantiate(itemTemplate, Content);
            clone.gameObject.SetActive(true);
            clone.SetData(items[i]);
            clone.OnSelectChanged = OnItemSelectChanged;
            clone.OnItemClick = OnItemClicked;
            _spawned.Add(clone);
        }

        UpdateSelectAllState();
        RefreshCardStatus();
        RefreshPrice();
        if (txt_dis != null) txt_dis.SetActive(_spawned.Count == 0);
    }

    // 点击某个 item：单选模式，只显示该 item 的 img_select，其余全隐藏
    private void OnItemClicked(ShoppingCartRootItem clicked)
    {
        for (int i = 0; i < _spawned.Count; i++)
        {
            if (_spawned[i] != null)
                _spawned[i].SetTryOnShown(_spawned[i] == clicked);
        }
    }

    // 购物车打开期间,ClassList 点击某类型时调用:0=显示全部,否则按该 class 类型筛选
    public void SetClassFilter(int classId)
    {
        _filterClassId = classId;
        Refresh();
    }

    // 购物车里只有 UGC,ClassList 的类型用 PGC ResourceType 构建,故按「子类型 + PGC/UGC 同族」匹配。
    // 宠物/人物区分已在 GetList(_forPet) 层处理，这里只做分类筛选。
    private bool MatchFilter(ShoppingCartItemData item)
    {
        if (item == null) return false;
        if (_filterClassId == 0) return true;
        int classSub = _filterClassId % 10000;
        int classFamily = NormalizeFamily(_filterClassId / 10000);
        int itemFamily = NormalizeFamily(item.type);

        if (classFamily != itemFamily) return false;

        // 动作/姿势/载具：item.subType = 1000+ugcType，classSub = UgcAnimSubType.PetAll 等，
        // 两套编号不统一，同族即视为匹配（分类只区分大类，不细分子类型）。
        // 皮肤/套装等 Avatar 类：classSub = AvatarSubType，item.subType 也是 AvatarSubType，需精确匹配。
        bool isAvatarFamily = classFamily == (int)ResourceType.Avatar
                           || classFamily == (int)ResourceType.PGCPetAvatar;
        if (!isAvatarFamily) return true;

        return classSub == item.subType;
    }

    // PGC/UGC 归一到同一家族
    private static int NormalizeFamily(int resType)
    {
        switch (resType)
        {
            case (int)ResourceType.UgcAvatar: return (int)ResourceType.Avatar;
            case (int)ResourceType.UgcEmote: return (int)ResourceType.Emote;
            case (int)ResourceType.UGCPetAvatar: return (int)ResourceType.PGCPetAvatar;
            case (int)ResourceType.UgcPose: return (int)ResourceType.Pose;
            case (int)ResourceType.UgcVehicle: return (int)ResourceType.Vehicle;
            default: return resType;
        }
    }

    // 点击 tog_selectAll: 选中/取消所有 Content 里的 item
    private void OnSelectAllChanged(bool isOn)
    {
        if (_suppressSelectAll) return;
        for (int i = 0; i < _spawned.Count; i++)
        {
            if (_spawned[i] != null) _spawned[i].SetSelected(isOn);
        }
        RefreshPrice();
    }

    // 单个 item 选中变化:同步全选态并刷新总价
    private void OnItemSelectChanged()
    {
        UpdateSelectAllState();
        RefreshPrice();
    }

    // item 选中状态变化后,同步全选 toggle 的勾选态(不反向触发全选)
    private void UpdateSelectAllState()
    {
        if (tog_selectAll == null) return;
        bool allSelected = _spawned.Count > 0;
        for (int i = 0; i < _spawned.Count; i++)
        {
            if (_spawned[i] != null && !_spawned[i].IsSelected) { allSelected = false; break; }
        }
        _suppressSelectAll = true;
        tog_selectAll.isOn = allSelected;
        _suppressSelectAll = false;
    }

    // 月卡/金卡/银卡显示与折扣文本
    private void RefreshCardStatus()
    {
        var mgr = AnniversaryMonthCardMgr.Inst;
        bool monthCard = mgr.IsAnyMonthCardActive();
        bool gold = mgr.IsAnyMonthGoldCardActive();
        bool silver = mgr.IsAnyMonthSilverCardActive();
        if (card1 != null) card1.SetActive(monthCard);//开了月卡就显示
        if (goldCard != null) goldCard.SetActive(gold);//开了金卡就显示
        if (silverCard != null) silverCard.SetActive(!gold && silver);//开了银卡就显示(金卡优先)
    }

    // 刷新选中项的货币总数:discount=实付金额, txt_price=月卡折扣说明("社区币银卡享X折  已减Y")
    private void RefreshPrice()
    {
        long total = 0;
        var selected = ShoppingCartManager.Inst.SelectedItems;
        for (int i = 0; i < selected.Count; i++)
        {
            if (selected[i] != null) total += selected[i].price;
        }
        long discounted = ApplyMonthCardDiscount(total);
        long saved = total - discounted;

        // price 已是月卡折后价，直接展示，不再二次打折
        if (discount != null) discount.text = total.ToString();

        if (txt_price != null)
        {
            var mgr = AnniversaryMonthCardMgr.Inst;
            if (mgr.IsAnyMonthCardActive() && saved > 0)
            {
                string cardType = mgr.IsAnyMonthGoldCardActive() ? "金卡" : "银卡";
                string rateStr = (mgr.GetDiscountRate() * 10f).ToString("0.#");
                txt_price.text = $"社区币{cardType}享{rateStr}折  已减{saved}";
            }
            else
            {
                txt_price.text = string.Empty;
            }
        }

        bool canBuy = total > 0;
        if (cbtn_buy != null) cbtn_buy.gameObject.SetActive(canBuy);
        if (cbtn_buy_disable != null) cbtn_buy_disable.SetActive(!canBuy);
        LayoutRebuilder.ForceRebuildLayoutImmediate(card.transform.GetComponent<RectTransform>());
    }

    private long ApplyMonthCardDiscount(long price)
    {
        if (AnniversaryMonthCardMgr.Inst.IsAnyMonthCardActive())
            return (long)Mathf.Ceil(price * AnniversaryMonthCardMgr.Inst.GetDiscountRate());
        return price;
    }

    // 删除当前选中的项
    private void OnClearClick()
    {
        var selected = ShoppingCartManager.Inst.SelectedItems;
        if (selected == null || selected.Count == 0) return;
        // 先复制 id,避免 RemoveItem 修改 selectedItems 集合导致遍历出错
        var ids = new List<string>(selected.Count);
        for (int i = 0; i < selected.Count; i++)
            if (selected[i] != null) ids.Add(selected[i].id);
        for (int i = 0; i < ids.Count; i++)
            ShoppingCartManager.Inst.RemoveItem(ids[i], _forPet);
        Refresh();
        RefreshPanelRedPoint();
        if (txt_dis != null) txt_dis.SetActive(_spawned.Count == 0);
    }

    // 购物车数量变化后,同步刷新试衣间购物车按钮上的红点
    private void RefreshPanelRedPoint()
    {
        var panel = UIManager.Inst.FindPanel<FittingRoomPanel>(PanelId.FittingRoomPanel);
        if (panel != null) panel.RefreshShoppingCartRedPoint();
    }

    // 批量购买:把选中项的 id 作为 ugcIds 调 /pay/buy/ugc
    private void OnBuyClick()
    {
        var selected = ShoppingCartManager.Inst.SelectedItems;
        if (selected == null || selected.Count == 0) return;

        var ugcIds = new List<string>(selected.Count);
        for (int i = 0; i < selected.Count; i++)
        {
            var it = selected[i];
            if (it != null && !string.IsNullOrEmpty(it.id) && !ugcIds.Contains(it.id))
                ugcIds.Add(it.id);
        }
        if (ugcIds.Count == 0) return;

        // 购买前快照选中项（网络回调时 SelectedItems 可能已变）
        var snapshot = new List<ShoppingCartItemData>(selected.Count);
        for (int i = 0; i < selected.Count; i++)
            if (selected[i] != null) snapshot.Add(selected[i]);

        // 与原商城一致：发请求前本地先校验余额，不足直接弹对应充值/兑换面板
        // PinkCoin 不足时传入 ugcIds/snapshot，供确认后静默兑换再购买
        if (CheckBalanceInsufficient(snapshot, ugcIds, snapshot)) return;

        ExecuteBuy(ugcIds, snapshot);
    }

    // 发起购买请求并处理结果
    private void ExecuteBuy(List<string> ugcIds, List<ShoppingCartItemData> snapshot)
    {
        var req = new BuyReq { ugcIds = ugcIds };
        NetworkManager.Inst.SendHttpRequest(
            HttpUrlDefine.BuyUgcPay, HttpMethod.POST,
            JsonConvert.SerializeObject(req),
            response =>
            {
                AccountDataManager.Inst.BalanceInfo.Refresh();
                for (int i = 0; i < ugcIds.Count; i++)
                    MessageHelper.Broadcast(MessageName.OnBuyUgcItemSuccess, ugcIds[i]);
                RefreshPanelRedPoint();

                if (snapshot.Count == 0) return;
                var firstName = snapshot[0].name;
                var content = $"{firstName}等{snapshot.Count}件物品购买成功！";
                var panel = UIManager.Inst.OpenPanel<BuySuccessTipPanel>(PanelId.BuySuccessTipPanel);
                if (panel == null) return;
                var goods = ShoppingCartManager.Inst.GetGoodsData(snapshot[0].id);
                if (goods != null) panel.InitData(goods, content);
                else panel.InitData(content, null);
            },
            _ => AccountDataManager.Inst.BalanceInfo.Refresh());
    }

    // 校验余额是否充足；不足则弹对应充值/兑换面板并返回 true（与原商城 Buy 逻辑一致）
    // PinkCoin 不足时：弹 BuyTipsPanel，玩家确认后静默兑换再购买
    private bool CheckBalanceInsufficient(List<ShoppingCartItemData> items,
        List<string> ugcIds = null, List<ShoppingCartItemData> snapshot = null)
    {
        // 按货币类型汇总需要支付的总价（月卡折扣已体现在 item.price 里）
        var totalByCurrency = new Dictionary<int, long>();
        for (int i = 0; i < items.Count; i++)
        {
            var it = items[i];
            if (it == null) continue;
            if (!totalByCurrency.ContainsKey(it.currencyType))
                totalByCurrency[it.currencyType] = 0;
            totalByCurrency[it.currencyType] += it.price;
        }

        foreach (var kv in totalByCurrency)
        {
            var currencyType = (CurrencyType)kv.Key;
            var need = (int)kv.Value;
            var have = AccountDataManager.Inst.BalanceInfo.GetAccountCount(currencyType);
            if (have >= need) continue;

            var needNum = need - have;
            switch (currencyType)
            {
                case CurrencyType.Coin:
                    var coinPanel = UIManager.Inst.OpenPanel<ExchangeCoinPanel>(PanelId.ExchangeCoinPanel);
                    coinPanel.SetData(CurrencyType.Coin, CurrencyType.Gem, needNum);
                    break;
                case CurrencyType.Badge:
                    var badgePanel = UIManager.Inst.OpenPanel<ExchangeCoinPanel>(PanelId.ExchangeCoinPanel);
                    badgePanel.SetData(CurrencyType.Badge, CurrencyType.Gem, needNum);
                    break;
                case CurrencyType.PinkCoin:
                    // 钻石够用时弹 BuyTipsPanel，静默兑换后直接购买；否则引导充值钻石
                    if (ExchangeCoinPanel.JudgePinkCoin(needNum))
                    {
                        var tipsPanel = UIManager.Inst.OpenPanel<RechargeBuyTipsPanel>(PanelId.RechargeBuyTipsPanel);
                        tipsPanel.Init(needNum, needNum, CurrencyType.PinkCoin,
                            () => DoExchangeAndBuy(needNum, ugcIds, snapshot));
                    }
                    break;
                case CurrencyType.Gem:
                    UIManager.Inst.OpenPanel(PanelId.GetMoreGemsPanel, needNum);
                    break;
            }
            return true;
        }
        return false;
    }

    // 静默兑换粉币后执行购买（玩家无感知）
    private void DoExchangeAndBuy(int gemsToExchange,
        List<string> ugcIds, List<ShoppingCartItemData> snapshot)
    {
        if (ugcIds == null || snapshot == null) return;
        var req = new ExchangeReq
        {
            fromCurrency = (int)CurrencyType.Gem,
            toCurrency   = (int)CurrencyType.PinkCoin,
            exchangeNum  = gemsToExchange
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.exchangePay, HttpMethod.POST,
            JsonConvert.SerializeObject(req),
            _ =>
            {
                // 等余额刷新回调后再发购买请求，确保服务端兑换事务已提交
                AccountDataManager.Inst.BalanceInfo.Refresh(() => ExecuteBuy(ugcIds, snapshot));
            },
            failData =>
            {
                AccountDataManager.Inst.BalanceInfo.Refresh();
                var rsp = JsonConvert.DeserializeObject<HttpResponseRawData>(failData);
                if (rsp != null && !string.IsNullOrEmpty(rsp.rmsg))
                    TipPanel.ShowToast(rsp.rmsg);
            });
    }

    private class BuyReq { public List<string> ugcIds; }
}
